using System;
using System.Collections.Generic;
using Graft.Data;

namespace Graft.Sim
{
    /// <summary>떨림 하나를 어떻게 분류했는가. 앞의 셋이 공정, 뒤의 셋이 불공정이다.</summary>
    public static class RejectVerdicts
    {
        /// <summary>쥐고 있었고 참이었다. 심기 전에 볼 수 있었다.</summary>
        public static string Held { get { return "Held"; } }
        /// <summary>안 띄웠지만 **명료도로 띄울 수 있었다.** 안 띄우기로 한 것은 선택이다.</summary>
        public static string Probeable { get { return "Probeable"; } }
        /// <summary>스크린 기억을 그대로 믿었다. 다만 **겹쳐 물으면 드러났다.**</summary>
        public static string DistortedButCheckable { get { return "DistortedButCheckable"; } }
        /// <summary>스크린 기억인데 **겹쳐 물을 길이 없었다.** 불공정하다.</summary>
        public static string DistortedUncheckable { get { return "DistortedUncheckable"; } }
        /// <summary>명료도를 다 써도 닿지 않는 기억이었다. 불공정하다 — 이것이 숨은 정보다.</summary>
        public static string Unknowable { get { return "Unknowable"; } }
        /// <summary>적히지 않은 규칙이 떨림을 냈다. 불공정하다 — 규칙표가 거짓이 된다.</summary>
        public static string RuleUnstated { get { return "RuleUnstated"; } }
        /// <summary>겹쳐 물어 참값을 봤는데도 값이 달랐다 — 모델 버그다. 감사기의 안전망.</summary>
        public static string ModelMismatch { get { return "ModelMismatch"; } }

        public static bool IsFair(string verdict)
        {
            return verdict == Held || verdict == Probeable || verdict == DistortedButCheckable;
        }
    }

    public sealed class RejectFact
    {
        public string Rule = "";
        public string ToMemoryId = "";
        public string Verdict = "";
        public int Strength;
        public string Detail = "";
        public bool Fair { get { return RejectVerdicts.IsFair(Verdict); } }
        public override string ToString() { return Rule + "/" + Verdict + " (" + Strength + ") " + Detail; }
    }

    public sealed class RejectionVerdict
    {
        public string CommissionId = "";
        public string PolicyName = "";
        public int Trial;
        public bool Rejected;
        public int TremorsChecked;
        public readonly List<RejectFact> Facts = new List<RejectFact>();

        public bool Fair
        {
            get { foreach (RejectFact f in Facts) if (!f.Fair) return false; return true; }
        }

        public List<RejectFact> Unfair()
        {
            List<RejectFact> bad = new List<RejectFact>();
            foreach (RejectFact f in Facts) if (!f.Fair) bad.Add(f);
            return bad;
        }

        public int Count(string verdict)
        {
            int n = 0;
            foreach (RejectFact f in Facts) if (f.Verdict == verdict) n++;
            return n;
        }
    }

