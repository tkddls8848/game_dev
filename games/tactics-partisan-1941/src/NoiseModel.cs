using System.Collections.Generic;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>소음 하나가 어느 구역에 얼마로 닿는가.</summary>
    public struct NoiseArrival
    {
        public string Zone;
        public int Loudness;     // 도착 음량(백분율 정수). hearingThreshold 이상이면 들린다
        public int Hops;         // 인접 몇 홉을 건너왔는가
    }

    /// <summary>
    /// 소음 전파. **인접 관계만 쓴다** — 거리가 아니다.
    ///
    /// mystery-blackwood의 AudibilityModel이 문이 아니라 벽 맞닿음으로 가청을 판정한 것과
    /// 같은 규칙이다. 거기서 거리는 음량·정위만 바꾸고 판정은 바꾸지 않았다.
    /// 여기서도 판정은 "몇 홉 건너왔는가 + 차폐가 있었는가"로만 내려간다.
    ///
    /// 한 홉마다 hopAttenuation 만큼 깎이고, noiseBlocked 쌍을 건널 때
    /// extraAttenuation 이 더 깎인다. 도착 음량이 hearingThreshold 미만이면 들리지 않는다.
    /// </summary>
    public sealed class NoiseModel
    {
        private readonly ZoneGraph _graph;
        private readonly NoiseBalance _balance;

        public NoiseModel(ZoneGraph graph, NoiseBalance balance)
        {
            _graph = graph;
            _balance = balance;
        }

        public int HearingThreshold { get { return _balance.hearingThresholdPercent; } }

        /// <summary>인접만으로 가능한 최대 홉 수. 차폐를 무시한 상한이다(NoiseReachability가 쓴다).</summary>
        public int MaxHops(int loudness)
        {
            int budget = loudness - _balance.hearingThresholdPercent;
            if (budget < 0) return -1;
            return budget / _balance.hopAttenuation;
        }

        /// <summary>
        /// origin 에서 loudness 로 난 소리가 닿는 구역 전부.
        /// 감쇠 총합이 가장 작은 경로를 고른다(Dijkstra) — 소리는 가장 잘 새는 길로 온다.
        /// </summary>
        public List<NoiseArrival> Propagate(string origin, int loudness)
        {
            List<NoiseArrival> reached = new List<NoiseArrival>();
            if (loudness < _balance.hearingThresholdPercent) return reached;

            Dictionary<string, int> bestCost = new Dictionary<string, int>();
            Dictionary<string, int> hopsAt = new Dictionary<string, int>();
            bestCost[origin] = 0;
            hopsAt[origin] = 0;

            // 구역이 여섯 개다. 우선순위 큐를 쓸 값이 없다 — 선형 선택이 더 읽기 쉽다.
            HashSet<string> settled = new HashSet<string>();
            while (true)
            {
                string cur = null;
                int curCost = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in bestCost)
                {
                    if (settled.Contains(kv.Key)) continue;
                    // 동률은 구역 선언 순서로 깬다 — 결정적이어야 한다.
                    if (kv.Value < curCost || (kv.Value == curCost && cur != null
                        && _graph.ZoneIds.IndexOf(kv.Key) < _graph.ZoneIds.IndexOf(cur)))
                    {
                        cur = kv.Key; curCost = kv.Value;
                    }
                }
                if (cur == null) break;
                settled.Add(cur);

                int arrival = loudness - curCost;
                if (arrival >= _balance.hearingThresholdPercent)
                {
                    reached.Add(new NoiseArrival { Zone = cur, Loudness = arrival, Hops = hopsAt[cur] });
                }
                else
                {
                    continue; // 여기서 이미 안 들리면 더 가도 안 들린다
                }

                foreach (string n in _graph.Neighbors(cur))
                {
                    int cost = curCost + _balance.hopAttenuation + _graph.ExtraAttenuation(cur, n);
                    int known;
                    if (!bestCost.TryGetValue(n, out known) || cost < known)
                    {
                        bestCost[n] = cost;
                        hopsAt[n] = hopsAt[cur] + 1;
                    }
                }
            }

            reached.Sort(CompareByZoneOrder);
            return reached;
        }

        /// <summary>origin 의 소리가 listener 에게 들리는가.</summary>
        public bool Hears(string origin, int loudness, string listener)
        {
            foreach (NoiseArrival a in Propagate(origin, loudness)) if (a.Zone == listener) return true;
            return false;
        }

        private int CompareByZoneOrder(NoiseArrival a, NoiseArrival b)
        {
            return _graph.ZoneIds.IndexOf(a.Zone).CompareTo(_graph.ZoneIds.IndexOf(b.Zone));
        }
    }
}
