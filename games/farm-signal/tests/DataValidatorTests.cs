using System.Collections.Generic;
using NUnit.Framework;

namespace FarmSignal.Tests
{
    /// 검사기 2 — DataValidator. 참조 무결성과 스키마 규율.
    /// 뿌리 CLAUDE.md 설계 원칙 3: 배열 + 문자열 ID · 값 없는 int 는 -1.
    [TestFixture]
    public class DataValidatorTests
    {
        [Test]
        public void 작물의_계절이_실제로_있다()
        {
            var d = Support.Data;
            var known = new HashSet<string>();
            foreach (var s in d.Seasons.seasons) known.Add(s.id);
            foreach (var c in d.Crops.crops)
            {
                Assert.That(c.seasons, Is.Not.Null.And.Not.Empty, $"{c.nameKo}: 심는 계절이 없다.");
                foreach (var sid in c.seasons)
                    Assert.That(known.Contains(sid), Is.True, $"{c.nameKo}: 없는 계절 {sid} 을 가리킨다.");
            }
        }

        [Test]
        public void 작물마다_상점가가_있다()
        {
            foreach (var c in Support.Data.Crops.crops)
            {
                var shop = Support.Data.Shop(c.id);
                Assert.That(shop, Is.Not.Null, $"{c.nameKo}: 상점가가 없다 — 살 수 없는 씨앗이다.");
                Assert.That(shop.buySeed, Is.GreaterThan(0), $"{c.nameKo}: 씨앗값이 0 이하다.");
                Assert.That(shop.sellUnit, Is.GreaterThan(0), $"{c.nameKo}: 판매단가가 0 이하다.");
                Assert.That(c.yieldUnits * shop.sellUnit, Is.GreaterThan(shop.buySeed),
                    $"{c.nameKo}: 기준가에서도 씨앗값을 못 뽑는다.");
            }
        }

        [Test]
        public void 상점에_없는_작물이_없다()
        {
            foreach (var row in Support.Data.Economy.shop)
                Assert.That(Support.Data.Crop(row.cropId), Is.Not.Null,
                    $"상점에 없는 작물 {row.cropId} 의 값이 적혀 있다.");
        }

        [Test]
        public void 계절의_날씨가_실제로_있다()
        {
            var d = Support.Data;
            foreach (var s in d.Seasons.seasons)
            {
                int total = 0;
                foreach (var w in s.weatherWeights)
                {
                    Assert.That(d.Weather(w.weatherId), Is.Not.Null,
                        $"{s.nameKo}: 없는 날씨 {w.weatherId} 를 가리킨다.");
                    Assert.That(w.weight, Is.GreaterThanOrEqualTo(0), $"{s.nameKo}/{w.weatherId}: 가중치가 음수다.");
                    total += w.weight;
                }
                Assert.That(total, Is.GreaterThan(0), $"{s.nameKo}: 날씨 가중치 합이 0 이다.");
            }
        }

        [Test]
        public void 계절_순서가_배열_순서와_같다()
        {
            var d = Support.Data;
            for (int i = 0; i < d.Seasons.seasons.Length; i++)
                Assert.That(d.Seasons.seasons[i].order, Is.EqualTo(i),
                    "계절의 order 와 배열 순서가 어긋난다. SeasonOfDay 는 배열 순서를 쓴다.");
        }

        [Test]
        public void 신호_열이_구간_안에_있다()
        {
            // 관측치 산술이 백분율 넷을 곱하므로 구간을 벗어나면 조용히 이상한 수가 나온다.
            foreach (var c in Support.Data.Crops.crops)
            {
                Assert.That(c.visibility, Is.InRange(0, 100), $"{c.nameKo}: visibility 가 0~100 밖이다.");
                Assert.That(c.copyAppeal, Is.InRange(0, 100), $"{c.nameKo}: copyAppeal 이 0~100 밖이다.");
                Assert.That(c.screenHeight, Is.InRange(0, 100), $"{c.nameKo}: screenHeight 가 0~100 밖이다.");
                Assert.That(c.renownGain, Is.InRange(0, 100), $"{c.nameKo}: renownGain 이 0~100 밖이다.");
                Assert.That(c.theftAppeal, Is.InRange(0, 100), $"{c.nameKo}: theftAppeal 이 0~100 밖이다.");
                Assert.That(c.minSoil, Is.InRange(0, 100), $"{c.nameKo}: minSoil 이 0~100 밖이다.");
                Assert.That(c.growDays, Is.GreaterThan(0), $"{c.nameKo}: growDays 가 0 이하다.");
            }
            foreach (var w in Support.Data.Seasons.weathers)
            {
                Assert.That(w.exposurePercent, Is.InRange(0, 200), $"{w.nameKo}: exposurePercent 가 구간 밖이다.");
                Assert.That(w.growthPercent, Is.InRange(0, 200), $"{w.nameKo}: growthPercent 가 구간 밖이다.");
            }
            foreach (var s in Support.Data.Seasons.seasons)
                Assert.That(s.roadTrafficPercent, Is.InRange(1, 200), $"{s.nameKo}: roadTrafficPercent 가 구간 밖이다.");
        }

