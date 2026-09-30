using System.Collections.Generic;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>
    /// **정찰 단계에서 플레이어가 실제로 알 수 있는 사실 전부.**
    ///
    /// 이 클래스가 이 PoC의 핵이다. 시뮬레이션과 **다른 경로로** 만들어진다 —
    /// 지도의 reconVantages 에서 보이는 것만 담고, 시뮬레이션 결과를 한 줄도 읽지 않는다.
    /// 그래서 FairnessAudit 이 "실패를 이 보고서로 설명할 수 있는가"를 물을 때
    /// 질문이 동어반복이 되지 않는다.
    ///
    /// 세 가지를 일부러 넣지 않았다:
    ///   · 관측 불가 구역의 순찰 구간 (그게 바로 숨은 정보다 → HiddenWindows 로 따로 센다)
    ///   · 관측 불가 구역에서 뻗는 시선
    ///   · 관측 불가 구역의 엄폐율
    ///
    /// 반대로 **소음 인접 관계는 전부 안다고 본다.** 며칠 지켜보면 어디서 난 소리가
    /// 어디까지 오는지는 귀로 들린다. 이 가정은 README 의 "모델에 없는 것"에 적어 두었다.
    /// </summary>
    public sealed class ReconReport
    {
        public string MissionId { get; private set; }
        public string MapId { get; private set; }

        public readonly HashSet<string> ObservableZones = new HashSet<string>();
        public readonly List<string> Vantages = new List<string>();

        /// <summary>정찰로 실제로 본 순찰 구간.</summary>
        public readonly List<PatrolWindow> ObservedWindows = new List<PatrolWindow>();

        /// <summary>순찰병이 지나갔지만 **어디서도 볼 수 없었던** 구간. 이게 있으면 숨은 정보다.</summary>
        public readonly List<PatrolWindow> HiddenWindows = new List<PatrolWindow>();

        public readonly HashSet<string> ObservedShiftChangeIds = new HashSet<string>();
        public readonly HashSet<string> HiddenShiftChangeIds = new HashSet<string>();

        private readonly HashSet<string> _knownSightLines = new HashSet<string>();
        private readonly HashSet<string> _knownCover = new HashSet<string>();
        private readonly HashSet<string> _knownNoiseLinks = new HashSet<string>();
        private NoiseBalance _noiseBalance;

        public IEnumerable<string> KnownSightLines { get { return _knownSightLines; } }
        public IEnumerable<string> KnownCoverZones { get { return _knownCover; } }

        /// <summary>
        /// 지도·순찰·교대만 보고 만든다. 계획도 회차 결과도 보지 않는다.
        /// hideZones 는 검사기의 음성 대조군용 — 이 구역들은 벤티지에서 보이지 않는 것으로 취급한다.
        /// </summary>
        public static ReconReport Observe(GameData data, MissionDef mission, ZoneGraph graph,
                                         PatrolModel patrols, IEnumerable<string> hideZones = null)
        {
            HashSet<string> hidden = new HashSet<string>();
            if (hideZones != null) foreach (string z in hideZones) hidden.Add(z);

            ReconReport r = new ReconReport { MissionId = mission.id, MapId = mission.mapId };
            // 감쇠 규칙은 숨은 정보가 아니다 — 며칠 들으면 소리가 얼마나 죽는지 몸으로 안다.
            r._noiseBalance = data.Balance.noise;

            foreach (string v in graph.Map.reconVantages)
            {
                r.Vantages.Add(v);
                if (hidden.Contains(v)) continue;
                foreach (string seen in graph.SeenFrom(v))
                {
                    if (hidden.Contains(seen)) continue;
                    r.ObservableZones.Add(seen);
                }
            }

            // 관측 가능한 구역에서 뻗는 시선과 그 구역의 엄폐율만 안다.
            foreach (string z in r.ObservableZones)
            {
                r._knownCover.Add(z);
                foreach (string seen in graph.SeenFrom(z)) r._knownSightLines.Add(SightKey(z, seen));
            }

            // 소음 인접 관계는 전부 안다 (며칠 들었다).
            foreach (LinkDef l in graph.Map.links) r._knownNoiseLinks.Add(LinkKey(l.a, l.b));

            foreach (string guardId in patrols.GuardIds)
            {
                foreach (PatrolWindow w in patrols.Windows(guardId))
                {
                    if (r.ObservableZones.Contains(w.Zone)) r.ObservedWindows.Add(w);
                    else r.HiddenWindows.Add(w);
                }
                foreach (ShiftChangeDef sc in patrols.ShiftsOf(guardId))
                {
                    if (sc.atMs < 0 || sc.atMs >= mission.lengthMs) continue;
                    string zoneThen = patrols.ZoneAt(guardId, sc.atMs);
                    if (r.ObservableZones.Contains(zoneThen)) r.ObservedShiftChangeIds.Add(sc.id);
                    else r.HiddenShiftChangeIds.Add(sc.id);
                }
            }

            return r;
        }

        /// <summary>"이 시각 이 순찰병이 이 구역에 있다"를 정찰로 알 수 있었는가.</summary>
        public bool KnowsPatrol(string guardId, string zone, int atMs)
        {
            foreach (PatrolWindow w in ObservedWindows)
                if (w.GuardId == guardId && w.Zone == zone && w.Contains(atMs)) return true;
            return false;
        }

        /// <summary>"이 자리에서 저 자리가 보인다"를 정찰로 알 수 있었는가.</summary>
        public bool KnowsSightLine(string from, string to) { return _knownSightLines.Contains(SightKey(from, to)); }

        public bool KnowsCover(string zone) { return _knownCover.Contains(zone); }

        public bool KnowsNoiseLink(string a, string b) { return _knownNoiseLinks.Contains(LinkKey(a, b)); }

        /// <summary>
        /// **정찰 지식만으로** 이 소리가 저 자리까지 들린다고 예측할 수 있는가.
        ///
        /// "경로가 있는가"를 묻는 것으로는 부족하다. 인접 관계 하나를 몰라도 우회로가 있으면
        /// 경로는 늘 있고, 그러면 감사가 아무것도 걸러 내지 못한다. 그래서 **아는 링크만으로
        /// 같은 감쇠 계산을 다시 돌려** 가청이 예측되는지를 본다 — NoiseModel 과 같은 규칙이다.
        /// </summary>
        public bool PredictsAudible(ZoneGraph graph, string origin, int sourceLoudness,
                                    string listener, int hearingThreshold)
        {
            if (_noiseBalance == null) return true;
            if (origin == listener) return sourceLoudness >= hearingThreshold;

            Dictionary<string, int> cost = new Dictionary<string, int> { { origin, 0 } };
            HashSet<string> done = new HashSet<string>();
            while (true)
            {
                string cur = null;
                int best = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in cost)
                    if (!done.Contains(kv.Key) && kv.Value < best) { cur = kv.Key; best = kv.Value; }
                if (cur == null) return false;
                done.Add(cur);

                int arrival = sourceLoudness - best;
                if (arrival < _noiseBalance.hearingThresholdPercent) continue;
                if (cur == listener) return arrival >= hearingThreshold;

                foreach (string n in graph.Neighbors(cur))
                {
                    if (!KnowsNoiseLink(cur, n)) continue;   // 정찰로 모르는 길은 쓸 수 없다
                    int step = best + _noiseBalance.hopAttenuation + graph.ExtraAttenuation(cur, n);
                    int known;
                    if (!cost.TryGetValue(n, out known) || step < known) cost[n] = step;
                }
            }
        }

        /// <summary>대조군용. 이 링크를 "정찰로 몰랐던 것"으로 만든다.</summary>
        public void ForgetNoiseLink(string a, string b) { _knownNoiseLinks.Remove(LinkKey(a, b)); }

        /// <summary>대조군용. 이 시선을 "정찰로 몰랐던 것"으로 만든다.</summary>
        public void ForgetSightLine(string from, string to) { _knownSightLines.Remove(SightKey(from, to)); }

        private static string SightKey(string from, string to) { return from + ">" + to; }

        private static string LinkKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }
    }
}
