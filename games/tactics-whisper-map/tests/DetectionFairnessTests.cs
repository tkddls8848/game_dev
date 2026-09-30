using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 검사기 3 — `DetectionFairness`. **실패 원인이 가진 정보로 설명되는가.**
    ///
    /// ⚠️ 먼저 만든 PoC(`games/tactics-partisan-1941`)와 여기가 갈린다.
    /// 거기서 "가진 정보"는 **눈으로 본 것**이었다. 여기서는 **산 정보 + 틀린 정보**다.
    /// 그래서 판정이 넷으로 갈라지고, 그중 셋이 공정이다:
    ///
    ///   · `Held`              쥔 참으로 죽었다 — 피할 수 있었다
    ///   · `Obtainable`        안 산 것으로 죽었다 — 물을 수 있었다. 안 묻기로 한 것은 선택이다
    ///   · `FalseButCheckable` **틀린 것으로 죽었다. 겹쳐 물으면 드러났다** ← 이게 어려운 칸이다
    ///   · `FalseAndUncheckable` / `Unknowable` / `TerrainUnknown` → **불공정**
    ///
    /// 여기서 하는 일 다섯:
    ///   ① **정적 보장** — 순찰 경로·배치·교대를 아는 사람이 다 있고, 틀릴 수 있는 항목은 출처가 둘 이상인가. 전수
    ///   ② **동적 감사** — 씨드로 뽑은 계획 수백 개를 돌려 **실패한 회차마다** 사건을 분류한다
    ///   ③ **표본 확인** — 실패가 없으면 ②는 아무 말도 하지 않는다
    ///   ④ **어려운 칸이 실제로 쓰였는가** — `FalseButCheckable` 가 0이면 ②는 쉬운 길만 걸은 것이다
    ///   ⑤ **음성 대조군 넷** — 목록을 지우고 · 출처를 하나로 줄이고 · 지형을 잊고 · 정직한 상태
    /// </summary>
    [TestFixture]
    public sealed class DetectionFairnessTests
    {
        [Test]
        public void 정적_보장_물으면_알_수_있는_것에_구멍이_없다()
        {
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Whispers market = TestWorld.Market(missionId);
                IntelKnowledge blind = TestWorld.Blind(missionId);
                List<string> gaps = FairnessAudit.StaticGaps(sim, blind, market);
                Assert.That(gaps, Is.Empty, missionId + " 에 구멍이 있다:\n  " + string.Join("\n  ", gaps));

                // 순찰병이 밟는 구역은 전부 예측 가능한 대상이어야 한다.
                foreach (string guardId in sim.Patrols.GuardIds)
                {
                    Assert.That(TestWorld.Data.ItemFor(IntelKinds.Route, guardId), Is.Not.Null);
                    Assert.That(TestWorld.Data.ItemFor(IntelKinds.Post, guardId), Is.Not.Null);
                }
            }
        }

        [Test]
        public void 모든_실패를_가진_정보로_설명할_수_있다()
        {
            GameData d = TestWorld.Data;
            int failures = 0, events = 0;
            int held = 0, obtainable = 0, falseCheckable = 0;

            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Whispers market = TestWorld.Market(missionId);

                // 지식 상태 셋을 섞어 본다: 눈먼 채 · 한 사람씩 물은 채 · **거짓말쟁이에게 물은 채**.
                IntelKnowledge[] states =
                {
                    TestWorld.Blind(missionId),
                    TestWorld.FullyAsked(missionId, market),
                    TestWorld.AskedLiarsFirst(missionId, market)
                };

                foreach (IntelKnowledge k in states)
                    foreach (SquadPlan plan in PlanSearch.RandomPlans(d, sim, k, d.Balance.search.seed, 150))
                    {
                        MissionResult r = sim.Run(plan);
                        if (r.Infeasible) continue;
                        if (r.Sightings.Count == 0 && r.NoiseHeard.Count == 0) continue;

                        failures++;
                        FairnessVerdict v = FairnessAudit.Audit(sim, k, market, r);
                        events += v.EventsChecked;
                        held += v.Count(FactVerdicts.Held);
                        obtainable += v.Count(FactVerdicts.Obtainable);
                        falseCheckable += v.Count(FactVerdicts.FalseButCheckable);

                        Assert.That(v.Fair, missionId + " 에서 알 길 없는 것이 실패를 만들었다.\n  계획: " + plan
                            + "\n  설명 못 한 사실:\n    " + string.Join("\n    ", v.Unfair()));
                    }
            }

            CheckerBalance cb = d.Balance.checkers;
            Assert.That(failures, Is.GreaterThanOrEqualTo(cb.failureSampleTarget),
                "감사할 실패 표본이 너무 적다: " + failures + "회 (목표 " + cb.failureSampleTarget + ")");
            Assert.That(events, Is.GreaterThan(failures), "실패마다 사건이 하나도 없다");
            // ④ 어려운 칸이 실제로 쓰였는가. 0이면 이 검사기는 쉬운 길만 걸은 것이다.
            Assert.That(falseCheckable, Is.GreaterThan(0),
                "틀린 정보로 실패한 회차가 하나도 없었다 — 이 검사기의 어려운 부분이 검사되지 않았다");
            TestContext.WriteLine("실패 " + failures + "회 · 사건 " + events + "건 — "
                + "쥔 참 " + held + " · 안 산 것 " + obtainable + " · **틀린 정보(확인 가능) " + falseCheckable + "**");
        }

        [Test]
        public void 대조군_아는_사람이_다_사라지면_반드시_걸린다()
        {
            // 골목 초병의 경로를 아는 사람 전원이 입을 닫으면 그 사실은 **살 수 없는 것**이 된다.
            // 그러면 그것으로 죽는 것은 불공정해야 한다. 걸리지 않으면 이 검사기에는 이가 없다.
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_dusk_checkpoint");
            IntelItemDef route = d.ItemFor(IntelKinds.Route, "g_laneguard");
            IntelItemDef post = d.ItemFor(IntelKinds.Post, "g_laneguard");

            HashSet<string> silenced = new HashSet<string>();
            foreach (VillagerDef v in d.SourcesOf(route.id)) silenced.Add(v.id);
            foreach (VillagerDef v in d.SourcesOf(post.id)) silenced.Add(v.id);

            Whispers honest = TestWorld.Market("m_dusk_checkpoint");
            Whispers crippled = new Whispers(d, sim.Mission, sim.Phases, silenced);

            // ① 정적 보장이 먼저 걸린다
            List<string> gaps = FairnessAudit.StaticGaps(sim, TestWorld.Blind("m_dusk_checkpoint"), crippled);
            Assert.That(gaps, Is.Not.Empty, "아는 사람을 다 없앴는데 정적 검사가 아무 말도 하지 않았다");

            // ② 동적 감사도 걸린다 — 정직한 시장에서는 설명되던 실패가 설명되지 않는다
            IntelKnowledge blind = TestWorld.Blind("m_dusk_checkpoint");
            int audited = 0, honestUnfair = 0, crippledUnfair = 0;
            foreach (SquadPlan plan in PlanSearch.RandomPlans(d, sim, blind, 31337, 200))
            {
                MissionResult r = sim.Run(plan);
                if (r.Infeasible) continue;
                if (r.Sightings.Count == 0 && r.NoiseHeard.Count == 0) continue;
                audited++;
                if (!FairnessAudit.Audit(sim, blind, honest, r).Fair) honestUnfair++;
                if (!FairnessAudit.Audit(sim, blind, crippled, r).Fair) crippledUnfair++;
            }

            Assert.That(audited, Is.GreaterThan(20), "대조군 표본이 너무 적다");
            Assert.That(honestUnfair, Is.Zero, "정직한 시장에서 설명 못 한 실패가 나왔다");
            Assert.That(crippledUnfair, Is.GreaterThan(0),
                "아는 사람을 다 없앴는데도 감사가 전부 통과했다 — 이 검사기에는 이가 없다");
            TestContext.WriteLine("대조군(아는 사람 소멸): 표본 " + audited + "회 중 " + crippledUnfair + "회가 걸렸다");
        }

        [Test]
        public void 대조군_겹쳐_물을_수_없는_거짓은_불공정이다()
        {
            // ★ **이 검사기의 어려운 부분을 정면으로 시험한다.**
            // 같은 거짓 정보로 같은 실패를 만들고, 출처가 둘일 때와 하나일 때의 판정이 갈리는지 본다.
            //   · 둘 → FalseButCheckable (공정: 겹쳐 물으면 드러났다)
            //   · 하나 → FalseAndUncheckable (불공정: 확인할 방법이 없었다)
            GameData d = TestWorld.Data;
            bool tested = false;

            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Whispers market = TestWorld.Market(missionId);
                IntelKnowledge k = TestWorld.AskedLiarsFirst(missionId, market);

                foreach (SquadPlan plan in PlanSearch.RandomPlans(d, sim, k, 4242, 200))
                {
                    MissionResult r = sim.Run(plan);
                    if (r.Infeasible) continue;
                    FairnessVerdict v = FairnessAudit.Audit(sim, k, market, r);
                    string badItem = null;
                    foreach (FactCheck f in v.Facts)
                        if (f.Verdict == FactVerdicts.FalseButCheckable) { badItem = f.ItemId; break; }
                    if (badItem == null) continue;

                    // 출처를 하나로 줄인 시장. 거짓말쟁이만 남는다 — 확인해 줄 사람이 없다.
                    HashSet<string> silenced = new HashSet<string>();
                    foreach (VillagerDef src in d.SourcesOf(badItem))
                        if (src.id != market.LiarOf(badItem)) silenced.Add(src.id);
                    Whispers lonely = new Whispers(d, sim.Mission, sim.Phases, silenced);
                    Assert.That(lonely.SourceCount(badItem), Is.EqualTo(1), "출처를 하나로 줄이지 못했다");

                    FairnessVerdict lonelyVerdict = FairnessAudit.Audit(sim, k, lonely, r);
                    Assert.That(lonelyVerdict.Count(FactVerdicts.FalseAndUncheckable), Is.GreaterThan(0),
                        "겹쳐 물을 수 없는 거짓인데도 공정으로 판정했다 (" + badItem + ")");
                    Assert.That(lonelyVerdict.Fair, Is.False);
                    Assert.That(v.Fair, "출처가 둘일 때는 공정이어야 한다 (" + badItem + ")");
                    TestContext.WriteLine("어려운 칸을 갈랐다: " + badItem + " — 출처 "
                        + market.SourceCount(badItem) + "사람이면 공정, 1사람이면 불공정");
                    tested = true;
                    break;
                }
                if (tested) break;
            }

            Assert.That(tested, "틀린 정보로 실패한 회차를 찾지 못해 어려운 칸을 시험하지 못했다");
        }

        [Test]
        public void 대조군_지형을_잊으면_들킨_사건이_설명되지_않는다()
        {
            // 지형은 공짜다 — 눈으로 보인다. 그래서 감사는 지형을 늘 안다고 본다.
            // 그 가정이 실제로 검사되고 있는지 확인한다: 시선 하나를 지우면 걸려야 한다.
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_dusk_checkpoint");
            Whispers market = TestWorld.Market("m_dusk_checkpoint");
            MissionResult r = sim.Run(ExposedPlan());
            Assert.That(r.Sightings.Count, Is.GreaterThan(0), "대조군 계획이 들키지 않았다 — 계획을 고쳐야 한다");

            IntelKnowledge honest = TestWorld.FullyAsked("m_dusk_checkpoint", market);
            Assert.That(FairnessAudit.Audit(sim, honest, market, r).Fair, "정직한 상태로 설명되지 않았다");

            SightingEvent s = r.Sightings[0];
            IntelKnowledge forgetful = TestWorld.FullyAsked("m_dusk_checkpoint", market);
            forgetful.ForgetSightLine(s.GuardZone, s.MemberZone);
            FairnessVerdict v = FairnessAudit.Audit(sim, forgetful, market, r);

            Assert.That(v.Fair, Is.False, "시선을 지웠는데도 감사가 통과했다");
            bool flagged = false;
            foreach (FactCheck f in v.Unfair()) if (f.Kind == "SightLine") flagged = true;
            Assert.That(flagged, "걸린 이유가 시선이 아니다: " + string.Join(" / ", v.Unfair()));
            TestContext.WriteLine("대조군(시선 망각): " + s.GuardZone + "→" + s.MemberZone + " 를 지우자 걸렸다");
        }

        [Test]
        public void 대조군_소음_경로를_잊으면_소리로_들킨_사건이_설명되지_않는다()
        {
            // 폭음은 반드시 들린다(곳간 폭파는 경보 30을 피할 수 없다) — 소음 사건의 재료로 이것을 쓴다.
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_granary_charge");
            Whispers market = TestWorld.Market("m_granary_charge");
            MissionResult r = sim.Run(ReferencePlans.GranaryChargeBlind());
            Assert.That(r.NoiseHeard.Count, Is.GreaterThan(0), "대조군 계획에서 아무도 폭음을 듣지 않았다");

            NoiseHeardEvent n = r.NoiseHeard[0];
            IntelKnowledge forgetful = TestWorld.FullyAsked("m_granary_charge", market);
            // 소리가 건너온 첫 홉을 지운다. "경로가 있는가"가 아니라 **아는 링크만으로 다시 계산**하므로 걸린다.
            foreach (string nb in sim.Graph.Neighbors(n.OriginZone)) forgetful.ForgetNoiseLink(n.OriginZone, nb);
            FairnessVerdict v = FairnessAudit.Audit(sim, forgetful, market, r);

            Assert.That(v.Fair, Is.False, "소음 경로를 지웠는데도 감사가 통과했다");
            bool flagged = false;
            foreach (FactCheck f in v.Unfair()) if (f.Kind == "NoisePath") flagged = true;
            Assert.That(flagged, "걸린 이유가 소음 경로가 아니다: " + string.Join(" / ", v.Unfair()));
            TestContext.WriteLine("대조군(소음 경로 망각): " + n.OriginZone + " 의 인접을 지우자 걸렸다");
        }

        [Test]
        public void 이긴_회차도_설명_가능한_사실로만_이룬다()
        {
            // 공정성은 실패에만 걸린 규칙이 아니다. 이기는 계획이 기대는 사실도
            // **물으면 알 수 있는 것**이어야 "묻기 → 계획" 이 성립한다.
            GameData d = TestWorld.Data;
            int checkedPlans = 0;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Whispers market = TestWorld.Market(missionId);
                IntelKnowledge k = TestWorld.Corroborated(missionId, market);
                List<SquadPlan> winners = TestWorld.Search(missionId).Winners;
                for (int i = 0; i < winners.Count && i < 60; i++)
                {
                    MissionResult r = sim.Run(winners[i]);
                    FairnessVerdict v = FairnessAudit.Audit(sim, k, market, r);
                    Assert.That(v.Fair, missionId + " 승리안이 알 길 없는 사실에 기댔다: " + winners[i]);
                    foreach (SquadOrder o in winners[i].Orders)
                        Assert.That(k.KnowsCover(o.Zone), missionId + " 승리안이 엄폐율을 모르는 자리를 쓴다: " + o.Zone);
                    checkedPlans++;
                }
            }
            TestContext.WriteLine("승리안 " + checkedPlans + "개가 쓴 사실이 전부 물어서 알 수 있는 것이었다");
        }

        [Test]
        public void 모델_어긋남이_한_번도_나오지_않는다()
        {
            // ModelMismatch 는 "참만 쥐었는데 머릿속 지도가 틀렸다" — 모델 버그의 신호다.
            // 이것이 나오면 위의 판정 전부를 믿을 수 없다. 감사기의 안전망이 울리는지 본다.
            GameData d = TestWorld.Data;
            int mismatches = 0;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Whispers market = TestWorld.Market(missionId);
                IntelKnowledge truth = TestWorld.Corroborated(missionId, market);
                foreach (SquadPlan plan in PlanSearch.RandomPlans(d, sim, truth, 909, 200))
                {
                    MissionResult r = sim.Run(plan);
                    if (r.Infeasible) continue;
                    mismatches += FairnessAudit.Audit(sim, truth, market, r).Count(FactVerdicts.ModelMismatch);
                }
            }
            Assert.That(mismatches, Is.Zero, "겹쳐 물어 참만 쥐었는데 머릿속 지도가 어긋난 사건이 " + mismatches + "건 있다");
        }

        /// <summary>
        /// 일부러 들키는 계획. 골목에서 함정을 놓는다 — 골목은 엄폐가 20뿐이고
        /// 순찰이 서는 구역 전부에서 보인다. 총성도 한 홉을 건너간다.
        /// 대조군 둘이 이 계획의 사건을 재료로 쓴다.
        /// </summary>
        private static SquadPlan ExposedPlan()
        {
            return new SquadPlan(new[]
            {
                new SquadOrder("m_sniper", ActionKinds.Shoot, "z_lane", "g_bellguard", 60000)
            });
        }
    }
}
