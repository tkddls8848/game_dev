using System.Collections.Generic;
using NUnit.Framework;
using Scent.Data;
using Scent.Sim;

namespace Scent.Tests
{
    /// <summary>
    /// ★ 핵 검사기 — `MixingForcesRevisits`.
    /// **섞임과 옅어짐이 실제로 되짚기를 강제하는가.**
    ///
    /// 한 번 훑는 것(각 방을 한 번씩, 어떤 순서로든)으로 모든 사실이 복원되면 섞임은 장식이다.
    /// 그래서 같은 데이터·같은 씨드·같은 정책으로 **네 세계**를 돌린다:
    ///
    ///   섞임x·감쇠x — 층이 굳어 그대로 읽힌다 (대조군)
    ///   섞임o·감쇠x — 섞이지만 옅어지지 않는다
    ///   섞임x·감쇠o — 옅어지지만 섞이지 않는다
    ///   섞임o·감쇠o — 이 PoC
    ///
    /// **대조군을 둘이나 더 둔 이유**가 있다. 끈 세계 하나만 두면 "무언가가 어렵게 만들었다"까지만
    /// 보이고 무엇이 그랬는지는 모른다. 셋을 견주면 이 PoC가 실제로 배운 것이 드러난다 —
    /// **섞임 단독도 감쇠 단독도 한 번 훑기를 막지 못한다. 둘이 물려야 막힌다.**
    /// (옅어져야 남의 냄새가 역치 아래로 내려가고, 그제서야 내 냄새의 순도가 열린다.)
    /// </summary>
    [TestFixture]
    public sealed class MixingForcesRevisitsTests
    {
        [Test]
        public void 섞임과_감쇠를_끈_세계는_한_번_훑기로_전부_복원된다()
        {
            SweepReport off = TestWorld.Sweep(World.Off);
            Assert.That(off.bestCount, Is.EqualTo(TestWorld.FactCount),
                "대조군에서조차 한 번 훑기로 전부 복원되지 않는다 — 비교의 바닥이 무너졌다");
            Assert.That(off.totalRecovered / off.sweeps, Is.EqualTo(TestWorld.FactCount),
                "대조군에서는 어떤 순서로 돌든 전부 나와야 한다");
            TestContext.WriteLine("섞임x·감쇠x: 순열 " + off.sweeps + "가지 전부가 "
                + TestWorld.FactCount + "/" + TestWorld.FactCount);
        }

        [Test]
        public void 한쪽만_켠_두_세계도_한_번_훑기를_막지_못한다()
        {
            // ★ 이 PoC가 실제로 배운 것. 통과가 아니라 **수치가 하는 말**이 중요하다.
            SweepReport mix = TestWorld.Sweep(World.MixingOnly);
            SweepReport dec = TestWorld.Sweep(World.DecayOnly);
            Assert.That(mix.bestCount, Is.EqualTo(TestWorld.FactCount),
                "섞임만으로 막혔다면 이 PoC의 설명이 틀린 것이다 — 모형을 다시 본다");
            Assert.That(dec.bestCount, Is.EqualTo(TestWorld.FactCount),
                "감쇠만으로 막혔다면 이 PoC의 설명이 틀린 것이다 — 모형을 다시 본다");
            TestContext.WriteLine("섞임o·감쇠x: 최고 " + mix.bestCount + " · 평균 " + (mix.totalRecovered / mix.sweeps));
            TestContext.WriteLine("섞임x·감쇠o: 최고 " + dec.bestCount + " · 평균 " + (dec.totalRecovered / dec.sweeps));
            TestContext.WriteLine("→ 한쪽만으로는 아무것도 막지 못한다. 막는 것은 둘의 맞물림이다.");
        }

