// 검사기 = 완료 조건. "구현했다"가 아니라 여기가 초록이면 Phase 를 닫는다.
// 기계가 판정하는 것(고장)만 여기 있다. 두 루프가 '느낌으로' 붙는지는 사람이 본다.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HybridSiegeSeasons;
using NUnit.Framework;

namespace HybridSiegeSeasons.Tests
{
    [TestFixture]
    public class CheckerTests
    {
        GameData _d;

        [OneTimeSetUp]
        public void Setup() { _d = GameData.LoadDefault(); }

        // ── 0. 규율 ───────────────────────────────────────────────────

        [Test]
        public void src_에_UnityEngine_의존이_없다()
        {
            var srcDir = Path.Combine(DataLocator.Root(), "src");
            var offenders = new List<string>();
            foreach (var f in Directory.GetFiles(srcDir, "*.cs", SearchOption.AllDirectories))
                foreach (var line in File.ReadAllLines(f))
                {
                    var trimmed = line.TrimStart();
                    if (trimmed.StartsWith("using UnityEngine") || trimmed.StartsWith("using UnityEditor"))
                    { offenders.Add(Path.GetFileName(f)); break; }
                }
            Assert.That(offenders, Is.Empty, "헤드리스 PoC 다. UnityEngine 을 끌어들인 파일: " + string.Join(", ", offenders));
        }

        [Test]
        public void cards_json_에_상점_항목이_없다()
        {
            var banned = new[] { "shop", "price", "gold", "buy", "purchase", "vendor", "merchant", "currency",
                                 "상점", "구매", "가격", "화폐" };
            var json = Newtonsoft.Json.Linq.JToken.Parse(
                File.ReadAllText(Path.Combine(DataLocator.Resolve(), "cards.json")));

            foreach (var prop in json.SelectTokens("$..*").OfType<Newtonsoft.Json.Linq.JProperty>())
            {
                if (prop.Name.StartsWith("_")) continue;
                foreach (var b in banned)
                    Assert.That(prop.Name.ToLowerInvariant().Contains(b), Is.False,
                        $"cards.json 에 상점 관련 필드 '{prop.Name}' 이 있다");
            }
            foreach (var card in _d.Cards)
                foreach (var e in card.effects)
                    foreach (var b in banned)
                        Assert.That(e.type.ToLowerInvariant().Contains(b), Is.False,
                            $"카드 {card.id} 에 상점 관련 효과 '{e.type}' 이 있다");

            foreach (var card in _d.Cards)
            {
                bool fromField = _d.Crops.Any(c => c.cardId == card.id);
                Assert.That(fromField, Is.True, $"카드 {card.id} 를 내는 작물이 없다 — 상점 없이 얻을 수 없다");
            }
        }

