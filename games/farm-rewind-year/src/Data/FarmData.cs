// 순수 C#만. using UnityEngine 금지.
// 스키마는 Unity의 JsonUtility가 읽을 수 있는 모양으로 유지한다:
// Dictionary·다형성 없음 · 배열 + 문자열 ID · 값 없는 int는 -1.
using System;
using System.Collections.Generic;

namespace FarmRewindYear.Data
{
    [Serializable] public class ConfigData
    {
        public string slug;
        public string titleKo;
        public int seed;
        public int weatherSeedOffset;
        public int priceSeedOffset;
        public int simAttempts;
        public int foresightGainPerRewind;
    }

    [Serializable] public class WeatherWeight { public string weatherId; public int weight; }

    [Serializable] public class MoonPhaseDef { public string id, nameKo, nameEn; public int dayInCycle; }

    [Serializable] public class SeasonDef
    {
        public string id, nameKo, nameEn;
        public int order, transitionSoilLoss;
        public WeatherWeight[] weatherWeights;
    }

    [Serializable] public class WeatherDef
    {
        public string id, nameKo, nameEn;
        public int waterDelta, growthPercent, growthLossPoints;
    }

    [Serializable] public class SeasonsData
    {
        public int daysPerSeason, moonCycleDays;
        public MoonPhaseDef[] moonPhases;
        public SeasonDef[] seasons;
        public WeatherDef[] weathers;
    }

    [Serializable] public class CropDef
    {
        public string id, nameKo, nameEn;
        public string[] seasons;
        public int growDays, waterPerDay, yieldUnits, soilDrain, minSoil;
    }

    [Serializable] public class CropsData { public CropDef[] crops; }

    [Serializable] public class PlotDef { public string id; public int x, y, soil, startActive; }
    [Serializable] public class ExpansionDef { public int enabled, costBase, costPerPlot, maxExtraPlots, deadlineDayInYear; }
    [Serializable] public class RewindDef { public int soilLossPerRewind, soilFloor, maxRewinds; }

    [Serializable] public class PlotsData
    {
        public int gridWidth, gridHeight, startActivePlots, soilFloorForPlanting;
        public ExpansionDef expansion;
        public RewindDef rewind;
        public PlotDef[] plots;
    }

    [Serializable] public class ShopRow { public string cropId; public int buySeed, sellUnit; }

    [Serializable] public class Limits
    {
        public int assetCeilingCoin, maxAttemptIncomeCoin;
        public int firstYearMarginMaxCoin, wallMinRewinds, wallWithinRewinds;
        public int seasonViabilityMinCrops, seasonViabilityMinCompletableMoneyCrops, seasonViabilityMinIncomeCoin;
        public int deadCropMinPlantings;
    }

    [Serializable] public class EconomyData
    {
        public int startMoney, wellWaterPerDay, livingCostPerSeason, quotaCoin;
        public int priceBandPercent, priceStepDays;
        public ShopRow[] shop;
        public Limits limits;
    }

    [Serializable] public class EventDef
    {
        public string id, nameKo, onSeason, textKo;
        public int onDay, minRewinds, minMoney, once;
    }

    [Serializable] public class EventsData
    {
        public int beatIntervalDays, linesPerEvent, rewindVariantLines, budgetEventCeiling, budgetLineCeiling;
        public EventDef[] events;
    }

    /// 다섯 개의 JSON을 한 덩어리로. 조회는 여기서만 한다.
    public sealed class GameData
    {
        public ConfigData Config;
        public SeasonsData Seasons;
        public CropsData Crops;
        public PlotsData Plots;
        public EconomyData Economy;
        public EventsData Events;

        readonly Dictionary<string, CropDef> _cropById = new Dictionary<string, CropDef>();
        readonly Dictionary<string, ShopRow> _shopByCrop = new Dictionary<string, ShopRow>();
        readonly Dictionary<string, WeatherDef> _weatherById = new Dictionary<string, WeatherDef>();
        readonly Dictionary<string, SeasonDef> _seasonById = new Dictionary<string, SeasonDef>();

        public void Index()
        {
            _cropById.Clear(); _shopByCrop.Clear(); _weatherById.Clear(); _seasonById.Clear();
            foreach (var c in Crops.crops) _cropById[c.id] = c;
            foreach (var s in Economy.shop) _shopByCrop[s.cropId] = s;
            foreach (var w in Seasons.weathers) _weatherById[w.id] = w;
            foreach (var s in Seasons.seasons) _seasonById[s.id] = s;
        }

        public int DaysPerYear => Seasons.daysPerSeason * Seasons.seasons.Length;

        public CropDef Crop(string id) => _cropById.TryGetValue(id, out var c) ? c : null;
        public ShopRow Shop(string cropId) => _shopByCrop.TryGetValue(cropId, out var s) ? s : null;
        public WeatherDef Weather(string id) => _weatherById.TryGetValue(id, out var w) ? w : null;
        public SeasonDef Season(string id) => _seasonById.TryGetValue(id, out var s) ? s : null;

        /// 한 해 안의 일자(0 기반)의 계절.
        public SeasonDef SeasonOfDay(int dayInYear) => Seasons.seasons[dayInYear / Seasons.daysPerSeason];
        public int DayInSeason(int dayInYear) => dayInYear % Seasons.daysPerSeason + 1;

        /// 달의 위상. 주기 안의 날짜를 넘지 않는 가장 늦은 위상을 쓴다.
        /// dayInCycle 이 1부터 시작하므로 항상 하나는 잡힌다.
        public MoonPhaseDef MoonOfDay(int dayInYear)
        {
            int inCycle = dayInYear % Seasons.moonCycleDays + 1;
            MoonPhaseDef best = Seasons.moonPhases[0];
            foreach (var m in Seasons.moonPhases)
                if (m.dayInCycle <= inCycle && m.dayInCycle >= best.dayInCycle) best = m;
            return best;
        }

        public bool CropFitsSeason(CropDef c, string seasonId)
        {
            if (c.seasons == null) return false;
            for (int i = 0; i < c.seasons.Length; i++) if (c.seasons[i] == seasonId) return true;
            return false;
        }
    }
}
