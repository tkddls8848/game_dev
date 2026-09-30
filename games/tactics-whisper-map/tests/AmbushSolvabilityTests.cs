using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 검사기 2 — `AmbushSolvability`. **적어도 한 계획이 경보 없이 초소를 지울 수 있는가.**
    /// 풀 수 없는 임무를 내놓으면 플레이어는 자기 실력을 의심한다.
    ///
    /// 손으로 적은 참조 계획과 **계획 탐색을 둘 다** 본다. 참조 계획만 보면
    /// "내가 아는 한 수"를 확인하는 것이고, 그건 퍼즐 확인이다 (PLAN_TACTICS §2).
    /// </summary>
    [TestFixture]
    public sealed class AmbushSolvabilityTests
    {
        [Test]
        public void 참조_계획이_손실_없이_이긴다()
        {
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                SquadPlan plan = ReferencePlans.For(missionId);
                MissionResult r = sim.Run(plan);
                Assert.That(r.Won, missionId + " 의 참조 계획이 졌다: " + r.FailureReason
                    + "\n  " + string.Join("\n  ", r.Log));
                Assert.That(r.MembersLost, Is.Empty, missionId + " 에서 분대원을 잃었다");
                Assert.That(r.AlarmPercent, Is.LessThanOrEqualTo(sim.Mission.alarmLimit), missionId);
                TestContext.WriteLine(missionId + ": 경보 " + r.AlarmPercent + "/" + sim.Mission.alarmLimit
                    + " · 보복 " + r.ReprisalPoints + " · 필드틱 " + r.FieldTicks
                    + " · 목표 " + r.ObjectiveDoneMs / 1000 + "s · 끝 " + r.EndMs / 1000 + "s");
            }
        }

        [Test]
        public void 경보_0_임무는_실제로_아무도_모르게_끝난다()
        {
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                if (sim.Mission.alarmLimit != 0) continue;
                MissionResult r = sim.Run(ReferencePlans.For(missionId));
                Assert.That(r.Sightings, Is.Empty, missionId + " 에서 목격됐다");
                Assert.That(r.NoiseHeard, Is.Empty, missionId + " 에서 소리를 들켰다");
                Assert.That(r.ReprisalPoints, Is.Zero, missionId + " 에서 마을이 값을 치렀다");
            }
        }

        [Test]
        public void 탐색도_임무마다_해를_찾는다()
        {
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                SearchResult sr = TestWorld.Search(missionId);
                Assert.That(sr.Winners.Count, Is.GreaterThanOrEqualTo(d.Balance.checkers.minWinningPlansPerMission),
                    missionId + " 의 승리안이 " + sr.Winners.Count + "개뿐이다 (회차 " + sr.Runs + "번 돌렸다)");
                Assert.That(sr.PlacementSignatures.Count,
                    Is.GreaterThanOrEqualTo(d.Balance.checkers.minDistinctSignaturesPerMission), missionId);
                TestContext.WriteLine(missionId + ": 회차 " + sr.Runs + " · 승리안 " + sr.Winners.Count
                    + " · 절차 " + sr.ActionSignatures.Count + " · 자리 " + sr.PlacementSignatures.Count
                    + " · 최소 필드틱 " + sr.BestFieldTicks);
            }
        }

        [Test]
        public void 쓸모없는_수단이_없다()
        {
            // POC_FACTORY §5 "쓸모없는 선택지가 없는가". 수단 다섯이 **어디선가는** 승리안에 쓰인다.
            GameData d = TestWorld.Data;
            SortedSet<string> used = new SortedSet<string>();
            foreach (string missionId in TestWorld.MissionIds())
                foreach (SquadPlan w in TestWorld.Search(missionId).Winners)
                    foreach (SquadOrder o in w.Orders) used.Add(o.Kind);

            List<string> unused = new List<string>();
            foreach (MemberDef m in d.AllMembers)
                foreach (MemberActionDef a in m.actions)
                    if (!used.Contains(a.kind)) unused.Add(m.id + "/" + a.kind);

            Assert.That(unused, Is.Empty, "어느 승리안에도 쓰이지 않는 수단이 있다: " + string.Join(" ", unused));
            TestContext.WriteLine("쓰인 수단: " + string.Join(" ", used));
        }
    }
}
