using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FarmSignal.Sim
{
    public sealed class YearRecord
    {
        public int Year;
        public int IncomeCoin;
        public int ExpenseCoin;
        public int MoneyAtYearEnd;
        public int TheftPlotsLost;
        public int TheftSeedLossCoin;
        public int ObservedIndexSum;    // 그 해 네 계절의 관측지수 합
        public int RenownAtYearEnd;
        public int MinPriceIndexAtYearEnd;
    }

    public sealed class SimResult
    {
        public string PolicyLabel;
        public bool SignalOn;

        public int Years;
        public int DaysSimulated;
        /// 파산으로 일찍 끝나면 그날까지. 끝까지 살면 전체 일수.
        public int PlayableDays;

        public List<YearRecord> YearRecords = new List<YearRecord>();
        public List<SeasonSignal> SignalHistory = new List<SeasonSignal>();

        public Dictionary<string, int> PlantCounts = new Dictionary<string, int>();
        public Dictionary<string, int> HarvestCounts = new Dictionary<string, int>();
        public Dictionary<string, int> CropRevenue = new Dictionary<string, int>();
        public Dictionary<string, int> SeasonIncome = new Dictionary<string, int>();
        public Dictionary<string, int> SeasonExpense = new Dictionary<string, int>();
        public Dictionary<string, int> EventFireCounts = new Dictionary<string, int>();
        public Dictionary<string, int> WeatherCounts = new Dictionary<string, int>();

        public int FinalMoneyCoin;
        public int TotalIncomeCoin;
        public int TotalExpenseCoin;
        public int SeedSpentCoin;
        public int UpkeepPaidCoin;
        public int PeakMoneyCoin;
        public int PeakMoneyDay = -1;

        // 신호가 남긴 것
        public int FirstWarnSeason = -1;
        public int FirstCollapseSeason = -1;
        public int FirstTheftSeason = -1;
        public int FirstVisitorSeason = -1;
        public int TheftCount;
        public int TheftPlotsLost;
        public int TheftSeedLossCoin;
        public int MaxRenown;
        public int MaxVisitors;
        public int MaxPremiumPercent;
        public int MinPriceIndexSeen = -1;
        public int MaxPriceIndexSeen = -1;
        public int EmptyPlotDays;      // 심을 것이 없어 빈 칸으로 보낸 칸·날 수

        public int BankruptYear = -1;
        public int BankruptDay = -1;

        public int Plant(string cropId) => PlantCounts.TryGetValue(cropId, out var v) ? v : 0;
        public int Harvest(string cropId) => HarvestCounts.TryGetValue(cropId, out var v) ? v : 0;
        public int Revenue(string cropId) => CropRevenue.TryGetValue(cropId, out var v) ? v : 0;
        public int Income(string seasonId) => SeasonIncome.TryGetValue(seasonId, out var v) ? v : 0;
        public int Expense(string seasonId) => SeasonExpense.TryGetValue(seasonId, out var v) ? v : 0;
        public int Fired(string eventId) => EventFireCounts.TryGetValue(eventId, out var v) ? v : 0;

        public int NetCoin => TotalIncomeCoin - TotalExpenseCoin;

        /// 씨드 재현성 검사가 비교하는 값. 결과 전체를 한 문자열로 굳혀 해시한다.
        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append("policy=").Append(PolicyLabel).Append(";signal=").Append(SignalOn ? 1 : 0);
            sb.Append(";years=").Append(Years).Append(";days=").Append(DaysSimulated);
            sb.Append(";playable=").Append(PlayableDays);
            sb.Append(";money=").Append(FinalMoneyCoin);
            sb.Append(";in=").Append(TotalIncomeCoin).Append(";out=").Append(TotalExpenseCoin);
            sb.Append(";seed=").Append(SeedSpentCoin).Append(";upkeep=").Append(UpkeepPaidCoin);
            sb.Append(";peak=").Append(PeakMoneyCoin).Append('@').Append(PeakMoneyDay);
            sb.Append(";warn=").Append(FirstWarnSeason).Append(";collapse=").Append(FirstCollapseSeason);
            sb.Append(";theft=").Append(FirstTheftSeason).Append('x').Append(TheftCount)
              .Append('/').Append(TheftPlotsLost).Append('/').Append(TheftSeedLossCoin);
            sb.Append(";visitor=").Append(FirstVisitorSeason).Append(";renown=").Append(MaxRenown)
              .Append('/').Append(MaxVisitors).Append('/').Append(MaxPremiumPercent);
            sb.Append(";idx=").Append(MinPriceIndexSeen).Append("..").Append(MaxPriceIndexSeen);
            sb.Append(";empty=").Append(EmptyPlotDays);
            sb.Append(";bankrupt=").Append(BankruptYear).Append('@').Append(BankruptDay);
            foreach (var r in YearRecords)
                sb.Append('|').Append(r.Year).Append(',').Append(r.IncomeCoin).Append(',')
                  .Append(r.ExpenseCoin).Append(',').Append(r.MoneyAtYearEnd).Append(',')
                  .Append(r.TheftPlotsLost).Append(',').Append(r.ObservedIndexSum).Append(',')
                  .Append(r.RenownAtYearEnd).Append(',').Append(r.MinPriceIndexAtYearEnd);
            foreach (var s in SignalHistory)
            {
                sb.Append('#').Append(s.SeasonNumber).Append(':');
                for (int i = 0; i < s.ObservedIndex.Length; i++)
                    sb.Append(s.ObservedIndex[i]).Append('/').Append(s.PriceAfter[i]).Append(',');
                sb.Append('r').Append(s.RenownAfter).Append('v').Append(s.Visitors)
                  .Append('t').Append(s.TheftRiskPer1000).Append('@').Append(s.TheftRoll)
                  .Append(s.TheftStruck ? "!" : "-");
                foreach (var t in s.TheftTargets) sb.Append(t).Append(';');
            }
            AppendSorted(sb, "plant", PlantCounts);
            AppendSorted(sb, "harvest", HarvestCounts);
            AppendSorted(sb, "revenue", CropRevenue);
            AppendSorted(sb, "seasonIn", SeasonIncome);
            AppendSorted(sb, "weather", WeatherCounts);
            AppendSorted(sb, "event", EventFireCounts);

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                var hex = new StringBuilder(32);
                for (int i = 0; i < 16; i++) hex.Append(hash[i].ToString("x2"));
                return hex.ToString();
            }
        }

        static void AppendSorted(StringBuilder sb, string tag, Dictionary<string, int> map)
        {
            var keys = new List<string>(map.Keys);
            keys.Sort(StringComparer.Ordinal);
            sb.Append(';').Append(tag).Append('=');
            foreach (var k in keys) sb.Append(k).Append(':').Append(map[k]).Append(',');
        }
    }
}