        [Test]
        public void 칸의_격자와_노출이_말이_된다()
        {
            var d = Support.Data;
            var seen = new HashSet<string>();
            foreach (var p in d.Plots.plots)
            {
                Assert.That(seen.Add(p.id), Is.True, $"칸 이름 {p.id} 가 두 번 나온다.");
                Assert.That(seen.Add(p.x + "," + p.y), Is.True, $"{p.id}: 좌표 ({p.x},{p.y}) 가 두 번 나온다.");
                Assert.That(p.x, Is.InRange(0, d.Plots.gridWidth - 1), $"{p.id}: x 가 격자 밖이다.");
                Assert.That(p.y, Is.InRange(0, d.Plots.gridHeight - 1), $"{p.id}: y 가 격자 밖이다.");
                Assert.That(p.exposure, Is.InRange(0, 100), $"{p.id}: exposure 가 0~100 밖이다.");
                Assert.That(p.soil, Is.InRange(1, 100), $"{p.id}: soil 이 구간 밖이다.");
            }
            Assert.That(d.Plots.plots.Length, Is.EqualTo(d.Plots.gridWidth * d.Plots.gridHeight),
                "격자 칸 수와 plots 배열 길이가 다르다.");
        }

        [Test]
        public void 길가로_갈수록_노출이_높다()
        {
            // y=0 이 길가라는 약속이 데이터에 지켜져 있어야 가림(screen)이 의미를 갖는다.
            var d = Support.Data;
            for (int y = 1; y < d.Plots.gridHeight; y++)
                for (int x = 0; x < d.Plots.gridWidth; x++)
                {
                    var here = Find(x, y);
                    var front = Find(x, y - 1);
                    Assert.That(here.exposure, Is.LessThan(front.exposure),
                        $"{here.id} 가 앞 칸 {front.id} 보다 잘 보인다. 길이 y=0 쪽이라는 약속이 깨졌다.");
                }
        }

        static Data.PlotDef Find(int x, int y)
        {
            foreach (var p in Support.Data.Plots.plots) if (p.x == x && p.y == y) return p;
            Assert.Fail($"({x},{y}) 칸이 없다.");
            return null;
        }

        [Test]
        public void 값_없는_int_는_모두_마이너스1_이다()
        {
            var d = Support.Data;
            Assert.That(d.Plots.expansion.enabled, Is.Zero, "이 PoC에는 확장이 없다.");
            Assert.That(d.Plots.expansion.costBase, Is.EqualTo(-1), "확장이 없으면 비용은 -1 이어야 한다.");
            Assert.That(d.Plots.expansion.costPerPlot, Is.EqualTo(-1));
            Assert.That(d.Plots.reclaimCost, Is.EqualTo(-1));
            foreach (var e in d.Events.events)
            {
                Assert.That(e.onYear, Is.EqualTo(-1).Or.GreaterThan(0), $"{e.id}: onYear 가 0 이다 — -1 을 쓴다.");
                Assert.That(e.onDay, Is.EqualTo(-1).Or.GreaterThan(0), $"{e.id}: onDay 가 0 이다 — -1 을 쓴다.");
                Assert.That(e.minObserved, Is.EqualTo(-1).Or.GreaterThan(0), $"{e.id}: minObserved 가 0 이다.");
                Assert.That(e.minRenownLevel, Is.EqualTo(-1).Or.GreaterThan(0), $"{e.id}: minRenownLevel 이 0 이다.");
                Assert.That(e.once, Is.InRange(0, 1), $"{e.id}: once 는 0 또는 1 이다.");
            }
        }

        [Test]
        public void 전문_틀이_한국어와_영어_줄수가_같다()
        {
            // 뿌리 CLAUDE.md 설계 원칙 6: 한국어가 원문, 영어는 덮어쓰기.
            int perEvent = Support.Data.Events.linesPerEvent;
            foreach (var t in Support.Data.Events.templates)
            {
                Assert.That(t.linesKo, Is.Not.Null.And.Not.Empty, $"{t.id}: 한국어 원문이 없다.");
                Assert.That(t.linesKo.Length, Is.EqualTo(perEvent),
                    $"{t.id}: 줄 수가 linesPerEvent({perEvent}) 와 다르다 — ContentBudget 의 셈이 어긋난다.");
                Assert.That(t.linesEn, Is.Not.Null, $"{t.id}: 영어가 없다.");
                Assert.That(t.linesEn.Length, Is.EqualTo(t.linesKo.Length), $"{t.id}: 두 언어의 줄 수가 다르다.");
                foreach (var l in t.linesKo) Assert.That(l, Is.Not.Empty, $"{t.id}: 빈 줄이 있다.");
                foreach (var l in t.linesEn) Assert.That(l, Is.Not.Empty, $"{t.id}: 빈 영어 줄이 있다.");
            }
        }

