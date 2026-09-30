// 작물 성장 · 계절 전환 · 물 배분, 그리고 작물 → 카드 변환.
// 이 파일이 "밭이 덱을 만든다"를 실제로 수행하는 곳이다. 카드 상점은 없다.
//
// hybrid-harvest-deck 과 다른 점: 덱이 영속이므로 심기 판단이 **이미 성에 쌓여 있는 카드의
// 남은 횟수**를 본다. 방벽이 아직 열두 번 남아 있으면 호박을 더 심을 이유가 없다.
using System.Collections.Generic;

namespace HybridSiegeSeasons
{
    public sealed class PlantDecision
    {
        public string CropId;
        public bool UseFertilizer;

        public static readonly PlantDecision None = null;
        public static PlantDecision Plant(string cropId, bool fert = false)
            => new PlantDecision { CropId = cropId, UseFertilizer = fert };
    }

    /// <summary>심기 정책이 보는 밭의 상태.</summary>
    public sealed class PlantView
    {
        public GameData Data;
        public SeasonDef Season;
        public int Turn;
        public int TurnsLeftInSeason;
        public PlotDef Plot;
        public int SeedsLeft;
        public int WaterLeft;
        public int FertilizerLeft;
        /// <summary>
        /// 올해 나온 카드 + 지금 자라는 중인 작물이 낼 카드 (cardId → 장수).
        /// 자라는 것을 빼면 정책이 같은 턴에 모든 밭에 같은 작물을 심는다.
        /// </summary>
        public IReadOnlyDictionary<string, int> DeckProjected;

        /// <summary>이미 성에 쌓여 있는 카드의 남은 횟수 (cardId → 횟수). 영속 덱이라 이것을 봐야 한다.</summary>
        public IReadOnlyDictionary<string, int> StandingUses;

        /// <summary>이 계절에 아직 쓸 수 있는 씨앗. 해의 남은 씨앗과 계절 상한 중 작은 쪽이다.</summary>
        public int SeedsUsableThisSeason;

        public bool CanPlant(CropDef c)
            => c.GrowsIn(Season.id)
               && c.growTurns <= TurnsLeftInSeason
               && c.seedCost <= SeedsUsableThisSeason
               && c.TotalWater <= WaterLeft;
    }

    public interface IPlantingPolicy
    {
        PlantDecision Choose(PlantView view);
    }

    public sealed class PlantingRecord
    {
        public string SeasonId;
        public string PlotId;
        public string CropId;
        public int PlantTurn;
        public int HarvestTurn;
        public int CardsYielded;
        public bool Fertilized;
        /// <summary>이미 성으로 들였는가. 계절 순서대로 한 번씩만 들인다.</summary>
        public bool Consumed;
    }

    public sealed class FieldYear
    {
        public List<PlantingRecord> Plantings = new List<PlantingRecord>();
        /// <summary>밭에서 나온 덱. 이것 말고 카드를 얻는 길은 없다.</summary>
        public List<string> Deck = new List<string>();
        public int SeedsSpent;
        public int SeedsLeft;
        public int FertilizerLeft;
        public int WaterUsed;

        public Dictionary<string, int> DeckCounts()
        {
            var d = new Dictionary<string, int>();
            foreach (var c in Deck) d[c] = d.TryGetValue(c, out var n) ? n + 1 : 1;
            return d;
        }
    }

    public sealed class FieldSim
    {
        readonly GameData _data;
        public FieldSim(GameData data) { _data = data; }

