using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 2 — `AmbushSolvability`.
    /// **적어도 한 계획이 경보 없이 초소를 지울 수 있는가.**
    /// 깨지면 풀 수 없는 임무가 되고, 플레이어는 자기 실력을 의심한다 (PLAN_TACTICS §5).
    ///
    /// 두 갈래로 본다. 사람이 적은 참조 계획이 이기는지(회귀)와,
    /// 사람의 의도를 모르는 탐색이 스스로 해를 찾는지(실제 주장).
    /// </summary>
    [TestFixture]
    public sealed class AmbushSolvabilityTests
    {
        [Test]
        public void 참조_계획이_모든_임무를_경보_없이_푼다()
        {
            foreach (KeyValuePair<string, SquadPlan> kv in ReferencePlans.ByMission())
            {
                MissionSim sim = TestWorld.Sim(kv.Key);
                MissionResult r = sim.Run(kv.Value);

                Assert.That(r.FailureReason, Is.Not.EqualTo("Infeasible"), kv.Key + " 의 참조 계획이 물리적으로 불가능하다");
                Assert.That(r.Won, kv.Key + " 의 참조 계획이 졌다: " + r.FailureReason + " alarm=" + r.Alarm);
                Assert.That(r.Alarm, Is.Zero, kv.Key + " 에서 경보가 올랐다");
                Assert.That(r.MembersDown, Is.Empty, kv.Key + " 에서 분대원을 잃었다");
                Assert.That(r.WastedActions, Is.Empty, kv.Key + " 에 헛된 명령이 있다: " + string.Join(" / ", r.WastedActions));
            }
        }

        [Test]
        public void 저격_변형도_경보_없이_푼다()
        {
            // 같은 임무의 다른 절차. 꺼진 골짜기는 소음 차폐 셋에 둘러싸여 있어
            // 거기서 쏘면 총성이 한 홉도 못 나간다 — 자리가 규칙이라는 증거다.
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            MissionResult r = sim.Run(ReferencePlans.RidgeDuskShots());

            Assert.That(r.Won, "저격 변형이 졌다: " + r.FailureReason + " alarm=" + r.Alarm);
            Assert.That(r.Alarm, Is.Zero, "골짜기에서 쏜 총성이 들렸다");
            Assert.That(r.MembersDown, Is.Empty);
            Assert.That(r.NoiseHeard, Is.Empty, "살아 있는 순찰병이 총성을 들었다");
            Assert.That(r.GuardsDown.Count, Is.EqualTo(2));
        }

        [Test]
        public void 탐색이_스스로_해를_찾는다()
        {
            foreach (string missionId in TestWorld.MissionIds())
            {
                SearchResult sr = TestWorld.Search(missionId);
                Assert.That(sr.Winners.Count, Is.GreaterThan(0),
                    missionId + " 에서 이기는 계획을 찾지 못했다 (계획 " + sr.PlansSimulated + "개 시험)");

                // 찾았다고 적은 계획이 실제로 이기는지 다시 확인한다.
                MissionSim sim = TestWorld.Sim(missionId);
                MissionDef mission = sim.Mission;
                for (int i = 0; i < sr.Winners.Count; i++)
                {
                    MissionResult r = sim.Run(sr.Winners[i]);
                    Assert.That(r.Won, missionId + " 의 승리안 " + i + " 가 다시 돌리면 진다: " + sr.Winners[i]);
                    Assert.That(r.Alarm, Is.LessThanOrEqualTo(mission.maxAlarm), missionId + " 승리안 " + i);
                }
            }
        }

        [Test]
        public void 경보_없는_해가_임무마다_있다()
        {
            // maxAlarm 이 0보다 큰 임무(철로 측선)에서도 경보 0 해가 있어야 한다 —
            // "폭음은 눈감아 준다"가 "시끄럽게 밖에 못 푼다"를 뜻하면 안 된다.
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                bool anyClean = false;
                foreach (MissionResult r in TestWorld.Search(missionId).WinnerResults)
                    if (r.Alarm == 0 && r.MembersDown.Count == 0) { anyClean = true; break; }
                Assert.That(anyClean, missionId + " 에 경보 0 · 손실 0 해가 없다");
            }
        }

        [Test]
        public void 아무_것도_하지_않으면_이기지_못한다()
        {
            // 검사기가 뭔가를 재고 있다는 최소 확인. 빈 계획이 이기면 승리 조건이 비어 있는 것이다.
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionResult r = TestWorld.Sim(missionId).Run(new SquadPlan());
                Assert.That(r.Won, Is.False, missionId + " 는 아무것도 하지 않아도 이긴다");
                Assert.That(r.FailureReason, Is.EqualTo("ObjectiveUnmet"), missionId);
            }
        }
    }
}
