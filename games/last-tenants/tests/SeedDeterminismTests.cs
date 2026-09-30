using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// **SeedDeterminism — 같은 씨드면 같은 결과.** 나머지 검사기 전부의 전제다.
    /// 여기가 깨지면 「침묵이 결과를 바꾼다」 같은 주장은 측정이 아니라 우연이 된다.
    /// </summary>
    [TestFixture]
    public class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드로_두_번_돌리면_소리가_똑같이_난다()
        {
            foreach (string bid in TestWorld.BuildingIds())
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    Night a = Night.Resolve(TestWorld.Data, bid, seed);
                    Night b = Night.Resolve(TestWorld.Data, bid, seed);
                    Assert.That(b.Cues.Count, Is.EqualTo(a.Cues.Count));
                    for (int i = 0; i < a.Cues.Count; i++)
                    {
                        Assert.That(b.Cues[i].CueId, Is.EqualTo(a.Cues[i].CueId));
                        Assert.That(b.Cues[i].Slot, Is.EqualTo(a.Cues[i].Slot), a.Cues[i].CueId + " 의 시간대가 흔들린다");
                        Assert.That(b.Cues[i].IsLie, Is.EqualTo(a.Cues[i].IsLie), a.Cues[i].CueId + " 의 참거짓이 흔들린다");
                        Assert.That(b.Cues[i].NeedId, Is.EqualTo(a.Cues[i].NeedId));
                    }
                }
        }

        [Test]
        public void 같은_씨드면_최선_계획과_총점이_똑같다()
        {
            foreach (string bid in TestWorld.BuildingIds())
                for (int seed = 0; seed < 6; seed++)
                {
                    ChoiceMatrix a = ChoiceMatrix.Build(Night.Resolve(TestWorld.Data, bid, seed));
                    ChoiceMatrix b = ChoiceMatrix.Build(Night.Resolve(TestWorld.Data, bid, seed));
                    Assert.That(b.Rows.Count, Is.EqualTo(a.Rows.Count));
                    for (int i = 0; i < a.Rows.Count; i++)
                    {
                        Assert.That(b.Rows[i].Total, Is.EqualTo(a.Rows[i].Total), bid + " " + a.Rows[i].Id);
                        Assert.That(b.Rows[i].Relief, Is.EqualTo(a.Rows[i].Relief));
                        Assert.That(b.Rows[i].Spill, Is.EqualTo(a.Rows[i].Spill));
                        Assert.That(b.Rows[i].KnowledgeLevel, Is.EqualTo(a.Rows[i].KnowledgeLevel));
                    }
                    Assert.That(b.Nodes, Is.EqualTo(a.Nodes), "탐색한 계획의 수까지 같아야 한다");
                }
        }

        [Test]
        public void 정책도_같은_씨드면_같은_결과를_낸다()
        {
            foreach (Policy p in Policy.All())
                foreach (string bid in TestWorld.BuildingIds())
                    for (int seed = 0; seed < 6; seed++)
                    {
                        Outcome a = Policy.Run(Night.Resolve(TestWorld.Data, bid, seed), p);
                        Outcome b = Policy.Run(Night.Resolve(TestWorld.Data, bid, seed), p);
                        Assert.That(b.Total, Is.EqualTo(a.Total), p.Name + " " + bid + " 씨드" + seed);
                        Assert.That(b.ChoiceIndex, Is.EqualTo(a.ChoiceIndex));
                    }
        }

        [Test]
        public void 씨드가_실제로_무엇을_바꾸는가()
        {
            // 씨드가 아무것도 바꾸지 않으면 재현성은 공짜로 지켜지지만 데이터가 죽어 있다.
            StringBuilder sb = new StringBuilder();
            int buildingsWithVariation = 0;
            foreach (string bid in TestWorld.BuildingIds())
            {
                HashSet<string> shapes = new HashSet<string>();
                HashSet<string> lies = new HashSet<string>();
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    Night n = Night.Resolve(TestWorld.Data, bid, seed);
                    StringBuilder key = new StringBuilder();
                    foreach (ResolvedCue c in n.Cues) key.Append(c.Slot).Append(c.IsLie ? "!" : ".");
                    shapes.Add(key.ToString());
                    foreach (ResolvedCue c in n.Lies()) lies.Add(c.CueId);
                }
                sb.AppendLine(bid + ": 서로 다른 하루 " + shapes.Count + "가지 · 거짓으로 뽑힌 소리 " + lies.Count + "종");
                Assert.That(shapes.Count, Is.GreaterThan(1), bid + " 에서 씨드가 하루를 바꾸지 않는다");
                Assert.That(lies.Count, Is.GreaterThan(1), bid + " 에서 거짓이 늘 같은 소리다");
                buildingsWithVariation++;
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(buildingsWithVariation, Is.EqualTo(3));
        }
    }
}
