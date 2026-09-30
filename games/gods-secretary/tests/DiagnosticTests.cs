using System.Text;
using NUnit.Framework;
using Secretary.Data;
using Secretary.Sim;

namespace Secretary.Tests
{
    /// <summary>판정하지 않고 보여 주기만 하는 테스트. 수치를 눈으로 보려고 둔다.</summary>
    [TestFixture]
    public class DiagnosticTests
    {
        [Test]
        public void 장부와_정책을_찍는다()
        {
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            foreach (string vid in TestWorld.VolumeIds())
            {
                VolumeDef v = d.Volume(vid);
                Ledger l0 = new Ledger(d, v);
                sb.AppendLine("== " + v.name + " (" + vid + ") 첫날 고통 " + l0.TotalHardship()
                              + " · 날 " + v.days + " · 편지 " + d.PrayersOf(vid).Count);
                foreach (DomainDef dd in d.AllDomains)
                    sb.AppendLine("   " + dd.name + " 총량 " + l0.InitialTotal(dd.id));
                foreach (Policy p in Policy.All())
                {
                    int sum = 0;
                    StringBuilder per = new StringBuilder();
                    for (int seed = 0; seed < 8; seed++)
                    {
                        SimState st = TestWorld.RunPolicy(vid, seed, p);
                        sum += st.Accrued;
                        per.Append(st.Accrued).Append("(").Append(st.Grants).Append("허").Append(st.Denies)
                           .Append("기").Append(st.Defers).Append("보) ");
                    }
                    sb.AppendLine(string.Format("   {0,-16} 평균 {1,5} | {2}", p.Name, sum / 8, per.ToString()));
                }
                for (int seed = 0; seed < 4; seed++)
                {
                    SimState b = TestWorld.Best(vid, seed);
                    sb.AppendLine("   [알려진 최선 씨드" + seed + "] " + b.Accrued
                                  + " · 허가 " + b.Grants + " 기각 " + b.Denies + " 보류 " + b.Defers
                                  + " · 건너간 " + b.GrantedUnits + " 빠진 " + b.DrawnUnits);
                }
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.Pass();
        }
    }
}
