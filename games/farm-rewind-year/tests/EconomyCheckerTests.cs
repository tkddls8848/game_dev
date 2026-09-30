using NUnit.Framework;

namespace FarmRewindYear.Tests
{
    /// 검사기 3 — EconomyChecker. 무한 증식 고리가 없는가.
    ///
    /// 이 PoC에서 "100년"은 100번의 되감기다(config.simAttempts). 파밍에서 발산은 해가 갈수록
    /// 커지는 고리에서 나오는데, 여기서는 해가 늘지 않고 **되감기만 늘어난다** —
    /// 그래서 되감기 사다리가 끝나는지가 이 장르의 발산 검사에 해당한다.
    [TestFixture]
    public class EconomyCheckerTests
    {
        /// 데이터만으로 계산하는 한 해 수입 상한. 시뮬레이션과 무관하게 성립해야 한다.
        static int TheoreticalMaxYearIncome()
        {
            var d = Support.Data;
            int minGrow = int.MaxValue, maxGross = 0;
            foreach (var c in d.Crops.crops)
            {
                var shop = d.Shop(c.id);
                int gross = c.yieldUnits * shop.sellUnit;
                if (c.growDays < minGrow) minGrow = c.growDays;
                if (gross > maxGross) maxGross = gross;
            }
            int plots = d.Plots.plots.Length;
            int cyclesPerYear = d.DaysPerYear / minGrow + 1;
            int priceCeilPercent = 100 + d.Economy.priceBandPercent;
            return plots * cyclesPerYear * maxGross * priceCeilPercent / 100;
        }

        [Test]
        public void 어떤_시도의_수입도_데이터로_계산한_상한을_넘지_않는다()
        {
            int bound = TheoreticalMaxYearIncome();
            Support.Line($"데이터로 계산한 한 해 수입 상한: {bound}");
            foreach (var r in new[] { Support.Learner, Support.Stubborn, Support.Omniscient, Support.Ladder })
                foreach (var a in r.Attempts)
                    Assert.That(a.IncomeCoin, Is.LessThanOrEqualTo(bound),
                        $"{a.Attempt}번째 시도 수입 {a.IncomeCoin}이 이론 상한 {bound}을 넘었다 — 증식 고리가 있다.");
        }

        [Test]
        public void 자산이_선언한_한계값_안에_있다()
        {
            var lim = Support.Data.Economy.limits;
            foreach (var r in new[] { Support.Learner, Support.Stubborn, Support.Omniscient, Support.Ladder })
            {
                Assert.That(r.PeakAssetCoin, Is.LessThanOrEqualTo(lim.assetCeilingCoin),
                    $"현금 최고 {r.PeakAssetCoin}이 한계 {lim.assetCeilingCoin}을 넘었다.");
                foreach (var a in r.Attempts)
                    Assert.That(a.IncomeCoin, Is.LessThanOrEqualTo(lim.maxAttemptIncomeCoin),
                        $"{a.Attempt}번째 시도 수입 {a.IncomeCoin}이 선언한 한계를 넘었다.");
            }
        }

        [Test]
        public void 되감기_사다리가_끝난다_벽이_있다()
        {
            // 완전한 지식으로도 할당량을 못 넘기는 되감기 횟수가 있어야 한다.
            // 없으면 되감기는 규칙이 아니라 무한 재시도 버튼이다.
            var d = Support.Data;
            int quota = d.Economy.quotaCoin;
            int wall = -1;
            foreach (var a in Support.Ladder.Attempts)
                if (!a.QuotaMet) { wall = a.RewindsBefore; break; }

            Support.Line($"할당량 {quota} · 벽은 되감기 {wall}회에서 처음 나타난다");
            Assert.That(wall, Is.GreaterThanOrEqualTo(0), "완전한 지식으로 스무 번을 되감아도 계속 넘긴다 — 벽이 없다.");
            Assert.That(wall, Is.LessThanOrEqualTo(d.Economy.limits.wallWithinRewinds));

            var last = Support.Ladder.Attempts[Support.Ladder.Attempts.Count - 1];
            Assert.That(last.QuotaMet, Is.False, "사다리의 마지막에서도 할당량을 넘긴다 — 흔적이 압력이 아니다.");
        }

