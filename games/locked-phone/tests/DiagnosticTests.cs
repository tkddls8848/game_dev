using System.Collections.Generic;
using NUnit.Framework;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>판정하지 않고 **수치를 찍는다**. 데이터를 고칠 때 보는 창이다.</summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 알림마다_화면에_남아_있던_구간을_찍는다()
        {
            GameData d = TestWorld.Data;
            Lockscreen s = TestWorld.Screen();
            List<string> need = Deduction.AllNeededNotifs(d);
            Session sess = TestWorld.Sess(World.Lockscreen);
            TestContext.WriteLine("주운 시각 " + TestWorld.Clock(d.Phone.foundAtMin)
                + " · 칸 " + d.Phone.stack.capacity + "장 · 잔량 " + (d.Phone.battery.startPermille / 10) + "%");
            TestContext.WriteLine("알림               도착     밀려남    필요   창(주운 뒤)");
            foreach (Card c in s.Cards)
            {
                int[] w = s.WindowOf(c.def.id, World.Lockscreen);
                bool req = need.Contains(c.def.id);
                List<int> cw = sess.CatchWindow(c.def.id, 2);
                string win = cw.Count == 0 ? "잡을 수 없다" : TestWorld.Clock(cw[0]) + "~" + TestWorld.Clock(cw[cw.Count - 1]);
                TestContext.WriteLine(c.def.id.PadRight(18) + TestWorld.Clock(c.def.arriveMin)
                    + "   " + (w[1] > d.Phone.lastMinuteMin ? "  끝까지" : TestWorld.Clock(w[1]))
                    + "   " + (req ? " ★  " : "    ") + "  " + win);
            }
            TestContext.WriteLine("필요한 알림 " + need.Count + "장: " + string.Join(", ", need));
        }

        [Test]
        public void 네_세계를_견준다()
        {
            GameData d = TestWorld.Data;
            World[] ws = { World.Unlocked, World.NoPushOut, World.Lockscreen, World.NoPreview };
            foreach (World w in ws)
            {
                Session s = TestWorld.Sess(w);
                SessionResult r = s.MinimalSession();
                TestContext.WriteLine(w.Name.PadRight(28) + "정답 " + r.verdicts.Count + "/" + TestWorld.VerdictCount
                    + " · 사실 " + r.facts.Count + "/" + d.Case.facts.Count
                    + " · 깨우기 " + r.wakes.Count + " (" + r.Text + ")");
            }
            // 잠긴 쪽만 — 알림을 하나도 못 본 세계
            HashSet<string> tk = Deduction.Tokens(d, new List<string>(), false, World.Unlocked);
            HashSet<string> fa = Deduction.Facts(d, tk, World.Unlocked);
            TestContext.WriteLine("잠긴 쪽만".PadRight(28) + "정답 " + Deduction.Verdicts(d, fa).Count + "/" + TestWorld.VerdictCount
                + " · 사실 " + fa.Count + "/" + d.Case.facts.Count);
        }

        [Test]
        public void 정책별_결과를_찍는다()
        {
            Session s = TestWorld.Sess(World.Lockscreen);
            SessionResult best = s.MinimalSession();
            TestContext.WriteLine("고른 일정   " + best.verdicts.Count + "/" + TestWorld.VerdictCount + "  " + best.Text);
            SessionResult burn = s.BurnEarly(5);
            TestContext.WriteLine("바로 몰아치기 " + burn.verdicts.Count + "/" + TestWorld.VerdictCount + "  " + burn.Text);
            SessionResult wait = s.WaitThenBurn(5, 3);
            TestContext.WriteLine("참았다 몰아치기 " + wait.verdicts.Count + "/" + TestWorld.VerdictCount + "  " + wait.Text);
            int ok = 0, all = 0;
            for (int p = 10; p <= 140; p += 5)
                for (int off = 0; off <= 30; off += 5)
                {
                    SessionResult r = s.Metronome(p, off);
                    all++;
                    if (r.verdicts.Count == TestWorld.VerdictCount) { ok++; TestContext.WriteLine("  고정 박자 " + p + "분·시작+" + off + " → 전부: " + r.Text); }
                }
            TestContext.WriteLine("고정 박자 " + all + "가지 중 전부 세운 것 " + ok + "가지 (" + (ok * 100 / all) + "%)");
        }

        [Test]
        public void 한_번만_깨우면_무엇이_보이는가()
        {
            GameData d = TestWorld.Data;
            World[] ws = { World.Lockscreen, World.NoPushOut };
            foreach (World w in ws)
            {
                Session s = new Session(d, TestWorld.Screen(), w);
                int bestV = -1, bestN = -1, atV = 0, atN = 0;
                List<string> need = Deduction.AllNeededNotifs(d);
                for (int t = d.Phone.foundAtMin; t <= d.Phone.lastMinuteMin; t++)
                {
                    SessionResult r = s.Run(new List<int> { t });
                    if (r.wakes.Count == 0) break;
                    int seenNeed = 0;
                    foreach (string n in need) if (r.seen.Contains(n)) seenNeed++;
                    if (r.verdicts.Count > bestV) { bestV = r.verdicts.Count; atV = t; }
                    if (seenNeed > bestN) { bestN = seenNeed; atN = t; }
                }
                TestContext.WriteLine(w.Name.PadRight(28) + "한 번 깨워 세우는 정답 최고 " + bestV + "/" + TestWorld.VerdictCount
                    + " (" + TestWorld.Clock(atV) + ") · 한 번에 보이는 필요 알림 최고 " + bestN + "/" + need.Count
                    + " (" + TestWorld.Clock(atN) + ")");
            }
        }
    }
}
