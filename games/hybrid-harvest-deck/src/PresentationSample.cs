// 연출 목업이 읽을 '실제로 돌린 한 해'를 굽는다.
// 손으로 그린 밭 그림이면 비교가 되지 않는다 — 시뮬레이터가 낸 밭과 그 밭에서 나온 덱이어야 한다.
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HybridHarvestDeck
{
    public static class PresentationSample
    {
        public sealed class PlantingOut
        {
            public string seasonId;
            public string plotId;
            public string cropId;
            public string cardId;
            public int plantTurn;
            public int harvestTurn;   // -1 = 익지 못하고 잃었다
            public int cardsYielded;
            public bool fertilized;
        }

        public sealed class DeckEntryOut
        {
            public string cardId;
            public int count;
            public List<string> fromCrops = new List<string>();
        }

        public sealed class SampleOut
        {
            public string _ = "src/PresentationSample.cs 가 구운 것. 손으로 고치지 말 것.";
            public int seed;
            public int year;
            public int seedsAtStart;
            public int unlockedPlots;
            public int seedsSpent;
            public int waterUsed;
            public int deckSize;
            public bool nightWon;
            public int battlesCleared;
            public int battlesTotal;
            public int playerHpLeft;
            public List<PlantingOut> plantings = new List<PlantingOut>();
            public List<DeckEntryOut> deck = new List<DeckEntryOut>();
        }

        public static SampleOut Build(GameData d, int seed)
        {
            var campaign = new Campaign(d);
            var rng = new Rng(seed);
            var yr = campaign.RunYear(1, d.Balance.economy.seedStipendPerYear, 0, d.StartPlots,
                                      new BalancedPlantingPolicy(), new SkilledPlayPolicy(d), rng);

            var outp = new SampleOut
            {
                seed = seed,
                year = yr.Year,
                seedsAtStart = yr.SeedsAtStart,
                unlockedPlots = yr.UnlockedPlots,
                seedsSpent = yr.Field.SeedsSpent,
                waterUsed = yr.Field.WaterUsed,
                deckSize = yr.Field.Deck.Count,
                nightWon = yr.Night.Won,
                battlesCleared = yr.Night.BattlesCleared,
                battlesTotal = yr.Night.BattlesTotal,
                playerHpLeft = yr.Night.PlayerHpLeft
            };

            foreach (var p in yr.Field.Plantings)
            {
                outp.plantings.Add(new PlantingOut
                {
                    seasonId = p.SeasonId,
                    plotId = p.PlotId,
                    cropId = p.CropId,
                    cardId = d.Crop(p.CropId).cardId,
                    plantTurn = p.PlantTurn,
                    harvestTurn = p.HarvestTurn,
                    cardsYielded = p.CardsYielded,
                    fertilized = p.Fertilized
                });
            }

            var counts = yr.Field.DeckCounts();
            foreach (var card in d.Cards)
            {
                if (!counts.TryGetValue(card.id, out var n)) n = 0;
                var entry = new DeckEntryOut { cardId = card.id, count = n };
                foreach (var crop in d.Crops) if (crop.cardId == card.id) entry.fromCrops.Add(crop.id);
                outp.deck.Add(entry);
            }
            return outp;
        }

        /// <summary>presentation/sample_year.json 에 굽는다. 경로를 돌려준다.</summary>
        public static string Write(GameData d, int seed)
        {
            var sample = Build(d, seed);
            var dir = Path.Combine(DataLocator.Root(), "presentation");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "sample_year.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(sample, Formatting.Indented));
            return path;
        }
    }
}
