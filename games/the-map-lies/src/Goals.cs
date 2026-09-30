using System.Collections.Generic;
using MapLies.Data;

namespace MapLies.Sim
{
    public sealed class ChapterResult
    {
        public string ChapterId = "";
        public string PolicyName = "";
        public int Seed;

        public bool ReachOk;
        public bool DwellingOk;
        public bool PlazaOk;
        public bool NoTrappedOk;
        public bool GoalMet { get { return ReachOk && DwellingOk && PlazaOk && NoTrappedOk; } }

        public int Dwellings;
        public int PlazaSize;
        public int Harm;
        public int PermitSpent;
        public int Days;
        public int Refused;
        public bool Converged;
        public readonly List<string> TrappedAtEnd = new List<string>();

        /// <summary>점수. 목표 보상 − 해 x 벌점 − 허가비. 전부 정수다.</summary>
        public int Score;

        public override string ToString()
        {
            return ChapterId + "/" + PolicyName + ": " + (GoalMet ? "됨" : "안 됨")
                   + " 점수 " + Score + " (집 " + Dwellings + " · 광장 " + PlazaSize
                   + " · 해 " + Harm + " · 허가 " + PermitSpent + " · 거부 " + Refused + ")";
        }
    }

    /// <summary>
    /// 시의회가 요구하는 것을 재는 자.
    ///
    /// **목표를 셋 겹쳐 쓴 이유가 있다.** 「길을 내라」만 두면 전부 길로 덮는 것이 늘 답이 되고,
    /// 「집을 지어라」만 두면 전부 건물로 덮는 것이 답이 된다. 셋을 같이 두면
    /// 어느 한쪽으로 덮는 정책이 반드시 다른 쪽에서 무너진다 — `NoDominantStrategy` 가 그것을 본다.
    /// </summary>
    public static class Goals
    {
        public static ChapterResult Evaluate(GameData d, ChapterDef ch, CitySim sim, string policyName)
        {
            ChapterResult r = new ChapterResult
            {
                ChapterId = ch.id, PolicyName = policyName, Seed = sim.Seed,
                Days = sim.Day, Harm = sim.TotalHarm, PermitSpent = sim.Plan.PermitSpent,
                Refused = sim.RefusedCells().Count, Converged = sim.Converged
            };

            r.ReachOk = true;
            foreach (CellRef c in ch.reachCells)
                if (!sim.Actual.ReachesExit(c.x, c.y, d.City.exits)) r.ReachOk = false;

            r.Dwellings = sim.Actual.Dwellings();
            r.DwellingOk = ch.dwellingMin < 0 || r.Dwellings >= ch.dwellingMin;

            if (ch.plazaMin < 0) { r.PlazaOk = true; r.PlazaSize = -1; }
            else
            {
                r.PlazaSize = sim.Actual.PlazaComponentSize(ch.plazaSeed.x, ch.plazaSeed.y);
                r.PlazaOk = r.PlazaSize >= ch.plazaMin;
            }

            r.TrappedAtEnd.AddRange(sim.Trapped());
            r.NoTrappedOk = !ch.requireNoTrapped || r.TrappedAtEnd.Count == 0;

            r.Score = (r.GoalMet ? ch.goalReward : 0)
                      - r.Harm * d.Balance.harmPenaltyPerPoint
                      - r.PermitSpent;
            return r;
        }
    }
}
