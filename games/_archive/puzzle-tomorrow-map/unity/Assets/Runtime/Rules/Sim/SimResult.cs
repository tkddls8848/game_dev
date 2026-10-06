using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FarmErosion.Sim
{
    public sealed class YearRecord
    {
        public int Year;
        public int IncomeCoin;
        public int ExpenseCoin;
        public int MoneyAtYearEnd;
        public int ActivePlotsAtYearEnd;
        public int PlotsLostThisYear;
        public int AssetAtYearEnd;      // 현금 + 남은 땅의 값
        public int CapacityAtYearEnd;   // 남은 칸의 토질 합. 이 게임이 실제로 줄어드는 것
    }

    public sealed class SimResult
    {
        public int Years;
        public int DaysSimulated;
        public List<YearRecord> YearRecords = new List<YearRecord>();

        public Dictionary<string, int> PlantCounts = new Dictionary<string, int>();
        public Dictionary<string, int> HarvestCounts = new Dictionary<string, int>();
        public Dictionary<string, int> SeasonIncome = new Dictionary<string, int>();
        public Dictionary<string, int> SeasonExpense = new Dictionary<string, int>();
        public Dictionary<string, int> EventFireCounts = new Dictionary<string, int>();
        public Dictionary<string, int> WeatherCounts = new Dictionary<string, int>();

        public int FirstLossYear = -1;
        public int HalfLandYear = -1;
        public int AllLostYear = -1;
        /// 마지막 칸을 잃은 날까지의 일수. 끝까지 남으면 시뮬레이션 전체 일수.
        public int PlayableDays;

        public int PeakIncomeYear = -1;
        public int PeakIncomeCoin;
        public int PeakAssetCoin;
        public int PeakAssetDay = -1;
        public int FinalMoneyCoin;
        public int TotalIncomeCoin;
        public int TotalExpenseCoin;
        public int DefensesBuilt;
        /// <summary>무너지기 전에 사람이 미리 접은 칸 수. 침식으로 잃은 것과 구별한다.</summary>
        public int PlotsAbandoned;
        public int UpkeepPaidCoin;
        /// <summary>방문해서 받은 셈의 합. 자급이 아니라 남에게서 온 것이다.</summary>
        public int GiftCoinReceived;
        /// <summary>의뢰로 내놓은 셈의 합. 의뢰는 보상이 아니라 이전이다.</summary>
        public int GiftCoinGiven;
        /// 파산한 연차. 파산하면 더 이상 심지도 쌓지도 못한다. -1이면 파산하지 않았다.
        public int BankruptYear = -1;
        public int BankruptDay = -1;

        public int Plant(string cropId) => PlantCounts.TryGetValue(cropId, out var v) ? v : 0;
        public int Harvest(string cropId) => HarvestCounts.TryGetValue(cropId, out var v) ? v : 0;
        public int Income(string seasonId) => SeasonIncome.TryGetValue(seasonId, out var v) ? v : 0;
        public int Expense(string seasonId) => SeasonExpense.TryGetValue(seasonId, out var v) ? v : 0;
        public int Fired(string eventId) => EventFireCounts.TryGetValue(eventId, out var v) ? v : 0;

        /// 씨드 재현성 검사가 비교하는 값. 결과 전체를 한 문자열로 굳혀 해시한다.
        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append("years=").Append(Years).Append(";days=").Append(DaysSimulated);
            sb.Append(";first=").Append(FirstLossYear).Append(";half=").Append(HalfLandYear);
            sb.Append(";all=").Append(AllLostYear).Append(";playable=").Append(PlayableDays);
            sb.Append(";peak=").Append(PeakAssetCoin).Append('@').Append(PeakAssetDay);
            sb.Append(";peakIn=").Append(PeakIncomeCoin).Append('@').Append(PeakIncomeYear);
            sb.Append(";money=").Append(FinalMoneyCoin);
            sb.Append(";in=").Append(TotalIncomeCoin).Append(";out=").Append(TotalExpenseCoin);
            sb.Append(";def=").Append(DefensesBuilt).Append(";upkeep=").Append(UpkeepPaidCoin);
            sb.Append(";bankrupt=").Append(BankruptYear).Append('@').Append(BankruptDay);
            foreach (var r in YearRecords)
                sb.Append('|').Append(r.Year).Append(',').Append(r.IncomeCoin).Append(',')
                  .Append(r.ExpenseCoin).Append(',').Append(r.MoneyAtYearEnd).Append(',')
                  .Append(r.ActivePlotsAtYearEnd).Append(',').Append(r.PlotsLostThisYear).Append(',')
                  .Append(r.AssetAtYearEnd);
            AppendSorted(sb, "plant", PlantCounts);
            AppendSorted(sb, "harvest", HarvestCounts);
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
