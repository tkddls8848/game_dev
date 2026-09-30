using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RedPen.Data;

namespace RedPen
{
    /// <summary>
    /// data/ 의 JSON 을 읽어 한 벌로 묶는다. 경로는 실행 폴더에서 위로 올라가며 `data/` 를 찾는다 —
    /// csproj 가 산출물을 PoC 폴더 안의 TestBuild/ 에 두므로 조상 중에 PoC 뿌리가 있다.
    /// </summary>
    public sealed class GameData
    {
        public ManuscriptFile Manuscript;
        public MarkFile Marks;
        public AuthorFile Author;
        public BalanceFile Balance;
        public EndingFile Endings;

        private readonly Dictionary<string, MarkDef> _marks = new Dictionary<string, MarkDef>();
        private readonly Dictionary<string, FlawDef> _flaws = new Dictionary<string, FlawDef>();
        private readonly Dictionary<string, SentenceDef> _sentences = new Dictionary<string, SentenceDef>();

        public static string DataRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, "data");
                if (File.Exists(Path.Combine(candidate, "manuscript.json"))) return candidate;
                DirectoryInfo parent = Directory.GetParent(dir);
                dir = parent == null ? null : parent.FullName;
            }
            throw new FileNotFoundException("data/manuscript.json 을 찾지 못했다. TestBuild/ 가 PoC 뿌리 안에 있어야 한다.");
        }

        private static T Read<T>(string root, string name)
        {
            return JsonConvert.DeserializeObject<T>(
                File.ReadAllText(Path.Combine(root, name), System.Text.Encoding.UTF8));
        }

        public static GameData Load()
        {
            string root = DataRoot();
            GameData d = new GameData();
            d.Manuscript = Read<ManuscriptFile>(root, "manuscript.json");
            d.Marks = Read<MarkFile>(root, "marks.json");
            d.Author = Read<AuthorFile>(root, "author.json");
            d.Balance = Read<BalanceFile>(root, "balance.json");
            d.Endings = Read<EndingFile>(root, "endings.json");
            foreach (MarkDef m in d.Marks.marks) d._marks[m.id] = m;
            foreach (FlawDef f in d.Manuscript.flaws) d._flaws[f.id] = f;
            foreach (SentenceDef s in d.Manuscript.sentences) d._sentences[s.id] = s;
            return d;
        }

        public IList<MarkDef> AllMarks { get { return Marks.marks; } }
        public IList<SentenceDef> AllSentences { get { return Manuscript.sentences; } }
        public IList<FlawDef> AllFlaws { get { return Manuscript.flaws; } }

        public MarkDef Mark(string id) { MarkDef m; return _marks.TryGetValue(id, out m) ? m : null; }
        public FlawDef Flaw(string id) { FlawDef f; return _flaws.TryGetValue(id, out f) ? f : null; }
        public SentenceDef Sentence(string id) { SentenceDef s; return _sentences.TryGetValue(id, out s) ? s : null; }

        public ObjectiveDef Objective(string id)
        {
            foreach (ObjectiveDef o in Balance.objectives) if (o.id == id) return o;
            return null;
        }

        public IList<int> AllSeeds()
        {
            List<int> seeds = new List<int> { Balance.seed };
            if (Balance.extraSeeds != null) seeds.AddRange(Balance.extraSeeds);
            return seeds;
        }
    }
}
