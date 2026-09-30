using System.Collections.Generic;
using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 지배 전략이 없는가 (`PLAN_GENRES.md` §2.5).
    /// 한 카드만 넣은 덱의 승률이 한계값을 넘으면 그 카드가 게임을 끝낸다 — 덱빌딩이 사라진다.
    ///
    /// 이 PoC에는 축이 하나 더 있다: <b>아낌의 정도</b>. 한 카드가 아니라 한 <i>정책</i>이
    /// 게임을 끝낼 수도 있다. 극단으로 아끼는 쪽도, 극단으로 쓰는 쪽도 최적이 아니어야 한다
    /// (그 판정은 <c>HoardingIsNotOptimalTests</c> 가 한다 — 여기서는 카드를 본다).
    /// </summary>
    [TestFixture]
    public class DominanceCheckerTests
    {
        [Test]
        public void 한_카드를_잔뜩_쌓은_덱이_회차를_끝내지_않는다()
        {
            var b = Fix.Data.Balance;
            var worst = new List<string>();
            int highest = 0; string highestId = null;

            foreach (var c in Fix.Data.Cards)
            {
                int pct = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase,
                                               b.dominanceRuns, Fix.StackedDeck(c.id, 7));
                worst.Add(c.id + " " + pct + "%");
                if (pct > highest) { highest = pct; highestId = c.id; }
            }

            TestContext.WriteLine("기본 활자 + 한 카드 7장 덱의 승률: " + string.Join(" · ", worst));
            int mono = 0;
            foreach (var c in Fix.Data.Cards)
            {
                int pct = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase,
                                               b.dominanceRuns, Fix.MonoDeck(c.id, 12));
                if (pct > mono) mono = pct;
            }
            TestContext.WriteLine("최고 " + highestId + " " + highest + "% (한계 " + b.dominanceMaxWinRatePct
                                  + "%) · 참고: 한 카드만 12장 넣은 덱의 최고는 " + mono + "%");
            Assert.That(highest, Is.LessThanOrEqualTo(b.dominanceMaxWinRatePct),
                highestId + " 를 쌓은 덱이 " + highest + "% 로 이긴다 — 그 카드가 게임이다");
        }

        [Test]
        public void 닳는_활자만_넣은_덱은_회차를_버티지_못한다()
        {
            // 이 PoC에서만 가능한 검사. 강한 카드만 모으면 <b>횟수가 먼저 떨어진다</b>.
            // 여기서 이겨 버리면 "닳는다"가 아무 제약이 아니라는 뜻이다.
            var b = Fix.Data.Balance;
            var deck = new List<string>();
            foreach (var c in Fix.Data.Cards)
                if (c.IsConsumable) for (int i = 0; i < 3; i++) deck.Add(c.id);

            int pct = RunEngine.WinRatePct(Fix.Data, s => Fix.SpendNow(), b.seedBase, b.dominanceRuns, deck);
            TestContext.WriteLine("닳는 활자만 " + deck.Count + "장 넣은 덱 승률 " + pct + "%");
            Assert.That(pct, Is.LessThanOrEqualTo(b.dominanceMaxWinRatePct),
                "강한 활자만 모아도 이긴다 — 마모가 제약이 아니다");
        }

        [Test]
        public void 보상_하나가_회차를_결정하지_않는다()
        {
            // 시작 덱에 카드 한 장을 더한 것만으로 승률이 크게 뛰면 그 카드가 보상이 아니라 정답이다.
            var b = Fix.Data.Balance;
            var baseDeck = new List<string>(Fix.Data.Run.startingDeck);
            int basePct = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, b.dominanceRuns, baseDeck);

            int biggest = 0; string biggestId = null;
            foreach (var id in Fix.Data.Run.rewardPool)
            {
                var deck = new List<string>(baseDeck) { id };
                int pct = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, b.dominanceRuns, deck);
                int delta = pct - basePct;
                if (delta > biggest) { biggest = delta; biggestId = id; }
            }

            TestContext.WriteLine("기본 " + basePct + "% · 보상 한 장이 올리는 최대 폭 "
                                  + biggest + "%p (" + biggestId + ")");
            Assert.That(biggest, Is.LessThanOrEqualTo(30),
                biggestId + " 한 장이 승률을 " + biggest + "%p 올린다 — 보상이 아니라 정답이다");
        }
    }
}
