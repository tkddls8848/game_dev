using System.Collections.Generic;
using System.Text;
using Interp.Sim;
using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// 공통 검사기 「NoDominantStrategy」 — 한 가지 수를 되풀이하는 것이 최적이 아닌가.
    /// 여기서 '한 가지 수'는 **한 가지 결(register)만 되풀이하는 것**이다.
    /// 목표마다 빔 탐색으로 최선 계획을 찾고, 그것이 순수 정책 넷을 모두 이기며
    /// 결을 섞어 쓰는지 본다.
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
            sb.AppendLine("── 목표별 최선 계획 (빔 탐색) vs 한 결만 되풀이하기 ───────────");
            sb.AppendLine("자            최선   정확만   완곡만   강경만   오역만   쓴 결 가짓수");

            IList<IPolicy> pures = new List<IPolicy> { Policies.Exact, Policies.Soft, Policies.Hard, Policies.False };

            foreach (string oid in Objectives.AllIds(D))
            {
                List<Plan> plans;
                int best = PlanSearch.BestTotalOverSeeds(D, oid, out plans, 32);

                int registers = 0;
                foreach (Plan p in plans) if (p.DistinctRegisters(D) > registers) registers = p.DistinctRegisters(D);

                sb.Append(string.Format("{0,-13}{1,6}", oid, best));
                foreach (IPolicy p in pures)
                {
                    int s = Objectives.TotalOverSeeds(D, oid, p);
                    sb.Append(string.Format("{0,9}", s));
                    Assert.That(best, Is.GreaterThan(s),
                        oid + ": 한 결(" + p.Id + ")만 되풀이하는 것이 탐색한 최선(" + best + ")을 이기거나 같다(" + s + ")");
                }
                sb.AppendLine(string.Format("{0,10}", registers));

                Assert.That(registers, Is.GreaterThanOrEqualTo(2),
                    oid + " 의 최선 계획이 결 한 가지만 쓴다 — 그 결이 정답이라는 뜻이다");
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        /// <summary>
        /// 통역 자신의 목표(받은 지시)에서는 결을 **셋 이상** 섞어야 최선이어야 한다.
        /// 플레이어가 실제로 쥐는 자가 이것이고, 여기서 한 가지로 밀어붙이는 것이 답이면
        /// 마디마다 고르는 일이 사라진다.
        /// </summary>
        [Test]
        public void ThePlayersOwnBriefNeedsAMixedHand()
        {
            List<Plan> plans;
            int best = PlanSearch.BestTotalOverSeeds(D, "o_mission", out plans, 40);
            int maxRegisters = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── o_mission 의 최선 계획 (씨드별) ─────────────────────────");
            for (int i = 0; i < plans.Count; i++)
            {
                Plan p = plans[i];
                int reg = p.DistinctRegisters(D);
                if (reg > maxRegisters) maxRegisters = reg;
                List<string> line = new List<string>();
                foreach (TraceStep s in p.Result.Trace) line.Add(Localization.Register(s.Register));
                sb.AppendLine(string.Format("씨드 {0,8} 점수 {1,5} 결 {2}가지  {3}",
                    D.AllSeeds()[i], p.Score, reg, string.Join(" ", line)));
            }
            sb.AppendLine("합계 " + best);
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(maxRegisters, Is.GreaterThanOrEqualTo(3),
                "받은 지시를 가장 잘 따르는 계획이 결을 " + maxRegisters + "가지만 쓴다");
        }

        /// <summary>
        /// 쓸모없는 후보가 없는가. **어떤 자 아래에서도 한 번도 고르이지 않는 역어**가 있으면
        /// 그것은 화면만 채우는 선택지다.
        /// </summary>
        [Test]
        public void EveryRenderingIsChosenByTheSearchUnderSomeMeasure()
        {
            HashSet<string> used = new HashSet<string>();
            foreach (string oid in Objectives.AllIds(D))
            {
                List<Plan> plans;
                PlanSearch.BestTotalOverSeeds(D, oid, out plans, 32);
                foreach (Plan p in plans) foreach (TraceStep s in p.Result.Trace) used.Add(s.RenderingId);
            }
            foreach (IPolicy pol in Policies.All())
                foreach (int seed in D.AllSeeds())
                    foreach (TraceStep s in SessionSim.Run(D, seed, pol).Trace) used.Add(s.RenderingId);

            List<string> never = new List<string>();
            int total = 0;
            foreach (Data.StageDef st in D.Stages)
                foreach (Data.RoundDef r in st.rounds)
                    foreach (Data.RenderDef g in r.renderings)
                    {
                        total++;
                        if (!used.Contains(g.id)) never.Add(g.id);
                    }
            TestContext.Out.WriteLine("후보 역어 " + total + "개 중 어느 계획에도 뽑히지 않은 것 " + never.Count
                                      + (never.Count > 0 ? ": " + string.Join(", ", never) : ""));
            Assert.That(never.Count * 100 / total, Is.LessThanOrEqualTo(20),
                "후보 역어의 " + (never.Count * 100 / total) + "%가 어디에서도 뽑히지 않는다 — 화면만 채우는 선택지다");
        }
    }
}
