using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Tactics.Data
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
        public MissionFile Missions { get; private set; }
        public CampFile Camp { get; private set; }
        public BalanceFile Balance { get; private set; }

        public string DataRoot { get; private set; }

        private readonly Dictionary<string, MapDef> _maps = new Dictionary<string, MapDef>();
        private readonly Dictionary<string, GuardDef> _guards = new Dictionary<string, GuardDef>();
        private readonly Dictionary<string, ShiftChangeDef> _shifts = new Dictionary<string, ShiftChangeDef>();
        private readonly Dictionary<string, MemberDef> _members = new Dictionary<string, MemberDef>();
        private readonly Dictionary<string, MissionDef> _missions = new Dictionary<string, MissionDef>();
        private readonly Dictionary<string, TargetDef> _targets = new Dictionary<string, TargetDef>();
        private readonly Dictionary<string, IntelDef> _intel = new Dictionary<string, IntelDef>();
        private readonly Dictionary<string, SupplyDef> _supplies = new Dictionary<string, SupplyDef>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Maps = Read<MapFile>(dataRoot, "map.json");
            d.Patrols = Read<PatrolFile>(dataRoot, "patrols.json");
            d.Squad = Read<SquadFile>(dataRoot, "squad.json");
            d.Missions = Read<MissionFile>(dataRoot, "mission.json");
            d.Camp = Read<CampFile>(dataRoot, "camp.json");
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
                if (File.Exists(Path.Combine(candidate, "map.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "data/map.json 을 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        private void Index()
        {
            foreach (MapDef m in Maps.maps) _maps[m.id] = m;
            foreach (GuardDef g in Patrols.guards) _guards[g.id] = g;
            foreach (ShiftChangeDef s in Patrols.shiftChanges) _shifts[s.id] = s;
            foreach (MemberDef m in Squad.members) _members[m.id] = m;
            foreach (MissionDef m in Missions.missions) _missions[m.id] = m;
            foreach (TargetDef t in Missions.targets) _targets[t.id] = t;
            foreach (IntelDef i in Missions.intel) _intel[i.id] = i;
            foreach (SupplyDef s in Camp.supplies) _supplies[s.id] = s;
        }

        public MapDef Map(string id) { return Get(_maps, id, "map"); }
        public GuardDef Guard(string id) { return Get(_guards, id, "guard"); }
        public ShiftChangeDef ShiftChange(string id) { return Get(_shifts, id, "shiftChange"); }
        public MemberDef Member(string id) { return Get(_members, id, "member"); }
        public MissionDef Mission(string id) { return Get(_missions, id, "mission"); }
        public TargetDef Target(string id) { return Get(_targets, id, "target"); }
        public IntelDef Intel(string id) { return Get(_intel, id, "intel"); }
        public SupplyDef Supply(string id) { return Get(_supplies, id, "supply"); }

        public bool HasTarget(string id) { return _targets.ContainsKey(id); }
        public bool HasIntel(string id) { return _intel.ContainsKey(id); }

        public IList<MissionDef> AllMissions { get { return Missions.missions; } }
        public IList<MemberDef> AllMembers { get { return Squad.members; } }

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

        /// <summary>이 지도에 놓인 폭파 목표들.</summary>
        public List<TargetDef> TargetsOnMap(string mapId)
        {
            List<TargetDef> list = new List<TargetDef>();
            foreach (TargetDef t in Missions.targets) if (t.mapId == mapId) list.Add(t);
            return list;
        }

        public List<IntelDef> IntelOnMap(string mapId)
        {
            List<IntelDef> list = new List<IntelDef>();
            foreach (IntelDef i in Missions.intel) if (i.mapId == mapId) list.Add(i);
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
