using System.Text;
using FarmRewindYear.Data;
using FarmRewindYear.Sim;

namespace FarmRewindYear.Report
{
    /// PLAN_FARMING §5: "이 장르에서 프로젝트를 죽이는 것은 밸런스가 아니라
    /// 필요한 콘텐츠 양을 늦게 아는 것이다." 그 수를 여기서 뽑는다.
    ///
    /// 이 훅(되돌릴 수 있는 한 해)의 실제 값이 여기서 드러난다 — 플레이 시간이 늘어도
    /// **달력은 한 해뿐**이다. 되감기는 콘텐츠를 다시 쓰지 않고 다시 쓴다(재사용한다).
    /// 새로 써야 하는 것은 되감기 단계마다의 변주뿐이다.
    public sealed class ContentBudget
    {
        public int DaysPerYear;
        public int BeatIntervalDays;
        public int LinesPerEvent;
        public int RewindVariantLines;

        public int RewindsSeen;          // 플레이어가 실제로 보는 되감기 단계 수
        public int TotalDaysPlayed;      // 되감기를 포함해 실제로 플레이한 일수

        public int BaseEvents;           // 한 해를 채우는 고유 사건
        public int BaseLines;
        public int VariantLines;         // 되감기 단계마다의 변주
        public int NeededUniqueEvents;
        public int NeededLines;

        /// 같은 일수를 되감기 없이(확장형으로) 채우려면 필요했을 줄 수. 비교용.
        public int LinesIfNoReuse;

        public int HaveEvents;
        public int ShortfallEvents;
        public int ShortfallLines;

        public int BudgetEventCeiling;
        public int BudgetLineCeiling;
        public bool FitsBudget => NeededUniqueEvents <= BudgetEventCeiling && NeededLines <= BudgetLineCeiling;

        public static ContentBudget Compute(GameData d, RunResult run)
        {
            var b = new ContentBudget
            {
                DaysPerYear = d.DaysPerYear,
                BeatIntervalDays = d.Events.beatIntervalDays,
                LinesPerEvent = d.Events.linesPerEvent,
                RewindVariantLines = d.Events.rewindVariantLines,
                HaveEvents = d.Events.events.Length,
                BudgetEventCeiling = d.Events.budgetEventCeiling,
                BudgetLineCeiling = d.Events.budgetLineCeiling,
                RewindsSeen = run.RewindsUsed,
                TotalDaysPlayed = run.Attempts.Count * d.DaysPerYear,
            };
            b.BaseEvents = IntMath.CeilDiv(b.DaysPerYear, b.BeatIntervalDays);
            b.BaseLines = b.BaseEvents * b.LinesPerEvent;
            b.VariantLines = b.RewindsSeen * b.RewindVariantLines;
            b.NeededUniqueEvents = b.BaseEvents + b.RewindsSeen;   // 단계마다 사건 하나가 다시 쓰인다
            b.NeededLines = b.BaseLines + b.VariantLines;
            b.LinesIfNoReuse = IntMath.CeilDiv(b.TotalDaysPlayed, b.BeatIntervalDays) * b.LinesPerEvent;
            b.ShortfallEvents = b.NeededUniqueEvents - b.HaveEvents;
            b.ShortfallLines = b.NeededLines - b.HaveEvents * b.LinesPerEvent;
            return b;
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("── ContentBudget · farm-rewind-year ────────────────────────");
            sb.AppendLine($"달력                    한 해 {DaysPerYear}일. 되감아도 늘지 않는다");
            sb.AppendLine($"실제 플레이 일수        {TotalDaysPlayed}일 (되감기 {RewindsSeen}회 포함)");
            sb.AppendLine($"사건 간격               {BeatIntervalDays}일마다 새 사건 · 사건당 {LinesPerEvent}줄 · 되감기 변주 {RewindVariantLines}줄");
            sb.AppendLine($"한 해를 채우는 사건      {BaseEvents}개 = {BaseLines}줄");
            sb.AppendLine($"되감기 변주             {RewindsSeen}단계 x {RewindVariantLines}줄 = {VariantLines}줄");
            sb.AppendLine($"필요한 고유 사건        {NeededUniqueEvents}개");
            sb.AppendLine($"필요한 고유 대사        {NeededLines}줄");
            sb.AppendLine($"재사용이 없었다면       {LinesIfNoReuse}줄  ← 같은 플레이 시간을 확장형으로 채울 때");
            sb.AppendLine($"되감기가 아껴 준 양      {LinesIfNoReuse - NeededLines}줄 ({(LinesIfNoReuse == 0 ? 0 : (LinesIfNoReuse - NeededLines) * 100 / LinesIfNoReuse)}%)");
            sb.AppendLine($"지금 있는 사건          {HaveEvents}개 = {HaveEvents * LinesPerEvent}줄");
            sb.AppendLine($"부족분                  사건 {ShortfallEvents}개 · 대사 {ShortfallLines}줄");
            sb.AppendLine($"사람이 감당한다고 본 선  사건 {BudgetEventCeiling}개 · 대사 {BudgetLineCeiling}줄");
            sb.AppendLine($"예산 안에 드는가        {(FitsBudget ? "예" : "아니오 — 훅을 더 좁혀야 한다")}");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            return sb.ToString();
        }
    }
}
