using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    [TestFixture]
    public class SilenceDiagnostics
    {
        [Test]
        public void 세_세계를_나란히_찍는다()
        {
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            foreach (string bid in TestWorld.BuildingIds())
            {
                HouseholdDef unable = TestWorld.Silent(bid, SilenceKinds.Unable);
                HouseholdDef gone = TestWorld.Silent(bid, SilenceKinds.Gone);
                sb.AppendLine("== " + d.Building(bid).name
                              + " unable=" + (unable == null ? "없음" : unable.id)
                              + " gone=" + (gone == null ? "없음" : gone.id));
                for (int seed = 0; seed < 8; seed++)
                {
                    ChoiceMatrix on = TestWorld.Matrix(bid, seed);
                    ChoiceMatrix noDoor = TestWorld.MatrixNoSilentDoors(bid, seed);
                    ChoiceMatrix asSound = TestWorld.MatrixSilenceAsSound(bid, seed);
                    string u = "-";
                    if (unable != null)
                        u = string.Format("unable 켠세계 {0}(Lv{1},엿{2}) · 문막음 {3} · 소리로바꿈 {4}(Lv{5},엿{6})",
                            on.Row(unable.id).Total, on.Row(unable.id).KnowledgeLevel, on.Row(unable.id).Listens,
                            noDoor.Row(unable.id).Total,
                            asSound.Row(unable.id).Total, asSound.Row(unable.id).KnowledgeLevel,
                            asSound.Row(unable.id).Listens);
                    sb.AppendLine(string.Format(
                        "  씨드{0} 최선 켠{1}/문막음{2}/소리{3} · none {4} · gone {5} · {6}",
                        seed, on.BestByTotal().Total, noDoor.BestByTotal().Total,
                        asSound.BestByTotal().Total, on.NoOne.Total,
                        gone == null ? -1 : on.Row(gone.id).Total, u));
                }
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.Pass();
        }

        [Test]
        public void 정책들을_찍는다()
        {
            StringBuilder sb = new StringBuilder();
            List<Policy> policies = Policy.All();
            foreach (string bid in TestWorld.BuildingIds())
            {
                sb.AppendLine("== " + TestWorld.Data.Building(bid).name);
                foreach (Policy p in policies)
                {
                    int sum = 0, wins = 0;
                    StringBuilder per = new StringBuilder();
                    for (int seed = 0; seed < 8; seed++)
                    {
                        Outcome o = Policy.Run(TestWorld.Night(bid, seed), p);
                        sum += o.Total;
                        per.Append(o.Total).Append(o.ChoiceIndex < 0 ? "x " : " ");
                        if (o.Total <= TestWorld.Matrix(bid, seed).BestByTotal().Total) wins++;
                    }
                    sb.AppendLine(string.Format("  {0,-18} 합{1,5} 평균{2,4} 최선일치{3} | {4}",
                        p.Name, sum, sum / 8, wins, per.ToString()));
                }
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.Pass();
        }
    }
}
