using System.Collections.Generic;
using Graft.Data;

namespace Graft.Sim
{
    /// <summary>
    /// 기억망을 **누구의 눈으로 보는가.**
    ///
    /// 엔진(꿈 자신)은 참값을 본다. 플레이어는 겉값을 보고, 겹쳐 물은 것만 참값으로 바뀐다.
    /// 거부 판정과 예측이 **같은 규칙 코드**를 돌리고 이 눈만 다르다 — 그래서
    /// 「예측이 어긋났다」가 곧 「눈이 달랐다」가 되고, 감사기가 그 차이를 이름 붙일 수 있다.
    /// </summary>
    public interface IMemoryView
    {
        IList<string> KnownIds { get; }
        bool Knows(string memoryId);
        int Day(string memoryId);
        int Span(string memoryId);
        string Place(string memoryId);
        string Mood(string memoryId);
        int Intensity(string memoryId);
        int Fixity(string memoryId);
        IList<string> People(string memoryId);
    }

    /// <summary>꿈이 보는 참값. 이 회차에 스크린이 된 기억은 true* 필드가 이긴다.</summary>
    public sealed class TruthView : IMemoryView
    {
        private readonly MemoryNet _net;
        private readonly HashSet<string> _distorted;
        private readonly List<string> _ids = new List<string>();

        public TruthView(MemoryNet net, IEnumerable<string> distortedThisSession)
        {
            _net = net;
            _distorted = new HashSet<string>(distortedThisSession);
            foreach (MemoryDef m in net.Memories) _ids.Add(m.id);
        }

        public IList<string> KnownIds { get { return _ids; } }
        public bool Knows(string id) { return _net.Has(id); }

        public int Day(string id)
        {
            MemoryDef m = _net.Memory(id);
            return (_distorted.Contains(id) && m.trueDayIndex >= 0) ? m.trueDayIndex : m.dayIndex;
        }

        public string Place(string id)
        {
            MemoryDef m = _net.Memory(id);
            return (_distorted.Contains(id) && m.truePlaceId.Length > 0) ? m.truePlaceId : m.placeId;
        }

        public string Mood(string id)
        {
            MemoryDef m = _net.Memory(id);
            return (_distorted.Contains(id) && m.trueMoodId.Length > 0) ? m.trueMoodId : m.moodId;
        }

        public int Span(string id) { return _net.Memory(id).spanDays; }
        public int Intensity(string id) { return _net.Memory(id).intensityPercent; }
        public int Fixity(string id) { return _net.Memory(id).fixityPercent; }
        public IList<string> People(string id) { return _net.Memory(id).peopleIds; }

        public bool IsDistorted(string id) { return _distorted.Contains(id); }
        public IList<string> DistortedIds { get { return new List<string>(_distorted); } }
    }

    /// <summary>
    /// 플레이어가 쥔 것. 띄운 기억만 보이고, **겹쳐 물은 것만 참값으로 보인다.**
    /// 명료도(lucidity)를 쓴 만큼만 띄울 수 있다 — 값이 여기 붙어 있어서
    /// 「전부 캐묻는 정책」이 저절로 벌을 받는다.
    /// </summary>
    public sealed class ProbeKnowledge : IMemoryView
    {
        private readonly MemoryNet _net;
        private readonly TruthView _truth;
        private readonly List<string> _surfaced = new List<string>();
        /// <summary>어느 이웃을 타고 띄웠는가. 서로 다른 이웃이 둘 이상이면 참값이 드러난다.</summary>
        private readonly Dictionary<string, HashSet<string>> _via = new Dictionary<string, HashSet<string>>();
        /// <summary>처음 이 기억을 띄워 준 이웃. 겹쳐 묻기가 **같은 길로 돌아온 것**인지 가리는 데 쓴다.</summary>
        private readonly Dictionary<string, string> _firstVia = new Dictionary<string, string>();
        private readonly int _routesToCorroborate;

        public int LucidityLeft { get; private set; }
        public int LuciditySpent { get; private set; }

