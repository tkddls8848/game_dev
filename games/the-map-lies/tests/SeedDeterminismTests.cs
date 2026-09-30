using System.Collections.Generic;
using System.Text;
using MapLies.Data;
using MapLies.Sim;
using NUnit.Framework;

namespace MapLies.Tests
{
    /// <summary>
    /// 공통 검사기 `SeedDeterminism` — **나머지 전부의 전제다.**
    ///
    /// 같은 씨드가 같은 결과를 내지 않으면 수렴 판정도, 되돌리기 탐색도, 점수 비교도 뜻을 잃는다.
    ///
    /// 뿌리 CLAUDE.md 설계 원칙 5: 난수는 System.Random 하나뿐이고 씨드는 데이터에서 온다.
    /// 압력·값·해가 전부 정수이므로(원칙 4) 재현에 부동소수 오차가 끼어들 자리가 없다.
    /// </summary>
    [TestFixture]
    public sealed class SeedDeterminismTests
    {
        private static GameData D { get { return TestWorld.Data; } }

        [Test]
        public void 같은_씨드가_같은_연대기를_만든다()
        {
            foreach (string cid in TestWorld.ChapterIds())
                for (int t = 0; t < 5; t++)
                {
                    string a = Fingerprint(cid, t);
                    string b = Fingerprint(cid, t);
                    Assert.That(b, Is.EqualTo(a), cid + "/씨드" + t + ": 같은 씨드가 다른 연대기를 만들었다");
                }
        }

        /// <summary>
        /// 씨드가 아무것도 바꾸지 않으면 씨드 스무 개를 도는 것이 한 번 도는 것과 같다.
        ///
        /// **어디서 봐야 하는지가 핵이다.** 골라 그려서 잘 푼 회차는 씨드를 바꿔도 결과가 같다 —
        /// 아무도 갇히지 않으니 사람이 어디 서 있었는지가 결과에 남지 않기 때문이다. 그것이 설계고,
        /// 씨드는 **사고가 났을 때** 값을 갖는다: 배씨가 그날 안마당 안에 있었는지 큰길에 있었는지.
        /// (한 번 틀렸다: 잘 푼 회차의 지문으로 걸었더니 세 장에서 세 가지만 나왔다.)
        /// </summary>
        [Test]
        public void 다른_씨드는_사고_때_실제로_다른_결과를_만든다()
        {
            HashSet<string> clean = new HashSet<string>();
            foreach (string cid in TestWorld.ChapterIds())
                for (int t = 0; t < D.Balance.trialSeeds; t++) clean.Add(Fingerprint(cid, t));

            HashSet<string> broken = new HashSet<string>();
            foreach (string mid in TestWorld.MistakeIds())
                for (int t = 0; t < TestWorld.RescueSeeds; t++)
                {
                    RescueResult r = TestWorld.Rescue(mid, t);
                    broken.Add(mid + "|" + string.Join(",", r.TrappedAfterMistake) + "|" + r.HarmPaid);
                }

            TestContext.WriteLine("잘 푼 회차: 장 " + TestWorld.ChapterIds().Count + " x 씨드 "
                                  + D.Balance.trialSeeds + " → 서로 다른 결과 " + clean.Count + "가지 (씨드가 결과에 안 남는다)");
            TestContext.WriteLine("사고 회차: 사고 " + TestWorld.MistakeIds().Count + " x 씨드 "
                                  + TestWorld.RescueSeeds + " → 서로 다른 결과 " + broken.Count + "가지");
            Assert.That(broken.Count, Is.GreaterThan(TestWorld.MistakeIds().Count),
                        "사고가 나도 씨드가 결과를 바꾸지 않는다 — 씨드가 아무것도 안 한다");
        }

        /// <summary>
        /// 사람이 실제로 걷는가. 걷지 않으면 위의 씨드 민감도가 어디서 오는지 설명되지 않는다.
        /// </summary>
        [Test]
        public void 사람이_실제로_걷고_반경을_넘지_않는다()
        {
            MapPlan plan = new MapPlan(D);
            Policies.DrawFor(D, D.Chapter("ch-both"), plan);
            CitySim sim = new CitySim(D, plan, D.Balance.seed);
            Dictionary<string, HashSet<string>> seen = new Dictionary<string, HashSet<string>>();
            foreach (CitizenDef c in D.AllCitizens) seen[c.id] = new HashSet<string>();

            for (int i = 0; i < 40; i++)
            {
                sim.Step();
                foreach (CitizenDef c in D.AllCitizens)
                {
                    int[] p = sim.PositionOf(c.id);
                    seen[c.id].Add(p[0] + "," + p[1]);
                    if (sim.IsEntombed(c.id)) continue;
                    int dist = System.Math.Abs(p[0] - c.x) + System.Math.Abs(p[1] - c.y);
                    Assert.That(dist, Is.LessThanOrEqualTo(System.Math.Max(1, c.walkRadius)),
                                c.id + " 가 반경 " + c.walkRadius + " 를 넘어 " + dist + "칸 갔다");
                }
            }
            int moved = 0;
            foreach (CitizenDef c in D.AllCitizens)
            {
                TestContext.WriteLine("    " + c.id + " 반경 " + c.walkRadius + " → 다녀 본 칸 " + seen[c.id].Count);
                if (seen[c.id].Count > 1) moved++;
            }
            Assert.That(moved, Is.GreaterThan(0), "아무도 걷지 않았다 — 씨드가 쓰이지 않는다");
        }

