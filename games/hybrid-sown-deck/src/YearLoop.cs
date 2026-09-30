// 바깥 루프. 한 해 = 봄·여름·가을(심기) + 겨울 하룻밤(침입) + **밭으로 돌려보내기**.
//
// 고리가 두 방향으로 돈다:
//   밭 → 덱 : 거둔 작물이 표본이 된다
//   덱 → 밭 : 밤에 놓지 않은 표본을 심는다. 다음 봄에 변해서 돌아온다
//
// WorldRules 하나로 두 번째 화살을 **끊을 수 있다.** LoopClosesBothWays 가 그 스위치로
// 같은 정책을 두 세계에서 돌려 견준다 — 같은 결과가 나오면 반대 방향 결합은 장식이다.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridSownDeck
{
    /// <summary>세계의 규칙. 지금 있는 스위치는 하나뿐이고, 그 하나가 이 PoC 전부다.</summary>
    public sealed class WorldRules
    {
        /// <summary>덱 → 밭 경로가 이어져 있는가. false 면 놓지 않은 표본은 그냥 버려진다.</summary>
        public bool DeckToFieldClosed;

        public static WorldRules Connected() => new WorldRules { DeckToFieldClosed = true };
        public static WorldRules Cut() => new WorldRules { DeckToFieldClosed = false };
        public string Label => DeckToFieldClosed ? "이은 세계" : "끊은 세계";
    }

    /// <summary>해를 넘어 남는 것. 덱은 여기 없다 — 밤이 끝나면 사라진다. 남는 것은 **심은 표본**이다.</summary>
    public sealed class Garden
    {
        public int Seeds;
        public int Fertilizer;
        public int Plots;
        /// <summary>지난 겨울에 밭으로 돌려보낸 표본. 다음 봄에 돋아난다.</summary>
        public List<SownSpecimen> Sown = new List<SownSpecimen>();
        /// <summary>표본 번호를 이어서 붙이려고 센다. 표본집의 통시적 번호다.</summary>
        public int LineageCounter;
    }

    public sealed class YearOutcome
    {
        public int Year;
        public string WorldLabel;
        public int SeedsAtStart;
        public int FertilizerAtStart;
        public int UnlockedPlots;
        public int SownAtStart;             // 지난 겨울에 심어 둔 표본 장수
        public FieldYear Field;
        public NightResult Night;
        public bool Held;
        public int DeckSize;
        public int CardsFromSown;
        public int SeedsFromSown;
        public int PlotTurnsToSown;
        public int UnplayedAfterNight;
        public int SownForNextYear;
        public int SowPlotBudget;
        public Dictionary<string, int> DeckCounts = new Dictionary<string, int>();
        public Dictionary<string, int> SownTypeCounts = new Dictionary<string, int>();
        public int SeedsCarried;
        public int FertilizerCarried;
    }

    public sealed class CampaignResult
    {
        public List<YearOutcome> Years = new List<YearOutcome>();
        public int Wins;
        public int Losses;
        public int MaxSeedsSeen;
        public int MaxFertilizerSeen;
        public int MaxPlotsSeen;
        public int MaxDeckSeen;
        public int MaxSownSeen;
        public int WinPct => Years.Count == 0 ? 0 : Wins * 100 / Years.Count;
    }

    public sealed class YearLoop
    {
        readonly GameData _data;
        readonly FieldSim _field;
        readonly NightSim _night;

        public YearLoop(GameData data)
        {
            _data = data;
            _field = new FieldSim(data);
            _night = new NightSim(data);
        }

        public YearOutcome RunYear(Garden g, int year, WorldRules world,
                                   IPlantingPolicy planting, IPlayPolicy play, ISowPolicy sowing,
                                   Rng rng, StringBuilder transcript = null)
        {
            var e = _data.Balance.economy;
            var outcome = new YearOutcome
            {
                Year = year,
                WorldLabel = world.Label,
                SeedsAtStart = g.Seeds,
                FertilizerAtStart = g.Fertilizer,
                UnlockedPlots = g.Plots,
                SownAtStart = g.Sown.Count
            };

            // 이번 겨울에 밭으로 돌려보낼 작정인 칸 수. 밤의 정책이 이것을 보고 아낀다.
            //
            // **두 세계에서 똑같이 셈한다.** 끊은 세계에서도 정책은 같은 만큼 아끼고,
            // 다만 그 표본이 밭으로 가지 못한다 — 그래서 아낀 값이 고스란히 손해가 된다.
            // 여기서 세계에 따라 0 으로 깎아 버리면 LoopClosesBothWays 가
            // "손잡이가 없는 세계에서는 손잡이가 아무것도 안 한다"는 동어반복을 재게 된다.
            int sowBudget = sowing.PlotsWanted;
            if (sowBudget > g.Plots) sowBudget = g.Plots;
            if (sowBudget > e.sownCeiling) sowBudget = e.sownCeiling;
            if (sowBudget < 0) sowBudget = 0;
            outcome.SowPlotBudget = sowBudget;

            // ── 봄·여름·가을 ──────────────────────────────────────────
            int counter = g.LineageCounter;
            outcome.Field = _field.RunYear(year, g.Seeds, g.Fertilizer, g.Plots, g.Sown, planting, ref counter);
            g.LineageCounter = counter;
            g.Sown = new List<SownSpecimen>();          // 심어 둔 것은 이 해에 다 돋았다

            outcome.DeckSize = outcome.Field.Deck.Count;
            outcome.CardsFromSown = outcome.Field.CardsFromSown;
            outcome.SeedsFromSown = outcome.Field.SeedsFromSown;
            outcome.PlotTurnsToSown = outcome.Field.PlotTurnsToSown;
            outcome.DeckCounts = outcome.Field.Deck.CountsByCard();

            // ── 겨울 하룻밤 ───────────────────────────────────────────
            int hpPct = 100 + (year - 1) * _data.Balance.year.hpGrowthPctPerYear;
            if (hpPct > _data.Balance.year.hpScaleMaxPct) hpPct = _data.Balance.year.hpScaleMaxPct;

            outcome.Night = _night.Run(outcome.Field.Deck, _data.Year.playerHp, hpPct, sowBudget, play, rng, transcript);
            outcome.Held = outcome.Night.Held;
            outcome.UnplayedAfterNight = outcome.Night.UnplayedLeft;

            // ── 덱 → 밭: 놓지 않은 표본을 심는다 ─────────────────────
            // **지면 심지 못한다.** 들짐승이 밭과 표본집을 함께 뒤집어 놓았기 때문이다.
            // 이 규칙 하나가 "전부 심고 아무것도 안 쓴다"를 자멸로 만든다.
            if (world.DeckToFieldClosed && outcome.Held && sowBudget > 0)
            {
                var picked = sowing.Choose(_data, outcome.Field.Deck.Unplayed(), sowBudget);
                foreach (var s in picked)
                {
                    var def = _data.Card(s.CardId);
                    g.Sown.Add(new SownSpecimen
                    {
                        Source = s, SownInYear = year,
                        OccupiesTurns = def.sownTurns < 1 ? 1 : def.sownTurns
                    });
                    string t = def.sown != null ? def.sown.type : "none";
                    outcome.SownTypeCounts[t] = outcome.SownTypeCounts.TryGetValue(t, out var n) ? n + 1 : 1;
                }
                outcome.SownForNextYear = g.Sown.Count;
                transcript?.Append('s').Append(g.Sown.Count).Append('\n');
            }

            // ── 해 경계: 자원이 느는 유일한 지점 ─────────────────────
            g.Seeds = Math.Min(outcome.Field.SeedsLeft, e.seedCarryCap);
            g.Fertilizer = Math.Min(outcome.Field.FertilizerLeft, e.fertilizerCarryCap);
            if (outcome.Held)
            {
                g.Seeds += e.seedWinBonus;
                g.Fertilizer += e.fertilizerWinBonus;
                if (g.Plots < _data.MaxPlots && g.Plots < _data.Plots.Count) g.Plots++;
            }
            g.Seeds = Math.Min(g.Seeds, e.seedCeiling);
            g.Fertilizer = Math.Min(g.Fertilizer, e.fertilizerCeiling);

            outcome.SeedsCarried = g.Seeds;
            outcome.FertilizerCarried = g.Fertilizer;
            return outcome;
        }

        /// <summary>정원 하나가 여러 해 이어진다. 그 사이를 건너는 것은 덱이 아니라 **심은 표본**이다.</summary>
        public CampaignResult Run(int years, WorldRules world, IPlantingPolicy planting, IPlayPolicy play,
                                  ISowPolicy sowing, int seed, StringBuilder transcript = null)
        {
            var e = _data.Balance.economy;
            var rng = new Rng(seed);
            var g = new Garden { Seeds = 0, Fertilizer = 0, Plots = _data.StartPlots };
            var result = new CampaignResult();

            for (int y = 1; y <= years; y++)
            {
                g.Seeds = Math.Min(g.Seeds + e.seedStipendPerYear, e.seedCeiling);

                if (g.Seeds > result.MaxSeedsSeen) result.MaxSeedsSeen = g.Seeds;
                if (g.Fertilizer > result.MaxFertilizerSeen) result.MaxFertilizerSeen = g.Fertilizer;
                if (g.Plots > result.MaxPlotsSeen) result.MaxPlotsSeen = g.Plots;
                if (g.Sown.Count > result.MaxSownSeen) result.MaxSownSeen = g.Sown.Count;

                var yr = RunYear(g, y, world, planting, play, sowing, rng, transcript);
                result.Years.Add(yr);
                if (yr.Held) result.Wins++; else result.Losses++;
                if (yr.DeckSize > result.MaxDeckSeen) result.MaxDeckSeen = yr.DeckSize;
            }
            return result;
        }
    }
}
