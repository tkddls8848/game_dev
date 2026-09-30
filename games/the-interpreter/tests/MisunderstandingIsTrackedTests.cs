using System.Collections.Generic;
using System.Text;
using Interp.Data;
using Interp.Sim;
using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// 곁들이는 검사기 — **「무엇이 오해로 남았는가」가 목록으로 나온다.**
    ///
    /// 결말이 "누가 이겼나"가 아니라 "무엇이 오해로 남았는가"로 갈린다는 것이
    /// 이 PoC의 규칙이다. 그러면 끝에 그 목록이 실제로 나와야 하고,
    /// 목록의 항목마다 **두 나라가 무엇을 다르게 믿는지**가 적혀 있어야 한다.
    /// </summary>
    [TestFixture]
    public class MisunderstandingIsTrackedTests
    {
        private GameData D { get { return TestWorld.Data; } }

        [Test]
        public void MisunderstandingIsTracked()
        {
            int nonEmpty = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 남은 오해 (씨드 " + D.Balance.seed + ") ──────────────────────────");
            foreach (IPolicy p in Policies.All())
            {
                SessionResult r = SessionSim.Run(D, D.Balance.seed, p);
                sb.AppendLine(p.Id + " — " + r.EndingId + " · 남은 " + r.Standing.Count + " · 드러난 " + r.Exposed.Count);
                if (r.Standing.Count > 0) nonEmpty++;
                foreach (string mid in r.Standing)
                {
                    MisunderstandingDef m = D.Misread(mid);
                    Assert.That(m, Is.Not.Null, "남은 오해 " + mid + " 가 데이터에 없다");
                    ClauseDef c = D.Clause(m.clauseId);
                    Assert.That(c, Is.Not.Null);
                    sb.AppendLine(Localization.Text("misread.standing",
                        "    「{0}」 — 하란: {1} / 케리아: {2}", m.word, m.aBelieves, m.bBelieves)
                        + "  (제" + c.article + "조)");
                    sb.AppendLine("      → " + m.consequence);
                }
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(nonEmpty, Is.GreaterThanOrEqualTo(3),
                "씨드 하나에서 오해가 남는 정책이 " + nonEmpty + "가지뿐이다");
        }

        [Test]
        public void FaithfulRenderingLeavesNothingMisunderstood()
        {
            foreach (int seed in D.AllSeeds())
            {
                SessionResult r = SessionSim.Run(D, seed, Policies.Exact);
                Assert.That(r.Standing, Is.Empty,
                    "씨드 " + seed + ": 늘 정확하게 옮겼는데 오해가 남았다 — " + string.Join(",", r.Standing));
                Assert.That(r.Exposed, Is.Empty, "씨드 " + seed + ": 정직한 통역에게 드러날 것이 있다");
            }
        }

        [Test]
        public void LyingLeavesAPileOfIt()
        {
            int total = 0;
            foreach (int seed in D.AllSeeds())
            {
                SessionResult r = SessionSim.Run(D, seed, Policies.False);
                total += r.Standing.Count + r.Exposed.Count;
                Assert.That(r.Standing.Count + r.Exposed.Count, Is.GreaterThanOrEqualTo(4),
                    "씨드 " + seed + ": 오역만 했는데 어긋난 것이 " + (r.Standing.Count + r.Exposed.Count) + "개뿐이다");
            }
            TestContext.Out.WriteLine("「오역만」이 남긴 어긋난 낱말 합계 " + total);
        }

        /// <summary>
        /// **완곡도 오해를 낳는다.** 이것이 없으면 "정확 아니면 거짓" 두 갈래뿐이고
        /// 후보 역어를 넷 둔 뜻이 없어진다.
        /// </summary>
        [Test]
        public void SofteningAloneAlreadyLeavesMisunderstandings()
        {
            int seeds = 0;
            foreach (int seed in D.AllSeeds())
            {
                SessionResult r = SessionSim.Run(D, seed, Policies.Soft);
                bool lied = false;
                foreach (TraceStep s in r.Trace) if (s.Register == "false") lied = true;
                Assert.That(lied, Is.False, "「완곡만」 정책이 거짓을 골랐다");
                if (r.Standing.Count + r.Exposed.Count >= 2) seeds++;
            }
            TestContext.Out.WriteLine("거짓 없이 완곡하기만 해도 오해가 둘 이상 남은 밤 " + seeds + " / " + D.AllSeeds().Count);
            Assert.That(seeds, Is.EqualTo(D.AllSeeds().Count),
                "완곡하게만 옮겨도 오해는 남아야 한다 — 그래야 '안전한 결'이 없다");
        }

        /// <summary>
        /// 심은 거짓이 **드러날 수 있어야** 한다. 영영 안 드러나면 의심이 장식이다.
        /// 그리고 조인 직전 낭독을 어떻게 하느냐로 그 확률이 실제로 갈려야 한다.
        /// </summary>
        [Test]
        public void ReadingTheTreatyAloudActuallyExposesLies()
        {
            int exposedWhenReadPlainly = 0, exposedWhenMumbled = 0;
            foreach (int seed in D.AllSeeds())
            {
                Dictionary<string, string> readAloud = new Dictionary<string, string> { { "s_reading", "g_rd_h" } };
                Dictionary<string, string> mumble = new Dictionary<string, string> { { "s_reading", "g_rd_f" } };
                exposedWhenReadPlainly += SessionSim.Run(D, seed, Policies.False, readAloud).Exposed.Count;
                exposedWhenMumbled += SessionSim.Run(D, seed, Policies.False, mumble).Exposed.Count;
            }
            TestContext.Out.WriteLine("낭독에서 되물었을 때 드러난 것 " + exposedWhenReadPlainly
                                      + " · 소리를 낮춰 넘겼을 때 " + exposedWhenMumbled);
            Assert.That(exposedWhenReadPlainly, Is.GreaterThan(exposedWhenMumbled),
                "조문을 짚어 읽으나 넘기나 드러나는 것이 같다 — 마지막 마디가 장식이다");
            Assert.That(exposedWhenReadPlainly, Is.GreaterThan(0));
        }
    }
}
