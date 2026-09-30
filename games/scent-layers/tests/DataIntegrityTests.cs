using System.Collections.Generic;
using NUnit.Framework;
using Scent.Data;
using Scent.Sim;

namespace Scent.Tests
{
    /// <summary>
    /// 공통 검사기 — `DataValidator`. 참조 무결성과 이 PoC가 기대는 구조 불변식.
    /// 여기서 걸리는 것은 전부 **고장**이지 설계 판단이 아니다.
    /// </summary>
    [TestFixture]
    public sealed class DataIntegrityTests
    {
        [Test]
        public void 향_계열이_성립한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> seen = new HashSet<string>();
            HashSet<int> orders = new HashSet<int>();
            foreach (NoteDef n in d.Notes.notes)
            {
                Assert.That(seen.Add(n.id), "계열 id가 겹친다: " + n.id);
                Assert.That(orders.Add(n.bandOrder), "bandOrder 가 겹친다: " + n.id);
                Assert.That(n.retainPermille, Is.InRange(1, 999), n.id + " 의 감쇠율이 범위 밖이다");
                Assert.That(n.klass, Is.AnyOf("top", "heart", "base"), n.id);
                Assert.That(n.ko, Is.Not.Null.And.Not.Empty, n.id + " 에 한국어 원문이 없다");
                StringAssert.IsMatch("^#[0-9A-Fa-f]{6}$", n.color, n.id + " 의 색이 6자리 16진수가 아니다");
            }
            Assert.That(d.Notes.decayStepMin, Is.GreaterThan(0));
            Assert.That(d.Notes.perceptionFloor, Is.GreaterThan(0));
        }

        [Test]
        public void 머리향만이_시각을_말한다는_약속이_수치로_지켜진다()
        {
            // 이 PoC의 규칙: top+base 쌍만 나이를 가른다. heart+base 는 갈라서는 안 된다.
            // 허용 오차(ratioTolPermille)와 계급별 감쇠율 간격이 실제로 그렇게 되어 있는지 본다.
            GameData d = TestWorld.Data;
            int tol = d.Balance.perception.ratioTolPermille;
            int slowest = 0, fastestTop = 1000, fastestHeart = 1000;
            foreach (NoteDef n in d.Notes.notes)
            {
                if (n.klass == "base" && n.retainPermille > slowest) slowest = n.retainPermille;
                if (n.klass == "top" && n.retainPermille < fastestTop) fastestTop = n.retainPermille;
                if (n.klass == "heart" && n.retainPermille < fastestHeart) fastestHeart = n.retainPermille;
            }
            // 걸음마다 비율이 꺾이는 폭(천분율). tol 보다 커야 이웃 나이를 함께 받아들이지 않는다.
            int topGap = 1000 - fastestTop * 1000 / slowest;
            foreach (NoteDef n in d.Notes.notes)
            {
                if (n.klass != "heart") continue;
                int heartGap = 1000 - n.retainPermille * 1000 / slowest;
                Assert.That(heartGap, Is.LessThan(tol),
                    n.id + " 는 잔향과 " + heartGap + "‰ 벌어져 허용 오차 " + tol + "‰ 를 넘는다 — 중간향이 시각을 말해 버린다");
            }
            Assert.That(topGap, Is.GreaterThan(tol * 2),
                "머리향과 잔향의 간격이 " + topGap + "‰ 뿐이다 — 되짚기가 흔들린다");
            TestContext.WriteLine("머리향-잔향 간격 " + topGap + "‰ · 허용 오차 " + tol + "‰ · 중간향은 전부 오차 안");
        }

