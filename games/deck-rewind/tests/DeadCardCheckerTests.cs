using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace DeckRewind.Tests
{
    /// <summary>
    /// 검사기 4 — `DeadCardChecker` (PLAN_DECKBUILDER §5).
    /// <b>어떤 회차에서도 채택되지 않는 카드가 없는가.</b>
    /// 있으면 그 카드를 주는 보상이 벌이 된다 — 뽑으면 손해인 카드가 보상 화면에 뜬다.
    ///
    /// 분모를 "덱에 있었던 횟수"로 두면 안 된다. 뽑히지 않아서 안 쓴 것과
    /// 뽑혔는데 안 쓴 것이 섞여 아무것도 판정하지 못한다.
    /// 그래서 분모는 "손에 있고 기력으로 낼 수 있었던 턴 수"다 (`AdoptionStats`).
    /// </summary>
    [TestFixture]
    public class DeadCardCheckerTests
    {
        const int Runs = 90;

        [Test]
        public void 모든_카드가_한_번_이상_채택된다()
        {
            var stats = Measure();
            var report = new StringBuilder();
            int min = int.MaxValue;
            string minId = null;

            foreach (var c in Fix.Data.Cards)
            {
                int plays = stats.Plays(c.id);
                int chances = stats.Chances(c.id);
                int pct = stats.AdoptionPct(c.id);
                report.Append("  ").Append(c.id).Append("  기회 ").Append(chances)
                      .Append(" · 채택 ").Append(plays).Append(" (").Append(pct).Append("%)\n");
                Assert.That(chances, Is.GreaterThan(0),
                    c.id + " 은 손에 온 적이 없다 — 표본이 부족하거나 덱에 들어가지 않는다");
                if (pct < min) { min = pct; minId = c.id; }
            }

            TestContext.WriteLine("DeadCardChecker (한계 " + Fix.Data.Balance.deadCardMinAdoptionPct
                                  + "%, 회차 " + Runs + "판)\n" + report);
            Assert.That(min, Is.GreaterThanOrEqualTo(Fix.Data.Balance.deadCardMinAdoptionPct),
                minId + " 의 채택률이 " + min + "% 다 — 준최적 플레이가 거의 쓰지 않는 카드다");
        }

        [Test]
        public void 유독_못한_보상이_없다()
        {
            // 채택률이 0이 아니어도 "받으면 손해"인 카드가 있을 수 있다.
            //
            // 비교 방법에 한 번 속았다. 처음에는 "시작 덱(10장)"과 "시작 덱 + 그 카드(11장)"를
            // 비교했는데, 그러면 <b>어떤 카드든</b> 10~13%p 떨어진다. 카드가 나빠서가 아니라
            // 열 장 덱에 한 장을 넣는 것 자체가 덱 구성의 10%를 바꾸고 각 카드가 뽑힐 확률을
            // 낮추기 때문이다. 즉 그 측정은 카드가 아니라 덱 크기를 재고 있었다.
            //
            // 그래서 <b>전부 같은 크기(11장)로 맞춰 놓고 서로 비교한다.</b> 덱 크기 효과가
            // 양쪽에서 상쇄되고, 남는 것은 "이 카드가 보통 카드보다 얼마나 못한가"다.
            var bal = Fix.Data.Balance;
            var ids = new List<string>();
            var rates = new List<int>();

            foreach (var c in Fix.Data.Cards)
            {
                var deck = new List<string>(Fix.Data.Run.startingDeck) { c.id };
                ids.Add(c.id);
                rates.Add(RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 7000,
                                               bal.dominanceRuns, deck));
            }

            int sum = 0;
            foreach (var r in rates) sum += r;
            int mean = sum / rates.Count;

            var report = new StringBuilder("보상 하나를 받았을 때의 승률 (덱 크기 "
                                           + (Fix.Data.Run.startingDeck.Length + 1)
                                           + "장으로 모두 같다, 평균 " + mean + "%)\n");
            int worst = int.MaxValue;
            string worstId = null;
            for (int i = 0; i < ids.Count; i++)
            {
                report.Append("  +1 ").Append(ids[i]).Append(" -> ").Append(rates[i]).Append("%\n");
                if (rates[i] < worst) { worst = rates[i]; worstId = ids[i]; }
            }
            TestContext.WriteLine(report.ToString());

            Assert.That(worst, Is.GreaterThanOrEqualTo(mean - bal.rewardPenaltyMaxPct),
                worstId + " 을 받으면 " + worst + "% 로, 보통 보상(" + mean + "%)보다 "
                + (mean - worst) + "%p 못하다 — 이 보상은 벌이다");
        }

        static AdoptionStats Measure()
        {
            // 모든 카드를 두 장씩 넣은 덱으로 돌려 기회를 고르게 준다.
            var agent = new GreedyAgent(Fix.Data);
            var deck = Fix.FullPoolDeck(2);
            for (int s = 0; s < Runs; s++)
                RunEngine.Play(Fix.Data, agent, Fix.Data.Balance.seedBase + 8000 + s, deck);
            return agent.Stats;
        }
    }
}
