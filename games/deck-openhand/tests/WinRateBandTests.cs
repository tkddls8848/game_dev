using NUnit.Framework;

namespace DeckOpenhand.Tests
{
    /// <summary>
    /// 검사기 5 — `WinRateBand` (PLAN_DECKBUILDER §5). <b>이 계획의 핵</b>.
    /// 몬테카를로 승률이 설계 구간 안인가. 무작위 플레이와 준최적 플레이를 각각 본다.
    ///
    /// 너무 쉬우면 한 번에 끝나고, 너무 어려우면 두 번에 그만둔다.
    /// 카드를 하나 고칠 때마다 승률이 어디로 움직였는지 숫자로 본다 — 감으로 맞추지 않는다.
    ///
    /// 구간의 목표값 자체는 확인하지 못한 것이다 (PLAN §10). 기존 성공작의 실제 승률 분포를
    /// 모르므로, 여기 적힌 구간은 <b>이 PoC가 지금 어디 있는지를 고정하는 값</b>일 뿐이다.
    /// </summary>
    [TestFixture]
    public class WinRateBandTests
    {
        [Test]
        public void 무작위_플레이의_승률이_설계_구간_안이다()
        {
            var bal = Fix.Data.Balance;
            int rate = RunEngine.WinRatePct(Fix.Data, s => Fix.Random(s), bal.seedBase + 10000, bal.monteCarloRuns);
            TestContext.WriteLine("무작위 플레이 승률 " + rate + "% (" + bal.monteCarloRuns + "판, 구간 "
                                  + bal.randomWinRatePctMin + "~" + bal.randomWinRatePctMax + "%)");
            Assert.That(rate, Is.InRange(bal.randomWinRatePctMin, bal.randomWinRatePctMax));
        }

        [Test]
        public void 첫_전투는_무작위로도_대개_이긴다()
        {
            // 회차 전체(전투 셋)를 무작위로 이기는 일은 거의 없어 승률이 0%에 붙는다.
            // 0%에 붙은 값은 아래쪽으로 아무것도 잡지 못하므로, 아래쪽 경계는 첫 전투 하나로 잰다.
            // 첫 전투는 규칙을 배우는 자리다 — 무작위로도 대개 이겨야 하고(아래 경계),
            // 그렇다고 아무 일도 일어나지 않아서는 안 된다(위 경계).
            var bal = Fix.Data.Balance;
            var run = Fix.Data.Run;
            int wins = 0, n = 200;
            for (int i = 0; i < n; i++)
            {
                int seed = bal.seedBase + 12000 + i;
                var b = Fix.NewBattle(run.battles[0], seed);
                b.RunToEnd(Fix.Random(seed));
                if (b.Outcome == BattleOutcome.PlayerWon) wins++;
            }
            int rate = wins * 100 / n;
            TestContext.WriteLine("무작위 플레이의 첫 전투 승률 " + rate + "% (" + n + "판, 구간 "
                                  + bal.randomBattleWinRatePctMin + "~" + bal.randomBattleWinRatePctMax + "%)");
            Assert.That(rate, Is.InRange(bal.randomBattleWinRatePctMin, bal.randomBattleWinRatePctMax));
        }

        [Test]
        public void 준최적_플레이의_승률이_설계_구간_안이다()
        {
            var bal = Fix.Data.Balance;
            int rate = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 10000, bal.monteCarloRuns);
            TestContext.WriteLine("준최적 플레이 승률 " + rate + "% (" + bal.monteCarloRuns + "판, 구간 "
                                  + bal.greedyWinRatePctMin + "~" + bal.greedyWinRatePctMax + "%)");
            Assert.That(rate, Is.InRange(bal.greedyWinRatePctMin, bal.greedyWinRatePctMax));
        }

        [Test]
        public void 실력이_값을_한다()
        {
            var bal = Fix.Data.Balance;
            int random = RunEngine.WinRatePct(Fix.Data, s => Fix.Random(s), bal.seedBase + 11000, bal.dominanceRuns);
            int greedy = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 11000, bal.dominanceRuns);
            TestContext.WriteLine("무작위 " + random + "% vs 준최적 " + greedy + "%");
            Assert.That(greedy - random, Is.GreaterThanOrEqualTo(25),
                "준최적이 무작위보다 25%p 이상 낫지 않다 — 판단이 결과를 바꾸지 않는 게임이다");
        }

        [Test]
        public void 승률이_씨드_구간을_바꿔도_흔들리지_않는다()
        {
            // 한 씨드 묶음에서만 구간에 드는 것은 우연이다. 다른 묶음에서도 들어야 값이다.
            var bal = Fix.Data.Balance;
            for (int block = 0; block < 3; block++)
            {
                int rate = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(),
                                                bal.seedBase + 20000 + block * 1000, 100);
                TestContext.WriteLine("씨드 묶음 " + block + " 준최적 승률 " + rate + "%");
                Assert.That(rate, Is.InRange(bal.greedyWinRatePctMin, bal.greedyWinRatePctMax),
                    "씨드 묶음 " + block + " 에서 구간을 벗어났다: " + rate + "%");
            }
        }
    }
}
