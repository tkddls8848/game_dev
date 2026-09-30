using System.Collections.Generic;
using NUnit.Framework;

namespace DeckOpenhand.Tests
{
    /// <summary>
    /// 검사기 2 — `RunSolvability` (PLAN_DECKBUILDER §5).
    /// 준최적 플레이로 <b>이길 수 있는가</b>. 이길 수 없는 회차를 내보내면
    /// 플레이어는 게임을 의심하지 않고 자기 실력을 의심한다. 그게 최악이다.
    ///
    /// 여기서 재는 것은 존재다 — "이길 수 있다". 얼마나 자주 이기는지는 `WinRateBand` 가 본다.
    /// </summary>
    [TestFixture]
    public class RunSolvabilityTests
    {
        const int Seeds = 30;

        [Test]
        public void 준최적_플레이가_회차를_이기는_씨드가_있다()
        {
            int wins = 0;
            var firstWin = "";
            for (int s = 0; s < Seeds; s++)
            {
                int seed = Fix.Data.Balance.seedBase + 2000 + s;
                var r = RunEngine.Play(Fix.Data, Fix.Greedy(), seed);
                if (r.Won) { wins++; if (firstWin == "") firstWin = seed.ToString(); }
            }
            Assert.That(wins, Is.GreaterThan(0),
                "씨드 " + Seeds + "개 전부에서 졌다 — 이 회차는 풀 수 없다");
            TestContext.WriteLine("RunSolvability: " + wins + "/" + Seeds + " 승, 첫 승리 씨드 " + firstWin);
        }

        [Test]
        public void 모든_전투가_하나씩_보면_이길_수_있다()
        {
            var order = new List<string>(Fix.Data.Run.battles) { Fix.Data.Run.boss };
            foreach (var enemyId in order)
            {
                bool any = false;
                for (int s = 0; s < Seeds && !any; s++)
                    any = BeatsAlone(enemyId, Fix.Data.Balance.seedBase + 3000 + s, Fix.FullPoolDeck(1));
                Assert.That(any, Is.True, enemyId + " 은 준최적 플레이로도 한 번도 못 이겼다");
            }
        }

        [Test]
        public void 회차의_마지막_전투가_첫_전투보다_어렵다()
        {
            // 난이도가 올라가지 않으면 회차 구성이 의미가 없다.
            // 비교는 시작 덱으로 한다 — 전체 풀 덱은 전투 셋을 다 100%로 만들어 차이를 지운다.
            int firstWins = CountWinsAlone(Fix.Data.Run.battles[0], 4000);
            int bossWins = CountWinsAlone(Fix.Data.Run.boss, 4000);
            TestContext.WriteLine("첫 전투 승 " + firstWins + "/" + Seeds + " · 보스 승 " + bossWins + "/" + Seeds);
            Assert.That(bossWins, Is.LessThan(firstWins), "보스가 첫 전투보다 쉽다");
        }

        [Test]
        public void 아무것도_내지_않으면_반드시_진다()
        {
            // 이길 수 있다는 것만으로는 부족하다. 지는 길도 있어야 게임이다.
            for (int s = 0; s < 5; s++)
            {
                var r = RunEngine.Play(Fix.Data, new PassAgent(), Fix.Data.Balance.seedBase + 5000 + s);
                Assert.That(r.Won, Is.False, "카드를 한 장도 내지 않았는데 이겼다");
            }
        }

        static bool BeatsAlone(string enemyId, int seed, IEnumerable<string> deck)
        {
            var b = Fix.NewBattle(enemyId, seed, deck);
            b.RunToEnd(Fix.Greedy());
            return b.Outcome == BattleOutcome.PlayerWon;
        }

        static int CountWinsAlone(string enemyId, int seedOffset)
        {
            int n = 0;
            for (int s = 0; s < Seeds; s++)
                if (BeatsAlone(enemyId, Fix.Data.Balance.seedBase + seedOffset + s,
                               Fix.Data.Run.startingDeck)) n++;
            return n;
        }

        sealed class PassAgent : IAgent
        {
            public string Name => "pass";
            public void TakePlayerTurn(Battle b) { }
        }
    }
}
