// 순수 C#만. using UnityEngine 금지.
// 스키마는 Unity의 JsonUtility가 읽을 수 있는 모양으로 유지한다:
// Dictionary·다형성 없음 · 배열 + 문자열 ID · 값 없는 int는 -1.
using System;
using System.Collections.Generic;

namespace FarmSignal.Data
{
    [Serializable] public class ConfigData
    {
        public string slug, titleKo, titleEn;
        public int seed, weatherSeedOffset, priceSeedOffset, signalSeedOffset;
        public int simYears, economySimYears;
    }

    [Serializable] public class WeatherWeight { public string weatherId; public int weight; }

    [Serializable] public class SeasonDef
    {
        public string id, nameKo, nameEn;
        public int order, transitionSoilLoss, roadTrafficPercent;
        public WeatherWeight[] weatherWeights;
    }

    [Serializable] public class WeatherDef
    {
        public string id, nameKo, nameEn;
        public int waterDelta, growthPercent, exposurePercent;
    }

    [Serializable] public class SeasonsData
    {
        public int daysPerSeason;
        public SeasonDef[] seasons;
        public WeatherDef[] weathers;
    }

    /// 뒤 다섯(visibility · copyAppeal · screenHeight · renownGain · theftAppeal)이 이 PoC의 축이다.
    /// visibility 는 "얼마나 보이나", copyAppeal 은 "보고 따라 심을 만한가" — 둘은 다르다.
    /// 라벤더는 가장 잘 보이지만 따라 심을 값이 크지 않고, 순무는 보여도 아무도 안 따라 한다.
    [Serializable] public class CropDef
    {
        public string id, nameKo, nameEn, noteKo;
        public string[] seasons;
        public int growDays, waterPerDay, yieldUnits, soilDrain, minSoil;
        public int visibility, copyAppeal, screenHeight, renownGain, theftAppeal;
    }

    [Serializable] public class CropsData { public CropDef[] crops; }

    [Serializable] public class PlotDef { public string id; public int x, y, soil, exposure; }
    [Serializable] public class ExpansionDef { public int enabled, costBase, costPerPlot; }

    [Serializable] public class PlotsData
    {
        public int gridWidth, gridHeight, soilFloorForPlanting, exposureFloor,
                   soilRegenPerSeason, screenDepth, screenEffectPercent;
        public ExpansionDef expansion;
        public int reclaimCost;
        public PlotDef[] plots;
    }

    [Serializable] public class ShopRow { public string cropId; public int buySeed, sellUnit; }

    [Serializable] public class Limits
    {
        public int maxYearIncomeCoin, assetCeilingCoin, economyWindowYears, incomeDriftMaxPercent;
        public int seasonViabilityMinCrops, seasonViabilityMinCompletableMoneyCrops,
                   seasonViabilityMinIncomeCoin, deadCropMinPlantings;
        public int firstPriceWarnSeasonMin, firstPriceWarnSeasonMax;
        public int firstCollapseSeasonMin, firstCollapseSeasonMax;
        public int firstTheftSeasonMin, firstTheftSeasonMax;
        public int firstVisitorSeasonMin, firstVisitorSeasonMax;
        public int signalMattersMinMoneyDeltaPercent, signalMattersMinMonoPenaltyPercent,
                   signalMattersMinDeltaSpreadPoints, signalMattersOffBestMinRankOn,
                   noSingleCropWinsMarginPercent;
    }

    [Serializable] public class EconomyData
    {
        public int startMoney, wellWaterPerDay, priceBandPercent, priceStepDays;
        public int livingCostPerSeason, taxPerPlotPerYear, bankruptcyAtCoin;
        public ShopRow[] shop;
        public Limits limits;
    }

    // ── 신호(이 PoC의 핵) ───────────────────────────────────────────────────

    [Serializable] public class CopycatDef
    {
        public int dropPerPointPer1000, recoverPerSeason, scarcityThresholdIndex, scarcityRisePerSeason,
                   floorPercent, ceilPercent, warnPercent, collapsePercent;
    }

    [Serializable] public class TheftDef
    {
        public int riskDivisor, riskCapPer1000, watchPer100Renown,
                   severityStepPer1000, maxPlotsHit, minRiskToRollPer1000;
    }

    [Serializable] public class RenownDef
    {
        public int gainDivisor, decayPerSeason, cap, perVisitor, premiumPerVisitor, premiumCapPercent;
    }

    [Serializable] public class ObservationDef { public int denominator; }

    [Serializable] public class SignalData
    {
        public CopycatDef copycat;
        public TheftDef theft;
        public RenownDef renown;
        public ObservationDef observation;
    }

