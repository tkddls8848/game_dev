using System;
using System.Collections.Generic;
using Interp.Data;

namespace Interp.Sim
{
    public sealed class Plan
    {
        public Dictionary<string, string> ByStage = new Dictionary<string, string>();
        public int Score;
        public SessionResult Result;

        public Plan Clone()
        {
            Plan p = new Plan { Score = Score, Result = Result };
            foreach (KeyValuePair<string, string> kv in ByStage) p.ByStage[kv.Key] = kv.Value;
            return p;
        }

        /// <summary>이 계획이 쓴 결의 가짓수. 한 가지만 되풀이하는 것이 최선이면 1이다.</summary>
        public int DistinctRegisters(GameData d)
        {
            HashSet<string> set = new HashSet<string>();
            if (Result == null) return 0;
            foreach (TraceStep s in Result.Trace) set.Add(s.Register);
            return set.Count;
        }
    }

    /// <summary>
    /// 한 목표 아래 가장 좋은 역어 조합을 찾는다.
    /// 마디 12개 × 후보 5개면 전수는 수천만 가지다 — 빔으로 훑고,
    /// 남은 마디는 정확 정책으로 채워 끝까지 굴려 점수를 낸다(끝까지 굴려야 결말이 나온다).
    /// </summary>
    public static class PlanSearch
    {
        public static Plan Best(GameData d, int seed, string objectiveId, int beamWidth = 48)
        {
            ObjectiveDef obj = d.Objective(objectiveId);
            IPolicy fill = Policies.Exact;

            List<Plan> beam = new List<Plan> { new Plan() };
            Plan best = null;

            for (int i = 0; i < d.Stages.Count; i++)
            {
                StageDef stage = d.Stages[i];
                List<Plan> next = new List<Plan>();

                foreach (Plan p in beam)
                {
                    // 이 계획으로 여기까지 왔을 때 실제로 나오는 발화를 알아야 한다.
                    SessionResult probe = SessionSim.Run(d, seed, new Policies.Scripted(p.ByStage, fill));
                    TraceStep here = null;
                    foreach (TraceStep s in probe.Trace) if (s.StageId == stage.id) { here = s; break; }
                    if (here == null) { next.Add(p); continue; }   // 이미 결렬돼 이 마디까지 오지 않는다

                    RoundDef round = Divergence.FindRound(d, stage.id, here.RoundId);
                    if (round == null) { next.Add(p); continue; }

                    foreach (RenderDef r in round.renderings)
                    {
                        Plan c = p.Clone();
                        c.ByStage[stage.id] = r.id;
                        c.Result = SessionSim.Run(d, seed, new Policies.Scripted(c.ByStage, fill));
                        c.Score = Objectives.Score(obj, c.Result);
                        next.Add(c);
                        if (best == null || c.Score > best.Score) best = c.Clone();
                    }
                }

                next.Sort((x, y) => y.Score.CompareTo(x.Score));
                beam = next.GetRange(0, Math.Min(beamWidth, next.Count));
            }

            foreach (Plan p in beam) if (best == null || p.Score > best.Score) best = p;
            if (best != null && best.Result == null)
                best.Result = SessionSim.Run(d, seed, new Policies.Scripted(best.ByStage, fill));
            return best;
        }

        /// <summary>씨드 전부에서 찾은 최선 계획들의 점수 합.</summary>
        public static int BestTotalOverSeeds(GameData d, string objectiveId, out List<Plan> plans, int beamWidth = 48)
        {
            plans = new List<Plan>();
            int sum = 0;
            foreach (int seed in d.AllSeeds())
            {
                Plan p = Best(d, seed, objectiveId, beamWidth);
                plans.Add(p);
                sum += p.Score;
            }
            return sum;
        }
    }
}
