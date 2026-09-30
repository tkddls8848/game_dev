using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// ★ 핵 검사기 둘 — `IntelEconomy`.
    /// **신뢰가 발산하지도(무한 정보) 영구 고갈하지도(되돌릴 수 없는 막힘) 않는가.**
    ///
    /// 이 PoC가 시험하려는 이음매가 여기 있다. 거점(신뢰)이 임무(정찰)의 입력이므로
    /// 두 층이 실제로 붙는데, 붙은 자리가 두 방향으로 깨질 수 있다:
    ///
    ///   · **발산** — 신뢰가 천장에 붙어 있으면 물음이 공짜가 되고, 정찰이 선택이 아니게 된다
    ///   · **영구 고갈** — 한 번 바닥나면 눈이 먼 채 갇힌다. 그러면 정보가 관문이 된다
    ///
    /// 둘째는 `BlindSolvability` 위에 서 있다 — 눈먼 채로도 이길 수 있어야 신뢰가 되돌아온다.
    /// **이 두 검사기는 한 검사기의 두 얼굴이다.**
    ///
    /// N회 임무를 세 가지 정책으로 돌린다: 절약가 · 탐욕가 · 서툰 자.
    /// </summary>
    [TestFixture]
    public sealed class IntelEconomyTests
    {
        private static MissionDef Probe(MissionDef m, int seed)
        {
            return new MissionDef
            {
                id = m.id, name = m.name, mapId = m.mapId, lengthMs = m.lengthMs, seed = seed,
                alarmLimit = m.alarmLimit, guardIds = m.guardIds, shiftChangeIds = m.shiftChangeIds,
                requireExtraction = m.requireExtraction, downAllGuards = m.downAllGuards,
                destroyTargetId = m.destroyTargetId, stealDocumentId = m.stealDocumentId,
                noGuardsDowned = m.noGuardsDowned
            };
        }

        private sealed class Round
        {
            public string MissionId;
            public int TrustBefore;
            public int TrustAfter;
            public int Asks;
            public int Spent;
            public int Refusals;
            public bool Won;
            public int Reprisal;
        }

        /// <summary>한 회차를 돈다. greedy 면 물을 수 있는 것을 다 묻고, fail 이면 일부러 들키는 계획을 쓴다.</summary>
        private static Round Play(GameData d, VillageLedger ledger, MissionDef basis, int round,
                                 bool greedy, bool deliberateFailure)
        {
            MissionDef m = Probe(basis, basis.seed + round);
            PhaseAssignment phases = PhaseAssignment.Tonight(d, m);
            MissionSim sim = new MissionSim(d, m, phases);
            Whispers market = new Whispers(d, m, phases, ledger.SilencedNow());
            IntelKnowledge k = new IntelKnowledge(d, m);

            Round r = new Round { MissionId = m.id, TrustBefore = ledger.TrustPercent };
            ledger.BeginMission();

            if (greedy)
                foreach (VillagerDef v in d.AllVillagers)
                    foreach (string itemId in v.knows)
                    {
                        AskOutcome o = ledger.Ask(market, k, v.id, itemId);
                        if (o.Granted) { r.Asks++; r.Spent += o.TrustSpent; }
                        else r.Refusals++;
                    }

            SquadPlan plan = deliberateFailure
                ? new SquadPlan(new[] { new SquadOrder("m_sapper", ActionKinds.PlantTrap, "z_lane", "", 60000) })
                : ReferencePlans.For(m.id);

            MissionResult result = sim.Run(plan);
            r.Won = result.Won;
            r.Reprisal = result.ReprisalPoints;
            ledger.Settle(result);
            r.TrustAfter = ledger.TrustPercent;
            return r;
        }

        private static List<Round> Run(GameData d, VillageLedger ledger, int rounds,
                                      bool greedy, bool deliberateFailure)
        {
            List<Round> log = new List<Round>();
            IList<MissionDef> missions = d.AllMissions;
            for (int i = 0; i < rounds; i++)
                log.Add(Play(d, ledger, missions[i % missions.Count], i, greedy, deliberateFailure));
            return log;
        }

        [Test]
        public void 탐욕가는_신뢰가_발산하지도_바닥에_갇히지도_않는다()
        {
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            TrustBalance tb = d.Balance.trust;
            VillageLedger ledger = new VillageLedger(d);
            List<Round> log = Run(d, ledger, cb.economyMissions, true, false);

            int lowest = int.MaxValue, highest = int.MinValue, blocked = 0, spokeIn = 0;
            foreach (Round r in log)
            {
                Assert.That(r.TrustAfter, Is.InRange(tb.floorPercent, tb.ceilingPercent),
                    r.MissionId + " 회차에서 신뢰가 범위를 벗어났다: " + r.TrustAfter);
                if (!r.Won) blocked++;
                if (r.Asks > 0) spokeIn++;
                if (r.TrustAfter < lowest) lowest = r.TrustAfter;
                if (r.TrustAfter > highest) highest = r.TrustAfter;
            }

            Assert.That(blocked, Is.Zero, "막힌 회차가 " + blocked + "번 있다 — 진행이 멈춘다");
            Assert.That(ledger.CeilingClamps, Is.LessThanOrEqualTo(cb.maxCeilingClampRounds),
                "신뢰가 천장에 " + ledger.CeilingClamps + "번 붙었다 — 물음이 공짜에 가까워진다");
            Assert.That(spokeIn, Is.GreaterThan(cb.economyMissions / 3),
                "마을이 입을 연 회차가 " + spokeIn + "번뿐이다 — 정보가 사실상 관문이다");
            Assert.That(ledger.TotalTrustSpent, Is.GreaterThan(0), "탐욕가가 신뢰를 한 푼도 쓰지 않았다");
            Assert.That(highest - lowest, Is.LessThanOrEqualTo(tb.ceilingPercent),
                "신뢰가 요동친 폭이 천장보다 크다");

            TestContext.WriteLine("탐욕가 " + cb.economyMissions + "회차: 신뢰 " + tb.startPercent + "% → "
                + ledger.TrustPercent + "% (최저 " + lowest + " · 최고 " + highest + ")"
                + " · 물은 횟수 " + ledger.TotalAsks + " · 쓴 신뢰 " + ledger.TotalTrustSpent
                + " · 마을이 입을 연 회차 " + spokeIn + "/" + cb.economyMissions
                + " · 천장 " + ledger.CeilingClamps + "번 · 바닥 " + ledger.FloorClamps + "번");
        }

        [Test]
        public void 절약가는_천장에_붙고_거기서_멈춘다()
        {
            // 아무것도 묻지 않으면 신뢰는 오르기만 한다. **천장이 그것을 막는다** —
            // 막지 않으면 값이 발산하고 물음이 공짜가 된다.
            GameData d = TestWorld.Data;
            VillageLedger ledger = new VillageLedger(d);
            List<Round> log = Run(d, ledger, d.Balance.checkers.economyMissions, false, false);
            foreach (Round r in log)
                Assert.That(r.TrustAfter, Is.LessThanOrEqualTo(d.Balance.trust.ceilingPercent));
            Assert.That(ledger.TrustPercent, Is.EqualTo(d.Balance.trust.ceilingPercent));
            Assert.That(ledger.CeilingClamps, Is.GreaterThan(0), "천장이 한 번도 걸리지 않았다");
            Assert.That(ledger.TotalAsks, Is.Zero);
            TestContext.WriteLine("절약가: 신뢰가 천장 " + ledger.TrustPercent + "% 에 붙었다 (천장 "
                + ledger.CeilingClamps + "번 걸림) — 모으기만 하는 것에도 상한이 있다");
        }

        [Test]
        public void 서툰_자가_바닥을_쳐도_되돌아온다()
        {
            // ★ **영구 고갈이 없다**는 것을 실제로 돌려 확인한다.
            // 일부러 들키는 계획으로 신뢰를 바닥까지 떨어뜨린 뒤, 눈먼 계획으로 돌아오는 데
            // 몇 회차가 걸리는지 센다. 되돌아오지 못하면 이 설계는 접어야 한다.
            GameData d = TestWorld.Data;
            TrustBalance tb = d.Balance.trust;
            VillageLedger ledger = new VillageLedger(d);

            List<Round> falling = Run(d, ledger, 10, true, true);
            foreach (Round r in falling) Assert.That(r.Won, Is.False, "일부러 실패하는 계획이 이겼다");
            Assert.That(ledger.TrustPercent, Is.EqualTo(tb.floorPercent),
                "일부러 열 번 실패했는데 신뢰가 바닥에 닿지 않았다: " + ledger.TrustPercent);
            Assert.That(ledger.VillageSpeaks, Is.False, "바닥인데 마을이 아직 입을 연다");
            Assert.That(ledger.FloorClamps, Is.GreaterThan(0));

            int recovered = -1;
            List<Round> rising = Run(d, ledger, d.Balance.checkers.maxFloorRecoveryMissions, false, false);
            for (int i = 0; i < rising.Count; i++)
                if (recovered < 0 && rising[i].TrustAfter >= tb.askFloorPercent) recovered = i + 1;

            Assert.That(recovered, Is.GreaterThan(0),
                "눈먼 승리를 " + rising.Count + "번 해도 말문이 트이지 않았다 — 한 번 바닥나면 갇힌다");
            Assert.That(recovered, Is.LessThanOrEqualTo(d.Balance.checkers.maxFloorRecoveryMissions));
            Assert.That(ledger.VillageSpeaks, "되돌아왔는데도 마을이 입을 닫고 있다");
            TestContext.WriteLine("서툰 자: 열 번 실패로 신뢰 " + tb.startPercent + "% → 바닥 0% · "
                + "눈먼 승리 " + recovered + "회차에 말문이 트였다 (지금 " + ledger.TrustPercent + "%)");
        }

        [Test]
        public void 같은_사람에게_계속_물으면_값이_오른다()
        {
            // 규칙: "같은 사람에게 계속 물으면 그 사람이 위험해진다 — 신뢰가 더 크게 깎인다."
            GameData d = TestWorld.Data;
            MissionDef m = d.Mission("m_dusk_checkpoint");
            Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
            VillageLedger ledger = new VillageLedger(d);
            ledger.ForceTrust(100);
            ledger.BeginMission();
            IntelKnowledge k = new IntelKnowledge(d, m);

            List<int> costs = new List<int>();
            foreach (string itemId in d.Villager("v_weaver").knows)
            {
                int quoted = ledger.QuotedCost("v_weaver");
                AskOutcome o = ledger.Ask(market, k, "v_weaver", itemId);
                Assert.That(o.Granted, "직조공이 " + itemId + " 에 답하지 않았다: " + o.Refusal);
                Assert.That(o.TrustSpent, Is.EqualTo(quoted), "견적과 실제 값이 다르다");
                costs.Add(o.TrustSpent);
            }

            Assert.That(costs.Count, Is.GreaterThanOrEqualTo(3), "한 사람에게 세 번 물을 항목이 없다");
            for (int i = 1; i < costs.Count; i++)
                Assert.That(costs[i], Is.GreaterThanOrEqualTo(costs[i - 1]),
                    i + "번째 물음이 더 싸다: " + string.Join(" ", costs));
            Assert.That(costs[costs.Count - 1], Is.GreaterThan(costs[0]),
                "계속 물어도 값이 오르지 않는다: " + string.Join(" ", costs));
            TestContext.WriteLine("직조공에게 연달아 묻는 값: " + string.Join(" → ", costs) + " (%)");
        }

        [Test]
        public void 위험이_쌓이면_입을_닫고_얼마_뒤_돌아온다()
        {
            // 사라진 사람이 영구히 사라지면 그것도 영구 고갈이다. 돌아와야 한다.
            GameData d = TestWorld.Data;
            AskBalance ab = d.Balance.ask;
            MissionDef m = d.Mission("m_dusk_checkpoint");
            Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
            VillageLedger ledger = new VillageLedger(d);
            ledger.ForceTrust(100);
            ledger.BeginMission();
            IntelKnowledge k = new IntelKnowledge(d, m);

            // 종지기 아이는 값이 싸지만 가장 위험하다 (exposureRiskPercent 4).
            int asks = 0;
            while (!ledger.IsSilent("v_boy") && asks < 20)
            {
                foreach (string itemId in d.Villager("v_boy").knows)
                {
                    if (ledger.IsSilent("v_boy")) break;
                    if (ledger.Ask(market, k, "v_boy", itemId).Granted) asks++;
                }
            }
            Assert.That(ledger.IsSilent("v_boy"),
                "위험이 " + ab.silenceThresholdPercent + " 를 넘도록 물었는데 입을 닫지 않았다");
            Assert.That(ledger.Ask(market, k, "v_boy", "in_post_lane").Refusal, Is.EqualTo(Refusals.Silent));
            TestContext.WriteLine("종지기 아이가 " + asks + "번 만에 입을 닫았다");

            // 얼마 뒤 돌아온다.
            for (int i = 0; i < ab.silenceMissions; i++)
            {
                Assert.That(ledger.IsSilent("v_boy"), "너무 빨리 돌아왔다 (회차 " + i + ")");
                ledger.Settle(TestWorld.Sim("m_ledger_theft").Run(ReferencePlans.LedgerTheftBlind()));
            }
            Assert.That(ledger.IsSilent("v_boy"), Is.False,
                "입을 닫은 사람이 " + ab.silenceMissions + " 회차 뒤에도 돌아오지 않았다 — 영구 상실이다");
            Assert.That(ledger.Exposure("v_boy"), Is.Zero, "돌아왔는데 위험이 남아 있다");
            TestContext.WriteLine("회차 " + ab.silenceMissions + "번 뒤에 돌아왔다");
        }

        [Test]
        public void 입을_닫은_사람이_있어도_임무가_막히지_않는다()
        {
            // 모두가 입을 닫은 회차를 만들어 본다. 그래도 눈먼 계획으로 나갈 수 있어야 한다.
            GameData d = TestWorld.Data;
            HashSet<string> everyone = new HashSet<string>();
            foreach (VillagerDef v in d.AllVillagers) everyone.Add(v.id);

            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Whispers market = new Whispers(d, sim.Mission, sim.Phases, everyone);
                foreach (IntelItemDef item in d.AllItems)
                {
                    Assert.That(market.SourceCount(item.id), Is.Zero, item.id);
                    Assert.That(market.LiarOf(item.id), Is.Null, "말해 줄 사람이 없는데 거짓이 섞였다");
                }
                MissionResult r = sim.Run(ReferencePlans.For(missionId));
                Assert.That(r.Won, missionId + " 가 마을이 전부 입을 닫자 막혔다");
            }
            TestContext.WriteLine("마을 전원이 입을 닫은 회차에서도 임무 셋 전부를 깼다");
        }

        [Test]
        public void 보복은_들킨_자리가_정한다()
        {
            // 두 층을 잇는 둘째 줄. 골목(마을 안)에서 들키는 것이 과수원보다 비싸야
            // "마을이 값을 치른다"가 규칙이 된다.
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_dusk_checkpoint");
            Assert.That(sim.Graph.ReprisalWeight("z_lane"),
                Is.GreaterThan(sim.Graph.ReprisalWeight("z_orchard")), "골목이 과수원보다 싸다");

            // 골목에서 들키는 계획과 종루에서 들키는 계획의 보복을 견준다.
            MissionResult lane = sim.Run(new SquadPlan(new[]
                { new SquadOrder("m_sapper", ActionKinds.PlantTrap, "z_lane", "", 60000) }));
            MissionResult belfry = sim.Run(new SquadPlan(new[]
                { new SquadOrder("m_sapper", ActionKinds.PlantTrap, "z_belfry", "", 60000) }));
            Assert.That(lane.Sightings.Count, Is.GreaterThan(0), "골목에서 들키지 않았다");
            Assert.That(lane.ReprisalPoints, Is.GreaterThan(0));

            VillageLedger a = new VillageLedger(d);
            VillageLedger b = new VillageLedger(d);
            a.Settle(lane);
            b.Settle(belfry);
            Assert.That(a.TrustPercent, Is.LessThanOrEqualTo(b.TrustPercent),
                "골목에서 들킨 값이 종루에서 들킨 값보다 싸다");
            TestContext.WriteLine("골목에서 들킴: 보복 " + lane.ReprisalPoints + " → 신뢰 " + a.TrustPercent
                + "% · 종루에서 들킴: 보복 " + belfry.ReprisalPoints + " → 신뢰 " + b.TrustPercent + "%");
        }
    }
}
