using System.Collections.Generic;
using Graft.Data;

namespace Graft.Sim
{
    /// <summary>한 기억을 띄우는 가장 싼 길 하나.</summary>
    public sealed class ProbeRoute
    {
        public string MemoryId;
        public int Cost;
        /// <summary>어느 이웃을 타고 왔는가. 겹쳐 물었는지를 판정할 때 이 이웃이 서로 달라야 한다.</summary>
        public string ViaMemoryId;
        public string ViaKind;
    }

    /// <summary>
    /// 기억망. 연상 간선이 두 일을 한꺼번에 한다:
    ///
    ///   ① **캐묻는 길** — 떠 있는 기억에서 간선을 타고 건너가 새 기억을 띄운다. 값은 간선의 probeCost.
    ///   ② **떨림이 흐르는 줄** — 심은 기억과 어긋난 기억 사이의 떨림이 이 줄 위에 그려진다.
    ///
    /// 둘이 같은 그래프라는 것이 이 PoC의 전부다. 알 수 있는 것과 거부가 같은 구조 위에 있어야
    /// 「거부를 예측할 수 있다」가 성립한다.
    /// </summary>
    public sealed class MemoryNet
    {
        private readonly SubjectDef _subject;
        private readonly Dictionary<string, MemoryDef> _memories = new Dictionary<string, MemoryDef>();
        private readonly Dictionary<string, List<LinkDef>> _adj = new Dictionary<string, List<LinkDef>>();

        public MemoryNet(SubjectDef subject)
        {
            _subject = subject;
            foreach (MemoryDef m in subject.memories)
            {
                _memories[m.id] = m;
                _adj[m.id] = new List<LinkDef>();
            }
            foreach (LinkDef l in subject.links)
            {
                if (_adj.ContainsKey(l.a)) _adj[l.a].Add(l);
                if (_adj.ContainsKey(l.b)) _adj[l.b].Add(l);
            }
        }

        public SubjectDef Subject { get { return _subject; } }
        public IList<MemoryDef> Memories { get { return _subject.memories; } }
        public IList<LinkDef> Links { get { return _subject.links; } }
        public bool Has(string id) { return _memories.ContainsKey(id); }

        public MemoryDef Memory(string id)
        {
            MemoryDef m;
            if (!_memories.TryGetValue(id, out m)) throw new KeyNotFoundException("없는 기억: " + id);
            return m;
        }

        public IList<LinkDef> LinksOf(string memoryId)
        {
            List<LinkDef> list;
            if (!_adj.TryGetValue(memoryId, out list)) return new List<LinkDef>();
            return list;
        }

        public string Other(LinkDef l, string me) { return l.a == me ? l.b : l.a; }

        public List<string> NeighboursOf(string memoryId)
        {
            List<string> ns = new List<string>();
            foreach (LinkDef l in LinksOf(memoryId))
            {
                string o = Other(l, memoryId);
                if (!ns.Contains(o)) ns.Add(o);
            }
            return ns;
        }

        public List<string> StartSurfaced()
        {
            List<string> s = new List<string>();
            foreach (string id in _subject.surfacedAtStart) s.Add(id);
            return s;
        }

        /// <summary>
        /// 떠 있는 집합에서 각 기억까지의 **가장 싼 길**. 다익스트라다(값이 양의 정수이므로 안전하다).
        /// 시작 집합의 기억은 값 0.
        /// </summary>
        public Dictionary<string, ProbeRoute> CheapestRoutes(IList<string> surfaced)
        {
            Dictionary<string, ProbeRoute> best = new Dictionary<string, ProbeRoute>();
            foreach (string s in surfaced)
                if (Has(s)) best[s] = new ProbeRoute { MemoryId = s, Cost = 0, ViaMemoryId = "", ViaKind = "" };

            // 기억 수가 스무 개 안쪽이라 단순 선택 루프로 충분하다. 씨드와 무관하게 결정적이다.
            HashSet<string> settled = new HashSet<string>();
            while (true)
            {
                string pick = null;
                int pickCost = int.MaxValue;
                foreach (KeyValuePair<string, ProbeRoute> kv in best)
                {
                    if (settled.Contains(kv.Key)) continue;
                    // 같은 값이면 id 순으로 — 결정적이어야 한다
                    if (kv.Value.Cost < pickCost || (kv.Value.Cost == pickCost && (pick == null || kv.Key.CompareTo(pick) < 0)))
                    {
                        pick = kv.Key;
                        pickCost = kv.Value.Cost;
                    }
                }
                if (pick == null) break;
                settled.Add(pick);

                foreach (LinkDef l in LinksOf(pick))
                {
                    string o = Other(l, pick);
                    if (!Has(o)) continue;
                    int cost = pickCost + l.probeCost;
                    ProbeRoute cur;
                    if (best.TryGetValue(o, out cur) && cur.Cost <= cost) continue;
                    best[o] = new ProbeRoute { MemoryId = o, Cost = cost, ViaMemoryId = pick, ViaKind = l.kind };
                }
            }
            return best;
        }

        /// <summary>
        /// 이 기억을 **서로 다른 이웃 둘 이상**으로 띄울 수 있는가. 겹쳐 물어 참값을 드러내는 조건이다.
        /// 값은 (서로 다른 이웃 수, 둘째 이웃까지 쓴 총값).
        /// </summary>
        public int DistinctRouteCount(string memoryId, IList<string> surfaced)
        {
            Dictionary<string, ProbeRoute> routes = CheapestRoutes(surfaced);
            int n = 0;
            foreach (string nb in NeighboursOf(memoryId))
                if (routes.ContainsKey(nb)) n++;
            return n;
        }
    }
}
