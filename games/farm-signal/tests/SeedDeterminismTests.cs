using NUnit.Framework;
using FarmSignal.Data;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 검사기 1 — SeedDeterminism. 같은 씨드면 같은 결과.
    ///
    /// 이 PoC에서는 특히 **시세 변동이 씨드 고정이어야** 한다. 시세판은 플레이어가 읽고
    /// 판단하는 화면이고, 같은 밭을 같은 날씨에 심었는데 판이 달라지면 판단할 것이 없다.
    /// 이것이 깨지면 SignalMatters 의 두 세계 비교가 전부 무의미해진다.
    [TestFixture]
    public class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드로_두_번_돌리면_지문이_같다()
        {
            foreach (var kind in Support.MainPolicies)
                foreach (var on in new[] { false, true })
                {
                    var a = Simulation.Run(DataLoader.Load(), kind, 3, on);
                    var b = Simulation.Run(DataLoader.Load(), kind, 3, on);
                    Assert.That(b.Fingerprint(), Is.EqualTo(a.Fingerprint()),
                        $"{kind}(신호 {(on ? "켬" : "끔")}): 같은 씨드인데 결과가 달라졌다. " +
                        "어딘가에 씨드 없는 난수나 딕셔너리 순회가 들어왔다.");
                }
        }

        [Test]
        public void 날씨_달력이_씨드마다_고정된다()
        {
            var d = DataLoader.Load();
            var one = new WeatherCalendar(d, 3);
            var two = new WeatherCalendar(d, 3);
            for (int day = 0; day < one.Days; day++)
                Assert.That(two.IdAt(day), Is.EqualTo(one.IdAt(day)), $"{day}일의 날씨가 달라졌다.");
        }

        [Test]
        public void 기준가_잡음이_씨드마다_고정된다()
        {
            var d = DataLoader.Load();
            var a = new PriceBook(d, 3);
            var b = new PriceBook(d, 3);
            foreach (var c in d.Crops.crops)
                for (int day = 0; day < 3 * d.DaysPerYear; day += 7)
                    Assert.That(b.BaseUnit(c.id, day), Is.EqualTo(a.BaseUnit(c.id, day)),
                        $"{c.nameKo} {day}일 기준가가 달라졌다.");
        }

        [Test]
        public void 시세판_전체가_씨드마다_고정된다()
        {
            // 시세지수는 기준가 잡음 x 남의 반응이다. 둘 다 고정이어야 판이 고정된다.
            var a = Support.Run(PolicyKind.SignalAware, true);
            var b = Simulation.Run(DataLoader.Load(), PolicyKind.SignalAware, Support.SimYears, true);
            Assert.That(b.SignalHistory.Count, Is.EqualTo(a.SignalHistory.Count));
            for (int s = 0; s < a.SignalHistory.Count; s++)
                for (int c = 0; c < a.SignalHistory[s].PriceAfter.Length; c++)
                    Assert.That(b.SignalHistory[s].PriceAfter[c],
                        Is.EqualTo(a.SignalHistory[s].PriceAfter[c]),
                        $"{s + 1}계절 {Support.Data.Crops.crops[c].nameKo} 시세지수가 달라졌다.");
        }

        [Test]
        public void 도둑_주사위가_씨드마다_고정된다()
        {
            var a = Support.Run(PolicyKind.Blind, true);
            var b = Simulation.Run(DataLoader.Load(), PolicyKind.Blind, Support.SimYears, true);
            for (int s = 0; s < a.SignalHistory.Count; s++)
                Assert.That(b.SignalHistory[s].TheftRoll, Is.EqualTo(a.SignalHistory[s].TheftRoll),
                    $"{s + 1}계절 도둑 주사위가 달라졌다.");
        }

        [Test]
        public void 씨드를_바꾸면_결과가_달라진다()
        {
            var d = DataLoader.Load();
            var before = Simulation.Run(d, PolicyKind.Blind, 3, true);
            var d2 = DataLoader.Load();
            d2.Config.seed += 1;
            var after = Simulation.Run(d2, PolicyKind.Blind, 3, true);
            Assert.That(after.Fingerprint(), Is.Not.EqualTo(before.Fingerprint()),
                "씨드를 바꿨는데 결과가 그대로다. 씨드가 실제로 쓰이지 않고 있다.");
        }

        [Test]
        public void 정책이_서로_다른_결과를_낸다()
        {
            // 정책이 결과에 영향을 주지 않으면 이 저장소의 검사기 전부가 허수가 된다.
            var seen = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var kind in Support.MainPolicies)
            {
                var fp = Support.Run(kind, true).Fingerprint();
                Assert.That(seen.ContainsKey(fp), Is.False,
                    $"{kind} 와 {(seen.ContainsKey(fp) ? seen[fp] : "")} 가 같은 결과를 냈다.");
                seen[fp] = kind.ToString();
            }
        }

        [Test]
        public void 시세판_보고서도_씨드마다_고정된다()
        {
            // 목업이 읽는 쪽(SignalBoard)도 재현되어야 화면과 테스트가 같은 판을 본다.
            foreach (var plan in Support.Data.Showcase.plans)
            {
                var a = Support.Board(plan.id, true);
                var b = Report.SignalBoard.Compute(DataLoader.Load(), plan, true, Support.SimYears);
                Assert.That(b.GrandRevenue, Is.EqualTo(a.GrandRevenue), $"{plan.nameKo}: 합계 매출이 달라졌다.");
                for (int s = 0; s < a.Seasons.Count; s++)
                {
                    Assert.That(b.Seasons[s].TheftRoll, Is.EqualTo(a.Seasons[s].TheftRoll));
                    for (int c = 0; c < a.Seasons[s].PriceAfter.Length; c++)
                        Assert.That(b.Seasons[s].PriceAfter[c], Is.EqualTo(a.Seasons[s].PriceAfter[c]));
                }
            }
        }
    }
}
