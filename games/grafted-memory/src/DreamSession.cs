using System;
using System.Collections.Generic;
using Graft.Data;

namespace Graft.Sim
{
    /// <summary>
    /// 꿈 한 회차. 씨드가 정하는 것은 딱 하나다 — **이번 밤에 어느 기억이 스크린이 되는가.**
    ///
    /// 난수는 System.Random 하나뿐이고 씨드는 데이터(commissions.json 의 seed)와 시행 번호에서 온다.
    /// UnityEngine.Random 은 쓰지 않는다(설계 원칙 5) — 한 번만 써도 헤드리스 재현이 깨진다.
    /// </summary>
    public sealed class DreamSession
    {
        public GameData Data { get; private set; }
        public CommissionDef Commission { get; private set; }
        public SubjectDef Subject { get; private set; }
        public MemoryNet Net { get; private set; }
        public MoodModel Moods { get; private set; }
        public TruthView Truth { get; private set; }
        public int Seed { get; private set; }
        public IList<string> Distorted { get; private set; }

        /// <summary>
        /// 일관성 규칙을 켠 세계인가. **ConsistencyIsNotTrivial 의 두 세계가 이 한 칸으로 갈린다** —
        /// 끈 세계에서는 떨림이 하나도 생기지 않아 무엇을 심어도 자리를 잡는다.
        /// </summary>
        public bool RulesEnabled { get; private set; }

        public static DreamSession Open(GameData data, string commissionId, int trial, bool rulesEnabled = true)
        {
            DreamSession s = new DreamSession();
            s.RulesEnabled = rulesEnabled;
            s.Data = data;
            s.Commission = data.Commission(commissionId);
            s.Subject = data.Subject(s.Commission.subjectId);
            s.Net = new MemoryNet(s.Subject);
            s.Moods = new MoodModel(data);
            s.Seed = unchecked(s.Commission.seed + trial * 7919);
            s.Distorted = DrawDistorted(s.Subject, s.Seed);
            s.Truth = new TruthView(s.Net, s.Distorted);
            return s;
        }

        /// <summary>
        /// 이번 밤의 스크린 기억을 뽑는다. 후보는 distortable 이 켜진 기억뿐이고, 순서는 id 로 못 박는다 —
        /// JSON 의 줄 순서가 바뀌어도 같은 씨드가 같은 결과를 내야 한다.
        /// </summary>
        private static List<string> DrawDistorted(SubjectDef subject, int seed)
        {
            List<string> pool = new List<string>();
            foreach (MemoryDef m in subject.memories) if (m.distortable) pool.Add(m.id);
            pool.Sort(StringComparer.Ordinal);

            int take = subject.distortedPerSession;
            if (take <= 0 || pool.Count == 0) return new List<string>();
            if (take > pool.Count) take = pool.Count;

            Random rng = new Random(seed);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                string t = pool[i]; pool[i] = pool[j]; pool[j] = t;
            }
            List<string> picked = pool.GetRange(0, take);
            picked.Sort(StringComparer.Ordinal);
            return picked;
        }

        public ProbeKnowledge FreshKnowledge()
        {
            return new ProbeKnowledge(Net, Truth, Commission.lucidityBudget, Data.Balance.corroborateRoutes);
        }

        /// <summary>꿈이 실제로 판정한다. 플레이어의 예측이 어땠는지는 여기에 들어오지 않는다.</summary>
        public GraftVerdict Judge(GraftChoice choice)
        {
            return GraftRules.Evaluate(Data, Net, Moods, Commission, choice, Truth, RulesEnabled);
        }

        /// <summary>플레이어가 쥔 것으로 미리 재 본다. 같은 규칙 코드, 다른 눈.</summary>
        public GraftVerdict Predict(ProbeKnowledge k, GraftChoice choice)
        {
            return GraftRules.Evaluate(Data, Net, Moods, Commission, choice, k, RulesEnabled);
        }
    }
}
