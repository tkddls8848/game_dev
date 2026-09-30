using System.Collections.Generic;
using Tenants.Data;

namespace Tenants.Sim
{
    /// <summary>
    /// 사람이 실제로 쓸 만한 수 하나. **정책은 숨은 값을 보지 못한다** —
    /// 복도에서 보이는 소리의 크기(ambient)와 지금까지 들은 것만 쓴다.
    /// baseStakes 를 들여다보는 정책은 정책이 아니라 정답이다.
    /// </summary>
    public abstract class Policy
    {
        public abstract string Name { get; }
        public abstract string Note { get; }

        /// <summary>지금 돕는다면 누구를 돕는가. 아직 없으면 -1.</summary>
        public abstract int ChooseTarget(Night night, Knowledge k);

        /// <summary>이 칸에 어느 문에 귀를 대는가. -1 이면 그만둔다.</summary>
        public abstract int ChooseDoor(Night night, Knowledge k, int slot);

        // ── 정책들이 같이 쓰는 도구 ──────────────────────────────────────────

        /// <summary>그 집의 사정이 얼마나 클 것 같은가. **짐작이다.**
        /// 얼마나 급한지까지 알면(Level 3) 실제 값이고, 필요한 것만 알면 그 일의 통념이며,
        /// 뭔가 있다는 것만 알면 최소값이다. 거짓을 믿고 있으면 짐작도 거짓을 따라간다.</summary>
        protected static int Estimate(Night night, Knowledge k, int i)
        {
            Belief b = k.Of(i);
            if (b.Level >= 3 && b.NeedId != null && !b.Misled) return night.Households[i].baseStakes;
            if (b.Level >= 2 && b.NeedId != null) return night.Data.Need(b.NeedId).perceivedWeight;
            if (b.Level >= 1) return night.Data.Balance.policy.hintEstimate;
            return 0;
        }

        /// <summary>이미 나갔다는 것이 밝혀진 칸. 도와도 건질 것이 없다.</summary>
        protected static bool KnownVacant(Knowledge k, int i)
        {
            Belief b = k.Of(i);
            return b.Level >= 3 && b.NeedId == "need_none";
        }

        protected static int BestByEstimate(Night night, Knowledge k)
        {
            int best = -1, bestV = 0;
            for (int i = 0; i < night.Households.Count; i++)
            {
                if (KnownVacant(k, i)) continue;
                int v = Estimate(night, k, i);
                if (v > bestV || (v == bestV && best >= 0 && night.AmbientOf(i) > night.AmbientOf(best)))
                { bestV = v; best = i; }
            }
            return bestV > 0 ? best : -1;
        }

        /// <summary>ambient 가 큰 순서로 세운 문 목록.</summary>
        protected static List<int> ByLoudness(Night night)
        {
            List<int> order = new List<int>();
            for (int i = 0; i < night.Households.Count; i++) order.Add(i);
            order.Sort(delegate (int a, int b)
            {
                int d = night.AmbientOf(b) - night.AmbientOf(a);
                return d != 0 ? d : a - b;
            });
            return order;
        }

        // ── 하루를 돌린다 ────────────────────────────────────────────────────

        /// <summary>
        /// 정책 하나로 하루를 돈다. 규칙 하나가 정책들을 공평하게 만든다:
        /// **한 칸 더 들으면 도울 시간이 없어지는 순간 돕는다.** 그렇지 않으면
        /// 엿듣기를 아끼는 정책만 이겨서 「듣는 것이 비싸다」가 아니라 「듣지 마라」가 된다.
        /// </summary>
        public static Outcome Run(Night night, Policy p)
        {
            int slots = night.Data.Balance.day.slotCount;
            Knowledge k = new Knowledge(night);
            int slot = 0;
            while (slot < slots)
            {
                int target = p.ChooseTarget(night, k);
                if (target >= 0)
                {
                    Outcome now = Outcome.Help(night, k, target, slot);
                    Outcome later = Outcome.Help(night, k, target, slot + 1);
                    if (now != null && later == null) return now;
                }
                int door = p.ChooseDoor(night, k, slot);
                if (door < 0) break;
                k.Listen(door, slot);
                slot++;
            }
            int t = p.ChooseTarget(night, k);
            if (t >= 0)
                for (int s = slot; s < slots; s++)
                {
                    Outcome o = Outcome.Help(night, k, t, s);
                    if (o != null) return o;
                }
            return Outcome.HelpNoOne(night);
        }

        public static List<Policy> All()
        {
            List<Policy> list = new List<Policy>();
            list.Add(new HelpNoOne());
            list.Add(new SweepInOrder());
            list.Add(new LoudestDoor());
            list.Add(new SilentFirst());
            list.Add(new NeverListenSilent());
            list.Add(new RepeatOneDoor());
            list.Add(new CheapestGrace());
            list.Add(new FollowTheNeighbours());
            return list;
        }
    }
}