    /// <summary>
    /// ★ **RejectionIsPredictable 을 기계가 판정하는 자리.**
    ///
    /// 물음은 하나다: 꿈이 거부한 이유가 **플레이어가 알 수 있었던 것으로 설명되는가.**
    /// 세 겹으로 본다 — games/tactics-whisper-map 의 DetectionFairness 가 같은 문제를 푼 모양을 따랐다.
    ///
    ///   ① 정적 보장 (StaticGaps)   — 표본이 아니라 **전수**. 설계 단계에서 구멍을 막는다
    ///   ② 동적 감사 (Audit)        — 실제로 일어난 떨림 하나하나를 분류한다
    ///   ③ 음성 대조군              — 데이터를 일부러 망가뜨리면 **감사기가 반드시 실패해야** 한다
    ///                                (테스트 쪽 RejectionIsPredictableTests 에 있다)
    ///
    /// ③이 없으면 ①②가 통과하는 것이 「설계가 옳다」인지 「감사기에 이가 없다」인지 알 수 없다.
    /// </summary>
    public static class RejectionAudit
    {
        public static RejectionVerdict Audit(Attempt a)
        {
            DreamSession s = a.Session;
            GameData d = s.Data;
            MemoryNet net = s.Net;
            CommissionDef c = s.Commission;
            ProbeKnowledge k = a.Knowledge;
            List<string> start = net.StartSurfaced();
            Dictionary<string, ProbeRoute> fromStart = net.CheapestRoutes(start);

            RejectionVerdict v = new RejectionVerdict
            {
                CommissionId = c.id, PolicyName = a.PolicyName, Trial = a.Trial,
                Rejected = !a.Truth.Accepted
            };

            foreach (Tremor t in a.Truth.Tremors)
            {
                v.TremorsChecked++;

                if (!Stated(d, t.Rule))
                {
                    Add(v, t, RejectVerdicts.RuleUnstated,
                        "규칙표(balance.json statedRules)에 없는 이유로 떨렸다: " + t.Rule);
                    continue;
                }

                // 사람·장소의 창은 의뢰서에 적혀 있다. 언제나 볼 수 있었으므로 공정하다.
                if (t.ToMemoryId.Length == 0)
                {
                    Add(v, t, RejectVerdicts.Held,
                        "의뢰서에 적힌 창으로 설명된다 (" + t.RefId + ")");
                    continue;
                }

                string m = t.ToMemoryId;
                if (!k.Knows(m))
                {
                    ProbeRoute r;
                    if (fromStart.TryGetValue(m, out r) && r.Cost <= c.lucidityBudget)
                        Add(v, t, RejectVerdicts.Probeable,
                            net.Memory(m).name + " 는 명료도 " + r.Cost + " 로 띄울 수 있었다 (예산 " + c.lucidityBudget + ")");
                    else
                        Add(v, t, RejectVerdicts.Unknowable,
                            net.Memory(m).name + " 에는 명료도를 다 써도 닿을 수 없다");
                    continue;
                }

                if (!AttributeMismatch(t.Rule, s.Truth, k, m))
                {
                    Add(v, t, RejectVerdicts.Held,
                        net.Memory(m).name + " 를 띄워 두고 있었고 값이 참이었다");
                    continue;
                }

                if (k.Corroborated(m))
                {
                    Add(v, t, RejectVerdicts.ModelMismatch,
                        net.Memory(m).name + " 를 겹쳐 물었는데도 꿈의 값과 달랐다");
                    continue;
                }

                int cost = CorroborationCost(net, fromStart, m);
                int routes = DistinctRoutes(net, fromStart, m);
                if (routes >= d.Balance.corroborateRoutes && cost >= 0 && cost <= c.lucidityBudget)
                    Add(v, t, RejectVerdicts.DistortedButCheckable,
                        net.Memory(m).name + " 는 스크린이었지만 서로 다른 길 " + routes + " 개로 겹쳐 물 수 있었다 (값 " + cost + ")");
                else
                    Add(v, t, RejectVerdicts.DistortedUncheckable,
                        net.Memory(m).name + " 는 스크린인데 겹쳐 물 길이 " + routes + " 개뿐이다 (값 " + cost + ")");
            }

            return v;
        }

        /// <summary>
        /// 규칙이 그 기억에서 **읽는 값**이 플레이어의 눈과 꿈의 눈에서 달랐는가.
        /// 이 짝이 어긋나면 감사기가 거짓을 말한다 — 규칙을 더할 때 여기도 같이 고쳐야 한다.
        /// </summary>
        private static bool AttributeMismatch(string rule, TruthView truth, ProbeKnowledge k, string m)
        {
            if (rule == TremorRules.TimePlaceConflict || rule == TremorRules.PersonElsewhere)
                return truth.Day(m) != k.Day(m) || truth.Place(m) != k.Place(m);
            if (rule == TremorRules.MoodClashEra)
                return truth.Day(m) != k.Day(m) || truth.Mood(m) != k.Mood(m);
            if (rule == TremorRules.MoodClashAnchor)
                return truth.Mood(m) != k.Mood(m);
            if (rule == TremorRules.IntensityClash)
                return false;      // 세기에는 스크린이 없다. 늘 참이다
            return false;
        }

        private static bool Stated(GameData d, string rule)
        {
            foreach (string s in d.Balance.statedRules) if (s == rule) return true;
            return false;
        }

        // ── 정적 보장 — 표본이 아니라 전수 ───────────────────────────────────────

