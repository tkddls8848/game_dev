using System;
using System.Collections.Generic;
using System.Text;
using Tactics.Data;

namespace Tactics.Sim
{
    public static class ActionKinds
    {
        public static string Shoot { get { return "Shoot"; } }
        public static string PlantTrap { get { return "PlantTrap"; } }
        public static string PlantCharge { get { return "PlantCharge"; } }
        public static string Detonate { get { return "Detonate"; } }
        public static string Infiltrate { get { return "Infiltrate"; } }
    }

    /// <summary>
    /// 계획 단계에서 분대원 하나에게 주는 명령 하나 — "언제 · 어디서 · 무엇을".
    ///
    /// 이동 명령은 없다. 분대원은 지정된 시각에 지정된 자리에 있으려고 **스스로 일찍 떠난다**
    /// (최단 경로, 인접 관계 위). 계획에서 사람이 고르는 것은 시각과 자리뿐이고,
    /// 그게 이 게임이 플레이어에게 묻는 질문이기도 하다.
    /// </summary>
    public sealed class SquadOrder
    {
        public string MemberId;
        public string Kind;
        public string Zone;        // 수행하는 자리 (저격은 사선 자리, 목표 구역이 아니다)
        public string TargetId;    // Shoot→순찰병 id · PlantCharge/Detonate→목표 id · Infiltrate→문서 id
        public int StartMs;

        public SquadOrder() { }

        public SquadOrder(string memberId, string kind, string zone, string targetId, int startMs)
        {
            MemberId = memberId; Kind = kind; Zone = zone; TargetId = targetId; StartMs = startMs;
        }

        public override string ToString()
        {
            return MemberId + ":" + Kind + "@" + Zone + (TargetId == null ? "" : "->" + TargetId) + "+" + StartMs;
        }
    }

    public sealed class SquadPlan
    {
        public readonly List<SquadOrder> Orders = new List<SquadOrder>();

        public SquadPlan() { }
        public SquadPlan(IEnumerable<SquadOrder> orders) { Orders.AddRange(orders); }

        public SquadPlan Add(SquadOrder order) { Orders.Add(order); return this; }

        public SquadPlan Copy()
        {
            SquadPlan p = new SquadPlan();
            foreach (SquadOrder o in Orders)
                p.Orders.Add(new SquadOrder(o.MemberId, o.Kind, o.Zone, o.TargetId, o.StartMs));
            return p;
        }

        /// <summary>쓰인 분대원 집합. NoDominantOrder 가 본다.</summary>
        public SortedSet<string> MembersUsed()
        {
            SortedSet<string> set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (SquadOrder o in Orders) set.Add(o.MemberId);
            return set;
        }

        /// <summary>
        /// 시각과 자리를 지운 "절차" — (분대원, 수단) 의 순서열.
        /// 이것이 임무마다 같으면 전술이 아니라 외운 절차다(NoDominantOrder).
        /// </summary>
        public string ActionSignature()
        {
            List<SquadOrder> sorted = SortedOrders();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append(" > ");
                sb.Append(sorted[i].MemberId).Append(':').Append(sorted[i].Kind);
            }
            return sb.Length == 0 ? "(빈 계획)" : sb.ToString();
        }

