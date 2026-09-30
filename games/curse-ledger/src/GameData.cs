using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace CurseLedger.Data
{
    /// <summary>
    /// data/ 의 JSON 넷을 읽어 한 벌로 묶는다.
    ///
    /// 테스트 산출물이 games/curse-ledger/TestBuild/... 에 놓이므로 실행 폴더에서 위로 올라가며
    /// data/curse.json 을 찾는다. 경로를 상수로 박으면 실행 위치가 바뀌는 순간 죽는다.
    /// </summary>
    public sealed class GameData
    {
        public KinFile Kin { get; private set; }
        public CurseFile Curse { get; private set; }
        public RiteFile Rites { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, HeirDef> _heirs = new Dictionary<string, HeirDef>();
        private readonly Dictionary<string, VillagerDef> _villagers = new Dictionary<string, VillagerDef>();
        private readonly Dictionary<string, RiteDef> _rites = new Dictionary<string, RiteDef>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Kin = Read<KinFile>(dataRoot, "kin.json");
            d.Curse = Read<CurseFile>(dataRoot, "curse.json");
            d.Rites = Read<RiteFile>(dataRoot, "rites.json");
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
                if (File.Exists(Path.Combine(candidate, "curse.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/curse.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            foreach (HeirDef h in Kin.heirs) _heirs[h.id] = h;
            foreach (VillagerDef v in Kin.villagers) _villagers[v.id] = v;
            foreach (RiteDef r in Rites.rites) _rites[r.id] = r;
        }

        public HeirDef Heir(string id) { return Get(_heirs, id, "heir"); }
        public VillagerDef Villager(string id) { return Get(_villagers, id, "villager"); }
        public RiteDef Rite(string id) { return Get(_rites, id, "rite"); }

        public IList<HeirDef> AllHeirs { get { return Kin.heirs; } }
        public IList<VillagerDef> AllVillagers { get { return Kin.villagers; } }
        public IList<RiteDef> AllRites { get { return Rites.rites; } }

        public List<string> RiteIds()
        {
            List<string> ids = new List<string>();
            foreach (RiteDef r in Rites.rites) ids.Add(r.id);
            return ids;
        }

        /// <summary>이 대의 종손. generationIndex 로 찾는다.</summary>
        public HeirDef HeirOfGeneration(int generation)
        {
            foreach (HeirDef h in Kin.heirs) if (h.generationIndex == generation) return h;
            throw new KeyNotFoundException("그 대의 종손이 족보에 없다: " + generation);
        }

        /// <summary>해당 계층의 사람들. 선언 순서를 지킨다(결정적).</summary>
        public List<VillagerDef> TierRoster(string tier)
        {
            List<VillagerDef> list = new List<VillagerDef>();
            foreach (VillagerDef v in Kin.villagers) if (v.tier == tier) list.Add(v);
            return list;
        }

        private static T Get<T>(Dictionary<string, T> table, string id, string what)
        {
            T value;
            if (id != null && table.TryGetValue(id, out value)) return value;
            throw new KeyNotFoundException(what + " 참조가 깨졌다: '" + id + "'");
        }
    }
}
