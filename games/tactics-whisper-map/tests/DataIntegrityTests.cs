using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 참조 무결성 · 정수 규율 · 설계 원칙 게이트.
    /// 이 묶음이 통과하지 않으면 아래 검사기 전부가 무의미해진다 — 데이터가 거짓말을 하고 있다는 뜻이므로.
    /// </summary>
    [TestFixture]
    public sealed class DataIntegrityTests
    {
        private static string SrcRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "src");
                if (File.Exists(Path.Combine(candidate, "MissionSim.cs"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("src/MissionSim.cs 를 찾지 못했다");
        }

        [Test]
        public void 모든_참조가_이어진다()
        {
            GameData d = TestWorld.Data;
            foreach (MissionDef m in d.AllMissions)
            {
                MapDef map = d.Map(m.mapId);
                Assert.That(m.guardIds.Length, Is.EqualTo(2), m.id + " 의 순찰은 2인이다");
                foreach (string g in m.guardIds)
                    Assert.That(d.Guard(g).mapId, Is.EqualTo(m.mapId), g + " 가 다른 지도 것이다");
                foreach (string sc in m.shiftChangeIds) d.ShiftChange(sc);
                if (!string.IsNullOrEmpty(m.destroyTargetId))
                    Assert.That(d.Target(m.destroyTargetId).mapId, Is.EqualTo(m.mapId));
                if (!string.IsNullOrEmpty(m.stealDocumentId))
                    Assert.That(d.Document(m.stealDocumentId).mapId, Is.EqualTo(m.mapId));
                Assert.That(map.zones.Length, Is.EqualTo(6), "구역은 6개다 (늘리지 않는다)");
            }
            Assert.That(d.AllMembers.Count, Is.EqualTo(3), "분대는 3인이다");
            Assert.That(d.AllMissions.Count, Is.InRange(2, 3), "임무는 2~3개다");

            foreach (VillagerDef v in d.AllVillagers)
                foreach (string itemId in v.knows) d.Item(itemId);
            foreach (IntelItemDef i in d.AllItems)
            {
                if (i.kind == IntelKinds.Shift) d.ShiftChange(i.subjectId);
                else d.Guard(i.subjectId);
            }
        }

        [Test]
        public void 틀릴_수_있는_항목은_출처가_둘_이상이다()
        {
            // 이것이 DetectionFairness 의 전제다. 하나뿐이면 겹쳐 물을 수 없고, 그러면
            // 틀린 정보로 죽는 것이 불공정해진다.
            GameData d = TestWorld.Data;
            foreach (IntelItemDef item in d.AllItems)
            {
                int sources = d.SourcesOf(item.id).Count;
                Assert.That(sources, Is.GreaterThanOrEqualTo(1), item.id + " 를 아는 사람이 없다");
                if (item.falsifiable)
                    Assert.That(sources, Is.GreaterThanOrEqualTo(2),
                        item.id + " 는 틀릴 수 있는데 출처가 " + sources + " 사람뿐이다 — 겹쳐 물을 수 없다");
            }
        }

        [Test]
        public void 순찰_경로와_배치와_교대를_아는_사람이_다_있다()
        {
            GameData d = TestWorld.Data;
            foreach (MissionDef m in d.AllMissions)
            {
                foreach (string g in m.guardIds)
                {
                    Assert.That(d.ItemFor(IntelKinds.Route, g), Is.Not.Null, g + " 의 경로를 아는 사람이 없다");
                    Assert.That(d.ItemFor(IntelKinds.Post, g), Is.Not.Null, g + " 의 배치를 아는 사람이 없다");
                }
                foreach (ShiftChangeDef sc in d.ShiftChangesOf(m))
                    Assert.That(d.ItemFor(IntelKinds.Shift, sc.id), Is.Not.Null, sc.id + " 을 아는 사람이 없다");
            }
        }

        [Test]
        public void 거짓_답은_참과_반드시_다르다()
        {
            // 한 칸 돌린 경로가 원래와 같아지면(예: 대칭 경로) 거짓말이 거짓말이 아니게 된다.
            GameData d = TestWorld.Data;
            foreach (MissionDef m in d.AllMissions)
            {
                PhaseAssignment truth = PhaseAssignment.Tonight(d, m);
                Whispers market = new Whispers(d, m, truth);
                foreach (IntelItemDef item in d.AllItems)
                {
                    if (!item.falsifiable) continue;
                    IList<VillagerDef> sources = market.AvailableSources(item.id);
                    Assert.That(sources.Count, Is.GreaterThanOrEqualTo(2), item.id);
                    // 거짓말쟁이를 강제로 세워 답을 비교한다 (시장이 이번에 거짓을 안 넣었을 수도 있다).
                    Whispers forced = new Whispers(d, m, truth);
                    Rumor t = forced.Truth(item.id);
                    Rumor lie = ForcedLie(d, m, truth, item, sources[0].id);
                    if (lie == null) continue;
                    Assert.That(lie.SameAs(t), Is.False, item.id + " 의 거짓 답이 참과 같다 — 거짓말이 성립하지 않는다");
                }
            }
        }

        /// <summary>거짓 답을 강제로 만든다. 씨드를 바꿔 가며 그 사람이 거짓말쟁이가 되는 시장을 찾는다.</summary>
        private static Rumor ForcedLie(GameData d, MissionDef mission, PhaseAssignment truth,
                                        IntelItemDef item, string villagerId)
        {
            for (int seed = 0; seed < 400; seed++)
            {
                MissionDef probe = new MissionDef
                {
                    id = mission.id, name = mission.name, mapId = mission.mapId,
                    lengthMs = mission.lengthMs, seed = seed, alarmLimit = mission.alarmLimit,
                    guardIds = mission.guardIds, shiftChangeIds = mission.shiftChangeIds
                };
                Whispers w = new Whispers(d, probe, truth);
                if (w.LiarOf(item.id) != villagerId) continue;
                return w.Answer(villagerId, item.id);
            }
            return null;
        }

        [Test]
        public void 분대원의_수단이_겹치지_않는다()
        {
            // 겹치면 한 명만 쓰는 최적해가 생긴다 (PLAN_TACTICS §4).
            GameData d = TestWorld.Data;
            Dictionary<string, string> owner = new Dictionary<string, string>();
            foreach (MemberDef m in d.AllMembers)
                foreach (MemberActionDef a in m.actions)
                {
                    Assert.That(owner.ContainsKey(a.kind), Is.False,
                        a.kind + " 를 " + owner.GetValueOrDefault(a.kind, "") + " 와 " + m.id + " 가 같이 갖는다");
                    owner[a.kind] = m.id;
                }
            Assert.That(owner.Count, Is.EqualTo(5), "수단은 다섯이다: 탈취·저격·함정·폭약·폭파");
        }

        [Test]
        public void src_에_UnityEngine_과_부동소수가_없다()
        {
            // 설계 원칙 1·4. 이 게이트가 있어야 헤드리스 검증이 무의미해지지 않는다.
            Regex floats = new Regex(@"\b(float|double|decimal)\b|\b\d+\.\d+[fdm]?\b");
            foreach (string path in Directory.GetFiles(SrcRoot(), "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(path);
                Assert.That(text.Contains("using UnityEngine"), Is.False, path + " 가 UnityEngine 을 끌어들였다");
                Assert.That(text.Contains("UnityEngine."), Is.False, path + " 가 UnityEngine 을 쓴다");
                Match m = floats.Match(text);
                Assert.That(m.Success, Is.False, path + " 에 부동소수가 있다: " + m.Value);
            }
        }

        [Test]
        public void 회차_전개에는_난수가_없다()
        {
            // 씨드는 위상과 거짓 배분에만 쓴다. 회차가 난수를 쓰면 TickDeterminism 이 거짓이 된다.
            string text = File.ReadAllText(Path.Combine(SrcRoot(), "MissionSim.cs"));
            Assert.That(text.Contains("Random"), Is.False, "MissionSim.cs 에 난수가 들어왔다");
            Assert.That(text.Contains("DateTime"), Is.False, "MissionSim.cs 가 시계를 읽는다");

            // 난수를 쓰는 곳은 셋뿐이다: 위상 선택 · 거짓 배분 · 무작위 계획 생성.
            List<string> withRandom = new List<string>();
            foreach (string path in Directory.GetFiles(SrcRoot(), "*.cs", SearchOption.AllDirectories))
                if (File.ReadAllText(path).Contains("new Random(")) withRandom.Add(Path.GetFileName(path));
            withRandom.Sort(StringComparer.Ordinal);
            Assert.That(withRandom, Is.EqualTo(new List<string> { "PatrolModel.cs", "PlanSearch.cs", "Whispers.cs" }),
                "난수를 쓰는 파일이 달라졌다: " + string.Join(" ", withRandom));
        }

        [Test]
        public void 수치가_전부_정수이고_값없는_int는_명시된다()
        {
            GameData d = TestWorld.Data;
            MapDef map = d.Map("map_village_edge");
            foreach (ZoneDef z in map.zones)
            {
                Assert.That(z.coverPercent, Is.InRange(0, 100), z.id);
                Assert.That(z.reprisalWeightPercent, Is.InRange(0, 100), z.id + " 의 보복 가중치");
            }
            foreach (LinkDef l in map.links)
                Assert.That(l.extraAttenuation, Is.EqualTo(-1),
                    "인접 링크의 extraAttenuation 은 '없다'는 뜻의 -1 이어야 한다 (설계 원칙 3)");
            foreach (LinkDef b in map.noiseBlocked)
                Assert.That(b.extraAttenuation, Is.GreaterThan(0), "차폐는 실제 값이 있어야 한다");
            foreach (GuardDef g in d.Patrols.guards)
            {
                int cycle = 0;
                foreach (LegDef leg in g.legs) { Assert.That(leg.dwellMs, Is.GreaterThan(0)); cycle += leg.dwellMs; }
                Assert.That(cycle, Is.EqualTo(180000), g.id + " 의 주기가 180초가 아니다");
                Assert.That(g.phaseOptionsMs.Length, Is.EqualTo(3), g.id + " 의 위상 선택지는 셋이다");
                foreach (int p in g.phaseOptionsMs) Assert.That(p % 1000, Is.Zero, "위상이 틱 격자에 없다");
            }
            foreach (MemberDef m in d.AllMembers)
            {
                Assert.That(m.moveMsPerZone % d.Balance.tickMs, Is.Zero, m.id + " 의 이동 시간이 틱 격자에 없다");
                foreach (MemberActionDef a in m.actions)
                {
                    Assert.That(a.durationMs % d.Balance.tickMs, Is.Zero, m.id + "/" + a.kind);
                    Assert.That(a.holdWindowMs % d.Balance.tickMs, Is.Zero, m.id + "/" + a.kind);
                }
            }
            foreach (ConcealDef c in map.concealment)
                Assert.That(c.settleMs % d.Balance.tickMs, Is.Zero, c.id + " 의 정착 시간이 틱 격자에 없다");
        }

        [Test]
        public void 신뢰_비용의_나머지를_버리지_않는다()
        {
            // 설계 원칙 4. 백분율 곱셈의 나머지를 버리면 값이 새고, 40회차 뒤에 경제가 다른 곳에 있다.
            GameData d = TestWorld.Data;
            VillageLedger ledger = new VillageLedger(d);
            ledger.ForceTrust(100);
            ledger.BeginMission();
            MissionDef m = d.Mission("m_dusk_checkpoint");
            Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
            IntelKnowledge k = new IntelKnowledge(d, m);

            int spentSum = 0;
            for (int i = 0; i < 5; i++)
            {
                AskOutcome o = ledger.Ask(market, k, "v_boy", "in_route_bell");
                if (!o.Granted) break;
                spentSum += o.TrustSpent;
            }
            Assert.That(ledger.TotalTrustSpent, Is.EqualTo(spentSum));
            Assert.That(ledger.CostRemainder, Is.InRange(0, 99), "나머지가 범위를 벗어났다");
            Assert.That(100 - ledger.TrustPercent, Is.EqualTo(spentSum), "쓴 값과 줄어든 신뢰가 다르다");
        }
    }
}
