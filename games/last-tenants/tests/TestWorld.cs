using System.Collections.Generic;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// 테스트가 같이 쓰는 세계. data/ 를 한 번만 읽고, 전수 탐색 결과를 재사용한다.
    /// 탐색이 이 PoC에서 가장 비싼 일이다 — 건물 하나 씨드 하나마다 계획을 수만 개 본다.
    /// </summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<string, ChoiceMatrix> _matrices =
            new Dictionary<string, ChoiceMatrix>();

        public static GameData Data
        {
            get { if (_data == null) _data = GameData.Load(); return _data; }
        }

        public static int SeedSweep { get { return Data.Balance.night.seedSweep; } }

        public static List<string> BuildingIds()
        {
            List<string> ids = new List<string>();
            foreach (BuildingDef b in Data.AllBuildings) ids.Add(b.id);
            return ids;
        }

        public static Night Night(string buildingId, int seed)
        {
            return Sim.Night.Resolve(Data, buildingId, seed);
        }

        public static Night NightSilenceAsSound(string buildingId, int seed)
        {
            return Sim.Night.Resolve(Data, buildingId, seed, true);
        }

        /// <summary>침묵이 켜진 세계의 선택지 표. 무겁다 — 한 번만 돈다.</summary>
        public static ChoiceMatrix Matrix(string buildingId, int seed)
        {
            string key = buildingId + "#" + seed;
            ChoiceMatrix m;
            if (_matrices.TryGetValue(key, out m)) return m;
            m = ChoiceMatrix.Build(Night(buildingId, seed));
            _matrices[key] = m;
            return m;
        }

        /// <summary>침묵한 문에 귀를 대지 못하게 막은 세계의 표.</summary>
        public static ChoiceMatrix MatrixNoSilentDoors(string buildingId, int seed)
        {
            string key = buildingId + "#" + seed + "#noSilent";
            ChoiceMatrix m;
            if (_matrices.TryGetValue(key, out m)) return m;
            SearchOptions opt = new SearchOptions();
            opt.ForbidSilentDoors = true;
            m = ChoiceMatrix.Build(Night(buildingId, seed), opt);
            _matrices[key] = m;
            return m;
        }

        /// <summary>침묵을 소리 있는 칸과 똑같이 다루는 대조 세계의 표.</summary>
        public static ChoiceMatrix MatrixSilenceAsSound(string buildingId, int seed)
        {
            string key = buildingId + "#" + seed + "#asSound";
            ChoiceMatrix m;
            if (_matrices.TryGetValue(key, out m)) return m;
            m = ChoiceMatrix.Build(NightSilenceAsSound(buildingId, seed));
            _matrices[key] = m;
            return m;
        }

        /// <summary>그 건물의 침묵 종류별 세대. 없으면 null.</summary>
        public static HouseholdDef Silent(string buildingId, string kind)
        {
            return Data.SilentOfKind(buildingId, kind);
        }
    }
}