        [Test]
        public void 모든_수치가_정수다()
        {
            foreach (var name in new[] { "cards.json", "crops.json", "seasons.json", "plots.json", "enemies.json", "balance.json" })
            {
                var json = Newtonsoft.Json.Linq.JToken.Parse(File.ReadAllText(Path.Combine(DataLocator.Resolve(), name)));
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

        // ── 2. SeedDeterminism ────────────────────────────────────────

        [Test]
        public void SeedDeterminism_같은_씨드면_한_해_전체가_같다()
        {
            string Run()
            {
                var sb = new StringBuilder();
                var loop = new SeasonLoop(_d);
                var f = new Fortress { Seeds = _d.Balance.economy.seedStipendPerYear, Fertilizer = 0, Plots = _d.StartPlots };
                var yr = loop.RunYear(f, 1, new BalancedPlantingPolicy(), WearAwarePlayPolicy.Skilled(_d), new Rng(_d.Balance.seed), sb);
                sb.Append("|deck:").Append(f.Deck.Count).Append('/').Append(f.Deck.TotalUses);
                foreach (var c in f.Deck.Cards) sb.Append(',').Append(c.CardId).Append(':').Append(c.Uses);
                sb.Append("|held:").Append(yr.Held);
                return sb.ToString();
            }
            Assert.That(Run(), Is.EqualTo(Run()));
        }

        [Test]
        public void SeedDeterminism_같은_씨드면_여러_해도_같다()
        {
            string Run()
            {
                var loop = new SeasonLoop(_d);
                var r = loop.Run(6, new BalancedPlantingPolicy(), WearAwarePlayPolicy.Skilled(_d), 31337);
                return string.Join(";", r.Years.Select(y => $"{y.Year}:{y.Held}:{y.DeckAfter}:{y.DeckUsesAfter}:{y.HarvestLost}"));
            }
            var a = Run();
            Assert.That(Run(), Is.EqualTo(a));
            TestContext.WriteLine(a);
        }

        [Test]
        public void SeedDeterminism_다른_씨드면_결과가_갈린다()
        {
            var loop = new SeasonLoop(_d);
            var seen = new HashSet<string>();
            for (int s = 0; s < 12; s++)
            {
                var r = loop.Run(4, new BalancedPlantingPolicy(), WearAwarePlayPolicy.Skilled(_d), s);
                seen.Add(string.Join(";", r.Years.Select(y => $"{y.Held}:{y.DeckAfter}:{y.DeckUsesAfter}")));
            }
            Assert.That(seen.Count, Is.GreaterThan(1), "씨드를 바꿔도 결과가 하나뿐이다 — 난수가 실제로 쓰이지 않는다");
        }

        // ── 3. 이 PoC 의 손잡이: 카드가 해를 넘겨 닳는다 ──────────────

        [Test]
        public void 카드는_해를_넘겨_닳고_다_쓰면_덱에서_사라진다()
        {
            var loop = new SeasonLoop(_d);
            var f = new Fortress { Seeds = _d.Balance.economy.seedStipendPerYear, Fertilizer = 0, Plots = _d.StartPlots };
            var rng = new Rng(_d.Balance.seed);

            var y1 = loop.RunYear(f, 1, new BalancedPlantingPolicy(), WearAwarePlayPolicy.Skilled(_d), rng);
            int usesAfterY1 = f.Deck.TotalUses;
            int brokenY1 = y1.Sieges.Sum(s => s.CardsBroken);

            // 남은 횟수가 실제로 깎였는가 — 새 카드의 총합보다 적어야 한다
            int mintedUses = y1.Field.Plantings.Where(p => p.HarvestTurn >= 0)
                              .Sum(p => p.CardsYielded * _d.Card(_d.Crop(p.CropId).cardId).durability);
            Assert.That(usesAfterY1, Is.LessThan(mintedUses),
                "한 해를 치렀는데 카드가 하나도 닳지 않았다 — 이 PoC 의 손잡이가 돌지 않는다");
            Assert.That(brokenY1, Is.GreaterThanOrEqualTo(0));

            // 덱이 해를 건넌다: 2년차 시작 시점에 1년차 카드가 남아 있어야 한다
            f.Seeds += _d.Balance.economy.seedStipendPerYear;
            int carried = f.Deck.Cards.Count(c => c.FromYear == 1);
            Assert.That(carried, Is.GreaterThan(0), "덱이 해를 넘기지 않는다 — 그러면 hybrid-harvest-deck 과 같은 게임이다");

            var y2 = loop.RunYear(f, 2, new BalancedPlantingPolicy(), WearAwarePlayPolicy.Skilled(_d), rng);
            TestContext.WriteLine($"1년차: 새 사용횟수 {mintedUses} → 남은 {usesAfterY1}, 부서짐 {brokenY1}장 · " +
                                  $"2년차로 넘어간 1년차 카드 {carried}장 · 2년차 뒤 덱 {y2.DeckAfter}장 / {y2.DeckUsesAfter}회");
        }

        [Test]
        public void 손질은_닳은_카드를_되살리고_자기_자신은_못_고친다()
        {
            // 마모 경제에서 '덜어 내기'는 덫이었다 — 검사기가 herb 를 빼는 편이 낫다고 잡아냈다.
            // 그래서 이 변형의 정리는 **손질**이다. 다만 손질끼리 서로 고치면 무한 증식 고리가 된다.
            var mend = _d.Card("card_sort");
            Assert.That(mend.effects.Any(e => e.type == "repair"), Is.True,
                "'손질'에 repair 가 없다 — 닳는 것을 늦출 수단이 사라진다");

            var deck = new StandingDeck();
            deck.Add(mend, 4, 1);
            var worn = new CardInstance { CardId = "card_slash", Uses = 1, MaxUses = 10, FromYear = 1 };
            deck.Cards.Add(worn);

            var battle = new SiegeBattleSim(_d);
            battle.Run(_d.Enemy("enemy_cur"), deck, _d.Year.playerHp,
                       WearAwarePlayPolicy.Skilled(_d), new Rng(7));
            Assert.That(worn.Uses, Is.GreaterThan(1), "닳은 카드가 손질되지 않았다");

            // 손질끼리는 서로 고치지 못한다: 손질만 있는 덱은 총 횟수가 늘 수 없다
            var onlyMend = new StandingDeck();
            onlyMend.Add(mend, 6, 1);
            int usesBefore = onlyMend.TotalUses;
            battle.Run(_d.Enemy("enemy_cur"), onlyMend, _d.Year.playerHp,
                       WearAwarePlayPolicy.Skilled(_d), new Rng(11));
            Assert.That(onlyMend.TotalUses, Is.LessThanOrEqualTo(usesBefore),
                "손질만으로 총 사용 횟수가 늘었다 — 무한 증식 고리다");
            TestContext.WriteLine($"닳은 베기 1 → {worn.Uses}회 · 손질만 있는 덱 {usesBefore} → {onlyMend.TotalUses}회");
        }

        // ── 4. 연결부: SeasonFeasibility ──────────────────────────────

        [Test]
        public void SeasonFeasibility_각_계절의_작물만으로_네_침입을_모두_막는다()
        {
            var rows = SeasonFeasibility.Check(_d);
            Assert.That(rows.Count, Is.EqualTo(_d.PlantingSeasons.Count));

            var lines = new List<string>();
            foreach (var r in rows)
            {
                lines.Add($"{r.SeasonName}({r.SeasonId}) 작물 {r.CropPool.Count}종 [{string.Join(",", r.CropPool)}] " +
                          $"→ 최선 '{r.BestStrategy}' 덱 {r.AvgDeckSize}장 · 해 승률 {r.WinPct}% · " +
                          $"침입 방어율 {r.SiegeHoldPct}% (기준 {_d.Balance.seasonFeasibility.minWinPct}%)");
                foreach (var kv in r.StrategyWinPct) lines.Add($"      {kv.Key}: {kv.Value}%");
            }
            TestContext.WriteLine(string.Join("\n", lines));

            foreach (var r in rows)
            {
                Assert.That(r.CropPool.Count, Is.GreaterThan(0), $"{r.SeasonName}: 심을 작물이 하나도 없다");
                Assert.That(r.Passed, Is.True,
                    Localization.Text("check.season.fail",
                        "{0}: 이 계절에 심을 수 있는 작물만으로는 그 침입들을 막지 못한다", r.SeasonName)
                    + $" (승률 {r.WinPct}% < {_d.Balance.seasonFeasibility.minWinPct}%)");
            }
        }

        [Test]
        public void SeasonFeasibility_모든_계절에_침입이_있고_갈수록_격화된다()
        {
            var ordered = _d.Seasons.OrderBy(s => s.order).ToList();
            int prev = 0;
            foreach (var s in ordered)
            {
                var siege = _d.SiegeOf(s.id);
                Assert.That(siege, Is.Not.Null, $"{s.nameKo} 에 침입이 없다 — 빈 계절");
                Assert.That(siege.escalationPct, Is.GreaterThan(prev),
                    $"{s.nameKo} 의 격화({siege.escalationPct}%)가 앞 계절({prev}%)보다 세지 않다");
                prev = siege.escalationPct;
            }
        }

        // ── 5. 연결부: FieldDeckMapping ───────────────────────────────

        [Test]
        public void FieldDeckMapping_모든_카드가_밭에서_나오고_모든_작물이_쓸모_있다()
        {
            var r = FieldDeckMapping.Check(_d);

            var lines = new List<string> { $"{_d.Balance.fieldDeckMapping.years}년 기준 승률 {r.BaselineWinPct}%" };
            foreach (var kv in r.CardSources)
                lines.Add($"  {kv.Key} ← [{string.Join(",", kv.Value)}] 놓인 비율 {r.CardPlayPct[kv.Key]}%");
            foreach (var kv in r.ExclusionGainPct)
                lines.Add($"  {kv.Key} 를 빼면 승률 {(kv.Value >= 0 ? "+" : "")}{kv.Value}%p");
            TestContext.WriteLine(string.Join("\n", lines));

            Assert.That(r.CardsWithNoCrop, Is.Empty, "상점 없이 얻을 수 없는 카드가 있다: " + string.Join(", ", r.CardsWithNoCrop));
            Assert.That(r.CropsWithBrokenCard, Is.Empty, "카드를 내지 못하는 작물이 있다: " + string.Join(", ", r.CropsWithBrokenCard));
            Assert.That(r.CardsNeverPlayed, Is.Empty, "밭에서 나오지만 아무도 놓지 않는 카드가 있다: " + string.Join(", ", r.CardsNeverPlayed));
            Assert.That(r.CropsBetterRemoved, Is.Empty, "빼는 편이 나은 작물이 있다 — 심을 이유가 없다: " + string.Join(", ", r.CropsBetterRemoved));
        }

        // ── 6. EconomyChecker ─────────────────────────────────────────

        [Test]
        public void EconomyChecker_무한_증식_고리가_없다()
        {
            var r = EconomyChecker.Check(_d);
            TestContext.WriteLine($"{r.Years}년 · 최대 씨앗 {r.MaxSeeds} · 최대 비료 {r.MaxFertilizer} · " +
                                  $"최대 밭 {r.MaxPlots} · 최대 덱 {r.MaxDeck}장 (상한 {_d.Balance.economy.deckCeiling})");
            Assert.That(r.Problems, Is.Empty, string.Join("\n", r.Problems));
            Assert.That(r.NoMidYearSeedMinting, Is.True);
            Assert.That(r.NoMonotoneDivergence, Is.True);
        }

        // ── 7. RunSolvability ─────────────────────────────────────────

        [Test]
        public void RunSolvability_이길_수_있다()
        {
            var r = RunSolvability.Check(_d);
            TestContext.WriteLine($"1년차 해 승률 {r.WinPct}% · 침입 방어율 {r.SiegeHoldPct}% (기준 {r.RequiredPct}%)");
            Assert.That(r.UnbeatableSieges, Is.Empty, "혼자서는 막을 수 없는 침입이 있다: " + string.Join(", ", r.UnbeatableSieges));
            Assert.That(r.WinPct, Is.GreaterThanOrEqualTo(r.RequiredPct));
        }

        // ── 8. DominanceChecker — 단작 + 아끼기만 하기 ────────────────

        [Test]
        public void DominanceChecker_단작도_아끼기만_하기도_이기지_못한다()
        {
            var rows = DominanceChecker.Check(_d);
            TestContext.WriteLine(string.Join("\n", rows.Select(r => $"  {r.Label}: {r.WinPct}% — {r.Why}")));
            foreach (var r in rows)
                Assert.That(r.Passed, Is.True, $"{r.Label} 이 지배 전략이다. {r.Why}");
        }

        // ── 9. WinRateBand ────────────────────────────────────────────

        [Test]
        public void WinRateBand_승률이_설계_구간_안이다()
        {
            var r = WinRateBand.Measure(_d);
            TestContext.WriteLine($"무작위 {r.RandomPct}% (구간 {r.RandomMin}~{r.RandomMax}) · " +
                                  $"준최적 {r.SkilledPct}% (구간 {r.SkilledMin}~{r.SkilledMax})");
            Assert.That(r.RandomInBand, Is.True, $"무작위 플레이 승률 {r.RandomPct}% 가 구간 밖이다");
            Assert.That(r.SkilledInBand, Is.True, $"준최적 플레이 승률 {r.SkilledPct}% 가 구간 밖이다");
            Assert.That(r.SkilledPct, Is.GreaterThan(r.RandomPct), "잘 두는 것이 이득이 아니다 — 실력 축이 없다");
        }

        // ── 10. 연출 목업이 읽을 표본 ─────────────────────────────────

        [Test]
        public void 연출_목업이_읽을_한_해를_굽는다()
        {
            var path = PresentationSample.Write(_d, _d.Balance.seed);
            Assert.That(File.Exists(path), Is.True);
            var s = PresentationSample.Build(_d, _d.Balance.seed);
            Assert.That(s.plantings.Count, Is.GreaterThan(0), "밭에 심은 것이 없으면 목업이 빈다");
            Assert.That(s.sieges.Count, Is.EqualTo(_d.Year.sieges.Count));
            Assert.That(s.deck.Sum(x => x.count), Is.GreaterThan(0), "밭에서 덱이 남지 않으면 목업의 요지가 사라진다");
            TestContext.WriteLine($"{path}: 심기 {s.plantings.Count}회 · 남은 덱 {s.deckAfter}장/{s.deckUsesAfter}회 · " +
                                  $"막은 침입 {s.sieges.Count(x => x.held)}/{s.sieges.Count}");
        }

        // ── 11. 한국어 원문 · 영어 덮어쓰기 ───────────────────────────

        [Test]
        public void Localization_영어는_덮어쓰기고_한국어가_원문이다()
        {
            Localization.Current = Language.Korean;
            Assert.That(Localization.Text("report.deck", "성에 쌓인 덱"), Is.EqualTo("성에 쌓인 덱"));
            Localization.Current = Language.English;
            Assert.That(Localization.Text("report.deck", "성에 쌓인 덱"), Is.EqualTo("Standing deck"));
            Assert.That(Localization.Text("없는.키", "원문 그대로"), Is.EqualTo("원문 그대로"),
                "표에 없는 키는 한국어 원문이 나와야 한다 — 화면이 비면 안 된다");
            Localization.Current = Language.Korean;
        }
    }
}
