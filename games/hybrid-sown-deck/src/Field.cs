// 밭. 작물 성장 · 계절 전환 · 물 배분, 그리고 **두 방향의 변환**.
//
//   밭 → 덱 : 씨앗으로 작물을 길러 거두면 표본(카드)이 된다        (먼저 만든 둘과 같다)
//   덱 → 밭 : 지난 겨울에 심은 표본이 봄에 돋아나 변한 표본이 된다  (이 PoC 가 더하는 것)
//
// 두 번째 경로의 값은 오직 **봄의 밭칸**이다. 씨앗도 물도 들지 않는다 —
// 대신 심은 칸은 봄 내내(또는 sownTurns 동안) 묶이고, 그만큼 새로 심을 자리가 없다.
// 이것이 "덱을 심는다"가 공짜가 아니게 만드는 자리다.
using System.Collections.Generic;

namespace HybridSownDeck
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
        /// <summary>올해 이미 손에 든 표본 + 자라는 중인 작물이 낼 표본 + 아직 돋지 않은 심은 표본 (cardId → 장수).</summary>
        public IReadOnlyDictionary<string, int> DeckProjected;
        /// <summary>이 계절에 아직 쓸 수 있는 씨앗. 해의 남은 씨앗과 계절 상한 중 작은 쪽이다.</summary>
        public int SeedsUsableThisSeason;
        /// <summary>지금 심은 표본에 묶여 있는 밭칸 수. 덱 → 밭 경로가 밭을 얼마나 먹었는지가 보인다.</summary>
        public int PlotsHeldBySown;

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
        public int HarvestTurn;      // -1 = 익지 못했다
        public int CardsYielded;
        public bool Fertilized;
    }

    /// <summary>심은 표본이 돋아난 기록. 덱 → 밭 경로가 실제로 돈 흔적이다.</summary>
    public sealed class SproutRecord
    {
        public string PlotId;
        public string SeasonId;
        public string FromCardId;
        public string SowType;       // grow | split | seedfall
        public List<string> BecameCardIds = new List<string>();
        public int SeedsGained;
        public int OccupiedTurns;
        public string LineageId;
        public int Generation;
    }

    public sealed class FieldYear
    {
        public List<PlantingRecord> Plantings = new List<PlantingRecord>();
        public List<SproutRecord> Sprouts = new List<SproutRecord>();
        /// <summary>그 해의 덱. 카드를 얻는 길은 이 두 가지뿐이다 — 상점은 없다.</summary>
        public YearDeck Deck = new YearDeck();
        public int SeedsSpent;
        public int SeedsLeft;
        public int FertilizerLeft;
        public int WaterUsed;
        /// <summary>심은 표본이 먹은 밭칸-턴. 덱 → 밭 경로가 치른 값이다.</summary>
        public int PlotTurnsToSown;
        /// <summary>심은 표본에서 돌아온 씨앗 (불꽃씨의 seedfall).</summary>
        public int SeedsFromSown;
        /// <summary>심어서 돋아난 표본 장수. 끊은 세계에서는 늘 0 이다.</summary>
        public int CardsFromSown;

        public Dictionary<string, int> DeckCounts() => Deck.CountsByCard();
    }

    public sealed class FieldSim
    {
        readonly GameData _data;
        public FieldSim(GameData data) { _data = data; }

        /// <summary>
        /// 한 해의 봄·여름·가을. 겨울(밤)은 Night 가 맡는다.
        /// sown 은 지난 겨울에 밭으로 돌아간 표본들이다 — 끊은 세계에서는 늘 빈 목록이다.
        /// </summary>
        public FieldYear RunYear(int year, int seeds, int fertilizer, int unlockedPlots,
                                 List<SownSpecimen> sown, IPlantingPolicy policy, ref int lineageCounter)
        {
            sown = sown ?? new List<SownSpecimen>();
            var yearOut = new FieldYear { SeedsLeft = seeds, FertilizerLeft = fertilizer };
            var deckProjected = new Dictionary<string, int>();

            int plotCount = unlockedPlots < _data.Plots.Count ? unlockedPlots : _data.Plots.Count;
            var acc = new PercentAccumulator[plotCount];
            for (int i = 0; i < plotCount; i++) acc[i] = new PercentAccumulator();

            // 아직 돋지 않은 심은 표본이 낼 카드를 미리 잡아 둔다 — 정책이 그것을 보고 심어야 한다.
            foreach (var s in sown)
            {
                var rule = _data.Card(s.Source.CardId).sown;
                if (rule != null && rule.YieldsCard && !string.IsNullOrEmpty(rule.becomes))
                    deckProjected[rule.becomes] =
                        (deckProjected.TryGetValue(rule.becomes, out var n0) ? n0 : 0) + (rule.amount < 1 ? 1 : rule.amount);
            }

            var pending = new List<SownSpecimen>(sown);

            foreach (var season in _data.PlantingSeasons)
            {
                int water = season.waterBudget;
                int seedsThisSeason = season.seedBudget;   // 봄에 한 해치를 다 써 버릴 수 없다
                var busyUntil = new int[plotCount];
                var growing = new PlantingRecord[plotCount];
                var sownAt = new SownSpecimen[plotCount];
                for (int i = 0; i < plotCount; i++) busyUntil[i] = -1;

                // ── 덱 → 밭: 심은 표본이 이 계절의 밭칸을 묶는다 (봄만) ──────────
                if (season.sowOccupies && pending.Count > 0)
                {
                    for (int i = 0; i < plotCount && pending.Count > 0; i++)
                    {
                        var s = pending[0];
                        pending.RemoveAt(0);
                        int turns = s.OccupiesTurns < 1 ? 1 : s.OccupiesTurns;
                        if (turns > season.turns) turns = season.turns;
                        sownAt[i] = s;
                        busyUntil[i] = turns;
                        yearOut.PlotTurnsToSown += turns;
                    }
                    // 밭칸이 모자라 심을 자리를 못 얻은 표본은 그대로 잃는다.
                    // (상한은 SowPolicy 가 이미 지키지만, 밭이 줄어든 해에는 여기서 걸린다)
                    pending.Clear();
                }

                for (int turn = 0; turn <= season.turns; turn++)
                {
                    // 1) 이번 턴에 익은 것 / 돋아난 것을 거둔다
                    for (int i = 0; i < plotCount; i++)
                    {
                        if (busyUntil[i] != turn) continue;

                        if (sownAt[i] != null)
                        {
                            var s = sownAt[i];
                            var sprouted = Sowing.Sprout(_data, s, year, season.id);
                            var rec = new SproutRecord
                            {
                                PlotId = _data.Plots[i].id, SeasonId = season.id,
                                FromCardId = s.Source.CardId,
                                SowType = _data.Card(s.Source.CardId).sown.type,
                                SeedsGained = sprouted.Seeds,
                                OccupiedTurns = busyUntil[i],
                                LineageId = s.Source.LineageId,
                                Generation = s.Source.Generation
                            };
                            foreach (var c in sprouted.Cards)
                            {
                                yearOut.Deck.Add(c);
                                yearOut.CardsFromSown++;
                                rec.BecameCardIds.Add(c.CardId);
                                deckProjected[c.CardId] = deckProjected.TryGetValue(c.CardId, out var had) ? had : 0;
                                // 미리 잡아 둔 예상치를 실제로 바꾼다 (같은 값이므로 그대로 둔다)
                            }
                            if (sprouted.Seeds > 0)
                            {
                                yearOut.SeedsLeft += sprouted.Seeds;
                                yearOut.SeedsFromSown += sprouted.Seeds;
                                seedsThisSeason += sprouted.Seeds;
                            }
                            yearOut.Sprouts.Add(rec);
                            sownAt[i] = null;
                            busyUntil[i] = -1;
                            continue;
                        }

                        if (growing[i] == null) continue;
                        var prec = growing[i];
                        var crop = _data.Crop(prec.CropId);
                        int baseYield = crop.cardsPerHarvest + (prec.Fertilized ? _data.Balance.economy.fertilizerYieldBonus : 0);
                        int yield = acc[i].Apply(baseYield, _data.Plots[i].fertility);
                        prec.HarvestTurn = turn;
                        prec.CardsYielded = yield;
                        yearOut.Deck.AddHarvest(_data.Card(crop.cardId), crop, yield, year, season.id, ref lineageCounter);
                        int projected = deckProjected.TryGetValue(crop.cardId, out var p0) ? p0 : 0;
                        deckProjected[crop.cardId] = projected - crop.cardsPerHarvest + yield;
                        growing[i] = null;
                        busyUntil[i] = -1;
                    }

                    if (turn == season.turns) break;   // 마지막 지점은 거두기만 한다

                    int heldBySown = 0;
                    for (int i = 0; i < plotCount; i++) if (sownAt[i] != null) heldBySown++;

                    // 2) 빈 밭에 심는다
                    for (int i = 0; i < plotCount; i++)
                    {
                        if (growing[i] != null || sownAt[i] != null) continue;
                        var view = new PlantView
                        {
                            Data = _data,
                            Season = season,
                            Turn = turn,
                            TurnsLeftInSeason = season.turns - turn,
                            Plot = _data.Plots[i],
                            SeedsLeft = yearOut.SeedsLeft,
                            SeedsUsableThisSeason = seedsThisSeason < yearOut.SeedsLeft ? seedsThisSeason : yearOut.SeedsLeft,
                            WaterLeft = water,
                            FertilizerLeft = yearOut.FertilizerLeft,
                            DeckProjected = deckProjected,
                            PlotsHeldBySown = heldBySown
                        };
                        var decision = policy.Choose(view);
                        if (decision == null || string.IsNullOrEmpty(decision.CropId)) continue;

                        var crop = _data.Crop(decision.CropId);
                        if (!view.CanPlant(crop)) continue;

                        bool fert = decision.UseFertilizer && yearOut.FertilizerLeft > 0;
                        if (fert) yearOut.FertilizerLeft--;

                        yearOut.SeedsLeft -= crop.seedCost;
                        yearOut.SeedsSpent += crop.seedCost;
                        seedsThisSeason -= crop.seedCost;
                        water -= crop.TotalWater;
                        yearOut.WaterUsed += crop.TotalWater;

                        var rec = new PlantingRecord
                        {
                            SeasonId = season.id, PlotId = _data.Plots[i].id, CropId = crop.id,
                            PlantTurn = turn, HarvestTurn = -1, CardsYielded = 0, Fertilized = fert
                        };
                        growing[i] = rec;
                        busyUntil[i] = turn + crop.growTurns;
                        deckProjected[crop.cardId] =
                            (deckProjected.TryGetValue(crop.cardId, out var pend) ? pend : 0) + crop.cardsPerHarvest;
                        yearOut.Plantings.Add(rec);
                    }
                }

                // 계절이 끝날 때 익지 않은 것은 잃는다 (HarvestTurn = -1 로 남는다)
                for (int i = 0; i < plotCount; i++)
                {
                    if (growing[i] == null) continue;
                    var lost = _data.Crop(growing[i].CropId);
                    deckProjected[lost.cardId] = deckProjected[lost.cardId] - lost.cardsPerHarvest;
                    growing[i] = null;
                }
            }

            return yearOut;
        }
    }
}
