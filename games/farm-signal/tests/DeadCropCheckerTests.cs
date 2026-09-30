using System.Collections.Generic;
using NUnit.Framework;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// 검사기 5 — DeadCropChecker. 심을 이유가 없는 작물이 없는가.
    /// 선택지처럼 보이는데 선택지가 아닌 작물은 화면만 어지럽힌다(PLAN_FARMING §5).
    ///
    /// 이 PoC에서 "심을 이유"는 돈만이 아니다. 작물 여덟 종은 **네 가지 이유** 중 하나 이상을 가져야 한다:
    /// 돈 · 가림(뒤를 숨긴다) · 평판(노출을 이득으로 바꾼다) · 낮은 모방(시세가 버틴다).
    /// 겨울호밀은 돈으로는 최악인데 가림 55 · 토질 회복 · 모방 8로 살아 있다.
    [TestFixture]
    public class DeadCropCheckerTests
    {
        [Test]
        public void 모든_작물이_어느_정책에선가_심기고_걷힌다()
        {
            var d = Support.Data;
            int need = d.Economy.limits.deadCropMinPlantings;
            var planted = new Dictionary<string, int>();
            var harvested = new Dictionary<string, int>();
            var by = new Dictionary<string, string>();

            foreach (var kind in Support.MainPolicies)
                foreach (var on in new[] { true, false })
                {
                    var r = Support.Run(kind, on);
                    foreach (var c in d.Crops.crops)
                    {
                        planted.TryGetValue(c.id, out var p);
                        planted[c.id] = p + r.Plant(c.id);
                        harvested.TryGetValue(c.id, out var h);
                        harvested[c.id] = h + r.Harvest(c.id);
                        if (r.Plant(c.id) > 0 && !by.ContainsKey(c.id)) by[c.id] = kind.ToString();
                    }
                }

            foreach (var c in d.Crops.crops)
            {
                Support.Line($"{c.nameKo,-8} 심음 {planted[c.id],5} 걷음 {harvested[c.id],5}  처음 심은 정책 {(by.ContainsKey(c.id) ? by[c.id] : "없음")}");
                Assert.That(planted[c.id], Is.GreaterThanOrEqualTo(need),
                    $"{c.nameKo}: 어떤 정책도 심지 않았다 — 선택지가 아니다.");
                Assert.That(harvested[c.id], Is.GreaterThanOrEqualTo(need),
                    $"{c.nameKo}: 심기지만 한 번도 걷히지 않는다 — 계절 안에 못 익는다.");
            }
        }

        [Test]
        public void 모든_작물이_심을_이유를_하나_이상_갖는다()
        {
            // 이유 네 가지 중 하나라도 1등급이면 살아 있다고 본다.
            var d = Support.Data;
            int bestMargin = 0;
            foreach (var c in d.Crops.crops)
            {
                var shop = d.Shop(c.id);
                int m = (c.yieldUnits * shop.sellUnit - shop.buySeed) * 100 / c.growDays;
                if (m > bestMargin) bestMargin = m;
            }

            foreach (var c in d.Crops.crops)
            {
                var shop = d.Shop(c.id);
                int margin = (c.yieldUnits * shop.sellUnit - shop.buySeed) * 100 / c.growDays;
                bool money = margin * 100 / bestMargin >= 40;      // 최고 마진의 40% 이상
                bool screen = c.screenHeight >= 40;
                bool renown = c.renownGain >= 10;
                bool quiet = c.copyAppeal <= 20;                   // 남이 따라 심지 않는다 = 시세가 버틴다
                bool soil = c.soilDrain < 0;                       // 땅을 살린다

                var why = new List<string>();
                if (money) why.Add("돈");
                if (screen) why.Add("가림");
                if (renown) why.Add("평판");
                if (quiet) why.Add("시세가 버틴다");
                if (soil) why.Add("땅을 살린다");

                Support.Line($"{c.nameKo,-8} 마진 {margin,5} (최고의 {margin * 100 / bestMargin,3}%) · 이유: {string.Join(" · ", why)}");
                Assert.That(why.Count, Is.GreaterThan(0),
                    $"{c.nameKo}: 심을 이유가 하나도 없다 — 돈도 안 되고 가리지도 못하고 평판도 없고 시세도 안 버틴다.");
            }
        }

        [Test]
        public void 값나가는_작물과_안전한_작물이_갈려_있다()
        {
            // 이 훅이 성립하려면 "값나가지만 위험한 것"과 "싸지만 안전한 것"이 실제로 갈려야 한다.
            var d = Support.Data;
            int risky = 0, safe = 0;
            foreach (var c in d.Crops.crops)
            {
                var shop = d.Shop(c.id);
                int margin = (c.yieldUnits * shop.sellUnit - shop.buySeed) * 100 / c.growDays;
                bool hot = c.copyAppeal >= 60 || c.theftAppeal >= 30;
                if (hot) risky++;
                if (!hot && margin > 0) safe++;
                Support.Line($"{c.nameKo,-8} 마진 {margin,5} · 모방 {c.copyAppeal,3} · 도둑 {c.theftAppeal,3} · {(hot ? "위험" : "안전")}");
            }
            Assert.That(risky, Is.GreaterThanOrEqualTo(2), "값나가고 위험한 작물이 둘도 안 된다 — 고민할 것이 없다.");
            Assert.That(safe, Is.GreaterThanOrEqualTo(3), "싸고 안전한 작물이 셋도 안 된다 — 도망갈 곳이 없다.");
        }

        [Test]
        public void 위험한_작물이_실제로_더_번다()
        {
            // 위험한 작물이 돈도 안 되면 아무도 안 심고, 그러면 훅 자체가 죽는다.
            var d = Support.Data;
            int hotMargin = 0, coolMargin = 0, hotN = 0, coolN = 0;
            foreach (var c in d.Crops.crops)
            {
                var shop = d.Shop(c.id);
                int margin = (c.yieldUnits * shop.sellUnit - shop.buySeed) * 100 / c.growDays;
                if (c.copyAppeal >= 60 || c.theftAppeal >= 30) { hotMargin += margin; hotN++; }
                else { coolMargin += margin; coolN++; }
            }
            int hot = hotMargin / hotN, cool = coolMargin / coolN;
            Support.Line($"위험한 작물 평균 마진 {hot} · 안전한 작물 평균 {cool} · 배수 {hot * 100 / cool}%");
            Assert.That(hot, Is.GreaterThan(cool * 150 / 100),
                "위험한 작물이 안전한 작물보다 1.5배도 못 번다 — 위험을 감수할 이유가 없다.");
        }

        [Test]
        public void 뒤_칸에서도_심을_것이_남는다()
        {
            // 노출이 낮은 뒤 칸은 토질이 나쁘다. 그 칸에 심을 것이 없으면 '숨기기'가 죽은 선택이 된다.
            var d = Support.Data;
            foreach (var p in d.Plots.plots)
            {
                if (p.y != d.Plots.gridHeight - 1) continue;
                foreach (var s in d.Seasons.seasons)
                {
                    int n = 0;
                    foreach (var c in d.Crops.crops)
                        if (d.CropFitsSeason(c, s.id) && p.soil >= c.minSoil) n++;
                    Assert.That(n, Is.GreaterThan(0),
                        $"뒤 칸 {p.id}(토질 {p.soil})에 {s.nameKo}에 심을 것이 없다.");
                }
            }
        }

        [Test]
        public void 뒤_칸에_돈작물을_전부_숨길_수는_없다()
        {
            // 숨길 수 있는 돈작물의 양이 데이터로 묶여 있어야 '가림'이 공짜 해답이 되지 않는다.
            var d = Support.Data;
            foreach (var s in d.Seasons.seasons)
            {
                foreach (var c in d.Crops.crops)
                {
                    if (!d.CropFitsSeason(c, s.id)) continue;
                    if (c.copyAppeal < 60 && c.theftAppeal < 30) continue;   // 숨길 값이 없는 작물
                    int hideable = 0;
                    foreach (var p in d.Plots.plots)
                        if (p.y == d.Plots.gridHeight - 1 && p.soil >= c.minSoil) hideable++;
                    Support.Line($"{s.nameKo} {c.nameKo}: 뒤 칸에 심을 수 있는 칸 {hideable}/{d.Plots.gridWidth}");
                    Assert.That(hideable, Is.LessThan(d.Plots.gridWidth),
                        $"{c.nameKo}를 뒤 칸 전부에 심을 수 있다 — 그러면 가림이 공짜 정답이 된다.");
                }
            }
        }
    }
}
