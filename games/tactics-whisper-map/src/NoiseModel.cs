using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
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
    /// 한 홉마다 hopAttenuation 이 깎이고, noiseBlocked 쌍을 건널 때 extraAttenuation 이 더 깎인다.
    /// 도착 음량이 hearingThreshold 미만이면 들리지 않는다.
    ///
    /// 이 지도에서 그 결과는 한 줄로 요약된다:
    /// **과수원에서 쏜 총성(90)은 차폐(40)와 홉(40)에 먹혀 10이 되고, 아무도 듣지 않는다.**
    /// 그래서 위상을 몰라도 과수원은 쓸 수 있다 — BlindSolvability 가 기대는 지형 사실이다.
    ///
    /// **소음 규칙은 지형 지식이다** — 며칠 살면 어디서 난 소리가 어디까지 오는지 몸으로 안다.
    /// 마을에 물어야 아는 것은 "누가 그때 그 자리에 있는가"뿐이다.
    /// </summary>
    public sealed class NoiseModel
    {
        private readonly ZoneGraph _graph;
        private readonly NoiseBalance _balance;

        public NoiseModel(ZoneGraph graph, NoiseBalance balance) { _graph = graph; _balance = balance; }

        public int HearingThreshold { get { return _balance.hearingThresholdPercent; } }

        /// <summary>인접만으로 가능한 최대 홉 수. 차폐를 무시한 상한이다(NoiseReachability 가 쓴다).</summary>
        public int MaxHops(int loudness)
        {
            int budget = loudness - _balance.hearingThresholdPercent;
            if (budget < 0) return -1;
            return budget / _balance.hopAttenuation;
        }

        /// <summary>
        /// origin 에서 loudness 로 난 소리가 닿는 구역 전부.
        /// 감쇠 총합이 가장 작은 경로를 고른다 — 소리는 가장 잘 새는 길로 온다.
        /// 구역이 여섯 개다. 우선순위 큐를 쓸 값이 없다 — 선형 선택이 더 읽기 쉽다.
        /// </summary>
        public List<NoiseArrival> Propagate(string origin, int loudness)
        {
            List<NoiseArrival> reached = new List<NoiseArrival>();
            if (loudness < _balance.hearingThresholdPercent) return reached;

            Dictionary<string, int> bestCost = new Dictionary<string, int> { { origin, 0 } };
            Dictionary<string, int> hopsAt = new Dictionary<string, int> { { origin, 0 } };
            HashSet<string> settled = new HashSet<string>();

            while (true)
            {
                string cur = null;
                int curCost = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in bestCost)
                {
                    if (settled.Contains(kv.Key)) continue;
                    // 동률은 구역 선언 순서로 깬다 — 결정적이어야 한다.
                    if (kv.Value < curCost
                        || (kv.Value == curCost && cur != null
                            && _graph.IndexOf(kv.Key) < _graph.IndexOf(cur)))
                    { cur = kv.Key; curCost = kv.Value; }
                }
                if (cur == null) break;
                settled.Add(cur);

                int arrival = loudness - curCost;
                if (arrival < _balance.hearingThresholdPercent) continue; // 여기서 안 들리면 더 가도 안 들린다
                reached.Add(new NoiseArrival { Zone = cur, Loudness = arrival, Hops = hopsAt[cur] });

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
        public bool Hears(string origin, int loudness, string listener, int threshold)
        {
            foreach (NoiseArrival a in Propagate(origin, loudness))
                if (a.Zone == listener) return a.Loudness >= threshold;
            return false;
        }

        private int CompareByZoneOrder(NoiseArrival a, NoiseArrival b)
        {
            return _graph.IndexOf(a.Zone).CompareTo(_graph.IndexOf(b.Zone));
        }
    }
}
