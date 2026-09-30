using System;
using System.Collections.Generic;
using System.IO;
using Interp.Data;
using Newtonsoft.Json;

namespace Interp
{
    /// <summary>
    /// data/ 의 JSON 을 읽어 한 벌로 묶는다. 테스트는 이 클래스만 보면 된다.
    /// 경로는 실행 폴더에서 위로 올라가며 `data/` 를 찾는다 — csproj 가 산출물을
    /// PoC 폴더 안의 TestBuild/ 에 두므로 조상 중에 PoC 뿌리가 있다.
    /// </summary>
    public sealed class GameData
    {
        public NationFile Nations;
        public TreatyFile Treaty;
        public SessionFile Session;
        public EndingFile Endings;
        public BalanceFile Balance;

        private readonly Dictionary<string, ClauseDef> _clauses = new Dictionary<string, ClauseDef>();
        private readonly Dictionary<string, VariantDef> _variants = new Dictionary<string, VariantDef>();
        private readonly Dictionary<string, string> _variantClause = new Dictionary<string, string>();
        private readonly Dictionary<string, MisunderstandingDef> _misread = new Dictionary<string, MisunderstandingDef>();
        private readonly Dictionary<string, NationDef> _nations = new Dictionary<string, NationDef>();

        public static string DataRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, "data");
                if (File.Exists(Path.Combine(candidate, "session.json"))) return candidate;
                DirectoryInfo parent = Directory.GetParent(dir);
                dir = parent == null ? null : parent.FullName;
            }
            throw new FileNotFoundException("data/session.json 을 찾지 못했다. TestBuild/ 가 PoC 뿌리 안에 있어야 한다.");
        }

        private static T Read<T>(string root, string name)
        {
            string path = Path.Combine(root, name);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path, System.Text.Encoding.UTF8));
        }

        public static GameData Load()
        {
            string root = DataRoot();
            GameData d = new GameData();
            d.Nations = Read<NationFile>(root, "nations.json");
            d.Treaty = Read<TreatyFile>(root, "treaty.json");
            d.Session = Read<SessionFile>(root, "session.json");
            d.Endings = Read<EndingFile>(root, "endings.json");
            d.Balance = Read<BalanceFile>(root, "balance.json");
            d.Index();
            return d;
        }

        private void Index()
        {
            foreach (NationDef n in Nations.nations) _nations[n.id] = n;
            foreach (ClauseDef c in Treaty.clauses)
            {
                _clauses[c.id] = c;
                foreach (VariantDef v in c.variants)
                {
                    _variants[v.id] = v;
                    _variantClause[v.id] = c.id;
                }
            }
            foreach (MisunderstandingDef m in Session.misunderstandings) _misread[m.id] = m;
        }

        public IList<ClauseDef> Clauses { get { return Treaty.clauses; } }
        public IList<StageDef> Stages { get { return Session.stages; } }
        public IList<MisunderstandingDef> Misunderstandings { get { return Session.misunderstandings; } }

        public ClauseDef Clause(string id)
        {
            ClauseDef c;
            return _clauses.TryGetValue(id, out c) ? c : null;
        }

        public VariantDef Variant(string id)
        {
            VariantDef v;
            return _variants.TryGetValue(id, out v) ? v : null;
        }

        public string ClauseOfVariant(string variantId)
        {
            string c;
            return _variantClause.TryGetValue(variantId, out c) ? c : null;
        }

        public MisunderstandingDef Misread(string id)
        {
            MisunderstandingDef m;
            return _misread.TryGetValue(id, out m) ? m : null;
        }

        public NationDef Nation(string id)
        {
            NationDef n;
            return _nations.TryGetValue(id, out n) ? n : null;
        }

        public ObjectiveDef Objective(string id)
        {
            foreach (ObjectiveDef o in Balance.objectives) if (o.id == id) return o;
            return null;
        }

        /// <summary>이 PoC가 쓰는 씨드 전부. 정책 비교는 한 씨드로 하면 안 된다.</summary>
        public IList<int> AllSeeds()
        {
            List<int> seeds = new List<int> { Balance.seed };
            if (Balance.extraSeeds != null) seeds.AddRange(Balance.extraSeeds);
            return seeds;
        }
    }
}