        [Test]
        public void 흔적이_쌓이면_완전한_지식으로도_덜_번다()
        {
            // 엄밀한 단조 감소는 성립하지 않는다(가격·날씨가 겹쳐 오르내림이 있다).
            // 그래서 앞 3분의 1과 뒤 3분의 1의 평균을 견준다.
            var ladder = Support.Ladder.Attempts;
            int third = ladder.Count / 3;
            Assert.That(third, Is.GreaterThan(0));
            int headSum = 0, tailSum = 0;
            for (int i = 0; i < third; i++) headSum += ladder[i].IncomeCoin;
            for (int i = ladder.Count - third; i < ladder.Count; i++) tailSum += ladder[i].IncomeCoin;
            int head = headSum / third, tail = tailSum / third;
            Support.Line($"사다리 앞 3분의 1 평균 수입 {head} → 뒤 3분의 1 {tail}");
            Assert.That(tail, Is.LessThan(head), "되감기를 쌓아도 수입이 줄지 않는다 — 흔적이 듣지 않는다.");
        }

        [Test]
        public void 어떤_정책도_되감아서_첫_해보다_더_남기지_못한다()
        {
            // "되감기가 공짜면 규칙이 아니라 편의가 된다"의 기계 판정.
            // 세 정책 모두에서 성립해야 한다 — 하나라도 되감아서 이득을 보면 그 정책이 지배 전략이 된다.
            foreach (var r in Support.AllLadders)
            {
                var first = r.Attempts[0];
                for (int i = 1; i < r.Attempts.Count; i++)
                {
                    var a = r.Attempts[i];
                    Assert.That(a.FinalMoneyCoin, Is.LessThanOrEqualTo(first.FinalMoneyCoin),
                        $"{a.Attempt}번째 시도가 첫 해보다 더 남겼다({a.FinalMoneyCoin} > {first.FinalMoneyCoin}) — 되감기가 그냥 이득이다.");
                }
            }
        }

        [Test]
        public void 되감기를_쌓으면_결국_할당량을_못_넘긴다()
        {
            var quota = Support.Data.Economy.quotaCoin;
            foreach (var r in Support.AllLadders)
            {
                var last = r.Attempts[r.Attempts.Count - 1];
                Assert.That(last.FinalMoneyCoin, Is.LessThan(quota),
                    $"한도까지 되감아도 연말 현금 {last.FinalMoneyCoin}이 할당량 {quota}을 넘는다 — 흔적이 압력이 아니다.");
            }
        }

        [Test]
        public void 씨앗값이_기대_수익을_넘는_작물이_없다()
        {
            var d = Support.Data;
            foreach (var c in d.Crops.crops)
            {
                var shop = d.Shop(c.id);
                int worstGross = c.yieldUnits * shop.sellUnit * (100 - d.Economy.priceBandPercent) / 100;
                Assert.That(worstGross, Is.GreaterThan(shop.buySeed),
                    $"{c.nameKo}: 최악의 가격에서도 씨앗값을 못 건진다 — 선택지처럼 보이는데 선택지가 아니다.");
            }
        }

        [Test]
        public void 확장은_한_해_안에_값을_뽑을_수_있는_기한_안에서만_된다()
        {
            // 되감으면 산 칸이 사라지므로, 늦게 살 수 있으면 플레이어가 반드시 손해를 본다.
            var ex = Support.Data.Plots.expansion;
            Assert.That(ex.deadlineDayInYear, Is.GreaterThan(0), "확장에 기한이 없다.");
            Assert.That(ex.deadlineDayInYear, Is.LessThanOrEqualTo(Support.Data.Seasons.daysPerSeason),
                "봄이 지난 뒤에도 땅을 뗄 수 있으면 한 해 안에 값을 못 뽑는 칸을 사게 된다.");
            foreach (var a in Support.Ladder.Attempts)
                Assert.That(a.PlotsBought, Is.LessThanOrEqualTo(ex.maxExtraPlots));
        }
    }
}
