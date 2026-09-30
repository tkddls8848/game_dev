using System.Collections.Generic;
using Interp.Data;

namespace Interp.Sim
{
    /// <summary>
    /// 결말을 재는 자들. **여럿이어야 한다** — 자가 하나뿐이면 "최선의 방침"이 하나로 정해지고
    /// 「안전한 낱말이 없다」가 물을 것이 없어진다.
    /// 가중치는 data/balance.json 의 objectives 에 있다. 전부 정수 산술이다.
    /// </summary>
    public static class Objectives
    {
        public static int Score(ObjectiveDef o, SessionResult r)
        {
            int s = 0;
            if (r.Signed) s += o.signBonus;
            s += (100 - r.Tension) * o.peaceWeight / 100;
            s += r.FavorA * o.favorAWeight / 100;
            s += r.FavorB * o.favorBWeight / 100;
            s -= r.Suspicion * o.suspicionWeight / 100;
            s -= r.Standing.Count * o.standingWeight;
            s -= r.Exposed.Count * o.exposedWeight;
            s -= r.UnsetClauses * o.unsetWeight;
            return s;
        }

        public static int Score(GameData d, string objectiveId, SessionResult r)
        {
            return Score(d.Objective(objectiveId), r);
        }

        /// <summary>한 정책을 씨드 전부에 돌린 합계. 한 씨드로 정책을 견주지 않는다.</summary>
        public static int TotalOverSeeds(GameData d, string objectiveId, IPolicy policy)
        {
            ObjectiveDef o = d.Objective(objectiveId);
            int sum = 0;
            foreach (int seed in d.AllSeeds()) sum += Score(o, SessionSim.Run(d, seed, policy));
            return sum;
        }

        public static IList<string> AllIds(GameData d)
        {
            List<string> ids = new List<string>();
            foreach (ObjectiveDef o in d.Balance.objectives) ids.Add(o.id);
            return ids;
        }
    }
}
