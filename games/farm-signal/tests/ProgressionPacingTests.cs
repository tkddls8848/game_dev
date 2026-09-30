using NUnit.Framework;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 검사기 6 — ProgressionPacing. 주요 단계 도달 일수가 설계 구간 안인가.
    /// 너무 빠르면 3시간에 끝나고, 너무 느리면 2시간에 그만둔다(PLAN_FARMING §5).
    ///
    /// 이 PoC의 "주요 단계"는 밭이 커지는 것이 아니라 **남이 반응하는 것**이다:
    /// 첫 시세 경고 · 첫 시세 붕괴 · 첫 도둑 · 첫 방문자. 단위는 계절(3년 = 12계절).
    [TestFixture]
    public class ProgressionPacingTests
    {
        [Test]
        public void 첫_시세_경고가_구간_안에_온다()
        {
            var lim = Support.Data.Economy.limits;
            var r = Support.Run(PolicyKind.Blind, true);
            Support.Line($"Blind 첫 시세 경고: {r.FirstWarnSeason}계절 " +
                         $"(구간 {lim.firstPriceWarnSeasonMin}~{lim.firstPriceWarnSeasonMax})");
            Assert.That(r.FirstWarnSeason,
                Is.InRange(lim.firstPriceWarnSeasonMin, lim.firstPriceWarnSeasonMax),
                "시세판을 안 보는 정책이 경고를 받는 시점이 구간 밖이다. " +
                "너무 늦으면 3년 안에 훅을 못 느끼고, 너무 이르면 첫 봄부터 벌을 받는다.");
        }

        [Test]
        public void 첫_시세_붕괴가_구간_안에_온다()
        {
            var lim = Support.Data.Economy.limits;
            var r = Support.Run(PolicyKind.MonoSeasonBest, true);
            Support.Line($"MonoSeasonBest 첫 붕괴: {r.FirstCollapseSeason}계절 " +
                         $"(구간 {lim.firstCollapseSeasonMin}~{lim.firstCollapseSeasonMax})");
            Assert.That(r.FirstCollapseSeason,
                Is.InRange(lim.firstCollapseSeasonMin, lim.firstCollapseSeasonMax),
                "단작이 시세를 무너뜨리는 시점이 구간 밖이다.");
        }

        [Test]
        public void 첫_도둑이_구간_안에_온다()
        {
            var lim = Support.Data.Economy.limits;
            var r = Support.Run(PolicyKind.Blind, true);
            Support.Line($"Blind 첫 도둑: {r.FirstTheftSeason}계절 · 3년에 {r.TheftCount}회 {r.TheftPlotsLost}칸 " +
                         $"(구간 {lim.firstTheftSeasonMin}~{lim.firstTheftSeasonMax})");
            Assert.That(r.FirstTheftSeason,
                Is.InRange(lim.firstTheftSeasonMin, lim.firstTheftSeasonMax),
                "도둑이 처음 드는 시점이 구간 밖이다.");
        }

        [Test]
        public void 첫_방문자가_구간_안에_온다()
        {
            var lim = Support.Data.Economy.limits;
            var r = Support.Run(PolicyKind.Renown, true);
            Support.Line($"Renown 첫 방문자: {r.FirstVisitorSeason}계절 · 최대 {r.MaxVisitors}명 " +
                         $"웃돈 {r.MaxPremiumPercent}% (구간 {lim.firstVisitorSeasonMin}~{lim.firstVisitorSeasonMax})");
            Assert.That(r.FirstVisitorSeason,
                Is.InRange(lim.firstVisitorSeasonMin, lim.firstVisitorSeasonMax),
                "평판을 쓰는 정책에 방문자가 오는 시점이 구간 밖이다.");
        }

        [Test]
        public void 신호를_쓰는_정책은_삼년_안에_시세_경고를_받지_않는다()
        {
            // 페이싱의 반대쪽. 벌이 피할 수 없으면 판단이 아니라 세금이다.
            foreach (var kind in new[] { PolicyKind.SignalAware, PolicyKind.Screen, PolicyKind.Rotate })
            {
                var r = Support.Run(kind, true);
                Support.Line($"{kind,-14} 첫 경고 {r.FirstWarnSeason} · 첫 붕괴 {r.FirstCollapseSeason} " +
                             $"· 최저지수 {r.MinPriceIndexSeen}");
                Assert.That(r.FirstCollapseSeason, Is.EqualTo(-1),
                    $"{kind}: 신호를 쓰는 정책인데 3년 안에 시세가 붕괴했다 — 벌을 피할 방법이 없다.");
            }
        }

        [Test]
        public void 첫_해에_망하지_않는다()
        {
            foreach (var kind in Support.MainPolicies)
                foreach (var on in new[] { true, false })
                {
                    var r = Support.Run(kind, on);
                    Assert.That(r.BankruptYear, Is.EqualTo(-1),
                        $"{kind}(신호 {(on ? "켬" : "끔")}): {r.BankruptYear}년에 파산했다 — " +
                        "3년짜리 PoC에서 어느 정책도 3년을 못 버티면 수치를 견줄 수 없다.");
                }
        }

        [Test]
        public void 첫_해가_배우는_해다()
        {
            // 첫 해는 벌을 받기 전이어야 한다. 관측 → 반응은 계절 마감에 걸리므로
            // 1년차에도 결과가 나타나지만, 첫 해 수입이 이후보다 크게 낮으면 안 된다.
            var r = Support.Run(PolicyKind.Reactive, true);
            int first = r.YearRecords[0].IncomeCoin;
            int last = r.YearRecords[r.YearRecords.Count - 1].IncomeCoin;
            Support.Line($"Reactive 1년차 수입 {first} · 마지막 해 {last}");
            Assert.That(first, Is.GreaterThan(0), "첫 해에 수입이 없다.");
            Assert.That(first * 100 / last, Is.GreaterThan(40),
                "첫 해 수입이 마지막 해의 40% 도 안 된다 — 씨앗 살 돈이 모자란 것이다.");
        }
    }
}
