using NUnit.Framework;
using FarmRewindYear.Sim;

namespace FarmRewindYear.Tests
{
    /// 검사기가 아니다. 사람이 수치를 보려고 두는 표.
    [TestFixture]
    public class DiagnosticsTests
    {
        [Test]
        public void 되감기_사다리를_출력한다()
        {
            var d = Support.Data;
            Support.Line($"씨드 {d.Config.seed} · 한 해 {d.DaysPerYear}일 · 할당량 {d.Economy.quotaCoin} · 되감기 한도 {d.Plots.rewind.maxRewinds}회");
            Support.Line($"흔적: 되감기마다 모든 칸 토질 -{d.Plots.rewind.soilLossPerRewind} (바닥 {d.Plots.rewind.soilFloor})");
            Support.Line($"예지: 되감기마다 +{d.Config.foresightGainPerRewind}일");
            Support.Line("");

            foreach (var name in new[] { "Learner", "Stubborn", "Omniscient" })
            {
                var r = name == "Learner" ? Support.Learner : name == "Stubborn" ? Support.Stubborn : Support.Omniscient;
                Support.Line($"[{name}] 성공 시도 {r.SuccessAttempt} · 되감기 {r.RewindsUsed}회 · 한도 소진 {r.Exhausted} · 최고 수입 {r.BestIncomeCoin}({r.BestIncomeAttempt}번째)");
                Support.Line("시도 | 되감기 | 예지 | 수입   | 지출   | 연말현금 | 봄토질 | 칸 | 심기 | 버림 | 달성");
                foreach (var a in r.Attempts)
                    Support.Line($"{a.Attempt,4} | {a.RewindsBefore,6} | {a.ForesightDays,4} | {a.IncomeCoin,6} | {a.ExpenseCoin,6} | {a.FinalMoneyCoin,8} | {a.SoilSumAtStart,6} | {a.ActivePlots,2} | {a.PlantingsTotal,4} | {a.CropsLost,4} | {(a.QuotaMet ? "O" : "X")}");
                Support.Line("");
            }

            Support.Line("[Ladder] 완전한 지식으로 되감기를 끝까지 밀어 본 것 — 흔적만이 변수다");
            Support.Line("되감기 | 봄토질 | 수입   | 연말현금 | 달성");
            foreach (var a in Support.Ladder.Attempts)
                Support.Line($"{a.RewindsBefore,6} | {a.SoilSumAtStart,6} | {a.IncomeCoin,6} | {a.FinalMoneyCoin,8} | {(a.QuotaMet ? "O" : "X")}");
            Support.Line("");

            Support.Line("심은 횟수 (Learner):");
            foreach (var c in d.Crops.crops)
                Support.Line($"  {c.nameKo,-8} 심기 {Support.Learner.Plant(c.id),4}회 · 수확 {Support.Learner.Harvest(c.id),4}회");
            Support.Line("");
            Support.Line("계절 수지 (Learner):");
            foreach (var s in d.Seasons.seasons)
                Support.Line($"  {s.nameKo,-4} 수입 {Support.Learner.Income(s.id),7} · 지출 {Support.Learner.Expense(s.id),7}");
            Support.Line("");
            Support.Line("한 해의 날씨와 달:");
            var sim = new Simulation(d, PolicyKind.Learner);
            for (int day = 0; day < d.DaysPerYear; day += 7)
                Support.Line($"  {day + 1,3}일 {d.SeasonOfDay(day).nameKo} {sim.Weather.At(day).nameKo} · 달 {d.MoonOfDay(day).nameKo}");
            Assert.Pass();
        }
    }
}
