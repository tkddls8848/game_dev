using System.Collections.Generic;
using RedPen.Data;

namespace RedPen.Sim
{
    public sealed class EditPlan
    {
        public Dictionary<string, string> ByRoundSentence = new Dictionary<string, string>();
        public int Score;
        public RunResult Result;

        public EditPlan Clone()
        {
            EditPlan p = new EditPlan { Score = Score, Result = Result };
            foreach (KeyValuePair<string, string> kv in ByRoundSentence) p.ByRoundSentence[kv.Key] = kv.Value;
            return p;
        }

        /// <summary>이 계획이 실제로 쓴 부호의 가짓수.</summary>
        public int DistinctMarks()
        {
            HashSet<string> set = new HashSet<string>();
            if (Result == null) return 0;
            foreach (LiveSentence s in Result.Sentences) foreach (string m in s.MarkHistory) set.Add(m);
            return set.Count;
        }
    }

    /// <summary>
    /// 한 목표 아래 가장 좋은 교정을 찾는다.
    /// (회차 × 문장 × 부호)를 전수로 훑으면 7^(5×9) 가지다 — 탐욕으로 한 칸씩 고르고
    /// 남은 칸은 「작가를 읽는다」로 채워 끝까지 굴려 점수를 낸다(끝까지 굴려야 결말이 나온다).
    /// 두 번 훑는다: 첫 번째는 앞 회차가 뒤를 모르고, 두 번째는 알고 고친다.
    /// </summary>
    public static class PlanSearch
    {
        /// <summary>
        /// 여러 출발점에서 굴려 가장 좋은 것을 고른다.
        /// 한 곳에서만 올라가면 "부호 하나만 되풀이하기"보다 못한 데서 멈출 수 있고,
        /// 그러면 NoDominantStrategy 가 설계가 아니라 **탐색의 약함**을 재게 된다.
        /// </summary>
        public static EditPlan Best(GameData d, int seed, string objectiveId, int passes = 2)
        {
            ObjectiveDef obj = d.Objective(objectiveId);
            List<EditPlan> starts = new List<EditPlan> { new EditPlan() };
            foreach (MarkDef m in d.AllMarks)
            {
                EditPlan seedPlan = new EditPlan();
                for (int round = 0; round < d.Balance.rounds; round++)
                    foreach (SentenceDef sd in d.AllSentences)
                        seedPlan.ByRoundSentence[Policies.Scripted.Key(round, sd.id)] = m.id;
                starts.Add(seedPlan);
            }

            EditPlan overall = null;
            foreach (EditPlan start in starts)
            {
                EditPlan p = Climb(d, seed, obj, start, passes);
                if (overall == null || p.Score > overall.Score) overall = p;
            }
            return overall;
        }

        private static EditPlan Climb(GameData d, int seed, ObjectiveDef obj, EditPlan plan, int passes)
        {
            IPolicy fill = Policies.MiddlePen;
            plan.Result = ManuscriptSim.Run(d, seed, new Policies.Scripted(plan.ByRoundSentence, fill));
            plan.Score = Objectives.Score(obj, plan.Result);

            for (int pass = 0; pass < passes; pass++)
            {
                for (int round = 0; round < d.Balance.rounds; round++)
                {
                    foreach (SentenceDef sd in d.AllSentences)
                    {
                        string key = Policies.Scripted.Key(round, sd.id);
                        string keep;
                        plan.ByRoundSentence.TryGetValue(key, out keep);
                        int bestScore = plan.Score;
                        string bestMark = keep;
                        RunResult bestResult = plan.Result;

                        foreach (MarkDef m in d.AllMarks)
                        {
                            plan.ByRoundSentence[key] = m.id;
                            RunResult r = ManuscriptSim.Run(d, seed, new Policies.Scripted(plan.ByRoundSentence, fill));
                            int s = Objectives.Score(obj, r);
                            if (s > bestScore) { bestScore = s; bestMark = m.id; bestResult = r; }
                        }

                        if (bestMark == null) plan.ByRoundSentence.Remove(key);
                        else plan.ByRoundSentence[key] = bestMark;
                        plan.Score = bestScore;
                        plan.Result = bestResult;
                    }
                }
            }
            return plan;
        }

        public static int BestTotalOverSeeds(GameData d, string objectiveId, out List<EditPlan> plans, int passes = 2)
        {
            plans = new List<EditPlan>();
            int sum = 0;
            foreach (int seed in d.AllSeeds())
            {
                EditPlan p = Best(d, seed, objectiveId, passes);
                plans.Add(p);
                sum += p.Score;
            }
            return sum;
        }
    }
}
