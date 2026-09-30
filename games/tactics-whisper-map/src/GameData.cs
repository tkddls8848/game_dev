using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Whisper.Data
{
    /// <summary>
    /// data/ 의 JSON 여섯 장을 읽어 한 벌로 묶는다.
    ///
    /// 테스트 산출물이 games/&lt;슬러그&gt;/TestBuild/... 에 놓이므로 실행 폴더에서 위로 올라가며
    /// data/map.json 을 찾는다. 경로를 상수로 박으면 실행 위치가 바뀌는 순간 죽는다.
    /// </summary>
    public sealed class GameData
    {
        public MapFile Maps { get; private set; }
        public PatrolFile Patrols { get; private set; }
        public SquadFile Squad { get; private set; }
        public VillageFile Village { get; private set; }
        public MissionFile Missions { get; private set; }
        public BalanceFile Balance { get; private set; }

        public string DataRoot { get; private set; }

        private readonly Dictionary<string, MapDef> _maps = new Dictionary<string, MapDef>();
        private readonly Dictionary<string, GuardDef> _guards = new Dictionary<string, GuardDef>();
        private readonly Dictionary<string, ShiftChangeDef> _shifts = new Dictionary<string, ShiftChangeDef>();
        private readonly Dictionary<string, MemberDef> _members = new Dictionary<string, MemberDef>();
        private readonly Dictionary<string, VillagerDef> _villagers = new Dictionary<string, VillagerDef>();
        private readonly Dictionary<string, IntelItemDef> _items = new Dictionary<string, IntelItemDef>();
        private readonly Dictionary<string, MissionDef> _missions = new Dictionary<string, MissionDef>();
        private readonly Dictionary<string, TargetDef> _targets = new Dictionary<string, TargetDef>();
        private readonly Dictionary<string, DocumentDef> _documents = new Dictionary<string, DocumentDef>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Maps = Read<MapFile>(dataRoot, "map.json");
            d.Patrols = Read<PatrolFile>(dataRoot, "patrols.json");
            d.Squad = Read<SquadFile>(dataRoot, "squad.json");
            d.Village = Read<VillageFile>(dataRoot, "village.json");
            d.Missions = Read<MissionFile>(dataRoot, "mission.json");
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

        /// <summary>실행 폴더에서 위로 올라가며 data/map.json 을 가진 폴더를 찾는다.</summary>
        public static string FindDataRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "data");
                if (File.Exists(Path.Combine(candidate, "village.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "data/village.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            foreach (MapDef m in Maps.maps) _maps[m.id] = m;
            foreach (GuardDef g in Patrols.guards) _guards[g.id] = g;
            foreach (ShiftChangeDef s in Patrols.shiftChanges) _shifts[s.id] = s;
            foreach (MemberDef m in Squad.members) _members[m.id] = m;
            foreach (VillagerDef v in Village.villagers) _villagers[v.id] = v;
            foreach (IntelItemDef i in Village.items) _items[i.id] = i;
            foreach (MissionDef m in Missions.missions) _missions[m.id] = m;
            foreach (TargetDef t in Missions.targets) _targets[t.id] = t;
            foreach (DocumentDef doc in Missions.documents) _documents[doc.id] = doc;
        }

        public MapDef Map(string id) { return Get(_maps, id, "map"); }
        public GuardDef Guard(string id) { return Get(_guards, id, "guard"); }
        public ShiftChangeDef ShiftChange(string id) { return Get(_shifts, id, "shiftChange"); }
        public MemberDef Member(string id) { return Get(_members, id, "member"); }
        public VillagerDef Villager(string id) { return Get(_villagers, id, "villager"); }
        public IntelItemDef Item(string id) { return Get(_items, id, "intelItem"); }
        public MissionDef Mission(string id) { return Get(_missions, id, "mission"); }
        public TargetDef Target(string id) { return Get(_targets, id, "target"); }
        public DocumentDef Document(string id) { return Get(_documents, id, "document"); }

        public bool HasTarget(string id) { return !string.IsNullOrEmpty(id) && _targets.ContainsKey(id); }
        public bool HasDocument(string id) { return !string.IsNullOrEmpty(id) && _documents.ContainsKey(id); }
        public bool HasItem(string id) { return !string.IsNullOrEmpty(id) && _items.ContainsKey(id); }

        public IList<MissionDef> AllMissions { get { return Missions.missions; } }
        public IList<MemberDef> AllMembers { get { return Squad.members; } }
        public IList<VillagerDef> AllVillagers { get { return Village.villagers; } }
        public IList<IntelItemDef> AllItems { get { return Village.items; } }

        /// <summary>회차에 참여하는 순찰병들. mission.guardIds 순서를 그대로 쓴다(결정적).</summary>
        public List<GuardDef> GuardsOf(MissionDef mission)
        {
            List<GuardDef> list = new List<GuardDef>();
            foreach (string id in mission.guardIds) list.Add(Guard(id));
            return list;
        }

        public List<ShiftChangeDef> ShiftChangesOf(MissionDef mission)
        {
            List<ShiftChangeDef> list = new List<ShiftChangeDef>();
            if (mission.shiftChangeIds == null) return list;
            foreach (string id in mission.shiftChangeIds) list.Add(ShiftChange(id));
            return list;
        }

        public MemberActionDef ActionOf(string memberId, string kind)
        {
            MemberDef m = Member(memberId);
            if (m.actions == null) return null;
            foreach (MemberActionDef a in m.actions) if (a.kind == kind) return a;
            return null;
        }

        /// <summary>이 항목을 아는 마을 사람들. villagers 선언 순서를 지킨다(결정적).</summary>
        public List<VillagerDef> SourcesOf(string itemId)
        {
            List<VillagerDef> list = new List<VillagerDef>();
            foreach (VillagerDef v in Village.villagers)
            {
                if (v.knows == null) continue;
                foreach (string k in v.knows) if (k == itemId) { list.Add(v); break; }
            }
            return list;
        }

        /// <summary>이 순찰병/교대에 대한 항목을 찾는다. 없으면 null — 그게 '알 길이 없는 것'이다.</summary>
        public IntelItemDef ItemFor(string kind, string subjectId)
        {
            foreach (IntelItemDef i in Village.items)
                if (i.kind == kind && i.subjectId == subjectId) return i;
            return null;
        }

        private static T Get<T>(Dictionary<string, T> table, string id, string what)
        {
            T value;
            if (id != null && table.TryGetValue(id, out value)) return value;
            throw new KeyNotFoundException(what + " 참조가 깨졌다: '" + id + "'");
        }
    }
}
