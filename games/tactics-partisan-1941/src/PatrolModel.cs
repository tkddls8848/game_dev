using System;
using System.Collections.Generic;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>순찰병이 한 구역에 머무는 구간 하나. [FromMs, ToMs) 반열린 구간이다.</summary>
    public struct PatrolWindow
    {
        public string GuardId;
        public string Zone;
        public int FromMs;
        public int ToMs;

        public bool Contains(int ms) { return ms >= FromMs && ms < ToMs; }
    }

    /// <summary>
    /// 순찰병이 언제 어디 있는가. **난수도 부동소수도 쓰지 않는 순수 함수**다.
    ///
    /// mystery-blackwood의 NpcSchedule과 같은 자리. 다른 점은 순찰이 **되풀이**된다는 것뿐이다
    /// (legs 의 dwellMs 합이 주기). 그래서 며칠 지켜보면 주기가 드러난다 —
    /// 그게 정찰 단계가 성립하는 근거이고, DetectionFairness 가 기대는 성질이다.
    ///
    /// 씨드는 phaseOptionsMs 중 어느 위상으로 오늘 순찰이 시작하는가만 고른다.
    /// 고른 위상 자체는 정찰로 관측된다 — 씨드가 숨은 정보를 만들지 않는다.
    /// </summary>
    public sealed class PatrolModel
    {
        private sealed class GuardState
        {
            public GuardDef Def;
            public int CycleMs;
            public int[] LegStartMs;                 // 누적 시작 시각
            public int BasePhaseMs;
            public List<ShiftChangeDef> Shifts = new List<ShiftChangeDef>();
        }

        private readonly Dictionary<string, GuardState> _guards = new Dictionary<string, GuardState>();
        private readonly List<string> _guardOrder = new List<string>();
        private readonly int _lengthMs;

        public IList<string> GuardIds { get { return _guardOrder; } }
        public int LengthMs { get { return _lengthMs; } }

        public PatrolModel(GameData data, MissionDef mission)
        {
            _lengthMs = mission.lengthMs;

            // 씨드는 위상 선택에만 쓴다. guardIds 순서대로 뽑으므로 재현된다.
            Random rng = new Random(mission.seed);

            List<ShiftChangeDef> shifts = data.ShiftChangesOf(mission);

            foreach (string guardId in mission.guardIds)
            {
                GuardDef def = data.Guard(guardId);
                if (def.mapId != mission.mapId)
                    throw new InvalidOperationException(
                        "순찰병 '" + guardId + "' 는 지도 '" + def.mapId + "' 것인데 임무는 '" + mission.mapId + "' 다");

                GuardState s = new GuardState { Def = def };
                s.LegStartMs = new int[def.legs.Length];
                int acc = 0;
                for (int i = 0; i < def.legs.Length; i++)
                {
                    s.LegStartMs[i] = acc;
                    acc += def.legs[i].dwellMs;
                }
                s.CycleMs = acc;
                if (s.CycleMs <= 0)
                    throw new InvalidOperationException("순찰 주기가 0이다: " + guardId);

                int[] options = def.phaseOptionsMs;
                s.BasePhaseMs = Mod(options[rng.Next(options.Length)], s.CycleMs);

                foreach (ShiftChangeDef sc in shifts) if (sc.guardId == guardId) s.Shifts.Add(sc);
                s.Shifts.Sort(delegate (ShiftChangeDef x, ShiftChangeDef y) { return x.atMs.CompareTo(y.atMs); });

                _guards[guardId] = s;
                _guardOrder.Add(guardId);
            }
        }

        public int CycleMs(string guardId) { return _guards[guardId].CycleMs; }
        public int BasePhaseMs(string guardId) { return _guards[guardId].BasePhaseMs; }
        public GuardDef Def(string guardId) { return _guards[guardId].Def; }

        /// <summary>이 순찰병에게 걸린 교대들(시각 순).</summary>
        public IList<ShiftChangeDef> ShiftsOf(string guardId) { return _guards[guardId].Shifts; }

        public string ZoneAt(string guardId, int ms)
        {
            GuardState s = _guards[guardId];
            int local = LocalTime(s, ms);
            for (int i = s.Def.legs.Length - 1; i >= 0; i--)
                if (local >= s.LegStartMs[i]) return s.Def.legs[i].zone;
            return s.Def.legs[0].zone;
        }

        /// <summary>
        /// [0, lengthMs) 를 구역별 구간으로 쪼갠다. 정찰 보고서와 화면 시간축이 같은 것을 읽는다.
        /// </summary>
        public List<PatrolWindow> Windows(string guardId)
        {
            GuardState s = _guards[guardId];
            List<PatrolWindow> windows = new List<PatrolWindow>();

            // 교대로 갈라진 위상 구간들
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
                    int leg = LegIndexOfLocal(s, local);
                    int legEnd = s.LegStartMs[leg] + s.Def.legs[leg].dwellMs;
                    int remaining = legEnd - local;
                    int end = Math.Min(to, t + remaining);
                    windows.Add(new PatrolWindow
                    {
                        GuardId = guardId, Zone = s.Def.legs[leg].zone, FromMs = t, ToMs = end
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

        private static int LegIndexOfLocal(GuardState s, int local)
        {
            for (int i = s.Def.legs.Length - 1; i >= 0; i--) if (local >= s.LegStartMs[i]) return i;
            return 0;
        }

        private static int Mod(int value, int m)
        {
            int r = value % m;
            return r < 0 ? r + m : r;
        }
    }
}