        /// <summary>
        /// 이 의뢰에서 **아직 아무도 당하지 않았을 뿐인 불공정**을 설계 단계에서 잡는다.
        ///
        ///   ① 기억마다 명료도 예산 안에 닿는 길이 있는가
        ///   ② 스크린이 될 수 있는 기억마다 **서로 다른 길 둘**로 겹쳐 물 수 있는가
        ///   ③ 엔진이 낼 수 있는 규칙 전부가 규칙표에 적혀 있는가
        ///
        /// ②가 깨지면 동적 감사는 그 기억이 뽑히는 씨드에서만 실패한다 — 표본이 놓칠 수 있다.
        /// </summary>
        public static List<string> StaticGaps(GameData d, CommissionDef c)
        {
            List<string> gaps = new List<string>();
            SubjectDef subj = d.Subject(c.subjectId);
            MemoryNet net = new MemoryNet(subj);
            List<string> start = net.StartSurfaced();
            Dictionary<string, ProbeRoute> routes = net.CheapestRoutes(start);

            foreach (MemoryDef m in subj.memories)
            {
                ProbeRoute r;
                if (!routes.TryGetValue(m.id, out r))
                { gaps.Add("어떤 길로도 닿지 않는 기억: " + m.id); continue; }
                if (r.Cost > c.lucidityBudget)
                    gaps.Add("명료도 예산(" + c.lucidityBudget + ")보다 먼 기억: " + m.id + " (" + r.Cost + ")");
            }

            foreach (MemoryDef m in subj.memories)
            {
                if (!m.distortable) continue;
                int n = DistinctRoutes(net, routes, m.id);
                if (n < d.Balance.corroborateRoutes)
                { gaps.Add("스크린이 될 수 있는데 겹쳐 물 길이 " + n + "개뿐이다: " + m.id); continue; }
                int cost = CorroborationCost(net, routes, m.id);
                if (cost < 0 || cost > c.lucidityBudget)
                    gaps.Add("겹쳐 묻는 값(" + cost + ")이 예산(" + c.lucidityBudget + ")을 넘는다: " + m.id);
                if (m.trueDayIndex < 0 && m.truePlaceId.Length == 0 && m.trueMoodId.Length == 0)
                    gaps.Add("distortable 이지만 참값이 하나도 적혀 있지 않다 — 스크린이 될 수 없다: " + m.id);
            }

            foreach (string rule in EngineRules())
                if (!Stated(d, rule)) gaps.Add("엔진이 내는데 규칙표에 없다: " + rule);
            foreach (string rule in d.Balance.statedRules)
                if (!Array.Exists(EngineRules(), x => x == rule))
                    gaps.Add("규칙표에 있는데 엔진이 내지 않는다 — 거짓 약속이다: " + rule);

            return gaps;
        }

        /// <summary>엔진이 낼 수 있는 이유 전부. GraftRules 에 규칙을 더하면 여기도 늘어야 한다.</summary>
        public static string[] EngineRules()
        {
            return new[]
            {
                TremorRules.TimePlaceConflict, TremorRules.PersonElsewhere,
                TremorRules.PersonAbsent, TremorRules.PlaceWindow,
                TremorRules.MoodClashAnchor, TremorRules.MoodClashEra, TremorRules.IntensityClash
            };
        }

        /// <summary>이 기억에 닿는 **서로 다른 이웃**이 몇인가. 겹쳐 묻기의 전제다.</summary>
        public static int DistinctRoutes(MemoryNet net, Dictionary<string, ProbeRoute> fromStart, string memoryId)
        {
            int n = 0;
            foreach (string nb in net.NeighboursOf(memoryId))
                if (fromStart.ContainsKey(nb)) n++;
            return n;
        }

        /// <summary>
        /// 이 기억을 서로 다른 길 둘로 띄우는 데 드는 명료도. 두 길이 겹치는 간선은 한 번만 센다 —
        /// 겹치는 것을 두 번 세면 「값이 넘는다」는 거짓 구멍이 생긴다.
        /// 닿을 수 없으면 -1.
        /// </summary>
        public static int CorroborationCost(MemoryNet net, Dictionary<string, ProbeRoute> fromStart, string memoryId)
        {
            List<string> reach = new List<string>();
            foreach (string nb in net.NeighboursOf(memoryId))
                if (fromStart.ContainsKey(nb)) reach.Add(nb);
            if (reach.Count < 2) return -1;
            reach.Sort(StringComparer.Ordinal);

            int best = -1;
            for (int i = 0; i < reach.Count; i++)
                for (int j = i + 1; j < reach.Count; j++)
                {
                    Dictionary<string, int> edges = new Dictionary<string, int>();
                    CollectPathEdges(net, fromStart, reach[i], edges);
                    CollectPathEdges(net, fromStart, reach[j], edges);
                    AddEdge(edges, reach[i], memoryId, Policies.LinkCost(net, reach[i], memoryId));
                    AddEdge(edges, reach[j], memoryId, Policies.LinkCost(net, reach[j], memoryId));
                    int sum = 0;
                    foreach (KeyValuePair<string, int> kv in edges) sum += kv.Value;
                    if (best < 0 || sum < best) best = sum;
                }
            return best;
        }

        private static void CollectPathEdges(MemoryNet net, Dictionary<string, ProbeRoute> fromStart,
                                             string target, Dictionary<string, int> edges)
        {
            string cur = target;
            int guard = 0;
            while (guard++ < 64)
            {
                ProbeRoute r;
                if (!fromStart.TryGetValue(cur, out r) || r.ViaMemoryId.Length == 0) return;
                AddEdge(edges, r.ViaMemoryId, cur, Policies.LinkCost(net, r.ViaMemoryId, cur));
                cur = r.ViaMemoryId;
            }
        }

        private static void AddEdge(Dictionary<string, int> edges, string a, string b, int cost)
        {
            if (cost < 0) return;
            string key = string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
            edges[key] = cost;
        }

        private static void Add(RejectionVerdict v, Tremor t, string verdict, string detail)
        {
            v.Facts.Add(new RejectFact
            {
                Rule = t.Rule, ToMemoryId = t.ToMemoryId, Verdict = verdict,
                Strength = t.Strength, Detail = detail
            });
        }
    }
}