    // ── 전문 ────────────────────────────────────────────────────────────────

    [Serializable] public class TelegramTemplate
    {
        public string id, kind, nameKo, nameEn;
        public string[] linesKo, linesEn;
    }

    [Serializable] public class EventDef
    {
        public string id, nameKo, onSeason, textKo;
        public int onYear, onDay, minObserved, minRenownLevel, once;
    }

    [Serializable] public class EventsData
    {
        public int beatIntervalDays, linesPerEvent, budgetEventCeiling, budgetLineCeiling;
        public TelegramTemplate[] templates;
        public EventDef[] events;
    }

    // ── 보여 주기용 계획 ────────────────────────────────────────────────────

    [Serializable] public class PlanSeason { public string seasonId; public string[] cropOrder; }
    [Serializable] public class PlanDef { public string id, nameKo, nameEn, noteKo; public PlanSeason[] bySeason; }
    [Serializable] public class ShowcaseData { public PlanDef[] plans; }

    /// JSON 여덟 개를 한 덩어리로. 조회는 여기서만 한다.
    public sealed class GameData
    {
        public ConfigData Config;
        public SeasonsData Seasons;
        public CropsData Crops;
        public PlotsData Plots;
        public EconomyData Economy;
        public SignalData Signal;
        public EventsData Events;
        public ShowcaseData Showcase;

        readonly Dictionary<string, CropDef> _cropById = new Dictionary<string, CropDef>();
        readonly Dictionary<string, ShopRow> _shopByCrop = new Dictionary<string, ShopRow>();
        readonly Dictionary<string, WeatherDef> _weatherById = new Dictionary<string, WeatherDef>();
        readonly Dictionary<string, SeasonDef> _seasonById = new Dictionary<string, SeasonDef>();
        readonly Dictionary<string, int> _cropIndex = new Dictionary<string, int>();
        readonly Dictionary<string, PlotDef> _plotById = new Dictionary<string, PlotDef>();

        public void Index()
        {
            _cropById.Clear(); _shopByCrop.Clear(); _weatherById.Clear();
            _seasonById.Clear(); _cropIndex.Clear(); _plotById.Clear();
            for (int i = 0; i < Crops.crops.Length; i++)
            {
                _cropById[Crops.crops[i].id] = Crops.crops[i];
                _cropIndex[Crops.crops[i].id] = i;
            }
            foreach (var s in Economy.shop) _shopByCrop[s.cropId] = s;
            foreach (var w in Seasons.weathers) _weatherById[w.id] = w;
            foreach (var s in Seasons.seasons) _seasonById[s.id] = s;
            foreach (var p in Plots.plots) _plotById[p.id] = p;
        }

        public int DaysPerYear => Seasons.daysPerSeason * Seasons.seasons.Length;
        public int SeasonsPerYear => Seasons.seasons.Length;
        public int CropCount => Crops.crops.Length;

        public CropDef Crop(string id) => _cropById.TryGetValue(id, out var c) ? c : null;
        public int CropIndex(string id) => _cropIndex.TryGetValue(id, out var i) ? i : -1;
        public ShopRow Shop(string cropId) => _shopByCrop.TryGetValue(cropId, out var s) ? s : null;
        public WeatherDef Weather(string id) => _weatherById.TryGetValue(id, out var w) ? w : null;
        public SeasonDef Season(string id) => _seasonById.TryGetValue(id, out var s) ? s : null;
        public PlotDef Plot(string id) => _plotById.TryGetValue(id, out var p) ? p : null;

        /// 절대 일자(0 기반)의 계절. 계절 순서는 order가 아니라 배열 순서를 따른다.
        public SeasonDef SeasonOfDay(int absoluteDay)
        {
            int inYear = absoluteDay % DaysPerYear;
            return Seasons.seasons[inYear / Seasons.daysPerSeason];
        }

        public int YearOfDay(int absoluteDay) => absoluteDay / DaysPerYear + 1;
        public int DayInSeason(int absoluteDay) => absoluteDay % Seasons.daysPerSeason + 1;
        /// 1부터 세는 절대 계절 번호. 검사기가 "몇 번째 계절에 처음 벌어졌나"를 이 단위로 본다.
        public int SeasonNumberOfDay(int absoluteDay) => absoluteDay / Seasons.daysPerSeason + 1;

        public bool CropFitsSeason(CropDef c, string seasonId)
        {
            if (c.seasons == null) return false;
            for (int i = 0; i < c.seasons.Length; i++) if (c.seasons[i] == seasonId) return true;
            return false;
        }
    }
}
