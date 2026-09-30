using System;
using System.Collections.Generic;

namespace DeckRewind
{
    public interface IAgent
    {
        string Name { get; }
        void TakePlayerTurn(Battle b);

        /// 적이 행동한 직후에 묻는다. true 면 한 라운드 되감는다.
        bool ShouldRewind(Battle b, int hpLost);
    }

    /// <summary>
    /// 채택 통계. `DeadCardChecker` 가 쓴다.
    /// 분모는 "손에 있고 기력으로 낼 수 있었던 턴 수", 분자는 "실제로 낸 횟수"다.
    /// 분모를 그냥 "덱에 있었던 횟수"로 두면 뽑히지 않아서 안 쓴 것과
    /// 뽑혔는데 안 쓴 것이 섞여 아무것도 판정하지 못한다.
    /// </summary>
    public sealed class AdoptionStats
    {
        readonly Dictionary<string, int> _plays = new Dictionary<string, int>();
        readonly Dictionary<string, int> _chances = new Dictionary<string, int>();

        public void NotePlay(string cardId) => Bump(_plays, cardId);
        public void NoteChance(string cardId) => Bump(_chances, cardId);

        static void Bump(Dictionary<string, int> d, string k) => d[k] = (d.TryGetValue(k, out var v) ? v : 0) + 1;

        public int Plays(string cardId) => _plays.TryGetValue(cardId, out var v) ? v : 0;
        public int Chances(string cardId) => _chances.TryGetValue(cardId, out var v) ? v : 0;

        public int AdoptionPct(string cardId)
        {
            int c = Chances(cardId);
            return c == 0 ? -1 : Plays(cardId) * 100 / c;
        }

        public void Clear() { _plays.Clear(); _chances.Clear(); }
    }

    /// <summary>
    /// 무작위 플레이. 승률의 아래쪽 경계를 준다.
    /// 자기 난수를 따로 들고 있다 — 전투 난수를 쓰면 되감기가 판단까지 되돌려
    /// 같은 수를 무한히 반복한다.
    /// </summary>
    public sealed class RandomAgent : IAgent
    {
        readonly Rng _rng;
        public string Name => "random";

        public RandomAgent(int seed) { _rng = new Rng(seed); }

        public void TakePlayerTurn(Battle b)
        {
            for (int plays = 0; plays < Battle.MaxPlaysPerTurn; plays++)
            {
                var playable = new List<int>();
                for (int i = 0; i < b.Hand.Count; i++) if (b.CanPlay(i)) playable.Add(i);
                if (playable.Count == 0) return;
                // 다섯 번에 한 번은 남기고 턴을 넘긴다. 사람이 아끼는 것을 흉내낸 것이 아니라,
                // "전부 쏟아붓기"가 유일한 무작위 정책이 되지 않게 하려는 것이다.
                if (_rng.Range(5) == 0) return;
                b.PlayCard(playable[_rng.Range(playable.Count)]);
                if (b.Outcome != BattleOutcome.InProgress) return;
            }
        }

        public bool ShouldRewind(Battle b, int hpLost)
        {
            if (b.Outcome == BattleOutcome.PlayerLost) return _rng.Range(2) == 0;
            return hpLost > 0 && _rng.Range(10) == 0;
        }
    }

    /// <summary>되감기를 언제 쓰는가. 정책이지 밸런스가 아니라 데이터에 두지 않는다.</summary>
    public enum RewindPolicy
    {
        /// 쓰지 않는다. 대조군.
        Never,
        /// 죽었을 때만. <b>측정해서 고른 정책이다</b> — 아래 Eager 보다 승률이 훨씬 높다.
        OnDeathOnly,
        /// 크게 맞으면 바로. 기억이 남으로 뒤가 더 아파져 실제로는 손해다.
        Eager
    }

    /// <summary>
    /// 준최적 플레이. 한 턴만 내다보는 탐욕 정책이고, 완벽한 플레이가 아니다 —
    /// `RunSolvability` 가 "이길 수 있다"를 이 정책으로 증명하면 사람은 적어도 그만큼 할 수 있다.
    ///
    /// 되감기를 쓰는 근거가 이 클래스의 핵심이다. 되감기의 값은 <b>정보</b>다:
    /// 되감기 전에는 적의 의도를 모르고, 되감은 뒤에는 방금 본 의도를 안다.
    ///
    /// <b>언제 되감아야 하는지는 정하지 않고 재서 알았다.</b> 처음에는 "크게 맞으면 되감는다"로
    /// 두었는데 승률이 34% 였고, 되감기를 아예 안 쓰는 쪽이 57% 였다. 적의 기억이 전투 내내
    /// 남으므로 선불로 되감는 것은 남은 전투 전부를 비싸게 만든다. 그래서 기본 정책은
    /// "죽었을 때만" 이다 — 되감기는 여유가 아니라 마지막 수단이다.
    /// </summary>
    public sealed class GreedyAgent : IAgent
    {
        readonly GameData _data;
        public string Name => "greedy";
        public AdoptionStats Stats { get; } = new AdoptionStats();
        public RewindPolicy Policy { get; }

        /// Eager 정책이 쓰는 문턱.
        const int RewindHpLossPct = 22;
        const int RewindHpLeftPct = 65;

        public GreedyAgent(GameData data, RewindPolicy policy = RewindPolicy.OnDeathOnly)
        {
            _data = data;
            Policy = policy;
        }

