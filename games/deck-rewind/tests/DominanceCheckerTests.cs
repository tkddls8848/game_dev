using System.Text;
using NUnit.Framework;

namespace DeckRewind.Tests
{
    /// <summary>
    /// 검사기 3 — `DominanceChecker` (PLAN_DECKBUILDER §5).
    /// <b>한 카드만 넣은 덱의 승률이 한계값을 넘으면 실패.</b>
    /// 넘으면 그 카드 하나가 게임을 끝내고, 덱빌딩이 사라진다.
    /// </summary>
    [TestFixture]
    public class DominanceCheckerTests
    {
        [Test]
        public void 한_카드만_넣은_덱이_한계값을_넘지_않는다()
        {
            var bal = Fix.Data.Balance;
            int deckSize = Fix.Data.Run.startingDeck.Length;
            var report = new StringBuilder();
            string worst = null;
            int worstRate = -1;

            foreach (var card in Fix.Data.Cards)
            {
                int rate = RunEngine.WinRatePct(
                    Fix.Data, _ => Fix.Greedy(), bal.seedBase + 6000, bal.dominanceRuns,
                    Fix.MonoDeck(card.id, deckSize));
                report.Append("  ").Append(card.id).Append(" 단일덱 승률 ").Append(rate).Append("%\n");
                if (rate > worstRate) { worstRate = rate; worst = card.id; }
            }

            TestContext.WriteLine("DominanceChecker (한계 " + bal.dominanceMaxWinRatePct + "%)\n" + report);
            Assert.That(worstRate, Is.LessThanOrEqualTo(bal.dominanceMaxWinRatePct),
                worst + " 하나만 넣은 덱이 " + worstRate + "% 를 이긴다 — 지배 전략이다");
        }

        [Test]
        public void 시작_덱이_어떤_단일덱보다_낫다()
        {
            // 섞은 덱이 한 장짜리 덱보다 못하면 덱빌딩이 손해가 된다.
            var bal = Fix.Data.Balance;
            int deckSize = Fix.Data.Run.startingDeck.Length;
            int mixed = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 6000, bal.dominanceRuns);

            int bestMono = -1;
            string bestId = null;
            foreach (var card in Fix.Data.Cards)
            {
                int rate = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 6000,
                                                bal.dominanceRuns, Fix.MonoDeck(card.id, deckSize));
                if (rate > bestMono) { bestMono = rate; bestId = card.id; }
            }

            TestContext.WriteLine("섞은 시작 덱 " + mixed + "% · 가장 센 단일덱 " + bestId + " " + bestMono + "%");
            Assert.That(mixed, Is.GreaterThan(bestMono),
                "섞은 덱(" + mixed + "%)이 " + bestId + " 단일덱(" + bestMono + "%)보다 못하다");
        }
    }
}