        [Test]
        public void 도시가_움직이는_것은_씨드와_무관하다()
        {
            // 사람의 걸음만 씨드에 달려 있어야 한다. 도시가 뒤집히는 순서가 씨드를 타면
            // 「지도를 고치면 도시가 따라온다」가 운이 된다.
            string first = null;
            for (int t = 0; t < 8; t++)
            {
                MapPlan plan = new MapPlan(D);
                Policies.DrawFor(D, D.Chapter("ch-both"), plan);
                CitySim sim = new CitySim(D, plan, unchecked(D.Balance.seed + t * 7919));
                sim.RunToFixedPoint();
                string rows = string.Join("/", sim.Actual.Rows());
                if (first == null) first = rows;
                else Assert.That(rows, Is.EqualTo(first),
                                 "씨드 " + t + ": 같은 지도인데 도시의 모습이 달라졌다");
            }
        }

        [Test]
        public void 갈라_낸_세계가_같은_길을_간다()
        {
            // RescueSearch 가 Fork 로 세계를 갈라 쓴다. 갈라 낸 뒤의 걸음이 원본과 같아야
            // 탐색이 찾은 답이 실제로 돌려 봤을 때도 같은 답이다.
            MapPlan plan = new MapPlan(D);
            Policies.DrawFor(D, D.Chapter("ch-soft"), plan);
            CitySim a = new CitySim(D, plan, D.Balance.seed);
            a.Advance(8);
            CitySim b = a.Fork();
            a.Advance(6);
            b.Advance(6);
            Assert.That(string.Join("/", b.Actual.Rows()), Is.EqualTo(string.Join("/", a.Actual.Rows())),
                        "갈라 낸 세계가 다른 도시가 됐다");
            foreach (CitizenDef c in D.AllCitizens)
            {
                Assert.That(b.PositionOf(c.id), Is.EqualTo(a.PositionOf(c.id)),
                            "갈라 낸 세계에서 " + c.id + " 가 다른 곳에 있다 — 난수 상태를 못 이었다");
                Assert.That(b.HarmOf(c.id), Is.EqualTo(a.HarmOf(c.id)));
            }
        }

        [Test]
        public void 되돌리기_탐색이_두_번_같은_답을_낸다()
        {
            foreach (MistakeDef mk in D.AllMistakes)
            {
                RescueResult one = RescueSearch.Find(D, mk, mk.seed);
                RescueResult two = RescueSearch.Find(D, mk, mk.seed);
                Assert.That(two.Rescued, Is.EqualTo(one.Rescued), mk.id);
                Assert.That(Join(two.RescueStrokes), Is.EqualTo(Join(one.RescueStrokes)),
                            mk.id + ": 같은 씨드에서 탐색이 다른 답을 냈다");
                Assert.That(two.RescueDays, Is.EqualTo(one.RescueDays));
                Assert.That(two.HarmPaid, Is.EqualTo(one.HarmPaid));
            }
        }

        [Test]
        public void 정수만_쓴다()
        {
            // 압력·값·해가 정수인지는 형이 보장한다. 여기서는 **누적이 어긋나지 않는지**를 본다:
            // 하루하루 더한 해의 합이 전체 해와 같아야 한다.
            MapPlan plan = new MapPlan(D);
            CitySim sim = new CitySim(D, plan, D.Balance.seed);
            sim.Advance(30);
            int daily = 0;
            foreach (DayRecord r in sim.Chronicle) daily += r.HarmToday;
            Assert.That(daily, Is.EqualTo(sim.TotalHarm),
                        "하루하루 쌓은 해의 합(" + daily + ")이 전체(" + sim.TotalHarm + ")와 다르다");
        }

        private static string Fingerprint(string chapterId, int trial)
        {
            ChapterResult r = Policies.Run(D, chapterId, "Targeted", trial);
            StringBuilder sb = new StringBuilder();
            sb.Append(r.Days).Append('|').Append(r.Harm).Append('|').Append(r.PermitSpent)
              .Append('|').Append(r.Dwellings).Append('|').Append(r.PlazaSize)
              .Append('|').Append(r.Score).Append('|').Append(string.Join(",", r.TrappedAtEnd));
            return sb.ToString();
        }

        private static string Join(IList<Stroke> strokes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Stroke s in strokes) sb.Append(s.X).Append(',').Append(s.Y).Append(s.Kind).Append(';');
            return sb.ToString();
        }
    }
}