        public void TakePlayerTurn(Battle b)
        {
            NoteChances(b);
            for (int plays = 0; plays < Battle.MaxPlaysPerTurn; plays++)
            {
                int best = -1, bestScore = int.MinValue;
                for (int i = 0; i < b.Hand.Count; i++)
                {
                    if (!b.CanPlay(i)) continue;
                    int s = Score(_data.Card(b.Hand[i]), b);
                    if (s > bestScore) { bestScore = s; best = i; }   // 앞선 것이 이긴다 = 결정적
                }
                if (best < 0 || bestScore <= 0) return;
                Stats.NotePlay(b.Hand[best]);
                b.PlayCard(best);
                if (b.Outcome != BattleOutcome.InProgress) return;
            }
        }

        void NoteChances(Battle b)
        {
            // 턴 시작 시점에 손에 있고 기력으로 낼 수 있는 장수를 센다 (같은 카드 두 장은 기회 둘).
            // 턴 중간에 뽑혀서 바로 나간 카드는 분모에 안 들어가므로 채택률이 100%를 넘을 수 있다.
            // 그 자체가 정보다 — "뽑히면 곧바로 나가는 카드"라는 뜻이다.
            foreach (var id in b.Hand)
                if (_data.Card(id).cost <= b.EnergyPerTurn) Stats.NoteChance(id);
        }

        /// <summary>
        /// 다음에 맞을 피해의 기대값. 본 의도가 있으면 그 값, 없으면 패턴 평균이다.
        /// <b>이 함수가 되감기의 값이 나오는 자리다</b> — 되감기가 아는 칸을 하나 늘린다.
        /// </summary>
        static int ExpectedIncoming(Battle b)
        {
            if (b.TryPeekKnownIntent(out var type, out var amount))
                return type == "attack" ? b.PreviewEnemyDamage(amount) : 0;
            return b.AverageAttack();
        }

        /// <summary>남은 턴 수의 어림값. 적 체력을 한 턴 피해량으로 나눈다. 1~6 사이로 자른다.</summary>
        static int EstimateRemainingTurns(Battle b)
        {
            int perTurn = Math.Max(1, b.EnergyPerTurn * 6 + b.Player.Strength * b.EnergyPerTurn);
            return Math.Max(1, Math.Min(6, (b.Enemy.Hp + b.Enemy.Block) / perTurn));
        }

        int Score(CardData c, Battle b)
        {
            int incoming = ExpectedIncoming(b);
            int needBlock = Math.Max(0, incoming - b.Player.Block);
            bool lethal = b.Player.Hp <= incoming;
            int hpPct = b.Player.Hp * 100 / Math.Max(1, b.Player.MaxHp);
            int s = 0;

            foreach (var e in c.effects)
            {
                switch (e.type)
                {
                    case "damage":
                        if (e.target == "enemy")
                        {
                            int d = b.PreviewPlayerDamage(e.amount);
                            s += d * 10;
                            if (d >= b.Enemy.Hp + b.Enemy.Block) s += 5000;  // 끝낼 수 있으면 끝낸다
                        }
                        else s -= e.amount * 14;
                        break;

                    case "block":
                        int useful = Math.Min(e.amount, needBlock);
                        s += useful * 13 + (e.amount - useful) * 2;
                        if (lethal && useful > 0) s += 2000;                 // 죽지 않는 쪽이 항상 먼저다
                        break;

                    case "strength":
                        // 힘은 남은 턴 수만큼 값을 하고, 이미 올려 둔 만큼 값이 준다.
                        // "이른 턴이면 좋다"로 두면 세 턴에 끝나는 전투에서도 힘부터 올려 방어가 늦고,
                        // 체감을 안 넣으면 두 장을 연달아 내고 그 턴을 버린다.
                        // 둘 다 `DeadCardChecker` 가 교정을 벌로 판정해서 알아낸 것이다.
                        s += e.amount * EstimateRemainingTurns(b) * 9 * 100
                             / (100 + b.Player.Strength * 30);
                        break;

                    case "draw":
                        s += e.amount * 20 + (b.Hand.Count <= 2 ? 34 : 0);
                        break;

                    case "energy":
                        s += e.amount * 30;
                        break;

                    case "heal":
                        int missing = b.Player.MaxHp - b.Player.Hp;
                        s += Math.Min(e.amount, missing) * (hpPct < 55 ? 17 : 3);
                        break;

                    case "weak":
                        s += e.amount * (incoming > 0 ? 20 : 6);
                        break;

                    case "burn":
                        s += e.amount * 9;
                        break;

                    case "rewind_charge":
                        // 충전은 값이 0이 되지 않는다. 다만 이미 넉넉하면 순위가 낮다.
                        s += e.amount * (b.RewindCharges == 0 ? 70 : 10);
                        break;
                }
            }
            return s - Math.Max(0, c.cost) * 7;
        }

        public bool ShouldRewind(Battle b, int hpLost)
        {
            if (Policy == RewindPolicy.Never) return false;
            // 죽음은 언제나 되돌릴 값이 있다. 대가는 남은 전투가 비싸지는 것이고, 죽으면 남은 전투가 없다.
            if (b.Outcome == BattleOutcome.PlayerLost) return true;
            if (Policy == RewindPolicy.OnDeathOnly) return false;

            if (b.RewindCharges <= 1) return false;                     // 마지막 한 장은 죽음을 위해 남긴다
            int lossPct = hpLost * 100 / Math.Max(1, b.Player.MaxHp);
            int hpPct = b.Player.Hp * 100 / Math.Max(1, b.Player.MaxHp);
            return lossPct >= RewindHpLossPct && hpPct <= RewindHpLeftPct;
        }
    }
}
