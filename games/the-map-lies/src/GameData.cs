using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MapLies.Data
{
    /// <summary>
    /// data/ 의 JSON 넷을 읽어 한 벌로 묶는다.
    ///
    /// 테스트 산출물이 games/the-map-lies/TestBuild/... 에 놓이므로 실행 폴더에서 위로 올라가며
    /// data/city.json 을 찾는다. 경로를 상수로 박으면 실행 위치가 바뀌는 순간 죽는다.
    /// </summary>
    public sealed class GameData
    {
        public CityFile City { get; private set; }
        public ChapterFile Chapters { get; private set; }
        public ScenarioFile Scenarios { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, ChapterDef> _chapters = new Dictionary<string, ChapterDef>();
        private readonly Dictionary<string, MistakeDef> _mistakes = new Dictionary<string, MistakeDef>();
        private readonly Dictionary<string, CitizenDef> _citizens = new Dictionary<string, CitizenDef>();
        private readonly Dictionary<string, DistrictDef> _districts = new Dictionary<string, DistrictDef>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.City = Read<CityFile>(dataRoot, "city.json");
            d.Chapters = Read<ChapterFile>(dataRoot, "chapters.json");
            d.Scenarios = Read<ScenarioFile>(dataRoot, "scenarios.json");
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
                if (File.Exists(Path.Combine(candidate, "city.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/city.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            foreach (ChapterDef c in Chapters.chapters) _chapters[c.id] = c;
            foreach (MistakeDef m in Scenarios.mistakes) _mistakes[m.id] = m;
            foreach (CitizenDef c in City.citizens) _citizens[c.id] = c;
            if (City.districts != null) foreach (DistrictDef d in City.districts) _districts[d.id] = d;
        }

        public ChapterDef Chapter(string id) { return Get(_chapters, id, "chapter"); }
        public MistakeDef Mistake(string id) { return Get(_mistakes, id, "mistake"); }
        public CitizenDef Citizen(string id) { return Get(_citizens, id, "citizen"); }
        public DistrictDef District(string id) { return Get(_districts, id, "district"); }
        public bool HasCitizen(string id) { return _citizens.ContainsKey(id); }

        public IList<ChapterDef> AllChapters { get { return Chapters.chapters; } }
        public IList<MistakeDef> AllMistakes { get { return Scenarios.mistakes; } }
        public IList<CitizenDef> AllCitizens { get { return City.citizens; } }

        /// <summary>칸 종류마다 다시 짓는 값이 다르다. 광장이 가장 싸고 건물이 가장 비싸다.</summary>
        public int FlipCost(char kind)
        {
            if (kind == Sim.Kinds.Block) return Balance.flipCostBlock;
            if (kind == Sim.Kinds.Street) return Balance.flipCostStreet;
            if (kind == Sim.Kinds.Plaza) return Balance.flipCostPlaza;
            return int.MaxValue;
        }

        /// <summary>지도에 한 칸을 그리는 값(허가비).</summary>
        public int EditCost(char kind)
        {
            if (kind == Sim.Kinds.Block) return Balance.editCostBlock;
            if (kind == Sim.Kinds.Street) return Balance.editCostStreet;
            if (kind == Sim.Kinds.Plaza) return Balance.editCostPlaza;
            return int.MaxValue;
        }

        private static T Get<T>(Dictionary<string, T> map, string id, string what)
        {
            T v;
            if (!map.TryGetValue(id, out v)) throw new KeyNotFoundException("없는 " + what + ": " + id);
            return v;
        }
    }
}
