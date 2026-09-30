using System.Collections.Generic;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>
    /// 구역 + 인접 관계. 이 PoC의 공간 모델 전부다 — 좌표가 없다.
    ///
    /// 인접 관계 하나가 세 가지를 동시에 정한다:
    ///   · 이동 경로 (한 홉 = moveMsPerZone)
    ///   · 소음 전파 경로 (한 홉 = hopAttenuation 감쇠)
    ///   · 사거리 (홉 수)
    ///
    /// 시야는 인접과 별개다(sightLines). 능선에서 건너편 창고가 보이는데
    /// 걸어서는 두 홉인 것이 정상이기 때문이다.
    /// </summary>
    public sealed class ZoneGraph
    {
        private readonly MapDef _map;
        private readonly List<string> _zoneIds = new List<string>();
        private readonly Dictionary<string, int> _index = new Dictionary<string, int>();
        private readonly Dictionary<string, ZoneDef> _zones = new Dictionary<string, ZoneDef>();
        private readonly Dictionary<string, List<string>> _neighbors = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, HashSet<string>> _sees = new Dictionary<string, HashSet<string>>();
        private readonly Dictionary<string, int> _blockExtra = new Dictionary<string, int>();
        private readonly Dictionary<string, List<ConcealDef>> _conceal = new Dictionary<string, List<ConcealDef>>();

        // ── 뜨거운 고리용 색인 ────────────────────────────────────────────────
        // 계획 탐색이 회차를 수만 번 돌린다. 틱마다 문자열 해시를 두드리면 그게 전부 시간이 된다.
        // 시야·엄폐는 여기서 정수 색인으로 미리 펼쳐 둔다 (값은 위 사전과 같다).
        private bool[] _seesFlat;
        private int[] _coverByIndex;

        public MapDef Map { get { return _map; } }
        public IList<string> ZoneIds { get { return _zoneIds; } }
        public string EntryZone { get { return Canon(_map.entryZone); } }
        public string ExtractionZone { get { return Canon(_map.extractionZone); } }

        public int ZoneCount { get { return _zoneIds.Count; } }
        public int IndexOf(string zone) { return _index[zone]; }
        public string ZoneAt(int index) { return _zoneIds[index]; }

        /// <summary>from 색인 * ZoneCount + to 색인 으로 읽는 시야 표.</summary>
        public bool[] SeesFlat { get { return _seesFlat; } }
        public int[] CoverByIndex { get { return _coverByIndex; } }

        /// <summary>같은 구역 id의 문자열 인스턴스를 하나로 모은다 — 참조 비교가 성립하게 한다.</summary>
        public string Canon(string zone)
        {
            int i;
            return _index.TryGetValue(zone, out i) ? _zoneIds[i] : zone;
        }

        public ZoneGraph(MapDef map)
        {
            _map = map;

            foreach (ZoneDef z in map.zones)
            {
                _index[z.id] = _zoneIds.Count;
                _zoneIds.Add(z.id);
                _zones[z.id] = z;
                _neighbors[z.id] = new List<string>();
                _sees[z.id] = new HashSet<string>();
                _conceal[z.id] = new List<ConcealDef>();
            }

            foreach (LinkDef l in map.links)
            {
                Require(l.a); Require(l.b);
                // 인접 목록에는 **정규 인스턴스**를 넣는다 — 뜨거운 고리에서 참조 비교가 되게 한다.
                string a = _zoneIds[_index[l.a]];
                string b = _zoneIds[_index[l.b]];
                if (!_neighbors[a].Contains(b)) _neighbors[a].Add(b);
                if (!_neighbors[b].Contains(a)) _neighbors[b].Add(a);
            }
            // 인접 목록을 구역 선언 순서로 정렬한다 — 경로 탐색 동률을 결정적으로 깨기 위해서다.
            foreach (string z in _zoneIds) _neighbors[z].Sort(CompareByDeclarationOrder);

            foreach (SightDef s in map.sightLines)
            {
                Require(s.from);
                foreach (string to in s.sees) { Require(to); _sees[s.from].Add(to); }
            }

            if (map.noiseBlocked != null)
            {
                foreach (LinkDef b in map.noiseBlocked)
                {
                    Require(b.a); Require(b.b);
                    _blockExtra[PairKey(b.a, b.b)] = b.extraAttenuation;
                }
            }

            if (map.concealment != null)
            {
                foreach (ConcealDef c in map.concealment) { Require(c.zone); _conceal[c.zone].Add(c); }
            }

            int n = _zoneIds.Count;
            _seesFlat = new bool[n * n];
            _coverByIndex = new int[n];
            for (int from = 0; from < n; from++)
            {
                _coverByIndex[from] = _zones[_zoneIds[from]].coverPercent;
                foreach (string seen in _sees[_zoneIds[from]]) _seesFlat[from * n + _index[seen]] = true;
            }
        }

        public bool HasZone(string id) { return _zones.ContainsKey(id); }
        public ZoneDef Zone(string id) { return _zones[id]; }
        public int CoverPercent(string zone) { return _zones[zone].coverPercent; }
        public IList<string> Neighbors(string zone) { return _neighbors[zone]; }

        /// <summary>zone 에 선 사람이 target 을 볼 수 있는가. 시야는 방향이 있다(꺼진 골짜기).</summary>
        public bool Sees(string zone, string target)
        {
            HashSet<string> set;
            return _sees.TryGetValue(zone, out set) && set.Contains(target);
        }

        public IEnumerable<string> SeenFrom(string zone) { return _sees[zone]; }

        public IList<ConcealDef> ConcealmentIn(string zone) { return _conceal[zone]; }

        public int BestConcealmentBonus(string zone)
        {
            int best = 0;
            foreach (ConcealDef c in _conceal[zone]) if (c.hideBonusPercent > best) best = c.hideBonusPercent;
            return best;
        }

        /// <summary>소음 차폐로 추가로 깎이는 양. 차폐가 없으면 0.</summary>
        public int ExtraAttenuation(string a, string b)
        {
            int extra;
            return _blockExtra.TryGetValue(PairKey(a, b), out extra) ? extra : 0;
        }

        /// <summary>홉 수. 닿지 않으면 -1.</summary>
        public int Hops(string from, string to)
        {
            if (from == to) return 0;
            Dictionary<string, int> dist = new Dictionary<string, int>();
            Queue<string> q = new Queue<string>();
            dist[from] = 0; q.Enqueue(from);
            while (q.Count > 0)
            {
                string cur = q.Dequeue();
                foreach (string n in _neighbors[cur])
                {
                    if (dist.ContainsKey(n)) continue;
                    dist[n] = dist[cur] + 1;
                    if (n == to) return dist[n];
                    q.Enqueue(n);
                }
            }
            return -1;
        }

        /// <summary>
        /// 최단 경로(구역 배열, from 포함 to 포함). 닿지 않으면 null.
        /// 동률은 구역 선언 순서로 깬다 — 같은 데이터면 늘 같은 경로가 나와야 한다.
        /// </summary>
        public List<string> Path(string from, string to)
        {
            from = Canon(from); to = Canon(to);
            if (from == to) return new List<string> { from };
            Dictionary<string, string> prev = new Dictionary<string, string>();
            HashSet<string> seen = new HashSet<string> { from };
            Queue<string> q = new Queue<string>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                string cur = q.Dequeue();
                foreach (string n in _neighbors[cur])
                {
                    if (seen.Contains(n)) continue;
                    seen.Add(n);
                    prev[n] = cur;
                    if (n == to)
                    {
                        List<string> path = new List<string> { to };
                        string walk = to;
                        while (walk != from) { walk = prev[walk]; path.Add(walk); }
                        path.Reverse();
                        return path;
                    }
                    q.Enqueue(n);
                }
            }
            return null;
        }

        private int CompareByDeclarationOrder(string a, string b) { return _index[a].CompareTo(_index[b]); }

        private static string PairKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }

        private void Require(string zone)
        {
            if (!_zones.ContainsKey(zone))
                throw new KeyNotFoundException("map '" + _map.id + "' 에 없는 구역 참조: '" + zone + "'");
        }
    }
}
