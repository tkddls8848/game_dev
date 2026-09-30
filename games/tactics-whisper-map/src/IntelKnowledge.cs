using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
{
    /// <summary>
    /// 한 항목에 대해 플레이어가 쥔 것. **받은 답들을 표로 센다.**
    ///
    /// `Whispers` 의 불변식(한 항목의 거짓말쟁이는 최대 한 명)이 여기서 규칙 하나로 바뀐다:
    /// > **두 표를 받은 답은 참이다.**
    ///
    /// 그래서 셋이 갈린다 — 한 번만 물었으면 쓸 수 있지만 참인지 모르고,
    /// 두 사람이 같은 말을 하면 참이고, 두 사람이 갈리면 그 항목은 **쓸 수 없다는 것을 안다.**
    /// 셋째 사람에게 물으면 갈린 표가 깨지고 다수가 참이 된다.
    /// </summary>
    public sealed class Belief
    {
        public string ItemId;
        public string Kind;
        public string SubjectId;
        public readonly List<Rumor> Answers = new List<Rumor>();
        public readonly List<string> AskedSources = new List<string>();

        /// <summary>지금 믿는 답 — 표가 가장 많은 것. 동수면 먼저 받은 것.</summary>
        public Rumor Chosen { get; private set; }
        /// <summary>가장 많은 표를 받은 답의 표 수.</summary>
        public int TopVotes { get; private set; }
        /// <summary>서로 다른 답이 몇 가지 왔는가.</summary>
        public int DistinctAnswers { get; private set; }

        public bool IsTrue { get { return Chosen != null && Chosen.IsTrue; } }
        /// <summary>겹쳐 물었는데 다수가 안 나왔다 → 이 항목은 쓸 수 없다(거짓이 드러난 자리).</summary>
        public bool Conflicted { get { return TopVotes < 2 && DistinctAnswers >= 2; } }
        /// <summary>두 사람 이상이 같은 말을 했다 → 불변식에 따라 참이다.</summary>
        public bool Corroborated { get { return TopVotes >= 2; } }
        public bool Usable { get { return Chosen != null && !Conflicted; } }

        public LegDef[] Route { get { return Chosen == null ? null : Chosen.Route; } }
        public int PostPhaseMs { get { return Chosen == null ? -1 : Chosen.PostPhaseMs; } }
        public int ShiftAtMs { get { return Chosen == null ? -1 : Chosen.ShiftAtMs; } }

        public void Add(Rumor w)
        {
            Answers.Add(w);
            AskedSources.Add(w.VillagerId);
            Recount();
        }

        private void Recount()
        {
            List<Rumor> distinct = new List<Rumor>();
            List<int> votes = new List<int>();
            foreach (Rumor w in Answers)
            {
                int found = -1;
                for (int i = 0; i < distinct.Count; i++) if (distinct[i].SameAs(w)) { found = i; break; }
                if (found < 0) { distinct.Add(w); votes.Add(1); }
                else votes[found]++;
            }
            DistinctAnswers = distinct.Count;
            TopVotes = 0;
            Chosen = null;
            for (int i = 0; i < distinct.Count; i++)
                if (votes[i] > TopVotes) { TopVotes = votes[i]; Chosen = distinct[i]; }
        }
    }

    /// <summary>
    /// ★ **계획 단계에서 플레이어가 실제로 쥔 것 전부.**
    ///
    /// 두 층으로 되어 있고, 그 경계가 이 PoC의 규칙이다:
    ///
    ///   · **지형은 공짜다** — 구역·엄폐·시야·인접·은폐 지점. 눈으로 보인다
    ///   · **시각은 사야 한다** — 순찰 경로·오늘의 배치·교대 시각. 마을 사람에게 묻는다
    ///
    /// 산 것에는 **틀린 것이 섞인다.** 이 클래스는 참·거짓을 구별하지 않고 그대로 담는다 —
    /// 플레이어가 구별할 수 없기 때문이다. IsTrue 칸은 감사기만 읽는다.
    ///
    /// 아무것도 사지 않은 상태(Blind)가 기본값이고, 그 상태로도 임무를 깰 수 있어야 한다
    /// (BlindSolvability). 그래서 이 클래스는 **비어 있어도 완전히 유효하다.**
    /// </summary>
    public sealed class IntelKnowledge
    {
        private readonly GameData _data;
        private readonly MissionDef _mission;
        private readonly Dictionary<string, Belief> _beliefs = new Dictionary<string, Belief>();
        private readonly HashSet<string> _forgottenSightLines = new HashSet<string>();
        private readonly HashSet<string> _forgottenCover = new HashSet<string>();
        private readonly HashSet<string> _forgottenNoiseLinks = new HashSet<string>();
        private PatrolModel _believedModel;
        private bool _modelStale = true;

        public MissionDef Mission { get { return _mission; } }
        public IEnumerable<Belief> Beliefs { get { return _beliefs.Values; } }
        public int BeliefCount { get { return _beliefs.Count; } }

        public IntelKnowledge(GameData data, MissionDef mission) { _data = data; _mission = mission; }

        /// <summary>아무것도 묻지 않은 상태. 지형만 안다.</summary>
        public static IntelKnowledge Blind(GameData data, MissionDef mission)
        {
            return new IntelKnowledge(data, mission);
        }

        public Belief Of(string itemId)
        {
            Belief b;
            return _beliefs.TryGetValue(itemId, out b) ? b : null;
        }

        public bool Holds(string itemId)
        {
            Belief b = Of(itemId);
            return b != null && b.Usable;
        }

        /// <summary>
        /// 답 하나를 받아 담는다. 같은 항목을 두 번 받으면 **겹쳐 물은 것**이다:
        /// 같은 말이면 그 값은 참이고(불변식: 거짓말쟁이는 최대 한 명), 갈리면 그 항목을 버린다.
        /// </summary>
        public void Accept(Rumor w)
        {
            Belief b = Of(w.ItemId);
            if (b == null)
            {
                b = new Belief { ItemId = w.ItemId, Kind = w.Kind, SubjectId = w.SubjectId };
                _beliefs[w.ItemId] = b;
            }
            b.Add(w);
            _modelStale = true;
        }

        /// <summary>겹쳐 물어 확인된 항목인가 (두 사람 이상이 같은 말을 했다 → 참이다).</summary>
        public bool IsCorroborated(string itemId)
        {
            Belief b = Of(itemId);
            return b != null && b.Corroborated;
        }

        /// <summary>겹쳐 물었더니 답이 갈린 항목인가 — 거짓이 드러난 자리다.</summary>
        public bool IsConflicted(string itemId)
        {
            Belief b = Of(itemId);
            return b != null && b.Conflicted;
        }

        // ── 순찰을 예측할 수 있는가 ──────────────────────────────────────────

        /// <summary>
        /// 이 순찰병이 언제 어디 있는지 **예측할 수 있는가.**
        /// 경로만 알아도, 배치만 알아도 안 된다 — 둘이 다 있어야 시각에 붙는다.
        /// 이게 이 PoC의 정보 단위가 둘로 쪼개진 이유다.
        /// </summary>
        public bool CanPredict(string guardId)
        {
            IntelItemDef route = _data.ItemFor(IntelKinds.Route, guardId);
            IntelItemDef post = _data.ItemFor(IntelKinds.Post, guardId);
            return route != null && post != null && Holds(route.id) && Holds(post.id);
        }

        /// <summary>예측할 수 있는 순찰병들. 한 명은 알고 한 명은 모르는 것이 보통이다.</summary>
        public List<string> PredictableGuardIds()
        {
            List<string> list = new List<string>();
            foreach (string guardId in _mission.guardIds) if (CanPredict(guardId)) list.Add(guardId);
            return list;
        }

        /// <summary>
        /// 플레이어의 머릿속 지도. **한 명도 예측할 수 없으면 null.**
        ///
        /// 예측 못 하는 순찰병 자리는 자리표시자로 채운다 — 그 자리를 읽는 것은 잘못이므로
        /// <see cref="BelievedZoneAt"/> 가 CanPredict 로 먼저 막는다.
        /// (한 번 틀렸다: 모델을 전부-아니면-전무로 만들자 "한 명만 아는" 흔한 상태에서 감사가
        ///  ModelMismatch 를 166건 뱉었다. 예측 단위는 **순찰병 하나**다.)
        /// </summary>
        public PatrolModel BelievedModel()
        {
            if (!_modelStale && _believedModel != null) return _believedModel;
            _modelStale = false;
            _believedModel = null;
            if (PredictableGuardIds().Count == 0) return null;

            PhaseAssignment phases = new PhaseAssignment();
            Dictionary<string, LegDef[]> routes = new Dictionary<string, LegDef[]>();
            foreach (string guardId in _mission.guardIds)
            {
                if (CanPredict(guardId))
                {
                    routes[guardId] = Of(_data.ItemFor(IntelKinds.Route, guardId).id).Route;
                    phases.Set(guardId, Of(_data.ItemFor(IntelKinds.Post, guardId).id).PostPhaseMs);
                }
                else
                {
                    // 자리표시자. 읽히지 않는다.
                    routes[guardId] = _data.Guard(guardId).legs;
                    phases.Set(guardId, _data.Guard(guardId).phaseOptionsMs[0]);
                }
            }

            Dictionary<string, int> shifts = new Dictionary<string, int>();
            foreach (ShiftChangeDef sc in _data.ShiftChangesOf(_mission))
            {
                IntelItemDef item = _data.ItemFor(IntelKinds.Shift, sc.id);
                if (item != null && Holds(item.id)) shifts[sc.id] = Of(item.id).ShiftAtMs;
                else shifts[sc.id] = sc.atMs + _mission.lengthMs;   // 모르면 이 회차에는 없다고 믿는다
            }

            _believedModel = new PatrolModel(_data, _mission, phases, routes, shifts);
            return _believedModel;
        }

        /// <summary>믿고 있는 바로는 이 순찰병이 이 시각에 어디 있는가. 예측 못 하면 null.</summary>
        public string BelievedZoneAt(string guardId, int atMs)
        {
            if (!CanPredict(guardId)) return null;
            PatrolModel model = BelievedModel();
            return model == null ? null : model.ZoneAt(guardId, atMs);
        }

        /// <summary>이 항목이 틀렸고 그것을 쥐고 있는가. 감사만 쓴다.</summary>
        public bool HoldsFalse(string itemId)
        {
            Belief b = Of(itemId);
            return b != null && b.Usable && !b.IsTrue;
        }

        // ── 지형 지식. 공짜지만 **대조군은 지울 수 있다** ─────────────────────
        // 지울 수 있어야 "감사기에 이가 있는가"를 감사기로 확인할 수 있다.

        public bool KnowsSightLine(string from, string to) { return !_forgottenSightLines.Contains(from + ">" + to); }
        public bool KnowsCover(string zone) { return !_forgottenCover.Contains(zone); }
        public bool KnowsNoiseLink(string a, string b) { return !_forgottenNoiseLinks.Contains(LinkKey(a, b)); }

        public void ForgetSightLine(string from, string to) { _forgottenSightLines.Add(from + ">" + to); }
        public void ForgetCover(string zone) { _forgottenCover.Add(zone); }
        public void ForgetNoiseLink(string a, string b) { _forgottenNoiseLinks.Add(LinkKey(a, b)); }

        /// <summary>
        /// **지형 지식만으로** 이 소리가 저 자리까지 들린다고 예측할 수 있는가.
        ///
        /// "경로가 있는가"를 묻는 것으로는 부족하다 — 인접 하나를 몰라도 우회로가 있으면 경로는 늘 있고,
        /// 그러면 감사가 아무것도 걸러 내지 못한다. 그래서 **아는 링크만으로 같은 감쇠 계산을 다시 돌린다.**
        /// </summary>
        public bool PredictsAudible(ZoneGraph graph, string origin, int sourceLoudness,
                                   string listener, int hearingThreshold)
        {
            NoiseBalance nb = _data.Balance.noise;
            if (origin == listener) return sourceLoudness >= hearingThreshold;

            Dictionary<string, int> cost = new Dictionary<string, int> { { origin, 0 } };
            HashSet<string> done = new HashSet<string>();
            while (true)
            {
                string cur = null;
                int best = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in cost)
                    if (!done.Contains(kv.Key) && kv.Value < best) { cur = kv.Key; best = kv.Value; }
                if (cur == null) return false;
                done.Add(cur);

                int arrival = sourceLoudness - best;
                if (arrival < nb.hearingThresholdPercent) continue;
                if (cur == listener) return arrival >= hearingThreshold;

                foreach (string n in graph.Neighbors(cur))
                {
                    if (!KnowsNoiseLink(cur, n)) continue;   // 모르는 길은 쓸 수 없다
                    int step = best + nb.hopAttenuation + graph.ExtraAttenuation(cur, n);
                    int known;
                    if (!cost.TryGetValue(n, out known) || step < known) cost[n] = step;
                }
            }
        }

        private static string LinkKey(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }
    }
}