        [Test]
        public void 전문_틀의_자리표시자가_두_언어에서_같다()
        {
            // 조각을 이어 붙이지 않고 {0}/{1} 로 쓴다. 한쪽만 자리표시자를 쓰면 번역에서 깨진다.
            foreach (var t in Support.Data.Events.templates)
                for (int i = 0; i < t.linesKo.Length; i++)
                {
                    var ko = Placeholders(t.linesKo[i]);
                    var en = Placeholders(t.linesEn[i]);
                    Assert.That(en, Is.EquivalentTo(ko),
                        $"{t.id} {i + 1}번째 줄: 자리표시자가 한국어 {Join(ko)} 영어 {Join(en)} 로 다르다.");
                }
        }

        [Test]
        public void 전문_틀의_반응_갈래가_실제_통로다()
        {
            var kinds = new HashSet<string> { "copycat", "theft", "renown", "screen", "weather" };
            var used = new HashSet<string>();
            foreach (var t in Support.Data.Events.templates)
            {
                Assert.That(kinds.Contains(t.kind), Is.True, $"{t.id}: 모르는 반응 갈래 {t.kind}.");
                used.Add(t.kind);
            }
            foreach (var k in new[] { "copycat", "theft", "renown" })
                Assert.That(used.Contains(k), Is.True,
                    $"신호 통로 {k} 에 전문 틀이 하나도 없다 — 규칙은 있는데 말이 없다.");
        }

        [Test]
        public void 보여_주기_계획이_실제_작물과_계절을_가리킨다()
        {
            var d = Support.Data;
            int nPlots = d.Plots.plots.Length;
            Assert.That(d.Showcase.plans.Length, Is.GreaterThanOrEqualTo(2),
                "목업이 비교할 계획이 둘도 안 된다.");
            foreach (var plan in d.Showcase.plans)
            {
                Assert.That(plan.bySeason.Length, Is.EqualTo(d.SeasonsPerYear),
                    $"{plan.id}: 계절 수가 {d.SeasonsPerYear} 가 아니다.");
                foreach (var bs in plan.bySeason)
                {
                    Assert.That(d.Season(bs.seasonId), Is.Not.Null, $"{plan.id}: 없는 계절 {bs.seasonId}.");
                    Assert.That(bs.cropOrder.Length, Is.EqualTo(nPlots),
                        $"{plan.id}/{bs.seasonId}: cropOrder 길이가 칸 수({nPlots}) 와 다르다.");
                    foreach (var cid in bs.cropOrder)
                    {
                        if (string.IsNullOrEmpty(cid)) continue;
                        var crop = d.Crop(cid);
                        Assert.That(crop, Is.Not.Null, $"{plan.id}/{bs.seasonId}: 없는 작물 {cid}.");
                        Assert.That(d.CropFitsSeason(crop, bs.seasonId), Is.True,
                            $"{plan.id}/{bs.seasonId}: {crop.nameKo} 는 그 계절에 심을 수 없다.");
                    }
                }
            }
        }

        [Test]
        public void 신호_계수가_말이_된다()
        {
            var s = Support.Data.Signal;
            Assert.That(s.copycat.floorPercent, Is.GreaterThan(0).And.LessThan(100));
            Assert.That(s.copycat.ceilPercent, Is.GreaterThan(100));
            Assert.That(s.copycat.collapsePercent, Is.LessThan(s.copycat.warnPercent),
                "붕괴 기준이 경고 기준보다 높다 — 경고가 붕괴보다 늦게 온다.");
            Assert.That(s.copycat.warnPercent, Is.LessThan(100));
            Assert.That(s.copycat.collapsePercent, Is.GreaterThanOrEqualTo(s.copycat.floorPercent));
            Assert.That(s.theft.riskDivisor, Is.GreaterThan(0));
            Assert.That(s.theft.maxPlotsHit, Is.InRange(1, Support.Data.Plots.plots.Length));
            Assert.That(s.renown.gainDivisor, Is.GreaterThan(0));
            Assert.That(s.renown.perVisitor, Is.GreaterThan(0));
            Assert.That(s.renown.cap / s.renown.perVisitor * s.renown.premiumPerVisitor,
                Is.GreaterThanOrEqualTo(s.renown.premiumCapPercent),
                "평판 상한으로도 웃돈 상한에 못 닿는다 — 웃돈 상한이 죽은 값이다.");
            Assert.That(s.observation.denominator, Is.GreaterThan(0));
        }

        static List<string> Placeholders(string s)
        {
            var found = new List<string>();
            for (int i = 0; i + 2 < s.Length; i++)
                if (s[i] == '{' && s[i + 2] == '}') found.Add(s.Substring(i, 3));
            found.Sort(System.StringComparer.Ordinal);
            return found;
        }

        static string Join(List<string> xs) => xs.Count == 0 ? "(없음)" : string.Join("", xs);
    }
}
