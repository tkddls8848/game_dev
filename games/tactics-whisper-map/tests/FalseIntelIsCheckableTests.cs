using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 검사기 8 — `FalseIntelIsCheckable`.
    /// **틀린 정보가 겹쳐 물으면 반드시 드러나는가.**
    ///
    /// 드러나지 않으면 `DetectionFairness` 가 무너진다. 거기서 "틀린 정보로 죽는 것은 공정하다"고
    /// 판정하는 근거가 **겹쳐 물어 확인할 수 있었다**는 것뿐이기 때문이다.
    /// 그 근거는 `Whispers` 의 불변식 하나에 걸려 있다:
    ///
    /// > **한 항목에 거짓을 말하는 사람은 최대 한 명이다.**
    ///
    /// 그래서 두 사람이 같은 말을 하면 그 답은 참이고, 갈리면 그 항목은 쓸 수 없다는 것을 안다.
    /// 여기서는 그 불변식을 **씨드 전수**로 확인한다 — 한 씨드에서만 성립하는 불변식은 불변식이 아니다.
    /// </summary>
    [TestFixture]
    public sealed class FalseIntelIsCheckableTests
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

        [Test]
        public void 씨드_전수에서_거짓말쟁이는_항목마다_최대_한_명이다()
        {
            GameData d = TestWorld.Data;
            MissionDef basis = d.Mission("m_dusk_checkpoint");
            int sweep = d.Balance.checkers.falseIntelSeedSweep;
            int itemsWithLiar = 0, itemsClean = 0;

            for (int seed = 0; seed < sweep; seed++)
            {
                MissionDef m = Probe(basis, seed);
                Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
                foreach (IntelItemDef item in d.AllItems)
                {
                    int liars = 0;
                    foreach (VillagerDef v in market.AvailableSources(item.id))
                        if (!market.Answer(v.id, item.id).IsTrue) liars++;
                    Assert.That(liars, Is.LessThanOrEqualTo(1),
                        "씨드 " + seed + " 의 " + item.id + " 에 거짓말쟁이가 " + liars + "명이다 — 불변식이 깨졌다");
                    if (liars == 1) itemsWithLiar++; else itemsClean++;
                }
            }
            Assert.That(itemsWithLiar, Is.GreaterThan(0), "씨드 전수에서 거짓이 한 번도 섞이지 않았다");
            Assert.That(itemsClean, Is.GreaterThan(0), "씨드 전수에서 참만 오는 경우가 없었다");
            TestContext.WriteLine("씨드 " + sweep + "개 x 항목 " + d.AllItems.Count + " — 거짓 섞인 항목 "
                + itemsWithLiar + " · 깨끗한 항목 " + itemsClean);
        }

        [Test]
        public void 두_사람이_같은_말을_하면_그_답은_참이다()
        {
            // 이것이 "겹쳐 물으면 확인된다"의 정확한 내용이다.
            GameData d = TestWorld.Data;
            MissionDef basis = d.Mission("m_ledger_theft");
            int agreements = 0;
            for (int seed = 0; seed < d.Balance.checkers.falseIntelSeedSweep; seed++)
            {
                MissionDef m = Probe(basis, seed);
                Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
                foreach (IntelItemDef item in d.AllItems)
                {
                    IList<VillagerDef> sources = market.AvailableSources(item.id);
                    for (int i = 0; i < sources.Count; i++)
                        for (int j = i + 1; j < sources.Count; j++)
                        {
                            Rumor a = market.Answer(sources[i].id, item.id);
                            Rumor b = market.Answer(sources[j].id, item.id);
                            if (!a.SameAs(b)) continue;
                            agreements++;
                            Assert.That(a.IsTrue, "씨드 " + seed + ": " + sources[i].id + " 와 "
                                + sources[j].id + " 가 " + item.id + " 에 대해 같은 거짓말을 했다");
                        }
                }
            }
            Assert.That(agreements, Is.GreaterThan(0));
            TestContext.WriteLine("같은 답이 나온 짝 " + agreements + "쌍이 전부 참이었다");
        }

        [Test]
        public void 거짓말쟁이를_끼워_물으면_반드시_답이_갈린다()
        {
            GameData d = TestWorld.Data;
            int caught = 0;
            foreach (MissionDef basis in d.AllMissions)
                for (int seed = 0; seed < 80; seed++)
                {
                    MissionDef m = Probe(basis, seed);
                    Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
                    foreach (IntelItemDef item in d.AllItems)
                    {
                        string liar = market.LiarOf(item.id);
                        if (liar == null) continue;
                        Rumor lie = market.Answer(liar, item.id);
                        Assert.That(lie.IsTrue, Is.False);
                        foreach (VillagerDef other in market.AvailableSources(item.id))
                        {
                            if (other.id == liar) continue;
                            Assert.That(lie.SameAs(market.Answer(other.id, item.id)), Is.False,
                                "거짓말쟁이와 다른 사람의 답이 같다 — 겹쳐 물어도 드러나지 않는다");
                            caught++;
                        }
                    }
                }
            Assert.That(caught, Is.GreaterThan(0));
            TestContext.WriteLine("거짓말쟁이를 끼운 짝 " + caught + "쌍이 전부 갈렸다");
        }

        [Test]
        public void 출처가_둘_미만이면_아무도_거짓을_말하지_않는다()
        {
            // 확인할 수 없는 소문은 전하지 않는다. 이 규칙이 없으면
            // DetectionFairness 에 FalseAndUncheckable(불공정)이 실제로 생긴다.
            GameData d = TestWorld.Data;
            MissionDef basis = d.Mission("m_granary_charge");
            int lonelyChecked = 0;
            foreach (IntelItemDef target in d.AllItems)
            {
                HashSet<string> silenced = new HashSet<string>();
                IList<VillagerDef> sources = d.SourcesOf(target.id);
                for (int i = 1; i < sources.Count; i++) silenced.Add(sources[i].id);

                for (int seed = 0; seed < 120; seed++)
                {
                    MissionDef m = Probe(basis, seed);
                    Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m), silenced);
                    Assert.That(market.SourceCount(target.id), Is.EqualTo(1), target.id);
                    Assert.That(market.LiarOf(target.id), Is.Null,
                        "출처가 하나뿐인 " + target.id + " 에 거짓이 섞였다 (씨드 " + seed + ")");
                    Assert.That(market.Answer(sources[0].id, target.id).IsTrue, target.id);
                    lonelyChecked++;
                }
            }
            TestContext.WriteLine("혼자 남은 출처 " + lonelyChecked + "가지에서 거짓이 한 번도 나오지 않았다");
        }

        [Test]
        public void 겹쳐_묻기가_표로_거짓을_깬다()
        {
            // 지식 상태 쪽의 규칙: 한 번 물으면 쓸 수 있지만 참인지 모르고,
            // 갈리면 못 쓰고, 셋째를 물으면 다수가 참이 된다.
            GameData d = TestWorld.Data;
            MissionDef basis = d.Mission("m_dusk_checkpoint");
            int single = 0, conflicted = 0, resolved = 0;

            for (int seed = 0; seed < d.Balance.checkers.falseIntelSeedSweep; seed++)
            {
                MissionDef m = Probe(basis, seed);
                Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
                foreach (IntelItemDef item in d.AllItems)
                {
                    string liar = market.LiarOf(item.id);
                    if (liar == null) continue;
                    IList<VillagerDef> sources = market.AvailableSources(item.id);

                    // ① 거짓말쟁이 한 사람에게만 물었다 — 쓸 수 있지만 거짓이다
                    IntelKnowledge one = new IntelKnowledge(d, m);
                    one.Accept(market.Answer(liar, item.id));
                    Assert.That(one.Holds(item.id), item.id + " 를 한 번 물었는데 못 쓴다");
                    Assert.That(one.HoldsFalse(item.id), item.id + " 의 거짓을 쥐었는데 참으로 본다");
                    single++;

                    // ② 다른 사람을 겹쳐 물었다 — 갈린다. 누가 거짓인지는 몰라도 못 쓴다는 것을 안다
                    IntelKnowledge two = new IntelKnowledge(d, m);
                    two.Accept(market.Answer(liar, item.id));
                    string second = null;
                    foreach (VillagerDef v in sources) if (v.id != liar) { second = v.id; break; }
                    two.Accept(market.Answer(second, item.id));
                    Assert.That(two.IsConflicted(item.id), item.id + " 를 겹쳐 물었는데 갈리지 않았다");
                    Assert.That(two.Holds(item.id), Is.False, "갈린 항목을 아직 쓴다");
                    conflicted++;

                    // ③ 셋째가 있으면 다수가 참이다
                    if (sources.Count < 3) continue;
                    IntelKnowledge three = new IntelKnowledge(d, m);
                    foreach (VillagerDef v in sources) three.Accept(market.Answer(v.id, item.id));
                    Assert.That(three.IsCorroborated(item.id), item.id + " 를 셋에게 물었는데 확인되지 않았다");
                    Assert.That(three.HoldsFalse(item.id), Is.False, item.id + " 의 다수가 거짓이다");
                    resolved++;
                }
            }
            Assert.That(single, Is.GreaterThan(0));
            Assert.That(conflicted, Is.EqualTo(single), "겹쳐 물어 갈리지 않은 경우가 있다");
            Assert.That(resolved, Is.GreaterThan(0), "셋째를 물어 다수로 푸는 경우가 없었다");
            TestContext.WriteLine("한 번 물어 거짓을 쥔 경우 " + single + " · 겹쳐 물어 갈린 경우 " + conflicted
                + " · 셋째로 다수를 세운 경우 " + resolved);
        }

        [Test]
        public void 겹쳐_묻기는_신뢰를_더_쓴다()
        {
            // 규칙: "두 사람에게 겹쳐 물으면 맞는지 알 수 있지만 신뢰를 두 배 쓴다."
            GameData d = TestWorld.Data;
            MissionDef m = d.Mission("m_dusk_checkpoint");
            Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));

            VillageLedger once = new VillageLedger(d);
            once.ForceTrust(100); once.BeginMission();
            IntelKnowledge k1 = new IntelKnowledge(d, m);
            once.Ask(market, k1, "v_weaver", "in_route_bell");

            VillageLedger twice = new VillageLedger(d);
            twice.ForceTrust(100); twice.BeginMission();
            IntelKnowledge k2 = new IntelKnowledge(d, m);
            twice.Ask(market, k2, "v_weaver", "in_route_bell");
            twice.Ask(market, k2, "v_widow", "in_route_bell");

            Assert.That(twice.TotalTrustSpent, Is.GreaterThan(once.TotalTrustSpent),
                "겹쳐 물었는데 값이 더 들지 않았다");
            Assert.That(k2.IsCorroborated("in_route_bell") || k2.IsConflicted("in_route_bell"),
                "겹쳐 물었는데 확인도 갈림도 아니다");
            TestContext.WriteLine("한 번 " + once.TotalTrustSpent + "% · 겹쳐 " + twice.TotalTrustSpent + "%");
        }
    }
}
