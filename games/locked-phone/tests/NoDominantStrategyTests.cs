using System.Collections.Generic;
using NUnit.Framework;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>
    /// 공통 검사기 — `NoDominantStrategy`.
    /// **한 가지 수를 되풀이하는 것이 최적이 아닌가.**
    ///
    /// 이 게임에서 할 수 있는 수는 "깨운다" 하나뿐이다. 그래서 되풀이의 꼴도 셋뿐이다:
    ///   ① 주운 순간부터 배터리가 다할 때까지 쉬지 않고 깨운다
    ///   ② 끝까지 아꼈다가 마지막에 몰아 깨운다
    ///   ③ 정해진 박자로 기계적으로 깨운다
    /// 셋 다 져야 "언제 깨울지 고르는 일"이 게임으로 남는다.
    /// </summary>
    [TestFixture]
    public sealed class NoDominantStrategyTests
    {
        [Test]
        public void 쉬지_않고_깨우면_아무것도_못_세운다()
        {
            Session s = TestWorld.Sess(World.Lockscreen);
            foreach (int every in new int[] { 1, 2, 5, 10 })
            {
                SessionResult r = s.BurnEarly(every);
                Assert.That(r.verdicts.Count, Is.LessThan(TestWorld.VerdictCount),
                    every + "분마다 몰아치기가 전부 세운다 — 많이 누르는 것이 답이 된다");
                TestContext.WriteLine(every + "분마다 몰아치기: 깨우기 " + r.wakes.Count + "번 → "
                    + r.verdicts.Count + "/" + TestWorld.VerdictCount + " · 마지막 "
                    + (r.wakes.Count > 0 ? TestWorld.Clock(r.wakes[r.wakes.Count - 1].atMin) : "-"));
            }
        }

        [Test]
        public void 끝까지_아꼈다_몰아_깨워도_못_세운다()
        {
            Session s = TestWorld.Sess(World.Lockscreen);
            for (int wakes = 2; wakes <= 5; wakes++)
            {
                SessionResult r = s.WaitThenBurn(5, wakes);
                Assert.That(r.verdicts.Count, Is.LessThan(TestWorld.VerdictCount),
                    "끝에 " + wakes + "번 몰아 깨우면 전부 세운다 — 아끼는 것이 답이 된다");
                TestContext.WriteLine("끝에 " + wakes + "번 몰아치기 → " + r.verdicts.Count + "/" + TestWorld.VerdictCount
                    + " · " + r.Text);
            }
        }

        [Test]
        public void 정해진_박자로_누르는_것은_거의_전부_진다()
        {
            Session s = TestWorld.Sess(World.Lockscreen);
            int all = 0, ok = 0, bestShort = 0;
            List<string> winners = new List<string>();
            for (int p = 10; p <= 140; p += 5)
                for (int off = 0; off <= 30; off += 5)
                {
                    SessionResult r = s.Metronome(p, off);
                    all++;
                    if (r.verdicts.Count == TestWorld.VerdictCount) { ok++; winners.Add(p + "분·+" + off); }
                    if (p <= 60 && r.verdicts.Count > bestShort) bestShort = r.verdicts.Count;
                }
            Assert.That(ok * 100, Is.LessThanOrEqualTo(all * TestWorld.C.metronomeSuccessMaxPercent),
                "고정 박자 " + all + "가지 중 " + ok + "가지가 전부 세운다 — 박자만 맞추면 되는 게임이다: "
                + string.Join(", ", winners));
            Assert.That(bestShort, Is.LessThan(TestWorld.VerdictCount),
                "짧은 박자(60분 이하)로도 전부 세운다");
            TestContext.WriteLine("고정 박자 " + all + "가지 중 전부 세운 것 " + ok + "가지 (" + (ok * 100 / all) + "%)"
                + " · 60분 이하 박자의 최고 성적 " + bestShort + "/" + TestWorld.VerdictCount);
        }

        [Test]
        public void 고른_일정은_이긴다()
        {
            // 위 셋이 다 지는데 아무것도 못 이기면 그냥 풀 수 없는 게임이다.
            SessionResult best = TestWorld.Best(World.Lockscreen);
            Assert.That(best.verdicts.Count, Is.EqualTo(TestWorld.VerdictCount),
                "고른 일정조차 전부 세우지 못한다 — 어려운 게 아니라 고장이다");
            TestContext.WriteLine("고른 일정: 깨우기 " + best.wakes.Count + "번 → " + best.verdicts.Count
                + "/" + TestWorld.VerdictCount + " · " + best.Text);
        }

        [Test]
        public void 예산에_여유가_거의_없다()
        {
            // "아무 때나 여러 번 누르면 된다"가 답이 되지 않으려면 예산이 실제로 빡빡해야 한다.
            GameData d = TestWorld.Data;
            SessionResult best = TestWorld.Best(World.Lockscreen);
            int used = best.wakes.Count;
            int lastNeeded = 0;
            foreach (string id in Deduction.AllNeededNotifs(d))
                if (d.Notif(id).arriveMin > lastNeeded) lastNeeded = d.Notif(id).arriveMin;

            // 마지막 증거가 도착한 뒤까지 살아 있으려면 깨우기를 몇 번까지 쓸 수 있는가
            int affordable = 0;
            for (int n = 0; n <= 12; n++)
                if (TestWorld.Screen().LastLivingMinute(n) >= lastNeeded) affordable = n + 1;

            Assert.That(affordable, Is.GreaterThanOrEqualTo(used), "필요한 만큼도 못 깨운다 — 풀 수 없다");
            Assert.That(affordable - used, Is.LessThanOrEqualTo(2),
                "마지막 증거까지 " + affordable + "번을 깨울 수 있는데 " + used + "번이면 끝난다 — 예산이 헐겁다");
            TestContext.WriteLine("마지막 증거 " + TestWorld.Clock(lastNeeded) + " 까지 살아 있으려면 깨우기는 최대 "
                + affordable + "번 · 실제로 필요한 것은 " + used + "번 (여유 " + (affordable - used) + "번)");
        }

        [Test]
        public void 같은_순간을_두_번_깨우는_것은_헛일이다()
        {
            // 화면은 1분 사이에 바뀌지 않는다. 연타가 전략이 되면 안 된다.
            GameData d = TestWorld.Data;
            Session s = TestWorld.Sess(World.Lockscreen);
            int pairs = 0, gained = 0;
            for (int t = d.Phone.foundAtMin; t <= d.Phone.lastMinuteMin; t += 17)
            {
                SessionResult one = s.Run(new List<int> { t });
                SessionResult twice = s.Run(new List<int> { t, t });
                if (one.wakes.Count == 0) break;
                pairs++;
                gained += twice.seen.Count - one.seen.Count;
            }
            Assert.That(gained, Is.EqualTo(0), "같은 순간을 두 번 깨웠더니 알림 " + gained + "장이 더 보였다");
            TestContext.WriteLine("같은 순간 연타 " + pairs + "번 시험 · 더 본 알림 0장 (배터리만 "
                + d.Phone.battery.wakeCostPermille + "‰ 씩 날아간다)");
        }
    }
}
