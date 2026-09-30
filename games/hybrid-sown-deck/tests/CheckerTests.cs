// 검사기 테스트. 이것이 Phase 를 닫는 것이다 — "구현했다"가 아니라 "여기가 통과한다".
// 값을 콘솔에 전부 찍는다. 반년 뒤에 이 숫자를 보고 왜 이 설계인지 되짚어야 한다.
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace HybridSownDeck.Tests
{
    [TestFixture]
    public class CheckerTests
    {
        static GameData _d;

        [OneTimeSetUp]
        public void Load() { _d = GameData.LoadDefault(); }

        static void Say(string s) => TestContext.Progress.WriteLine(s);

        // ── 1. 참조 무결성 ────────────────────────────────────────────
        [Test]
        public void 데이터에_깨진_참조가_없다()
        {
            var problems = DataValidator.Validate(_d);
            foreach (var p in problems) Say("  ! " + p);
            Assert.That(problems, Is.Empty, "데이터 참조가 깨졌다");
            Say($"작물 {_d.Crops.Count} · 카드 {_d.Cards.Count} · 적 {_d.Enemies.Count} · 밭 {_d.StartPlots}/{_d.MaxPlots}칸");
        }

        [Test]
        public void 카드를_얻는_길은_밭뿐이고_상점_항목이_없다()
        {
            // 상점을 넣는 순간 밭이 장식이 되고 이 기획의 차별점이 사라진다 (계획서 §3).
            var json = System.IO.File.ReadAllText(System.IO.Path.Combine(DataLocator.Resolve(), "cards.json"));
            foreach (var word in new[] { "\"shop\"", "\"buy\"", "\"price\"", "\"gold\"", "\"gain_card\"" })
                Assert.That(json.Contains(word), Is.False, $"cards.json 에 상점 냄새가 나는 '{word}' 가 있다");

            var r = FieldDeckMapping.Check(_d);
            Assert.That(r.ShopLikeSources, Is.Empty, "밭을 거치지 않고 카드를 얻는 길이 있다");
            foreach (var kv in r.CardSources)
                Say($"  {kv.Key,-16} ← {string.Join(" · ", kv.Value)}");
            Assert.That(r.CardsWithNoFieldPath, Is.Empty, "밭에서 나올 수 없는 카드가 있다");
        }

        // ── 2. 씨드 재현성 ───────────────────────────────────────────
        [Test]
        public void 같은_씨드는_같은_기록을_낸다()
        {
            Assert.That(Transcript(_d.Balance.seed, 3), Is.EqualTo(Transcript(_d.Balance.seed, 3)));
        }

        [Test]
        public void 다른_씨드는_다른_기록을_낸다()
        {
            Assert.That(Transcript(_d.Balance.seed, 3), Is.Not.EqualTo(Transcript(_d.Balance.seed + 1, 3)));
        }

        [Test]
        public void 세_해를_이어도_재현된다()
        {
            var a = Transcript(4242, _d.Balance.year.campaignYears);
            var b = Transcript(4242, _d.Balance.year.campaignYears);
            Assert.That(a, Is.EqualTo(b));
            Say($"세 해 기록 {a.Length}자");
        }

        static string Transcript(int seed, int years)
        {
            var loop = new YearLoop(_d);
            var sb = new StringBuilder();
            loop.Run(years, WorldRules.Connected(), new BalancedPlantingPolicy(),
                     SowAwarePlayPolicy.Skilled(_d), new GreedySowPolicy(_d.Balance.sowing.plotsSkilled), seed, sb);
            return sb.ToString();
        }

        // ── 3. 심는 규칙 그 자체 ─────────────────────────────────────
        [Test]
        public void 자라는_표본은_한_장이_더_좋은_한_장이_된다()
        {
            var sprout = _d.Card("card_sprout");
            Assert.That(sprout.sown.type, Is.EqualTo("grow"));
            var grown = _d.Card(sprout.sown.becomes);
            Assert.That(CardValue.Combat(_d, grown), Is.GreaterThan(CardValue.Combat(_d, sprout)),
                        "자란 표본이 더 좋지 않으면 심을 이유가 없다");
            Say($"어린싹 {CardValue.Combat(_d, sprout)} → 가시덤불 {CardValue.Combat(_d, grown)} (이득 {CardValue.SowGain(_d, sprout)})");
        }

        [Test]
        public void 갈라지는_표본은_둘이_되지만_이득이_아니다()
        {
            foreach (var card in _d.Cards)
            {
                if (card.sown.type != "split") continue;
                Assert.That(card.sown.amount, Is.GreaterThanOrEqualTo(2));
                // 갈라지면 장수는 늘지만 코스트가 늘어 값이 안 된다. 이것이 '자람 ↔ 갈라짐' 왕복을
                // 무한 증식이 아니라 선택으로 만드는 자리다.
                int gain = CardValue.SowGain(_d, card);
                Say($"{card.id} → {card.sown.becomes} ×{card.sown.amount} : 이득 {gain}");
                Assert.That(gain, Is.LessThanOrEqualTo(0), $"{card.id} 를 갈라 심는 것이 이득이면 표본이 발산한다");
            }
        }

        [Test]
        public void 놓은_표본은_밭으로_돌아가지_못한다()
        {
            int counter = 0;
            var field = new FieldSim(_d);
            var year = field.RunYear(1, _d.Balance.economy.seedStipendPerYear, 0, _d.StartPlots,
                                     new List<SownSpecimen>(), new BalancedPlantingPolicy(), ref counter);
            var night = new NightSim(_d);
            var res = night.Run(year.Deck, _d.Year.playerHp, 100, 3,
                                SowAwarePlayPolicy.Skilled(_d), new Rng(_d.Balance.seed));

            int played = year.Deck.PlayedCount;
            var unplayed = year.Deck.Unplayed();
            foreach (var s in unplayed) Assert.That(s.Played, Is.False);
            Assert.That(played + unplayed.Count, Is.EqualTo(year.Deck.Count));
            Assert.That(played, Is.GreaterThan(0), "한 장도 안 놓고 밤을 보냈다면 전투가 없는 것이다");
            Say($"덱 {year.Deck.Count}장 · 놓은 것 {played} · 남은 것 {unplayed.Count} · 밤 {(res.Held ? "넘겼다" : "졌다")}");
        }

        [Test]
        public void 표본_번호는_해를_넘어_이어진다()
        {
            var loop = new YearLoop(_d);
            var g = new Garden { Seeds = 0, Fertilizer = 0, Plots = _d.StartPlots };
            var rng = new Rng(_d.Balance.seed);
            var sow = new GreedySowPolicy(3);
            int maxGen = 1;
            var seen = new List<string>();

            for (int y = 1; y <= 3; y++)
            {
                g.Seeds = Math.Min(g.Seeds + _d.Balance.economy.seedStipendPerYear, _d.Balance.economy.seedCeiling);
                var yr = loop.RunYear(g, y, WorldRules.Connected(), new BalancedPlantingPolicy(),
                                      SowAwarePlayPolicy.Skilled(_d), sow, rng);
                foreach (var s in yr.Field.Deck.Cards)
                {
                    if (s.Generation > maxGen) maxGen = s.Generation;
                    if (s.Generation >= 2 && seen.Count < 4)
                    {
                        var steps = new List<string>();
                        foreach (var st in s.Lineage) steps.Add($"{st.Year}년 {st.CardId}({st.How})");
                        seen.Add(s.LineageId + ": " + string.Join(" → ", steps));
                    }
                }
            }
            foreach (var line in seen) Say("  " + line);
            Assert.That(maxGen, Is.GreaterThanOrEqualTo(2), "세 해를 돌려도 두 해째 표본이 없으면 카드가 자라지 않는 것이다");
            Say($"가장 오래 이어진 표본 {maxGen}해차");
        }

        // ── 4. 계절 성립성 (hybrid-harvest-deck 에서 두 번 실패한 검사기) ──
        [Test]
        public void 각_계절의_작물만으로_그_밤을_넘길_수_있다()
        {
            var rows = SeasonFeasibility.Check(_d);
            foreach (var r in rows)
            {
                var strat = new List<string>();
                foreach (var kv in r.StrategyWinPct) strat.Add($"{kv.Key} {kv.Value}%");
                Say($"  {r.SeasonName,-3} 작물 {r.CropPool.Count}종 · 최선 {r.BestStrategy} {r.WinPct}% · 덱 {r.AvgDeckSize}장");
                Say($"       {string.Join(" · ", strat)}");
            }
            foreach (var r in rows)
                Assert.That(r.Passed, Is.True,
                    $"{r.SeasonName} 의 작물만으로는 밤을 못 넘긴다 ({r.WinPct}% < {_d.Balance.seasonFeasibility.minWinPct}%)");
        }

        // ── 5. 밭 ↔ 덱 대응 ─────────────────────────────────────────
        [Test]
        public void 모든_카드가_쓰이고_빼는_편이_나은_작물이_없다()
        {
            var r = FieldDeckMapping.Check(_d);
            Say($"기준선 승률 {r.BaselineWinPct}%");
            foreach (var kv in r.CardPlayPct) Say($"  {kv.Key,-16} 놓인 비율 {kv.Value}%");
            foreach (var kv in r.ExclusionGainPct) Say($"  {kv.Key,-16} 빼면 {kv.Value:+#;-#;0}%p");
            Assert.That(r.CardsNeverPlayed, Is.Empty, "아무도 놓지 않는 카드가 있다");
            Assert.That(r.CropsBetterRemoved, Is.Empty, "빼는 편이 나은 작물이 있다");
            Assert.That(r.CropsWithBrokenCard, Is.Empty);
            Assert.That(r.Passed, Is.True);
        }

        // ── 6. ★ 고리가 두 방향으로 도는가 ───────────────────────────
        [Test]
        public void 덱을_심는_경로가_실제로_다른_세계를_만든다()
        {
            var r = LoopClosesBothWays.Check(_d);
            Say("── LoopClosesBothWays ──────────────────────────────");
            foreach (var n in r.Notes) Say("  " + n);
            Say("  마지막 해 덱 구성 (이은 / 끊은):");
            foreach (var card in _d.Cards)
            {
                r.ConnectedFinalDeck.TryGetValue(card.id, out var a);
                r.CutFinalDeck.TryGetValue(card.id, out var b);
                Say($"    {card.id,-16} {a,6} / {b,6}");
            }
            Say("  심은 표본의 종류별 수: " + Join(r.SownTypeTotals));
            Say("  '몇 칸 심나' 축:");
            for (int i = 0; i < r.SowPlotSweep.Length; i++)
                Say($"    {r.SowPlotSweep[i]}칸 → 이은 {r.ConnectedSweepWinPct[i],3}% · 끊은 {r.CutSweepWinPct[i],3}%");

            Assert.That(r.PathActuallyFlows, Is.True,
                $"세 해차 덱에서 심어서 온 표본이 {r.SownSharePct}% 뿐이다 — 경로가 있어도 거의 흐르지 않는다");
            Assert.That(r.ShiftNotAbsorbed, Is.True,
                $"덱 구성 차이가 {r.DeckShiftPct}% (흘러든 몫의 {r.ShiftAsPctOfSownShare}%) 뿐이다 — " +
                "심기 정책이 유입을 전부 흡수해 표본집이 같은 모습으로 수렴한다");
            Assert.That(r.WinDiverges, Is.True,
                $"이은 세계의 최적 설정을 끊은 세계에서 돌렸을 때 차이가 {r.WinGapAtBestPct}%p 뿐이다");
            Assert.That(r.KnobFlips, Is.True,
                $"최적 심기 칸이 이은 {r.BestConnectedPlots} · 끊은 {r.BestCutPlots} 이다 — " +
                "끊은 세계에서 아끼는 것이 손해가 아니면 아끼는 값이 심기에서 오는 것이 아니다");
            Assert.That(r.KnobHasSpread, Is.True,
                $"이은 세계에서 축의 승률 폭이 {r.ConnectedSpreadPct}%p 뿐이다 — 심는 것이 장식이다");
            Assert.That(r.Passed, Is.True);
        }

        // ── 7. 전부 심는 것이 최적이 아닌가 ──────────────────────────
        [Test]
        public void 전부_심고_아무것도_안_쓰는_것이_최적이_아니다()
        {
            var r = SowingIsNotAlwaysBest.Check(_d);
            foreach (var n in r.Notes) Say("  " + n);
            Say($"  극단 정책의 남은 표본 평균 {r.ExtremeAvgUnplayed}장");
            Assert.That(r.ExtremeIsNotBest, Is.True, "전부 심는 것이 최적이면 밤이 의미를 잃는다");
            Assert.That(r.InteriorBeatsBothEnds, Is.True, "준최적이 양 끝 어느 쪽보다도 낫지 않다 — 손잡이가 장식이다");
        }

        // ── 8. 이길 수 있는가 ────────────────────────────────────────
        [Test]
        public void 첫_해를_이길_수_있다()
        {
            var r = RunSolvability.Check(_d);
            Say($"첫 해 승률 {r.WinPct}% (기준 {r.RequiredPct}%)");
            foreach (var u in r.UnbeatableVisits) Say("  ! 혼자 못 막는 침입: " + u);
            Assert.That(r.UnbeatableVisits, Is.Empty);
            Assert.That(r.WinPct, Is.GreaterThanOrEqualTo(r.RequiredPct));
        }

        // ── 9. 지배 전략 ─────────────────────────────────────────────
        [Test]
        public void 지배_전략이_없다()
        {
            var rows = DominanceChecker.Check(_d);
            foreach (var row in rows) Say($"  {(row.Passed ? "ok" : "!!")} {row.Label,-24} {row.WinPct,3}% — {row.Why}");
            foreach (var row in rows) Assert.That(row.Passed, Is.True, row.Label + ": " + row.Why);
        }

        // ── 10. 경제 ─────────────────────────────────────────────────
        [Test]
        public void 자원에_무한_증식_고리가_없다()
        {
            var r = EconomyChecker.Check(_d);
            Say($"{r.Years}년 · 최대 씨앗 {r.MaxSeeds}/{_d.Balance.economy.seedCeiling} · " +
                $"비료 {r.MaxFertilizer}/{_d.Balance.economy.fertilizerCeiling} · " +
                $"밭 {r.MaxPlots}/{_d.MaxPlots} · 덱 {r.MaxDeck}/{_d.Balance.economy.deckCeiling} · " +
                $"심은 표본 {r.MaxSown}/{_d.Balance.economy.sownCeiling}");
            Say($"심어서 돌아온 씨앗 총 {r.SeedsFromSownTotal}");
            foreach (var p in r.Problems) Say("  ! " + p);
            Assert.That(r.NoBattleMinting, Is.True);
            Assert.That(r.NoMonotoneDivergence, Is.True);
            Assert.That(r.Problems, Is.Empty);
        }

        // ── 11. 승률 구간 ────────────────────────────────────────────
        [Test]
        public void 무작위와_준최적의_승률이_구간_안에_있다()
        {
            var r = WinRateBand.Measure(_d);
            Say($"무작위 {r.RandomPct}% (구간 {r.RandomMin}~{r.RandomMax}) · 준최적 {r.SkilledPct}% (구간 {r.SkilledMin}~{r.SkilledMax})");
            Assert.That(r.RandomInBand, Is.True, $"무작위 승률 {r.RandomPct}% 가 구간 밖이다");
            Assert.That(r.SkilledInBand, Is.True, $"준최적 승률 {r.SkilledPct}% 가 구간 밖이다");
        }

        // ── 12. 로컬라이제이션 ───────────────────────────────────────
        [Test]
        public void 한국어가_원문이고_영어는_덮어쓰기다()
        {
            Localization.Current = Language.Korean;
            Assert.That(_d.Card("card_sprout").Name, Is.EqualTo("어린싹"));
            Localization.Current = Language.English;
            Assert.That(_d.Card("card_sprout").Name, Is.EqualTo("Sprout"));
            Assert.That(_d.Card("card_sprout").Label, Is.EqualTo("Seedwort, first year"));
            Localization.Current = Language.Korean;
            Assert.That(_d.Card("card_sprout").Name, Is.EqualTo("어린싹"), "정적 필드에 굳으면 이 줄이 깨진다");

            var missing = new List<string>();
            foreach (var c in _d.Cards) { if (!Localization.HasOverlay("card." + c.id)) missing.Add("card." + c.id); }
            foreach (var c in _d.Crops) { if (!Localization.HasOverlay("crop." + c.id)) missing.Add("crop." + c.id); }
            foreach (var e in _d.Enemies) { if (!Localization.HasOverlay("enemy." + e.id)) missing.Add("enemy." + e.id); }
            Assert.That(missing, Is.Empty, "영어 표에 빠진 키: " + string.Join(", ", missing));
        }

        // ── 13. 연출 목업이 읽는 표본을 굽는다 ───────────────────────
        [Test]
        public void 연출_목업이_읽을_실제_판을_굽는다()
        {
            var path = PresentationSample.Write(_d, _d.Balance.seed, _d.Balance.year.campaignYears);
            Assert.That(System.IO.File.Exists(path), Is.True);
            var text = System.IO.File.ReadAllText(path);
            Assert.That(text.Length, Is.GreaterThan(400));
            Assert.That(text.Contains("lineages"), Is.True, "표본의 여러 해 모습이 없으면 연출이 손그림이 된다");
            Say("구웠다: " + path);
        }

        static string Join(Dictionary<string, int> d)
        {
            var parts = new List<string>();
            foreach (var kv in d) parts.Add($"{kv.Key} {kv.Value}");
            return parts.Count == 0 ? "(없음)" : string.Join(" · ", parts);
        }
    }
}
