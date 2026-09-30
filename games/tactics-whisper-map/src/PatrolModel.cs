using System;
using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
{
    /// <summary>순찰병이 한 구역에 머무는 구간 하나. [FromMs, ToMs) 반열린 구간이다.</summary>
    public struct PatrolWindow
    {
        public string GuardId;
        public string Zone;
        public int FromMs;
        public int ToMs;

        public bool Contains(int ms) { return ms >= FromMs && ms < ToMs; }
        public override string ToString() { return GuardId + "@" + Zone + "[" + FromMs + "," + ToMs + ")"; }
    }

    /// <summary>
    /// 오늘 밤 각 순찰병이 어느 위상으로 시작하는가. **이 PoC가 사고파는 것의 절반이다.**
    ///
    /// 먼저 만든 PoC는 위상을 정찰 벤티지에서 눈으로 봤다. 여기서는 보이지 않는다 —
    /// 마을 사람에게 물어야 알고, 묻지 않으면 아홉 조합 중 어느 것인지 모른다.
    /// 그래서 이 타입이 **명시적**이다: 씨드가 고른 하나(오늘 밤)도, 전수 조합(블라인드 검사)도 같은 타입으로 돈다.
    /// </summary>
    public sealed class PhaseAssignment
    {
        private readonly Dictionary<string, int> _phaseMs = new Dictionary<string, int>();
        private readonly List<string> _order = new List<string>();

        public IList<string> GuardIds { get { return _order; } }
        public int PhaseMs(string guardId) { return _phaseMs[guardId]; }
        public bool Has(string guardId) { return _phaseMs.ContainsKey(guardId); }

        public PhaseAssignment Set(string guardId, int phaseMs)
        {
            if (!_phaseMs.ContainsKey(guardId)) _order.Add(guardId);
            _phaseMs[guardId] = phaseMs;
            return this;
        }

        /// <summary>오늘 밤. mission.seed 가 guardIds 순서대로 고른다 — 재현된다.</summary>
        public static PhaseAssignment Tonight(GameData data, MissionDef mission)
        {
            Random rng = new Random(mission.seed);
            PhaseAssignment p = new PhaseAssignment();
            foreach (string guardId in mission.guardIds)
            {
                int[] options = data.Guard(guardId).phaseOptionsMs;
                p.Set(guardId, options[rng.Next(options.Length)]);
            }
            return p;
        }

        /// <summary>
        /// 가능한 위상 조합 **전부**. 묻지 않은 플레이어가 마주할 수 있는 세계의 목록이고,
        /// BlindSolvability 가 이 전부에서 이기는 계획을 찾는다.
        /// </summary>
        public static List<PhaseAssignment> AllCombinations(GameData data, MissionDef mission)
        {
            List<PhaseAssignment> all = new List<PhaseAssignment> { new PhaseAssignment() };
            foreach (string guardId in mission.guardIds)
            {
                List<PhaseAssignment> next = new List<PhaseAssignment>();
                foreach (PhaseAssignment prefix in all)
                    foreach (int option in data.Guard(guardId).phaseOptionsMs)
                    {
                        PhaseAssignment copy = new PhaseAssignment();
                        foreach (string g in prefix.GuardIds) copy.Set(g, prefix.PhaseMs(g));
                        copy.Set(guardId, option);
                        next.Add(copy);
                    }
                all = next;
            }
            return all;
        }

        public override string ToString()
        {
            List<string> parts = new List<string>();
            foreach (string g in _order) parts.Add(g + "=" + _phaseMs[g]);
            return string.Join(",", parts);
        }
    }

    /// <summary>
    /// 순찰병이 언제 어디 있는가. **난수도 부동소수도 쓰지 않는 순수 함수**다.
    ///
    /// 위상은 밖에서 주어진다(PhaseAssignment) — 그래서 같은 경로를 **오늘 밤의 진실**로도,
    /// **마을에서 들은 소문**으로도 펼칠 수 있다. 공정성 감사가 그 둘을 나란히 놓고 비교한다.
    ///
    /// 교대(shiftChanges)는 회차 중간에 위상을 갈아탄다. 교대 시각 자체도 물어서 아는 것이고,
    /// 틀린 교대 시각을 믿으면 **교대 뒤의 모든 구간이 어긋난다** — 거짓 정보가 비싼 이유다.
    /// </summary>
    public sealed class PatrolModel
    {
        private sealed class GuardState
        {
            public GuardDef Def;
            public int CycleMs;
            public int[] LegStartMs;
            public int BasePhaseMs;
            public List<ShiftChangeDef> Shifts = new List<ShiftChangeDef>();
        }

        private readonly Dictionary<string, GuardState> _guards = new Dictionary<string, GuardState>();
        private readonly List<string> _guardOrder = new List<string>();
        private readonly int _lengthMs;
        private readonly Dictionary<string, List<PatrolWindow>> _windowCache =
            new Dictionary<string, List<PatrolWindow>>();

        public IList<string> GuardIds { get { return _guardOrder; } }
        public int LengthMs { get { return _lengthMs; } }

        /// <summary>
        /// 오늘 밤의 진실을 펼친다. routeOverride / shiftOverride 가 있으면 **믿고 있는 것**을 펼친다
        /// (마을에서 틀린 답을 들었을 때 플레이어의 머릿속 지도가 이것이다).
        /// </summary>
        public PatrolModel(GameData data, MissionDef mission, PhaseAssignment phases,
                           Dictionary<string, LegDef[]> routeOverride = null,
                           Dictionary<string, int> shiftOverride = null)
        {
            _lengthMs = mission.lengthMs;
            List<ShiftChangeDef> shifts = data.ShiftChangesOf(mission);

            foreach (string guardId in mission.guardIds)
            {
                GuardDef def = data.Guard(guardId);
                if (def.mapId != mission.mapId)
                    throw new InvalidOperationException(
                        "순찰병 " + guardId + " 는 지도 " + def.mapId + " 것인데 임무는 " + mission.mapId + " 다");

                LegDef[] legs = def.legs;
                if (routeOverride != null && routeOverride.ContainsKey(guardId)) legs = routeOverride[guardId];

                GuardState s = new GuardState { Def = def };
                s.LegStartMs = new int[legs.Length];
                int acc = 0;
                for (int i = 0; i < legs.Length; i++) { s.LegStartMs[i] = acc; acc += legs[i].dwellMs; }
                s.CycleMs = acc;
                if (s.CycleMs <= 0) throw new InvalidOperationException("순찰 주기가 0이다: " + guardId);
                s.Def = def;
                _legs[guardId] = legs;

                if (!phases.Has(guardId))
                    throw new InvalidOperationException("위상이 지정되지 않은 순찰병: " + guardId);
                s.BasePhaseMs = Mod(phases.PhaseMs(guardId), s.CycleMs);

                foreach (ShiftChangeDef sc in shifts)
                {
                    if (sc.guardId != guardId) continue;
                    ShiftChangeDef used = sc;
                    if (shiftOverride != null && shiftOverride.ContainsKey(sc.id))
                        used = new ShiftChangeDef
                        {
                            id = sc.id, guardId = sc.guardId,
                            atMs = shiftOverride[sc.id], newPhaseMs = sc.newPhaseMs, note = sc.note
                        };
                    s.Shifts.Add(used);
                }
                s.Shifts.Sort(delegate (ShiftChangeDef x, ShiftChangeDef y) { return x.atMs.CompareTo(y.atMs); });

                _guards[guardId] = s;
                _guardOrder.Add(guardId);
            }
        }

        private readonly Dictionary<string, LegDef[]> _legs = new Dictionary<string, LegDef[]>();

        public int CycleMs(string guardId) { return _guards[guardId].CycleMs; }
        public int BasePhaseMs(string guardId) { return _guards[guardId].BasePhaseMs; }
        public GuardDef Def(string guardId) { return _guards[guardId].Def; }
        public LegDef[] Legs(string guardId) { return _legs[guardId]; }
        public IList<ShiftChangeDef> ShiftsOf(string guardId) { return _guards[guardId].Shifts; }

        public string ZoneAt(string guardId, int ms)
        {
            GuardState s = _guards[guardId];
            LegDef[] legs = _legs[guardId];
            int local = LocalTime(s, ms);
            for (int i = legs.Length - 1; i >= 0; i--) if (local >= s.LegStartMs[i]) return legs[i].zone;
            return legs[0].zone;
        }

        /// <summary>[0, lengthMs) 를 구역별 구간으로 쪼갠다. 화면 시간축과 감사가 같은 것을 읽는다.</summary>
        public List<PatrolWindow> Windows(string guardId)
        {
            List<PatrolWindow> cached;
            if (_windowCache.TryGetValue(guardId, out cached)) return cached;

            GuardState s = _guards[guardId];
            LegDef[] legs = _legs[guardId];
            List<PatrolWindow> windows = new List<PatrolWindow>();

            List<int> segStart = new List<int> { 0 };
            List<int> segPhase = new List<int> { s.BasePhaseMs };
            foreach (ShiftChangeDef sc in s.Shifts)
            {
                if (sc.atMs <= 0 || sc.atMs >= _lengthMs) continue;
                segStart.Add(sc.atMs);
                segPhase.Add(Mod(sc.newPhaseMs, s.CycleMs));
            }

            for (int seg = 0; seg < segStart.Count; seg++)
            {
                int from = segStart[seg];
                int to = seg + 1 < segStart.Count ? segStart[seg + 1] : _lengthMs;
                int t = from;
                while (t < to)
                {
                    int local = Mod(segPhase[seg] + (t - from), s.CycleMs);
                    int leg = LegIndexOfLocal(s, legs, local);
                    int legEnd = s.LegStartMs[leg] + legs[leg].dwellMs;
                    int end = Math.Min(to, t + (legEnd - local));
                    windows.Add(new PatrolWindow
                    {
                        GuardId = guardId, Zone = legs[leg].zone, FromMs = t, ToMs = end
                    });
                    t = end;
                }
            }

            // 같은 구역이 연달아 나오면 합친다 (교대가 같은 구간 안에서 일어난 경우).
            List<PatrolWindow> merged = new List<PatrolWindow>();
            foreach (PatrolWindow w in windows)
            {
                if (merged.Count > 0)
                {
                    PatrolWindow last = merged[merged.Count - 1];
                    if (last.Zone == w.Zone && last.ToMs == w.FromMs)
                    {
                        last.ToMs = w.ToMs;
                        merged[merged.Count - 1] = last;
                        continue;
                    }
                }
                merged.Add(w);
            }
            _windowCache[guardId] = merged;
            return merged;
        }

        /// <summary>회차에 참여하는 순찰병이 한 번이라도 밟는 구역 전부.</summary>
        public HashSet<string> OccupiedZones()
        {
            HashSet<string> zones = new HashSet<string>();
            foreach (string g in _guardOrder)
                foreach (PatrolWindow w in Windows(g)) zones.Add(w.Zone);
            return zones;
        }

        /// <summary>이 구역에 아무 순찰병도 없는, 가장 긴 빈 구간. 저격수가 기다려야 하는 상한이다.</summary>
        public int LongestAbsenceMs(string guardId, string zone)
        {
            int worst = 0; int runStart = -1;
            foreach (PatrolWindow w in Windows(guardId))
            {
                if (w.Zone == zone)
                {
                    if (runStart >= 0) { int run = w.FromMs - runStart; if (run > worst) worst = run; }
                    runStart = w.ToMs;
                }
            }
            if (runStart >= 0 && _lengthMs - runStart > worst) worst = _lengthMs - runStart;
            if (runStart < 0) worst = _lengthMs;
            return worst;
        }

        private int LocalTime(GuardState s, int ms)
        {
            int segStart = 0;
            int phase = s.BasePhaseMs;
            foreach (ShiftChangeDef sc in s.Shifts)
            {
                if (sc.atMs <= 0 || sc.atMs > ms) continue;
                segStart = sc.atMs;
                phase = Mod(sc.newPhaseMs, s.CycleMs);
            }
            return Mod(phase + (ms - segStart), s.CycleMs);
        }

        private static int LegIndexOfLocal(GuardState s, LegDef[] legs, int local)
        {
            for (int i = legs.Length - 1; i >= 0; i--) if (local >= s.LegStartMs[i]) return i;
            return 0;
        }

        private static int Mod(int value, int m)
        {
            int r = value % m;
            return r < 0 ? r + m : r;
        }
    }
}
