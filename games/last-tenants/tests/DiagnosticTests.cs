using System.Text;
using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>판정하지 않고 보여 주기만 하는 테스트. 수치를 눈으로 보려고 둔다.</summary>
    [TestFixture]
    public class DiagnosticTests
    {
        [Test]
        public void 선택지_표를_찍는다()
        {
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            foreach (string bid in TestWorld.BuildingIds())
            {
                BuildingDef b = d.Building(bid);
                sb.AppendLine("== " + b.name + " (" + bid + ") 기본 손실 합 "
                              + Total(d, bid) + " · 유예 " + b.graceDaysTotal + "일");
                for (int seed = 0; seed < 4; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    sb.AppendLine("  씨드 " + seed + " · 아무도 돕지 않으면 " + m.NoOne.Total
                                  + " · 계획 노드 " + m.Nodes);
                    foreach (ChoiceRow r in m.Rows)
                        sb.AppendLine(string.Format(
                            "    {0} {1,-14} 침묵={2,-6} Lv{3}{4} 유예{5} 건짐{6,3} 물림{7,3} 총{8,4} 엿듣기{9}(침묵문{10})",
                            r.UnitNo, r.Name, r.SilenceKind, r.KnowledgeLevel,
                            r.Misled ? "*" : " ", r.GraceSpent, r.Relief, r.Spill, r.Total,
                            r.Listens, r.SilentDoorListens));
                    ChoiceRow best = m.BestByTotal();
                    sb.AppendLine("    최선: " + best.UnitNo + " " + best.Name + " 총 " + best.Total
                                  + " · 파레토 " + m.ParetoFront().Count + "개");
                }
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.Pass();
        }

        private static int Total(GameData d, string bid)
        {
            int t = 0;
            foreach (HouseholdDef h in d.HouseholdsOf(bid)) t += h.baseStakes;
            return t;
        }
    }
}
