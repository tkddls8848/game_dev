using System.Collections.Generic;
using NUnit.Framework;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>
    /// 공통 검사기 — `SeedDeterminism`. **나머지 전부의 전제다.**
    /// 씨드는 조사 중에 끼어드는 잡음 알림을 만든다 — 그것이 밀려남의 속도를 정한다.
    /// </summary>
    [TestFixture]
    public sealed class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드는_같은_화면을_만든다()
        {
            GameData d = TestWorld.Data;
            for (int i = 0; i < TestWorld.C.seedsToCheck; i++)
            {
                int seed = d.Phone.seed + i * 613;
                Lockscreen a = Lockscreen.Build(d, seed);
                Lockscreen b = Lockscreen.Build(d, seed);
                Assert.That(b.Cards.Count, Is.EqualTo(a.Cards.Count), "씨드 " + seed);
                for (int k = 0; k < a.Cards.Count; k++)
                {
                    Assert.That(b.Cards[k].def.id, Is.EqualTo(a.Cards[k].def.id), "씨드 " + seed + " 카드 " + k);
                    Assert.That(b.Cards[k].def.arriveMin, Is.EqualTo(a.Cards[k].def.arriveMin));
                }
            }
            TestContext.WriteLine("씨드 " + TestWorld.C.seedsToCheck + "개가 두 번씩 같은 화면을 냈다");
        }

        [Test]
        public void 같은_씨드는_같은_조사_결과를_낸다()
        {
            GameData d = TestWorld.Data;
            World[] ws = { World.Lockscreen, World.NoPushOut, World.NoPreview, World.Unlocked };
            foreach (World w in ws)
            {
                SessionResult a = new Session(d, Lockscreen.Build(d, d.Phone.seed), w).MinimalSession();
                SessionResult b = new Session(d, Lockscreen.Build(d, d.Phone.seed), w).MinimalSession();
                Assert.That(b.Text, Is.EqualTo(a.Text), w.Name + " 의 일정이 달라졌다");
                Assert.That(b.verdicts.Count, Is.EqualTo(a.verdicts.Count), w.Name);
            }
            TestContext.WriteLine("네 세계가 두 번씩 같은 일정·같은 결과를 냈다");
        }

        [Test]
        public void 다른_씨드는_다른_잡음을_만든다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> shapes = new HashSet<string>();
            for (int i = 0; i < TestWorld.C.seedsToCheck; i++)
            {
                Lockscreen s = Lockscreen.Build(d, d.Phone.seed + i * 613);
                List<string> noise = new List<string>();
                foreach (Card c in s.Cards) if (c.def.kind == "noise") noise.Add(c.def.arriveMin + ":" + c.def.app);
                shapes.Add(string.Join("|", noise));
            }
            Assert.That(shapes.Count, Is.GreaterThan(1), "씨드를 바꿔도 잡음이 같다 — 씨드가 아무것도 하지 않는다");
            TestContext.WriteLine("씨드 " + TestWorld.C.seedsToCheck + "개가 서로 다른 잡음 " + shapes.Count + "가지를 냈다");
        }

        [Test]
        public void 씨드를_바꿔도_핵_검사기의_부호가_뒤집히지_않는다()
        {
            GameData d = TestWorld.Data;
            List<string> rows = new List<string>();
            for (int i = 0; i < TestWorld.C.seedsToCheck; i++)
            {
                int seed = d.Phone.seed + i * 613;
                Lockscreen s = Lockscreen.Build(d, seed);
                SessionResult on = new Session(d, s, World.Lockscreen).MinimalSession();
                SessionResult off = new Session(d, s, World.NoPushOut).MinimalSession();
                rows.Add(seed + ": 밀려남o " + on.wakes.Count + "번 " + on.verdicts.Count + "/3 · x " + off.wakes.Count + "번 " + off.verdicts.Count + "/3");
                Assert.That(on.verdicts.Count, Is.EqualTo(TestWorld.VerdictCount),
                    "씨드 " + seed + " 에서는 풀 수 없다: " + on.Text);
                Assert.That(off.verdicts.Count, Is.EqualTo(TestWorld.VerdictCount), "씨드 " + seed + " 의 대조군이 무너졌다");
                Assert.That(on.wakes.Count - off.wakes.Count, Is.GreaterThanOrEqualTo(TestWorld.C.wakeGapMin),
                    "씨드 " + seed + " 에서 밀려남이 늘린 깨우기가 " + (on.wakes.Count - off.wakes.Count) + "번뿐이다");
            }
            TestContext.WriteLine(string.Join("  ·  ", rows));
        }
    }
}
