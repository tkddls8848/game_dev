// 검사기. Phase 를 닫는 것은 "구현했다"가 아니라 "이것들이 통과한다"이다.
// 기계가 판정하는 것(고장)만 여기 있다. 재미는 사람이 본다 — README 의 판정 칸.
//
// 이 PoC 의 핵은 LoopClosesBothWays 다. 나머지 일곱은 그것이 뜻을 가지도록 받치는 것들이다.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridSownDeck
{
    public sealed class TrialSummary
    {
        public int Trials;                 // 치른 해의 수
        public int Wins;
        public int WinPct => Trials == 0 ? 0 : Wins * 100 / Trials;
        public int AvgDeckSize;
        public int AvgUnplayed;
        public int AvgCardsFromSown;
        public int TotalSeedsFromSown;
        public int TotalSownForNextYear;
        public Dictionary<string, int> CardPlayCounts = new Dictionary<string, int>();
        /// <summary>마지막 해 덱의 카드별 장수 합. 두 세계의 구성을 견주는 데 쓴다.</summary>
        public Dictionary<string, int> FinalDeckTotals = new Dictionary<string, int>();
        /// <summary>마지막 해 덱에서 '심어서 온 표본'(씨앗으로 기른 것이 아닌)이 차지하는 몫(%).</summary>
        public int SownShareOfDeckPct;
        public Dictionary<string, int> SownTypeTotals = new Dictionary<string, int>();
        public int MaxDeckSeen;
        public int MaxSownSeen;
    }

    public static class Trials
    {
        public static TrialSummary RunCampaign(GameData data, WorldRules world,
                                               Func<Rng, IPlantingPolicy> planting,
                                               Func<Rng, IPlayPolicy> play,
                                               Func<Rng, ISowPolicy> sowing,
                                               int trials, int baseSeed, int years, bool countPlays = false)
        {
            var loop = new YearLoop(data);
            var s = new TrialSummary { Trials = trials * years };
            long deckTotal = 0, unplayedTotal = 0, fromSownTotal = 0;
            long finalTotal = 0, finalSown = 0;

            for (int t = 0; t < trials; t++)
            {
                var rng = new Rng(baseSeed + t);
                var sb = countPlays ? new StringBuilder() : null;
                var run = loop.Run(years, world, planting(rng), play(rng), sowing(rng), baseSeed + t, sb);

                s.Wins += run.Wins;
                if (run.MaxDeckSeen > s.MaxDeckSeen) s.MaxDeckSeen = run.MaxDeckSeen;
                if (run.MaxSownSeen > s.MaxSownSeen) s.MaxSownSeen = run.MaxSownSeen;

                foreach (var y in run.Years)
                {
                    deckTotal += y.DeckSize;
                    unplayedTotal += y.UnplayedAfterNight;
                    fromSownTotal += y.CardsFromSown;
                    s.TotalSeedsFromSown += y.SeedsFromSown;
                    s.TotalSownForNextYear += y.SownForNextYear;
                    foreach (var kv in y.SownTypeCounts)
                        s.SownTypeTotals[kv.Key] = s.SownTypeTotals.TryGetValue(kv.Key, out var n) ? n + kv.Value : kv.Value;
                }

                var last = run.Years[run.Years.Count - 1];
                foreach (var kv in last.DeckCounts)
                    s.FinalDeckTotals[kv.Key] = s.FinalDeckTotals.TryGetValue(kv.Key, out var m) ? m + kv.Value : kv.Value;
                foreach (var sp in last.Field.Deck.Cards)
                { finalTotal++; if (string.IsNullOrEmpty(sp.SourceCropId)) finalSown++; }

                if (sb != null)
                    foreach (var line in sb.ToString().Split('\n'))
                    {
                        if (line.Length < 2 || line[0] != 'p') continue;
                        var id = line.Substring(1);
                        s.CardPlayCounts[id] = s.CardPlayCounts.TryGetValue(id, out var n) ? n + 1 : 1;
                    }
            }

            s.AvgDeckSize = s.Trials == 0 ? 0 : (int)(deckTotal / s.Trials);
            s.AvgUnplayed = s.Trials == 0 ? 0 : (int)(unplayedTotal / s.Trials);
            s.AvgCardsFromSown = s.Trials == 0 ? 0 : (int)(fromSownTotal / s.Trials);
            s.SownShareOfDeckPct = finalTotal == 0 ? 0 : (int)(finalSown * 100 / finalTotal);
            return s;
        }

        /// <summary>준최적 한 벌. 검사기들이 기준선으로 계속 쓴다.</summary>
        public static Func<Rng, IPlayPolicy> SkilledPlay(GameData d) => _ => SowAwarePlayPolicy.Skilled(d);
        public static Func<Rng, ISowPolicy> SkilledSow(GameData d) => _ => new GreedySowPolicy(d.Balance.sowing.plotsSkilled);
        public static Func<Rng, IPlantingPolicy> BalancedPlant() => _ => new BalancedPlantingPolicy();
    }

    // ── 1. 참조 무결성 ────────────────────────────────────────────────
    public static class DataValidator
    {
        public static readonly string[] KnownEffects = { "damage", "block", "burn" };
        public static readonly string[] KnownEnemyActions = { "attack", "block", "ramp" };
        public static readonly string[] KnownSowTypes = { "grow", "split", "seedfall" };

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

            // 심은 표본이 돋아나는 계절. 한 계절만 sowOccupies 여야 한다 — 봄이다.
            var sowSeasons = new List<SeasonDef>();
            foreach (var s in d.Seasons) if (s.sowOccupies) sowSeasons.Add(s);
            if (sowSeasons.Count != 1)
                problems.Add($"sowOccupies 인 계절이 {sowSeasons.Count} 개다 — 심은 표본이 돋아나는 계절은 하나여야 한다");
            int sowSeasonTurns = sowSeasons.Count == 1 ? sowSeasons[0].turns : 0;

            foreach (var card in d.Cards)
            {
                if (card.cost < 0) problems.Add($"카드 {card.id} 의 코스트가 음수다");
                if (card.effects.Count == 0) problems.Add($"카드 {card.id} 에 효과가 없다");
                foreach (var e in card.effects)
                {
                    if (Array.IndexOf(KnownEffects, e.type) < 0) problems.Add($"카드 {card.id} 에 모르는 효과 '{e.type}' 이 있다");
                    if (e.amount < 0) problems.Add($"카드 {card.id} 의 효과 수치가 음수다");
                }

                // ── 덱 → 밭 규칙 ──────────────────────────────────────
                if (card.sown == null) { problems.Add($"카드 {card.id} 에 sown 규칙이 없다 — 심을 수 없는 표본이 있으면 고리가 새는 곳이 생긴다"); continue; }
                var r = card.sown;
                if (Array.IndexOf(KnownSowTypes, r.type) < 0) problems.Add($"카드 {card.id} 의 sown.type '{r.type}' 을 모른다");
                if (card.sownTurns < 1) problems.Add($"카드 {card.id} 의 sownTurns 가 {card.sownTurns} 다 — 심은 표본이 밭칸을 먹지 않는다");
                if (card.sownTurns > sowSeasonTurns)
                    problems.Add($"카드 {card.id} 의 sownTurns {card.sownTurns} 가 봄 turns {sowSeasonTurns} 보다 크다 — 영영 돋지 않는다");
                if (r.YieldsCard)
                {
                    if (string.IsNullOrEmpty(r.becomes) || !d.HasCard(r.becomes))
                        problems.Add($"카드 {card.id} 의 sown.becomes '{r.becomes}' 가 cards.json 에 없다");
                    if (r.amount < 1) problems.Add($"카드 {card.id} 의 sown.amount 가 {r.amount} 다");
                    if (r.type == "grow" && r.amount != 1) problems.Add($"카드 {card.id}: grow 는 한 장이 한 장이 되는 것이다 (amount {r.amount})");
                    if (r.type == "split" && r.amount < 2) problems.Add($"카드 {card.id}: split 은 둘 이상으로 갈라지는 것이다 (amount {r.amount})");
                    if (r.seeds > 0) problems.Add($"카드 {card.id}: 카드가 되는 sown 은 씨앗을 내지 않는다 (seeds {r.seeds})");
                }
                if (r.YieldsSeeds)
                {
                    if (r.seeds < 1) problems.Add($"카드 {card.id}: seedfall 인데 seeds 가 {r.seeds} 다");
                    if (!string.IsNullOrEmpty(r.becomes)) problems.Add($"카드 {card.id}: seedfall 은 카드를 남기지 않는다 (becomes '{r.becomes}')");
                }
            }

            foreach (var e in d.Enemies)
            {
                if (e.hp < 1) problems.Add($"적 {e.id} 의 체력이 {e.hp} 다");
                if (e.pattern.Count == 0) problems.Add($"적 {e.id} 에 행동 패턴이 없다");
                foreach (var a in e.pattern)
                    if (Array.IndexOf(KnownEnemyActions, a.type) < 0) problems.Add($"적 {e.id} 에 모르는 행동 '{a.type}' 이 있다");
            }

            var seenOrder = new HashSet<int>();
            foreach (var v in d.Year.visits)
            {
                if (!d.HasEnemy(v.enemyId)) problems.Add($"침입의 적 '{v.enemyId}' 가 enemies.json 에 없다");
                if (!seenOrder.Add(v.order)) problems.Add($"침입 순서 {v.order} 가 둘이다");
            }
            if (d.Year.visits.Count == 0) problems.Add("겨울밤에 오는 것이 없다");
            if (d.Year.playerHp < 1) problems.Add($"playerHp 가 {d.Year.playerHp} 다");

            int nights = 0;
            foreach (var s in d.Seasons) if (s.isNight) nights++;
            if (nights != 1) problems.Add($"밤인 계절이 {nights} 개다 — 한 해에 하룻밤이다");

            if (d.StartPlots > d.Plots.Count) problems.Add("startPlots 가 plots 배열보다 크다");
            if (d.MaxPlots > d.Plots.Count) problems.Add("maxPlots 가 plots 배열보다 크다");
            if (d.Balance.economy.sownCeiling > d.Plots.Count)
                problems.Add("sownCeiling 이 밭 칸 수보다 크다 — 심은 표본은 밭칸에 앉는다");
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
                    { { "card_sprout", 40 }, { "card_bramble", 28 }, { "card_broadleaf", 12 },
                      { "card_thicket", 5 }, { "card_ember", 15 } }), pool) },
                { "수비", new RestrictedPlantingPolicy(new BalancedPlantingPolicy(new Dictionary<string, int>
                    { { "card_sprout", 18 }, { "card_bramble", 14 }, { "card_broadleaf", 42 },
                      { "card_thicket", 16 }, { "card_ember", 10 } }), pool) },
            };
            foreach (var cropId in pool) map["단작:" + cropId] = new MonoCropPlantingPolicy(cropId);
            return map;
        }
    }

    /// <summary>
    /// 각 계절에 심을 수 있는 작물만으로 그 해의 밤을 넘길 수 있는가.
    /// hybrid-harvest-deck 에서 **두 번 실패해 설계를 바꾸게 한** 검사기다.
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
                    // 첫 해 하나만 본다 — 심어 둔 표본의 도움 없이 그 계절의 작물만으로 되는가가 물음이다.
                    var s = Trials.RunCampaign(d, WorldRules.Connected(), _ => kv.Value,
                                               Trials.SkilledPlay(d), _ => new NoSowPolicy(),
                                               cfg.trials, d.Balance.seed + season.order * 1000, years: 1);
                    row.StrategyWinPct[kv.Key] = s.WinPct;
                    if (kv.Key.StartsWith("단작:")) continue;   // 단작은 참고로만 적는다
                    if (row.BestStrategy == null || s.WinPct > row.WinPct)
                    {
                        row.WinPct = s.WinPct;
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
        public List<string> CardsWithNoFieldPath = new List<string>();
        public List<string> CropsWithBrokenCard = new List<string>();
        public List<string> CardsNeverPlayed = new List<string>();
        public List<string> CropsBetterRemoved = new List<string>();
        public List<string> ShopLikeSources = new List<string>();
        /// <summary>카드마다 밭에서 오는 길. "기름:작물" 과 "심기:카드" 두 종류뿐이다.</summary>
        public Dictionary<string, List<string>> CardSources = new Dictionary<string, List<string>>();
        public Dictionary<string, int> CardPlayPct = new Dictionary<string, int>();
        public Dictionary<string, int> ExclusionGainPct = new Dictionary<string, int>();
        public int BaselineWinPct;
        public bool Passed =>
            CardsWithNoFieldPath.Count == 0 && CropsWithBrokenCard.Count == 0 &&
            CardsNeverPlayed.Count == 0 && CropsBetterRemoved.Count == 0 && ShopLikeSources.Count == 0;
    }

    public static class FieldDeckMapping
    {
        public static FieldDeckMappingResult Check(GameData d)
        {
            var cfg = d.Balance.fieldDeckMapping;
            var r = new FieldDeckMappingResult();

            foreach (var card in d.Cards) r.CardSources[card.id] = new List<string>();

            // 길 1: 씨앗으로 길러 거둔다
            foreach (var crop in d.Crops)
            {
                if (!d.HasCard(crop.cardId) || crop.cardsPerHarvest < 1 || crop.seasons.Length == 0)
                { r.CropsWithBrokenCard.Add(crop.id); continue; }
                r.CardSources[crop.cardId].Add("기름:" + crop.id);
            }
            // 길 2: 쓰지 않은 표본을 심는다 — 이 길도 밭이다
            foreach (var card in d.Cards)
            {
                var rule = card.sown;
                if (rule == null || !rule.YieldsCard || string.IsNullOrEmpty(rule.becomes)) continue;
                if (!d.HasCard(rule.becomes)) continue;
                r.CardSources[rule.becomes].Add("심기:" + card.id);
            }
            foreach (var kv in r.CardSources) if (kv.Value.Count == 0) r.CardsWithNoFieldPath.Add(kv.Key);

            // 밭을 거치지 않는 길이 하나라도 있으면 그것이 상점이다. 효과 중에 카드를 만드는 것이 없어야 한다.
            foreach (var card in d.Cards)
                foreach (var e in card.effects)
                    if (e.type == "gain_card" || e.type == "buy" || e.type == "seed" || e.type == "fertilizer")
                        r.ShopLikeSources.Add(card.id + "/" + e.type);

            var baseline = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                              Trials.SkilledPlay(d), Trials.SkilledSow(d),
                                              cfg.campaigns, d.Balance.seed + 7777, cfg.years, countPlays: true);
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

                var s = Trials.RunCampaign(d, WorldRules.Connected(),
                    _ => new RestrictedPlantingPolicy(new BalancedPlantingPolicy(), without),
                    Trials.SkilledPlay(d), Trials.SkilledSow(d),
                    cfg.campaigns, d.Balance.seed + 7777, cfg.years);

                int gain = s.WinPct - r.BaselineWinPct;
                r.ExclusionGainPct[crop.id] = gain;
                if (gain > cfg.maxExclusionGainPct) r.CropsBetterRemoved.Add(crop.id);
            }
            return r;
        }
    }

    // ── 4. ★ 고리가 두 방향으로 도는가 ────────────────────────────────
    public sealed class LoopClosureResult
    {
        public int SkilledPlots;
        public int ConnectedWinPct, CutWinPct;          // 준최적 설정에서
        public int SownSharePct;                        // 세 해차 덱에서 심어서 온 표본의 몫
        public int DeckShiftPct;                        // 두 세계 덱 구성의 총변동거리(%)
        public int ShiftAsPctOfSownShare;               // 흘러든 몫 가운데 구성 변화로 남은 비율
        public int[] SowPlotSweep = new int[0];
        public int[] ConnectedSweepWinPct = new int[0];
        public int[] CutSweepWinPct = new int[0];
        public int BestConnectedPlots, BestCutPlots;
        public int BestConnectedWinPct, CutAtBestConnectedPct, WinGapAtBestPct;
        public int ConnectedSpreadPct, CutSpreadPct;
        public int ConnectedAvgCardsFromSown, CutAvgCardsFromSown;
        public int ConnectedSeedsFromSown;
        public Dictionary<string, int> ConnectedFinalDeck = new Dictionary<string, int>();
        public Dictionary<string, int> CutFinalDeck = new Dictionary<string, int>();
        public Dictionary<string, int> SownTypeTotals = new Dictionary<string, int>();

        public bool PathActuallyFlows, ShiftNotAbsorbed, WinDiverges, KnobFlips, KnobHasSpread;
        public bool Passed => PathActuallyFlows && ShiftNotAbsorbed && WinDiverges && KnobFlips && KnobHasSpread;
        public List<string> Notes = new List<string>();
    }

    /// <summary>
    /// **이 PoC 의 핵.** 덱 → 밭 경로를 끊은 세계와 이은 세계에서 같은 정책을 돌려 견준다.
    /// 이은 쪽이 유의하게 다른 결과를 내야 통과한다 — 같으면 반대 방향 결합이 장식이라는 뜻이다.
    ///
    /// 동어반복을 피하는 장치가 둘 있다.
    ///   1) **끊은 세계에서도 정책은 똑같이 아낀다**(YearLoop 의 sowBudget). 그래서 끊은 쪽에서
    ///      아끼는 것은 순손해가 되고, 손잡이의 최적점이 두 세계에서 반대쪽에 있어야 한다
    ///   2) 구성 변화의 기준을 **고정값이 아니라 흘러든 양에 비례**해 잡는다. 심어서 온 표본이
    ///      덱의 12% 인데 구성이 전혀 안 바뀌었다면, 심기 정책이 그 유입을 전부 흡수해
    ///      표본집이 같은 모습으로 수렴한 것이다 — 그것은 경로가 장식이라는 뜻이다
    /// </summary>
    public static class LoopClosesBothWays
    {
        public static LoopClosureResult Check(GameData d)
        {
            var cfg = d.Balance.loopClosure;
            var r = new LoopClosureResult { SowPlotSweep = cfg.sowPlotSweep, SkilledPlots = d.Balance.sowing.plotsSkilled };

            var connected = Run(d, WorldRules.Connected(), r.SkilledPlots, cfg, 0);
            var cut = Run(d, WorldRules.Cut(), r.SkilledPlots, cfg, 0);

            r.ConnectedWinPct = connected.WinPct;
            r.CutWinPct = cut.WinPct;
            r.SownSharePct = connected.SownShareOfDeckPct;
            r.ConnectedAvgCardsFromSown = connected.AvgCardsFromSown;
            r.CutAvgCardsFromSown = cut.AvgCardsFromSown;
            r.ConnectedSeedsFromSown = connected.TotalSeedsFromSown;
            r.ConnectedFinalDeck = connected.FinalDeckTotals;
            r.CutFinalDeck = cut.FinalDeckTotals;
            r.SownTypeTotals = connected.SownTypeTotals;

            r.DeckShiftPct = Shift(d, connected.FinalDeckTotals, cut.FinalDeckTotals);
            r.ShiftAsPctOfSownShare = r.SownSharePct == 0 ? 0 : r.DeckShiftPct * 100 / r.SownSharePct;

            // ── '몇 칸을 심는가' 축을 두 세계에서 훑는다 ─────────────
            var sweep = cfg.sowPlotSweep;
            r.ConnectedSweepWinPct = new int[sweep.Length];
            r.CutSweepWinPct = new int[sweep.Length];
            for (int i = 0; i < sweep.Length; i++)
            {
                r.ConnectedSweepWinPct[i] = Run(d, WorldRules.Connected(), sweep[i], cfg, 991).WinPct;
                r.CutSweepWinPct[i] = Run(d, WorldRules.Cut(), sweep[i], cfg, 991).WinPct;
            }
            int bi = ArgMax(r.ConnectedSweepWinPct);
            r.BestConnectedPlots = sweep[bi];
            r.BestConnectedWinPct = r.ConnectedSweepWinPct[bi];
            r.CutAtBestConnectedPct = r.CutSweepWinPct[bi];
            r.WinGapAtBestPct = r.BestConnectedWinPct - r.CutAtBestConnectedPct;
            r.BestCutPlots = sweep[ArgMax(r.CutSweepWinPct)];
            r.ConnectedSpreadPct = Spread(r.ConnectedSweepWinPct);
            r.CutSpreadPct = Spread(r.CutSweepWinPct);

            r.PathActuallyFlows = r.SownSharePct >= cfg.minSownSharePct;
            r.ShiftNotAbsorbed = r.ShiftAsPctOfSownShare >= cfg.minShiftAsPctOfSownShare;
            r.WinDiverges = r.WinGapAtBestPct >= cfg.minWinGapPct;
            r.KnobFlips = r.BestConnectedPlots > 0 && r.BestCutPlots == 0;
            r.KnobHasSpread = r.ConnectedSpreadPct >= cfg.minSowKnobSpreadPct;

            r.Notes.Add($"경로가 흐른다: 세 해차 덱의 {r.SownSharePct}% 가 심어서 온 표본이다 (기준 {cfg.minSownSharePct}% 이상)");
            r.Notes.Add($"흡수되지 않는다: 덱 구성 차이 {r.DeckShiftPct}% = 흘러든 몫의 {r.ShiftAsPctOfSownShare}% " +
                        $"(기준 {cfg.minShiftAsPctOfSownShare}% 이상). 심기 정책이 유입을 전부 흡수하면 이 값이 0 에 붙는다");
            r.Notes.Add($"승률: 이은 세계의 최적({r.BestConnectedPlots}칸) {r.BestConnectedWinPct}% vs 같은 설정을 끊은 세계에서 " +
                        $"{r.CutAtBestConnectedPct}% → 차이 {r.WinGapAtBestPct}%p (기준 {cfg.minWinGapPct}%p 이상)");
            r.Notes.Add($"손잡이가 뒤집힌다: 최적 심기 칸이 이은 세계 {r.BestConnectedPlots}칸 · 끊은 세계 {r.BestCutPlots}칸. " +
                        "끊은 쪽에서 0 이 최적이어야 아낀 값이 오직 심기에서 온다는 뜻이다");
            r.Notes.Add($"축의 승률 폭: 이은 {r.ConnectedSpreadPct}%p · 끊은 {r.CutSpreadPct}%p (이은 쪽 기준 {cfg.minSowKnobSpreadPct}%p 이상)");
            r.Notes.Add($"준최적({r.SkilledPlots}칸) 승률: 이은 {r.ConnectedWinPct}% · 끊은 {r.CutWinPct}%. " +
                        $"심어서 돋은 표본 해마다 평균 이은 {r.ConnectedAvgCardsFromSown}장 · 끊은 {r.CutAvgCardsFromSown}장");
            return r;
        }

        static TrialSummary Run(GameData d, WorldRules world, int sowPlots, LoopClosureBalance cfg, int seedOffset)
            => Trials.RunCampaign(d, world, Trials.BalancedPlant(), Trials.SkilledPlay(d),
                                  _ => new GreedySowPolicy(sowPlots),
                                  cfg.trials, d.Balance.seed + 4242 + seedOffset, cfg.years);

        /// <summary>두 덱 구성의 총변동거리. 비율로 옮겨 L1 거리를 반으로 나눈 값(%).</summary>
        public static int Shift(GameData d, Dictionary<string, int> a, Dictionary<string, int> b)
        {
            int ta = 0, tb = 0;
            foreach (var kv in a) ta += kv.Value;
            foreach (var kv in b) tb += kv.Value;
            if (ta == 0 || tb == 0) return 100;
            int l1 = 0;
            foreach (var card in d.Cards)
            {
                int pa = (a.TryGetValue(card.id, out var va) ? va : 0) * 1000 / ta;
                int pb = (b.TryGetValue(card.id, out var vb) ? vb : 0) * 1000 / tb;
                l1 += Math.Abs(pa - pb);
            }
            return l1 / 20;   // (l1/1000)/2*100 = l1/20. 정수만 쓴다
        }

        static int ArgMax(int[] v)
        {
            int best = 0;
            for (int i = 1; i < v.Length; i++) if (v[i] > v[best]) best = i;
            return best;
        }

        static int Spread(int[] v)
        {
            if (v.Length == 0) return 0;
            int lo = v[0], hi = v[0];
            foreach (var x in v) { if (x < lo) lo = x; if (x > hi) hi = x; }
            return hi - lo;
        }
    }

    // ── 5. 전부 심고 아무것도 안 쓰는 것이 최적이 아닌가 ──────────────
    public sealed class SowingBestResult
    {
        public int SkilledWinPct;
        public int ExtremeSowWinPct;     // 전부 심고 아무것도 안 쓴다
        public int NoSowWinPct;          // 아무것도 심지 않는다
        public int ExtremeGainPct;
        public int InteriorGainOverNoSowPct;
        public int InteriorGainOverExtremePct;
        public int ExtremeAvgUnplayed;
        public bool ExtremeIsNotBest, InteriorBeatsBothEnds;
        public bool Passed => ExtremeIsNotBest && InteriorBeatsBothEnds;
        public List<string> Notes = new List<string>();
    }

    /// <summary>
    /// "전부 심고 아무것도 안 쓴다"가 최적이면 밤이 의미를 잃는다.
    /// 이 PoC 에서 그것을 막는 것은 규칙 하나다 — **밤에 지면 그해 표본을 심지 못한다.**
    /// </summary>
    public static class SowingIsNotAlwaysBest
    {
        public static SowingBestResult Check(GameData d)
        {
            var cfg = d.Balance.sowingNotAlwaysBest;
            var b = d.Balance;
            var r = new SowingBestResult();

            var skilled = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                             Trials.SkilledPlay(d), Trials.SkilledSow(d),
                                             cfg.trials, b.seed + 313, cfg.years);
            var extreme = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                             _ => SowAwarePlayPolicy.Hoarder(d),
                                             _ => new SowEverythingPolicy(b.sowing.plotsAll),
                                             cfg.trials, b.seed + 313, cfg.years);
            var none = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                          _ => SowAwarePlayPolicy.Spender(d), _ => new NoSowPolicy(),
                                          cfg.trials, b.seed + 313, cfg.years);

            r.SkilledWinPct = skilled.WinPct;
            r.ExtremeSowWinPct = extreme.WinPct;
            r.NoSowWinPct = none.WinPct;
            r.ExtremeAvgUnplayed = extreme.AvgUnplayed;
            r.ExtremeGainPct = extreme.WinPct - skilled.WinPct;
            r.InteriorGainOverNoSowPct = skilled.WinPct - none.WinPct;
            r.InteriorGainOverExtremePct = skilled.WinPct - extreme.WinPct;

            r.ExtremeIsNotBest = r.ExtremeGainPct <= cfg.maxExtremeGainPct;
            r.InteriorBeatsBothEnds =
                r.InteriorGainOverNoSowPct >= cfg.minInteriorGainPct &&
                r.InteriorGainOverExtremePct >= cfg.minInteriorGainPct;

            r.Notes.Add($"준최적 {r.SkilledWinPct}% · 전부 심기 {r.ExtremeSowWinPct}% · 안 심기 {r.NoSowWinPct}%");
            r.Notes.Add($"전부 심기가 준최적보다 {r.ExtremeGainPct}%p (한계 {cfg.maxExtremeGainPct}%p 이하) — " +
                        "넘으면 밤이 의미를 잃는다");
            r.Notes.Add($"준최적이 양 끝보다 안 심기 +{r.InteriorGainOverNoSowPct}%p · 전부 심기 +{r.InteriorGainOverExtremePct}%p " +
                        $"(둘 다 기준 {cfg.minInteriorGainPct}%p 이상)");
            return r;
        }
    }

    // ── 6. 이길 수 있는가 ─────────────────────────────────────────────
    public sealed class RunSolvabilityResult
    {
        public int WinPct;
        public int RequiredPct;
        public List<string> UnbeatableVisits = new List<string>();
        public bool Passed => WinPct >= RequiredPct && UnbeatableVisits.Count == 0;
    }

    public static class RunSolvability
    {
        public static RunSolvabilityResult Check(GameData d)
        {
            var cfg = d.Balance.runSolvability;
            var r = new RunSolvabilityResult { RequiredPct = cfg.minWinPct };

            // 첫 해. 심어 둔 표본 없이 밭 하나로만 밤을 넘길 수 있어야 한다.
            var s = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                       Trials.SkilledPlay(d), Trials.SkilledSow(d),
                                       cfg.trials, d.Balance.seed + 31, years: 1);
            r.WinPct = s.WinPct;

            // 침입 하나씩 따로도 막을 수 있어야 한다.
            var night = new NightSim(d);
            foreach (var visit in d.OrderedVisits)
            {
                bool any = false;
                for (int t = 0; t < 12 && !any; t++)
                {
                    var deck = FreshDeck(d);
                    if (deck.Count == 0) continue;
                    var solo = new YearDef
                    {
                        id = d.Year.id, nameKo = d.Year.nameKo, playerHp = d.Year.playerHp,
                        visits = new List<VisitDef> { new VisitDef { order = 0, enemyId = visit.enemyId, isBoss = visit.isBoss } }
                    };
                    var soloData = SwapYear(d, solo);
                    any = new NightSim(soloData).Run(deck, d.Year.playerHp, 100, 0,
                                                     SowAwarePlayPolicy.Spender(soloData), new Rng(d.Balance.seed + t)).Held;
                }
                if (!any) r.UnbeatableVisits.Add(visit.order + "/" + visit.enemyId);
            }
            return r;
        }

        /// <summary>한 해 농사분의 덱. 침입 하나를 따로 재 볼 때 쓴다.</summary>
        public static YearDeck FreshDeck(GameData d)
        {
            var field = new FieldSim(d);
            int counter = 0;
            var year = field.RunYear(1, d.Balance.economy.seedStipendPerYear, 0, d.StartPlots,
                                     new List<SownSpecimen>(), new BalancedPlantingPolicy(), ref counter);
            return year.Deck;
        }

        static GameData SwapYear(GameData d, YearDef year)
        {
            var all = new List<string>();
            foreach (var c in d.Crops) all.Add(c.id);
            var copy = d.WithCropSubset(all);
            copy.Year = year;
            return copy;
        }
    }

    // ── 7. 지배 전략 ──────────────────────────────────────────────────
    public sealed class DominanceRow
    {
        public string Label;
        public int WinPct;
        public int AvgDeckSize;
        public bool Passed;
        public string Why;
    }

    public static class DominanceChecker
    {
        public static List<DominanceRow> Check(GameData d)
        {
            var cfg = d.Balance.dominance;
            var rows = new List<DominanceRow>();

            foreach (var crop in d.Crops)
            {
                var s = Trials.RunCampaign(d, WorldRules.Connected(), _ => new MonoCropPlantingPolicy(crop.id),
                                           Trials.SkilledPlay(d), Trials.SkilledSow(d),
                                           cfg.trials, d.Balance.seed + crop.sortOrder * 101, cfg.years);
                rows.Add(new DominanceRow
                {
                    Label = "단작:" + crop.id, WinPct = s.WinPct, AvgDeckSize = s.AvgDeckSize,
                    Passed = s.WinPct <= cfg.monoCropMaxWinPct,
                    Why = $"한 작물만 심어도 {s.WinPct}% (한계 {cfg.monoCropMaxWinPct}%)"
                });
            }

            // ── 손잡이 둘을 훑는다 ───────────────────────────────────
            // '몇 칸을 심는가'가 이 PoC 의 진짜 손잡이다 — 여기는 판정한다.
            var plotSweep = cfg.sowPlotSweep;
            var plotWin = new int[plotSweep.Length];
            int plotBest = 0;
            for (int i = 0; i < plotSweep.Length; i++)
            {
                plotWin[i] = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                                Trials.SkilledPlay(d), _ => new GreedySowPolicy(plotSweep[i]),
                                                cfg.trials, d.Balance.seed + 777, cfg.years).WinPct;
                if (plotWin[i] > plotWin[plotBest]) plotBest = i;
            }
            for (int i = 0; i < plotSweep.Length; i++)
                rows.Add(new DominanceRow
                {
                    Label = $"칸:{plotSweep[i]}", WinPct = plotWin[i], Passed = true,
                    Why = i == plotBest ? "이 축의 최적" : "기준선"
                });
            int plotLo = plotWin[0], plotHi = plotWin[0];
            foreach (var w in plotWin) { if (w < plotLo) plotLo = w; if (w > plotHi) plotHi = w; }
            rows.Add(new DominanceRow
            {
                Label = "칸축의 최적이 양 끝이 아닌가",
                WinPct = plotWin[plotBest],
                Passed = plotBest != 0 && plotBest != plotSweep.Length - 1,
                Why = $"최적은 {plotSweep[plotBest]}칸 ({plotWin[plotBest]}%). 0칸(안 심기 {plotWin[0]}%)이나 " +
                      $"{plotSweep[plotSweep.Length - 1]}칸(전부 심기 {plotWin[plotSweep.Length - 1]}%)이 최적이면 손잡이가 장식이다"
            });
            rows.Add(new DominanceRow
            {
                Label = "칸축이 무언가를 하는가",
                WinPct = plotHi - plotLo,
                Passed = plotHi - plotLo >= cfg.minSowPlotSpreadPct,
                Why = $"축을 훑은 승률 폭 {plotHi - plotLo}%p (기준 {cfg.minSowPlotSpreadPct}%p 이상)"
            });

            // '지금 쓸까 남길까' 축. **여기는 재서 적기만 하고 판정하지 않는다.**
            // 남는 표본의 수가 이미 밭칸 수에 묶여 있어서, 일부러 아끼지 않아도 심을 거리가 생긴다.
            // 그래서 이 축이 얇은 것은 고장이 아니라 사람이 볼 설계 물음이다(README 판정 칸).
            var sweep = cfg.sowWeightSweep;
            var win = new int[sweep.Length];
            int bestAt = 0;
            for (int i = 0; i < sweep.Length; i++)
            {
                win[i] = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                            _ => new SowAwarePlayPolicy(d, sweep[i]), Trials.SkilledSow(d),
                                            cfg.trials, d.Balance.seed + 555, cfg.years).WinPct;
                if (win[i] > win[bestAt]) bestAt = i;
            }
            for (int i = 0; i < sweep.Length; i++)
                rows.Add(new DominanceRow
                {
                    Label = $"남김:{sweep[i]}", WinPct = win[i], Passed = true,
                    Why = i == bestAt ? "이 축의 최적" : "기준선"
                });
            int lo = win[0], hi = win[0];
            foreach (var w in win) { if (w < lo) lo = w; if (w > hi) hi = w; }
            rows.Add(new DominanceRow
            {
                Label = "남김축의 최댓값이 최적이 아닌가",
                WinPct = win[sweep.Length - 1],
                Passed = bestAt != sweep.Length - 1,
                Why = $"최적은 남김={sweep[bestAt]} ({win[bestAt]}%), 극단 남김={sweep[sweep.Length - 1]} 은 {win[sweep.Length - 1]}%. " +
                      "극단이 최적이면 '아끼기만 하는 지루한 최적해'다"
            });
            rows.Add(new DominanceRow
            {
                Label = "(참고) 남김축의 폭",
                WinPct = hi - lo,
                Passed = true,
                Why = $"축을 훑은 승률 폭 {hi - lo}%p — **판정하지 않는다.** 밭칸 수가 이미 아낄 수 있는 양을 묶기 때문에 " +
                      "일부러 아끼지 않아도 남는 표본이 생긴다. 이 축이 얇은 것이 손맛으로 어떻게 느껴지는지는 사람 몫이다"
            });
            return rows;
        }
    }

    // ── 8. 씨앗·밭·덱 경제 ───────────────────────────────────────────
    public sealed class EconomyResult
    {
        public int Years;
        public int MaxSeeds, MaxFertilizer, MaxPlots, MaxDeck, MaxSown;
        public int SeedsFromSownTotal;
        public bool NoBattleMinting;
        public bool NoMonotoneDivergence;
        public List<string> Problems = new List<string>();
        public bool Passed => Problems.Count == 0;
    }

    /// <summary>
    /// 자원에 무한 증식 고리가 없는가.
    /// 이 PoC 에는 고리가 하나 있다: **갈라지는 표본**(한 장 → 두 장)과 **씨앗을 내는 표본**.
    /// 둘 다 밭칸을 먹기 때문에 밭 칸 수와 sownCeiling 이 브레이크다. 그것이 실제로 잡는지 본다.
    /// </summary>
    public static class EconomyChecker
    {
        public static EconomyResult Check(GameData d)
        {
            var e = d.Balance.economy;
            var r = new EconomyResult { Years = e.economyYears, NoBattleMinting = true };

            // 전투 중에 자원을 만드는 효과가 없어야 한다. 자원이 느는 곳은 해 경계와 seedfall 뿐이다.
            foreach (var card in d.Cards)
                foreach (var eff in card.effects)
                    if (eff.type == "seed" || eff.type == "fertilizer")
                    { r.NoBattleMinting = false; r.Problems.Add($"카드 {card.id} 이 전투 중에 자원을 만든다"); }

            var loop = new YearLoop(d);
            // 가장 세게 증식시키는 정책으로 돌린다 — 칸을 다 써서 심고, 최대한 아낀다.
            var run = loop.Run(e.economyYears, WorldRules.Connected(), new BalancedPlantingPolicy(),
                               SowAwarePlayPolicy.Skilled(d), new SowEverythingPolicy(d.Balance.sowing.plotsAll),
                               d.Balance.seed);

            r.MaxSeeds = run.MaxSeedsSeen;
            r.MaxFertilizer = run.MaxFertilizerSeen;
            r.MaxPlots = run.MaxPlotsSeen;
            r.MaxDeck = run.MaxDeckSeen;
            r.MaxSown = run.MaxSownSeen;
            foreach (var y in run.Years) r.SeedsFromSownTotal += y.SeedsFromSown;

            if (run.MaxSeedsSeen > e.seedCeiling) r.Problems.Add($"씨앗이 상한 {e.seedCeiling} 을 넘었다: {run.MaxSeedsSeen}");
            if (run.MaxFertilizerSeen > e.fertilizerCeiling) r.Problems.Add($"비료가 상한 {e.fertilizerCeiling} 을 넘었다: {run.MaxFertilizerSeen}");
            if (run.MaxPlotsSeen > d.MaxPlots) r.Problems.Add($"밭이 상한 {d.MaxPlots} 을 넘었다: {run.MaxPlotsSeen}");
            if (run.MaxDeckSeen > e.deckCeiling)
                r.Problems.Add($"덱이 상한 {e.deckCeiling} 장을 넘었다: {run.MaxDeckSeen} — 갈라지는 표본이 발산한다");
            if (run.MaxSownSeen > e.sownCeiling)
                r.Problems.Add($"심은 표본이 상한 {e.sownCeiling} 장을 넘었다: {run.MaxSownSeen}");

            int from = e.economyYears * 3 / 4;
            bool seedsUp = true, deckUp = true;
            for (int i = from + 1; i < run.Years.Count; i++)
            {
                if (run.Years[i].SeedsAtStart <= run.Years[i - 1].SeedsAtStart) seedsUp = false;
                if (run.Years[i].DeckSize <= run.Years[i - 1].DeckSize) deckUp = false;
            }
            r.NoMonotoneDivergence = !seedsUp && !deckUp;
            if (seedsUp) r.Problems.Add("마지막 구간에서 시작 씨앗이 단조 증가한다 — 무한 증식 고리");
            if (deckUp) r.Problems.Add("마지막 구간에서 덱이 단조 증가한다 — 갈라지는 표본에 브레이크가 없다");

            return r;
        }
    }

    // ── 9. 승률 구간 ──────────────────────────────────────────────────
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
            var random = Trials.RunCampaign(d, WorldRules.Connected(), rng => new RandomPlantingPolicy(rng),
                                            rng => new RandomPlayPolicy(rng), _ => new GreedySowPolicy(1),
                                            cfg.trials, d.Balance.seed + 991, cfg.years);
            var skilled = Trials.RunCampaign(d, WorldRules.Connected(), Trials.BalancedPlant(),
                                             Trials.SkilledPlay(d), Trials.SkilledSow(d),
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
