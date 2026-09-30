using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SilentBaton.Data
{
    /// <summary>
    /// data/ 의 JSON 다섯을 읽어 한 벌로 묶는다.
    /// 실행 폴더에서 위로 올라가며 data/score.json 을 찾는다 —
    /// 경로를 상수로 박으면 실행 위치가 바뀌는 순간 죽는다.
    /// </summary>
    public sealed class GameData
    {
        public SectionFile Sections { get; private set; }
        public ScoreFile Score { get; private set; }
        public CueFile Cues { get; private set; }
        public PressFile Press { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, SectionDef> _sections = new Dictionary<string, SectionDef>();
        private readonly Dictionary<string, int> _sectionIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, PieceDef> _pieces = new Dictionary<string, PieceDef>();
        private readonly Dictionary<string, CueDef> _cues = new Dictionary<string, CueDef>();
        private readonly Dictionary<string, NoteDef> _notes = new Dictionary<string, NoteDef>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Sections = Read<SectionFile>(dataRoot, "sections.json");
            d.Score = Read<ScoreFile>(dataRoot, "score.json");
            d.Cues = Read<CueFile>(dataRoot, "cues.json");
            d.Press = Read<PressFile>(dataRoot, "press.json");
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
                if (File.Exists(Path.Combine(candidate, "score.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/score.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            for (int i = 0; i < Sections.sections.Length; i++)
            {
                _sections[Sections.sections[i].id] = Sections.sections[i];
                _sectionIndex[Sections.sections[i].id] = i;
            }
            foreach (PieceDef p in Score.pieces) _pieces[p.id] = p;
            foreach (CueDef c in Cues.cues) _cues[c.id] = c;
            foreach (NoteDef n in Press.notes) _notes[n.id] = n;
        }

        public SectionDef Section(string id) { return Get(_sections, id, "section"); }
        public int SectionIndex(string id) { return Get(_sectionIndex, id, "section"); }
        public PieceDef Piece(string id) { return Get(_pieces, id, "piece"); }
        public CueDef Cue(string id) { return Get(_cues, id, "cue"); }
        public NoteDef Note(string id) { return Get(_notes, id, "note"); }

        public SectionDef[] AllSections { get { return Sections.sections; } }
        public PieceDef[] AllPieces { get { return Score.pieces; } }
        public CueDef[] AllCues { get { return Cues.cues; } }
        public int SectionCount { get { return Sections.sections.Length; } }

        public List<string> PieceIds()
        {
            List<string> ids = new List<string>();
            foreach (PieceDef p in Score.pieces) ids.Add(p.id);
            return ids;
        }

        public NoteDef NoteOfKind(string kind)
        {
            foreach (NoteDef n in Press.notes) if (n.kind == kind) return n;
            throw new KeyNotFoundException("그런 지적이 없다: " + kind);
        }

        /// <summary>총점으로 등급을 찾는다. grades 는 minTotal 내림차순으로 적혀 있다.</summary>
        public GradeDef GradeOf(int total)
        {
            foreach (GradeDef g in Press.grades) if (total >= g.minTotal) return g;
            return Press.grades[Press.grades.Length - 1];
        }

        /// <summary>박 하나가 기준박의 몇 %인가. 느린 박에서 몸이 더 벌어진다.</summary>
        public int BeatScalePercent(BarDef bar) { return bar.beatMs * 100 / Score.referenceBeatMs; }

        private static T Get<T>(Dictionary<string, T> table, string id, string what)
        {
            T value;
            if (id != null && table.TryGetValue(id, out value)) return value;
            throw new KeyNotFoundException(what + " 참조가 깨졌다: '" + id + "'");
        }
    }
}
