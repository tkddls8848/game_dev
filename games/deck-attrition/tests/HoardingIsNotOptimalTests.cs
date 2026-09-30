using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// ★ <b>이 PoC의 핵.</b>
    ///
    /// `docs/PLAN_GENRES.md §2` §3이 "소모되는 카드" 후보에 붙인 위험은 하나다:
    /// <i>"아끼기만 하는 지루한 최적해가 생길 수 있다"</i>.
    ///
    /// 여기서 그 위험을 <b>수치로 잡는다.</b> 세 정책의 승률을 나란히 재서
    /// <b>적절히 쓰는 쪽이 이겨야</b> 통과다. 아끼는 쪽이 이기면 이 규칙은 긴장이 아니라
    /// "강한 카드를 안 쓰는 잡일"이라는 뜻이고, 그러면 데이터를 고쳐 다시 돈다.
    ///
    /// 세 정책은 <b>같은 탐욕 코드</b>를 쓴다. 갈라지는 자리는
    /// <c>TypesetterAgent.AllowsConsumable</c> 하나뿐이다 — 그래야 승률 차이를
    /// "정책의 차이"라고 말할 수 있다.
    /// </summary>
    [TestFixture]
    public class HoardingIsNotOptimalTests
    {
        static RunEngine.PolicyReport Run(AttritionPolicy p)
        {
            var b = Fix.Data.Balance;
            return RunEngine.Measure(Fix.Data, p, b.seedBase, b.monteCarloRuns);
        }

        [Test]
        public void 적절히_쓰는_쪽이_끝까지_아끼는_쪽을_이긴다()
        {
            var measured = Run(AttritionPolicy.Measured);
            var hoard = Run(AttritionPolicy.Hoard);
            var spendNow = Run(AttritionPolicy.SpendNow);
            var bal = Fix.Data.Balance;

            // 세 수치를 전부 출력한다. 통과/실패만 남으면 다음에 고칠 때 어디로 움직였는지 모른다.
            TestContext.WriteLine("씨드 " + bal.seedBase + " 부터 " + bal.monteCarloRuns + "회차");
            foreach (var r in new[] { hoard, measured, spendNow })
                TestContext.WriteLine(string.Format(
                    "  {0,-9} 승률 {1,3}%  활자 {2}회/회차  녹음 {3,3}%  깬 전투 {4}  보스에서 짐 {5,3}%  이긴 판 체력 {6}",
                    r.Policy, r.WinRatePct,
                    r.UsesPerRunX100 / 100 + "." + (r.UsesPerRunX100 % 100).ToString("00"),
                    r.ExhaustedRunPct,
                    r.ClearedX100 / 100 + "." + (r.ClearedX100 % 100).ToString("00"),
                    r.LostAtBossPct, r.AvgHpOnWin));

            Assert.That(measured.WinRatePct - hoard.WinRatePct,
                Is.GreaterThanOrEqualTo(bal.hoardingMarginPct),
                "아끼기만 하는 정책이 적절히 쓰는 정책만큼 좋다 — 계획서 §3이 경고한 지루한 최적해다. "
                + "적절히 " + measured.WinRatePct + "% vs 아낌 " + hoard.WinRatePct + "%");
        }

        [Test]
        public void 적절히_쓰는_쪽이_즉시_쓰는_쪽을_이긴다()
        {
            var measured = Run(AttritionPolicy.Measured);
            var spendNow = Run(AttritionPolicy.SpendNow);
            var bal = Fix.Data.Balance;

            TestContext.WriteLine("적절히 " + measured.WinRatePct + "% vs 즉시 " + spendNow.WinRatePct + "%");

            Assert.That(measured.WinRatePct - spendNow.WinRatePct,
                Is.GreaterThanOrEqualTo(bal.spendNowMarginPct),
                "손에 오는 대로 쓰는 정책이 적절히 쓰는 정책만큼 좋다 — 그러면 '아껴 둘까'가 결정이 아니다. "
                + "적절히 " + measured.WinRatePct + "% vs 즉시 " + spendNow.WinRatePct + "%");
        }

        [Test]
        public void 세_정책이_보스_앞에_다른_것을_들고_선다()
        {
            var measured = Run(AttritionPolicy.Measured);
            var hoard = Run(AttritionPolicy.Hoard);
            var spendNow = Run(AttritionPolicy.SpendNow);

            // 승률이 갈리기 전에 <b>행동</b>이 갈려야 한다. 셋이 같은 것을 들고 보스 앞에 서는데
            // 승률만 다르면 측정하고 있는 것은 정책이 아니라 잡음이다.
            TestContext.WriteLine("보스 앞에 섰을 때 (회차 중 보스까지 간 비율 · 남은 인쇄 횟수 · 체력)");
            foreach (var r in new[] { hoard, measured, spendNow })
                TestContext.WriteLine(string.Format("  {0,-9} 도달 {1,3}%  남은 횟수 {2}  체력 {3}",
                    r.Policy, r.ReachedBossPct,
                    r.UsesLeftAtBossX100 / 100 + "." + (r.UsesLeftAtBossX100 % 100).ToString("00"),
                    r.HpAtBossX100 / 100 + "." + (r.HpAtBossX100 % 100).ToString("00")));

            Assert.That(hoard.UsesLeftAtBossX100, Is.GreaterThan(measured.UsesLeftAtBossX100),
                "아끼는 정책이 보스 앞에 더 많이 남기지 못했다 — 정책 분리가 실패했다");
            Assert.That(spendNow.UsesLeftAtBossX100, Is.LessThan(measured.UsesLeftAtBossX100),
                "즉시 쓰는 정책이 보스 앞에 더 적게 남기지 못했다 — 정책 분리가 실패했다");
            // 아끼는 쪽은 활자 대신 체력으로 낸다. 그 교환이 이 규칙의 전부다.
            Assert.That(hoard.HpAtBossX100, Is.LessThan(measured.HpAtBossX100),
                "아끼는 정책이 체력을 더 잃지 않았다 — 아끼는 데 드는 값이 없다");
            Assert.That(spendNow.HpAtBossX100, Is.GreaterThan(measured.HpAtBossX100),
                "즉시 쓰는 정책이 체력을 더 아끼지 못했다 — 일찍 쓰는 데 얻는 것이 없다");
        }

        [Test]
        public void 끝까지_아끼면_보스를_보지도_못한다()
        {
            // 이 규칙의 값은 승률 표가 아니라 여기에 있다. "아껴 두자"의 대가는
            // 보스에서 힘이 모자란 것이 아니라 <b>보스 앞에 못 서는 것</b>이다.
            var measured = Run(AttritionPolicy.Measured);
            var hoard = Run(AttritionPolicy.Hoard);
            TestContext.WriteLine("보스 도달률 — 아낌 " + hoard.ReachedBossPct
                                  + "% · 적절히 " + measured.ReachedBossPct + "%");
            Assert.That(measured.ReachedBossPct - hoard.ReachedBossPct, Is.GreaterThanOrEqualTo(10),
                "아끼는 쪽도 보스 앞까지는 멀쩡히 간다 — 아끼는 데 드는 값이 너무 싸다");
        }

    }
}
