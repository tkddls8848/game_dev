// 데이터 정의와 적재. 순수 C# — using UnityEngine 금지.
// JSON 은 배열 + 문자열 ID 로 평평하다. 값 없는 int 는 데이터에서 -1 을 명시한다.
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HybridHarvestDeck
{
    public sealed class EffectDef
    {
        public string type;
        public int amount;
        public string target;
    }

    public sealed class CardDef
    {
        public string id;
        public string nameKo;
        public string nameEn;
        public int cost;
        public int sortWeight;
        public string glyph;
        public List<EffectDef> effects = new List<EffectDef>();

        public string Name => Localization.Text("card." + id, nameKo);
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
        public string glyph;
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
        public int seedBudget;   // 한 계절에 쓸 수 있는 씨앗 상한
        public bool plantable;
        public bool isNight;

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
        public string glyph;
        public List<EnemyActionDef> pattern = new List<EnemyActionDef>();

        public string Name => Localization.Text("enemy." + id, nameKo);
    }

    public sealed class NightDef
    {
        public string id;
        public string nameKo;
        public int playerHp;
        public int healBetweenBattles;
        public string[] battles = new string[0];
        public string boss;
    }

    public sealed class BattleBalance
    {
        public int energyPerTurn;
        public int drawPerTurn;
        public int maxTurnsPerBattle;
        public int maxPlaysPerTurn;
        public int maxBurnStacks;
        public int burnTurnsAssumed;
    }

    public sealed class NightBalance
    {
        public int hpGrowthPctPerYear;
        public int hpScaleMaxPct;
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
        public int minCardPlayPct;
        public int maxExclusionGainPct;
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

    public sealed class DominanceBalance
    {
        public int trials;
        public int years;
        public int monoCropMaxWinPct;
    }

    public sealed class BalanceDef
    {
        public int seed;
        public BattleBalance battle;
        public NightBalance night;
        public EconomyBalance economy;
        public TrialBandBalance seasonFeasibility;
        public TrialBandBalance runSolvability;
        public FieldDeckMappingBalance fieldDeckMapping;
        public WinRateBandBalance winRateBand;
        public DominanceBalance dominance;
    }

    /// <summary>모든 데이터. 한 번 읽고 돌려 쓴다.</summary>
    public sealed class GameData
    {
        public List<CardDef> Cards = new List<CardDef>();
        public List<CropDef> Crops = new List<CropDef>();
        public List<SeasonDef> Seasons = new List<SeasonDef>();
        public List<PlotDef> Plots = new List<PlotDef>();
        public List<EnemyDef> Enemies = new List<EnemyDef>();
        public NightDef Night;
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

        /// <summary>심을 수 있는 계절만. order 순.</summary>
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

        /// <summary>
        /// Crops 목록만 좁힌 얕은 사본. 나머지 표는 같은 객체를 가리킨다.
        /// 검사기가 "이 작물만 심을 수 있다면?"을 물을 때 쓴다.
        /// </summary>
        public GameData WithCropSubset(ICollection<string> allowedCropIds)
        {
            var copy = new GameData
            {
                Cards = Cards,
                Seasons = Seasons,
                Plots = Plots,
                Enemies = Enemies,
                Night = Night,
                Balance = Balance,
                StartPlots = StartPlots,
                MaxPlots = MaxPlots,
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
            d.Night = Read<NightDef>(Path.Combine(dataDir, "runs", "night_01.json"));
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

        /// <summary>data/ 의 형제인 PoC 뿌리.</summary>
        public static string Root() => Directory.GetParent(Resolve()).FullName;
    }
}
