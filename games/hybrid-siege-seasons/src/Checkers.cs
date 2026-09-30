// 검사기. Phase 를 닫는 것은 "구현했다"가 아니라 "이것들이 통과한다"이다.
// 기계가 판정하는 것(고장)만 여기 있다. 재미는 사람이 본다 — README 의 판정 칸.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridSiegeSeasons
{
    public sealed class TrialSummary
    {
        public int Trials;             // 치른 해의 수
        public int Wins;               // 네 침입을 모두 막은 해
        public int WinPct => Trials == 0 ? 0 : Wins * 100 / Trials;
        public int AvgDeckSize;
        public int AvgDeckUses;
        public int SiegesHeld;
        public int SiegesTotal;
        public int SiegeHoldPct => SiegesTotal == 0 ? 0 : SiegesHeld * 100 / SiegesTotal;
        public Dictionary<string, int> CardPlayCounts = new Dictionary<string, int>();
    }

    public static class Trials
    {
        /// <summary>성 하나를 years 해 동안 돌린다. 덱이 그 해들을 건너므로 한 해만 재는 것은 뜻이 없다.</summary>
        public static TrialSummary RunCampaign(GameData data, Func<Rng, IPlantingPolicy> planting,
                                               Func<Rng, IPlayPolicy> play, int trials, int baseSeed,
                                               int years, bool countPlays = false)
        {
            var loop = new SeasonLoop(data);
            var s = new TrialSummary { Trials = trials * years };
            long deckTotal = 0, usesTotal = 0;

            for (int t = 0; t < trials; t++)
            {
                var rng = new Rng(baseSeed + t);
                var sb = countPlays ? new StringBuilder() : null;

                var e = data.Balance.economy;
                var f = new Fortress { Seeds = 0, Fertilizer = 0, Plots = data.StartPlots };
                var plantPolicy = planting(rng);
                var playPolicy = play(rng);

                for (int y = 1; y <= years; y++)
                {
                    f.Seeds = Math.Min(f.Seeds + e.seedStipendPerYear, e.seedCeiling);
                    var yr = loop.RunYear(f, y, plantPolicy, playPolicy, rng, sb);
                    if (yr.Held) s.Wins++;
                    foreach (var so in yr.Sieges) { s.SiegesTotal++; if (so.Held) s.SiegesHeld++; }
                    deckTotal += yr.DeckAfter;
                    usesTotal += yr.DeckUsesAfter;
                }

                if (sb != null)
                    foreach (var line in sb.ToString().Split('\n'))
                    {
                        if (line.Length < 2 || line[0] != 'p') continue;
                        int hash = line.IndexOf('#');
                        var id = hash > 1 ? line.Substring(1, hash - 1) : line.Substring(1);
                        s.CardPlayCounts[id] = s.CardPlayCounts.TryGetValue(id, out var n) ? n + 1 : 1;
                    }
            }

            s.AvgDeckSize = s.Trials == 0 ? 0 : (int)(deckTotal / s.Trials);
            s.AvgDeckUses = s.Trials == 0 ? 0 : (int)(usesTotal / s.Trials);
            return s;
        }
    }

    // ── 1. 참조 무결성 ────────────────────────────────────────────────
    public static class DataValidator
    {
        public static readonly string[] KnownEffects =
            { "damage", "damage_per_remaining", "block", "burn", "draw", "exhaust", "repair" };
        public static readonly string[] KnownEnemyActions = { "attack", "block", "ramp" };

        public static List<string> Validate(GameData d)
        {
            var problems = new List<string>();

            foreach (var crop in d.Crops)
            {
                if (!d.HasCard(crop.cardId)) problems.Add($"작물 {crop.id} 의 cardId '{crop.cardId}' 가 cards.json 에 없다");
                if (crop.cardsPerHarvest < 1) problems.Add($"작물 {crop.id} 의 cardsPerHarvest 가 {crop.cardsPerHarvest} 다");
                if (crop.growTurns < 1) problems.Add($"작물 {crop.id} 의 growTurns 가 {crop.growTurns} 다");
                if (crop.seasons.Length == 0) problems.Add($"작물 {crop.id} 이 어느 계절에도 자라지 않는다");
                foreach (var s in crop.seasons)
                {
                    if (!d.HasSeason(s)) problems.Add($"작물 {crop.id} 의 계절 '{s}' 가 seasons.json 에 없다");
                    else if (!d.Season(s).plantable) problems.Add($"작물 {crop.id} 이 심을 수 없는 계절 '{s}' 을 가리킨다");
                    else if (crop.growTurns > d.Season(s).turns)
                        problems.Add($"작물 {crop.id} 은 {s} 안에 익지 않는다 (growTurns {crop.growTurns} > turns {d.Season(s).turns})");
                }
            }

            foreach (var card in d.Cards)
            {
                if (card.cost < 0) problems.Add($"카드 {card.id} 의 코스트가 음수다");
                if (card.durability < 1) problems.Add($"카드 {card.id} 의 durability 가 {card.durability} 다 — 한 번도 못 쓴다");
                if (card.effects.Count == 0) problems.Add($"카드 {card.id} 에 효과가 없다");
                foreach (var e in card.effects)
                {
                    if (Array.IndexOf(KnownEffects, e.type) < 0) problems.Add($"카드 {card.id} 에 모르는 효과 '{e.type}' 이 있다");
                    if (e.amount < 0) problems.Add($"카드 {card.id} 의 효과 수치가 음수다");
                }
            }

            foreach (var e in d.Enemies)
            {
                if (e.hp < 1) problems.Add($"적 {e.id} 의 체력이 {e.hp} 다");
                if (e.pattern.Count == 0) problems.Add($"적 {e.id} 에 행동 패턴이 없다");
                foreach (var a in e.pattern)
                    if (Array.IndexOf(KnownEnemyActions, a.type) < 0) problems.Add($"적 {e.id} 에 모르는 행동 '{a.type}' 이 있다");
            }

            var seen = new HashSet<string>();
            foreach (var s in d.Year.sieges)
            {
                if (!d.HasSeason(s.seasonId)) problems.Add($"침입이 없는 계절 '{s.seasonId}' 을 가리킨다");
                if (!d.HasEnemy(s.enemyId)) problems.Add($"침입의 적 '{s.enemyId}' 가 enemies.json 에 없다");
                if (!seen.Add(s.seasonId)) problems.Add($"계절 '{s.seasonId}' 에 침입이 둘이다");
                if (s.escalationPct < 100) problems.Add($"'{s.seasonId}' 의 escalationPct 가 100 미만이다 — 격화가 거꾸로다");
            }
            foreach (var season in d.Seasons)
                if (d.SiegeOf(season.id) == null) problems.Add($"계절 '{season.id}' 에 침입이 없다 — 빈 계절");

            // 격화는 계절 순서대로 세져야 한다
            int prev = -1;
            var ordered = new List<SeasonDef>(d.Seasons);
            ordered.Sort((a, b) => a.order.CompareTo(b.order));
            foreach (var season in ordered)
            {
                var s = d.SiegeOf(season.id);
                if (s == null) continue;
                if (s.escalationPct <= prev) problems.Add($"'{season.id}' 의 격화가 앞 계절보다 세지 않다");
                prev = s.escalationPct;
            }

            if (d.StartPlots > d.Plots.Count) problems.Add("startPlots 가 plots 배열보다 크다");
            if (d.MaxPlots > d.Plots.Count) problems.Add("maxPlots 가 plots 배열보다 크다");
            foreach (var p in d.Plots) if (p.fertility <= 0) problems.Add($"밭 {p.id} 의 비옥도가 {p.fertility} 다");

            return problems;
        }
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
        public int SiegeHoldPct;
        public string BestStrategy;
        public Dictionary<string, int> StrategyWinPct = new Dictionary<string, int>();
        public bool Passed;
    }

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
    /// 각 계절에 심을 수 있는 작물만으로 그 해의 네 침입을 모두 막을 수 있는가.
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
                    SeasonId = season.id, SeasonName = season.nameKo,
                    CropPool = pool, CardPool = cards
                };

                foreach (var kv in CandidateStrategies.For(d, pool))
                {
                    // 첫 해 하나만 본다 — "이 계절의 작물로 그 밤들을 넘길 수 있는가"가 물음이다
                    var s = Trials.RunCampaign(d, _ => kv.Value, _ => WearAwarePlayPolicy.Skilled(d),
                                               cfg.trials, d.Balance.seed + season.order * 1000, years: 1);
                    row.StrategyWinPct[kv.Key] = s.WinPct;
                    if (kv.Key.StartsWith("단작:")) continue;   // 단작은 참고로만 적는다
                    if (row.BestStrategy == null || s.WinPct > row.WinPct)
                    {
                        row.WinPct = s.WinPct;
                        row.SiegeHoldPct = s.SiegeHoldPct;
                        row.BestStrategy = kv.Key;
                        row.AvgDeckSize = s.AvgDeckSize;
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
        public bool Passed =>
            CardsWithNoCrop.Count == 0 && CropsWithBrokenCard.Count == 0 &&
            CardsNeverPlayed.Count == 0 && CropsBetterRemoved.Count == 0;
    }

    public static class FieldDeckMapping
    {
        public static FieldDeckMappingResult Check(GameData d)
        {
            var cfg = d.Balance.fieldDeckMapping;
            var r = new FieldDeckMappingResult();

            foreach (var card in d.Cards) r.CardSources[card.id] = new List<string>();
            foreach (var crop in d.Crops)
            {
                if (!d.HasCard(crop.cardId) || crop.cardsPerHarvest < 1 || crop.seasons.Length == 0)
                { r.CropsWithBrokenCard.Add(crop.id); continue; }
                r.CardSources[crop.cardId].Add(crop.id);
            }
            foreach (var kv in r.CardSources) if (kv.Value.Count == 0) r.CardsWithNoCrop.Add(kv.Key);

            var baseline = Trials.RunCampaign(d, _ => new BalancedPlantingPolicy(),
                                              _ => WearAwarePlayPolicy.Skilled(d),
                                              cfg.trials, d.Balance.seed + 7777, cfg.years, countPlays: true);
            r.BaselineWinPct = baseline.WinPct;

            int totalPlays = 0;
            foreach (var kv in baseline.CardPlayCounts) totalPlays += kv.Value;
            foreach (var card in d.Cards)
            {
                int plays = baseline.CardPlayCounts.TryGetValue(card.id, out var n) ? n : 0;
                int pct = totalPlays == 0 ? 0 : plays * 100 / totalPlays;
                r.CardPlayPct[card.id] = pct;
                if (pct < cfg.minCardPlayPct) r.CardsNeverPlayed.Add(card.id);
            }

            foreach (var crop in d.Crops)
            {
                var without = new List<string>();
                foreach (var c in d.Crops) if (c.id != crop.id) without.Add(c.id);

                var s = Trials.RunCampaign(d,
                    _ => new RestrictedPlantingPolicy(new BalancedPlantingPolicy(), without),
                    _ => WearAwarePlayPolicy.Skilled(d),
                    cfg.trials, d.Balance.seed + 7777, cfg.years);

                int gain = s.WinPct - r.BaselineWinPct;
                r.ExclusionGainPct[crop.id] = gain;
                if (gain > cfg.maxExclusionGainPct) r.CropsBetterRemoved.Add(crop.id);
            }
            return r;
        }
    }

    // ── 4. 씨앗·비료·덱 경제 ──────────────────────────────────────────
    public sealed class EconomyResult
    {
        public int Years;
        public int MaxSeeds, MaxFertilizer, MaxPlots, MaxDeck;
        public bool NoMidYearSeedMinting;
        public bool NoMonotoneDivergence;
        public List<string> Problems = new List<string>();
        public bool Passed => Problems.Count == 0;
    }

    /// <summary>
    /// 씨앗·비료에 무한 증식 고리가 없는가. 여기서는 **영속 덱**도 같이 본다 —
    /// 카드가 닳는 속도보다 밭이 내는 속도가 빠르면 덱이 끝없이 두꺼워진다.
    /// </summary>
    public static class EconomyChecker
    {
        public static EconomyResult Check(GameData d)
        {
            var e = d.Balance.economy;
            var r = new EconomyResult { Years = e.economyYears, NoMidYearSeedMinting = true };

            foreach (var card in d.Cards)
                foreach (var eff in card.effects)
                    if (eff.type == "seed" || eff.type == "fertilizer")
                    { r.NoMidYearSeedMinting = false; r.Problems.Add($"카드 {card.id} 이 해 도중에 자원을 만든다"); }

            var loop = new SeasonLoop(d);
            var run = loop.Run(e.economyYears, new BalancedPlantingPolicy(), WearAwarePlayPolicy.Skilled(d), d.Balance.seed);

            r.MaxSeeds = run.MaxSeedsSeen;
            r.MaxFertilizer = run.MaxFertilizerSeen;
            r.MaxPlots = run.MaxPlotsSeen;
            r.MaxDeck = run.MaxDeckSeen;

            if (run.MaxSeedsSeen > e.seedCeiling) r.Problems.Add($"씨앗이 상한 {e.seedCeiling} 을 넘었다: {run.MaxSeedsSeen}");
            if (run.MaxFertilizerSeen > e.fertilizerCeiling) r.Problems.Add($"비료가 상한 {e.fertilizerCeiling} 을 넘었다: {run.MaxFertilizerSeen}");
            if (run.MaxPlotsSeen > d.MaxPlots) r.Problems.Add($"밭이 상한 {d.MaxPlots} 을 넘었다: {run.MaxPlotsSeen}");
            if (run.MaxDeckSeen > e.deckCeiling)
                r.Problems.Add($"영속 덱이 상한 {e.deckCeiling} 장을 넘었다: {run.MaxDeckSeen} — 닳는 속도보다 나는 속도가 빠르다");

            int from = e.economyYears * 3 / 4;
            bool seedsUp = true, deckUp = true;
            for (int i = from + 1; i < run.Years.Count; i++)
            {
                if (run.Years[i].SeedsAtStart <= run.Years[i - 1].SeedsAtStart) seedsUp = false;
                if (run.Years[i].DeckAfter <= run.Years[i - 1].DeckAfter) deckUp = false;
            }
            r.NoMonotoneDivergence = !seedsUp && !deckUp;
            if (seedsUp) r.Problems.Add("마지막 구간에서 시작 씨앗이 단조 증가한다 — 무한 증식 고리");
            if (deckUp) r.Problems.Add("마지막 구간에서 덱이 단조 증가한다 — 카드가 닳는 것보다 빨리 는다");

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
        public int SiegeHoldPct;
        public int RequiredPct;
        public List<string> UnbeatableSieges = new List<string>();
        public bool Passed => WinPct >= RequiredPct && UnbeatableSieges.Count == 0;
    }

    public static class RunSolvability
    {
        public static RunSolvabilityResult Check(GameData d)
        {
            var cfg = d.Balance.runSolvability;
            var r = new RunSolvabilityResult { RequiredPct = cfg.minWinPct };

            var s = Trials.RunCampaign(d, _ => new BalancedPlantingPolicy(),
                                       _ => WearAwarePlayPolicy.Skilled(d),
                                       cfg.trials, d.Balance.seed + 31, years: 1);
            r.WinPct = s.WinPct;
            r.SiegeHoldPct = s.SiegeHoldPct;

            // 침입 하나씩 따로도 막을 수 있어야 한다.
            // 첫 해의 밭 하나로 덱을 만들어, 각 침입을 그 덱으로 혼자 붙여 본다.
            var battle = new SiegeBattleSim(d);
            foreach (var siege in d.Year.sieges)
            {
                bool any = false;
                for (int t = 0; t < 12 && !any; t++)
                {
                    var deck = FreshDeck(d, t);
                    if (deck.Count == 0) continue;
                    var baseEnemy = d.Enemy(siege.enemyId);
                    var acc = new PercentAccumulator();
                    var enemy = new EnemyDef
                    {
                        id = baseEnemy.id, nameKo = baseEnemy.nameKo, glyph = baseEnemy.glyph,
                        hp = acc.Apply(baseEnemy.hp, siege.escalationPct), pattern = baseEnemy.pattern
                    };
                    any = battle.Run(enemy, deck, d.Year.playerHp,
                                     WearAwarePlayPolicy.Skilled(d), new Rng(d.Balance.seed + t)).PlayerWon;
                }
                if (!any) r.UnbeatableSieges.Add(siege.seasonId + "/" + siege.enemyId);
            }
            return r;
        }

        /// <summary>한 해 농사분의 덱. 한 침입을 따로 재 볼 때 쓴다.</summary>
        public static StandingDeck FreshDeck(GameData d, int variant)
        {
            var field = new FieldSim(d);
            var year = field.RunYear(d.Balance.economy.seedStipendPerYear, 0, d.StartPlots, new BalancedPlantingPolicy());
            var deck = new StandingDeck();
            foreach (var cardId in year.Deck) deck.Add(d.Card(cardId), 1, 1);
            return deck;
        }
    }

    // ── 6. 지배 전략 — 단작 + '아끼기만 하기' ─────────────────────────
    public sealed class DominanceRow
    {
        public string Label;
        public int WinPct;
        public int AvgDeckSize;
        public int AvgDeckUses;
        public bool Passed;
        public string Why;
    }

    /// <summary>
    /// 단작이 이기지 않는지, 그리고 이 PoC 고유의 위험 — **아끼기만 하는 최적해** — 가 없는지 본다.
    /// 카드가 해를 넘겨 닳으니 "강한 카드를 끝내 쓰지 않는" 쪽이 이기면 게임이 멈춘 것처럼 굴러간다.
    /// </summary>
    public static class DominanceChecker
    {
        public static List<DominanceRow> Check(GameData d)
        {
            var cfg = d.Balance.dominance;
            var rows = new List<DominanceRow>();

            foreach (var crop in d.Crops)
            {
                var s = Trials.RunCampaign(d, _ => new MonoCropPlantingPolicy(crop.id),
                                           _ => WearAwarePlayPolicy.Skilled(d),
                                           cfg.trials, d.Balance.seed + crop.sortOrder * 101, cfg.years);
                rows.Add(new DominanceRow
                {
                    Label = "단작:" + crop.id, WinPct = s.WinPct,
                    AvgDeckSize = s.AvgDeckSize, AvgDeckUses = s.AvgDeckUses,
                    Passed = s.WinPct <= cfg.monoCropMaxWinPct,
                    Why = $"한 작물만 심어도 {s.WinPct}% (한계 {cfg.monoCropMaxWinPct}%)"
                });
            }

            // ── '얼마나 아끼는가' 축을 훑는다 ─────────────────────────
            // 이 PoC 고유의 위험은 여기 있다. 최적이 양 끝이면 손잡이가 장식이다.
            var sweep = cfg.wearSweep;
            var win = new int[sweep.Length];
            int bestAt = 0;
            for (int i = 0; i < sweep.Length; i++)
            {
                int w = sweep[i];
                win[i] = Trials.RunCampaign(d, _ => new BalancedPlantingPolicy(),
                                            _ => new WearAwarePlayPolicy(d, w),
                                            cfg.trials, d.Balance.seed + 555, cfg.years).WinPct;
                if (win[i] > win[bestAt]) bestAt = i;
            }

            for (int i = 0; i < sweep.Length; i++)
                rows.Add(new DominanceRow
                {
                    Label = $"아낌:{sweep[i]}", WinPct = win[i], Passed = true,
                    Why = i == bestAt ? "이 축의 최적" : "기준선"
                });

            int hoardGain = win[sweep.Length - 1] - win[bestAt];
            rows.Add(new DominanceRow
            {
                Label = "아낌:극단이 최적인가",
                WinPct = win[sweep.Length - 1],
                Passed = bestAt != sweep.Length - 1 && hoardGain <= cfg.hoardMaxGainPct,
                Why = $"최적은 아낌={sweep[bestAt]} ({win[bestAt]}%), 극단 아낌={sweep[sweep.Length - 1]} 은 {win[sweep.Length - 1]}%. " +
                      "극단이 최적이면 계획서가 경고한 '아끼기만 하는 지루한 최적해'다"
            });

            // 손잡이가 실제로 무언가를 하는가: 축을 훑은 승률의 폭이 충분해야 한다.
            int lo = win[0], hi = win[0];
            foreach (var w in win) { if (w < lo) lo = w; if (w > hi) hi = w; }
            rows.Add(new DominanceRow
            {
                Label = "아낌축이 무언가를 하는가",
                WinPct = hi - lo,
                Passed = hi - lo >= cfg.minWearSpreadPct,
                Why = $"축을 훑은 승률 폭 {hi - lo}%p (기준 {cfg.minWearSpreadPct}%p 이상). " +
                      "폭이 좁으면 '카드가 닳는다'가 장식이다"
            });

            // 여기는 **판정하지 않고 적기만 한다.** 최적이 0 이면 '적당히 아끼기'가 '그냥 쓰기'보다
            // 낫지 않다는 뜻이고, 그건 고장이 아니라 사람이 봐야 할 설계 물음이다.
            rows.Add(new DominanceRow
            {
                Label = "(참고) 최적이 안쪽에 있는가",
                WinPct = win[bestAt],
                Passed = true,
                Why = bestAt == 0
                    ? $"아니다. 최적이 아낌=0 ({win[0]}%) 이다 — 극단적으로 아끼는 것은 확실히 벌받지만, " +
                      "적당히 아끼는 것이 그냥 쓰는 것보다 낫지는 않다. 사람이 볼 물음이다"
                    : $"그렇다. 최적이 아낌={sweep[bestAt]} ({win[bestAt]}%) 로 양 끝이 아니다"
            });
            return rows;
        }
    }

    // ── 7. 승률 구간 ──────────────────────────────────────────────────
    public sealed class WinRateBandResult
    {
        public int RandomPct, SkilledPct;
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
            var random = Trials.RunCampaign(d, rng => new RandomPlantingPolicy(rng), rng => new RandomPlayPolicy(rng),
                                            cfg.trials, d.Balance.seed + 991, cfg.years);
            var skilled = Trials.RunCampaign(d, _ => new BalancedPlantingPolicy(), _ => WearAwarePlayPolicy.Skilled(d),
                                             cfg.trials, d.Balance.seed + 991, cfg.years);
            return new WinRateBandResult
            {
                RandomPct = random.WinPct, SkilledPct = skilled.WinPct,
                RandomMin = cfg.randomMinPct, RandomMax = cfg.randomMaxPct,
                SkilledMin = cfg.skilledMinPct, SkilledMax = cfg.skilledMaxPct
            };
        }
    }
}