        /// <summary>자리까지 넣은 서명. "같은 절차의 다른 해"를 세는 데 쓴다.</summary>
        public string PlacementSignature()
        {
            List<SquadOrder> sorted = SortedOrders();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append(" > ");
                sb.Append(sorted[i].MemberId).Append(':').Append(sorted[i].Kind)
                  .Append('@').Append(sorted[i].Zone);
            }
            return sb.Length == 0 ? "(빈 계획)" : sb.ToString();
        }

        public List<SquadOrder> SortedOrders()
        {
            List<SquadOrder> sorted = new List<SquadOrder>(Orders);
            sorted.Sort(delegate (SquadOrder a, SquadOrder b)
            {
                int c = a.StartMs.CompareTo(b.StartMs);
                if (c != 0) return c;
                c = string.CompareOrdinal(a.MemberId, b.MemberId);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Kind, b.Kind);
            });
            return sorted;
        }

        public override string ToString()
        {
            List<SquadOrder> sorted = SortedOrders();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.Append(sorted[i].ToString());
            }
            return sb.Length == 0 ? "(빈 계획)" : sb.ToString();
        }
    }

    public enum Activity { Idle = 0, Moving = 1, Acting = 2, Down = 3 }

    /// <summary>
    /// 계획 하나를 틱 배열로 펼친 것. 순찰병과 무관하게 먼저 계산된다 —
    /// 순찰병이 이동을 막지 않기 때문이고, 덕분에 회차 시뮬레이션이 한 번의 스캔이 된다.
    /// </summary>
    public sealed class MemberTimeline
    {
        public string MemberId;
        public string[] Zone;              // 틱마다 어디 있는가
        public int[] ZoneIdx;              // 같은 값의 정수 색인 (뜨거운 고리용)
        public Activity[] Act;
        public int[] Exposure;             // 그 틱의 노출 백분율
        public int[] ZoneSinceMs;          // 이 구역에 언제부터 있는가 (은폐 지점 정착 판정)
        public int ExtractedAtMs = -1;     // 탈출 지점에 언제 닿았는가. 못 닿으면 -1
        public List<SquadOrder> Ordered = new List<SquadOrder>();
    }

    /// <summary>계획을 틱 배열로 펼친다. 물리적으로 불가능한 계획은 여기서 걸러진다.</summary>
    public static class PlanRasterizer
    {
        public sealed class Result
        {
            public bool Feasible;
            public string Reason = "";
            public Dictionary<string, MemberTimeline> Timelines = new Dictionary<string, MemberTimeline>();

            /// <summary>틱마다 그 틱에 **완료되는** 명령들. 틱마다 전체 명령을 훑지 않기 위해 미리 모은다.</summary>
            public List<SquadOrder>[] CompletionsAt;
        }

        public static Result Build(GameData data, MissionDef mission, ZoneGraph graph, SquadPlan plan)
        {
            Result result = new Result();
            int tickMs = data.Balance.tickMs;
            int ticks = mission.lengthMs / tickMs;
            result.CompletionsAt = new List<SquadOrder>[ticks];

            Dictionary<string, List<SquadOrder>> byMember = new Dictionary<string, List<SquadOrder>>();
            foreach (MemberDef m in data.AllMembers) byMember[m.id] = new List<SquadOrder>();

            foreach (SquadOrder o in plan.Orders)
            {
                if (!byMember.ContainsKey(o.MemberId))
                    return Fail(result, "분대에 없는 사람: " + o.MemberId);
                if (!graph.HasZone(o.Zone))
                    return Fail(result, "지도에 없는 구역: " + o.Zone);
                MemberActionDef def = data.ActionOf(o.MemberId, o.Kind);
                if (def == null)
                    return Fail(result, o.MemberId + " 는 " + o.Kind + " 를 못 한다");
                if (o.StartMs % tickMs != 0 || def.durationMs % tickMs != 0)
                    return Fail(result, "틱에 맞지 않는 시각: " + o);
                if (o.StartMs < 0 || o.StartMs + def.durationMs > mission.lengthMs)
                    return Fail(result, "회차 밖의 명령: " + o);
                byMember[o.MemberId].Add(o);
            }

            foreach (MemberDef m in data.AllMembers)
            {
                List<SquadOrder> orders = byMember[m.id];
                orders.Sort(delegate (SquadOrder a, SquadOrder b) { return a.StartMs.CompareTo(b.StartMs); });

                // 수단마다 회차당 횟수 제한
                Dictionary<string, int> used = new Dictionary<string, int>();
                foreach (SquadOrder o in orders)
                {
                    MemberActionDef def = data.ActionOf(m.id, o.Kind);
                    int n;
                    used[o.Kind] = used.TryGetValue(o.Kind, out n) ? n + 1 : 1;
                    if (def.charges >= 0 && used[o.Kind] > def.charges)
                        return Fail(result, m.id + " 의 " + o.Kind + " 가 " + def.charges + "회를 넘는다");
                }

                MemberTimeline t = new MemberTimeline
                {
                    MemberId = m.id,
                    Zone = new string[ticks],
                    ZoneIdx = new int[ticks],
                    Act = new Activity[ticks],
                    Exposure = new int[ticks],
                    ZoneSinceMs = new int[ticks],
                    Ordered = orders
                };

                string entry = graph.EntryZone;
                int entryIdx = graph.IndexOf(entry);
                for (int k = 0; k < ticks; k++)
                {
                    t.Zone[k] = entry; t.ZoneIdx[k] = entryIdx; t.Act[k] = Activity.Idle;
                }

                int availableMs = 0;
                string here = graph.EntryZone;

                foreach (SquadOrder o in orders)
                {
                    MemberActionDef def = data.ActionOf(m.id, o.Kind);
                    List<string> path = graph.Path(here, o.Zone);
                    if (path == null) return Fail(result, here + " 에서 " + o.Zone + " 로 갈 길이 없다");
                    int hops = path.Count - 1;
                    int travelMs = hops * m.moveMsPerZone;
                    int departMs = o.StartMs - travelMs;
                    if (departMs < availableMs)
                        return Fail(result, m.id + " 가 " + o.StartMs + "ms 까지 " + o.Zone
                                            + " 에 닿을 수 없다 (출발 가능 " + availableMs + "ms, 이동 " + travelMs + "ms)");

                    // 이동: 홉 하나마다 도착 구역을 그 구간 동안 점유한다(보수적으로 잡는다).
                    for (int i = 1; i <= hops; i++)
                    {
                        int from = departMs + (i - 1) * m.moveMsPerZone;
                        int to = from + m.moveMsPerZone;
                        Paint(graph, t, tickMs, ticks, from, to, path[i], Activity.Moving, 0);
                    }
                    Paint(graph, t, tickMs, ticks, o.StartMs, o.StartMs + def.durationMs, o.Zone,
                          Activity.Acting, def.exposurePercent);

                    int doneTick = (o.StartMs + def.durationMs) / tickMs;
                    if (doneTick < ticks)
                    {
                        if (result.CompletionsAt[doneTick] == null) result.CompletionsAt[doneTick] = new List<SquadOrder>();
                        result.CompletionsAt[doneTick].Add(o);
                    }

                    availableMs = o.StartMs + def.durationMs;
                    here = o.Zone;
                }

                // 마지막 명령 뒤에는 탈출 지점으로 돌아간다. 최대한 일찍.
                List<string> back = graph.Path(here, graph.ExtractionZone);
                if (back == null) return Fail(result, here + " 에서 탈출 지점으로 갈 길이 없다");
                int backHops = back.Count - 1;
                for (int i = 1; i <= backHops; i++)
                {
                    int from = availableMs + (i - 1) * m.moveMsPerZone;
                    int to = from + m.moveMsPerZone;
                    Paint(graph, t, tickMs, ticks, from, to, back[i], Activity.Moving, 0);
                }
                int arriveMs = availableMs + backHops * m.moveMsPerZone;
                t.ExtractedAtMs = arriveMs <= mission.lengthMs ? arriveMs : -1;
                Paint(graph, t, tickMs, ticks, arriveMs, mission.lengthMs, graph.ExtractionZone, Activity.Idle, 0);

                // 구역에 언제부터 있는가 (은폐 지점 정착 판정에 쓴다)
                int sinceMs = 0;
                for (int k = 0; k < ticks; k++)
                {
                    if (k > 0 && t.ZoneIdx[k] != t.ZoneIdx[k - 1]) sinceMs = k * tickMs;
                    t.ZoneSinceMs[k] = sinceMs;
                }

                result.Timelines[m.id] = t;
            }

            result.Feasible = true;
            return result;
        }

        private static void Paint(ZoneGraph graph, MemberTimeline t, int tickMs, int ticks, int fromMs, int toMs,
                                  string zone, Activity act, int exposure)
        {
            int k0 = Math.Max(0, fromMs / tickMs);
            int k1 = Math.Min(ticks, (toMs + tickMs - 1) / tickMs);
            if (k0 >= k1) return;
            string canon = graph.Canon(zone);
            int idx = graph.IndexOf(canon);
            for (int k = k0; k < k1; k++)
            {
                t.Zone[k] = canon; t.ZoneIdx[k] = idx; t.Act[k] = act; t.Exposure[k] = exposure;
            }
        }

        private static Result Fail(Result r, string reason)
        {
            r.Feasible = false; r.Reason = reason; return r;
        }
    }
}
