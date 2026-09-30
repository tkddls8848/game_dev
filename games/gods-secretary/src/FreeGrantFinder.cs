using System.Collections.Generic;
using Secretary.Data;

namespace Secretary.Sim
{
    /// <summary>대가 없이 들어줄 수 있었던 한 수. 하나라도 있으면 최적 전략이 「그것만 허가한다」가 된다.</summary>
    public sealed class FreeGrant
    {
        public int Day;
        public string PrayerId;
        public string SourceSoulId;
        public string Domain;
        public int Units;
        public string Why;
        public override string ToString()
        {
            return "날 " + Day + " · " + PrayerId + " 를 " + SourceSoulId + " 에게서 " + Units
                   + " 빼 들어줬는데 그 자리의 고통이 오르지 않았다 (" + Why + ")";
        }
    }

    /// <summary>
    /// ★ **NoFreeGrant 의 탐색기.**
    ///
    /// 「구조적으로 불가능하다」를 믿지 않고 매 판정 직전의 장부에서 실제로 뒤진다:
    /// 접수함의 편지 하나하나에 대해, 쓸 수 있는 출처 하나하나에서 한 단위부터 최대치까지 빼 보고
    /// **그 자리의 고통이 오르지 않는 경우**를 찾는다. 하나라도 찾으면 그 수가 공짜 허가다.
    ///
    /// 이 탐색기가 이가 있는지는 대조군으로 확인한다 — 장부 복사본에서 누구 하나에게 여유를
    /// 만들어 주면 탐색기가 **반드시** 그것을 찾아내야 한다(테스트가 그렇게 돌린다).
    /// </summary>
    public static class FreeGrantFinder
    {
        public sealed class Report
        {
            public int Probes;
            public List<FreeGrant> Found = new List<FreeGrant>();
            public int CheapestCost = int.MaxValue;
            public string CheapestWhere;
        }

        public static void Probe(SimState st, Report r)
        {
            foreach (Letter l in st.Docket)
            {
                int want = Office.GrantableUnits(st, l);
                if (want <= 0) continue;
                foreach (SourceDef src in l.Def.sources)
                {
                    int have = st.L.Have(src.soulId, l.Def.domain);
                    int max = src.maxUnits < have ? src.maxUnits : have;
                    if (max > want) max = want;
                    for (int u = 1; u <= max; u++)
                    {
                        r.Probes++;
                        int cost = st.L.CostOfDrawing(src.soulId, l.Def.domain, u);
                        if (cost < r.CheapestCost)
                        {
                            r.CheapestCost = cost;
                            r.CheapestWhere = l.Def.id + " / " + src.soulId + " / " + u;
                        }
                        if (cost <= 0)
                            r.Found.Add(new FreeGrant
                            {
                                Day = st.Day, PrayerId = l.Def.id, SourceSoulId = src.soulId,
                                Domain = l.Def.domain, Units = u,
                                Why = "have " + have + " · need " + st.L.Need(src.soulId, l.Def.domain)
                            });
                    }
                }
            }
        }
    }
}
