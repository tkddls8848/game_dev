using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FarmRewindYear.Sim
{
    /// 한 해를 한 번 살아 본 기록. 되감기마다 하나씩 쌓인다.
    public sealed class AttemptRecord
    {
        public int Attempt;          // 1부터
        public int RewindsBefore;    // 이 시도에 들어가기 전까지 되감은 횟수
        public int ForesightDays;    // 이 시도에서 미리 알던 날 수
        public int IncomeCoin;
        public int ExpenseCoin;
        public int FinalMoneyCoin;
        public int SoilSumAtStart;
        public int SoilSumAtEnd;
        public int PlotsBought;
        public int ActivePlots;
        public int PlantingsTotal;
        public int CropsLost;
        public bool QuotaMet;
    }

    public sealed class RunResult
    {
        public List<AttemptRecord> Attempts = new List<AttemptRecord>();

        public Dictionary<string, int> PlantCounts = new Dictionary<string, int>();
        public Dictionary<string, int> HarvestCounts = new Dictionary<string, int>();
        public Dictionary<string, int> LostCounts = new Dictionary<string, int>();
        public Dictionary<string, int> SeasonIncome = new Dictionary<string, int>();
        public Dictionary<string, int> SeasonExpense = new Dictionary<string, int>();
        public Dictionary<string, int> EventFireCounts = new Dictionary<string, int>();
        public Dictionary<string, int> WeatherCounts = new Dictionary<string, int>();

        /// 할당량을 넘긴 시도(1부터). -1이면 끝내 못 넘겼다.
        public int SuccessAttempt = -1;
        public int RewindsUsed;
        /// 되감기 한도를 다 쓰고도 못 넘긴 경우.
        public bool Exhausted;
        public int PeakAssetCoin;
        public int BestIncomeCoin;
        public int BestIncomeAttempt = -1;

        public int Plant(string cropId) => PlantCounts.TryGetValue(cropId, out var v) ? v : 0;
        public int Harvest(string cropId) => HarvestCounts.TryGetValue(cropId, out var v) ? v : 0;
        public int Lost(string cropId) => LostCounts.TryGetValue(cropId, out var v) ? v : 0;
        public int Income(string seasonId) => SeasonIncome.TryGetValue(seasonId, out var v) ? v : 0;
        public int Expense(string seasonId) => SeasonExpense.TryGetValue(seasonId, out var v) ? v : 0;
        public int Fired(string eventId) => EventFireCounts.TryGetValue(eventId, out var v) ? v : 0;

        /// 씨드 재현성 검사가 비교하는 값.
        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append("success=").Append(SuccessAttempt).Append(";rewinds=").Append(RewindsUsed);
            sb.Append(";exhausted=").Append(Exhausted).Append(";peak=").Append(PeakAssetCoin);
            sb.Append(";best=").Append(BestIncomeCoin).Append('@').Append(BestIncomeAttempt);
            foreach (var a in Attempts)
                sb.Append('|').Append(a.Attempt).Append(',').Append(a.RewindsBefore).Append(',')
                  .Append(a.ForesightDays).Append(',').Append(a.IncomeCoin).Append(',')
                  .Append(a.ExpenseCoin).Append(',').Append(a.FinalMoneyCoin).Append(',')
                  .Append(a.SoilSumAtStart).Append(',').Append(a.SoilSumAtEnd).Append(',')
                  .Append(a.PlotsBought).Append(',').Append(a.ActivePlots).Append(',')
                  .Append(a.PlantingsTotal).Append(',').Append(a.CropsLost)
                  .Append(',').Append(a.QuotaMet ? 1 : 0);
            AppendSorted(sb, "plant", PlantCounts);
            AppendSorted(sb, "harvest", HarvestCounts);
            AppendSorted(sb, "lost", LostCounts);
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
