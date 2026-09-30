using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// 공통 검사기 — `SeedDeterminism`. **나머지 전부의 전제다.**
    /// 같은 씨드로 두 번 돌려 같은 결과가 나오지 않으면 위의 핵 검사기들이 재는 것이 아무것도 아니다.
    /// </summary>
    [TestFixture]
    public sealed class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드_같은_정책은_글자_하나까지_같은_장부를_남긴다()
        {
            GameData d = TestWorld.Data;
            string[] policy = { "house_pays", "offer_young", "rite_reinforce", "offer_elder",
                                "close_ledger", "offer_young", "rite_release", "house_pays" };
            foreach (int seed in TestWorld.Seeds)
            {
                string a = Transcript(new CurseSim(d, seed, TestWorld.Generations).Run(policy));
                string b = Transcript(new CurseSim(d, seed, TestWorld.Generations).Run(policy));
                Assert.That(b, Is.EqualTo(a), "씨드 " + seed + " 에서 두 회차가 갈라졌다");
                TestContext.WriteLine("씨드 " + seed + " 장부 " + a.Length + "자 재현됨");
            }
        }

        [Test]
        public void 씨드를_바꾸면_흉년과_부름_순서가_달라진다()
        {
            GameData d = TestWorld.Data;
            string[] policy = { "offer_young", "offer_young", "offer_elder", "house_pays",
                                "rite_reinforce", "close_ledger", "offer_elder", "house_pays" };
            HashSet<string> seen = new HashSet<string>();
            foreach (int seed in TestWorld.Seeds)
            {
                CurseSim sim = new CurseSim(d, seed, TestWorld.Generations);
                StringBuilder sb = new StringBuilder();
                for (int g = 1; g <= TestWorld.Generations; g++) sb.Append(sim.LeanYearPercent(g)).Append(',');
                Assert.That(seen.Add(sb.ToString()), "씨드 " + seed + " 의 흉년 표가 다른 씨드와 같다");
                TestContext.WriteLine("씨드 " + seed + " 흉년표 " + sb + " → " + LedgerAudit.OneLine(sim.Run(policy)));
            }
            Assert.That(seen.Count, Is.EqualTo(TestWorld.Seeds.Length));
        }

        [Test]
        public void 전수_훑기도_두_번_돌려_같은_수를_낸다()
        {
            int seed = TestWorld.Seeds[0];
            SweepResult a = PolicySweep.Exhaustive(TestWorld.Data, seed, TestWorld.Generations);
            SweepResult b = PolicySweep.Exhaustive(TestWorld.Data, seed, TestWorld.Generations);
            Assert.That(b.RunsSettled, Is.EqualTo(a.RunsSettled));
            Assert.That(b.NodesVisited, Is.EqualTo(a.NodesVisited));
            Assert.That(b.CleanExits.Count, Is.EqualTo(a.CleanExits.Count));
            Assert.That(b.AliveWithVictims.Count, Is.EqualTo(a.AliveWithVictims.Count));
            Assert.That(b.Best.Score, Is.EqualTo(a.Best.Score));
            Assert.That(b.Best.PolicyText, Is.EqualTo(a.Best.PolicyText));
            TestContext.WriteLine("전수 " + a.RunsSettled + "결말 · 마디 " + a.NodesVisited + " 두 번 같았다");
        }

        [Test]
        public void 난수는_System_Random_뿐이고_씨드는_데이터에_적혀_있다()
        {
            // 설계 원칙 5. UnityEngine.Random 이 한 번이라도 들어오면 헤드리스 재현이 깨진다.
            Assert.That(TestWorld.Data.Curse.seed, Is.GreaterThan(0), "curse.json 에 씨드가 없다");
            Assert.That(TestWorld.Data.Balance.checkers.noCleanExitSeeds, Is.Not.Null.And.Not.Empty);
            Assert.That(TestWorld.Data.Balance.checkers.noCleanExitSeeds, Contains.Item(TestWorld.Data.Curse.seed),
                "검사기 씨드 목록에 본 씨드가 없다");
            TestContext.WriteLine("본 씨드 " + TestWorld.Data.Curse.seed + " · 검사 씨드 "
                + string.Join(", ", TestWorld.Data.Balance.checkers.noCleanExitSeeds));
        }

        private static string Transcript(LedgerRun r)
        {
            Assert.That(r, Is.Not.Null, "정책을 끝까지 돌리지 못했다");
            StringBuilder sb = new StringBuilder();
            sb.Append(r.Ending).Append('|').Append(r.GenerationsSurvived).Append('|')
              .Append(r.Prosperity).Append('|').Append(r.Binding).Append('|').Append(r.Wrath).Append('|')
              .Append(r.HouseVitality).Append('|').Append(r.Resentment).Append('|')
              .Append(r.ReleaseProgress).Append('|').Append(r.Coffers).Append('|').Append(r.Score).Append('\n');
            foreach (LedgerLine l in r.Lines)
            {
                sb.Append(l.Generation).Append(':').Append(l.RiteId).Append(':').Append(l.DemandDue)
                  .Append('/').Append(l.Paid).Append('/').Append(l.Unpaid)
                  .Append(':').Append(l.Prosperity).Append(',').Append(l.Binding).Append(',').Append(l.Wrath)
                  .Append(',').Append(l.HouseVitality).Append(',').Append(l.Resentment)
                  .Append(':').Append(string.Join("+", l.VictimIds)).Append('\n');
                foreach (Bill b in l.BillsArrived) sb.Append("  <").Append(b.Source).Append(b.Note).Append('\n');
                foreach (Bill b in l.BillsScheduled) sb.Append("  >").Append(b.AtGeneration).Append(b.Note).Append('\n');
            }
            foreach (VictimRecord v in r.Victims)
                sb.Append('#').Append(v.LedgerLine).Append(v.VillagerId).Append(v.Name).Append(v.Note).Append('\n');
            return sb.ToString();
        }
    }
}
