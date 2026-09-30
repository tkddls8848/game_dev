using System;
using RedPen.Data;

namespace RedPen.Sim
{
    /// <summary>
    /// 결말은 data/endings.json 에 있고 priority 오름차순으로 훑어 처음 맞는 것이다.
    /// </summary>
    public static class Endings
    {
        public static string Resolve(GameData d, RunResult r)
        {
            EndingDef[] list = (EndingDef[])d.Endings.endings.Clone();
            Array.Sort(list, (a, b) => a.priority.CompareTo(b.priority));
            foreach (EndingDef e in list) if (Matches(e, r)) return e.id;
            return "e_unclassified";
        }

        public static bool Matches(EndingDef e, RunResult r)
        {
            if (e.requiresWithdrawn == 1 && !r.Withdrawn) return false;
            if (e.requiresWithdrawn == 0 && r.Withdrawn) return false;
            if (e.requiresSilenced == 1 && !r.Silenced) return false;
            if (e.requiresSilenced == 0 && r.Silenced) return false;
            if (e.requiresPublishable == 1 && !r.Publishable) return false;
            if (e.requiresPublishable == 0 && r.Publishable) return false;
            if (e.masterpieceMin >= 0 && r.Masterpieces < e.masterpieceMin) return false;
            if (e.qualityMin >= 0 && r.Quality < e.qualityMin) return false;
            if (e.qualityMax >= 0 && r.Quality > e.qualityMax) return false;
            if (e.voiceMin >= 0 && r.Voice < e.voiceMin) return false;
            if (e.voiceMax >= 0 && r.Voice > e.voiceMax) return false;
            if (e.trustMax >= 0 && r.Trust > e.trustMax) return false;
            if (e.confidenceMax >= 0 && r.Confidence > e.confidenceMax) return false;
            if (e.lostSentenceMin >= 0 && r.LostSentences < e.lostSentenceMin) return false;
            return true;
        }
    }
}
