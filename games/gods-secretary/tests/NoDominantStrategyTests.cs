using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Secretary.Data;
using Secretary.Sim;

namespace Secretary.Tests
{
    /// <summary>
    /// **NoDominantStrategy — 한 가지 수를 반복하는 것이 최적이 아닌가.**
    ///
    /// 봉랍은 셋이다(허가·기각·보류). 셋 중 하나만 계속 찍는 것이 최선이면 게임이 아니다.
    /// 장부 셋에서 최선 정책이 갈리는지, 알려진 최선이 봉랍을 섞어 쓰는지를 본다.
    /// </summary>
    [TestFixture]
    public class NoDominantStrategyTests
    {
        private static Dictionary<string, int> Sums(string vid)
        {
            Dictionary<string, int> sums = new Dictionary<string, int>();
            foreach (Policy p in Policy.All())
            {
                int sum = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                    sum += TestWorld.RunPolicy(vid, seed, p).Accrued;
                sums[p.Name] = sum;
            }
            return sums;
        }

        [Test]
        public void 장부_셋_전부에서_최선인_정책이_없다()
        {
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            Dictionary<string, int> bestCount = new Dictionary<string, int>();
            HashSet<string> winners = new HashSet<string>();
            foreach (Policy p in Policy.All()) bestCount[p.Name] = 0;

            foreach (string vid in TestWorld.VolumeIds())
            {
                Dictionary<string, int> sums = Sums(vid);
                int best = int.MaxValue, worst = 0;
                string bestName = null;
                foreach (KeyValuePair<string, int> kv in sums)
                {
                    if (kv.Value < best) { best = kv.Value; bestName = kv.Key; }
                    if (kv.Value > worst) worst = kv.Value;
                }
                bestCount[bestName]++; winners.Add(bestName);
                sb.AppendLine("== " + d.Volume(vid).name);
                foreach (Policy p in Policy.All())
                    sb.AppendLine(string.Format("   {0,-16} 평균 {1,5}{2}",
                        p.Name, sums[p.Name] / TestWorld.SeedSweep,
                        sums[p.Name] == best ? "  ← 최선" : ""));
                int spread = (worst - best) / TestWorld.SeedSweep;
                Assert.That(spread, Is.GreaterThanOrEqualTo(d.Balance.checkers.policySpreadMin),
                    vid + ": 최선과 최악의 차이가 한 회차당 " + spread + "밖에 안 된다 — 무엇을 해도 같다는 뜻이다");
                sb.AppendLine("   최선-최악 차이 " + spread);
            }

            foreach (Policy p in Policy.All())
                Assert.That(bestCount[p.Name], Is.LessThan(TestWorld.VolumeIds().Count),
                    "「" + p.Name + "」 가 장부 셋 전부에서 최선이다 — 지배 전략이다");
            Assert.That(winners.Count, Is.GreaterThanOrEqualTo(d.Balance.checkers.minDistinctBestPolicies),
                "장부마다 최선 정책이 같다");
            sb.AppendLine("최선이 된 정책 " + winners.Count + "가지: " + string.Join(" · ", winners));
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 봉랍_하나만_계속_찍는_것은_어느_장부에서도_최선이_아니다()
        {
            StringBuilder sb = new StringBuilder();
            string[] single = { "전부 허가", "전부 기각", "전부 보류" };
            foreach (string vid in TestWorld.VolumeIds())
            {
                Dictionary<string, int> sums = Sums(vid);
                int best = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in sums) if (kv.Value < best) best = kv.Value;
                foreach (string name in single)
                    Assert.That(sums[name], Is.GreaterThan(best),
                        vid + ": 「" + name + "」 가 최선이다 — 봉랍 하나면 충분하다는 뜻이다");
                sb.AppendLine(TestWorld.Data.Volume(vid).name + ": 최선 " + (best / TestWorld.SeedSweep)
                              + " · 전부 허가 " + (sums["전부 허가"] / TestWorld.SeedSweep)
                              + " · 전부 기각 " + (sums["전부 기각"] / TestWorld.SeedSweep)
                              + " · 전부 보류 " + (sums["전부 보류"] / TestWorld.SeedSweep));
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 알려진_최선은_허가와_기각을_둘_다_쓴다()
        {
            // 하나만 쓰면 봉랍 둘이 장식이다.
            GameData d = TestWorld.Data;
            int minG = d.Balance.checkers.minGrantsInBestRun, minD = d.Balance.checkers.minDeniesInBestRun;
            StringBuilder sb = new StringBuilder();
            foreach (string vid in TestWorld.VolumeIds())
            {
                int grants = 0, denies = 0, defers = 0, seedsWithBoth = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    SimState b = TestWorld.Best(vid, seed);
                    grants += b.Grants; denies += b.Denies; defers += b.Defers;
                    if (b.Grants >= minG && b.Denies >= minD) seedsWithBoth++;
                }
                Assert.That(seedsWithBoth, Is.GreaterThan(TestWorld.SeedSweep / 2),
                    vid + ": 허가와 기각을 둘 다 쓰는 씨드가 " + seedsWithBoth + "개뿐이다");
                sb.AppendLine(d.Volume(vid).name + " 알려진 최선 합계: 허가 " + grants
                              + " 기각 " + denies + " 보류 " + defers
                              + " · 둘 다 쓴 씨드 " + seedsWithBoth + "/" + TestWorld.SeedSweep);
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 어떤_정책도_알려진_최선을_따라잡지_못한다()
        {
            // 따라잡는 정책이 있으면 사람이 더 잘할 여지가 없다.
            StringBuilder sb = new StringBuilder();
            foreach (string vid in TestWorld.VolumeIds())
            {
                int best = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++) best += TestWorld.Best(vid, seed).Accrued;
                Dictionary<string, int> sums = Sums(vid);
                string closest = null; int closestValue = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in sums)
                {
                    Assert.That(kv.Value, Is.GreaterThan(best),
                        vid + ": 「" + kv.Key + "」 가 알려진 최선과 같거나 낫다 — 그 수만 반복하면 된다는 뜻이다");
                    if (kv.Value < closestValue) { closestValue = kv.Value; closest = kv.Key; }
                }
                sb.AppendLine(TestWorld.Data.Volume(vid).name + ": 알려진 최선 " + (best / TestWorld.SeedSweep)
                              + " · 가장 가까운 정책 「" + closest + "」 " + (closestValue / TestWorld.SeedSweep)
                              + " (차이 " + ((closestValue - best) / TestWorld.SeedSweep) + ")");
            }
            TestContext.Out.WriteLine(sb.ToString());
        }
    }
}
