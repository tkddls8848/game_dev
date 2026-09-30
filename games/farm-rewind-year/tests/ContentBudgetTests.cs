using NUnit.Framework;
using FarmRewindYear.Report;

namespace FarmRewindYear.Tests
{
    /// 검사기 7 — ContentBudget. N일을 채우려면 고유 사건·대사가 몇 개 필요한가.
    ///
    /// PLAN_FARMING §5: "이 장르에서 프로젝트를 죽이는 것은 밸런스가 아니라
    /// 필요한 콘텐츠 양을 늦게 아는 것이다."
    ///
    /// 이 훅의 실제 값이 여기서 나온다 — 플레이 시간이 늘어도 **달력은 한 해뿐**이다.
    [TestFixture]
    public class ContentBudgetTests
    {
        [Test]
        public void 콘텐츠_청구서를_뽑는다()
        {
            var b = ContentBudget.Compute(Support.Data, Support.Ladder);
            Support.Print(b.Report());
            Assert.That(b.BeatIntervalDays, Is.GreaterThan(0), "events.json 에 beatIntervalDays 가 없다.");
            Assert.That(b.LinesPerEvent, Is.GreaterThan(0));
            Assert.That(b.RewindVariantLines, Is.GreaterThan(0),
                "되감기 변주가 0줄이면 같은 해를 다시 살아도 문장이 같다 — 흔적이 안 느껴진다.");
            Assert.That(b.NeededUniqueEvents, Is.GreaterThan(0));
        }

        [Test]
        public void 청구서가_1인이_감당한다고_선언한_선_안에_있다()
        {
            var b = ContentBudget.Compute(Support.Data, Support.Ladder);
            Assert.That(b.NeededUniqueEvents, Is.LessThanOrEqualTo(b.BudgetEventCeiling),
                $"고유 사건 {b.NeededUniqueEvents}개가 필요한데 선은 {b.BudgetEventCeiling}개다. " +
                "콘텐츠를 늘리지 말고 훅을 좁혀라.");
            Assert.That(b.NeededLines, Is.LessThanOrEqualTo(b.BudgetLineCeiling),
                $"고유 대사 {b.NeededLines}줄이 필요한데 선은 {b.BudgetLineCeiling}줄이다.");
        }

        [Test]
        public void 되감기가_콘텐츠를_재사용한다()
        {
            // 이 훅의 값. 같은 플레이 시간을 확장형으로 채우면 몇 배가 드는지 견준다.
            var b = ContentBudget.Compute(Support.Data, Support.Ladder);
            Assert.That(b.LinesIfNoReuse, Is.GreaterThan(b.NeededLines),
                "되감기가 콘텐츠를 아껴 주지 않는다 — 그러면 이 훅의 값이 규칙 하나뿐이다.");
            Support.Line($"플레이 {b.TotalDaysPlayed}일 · 필요 대사 {b.NeededLines}줄 · " +
                         $"확장형이면 {b.LinesIfNoReuse}줄 · 아낀 양 {b.LinesIfNoReuse - b.NeededLines}줄");
        }

        [Test]
        public void 지금_있는_사건이_전부_실제로_발동한다()
        {
            var r = Support.Ladder;
            foreach (var e in Support.Data.Events.events)
                Assert.That(r.Fired(e.id), Is.GreaterThan(0),
                    $"{e.nameKo}({e.id}): 사다리를 끝까지 돌려도 한 번도 걸리지 않았다 — 조건이 닿지 않는다.");
        }

        [Test]
        public void 되감기_단계마다_다른_문장이_걸리는_사건이_있다()
        {
            // 흔적이 수치로만 남고 문장으로 안 남으면 플레이어는 그것을 못 읽는다.
            var d = Support.Data;
            int scarEvents = 0;
            foreach (var e in d.Events.events) if (e.minRewinds > 0) scarEvents++;
            Assert.That(scarEvents, Is.GreaterThanOrEqualTo(2),
                $"되감기 횟수로 걸리는 사건이 {scarEvents}개뿐이다 — 흔적을 읽을 자리가 없다.");
            Support.Line($"되감기 횟수로 걸리는 사건 {scarEvents}개");
        }
    }
}
