using System.Collections.Generic;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// 테스트가 공유하는 세계. data/ 를 한 번만 읽고, 전수 훑기 결과를 재사용한다.
    /// 전수 훑기가 이 PoC에서 가장 비싼 일이다 — 씨드마다 수십만 회차를 정산한다.
    /// </summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<int, SweepResult> _full = new Dictionary<int, SweepResult>();
        private static readonly Dictionary<int, SweepResult> _bloodless = new Dictionary<int, SweepResult>();

        public static GameData Data
        {
            get
            {
                if (_data == null) _data = GameData.Load();
                return _data;
            }
        }

        public static int Generations { get { return Data.Balance.checkers.exhaustiveGenerations; } }
        public static int[] Seeds { get { return Data.Balance.checkers.noCleanExitSeeds; } }
        public static int MainSeed { get { return Data.Curse.seed; } }

        public static CurseSim Sim(int seed) { return new CurseSim(Data, seed, Generations); }
        public static CurseSim SimNoDeferral(int seed) { return new CurseSim(Data, seed, Generations, false); }

        /// <summary>제례 여섯 전부로 내려가는 전수 훑기.</summary>
        public static SweepResult Full(int seed)
        {
            SweepResult r;
            if (_full.TryGetValue(seed, out r)) return r;
            r = PolicySweep.Exhaustive(Data, seed, Generations);
            _full[seed] = r;
            return r;
        }

        /// <summary>사람을 바치는 제례를 **뺀** 전수 훑기. NoCleanExit 이 이것을 본다.</summary>
        public static SweepResult Bloodless(int seed)
        {
            SweepResult r;
            if (_bloodless.TryGetValue(seed, out r)) return r;
            r = PolicySweep.Restricted(Data, seed, Generations, BloodlessRiteIds());
            _bloodless[seed] = r;
            return r;
        }

        public static List<string> BloodlessRiteIds()
        {
            List<string> ids = new List<string>();
            foreach (RiteDef r in Data.AllRites) if (!r.TakesVictim) ids.Add(r.id);
            return ids;
        }

        public static List<string> VictimRiteIds()
        {
            List<string> ids = new List<string>();
            foreach (RiteDef r in Data.AllRites) if (r.TakesVictim) ids.Add(r.id);
            return ids;
        }

        /// <summary>한 제례만 되풀이하는 회차. 고를 수 없게 되면 그 자리에서 멈춘다.</summary>
        public static LedgerRun MonoRun(int seed, string riteId)
        {
            CurseSim sim = Sim(seed);
            LedgerState s = sim.NewState();
            List<string> taken = new List<string>();
            for (int g = 0; g < Generations && !s.Ended; g++)
            {
                if (!sim.Step(s, riteId)) break;
                taken.Add(riteId);
            }
            return sim.Settle(s, taken.ToArray());
        }
    }
}
