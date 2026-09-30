using System;
using System.Collections.Generic;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// 아무것도 단정하지 않고 **수치만 찍는다.** 데이터를 고칠 때 보는 창이다.
    /// 여기서 본 것으로 기준값을 낮추지 않는다 — 데이터를 고친다 (POC_FACTORY 5).
    /// </summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 전수_훑기의_결말_분포를_찍는다()
        {
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult full = TestWorld.Full(seed);
                TestContext.WriteLine("── 씨드 " + seed + " · " + TestWorld.Generations + "대 전수");
                TestContext.WriteLine("   훑은 정책 " + full.PoliciesExplored + " · 정산 " + full.RunsSettled);
                TestContext.WriteLine("   깨끗한 출구(마을 살고 희생자 0) " + full.CleanExits.Count);
                TestContext.WriteLine("   마을 살고 희생자 있음 " + full.AliveWithVictims.Count);

                Dictionary<Ending, int> dist = new Dictionary<Ending, int>();
                foreach (KeyValuePair<int, LedgerRun> kv in full.BestByVictimCount)
                {
                    int n;
                    dist.TryGetValue(kv.Value.Ending, out n);
                }
                List<int> counts = new List<int>(full.BestByVictimCount.Keys);
                counts.Sort();
                foreach (int vc in counts)
                {
                    LedgerRun r = full.BestByVictimCount[vc];
                    TestContext.WriteLine("   희생자 " + vc + "명 최선: " + r.EndingKorean
                        + " · " + r.GenerationsSurvived + "대 · 번영 " + r.Prosperity
                        + " · 기력 " + r.HouseVitality + " · 원한 " + r.Resentment
                        + " · 살아있음 " + r.VillageAlive + " · 점수 " + r.Score);
                }
                TestContext.WriteLine("   최선 전체: " + full.Best.EndingKorean + " 점수 " + full.Best.Score
                    + " 희생자 " + full.Best.VictimCount + " · " + full.Best.PolicyText);
            }
        }

        [Test]
        public void 사람을_바치지_않는_정책의_결말_분포를_찍는다()
        {
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult b = TestWorld.Bloodless(seed);
                TestContext.WriteLine("── 씨드 " + seed + " · 사람을 바치지 않는 전수 (" + b.PoliciesExplored + "가지)");
                List<Ending> keys = new List<Ending>(b.BloodlessEndings.Keys);
                keys.Sort();
                foreach (Ending e in keys)
                    TestContext.WriteLine("   " + Endings.Korean(e) + " " + b.BloodlessEndings[e] + "회차");
                TestContext.WriteLine("   깨끗한 출구 " + b.CleanExits.Count);
                if (b.BloodlessBest != null)
                    TestContext.WriteLine("   가장 오래 버틴 무혈 정책: " + b.BloodlessBest.GenerationsSurvived
                        + "대 · " + b.BloodlessBest.EndingKorean + " · 번영 " + b.BloodlessBest.Prosperity
                        + " · " + b.BloodlessBest.PolicyText);
            }
        }

        [Test]
        public void 한_제례만_되풀이하는_정책들을_찍는다()
        {
            foreach (string riteId in TestWorld.Data.RiteIds())
            {
                LedgerRun r = TestWorld.MonoRun(TestWorld.MainSeed, riteId);
                TestContext.WriteLine(riteId.PadRight(16) + " " + r.GenerationsSurvived + "대 · "
                    + r.EndingKorean + " · 희생 " + r.VictimCount + " · 번영 " + r.Prosperity
                    + " · 속박 " + r.Binding + " · 노여움 " + r.Wrath + " · 기력 " + r.HouseVitality
                    + " · 원한 " + r.Resentment + " · 해제 " + r.ReleaseProgress + " · 점수 " + r.Score);
            }
        }

        [Test]
        public void 한_회차의_장부를_줄줄이_찍는다()
        {
            SweepResult full = TestWorld.Full(TestWorld.MainSeed);
            LedgerRun r = full.Best;
            TestContext.WriteLine("최선 정책 (잣대 기준): " + r.PolicyText);
            foreach (LedgerLine l in r.Lines)
            {
                TestContext.WriteLine(l.Generation + "대 " + l.HeirName + " · " + l.RiteName
                    + " · 요구 " + l.DemandDue + " 갚음 " + l.Paid + " 미납 " + l.Unpaid
                    + (l.LeanYear ? " (흉년)" : "")
                    + " → 번영 " + l.Prosperity + " 속박 " + l.Binding + " 노여움 " + l.Wrath
                    + " 기력 " + l.HouseVitality + " 원한 " + l.Resentment);
                foreach (Bill b in l.BillsArrived)
                    TestContext.WriteLine("      청구서 도착 (" + (b.FromGeneration < 0 ? "앞 세대" : b.FromGeneration + "대")
                        + " · " + b.Source + "): " + b.Note);
                foreach (Bill b in l.BillsScheduled)
                    TestContext.WriteLine("      청구서 예약 → " + b.AtGeneration + "대: " + b.Note);
            }
            TestContext.WriteLine("결말 " + r.EndingKorean + " · 희생자 " + r.VictimCount);
            foreach (VictimRecord v in r.Victims)
                TestContext.WriteLine("   붉은 줄 " + v.LedgerLine + ": " + v.Name + " (" + v.Age + ") "
                    + v.Household + " — " + v.Note);
        }

        [Test]
        public void 제례마다_지연으로_오는_몫을_찍는다()
        {
            foreach (RiteDef r in TestWorld.Data.AllRites)
                TestContext.WriteLine(r.id.PadRight(16) + " 지연 비중 " + ConsequenceWeight.DeferredPercent(r)
                    + "% · 가장 먼 값 " + ConsequenceWeight.LongestDelay(r) + "대 뒤");
        }
    }
}
