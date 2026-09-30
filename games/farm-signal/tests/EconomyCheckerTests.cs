using NUnit.Framework;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 검사기 3 — EconomyChecker. 100년 시뮬레이션에서 자산이 발산하지 않는가.
    ///
    /// 이 PoC에서 발산의 실제 위험은 땅이 아니라 **신호의 되먹임 고리**다:
    ///   평판 → 방문자 → 판매 웃돈 → 현금 → 씨앗 → 더 많이 심음 → 더 많이 보임 → 평판
    /// 이 고리에 상한이 없으면 3년차에 돈이 무의미해진다. 상한(renown.cap · premiumCapPercent ·
    /// copycat.ceilPercent)이 실제로 잡는지를 100년으로 확인한다.
    [TestFixture]
    public class EconomyCheckerTests
    {
        [Test]
        public void 이론_상한이_선언한_한계값보다_크다()
        {
            // 데이터만으로 계산한 한 해 매출 천장. 선언한 한계값이 이것보다 작아야 검사에 값이 있다.
            var d = Support.Data;
            int plots = d.Plots.plots.Length;
            int daysPerSeason = d.Seasons.daysPerSeason;
            int ceilingTotal = 0;
            foreach (var s in d.Seasons.seasons)
            {
                int best = 0;
                foreach (var c in d.Crops.crops)
                {
                    if (!d.CropFitsSeason(c, s.id)) continue;
                    var shop = d.Shop(c.id);
                    int cycles = daysPerSeason / c.growDays;            // 100% 성장일 때의 최대 회전
                    int gross = cycles * c.yieldUnits * shop.sellUnit
                                * d.Signal.copycat.ceilPercent / 100
                                * (100 + d.Signal.renown.premiumCapPercent) / 100;
                    if (gross > best) best = gross;
                }
                ceilingTotal += best * plots;
            }
            Support.Line($"데이터로 계산한 한 해 매출 천장 {ceilingTotal} · 선언한 한계 {d.Economy.limits.maxYearIncomeCoin}");
            Assert.That(d.Economy.limits.maxYearIncomeCoin, Is.LessThanOrEqualTo(ceilingTotal),
                "선언한 한계값이 이론 천장보다 크다 — 그런 한계값은 아무것도 막지 못한다.");
        }

        [Test]
        public void 어떤_해도_선언한_수입_한계를_넘지_않는다()
        {
            int limit = Support.Data.Economy.limits.maxYearIncomeCoin;
            foreach (var kind in Support.MainPolicies)
            {
                var r = Support.Run(kind, true, Support.EconomyYears);
                foreach (var y in r.YearRecords)
                    Assert.That(y.IncomeCoin, Is.LessThanOrEqualTo(limit),
                        $"{kind}: {y.Year}년 수입 {y.IncomeCoin} 이 한계 {limit} 를 넘었다.");
            }
        }

        [Test]
        public void 백년을_돌려도_수입이_복리로_늘지_않는다()
        {
            // 이 PoC에는 확장이 없으므로 수입은 정상 상태여야 한다.
            // 그것을 주장이 아니라 검사로 만든다: 앞 창과 뒤 창의 평균이 크게 벌어지면 실패.
            var lim = Support.Data.Economy.limits;
            int w = lim.economyWindowYears;
            foreach (var kind in Support.MainPolicies)
            {
                var r = Support.Run(kind, true, Support.EconomyYears);
                Assert.That(r.YearRecords.Count, Is.GreaterThanOrEqualTo(4 * w),
                    "창 두 개를 잡을 만큼 연차가 없다.");
                int early = Average(r, w, 2 * w);                        // 11~30년 (초기 자금 효과를 뺀다)
                int late = Average(r, r.YearRecords.Count - w, r.YearRecords.Count);
                int drift = early == 0 ? 0 : (late - early) * 100 / early;
                Support.Line($"{kind,-16} 앞 창 평균 {early,8} · 뒤 창 평균 {late,8} · 표류 {drift,4}%");
                Assert.That(System.Math.Abs(drift), Is.LessThanOrEqualTo(lim.incomeDriftMaxPercent),
                    $"{kind}: 100년에서 수입이 {drift}% 표류했다. 어딘가에 무한 증식 고리가 있다.");
            }
        }

        [Test]
        public void 백년_자산이_선언한_천장_아래에_머문다()
        {
            int ceiling = Support.Data.Economy.limits.assetCeilingCoin;
            foreach (var kind in Support.MainPolicies)
            {
                var r = Support.Run(kind, true, Support.EconomyYears);
                Support.Line($"{kind,-16} 100년 최종 현금 {r.FinalMoneyCoin,10} · 최고 {r.PeakMoneyCoin,10}");
                Assert.That(r.PeakMoneyCoin, Is.LessThanOrEqualTo(ceiling),
                    $"{kind}: 100년 최고 현금 {r.PeakMoneyCoin} 이 천장 {ceiling} 을 넘었다.");
            }
        }

        [Test]
        public void 신호_되먹임_고리에_상한이_실제로_걸린다()
        {
            var s = Support.Data.Signal;
            foreach (var kind in Support.MainPolicies)
            {
                var r = Support.Run(kind, true, Support.EconomyYears);
                Assert.That(r.MaxRenown, Is.LessThanOrEqualTo(s.renown.cap),
                    $"{kind}: 평판이 상한 {s.renown.cap} 을 넘었다 — 되먹임 고리가 열려 있다.");
                // 방문자 수 자체는 상한을 넘을 수 있다. 막아야 하는 것은 **웃돈**이다 —
                // 웃돈이 열려 있으면 평판 → 현금 → 씨앗 → 평판 고리가 복리가 된다.
                Assert.That(r.MaxPremiumPercent, Is.LessThanOrEqualTo(s.renown.premiumCapPercent),
                    $"{kind}: 방문자 웃돈 {r.MaxPremiumPercent}% 가 상한 {s.renown.premiumCapPercent}% 를 넘었다.");
                foreach (var h in r.SignalHistory)
                    for (int c = 0; c < h.PriceAfter.Length; c++)
                        Assert.That(h.PriceAfter[c],
                            Is.InRange(s.copycat.floorPercent, s.copycat.ceilPercent),
                            $"{kind}: {h.SeasonNumber}계절 시세지수 {h.PriceAfter[c]} 가 구간 밖이다.");
            }
        }

        [Test]
        public void 백년을_돌려도_망하지_않는다()
        {
            // 신호를 쓰는 정책은 100년을 버텨야 한다. 버티지 못하면 고정비가 과한 것이다.
            foreach (var kind in new[] { PolicyKind.SignalAware, PolicyKind.Rotate,
                                         PolicyKind.Screen, PolicyKind.Renown, PolicyKind.Reactive })
            {
                var r = Support.Run(kind, true, Support.EconomyYears);
                Assert.That(r.BankruptYear, Is.EqualTo(-1),
                    $"{kind}: {r.BankruptYear}년에 파산했다.");
            }
        }

        [Test]
        public void 고정비가_실제로_걸린다()
        {
            // 고정비가 없으면 곡선이 무조건 우상향이 된다(farm-erosion 에서 배운 것).
            var d = Support.Data;
            Assert.That(d.Economy.livingCostPerSeason, Is.GreaterThan(0), "계절 살림비가 없다.");
            var r = Support.Run(PolicyKind.SignalAware, true, Support.EconomyYears);
            int expected = d.Economy.livingCostPerSeason * d.SeasonsPerYear * Support.EconomyYears
                         + d.Economy.taxPerPlotPerYear * d.Plots.plots.Length * Support.EconomyYears;
            Assert.That(r.UpkeepPaidCoin, Is.EqualTo(expected),
                "걷힌 고정비가 셈과 다르다 — 어느 계절에 살림비를 빼먹고 있다.");
        }

        static int Average(SimResult r, int fromIndex, int toExclusive)
        {
            int sum = 0, n = 0;
            for (int i = fromIndex; i < toExclusive && i < r.YearRecords.Count; i++)
            {
                sum += r.YearRecords[i].IncomeCoin;
                n++;
            }
            return n == 0 ? 0 : sum / n;
        }
    }
}
