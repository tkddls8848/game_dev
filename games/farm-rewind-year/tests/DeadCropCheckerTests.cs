using System.Collections.Generic;
using NUnit.Framework;
using FarmRewindYear.Sim;

namespace FarmRewindYear.Tests
{
    /// 검사기 5 — DeadCropChecker. 어떤 상황에서도 심을 이유가 없는 작물이 없는가.
    /// 증상: 선택지처럼 보이는데 선택지가 아니다.
    [TestFixture]
    public class DeadCropCheckerTests
    {
        [Test]
        public void 모든_작물이_시뮬레이션에서_실제로_심긴다()
        {
            var d = Support.Data;
            int min = d.Economy.limits.deadCropMinPlantings;
            var r = Support.Ladder;
            foreach (var c in d.Crops.crops)
            {
                Support.Line($"{c.nameKo,-8} 심기 {r.Plant(c.id),4}회 · 수확 {r.Harvest(c.id),4}회 · 잃음 {r.Lost(c.id),3}회");
                Assert.That(r.Plant(c.id), Is.GreaterThanOrEqualTo(min),
                    $"{c.nameKo}: 사다리를 끝까지 돌려도 한 번도 심지 않았다 — 죽은 작물이다.");
            }
        }

        [Test]
        public void 모든_작물이_실제로_수확까지_간다()
        {
            var r = Support.Ladder;
            foreach (var c in Support.Data.Crops.crops)
                Assert.That(r.Harvest(c.id), Is.GreaterThan(0),
                    $"{c.nameKo}: 심기는 하는데 한 번도 다 자라지 못했다 — 자라는 일수나 계절 창이 안 맞는다.");
        }

        [Test]
        public void 각_작물이_어떤_날_어떤_칸에서는_최선의_선택이_된다()
        {
            var d = Support.Data;
            var sim = new Simulation(d, PolicyKind.Omniscient);
            var probe = sim.Plots[0];
            var won = new HashSet<string>();

            for (int day = 0; day < d.DaysPerYear; day++)
                for (int soil = 0; soil <= 100; soil += 2)
                {
                    probe.Active = true;
                    probe.Soil = soil;
                    probe.CropId = null;
                    var pick = sim.Choose(probe, day, d.SeasonOfDay(day), d.DaysPerYear);
                    if (pick != null) won.Add(pick.id);
                }

            Support.Line("1등이 되는 작물: " + string.Join(", ", won));
            foreach (var c in d.Crops.crops)
                Assert.That(won, Does.Contain(c.id),
                    $"{c.nameKo}: 한 해의 어떤 (날 · 토질)에서도 1등이 되지 못한다 — 지배당하는 작물이다.");
        }

        [Test]
        public void 작물이_토질_사다리를_이룬다()
        {
            // 이 PoC의 흔적이 지나가는 통로. minSoil 이 오르면 판매단가도 올라야
            // "토질을 잃으면 사다리를 한 칸 내려간다"가 성립한다.
            var d = Support.Data;
            var sorted = new List<Data.CropDef>(d.Crops.crops);
            sorted.Sort((a, b) => a.minSoil.CompareTo(b.minSoil));
            int prevGross = -1;
            foreach (var c in sorted)
            {
                int gross = c.yieldUnits * d.Shop(c.id).sellUnit;
                Support.Line($"minSoil {c.minSoil,3} · {c.nameKo,-8} 총수익 {gross,4}");
                Assert.That(gross, Is.GreaterThanOrEqualTo(prevGross),
                    $"{c.nameKo}: 더 좋은 토질을 요구하는데 총수익이 더 낮다 — 사다리가 끊겼다.");
                prevGross = gross;
            }
        }

        [Test]
        public void 모든_작물이_서로_구별되는_자리를_갖는다()
        {
            var d = Support.Data;
            for (int i = 0; i < d.Crops.crops.Length; i++)
                for (int j = i + 1; j < d.Crops.crops.Length; j++)
                {
                    var a = d.Crops.crops[i];
                    var b = d.Crops.crops[j];
                    var sa = d.Shop(a.id);
                    var sb = d.Shop(b.id);
                    bool identical = a.growDays == b.growDays && a.waterPerDay == b.waterPerDay
                                     && a.yieldUnits == b.yieldUnits && a.soilDrain == b.soilDrain
                                     && a.minSoil == b.minSoil
                                     && sa.buySeed == sb.buySeed && sa.sellUnit == sb.sellUnit;
                    Assert.That(identical, Is.False, $"{a.nameKo}와 {b.nameKo}의 수치가 완전히 같다.");
                }
        }

        [Test]
        public void 토질_바닥에서도_심을_수_있는_작물이_하나는_있다()
        {
            // 흔적이 바닥까지 갔을 때 아무것도 못 심으면 그건 흔적이 아니라 게임 종료다.
            var d = Support.Data;
            int floor = d.Plots.rewind.soilFloor;
            int n = 0;
            foreach (var c in d.Crops.crops) if (c.minSoil <= floor) n++;
            Assert.That(n, Is.GreaterThan(0),
                $"토질 바닥 {floor}에서 심을 수 있는 작물이 없다 — 되감기가 화면을 비운다.");
            Support.Line($"토질 바닥 {floor}에서도 심을 수 있는 작물 {n}종");
        }
    }
}
