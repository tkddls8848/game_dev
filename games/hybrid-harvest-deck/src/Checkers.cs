// 검사기. Phase 를 닫는 것은 "구현했다"가 아니라 "이것들이 통과한다"이다.
// 기계가 판정하는 것(고장)만 여기 있다. 재미는 사람이 본다 — README 의 판정 칸.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridHarvestDeck
{
    /// <summary>한 해를 돌려 얻은 측정값. 검사기들이 공통으로 쓴다.</summary>
    public sealed class TrialSummary
    {
        public int Trials;
        public int Wins;
        public int WinPct => Trials == 0 ? 0 : Wins * 100 / Trials;
        public int AvgDeckSize;
        public int EmptyDeckTrials;
        public Dictionary<string, int> CardPlayCounts = new Dictionary<string, int>();
        public Dictionary<string, int> DeckCardCounts = new Dictionary<string, int>();
    }

    public static class Trials
    {
        /// <summary>
        /// seed 를 바꿔 가며 한 해(밭 → 밤)를 반복한다.
        /// countPlays 를 켜면 카드가 실제로 놓였는지 센다(DeadCard 판정용).
        /// </summary>
        public static TrialSummary RunYear(GameData data, Func<Rng, IPlantingPolicy> planting,
                                           Func<Rng, IPlayPolicy> play, int trials, int baseSeed,
                                           bool countPlays = false, int year = 1)
        {
            var campaign = new Campaign(data);
            var s = new TrialSummary { Trials = trials };
            long deckTotal = 0;

            for (int t = 0; t < trials; t++)
            {
                var rng = new Rng(baseSeed + t);
                var yr = campaign.RunYear(year,
                    Math.Min(data.Balance.economy.seedStipendPerYear, data.Balance.economy.seedCeiling),
                    0, data.StartPlots, planting(rng), play(rng), rng,
                    countPlays ? new StringBuilder() : null);

                deckTotal += yr.Field.Deck.Count;
                if (yr.Field.Deck.Count == 0) s.EmptyDeckTrials++;
                if (yr.Night.Won) s.Wins++;

                foreach (var kv in yr.Field.DeckCounts())
                    s.DeckCardCounts[kv.Key] = s.DeckCardCounts.TryGetValue(kv.Key, out var n) ? n + kv.Value : kv.Value;
            }

            if (countPlays)
            {
                // 놓인 카드는 전투 기록에 'p<카드id>' 로 남는다. 다시 돌려 센다.
                for (int t = 0; t < trials; t++)
                {
                    var rng = new Rng(baseSeed + t);
                    var sb = new StringBuilder();
                    campaign.RunYear(year,
                        Math.Min(data.Balance.economy.seedStipendPerYear, data.Balance.economy.seedCeiling),
                        0, data.StartPlots, planting(rng), play(rng), rng, sb);
                    foreach (var line in sb.ToString().Split('\n'))
                    {
                        if (line.Length < 2 || line[0] != 'p') continue;
                        var id = line.Substring(1);
                        s.CardPlayCounts[id] = s.CardPlayCounts.TryGetValue(id, out var n) ? n + 1 : 1;
                    }
                }
            }

            s.AvgDeckSize = trials == 0 ? 0 : (int)(deckTotal / trials);
            return s;
        }

        /// <summary>
        /// 여러 해를 잇는 캠페인을 여러 씨드로 돌리고 "넘긴 해 / 치른 해"를 잰다.
        /// 한 해만 재면 결정적 정책의 승률이 0% 아니면 100% 로 붙어 구간 자체가 의미를 잃는다 —
        /// 적 체력이 해마다 커지는 축이 있어야 승률이 퍼진다.
        /// </summary>
        public static TrialSummary RunCampaign(GameData data, Func<Rng, IPlantingPolicy> planting,
                                               Func<Rng, IPlayPolicy> play, int trials, int baseSeed, int years)
        {
            var campaign = new Campaign(data);
            var s = new TrialSummary { Trials = trials * years };
            long deckTotal = 0;

            for (int t = 0; t < trials; t++)
            {
                var rng = new Rng(baseSeed + t);
                var run = campaign.Run(years, planting(rng), play(rng), baseSeed + t);
                s.Wins += run.Wins;
                foreach (var y in run.Years) deckTotal += y.Field.Deck.Count;
            }
            s.AvgDeckSize = s.Trials == 0 ? 0 : (int)(deckTotal / s.Trials);
            return s;
        }
    }

    // ── 1. 참조 무결성 ────────────────────────────────────────────────
    public static class DataValidator
    {
        public static List<string> Validate(GameData d)
        {
            var problems = new List<string>();

            foreach (var crop in d.Crops)
            {
                if (!d.HasCard(crop.cardId))
                    problems.Add($"작물 {crop.id} 의 cardId '{crop.cardId}' 가 cards.json 에 없다");
                if (crop.cardsPerHarvest < 1)
                    problems.Add($"작물 {crop.id} 의 cardsPerHarvest 가 {crop.cardsPerHarvest} 다");
                if (crop.growTurns < 1) problems.Add($"작물 {crop.id} 의 growTurns 가 {crop.growTurns} 다");
                if (crop.seasons.Length == 0) problems.Add($"작물 {crop.id} 이 어느 계절에도 자라지 않는다");
                foreach (var s in crop.seasons)
                {
                    if (!d.HasSeason(s)) problems.Add($"작물 {crop.id} 의 계절 '{s}' 가 seasons.json 에 없다");
                    else if (!d.Season(s).plantable) problems.Add($"작물 {crop.id} 이 심을 수 없는 계절 '{s}' 을 가리킨다");
                }
                foreach (var s in crop.seasons)
                    if (d.HasSeason(s) && crop.growTurns > d.Season(s).turns)
                        problems.Add($"작물 {crop.id} 은 {s} 안에 익지 않는다 (growTurns {crop.growTurns} > turns {d.Season(s).turns})");
            }

            foreach (var card in d.Cards)
            {
                if (card.cost < 0) problems.Add($"카드 {card.id} 의 코스트가 음수다");
                if (card.effects.Count == 0) problems.Add($"카드 {card.id} 에 효과가 없다");
                foreach (var e in card.effects)
                {
                    if (Array.IndexOf(KnownEffects, e.type) < 0)
                        problems.Add($"카드 {card.id} 에 모르는 효과 '{e.type}' 이 있다");
                    if (e.amount < 0) problems.Add($"카드 {card.id} 의 효과 수치가 음수다");
                }
            }

            foreach (var e in d.Enemies)
            {
                if (e.hp < 1) problems.Add($"적 {e.id} 의 체력이 {e.hp} 다");
                if (e.pattern.Count == 0) problems.Add($"적 {e.id} 에 행동 패턴이 없다");
                foreach (var a in e.pattern)
                    if (Array.IndexOf(KnownEnemyActions, a.type) < 0)
                        problems.Add($"적 {e.id} 에 모르는 행동 '{a.type}' 이 있다");
            }

            foreach (var b in d.Night.battles)
                if (!d.HasEnemy(b)) problems.Add($"밤 {d.Night.id} 의 전투 '{b}' 가 enemies.json 에 없다");
            if (!d.HasEnemy(d.Night.boss)) problems.Add($"밤 {d.Night.id} 의 보스 '{d.Night.boss}' 가 enemies.json 에 없다");

            if (d.StartPlots > d.Plots.Count) problems.Add("startPlots 가 plots 배열보다 크다");
            if (d.MaxPlots > d.Plots.Count) problems.Add("maxPlots 가 plots 배열보다 크다");
            foreach (var p in d.Plots)
                if (p.fertility <= 0) problems.Add($"밭 {p.id} 의 비옥도가 {p.fertility} 다");

            return problems;
        }

        public static readonly string[] KnownEffects =
            { "damage", "damage_per_remaining", "block", "burn", "draw", "discard", "exhaust" };
        public static readonly string[] KnownEnemyActions = { "attack", "block", "ramp" };
    }

    // ── 2. 연결부: 계절 성립성 ────────────────────────────────────────
    public sealed class SeasonFeasibilityRow
    {
        public string SeasonId;
        public string SeasonName;
        public List<string> CropPool = new List<string>();
        public List<string> CardPool = new List<string>();
        public int AvgDeckSize;
        public int WinPct;
        public string BestStrategy;
        public Dictionary<string, int> StrategyWinPct = new Dictionary<string, int>();
        public bool Passed;
    }

    /// <summary>검사기가 "이 작물들로 어떻게든 될 수 있는가"를 물을 때 쓰는 후보 전략들.</summary>
    public static class CandidateStrategies
    {
        public static Dictionary<string, IPlantingPolicy> For(GameData d, ICollection<string> pool)
        {
            var map = new Dictionary<string, IPlantingPolicy>
            {
                { "균형", new RestrictedPlantingPolicy(new BalancedPlantingPolicy(), pool) },
                { "공격", new RestrictedPlantingPolicy(new BalancedPlantingPolicy(new Dictionary<string, int>
                    { { "card_slash", 45 }, { "card_bulwark", 20 }, { "card_ember", 20 },
                      { "card_sort", 5 }, { "card_harvest", 10 } }), pool) },
                { "수비", new RestrictedPlantingPolicy(new BalancedPlantingPolicy(new Dictionary<string, int>
                    { { "card_slash", 22 }, { "card_bulwark", 38 }, { "card_ember", 15 },
                      { "card_sort", 10 }, { "card_harvest", 15 } }), pool) },
            };
            foreach (var cropId in pool) map["단작:" + cropId] = new MonoCropPlantingPolicy(cropId);
            return map;
        }
    }

    /// <summary>
    /// 각 계절에 심을 수 있는 작물만으로 그 밤을 넘길 수 있는가.
    /// 두 루프가 붙어 있다는 것을 기계가 확인하는 유일한 지점이다.
    /// </summary>
    public static class SeasonFeasibility
    {
        public static List<SeasonFeasibilityRow> Check(GameData d)
        {
            var cfg = d.Balance.seasonFeasibility;
            var rows = new List<SeasonFeasibilityRow>();

            foreach (var season in d.PlantingSeasons)
            {
                var pool = new List<string>();
                var cards = new List<string>();
                foreach (var crop in d.Crops)
                    if (crop.GrowsIn(season.id)) { pool.Add(crop.id); cards.Add(crop.cardId); }

                var row = new SeasonFeasibilityRow
                {
                    SeasonId = season.id,
                    SeasonName = season.nameKo,
                    CropPool = pool,
                    CardPool = cards
                };

                if (pool.Count > 0)
                {
                    // "이 작물들로 넘길 방법이 있는가"를 묻는다 — 한 가지 심기 방식만 재면
                    // 그 방식이 나쁜 것인지 계절이 나쁜 것인지 갈라지지 않는다.
                    foreach (var kv in CandidateStrategies.For(d, pool))
                    {
                        var summary = Trials.RunYear(d, _ => kv.Value, _ => new SkilledPlayPolicy(d),
                                                     cfg.trials, d.Balance.seed + season.order * 1000);
                        row.StrategyWinPct[kv.Key] = summary.WinPct;
                        // 단작은 참고로만 적는다 — 단작으로만 넘어가는 계절은 DominanceChecker 와 어긋난다
                        if (kv.Key.StartsWith("단작:")) continue;
                        if (summary.WinPct > row.WinPct)
                        {
                            row.WinPct = summary.WinPct;
                            row.BestStrategy = kv.Key;
                            row.AvgDeckSize = summary.AvgDeckSize;
                        }
                    }
                }
                row.Passed = row.WinPct >= cfg.minWinPct;
                rows.Add(row);
            }
            return rows;
        }
    }

    // ── 3. 연결부: 밭 ↔ 덱 대응 ───────────────────────────────────────
    public sealed class FieldDeckMappingResult
    {
        public List<string> CardsWithNoCrop = new List<string>();
        public List<string> CropsWithBrokenCard = new List<string>();
        public List<string> CardsNeverPlayed = new List<string>();
        public List<string> CropsBetterRemoved = new List<string>();
        public Dictionary<string, List<string>> CardSources = new Dictionary<string, List<string>>();
        public Dictionary<string, int> CardPlayPct = new Dictionary<string, int>();
        public Dictionary<string, int> ExclusionGainPct = new Dictionary<string, int>();
        public int BaselineWinPct;
        public int BaselineCampaignWinPct;
        public bool Passed =>
            CardsWithNoCrop.Count == 0 && CropsWithBrokenCard.Count == 0 &&
            CardsNeverPlayed.Count == 0 && CropsBetterRemoved.Count == 0;
    }

    /// <summary>
    /// 모든 카드가 어떤 작물에서든 나오는가(상점 없이 얻을 수 없는 카드가 없는가),
    /// 모든 작물이 쓸모 있는 카드를 내는가(심을 이유 없는 작물이 없는가).
    /// </summary>
    public static class FieldDeckMapping
    {
        public static FieldDeckMappingResult Check(GameData d)
        {
            var cfg = d.Balance.fieldDeckMapping;
            var r = new FieldDeckMappingResult();

            // (가) 구조: 카드마다 그것을 내는 작물이 있어야 한다
            foreach (var card in d.Cards) r.CardSources[card.id] = new List<string>();
            foreach (var crop in d.Crops)
            {
                if (!d.HasCard(crop.cardId) || crop.cardsPerHarvest < 1 || crop.seasons.Length == 0)
                { r.CropsWithBrokenCard.Add(crop.id); continue; }
                r.CardSources[crop.cardId].Add(crop.id);
            }
            foreach (var kv in r.CardSources) if (kv.Value.Count == 0) r.CardsWithNoCrop.Add(kv.Key);

            // (나) 측정: 그 카드가 실제로 놓이는가
            var baseline = Trials.RunYear(d,
                _ => new BalancedPlantingPolicy(), _ => new SkilledPlayPolicy(d),
                cfg.trials, d.Balance.seed + 7777, countPlays: true);
            r.BaselineWinPct = baseline.WinPct;
            r.BaselineCampaignWinPct = Trials.RunCampaign(d,
                _ => new BalancedPlantingPolicy(), _ => new SkilledPlayPolicy(d),
                cfg.trials, d.Balance.seed + 7777, cfg.years).WinPct;

            int totalPlays = 0;
            foreach (var kv in baseline.CardPlayCounts) totalPlays += kv.Value;
            foreach (var card in d.Cards)
            {
                int plays = baseline.CardPlayCounts.TryGetValue(card.id, out var n) ? n : 0;
                int pct = totalPlays == 0 ? 0 : plays * 100 / totalPlays;
                r.CardPlayPct[card.id] = pct;
                if (pct < cfg.minCardPlayPct) r.CardsNeverPlayed.Add(card.id);
            }

            // (다) 측정: 그 작물을 빼는 편이 확실히 나은 작물이 있으면 심을 이유가 없는 작물이다
            foreach (var crop in d.Crops)
            {
                var without = new List<string>();
                foreach (var c in d.Crops) if (c.id != crop.id) without.Add(c.id);

                var s = Trials.RunCampaign(d,
                    _ => new RestrictedPlantingPolicy(new BalancedPlantingPolicy(), without),
                    _ => new SkilledPlayPolicy(d),
                    cfg.trials, d.Balance.seed + 7777, cfg.years);

                int gain = s.WinPct - r.BaselineCampaignWinPct;
                r.ExclusionGainPct[crop.id] = gain;
                if (gain > cfg.maxExclusionGainPct) r.CropsBetterRemoved.Add(crop.id);
            }

            return r;
        }
    }

    // ── 4. 씨앗·비료 경제 ─────────────────────────────────────────────
    public sealed class EconomyResult
    {
        public int Years;
        public int MaxSeeds;
        public int MaxFertilizer;
        public int MaxPlots;
        public bool SeedsBounded;
        public bool FertilizerBounded;
        public bool PlotsBounded;
        public bool NoMidYearSeedMinting;
        public bool NoMonotoneDivergence;
        public List<string> Problems = new List<string>();
        public bool Passed => Problems.Count == 0;
    }

    /// <summary>씨앗·비료 경제에 무한 증식 고리가 없는가. 구조 + 200년 시뮬레이션 두 갈래로 본다.</summary>
    public static class EconomyChecker
    {
        public static EconomyResult Check(GameData d)
        {
            var e = d.Balance.economy;
            var r = new EconomyResult { Years = e.economyYears };

            // (가) 구조: 어떤 카드 효과도, 어떤 작물도 씨앗을 만들지 않는다.
            //     씨앗이 느는 지점은 해 경계 하나뿐이어야 한다.
            r.NoMidYearSeedMinting = true;
            foreach (var card in d.Cards)
                foreach (var eff in card.effects)
                    if (eff.type == "seed" || eff.type == "fertilizer")
                    { r.NoMidYearSeedMinting = false; r.Problems.Add($"카드 {card.id} 이 해 도중에 자원을 만든다"); }

            // (나) 시뮬레이션: 잘 두는 사람이 계속 이겨도 자원이 발산하지 않아야 한다
            var campaign = new Campaign(d);
            var run = campaign.Run(e.economyYears, new BalancedPlantingPolicy(), new SkilledPlayPolicy(d), d.Balance.seed);

            r.MaxSeeds = run.MaxSeedsSeen;
            r.MaxFertilizer = run.MaxFertilizerSeen;
            r.MaxPlots = run.MaxPlotsSeen;
            r.SeedsBounded = run.MaxSeedsSeen <= e.seedCeiling;
            r.FertilizerBounded = run.MaxFertilizerSeen <= e.fertilizerCeiling;
            r.PlotsBounded = run.MaxPlotsSeen <= d.MaxPlots;

            if (!r.SeedsBounded) r.Problems.Add($"씨앗이 상한 {e.seedCeiling} 을 넘었다: {run.MaxSeedsSeen}");
            if (!r.FertilizerBounded) r.Problems.Add($"비료가 상한 {e.fertilizerCeiling} 을 넘었다: {run.MaxFertilizerSeen}");
            if (!r.PlotsBounded) r.Problems.Add($"밭이 상한 {d.MaxPlots} 을 넘었다: {run.MaxPlotsSeen}");

            // (다) 발산: 마지막 4분의 1 구간에서 시작 씨앗이 계속 늘기만 하면 고리가 있다
            int from = e.economyYears * 3 / 4;
            bool strictlyIncreasing = true;
            for (int i = from + 1; i < run.Years.Count; i++)
                if (run.Years[i].SeedsAtStart <= run.Years[i - 1].SeedsAtStart) { strictlyIncreasing = false; break; }
            r.NoMonotoneDivergence = !strictlyIncreasing;
            if (strictlyIncreasing) r.Problems.Add("마지막 구간에서 시작 씨앗이 단조 증가한다 — 무한 증식 고리");

            // 한 해 안에서 씨앗은 늘지 않는다
            foreach (var y in run.Years)
                if (y.Field.SeedsLeft > y.SeedsAtStart)
                { r.Problems.Add($"{y.Year}년: 해 도중에 씨앗이 늘었다"); r.NoMidYearSeedMinting = false; break; }

            return r;
        }
    }

    // ── 5. 이길 수 있는가 ─────────────────────────────────────────────
    public sealed class RunSolvabilityResult
    {
        public int WinPct;
        public int RequiredPct;
        public List<string> UnbeatableEnemies = new List<string>();
        public bool Passed => WinPct >= RequiredPct && UnbeatableEnemies.Count == 0;
    }

    public static class RunSolvability
    {
        public static RunSolvabilityResult Check(GameData d)
        {
            var cfg = d.Balance.runSolvability;
            var r = new RunSolvabilityResult { RequiredPct = cfg.minWinPct };

            var summary = Trials.RunYear(d,
                _ => new BalancedPlantingPolicy(), _ => new SkilledPlayPolicy(d),
                cfg.trials, d.Balance.seed + 31);
            r.WinPct = summary.WinPct;

            // 전투 하나씩 따로도 이길 수 있어야 한다 (한 전투가 구조적으로 불가능하면 밤 전체가 사기다)
            var field = new FieldSim(d);
            var battle = new BattleSim(d);
            var deck = field.RunYear(d.Balance.economy.seedStipendPerYear, 0, d.StartPlots, new BalancedPlantingPolicy()).Deck;

            var all = new List<string>(d.Night.battles) { d.Night.boss };
            foreach (var enemyId in all)
            {
                bool anyWin = false;
                for (int t = 0; t < 20 && !anyWin; t++)
                {
                    var res = battle.Run(d.Enemy(enemyId), deck, d.Night.playerHp,
                                         new SkilledPlayPolicy(d), new Rng(d.Balance.seed + t));
                    anyWin = res.PlayerWon;
                }
                if (!anyWin) r.UnbeatableEnemies.Add(enemyId);
            }
            return r;
        }
    }

    // ── 6. 지배 전략 ──────────────────────────────────────────────────
    public sealed class DominanceRow
    {
        public string CropId;
        public string CardId;
        public int WinPct;
        public int AvgDeckSize;
        public bool Passed;
    }

    /// <summary>한 작물만 심는 밭이 설계 한계를 넘는 승률을 내면 덱빌딩이 사라진다.</summary>
    public static class DominanceChecker
    {
        public static List<DominanceRow> Check(GameData d)
        {
            var cfg = d.Balance.dominance;
            var rows = new List<DominanceRow>();
            foreach (var crop in d.Crops)
            {
                var s = Trials.RunCampaign(d,
                    _ => new MonoCropPlantingPolicy(crop.id), _ => new SkilledPlayPolicy(d),
                    cfg.trials, d.Balance.seed + crop.sortOrder * 101, cfg.years);
                rows.Add(new DominanceRow
                {
                    CropId = crop.id,
                    CardId = crop.cardId,
                    WinPct = s.WinPct,
                    AvgDeckSize = s.AvgDeckSize,
                    Passed = s.WinPct <= cfg.monoCropMaxWinPct
                });
            }
            return rows;
        }
    }

    // ── 7. 승률 구간 ──────────────────────────────────────────────────
    public sealed class WinRateBandResult
    {
        public int RandomPct;
        public int SkilledPct;
        public int RandomMin, RandomMax, SkilledMin, SkilledMax;
        public bool RandomInBand => RandomPct >= RandomMin && RandomPct <= RandomMax;
        public bool SkilledInBand => SkilledPct >= SkilledMin && SkilledPct <= SkilledMax;
        public bool Passed => RandomInBand && SkilledInBand;
    }

    public static class WinRateBand
    {
        public static WinRateBandResult Measure(GameData d)
        {
            var cfg = d.Balance.winRateBand;
            var random = Trials.RunCampaign(d,
                rng => new RandomPlantingPolicy(rng), rng => new RandomPlayPolicy(rng),
                cfg.trials, d.Balance.seed + 991, cfg.years);
            var skilled = Trials.RunCampaign(d,
                _ => new BalancedPlantingPolicy(), _ => new SkilledPlayPolicy(d),
                cfg.trials, d.Balance.seed + 991, cfg.years);

            return new WinRateBandResult
            {
                RandomPct = random.WinPct,
                SkilledPct = skilled.WinPct,
                RandomMin = cfg.randomMinPct,
                RandomMax = cfg.randomMaxPct,
                SkilledMin = cfg.skilledMinPct,
                SkilledMax = cfg.skilledMaxPct
            };
        }
    }
}
