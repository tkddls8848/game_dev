using System.Collections.Generic;
using Graft.Data;
using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// 공통 검사기 `DataValidator` — 참조 무결성.
    ///
    /// 여기서 잡는 것은 전부 **고장**이다(POC_FACTORY §5). 재미와 섞지 않는다.
    /// 다른 검사기 전부가 이 파일이 통과한다는 것을 전제로 돈다 — 참조가 깨진 데이터에서
    /// 성공률을 비교하는 것은 아무 뜻이 없다.
    /// </summary>
    [TestFixture]
    public sealed class DataIntegrityTests
    {
        private static GameData D { get { return TestWorld.Data; } }

        [Test]
        public void 아이디가_겹치지_않는다()
        {
            AssertUnique("people", Ids(D.AllPeople));
            AssertUnique("places", Ids(D.AllPlaces));
            List<string> moods = new List<string>();
            foreach (MoodDef m in D.AllMoods) moods.Add(m.id);
            AssertUnique("moods", moods);
            List<string> coms = new List<string>();
            foreach (CommissionDef c in D.AllCommissions) coms.Add(c.id);
            AssertUnique("commissions", coms);

            foreach (SubjectDef s in AllSubjects())
            {
                List<string> ids = new List<string>();
                foreach (MemoryDef m in s.memories) ids.Add(m.id);
                AssertUnique(s.id + ".memories", ids);
            }
        }

        [Test]
        public void 정서_거리_행렬이_옳다()
        {
            int n = D.AllMoods.Count;
            Assert.That(D.Net.moodDistance.Length, Is.EqualTo(n * n),
                        "정서 거리 행렬의 크기가 정서 수의 제곱이 아니다");
            for (int a = 0; a < n; a++)
            {
                Assert.That(D.Net.moodDistance[a * n + a], Is.Zero, "대각이 0이 아니다: " + a);
                for (int b = 0; b < n; b++)
                {
                    int ab = D.Net.moodDistance[a * n + b];
                    int ba = D.Net.moodDistance[b * n + a];
                    Assert.That(ab, Is.EqualTo(ba), "대칭이 아니다: " + a + "," + b);
                    Assert.That(ab, Is.InRange(0, 100), "0~100 밖이다: " + a + "," + b);
                }
            }
        }

        [Test]
        public void 기억이_가리키는_것이_전부_있다()
        {
            foreach (SubjectDef s in AllSubjects())
                foreach (MemoryDef m in s.memories)
                {
                    Assert.That(D.HasPlace(m.placeId), Is.True, m.id + " 의 장소가 없다: " + m.placeId);
                    Assert.That(D.HasMood(m.moodId), Is.True, m.id + " 의 정서가 없다: " + m.moodId);
                    Assert.That(m.peopleIds, Is.Not.Null, m.id + " 의 peopleIds 가 null 이다");
                    foreach (string p in m.peopleIds)
                        Assert.That(D.HasPerson(p), Is.True, m.id + " 의 사람이 없다: " + p);

                    Assert.That(m.dayIndex, Is.GreaterThanOrEqualTo(0), m.id + " 의 날이 음수다");
                    Assert.That(m.spanDays, Is.GreaterThan(0), m.id + " 의 기간이 0 이하다");
                    Assert.That(m.intensityPercent, Is.InRange(0, 100), m.id + " 의 세기가 0~100 밖이다");
                    Assert.That(m.fixityPercent, Is.InRange(1, 100), m.id + " 의 굳기가 1~100 밖이다");

                    if (m.truePlaceId.Length > 0)
                        Assert.That(D.HasPlace(m.truePlaceId), Is.True, m.id + " 의 참 장소가 없다");
                    if (m.trueMoodId.Length > 0)
                        Assert.That(D.HasMood(m.trueMoodId), Is.True, m.id + " 의 참 정서가 없다");
                }
        }

        /// <summary>
        /// **기억망 자신이 제 규칙을 지키는가.** 사람이 없던 날, 없던 장소에 기억이 놓여 있으면
        /// 그 기억은 태어날 때부터 거부 반응을 안고 있는 것이고, 검사기가 낸 수치가 전부 거짓이 된다.
        /// </summary>
        [Test]
        public void 기억망이_스스로_모순되지_않는다()
        {
            foreach (SubjectDef s in AllSubjects())
                foreach (MemoryDef m in s.memories)
                {
                    int start = m.dayIndex, end = m.dayIndex + m.spanDays - 1;
                    Assert.That(D.PlaceExists(m.placeId, start) && D.PlaceExists(m.placeId, end), Is.True,
                                m.id + ": " + m.placeId + " 가 " + start + "~" + end + " 에 없다");
                    foreach (string p in m.peopleIds)
                        Assert.That(D.PersonPresent(p, start) && D.PersonPresent(p, end), Is.True,
                                    m.id + ": " + p + " 가 " + start + "~" + end + " 에 없다");
                }
        }

        [Test]
        public void 연상_간선이_옳다()
        {
            foreach (SubjectDef s in AllSubjects())
            {
                MemoryNet net = new MemoryNet(s);
                HashSet<string> seen = new HashSet<string>();
                foreach (LinkDef l in s.links)
                {
                    Assert.That(net.Has(l.a), Is.True, s.id + ": 없는 기억을 잇는다: " + l.a);
                    Assert.That(net.Has(l.b), Is.True, s.id + ": 없는 기억을 잇는다: " + l.b);
                    Assert.That(l.a, Is.Not.EqualTo(l.b), s.id + ": 자기 자신을 잇는다: " + l.a);
                    Assert.That(l.probeCost, Is.GreaterThan(0), s.id + ": 값이 0 이하인 간선: " + l.a + "-" + l.b);
                    Assert.That(l.kind, Is.AnyOf("place", "person", "time", "cause"),
                                s.id + ": 없는 연상 종류: " + l.kind);
                    string key = string.CompareOrdinal(l.a, l.b) < 0 ? l.a + "|" + l.b : l.b + "|" + l.a;
                    Assert.That(seen.Add(key), Is.True, s.id + ": 같은 짝이 두 번 이어져 있다: " + key);
                }
            }
        }

        [Test]
        public void 모든_기억에_닿을_수_있다()
        {
            foreach (SubjectDef s in AllSubjects())
            {
                MemoryNet net = new MemoryNet(s);
                Assert.That(s.surfacedAtStart.Length, Is.GreaterThan(0), s.id + ": 처음 떠 있는 기억이 없다");
                foreach (string id in s.surfacedAtStart)
                    Assert.That(net.Has(id), Is.True, s.id + ": 없는 기억이 처음부터 떠 있다: " + id);

                Dictionary<string, ProbeRoute> routes = net.CheapestRoutes(net.StartSurfaced());
                foreach (MemoryDef m in s.memories)
                    Assert.That(routes.ContainsKey(m.id), Is.True,
                                s.id + ": 어떤 길로도 닿지 않는 기억이 있다: " + m.id);
            }
        }

        [Test]
        public void 스크린_후보에_참값이_적혀_있다()
        {
            foreach (SubjectDef s in AllSubjects())
                foreach (MemoryDef m in s.memories)
                {
                    if (!m.distortable) continue;
                    bool any = m.trueDayIndex >= 0 || m.truePlaceId.Length > 0 || m.trueMoodId.Length > 0;
                    Assert.That(any, Is.True, m.id + ": distortable 인데 참값이 하나도 없다 — 스크린이 될 수 없다");
                    if (m.truePlaceId.Length > 0)
                        Assert.That(m.truePlaceId, Is.Not.EqualTo(m.placeId), m.id + ": 참 장소가 겉값과 같다");
                    if (m.trueMoodId.Length > 0)
                        Assert.That(m.trueMoodId, Is.Not.EqualTo(m.moodId), m.id + ": 참 정서가 겉값과 같다");
                    if (m.trueDayIndex >= 0)
                        Assert.That(m.trueDayIndex, Is.Not.EqualTo(m.dayIndex), m.id + ": 참 날이 겉값과 같다");
                }
        }

        [Test]
        public void 의뢰가_가리키는_것이_전부_있고_격자에_맞는다()
        {
            BalanceFile b = D.Balance;
            foreach (CommissionDef c in D.AllCommissions)
            {
                SubjectDef s = D.Subject(c.subjectId);
                MemoryNet net = new MemoryNet(s);

                foreach (string p in c.requiredPeopleIds)
                    Assert.That(D.HasPerson(p), Is.True, c.id + ": 없는 사람을 요구한다: " + p);
                Assert.That(c.allowedPlaceIds.Length, Is.GreaterThan(0), c.id + ": 고를 장소가 없다");
                foreach (string p in c.allowedPlaceIds)
                    Assert.That(D.HasPlace(p), Is.True, c.id + ": 없는 장소를 허락한다: " + p);
                Assert.That(c.allowedMoodIds.Length, Is.GreaterThan(0), c.id + ": 고를 정서가 없다");
                foreach (string m in c.allowedMoodIds)
                    Assert.That(D.HasMood(m), Is.True, c.id + ": 없는 정서를 허락한다: " + m);

                Assert.That(c.dayWindowEnd, Is.GreaterThan(c.dayWindowStart), c.id + ": 날 창이 뒤집혔다");
                Assert.That((c.dayWindowEnd - c.dayWindowStart) % b.dayStep, Is.Zero,
                            c.id + ": 날 창이 격자(" + b.dayStep + ")로 나누어지지 않는다 — 끝 날을 고를 수 없다");
                Assert.That(c.intensityStep, Is.GreaterThan(0), c.id + ": 세기 격자가 0 이하다");
                Assert.That((c.intensityMax - c.intensityMin) % c.intensityStep, Is.Zero,
                            c.id + ": 세기 범위가 격자로 나누어지지 않는다");
                Assert.That(c.spanDays, Is.GreaterThan(0), c.id + ": 심을 기억의 기간이 0 이하다");
                Assert.That(c.lucidityBudget, Is.GreaterThan(0), c.id + ": 명료도 예산이 0 이하다");
                Assert.That(c.toleranceBudget, Is.GreaterThan(0), c.id + ": 떨림 허용치가 0 이하다");
                Assert.That(c.singleEdgeCap, Is.GreaterThan(c.toleranceBudget), c.id + ": 간선 상한이 총합 허용치보다 작다");
                Assert.That(c.seed, Is.GreaterThan(0), c.id + ": 씨드가 없다");

                // 이 의뢰가 실제로 고를 수 있는 선택이 있는가
                List<GraftChoice> all = GraftRules.LegalChoices(c, net.StartSurfaced(), b.dayStep);
                Assert.That(all.Count, Is.GreaterThan(0), c.id + ": 합법 선택이 하나도 없다");
            }
        }

        [Test]
        public void 저울과_규칙표가_옳다()
        {
            BalanceFile b = D.Balance;
            Assert.That(b.trialSeeds, Is.GreaterThan(0));
            Assert.That(b.dayStep, Is.GreaterThan(0));
            Assert.That(b.corroborateRoutes, Is.GreaterThanOrEqualTo(2),
                        "겹쳐 묻기가 길 하나로 되면 스크린 기억이 뜻을 잃는다");
            foreach (int w in new[] { b.timePlaceWeight, b.personElsewhereWeight, b.personAbsentWeight,
                                      b.placeWindowWeight, b.moodAnchorWeight, b.eraWeight, b.intensityWeight })
                Assert.That(w, Is.GreaterThan(0), "저울에 0 이하가 있다");
            foreach (int t in new[] { b.moodTolerance, b.eraTolerance, b.intensityTolerance })
                Assert.That(t, Is.InRange(0, 100));
            Assert.That(b.eraWindowDays, Is.GreaterThan(0));
            Assert.That(b.selfSignature.minTrembleEdges, Is.GreaterThanOrEqualTo(2));
            Assert.That(b.selfSignature.secondaryFloor, Is.GreaterThan(0));

            // 규칙표와 엔진이 정확히 맞물려야 한다. 한쪽이 더 많으면 거짓 약속이다.
            string[] engine = RejectionAudit.EngineRules();
            Assert.That(b.statedRules.Length, Is.EqualTo(engine.Length), "규칙표와 엔진의 규칙 수가 다르다");
            foreach (string r in engine)
                Assert.That(b.statedRules, Contains.Item(r), "엔진이 내는데 규칙표에 없다: " + r);
        }

        [Test]
        public void 자기_기억망에_심어진_것이_적혀_있다()
        {
            SubjectDef s = D.Subject("subj-operator");
            int grafted = 0;
            foreach (MemoryDef m in s.memories) if (m.graftedOnDay >= 0) grafted++;
            Assert.That(grafted, Is.GreaterThan(0),
                        "시술자 기억망에 심어진 기억이 없으면 SelfSuspicion 이 아무것도 재지 않는다");

            SubjectDef client = D.Subject("subj-hanwoo");
            foreach (MemoryDef m in client.memories)
                Assert.That(m.graftedOnDay, Is.LessThan(0),
                            "의뢰인 기억망은 음성 대조군이다. 심어진 것이 있으면 안 된다: " + m.id);
        }

        // ── 도움 ─────────────────────────────────────────────────────────────

        private static List<SubjectDef> AllSubjects()
        {
            List<SubjectDef> all = new List<SubjectDef>();
            foreach (SubjectDef s in D.Net.subjects) all.Add(s);
            foreach (SubjectDef s in D.SelfNet.subjects) all.Add(s);
            return all;
        }

        private static List<string> Ids(IList<PersonDef> ps)
        {
            List<string> ids = new List<string>();
            foreach (PersonDef p in ps) ids.Add(p.id);
            return ids;
        }

        private static List<string> Ids(IList<PlaceDef> ps)
        {
            List<string> ids = new List<string>();
            foreach (PlaceDef p in ps) ids.Add(p.id);
            return ids;
        }

        private static void AssertUnique(string what, List<string> ids)
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (string id in ids)
            {
                Assert.That(id, Is.Not.Null.And.Not.Empty, what + ": 빈 id 가 있다");
                Assert.That(seen.Add(id), Is.True, what + ": id 가 겹친다: " + id);
            }
        }
    }
}
