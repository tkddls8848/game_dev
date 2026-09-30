using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Scent.Data
{
    /// <summary>
    /// data/ 의 JSON 넉 장을 한 벌로 묶는다.
    /// 테스트 산출물이 games/scent-layers/TestBuild/... 에 놓이므로 실행 폴더에서 위로
    /// 올라가며 data/notes.json 을 찾는다. 경로를 상수로 박으면 실행 위치가 바뀌는 순간 죽는다.
    /// </summary>
    public sealed class GameData
    {
        public NotesFile Notes { get; private set; }
        public HouseFile House { get; private set; }
        public ActorsFile Actors { get; private set; }
        public DayFile Day { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, int> _noteIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, NoteDef> _notes = new Dictionary<string, NoteDef>();
        private readonly Dictionary<string, RoomDef> _rooms = new Dictionary<string, RoomDef>();
        private readonly Dictionary<string, ActorDef> _actors = new Dictionary<string, ActorDef>();
        private readonly Dictionary<string, VisitDef> _visits = new Dictionary<string, VisitDef>();
        private int[,] _walk;
        private List<string> _roomIds;

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Notes = Read<NotesFile>(dataRoot, "notes.json");
            d.House = Read<HouseFile>(dataRoot, "house.json");
            d.Actors = Read<ActorsFile>(dataRoot, "actors.json");
            d.Day = Read<DayFile>(dataRoot, "day.json");
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
                if (File.Exists(Path.Combine(candidate, "notes.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/notes.json 을 위로 올라가며 찾지 못했다: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            for (int i = 0; i < Notes.notes.Count; i++)
            {
                _noteIndex[Notes.notes[i].id] = i;
                _notes[Notes.notes[i].id] = Notes.notes[i];
            }
            _roomIds = new List<string>();
            foreach (RoomDef r in House.rooms) { _rooms[r.id] = r; _roomIds.Add(r.id); }
            foreach (ActorDef a in Actors.actors) _actors[a.id] = a;
            foreach (VisitDef v in Day.visits) _visits[v.id] = v;
            BuildWalk();
        }

        // ── 조회 ──────────────────────────────────────────────────────────────
        public int NoteCount { get { return Notes.notes.Count; } }
        public int NoteIndex(string id) { return _noteIndex[id]; }
        public bool HasNote(string id) { return _noteIndex.ContainsKey(id); }
        public NoteDef Note(int i) { return Notes.notes[i]; }
        public NoteDef Note(string id) { return _notes[id]; }
        public bool HasRoom(string id) { return _rooms.ContainsKey(id); }
        public RoomDef Room(string id) { return _rooms[id]; }
        public IList<string> RoomIds { get { return _roomIds; } }
        public bool HasActor(string id) { return _actors.ContainsKey(id); }
        public ActorDef Actor(string id) { return _actors[id]; }
        public VisitDef Visit(string id) { return _visits[id]; }
        public int StepMin { get { return Notes.decayStepMin; } }

        /// <summary>두 방 사이의 최단 도보 시간(분). 같은 방이면 0.</summary>
        public int Walk(string from, string to)
        {
            return _walk[_roomIds.IndexOf(from), _roomIds.IndexOf(to)];
        }

        private void BuildWalk()
        {
            int n = _roomIds.Count;
            const int INF = 1 << 20;
            _walk = new int[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++) _walk[i, j] = (i == j) ? 0 : INF;
            foreach (EdgeDef e in House.edges)
            {
                int a = _roomIds.IndexOf(e.a), b = _roomIds.IndexOf(e.b);
                if (a < 0 || b < 0) continue;
                if (e.walkMin < _walk[a, b]) { _walk[a, b] = e.walkMin; _walk[b, a] = e.walkMin; }
            }
            for (int k = 0; k < n; k++)
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (_walk[i, k] + _walk[k, j] < _walk[i, j]) _walk[i, j] = _walk[i, k] + _walk[k, j];
        }

        /// <summary>이 사람이 이 계열에 남기는 처음 세기 (100%일 때). 비율은 플레이어도 안다.</summary>
        public int ProfileStrength(ActorDef a, string noteId)
        {
            foreach (NoteWeight w in a.profile)
                if (w.note == noteId) return (int)((long)Day.depositUnits * w.permille / 1000);
            return 0;
        }
    }
}
