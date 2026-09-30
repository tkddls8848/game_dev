using NUnit.Framework;
using FarmSignal.Report;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 검사기 7 — ContentBudget. N일을 채우는 데 필요한 고유 사건·대사 수.
    /// **실측치는 README에 적는다.**
    ///
    /// 이 훅의 콘텐츠 값은 격자에 있다. 전문은 "무슨 일이 났나"가 아니라
    /// "남이 무엇을 보고 어떻게 반응했나"이므로 (반응 틀 x 작물)로 곱해진다.
    [TestFixture]
    public class ContentBudgetTests
    {
        static ContentBudget Budget => ContentBudget.Compute(Support.Data, Support.Aware);

        [Test]
        public void 실측치를_출력한다()
        {
            Support.Print(Budget.Report());
            Assert.Pass();
        }

        [Test]
        public void 전문_격자가_필요한_자리를_덮는다()
        {
            var b = Budget;
            Assert.That(b.CoversBeats, Is.True,
                $"고유 전문 {b.DistinctTelegrams}통으로 자리 {b.NeededBeats}개를 못 덮는다 — " +
                "같은 전문이 두 번 온다. 틀이나 작물을 늘려야 한다.");
        }

        [Test]
        public void 예산_안에_든다()
        {
            var b = Budget;
            Assert.That(b.WrittenLines, Is.LessThanOrEqualTo(b.BudgetLineCeiling),
                $"쓸 줄이 {b.WrittenLines}줄로 사람이 감당한다고 본 선 {b.BudgetLineCeiling}줄을 넘었다.");
            Assert.That(b.NeededBeats, Is.LessThanOrEqualTo(b.BudgetEventCeiling),
                $"채울 자리가 {b.NeededBeats}개로 선 {b.BudgetEventCeiling}개를 넘었다.");
        }

        [Test]
        public void 격자가_실제로_아껴_준다()
        {
            var b = Budget;
            Support.Line($"자리마다 새로 쓴다면 {b.NeededLinesIfAllUnique}줄 · 격자로 쓰면 {b.WrittenLines}줄 " +
                         $"· 아낀 양 {b.SavedPercent}%");
            Assert.That(b.SavedPercent, Is.GreaterThanOrEqualTo(50),
                "격자가 절반도 아껴 주지 못한다 — 그러면 격자를 쓰는 값이 없다.");
        }

        [Test]
        public void 예산이_수치와_어긋나지_않는다()
        {
            var b = Budget;
            var d = Support.Data;
            Assert.That(b.PlayableDays, Is.EqualTo(d.Config.simYears * d.DaysPerYear),
                "플레이 가능 일수가 3년과 다르다 — 어느 정책이 파산해서 일찍 끝났다.");
            Assert.That(b.TemplateCount, Is.EqualTo(d.Events.templates.Length));
            Assert.That(b.CropCount, Is.EqualTo(d.CropCount));
            Assert.That(b.DistinctTelegrams, Is.EqualTo(b.TemplateCount * b.CropCount));
        }

        [Test]
        public void 플레이_길이가_계획서의_구간_안이다()
        {
            // PLAN_FARMING: 작물 3~8종 · 계절 4개 · 1~3년. 늘리지 말 것.
            var d = Support.Data;
            Assert.That(d.CropCount, Is.InRange(3, 8), $"작물이 {d.CropCount}종이다 — 계획서는 3~8종이다.");
            Assert.That(d.SeasonsPerYear, Is.EqualTo(4), "계절이 4개가 아니다.");
            Assert.That(d.Config.simYears, Is.InRange(1, 3), $"{d.Config.simYears}년이다 — 계획서는 1~3년이다.");
        }

        [Test]
        public void 정책이_달라도_플레이_길이가_같다()
        {
            // 콘텐츠 예산은 "게임이 몇 일인가"에 걸려 있다. 정책마다 다르면 예산을 못 정한다.
            int expect = Support.Data.Config.simYears * Support.Data.DaysPerYear;
            foreach (var kind in Support.MainPolicies)
                foreach (var on in new[] { true, false })
                    Assert.That(Support.Run(kind, on).PlayableDays, Is.EqualTo(expect),
                        $"{kind}(신호 {(on ? "켬" : "끔")}): 플레이 일수가 {expect} 가 아니다.");
        }
    }
}
