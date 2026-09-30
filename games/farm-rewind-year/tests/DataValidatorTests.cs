using System.Collections.Generic;
using NUnit.Framework;

namespace FarmRewindYear.Tests
{
    /// 검사기 2 — DataValidator. 참조 무결성: 작물 → 계절 → 상점가.
    /// 깨졌을 때의 증상은 "살 수 없는 씨앗"이다.
    [TestFixture]
    public class DataValidatorTests
    {
        [Test]
        public void 모든_작물이_실재하는_계절만_가리킨다()
        {
            var d = Support.Data;
            var seasonIds = new HashSet<string>();
            foreach (var s in d.Seasons.seasons) seasonIds.Add(s.id);
            foreach (var c in d.Crops.crops)
            {
                Assert.That(c.seasons, Is.Not.Null.And.Not.Empty, $"{c.id}: 심을 계절이 없다.");
                foreach (var sid in c.seasons)
                    Assert.That(seasonIds, Does.Contain(sid), $"{c.id}가 없는 계절 {sid}을 가리킨다.");
            }
        }

        [Test]
        public void 모든_작물에_상점가가_있고_상점가는_전부_실재하는_작물이다()
        {
            var d = Support.Data;
            foreach (var c in d.Crops.crops)
                Assert.That(d.Shop(c.id), Is.Not.Null, $"{c.id}: economy.json 의 shop 에 줄이 없다 — 살 수 없는 씨앗이다.");
            foreach (var row in d.Economy.shop)
                Assert.That(d.Crop(row.cropId), Is.Not.Null, $"shop 의 {row.cropId}: crops.json 에 없다.");
            Assert.That(d.Economy.shop.Length, Is.EqualTo(d.Crops.crops.Length), "두 표의 줄 수가 다르다.");
        }

        [Test]
        public void 계절의_날씨_분포가_실재하는_날씨만_가리키고_합이_0이_아니다()
        {
            var d = Support.Data;
            foreach (var s in d.Seasons.seasons)
            {
                int total = 0;
                foreach (var w in s.weatherWeights)
                {
                    Assert.That(d.Weather(w.weatherId), Is.Not.Null, $"{s.id}이 없는 날씨 {w.weatherId}을 가리킨다.");
                    Assert.That(w.weight, Is.GreaterThanOrEqualTo(0));
                    total += w.weight;
                }
                Assert.That(total, Is.GreaterThan(0), $"{s.id}: 날씨 가중치 합이 0이다.");
            }
        }

        [Test]
        public void 달의_위상표가_주기_안에서_오름차순이고_겹치지_않는다()
        {
            var d = Support.Data;
            int prev = 0;
            var seen = new HashSet<string>();
            foreach (var m in d.Seasons.moonPhases)
            {
                Assert.That(seen.Add(m.id), Is.True, $"위상 id 중복: {m.id}");
                Assert.That(m.dayInCycle, Is.InRange(1, d.Seasons.moonCycleDays), $"{m.id}: 주기 밖의 날.");
                Assert.That(m.dayInCycle, Is.GreaterThan(prev), $"{m.id}: 순서가 어긋났다.");
                prev = m.dayInCycle;
            }
            Assert.That(d.Seasons.moonPhases[0].dayInCycle, Is.EqualTo(1),
                "첫 위상이 1일이 아니면 주기 앞쪽에 위상이 없는 날이 생긴다.");
        }

        [Test]
        public void 격자와_밭_목록이_맞는다()
        {
            var d = Support.Data;
            Assert.That(d.Plots.plots.Length, Is.EqualTo(d.Plots.gridWidth * d.Plots.gridHeight));
            var seen = new HashSet<string>();
            int active = 0;
            foreach (var p in d.Plots.plots)
            {
                Assert.That(seen.Add(p.id), Is.True, $"밭 id 중복: {p.id}");
                Assert.That(p.x, Is.InRange(0, d.Plots.gridWidth - 1));
                Assert.That(p.y, Is.InRange(0, d.Plots.gridHeight - 1));
                Assert.That(p.soil, Is.InRange(0, 100), $"{p.id}: 토질이 0~100 밖이다.");
                Assert.That(p.startActive, Is.InRange(0, 1));
                active += p.startActive;
            }
            Assert.That(active, Is.EqualTo(d.Plots.startActivePlots), "startActivePlots 가 실제 표와 다르다.");
            Assert.That(d.Plots.expansion.maxExtraPlots, Is.EqualTo(d.Plots.plots.Length - active),
                "살 수 있는 칸 수가 남은 칸 수와 다르다.");
        }

