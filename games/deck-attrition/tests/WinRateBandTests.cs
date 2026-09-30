using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 몬테카를로 승률이 설계 구간 안인가 (`PLAN_GENRES.md` §2.5 — "이 계획의 핵").
    ///
    /// <b>구간의 목표값 자체는 확인하지 못한 것이다</b> (§10). 기존 성공작의 실제 승률 분포를 모른다.
    /// `data/balance.json` 의 구간은 "옳은 값"이 아니라 <b>이 PoC가 지금 어디 있는지를 고정하는 값</b>이다 —
    /// 카드를 하나 고치면 어디로 움직였는지가 숫자로 보인다.
    /// </summary>
    [TestFixture]
    public class WinRateBandTests
    {
        [Test]
        public void 무작위_회차_승률이_구간_안이다()
        {
            var b = Fix.Data.Balance;
            int pct = RunEngine.WinRatePct(Fix.Data, s => Fix.Random(s), b.seedBase, b.monteCarloRuns);
            TestContext.WriteLine("무작위 회차 승률 " + pct + "%");
            Assert.That(pct, Is.InRange(b.randomWinRatePctMin, b.randomWinRatePctMax));
        }

        [Test]
        public void 무작위_첫_전투_승률이_구간_안이다()
        {
            // 회차 전체를 무작위로 이기는 일은 거의 없어 0%에 붙는다. 그래서 첫 전투 하나를 따로 잰다 —
            // 첫 전투는 아무렇게나 내도 대개 이겨야 하고(아래 경계), 그렇다고 아무 일도 없어서는 안 된다(위 경계).
            var b = Fix.Data.Balance;
            var run = Fix.Data.Run;
            int wins = 0, n = 120;
            for (int i = 0; i < n; i++)
            {
                var tc = new TypeCase(Fix.Data);
                var battle = new Battle(Fix.Data, tc, Fix.Data.Enemy(run.battles[0]), run.startingDeck,
                                        run.playerMaxHp, run.playerMaxHp, b.seedBase + i,
                                        run.handSize, run.energyPerTurn, run.maxRoundsPerBattle);
                battle.RunToEnd(Fix.Random(b.seedBase + i));
                if (battle.Outcome == BattleOutcome.PlayerWon) wins++;
            }
            int pct = wins * 100 / n;
            TestContext.WriteLine("무작위 첫 전투 승률 " + pct + "%");
            Assert.That(pct, Is.InRange(b.randomBattleWinRatePctMin, b.randomBattleWinRatePctMax));
        }

        [Test]
        public void 준최적_회차_승률이_구간_안이다()
        {
            var b = Fix.Data.Balance;
            int pct = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, b.monteCarloRuns);
            TestContext.WriteLine("준최적(적절히 쓴다) 회차 승률 " + pct + "%");
            Assert.That(pct, Is.InRange(b.measuredWinRatePctMin, b.measuredWinRatePctMax));
        }

        [Test]
        public void 준최적이_무작위보다_확실히_낫다()
        {
            var b = Fix.Data.Balance;
            int random = RunEngine.WinRatePct(Fix.Data, s => Fix.Random(s), b.seedBase, b.monteCarloRuns);
            int measured = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, b.monteCarloRuns);
            TestContext.WriteLine("무작위 " + random + "% · 준최적 " + measured + "%");
            Assert.That(measured - random, Is.GreaterThanOrEqualTo(30),
                "실력이 승률로 나타나지 않는다 — 결정이 결과를 못 바꾸고 있다");
        }
    }
}
