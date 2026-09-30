// 전투 상태 전이. 순수 C#, 결정적. 같은 씨드 + 같은 정책 = 같은 기록.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridHarvestDeck
{
    public sealed class BattleResult
    {
        public bool PlayerWon;
        public int PlayerHpLeft;
        public int Turns;
        public bool TimedOut;
    }

    /// <summary>카드 놓기 정책이 보는 전투. 상태를 직접 바꾸지 않는다.</summary>
    public sealed class BattleView
    {
        public GameData Data;
        public List<string> Hand;
        public int Energy;
        public int PlayerHp;
        public int PlayerBlock;
        public int DrawCount;
        public int DiscardCount;
        public int EnemyHp;
        public int EnemyBlock;
        public int EnemyBurn;
        /// <summary>적의 다음 행동이 줄 피해(ramp 포함). 공격이 아니면 0.</summary>
        public int IncomingDamage;

        public int RemainingCards => Hand.Count + DrawCount;
    }

    public interface IPlayPolicy
    {
        /// <summary>놓을 카드의 손 안 인덱스. -1 이면 턴을 끝낸다.</summary>
        int ChooseCard(BattleView view);

        /// <summary>버릴 카드의 손 안 인덱스. '정리'가 부른다.</summary>
        int ChooseDiscard(BattleView view);
    }

    public sealed class BattleSim
    {
        readonly GameData _data;
        readonly BattleBalance _bal;

        public BattleSim(GameData data)
        {
            _data = data;
            _bal = data.Balance.battle;
        }

        public BattleResult Run(EnemyDef enemy, IReadOnlyList<string> deck, int playerHp,
                                IPlayPolicy policy, Rng rng, StringBuilder transcript = null)
        {
            var draw = new List<string>(deck);
            rng.Shuffle(draw);
            var hand = new List<string>();
            var discard = new List<string>();

            int hp = playerHp;
            int block = 0;
            int enemyHp = enemy.hp;
            int enemyBlock = 0;
            int enemyBurn = 0;
            int enemyBonus = 0;
            int patternIndex = 0;
            int turn = 0;

            transcript?.Append("B:").Append(enemy.id).Append('/').Append(deck.Count).Append('\n');

            while (true)
            {
                if (enemyHp <= 0) return Done(true, hp, turn, false, transcript);
                if (hp <= 0) return Done(false, hp, turn, false, transcript);
                if (turn >= _bal.maxTurnsPerBattle) return Done(false, hp, turn, true, transcript);

                turn++;
                block = 0;
                int energy = _bal.energyPerTurn;
                Draw(_bal.drawPerTurn, draw, hand, discard, rng);

                // ── 플레이어 턴 ────────────────────────────────────────
                // 놓은 횟수에 상한을 둔다. 0코스트 순환 카드 하나면 한 턴이 끝나지 않는다 —
                // 실제로 한 번 겪었다: '정리'(0코스트, 뽑기)가 버림 더미에서 다시 섞여 돌아왔다.
                int playsThisTurn = 0;
                while (playsThisTurn < _bal.maxPlaysPerTurn)
                {
                    playsThisTurn++;
                    var view = MakeView(hand, energy, hp, block, draw.Count, discard.Count,
                                        enemyHp, enemyBlock, enemyBurn, enemy, patternIndex, enemyBonus);
                    int idx = policy.ChooseCard(view);
                    if (idx < 0 || idx >= hand.Count) break;

                    var card = _data.Card(hand[idx]);
                    if (card.cost > energy) break;

                    hand.RemoveAt(idx);
                    energy -= card.cost;
                    transcript?.Append('p').Append(card.id).Append('\n');

                    foreach (var eff in card.effects)
                    {
                        switch (eff.type)
                        {
                            case "damage":
                                DealToEnemy(eff.amount, ref enemyHp, ref enemyBlock);
                                break;
                            case "damage_per_remaining":
                                DealToEnemy(eff.amount * (draw.Count + hand.Count), ref enemyHp, ref enemyBlock);
                                break;
                            case "block":
                                block += eff.amount;
                                break;
                            case "burn":
                                // 상한이 없으면 화상이 2차식으로 커져 '고추만 심기'가 지배 전략이 된다
                                enemyBurn += eff.amount;
                                if (enemyBurn > _bal.maxBurnStacks) enemyBurn = _bal.maxBurnStacks;
                                break;
                            case "draw":
                                Draw(eff.amount, draw, hand, discard, rng);
                                break;
                            case "discard":
                            case "exhaust":
                                // exhaust 는 이번 전투에서 아예 뺀다 — 덱을 얇게 만드는 유일한 수단이다.
                                for (int k = 0; k < eff.amount && hand.Count > 0; k++)
                                {
                                    var dv = MakeView(hand, energy, hp, block, draw.Count, discard.Count,
                                                      enemyHp, enemyBlock, enemyBurn, enemy, patternIndex, enemyBonus);
                                    int di = policy.ChooseDiscard(dv);
                                    if (di < 0 || di >= hand.Count) di = 0;
                                    transcript?.Append(eff.type == "exhaust" ? 'x' : 'd').Append(hand[di]).Append('\n');
                                    if (eff.type == "discard") discard.Add(hand[di]);
                                    hand.RemoveAt(di);
                                }
                                break;
                            default:
                                throw new InvalidOperationException("모르는 효과: " + eff.type);
                        }
                    }

                    discard.Add(card.id);
                    if (enemyHp <= 0) return Done(true, hp, turn, false, transcript);
                }

                // 턴 끝: 손을 버린다
                discard.AddRange(hand);
                hand.Clear();
                if (enemyHp <= 0) return Done(true, hp, turn, false, transcript);

                // ── 적 턴 ──────────────────────────────────────────────
                enemyBlock = 0;
                if (enemyBurn > 0)
                {
                    enemyHp -= enemyBurn;      // 화상은 방어를 무시한다
                    enemyBurn--;
                    transcript?.Append('f').Append(enemyHp).Append('\n');
                    if (enemyHp <= 0) return Done(true, hp, turn, false, transcript);
                }

                var act = enemy.pattern[patternIndex % enemy.pattern.Count];
                patternIndex++;
                switch (act.type)
                {
                    case "attack":
                        int dmg = act.amount + enemyBonus;
                        int absorbed = Math.Min(block, dmg);
                        block -= absorbed;
                        hp -= (dmg - absorbed);
                        break;
                    case "block":
                        enemyBlock += act.amount;
                        break;
                    case "ramp":
                        enemyBonus += act.amount;
                        break;
                    default:
                        throw new InvalidOperationException("모르는 적 행동: " + act.type);
                }
                transcript?.Append('e').Append(hp).Append(',').Append(enemyHp).Append('\n');
            }
        }

        BattleView MakeView(List<string> hand, int energy, int hp, int block, int drawCount, int discardCount,
                            int enemyHp, int enemyBlock, int enemyBurn, EnemyDef enemy, int patternIndex, int enemyBonus)
        {
            var next = enemy.pattern[patternIndex % enemy.pattern.Count];
            return new BattleView
            {
                Data = _data,
                Hand = hand,
                Energy = energy,
                PlayerHp = hp,
                PlayerBlock = block,
                DrawCount = drawCount,
                DiscardCount = discardCount,
                EnemyHp = enemyHp,
                EnemyBlock = enemyBlock,
                EnemyBurn = enemyBurn,
                IncomingDamage = next.type == "attack" ? next.amount + enemyBonus : 0
            };
        }

        static void DealToEnemy(int amount, ref int enemyHp, ref int enemyBlock)
        {
            int absorbed = Math.Min(enemyBlock, amount);
            enemyBlock -= absorbed;
            enemyHp -= (amount - absorbed);
        }

        static void Draw(int n, List<string> draw, List<string> hand, List<string> discard, Rng rng)
        {
            for (int i = 0; i < n; i++)
            {
                if (draw.Count == 0)
                {
                    if (discard.Count == 0) return;
                    draw.AddRange(discard);
                    discard.Clear();
                    rng.Shuffle(draw);
                }
                hand.Add(draw[draw.Count - 1]);
                draw.RemoveAt(draw.Count - 1);
            }
        }

        static BattleResult Done(bool won, int hp, int turns, bool timedOut, StringBuilder t)
        {
            t?.Append(won ? "W" : "L").Append(hp).Append('\n');
            return new BattleResult { PlayerWon = won, PlayerHpLeft = hp, Turns = turns, TimedOut = timedOut };
        }
    }
}
