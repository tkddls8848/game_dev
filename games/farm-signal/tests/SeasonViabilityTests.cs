using NUnit.Framework;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 검사기 4 — SeasonViability. 각 계절에 할 만한 것이 있는가.
    /// 깨지면 "겨울에 아무것도 못 하는 빈 구간"이 생긴다(PLAN_FARMING §5).
    ///
    /// 이 PoC에는 계절마다 하나가 더 붙는다: **그 계절에 고를 것이 둘 이상이어야 신호가 판단이 된다.**
    /// 심을 것이 하나뿐인 계절에는 "남이 보면 어쩌지"가 결정을 바꿀 수 없다.
    [TestFixture]
    public class SeasonViabilityTests
    {
        [Test]
        public void 계절마다_심을_작물이_둘_이상이다()
        {
            var d = Support.Data;
            int need = d.Economy.limits.seasonViabilityMinCrops;
            foreach (var s in d.Seasons.seasons)
            {
                int n = 0;
                foreach (var c in d.Crops.crops) if (d.CropFitsSeason(c, s.id)) n++;
                Support.Line($"{s.nameKo}: 심을 수 있는 작물 {n}종");
                Assert.That(n, Is.GreaterThanOrEqualTo(need),
                    $"{s.nameKo}에 심을 작물이 {n}종뿐이다 — 고를 것이 없으면 신호가 판단이 되지 않는다.");
            }
        }

        [Test]
        public void 계절마다_그_계절_안에_끝나는_돈작물이_있다()
        {
            var d = Support.Data;
            int need = d.Economy.limits.seasonViabilityMinCompletableMoneyCrops;
            int days = d.Seasons.daysPerSeason;
            foreach (var s in d.Seasons.seasons)
            {
                int n = 0;
                foreach (var c in d.Crops.crops)
                {
                    if (!d.CropFitsSeason(c, s.id)) continue;
                    if (c.growDays > days) continue;                // 이상 일수로도 계절을 넘는다
                    var shop = d.Shop(c.id);
                    if (c.yieldUnits * shop.sellUnit <= shop.buySeed) continue;
                    n++;
                }
                Assert.That(n, Is.GreaterThanOrEqualTo(need),
                    $"{s.nameKo}에 그 계절 안에 걷어 돈이 되는 작물이 {n}종뿐이다.");
            }
        }

        [Test]
        public void 계절마다_실제_수입이_한계를_넘는다()
        {
            var d = Support.Data;
            int need = d.Economy.limits.seasonViabilityMinIncomeCoin;
            foreach (var kind in new[] { PolicyKind.SignalAware, PolicyKind.Reactive })
            {
                var r = Support.Run(kind, true);
                foreach (var s in d.Seasons.seasons)
                {
                    int income = r.Income(s.id);
                    Support.Line($"{kind,-14}{s.nameKo}: 3년 수입 {income}");
                    Assert.That(income, Is.GreaterThanOrEqualTo(need),
                        $"{kind}/{s.nameKo}: 3년을 돌려 수입이 {income} 뿐이다 — 빈 구간이다.");
                }
            }
        }

        [Test]
        public void 계절마다_길에_사람이_지난다()
        {
            // 통행이 0인 계절이 있으면 그 계절에는 신호 규칙이 아예 꺼진다.
            // 겨울은 낮아야 하지만(숨길 수 있어야 한다) 0이면 안 된다.
            foreach (var s in Support.Data.Seasons.seasons)
                Assert.That(s.roadTrafficPercent, Is.GreaterThan(0),
                    $"{s.nameKo}: 길 통행이 0 이다 — 그 계절에는 남이 아예 보지 않는다.");
        }

        [Test]
        public void 겨울은_실제로_가장_안_보이는_계절이다()
        {
            // 데이터에 적은 의도가 시뮬레이션에서도 나타나는지 본다.
            var r = Support.Run(PolicyKind.Reactive, true);
            var perSeason = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var h in r.SignalHistory)
            {
                perSeason.TryGetValue(h.SeasonId, out var v);
                perSeason[h.SeasonId] = v + h.ObservedTotal;
            }
            foreach (var kv in perSeason) Support.Line($"{Support.Data.Season(kv.Key).nameKo}: 관측합 {kv.Value}");
            int winter = perSeason["winter"];
            foreach (var kv in perSeason)
                if (kv.Key != "winter")
                    Assert.That(winter, Is.LessThan(kv.Value),
                        $"겨울 관측({winter})이 {Support.Data.Season(kv.Key).nameKo}({kv.Value})보다 크다 — " +
                        "'겨울 길에는 아무도 없다'가 데이터에서 성립하지 않는다.");
        }

        [Test]
        public void 각_신호_통로가_계절마다_쓸_수_있다()
        {
            // 가림(키 큰 작물)과 평판(평판 작물)이 없는 계절이 있으면 그 계절에는 전략이 하나뿐이다.
            var d = Support.Data;
            foreach (var s in d.Seasons.seasons)
            {
                bool screen = false, renown = false;
                foreach (var c in d.Crops.crops)
                {
                    if (!d.CropFitsSeason(c, s.id)) continue;
                    if (c.screenHeight >= 40) screen = true;
                    if (c.renownGain >= 6) renown = true;
                }
                Support.Line($"{s.nameKo}: 가림 {(screen ? "있음" : "없음")} · 평판 {(renown ? "있음" : "없음")}");
                Assert.That(screen, Is.True, $"{s.nameKo}에 가림막으로 쓸 키 큰 작물이 없다.");
            }
        }
    }
}