        [Test]
        public void 사람의_향_지문이_성립한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> ids = new HashSet<string>();
            Dictionary<string, string> markerOwner = new Dictionary<string, string>();
            foreach (ActorDef a in d.Actors.actors)
            {
                Assert.That(ids.Add(a.id), "사람 id가 겹친다: " + a.id);
                Assert.That(a.ko, Is.Not.Null.And.Not.Empty, a.id + " 에 한국어 원문이 없다");
                int sum = 0;
                bool hasTop = false, hasMarker = false;
                HashSet<string> notes = new HashSet<string>();
                foreach (NoteWeight w in a.profile)
                {
                    Assert.That(d.HasNote(w.note), a.id + " 가 없는 계열을 쓴다: " + w.note);
                    Assert.That(notes.Add(w.note), a.id + " 의 지문에 " + w.note + " 가 두 번 있다");
                    Assert.That(w.permille, Is.GreaterThan(0), a.id + "·" + w.note);
                    sum += w.permille;
                    if (d.Note(w.note).klass == "top") hasTop = true;
                    if (w.note == a.marker) hasMarker = true;
                }
                Assert.That(sum, Is.EqualTo(1000), a.id + " 의 지문 합이 " + sum + " 이다");
                Assert.That(hasTop, a.id + " 에게 머리향이 없다 — 시각을 영영 못 잰다");
                Assert.That(hasMarker, a.id + " 의 표지 " + a.marker + " 가 지문에 없다");
                Assert.That(d.Note(a.marker).klass, Is.EqualTo("base"),
                    a.id + " 의 표지가 잔향이 아니다 — 알아보는 것이 늦게 불가능해진다");
                Assert.That(markerOwner.ContainsKey(a.marker), Is.False,
                    "표지 " + a.marker + " 를 " + a.id + " 와 " + (markerOwner.ContainsKey(a.marker) ? markerOwner[a.marker] : "") + " 가 함께 쓴다");
                markerOwner[a.marker] = a.id;
            }
            // 표지가 아닌 계열은 정말로 겹쳐야 한다 — 겹치지 않으면 섞임이 일어나지 않는다
            Dictionary<string, int> users = new Dictionary<string, int>();
            foreach (ActorDef a in d.Actors.actors)
                foreach (NoteWeight w in a.profile)
                    users[w.note] = (users.ContainsKey(w.note) ? users[w.note] : 0) + 1;
            int shared = 0;
            foreach (KeyValuePair<string, int> kv in users) if (kv.Value > 1) shared++;
            Assert.That(shared, Is.GreaterThanOrEqualTo(3), "겹치는 계열이 " + shared + "종뿐이다 — 섞일 것이 없다");
            TestContext.WriteLine("사람 " + d.Actors.actors.Count + "명 · 표지 " + markerOwner.Count + "종 · 겹치는 계열 " + shared + "종");
        }

        [Test]
        public void 집이_성립한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> ids = new HashSet<string>();
            foreach (RoomDef r in d.House.rooms)
            {
                Assert.That(ids.Add(r.id), "방 id가 겹친다: " + r.id);
                Assert.That(r.ko, Is.Not.Null.And.Not.Empty, r.id + " 에 한국어 원문이 없다");
            }
            Assert.That(d.HasRoom(d.House.entry), "현관 " + d.House.entry + " 가 방 목록에 없다");
            foreach (EdgeDef e in d.House.edges)
            {
                Assert.That(d.HasRoom(e.a), "없는 방을 잇는다: " + e.a);
                Assert.That(d.HasRoom(e.b), "없는 방을 잇는다: " + e.b);
                Assert.That(e.a, Is.Not.EqualTo(e.b), "자기 자신을 잇는 길이 있다: " + e.a);
                Assert.That(e.walkMin, Is.GreaterThan(0), e.a + "-" + e.b);
            }
            foreach (string a in d.RoomIds)
                foreach (string b in d.RoomIds)
                    Assert.That(d.Walk(a, b), Is.LessThan(1 << 20), a + " 에서 " + b + " 로 갈 수 없다");
            TestContext.WriteLine("방 " + d.House.rooms.Count + "개 · 길 " + d.House.edges.Count + "개 · 전부 이어져 있다");
        }

        [Test]
        public void 그날_하루가_성립한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> ids = new HashSet<string>();
            HashSet<string> pairs = new HashSet<string>();
            foreach (VisitDef v in d.Day.visits)
            {
                Assert.That(ids.Add(v.id), "방문 id가 겹친다: " + v.id);
                Assert.That(d.HasActor(v.actor), v.id + " 가 없는 사람을 가리킨다: " + v.actor);
                Assert.That(d.HasRoom(v.room), v.id + " 가 없는 방을 가리킨다: " + v.room);
                Assert.That(v.atMin % d.StepMin, Is.EqualTo(0),
                    v.id + " 의 시각 " + v.atMin + " 이 " + d.StepMin + "분 격자 위에 없다 — 되짚은 시각이 영영 안 맞는다");
                Assert.That(v.atMin, Is.InRange(d.Day.dayStartMin, d.Day.dayEndMin), v.id);
                Assert.That(v.strengthPercent, Is.GreaterThan(0), v.id);
                Assert.That(v.ko, Is.Not.Null.And.Not.Empty, v.id + " 에 한국어 원문이 없다");
                Assert.That(pairs.Add(v.actor + "|" + v.room), Is.True,
                    v.id + ": 같은 사람이 같은 방에 두 번 다녀갔다 — 두 층이 겹쳐 시각을 영영 못 가른다");
            }
            Assert.That(d.Day.depositUnits, Is.GreaterThan(0));
            TestContext.WriteLine("방문 " + d.Day.visits.Count + "건 · 모두 " + d.StepMin + "분 격자 위");
        }

        [Test]
        public void 그날의_동선이_걸어서_가능하다()
        {
            // 데이터가 "같은 사람이 3분 만에 집 반대편에 있었다"고 말하면 정답 자체가 거짓말이다.
            GameData d = TestWorld.Data;
            Dictionary<string, List<VisitDef>> byActor = new Dictionary<string, List<VisitDef>>();
            foreach (VisitDef v in d.Day.visits)
            {
                if (!byActor.ContainsKey(v.actor)) byActor[v.actor] = new List<VisitDef>();
                byActor[v.actor].Add(v);
            }
            foreach (KeyValuePair<string, List<VisitDef>> kv in byActor)
            {
                kv.Value.Sort(delegate (VisitDef a, VisitDef b) { return a.atMin.CompareTo(b.atMin); });
                for (int i = 1; i < kv.Value.Count; i++)
                {
                    int need = d.Walk(kv.Value[i - 1].room, kv.Value[i].room);
                    Assert.That(kv.Value[i - 1].atMin + need, Is.LessThanOrEqualTo(kv.Value[i].atMin),
                        kv.Key + " 가 " + kv.Value[i - 1].room + " 에서 " + kv.Value[i].room + " 까지 "
                        + need + "분 걸리는 길을 " + (kv.Value[i].atMin - kv.Value[i - 1].atMin) + "분에 갔다");
                }
            }
            TestContext.WriteLine("사람 " + byActor.Count + "명의 동선이 전부 걸어서 가능하다");
        }

        [Test]
        public void 조사_설정이_성립한다()
        {
            GameData d = TestWorld.Data;
            InvestigationDef iv = d.Balance.investigation;
            Assert.That(d.HasRoom(iv.startRoom), "조사를 시작하는 방이 없다: " + iv.startRoom);
            Assert.That(iv.startMin, Is.GreaterThanOrEqualTo(d.Day.dayEndMin), "그날이 끝나기 전에 조사가 시작된다");
            Assert.That(iv.endMin, Is.GreaterThan(iv.startMin));
            Assert.That(iv.sniffCostMin, Is.GreaterThan(0));
            Assert.That(iv.maxSniffs, Is.GreaterThan(d.House.rooms.Count),
                "맡을 수 있는 횟수가 방 수 이하다 — 되돌아갈 수가 없다");

            PerceptionDef p = d.Balance.perception;
            Assert.That(p.dominancePercent, Is.InRange(1, 100));
            Assert.That(p.purityPercent, Is.InRange(1, 100));
            Assert.That(p.purityPercent, Is.GreaterThan(p.dominancePercent), "되짚기가 알아보기보다 쉬우면 설계가 뒤집힌다");
            Assert.That(p.datingFloor, Is.GreaterThan(d.Notes.perceptionFloor),
                "되짚기 바닥이 지각 역치보다 낮다 — 남이 사라져도 내가 이미 죽어 창이 안 열린다");
            Assert.That(p.ratioScale, Is.GreaterThanOrEqualTo(1000));
            Assert.That(p.maxAgeSteps * d.StepMin, Is.GreaterThanOrEqualTo(iv.endMin - d.Day.dayStartMin),
                "나이 맞춤 범위가 하루보다 짧다 — 오래된 층을 맞출 수 없다");

            AmbientSpec s = d.Day.ambient;
            foreach (string n in s.notePool) Assert.That(d.HasNote(n), "잡내가 없는 계열을 쓴다: " + n);
            Assert.That(s.perRoomMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(s.perRoomMax, Is.GreaterThanOrEqualTo(s.perRoomMin));
            Assert.That(s.strengthMax, Is.GreaterThanOrEqualTo(s.strengthMin));
            Assert.That(s.atMinMin % d.StepMin, Is.EqualTo(0));
            Assert.That(s.atMinMax % d.StepMin, Is.EqualTo(0));
            TestContext.WriteLine("조사 " + TestWorld.Clock(iv.startMin) + "~" + TestWorld.Clock(iv.endMin)
                + " · 맡는 횟수 " + iv.maxSniffs + " · 한 번에 " + iv.sniffCostMin + "분");
        }

        [Test]
        public void 감쇠는_정수이고_단조롭다()
        {
            // 설계 원칙 4·5. 부동소수가 하나라도 끼면 헤드리스 재현이 깨진다.
            GameData d = TestWorld.Data;
            foreach (NoteDef n in d.Notes.notes)
            {
                int prev = 1000000;
                for (int k = 1; k <= 120; k++)
                {
                    int v = Decay.Remain(1000000, n.retainPermille, k);
                    Assert.That(v, Is.LessThanOrEqualTo(prev), n.id + " 의 감쇠가 " + k + "걸음에서 늘었다");
                    Assert.That(v, Is.GreaterThanOrEqualTo(0));
                    prev = v;
                }
                Assert.That(Decay.Remain(1000000, n.retainPermille, 0), Is.EqualTo(1000000));
            }
            // 격자 되돌리기가 정확히 뒤집히는가
            for (int at = 900; at <= 1560; at += 7)
                for (int k = 0; k <= 40; k++)
                {
                    int grid = Decay.GridTimeFromSteps(at, k, d.StepMin);
                    Assert.That(grid % d.StepMin, Is.EqualTo(0));
                    Assert.That(Decay.Steps(grid, at, d.StepMin), Is.EqualTo(k),
                        "at=" + at + " k=" + k + " 에서 나이 되돌리기가 뒤집히지 않는다");
                }
            TestContext.WriteLine("계열 " + d.Notes.notes.Count + "종의 감쇠가 정수·단조 · 나이 되돌리기가 전 구간에서 뒤집힌다");
        }
    }
}