        [Test]
        public void 둘_다_켠_세계는_한_번_훑기로_부족하다()
        {
            GameData d = TestWorld.Data;
            SweepReport on = TestWorld.Sweep(World.On);
            int shortfall = TestWorld.FactCount - on.bestCount;
            Assert.That(shortfall, Is.GreaterThanOrEqualTo(d.Balance.checkers.sweepShortfallMin),
                "한 번 훑기가 " + on.bestCount + "/" + TestWorld.FactCount
                + " 를 가져간다 — 섞임이 장식이다. 기준을 내리지 말고 데이터를 고쳐라");
            TestContext.WriteLine("섞임o·감쇠o: 순열 " + on.sweeps + "가지 중 최고 " + on.bestCount
                + "/" + TestWorld.FactCount + " · 평균 " + (on.totalRecovered / on.sweeps)
                + " · 모자란 사실 " + shortfall + "개");
            TestContext.WriteLine("    최고 순서: " + string.Join(" > ", on.bestRoute));
        }

        [Test]
        public void 되돌아가면_전부_복원된다()
        {
            GameData d = TestWorld.Data;
            RouteResult r = TestWorld.Revisit(World.On);
            Assert.That(r.recovered.Count, Is.EqualTo(TestWorld.FactCount),
                "되돌아가도 전부 복원되지 않는다 — 풀 수 없는 게임이다. 복원한 것: "
                + string.Join(",", new List<string>(r.recovered)));
            Assert.That(r.RevisitedRooms, Is.GreaterThanOrEqualTo(d.Balance.checkers.revisitRoomsMin),
                "되돌아간 방이 " + r.RevisitedRooms + "개뿐이다 — 되짚기가 강제되지 않는다");
            Assert.That(r.steps.Count, Is.LessThanOrEqualTo(d.Balance.investigation.maxSniffs));
            Assert.That(r.endMin, Is.LessThanOrEqualTo(d.Balance.investigation.endMin));
            TestContext.WriteLine("되짚기: " + r.recovered.Count + "/" + TestWorld.FactCount
                + " · 맡은 횟수 " + r.steps.Count + " · 되돌아간 방 " + r.RevisitedRooms
                + " · " + TestWorld.Clock(d.Balance.investigation.startMin) + "~" + TestWorld.Clock(r.endMin));
            TestContext.WriteLine("    " + r.RouteText);
        }

        [Test]
        public void 어떤_한_번_훑기로도_잡히지_않는_사실이_있다()
        {
            // 순열 720가지를 합쳐도 못 잡는 사실이 있어야 "되돌아가야 한다"가 말이 된다.
            SweepReport on = TestWorld.Sweep(World.On);
            List<string> never = new List<string>();
            foreach (string id in TestWorld.FactIds())
                if (!on.everRecoveredBySomeSweep.Contains(id)) never.Add(id);
            Assert.That(never.Count, Is.GreaterThan(0),
                "모든 사실이 어떤 순서로든 한 번 훑기로 잡힌다 — 되돌아갈 이유가 순서 문제로 줄어든다");
            GameData d = TestWorld.Data;
            List<string> pretty = new List<string>();
            foreach (string id in never)
            {
                VisitDef v = d.Visit(id);
                pretty.Add(id + "(" + d.Actor(v.actor).ko + "·" + d.Room(v.room).ko + " " + TestWorld.Clock(v.atMin) + ")");
            }
            TestContext.WriteLine("순열 720가지 어느 것으로도 못 잡는 사실 " + never.Count + "개: " + string.Join(", ", pretty));
        }

        [Test]
        public void 되짚기_길에서_늦게_열리는_창이_실제로_늦게_채워진다()
        {
            // 되짚기가 "그냥 더 많이 맡아서" 이긴 것이 아니라 **늦은 창을 기다려서** 이겼는지 본다.
            RouteResult r = TestWorld.Revisit(World.On);
            int half = r.steps.Count / 2;
            int early = 0, late = 0;
            for (int i = 0; i < r.newPerStep.Count; i++)
            {
                if (i < half) early += r.newPerStep[i]; else late += r.newPerStep[i];
            }
            Assert.That(late, Is.GreaterThan(0), "뒤쪽 절반이 아무것도 새로 주지 않았다 — 되짚기가 필요 없다는 뜻이다");
            TestContext.WriteLine("앞쪽 절반이 준 사실 " + early + "개 · 뒤쪽 절반이 준 사실 " + late + "개");
        }
    }
}
