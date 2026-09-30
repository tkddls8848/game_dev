// 연출 목업이 읽을 '실제로 돌린 한 해'를 굽는다.
// 손으로 그린 밭 그림이면 비교가 되지 않는다 — 시뮬레이터가 낸 밭과 그 밭에서 남은 덱이어야 한다.
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HybridSiegeSeasons
{
    public static class PresentationSample
    {
        public sealed class PlantingOut
        {
            public string seasonId, plotId, cropId, cardId;
            public int plantTurn, harvestTurn, cardsYielded;
            public bool fertilized;
        }

        public sealed class SiegeOut
        {
            public string seasonId, enemyId;
            public int enemyHp;
            public bool isBoss, held, emptyDeck;
            public int playerHpLeft, deckBefore, cardsBroken, cardsTidied;
        }

        public sealed class DeckEntryOut
        {
            public string cardId;
            public int count;
            public int usesLeft;
            public int usesMax;
            public int wornPct;
            public List<string> fromCrops = new List<string>();
        }

        public sealed class SampleOut
        {
            public string _ = "src/PresentationSample.cs 가 구운 것. 손으로 고치지 말 것.";
            public int seed, year, seedsAtStart, unlockedPlots, seedsSpent, waterUsed;
            public int deckAfter, deckUsesAfter, harvestLost;
            public bool held;
            public List<PlantingOut> plantings = new List<PlantingOut>();
            public List<SiegeOut> sieges = new List<SiegeOut>();
            public List<DeckEntryOut> deck = new List<DeckEntryOut>();
        }

        public static SampleOut Build(GameData d, int seed, int throughYear = 1)
        {
            var loop = new SeasonLoop(d);
            var rng = new Rng(seed);
            var f = new Fortress { Seeds = 0, Fertilizer = 0, Plots = d.StartPlots };
            YearOutcome yr = null;

            for (int y = 1; y <= throughYear; y++)
            {
                f.Seeds = System.Math.Min(f.Seeds + d.Balance.economy.seedStipendPerYear, d.Balance.economy.seedCeiling);
                yr = loop.RunYear(f, y, new BalancedPlantingPolicy(), WearAwarePlayPolicy.Skilled(d), rng);
            }

            var o = new SampleOut
            {
                seed = seed, year = yr.Year, seedsAtStart = yr.SeedsAtStart,
                unlockedPlots = yr.UnlockedPlots, seedsSpent = yr.Field.SeedsSpent,
                waterUsed = yr.Field.WaterUsed, deckAfter = yr.DeckAfter,
                deckUsesAfter = yr.DeckUsesAfter, harvestLost = yr.HarvestLost, held = yr.Held
            };

            foreach (var p in yr.Field.Plantings)
                o.plantings.Add(new PlantingOut
                {
                    seasonId = p.SeasonId, plotId = p.PlotId, cropId = p.CropId,
                    cardId = d.Crop(p.CropId).cardId, plantTurn = p.PlantTurn,
                    harvestTurn = p.HarvestTurn, cardsYielded = p.CardsYielded, fertilized = p.Fertilized
                });

            foreach (var s in yr.Sieges)
                o.sieges.Add(new SiegeOut
                {
                    seasonId = s.SeasonId, enemyId = s.EnemyId, enemyHp = s.EnemyHp, isBoss = s.IsBoss,
                    held = s.Held, emptyDeck = s.EmptyDeck, playerHpLeft = s.PlayerHpLeft,
                    deckBefore = s.DeckBefore, cardsBroken = s.CardsBroken, cardsTidied = s.CardsTidied
                });

            var counts = f.Deck.CountsByCard();
            var uses = f.Deck.UsesByCard();
            foreach (var card in d.Cards)
            {
                counts.TryGetValue(card.id, out var n);
                uses.TryGetValue(card.id, out var u);
                var entry = new DeckEntryOut
                {
                    cardId = card.id, count = n, usesLeft = u,
                    usesMax = n * card.durability,
                    wornPct = n * card.durability == 0 ? 0 : (n * card.durability - u) * 100 / (n * card.durability)
                };
                foreach (var crop in d.Crops) if (crop.cardId == card.id) entry.fromCrops.Add(crop.id);
                o.deck.Add(entry);
            }
            return o;
        }

        /// <summary>presentation/sample_year.json 에 굽는다. 경로를 돌려준다.</summary>
        public static string Write(GameData d, int seed, int throughYear = 1)
        {
            var sample = Build(d, seed, throughYear);
            var dir = Path.Combine(DataLocator.Root(), "presentation");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "sample_year.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(sample, Formatting.Indented));
            return path;
        }
    }
}
