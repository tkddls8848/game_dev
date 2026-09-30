// 데이터 정의와 적재. 순수 C# — using UnityEngine 금지.
// JSON 은 배열 + 문자열 ID 로 평평하다. 값 없는 int 는 데이터에서 -1 을 명시한다.
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HybridSownDeck
{
    public sealed class EffectDef
    {
        public string type;
        public int amount;
        public string target;
    }

    /// <summary>
    /// 밤에 한 번도 놓지 않은 표본을 밭에 심으면 다음 해에 무엇이 되는가.
    /// 이 PoC 를 먼저 만든 둘과 가르는 자리다 — 덱이 밭으로 돌아가는 경로가 여기 하나에 적혀 있다.
    /// </summary>
    public sealed class SowDef
    {
        public string type;      // grow | split | seedfall
        public string becomes;   // 다음 해에 되는 카드. seedfall 이면 빈 문자열
        public int amount;       // 몇 장이 되는가. seedfall 이면 0
        public int seeds;        // seedfall 일 때 생기는 씨앗. 아니면 -1 (값 없는 int 는 -1 을 명시한다)

        public bool YieldsCard => type == "grow" || type == "split";
        public bool YieldsSeeds => type == "seedfall";
    }

    public sealed class CardDef
    {
        public string id;
        public string nameKo;
        public string nameEn;
        public int cost;
        public int sortWeight;
        /// <summary>심었을 때 봄의 밭칸을 묶는 턴 수. 덱 → 밭 경로가 값을 치르는 자리다.</summary>
        public int sownTurns;
        /// <summary>표본집 라벨에 손글씨로 적히는 말. 몇 해살이인지가 여기 적혀 있다.</summary>
        public string labelKo;
        public List<EffectDef> effects = new List<EffectDef>();
        public SowDef sown;

        public string Name => Localization.Text("card." + id, nameKo);
        public string Label => Localization.Text("label." + id, labelKo);
    }

    public sealed class CropDef
    {
        public string id;
        public string nameKo;
        public string nameEn;
        public int growTurns;
        public int waterPerTurn;
        public int seedCost;
        public string[] seasons = new string[0];
        public string cardId;
        public int cardsPerHarvest;
        public int sortOrder;
        public string note;

        public int TotalWater => waterPerTurn * growTurns;
        public bool GrowsIn(string seasonId) => Array.IndexOf(seasons, seasonId) >= 0;
        public string Name => Localization.Text("crop." + id, nameKo);
    }

    public sealed class SeasonDef
    {
        public string id;
        public string nameKo;
        public string nameEn;
        public int order;
        public int turns;
        public int waterBudget;
        public int seedBudget;
        public bool plantable;
        public bool isNight;
        /// <summary>지난 겨울에 심은 표본이 이 계절에 밭칸을 묶는가. 봄만 true 다.</summary>
        public bool sowOccupies;

        public string Name => Localization.Text("season." + id, nameKo);
    }

    public sealed class PlotDef
    {
        public string id;
        public string nameKo;
        public int fertility;   // 백분율 정수. 100 = 기준
        public int unlockYear;  // -1 = 아직 열리지 않음
    }

    public sealed class EnemyActionDef
    {
        public string type;
        public int amount;
    }

    public sealed class EnemyDef
    {
        public string id;
        public string nameKo;
        public string nameEn;
        public int hp;
        public List<EnemyActionDef> pattern = new List<EnemyActionDef>();

        public string Name => Localization.Text("enemy." + id, nameKo);
    }

    public sealed class VisitDef
    {
        public int order;
        public string enemyId;
        public bool isBoss;
    }

    public sealed class YearDef
    {
        public string id;
        public string nameKo;
        public int playerHp;
        public List<VisitDef> visits = new List<VisitDef>();
    }

    public sealed class BattleBalance
    {
        public int energyPerTurn;
        public int drawPerTurn;
        public int maxTurnsPerBattle;
        public int maxPlaysPerTurn;
        public int maxBurnStacks;
        public int burnTurnsAssumed;
        /// <summary>표본을 남겨 둘 값. 클수록 아껴서 심는다.</summary>
        public int sowWeightSpender;
        public int sowWeightSkilled;
        public int sowWeightHoarder;
    }

    public sealed class YearBalance
    {
        public int hpGrowthPctPerYear;
        public int hpScaleMaxPct;
        public int campaignYears;
    }

    public sealed class SowingBalance
    {
        public int plotsSkilled;
        public int plotsNone;
        public int plotsAll;
        /// <summary>seedfall 로 돌아오는 씨앗 하나를 전투 값으로 환산한 수치. 정책의 셈에만 쓴다.</summary>
        public int seedValueForSowing;
    }

    public sealed class EconomyBalance
    {
        public int seedStipendPerYear;
        public int seedCarryCap;
        public int seedWinBonus;
        public int fertilizerCarryCap;
        public int fertilizerWinBonus;
        public int fertilizerYieldBonus;
        public int seedCeiling;
        public int fertilizerCeiling;
        public int deckCeiling;
        /// <summary>한 해에 밭으로 돌아갈 수 있는 표본의 상한. 갈라지는 카드의 증식을 막는 자리다.</summary>
        public int sownCeiling;
        public int economyYears;
    }

    public sealed class TrialBandBalance
    {
        public int trials;
        public int minWinPct;
    }

    public sealed class FieldDeckMappingBalance
    {
        public int trials;
        public int years;
        public int campaigns;
        public int minCardPlayPct;
        public int maxExclusionGainPct;
    }

    public sealed class LoopClosureBalance
    {
        public int trials;
        public int years;
        /// <summary>세 해차 덱에서 '심어서 온 표본'이 차지해야 하는 최소 몫(%). 경로가 실제로 흐르는가.</summary>
        public int minSownSharePct;
        /// <summary>심어서 들어온 몫 가운데 덱 구성 변화로 남아야 하는 비율(%). 고정 한계가 아니라 흘러든 양에 비례한다.</summary>
        public int minShiftAsPctOfSownShare;
        /// <summary>이은 세계의 최적 설정을 끊은 세계에서 돌렸을 때의 승률 차이 하한.</summary>
        public int minWinGapPct;
        /// <summary>이은 세계에서 '몇 칸을 심는가' 축이 만드는 승률 폭의 하한.</summary>
        public int minSowKnobSpreadPct;
        public int[] sowPlotSweep = new int[0];
    }

    public sealed class SowingNotAlwaysBestBalance
    {
        public int trials;
        public int years;
        /// <summary>극단(전부 심고 아무것도 안 쓴다)이 준최적보다 이만큼 넘게 이기면 밤이 의미를 잃는다.</summary>
        public int maxExtremeGainPct;
        /// <summary>안쪽 최적이 양 끝보다 이만큼은 나아야 한다.</summary>
        public int minInteriorGainPct;
    }

    public sealed class DominanceBalance
    {
        public int trials;
        public int years;
        public int monoCropMaxWinPct;
        public int[] sowWeightSweep = new int[0];
        public int[] sowPlotSweep = new int[0];
        public int minSowPlotSpreadPct;
    }

    public sealed class WinRateBandBalance
    {
        public int trials;
        public int years;
        public int randomMinPct;
        public int randomMaxPct;
        public int skilledMinPct;
        public int skilledMaxPct;
    }

    public sealed class BalanceDef
    {
        public int seed;
        public BattleBalance battle;
        public YearBalance year;
        public SowingBalance sowing;
        public EconomyBalance economy;
        public TrialBandBalance seasonFeasibility;
        public TrialBandBalance runSolvability;
        public FieldDeckMappingBalance fieldDeckMapping;
        public LoopClosureBalance loopClosure;
        public SowingNotAlwaysBestBalance sowingNotAlwaysBest;
        public DominanceBalance dominance;
        public WinRateBandBalance winRateBand;
    }

    public sealed class GameData
    {
        public List<CardDef> Cards = new List<CardDef>();
        public List<CropDef> Crops = new List<CropDef>();
        public List<SeasonDef> Seasons = new List<SeasonDef>();
        public List<PlotDef> Plots = new List<PlotDef>();
        public List<EnemyDef> Enemies = new List<EnemyDef>();
        public YearDef Year;
        public BalanceDef Balance;
        public int StartPlots;
        public int MaxPlots;

        readonly Dictionary<string, CardDef> _cards = new Dictionary<string, CardDef>();
        readonly Dictionary<string, CropDef> _crops = new Dictionary<string, CropDef>();
        readonly Dictionary<string, EnemyDef> _enemies = new Dictionary<string, EnemyDef>();
        readonly Dictionary<string, SeasonDef> _seasons = new Dictionary<string, SeasonDef>();

        public CardDef Card(string id) => _cards[id];
        public CropDef Crop(string id) => _crops[id];
        public EnemyDef Enemy(string id) => _enemies[id];
        public SeasonDef Season(string id) => _seasons[id];
        public bool HasCard(string id) => _cards.ContainsKey(id);
        public bool HasEnemy(string id) => _enemies.ContainsKey(id);
        public bool HasSeason(string id) => _seasons.ContainsKey(id);

        public List<SeasonDef> PlantingSeasons
        {
            get
            {
                var list = new List<SeasonDef>();
                foreach (var s in Seasons) if (s.plantable) list.Add(s);
                list.Sort((a, b) => a.order.CompareTo(b.order));
                return list;
            }
        }

        public List<VisitDef> OrderedVisits
        {
            get
            {
                var list = new List<VisitDef>(Year.visits);
                list.Sort((a, b) => a.order.CompareTo(b.order));
                return list;
            }
        }

        /// <summary>Crops 목록만 좁힌 얕은 사본. 검사기가 "이 작물만이라면?"을 물을 때 쓴다.</summary>
        public GameData WithCropSubset(ICollection<string> allowedCropIds)
        {
            var copy = new GameData
            {
                Cards = Cards, Seasons = Seasons, Plots = Plots, Enemies = Enemies,
                Year = Year, Balance = Balance, StartPlots = StartPlots, MaxPlots = MaxPlots,
                Crops = new List<CropDef>()
            };
            foreach (var c in Crops) if (allowedCropIds.Contains(c.id)) copy.Crops.Add(c);
            foreach (var c in Cards) copy._cards[c.id] = c;
            foreach (var c in Crops) copy._crops[c.id] = c;   // 조회는 전체를 유지한다
            foreach (var e in Enemies) copy._enemies[e.id] = e;
            foreach (var s in Seasons) copy._seasons[s.id] = s;
            return copy;
        }

        public static GameData Load(string dataDir)
        {
            var d = new GameData();
            d.Cards = Read<CardsFile>(Path.Combine(dataDir, "cards.json")).cards;
            d.Crops = Read<CropsFile>(Path.Combine(dataDir, "crops.json")).crops;
            d.Seasons = Read<SeasonsFile>(Path.Combine(dataDir, "seasons.json")).seasons;
            var pf = Read<PlotsFile>(Path.Combine(dataDir, "plots.json"));
            d.Plots = pf.plots; d.StartPlots = pf.startPlots; d.MaxPlots = pf.maxPlots;
            d.Enemies = Read<EnemiesFile>(Path.Combine(dataDir, "enemies.json")).enemies;
            d.Year = Read<YearDef>(Path.Combine(dataDir, "runs", "year_01.json"));
            d.Balance = Read<BalanceDef>(Path.Combine(dataDir, "balance.json"));

            foreach (var c in d.Cards) d._cards[c.id] = c;
            foreach (var c in d.Crops) d._crops[c.id] = c;
            foreach (var e in d.Enemies) d._enemies[e.id] = e;
            foreach (var s in d.Seasons) d._seasons[s.id] = s;

            Localization.LoadOverlay(Path.Combine(dataDir, "locale_en.json"));
            return d;
        }

        public static GameData LoadDefault() => Load(DataLocator.Resolve());

        static T Read<T>(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("데이터 파일이 없다: " + path, path);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        sealed class CardsFile { public List<CardDef> cards = new List<CardDef>(); }
        sealed class CropsFile { public List<CropDef> crops = new List<CropDef>(); }
        sealed class SeasonsFile { public List<SeasonDef> seasons = new List<SeasonDef>(); }
        sealed class PlotsFile { public int startPlots = 0; public int maxPlots = 0; public List<PlotDef> plots = new List<PlotDef>(); }
        sealed class EnemiesFile { public List<EnemyDef> enemies = new List<EnemyDef>(); }
    }

    /// <summary>실행 폴더에서 위로 올라가며 이 PoC 의 data/ 를 찾는다. 테스트 프레임워크에 의존하지 않는다.</summary>
    public static class DataLocator
    {
        public static string Resolve()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "data");
                if (File.Exists(Path.Combine(candidate, "crops.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/ 를 찾지 못했다. 시작점: " + AppContext.BaseDirectory);
        }

        public static string Root() => Directory.GetParent(Resolve()).FullName;
    }
}
