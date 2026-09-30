// 겨울 하룻밤. 셋이 차례로 온다 — 체력은 그 사이에 이어지고 회복하지 않는다.
// 순수 C#, 결정적. 같은 씨드 + 같은 덱 + 같은 정책 = 같은 기록.
//
// 먼저 만든 둘과 다른 점 하나: **표본을 한 번이라도 놓으면 Played 가 켜지고,
// 그 표본은 밭으로 돌아가지 못한다.** 카드가 사라지거나 닳는 것이 아니다 —
// 사라지는 것은 '다음 해에 자랄 수 있었던 미래'다.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridSownDeck
{
    public sealed class VisitResult
    {
        public string EnemyId;
        public int EnemyHp;
        public bool IsBoss;
        public bool Held;
        public int PlayerHpLeft;
        public int Turns;
        public bool TimedOut;
        public int CardsPlayedFirstTime;
    }

    public sealed class NightResult
    {
        public bool Held;                  // 셋을 모두 넘겼는가
        public int PlayerHpLeft;
        public List<VisitResult> Visits = new List<VisitResult>();
        public int UnplayedLeft;           // 한 번도 놓지 않은 표본 장수
        public bool EmptyDeck;
    }

    /// <summary>표본을 놓는 정책이 보는 판. 상태를 직접 바꾸지 않는다.</summary>
    public sealed class NightView
    {
        public GameData Data;
        public List<Specimen> Hand;
        public int Energy;
        public int PlayerHp;
        public int PlayerBlock;
        public int DrawCount;
        public int DiscardCount;
        public int EnemyHp;
        public int EnemyBlock;
        public int EnemyBurn;
        public int IncomingDamage;
        public int VisitIndex;
        public int VisitsLeft;
        /// <summary>아직 한 번도 놓지 않은 표본 장수 (cardId → 장수). 심을 거리가 얼마나 있는지가 여기 보인다.</summary>
        public IReadOnlyDictionary<string, int> UnplayedByCard;
        /// <summary>이번 겨울에 밭으로 돌려보낼 수 있는 칸 수. 0 이면 아껴도 쓸 데가 없다.</summary>
        public int SowPlotsAvailable;

        public int RemainingCards => Hand.Count + DrawCount;
    }

    public interface IPlayPolicy
    {
        int ChooseCard(NightView view);
    }

    public sealed class NightSim
    {
        readonly GameData _data;
        readonly BattleBalance _bal;

        public NightSim(GameData data)
        {
            _data = data;
            _bal = data.Balance.battle;
        }

        /// <summary>
        /// 하룻밤 전체. 덱의 Specimen 객체를 그대로 쓴다 — Played 가 실제로 켜진다.
        /// hpScalePct 는 해마다 커지는 적 체력(백분율 정수)이다.
        /// </summary>
        public NightResult Run(YearDeck deck, int playerHp, int hpScalePct, int sowPlotsAvailable,
                               IPlayPolicy policy, Rng rng, StringBuilder transcript = null)
        {
            var result = new NightResult { PlayerHpLeft = playerHp, Held = true };
            if (deck.Count == 0)
            {
                result.EmptyDeck = true;
                result.Held = false;
                transcript?.Append("N:empty\n");
                return result;
            }

            int hp = playerHp;
            var visits = _data.OrderedVisits;
            var hpAcc = new PercentAccumulator();

            foreach (var visit in visits)
            {
                var baseEnemy = _data.Enemy(visit.enemyId);
                int scaled = hpAcc.Apply(baseEnemy.hp, hpScalePct);
                var enemy = new EnemyDef
                {
                    id = baseEnemy.id, nameKo = baseEnemy.nameKo, nameEn = baseEnemy.nameEn,
                    hp = scaled < 1 ? 1 : scaled, pattern = baseEnemy.pattern
                };

                var vr = RunVisit(enemy, visit, deck, ref hp, sowPlotsAvailable,
                                  visits.Count - visit.order - 1, visit.order, policy, rng, transcript);
                result.Visits.Add(vr);
                if (!vr.Held) { result.Held = false; break; }
            }

            result.PlayerHpLeft = hp;
            result.UnplayedLeft = deck.Unplayed().Count;
            transcript?.Append(result.Held ? "W" : "L").Append(hp).Append('/').Append(result.UnplayedLeft).Append('\n');
            return result;
        }

        VisitResult RunVisit(EnemyDef enemy, VisitDef visit, YearDeck deck, ref int hp,
                             int sowPlots, int visitsLeft, int visitIndex,
                             IPlayPolicy policy, Rng rng, StringBuilder transcript)
        {
            var vr = new VisitResult
            {
                EnemyId = enemy.id, EnemyHp = enemy.hp, IsBoss = visit.isBoss
            };

            var draw = new List<Specimen>(deck.Cards);
            rng.Shuffle(draw);
            var hand = new List<Specimen>();
            var discard = new List<Specimen>();
            var unplayed = UnplayedCounts(deck);

            int block = 0;
            int enemyHp = enemy.hp;
            int enemyBlock = 0;
            int enemyBurn = 0;
            int enemyBonus = 0;
            int patternIndex = 0;
            int turn = 0;

            transcript?.Append("V:").Append(enemy.id).Append('/').Append(enemy.hp)
                       .Append('/').Append(deck.Count).Append('\n');

            while (true)
            {
                if (enemyHp <= 0) return Close(vr, true, hp, turn, false, transcript);
                if (hp <= 0) return Close(vr, false, hp, turn, false, transcript);
                if (turn >= _bal.maxTurnsPerBattle) return Close(vr, false, hp, turn, true, transcript);

                turn++;
                block = 0;
                int energy = _bal.energyPerTurn;
                Draw(_bal.drawPerTurn, draw, hand, discard, rng);

                int playsThisTurn = 0;
                while (playsThisTurn < _bal.maxPlaysPerTurn)
                {
                    playsThisTurn++;
                    var view = MakeView(hand, energy, hp, block, draw.Count, discard.Count,
                                        enemyHp, enemyBlock, enemyBurn, enemy, patternIndex, enemyBonus,
                                        unplayed, sowPlots, visitIndex, visitsLeft);
                    int idx = policy.ChooseCard(view);
                    if (idx < 0 || idx >= hand.Count) break;

                    var inst = hand[idx];
                    var card = _data.Card(inst.CardId);
                    if (card.cost > energy) break;

                    hand.RemoveAt(idx);
                    energy -= card.cost;

                    // 이 한 줄이 이 PoC 의 값이다. 놓으면 그 표본은 밭으로 돌아가지 못한다.
                    if (!inst.Played)
                    {
                        inst.Played = true;
                        unplayed[inst.CardId] = unplayed[inst.CardId] - 1;
                        vr.CardsPlayedFirstTime++;
                    }
                    transcript?.Append('p').Append(card.id).Append('\n');

                    foreach (var eff in card.effects)
                    {
                        switch (eff.type)
                        {
                            case "damage":
                                DealToEnemy(eff.amount, ref enemyHp, ref enemyBlock);
                                break;
                            case "block":
                                block += eff.amount;
                                break;
                            case "burn":
                                enemyBurn += eff.amount;
                                if (enemyBurn > _bal.maxBurnStacks) enemyBurn = _bal.maxBurnStacks;
                                break;
                            default:
                                throw new InvalidOperationException("모르는 효과: " + eff.type);
                        }
                    }

                    discard.Add(inst);
                    if (enemyHp <= 0) return Close(vr, true, hp, turn, false, transcript);
                }

                discard.AddRange(hand);
                hand.Clear();
                if (enemyHp <= 0) return Close(vr, true, hp, turn, false, transcript);

                enemyBlock = 0;
                if (enemyBurn > 0)
                {
                    enemyHp -= enemyBurn;      // 화상은 방어를 무시한다
                    enemyBurn--;
                    transcript?.Append('f').Append(enemyHp).Append('\n');
                    if (enemyHp <= 0) return Close(vr, true, hp, turn, false, transcript);
                }

                var act = enemy.pattern[patternIndex % enemy.pattern.Count];
                patternIndex++;
                switch (act.type)
                {
                    case "attack":
                    {
                        int dmg = act.amount + enemyBonus;
                        int absorbed = Math.Min(block, dmg);
                        block -= absorbed;
                        hp -= (dmg - absorbed);
                        break;
                    }
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

        Dictionary<string, int> UnplayedCounts(YearDeck deck)
        {
            var d = new Dictionary<string, int>();
            foreach (var c in _data.Cards) d[c.id] = 0;
            foreach (var s in deck.Cards) if (!s.Played) d[s.CardId] = d[s.CardId] + 1;
            return d;
        }

        NightView MakeView(List<Specimen> hand, int energy, int hp, int block, int drawCount, int discardCount,
                           int enemyHp, int enemyBlock, int enemyBurn, EnemyDef enemy, int patternIndex, int enemyBonus,
                           Dictionary<string, int> unplayed, int sowPlots, int visitIndex, int visitsLeft)
        {
            var next = enemy.pattern[patternIndex % enemy.pattern.Count];
            return new NightView
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
                VisitIndex = visitIndex,
                VisitsLeft = visitsLeft,
                UnplayedByCard = unplayed,
                SowPlotsAvailable = sowPlots
            };
        }

        static void DealToEnemy(int amount, ref int enemyHp, ref int enemyBlock)
        {
            int absorbed = Math.Min(enemyBlock, amount);
            enemyBlock -= absorbed;
            enemyHp -= (amount - absorbed);
        }

        static void Draw(int n, List<Specimen> draw, List<Specimen> hand, List<Specimen> discard, Rng rng)
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

        static VisitResult Close(VisitResult vr, bool held, int hp, int turns, bool timedOut, StringBuilder t)
        {
            t?.Append(held ? 'h' : 'x').Append(hp).Append('\n');
            vr.Held = held; vr.PlayerHpLeft = hp; vr.Turns = turns; vr.TimedOut = timedOut;
            return vr;
        }
    }
}
