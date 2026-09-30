// 한 번의 침입. 순수 C#, 결정적. 같은 씨드 + 같은 덱 + 같은 정책 = 같은 기록.
//
// hybrid-harvest-deck 과 다른 점 둘:
//   1) 카드를 놓으면 남은 횟수가 준다. 0 이 되면 이 판이 아니라 **덱에서 영영** 빠진다
//   2) '정리'의 소멸은 이 판만이 아니라 영구다 — 영속 덱을 얇게 만드는 유일한 수단
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridSiegeSeasons
{
    public sealed class BattleResult
    {
        public bool PlayerWon;
        public int PlayerHpLeft;
        public int Turns;
        public bool TimedOut;
        /// <summary>이 판에서 덱을 떠난 카드들 (다 닳았거나 '정리'로 없앴다).</summary>
        public List<CardInstance> LeftTheDeck = new List<CardInstance>();
    }

    /// <summary>카드 놓기 정책이 보는 침입. 상태를 직접 바꾸지 않는다.</summary>
    public sealed class BattleView
    {
        public GameData Data;
        public List<CardInstance> Hand;
        public int Energy;
        public int PlayerHp;
        public int PlayerBlock;
        public int DrawCount;
        public int DiscardCount;
        public int EnemyHp;
        public int EnemyBlock;
        public int EnemyBurn;
        public int IncomingDamage;
        /// <summary>덱에 남은 총 사용 횟수 (카드 종류별). 아낄지 말지를 여기서 판단한다.</summary>
        public IReadOnlyDictionary<string, int> DeckUsesByCard;

        public int RemainingCards => Hand.Count + DrawCount;
    }

    public interface IPlayPolicy
    {
        int ChooseCard(BattleView view);
        int ChooseExhaust(BattleView view);
    }

    public sealed class SiegeBattleSim
    {
        readonly GameData _data;
        readonly BattleBalance _bal;

        public SiegeBattleSim(GameData data)
        {
            _data = data;
            _bal = data.Balance.battle;
        }

        /// <summary>
        /// deck 의 카드 객체를 그대로 쓴다 — 남은 횟수가 실제로 깎인다.
        /// 덱을 떠난 카드는 result.LeftTheDeck 에 담기고, 부르는 쪽이 덱에서 뺀다.
        /// </summary>
        public BattleResult Run(EnemyDef enemy, StandingDeck deck, int playerHp,
                                IPlayPolicy policy, Rng rng, StringBuilder transcript = null)
        {
            var result = new BattleResult();
            var draw = new List<CardInstance>(deck.Cards);
            rng.Shuffle(draw);
            var hand = new List<CardInstance>();
            var discard = new List<CardInstance>();
            var usesLeft = deck.UsesByCard();

            int hp = playerHp;
            int block = 0;
            int enemyHp = enemy.hp;
            int enemyBlock = 0;
            int enemyBurn = 0;
            int enemyBonus = 0;
            int patternIndex = 0;
            int turn = 0;

            transcript?.Append("S:").Append(enemy.id).Append('/').Append(enemy.hp)
                       .Append('/').Append(deck.Count).Append('\n');

            while (true)
            {
                if (enemyHp <= 0) return Done(result, true, hp, turn, false, transcript);
                if (hp <= 0) return Done(result, false, hp, turn, false, transcript);
                if (turn >= _bal.maxTurnsPerBattle) return Done(result, false, hp, turn, true, transcript);

                turn++;
                block = 0;
                int energy = _bal.energyPerTurn;
                Draw(_bal.drawPerTurn, draw, hand, discard, rng);

                int playsThisTurn = 0;
                while (playsThisTurn < _bal.maxPlaysPerTurn)
                {
                    playsThisTurn++;
                    var view = MakeView(hand, energy, hp, block, draw.Count, discard.Count,
                                        enemyHp, enemyBlock, enemyBurn, enemy, patternIndex, enemyBonus, usesLeft);
                    int idx = policy.ChooseCard(view);
                    if (idx < 0 || idx >= hand.Count) break;

                    var inst = hand[idx];
                    var card = _data.Card(inst.CardId);
                    if (card.cost > energy) break;

                    hand.RemoveAt(idx);
                    energy -= card.cost;

                    // 닳는다. 이것이 이 PoC 의 손잡이다.
                    inst.Uses--;
                    usesLeft[inst.CardId] = usesLeft[inst.CardId] - 1;
                    transcript?.Append('p').Append(card.id).Append('#').Append(inst.Uses).Append('\n');

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
                                enemyBurn += eff.amount;
                                if (enemyBurn > _bal.maxBurnStacks) enemyBurn = _bal.maxBurnStacks;
                                break;
                            case "draw":
                                Draw(eff.amount, draw, hand, discard, rng);
                                break;
                            case "repair":
                            {
                                // 손패에서 가장 닳은 카드를 손질한다.
                                // **자기와 같은 카드는 고르지 않는다** — 손질 두 장이 서로를 고치면
                                // 쓰는 횟수보다 도는 횟수가 많아져 무한 증식 고리가 된다.
                                CardInstance target = null;
                                foreach (var h in hand)
                                {
                                    if (h.CardId == card.id) continue;
                                    if (h.Uses >= h.MaxUses) continue;
                                    if (target == null || h.Uses < target.Uses) target = h;
                                }
                                if (target != null)
                                {
                                    int before = target.Uses;
                                    target.Uses = System.Math.Min(target.MaxUses, target.Uses + eff.amount);
                                    usesLeft[target.CardId] = usesLeft[target.CardId] + (target.Uses - before);
                                    transcript?.Append('r').Append(target.CardId).Append('+')
                                               .Append(target.Uses - before).Append('\n');
                                }
                                break;
                            }
                            case "exhaust":
                                // 영속 덱에서 영구히 없앤다 — 덱을 얇게 만드는 유일한 수단
                                for (int k = 0; k < eff.amount && hand.Count > 0; k++)
                                {
                                    var ev = MakeView(hand, energy, hp, block, draw.Count, discard.Count,
                                                      enemyHp, enemyBlock, enemyBurn, enemy, patternIndex, enemyBonus, usesLeft);
                                    int ei = policy.ChooseExhaust(ev);
                                    if (ei < 0 || ei >= hand.Count) ei = 0;
                                    var gone = hand[ei];
                                    transcript?.Append('x').Append(gone.CardId).Append('\n');
                                    usesLeft[gone.CardId] = usesLeft[gone.CardId] - gone.Uses;
                                    result.LeftTheDeck.Add(gone);
                                    hand.RemoveAt(ei);
                                }
                                break;
                            default:
                                throw new InvalidOperationException("모르는 효과: " + eff.type);
                        }
                    }

                    if (inst.Broken)
                    {
                        transcript?.Append('b').Append(inst.CardId).Append('\n');
                        result.LeftTheDeck.Add(inst);      // 부서졌다. 버림 더미로도 가지 않는다
                    }
                    else discard.Add(inst);

                    if (enemyHp <= 0) return Done(result, true, hp, turn, false, transcript);
                }

                discard.AddRange(hand);
                hand.Clear();
                if (enemyHp <= 0) return Done(result, true, hp, turn, false, transcript);

                enemyBlock = 0;
                if (enemyBurn > 0)
                {
                    enemyHp -= enemyBurn;      // 화상은 방어를 무시한다
                    enemyBurn--;
                    transcript?.Append('f').Append(enemyHp).Append('\n');
                    if (enemyHp <= 0) return Done(result, true, hp, turn, false, transcript);
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

        BattleView MakeView(List<CardInstance> hand, int energy, int hp, int block, int drawCount, int discardCount,
                            int enemyHp, int enemyBlock, int enemyBurn, EnemyDef enemy, int patternIndex, int enemyBonus,
                            Dictionary<string, int> usesLeft)
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
                IncomingDamage = next.type == "attack" ? next.amount + enemyBonus : 0,
                DeckUsesByCard = usesLeft
            };
        }

        static void DealToEnemy(int amount, ref int enemyHp, ref int enemyBlock)
        {
            int absorbed = Math.Min(enemyBlock, amount);
            enemyBlock -= absorbed;
            enemyHp -= (amount - absorbed);
        }

        static void Draw(int n, List<CardInstance> draw, List<CardInstance> hand, List<CardInstance> discard, Rng rng)
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

        static BattleResult Done(BattleResult r, bool won, int hp, int turns, bool timedOut, StringBuilder t)
        {
            t?.Append(won ? "W" : "L").Append(hp).Append('/').Append(r.LeftTheDeck.Count).Append('\n');
            r.PlayerWon = won; r.PlayerHpLeft = hp; r.Turns = turns; r.TimedOut = timedOut;
            return r;
        }
    }
}
