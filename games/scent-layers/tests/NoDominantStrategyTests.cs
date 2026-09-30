using System.Collections.Generic;
using NUnit.Framework;
using Scent.Data;
using Scent.Sim;

namespace Scent.Tests
{
    /// <summary>
    /// 공통 검사기 — `NoDominantStrategy`.
    /// **한 가지 수를 되풀이하는 것이 최적이 아닌가.**
    ///
    /// 이 게임에서 되풀이할 수 있는 수는 셋이다:
    ///   ① 한 방에 죽치고 계속 맡는다 (창이 열릴 때까지 기다린다)
    ///   ② 같은 순서로 집을 두 바퀴 돈다 (아무 생각 없는 되짚기)
    ///   ③ 방금 맡은 방을 바로 한 번 더 맡는다 (제자리 반복)
    /// 셋 다 최적보다 나빠야 "어디로 되돌아갈지 고르는 일"이 게임으로 남는다.
    /// </summary>
    [TestFixture]
    public sealed class NoDominantStrategyTests
    {
        [Test]
        public void 한_방에_죽치는_것은_최적이_아니다()
        {
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            int cap = TestWorld.FactCount * d.Balance.checkers.campRecoveryMaxPercent / 100;
            int best = 0; string bestRoom = "";
            foreach (string room in d.RoomIds)
            {
                List<string> camp = new List<string>();
                for (int i = 0; i < d.Balance.investigation.maxSniffs; i++) camp.Add(room);
                RouteResult r = inv.Run(camp);
                TestContext.WriteLine("죽치기 " + room.PadRight(14) + r.recovered.Count + "/" + TestWorld.FactCount);
                Assert.That(r.recovered.Count, Is.LessThanOrEqualTo(cap),
                    room + " 에 죽치는 것만으로 " + r.recovered.Count + "/" + TestWorld.FactCount
                    + " 를 가져간다 — 돌아다닐 이유가 없다");
                if (r.recovered.Count > best) { best = r.recovered.Count; bestRoom = room; }
            }
            RouteResult opt = TestWorld.Revisit(World.On);
            Assert.That(best, Is.LessThan(opt.recovered.Count),
                "죽치기가 최적과 같거나 낫다 — 지배 전략이다");
            TestContext.WriteLine("가장 나은 죽치기 " + bestRoom + " " + best + "/" + TestWorld.FactCount
                + " · 최적 " + opt.recovered.Count + "/" + TestWorld.FactCount + " (상한 " + cap + ")");
        }

        [Test]
        public void 같은_순서로_두_바퀴_도는_것은_최적이_아니다()
        {
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            SweepReport s = TestWorld.Sweep(World.On);

            // 한 번 훑기에서 가장 나았던 순서를 그대로 한 바퀴 더 돈다
            List<string> twice = new List<string>(s.bestRoute);
            foreach (string room in s.bestRoute)
            {
                if (twice.Count >= d.Balance.investigation.maxSniffs) break;
                twice.Add(room);
            }
            RouteResult r = inv.Run(twice);
            RouteResult opt = TestWorld.Revisit(World.On);
            Assert.That(r.recovered.Count, Is.LessThan(opt.recovered.Count),
                "같은 순서로 두 바퀴 도는 것이 최적과 같다 — 되돌아갈 곳을 고르는 결정이 없다");
            TestContext.WriteLine("두 바퀴(같은 순서) " + r.recovered.Count + "/" + TestWorld.FactCount
                + " · 최적 " + opt.recovered.Count + "/" + TestWorld.FactCount);
            TestContext.WriteLine("    " + r.RouteText);
        }

        [Test]
        public void 방금_맡은_방을_바로_다시_맡는_것은_거의_헛일이다()
        {
            // 15분 뒤에 창이 열리는 우연은 있을 수 있다. 그것이 **규칙이 되면** 안 된다.
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            int pairs = 0, gained = 0;
            foreach (string room in d.RoomIds)
                foreach (string other in d.RoomIds)
                {
                    List<string> a = new List<string> { other, room };
                    List<string> b = new List<string> { other, room, room };
                    pairs++;
                    gained += inv.Run(b).recovered.Count - inv.Run(a).recovered.Count;
                }
            Assert.That(gained * 100, Is.LessThanOrEqualTo(pairs * 20),
                "제자리에서 한 번 더 맡는 것이 " + gained + "/" + pairs + " 꼴로 새 사실을 준다 — 반복이 전략이 된다");
            TestContext.WriteLine("제자리 재시도 " + pairs + "번 중 새 사실 " + gained + "개");
        }

        [Test]
        public void 어느_방도_혼자_판을_결정하지_않는다()
        {
            GameData d = TestWorld.Data;
            Dictionary<string, int> perRoom = new Dictionary<string, int>();
            foreach (VisitDef v in d.Day.visits)
                perRoom[v.room] = (perRoom.ContainsKey(v.room) ? perRoom[v.room] : 0) + 1;
            foreach (KeyValuePair<string, int> kv in perRoom)
                Assert.That(kv.Value * 100, Is.LessThanOrEqualTo(TestWorld.FactCount * 30),
                    kv.Key + " 에 사실이 " + kv.Value + "개 몰려 있다");
            List<string> parts = new List<string>();
            foreach (string r in d.RoomIds) parts.Add(r + " " + (perRoom.ContainsKey(r) ? perRoom[r] : 0));
            TestContext.WriteLine(string.Join(" · ", parts));
        }

        [Test]
        public void 이른_길과_늦은_길_어느_쪽도_혼자_이기지_않는다()
        {
            // "무조건 서둘러라"도 "무조건 기다려라"도 답이면 안 된다.
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            SweepReport s = TestWorld.Sweep(World.On);

            // 서두르기: 최고 순서로 한 바퀴만 돌고 끝낸다
            RouteResult hurry = inv.Run(s.bestRoute);

            // 기다리기: 앞쪽 다섯 번을 현관에서 흘려보내고 늦게 한 바퀴 돈다
            List<string> wait = new List<string>();
            for (int i = 0; i < 5; i++) wait.Add(d.Balance.investigation.startRoom);
            foreach (string room in s.bestRoute)
            {
                if (wait.Count >= d.Balance.investigation.maxSniffs) break;
                wait.Add(room);
            }
            RouteResult late = inv.Run(wait);
            RouteResult opt = TestWorld.Revisit(World.On);

            Assert.That(hurry.recovered.Count, Is.LessThan(opt.recovered.Count), "서두르기만으로 최적이다");
            Assert.That(late.recovered.Count, Is.LessThan(opt.recovered.Count), "기다리기만으로 최적이다");
            TestContext.WriteLine("서두르기 " + hurry.recovered.Count + " · 기다리기 " + late.recovered.Count
                + " · 섞기(최적) " + opt.recovered.Count + " / " + TestWorld.FactCount);
        }
    }
}
