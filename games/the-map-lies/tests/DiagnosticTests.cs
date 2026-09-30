using System.Collections.Generic;
using MapLies.Data;
using MapLies.Sim;
using NUnit.Framework;

namespace MapLies.Tests
{
    /// <summary>
    /// 판정하지 않고 **수치만 찍는다.** 검사기가 실패했을 때 기준값을 내리는 대신
    /// 데이터를 고쳐 다시 돌리려면 무엇이 얼마인지부터 보여야 한다.
    /// </summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 처음_도시()
        {
            GameData d = TestWorld.Data;
            CityGrid g = new CityGrid(d.City.width, d.City.height, d.City.actualRows);
            TestContext.WriteLine("길에 붙은 건물(집) " + g.Dwellings()
                                  + " · 건물 " + g.Count(Kinds.Block)
                                  + " · 길 " + g.Count(Kinds.Street)
                                  + " · 광장 " + g.Count(Kinds.Plaza));
            foreach (CitizenDef c in d.AllCitizens)
                TestContext.WriteLine("    " + c.id + " (" + c.x + "," + c.y + ") "
                                      + Kinds.Name(g.At(c.x, c.y))
                                      + (g.ReachesExit(c.x, c.y, d.City.exits) ? " 문에 닿는다" : " **갇혀 있다**"));
            MapPlan plan = new MapPlan(d);
            TestContext.WriteLine("고칠 수 있는 칸 " + plan.EditableCells().Count + " / " + (g.Width * g.Height));
        }

        [Test]
        public void 아무것도_그리지_않으면()
        {
            GameData d = TestWorld.Data;
            CitySim sim = new CitySim(d, new MapPlan(d), d.Balance.seed);
            sim.RunToFixedPoint();
            TestContext.WriteLine("닿았나 " + sim.Converged + " (" + sim.ConvergedOnDay + "일) · 주기 " + sim.DetectCyclePeriod(d.Balance.quietDays * 3));
            TestContext.WriteLine("거부된 칸 " + sim.RefusedCells().Count + " · 어긋남 " + sim.Actual.Disagreement(sim.Plan.Grid));
            TestContext.WriteLine("집 " + sim.Actual.Dwellings() + " · 갇힌 사람 " + string.Join(",", sim.Trapped())
                                  + " · 해 " + sim.TotalHarm);
            foreach (string row in sim.Actual.Rows()) TestContext.WriteLine("    " + row);
        }

        [Test]
        public void 정책별_결과()
        {
            TestContext.WriteLine("정책               장                점수    목표달성/씨드  집   광장  해    허가  날   거부  수렴");
            foreach (string p in Policies.Names())
            {
                foreach (string cid in TestWorld.ChapterIds())
                {
                    ChapterResult r = TestWorld.Run(cid, p, 0);
                    TestContext.WriteLine(Pad(p, 19) + Pad(cid, 18)
                        + Pad(TestWorld.MeanScore(cid, p).ToString(), 8)
                        + Pad(TestWorld.GoalMetCount(cid, p) + "/" + TestWorld.Data.Balance.trialSeeds, 15)
                        + Pad(r.Dwellings.ToString(), 5) + Pad(r.PlazaSize.ToString(), 6)
                        + Pad(r.Harm.ToString(), 6) + Pad(r.PermitSpent.ToString(), 6)
                        + Pad(r.Days.ToString(), 5) + Pad(r.Refused.ToString(), 6) + r.Converged);
                }
                TestContext.WriteLine(Pad(p, 19) + Pad("(전체 평균)", 18) + TestWorld.MeanScore(p));
                TestContext.WriteLine("");
            }
        }

        [Test]
        public void 왜_실패했나()
        {
            foreach (string p in Policies.Names())
                foreach (string cid in TestWorld.ChapterIds())
                {
                    ChapterResult r = TestWorld.Run(cid, p, 0);
                    if (r.GoalMet) continue;
                    List<string> why = new List<string>();
                    if (!r.ReachOk) why.Add("닿지 않는다");
                    if (!r.DwellingOk) why.Add("집이 " + r.Dwellings + " (필요 " + TestWorld.Data.Chapter(cid).dwellingMin + ")");
                    if (!r.PlazaOk) why.Add("광장이 " + r.PlazaSize + " (필요 " + TestWorld.Data.Chapter(cid).plazaMin + ")");
                    if (!r.NoTrappedOk) why.Add("갇힌 사람 " + string.Join(",", r.TrappedAtEnd));
                    TestContext.WriteLine(Pad(p, 19) + Pad(cid, 18) + string.Join(" · ", why));
                }
        }

        [Test]
        public void 사고와_되돌리기()
        {
            foreach (string mid in TestWorld.MistakeIds())
                for (int t = 0; t < TestWorld.RescueSeeds; t++)
                {
                    RescueResult r = TestWorld.Rescue(mid, t);
                    TestContext.WriteLine(Pad(mid, 16) + "씨드" + t
                        + " 사고전갇힘 [" + string.Join(",", r.TrappedBeforeMistake) + "]"
                        + " 사고후갇힘 [" + string.Join(",", r.TrappedAfterMistake) + "]"
                        + " " + r.DayTrapped + "일"
                        + " 사고값 " + r.MistakeCost
                        + (r.Rescued
                           ? " → 되돌림 " + r.RescueStrokes.Count + "획 · " + r.RescueDays + "일 · "
                             + r.RescueCost + "원 · 남은해 " + r.HarmPaid
                           : " → **못 꺼냈다**"));
                    foreach (Stroke s in r.RescueStrokes) TestContext.WriteLine("        " + s);
                }
        }

        [Test]
        public void 필지_상한을_없애면()
        {
            GameData d = TestWorld.Data;
            SimOptions free = SimOptions.From(d);
            free.MaxFlipsPerCell = -1;
            CitySim sim = new CitySim(d, new MapPlan(d), d.Balance.seed, free);
            sim.RunToFixedPoint();
            TestContext.WriteLine("상한 없음: 닿았나 " + sim.Converged + " · 주기 " + sim.DetectCyclePeriod(d.Balance.quietDays * 3)
                                  + " · " + sim.Day + "일 돌렸다");
            CitySim capped = new CitySim(d, new MapPlan(d), d.Balance.seed);
            capped.RunToFixedPoint();
            TestContext.WriteLine("상한 " + d.Balance.maxFlipsPerCell + ": 닿았나 " + capped.Converged
                                  + " (" + capped.ConvergedOnDay + "일) · 주기 " + capped.DetectCyclePeriod(d.Balance.quietDays * 3));
        }

        private static string Pad(string s, int n) { while (s.Length < n) s += " "; return s; }
    }
}