        /// <summary>한 해의 봄·여름·가을을 돌려 덱을 만든다. 겨울은 Night 가 맡는다.</summary>
        public FieldYear RunYear(int seeds, int fertilizer, int unlockedPlots, IPlantingPolicy policy,
                                 IReadOnlyDictionary<string, int> standingUses = null)
        {
            standingUses = standingUses ?? new Dictionary<string, int>();
            var year = new FieldYear { SeedsLeft = seeds, FertilizerLeft = fertilizer };
            var deckProjected = new Dictionary<string, int>();   // 수확분 + 자라는 중인 것

            int plotCount = unlockedPlots < _data.Plots.Count ? unlockedPlots : _data.Plots.Count;
            var acc = new PercentAccumulator[plotCount];
            for (int i = 0; i < plotCount; i++) acc[i] = new PercentAccumulator();

            foreach (var season in _data.PlantingSeasons)
            {
                int water = season.waterBudget;
                int seedsThisSeason = season.seedBudget;   // 봄에 한 해치를 다 써 버릴 수 없다
                var busyUntil = new int[plotCount];      // 이 턴이 되면 수확된다. 0 = 비어 있음
                var growing = new PlantingRecord[plotCount];
                for (int i = 0; i < plotCount; i++) busyUntil[i] = -1;

                for (int turn = 0; turn <= season.turns; turn++)
                {
                    // 1) 이번 턴에 익은 것을 거둔다
                    for (int i = 0; i < plotCount; i++)
                    {
                        if (growing[i] == null || busyUntil[i] != turn) continue;
                        var rec = growing[i];
                        var crop = _data.Crop(rec.CropId);
                        int baseYield = crop.cardsPerHarvest + (rec.Fertilized ? _data.Balance.economy.fertilizerYieldBonus : 0);
                        int yield = acc[i].Apply(baseYield, _data.Plots[i].fertility);
                        rec.HarvestTurn = turn;
                        rec.CardsYielded = yield;
                        for (int k = 0; k < yield; k++) year.Deck.Add(crop.cardId);
                        // 자라는 중으로 잡아 두었던 예상치를 실제 수확량으로 바꾼다
                        int projected = deckProjected.TryGetValue(crop.cardId, out var had) ? had : 0;
                        deckProjected[crop.cardId] = projected - crop.cardsPerHarvest + yield;
                        growing[i] = null;
                        busyUntil[i] = -1;
                    }

                    if (turn == season.turns) break;   // 마지막 지점은 수확만 한다

                    // 2) 빈 밭에 심는다
                    for (int i = 0; i < plotCount; i++)
                    {
                        if (growing[i] != null) continue;
                        var view = new PlantView
                        {
                            Data = _data,
                            Season = season,
                            Turn = turn,
                            TurnsLeftInSeason = season.turns - turn,
                            Plot = _data.Plots[i],
                            SeedsLeft = year.SeedsLeft,
                            SeedsUsableThisSeason = seedsThisSeason < year.SeedsLeft ? seedsThisSeason : year.SeedsLeft,
                            WaterLeft = water,
                            FertilizerLeft = year.FertilizerLeft,
                            DeckProjected = deckProjected,
                            StandingUses = standingUses
                        };
                        var decision = policy.Choose(view);
                        if (decision == null || string.IsNullOrEmpty(decision.CropId)) continue;

                        var crop = _data.Crop(decision.CropId);
                        if (!view.CanPlant(crop)) continue;

                        bool fert = decision.UseFertilizer && year.FertilizerLeft > 0;
                        if (fert) year.FertilizerLeft--;

                        year.SeedsLeft -= crop.seedCost;
                        year.SeedsSpent += crop.seedCost;
                        seedsThisSeason -= crop.seedCost;
                        water -= crop.TotalWater;
                        year.WaterUsed += crop.TotalWater;

                        var rec = new PlantingRecord
                        {
                            SeasonId = season.id,
                            PlotId = _data.Plots[i].id,
                            CropId = crop.id,
                            PlantTurn = turn,
                            HarvestTurn = -1,
                            CardsYielded = 0,
                            Fertilized = fert
                        };
                        growing[i] = rec;
                        busyUntil[i] = turn + crop.growTurns;
                        deckProjected[crop.cardId] =
                            (deckProjected.TryGetValue(crop.cardId, out var pend) ? pend : 0) + crop.cardsPerHarvest;
                        year.Plantings.Add(rec);
                    }
                }

                // 계절이 끝날 때 익지 않은 것은 잃는다(기록에는 HarvestTurn = -1 로 남는다)
                for (int i = 0; i < plotCount; i++)
                {
                    if (growing[i] == null) continue;
                    var lost = _data.Crop(growing[i].CropId);
                    deckProjected[lost.cardId] = deckProjected[lost.cardId] - lost.cardsPerHarvest;
                    growing[i] = null;
                }
            }

            return year;
        }
    }
}
