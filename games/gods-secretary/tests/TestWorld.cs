using System.Collections.Generic;
using Secretary.Data;
using Secretary.Sim;

namespace Secretary.Tests
{
    /// <summary>테스트가 같이 쓰는 세계. data/ 를 한 번만 읽고 빔 탐색 결과를 재사용한다.</summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<string, SimState> _best = new Dictionary<string, SimState>();

        public static GameData Data
        {
            get { if (_data == null) _data = GameData.Load(); return _data; }
        }

        public static int SeedSweep { get { return Data.Balance.night.seedSweep; } }
        public static List<string> VolumeIds() { return Data.VolumeIds(); }

        /// <summary>알려진 최선(빔 탐색). 무겁다 — 한 번만 돈다.</summary>
        public static SimState Best(string volumeId, int seed)
        {
            string key = volumeId + "#" + seed;
            SimState s;
            if (_best.TryGetValue(key, out s)) return s;
            s = PlanSearch.Best(Data, volumeId, seed);
            _best[key] = s;
            return s;
        }

        /// <summary>보존을 끈 대조 세계의 알려진 최선.</summary>
        public static SimState BestNoConservation(string volumeId, int seed)
        {
            string key = volumeId + "#" + seed + "#free";
            SimState s;
            if (_best.TryGetValue(key, out s)) return s;
            s = PlanSearch.Best(Data, volumeId, seed, false);
            _best[key] = s;
            return s;
        }

        public static SimState RunPolicy(string volumeId, int seed, Policy p)
        {
            return Policy.Run(Data, volumeId, seed, p);
        }
    }
}
