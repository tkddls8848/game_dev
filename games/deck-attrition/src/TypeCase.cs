using System;
using System.Collections.Generic;
using System.Text;

namespace DeckAttrition
{
    /// <summary>
    /// 활자 상자 — <b>이 PoC의 규칙이 사는 곳</b>.
    ///
    /// 쓴 카드는 한 판이 아니라 <b>회차 전체</b>에서 닳는다. 그래서 마모 상태는 전투가 아니라
    /// 여기에 있고, 전투는 이 상자를 빌려 쓸 뿐이다. 전투가 끝나도 상자는 그대로 남는다 —
    /// 이 한 줄이 `games/deck-rewind`·`games/deck-openhand` 와 다른 전부다.
    ///
    /// 두 가지가 한꺼번에 줄어든다:
    /// <list type="bullet">
    ///   <item><b>남은 횟수</b>(<c>UsesLeft</c>) — 0 이 되면 그 활자는 회차에서 사라진다(녹인다).</item>
    ///   <item><b>선명도</b>(<c>SharpnessPct</c>) — 찍을수록 뭉개져 효과가 약해진다.</item>
    /// </list>
    /// 선명도가 있는 이유는 연출이 아니라 결정이다. 닳는 것이 <b>있다/없다</b> 둘뿐이면
    /// "마지막에 몰아 쓴다"가 항상 옳고, 계획서 §3이 경고한 지루한 최적해가 그대로 생긴다.
    /// 선명도가 있으면 <b>첫 인쇄가 가장 선명하다</b>는 사실이 생겨 "언제 찍을까"가 값을 갖는다.
    ///
    /// <b>횟수는 활자마다가 아니라 활자 <i>종류</i>마다</b> 있다. 덱에 같은 카드가 두 장 있어도
    /// 물리적인 활자는 하나다. 그래서 다 닳으면 덱·손·버림에 있는 그 카드가 <b>전부</b> 사라진다.
    /// </summary>
    public sealed class TypeCase
    {
        /// 마모가 <b>배율로 깎지 않는</b> 효과들. 장 수와 기력을 백분율로 깎으면
        /// "1.5장 뽑기"가 생기고, 정수로 내림하면 선명도 99%에서 카드가 0장이 된다.
        static readonly string[] NoWearScaling = { "draw", "energy", "recast" };

        readonly GameData _data;
        readonly int _minSharpnessPct;

        // 카드 id 로 색인하되, <b>순회는 절대 하지 않는다</b>. 순서가 필요한 자리는
        // `_data.Cards` 배열 순서를 쓴다 — Dictionary 순회 순서에 기대면 씨드 재현이 깨진다.
        readonly Dictionary<string, int> _usesLeft = new Dictionary<string, int>();
        readonly Dictionary<string, int> _timesUsed = new Dictionary<string, int>();
        readonly Dictionary<string, int> _remainder = new Dictionary<string, int>();

        readonly List<string> _exhausted = new List<string>();

        public TypeCase(GameData data)
        {
            _data = data;
            _minSharpnessPct = data.Balance.minSharpnessPct;
            foreach (var c in data.Cards)
            {
                if (!c.IsConsumable) continue;
                _usesLeft[c.id] = c.uses;
                _timesUsed[c.id] = 0;
                _remainder[c.id] = 0;
            }
        }

        /// 이 회차에서 다 닳아 사라진 활자들. 사라진 순서 그대로다.
        public IReadOnlyList<string> Exhausted => _exhausted;

        /// 회차 전체에서 닳는 활자를 실제로 찍은 총 횟수.
        public int TotalUsesSpent
        {
            get
            {
                int n = 0;
                foreach (var c in _data.Cards) if (c.IsConsumable) n += TimesUsed(c.id);
                return n;
            }
        }

        /// -1 이면 닳지 않는 활자다(상용 활자). 그 밖에는 남은 인쇄 횟수.
        public int UsesLeft(string cardId)
        {
            var card = _data.Card(cardId);
            if (!card.IsConsumable) return -1;
            return _usesLeft[cardId];
        }

        public int TimesUsed(string cardId) =>
            _timesUsed.TryGetValue(cardId, out var v) ? v : 0;

        public bool IsSpent(string cardId)
        {
            var card = _data.Card(cardId);
            return card.IsConsumable && _usesLeft[cardId] <= 0;
        }

