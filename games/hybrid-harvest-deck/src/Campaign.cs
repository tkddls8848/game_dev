// 여러 해를 잇는 바깥 루프. 회차에서 얻은 것만 다음 해로 넘어가고, 지면 그 해의 수확을 잃는다.
// 씨앗·비료는 해 경계에서만 늘어난다 — 이것이 EconomyChecker 가 기대는 구조적 사실이다.
using System;
using System.Collections.Generic;
using System.Text;

namespace HybridHarvestDeck
{
    public sealed class YearResult
    {
        public int Year;
        public int SeedsAtStart;
        public int FertilizerAtStart;
        public int UnlockedPlots;
        public FieldYear Field;
        public NightResult Night;
        public bool HarvestLost;      // 지면 그 해의 수확을 잃는다
        public int SeedsCarried;
        public int FertilizerCarried;
    }

    public sealed class CampaignResult
    {
        public List<YearResult> Years = new List<YearResult>();
        public int Wins;
        public int Losses;
        public int MaxSeedsSeen;
        public int MaxFertilizerSeen;
        public int MaxPlotsSeen;
    }

    public sealed class Campaign
    {
        readonly GameData _data;
        readonly FieldSim _field;
        readonly NightSim _night;

        public Campaign(GameData data)
        {
            _data = data;
            _field = new FieldSim(data);
            _night = new NightSim(data);
        }

        /// <summary>한 해만. 검사기 대부분이 이것을 부른다.</summary>
        public YearResult RunYear(int year, int seeds, int fertilizer, int unlockedPlots,
                                  IPlantingPolicy planting, IPlayPolicy play, Rng rng,
                                  StringBuilder transcript = null)
        {
            var e = _data.Balance.economy;
            var res = new YearResult
            {
                Year = year,
                SeedsAtStart = seeds,
                FertilizerAtStart = fertilizer,
                UnlockedPlots = unlockedPlots
            };

            res.Field = _field.RunYear(seeds, fertilizer, unlockedPlots, planting);
            res.Night = _night.Run(res.Field.Deck, play, rng, year, transcript);
            res.HarvestLost = !res.Night.Won;

            int carriedSeeds = Math.Min(res.Field.SeedsLeft, e.seedCarryCap);
            int carriedFert = Math.Min(res.Field.FertilizerLeft, e.fertilizerCarryCap);
            if (res.Night.Won)
            {
                carriedSeeds += e.seedWinBonus;
                carriedFert += e.fertilizerWinBonus;
            }
            res.SeedsCarried = Math.Min(carriedSeeds, e.seedCeiling);
            res.FertilizerCarried = Math.Min(carriedFert, e.fertilizerCeiling);
            return res;
        }

        /// <summary>여러 해. EconomyChecker 가 100년 단위로 돌린다.</summary>
        public CampaignResult Run(int years, IPlantingPolicy planting, IPlayPolicy play, int seed)
        {
            var e = _data.Balance.economy;
            var result = new CampaignResult();
            var rng = new Rng(seed);

            int carriedSeeds = 0;
            int carriedFert = 0;
            int plots = _data.StartPlots;

            for (int y = 1; y <= years; y++)
            {
                int seeds = Math.Min(carriedSeeds + e.seedStipendPerYear, e.seedCeiling);
                int fert = Math.Min(carriedFert, e.fertilizerCeiling);

                var yr = RunYear(y, seeds, fert, plots, planting, play, rng);
                result.Years.Add(yr);

                if (yr.Night.Won)
                {
                    result.Wins++;
                    if (plots < _data.MaxPlots && plots < _data.Plots.Count) plots++;
                }
                else
                {
                    result.Losses++;
                }

                carriedSeeds = yr.SeedsCarried;
                carriedFert = yr.FertilizerCarried;

                if (seeds > result.MaxSeedsSeen) result.MaxSeedsSeen = seeds;
                if (fert > result.MaxFertilizerSeen) result.MaxFertilizerSeen = fert;
                if (plots > result.MaxPlotsSeen) result.MaxPlotsSeen = plots;
            }
            return result;
        }
    }
}
