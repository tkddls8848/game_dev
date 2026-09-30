using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 1 — `TickDeterminism`.
    /// **같은 씨드 · 같은 명령 = 같은 결과.** 나머지 여섯의 전제다.
    /// 이게 깨지면 아래 전부가 무엇을 재는지 알 수 없다 (PLAN_TACTICS §5).
    /// </summary>
    [TestFixture]
    public sealed class TickDeterminismTests
    {
        [Test]
        public void 같은_계획을_두_번_돌리면_로그가_같다()
        {
            foreach (KeyValuePair<string, SquadPlan> kv in ReferencePlans.ByMission())
            {
                MissionSim sim = TestWorld.Sim(kv.Key);
                MissionResult a = sim.Run(kv.Value);
                MissionResult b = sim.Run(kv.Value);

                Assert.That(b.LogHash, Is.EqualTo(a.LogHash), kv.Key + " 의 로그가 달라졌다");
                Assert.That(b.Won, Is.EqualTo(a.Won), kv.Key);
                Assert.That(b.Alarm, Is.EqualTo(a.Alarm), kv.Key);
                Assert.That(b.GuardsDown, Is.EquivalentTo(a.GuardsDown), kv.Key);
            }
        }

        [Test]
        public void 새_시뮬레이터에서도_로그가_같다()
        {
            // 시뮬레이터가 회차 사이에 상태를 흘리면 여기서 잡힌다 (틱 캐시·소음 캐시).
            GameData fresh = GameData.Load();
            foreach (KeyValuePair<string, SquadPlan> kv in ReferencePlans.ByMission())
            {
                string hashA = TestWorld.Sim(kv.Key).Run(kv.Value).LogHash;
                string hashB = new MissionSim(fresh, fresh.Mission(kv.Key)).Run(kv.Value).LogHash;
                Assert.That(hashB, Is.EqualTo(hashA), kv.Key + " 가 새 시뮬레이터에서 다른 로그를 냈다");
            }
        }

        [Test]
        public void 무작위_계획_240개가_두_번_같은_로그를_낸다()
        {
            GameData d = TestWorld.Data;
            int checked_ = 0;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                List<SquadPlan> plans = PlanSearch.RandomPlans(d, sim, d.Balance.search.seed, 80);
                Assert.That(plans.Count, Is.GreaterThan(0), missionId);
                foreach (SquadPlan p in plans)
                {
                    MissionResult a = sim.Run(p);
                    MissionResult b = sim.Run(p);
                    Assert.That(b.LogHash, Is.EqualTo(a.LogHash), missionId + " / " + p);
                    checked_++;
                }
            }
            Assert.That(checked_, Is.GreaterThanOrEqualTo(240), "표본이 너무 적다");
        }

        [Test]
        public void 같은_씨드는_같은_무작위_계획_목록을_낸다()
        {
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            List<SquadPlan> a = PlanSearch.RandomPlans(d, sim, 4242, 40);
            List<SquadPlan> b = PlanSearch.RandomPlans(d, sim, 4242, 40);
            List<SquadPlan> c = PlanSearch.RandomPlans(d, sim, 4243, 40);

            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int i = 0; i < a.Count; i++)
                Assert.That(b[i].ToString(), Is.EqualTo(a[i].ToString()), "표본 " + i);

            // 씨드가 실제로 쓰이는지 — 다른 씨드가 같은 목록을 내면 씨드가 배선돼 있지 않다는 뜻이다.
            bool differs = false;
            for (int i = 0; i < a.Count && i < c.Count; i++)
                if (c[i].ToString() != a[i].ToString()) { differs = true; break; }
            Assert.That(differs, "다른 씨드가 같은 계획 목록을 냈다 — 씨드가 배선되지 않았다");
        }

        [Test]
        public void 계획_탐색도_두_번_같은_답을_낸다()
        {
            GameData a = GameData.Load();
            GameData b = GameData.Load();
            foreach (string missionId in TestWorld.MissionIds())
            {
                SearchResult ra = PlanSearch.Solve(a, new MissionSim(a, a.Mission(missionId)));
                SearchResult rb = PlanSearch.Solve(b, new MissionSim(b, b.Mission(missionId)));
                Assert.That(rb.PlansSimulated, Is.EqualTo(ra.PlansSimulated), missionId);
                Assert.That(rb.Winners.Count, Is.EqualTo(ra.Winners.Count), missionId);
                for (int i = 0; i < ra.Winners.Count; i++)
                    Assert.That(rb.Winners[i].ToString(), Is.EqualTo(ra.Winners[i].ToString()), missionId + " 승리안 " + i);
            }
        }

        [Test]
        public void 씨드가_순찰_위상을_고르고_그_선택이_재현된다()
        {
            // 배포 데이터는 설계한 위상 하나씩만 갖는다. 씨드가 실제로 위상을 고르는지 보려면
            // 후보를 여럿 준 사본이 필요하다 — GameData 를 따로 읽어 그 사본만 고친다.
            GameData d = GameData.Load();
            d.Guard("g_sentry").phaseOptionsMs = new[] { 0, 55000, 110000 };
            d.Guard("g_rover").phaseOptionsMs = new[] { 0, 40000, 120000 };

            MissionDef mission = d.Mission("m_ridge_dusk");
            HashSet<string> phasePairs = new HashSet<string>();
            for (int seed = 1; seed <= 24; seed++)
            {
                mission.seed = seed;
                PatrolModel p1 = new PatrolModel(d, mission);
                PatrolModel p2 = new PatrolModel(d, mission);
                Assert.That(p2.BasePhaseMs("g_sentry"), Is.EqualTo(p1.BasePhaseMs("g_sentry")), "씨드 " + seed);
                Assert.That(p2.BasePhaseMs("g_rover"), Is.EqualTo(p1.BasePhaseMs("g_rover")), "씨드 " + seed);
                phasePairs.Add(p1.BasePhaseMs("g_sentry") + "/" + p1.BasePhaseMs("g_rover"));
            }
            Assert.That(phasePairs.Count, Is.GreaterThan(1),
                "씨드를 바꿔도 위상이 하나뿐이다 — 씨드가 순찰에 배선되지 않았다");
        }

        [Test]
        public void 배포_데이터의_위상은_설계한_하나뿐이다()
        {
            // 위 시험은 사본을 고쳐서 본다. 배포 데이터가 실제로 결정적인지는 따로 못 박는다 —
            // 이 PoC의 검사기 여섯이 전부 "설계한 창"을 전제로 수치를 읽기 때문이다.
            foreach (GuardDef g in TestWorld.Data.Patrols.guards)
                Assert.That(g.phaseOptionsMs.Length, Is.EqualTo(1),
                    g.id + " 에 위상 후보가 여럿이다. 후보를 늘리려면 AmbushSolvability 를 모든 후보에 대해 돌려야 한다");
        }
    }
}
