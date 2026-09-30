using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Secretary.Data
{
    /// <summary>
    /// data/ 의 JSON 다섯 장을 한 벌로 묶는다.
    /// 테스트 산출물이 games/gods-secretary/TestBuild/... 에 놓이므로 실행 폴더에서 위로 올라가며 data/ 를 찾는다.
    /// </summary>
    public sealed class GameData
    {
        public DomainFile Domains { get; private set; }
        public SoulFile Souls { get; private set; }
        public LedgerFile Ledgers { get; private set; }
        public PrayerFile Prayers { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, DomainDef> _domains = new Dictionary<string, DomainDef>();
        private readonly Dictionary<string, SoulDef> _souls = new Dictionary<string, SoulDef>();
        private readonly Dictionary<string, VolumeDef> _volumes = new Dictionary<string, VolumeDef>();
        private readonly Dictionary<string, PrayerDef> _prayers = new Dictionary<string, PrayerDef>();
        private readonly Dictionary<string, List<PrayerDef>> _byVolume = new Dictionary<string, List<PrayerDef>>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Domains = Read<DomainFile>(dataRoot, "domains.json");
            d.Souls = Read<SoulFile>(dataRoot, "souls.json");
            d.Ledgers = Read<LedgerFile>(dataRoot, "ledgers.json");
            d.Prayers = Read<PrayerFile>(dataRoot, "prayers.json");
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
                if (File.Exists(Path.Combine(candidate, "ledgers.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/ledgers.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            foreach (DomainDef x in Domains.domains) _domains[x.id] = x;
            foreach (SoulDef x in Souls.souls) _souls[x.id] = x;
            foreach (VolumeDef v in Ledgers.volumes) { _volumes[v.id] = v; _byVolume[v.id] = new List<PrayerDef>(); }
            foreach (PrayerDef p in Prayers.prayers)
            {
                _prayers[p.id] = p;
                List<PrayerDef> list;
                if (_byVolume.TryGetValue(p.volumeId, out list)) list.Add(p);
            }
            // 씨드 소모 순서를 JSON 줄 순서에서 떼어 낸다 — id 로 정렬해 고정한다.
            foreach (KeyValuePair<string, List<PrayerDef>> kv in _byVolume)
                kv.Value.Sort(delegate (PrayerDef a, PrayerDef b) { return string.CompareOrdinal(a.id, b.id); });
        }

        public DomainDef Domain(string id) { return _domains[id]; }
        public bool HasDomain(string id) { return _domains.ContainsKey(id); }
        public SoulDef Soul(string id) { return _souls[id]; }
        public bool HasSoul(string id) { return _souls.ContainsKey(id); }
        public VolumeDef Volume(string id) { return _volumes[id]; }
        public PrayerDef Prayer(string id) { return _prayers[id]; }

        public IList<DomainDef> AllDomains { get { return Domains.domains; } }
        public IList<SoulDef> AllSouls { get { return Souls.souls; } }
        public IList<VolumeDef> AllVolumes { get { return Ledgers.volumes; } }
        public IList<PrayerDef> AllPrayers { get { return Prayers.prayers; } }
        public IList<PrayerDef> PrayersOf(string volumeId) { return _byVolume[volumeId]; }

        public List<string> VolumeIds()
        {
            List<string> ids = new List<string>();
            foreach (VolumeDef v in AllVolumes) ids.Add(v.id);
            return ids;
        }
    }
}
