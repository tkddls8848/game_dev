using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 3 — **`DetectionFairness`. 이 PoC의 핵이다.**
    ///
    /// > 정찰에서 볼 수 없었던 것이 실패의 원인이 되지 않는다.
    ///
    /// 숨은 정보로 죽으면 플레이어는 배우지 못하고 저장·불러오기만 반복한다.
    /// 여기서 실제로 하는 일은 넷이다:
    ///
    ///   ① **정적 보장** — 순찰병이 한 번이라도 밟는 구역이 전부 정찰 벤티지에서 보이는가.
    ///      전수 검사다. 아직 아무도 그 구역에서 죽지 않았을 뿐인 사각을 미리 잡는다
    ///   ② **동적 감사** — 씨드로 뽑은 계획 수백 개를 돌려 **실패한 회차마다** 그 실패를 만든
    ///      사건(들켰다·소리를 들켰다·쓰러졌다)을 정찰 사실로 설명해 본다. 설명 못 하면 숨은 정보다
    ///   ③ **표본이 비어 있지 않은가** — 실패가 하나도 없으면 ②는 아무것도 말하지 않는다
    ///   ④ **음성 대조군** — 일부러 정보를 숨긴 정찰 보고서에서는 **반드시 걸려야 한다.**
    ///      이게 없으면 ②는 동어반복이다. 검사기에 이가 있는지를 검사기로 확인하는 자리다
    /// </summary>
    [TestFixture]
    public sealed class DetectionFairnessTests
    {
        [Test]
        public void 순찰병이_밟는_구역은_전부_정찰에서_보인다()
        {
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                ReconReport recon = TestWorld.Recon(missionId);
                List<string> gaps = FairnessAudit.StaticGaps(recon, sim.Patrols, sim.Graph);

                Assert.That(gaps, Is.Empty, missionId + " 에 정찰 사각이 있다:\n  " + string.Join("\n  ", gaps));

                // 순찰병이 서는 구역 전부가 관측 가능 집합에 들어 있는지 직접도 확인한다.
                foreach (string zone in sim.Patrols.OccupiedZones())
                    Assert.That(recon.ObservableZones.Contains(zone), missionId + " 의 " + zone + " 이 정찰 사각이다");

                // 교대 시각도 볼 수 있어야 한다 — 그게 계획의 축이기 때문이다.
                Assert.That(recon.HiddenShiftChangeIds, Is.Empty, missionId);
                Assert.That(recon.ObservedShiftChangeIds.Count, Is.GreaterThan(0), missionId + " 에 관측된 교대가 없다");
            }
        }

        [Test]
        public void 모든_실패를_정찰_사실로_설명할_수_있다()
        {
            GameData d = TestWorld.Data;
            int failures = 0;
            int events = 0;

            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                ReconReport recon = TestWorld.Recon(missionId);

                foreach (SquadPlan plan in PlanSearch.RandomPlans(d, sim, d.Balance.search.seed, 200))
                {
                    MissionResult r = sim.Run(plan);
                    if (r.FailureReason == "Infeasible") continue;
                    if (r.Sightings.Count == 0 && r.NoiseHeard.Count == 0) continue;

                    failures++;
                    FairnessVerdict v = FairnessAudit.Audit(sim.Graph, recon, r);
                    events += v.EventsChecked;

                    Assert.That(v.Fair, missionId + " 에서 정찰로 볼 수 없었던 것이 실패를 만들었다.\n"
                                        + "  계획: " + plan + "\n  설명 못 한 사실:\n    "
                                        + string.Join("\n    ", v.Unexplained));
                }
            }

            // 표본이 비어 있으면 위 단정은 아무것도 말하지 않는다.
            Assert.That(failures, Is.GreaterThanOrEqualTo(d.Balance.checkers.failureSampleTarget),
                "감사할 실패 표본이 너무 적다: " + failures + "회 (목표 "
                + d.Balance.checkers.failureSampleTarget + ")");
            Assert.That(events, Is.GreaterThan(failures), "실패마다 사건이 하나도 없다");
            TestContext.WriteLine("실패 " + failures + "회 · 사건 " + events + "건을 정찰 사실로 설명했다");
        }

        [Test]
        public void 대조군_정찰에서_구역을_숨기면_반드시_걸린다()
        {
            // 순찰병이 가장 오래 서 있는 구역(수레길)을 정찰에서 지운다.
            // 그러면 "수레길에 선 순찰병이 나를 봤다"를 설명할 수 없어야 한다.
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            ReconReport honest = TestWorld.Recon("m_ridge_dusk");
            ReconReport crippled = ReconReport.Observe(d, sim.Mission, sim.Graph, sim.Patrols,
                                                       new[] { "z_track" });

            // ① 정적 보장이 먼저 걸린다
            List<string> gaps = FairnessAudit.StaticGaps(crippled, sim.Patrols, sim.Graph);
            Assert.That(gaps, Is.Not.Empty, "구역을 숨겼는데 정적 검사가 아무 말도 하지 않았다");

            // ② 동적 감사도 걸린다 — 정직한 보고서로는 설명되던 실패가 설명되지 않는다
            int honestUnfair = 0;
            int crippledUnfair = 0;
            int audited = 0;
            foreach (SquadPlan plan in PlanSearch.RandomPlans(d, sim, 31337, 200))
            {
                MissionResult r = sim.Run(plan);
                if (r.FailureReason == "Infeasible") continue;
                if (r.Sightings.Count == 0 && r.NoiseHeard.Count == 0) continue;
                audited++;
                if (!FairnessAudit.Audit(sim.Graph, honest, r).Fair) honestUnfair++;
                if (!FairnessAudit.Audit(sim.Graph, crippled, r).Fair) crippledUnfair++;
            }

            Assert.That(audited, Is.GreaterThan(20), "대조군 표본이 너무 적다");
            Assert.That(honestUnfair, Is.Zero, "정직한 정찰 보고서에서 설명 못 한 실패가 나왔다");
            Assert.That(crippledUnfair, Is.GreaterThan(0),
                "정찰에서 구역을 숨겼는데도 감사가 전부 통과했다 — 이 검사기에는 이가 없다");
            TestContext.WriteLine("대조군: 표본 " + audited + "회 중 " + crippledUnfair + "회가 숨은 정보로 걸렸다");
        }

        [Test]
        public void 대조군_시선을_잊으면_들킨_사건이_설명되지_않는다()
        {
            // 수레길의 순찰병이 능선을 본다는 사실 하나만 지운다. 그 한 줄로 감사가 걸려야 한다.
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            MissionResult r = sim.Run(ExposedSniperPlan());
            Assert.That(r.Sightings.Count, Is.GreaterThan(0), "대조군 계획이 들키지 않았다 — 계획을 고쳐야 한다");

            ReconReport honest = TestWorld.Recon("m_ridge_dusk");
            Assert.That(FairnessAudit.Audit(sim.Graph, honest, r).Fair, "정직한 보고서로 설명되지 않았다");

            ReconReport forgetful = TestWorld.Recon("m_ridge_dusk");
            forgetful.ForgetSightLine("z_track", "z_ridge");
            FairnessVerdict v = FairnessAudit.Audit(sim.Graph, forgetful, r);
            Assert.That(v.Fair, Is.False, "시선을 지웠는데도 감사가 통과했다");
            bool sightLineFlagged = false;
            foreach (UnexplainedFact f in v.Unexplained) if (f.Kind == "SightLine") sightLineFlagged = true;
            Assert.That(sightLineFlagged, "걸린 이유가 시선이 아니다: " + string.Join(" / ", v.Unexplained));
        }

        [Test]
        public void 대조군_소음_경로를_잊으면_소리로_들킨_사건이_설명되지_않는다()
        {
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            MissionResult r = sim.Run(ExposedSniperPlan());
            Assert.That(r.NoiseHeard.Count, Is.GreaterThan(0), "대조군 계획에서 아무도 총성을 듣지 않았다");

            // 총성이 능선에서 수레길로 건너간 한 홉을 지운다.
            ReconReport forgetful = TestWorld.Recon("m_ridge_dusk");
            forgetful.ForgetNoiseLink("z_ridge", "z_track");
            FairnessVerdict v = FairnessAudit.Audit(sim.Graph, forgetful, r);

            Assert.That(v.Fair, Is.False, "소음 경로를 지웠는데도 감사가 통과했다");
            bool noiseFlagged = false;
            foreach (UnexplainedFact f in v.Unexplained) if (f.Kind == "NoisePath") noiseFlagged = true;
            Assert.That(noiseFlagged, "걸린 이유가 소음 경로가 아니다: " + string.Join(" / ", v.Unexplained));
        }

        [Test]
        public void 이긴_회차도_설명_가능한_사실로만_이룬다()
        {
            // 공정성은 실패에만 걸린 규칙이 아니다. 이기는 계획이 기대는 사실도 정찰로 볼 수 있어야
            // "정찰 → 계획" 이 성립한다.
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                ReconReport recon = TestWorld.Recon(missionId);
                foreach (SquadPlan plan in TestWorld.Search(missionId).Winners)
                {
                    MissionResult r = sim.Run(plan);
                    FairnessVerdict v = FairnessAudit.Audit(sim.Graph, recon, r);
                    Assert.That(v.Fair, missionId + " 승리안이 정찰로 볼 수 없는 사실에 기댔다: " + plan);

                    // 승리안이 쓴 자리는 전부 정찰로 엄폐율을 읽을 수 있어야 한다.
                    foreach (SquadOrder o in plan.Orders)
                        Assert.That(recon.KnowsCover(o.Zone),
                            missionId + " 승리안이 정찰로 엄폐율을 모르는 자리를 쓴다: " + o.Zone);
                }
            }
        }

        /// <summary>
        /// 일부러 들키는 계획. 수레길에 두 순찰병이 겹치는 시각(70,000ms)에 능선에서 쏜다 —
        /// 능선은 수레길에서 보이고, 총성은 능선에서 수레길로 한 홉 건너간다.
        /// 대조군 둘이 이 계획의 사건을 재료로 쓴다.
        /// </summary>
        private static SquadPlan ExposedSniperPlan()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("sniper", ActionKinds.Shoot, "z_ridge", "g_rover", 62000)
            });
        }
    }
}
