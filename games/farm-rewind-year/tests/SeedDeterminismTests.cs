using NUnit.Framework;
using FarmRewindYear.Data;
using FarmRewindYear.Sim;

namespace FarmRewindYear.Tests
{
    /// 검사기 1 — SeedDeterminism. 같은 씨드면 같은 결과.
    ///
    /// 이 PoC에서는 이것이 재현성 문제가 아니라 **규칙**이다. 되감은 해의 날씨와 시세가
    /// 같지 않으면 플레이어의 기억이 값을 갖지 못하고, 되감기는 다시 하기가 아니라 다른 해가 된다.
    [TestFixture]
    public class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드로_두_번_돌리면_지문이_같다()
        {
            var a = Simulation.Run(DataLoader.Load(), PolicyKind.Learner);
            var b = Simulation.Run(DataLoader.Load(), PolicyKind.Learner);
            Assert.That(b.Fingerprint(), Is.EqualTo(a.Fingerprint()),
                "같은 씨드인데 결과가 달라졌다. 어딘가에 씨드 없는 난수나 딕셔너리 순회가 들어왔다.");
        }

        [Test]
        public void 한_해의_날씨는_달력_하나뿐이다()
        {
            var d = DataLoader.Load();
            var one = new WeatherCalendar(d);
            var two = new WeatherCalendar(d);
            Assert.That(one.Days, Is.EqualTo(d.DaysPerYear), "달력이 한 해보다 길다 — 되감아도 같은 해여야 한다.");
            for (int day = 0; day < one.Days; day++)
                Assert.That(two.IdAt(day), Is.EqualTo(one.IdAt(day)), $"{day}일의 날씨가 달라졌다.");
        }

        [Test]
        public void 되감은_해의_날씨와_시세가_첫_해와_같다()
        {
            // 이 PoC의 규칙 그 자체. 시뮬레이션이 실제로 같은 달력·같은 가격표를 다시 쓰는지 본다.
            var d = Support.Data;
            var sim = new Simulation(d, PolicyKind.Learner, true);
            var weatherFirst = new string[d.DaysPerYear];
            var priceFirst = new int[d.DaysPerYear];
            for (int day = 0; day < d.DaysPerYear; day++)
            {
                weatherFirst[day] = sim.Weather.IdAt(day);
                priceFirst[day] = sim.Prices.SellUnit("turnip", day);
            }
            sim.Execute();   // 스무 번 되감는다
            for (int day = 0; day < d.DaysPerYear; day++)
            {
                Assert.That(sim.Weather.IdAt(day), Is.EqualTo(weatherFirst[day]),
                    $"{day}일 날씨가 되감기 뒤에 달라졌다 — 기억이 값을 잃는다.");
                Assert.That(sim.Prices.SellUnit("turnip", day), Is.EqualTo(priceFirst[day]),
                    $"{day}일 시세가 되감기 뒤에 달라졌다.");
            }
        }

        [Test]
        public void 씨드를_바꾸면_해가_달라진다()
        {
            var d = DataLoader.Load();
            var before = new WeatherCalendar(d);
            d.Config.seed += 1;
            var after = new WeatherCalendar(d);
            int same = 0;
            for (int day = 0; day < before.Days; day++) if (before.IdAt(day) == after.IdAt(day)) same++;
            Assert.That(same, Is.LessThan(before.Days), "씨드를 바꿨는데 달력이 그대로다.");
        }

        [Test]
        public void 세_정책이_서로_다른_결과를_낸다()
        {
            Assert.That(Support.Omniscient.Fingerprint(), Is.Not.EqualTo(Support.Stubborn.Fingerprint()));
            Assert.That(Support.Ladder.Fingerprint(), Is.Not.EqualTo(Support.Omniscient.Fingerprint()));
        }

        [Test]
        public void 달의_위상이_날짜마다_하나로_정해진다()
        {
            var d = Support.Data;
            for (int day = 0; day < d.DaysPerYear; day++)
            {
                var m = d.MoonOfDay(day);
                Assert.That(m, Is.Not.Null, $"{day}일에 달의 위상이 없다.");
                Assert.That(d.MoonOfDay(day).id, Is.EqualTo(m.id));
            }
            // 주기가 계절 길이와 같으므로 한 계절에 정확히 한 번 찬다 — 연감 연출이 이 사실을 쓴다.
            Assert.That(d.Seasons.moonCycleDays, Is.EqualTo(d.Seasons.daysPerSeason));
        }
    }
}
