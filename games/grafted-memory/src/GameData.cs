using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Graft.Data
{
    /// <summary>
    /// data/ 의 JSON 넷을 읽어 한 벌로 묶는다.
    ///
    /// 테스트 산출물이 games/grafted-memory/TestBuild/... 에 놓이므로 실행 폴더에서 위로 올라가며
    /// data/net.json 을 찾는다. 경로를 상수로 박으면 실행 위치가 바뀌는 순간 죽는다.
    /// </summary>
    public sealed class GameData
    {
        public NetFile Net { get; private set; }
        public NetFile SelfNet { get; private set; }
        public CommissionFile Commissions { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, PersonDef> _people = new Dictionary<string, PersonDef>();
        private readonly Dictionary<string, PlaceDef> _places = new Dictionary<string, PlaceDef>();
        private readonly Dictionary<string, int> _moodIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, MoodDef> _moods = new Dictionary<string, MoodDef>();
        private readonly Dictionary<string, SubjectDef> _subjects = new Dictionary<string, SubjectDef>();
        private readonly Dictionary<string, CommissionDef> _commissions = new Dictionary<string, CommissionDef>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Net = Read<NetFile>(dataRoot, "net.json");
            d.SelfNet = Read<NetFile>(dataRoot, "selfnet.json");
            d.Commissions = Read<CommissionFile>(dataRoot, "commissions.json");
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

        /// <summary>실행 폴더에서 위로 올라가며 data/net.json 을 가진 폴더를 찾는다.</summary>
        public static string FindDataRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "data");
                if (File.Exists(Path.Combine(candidate, "net.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/net.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            foreach (PersonDef p in Net.people) _people[p.id] = p;
            foreach (PlaceDef p in Net.places) _places[p.id] = p;
            for (int i = 0; i < Net.moods.Length; i++)
            {
                _moodIndex[Net.moods[i].id] = i;
                _moods[Net.moods[i].id] = Net.moods[i];
            }
            foreach (SubjectDef s in Net.subjects) _subjects[s.id] = s;
            foreach (SubjectDef s in SelfNet.subjects) _subjects[s.id] = s;
            foreach (CommissionDef c in Commissions.commissions) _commissions[c.id] = c;
        }

        public PersonDef Person(string id) { return Get(_people, id, "person"); }
        public PlaceDef Place(string id) { return Get(_places, id, "place"); }
        public MoodDef Mood(string id) { return Get(_moods, id, "mood"); }
        public SubjectDef Subject(string id) { return Get(_subjects, id, "subject"); }
        public CommissionDef Commission(string id) { return Get(_commissions, id, "commission"); }

        public bool HasPerson(string id) { return _people.ContainsKey(id); }
        public bool HasPlace(string id) { return _places.ContainsKey(id); }
        public bool HasMood(string id) { return _moods.ContainsKey(id); }

        public int MoodIndex(string id)
        {
            int i;
            if (!_moodIndex.TryGetValue(id, out i)) throw new KeyNotFoundException("없는 정서: " + id);
            return i;
        }

        public IList<CommissionDef> AllCommissions { get { return Commissions.commissions; } }
        public IList<MoodDef> AllMoods { get { return Net.moods; } }
        public IList<PersonDef> AllPeople { get { return Net.people; } }
        public IList<PlaceDef> AllPlaces { get { return Net.places; } }

        /// <summary>사람이 그 날에 있었는가. 창은 의뢰서에 적힌 공개 정보다 — 이것으로 죽는 것은 공정하다.</summary>
        public bool PersonPresent(string personId, int dayIndex)
        {
            PersonDef p = Person(personId);
            if (dayIndex < p.presentFromDay) return false;
            if (p.presentUntilDay >= 0 && dayIndex > p.presentUntilDay) return false;
            return true;
        }

        /// <summary>장소가 그 날에 있었는가. 이것도 공개 정보다.</summary>
        public bool PlaceExists(string placeId, int dayIndex)
        {
            PlaceDef p = Place(placeId);
            if (dayIndex < p.existsFromDay) return false;
            if (p.existsUntilDay >= 0 && dayIndex > p.existsUntilDay) return false;
            return true;
        }

        private static T Get<T>(Dictionary<string, T> map, string id, string what)
        {
            T v;
            if (!map.TryGetValue(id, out v)) throw new KeyNotFoundException("없는 " + what + ": " + id);
            return v;
        }
    }
}
