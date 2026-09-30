using System.Collections.Generic;
using System.Text;
using Interp.Data;
using Interp.Sim;
using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// ★ 핵 검사기 2 — **「안전한 낱말이 없다」**
    ///
    /// 늘 정확하게만 옮기는 정책이 **모든 결말에서** 최선은 아닌가.
    /// 최선이면 고민이 사라지고, 고민이 사라지면 이 게임에는 조작이 없다.
    /// 정책 여섯(정확만·완곡만·강경만·오역만·상황별·고용주 편)을 씨드 전부에 돌려 견준다.
    ///
    /// 자가 하나뿐이면 이 물음이 성립하지 않는다 — 그래서 자가 다섯이다.
    /// 「평화」「하란이 가져가는 것」「케리아가 지켜 내는 것」「같은 것을 읽었는가」「통역이 받은 지시」.
    /// </summary>
    [TestFixture]
    public class NoSafeWordTests
    {
        private GameData D { get { return TestWorld.Data; } }

        private Dictionary<string, Dictionary<string, int>> Table()
        {
            Dictionary<string, Dictionary<string, int>> t = new Dictionary<string, Dictionary<string, int>>();
            foreach (string oid in Objectives.AllIds(D))
            {
                Dictionary<string, int> row = new Dictionary<string, int>();
                foreach (IPolicy p in Policies.All()) row[p.Id] = Objectives.TotalOverSeeds(D, oid, p);
                t[oid] = row;
            }
            return t;
        }

        private static string ArgMax(Dictionary<string, int> row)
        {
            string best = null;
            foreach (KeyValuePair<string, int> kv in row)
                if (best == null || kv.Value > row[best]) best = kv.Key;
            return best;
        }

        [Test]
        public void NoSafeWord()
        {
            Dictionary<string, Dictionary<string, int>> t = Table();
            List<string> lost = new List<string>();
            List<string> won = new List<string>();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 정책 × 자 (씨드 " + D.AllSeeds().Count + "개 합계) ───────────────────────");
            sb.Append(string.Format("{0,-28}", "자 (누가 재는가)"));
            foreach (IPolicy p in Policies.All()) sb.Append(string.Format("{0,15}", p.Id));
            sb.AppendLine("   이긴 정책");
            foreach (string oid in Objectives.AllIds(D))
            {
                ObjectiveDef o = D.Objective(oid);
                sb.Append(string.Format("{0,-28}", oid + " (" + o.whose + ")"));
                foreach (IPolicy p in Policies.All()) sb.Append(string.Format("{0,15}", t[oid][p.Id]));
                string winner = ArgMax(t[oid]);
                sb.AppendLine("   " + winner);
                if (winner == "p_exact") won.Add(oid); else lost.Add(oid);
            }
            TestContext.Out.WriteLine(sb.ToString());
            TestContext.Out.WriteLine(Localization.Text("audit.nosafe",
                "「늘 정확하게」가 자 {1}개 가운데 {0}개에서 최선이 아니다", lost.Count, t.Count));

            Assert.That(lost.Count, Is.GreaterThanOrEqualTo(2),
                "「늘 정확하게 옮긴다」가 자 " + t.Count + "개 중 " + (t.Count - lost.Count) + "개에서 최선이다 — " +
                "안전한 낱말이 있다는 뜻이고, 그러면 고를 이유가 없다. 진 자: " + string.Join(", ", lost));

            // 반대쪽도 막는다. 정확이 어디서도 못 이기면 정직이 그냥 벌이고, 그것도 고민이 아니다.
            Assert.That(won.Count, Is.GreaterThanOrEqualTo(1),
                "「늘 정확하게 옮긴다」가 어느 자로도 이기지 못한다 — 정직이 그냥 손해면 그것도 선택이 아니다");
        }

        /// <summary>
        /// 어떤 정책도 자 전부를 쓸어서는 안 된다. 하나가 쓸면 그 정책이 정답이 된다.
        /// (공통 검사기 「NoDominantStrategy」의 정책 층이다.)
        /// </summary>
        [Test]
        public void NoPolicyWinsEveryMeasure()
        {
            Dictionary<string, Dictionary<string, int>> t = Table();
            Dictionary<string, int> wins = new Dictionary<string, int>();
            foreach (IPolicy p in Policies.All()) wins[p.Id] = 0;
            foreach (string oid in Objectives.AllIds(D)) wins[ArgMax(t[oid])]++;

            List<string> line = new List<string>();
            foreach (KeyValuePair<string, int> kv in wins) line.Add(kv.Key + " " + kv.Value);
            TestContext.Out.WriteLine("자를 이긴 횟수: " + string.Join(" · ", line));

            int max = 0, winners = 0;
            foreach (KeyValuePair<string, int> kv in wins) if (kv.Value > max) max = kv.Value;
            foreach (KeyValuePair<string, int> kv in wins) if (kv.Value > 0) winners++;

            Assert.That(max, Is.LessThan(Objectives.AllIds(D).Count),
                "한 정책이 자 전부를 쓸었다 — 정답이 하나 있다는 뜻이다");
            Assert.That(winners, Is.GreaterThanOrEqualTo(3),
                "자 다섯을 이긴 정책이 " + winners + "가지뿐이다 — 자를 더 갈라야 한다");
        }

        /// <summary>
        /// 「늘 강경하게」는 회담을 깬다. 이것까지 통과하면 안 되는 정책은 실제로 실패해야 한다 —
        /// 못 이기는 수가 하나도 없으면 결정에 무게가 없다.
        /// </summary>
        [Test]
        public void AlwaysHardBreaksTheRoom()
        {
            int collapsed = 0;
            foreach (int seed in D.AllSeeds())
                if (SessionSim.Run(D, seed, Policies.Hard).Collapsed) collapsed++;
            TestContext.Out.WriteLine("「늘 강경하게」가 깬 회담 " + collapsed + " / " + D.AllSeeds().Count);
            Assert.That(collapsed, Is.EqualTo(D.AllSeeds().Count), "늘 강경한데도 조인되는 밤이 있다");
        }

        /// <summary>
        /// 그런데 **늘 정확해도 깨지는 밤이 있어야 한다.** 정확이 안전하면 그것이 곧 안전한 낱말이다.
        /// </summary>
        [Test]
        public void EvenFaithfulRenderingBreaksSomeNights()
        {
            int collapsed = 0;
            List<int> which = new List<int>();
            foreach (int seed in D.AllSeeds())
                if (SessionSim.Run(D, seed, Policies.Exact).Collapsed) { collapsed++; which.Add(seed); }
            TestContext.Out.WriteLine("「늘 정확하게」가 깨진 밤 " + collapsed + " / " + D.AllSeeds().Count
                                      + " (씨드 " + string.Join(",", which) + ")");
            Assert.That(collapsed, Is.GreaterThan(0), "늘 정확하게만 옮기면 한 밤도 깨지지 않는다");
            Assert.That(collapsed, Is.LessThan(D.AllSeeds().Count), "늘 정확하게 옮기면 반드시 깨진다 — 그것도 답이 하나인 것이다");
        }

        /// <summary>
        /// ★ **거짓말을 한마디도 하지 않는 정책이 고용주에게 가장 좋은 조약을 만들 수 있어야 한다.**
        /// 이것이 이 PoC가 하려는 말이다 — 통역은 거짓말하지 않고도 조약을 기울인다.
        /// </summary>
        [Test]
        public void TiltingWithoutLyingBeatsFaithfulForThePaymaster()
        {
            int shade = Objectives.TotalOverSeeds(D, "o_haran", Policies.Tilt);
            int exact = Objectives.TotalOverSeeds(D, "o_haran", Policies.Exact);
            int lies = 0;
            foreach (int seed in D.AllSeeds())
                foreach (TraceStep s in SessionSim.Run(D, seed, Policies.Tilt).Trace)
                    if (s.Register == "false") lies++;
            TestContext.Out.WriteLine("고용주 편 " + shade + " vs 늘 정확 " + exact + " · 그 사이 한 거짓말 " + lies + "번");
            Assert.That(lies, Is.Zero, "「고용주 편」 정책이 거짓말을 했다 — 이 검사기의 뜻이 사라진다");
            Assert.That(shade, Is.GreaterThan(exact),
                "거짓말 없이 기울여도 고용주에게 더 좋은 조약이 나오지 않는다");
        }
    }
}
