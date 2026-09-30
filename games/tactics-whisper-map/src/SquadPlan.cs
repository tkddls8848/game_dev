using System;
using System.Collections.Generic;
using System.Text;

namespace Whisper.Sim
{
    public static class ActionKinds
    {
        public static string Infiltrate { get { return "Infiltrate"; } }
        public static string Shoot { get { return "Shoot"; } }
        public static string PlantTrap { get { return "PlantTrap"; } }
        public static string PlantCharge { get { return "PlantCharge"; } }
        public static string Detonate { get { return "Detonate"; } }
    }

    /// <summary>
    /// 계획 단계에서 분대원 하나에게 주는 명령 하나 — "언제 · 어디서 · 무엇을".
    ///
    /// 이동 명령은 없다. 분대원은 지정된 시각에 지정된 자리에 있으려고 **스스로 일찍 떠난다**
    /// (최단 경로, 인접 관계 위). 사람이 고르는 것은 시각과 자리뿐이다.
    ///
    /// Settle 이 참이면 그 자리의 은폐 지점에 몸을 묻는다 — settleMs 만큼 **더 일찍** 도착해야 한다.
    /// 위상을 모르면 늘 묻어야 하고, 알면 안 묻고 들어갔다 나올 수 있다. 정보가 사는 것이 이 차이다.
    /// </summary>
    public sealed class SquadOrder
    {
        public string MemberId;
        public string Kind;
        public string Zone;        // 수행하는 자리 (저격은 사선 자리, 표적의 구역이 아니다)
        public string TargetId;    // Shoot→순찰병 id · PlantCharge/Detonate→목표 id · Infiltrate→문서 id
        public int StartMs;
        public bool Settle;

        public SquadOrder() { }

        public SquadOrder(string memberId, string kind, string zone, string targetId, int startMs, bool settle = false)
        {
            MemberId = memberId; Kind = kind; Zone = zone; TargetId = targetId;
            StartMs = startMs; Settle = settle;
        }

        public SquadOrder Copy()
        {
            return new SquadOrder(MemberId, Kind, Zone, TargetId, StartMs, Settle);
        }

        public override string ToString()
        {
            return MemberId + ":" + Kind + "@" + Zone + (TargetId == null || TargetId.Length == 0 ? "" : "->" + TargetId)
                   + "+" + StartMs + (Settle ? "(묻고)" : "");
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
            foreach (SquadOrder o in Orders) p.Orders.Add(o.Copy());
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
        /// 이것이 임무마다 같으면 전술이 아니라 외운 절차다 (NoDominantOrder).
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
            for (int i = 0; i < sorted.Count; i++) { if (i > 0) sb.Append(" | "); sb.Append(sorted[i]); }
            return sb.Length == 0 ? "(빈 계획)" : sb.ToString();
        }
    }
}