        /// <summary>
        /// 찍힘의 선명도(백분율 정수). 닳지 않는 활자는 늘 100.
        /// 바닥(<c>minSharpnessPct</c>)을 두는 이유: 0 까지 떨어지면 "남아 있는데 아무 일도
        /// 일어나지 않는 카드"가 되고, 그건 규칙이 아니라 고장으로 읽힌다.
        /// </summary>
        public int SharpnessPct(string cardId)
        {
            var card = _data.Card(cardId);
            if (!card.IsConsumable) return 100;
            int s = 100 - card.wearPerUsePct * TimesUsed(cardId);
            return s < _minSharpnessPct ? _minSharpnessPct : s;
        }

        /// <summary>
        /// 실제로 찍히는 값. 백분율 정수 + <b>나머지 누적</b>이다 (설계 원칙 4) —
        /// 버림만 하면 선명도 80%로 열 번 찍은 총합이 설계값과 어긋난다.
        /// 나머지는 활자마다 따로 쌓인다.
        /// </summary>
        public int Print(string cardId, string effectType, int amount)
        {
            var card = _data.Card(cardId);
            if (!card.IsConsumable || amount <= 0) return amount;
            foreach (var t in NoWearScaling) if (t == effectType) return amount;

            int pct = SharpnessPct(cardId);
            int total = amount * pct + _remainder[cardId];
            int value = total / 100;
            _remainder[cardId] = total % 100;
            // 선명도가 바닥이어도 아주 작은 값이 0 으로 사라지지는 않게 한다.
            return value < 1 ? 1 : value;
        }

        /// <summary>
        /// 한 번 찍었다. 남은 횟수가 줄고 선명도가 떨어진다.
        /// 0 이 되면 true 를 돌려주고 — 부르는 쪽이 덱·손·버림에서 그 활자를 전부 걷어낸다.
        /// </summary>
        public bool Spend(string cardId)
        {
            var card = _data.Card(cardId);
            if (!card.IsConsumable) return false;
            if (_usesLeft[cardId] <= 0) throw new InvalidOperationException("이미 다 닳은 활자: " + cardId);

            _usesLeft[cardId]--;
            _timesUsed[cardId]++;
            if (_usesLeft[cardId] > 0) return false;

            _exhausted.Add(cardId);
            return true;
        }

        /// <summary>
        /// 다시 주조한다. 남은 횟수가 늘고 <b>선명도도 같이 돌아온다</b> — 납을 다시 부었으니까.
        /// 다 닳아 사라진 활자는 되살리지 않는다. 사라진 것이 돌아오면 규칙이 무를 수 있는 것이 되고,
        /// 무를 수 있으면 "지금 쓸까"가 결정이 아니게 된다.
        /// </summary>
        public bool Recast(string cardId, int amount)
        {
            var card = _data.Card(cardId);
            if (!card.IsConsumable || amount <= 0) return false;
            if (_usesLeft[cardId] <= 0) return false;

            _usesLeft[cardId] += amount;
            int t = _timesUsed[cardId] - amount;
            _timesUsed[cardId] = t < 0 ? 0 : t;
            return true;
        }

        /// <summary>
        /// 가장 뭉개진 활자. 재주조 대상이다.
        /// 동점이면 <c>data/cards.json</c> 의 <b>배열 순서</b>로 자른다 —
        /// Dictionary 순회 순서로 자르면 같은 씨드가 다른 결과를 낸다.
        /// </summary>
        public string MostWorn(string exceptCardId = null)
        {
            string best = null;
            int bestSharp = int.MaxValue;
            foreach (var c in _data.Cards)
            {
                if (!c.IsConsumable) continue;
                if (c.id == exceptCardId) continue;
                if (_usesLeft[c.id] <= 0) continue;
                if (TimesUsed(c.id) == 0) continue;      // 아직 새 활자는 다시 부을 것이 없다
                int s = SharpnessPct(c.id);
                if (s < bestSharp) { bestSharp = s; best = c.id; }
            }
            return best;
        }

        /// 사람이 읽는 한 줄. 기록에 남긴다.
        public string Describe()
        {
            var sb = new StringBuilder();
            foreach (var c in _data.Cards)
            {
                if (!c.IsConsumable) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(c.id).Append('=').Append(_usesLeft[c.id])
                  .Append('/').Append(SharpnessPct(c.id)).Append('%');
            }
            return sb.ToString();
        }
    }
}
