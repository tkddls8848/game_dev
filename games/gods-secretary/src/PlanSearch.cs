using System.Collections.Generic;
using Secretary.Data;

namespace Secretary.Sim
{
    /// <summary>
    /// 빔 탐색. 하루마다 (편지 x 판정 x 출처 순서) 를 펼치고 쌓인 고통이 작은 순으로 남긴다.
    ///
    /// **전수가 아니다** — 엿새 x 여덟 장 x 여덟 갈래면 5억을 넘는다. 그래서 여기서 나오는 것은
    /// 「최선」이 아니라 **「알려진 최선」**이고, 검사기도 그렇게 부른다. 정직하게 적어 둔다.
    /// </summary>
    public static class PlanSearch
    {
        public static SimState Best(GameData d, string volumeId, int seed)
        {
            return Best(d, volumeId, seed, d.Balance.conservation.enabled);
        }

        public static SimState Best(GameData d, string volumeId, int seed, bool conservation)
        {
            int width = d.Balance.search.beamWidth;
            List<SimState> beam = new List<SimState>();
            beam.Add(Office.Begin(d, volumeId, seed, conservation));

            while (true)
            {
                List<SimState> next = new List<SimState>();
                bool anyOpen = false;
                foreach (SimState st in beam)
                {
                    if (st.Finished) { next.Add(st); continue; }
                    anyOpen = true;
                    foreach (SimState child in Expand(st)) next.Add(child);
                }
                if (!anyOpen) break;
                next.Sort(delegate (SimState a, SimState b)
                {
                    int c = Score(a) - Score(b);
                    if (c != 0) return c;
                    return string.CompareOrdinal(Signature(a), Signature(b));
                });
                if (next.Count > width) next.RemoveRange(width, next.Count - width);
                beam = next;
            }
            beam.Sort(delegate (SimState a, SimState b) { return a.Accrued - b.Accrued; });
            return beam[0];
        }

        /// <summary>지금까지 쌓인 고통 + 남은 날에 그대로 두면 쌓일 고통. 빔이 앞을 보게 해 준다.</summary>
        private static int Score(SimState st)
        {
            int daysLeft = st.Volume.days - st.Day;
            if (daysLeft < 0) daysLeft = 0;
            return st.Accrued + st.L.TotalHardship() * daysLeft;
        }

        /// <summary>같은 점수일 때 순서를 고정한다 — 씨드 재현성이 정렬 안정성에 기대지 않게 한다.</summary>
        private static string Signature(SimState st)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (VerdictRecord r in st.Log) sb.Append(r.PrayerId).Append(r.Kind).Append(r.Drawn).Append('|');
            return sb.ToString();
        }

        private static IEnumerable<SimState> Expand(SimState st)
        {
            List<SimState> outp = new List<SimState>();
            List<Letter> docket = new List<Letter>(st.Docket);
            foreach (Letter l in docket)
            {
                // 허가 — 출처 순서마다
                if (Office.GrantableUnits(st, l) > 0)
                    foreach (string[] order in Office.SourceOrders(l.Def))
                    {
                        SimState c = st.Clone();
                        if (!Office.Stamp(c, new Verdict { Kind = Verdicts.Grant, PrayerId = l.Def.id, SourceOrder = order }))
                            continue;
                        Settle(c); outp.Add(c);
                    }
                // 기각
                SimState dn = st.Clone();
                if (Office.Stamp(dn, new Verdict { Kind = Verdicts.Deny, PrayerId = l.Def.id })) { Settle(dn); outp.Add(dn); }
                // 보류
                SimState df = st.Clone();
                if (Office.Stamp(df, new Verdict { Kind = Verdicts.Defer, PrayerId = l.Def.id })) { Settle(df); outp.Add(df); }
            }
            // 오늘은 아무 도장도 찍지 않는다
            SimState pass = st.Clone();
            while (!pass.Finished && pass.StampsLeft > 0) pass.StampsLeft = 0;
            Settle(pass); outp.Add(pass);
            return outp;
        }

        private static void Settle(SimState st)
        {
            while (!st.Finished && st.StampsLeft <= 0) Office.EndDay(st);
        }
    }
}
