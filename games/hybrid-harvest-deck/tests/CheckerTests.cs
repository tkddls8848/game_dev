// 검사기 = 완료 조건. "구현했다"가 아니라 여기가 초록이면 Phase 를 닫는다.
// 기계가 판정하는 것(고장)만 여기 있다. 두 루프가 '느낌으로' 붙는지는 사람이 본다.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HybridHarvestDeck;
using NUnit.Framework;

namespace HybridHarvestDeck.Tests
{
    [TestFixture]
    public class CheckerTests
    {
        GameData _d;

        [OneTimeSetUp]
        public void Setup()
        {
            _d = GameData.LoadDefault();
        }

        // ── 0. 규율: 순수 C# 경계 · 카드 상점 없음 ────────────────────

        [Test]
        public void src_에_UnityEngine_의존이_없다()
        {
            var srcDir = Path.Combine(DataLocator.Root(), "src");
            var offenders = new List<string>();
            foreach (var f in Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories))
            {
                // 주석에 적힌 금지 문구가 아니라 실제 using 선언만 잡는다
                foreach (var line in File.ReadAllLines(f))
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("using UnityEngine") || trimmed.StartsWith("using UnityEditor"))
                    { offenders.Add(Path.GetFileName(f)); break; }
                }
            }
            Assert.That(offenders, Is.Empty, "헤드리스 PoC 다. UnityEngine 을 끌어들인 파일: " + string.Join(", ", offenders));
        }

        [Test]
        public void cards_json_에_상점_항목이_없다()
        {
            // 카드를 얻는 유일한 길은 밭이다. 상점을 넣는 순간 밭이 장식이 되고 차별점이 사라진다.
            // 주석(`_` 로 시작하는 키)이 아니라 실제 필드 이름과 값을 본다.
            var banned = new[] { "shop", "price", "gold", "buy", "purchase", "vendor", "merchant", "currency",
                                 "상점", "구매", "가격", "화폐" };
            var json = Newtonsoft.Json.Linq.JToken.Parse(
                File.ReadAllText(Path.Combine(DataLocator.Resolve(), "cards.json")));

            foreach (var prop in json.SelectTokens("$..*").OfType<Newtonsoft.Json.Linq.JProperty>())
            {
                if (prop.Name.StartsWith("_")) continue;   // 주석 키
                foreach (var b in banned)
                    Assert.That(prop.Name.ToLowerInvariant().Contains(b), Is.False,
                        $"cards.json 에 상점 관련 필드 '{prop.Name}' 이 있다");
            }

            foreach (var card in _d.Cards)
                foreach (var e in card.effects)
                    foreach (var b in banned)
                        Assert.That(e.type.ToLowerInvariant().Contains(b), Is.False,
                            $"카드 {card.id} 에 상점 관련 효과 '{e.type}' 이 있다");

            // 작물만이 카드의 출처다
            foreach (var card in _d.Cards)
            {
                bool fromField = false;
                foreach (var crop in _d.Crops) if (crop.cardId == card.id) fromField = true;
                Assert.That(fromField, Is.True, $"카드 {card.id} 를 내는 작물이 없다 — 상점 없이 얻을 수 없다");
            }
        }

        [Test]
        public void 모든_수치가_정수다()
        {
            // 데이터에 소수점이 끼면 "9.999 피해"가 생기고 테스트를 쓸 수 없다.
            foreach (var name in new[] { "cards.json", "crops.json", "seasons.json", "plots.json", "enemies.json", "balance.json" })
            {
                var text = File.ReadAllText(Path.Combine(DataLocator.Resolve(), name));
                var json = Newtonsoft.Json.Linq.JToken.Parse(text);
                foreach (var t in json.SelectTokens("$..*"))
                    if (t.Type == Newtonsoft.Json.Linq.JTokenType.Float)
                        Assert.Fail($"{name} 에 부동소수가 있다: {t.Path} = {t}");
            }
        }

        // ── 1. 참조 무결성 ────────────────────────────────────────────

        [Test]
        public void DataValidator_참조_무결성()
        {
            var problems = DataValidator.Validate(_d);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // ── 2. SeedDeterminism — 나머지 전부의 전제 ───────────────────

        [Test]
        public void SeedDeterminism_같은_씨드면_전투_기록이_같다()
        {
            var deck = new FieldSim(_d).RunYear(_d.Balance.economy.seedStipendPerYear, 0, _d.StartPlots,
                                                new BalancedPlantingPolicy()).Deck;
            var sim = new BattleSim(_d);
            var a = new StringBuilder();
            var b = new StringBuilder();
            sim.Run(_d.Enemy(_d.Night.boss), deck, _d.Night.playerHp, new SkilledPlayPolicy(_d), new Rng(4242), a);
            sim.Run(_d.Enemy(_d.Night.boss), deck, _d.Night.playerHp, new SkilledPlayPolicy(_d), new Rng(4242), b);
            Assert.That(b.ToString(), Is.EqualTo(a.ToString()));
            Assert.That(a.Length, Is.GreaterThan(0));
        }

        [Test]
        public void SeedDeterminism_같은_씨드면_한_해_전체가_같다()
        {
            string Run()
            {
                var sb = new StringBuilder();
                var c = new Campaign(_d);
                var yr = c.RunYear(1, _d.Balance.economy.seedStipendPerYear, 0, _d.StartPlots,
                                   new BalancedPlantingPolicy(), new SkilledPlayPolicy(_d), new Rng(_d.Balance.seed), sb);
                sb.Append("deck:").Append(string.Join(",", yr.Field.Deck));
                sb.Append("|carry:").Append(yr.SeedsCarried).Append(',').Append(yr.FertilizerCarried);
                return sb.ToString();
            }
            Assert.That(Run(), Is.EqualTo(Run()));
        }

        [Test]
        public void SeedDeterminism_다른_씨드면_결과가_갈린다()
        {
            // 같은 씨드가 같은 결과인 것만으로는 부족하다 — 씨드가 실제로 무언가를 바꿔야 한다.
            var deck = new FieldSim(_d).RunYear(_d.Balance.economy.seedStipendPerYear, 0, _d.StartPlots,
                                                new BalancedPlantingPolicy()).Deck;
            var sim = new BattleSim(_d);
            var seen = new HashSet<string>();
            for (int s = 0; s < 12; s++)
            {
                var sb = new StringBuilder();
                sim.Run(_d.Enemy(_d.Night.boss), deck, _d.Night.playerHp, new SkilledPlayPolicy(_d), new Rng(s), sb);
                seen.Add(sb.ToString());
            }
            Assert.That(seen.Count, Is.GreaterThan(1), "씨드를 바꿔도 기록이 하나뿐이다 — 난수가 실제로 쓰이지 않는다");
        }

        // ── 3. 연결부: SeasonFeasibility ──────────────────────────────

        [Test]
        public void SeasonFeasibility_각_계절의_작물만으로_그_밤을_넘긴다()
        {
            var rows = SeasonFeasibility.Check(_d);
            Assert.That(rows.Count, Is.EqualTo(_d.PlantingSeasons.Count));

            var lines = new List<string>();
            foreach (var r in rows)
            {
                lines.Add($"{r.SeasonName}({r.SeasonId}) 작물 {r.CropPool.Count}종 " +
                          $"[{string.Join(",", r.CropPool)}] → 최선 '{r.BestStrategy}' " +
                          $"덱 {r.AvgDeckSize}장 승률 {r.WinPct}% (기준 {_d.Balance.seasonFeasibility.minWinPct}%)");
                foreach (var kv in r.StrategyWinPct) lines.Add($"      {kv.Key}: {kv.Value}%");
            }
            TestContext.WriteLine(string.Join("\n", lines));

            foreach (var r in rows)
            {
                Assert.That(r.CropPool.Count, Is.GreaterThan(0), $"{r.SeasonName}: 심을 작물이 하나도 없다");
                Assert.That(r.AvgDeckSize, Is.GreaterThan(0), $"{r.SeasonName}: 밭에서 카드가 나오지 않는다");
                Assert.That(r.Passed, Is.True,
                    Localization.Text("check.season.fail",
                        "{0}: 이 계절에 심을 수 있는 작물만으로는 밤을 넘기지 못한다", r.SeasonName)
                    + $" (승률 {r.WinPct}% < {_d.Balance.seasonFeasibility.minWinPct}%)");
            }
        }

        [Test]
        public void SeasonFeasibility_모든_계절에_심을_작물이_있다()
        {
            foreach (var s in _d.PlantingSeasons)
            {
                int n = 0;
                foreach (var c in _d.Crops) if (c.GrowsIn(s.id)) n++;
                Assert.That(n, Is.GreaterThan(0), $"{s.nameKo} 에 심을 수 있는 작물이 없다 — 빈 계절");
            }
        }

        // ── 4. 연결부: FieldDeckMapping ───────────────────────────────

        [Test]
        public void FieldDeckMapping_모든_카드가_밭에서_나오고_모든_작물이_쓸모_있다()
        {
            var r = FieldDeckMapping.Check(_d);

            var lines = new List<string> { $"1년차 승률 {r.BaselineWinPct}% · {_d.Balance.fieldDeckMapping.years}년 승률 {r.BaselineCampaignWinPct}%" };
            foreach (var kv in r.CardSources)
                lines.Add($"  {kv.Key} ← [{string.Join(",", kv.Value)}] 놓인 비율 {r.CardPlayPct[kv.Key]}%");
            foreach (var kv in r.ExclusionGainPct)
                lines.Add($"  {kv.Key} 를 빼면 승률 {(kv.Value >= 0 ? "+" : "")}{kv.Value}%p");
            TestContext.WriteLine(string.Join("\n", lines));

            Assert.That(r.CardsWithNoCrop, Is.Empty,
                "상점 없이 얻을 수 없는 카드가 있다: " + string.Join(", ", r.CardsWithNoCrop));
            Assert.That(r.CropsWithBrokenCard, Is.Empty,
                "카드를 내지 못하는 작물이 있다: " + string.Join(", ", r.CropsWithBrokenCard));
            Assert.That(r.CardsNeverPlayed, Is.Empty,
                "밭에서 나오지만 아무도 놓지 않는 카드가 있다: " + string.Join(", ", r.CardsNeverPlayed));
            Assert.That(r.CropsBetterRemoved, Is.Empty,
                "빼는 편이 나은 작물이 있다 — 심을 이유가 없다: " + string.Join(", ", r.CropsBetterRemoved));
        }

        // ── 5. EconomyChecker ─────────────────────────────────────────

        [Test]
        public void EconomyChecker_무한_증식_고리가_없다()
        {
            var r = EconomyChecker.Check(_d);
            TestContext.WriteLine($"{r.Years}년 · 최대 씨앗 {r.MaxSeeds} · 최대 비료 {r.MaxFertilizer} · 최대 밭 {r.MaxPlots}");
            Assert.That(r.Problems, Is.Empty, string.Join("\n", r.Problems));
            Assert.That(r.NoMidYearSeedMinting, Is.True);
            Assert.That(r.NoMonotoneDivergence, Is.True);
        }

        // ── 6. RunSolvability ─────────────────────────────────────────

        [Test]
        public void RunSolvability_이길_수_있다()
        {
            var r = RunSolvability.Check(_d);
            TestContext.WriteLine($"승률 {r.WinPct}% (기준 {r.RequiredPct}%)");
            Assert.That(r.UnbeatableEnemies, Is.Empty,
                "혼자서는 이길 수 없는 적이 있다: " + string.Join(", ", r.UnbeatableEnemies));
            Assert.That(r.WinPct, Is.GreaterThanOrEqualTo(r.RequiredPct));
        }

        // ── 7. DominanceChecker ───────────────────────────────────────

        [Test]
        public void DominanceChecker_한_작물만_심어서_이기지_못한다()
        {
            var rows = DominanceChecker.Check(_d);
            var lines = new List<string>();
            foreach (var r in rows)
                lines.Add($"  {r.CropId} 만 → 덱 {r.AvgDeckSize}장 승률 {r.WinPct}% (한계 {_d.Balance.dominance.monoCropMaxWinPct}%)");
            TestContext.WriteLine(string.Join("\n", lines));

            foreach (var r in rows)
                Assert.That(r.Passed, Is.True,
                    $"{r.CropId} 만 심어도 승률 {r.WinPct}% 다 — 지배 전략. 덱빌딩이 사라진다");
        }

        // ── 8. WinRateBand ────────────────────────────────────────────

        [Test]
        public void WinRateBand_승률이_설계_구간_안이다()
        {
            var r = WinRateBand.Measure(_d);
            TestContext.WriteLine($"무작위 {r.RandomPct}% (구간 {r.RandomMin}~{r.RandomMax}) · " +
                                  $"준최적 {r.SkilledPct}% (구간 {r.SkilledMin}~{r.SkilledMax})");
            Assert.That(r.RandomInBand, Is.True,
                $"무작위 플레이 승률 {r.RandomPct}% 가 구간 {r.RandomMin}~{r.RandomMax} 밖이다");
            Assert.That(r.SkilledInBand, Is.True,
                $"준최적 플레이 승률 {r.SkilledPct}% 가 구간 {r.SkilledMin}~{r.SkilledMax} 밖이다");
            Assert.That(r.SkilledPct, Is.GreaterThan(r.RandomPct),
                "잘 두는 것이 이득이 아니다 — 실력 축이 없다");
        }

        // ── 9. 연출 목업이 읽을 표본 ──────────────────────────────────

        [Test]
        public void 연출_목업이_읽을_한_해를_굽는다()
        {
            var path = PresentationSample.Write(_d, _d.Balance.seed);
            Assert.That(File.Exists(path), Is.True);
            var sample = PresentationSample.Build(_d, _d.Balance.seed);
            Assert.That(sample.plantings.Count, Is.GreaterThan(0), "밭에 심은 것이 없으면 목업이 빈다");
            Assert.That(sample.deckSize, Is.GreaterThan(0), "밭에서 덱이 나오지 않으면 목업의 요지가 사라진다");
            TestContext.WriteLine($"{path}: 심기 {sample.plantings.Count}회 · 덱 {sample.deckSize}장 · " +
                                  $"밤 {(sample.nightWon ? "승" : "패")}");
        }

        // ── 10. 한국어 원문 · 영어 덮어쓰기 ───────────────────────────

        [Test]
        public void Localization_영어는_덮어쓰기고_한국어가_원문이다()
        {
            Localization.Current = Language.Korean;
            Assert.That(Localization.Text("check.season.ok", "봄"), Is.EqualTo("봄"));
            Localization.Current = Language.English;
            Assert.That(Localization.Text("report.field", "밭"), Is.EqualTo("Field"));
            Assert.That(Localization.Text("없는.키", "원문 그대로"), Is.EqualTo("원문 그대로"),
                "표에 없는 키는 한국어 원문이 나와야 한다 — 화면이 비면 안 된다");
            Localization.Current = Language.Korean;
        }
    }
}
