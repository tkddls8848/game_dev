using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// ★ 핵 검사기 하나 — `BlindSolvability`.
    /// **정보를 하나도 사지 않고도 임무를 깰 수 있는가.**
    ///
    /// 깰 수 없으면 정보가 선택이 아니라 **관문**이고, 신뢰가 바닥난 플레이어는 막힌다.
    /// 그러면 거점 층(신뢰)이 임무 층을 잠그고, 한 번 미끄러진 플레이어는 되돌아올 길이 없다.
    ///
    /// **정의가 이 PoC의 설계다.** 묻지 않은 플레이어는 오늘 밤이 아홉 위상 조합 중
    /// 어느 것인지 모른다. 그러므로 "정보 없이 깬다"는 것은
    ///
    /// > **아홉 조합 전부에서 이기는 한 계획이 있다**
    ///
    /// 는 뜻이고, 이 검사기는 그것을 말 그대로 아홉 번 돌려 확인한다.
    /// 지형(엄폐·시야·인접·은폐 지점)은 공짜이므로 눈먼 계획은 지형만으로 서야 한다 —
    /// 이 지도에서 그 자리가 **과수원**(아무 데서도 보이지 않고 소리를 먹는다)과
    /// **곡물 자루 뒤**(묻으면 위상과 무관하게 보이지 않는다)다.
    /// </summary>
    [TestFixture]
    public sealed class BlindSolvabilityTests
    {
        [Test]
        public void 참조_계획_셋이_아홉_위상_전부에서_이긴다()
        {
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionDef m = d.Mission(missionId);
                List<PhaseAssignment> all = PhaseAssignment.AllCombinations(d, m);
                Assert.That(all.Count, Is.GreaterThanOrEqualTo(d.Balance.checkers.blindPhaseCombinationsMin),
                    missionId + " 의 위상 조합이 " + all.Count + "가지뿐이다 — 눈먼 판정이 약해진다");

                SquadPlan plan = ReferencePlans.For(missionId);
                foreach (PhaseAssignment p in all)
                {
                    MissionResult r = new MissionSim(d, m, p).Run(plan);
                    Assert.That(r.Won, missionId + " 의 눈먼 계획이 위상 " + p + " 에서 졌다: "
                        + r.FailureReason + "\n  " + string.Join("\n  ", r.Log));
                    Assert.That(r.MembersLost, Is.Empty, missionId + " 위상 " + p);
                }
                TestContext.WriteLine(missionId + ": 눈먼 참조 계획이 위상 " + all.Count + "가지 전부에서 이겼다");
            }
        }

        [Test]
        public void 탐색도_눈먼_승리안을_찾는다()
        {
            // 참조 계획만 보면 "내가 아는 한 수"의 확인이다. 탐색이 스스로도 찾아야 한다.
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                List<SquadPlan> robust = TestWorld.BlindWinners(missionId);
                Assert.That(robust.Count, Is.GreaterThan(0),
                    missionId + " 에서 탐색이 눈먼 승리안을 찾지 못했다 — 이 임무는 정보가 관문일 수 있다");
                foreach (SquadPlan p in robust)
                    Assert.That(PlanSearch.IsRobust(d, d.Mission(missionId), p), "재확인에서 무너졌다: " + p);
                TestContext.WriteLine(missionId + ": 눈먼 승리안 " + robust.Count + "개 · 첫째 " + robust[0]);
            }
        }

        [Test]
        public void 신뢰가_바닥이면_아무도_말해_주지_않는데도_임무를_깬다()
        {
            // 거점 층과 임무 층을 실제로 이어 돌린다: 신뢰 0 → 모든 물음이 거절 → 눈먼 계획으로 승리.
            GameData d = TestWorld.Data;
            VillageLedger ledger = new VillageLedger(d);
            ledger.ForceTrust(d.Balance.trust.floorPercent);
            Assert.That(ledger.VillageSpeaks, Is.False, "신뢰 바닥인데 마을이 입을 연다");

            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Whispers market = TestWorld.Market(missionId);
                IntelKnowledge k = new IntelKnowledge(d, sim.Mission);
                ledger.ForceTrust(d.Balance.trust.floorPercent);   // 임무마다 바닥에서 다시 시작한다
                ledger.BeginMission();

                int refused = 0;
                foreach (VillagerDef v in d.AllVillagers)
                    foreach (string itemId in v.knows)
                    {
                        AskOutcome o = ledger.Ask(market, k, v.id, itemId);
                        Assert.That(o.Granted, Is.False, "신뢰 0인데 답을 받았다");
                        Assert.That(o.Refusal, Is.EqualTo(Refusals.VillageClosed));
                        refused++;
                    }
                Assert.That(k.BeliefCount, Is.Zero, "거절만 받았는데 쥔 것이 생겼다");
                Assert.That(refused, Is.GreaterThan(0));

                MissionResult r = sim.Run(ReferencePlans.For(missionId));
                Assert.That(r.Won, "눈이 먼 채로 " + missionId + " 를 깨지 못했다 — 정보가 관문이다");
                ledger.Settle(r);
                TestContext.WriteLine(missionId + ": 물음 " + refused + "번 전부 거절 · 승리 · 신뢰 "
                    + ledger.TrustPercent + "%");
            }

            // 그리고 눈먼 승리 **한 번**으로 말문이 트이는 선까지 돌아온다 — 이게 막힘이 없다는 뜻이다.
            VillageLedger fresh = new VillageLedger(d);
            fresh.ForceTrust(d.Balance.trust.floorPercent);
            fresh.Settle(TestWorld.Sim("m_ledger_theft").Run(ReferencePlans.LedgerTheftBlind()));
            Assert.That(fresh.VillageSpeaks,
                "눈먼 승리 한 번으로도 신뢰가 말문이 트이는 선(" + d.Balance.trust.askFloorPercent
                + "%)까지 오르지 않았다 — 한 번 바닥나면 되돌아올 수 없다");
            TestContext.WriteLine("바닥에서 눈먼 승리 한 번 → 신뢰 " + fresh.TrustPercent + "% (말문 트임 선 "
                + d.Balance.trust.askFloorPercent + "%)");
        }

        [Test]
        public void 정보는_관문이_아니라_시간을_산다()
        {
            // 눈먼 계획이 있다는 것만으로는 정보가 **쓸모없다**는 뜻이 될 수 있다.
            // 정보를 산 계획이 눈먼 계획보다 짧아야 이 층이 서로 붙어 있다.
            //
            // 재는 값: 눈먼 계획은 **위상 전부에서 이겨야** 하므로 그 최악값을 쓰고,
            // 산 계획은 오늘 밤의 값을 쓴다. 눈먼 쪽이 짊어지는 것이 곧 기다림이다.
            GameData d = TestWorld.Data;
            int strictlyBetter = 0;
            foreach (string missionId in TestWorld.MissionIds())
            {
                int informed = TestWorld.Search(missionId).BestFieldTicks;
                int blindWorst = int.MaxValue;
                foreach (SquadPlan p in TestWorld.BlindWinners(missionId))
                {
                    int w = PlanSearch.WorstFieldTicks(d, d.Mission(missionId), p);
                    if (w < blindWorst) blindWorst = w;
                }
                Assert.That(informed, Is.LessThanOrEqualTo(blindWorst),
                    missionId + " 는 정보를 사고도 눈먼 계획보다 오래 걸린다 (" + informed + " vs " + blindWorst + ")");
                if (informed < blindWorst) strictlyBetter++;
                TestContext.WriteLine(missionId + ": 산 정보 " + informed + "틱 · 눈먼 최악 " + blindWorst + "틱"
                    + (informed < blindWorst ? "  ← 정보가 " + (blindWorst - informed) + "틱을 샀다" : "  ← 같다"));
            }
            Assert.That(strictlyBetter, Is.GreaterThanOrEqualTo(2),
                "정보를 사도 짧아지지 않는 임무가 둘 이상이다 — 정보에 값이 없다");
        }

        [Test]
        public void 눈먼_계획이_기대는_것은_지형뿐이다()
        {
            // 이 임무들의 눈먼 해가 무엇에 기대는지 못 박는다. 이 넷이 깨지면 눈먼 해가 사라진다.
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_dusk_checkpoint");
            ZoneGraph g = sim.Graph;

            // ① 과수원은 아무 데서도 보이지 않는다
            Assert.That(g.ZonesSeeing("z_orchard"), Is.EqualTo(new List<string> { "z_orchard" }),
                "과수원이 밖에서 보인다 — 눈먼 저격 자리가 사라진다");
            // ② 과수원에서 골목이 보이고 한 홉이다
            Assert.That(g.Sees("z_orchard", "z_lane"), "과수원에서 골목이 보이지 않는다");
            Assert.That(g.Hops("z_orchard", "z_lane"), Is.EqualTo(1));
            // ③ 두 순찰 모두 골목을 밟는다 — 기다리면 반드시 사선에 들어온다
            foreach (string guardId in sim.Patrols.GuardIds)
            {
                bool visitsLane = false;
                foreach (PatrolWindow w in sim.Patrols.Windows(guardId)) if (w.Zone == "z_lane") visitsLane = true;
                Assert.That(visitsLane, guardId + " 가 골목에 오지 않는다 — 기다려도 쏠 수 없다");
                int hold = d.ActionOf("m_sniper", ActionKinds.Shoot).holdWindowMs;
                int absence = sim.Patrols.LongestAbsenceMs(guardId, "z_lane");
                Assert.That(absence, Is.LessThan(hold),
                    guardId + " 가 골목을 " + absence + "ms 비운다 — 저격수의 기다림 " + hold + "ms 를 넘는다");
            }
            // ④ 곳간은 순찰이 들어오지 않는다 — 묻으면 위상과 무관하게 보이지 않는다
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim s = TestWorld.Sim(missionId);
                Assert.That(s.Patrols.OccupiedZones().Contains("z_granary"), Is.False,
                    missionId + " 에서 순찰이 곳간에 들어온다 — 묻어도 같은 구역 벌점에 걸린다");
            }
            int floor = d.Balance.vision.concealFloorPercent;
            int settledScout = d.Member("m_scout").stealthPercent + g.CoverPercent("z_granary")
                               + g.BestConcealmentBonus("z_granary")
                               - d.ActionOf("m_scout", ActionKinds.Infiltrate).exposurePercent;
            Assert.That(settledScout, Is.GreaterThanOrEqualTo(floor),
                "곡물 자루 뒤에 묻은 정찰병이 탈취 중에 보인다 (" + settledScout + " < " + floor + ")");
            TestContext.WriteLine("묻은 정찰병의 은폐 합계 " + settledScout + " ≥ 바닥 " + floor);
        }

        [Test]
        public void 눈먼_승리안은_순찰_시각을_한_번도_읽지_않는다()
        {
            // 형식적 확인: 눈먼 계획은 IntelKnowledge 가 비어 있는 상태에서 만들어졌고,
            // 그 상태에서는 머릿속 지도가 없다. 그래서 후보 시각이 격자뿐이어야 한다.
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                IntelKnowledge blind = TestWorld.Blind(missionId);
                Assert.That(blind.BeliefCount, Is.Zero);
                Assert.That(blind.BelievedModel(), Is.Null, "아무것도 묻지 않았는데 머릿속 지도가 있다");
                Assert.That(blind.PredictableGuardIds(), Is.Empty);
                List<int> times = PlanSearch.CandidateTimes(d, sim, blind, 30000);
                foreach (int t in times)
                    Assert.That(t % 30000, Is.Zero, "눈먼 상태인데 격자 밖의 시각이 후보로 들어왔다: " + t);
            }
        }
    }
}
