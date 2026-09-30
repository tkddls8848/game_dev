using System;
using System.Collections.Generic;

namespace DeckAttrition
{
    public interface IAgent
    {
        string Name { get; }
        void TakePlayerTurn(Battle b);
    }

    /// <summary>
    /// 닳는 활자를 <b>언제 쓰는가</b>. 이 PoC의 핵 검사기(`HoardingIsNotOptimal`)가 재는 축이다.
    /// 셋의 차이는 오직 이것뿐이다 — 카드를 고르는 나머지 판단은 완전히 같은 코드를 쓴다.
    /// 그래야 승률 차이가 <b>정책의 차이</b>라고 말할 수 있다.
    /// </summary>
    public enum AttritionPolicy
    {
        /// 끝까지 아낀다. 보스 전투에 가서야 활자를 꺼낸다.
        Hoard,
        /// 즉시 쓴다. 손에 오면 첫 전투부터 찍는다.
        SpendNow,
        /// 적절히 쓴다. 죽을 자리 · 끝낼 수 있는 자리 · 막바지에만 꺼낸다.
        Measured
    }

    /// <summary>
    /// 채택 통계. `DeadCardChecker` 가 쓴다.
    /// 분모는 "손에 있고 기력으로 낼 수 있었던 턴 수", 분자는 "실제로 낸 횟수"다.
    /// 분모를 "덱에 있었던 횟수"로 두면 뽑히지 않아서 안 쓴 것과 뽑혔는데 안 쓴 것이 섞인다.
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
    /// 자기 난수를 따로 들고 있다 — 전투 난수를 빌려 쓰면 덱을 섞는 순서까지 바꿔
    /// "무작위 정책"이 아니라 "다른 게임"을 재게 된다.
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
                // 절반의 확률로 턴을 접는다. 그래야 "기력을 다 쓰는 것"조차 정책이 아니게 된다.
                if (_rng.Range(2) == 0) return;
                b.PlayCard(playable[_rng.Range(playable.Count)]);
                if (b.Outcome != BattleOutcome.InProgress) return;
            }
        }
    }

    /// <summary>
    /// 식자공. 준최적 탐욕 정책 + <see cref="AttritionPolicy"/> 하나.
    ///
    /// <b>셋이 같은 코드를 쓴다.</b> 갈라지는 자리는 <see cref="AllowsConsumable"/> 하나뿐이고,
    /// 나머지(무엇을 먼저 낼지 · 막을지 때릴지)는 동일하다. 정책마다 다른 탐욕 규칙을 쓰면
    /// 승률 차이가 규칙 때문인지 정책 때문인지 말할 수 없게 되고, 그러면 핵 검사기가 아무것도
    /// 증명하지 못한다.
    /// </summary>
    public sealed class TypesetterAgent : IAgent
    {
        readonly GameData _data;
        readonly AttritionPolicy _policy;
        readonly int _hpTriggerPct;
        readonly int _endgameBattlesLeft;
        readonly int _incomingTriggerPct;

        public AdoptionStats Stats { get; } = new AdoptionStats();

        public string Name => _policy.ToString();
        public AttritionPolicy Policy => _policy;

        public TypesetterAgent(GameData data, AttritionPolicy policy)
        {
            _data = data;
            _policy = policy;
            _hpTriggerPct = data.Balance.measuredHpTriggerPct;
            _endgameBattlesLeft = data.Balance.measuredEndgameBattlesLeft;
            _incomingTriggerPct = data.Balance.measuredIncomingTriggerPct;
        }

        // ---------- 정책: 닳는 활자를 지금 꺼내도 되는가 ----------

        /// <summary>
        /// 셋이 갈라지는 단 한 자리.
        ///
        /// <b>"적절히"는 언제가 아니라 무엇을 쓰는가이기도 하다.</b> 큰 활자를 잡졸에게 찍는 것과
        /// 큰 게 날아올 때 조판틀로 받는 것은 같은 "지금 쓴다"가 아니다. 앞의 것은 낭비고
        /// 뒤의 것은 체력으로 낼 것을 활자로 내는 교환이다. 그래서 이 판단은 카드마다 묻는다.
        ///
        /// 세 정책의 차이는 여기뿐이고, 무엇을 먼저 낼지는 <see cref="ChooseCard"/> 의 같은 코드가 정한다.
        /// </summary>
        bool AllowsConsumable(Battle b, CardData c, Preview p)
        {
            switch (_policy)
            {
                case AttritionPolicy.SpendNow:
                    return true;

                case AttritionPolicy.Hoard:
                    // 끝까지 아낀다. 보스에서만 꺼낸다 — 계획서 §3이 경고한 "지루한 최적해"가
                    // 실제로 최적인지 재는 것이 이 정책의 존재 이유다.
                    return b.IsBoss;

                case AttritionPolicy.Measured:
                    // 막바지. 아껴 봐야 쓸 데가 없다.
                    if (b.BattlesLeftAfter <= _endgameBattlesLeft) return true;
                    // 이 한 장이 판을 끝낸다 / 이번 턴에 죽는 것을 막는다.
                    if (p.Finishes || p.Saves) return true;
                    // 막는 쪽: 큰 게 오는데 막을 수 있으면 막는다. 체력은 회차 자원이므로
                    // 여기서 아낀 활자는 나중에 체력으로 갚아야 한다.
                    if (p.Block > 0 && p.Unblocked > 0
                        && p.Unblocked * 100 >= b.Player.Hp * _incomingTriggerPct
                        && p.Block * 2 >= p.Unblocked) return true;
                    // 회복 쪽: 체력이 내려갔고 회복이 헛되이 넘치지 않을 때만.
                    if (p.Heal > 0 && b.Player.HpPct <= _hpTriggerPct
                        && b.Player.MaxHp - b.Player.Hp >= p.Heal) return true;
                    // 다시 붓는 쪽: 이건 쓰는 것이 아니라 <b>옮기는 것</b>이다. 뭉개진 활자가
                    // 있을 때만 값이 있고, 회차의 총 인쇄 횟수를 줄이지 않는다.
                    if (p.Recast > 0 && b.Case.MostWorn(c.id) != null) return true;
                    // 그 밖에는 상자에 둔다. 특히 <b>큰 활자를 잡졸에게 찍지 않는다</b>.
                    return false;

                default:
                    return false;
            }
        }

        // ---------- 턴 ----------

        public void TakePlayerTurn(Battle b)
        {
            NoteChances(b);

            for (int plays = 0; plays < Battle.MaxPlaysPerTurn; plays++)
            {
                int pick = ChooseCard(b);
                if (pick < 0) return;
                string id = b.Hand[pick];
                b.PlayCard(pick);
                Stats.NotePlay(id);
                if (b.Outcome != BattleOutcome.InProgress) return;
                // 카드를 뽑는 효과가 손을 바꾸므로 기회를 다시 센다.
                NoteChances(b);
            }
        }

        void NoteChances(Battle b)
        {
            var seen = new List<string>();
            for (int i = 0; i < b.Hand.Count; i++)
            {
                if (!b.CanPlay(i)) continue;
                string id = b.Hand[i];
                if (seen.Contains(id)) continue;    // 한 턴에 같은 카드를 두 번 세지 않는다
                seen.Add(id);
                Stats.NoteChance(id);
            }
        }

        // ---------- 탐욕 판단 ----------

        /// <summary>
        /// 한 장을 냈을 때 <b>지금</b> 무슨 일이 일어나는지. 선명도를 반영한다.
        /// <see cref="TypeCase.Print"/> 를 부르면 나머지가 소비되므로 미리보기는 반드시
        /// 여기서 따로 계산한다 — 보기만 했는데 상태가 바뀌면 재현이 깨진다.
        /// </summary>
        struct Preview
        {
            public int Damage, Block, Heal, Strength, Smudge, Draw, Energy, Recast;
            public int Unblocked;
            public bool Finishes, Saves;
        }

        static int Scaled(CardData c, int sharp, string type, int amount)
        {
            if (!c.IsConsumable || type == "draw" || type == "energy" || type == "recast") return amount;
            int v = amount * sharp / 100;
            return v < 1 ? 1 : v;
        }

        Preview Look(Battle b, CardData c)
        {
            var p = new Preview();
            int sharp = b.Case.SharpnessPct(c.id);
            foreach (var e in c.effects)
            {
                int v = Scaled(c, sharp, e.type, e.amount);
                switch (e.type)
                {
                    case "damage": p.Damage += v; break;
                    case "block": p.Block += v; break;
                    case "heal": p.Heal += v; break;
                    case "strength": p.Strength += v; break;
                    case "smudge": p.Smudge += v; break;
                    case "draw": p.Draw += v; break;
                    case "energy": p.Energy += v; break;
                    case "recast": p.Recast += v; break;
                }
            }
            if (p.Damage > 0) p.Damage += b.Player.Strength;

            p.Unblocked = Math.Max(0, b.IncomingDamage - b.Player.Block);
            bool lethalThreat = p.Unblocked >= b.Player.Hp;
            p.Finishes = p.Damage > 0 && p.Damage + p.Smudge >= b.Enemy.Hp + b.Enemy.Block;
            p.Saves = lethalThreat && (p.Block >= p.Unblocked || b.Player.Hp + p.Heal > p.Unblocked);
            return p;
        }

        int ChooseCard(Battle b)
        {
            int best = -1, bestScore = 0;

            for (int i = 0; i < b.Hand.Count; i++)
            {
                if (!b.CanPlay(i)) continue;
                var c = _data.Card(b.Hand[i]);
                var p = Look(b, c);

                if (c.IsConsumable && !AllowsConsumable(b, c, p)) continue;

                int score = 0;

                // 1. 판을 끝낸다 / 죽는 것을 막는다. 다른 무엇보다 먼저다.
                if (p.Finishes) score += 10000;
                if (p.Saves) score += 8000;

                // 2. 공짜로 손을 넓히는 것 — 기력을 쓰지 않으면 늘 먼저다.
                if (c.cost == 0 && (p.Draw > 0 || p.Energy > 0)) score += 3000 + p.Draw * 20 + p.Energy * 30;
                else score += p.Draw * 40 + p.Energy * 60;

                // 3. 막아야 할 만큼만 막는다. 필요 없는 방어는 낭비다.
                if (p.Unblocked > 0) score += Math.Min(p.Block, p.Unblocked) * 30;
                else score += p.Block * 2;

                // 4. 때린다. 초과 피해는 값이 없다.
                score += Math.Min(p.Damage, b.Enemy.Hp + b.Enemy.Block) * 22;
                score += p.Smudge * 16;

                // 5. 회복은 실제로 찬 만큼만.
                score += Math.Min(p.Heal, b.Player.MaxHp - b.Player.Hp) * 14;

                // 6. 힘은 이른 라운드에만 값이 있다. 마지막 턴에 올린 힘은 쓰이지 않는다.
                if (p.Strength > 0) score += b.Round <= 3 ? p.Strength * 40 : p.Strength * 6;

                // 7. 다시 붓기는 뭉개진 활자가 있을 때만.
                if (p.Recast > 0) score += b.Case.MostWorn(c.id) != null ? 900 : -500;

                // 8. 기력 값. 같은 값이면 싼 쪽.
                score -= c.cost * 12;

                // 9. 값이 같으면 닳지 않는 쪽을 낸다. <b>작은 기울기여야 한다</b> —
                //    크게 주면 AllowsConsumable 이 허락한 카드를 여기서 다시 막아
                //    정책이 두 번 걸러지고, 회복 활자처럼 점수가 낮은 카드가 통째로 죽는다.
                //    (SpendNow 는 이 기울기를 받지 않는다. 그게 "즉시 쓴다"의 정의다.)
                if (c.IsConsumable && !p.Finishes && !p.Saves && _policy != AttritionPolicy.SpendNow)
                    score -= 60;

                if (score > bestScore) { bestScore = score; best = i; }
            }

            return best;
        }
    }
}
