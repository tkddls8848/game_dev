// 순수 C#만. using UnityEngine 금지.
// 스키마는 Unity의 JsonUtility가 읽을 수 있는 모양으로 유지한다:
// Dictionary·다형성 없음 · 배열 + 문자열 ID · 값 없는 int는 -1.
using System;
using System.Collections.Generic;

namespace FarmErosion.Data
{
    [Serializable] public class ConfigData
    {
        public string slug;
        public string titleKo;
        public int seed;
        public int weatherSeedOffset;
        public int priceSeedOffset;
        public int simYears;
    }

    [Serializable] public class WeatherWeight { public string weatherId; public int weight; }

    [Serializable] public class SeasonDef
    {
        public string id, nameKo, nameEn;
        public int order, erosionPerDay, transitionErosion, transitionSoilLoss;
        public WeatherWeight[] weatherWeights;
    }

    [Serializable] public class WeatherDef
    {
        public string id, nameKo, nameEn;
        public int waterDelta, erosionDelta, growthPercent;
    }

    [Serializable] public class SeasonsData
    {
        public int daysPerSeason;
        public SeasonDef[] seasons;
        public WeatherDef[] weathers;
    }

    [Serializable] public class CropDef
    {
        public string id, nameKo, nameEn;
        public string[] seasons;
        public int growDays, waterPerDay, yieldUnits, soilDrain, minSoil, erosionGuard;
    }

    [Serializable] public class CropsData { public CropDef[] crops; }

    [Serializable] public class PlotDef { public string id; public int x, y, soil, startErosion; }
    [Serializable] public class DefenseDef { public int cost, costPerOpenSide, erosionReduction, durationDays; }
    [Serializable] public class ExpansionDef { public int enabled, costBase, costPerPlot; }

    [Serializable] public class PlotsData
    {
        public int gridWidth, gridHeight, lossThresholdErosion, bareErosionBonus,
                   edgeErosionBonus, neighborLossErosion, erosionPressurePerYear, soilFloorForPlanting;
        public DefenseDef defense;
        public SalvageDef salvage;
        public WaterPriorityDef waterPriority;
        public ExpansionDef expansion;
        public int reclaimCost;
        public PlotDef[] plots;
    }

    /// <summary>미리 접을 때 돌려받는 것. 그냥 두고 무너지면 0 이다.</summary>
    [Serializable] public class SalvageDef
    {
        public int enabled;
        public int coinPerPlot;              // 밭 하나를 접으면 받는 기본값
        public int coinPerDefenseDay;        // 방벽이 남아 있으면 남은 날마다 더
        public int soilShareToNeighbors;     // 흙의 몇 %가 이웃 토질로 가는가
    }

    /// <summary>물은 모자란다. 어느 칸부터 적실지 사람이 정한다.</summary>
    [Serializable] public class WaterPriorityDef { public int enabled; }

    [Serializable] public class ShopRow { public string cropId; public int buySeed, sellUnit; }

    [Serializable] public class Limits
    {
        public int assetCeilingCoin, maxYearIncomeCoin, quietTailYears;
        public int firstLossYearMin, firstLossYearMax;
        public int halfLandYearMin, halfLandYearMax;
        public int allLostYearMin, allLostYearMax;
        public int seasonViabilityMinCrops, seasonViabilityMinIncomeCoin, seasonViabilityMinCompletableMoneyCrops;
        public int deadCropMinPlantings, peakYearMaxRatio;
    }

    [Serializable] public class EconomyData
    {
        public int startMoney, wellWaterPerDay, landValueCoinPerSoil, priceBandPercent, priceStepDays;
        public int livingCostPerSeason, taxPerActivePlotPerYear, bankruptcyAtCoin;
        public ShopRow[] shop;
        public Limits limits;
    }

    [Serializable] public class EventDef
    {
        public string id, nameKo, onSeason, textKo;
        public int onYear, onDay, minPlotsLost, minMoney, once;
    }

