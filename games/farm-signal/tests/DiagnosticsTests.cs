using NUnit.Framework;
using FarmSignal.Report;
using FarmSignal.Sim;

namespace FarmSignal.Tests
{
    /// **검사기가 아니다.** 사람이 수치를 보려고 두는 표다.
    /// 통과/실패를 주장하지 않는다 — 주장하는 것은 나머지 검사기 파일들이다.
    [TestFixture]
    public class DiagnosticsTests
    {
        [Test]
        public void 두_세계의_정책표를_찍는다()
        {
            var d = Support.Data;
            Support.Line("정책            세계   최종현금    수입     지출   씨앗   도둑(회/칸)  평판  방문  지수범위  빈칸일");
            foreach (var kind in Support.MainPolicies)
                foreach (var on in new[] { false, true })
                {
                    var r = Support.Run(kind, on);
                    Support.Line($"{kind,-15}{(on ? "켬" : "끔"),-5}{r.FinalMoneyCoin,9}{r.TotalIncomeCoin,9}" +
                                 $"{r.TotalExpenseCoin,9}{r.SeedSpentCoin,7}" +
                                 $"{r.TheftCount,7}/{r.TheftPlotsLost,-5}{r.MaxRenown,6}{r.MaxVisitors,6}" +
                                 $"  {r.MinPriceIndexSeen,3}~{r.MaxPriceIndexSeen,-4}{r.EmptyPlotDays,7}");
                }

            Support.Line("");
            Support.Line("첫 사건이 몇 번째 계절에 오나 (신호 켬) — ProgressionPacing 이 보는 값");
            Support.Line("정책             첫 경고  첫 붕괴  첫 도둑  첫 방문자  최대평판  최대방문");
            foreach (var kind in Support.MainPolicies)
            {
                var r = Support.Run(kind, true);
                Support.Line($"{kind,-16}{r.FirstWarnSeason,7}{r.FirstCollapseSeason,9}{r.FirstTheftSeason,9}" +
                             $"{r.FirstVisitorSeason,10}{r.MaxRenown,10}{r.MaxVisitors,10}");
            }

            Support.Line("");
            Support.Line("작물별 단작 (신호 켬 / 끔) — 최종현금");
            foreach (var c in d.Crops.crops)
            {
                var on = Support.Run(PolicyKind.MonoCrop, true, 0, c.id);
                var off = Support.Run(PolicyKind.MonoCrop, false, 0, c.id);
                Support.Line($"  {c.nameKo,-8}{on.FinalMoneyCoin,9}{off.FinalMoneyCoin,9}   심음 {on.Plant(c.id),4} 걷음 {on.Harvest(c.id),4}");
            }

            Support.Line("");
            Support.Line("작물별 심음/걷음/매출 (정책 SignalAware · 신호 켬)");
            var a = Support.Aware;
            foreach (var c in d.Crops.crops)
                Support.Line($"  {c.nameKo,-8} 심음 {a.Plant(c.id),4}  걷음 {a.Harvest(c.id),4}  매출 {a.Revenue(c.id),8}");

            Support.Line("");
            Support.Line("계절별 수입/지출 (SignalAware · 신호 켬)");
            foreach (var s in d.Seasons.seasons)
                Support.Line($"  {s.nameKo,-4} 수입 {a.Income(s.id),8}  지출 {a.Expense(s.id),8}");
        }

        [Test]
        public void 신호_이력을_계절마다_찍는다()
        {
            var d = Support.Data;
            foreach (var kind in new[] { PolicyKind.Blind, PolicyKind.SignalAware })
            {
                var r = Support.Run(kind, true);
                Support.Line("");
                Support.Line($"── {kind} · 신호 켬 ─────────────────────────────────────");
                var head = "계절  ";
                foreach (var c in d.Crops.crops) head += c.nameKo.PadLeft(7);
                Support.Line(head + "   평판 방문 웃돈  위험 주사위 도둑");
                foreach (var s in r.SignalHistory)
                {
                    var line = $"{s.Year}년{d.Season(s.SeasonId).nameKo,-2}";
                    for (int c = 0; c < d.CropCount; c++)
                        line += $"{s.PriceAfter[c],4}/{s.ObservedIndex[c],-3}".PadLeft(7);
                    line += $"{s.RenownAfter,6}{s.Visitors,5}{s.PremiumPercent,5}{s.TheftRiskPer1000,6}{s.TheftRoll,7}";
                    line += s.TheftStruck ? "  " + s.TheftPlotsHit + "칸 " + string.Join(",", s.TheftTargets) : "  -";
                    Support.Line(line);
                }
            }
        }

        [Test]
        public void 계획별_시세판을_찍는다()
        {
            foreach (var plan in Support.Data.Showcase.plans)
                foreach (var on in new[] { true, false })
                    Support.Print(Support.Board(plan.id, on).Report(Support.Data));
        }

        [Test]
        public void 백년_연차표를_찍는다()
        {
            var r = Support.Run(PolicyKind.SignalAware, true, Support.EconomyYears);
            Support.Line("연차   수입     지출    현금   도둑칸  관측합  평판  최저지수");
            for (int i = 0; i < r.YearRecords.Count; i++)
            {
                var y = r.YearRecords[i];
                if (i < 12 || i % 10 == 0 || i >= r.YearRecords.Count - 3)
                    Support.Line($"{y.Year,4}{y.IncomeCoin,8}{y.ExpenseCoin,9}{y.MoneyAtYearEnd,9}" +
                                 $"{y.TheftPlotsLost,7}{y.ObservedIndexSum,8}{y.RenownAtYearEnd,6}{y.MinPriceIndexAtYearEnd,8}");
            }
            Support.Line($"최종 현금 {r.FinalMoneyCoin} · 파산 {r.BankruptYear}년");
        }

        [Test]
        public void 콘텐츠_예산을_찍는다()
        {
            Support.Print(ContentBudget.Compute(Support.Data, Support.Aware).Report());
        }
    }
}
