using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Secretary.Data;
using Secretary.Sim;

namespace Secretary.Tests
{
    /// <summary>
    /// **SeedDeterminism — 같은 씨드면 같은 결과.** 나머지 검사기 전부의 전제다.
    /// </summary>
    [TestFixture]
    public class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드면_편지가_같은_날_온다()
        {
            GameData d = TestWorld.Data;
            foreach (string vid in TestWorld.VolumeIds())
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    SimState a = Office.Begin(d, vid, seed);
                    SimState b = Office.Begin(d, vid, seed);
                    Assert.That(b.Pending.Count + b.Docket.Count, Is.EqualTo(a.Pending.Count + a.Docket.Count));
                    Dictionary<string, int> days = new Dictionary<string, int>();
                    foreach (Letter l in a.Docket) days[l.Def.id] = l.ArrivedDay;
                    foreach (Letter l in a.Pending) days[l.Def.id] = l.ArrivedDay;
                    foreach (Letter l in b.Docket)
                        Assert.That(l.ArrivedDay, Is.EqualTo(days[l.Def.id]), l.Def.id + " 의 도착 날이 흔들린다");
                    foreach (Letter l in b.Pending)
                        Assert.That(l.ArrivedDay, Is.EqualTo(days[l.Def.id]), l.Def.id + " 의 도착 날이 흔들린다");
                }
        }

        [Test]
        public void 같은_씨드면_정책의_결과가_똑같다()
        {
            foreach (Policy p in Policy.All())
                foreach (string vid in TestWorld.VolumeIds())
                    for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                    {
                        SimState a = TestWorld.RunPolicy(vid, seed, p);
                        SimState b = TestWorld.RunPolicy(vid, seed, p);
                        Assert.That(b.Accrued, Is.EqualTo(a.Accrued), p.Name + " " + vid + " 씨드" + seed);
                        Assert.That(b.Grants, Is.EqualTo(a.Grants));
                        Assert.That(b.Denies, Is.EqualTo(a.Denies));
                        Assert.That(b.Log.Count, Is.EqualTo(a.Log.Count));
                        for (int i = 0; i < a.Log.Count; i++)
                        {
                            Assert.That(b.Log[i].PrayerId, Is.EqualTo(a.Log[i].PrayerId));
                            Assert.That(b.Log[i].Kind, Is.EqualTo(a.Log[i].Kind));
                            Assert.That(b.Log[i].Drawn, Is.EqualTo(a.Log[i].Drawn));
                        }
                    }
        }

        [Test]
        public void 같은_씨드면_빔_탐색도_같은_길을_고른다()
        {
            GameData d = TestWorld.Data;
            foreach (string vid in TestWorld.VolumeIds())
                for (int seed = 0; seed < 4; seed++)
                {
                    SimState a = PlanSearch.Best(d, vid, seed);
                    SimState b = PlanSearch.Best(d, vid, seed);
                    Assert.That(b.Accrued, Is.EqualTo(a.Accrued), vid + " 씨드" + seed);
                    Assert.That(b.Log.Count, Is.EqualTo(a.Log.Count));
                    for (int i = 0; i < a.Log.Count; i++)
                    {
                        Assert.That(b.Log[i].PrayerId, Is.EqualTo(a.Log[i].PrayerId));
                        Assert.That(b.Log[i].Kind, Is.EqualTo(a.Log[i].Kind));
                        Assert.That(b.Log[i].Debits.Count, Is.EqualTo(a.Log[i].Debits.Count));
                    }
                }
        }

        [Test]
        public void 씨드가_실제로_무엇을_바꾸는가()
        {
            // 씨드가 아무것도 바꾸지 않으면 재현성은 공짜로 지켜지지만 데이터가 죽어 있다.
            StringBuilder sb = new StringBuilder();
            GameData d = TestWorld.Data;
            foreach (string vid in TestWorld.VolumeIds())
            {
                HashSet<string> shapes = new HashSet<string>();
                HashSet<int> scores = new HashSet<int>();
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    SimState st = Office.Begin(d, vid, seed);
                    List<Letter> all = new List<Letter>(st.Docket);
                    all.AddRange(st.Pending);
                    all.Sort(delegate (Letter a, Letter b) { return string.CompareOrdinal(a.Def.id, b.Def.id); });
                    StringBuilder key = new StringBuilder();
                    foreach (Letter l in all) key.Append(l.ArrivedDay);
                    shapes.Add(key.ToString());
                    scores.Add(TestWorld.RunPolicy(vid, seed, new OnlyWhenItHelps()).Accrued);
                }
                Assert.That(shapes.Count, Is.GreaterThan(1), vid + " 에서 씨드가 도착 날을 바꾸지 않는다");
                Assert.That(scores.Count, Is.GreaterThan(1), vid + " 에서 씨드가 결과를 바꾸지 않는다");
                sb.AppendLine(vid + ": 서로 다른 도착표 " + shapes.Count + "가지 · 서로 다른 결과 " + scores.Count + "가지");
            }
            TestContext.Out.WriteLine(sb.ToString());
        }
    }
}
