using System.Text;
using FarmSignal.Data;
using FarmSignal.Sim;

namespace FarmSignal.Report
{
    /// PLAN_FARMING §5: "이 장르에서 프로젝트를 죽이는 것은 밸런스가 아니라
    /// 필요한 콘텐츠 양을 늦게 아는 것이다." 그 수를 여기서 뽑는다.
    ///
    /// 이 훅(작물이 정보다)의 콘텐츠 값은 **격자**에 있다. 사건이 "무슨 일이 났나"가 아니라
    /// "남이 무엇을 보고 어떻게 반응했나"이므로, 쓸 것은 (반응 틀 x 작물)이다.
    /// 틀 하나를 쓰면 작물 수만큼 전문이 생긴다 — 이것이 확장형 파밍이 못 하는 절약이다.
    public sealed class ContentBudget
    {
        public int PlayableDays;
        public int PlayableYears;
        public int BeatIntervalDays;
        public int LinesPerEvent;

        public int NeededBeats;              // 채워야 하는 사건 자리
        public int NeededLinesIfAllUnique;   // 자리마다 새로 쓴다면

        public int TemplateCount;
        public int CropCount;
        public int DistinctTelegrams;        // 틀 x 작물
        public int WrittenLines;             // 실제로 사람이 쓰는 줄 수
        public int SavedLines;
        public int SavedPercent;

        public int HaveFixedEvents;
        public int HaveRepeatableEvents;

        public int BudgetEventCeiling;
        public int BudgetLineCeiling;

        public bool CoversBeats => DistinctTelegrams >= NeededBeats;
        public bool FitsBudget => WrittenLines <= BudgetLineCeiling && NeededBeats <= BudgetEventCeiling;

        public static ContentBudget Compute(GameData d, SimResult run)
        {
            var b = new ContentBudget
            {
                PlayableDays = run.PlayableDays,
                BeatIntervalDays = d.Events.beatIntervalDays,
                LinesPerEvent = d.Events.linesPerEvent,
                TemplateCount = d.Events.templates.Length,
                CropCount = d.CropCount,
                HaveFixedEvents = d.Events.events.Length,
                BudgetEventCeiling = d.Events.budgetEventCeiling,
                BudgetLineCeiling = d.Events.budgetLineCeiling,
            };
            b.PlayableYears = IntMath.CeilDiv(b.PlayableDays, d.DaysPerYear);
            b.NeededBeats = IntMath.CeilDiv(b.PlayableDays, b.BeatIntervalDays);
            b.NeededLinesIfAllUnique = b.NeededBeats * b.LinesPerEvent;
            b.DistinctTelegrams = b.TemplateCount * b.CropCount;
            // 쓰는 것: 틀의 본문 + 작물 이름 한 줄씩 + 날짜로 걸리는 고정 사건 한 줄씩
            b.WrittenLines = b.TemplateCount * b.LinesPerEvent + b.CropCount + b.HaveFixedEvents;
            b.SavedLines = b.NeededLinesIfAllUnique - b.WrittenLines;
            b.SavedPercent = b.NeededLinesIfAllUnique == 0
                ? 0 : b.SavedLines * 100 / b.NeededLinesIfAllUnique;
            foreach (var e in d.Events.events) if (e.once == 0) b.HaveRepeatableEvents++;
            return b;
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("── ContentBudget · farm-signal ─────────────────────────────");
            sb.AppendLine($"플레이 가능 일수        {PlayableDays}일 ({PlayableYears}년)");
            sb.AppendLine($"사건 간격               {BeatIntervalDays}일마다 새 사건 · 사건당 {LinesPerEvent}줄");
            sb.AppendLine($"채워야 하는 자리        {NeededBeats}개");
            sb.AppendLine($"자리마다 새로 쓴다면    {NeededLinesIfAllUnique}줄");
            sb.AppendLine($"전문 격자               반응 틀 {TemplateCount}개 x 작물 {CropCount}종 = 고유 전문 {DistinctTelegrams}통");
            sb.AppendLine($"격자가 자리를 덮는가    {(CoversBeats ? "예" : "아니오 — 틀이나 작물을 늘려야 한다")} ({DistinctTelegrams} >= {NeededBeats})");
            sb.AppendLine($"실제로 쓰는 줄          {WrittenLines}줄 (틀 {TemplateCount}x{LinesPerEvent} + 작물 이름 {CropCount} + 고정 사건 {HaveFixedEvents})");
            sb.AppendLine($"격자가 아껴 준 양       {SavedLines}줄 ({SavedPercent}%)");
            sb.AppendLine($"날짜로 걸리는 고정 사건 {HaveFixedEvents}개 (그중 반복 가능 {HaveRepeatableEvents}개)");
            sb.AppendLine($"사람이 감당한다고 본 선  사건 {BudgetEventCeiling}개 · 대사 {BudgetLineCeiling}줄");
            sb.AppendLine($"예산 안에 드는가        {(FitsBudget ? "예" : "아니오 — 훅을 더 좁혀야 한다")}");
            sb.AppendLine("기계가 못 보는 것        틀에서 뽑은 전문이 '사람이 쓴 글'로 읽히는가는 사람만 판정한다.");
            sb.AppendLine("                        이 표는 값을 세는 것이고, 값이 싸다는 것이 좋다는 뜻은 아니다.");
            sb.AppendLine("────────────────────────────────────────────────────────────");
            return sb.ToString();
        }
    }
}
