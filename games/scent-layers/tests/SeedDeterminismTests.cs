using System.Collections.Generic;
using NUnit.Framework;
using Scent.Data;
using Scent.Sim;

namespace Scent.Tests
{
    /// <summary>
    /// 공통 검사기 — `SeedDeterminism`. **나머지 전부의 전제다.**
    /// 같은 씨드로 두 번 돌려 다른 결과가 나오면 `MixingForcesRevisits` 의 9/14 도 의미가 없다.
    /// </summary>
    [TestFixture]
    public sealed class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드는_같은_냄새_지형을_만든다()
        {
            GameData d = TestWorld.Data;
            for (int s = 0; s < d.Balance.checkers.seedsToCheck; s++)
            {
                int seed = d.Day.seed + s * 977;
                ScentField a = ScentField.Build(d, seed);
                ScentField b = ScentField.Build(d, seed);
                Assert.That(b.All.Count, Is.EqualTo(a.All.Count), "씨드 " + seed);
                for (int i = 0; i < a.All.Count; i++)
                {
                    Assert.That(b.All[i].id, Is.EqualTo(a.All[i].id), "씨드 " + seed + " 층 " + i);
                    Assert.That(b.All[i].roomId, Is.EqualTo(a.All[i].roomId));
                    Assert.That(b.All[i].atMin, Is.EqualTo(a.All[i].atMin));
                    Assert.That(b.All[i].initial, Is.EqualTo(a.All[i].initial));
                }
            }
            TestContext.WriteLine("씨드 " + d.Balance.checkers.seedsToCheck + "개가 두 번씩 같은 지형을 냈다");
        }

        [Test]
        public void 같은_씨드는_같은_조사_결과를_낸다()
        {
            GameData d = TestWorld.Data;
            World[] worlds = { World.Off, World.MixingOnly, World.DecayOnly, World.On };
            foreach (World w in worlds)
            {
                Investigation a = new Investigation(d, ScentField.Build(d, d.Day.seed), w);
                Investigation b = new Investigation(d, ScentField.Build(d, d.Day.seed), w);
                RouteResult ra = a.BestRouteWithRevisits(d.Day.seed, 8);
                RouteResult rb = b.BestRouteWithRevisits(d.Day.seed, 8);
                Assert.That(rb.RouteText, Is.EqualTo(ra.RouteText), w.Name + " 의 길이 달라졌다");
                Assert.That(rb.recovered.Count, Is.EqualTo(ra.recovered.Count), w.Name);
                Assert.That(rb.endMin, Is.EqualTo(ra.endMin), w.Name);
            }
            TestContext.WriteLine("네 세계가 두 번씩 같은 길·같은 복원 수를 냈다");
        }

        [Test]
        public void 다른_씨드는_다른_잡내를_만든다()
        {
            // 씨드가 아무것도 바꾸지 않으면 `SeedDeterminism` 은 빈 검사기다.
            GameData d = TestWorld.Data;
            HashSet<string> shapes = new HashSet<string>();
            for (int s = 0; s < d.Balance.checkers.seedsToCheck; s++)
            {
                ScentField f = ScentField.Build(d, d.Day.seed + s * 977);
                List<string> parts = new List<string>();
                foreach (Layer L in f.All) if (!L.IsVisit) parts.Add(L.roomId + "@" + L.atMin + ":" + string.Join(",", L.initial));
                shapes.Add(string.Join("|", parts));
            }
            Assert.That(shapes.Count, Is.GreaterThan(1), "씨드를 바꿔도 잡내가 같다 — 씨드가 아무것도 하지 않는다");
            TestContext.WriteLine("씨드 " + d.Balance.checkers.seedsToCheck + "개가 서로 다른 잡내 " + shapes.Count + "가지를 냈다");
        }

        [Test]
        public void 씨드를_바꿔도_핵_검사기의_부호는_뒤집히지_않는다()
        {
            // 잡내가 판을 뒤집으면 9/14 라는 수치가 이 씨드 하나의 우연이 된다.
            GameData d = TestWorld.Data;
            List<string> rows = new List<string>();
            for (int s = 0; s < d.Balance.checkers.seedsToCheck; s++)
            {
                int seed = d.Day.seed + s * 977;
                ScentField f = ScentField.Build(d, seed);
                int on = new Investigation(d, f, World.On).BestSingleSweep().bestCount;
                int off = new Investigation(d, f, World.Off).BestSingleSweep().bestCount;
                rows.Add(seed + ": 켠 " + on + " / 끈 " + off);
                Assert.That(off, Is.EqualTo(TestWorld.FactCount), "씨드 " + seed + " 의 대조군이 무너졌다");
                Assert.That(off - on, Is.GreaterThanOrEqualTo(d.Balance.checkers.sweepShortfallMin),
                    "씨드 " + seed + " 에서 한 번 훑기가 " + on + "/" + off + " 를 가져간다");
            }
            TestContext.WriteLine(string.Join("  ·  ", rows));
        }
    }
}