        public ProbeKnowledge(MemoryNet net, TruthView truth, int lucidityBudget, int routesToCorroborate)
        {
            _net = net;
            _truth = truth;
            LucidityLeft = lucidityBudget;
            _routesToCorroborate = routesToCorroborate;
            foreach (string id in net.StartSurfaced())
            {
                if (!net.Has(id)) continue;
                _surfaced.Add(id);
                _via[id] = new HashSet<string>();
            }
        }

        public IList<string> KnownIds { get { return _surfaced; } }
        public bool Knows(string id) { return _via.ContainsKey(id); }

        /// <summary>이 기억의 참값이 드러났는가 (서로 다른 길로 겹쳐 물었는가).</summary>
        public bool Corroborated(string id)
        {
            HashSet<string> v;
            if (!_via.TryGetValue(id, out v)) return false;
            if (v.Count == 0) return true;      // 처음부터 떠 있던 기억은 꿈이 스스로 내놓은 것이다
            return v.Count >= _routesToCorroborate;
        }

        /// <summary>
        /// 떠 있는 이웃 하나를 타고 건너가 기억을 띄운다. 값을 치를 수 없으면 아무 일도 일어나지 않는다.
        /// 이미 뜬 기억을 **다른** 이웃으로 또 타면 겹쳐 물은 것이 된다.
        /// </summary>
        public bool Probe(string fromMemoryId, string toMemoryId)
        {
            if (!Knows(fromMemoryId)) return false;
            if (!_net.Has(toMemoryId)) return false;
            LinkDef link = null;
            foreach (LinkDef l in _net.LinksOf(fromMemoryId))
                if (_net.Other(l, fromMemoryId) == toMemoryId) { link = l; break; }
            if (link == null) return false;

            HashSet<string> v;
            bool isCorroboration = _via.TryGetValue(toMemoryId, out v);
            if (isCorroboration && v.Contains(fromMemoryId)) return false;  // 같은 길을 두 번
            // **같은 길로 돌아온 것은 확인이 아니다.** 저 기억을 거쳐 뜬 이웃으로 저 기억을 확인할 수 없다.
            if (isCorroboration && SurfacedThrough(fromMemoryId, toMemoryId)) return false;
            if (LucidityLeft < link.probeCost) return false;

            LucidityLeft -= link.probeCost;
            LuciditySpent += link.probeCost;
            if (v == null)
            {
                v = new HashSet<string>();
                _via[toMemoryId] = v;
                _surfaced.Add(toMemoryId);
                _firstVia[toMemoryId] = fromMemoryId;
            }
            v.Add(fromMemoryId);
            return true;
        }

        /// <summary>from 이 target 을 거쳐 떠오른 것인가. 처음 띄워 준 이웃의 사슬을 거슬러 본다.</summary>
        public bool SurfacedThrough(string from, string target)
        {
            string cur = from;
            int guard = 0;
            while (guard++ < 64)
            {
                string via;
                if (!_firstVia.TryGetValue(cur, out via)) return false;   // 처음부터 떠 있던 기억
                if (via == target) return true;
                cur = via;
            }
            return false;
        }

        public int Day(string id) { return Corroborated(id) ? _truth.Day(id) : _net.Memory(id).dayIndex; }
        public string Place(string id) { return Corroborated(id) ? _truth.Place(id) : _net.Memory(id).placeId; }
        public string Mood(string id) { return Corroborated(id) ? _truth.Mood(id) : _net.Memory(id).moodId; }
        public int Span(string id) { return _net.Memory(id).spanDays; }
        public int Intensity(string id) { return _net.Memory(id).intensityPercent; }
        public int Fixity(string id) { return _net.Memory(id).fixityPercent; }
        public IList<string> People(string id) { return _net.Memory(id).peopleIds; }

        public IList<string> ViaOf(string id)
        {
            HashSet<string> v;
            if (!_via.TryGetValue(id, out v)) return new List<string>();
            return new List<string>(v);
        }
    }
}
