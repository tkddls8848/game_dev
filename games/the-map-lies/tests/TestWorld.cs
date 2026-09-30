using System.Collections.Generic;
using MapLies.Data;
using MapLies.Sim;

namespace MapLies.Tests
{
    /// <summary>
    /// 테스트가 공유하는 세계. data/ 를 한 번만 읽고 무거운 회차를 재사용한다.
    /// 되돌릴 길 탐색이 이 PoC에서 가장 비싼 일이다 — 사고마다 후보 수십 개를 깊이 셋까지 본다.
    /// </summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<string, ChapterResult> _runs = new Dictionary<string, ChapterResult>();
        private static readonly Dictionary<string, RescueResult> _rescues = new Dictionary<string, RescueResult>();

        public static GameData Data
        {
            get { if (_data == null) _data = GameData.Load(); return _data; }
        }

        /// <summary>data/ 를 다시 읽어 마음대로 망가뜨릴 수 있는 사본을 만든다. 음성 대조군이 쓴다.</summary>
        public static GameData CloneData() { return GameData.Load(Data.DataRoot); }

        public static List<string> ChapterIds()
        {
            List<string> ids = new List<string>();
            foreach (ChapterDef c in Data.AllChapters) ids.Add(c.id);
            return ids;
        }

        public static List<string> MistakeIds()
        {
            List<string> ids = new List<string>();
            foreach (MistakeDef m in Data.AllMistakes) ids.Add(m.id);
            return ids;
        }

        public static ChapterResult Run(string chapterId, string policy, int trial)
        {
            string key = chapterId + "|" + policy + "|" + trial;
            ChapterResult r;
            if (_runs.TryGetValue(key, out r)) return r;
            r = Policies.Run(Data, chapterId, policy, trial);
            _runs[key] = r;
            return r;
        }

        /// <summary>정책 하나의 장 전체 평균 점수(정수 나눗셈). 씨드 전부를 돈다.</summary>
        public static int MeanScore(string policy)
        {
            int sum = 0, n = 0;
            foreach (string cid in ChapterIds())
                for (int t = 0; t < Data.Balance.trialSeeds; t++) { sum += Run(cid, policy, t).Score; n++; }
            return n == 0 ? 0 : sum / n;
        }

        public static int MeanScore(string chapterId, string policy)
        {
            int sum = 0, n = 0;
            for (int t = 0; t < Data.Balance.trialSeeds; t++) { sum += Run(chapterId, policy, t).Score; n++; }
            return n == 0 ? 0 : sum / n;
        }

        public static int GoalMetCount(string chapterId, string policy)
        {
            int n = 0;
            for (int t = 0; t < Data.Balance.trialSeeds; t++) if (Run(chapterId, policy, t).GoalMet) n++;
            return n;
        }

        public static RescueResult Rescue(string mistakeId, int trial)
        {
            string key = mistakeId + "|" + trial;
            RescueResult r;
            if (_rescues.TryGetValue(key, out r)) return r;
            MistakeDef mk = Data.Mistake(mistakeId);
            r = RescueSearch.Find(Data, mk, unchecked(mk.seed + trial * 7919));
            _rescues[key] = r;
            return r;
        }

        /// <summary>되돌릴 길 탐색이 무거우므로 씨드를 적게 돈다. 몇 개인지는 여기 한 곳에 적는다.</summary>
        public const int RescueSeeds = 6;
    }
}