    [Serializable] public class EventsData
    {
        public int beatIntervalDays, linesPerEvent, budgetEventCeiling, budgetLineCeiling;
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
        /// <summary>이동·사람·의뢰. 없을 수도 있다(이동을 끈 변형).</summary>
        public TravelDataFile Travel;
        /// <summary>난이도. 없으면 전부 100% 로 본다(= 보통).</summary>
        public DifficultyDataFile Difficulty;

        readonly Dictionary<string, CropDef> _cropById = new Dictionary<string, CropDef>();
        readonly Dictionary<string, ShopRow> _shopByCrop = new Dictionary<string, ShopRow>();
        readonly Dictionary<string, WeatherDef> _weatherById = new Dictionary<string, WeatherDef>();
        readonly Dictionary<string, SeasonDef> _seasonById = new Dictionary<string, SeasonDef>();
        readonly Dictionary<string, RegionDef> _regionById = new Dictionary<string, RegionDef>();
        readonly Dictionary<string, ErrandDef> _errandById = new Dictionary<string, ErrandDef>();

        public void Index()
        {
            _cropById.Clear(); _shopByCrop.Clear(); _weatherById.Clear(); _seasonById.Clear();
            _regionById.Clear(); _errandById.Clear();
            foreach (var c in Crops.crops) _cropById[c.id] = c;
            foreach (var s in Economy.shop) _shopByCrop[s.cropId] = s;
            foreach (var w in Seasons.weathers) _weatherById[w.id] = w;
            foreach (var s in Seasons.seasons) _seasonById[s.id] = s;
            if (Travel != null)
            {
                if (Travel.regions != null) foreach (var r in Travel.regions) _regionById[r.id] = r;
                if (Travel.errands != null) foreach (var e in Travel.errands) _errandById[e.id] = e;
            }
        }

        public int DaysPerYear => Seasons.daysPerSeason * Seasons.seasons.Length;

        // **id 가 null 이면 '없는 것'이지 오류가 아니다.** Dictionary.TryGetValue 는 null 키에
        // ArgumentNullException 을 던진다 — 의뢰를 받지 않고 그냥 들르는 경우(errandId == null)가
        // 정상인데 화면이 매 프레임 예외로 무너졌다. 한 번 겪었다.
        public CropDef Crop(string id) => id != null && _cropById.TryGetValue(id, out var c) ? c : null;
        public ShopRow Shop(string cropId) => cropId != null && _shopByCrop.TryGetValue(cropId, out var s) ? s : null;
        public WeatherDef Weather(string id) => id != null && _weatherById.TryGetValue(id, out var w) ? w : null;
        public SeasonDef Season(string id) => id != null && _seasonById.TryGetValue(id, out var s) ? s : null;
        public RegionDef Region(string id) => id != null && _regionById.TryGetValue(id, out var r) ? r : null;

        /// <summary>난이도 한 칸. 이름이 없거나 표가 없으면 **보통과 같은 눈금**을 돌려준다.</summary>
        public DifficultyLevel Level(string id)
        {
            if (Difficulty != null && Difficulty.levels != null)
            {
                foreach (var l in Difficulty.levels) if (l.id == id) return l;
                foreach (var l in Difficulty.levels) if (l.id == Difficulty.defaultId) return l;
            }
            return NeutralLevel;
        }

        /// <summary>표가 없을 때의 기준. 아무것도 바꾸지 않는다.</summary>
        public static DifficultyLevel NeutralLevel => new DifficultyLevel
        {
            id = "normal", nameKo = "보통", nameEn = "Just barely", captionKo = "",
            startCoinPercent = 100, yieldPercent = 100,
            defenseCostPercent = 100, defenseDurationPercent = 100,
            bankruptcyAtCoin = -400, erosionPerDayBonus = 0,
        };
        public ErrandDef Errand(string id) => id != null && _errandById.TryGetValue(id, out var e) ? e : null;

        /// 절대 일자(0 기반) 의 계절. 계절 순서는 order가 아니라 배열 순서를 따른다.
        public SeasonDef SeasonOfDay(int absoluteDay)
        {
            int inYear = absoluteDay % DaysPerYear;
            return Seasons.seasons[inYear / Seasons.daysPerSeason];
        }

        public int YearOfDay(int absoluteDay) => absoluteDay / DaysPerYear + 1;
        public int DayInSeason(int absoluteDay) => absoluteDay % Seasons.daysPerSeason + 1;

        public bool CropFitsSeason(CropDef c, string seasonId)
        {
            if (c.seasons == null) return false;
            for (int i = 0; i < c.seasons.Length; i++) if (c.seasons[i] == seasonId) return true;
            return false;
        }
    }
}
