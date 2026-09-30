using NUnit.Framework;

namespace FarmRewindYear.Tests
{
    /// 검사기 4 — SeasonViability. 각 계절에 할 만한 것이 있는가(빈 구간이 없는지).
    ///
    /// 한 해가 전부이므로 이 PoC에서는 빈 계절이 곧 게임의 4분의 1이 빈 것이다.
    /// 수치는 사다리 시뮬레이션(21번의 한 해)에서 본다 — 토질 전 구간을 지나므로 표본이 가장 넓다.
    [TestFixture]
    public class SeasonViabilityTests
    {
        [Test]
        public void 모든_계절에_심을_작물이_최소치만큼_있다()
        {
            var d = Support.Data;
            int min = d.Economy.limits.seasonViabilityMinCrops;
            foreach (var s in d.Seasons.seasons)
            {
                int n = 0;
                foreach (var c in d.Crops.crops) if (d.CropFitsSeason(c, s.id)) n++;
                Support.Line($"{s.nameKo}: 심을 수 있는 작물 {n}종");
                Assert.That(n, Is.GreaterThanOrEqualTo(min), $"{s.nameKo}: 심을 수 있는 작물이 {n}종뿐이다 — 빈 구간이다.");
            }
        }

        [Test]
        public void 모든_계절에_그_계절_안에_끝나는_작물이_있다()
        {
            // 계절이 바뀌면 그 계절을 못 견디는 작물은 죽는다. 그래서 '계절 안에 끝나는 것'이
            // 있어야 그 계절이 독립적으로 쓸모가 있다.
            var d = Support.Data;
            int min = d.Economy.limits.seasonViabilityMinCompletableMoneyCrops;
            foreach (var s in d.Seasons.seasons)
            {
                int n = 0;
                foreach (var c in d.Crops.crops)
                {
                    if (!d.CropFitsSeason(c, s.id)) continue;
                    if (c.growDays > d.Seasons.daysPerSeason) continue;
                    n++;
                }
                Assert.That(n, Is.GreaterThanOrEqualTo(min),
                    $"{s.nameKo}: 그 계절 안에 끝나는 작물이 없다 — 그 계절은 기다리는 시간이 된다.");
            }
        }

        [Test]
        public void 시뮬레이션에서_모든_계절에_실제로_수입이_난다()
        {
            var d = Support.Data;
            int min = d.Economy.limits.seasonViabilityMinIncomeCoin;
            var r = Support.Ladder;
            foreach (var s in d.Seasons.seasons)
            {
                Support.Line($"{s.nameKo}: 수입 {r.Income(s.id)} · 지출 {r.Expense(s.id)}");
                Assert.That(r.Income(s.id), Is.GreaterThanOrEqualTo(min),
                    $"{s.nameKo}: 사다리를 끝까지 돌려도 수입이 {r.Income(s.id)}뿐이다.");
            }
        }

        [Test]
        public void 모든_계절에_날씨가_한_가지로_굳지_않는다()
        {
            var d = Support.Data;
            foreach (var s in d.Seasons.seasons)
            {
                int nonZero = 0;
                foreach (var w in s.weatherWeights) if (w.weight > 0) nonZero++;
                Assert.That(nonZero, Is.GreaterThanOrEqualTo(3),
                    $"{s.nameKo}: 걸릴 수 있는 날씨가 {nonZero}가지뿐이다.");
            }
        }

        [Test]
        public void 한_해의_달력에_모든_날씨가_적어도_한_번_들어간다()
        {
            // 한 해가 전부이므로, 달력에 없는 날씨는 게임에 없는 날씨다.
            var d = Support.Data;
            var cal = new FarmRewindYear.Sim.WeatherCalendar(d);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int day = 0; day < cal.Days; day++) seen.Add(cal.IdAt(day));
            foreach (var w in d.Seasons.weathers)
                Assert.That(seen, Does.Contain(w.id),
                    $"{w.nameKo}: 이 씨드의 한 해에 한 번도 안 온다 — 데이터에만 있는 날씨다.");
            Support.Line("한 해에 들어간 날씨: " + string.Join(", ", seen));
        }

        [Test]
        public void 달의_위상이_한_해에_네_번_찬다()
        {
            // 연출이 이 사실 위에 서 있다. 주기가 바뀌면 연감 그림이 거짓이 된다.
            var d = Support.Data;
            int fulls = 0;
            for (int day = 0; day < d.DaysPerYear; day++)
                if (d.MoonOfDay(day).id == "full" && d.MoonOfDay(day == 0 ? 0 : day - 1).id != "full") fulls++;
            Assert.That(fulls, Is.EqualTo(d.Seasons.seasons.Length), $"한 해에 망이 {fulls}번이다.");
        }
    }
}
