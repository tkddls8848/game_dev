using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// **NoDominantStrategy — 한 가지 수를 반복하는 것이 최적이 아닌가.**
    ///
    /// 두 층에서 본다.
    ///   (1) 정책: 여덟 가지 수 중 세 건물 전부에서 최선인 것이 있는가
    ///   (2) 선택: 건물마다 최선 선택이 같은 자리(가장 시끄러운 칸 · 침묵한 칸 · 101호…)인가
    /// </summary>
    [TestFixture]
    public class NoDominantStrategyTests
    {
        [Test]
        public void 세_건물_전부에서_최선인_정책이_없다()
        {
            StringBuilder sb = new StringBuilder();
            List<Policy> all = Policy.All();
            List<string> buildings = TestWorld.BuildingIds();
            Dictionary<string, int> bestCount = new Dictionary<string, int>();
            foreach (Policy p in all) bestCount[p.Name] = 0;

            foreach (string bid in buildings)
            {
                int best = int.MaxValue;
                Dictionary<string, int> sums = new Dictionary<string, int>();
                foreach (Policy p in all)
                {
                    int sum = 0;
                    for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                        sum += Policy.Run(TestWorld.Night(bid, seed), p).Total;
                    sums[p.Name] = sum;
                    if (sum < best) best = sum;
                }
                sb.AppendLine("== " + bid);
                foreach (Policy p in all)
                {
                    bool isBest = sums[p.Name] == best;
                    if (isBest) bestCount[p.Name]++;
                    sb.AppendLine(string.Format("   {0,-18} 평균 {1,4}{2}",
                        p.Name, sums[p.Name] / TestWorld.SeedSweep, isBest ? "  ← 최선" : ""));
                }
            }

            foreach (Policy p in all)
                Assert.That(bestCount[p.Name], Is.LessThan(buildings.Count),
                    "「" + p.Name + "」 가 세 건물 전부에서 최선이다 — 지배 전략이다");
            sb.AppendLine("어느 정책도 세 건물 전부에서 최선이 아니다");
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 한_문만_되풀이하는_것은_어느_건물에서도_최선이_아니다()
        {
            StringBuilder sb = new StringBuilder();
            Policy repeat = new RepeatOneDoor();
            Policy follow = new FollowTheNeighbours();
            foreach (string bid in TestWorld.BuildingIds())
            {
                int r = 0, f = 0, exhaustive = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    r += Policy.Run(TestWorld.Night(bid, seed), repeat).Total;
                    f += Policy.Run(TestWorld.Night(bid, seed), follow).Total;
                    exhaustive += TestWorld.Matrix(bid, seed).BestByTotal().Total;
                }
                Assert.That(r, Is.GreaterThan(f),
                    bid + ": 한 문만 되풀이하는 것이 이웃의 말을 따라가는 것보다 낫다");
                Assert.That(f, Is.GreaterThan(exhaustive),
                    bid + ": 가장 좋은 정책이 전수 탐색의 최선과 같다 — 사람이 더 잘할 여지가 없다");
                sb.AppendLine(bid + ": 한 문 되풀이 " + (r / TestWorld.SeedSweep)
                              + " · 이웃의 말 " + (f / TestWorld.SeedSweep)
                              + " · 전수 탐색 최선 " + (exhaustive / TestWorld.SeedSweep));
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 건물마다_최선_선택이_다르다()
        {
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            HashSet<string> bestIds = new HashSet<string>();
            foreach (string bid in TestWorld.BuildingIds())
            {
                Dictionary<string, int> wins = new Dictionary<string, int>();
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceRow best = TestWorld.Matrix(bid, seed).BestByTotal();
                    if (!wins.ContainsKey(best.Id)) wins[best.Id] = 0;
                    wins[best.Id]++;
                }
                string top = null; int topN = 0;
                foreach (KeyValuePair<string, int> kv in wins)
                    if (kv.Value > topN) { topN = kv.Value; top = kv.Key; }
                bestIds.Add(top);
                HouseholdDef h = d.Household(top);
                sb.AppendLine(bid + ": 최선 " + d.Unit(h.unitId).unitNo + " " + h.name
                              + " (복도 소음 " + h.ambientLoudnessPercent + ", 침묵 " + h.silenceKind
                              + ") " + topN + "/" + TestWorld.SeedSweep + "회");
            }
            Assert.That(bestIds.Count, Is.GreaterThanOrEqualTo(d.Balance.checkers.minDistinctBestChoices),
                "건물마다 최선 선택이 같은 자리다 — 「가장 시끄러운 칸을 돕는다」 같은 한 수가 통한다는 뜻이다");
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 최선_선택이_소음_순위로_설명되지_않는다()
        {
            // 복도에서 보이는 것(소리의 크기)만으로 최선을 찾을 수 있으면 엿듣기가 필요 없다.
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            List<int> ranks = new List<int>();
            foreach (string bid in TestWorld.BuildingIds())
            {
                ChoiceRow best = TestWorld.Matrix(bid, 0).BestByTotal();
                List<HouseholdDef> hs = new List<HouseholdDef>(d.HouseholdsOf(bid));
                hs.Sort(delegate (HouseholdDef a, HouseholdDef b)
                { return b.ambientLoudnessPercent - a.ambientLoudnessPercent; });
                int rank = 0;
                for (int i = 0; i < hs.Count; i++) if (hs[i].id == best.Id) rank = i + 1;
                ranks.Add(rank);
                sb.AppendLine(bid + ": 최선 선택의 소음 순위 " + rank + "/" + hs.Count);
            }
            HashSet<int> distinct = new HashSet<int>(ranks);
            Assert.That(distinct.Count, Is.GreaterThanOrEqualTo(3),
                "최선 선택의 소음 순위가 건물마다 같다 — 복도에서 보이는 것만으로 답이 나온다");
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 어떤_정책도_전수_탐색의_최선을_모든_회차에서_맞히지_못한다()
        {
            StringBuilder sb = new StringBuilder();
            List<Policy> all = Policy.All();
            int cases = TestWorld.BuildingIds().Count * TestWorld.SeedSweep;
            foreach (Policy p in all)
            {
                int hits = 0;
                foreach (string bid in TestWorld.BuildingIds())
                    for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                    {
                        Outcome o = Policy.Run(TestWorld.Night(bid, seed), p);
                        if (o.Total <= TestWorld.Matrix(bid, seed).BestByTotal().Total) hits++;
                    }
                sb.AppendLine(string.Format("   {0,-18} 최선 일치 {1}/{2}", p.Name, hits, cases));
                Assert.That(hits, Is.LessThan(cases),
                    "「" + p.Name + "」 가 모든 회차에서 최선을 맞힌다 — 그 수만 반복하면 된다는 뜻이다");
            }
            TestContext.Out.WriteLine(sb.ToString());
        }
    }
}
