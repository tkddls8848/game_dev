using System.Collections.Generic;
using CurseLedger.Data;

namespace CurseLedger.Sim
{
    /// <summary>
    /// 회차 하나를 뜯어보는 자.
    /// **"결정의 값이 언제 오는가"를 수치로 만든다** — 이 PoC의 기제가 장식이 아니라는 증거다.
    /// </summary>
    public static class LedgerAudit
    {
        /// <summary>
        /// 한 회차에서 상태를 움직인 총량 중 **앞 세대가 정한 몫**(%).
        /// 100%에 가까우면 플레이어는 자기 결정의 결과를 거의 보지 못한다는 뜻이고,
        /// 0%에 가까우면 이 PoC의 기제가 거짓이라는 뜻이다.
        /// </summary>
        public static int AncestorSharePercent(GameData d, LedgerRun r)
        {
            int fromBefore = 0, fromNow = 0;
            foreach (LedgerLine l in r.Lines)
            {
                foreach (Bill b in l.BillsArrived)
                {
                    if (b.FromGeneration < l.Generation) fromBefore += b.Magnitude;
                    else fromNow += b.Magnitude;
                }
                if (l.Aborted || string.IsNullOrEmpty(l.RiteId)) continue;
                RiteDef rite = d.Rite(l.RiteId);
                fromNow += Bill.From(rite.immediate, 0, 0, rite.id).Magnitude;
            }
            int total = fromBefore + fromNow;
            if (total == 0) return 0;
            return fromBefore * 100 / total;
        }

        /// <summary>결정한 대에는 보이지 않고 미래에 놓인 청구서의 수.</summary>
        public static int BillsScheduled(LedgerRun r)
        {
            int n = 0;
            foreach (LedgerLine l in r.Lines) n += l.BillsScheduled.Count;
            return n;
        }

        /// <summary>앞 세대의 청구서가 도착한 순간 끝나 제례조차 올리지 못한 대의 수.</summary>
        public static int AbortedGenerations(LedgerRun r)
        {
            int n = 0;
            foreach (LedgerLine l in r.Lines) if (l.Aborted) n++;
            return n;
        }

        /// <summary>이 정책이 쓴 제례의 종류.</summary>
        public static HashSet<string> RitesUsed(LedgerRun r)
        {
            HashSet<string> s = new HashSet<string>();
            foreach (string id in r.Policy) if (id != null) s.Add(id);
            return s;
        }

        /// <summary>회차를 한 줄로. 검사기가 수치를 나란히 낼 때 쓴다.</summary>
        public static string OneLine(LedgerRun r)
        {
            return r.EndingKorean + " · " + r.GenerationsSurvived + "대"
                   + " · 희생자 " + r.VictimCount
                   + " · 번영 " + r.Prosperity
                   + " · 속박 " + r.Binding
                   + " · 노여움 " + r.Wrath
                   + " · 기력 " + r.HouseVitality
                   + " · 원한 " + r.Resentment
                   + " · 해제 " + r.ReleaseProgress
                   + " · 마을 살아있음 " + (r.VillageAlive ? "예" : "아니오");
        }
    }

    /// <summary>
    /// 두 극단을 만드는 정책 가족. BothExtremesFail 이 이것들을 돌린다.
    /// </summary>
    public static class ExtremePolicies
    {
        /// <summary>「계속 유지」 — 언제나 사람을 바친다. 젊은 이가 있으면 젊은 이를 고른다.</summary>
        public static LedgerRun Keep(GameData d, int seed, int generations)
        {
            CurseSim sim = new CurseSim(d, seed, generations);
            LedgerState s = sim.NewState();
            List<string> taken = new List<string>();
            for (int g = 0; g < generations && !s.Ended; g++)
            {
                string pick = null;
                if (sim.Available(s, d.Rite("offer_young"))) pick = "offer_young";
                else if (sim.Available(s, d.Rite("offer_elder"))) pick = "offer_elder";
                if (pick == null) break;
                if (!sim.Step(s, pick)) break;
                taken.Add(pick);
            }
            return sim.Settle(s, taken.ToArray());
        }

        /// <summary>「즉시 해제」 — 언제나 해제 의식을 잇는다. 곳간이 비면 장부를 덮어 채운다.</summary>
        public static LedgerRun Release(GameData d, int seed, int generations)
        {
            CurseSim sim = new CurseSim(d, seed, generations);
            LedgerState s = sim.NewState();
            List<string> taken = new List<string>();
            for (int g = 0; g < generations && !s.Ended; g++)
            {
                string pick = sim.Available(s, d.Rite("rite_release")) ? "rite_release" : "close_ledger";
                if (!sim.Step(s, pick)) break;
                taken.Add(pick);
            }
            return sim.Settle(s, taken.ToArray());
        }
    }
}