        [Test]
        public void 사건의_조건이_실재하는_계절만_가리키고_id가_겹치지_않는다()
        {
            var d = Support.Data;
            var seen = new HashSet<string>();
            foreach (var e in d.Events.events)
            {
                Assert.That(seen.Add(e.id), Is.True, $"사건 id 중복: {e.id}");
                if (!string.IsNullOrEmpty(e.onSeason))
                    Assert.That(d.Season(e.onSeason), Is.Not.Null, $"{e.id}이 없는 계절 {e.onSeason}을 가리킨다.");
                if (e.onDay != -1)
                    Assert.That(e.onDay, Is.InRange(1, d.Seasons.daysPerSeason), $"{e.id}: 계절 밖의 날짜.");
                if (e.minRewinds != -1)
                    Assert.That(e.minRewinds, Is.InRange(0, d.Plots.rewind.maxRewinds),
                        $"{e.id}: 되감기 한도를 넘는 조건 — 영원히 걸리지 않는다.");
                Assert.That(e.textKo, Is.Not.Null.And.Not.Empty, $"{e.id}: 한국어 원문이 없다.");
            }
        }

        [Test]
        public void 값_없는_int는_0이_아니라_음수1로_적혀_있다()
        {
            var d = Support.Data;
            foreach (var e in d.Events.events)
            {
                Assert.That(e.onDay, Is.Not.Zero, $"{e.id}: onDay 0은 '없음'인가 '0일'인가 — -1로 적어야 한다.");
                Assert.That(e.minMoney, Is.Not.Zero, $"{e.id}: minMoney 0은 조건이 아니다 — -1로 적어야 한다.");
            }
            Assert.That(d.Plots.expansion.enabled, Is.EqualTo(1), "이 PoC는 확장이 켜져 있다(되감으면 사라진다).");
        }

        [Test]
        public void 수치가_전부_정수_범위에서_말이_된다()
        {
            var d = Support.Data;
            Assert.That(d.Seasons.daysPerSeason, Is.GreaterThan(0));
            Assert.That(d.Economy.wellWaterPerDay, Is.GreaterThan(0));
            Assert.That(d.Economy.quotaCoin, Is.GreaterThan(d.Economy.startMoney), "할당량이 시작 현금보다 작으면 첫날에 끝난다.");
            Assert.That(d.Economy.priceStepDays, Is.GreaterThan(0));
            Assert.That(d.Economy.priceBandPercent, Is.InRange(0, 99));
            Assert.That(d.Config.foresightGainPerRewind, Is.GreaterThan(0), "되감아도 얻는 것이 없으면 훅이 아니다.");
            Assert.That(d.Plots.rewind.soilLossPerRewind, Is.GreaterThan(0), "흔적이 0이면 되감기가 공짜다.");
            Assert.That(d.Plots.rewind.maxRewinds, Is.GreaterThan(0));
            foreach (var c in d.Crops.crops)
            {
                Assert.That(c.growDays, Is.GreaterThan(0));
                Assert.That(c.waterPerDay, Is.GreaterThan(0));
                Assert.That(c.yieldUnits, Is.GreaterThan(0));
                Assert.That(c.minSoil, Is.InRange(0, 100));
            }
            foreach (var w in d.Seasons.weathers)
            {
                Assert.That(w.growthPercent, Is.GreaterThan(0), $"{w.id}: 성장률 0이면 그 날은 없는 날이다.");
                Assert.That(w.growthLossPoints, Is.GreaterThanOrEqualTo(0));
            }
        }
    }
}
