using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RedPen.Data;
using RedPen.Sim;

namespace RedPen.Tests
{
    /// <summary>
    /// 공통 검사기 「NoDominantStrategy」 — 한 가지 수를 되풀이하는 것이 최적이 아닌가.
    /// 여기서 '한 가지 수'는 **한 부호만 다섯 회차 내내 되풀이하는 것**이다.
    /// 자마다 탐욕 탐색으로 최선 교정을 찾고, 그것이 부호 되풀이 정책 전부를 이기는지 본다.
    /// </summary>
    [TestFixture]
    public class NoDominantStrategyTests
    {
        private GameData D { get { return TestWorld.Data; } }

        [Test]
        public void NoDominantStrategy()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 자별 최선 교정(탐색) vs 한 부호만 되풀이하기 ───────────────");
            sb.Append(string.Format("{0,-15}{1,8}", "자", "최선"));
            foreach (IPolicy p in Policies.Singles(D)) sb.Append(string.Format("{0,10}", p.Id));
            sb.AppendLine("   쓴 부호");

            foreach (string oid in Objectives.AllIds(D))
            {
                List<EditPlan> plans;
                int best = PlanSearch.BestTotalOverSeeds(D, oid, out plans);
                int marks = 0;
                foreach (EditPlan p in plans) if (p.DistinctMarks() > marks) marks = p.DistinctMarks();

                sb.Append(string.Format("{0,-15}{1,8}", oid, best));
                foreach (IPolicy p in Policies.Singles(D))
                {
                    int s = Objectives.TotalOverSeeds(D, oid, p);
                    sb.Append(string.Format("{0,10}", s));
                    Assert.That(best, Is.GreaterThan(s),
                        oid + ": 한 부호(" + p.Id + ")만 되풀이하는 것이 탐색한 최선(" + best + ")과 같거나 낫다(" + s + ")");
                }
                sb.AppendLine(string.Format("{0,9}", marks + "가지"));
                Assert.That(marks, Is.GreaterThanOrEqualTo(2),
                    oid + " 의 최선 교정이 부호 한 가지만 쓴다 — 그 부호가 정답이라는 뜻이다");
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        /// <summary>
        /// 출판사의 셈(플레이어가 실제로 쥐는 자)에서는 부호를 **셋 이상** 섞어야 최선이어야 한다.
        /// </summary>
        [Test]
        public void ThePublishersMeasureNeedsAMixedHand()
        {
            List<EditPlan> plans;
            int best = PlanSearch.BestTotalOverSeeds(D, "o_house", out plans);
            int maxMarks = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── o_house 의 최선 교정 (씨드별) ─────────────────────────────");
            for (int i = 0; i < plans.Count; i++)
            {
                EditPlan p = plans[i];
                if (p.DistinctMarks() > maxMarks) maxMarks = p.DistinctMarks();
                Dictionary<string, int> count = new Dictionary<string, int>();
                foreach (LiveSentence s in p.Result.Sentences)
                    foreach (string m in s.MarkHistory)
                    { int c; count.TryGetValue(m, out c); count[m] = c + 1; }
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, int> kv in count) parts.Add(kv.Key + "×" + kv.Value);
                sb.AppendLine(string.Format("씨드 {0,8} 점수 {1,5} 부호 {2}가지 · {3} · 걸작 {4} · 결말 {5}",
                    D.AllSeeds()[i], p.Score, p.DistinctMarks(), string.Join(" ", parts),
                    p.Result.Masterpieces, p.Result.EndingId));
            }
            sb.AppendLine("합계 " + best);
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(maxMarks, Is.GreaterThanOrEqualTo(3),
                "출판사의 셈을 가장 잘 만족시키는 교정이 부호를 " + maxMarks + "가지만 쓴다");
            Assert.That(best, Is.GreaterThan(Objectives.TotalOverSeeds(D, "o_house", Policies.MiddlePen)),
                "손으로 쓴 「작가를 읽는다」가 탐색보다 낫다 — 탐색이 약하거나 정책이 이미 최적이다");
        }

        /// <summary>
        /// 쓸모없는 부호가 없는가. **어떤 자 아래에서도 한 번도 쓰이지 않는 부호**가 있으면
        /// 그것은 교정지를 채우는 장식이다.
        /// </summary>
        [Test]
        public void EveryMarkIsWorthUsingUnderSomeMeasure()
        {
            HashSet<string> used = new HashSet<string>();
            foreach (string oid in Objectives.AllIds(D))
            {
                List<EditPlan> plans;
                PlanSearch.BestTotalOverSeeds(D, oid, out plans);
                foreach (EditPlan p in plans)
                    foreach (LiveSentence s in p.Result.Sentences)
                        foreach (string m in s.MarkHistory) used.Add(m);
            }
            List<string> never = new List<string>();
            foreach (MarkDef m in D.AllMarks) if (!used.Contains(m.id)) never.Add(m.id);
            TestContext.Out.WriteLine("부호 " + D.AllMarks.Count + "개 중 어느 최선 교정에도 쓰이지 않은 것 "
                                      + never.Count + (never.Count > 0 ? ": " + string.Join(", ", never) : ""));
            Assert.That(never.Count, Is.LessThanOrEqualTo(1),
                "쓰이지 않는 부호가 " + never.Count + "개다: " + string.Join(", ", never));
        }

        /// <summary>
        /// **아끼는 문장에 삭제선을 긋는 것이 최선이 되어서는 안 된다.**
        /// 되면 "작가가 아끼는 것"이라는 값이 아무 일도 하지 않는다는 뜻이다.
        /// </summary>
        [Test]
        public void CuttingTheBelovedSentenceIsNeverOptimal()
        {
            SentenceDef beloved = null;
            foreach (SentenceDef s in D.AllSentences)
                if (beloved == null || s.prideGuard > beloved.prideGuard) beloved = s;

            foreach (string oid in Objectives.AllIds(D))
            {
                List<EditPlan> plans;
                PlanSearch.BestTotalOverSeeds(D, oid, out plans);
                foreach (EditPlan p in plans)
                    foreach (LiveSentence s in p.Result.Sentences)
                        if (s.Id == beloved.id)
                            Assert.That(s.Deleted, Is.False,
                                oid + " 의 최선 교정이 작가가 가장 아끼는 문장(" + beloved.id + ")을 지운다");
            }
            TestContext.Out.WriteLine("가장 아끼는 문장 " + beloved.id + " (애착 " + beloved.prideGuard
                                      + ") 은 어느 자의 최선 교정에서도 살아남는다");
        }
    }
}
