using System.Collections.Generic;
using NUnit.Framework;
using Scent.Data;
using Scent.Sim;

namespace Scent.Tests
{
    /// <summary>
    /// ★ 핵 검사기 곁들이 — `DecayIsFair`.
    /// **모든 사실은 어느 시점엔가 맡을 수 있었다.**
    ///
    /// 되짚기를 강제하는 것과 "닿을 수 없는 시점에만 있다 사라지는 사실"을 두는 것은 다르다.
    /// 뒤쪽은 그냥 고장이다 — 플레이어가 무엇을 해도 복원할 수 없는 사실이 있으면
    /// 이 게임은 풀 수 없고, 그러면 `MixingForcesRevisits` 의 "되돌아가면 전부"도 거짓이 된다.
    ///
    /// 그래서 사실마다 **창**을 직접 잰다: 조사 시간 전체를 1분 격자로 훑어
    /// "그 방에 그때 있었다면 이 사실을 복원했는가"를 본다. 창은
    ///   · 비어 있으면 안 되고
    ///   · 충분히 길어야 하고 (한 걸음 어긋났다고 영영 놓치면 벌이다)
    ///   · 플레이어가 그 방에 닿을 수 있는 가장 이른 시각보다 나중에 닫혀야 한다
    /// </summary>
    [TestFixture]
    public sealed class DecayIsFairTests
    {
        [Test]
        public void 모든_사실은_어느_순간엔가_맡을_수_있다()
        {
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            int min = int.MaxValue;
            foreach (VisitDef v in d.Day.visits)
            {
                List<int> w = inv.WindowOf(v.id);
                Assert.That(w.Count, Is.GreaterThan(0),
                    v.id + " (" + d.Actor(v.actor).ko + " · " + d.Room(v.room).ko + ") 은 조사 내내 한 번도 맡을 수 없다");
                Assert.That(w.Count, Is.GreaterThanOrEqualTo(d.Balance.checkers.fairWindowSlotsMin),
                    v.id + " 의 창이 " + w.Count + "분뿐이다 — 한 걸음 어긋났다고 영영 놓치는 것은 벌이다");
                if (w.Count < min) min = w.Count;
            }
            TestContext.WriteLine("사실 " + d.Day.visits.Count + "개 · 가장 좁은 창 " + min + "분 (하한 "
                + d.Balance.checkers.fairWindowSlotsMin + "분)");
        }

        [Test]
        public void 창은_끊기지_않는다()
        {
            // 창이 열렸다 닫혔다 하면 플레이어가 규칙을 배울 수 없다. 한 덩어리여야 한다.
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            foreach (VisitDef v in d.Day.visits)
            {
                List<int> w = inv.WindowOf(v.id);
                for (int i = 1; i < w.Count; i++)
                    Assert.That(w[i] - w[i - 1], Is.EqualTo(1),
                        v.id + " 의 창이 " + TestWorld.Clock(w[i - 1]) + " 와 " + TestWorld.Clock(w[i]) + " 사이에서 끊긴다");
            }
            TestContext.WriteLine("창 " + d.Day.visits.Count + "개가 모두 한 덩어리다");
        }

        [Test]
        public void 플레이어가_닿기_전에_닫히는_창이_없다()
        {
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            foreach (VisitDef v in d.Day.visits)
            {
                List<int> w = inv.WindowOf(v.id);
                int reach = inv.EarliestReach(v.room);
                Assert.That(w[w.Count - 1], Is.GreaterThanOrEqualTo(reach),
                    v.id + " 의 창은 " + TestWorld.Clock(w[w.Count - 1]) + " 에 닫히는데 "
                    + d.Room(v.room).ko + " 에 가장 빨리 닿아도 " + TestWorld.Clock(reach) + " 다");
                Assert.That(w[0], Is.LessThanOrEqualTo(d.Balance.investigation.endMin),
                    v.id + " 의 창이 밤이 끝난 뒤에 열린다");
            }
            TestContext.WriteLine("창 " + d.Day.visits.Count + "개가 모두 실제로 닿을 수 있는 자리에 있다");
        }

        [Test]
        public void 알아보는_것은_되짚는_것보다_늘_쉽다()
        {
            // 설계의 약속: 사람마다 자기만 가진 잔향이 있어서 **누구인지는 늦게라도 늘 안다.**
            // 어려운 것은 **언제**다. 이 구별이 깨지면 "묻혀서 존재조차 모른다"가 되고 그건 벌이다.
            GameData d = TestWorld.Data;
            Investigation inv = TestWorld.Inv(World.On);
            foreach (VisitDef v in d.Day.visits)
            {
                bool recognizedSomewhere = false;
                for (int t = d.Balance.investigation.startMin; t <= d.Balance.investigation.endMin; t += d.StepMin)
                {
                    Reading r = inv.N.Sniff(v.room, t, World.On);
                    if (r.recognized.Contains(v.actor)) { recognizedSomewhere = true; break; }
                }
                Assert.That(recognizedSomewhere, v.id + ": " + d.Actor(v.actor).ko + " 가 "
                    + d.Room(v.room).ko + " 에서 한 번도 알아볼 수 없다");
            }

            // 알아보는 창이 되짚는 창을 반드시 덮는다
            foreach (VisitDef v in d.Day.visits)
            {
                List<int> dateWindow = inv.WindowOf(v.id);
                foreach (int t in dateWindow)
                {
                    Reading r = inv.N.Sniff(v.room, t, World.On);
                    Assert.That(r.recognized.Contains(v.actor), Is.True,
                        v.id + ": 시각은 읽히는데 사람은 못 알아본다 — 모형이 어긋났다");
                }
            }
            TestContext.WriteLine("사실 " + d.Day.visits.Count + "개 전부: 누구인지는 늘 알 수 있고, 어려운 것은 언제인가다");
        }

        [Test]
        public void 잡내는_사실을_가려도_영영_묻지는_못한다()
        {
            // 잡내(씨드가 만든다)의 향 계열에 표지 잔향이 하나도 없어야 알아보기가 공정하다.
            GameData d = TestWorld.Data;
            HashSet<string> markers = new HashSet<string>();
            foreach (ActorDef a in d.Actors.actors) markers.Add(a.marker);
            foreach (string n in d.Day.ambient.notePool)
                Assert.That(markers.Contains(n), Is.False,
                    "잡내가 표지 잔향 " + n + " 을 쓴다 — 씨드에 따라 사람을 영영 못 알아볼 수 있다");
            TestContext.WriteLine("잡내 계열 " + d.Day.ambient.notePool.Count + "종 중 표지 잔향 0종");
        }
    }
}
