using System.Collections.Generic;
using MapLies.Data;
using MapLies.Sim;
using NUnit.Framework;

namespace MapLies.Tests
{
    /// <summary>
    /// ★ 핵 검사기 하나 — **도시가 N일 안에 지도 상태에 반드시 닿는가(진동하지 않는가).**
    ///
    /// 수렴하지 않으면 플레이어가 무엇을 한 것인지 알 수 없다. 오늘 길인 칸이 모레 건물이고
    /// 다시 글피에 길이면 지도를 고친 결과가 없는 것과 같다.
    ///
    /// **두 세계 비교로 판정한다.**
    ///   · 켠 세계: 한 필지를 maxFlipsPerCell 번까지만 다시 짓는다 (실제 설계) → 닿는다
    ///   · 끈 세계: 상한을 없앤다 (-1)                                     → 영원히 진동한다
    /// 끈 세계가 진동하지 않으면 상한이 장식이라는 뜻이고, 이 검사기는 아무것도 재지 않은 것이다.
    /// </summary>
    [TestFixture]
    public sealed class CityConvergesTests
    {
        private static GameData D { get { return TestWorld.Data; } }

        [Test]
        public void 아무_지도로도_반드시_닿는다()
        {
            List<string> bad = new List<string>();
            foreach (string cid in TestWorld.ChapterIds())
                foreach (string p in Policies.Names())
                    for (int t = 0; t < D.Balance.trialSeeds; t++)
                    {
                        ChapterResult r = TestWorld.Run(cid, p, t);
                        if (!r.Converged) bad.Add(cid + "/" + p + "/씨드" + t + " (" + r.Days + "일)");
                    }
            Assert.That(bad, Is.Empty, "닿지 못한 회차가 있다: " + string.Join(" ", bad));
        }

        [Test]
        public void 닿은_날이_한도_안이다()
        {
            int worst = 0;
            string worstWho = "";
            foreach (string cid in TestWorld.ChapterIds())
                foreach (string p in Policies.Names())
                    for (int t = 0; t < D.Balance.trialSeeds; t++)
                    {
                        ChapterResult r = TestWorld.Run(cid, p, t);
                        if (r.Days > worst) { worst = r.Days; worstWho = cid + "/" + p; }
                    }
            TestContext.WriteLine("가장 오래 걸린 회차 " + worst + "일 (" + worstWho
                                  + ") · 한도 " + D.Balance.convergeDays);
            Assert.That(worst, Is.LessThanOrEqualTo(D.Balance.convergeDays));
        }

        [Test]
        public void 닿은_뒤에는_한_칸도_움직이지_않는다()
        {
            foreach (string cid in TestWorld.ChapterIds())
            {
                MapPlan plan = new MapPlan(D);
                Policies.DrawFor(D, D.Chapter(cid), plan);
                CitySim sim = new CitySim(D, plan, D.Chapter(cid).seed);
                Assert.That(sim.RunToFixedPoint(), Is.True, cid + ": 닿지 못했다");

                string frozen = string.Join("/", sim.Actual.Rows());
                sim.Advance(D.Balance.quietDays * 3);
                Assert.That(string.Join("/", sim.Actual.Rows()), Is.EqualTo(frozen),
                            cid + ": 닿았다고 한 뒤에 도시가 다시 움직였다 — quietDays 가 너무 짧다");
                Assert.That(sim.DetectCyclePeriod(D.Balance.quietDays * 3), Is.Zero, cid + ": 주기가 잡혔다");
            }
        }

        /// <summary>
        /// **어긋남이 줄기만 하는 것이 아니다.** 도시가 지도에 못 맞추고 거부하는 칸이 남는다 —
        /// 그것이 게임의 말이고, 수렴이 「전부 맞춰졌다」가 아니라 「멈췄다」임을 못 박는다.
        /// </summary>
        /// <summary>
        /// **필지가 닳으면 도시가 지도를 거부한다.** 그것이 수렴의 값이고 동시에 게임의 말이다 —
        /// 지도는 그 칸에 대해 영원히 거짓말이 된다.
        ///
        /// 두 번 고치는 것으로 보인다: 한 번 그으면 도시가 짓고, 같은 칸을 다시 그으면 듣지 않는다.
        /// (한 번 틀렸다: 「장을 풀어 놓고 상한을 1로」 두는 방식은 거부를 만들지 못했다.
        ///  장이 요구하는 획은 대부분 한 번만 뒤집으면 되기 때문이다.)
        /// </summary>
        [Test]
        public void 거부하는_칸이_실제로_생긴다()
        {
            SimOptions once = SimOptions.From(D);
            once.MaxFlipsPerCell = 1;

            MapPlan plan = new MapPlan(D);
            Assert.That(plan.Draw(0, 7, 4, Kinds.Block, 1000), Is.True);
            CitySim sim = new CitySim(D, plan, D.Balance.seed, once);
            sim.RunToFixedPoint();
            Assert.That(sim.Actual.At(7, 4), Is.EqualTo(Kinds.Block), "도시가 처음 그은 것을 짓지 않았다");
            Assert.That(sim.RefusedCells(), Is.Empty, "처음 그은 것만으로 거부가 생겼다");

            // 같은 칸을 다시 그린다. 필지를 이미 한 번 썼으므로 도시가 듣지 않는다
            Assert.That(sim.Plan.Draw(sim.Day, 7, 4, Kinds.Street, 1000), Is.True);
            sim.Advance(D.Balance.quietDays * 4);

            List<int[]> refused = sim.RefusedCells();
            TestContext.WriteLine("필지를 한 번만 쓰게 한 세계에서 거부된 칸 " + refused.Count);
            foreach (int[] c in refused)
                TestContext.WriteLine("    (" + c[0] + "," + c[1] + ") 지도는 "
                                      + Kinds.Name(sim.Plan.At(c[0], c[1])) + ", 도시는 "
                                      + Kinds.Name(sim.Actual.At(c[0], c[1])) + " — 지도가 거짓말이 됐다");
            Assert.That(refused.Count, Is.GreaterThan(0),
                        "필지를 다 쓴 칸을 다시 그렸는데도 거부가 없다 — 상한이 판정에 들어가지 않는다");
            Assert.That(sim.Actual.At(7, 4), Is.EqualTo(Kinds.Block), "필지를 다 썼는데 또 뒤집혔다");
            Assert.That(sim.FlipsAt(7, 4), Is.EqualTo(1));
        }

