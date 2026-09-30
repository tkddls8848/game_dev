using System.Collections.Generic;
using RedPen.Data;

namespace RedPen.Sim
{
    /// <summary>
    /// 결과를 재는 자들. **여럿이어야 한다** — 자가 하나뿐이면 "가장 좋은 펜"이 하나로 정해지고
    /// 「혹독함은 양날이다」가 물을 것이 없어진다.
    /// 가중치는 data/balance.json 의 objectives 에 있다. 전부 정수 산술이다.
    /// </summary>
    public static class Objectives
    {
        public static int Score(ObjectiveDef o, RunResult r)
        {
            int s = 0;
            s += r.Quality * o.qualityWeight / 100;
            s += r.Voice * o.voiceWeight / 100;
            s += r.Confidence * o.confidenceWeight / 100;
            s += r.Trust * o.trustWeight / 100;
            s -= r.Stubborn * o.stubbornPenalty / 100;
            s += r.Masterpieces * o.masterpieceBonus;
            if (r.Publishable) s += o.publishBonus;
            s -= r.LostSentences * o.lostSentencePenalty;
            if (r.Withdrawn || r.Silenced) s -= o.withdrawnPenalty;
            return s;
        }

        public static int Score(GameData d, string objectiveId, RunResult r)
        {
            return Score(d.Objective(objectiveId), r);
        }

        public static int TotalOverSeeds(GameData d, string objectiveId, IPolicy p, bool editsMatter = true)
        {
            ObjectiveDef o = d.Objective(objectiveId);
            int sum = 0;
            foreach (int seed in d.AllSeeds()) sum += Score(o, ManuscriptSim.Run(d, seed, p, editsMatter));
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
