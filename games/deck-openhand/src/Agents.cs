using System;
using System.Collections.Generic;

namespace DeckOpenhand
{
    public interface IAgent
    {
        string Name { get; }
        void TakePlayerTurn(Battle b);
    }

    /// <summary>
    /// 채택 통계. `DeadCardChecker` 가 쓴다.
    /// 분모는 "턴 시작에 손에 있고 기력으로 낼 수 있었던 장수", 분자는 "실제로 낸 횟수"다.
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
    }

    /// <summary>무작위 플레이. 승률의 아래쪽 경계를 준다. 보이는 순서를 전혀 쓰지 않는다.</summary>
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
                if (_rng.Range(5) == 0) return;
                b.PlayCard(playable[_rng.Range(playable.Count)]);
                if (b.Outcome != BattleOutcome.InProgress) return;
            }
        }
    }

    /// <summary>순서를 읽는가. `Blind` 는 이 PoC의 규칙을 끈 대조군이다.</summary>
    public enum Sight
    {
        /// 보이는 다섯 수를 그대로 쓴다. 이 PoC의 기본.
        Open,
        /// 다음 수를 모르는 것처럼 패턴 평균으로만 판단한다. 규칙이 값을 하는지 재는 대조군.
        Blind
    }

    /// <summary>
    /// 준최적 플레이. 한 턴만 내다보는 탐욕 정책이고, 완벽한 플레이가 아니다 —
    /// `RunSolvability` 가 "이길 수 있다"를 이 정책으로 증명하면 사람은 적어도 그만큼 할 수 있다.
    ///
    /// <b>이 PoC의 규칙이 값을 하는 자리는 딱 하나다</b>: 다음에 맞을 피해를 정확히 안다는 것.
    /// 그래서 방어를 정확히 사고(넘치게 사지 않고), 가림막을 가장 큰 공격에 쓰고,
    /// 역순으로 큰 공격을 뒤로 밀 수 있다. `Sight.Blind` 는 그걸 다 끈 상태다.
    /// </summary>
    public sealed class GreedyAgent : IAgent
    {
        readonly GameData _data;
        public string Name => _sight == Sight.Open ? "greedy" : "greedy-blind";
        public AdoptionStats Stats { get; } = new AdoptionStats();

        readonly Sight _sight;

        public GreedyAgent(GameData data, Sight sight = Sight.Open)
        {
            _data = data;
            _sight = sight;
        }

        public void TakePlayerTurn(Battle b)
        {
            foreach (var id in b.Hand)
                if (_data.Card(id).cost <= b.EnergyPerTurn) Stats.NoteChance(id);

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

        /// <summary>다음에 맞을 피해. Open 이면 정확한 값, Blind 면 패턴 평균이다.</summary>
        int ExpectedIncoming(Battle b)
        {
            if (_sight == Sight.Blind) return b.AverageAttack();
            var next = b.NextIntent;
            return next.type == "attack" ? b.PreviewEnemyDamage(next.amount) : 0;
        }

        /// <summary>보이는 창 안에서 가장 큰 공격. 가림막·역순을 어디에 쓸지의 근거다.</summary>
        int BiggestAttackAhead(Battle b, out int position)
        {
            position = -1;
            if (_sight == Sight.Blind) return b.AverageAttack();
            var window = b.PeekIntents(b.PeekWindow);
            int biggest = 0;
            for (int i = 0; i < window.Length; i++)
            {
                if (window[i].type != "attack") continue;
                int d = b.PreviewEnemyDamage(window[i].amount);
                if (d > biggest) { biggest = d; position = i; }
            }
            return biggest;
        }

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
            int biggestPos;
            int biggest = BiggestAttackAhead(b, out biggestPos);
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
                        if (lethal && useful > 0) s += 2000;
                        break;

                    case "brace":
                        // 다음 수가 공격일 때만 값이 크다. 아니면 amount 만 남는다.
                        int braced = e.amount + b.BraceValue();
                        int braceUseful = Math.Min(braced, needBlock);
                        s += braceUseful * 13 + (braced - braceUseful) * 2;
                        if (lethal && braceUseful > 0) s += 2000;
                        break;

                    case "echo":
                        int echoDamage = b.PreviewPlayerDamage(e.amount * b.AttacksInWindow);
                        s += echoDamage * 10;
                        if (echoDamage >= b.Enemy.Hp + b.Enemy.Block) s += 5000;
                        break;

                    case "skip_intent":
                        // 가장 큰 공격이 바로 다음일 때만 값이 크다. 아무 때나 쓰면 낭비다.
                        // 값의 단위를 방어와 같게(13) 두어야 "막을까 지울까"가 같은 자에서 비교된다.
                        // 이미 사 둔 방어가 덮는 몫은 빼고 센다 — 안 빼면 방어와 가림막을 겹쳐 쓴다.
                        s += biggestPos == 0
                             ? Math.Max(0, biggest - b.Player.Block) * 13
                             : biggest;
                        break;

                    case "swap_intent":
                        // 큰 공격을 뒤로 미는 값. 다음이 큰 공격이고 그다음이 아니면 이득이다.
                        if (_sight == Sight.Open && b.PeekWindow >= 2)
                        {
                            var w = b.PeekIntents(2);
                            int now = w[0].type == "attack" ? b.PreviewEnemyDamage(w[0].amount) : 0;
                            int later = w[1].type == "attack" ? b.PreviewEnemyDamage(w[1].amount) : 0;
                            int gain = now - later;
                            // 지금 막을 수 있는 양보다 큰 공격을 뒤로 미는 만큼만 값이다.
                            s += Math.Max(0, gain - b.Player.Block) * 11;
                        }
                        break;

                    case "strength":
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
                }
            }
            return s - Math.Max(0, c.cost) * 7;
        }
    }
}
