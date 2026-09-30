using System.Collections.Generic;
using Tenants.Data;

namespace Tenants.Sim
{
    public sealed class SearchOptions
    {
        /// <summary>침묵한 문에 귀를 대지 못하게 막는다. 「침묵을 내용 없음으로 취급한 세계」다.</summary>
        public bool ForbidSilentDoors;
    }

    /// <summary>한 선택지의 최선 계획 하나.</summary>
    public sealed class BestPlan
    {
        public int ChoiceIndex;
        public string ChoiceId;
        public Outcome Result;
        /// <summary>슬롯마다 귀를 댄 문의 index. -1 은 아무것도 하지 않았다는 뜻이고,
        /// 도움이 차지한 칸도 -1 이다(HelpStartSlot 부터 세면 된다).</summary>
        public int[] Listens;

        /// <summary>계획을 한 줄로 — 슬롯마다 귀를 댄 문의 index, 도움이 차지한 칸은 -2.</summary>
        public int[] Plan()
        {
            int[] p = (int[])Listens.Clone();
            for (int s = Result.HelpStartSlot; s >= 0 && s < p.Length; s++)
            {
                p[s] = -2;
                if (s - Result.HelpStartSlot + 1 >= SlotsUsedByHelp()) break;
            }
            return p;
        }

        private int SlotsUsedByHelp()
        {
            return Result.HelpSlots;
        }
    }

    /// <summary>
    /// 하루를 **전수 탐색**한다. 빔이나 표본이 아니다 — 여섯 칸뿐이므로 다 볼 수 있고,
    /// 다 봐야 「지배적 최선이 없다」를 증명이라고 부를 수 있다.
    ///
    /// 가지를 줄이는 규칙 하나만 쓴다: **얻을 것이 없는 문에 귀를 대는 가지는 지운다.**
    /// 그 칸에 소리가 없고 침묵도 이미 확인했다면, 귀를 대는 것은 쉬는 것과 결과가 같다 —
    /// 쉬는 가지가 이미 있으므로 최선을 잃지 않는다.
    ///
    /// 도움이 시작된 뒤의 엿듣기는 결과를 바꾸지 못하므로 탐색하지 않는다.
    /// 그래서 계획 하나는 「슬롯 0..s-1 의 엿듣기 + 슬롯 s 에서 시작하는 도움」으로 끝난다.
    /// </summary>
    public static class PlanSearch
    {
        /// <summary>선택지마다의 최선. index 는 세대 index 이고 -1 칸은 「아무도 돕지 않기」다.</summary>
        public sealed class Result
        {
            public BestPlan[] ByChoice;      // 세대 수만큼
            public Outcome NoOne;
            public int Nodes;

            /// <summary>총 손실이 가장 작은 선택. 아무도 돕지 않는 것이 최선이면 -1.</summary>
            public int BestChoiceIndex()
            {
                int best = -1, bestTotal = NoOne.Total;
                for (int i = 0; i < ByChoice.Length; i++)
                {
                    if (ByChoice[i] == null) continue;
                    if (ByChoice[i].Result.Total < bestTotal)
                    { bestTotal = ByChoice[i].Result.Total; best = i; }
                }
                return best;
            }

            public int BestTotal()
            {
                int b = BestChoiceIndex();
                return b < 0 ? NoOne.Total : ByChoice[b].Result.Total;
            }
        }

        public static Result Exhaustive(Night night) { return Exhaustive(night, new SearchOptions()); }

        public static Result Exhaustive(Night night, SearchOptions opt)
        {
            int n = night.Households.Count;
            int slots = night.Data.Balance.day.slotCount;
            Knowledge k = new Knowledge(night);
            Result res = new Result();
            res.ByChoice = new BestPlan[n];
            res.NoOne = Outcome.HelpNoOne(night);
            int[] listens = new int[slots];
            for (int i = 0; i < slots; i++) listens[i] = -1;
            Dfs(night, k, opt, res, listens, 0, slots, n);
            return res;
        }

        private static void Dfs(Night night, Knowledge k, SearchOptions opt, Result res,
                                int[] listens, int slot, int slots, int n)
        {
            res.Nodes++;

            // 이 칸에서 도움을 시작하는 계획들을 채점한다.
            for (int c = 0; c < n; c++)
            {
                Outcome o = Outcome.Help(night, k, c, slot);
                if (o == null) continue;
                BestPlan cur = res.ByChoice[c];
                if (cur == null || o.Total < cur.Result.Total
                    || (o.Total == cur.Result.Total && o.Listens < cur.Result.Listens))
                {
                    BestPlan bp = new BestPlan();
                    bp.ChoiceIndex = c;
                    bp.ChoiceId = night.Households[c].id;
                    bp.Result = o;
                    bp.Listens = (int[])listens.Clone();
                    res.ByChoice[c] = bp;
                }
            }

            if (slot >= slots) return;

            // 이 칸에 아무것도 하지 않는다.
            listens[slot] = -1;
            Dfs(night, k, opt, res, listens, slot + 1, slots, n);

            // 이 칸에 문 하나에 귀를 댄다. 얻을 것이 있는 문만 본다.
            for (int d = 0; d < n; d++)
            {
                if (opt.ForbidSilentDoors && night.Households[d].voice == Voices.Silent) continue;
                if (!k.WorthListening(d, slot)) continue;
                k.Listen(d, slot);
                listens[slot] = d;
                Dfs(night, k, opt, res, listens, slot + 1, slots, n);
                k.Unlisten(d, slot);
            }
            listens[slot] = -1;
        }
    }
}
