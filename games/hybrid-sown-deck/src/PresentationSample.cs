// 연출 목업이 읽을 '실제로 돌린 세 해'를 굽는다.
// 손으로 그린 표본집이면 다른 PoC 와 견줄 수 없다 — 시뮬레이터가 낸 밭과 그 밭에서 나온 표본이어야 한다.
//
// 같은 씨드로 **끊은 세계**도 함께 굽는다. 목업이 둘을 나란히 놓으면
// "덱을 심으면 표본집이 달라진다"가 그림 없이 수치로 보인다.
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HybridSownDeck
{
    public static class PresentationSample
    {
        public sealed class PlantingOut
        {
            public string seasonId, plotId, cropId, cardId;
            public int plantTurn, harvestTurn, cardsYielded;
            public bool fertilized;
        }

        public sealed class SproutOut
        {
            public string plotId, seasonId, fromCardId, sowType, lineageId;
            public List<string> becameCardIds = new List<string>();
            public int seedsGained, occupiedTurns, generation;
        }

        public sealed class VisitOut
        {
            public string enemyId;
            public int enemyHp, playerHpLeft, turns;
            public bool isBoss, held;
        }

        public sealed class DeckEntryOut
        {
            public string cardId;
            public int count;
            public int fromHarvest;   // 씨앗으로 길러 거둔 장수
            public int fromSown;      // 심어서 돋아난 장수
            public int unplayed;      // 밤에 한 번도 놓지 않은 장수
            public List<string> fromCrops = new List<string>();
        }

        public sealed class YearOut
        {
            public int year, seedsAtStart, plots, sownAtStart;
            public int deckSize, cardsFromSown, seedsFromSown, plotTurnsToSown;
            public int unplayedAfterNight, sownForNextYear, sowPlotBudget, playerHpLeft;
            public bool held;
            public List<PlantingOut> plantings = new List<PlantingOut>();
            public List<SproutOut> sprouts = new List<SproutOut>();
            public List<VisitOut> visits = new List<VisitOut>();
            public List<DeckEntryOut> deck = new List<DeckEntryOut>();
        }

        public sealed class LineageStepOut
        {
            public int year;
            public string cardId, how;
        }

        /// <summary>한 표본의 여러 해 모습. 연출이 이것을 옆으로 늘어놓는다.</summary>
        public sealed class LineageOut
        {
            public string lineageId;
            public int generations;
            public string sourceCropId;
            public List<LineageStepOut> steps = new List<LineageStepOut>();
        }

        public sealed class CutYearOut
        {
            public int year, deckSize;
            public bool held;
            public List<DeckEntryOut> deck = new List<DeckEntryOut>();
        }

        public sealed class SampleOut
        {
            public string _ = "src/PresentationSample.cs 가 구운 것. 손으로 고치지 말 것.";
            public int seed, years, sowPlots;
            public string world = "이은 세계";
            public List<YearOut> yearsOut = new List<YearOut>();
            public List<LineageOut> lineages = new List<LineageOut>();
            public string cutWorld = "끊은 세계 — 같은 씨드, 같은 정책. 쓰지 않은 표본을 버린다";
            public List<CutYearOut> cut = new List<CutYearOut>();
        }

        public static SampleOut Build(GameData d, int seed, int years)
        {
            int sowPlots = d.Balance.sowing.plotsSkilled;
            var o = new SampleOut { seed = seed, years = years, sowPlots = sowPlots };

            // ── 이은 세계 ─────────────────────────────────────────────
            var loop = new YearLoop(d);
            var rng = new Rng(seed);
            var g = new Garden { Seeds = 0, Fertilizer = 0, Plots = d.StartPlots };
            var sow = new GreedySowPolicy(sowPlots);
            var lineagePool = new List<Specimen>();

            for (int y = 1; y <= years; y++)
            {
                g.Seeds = System.Math.Min(g.Seeds + d.Balance.economy.seedStipendPerYear, d.Balance.economy.seedCeiling);
                var yr = loop.RunYear(g, y, WorldRules.Connected(), new BalancedPlantingPolicy(),
                                      SowAwarePlayPolicy.Skilled(d), sow, rng);
                o.yearsOut.Add(ToYearOut(d, yr));
                lineagePool.AddRange(yr.Field.Deck.Cards);
            }

            // ── 표본의 내력. 여러 해를 건넌 것 먼저 ──────────────────
            var picked = new List<Specimen>();
            var seenLineage = new HashSet<string>();
            lineagePool.Sort((a, b) =>
            {
                if (a.Generation != b.Generation) return b.Generation.CompareTo(a.Generation);
                if (a.Lineage.Count != b.Lineage.Count) return b.Lineage.Count.CompareTo(a.Lineage.Count);
                return string.CompareOrdinal(a.LineageId, b.LineageId);
            });
            // 여러 해를 건넌 표본을 먼저 채우고, 대조로 한 해만 산 표본을 둘만 붙인다.
            // 목업의 주제가 '카드가 자란다'이므로 한 해짜리로 화면을 채우면 주제가 묻힌다.
            int singles = 0;
            foreach (var s in lineagePool)
            {
                if (picked.Count >= 8) break;
                if (s.Generation < 2 && singles >= 2) continue;
                if (!seenLineage.Add(s.LineageId)) continue;
                if (s.Generation < 2) singles++;
                picked.Add(s);
            }
            foreach (var s in picked)
            {
                var lo = new LineageOut
                {
                    lineageId = s.LineageId, generations = s.Generation,
                    sourceCropId = FindSourceCrop(d, s)
                };
                foreach (var st in s.Lineage)
                    lo.steps.Add(new LineageStepOut { year = st.Year, cardId = st.CardId, how = st.How });
                o.lineages.Add(lo);
            }

            // ── 끊은 세계. 같은 씨드·같은 정책 ───────────────────────
            var cutRun = new YearLoop(d).Run(years, WorldRules.Cut(), new BalancedPlantingPolicy(),
                                             SowAwarePlayPolicy.Skilled(d), new GreedySowPolicy(sowPlots), seed);
            foreach (var yr in cutRun.Years)
            {
                var cy = new CutYearOut { year = yr.Year, deckSize = yr.DeckSize, held = yr.Held };
                cy.deck = DeckEntries(d, yr);
                o.cut.Add(cy);
            }
            return o;
        }

        static string FindSourceCrop(GameData d, Specimen s)
        {
            if (!string.IsNullOrEmpty(s.SourceCropId)) return s.SourceCropId;
            // 심어서 온 표본은 뿌리까지 올라가 처음 모습을 낸 작물을 찾는다.
            if (s.Lineage.Count == 0) return "";
            var first = s.Lineage[0].CardId;
            foreach (var crop in d.Crops) if (crop.cardId == first) return crop.id;
            return "";
        }

        static List<DeckEntryOut> DeckEntries(GameData d, YearOutcome yr)
        {
            var list = new List<DeckEntryOut>();
            foreach (var card in d.Cards)
            {
                var e = new DeckEntryOut { cardId = card.id };
                foreach (var s in yr.Field.Deck.Cards)
                {
                    if (s.CardId != card.id) continue;
                    e.count++;
                    if (string.IsNullOrEmpty(s.SourceCropId)) e.fromSown++; else e.fromHarvest++;
                    if (!s.Played) e.unplayed++;
                }
                foreach (var crop in d.Crops) if (crop.cardId == card.id) e.fromCrops.Add(crop.id);
                list.Add(e);
            }
            return list;
        }

        static YearOut ToYearOut(GameData d, YearOutcome yr)
        {
            var y = new YearOut
            {
                year = yr.Year, seedsAtStart = yr.SeedsAtStart, plots = yr.UnlockedPlots,
                sownAtStart = yr.SownAtStart, deckSize = yr.DeckSize,
                cardsFromSown = yr.CardsFromSown, seedsFromSown = yr.SeedsFromSown,
                plotTurnsToSown = yr.PlotTurnsToSown, unplayedAfterNight = yr.UnplayedAfterNight,
                sownForNextYear = yr.SownForNextYear, sowPlotBudget = yr.SowPlotBudget,
                playerHpLeft = yr.Night.PlayerHpLeft, held = yr.Held
            };
            foreach (var p in yr.Field.Plantings)
                y.plantings.Add(new PlantingOut
                {
                    seasonId = p.SeasonId, plotId = p.PlotId, cropId = p.CropId,
                    cardId = d.Crop(p.CropId).cardId, plantTurn = p.PlantTurn,
                    harvestTurn = p.HarvestTurn, cardsYielded = p.CardsYielded, fertilized = p.Fertilized
                });
            foreach (var s in yr.Field.Sprouts)
            {
                var so = new SproutOut
                {
                    plotId = s.PlotId, seasonId = s.SeasonId, fromCardId = s.FromCardId,
                    sowType = s.SowType, seedsGained = s.SeedsGained,
                    occupiedTurns = s.OccupiedTurns, lineageId = s.LineageId, generation = s.Generation
                };
                so.becameCardIds.AddRange(s.BecameCardIds);
                y.sprouts.Add(so);
            }
            foreach (var v in yr.Night.Visits)
                y.visits.Add(new VisitOut
                {
                    enemyId = v.EnemyId, enemyHp = v.EnemyHp, isBoss = v.IsBoss,
                    held = v.Held, playerHpLeft = v.PlayerHpLeft, turns = v.Turns
                });
            y.deck = DeckEntries(d, yr);
            return y;
        }

        /// <summary>presentation/sample_run.json 에 굽는다. 경로를 돌려준다.</summary>
        public static string Write(GameData d, int seed, int years)
        {
            var sample = Build(d, seed, years);
            var dir = Path.Combine(DataLocator.Root(), "presentation");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "sample_run.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(sample, Formatting.Indented));
            return path;
        }
    }
}
