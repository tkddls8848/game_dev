using NUnit.Framework;
using FarmSignal.Report;

namespace FarmSignal.Tests
{
    /// 목업과 C#이 **같은 시세판**을 그리는지 보는 다리.
    ///
    /// presentation/index.html 은 이 계산(SignalMath + PriceBook + WeatherCalendar)을 JS로 이식해
    /// data/showcase.json 의 계획으로 판을 그린다. 화면과 테스트가 갈라지면 목업이 손그림이 되므로,
    /// 여기서 **사람이 눈으로 맞춰 볼 수 있는 표본값**을 찍어 둔다.
    /// (farm-rewind-year 가 .NET System.Random 이식을 이렇게 맞췄다.)
    [TestFixture]
    public class SignalBoardTests
    {
        [Test]
        public void 목업이_맞춰_볼_표본값을_찍는다()
        {
            var d = Support.Data;
            Support.Line("── 목업 대조표 (index.html 이 같은 값을 그려야 한다) ──────────");
            foreach (var plan in d.Showcase.plans)
            {
                var on = Support.Board(plan.id, true);
                var off = Support.Board(plan.id, false);
                Support.Line($"[{plan.id}] 신호 켬 합계매출 {on.GrandRevenue} · 끔 {off.GrandRevenue}");
                for (int i = 0; i < on.Seasons.Count; i++)
                {
                    var s = on.Seasons[i];
                    var line = $"  {s.Year}년{s.SeasonNameKo}  지수";
                    for (int c = 0; c < d.CropCount; c++) line += $" {s.PriceBefore[c],3}";
                    line += $"  관측 {s.ObservedTotal,4}  평판 {s.RenownAfter,3} 방문 {s.Visitors,2}" +
                            $" 웃돈 {s.PremiumPercent,2}%  위험 {s.TheftRiskPer1000,3} 주사위 {s.TheftRoll,3}" +
                            $"  매출 {s.RevenueTotal,6}";
                    if (s.TheftStruck) line += $"  도둑 {s.TheftPlotsHit}칸";
                    Support.Line(line);
                }
            }
            Support.Line("작물 순서: " + string.Join(" ", System.Array.ConvertAll(d.Crops.crops, c => c.nameKo)));
            Assert.Pass();
        }

        [Test]
        public void 계획이_칸에_제대로_펼쳐진다()
        {
            var d = Support.Data;
            var order = SignalBoard.PlotsByExposureDesc(d);
            Support.Line("노출 내림차순: " + string.Join(" ", System.Array.ConvertAll(order.ToArray(),
                p => p.id + "(" + p.exposure + "/" + p.soil + ")")));

            foreach (var plan in d.Showcase.plans)
            {
                var b = SignalBoard.Compute(d, plan, true, 1);
                for (int s = 0; s < b.Seasons.Count; s++)
                {
                    var row = b.Seasons[s];
                    int filled = 0;
                    for (int i = 0; i < row.PlotCrop.Length; i++) if (row.PlotCrop[i] != null) filled++;
                    Assert.That(filled + row.SkippedBySoil, Is.EqualTo(CountNonEmpty(plan, row.SeasonId)),
                        $"{plan.id}/{row.SeasonId}: 계획이 지시한 칸 수와 실제로 채워진 칸 수가 안 맞는다.");
                }
            }
        }

        [Test]
        public void 가림_계획이_실제로_관측치를_낮춘다()
        {
            // 규칙이 데이터에서 성립하는지. 가림 계획은 같은 돈작물을 심고도 덜 보여야 한다.
            var d = Support.Data;
            var mono = Support.Board("mono", true);
            var screen = Support.Board("screen", true);
            int saffron = d.CropIndex("saffron");

            int monoAutumn = 0, screenAutumn = 0;
            foreach (var s in mono.Seasons) if (s.SeasonId == "autumn") monoAutumn += s.ObservedIndex[saffron];
            foreach (var s in screen.Seasons) if (s.SeasonId == "autumn") screenAutumn += s.ObservedIndex[saffron];

            Support.Line($"가을 사프란 관측치 합 — 단작 {monoAutumn} · 가림 {screenAutumn}");
            Assert.That(screenAutumn, Is.LessThan(monoAutumn),
                "키 큰 것을 길가에 세워도 사프란이 단작만큼 보인다 — 가림이 듣지 않는다.");
        }

        [Test]
        public void 단작_계획이_시세를_무너뜨리고_윤작은_지킨다()
        {
            var d = Support.Data;
            int saffron = d.CropIndex("saffron");
            var mono = Support.Board("mono", true);
            var rotate = Support.Board("rotate", true);

            int monoLast = mono.Seasons[mono.Seasons.Count - 1].PriceAfter[saffron];
            int rotateLast = rotate.Seasons[rotate.Seasons.Count - 1].PriceAfter[saffron];
            Support.Line($"3년 뒤 사프란 시세지수 — 단작 {monoLast}% · 윤작 {rotateLast}%");
            Assert.That(monoLast, Is.LessThanOrEqualTo(d.Signal.copycat.collapsePercent),
                "단작 계획인데 사프란 시세가 붕괴선 아래로 안 내려갔다.");
            Assert.That(rotateLast, Is.GreaterThan(monoLast + 20),
                "윤작 계획의 사프란 시세가 단작보다 20%p 이상 높지 않다.");
        }

        [Test]
        public void 평판을_길가에_두면_방문자가_온다()
        {
            // 윤작 계획은 라벤더를 길가에, 가림 계획은 뒤에 둔다. 그 차이가 평판으로 나와야 한다.
            var rotate = Support.Board("rotate", true);
            var screen = Support.Board("screen", true);
            int rotateRenown = rotate.Seasons[rotate.Seasons.Count - 1].RenownAfter;
            int screenRenown = screen.Seasons[screen.Seasons.Count - 1].RenownAfter;
            Support.Line($"3년 뒤 평판 — 윤작(라벤더 길가) {rotateRenown} · 가림(라벤더 뒤) {screenRenown}");
            Assert.That(rotateRenown, Is.GreaterThan(screenRenown),
                "라벤더를 길가에 둔 계획의 평판이 뒤에 둔 계획보다 높지 않다 — " +
                "'보이는 것이 평판이 된다'가 데이터에서 성립하지 않는다.");
        }

        [Test]
        public void 신호를_끈_판은_지수가_전부_100이다()
        {
            foreach (var plan in Support.Data.Showcase.plans)
            {
                var off = Support.Board(plan.id, false);
                foreach (var s in off.Seasons)
                    for (int c = 0; c < s.PriceAfter.Length; c++)
                        Assert.That(s.PriceAfter[c], Is.EqualTo(100),
                            $"{plan.id}: 신호를 끈 판인데 지수가 움직였다.");
            }
        }

        [Test]
        public void 신호를_켠_판과_끈_판의_매출이_다르다()
        {
            foreach (var plan in Support.Data.Showcase.plans)
            {
                var on = Support.Board(plan.id, true);
                var off = Support.Board(plan.id, false);
                Assert.That(on.GrandRevenue, Is.Not.EqualTo(off.GrandRevenue),
                    $"{plan.id}: 목업이 두 세계를 같은 판으로 그리게 된다.");
            }
        }

        static int CountNonEmpty(Data.PlanDef plan, string seasonId)
        {
            foreach (var bs in plan.bySeason)
                if (bs.seasonId == seasonId)
                {
                    int n = 0;
                    foreach (var c in bs.cropOrder) if (!string.IsNullOrEmpty(c)) n++;
                    return n;
                }
            return 0;
        }
    }
}
