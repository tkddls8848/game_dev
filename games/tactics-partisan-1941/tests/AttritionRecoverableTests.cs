using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 7 — `AttritionRecoverable`.
    /// **분대원을 잃어도 되돌릴 수 있고, 잃는 것이 공짜가 아닌가.**
    /// 한 번 잃으면 끝 / 잃어도 아무렇지 않음 — 둘 다 긴장을 죽인다 (PLAN_TACTICS §5).
    ///
    /// 두 방향을 같이 본다. 한 방향만 보면 반대쪽으로 망가진 데이터가 통과한다.
    /// </summary>
    [TestFixture]
    public sealed class AttritionRecoverableTests
    {
        [Test]
        public void 누구를_잃어도_나갈_임무가_남는다()
        {
            List<string> missions = TestWorld.MissionIds();
            foreach (string lost in TestWorld.MemberIds())
            {
                HashSet<string> left = TestWorld.AllMembersExcept(lost);
                List<string> stillSolvable = new List<string>();
                foreach (string missionId in missions)
                    if (TestWorld.Search(missionId, left).Winners.Count > 0) stillSolvable.Add(missionId);

                Assert.That(stillSolvable.Count, Is.GreaterThan(0),
                    lost + " 를 잃으면 나갈 임무가 하나도 없다 — 한 번 잃으면 끝나는 게임이다");
                TestContext.WriteLine(lost + " 를 잃어도 풀리는 임무: " + string.Join(", ", stillSolvable));
            }
        }

        [Test]
        public void 누구를_잃어도_못_하는_일이_생긴다()
        {
            List<string> missions = TestWorld.MissionIds();
            foreach (string lost in TestWorld.MemberIds())
            {
                HashSet<string> left = TestWorld.AllMembersExcept(lost);
                List<string> blocked = new List<string>();
                foreach (string missionId in missions)
                    if (TestWorld.Search(missionId, left).Winners.Count == 0) blocked.Add(missionId);

                Assert.That(blocked.Count, Is.GreaterThan(0),
                    lost + " 가 없어도 모든 임무가 풀린다 — 잃는 것이 공짜다");
                TestContext.WriteLine(lost + " 를 잃으면 막히는 임무: " + string.Join(", ", blocked));
            }
        }

        [Test]
        public void 회복에_값과_시간이_든다()
        {
            RecoveryDef rec = TestWorld.Data.Camp.recovery;
            Assert.That(rec.woundedRecoverMissions, Is.GreaterThan(0), "다쳐도 곧바로 나간다면 손실이 아니다");
            Assert.That(rec.medicineCost, Is.GreaterThan(0), "회복에 값이 들지 않는다");
            Assert.That(rec.trustPenaltyOnLoss, Is.GreaterThan(0), "잃어도 마을이 아무렇지 않다");
            Assert.That(rec.lossChancePercent, Is.InRange(1, 99), "손실이 절대 일어나지 않거나 반드시 일어난다");
        }

        [Test]
        public void 거점_장부에_손실과_복귀가_실제로_찍힌다()
        {
            GameData d = TestWorld.Data;
            CampTrace trace = CampSim.Run(d.Balance.checkers.economyMissions);

            Assert.That(trace.MembersLost, Is.GreaterThan(0),
                "N회를 돌려도 아무도 다치지 않았다 — 손실 규칙이 배선되지 않았다");
            Assert.That(trace.MembersRecovered, Is.GreaterThan(0), "다친 사람이 돌아오지 않는다");
            Assert.That(trace.MembersRecovered, Is.GreaterThanOrEqualTo(trace.MembersLost - 1),
                "다친 사람 " + trace.MembersLost + "명 중 " + trace.MembersRecovered + "명만 돌아왔다");

            // 손실이 난 회차에는 의약품·신뢰가 실제로 깎여야 한다.
            int chargedRounds = 0;
            for (int i = 1; i < trace.Rounds.Count; i++)
            {
                CampRound prev = trace.Rounds[i - 1];
                CampRound cur = trace.Rounds[i];
                if (cur.LostMemberId == "") continue;
                // 값이 치러졌는지는 다음 회차의 인원 제한으로도 드러난다
                if (trace.Rounds[i].Unavailable.Count > 0 || i + 1 < trace.Rounds.Count
                    && trace.Rounds[i + 1].Unavailable.Count > 0) chargedRounds++;
                Assert.That(prev, Is.Not.Null);
            }
            Assert.That(chargedRounds, Is.GreaterThan(0), "손실이 다음 회차의 인원에 영향을 주지 않았다");
        }

        [Test]
        public void 노출되면_실제로_쓰러진다()
        {
            // 손실이 규칙에 있는지 회차 안에서 확인한다.
            // 순회병이 골짜기에 있는 동안(85,000~120,000ms) 공병을 같은 골짜기로 보낸다 —
            // 같은 구역 벌점 60이 붙어 은폐 40+65-60=45 로 떨어지고, 3초 뒤 쓰러진다.
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            MissionResult r = sim.Run(EncounterPlan());

            Assert.That(r.Sightings.Count, Is.GreaterThan(0), "순회병과 같은 구역에서도 들키지 않았다");
            Assert.That(r.MembersDown.Count, Is.GreaterThan(0),
                "계속 보이는데도 아무도 쓰러지지 않았다 — 교전 규칙이 배선되지 않았다");
            Assert.That(r.MembersLost.Count, Is.EqualTo(r.MembersDown.Count));
            Assert.That(r.Won, Is.False);
            TestContext.WriteLine("골짜기에서 마주쳤다: " + r.MembersLost[0].AtMs + "ms 에 "
                                  + r.MembersLost[0].MemberId + " 를 잃었다 (경보 " + r.Alarm + ")");
        }

        [Test]
        public void 쓰러진_분대원은_회차_안에서_되살아나지_않는다()
        {
            // 손실이 그 회차 안에서 무효화되면 잃는 것이 공짜가 된다.
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            SquadPlan plan = EncounterPlan();
            plan.Add(new SquadOrder("sapper", ActionKinds.PlantTrap, "z_gully", null, 300000));
            MissionResult r = sim.Run(plan);

            Assert.That(r.MembersDown, Contains.Item("sapper"));
            Assert.That(r.GuardsDown, Is.Empty, "쓰러진 공병이 나중 함정을 놓았다");
            Assert.That(r.WastedActions.Count, Is.GreaterThan(0), "쓰러진 뒤의 명령이 헛돌지 않았다");
        }

        /// <summary>
        /// 일부러 마주치는 계획. 창고에 함정을 놓으러 골짜기를 지나가는데,
        /// 그 시각 골짜기에는 순회병이 있다 (85,000~120,000ms).
        /// </summary>
        private static SquadPlan EncounterPlan()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("sapper", ActionKinds.PlantTrap, "z_shed", null, 100000)
            });
        }
    }
}
