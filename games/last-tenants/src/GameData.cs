using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Tenants.Data
{
    /// <summary>
    /// data/ 의 JSON 다섯 장을 한 벌로 묶는다.
    ///
    /// 테스트 산출물이 games/last-tenants/TestBuild/... 에 놓이므로 실행 폴더에서 위로 올라가며
    /// data/ 를 찾는다. 경로를 상수로 박으면 실행 위치가 바뀌는 순간 죽는다.
    /// </summary>
    public sealed class GameData
    {
        public NeedFile Needs { get; private set; }
        public BuildingFile Buildings { get; private set; }
        public HouseholdFile Households { get; private set; }
        public CueFile Cues { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, NeedDef> _needs = new Dictionary<string, NeedDef>();
        private readonly Dictionary<string, BuildingDef> _buildings = new Dictionary<string, BuildingDef>();
        private readonly Dictionary<string, HouseholdDef> _households = new Dictionary<string, HouseholdDef>();
        private readonly Dictionary<string, UnitDef> _units = new Dictionary<string, UnitDef>();
        private readonly Dictionary<string, CueDef> _cues = new Dictionary<string, CueDef>();
        private readonly Dictionary<string, List<HouseholdDef>> _byBuilding =
            new Dictionary<string, List<HouseholdDef>>();
        private readonly Dictionary<string, List<CueDef>> _cuesByBuilding =
            new Dictionary<string, List<CueDef>>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Needs = Read<NeedFile>(dataRoot, "needs.json");
            d.Buildings = Read<BuildingFile>(dataRoot, "buildings.json");
            d.Households = Read<HouseholdFile>(dataRoot, "households.json");
            d.Cues = Read<CueFile>(dataRoot, "cues.json");
            d.Balance = Read<BalanceFile>(dataRoot, "balance.json");
            d.Index();
            return d;
        }

        private static T Read<T>(string root, string file)
        {
            string path = Path.Combine(root, file);
            if (!File.Exists(path)) throw new FileNotFoundException("데이터 파일이 없다: " + path);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        public static string FindDataRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "data");
                if (File.Exists(Path.Combine(candidate, "households.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "data/households.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            foreach (NeedDef n in Needs.needs) _needs[n.id] = n;
            foreach (BuildingDef b in Buildings.buildings)
            {
                _buildings[b.id] = b;
                _byBuilding[b.id] = new List<HouseholdDef>();
                _cuesByBuilding[b.id] = new List<CueDef>();
                foreach (UnitDef u in b.units) _units[u.id] = u;
            }
            foreach (HouseholdDef h in Households.households)
            {
                _households[h.id] = h;
                List<HouseholdDef> list;
                if (_byBuilding.TryGetValue(h.buildingId, out list)) list.Add(h);
            }
            // 건물마다 세대를 호수 순서로 세운다. 이 순서가 배치도의 순서이고 나머지 누적의 순서다.
            foreach (KeyValuePair<string, List<HouseholdDef>> kv in _byBuilding)
                kv.Value.Sort(delegate (HouseholdDef a, HouseholdDef b)
                {
                    return string.CompareOrdinal(Unit(a.unitId).unitNo, Unit(b.unitId).unitNo);
                });
            foreach (CueDef c in Cues.cues)
            {
                _cues[c.id] = c;
                HouseholdDef at;
                if (!_households.TryGetValue(c.atHouseholdId, out at)) continue;
                List<CueDef> list;
                if (_cuesByBuilding.TryGetValue(at.buildingId, out list)) list.Add(c);
            }
            // 씨드 소모 순서를 데이터의 줄 순서에서 떼어 낸다 — id 로 정렬해 고정한다.
            foreach (KeyValuePair<string, List<CueDef>> kv in _cuesByBuilding)
                kv.Value.Sort(delegate (CueDef a, CueDef b) { return string.CompareOrdinal(a.id, b.id); });
        }

        public NeedDef Need(string id) { return _needs[id]; }
        public bool HasNeed(string id) { return _needs.ContainsKey(id); }
        public BuildingDef Building(string id) { return _buildings[id]; }
        public HouseholdDef Household(string id) { return _households[id]; }
        public bool HasHousehold(string id) { return _households.ContainsKey(id); }
        public UnitDef Unit(string id) { return _units[id]; }
        public bool HasUnit(string id) { return _units.ContainsKey(id); }
        public CueDef Cue(string id) { return _cues[id]; }

        public IList<BuildingDef> AllBuildings { get { return Buildings.buildings; } }
        public IList<NeedDef> AllNeeds { get { return Needs.needs; } }
        public IList<HouseholdDef> AllHouseholds { get { return Households.households; } }
        public IList<CueDef> AllCues { get { return Cues.cues; } }

        /// <summary>건물의 세대들. 호수 순서다.</summary>
        public IList<HouseholdDef> HouseholdsOf(string buildingId) { return _byBuilding[buildingId]; }

        /// <summary>건물의 소리들. id 순서다(씨드 소모 순서).</summary>
        public IList<CueDef> CuesOf(string buildingId) { return _cuesByBuilding[buildingId]; }

        /// <summary>
        /// 인접 판정. 같은 층 옆칸이거나 같은 쪽 위아랫칸이면 인접이다. 대각선은 아니다.
        /// 이웃의 말은 인접한 칸에서만 들린다 — 그래서 배치도가 연출이 아니라 규칙이다.
        /// </summary>
        public bool AreAdjacent(string householdA, string householdB)
        {
            if (householdA == householdB) return false;
            HouseholdDef a = Household(householdA), b = Household(householdB);
            if (a.buildingId != b.buildingId) return false;
            UnitDef ua = Unit(a.unitId), ub = Unit(b.unitId);
            if (ua.floor == ub.floor) return true;
            return ua.side == ub.side && Math.Abs(ua.floor - ub.floor) == 1;
        }

        public bool IsSilent(string householdId) { return Household(householdId).voice == Voices.Silent; }

        /// <summary>건물의 세대 중 침묵한 것들. 복도에서 보이는 것이 여기까지다.</summary>
        public List<HouseholdDef> SilentOf(string buildingId)
        {
            List<HouseholdDef> outp = new List<HouseholdDef>();
            foreach (HouseholdDef h in HouseholdsOf(buildingId))
                if (h.voice == Voices.Silent) outp.Add(h);
            return outp;
        }

        /// <summary>그 건물에서 silenceKind 가 맞는 세대. 없으면 null.</summary>
        public HouseholdDef SilentOfKind(string buildingId, string kind)
        {
            foreach (HouseholdDef h in SilentOf(buildingId))
                if (h.silenceKind == kind) return h;
            return null;
        }
    }
}
