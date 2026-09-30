using System.Collections.Generic;
using MapLies.Data;
using MapLies.Sim;
using NUnit.Framework;

namespace MapLies.Tests
{
    /// <summary>
    /// 공통 검사기 `NoDominantStrategy` — **한 가지 수를 반복하는 것이 최적이 아닌가.**
    ///
    /// 이 PoC에서 「한 가지 수」는 **한 종류로 덮는 것**이다. 전부 길로, 전부 건물로, 전부 광장으로,
    /// 또는 아무것도 그리지 않기. 목표를 넷 겹쳐 둔 이유가 여기 있다 —
    /// 하나만 두면 그 하나를 채우는 덮기가 늘 답이 된다.
    /// </summary>
    [TestFixture]
    public sealed class NoDominantStrategyTests
    {
        private static GameData D { get { return TestWorld.Data; } }

        private static readonly string[] Blanket =
        {
            "PaveEverything", "BlockEverything", "PlazaEverything", "NoEdit"
        };

        [Test]
        public void 골라_그리는_쪽이_모든_장에서_낫다()
        {
            foreach (string cid in TestWorld.ChapterIds())
            {
                int targeted = TestWorld.MeanScore(cid, "Targeted");
                TestContext.WriteLine(cid + " — Targeted " + targeted);
                foreach (string p in Blanket)
                {
                    int rate = TestWorld.MeanScore(cid, p);
                    TestContext.WriteLine("    " + p + " " + rate);
                    Assert.That(targeted, Is.GreaterThan(rate),
                                cid + ": " + p + " 가 골라 그리는 것과 같거나 낫다 (" + rate + " vs " + targeted + ")");
                }
            }
        }

        [Test]
        public void 한_종류로_덮는_것은_어느_장도_통과하지_못한다()
        {
            foreach (string p in Blanket)
                foreach (string cid in TestWorld.ChapterIds())
                {
                    int met = TestWorld.GoalMetCount(cid, p);
                    Assert.That(met, Is.Zero,
                                p + " 가 " + cid + " 를 " + met + "/" + D.Balance.trialSeeds + " 통과했다 — 덮기가 답이 되는 장이다");
                }
        }

        [Test]
        public void 골라_그리는_쪽은_모든_장을_모든_씨드에서_통과한다()
        {
            foreach (string cid in TestWorld.ChapterIds())
                Assert.That(TestWorld.GoalMetCount(cid, "Targeted"), Is.EqualTo(D.Balance.trialSeeds),
                            cid + ": 골라 그려도 통과하지 못하는 씨드가 있다 — 풀 수 없는 장이다");
        }

        /// <summary>
        /// **덮기가 무너지는 이유가 서로 달라야 한다.** 넷이 같은 이유로 무너지면
        /// 목표를 넷 겹친 것이 하나만 겹친 것과 같다.
        /// </summary>
        [Test]
        public void 덮기마다_무너지는_이유가_다르다()
        {
            Dictionary<string, HashSet<string>> why = new Dictionary<string, HashSet<string>>();
            foreach (string p in Blanket)
            {
                why[p] = new HashSet<string>();
                foreach (string cid in TestWorld.ChapterIds())
                {
                    ChapterResult r = TestWorld.Run(cid, p, 0);
                    if (!r.ReachOk) why[p].Add("닿지 않는다");
                    if (!r.DwellingOk) why[p].Add("집이 준다");
                    if (!r.PlazaOk) why[p].Add("광장이 없다");
                    if (!r.NoTrappedOk) why[p].Add("사람이 갇힌다");
                }
                TestContext.WriteLine(p + ": " + string.Join(" · ", why[p]));
                Assert.That(why[p], Is.Not.Empty, p + " 가 무너진 이유를 못 찾았다");
            }

            HashSet<string> all = new HashSet<string>();
            foreach (string p in Blanket) foreach (string w in why[p]) all.Add(w);
            Assert.That(all.Count, Is.GreaterThanOrEqualTo(3),
                        "덮기들이 무너지는 이유가 " + all.Count + "가지뿐이다: " + string.Join(" ", all));

            // 「사람이 갇힌다」로 무너지는 덮기가 반드시 있어야 한다 — 그것이 이 게임의 값이다
            bool trapsSomeone = false;
            foreach (string p in Blanket) if (why[p].Contains("사람이 갇힌다")) trapsSomeone = true;
            Assert.That(trapsSomeone, Is.True, "어떤 덮기도 사람을 갇히게 하지 않는다 — 갇힘이 벌이 아니다");
        }

        /// <summary>
        /// **아무것도 그리지 않는 것도 선택이다.** 지도를 그대로 두면 도시가 제 짐작으로 메우고,
        /// 그 짐작이 사람을 건물 사이에 남긴다. 「가만히 있기」가 안전하면 이 컨셉이 죽는다.
        /// </summary>
        [Test]
        public void 아무것도_그리지_않는_것이_안전하지_않다()
        {
            CitySim sim = new CitySim(D, new MapPlan(D), D.Balance.seed);
            sim.RunToFixedPoint();

            CityGrid before = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            int changed = 0;
            for (int y = 0; y < D.City.height; y++)
                for (int x = 0; x < D.City.width; x++)
                    if (before.At(x, y) != sim.Actual.At(x, y)) changed++;

            TestContext.WriteLine("지도를 그대로 뒀는데 도시가 " + changed + "칸을 스스로 바꿨다");
            Assert.That(changed, Is.GreaterThan(0),
                        "지도를 그대로 뒀을 때 도시가 한 칸도 움직이지 않는다 — 미지정 칸이 뜻을 잃었다");

            // 그리고 그 짐작이 시의회의 요구를 깬다
            foreach (string cid in TestWorld.ChapterIds())
                Assert.That(TestWorld.GoalMetCount(cid, "NoEdit"), Is.Zero,
                            cid + ": 아무것도 그리지 않아도 통과한다");
        }

        /// <summary>
        /// 장마다 **골라 그린 획이 달라야 한다.** 같은 획이 세 장에 다 통하면 장이 셋이 아니라 하나다.
        /// </summary>
        [Test]
        public void 장마다_그어야_하는_획이_다르다()
        {
            Dictionary<string, string> fingerprints = new Dictionary<string, string>();
            foreach (ChapterDef ch in D.AllChapters)
            {
                MapPlan plan = new MapPlan(D);
                Policies.DrawFor(D, ch, plan);
                List<string> keys = new List<string>();
                foreach (Stroke s in plan.Strokes) keys.Add(s.X + "," + s.Y + s.Kind);
                keys.Sort();
                fingerprints[ch.id] = string.Join(" ", keys);
                TestContext.WriteLine(ch.id + " (" + plan.Strokes.Count + "획 · " + plan.PermitSpent + "원): "
                                      + fingerprints[ch.id]);
                Assert.That(plan.Strokes.Count, Is.GreaterThan(0), ch.id + ": 그을 것이 없다");
            }
            HashSet<string> distinct = new HashSet<string>(fingerprints.Values);
            Assert.That(distinct.Count, Is.EqualTo(D.AllChapters.Count),
                        "서로 다른 장에서 같은 획을 그으면 통과한다 — 장이 하나뿐인 것과 같다");
        }
    }
}
