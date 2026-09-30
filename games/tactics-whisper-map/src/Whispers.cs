using System;
using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
{
    /// <summary>마을 사람 하나가 한 항목에 대해 내놓은 답 하나.</summary>
    public sealed class Rumor
    {
        public string ItemId;
        public string VillagerId;
        public string Kind;
        public string SubjectId;
        public bool IsTrue;          // 감사만 본다. 플레이어는 이 칸을 볼 수 없다
        public LegDef[] Route;       // kind == route
        public int PostPhaseMs = -1; // kind == post
        public int ShiftAtMs = -1;   // kind == shift

        /// <summary>두 답이 같은 말을 하는가. 겹쳐 물었을 때 이것만으로 판정한다.</summary>
        public bool SameAs(Rumor other)
        {
            if (other == null || other.ItemId != ItemId) return false;
            if (Kind == IntelKinds.Post) return PostPhaseMs == other.PostPhaseMs;
            if (Kind == IntelKinds.Shift) return ShiftAtMs == other.ShiftAtMs;
            if (Route == null || other.Route == null) return false;
            if (Route.Length != other.Route.Length) return false;
            for (int i = 0; i < Route.Length; i++)
                if (Route[i].zone != other.Route[i].zone || Route[i].dwellMs != other.Route[i].dwellMs) return false;
            return true;
        }

        public override string ToString()
        {
            if (Kind == IntelKinds.Post) return ItemId + "=" + PostPhaseMs + (IsTrue ? "" : "(거짓)");
            if (Kind == IntelKinds.Shift) return ItemId + "=" + ShiftAtMs + (IsTrue ? "" : "(거짓)");
            List<string> zones = new List<string>();
            if (Route != null) foreach (LegDef l in Route) zones.Add(l.zone);
            return ItemId + "=[" + string.Join(">", zones) + "]" + (IsTrue ? "" : "(거짓)");
        }
    }

    /// <summary>
    /// ★ **한 임무의 정보 시장.** 누가 무엇을 알고, 그중 누가 무엇에 대해 거짓을 말하는가.
    ///
    /// **불변식 하나가 이 PoC 전체를 떠받친다:**
    /// > 한 항목에 거짓을 말하는 사람은 **최대 한 명**이다.
    ///
    /// 따라서 두 사람이 같은 답을 하면 그 답은 참이고, 답이 갈리면 그 항목은 믿을 수 없다.
    /// 이 불변식이 없으면 "겹쳐 물어 확인할 수 있었다"가 거짓이 되고, 그러면
    /// **틀린 정보로 실패하는 것이 불공정**해진다 — DetectionFairness 가 무너지는 자리다.
    /// `FalseIntelIsCheckableTests` 가 씨드 전수로 이것을 확인한다.
    ///
    /// 딸린 규칙: 출처가 둘 미만이면(입을 닫은 사람이 있어서) **아무도 거짓을 말하지 않는다.**
    /// 혼자 남은 사람은 확인할 수 없는 소문을 전하지 않는다. 그게 공정의 하한이다.
    /// </summary>
    public sealed class Whispers
    {
        private readonly GameData _data;
        private readonly MissionDef _mission;
        private readonly PhaseAssignment _truth;
        private readonly Dictionary<string, string> _liarOf = new Dictionary<string, string>();
        private readonly Dictionary<string, List<VillagerDef>> _available =
            new Dictionary<string, List<VillagerDef>>();

        public IEnumerable<string> ItemsWithLiar { get { return _liarOf.Keys; } }

        public string LiarOf(string itemId)
        {
            string v;
            return _liarOf.TryGetValue(itemId, out v) ? v : null;
        }

        public IList<VillagerDef> AvailableSources(string itemId) { return _available[itemId]; }

        /// <summary>출처가 몇 사람인가 (입을 닫은 사람을 뺀 값). 겹쳐 물을 수 있는지가 이것으로 정해진다.</summary>
        public int SourceCount(string itemId) { return _available[itemId].Count; }

        /// <summary>
        /// silenced 는 이번 임무에 입을 닫은 사람들이다(계속 물어서 위험해진 사람).
        /// 거짓 배분은 씨드로만 정해진다 — 같은 씨드·같은 침묵 상태면 같은 거짓이 나온다.
        /// </summary>
        public Whispers(GameData data, MissionDef mission, PhaseAssignment truth,
                        ICollection<string> silenced = null)
        {
            _data = data;
            _mission = mission;
            _truth = truth;

            foreach (IntelItemDef item in data.AllItems)
            {
                List<VillagerDef> avail = new List<VillagerDef>();
                foreach (VillagerDef v in data.SourcesOf(item.id))
                    if (silenced == null || !silenced.Contains(v.id)) avail.Add(v);
                _available[item.id] = avail;
            }

            Random rng = new Random(mission.seed);
            foreach (IntelItemDef item in data.AllItems)
            {
                List<VillagerDef> avail = _available[item.id];
                if (!item.falsifiable || avail.Count < 2) continue;      // 겹쳐 물을 수 없으면 거짓도 없다
                if (rng.Next(100) >= data.Balance.ask.falseChancePercent) continue;
                _liarOf[item.id] = avail[rng.Next(avail.Count)].id;
            }
        }

        /// <summary>이 사람이 이 항목에 대해 내놓는 답. 아는 항목이 아니면 null.</summary>
        public Rumor Answer(string villagerId, string itemId)
        {
            IntelItemDef item = _data.Item(itemId);
            bool knows = false;
            foreach (VillagerDef v in _available[itemId]) if (v.id == villagerId) { knows = true; break; }
            if (!knows) return null;

            bool lies = LiarOf(itemId) == villagerId;
            Rumor w = new Rumor
            {
                ItemId = itemId, VillagerId = villagerId,
                Kind = item.kind, SubjectId = item.subjectId, IsTrue = !lies
            };

            if (item.kind == IntelKinds.Route)
            {
                LegDef[] legs = _data.Guard(item.subjectId).legs;
                w.Route = lies ? RotateOne(legs) : legs;
            }
            else if (item.kind == IntelKinds.Post)
            {
                int[] options = _data.Guard(item.subjectId).phaseOptionsMs;
                int actual = _truth.PhaseMs(item.subjectId);
                w.PostPhaseMs = lies ? NextOption(options, actual) : actual;
            }
            else if (item.kind == IntelKinds.Shift)
            {
                ShiftChangeDef sc = _data.ShiftChange(item.subjectId);
                w.ShiftAtMs = lies ? sc.atMs + _data.Balance.ask.shiftLieDeltaMs : sc.atMs;
            }
            else throw new InvalidOperationException("모르는 정보 종류: " + item.kind);

            return w;
        }

        /// <summary>참값. 감사와 검사기만 쓴다 — 플레이어의 지식에는 들어가지 않는다.</summary>
        public Rumor Truth(string itemId)
        {
            IntelItemDef item = _data.Item(itemId);
            Rumor w = new Rumor { ItemId = itemId, Kind = item.kind, SubjectId = item.subjectId, IsTrue = true };
            if (item.kind == IntelKinds.Route) w.Route = _data.Guard(item.subjectId).legs;
            else if (item.kind == IntelKinds.Post) w.PostPhaseMs = _truth.PhaseMs(item.subjectId);
            else w.ShiftAtMs = _data.ShiftChange(item.subjectId).atMs;
            return w;
        }

        /// <summary>거짓 순찰 경로는 **한 칸 돌린 것**이다. 구간은 그대로고 순서만 어긋난다 — 그럴듯한 오답.</summary>
        private static LegDef[] RotateOne(LegDef[] legs)
        {
            LegDef[] rotated = new LegDef[legs.Length];
            for (int i = 0; i < legs.Length; i++) rotated[i] = legs[(i + 1) % legs.Length];
            return rotated;
        }

        /// <summary>거짓 배치는 **다음 위상 선택지**다. 있을 수 있는 값이라 의심하기 어렵다.</summary>
        private static int NextOption(int[] options, int actual)
        {
            for (int i = 0; i < options.Length; i++)
                if (options[i] == actual) return options[(i + 1) % options.Length];
            return options[0];
        }
    }
}
