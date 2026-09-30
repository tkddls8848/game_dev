using System;
using Interp.Data;

namespace Interp.Sim
{
    /// <summary>
    /// 결말은 "누가 이겼나"가 아니라 **무엇이 오해로 남았는가**로 갈린다.
    /// 조건은 data/endings.json 에 있고 priority 오름차순으로 훑어 처음 맞는 것이 결말이다.
    /// </summary>
    public static class Endings
    {
        public static string Resolve(GameData d, SessionResult r)
        {
            EndingDef[] list = (EndingDef[])d.Endings.endings.Clone();
            Array.Sort(list, (a, b) => a.priority.CompareTo(b.priority));
            foreach (EndingDef e in list) if (Matches(e, r)) return e.id;
            return "e_unclassified";
        }

        public static bool Matches(EndingDef e, SessionResult r)
        {
            if (e.requiresSigned == 1 && !r.Signed) return false;
            if (e.requiresSigned == 0 && r.Signed) return false;
            if (e.tensionMin >= 0 && r.Tension < e.tensionMin) return false;
            if (e.tensionMax >= 0 && r.Tension > e.tensionMax) return false;
            if (e.suspicionMin >= 0 && r.Suspicion < e.suspicionMin) return false;
            if (e.suspicionMax >= 0 && r.Suspicion > e.suspicionMax) return false;
            if (e.standingMin >= 0 && r.Standing.Count < e.standingMin) return false;
            if (e.standingMax >= 0 && r.Standing.Count > e.standingMax) return false;
            if (e.exposedMin >= 0 && r.Exposed.Count < e.exposedMin) return false;
            if (e.unsetClauseMin >= 0 && r.UnsetClauses < e.unsetClauseMin) return false;
            if (e.favorGapMin >= 0 && Math.Abs(r.FavorA - r.FavorB) < e.favorGapMin) return false;
            return true;
        }
    }
}