        /// <summary>보통 세계에서는 시의회의 요구를 골라 그리는 동안 거부가 생기지 않아야 한다.</summary>
        [Test]
        public void 골라_그리면_거부가_생기지_않는다()
        {
            foreach (string cid in TestWorld.ChapterIds())
                for (int t = 0; t < D.Balance.trialSeeds; t++)
                    Assert.That(TestWorld.Run(cid, "Targeted", t).Refused, Is.Zero,
                                cid + "/씨드" + t + ": 골라 그렸는데 도시가 거부한 칸이 있다");
        }

        // ── 음성 대조군 ──────────────────────────────────────────────────────

        /// <summary>
        /// **상한을 없애면 영원히 진동해야 한다.** 진동하지 않으면 「닿는다」가
        /// 규칙 때문인지 그냥 데이터가 순해서인지 가릴 수 없다.
        /// </summary>
        [Test]
        public void 대조군_필지_상한을_없애면_진동한다()
        {
            SimOptions free = SimOptions.From(D);
            free.MaxFlipsPerCell = -1;

            CitySim sim = new CitySim(D, new MapPlan(D), D.Balance.seed, free);
            sim.RunToFixedPoint();
            // 여기서는 **연대기 전체**를 본다. 멈추지 않는 도시에는 「멈춘 뒤」가 없으므로
            // 짧은 끝자락만 보면 주기(27일)보다 창이 좁아 놓친다. 멈추는 도시에만 끝자락을 쓴다.
            int period = sim.DetectCyclePeriod(-1);
            TestContext.WriteLine("상한 없음: 닿았나 " + sim.Converged + " · 주기 " + period
                                  + " · " + sim.Day + "일 돌렸다");

            Assert.That(sim.Converged, Is.False,
                        "필지 상한을 없앴는데도 도시가 멈췄다 — 수렴이 그 규칙에서 오는 것이 아니다");
            Assert.That(period, Is.GreaterThanOrEqualTo(2),
                        "멈추지도 않고 주기도 잡히지 않는다 — 무엇이 일어나는지 이름을 붙일 수 없다");
        }

        [Test]
        public void 대조군_같은_지도라도_상한이_있으면_멈춘다()
        {
            CitySim capped = new CitySim(D, new MapPlan(D), D.Balance.seed);
            Assert.That(capped.RunToFixedPoint(), Is.True, "상한이 있는데 멈추지 않았다");
            TestContext.WriteLine("상한 " + D.Balance.maxFlipsPerCell + ": " + capped.ConvergedOnDay + "일에 멈췄다");
            Assert.That(capped.DetectCyclePeriod(D.Balance.quietDays * 3), Is.Zero);
        }

        /// <summary>
        /// 진동이 **무른 구역에서 온다**는 것을 이름으로 확인한다. 엉뚱한 곳에서 오는 것이면
        /// 데이터 설명이 거짓이 된다.
        /// </summary>
        [Test]
        public void 진동이_무른_구역에서_온다()
        {
            SimOptions free = SimOptions.From(D);
            free.MaxFlipsPerCell = -1;
            CitySim sim = new CitySim(D, new MapPlan(D), D.Balance.seed, free);
            sim.RunToFixedPoint();

            HashSet<int> soft = new HashSet<int>();
            foreach (CellRef c in D.District("d-soft").cells) soft.Add(sim.Actual.Index(c.x, c.y));

            List<string> moving = new List<string>();
            string[] first = sim.Chronicle[0].ActualRows;
            string[] last = sim.Chronicle[sim.Chronicle.Count - 1].ActualRows;
            for (int y = 0; y < D.City.height; y++)
                for (int x = 0; x < D.City.width; x++)
                {
                    if (first[y][x] == last[y][x]) continue;
                    if (!soft.Contains(sim.Actual.Index(x, y))) moving.Add("(" + x + "," + y + ")");
                }
            TestContext.WriteLine("무른 구역 밖에서 바뀐 칸: " + (moving.Count == 0 ? "없다" : string.Join(" ", moving)));
            Assert.That(moving, Is.Empty,
                        "지도가 그린 칸이 움직였다 — 도시가 지도를 따르는 것이 아니라 제멋대로 하고 있다");
        }
    }
}
