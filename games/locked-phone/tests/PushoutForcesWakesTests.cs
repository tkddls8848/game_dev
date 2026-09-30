using System.Collections.Generic;
using NUnit.Framework;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>
    /// ★ 핵 검사기 셋 — `PushoutForcesWakes`.
    /// **알림이 밀려 사라지는 것이 실제로 게임을 바꾸는가.**
    ///
    /// 앞의 두 검사기는 "잠금 화면만으로 풀린다"와 "한 장으로는 안 풀린다"를 본다.
    /// 그런데 밀려남이 없으면 **끝까지 기다렸다가 한 번 깨우면 전부 보인다** — 그러면 이 게임에
    /// 시간이 없다. 이 검사기가 그 자리를 막는다. 두 세계를 같은 씨드·같은 데이터로 돌린다:
    ///
    ///   밀려남 x — 알림이 전부 남는다
    ///   밀려남 o — 칸 여덟 장. 넘치면 오래된 것부터 사라지고 잠금을 풀 수 없으니 영영 못 본다
    /// </summary>
    [TestFixture]
    public sealed class PushoutForcesWakesTests
    {
        [Test]
        public void 밀려남이_없으면_한두_번으로_끝난다()
        {
            SessionResult off = TestWorld.Sess(World.NoPushOut).MinimalSession();
            Assert.That(off.verdicts.Count, Is.EqualTo(TestWorld.VerdictCount),
                "대조군에서조차 다 세우지 못한다 — 비교의 바닥이 무너졌다");
            Assert.That(off.wakes.Count, Is.LessThanOrEqualTo(2),
                "밀려남이 없는데도 " + off.wakes.Count + "번을 깨워야 한다 — 비교가 성립하지 않는다");
            TestContext.WriteLine("밀려남x: 깨우기 " + off.wakes.Count + "번으로 " + off.verdicts.Count
                + "/" + TestWorld.VerdictCount + " · " + off.Text);
            TestContext.WriteLine("    (두 번인 이유는 하나뿐이다 — 잔량 감소율을 재려면 벌려서 두 번 읽어야 한다)");
        }

        [Test]
        public void 밀려나면_훨씬_더_깨워야_한다()
        {
            SessionResult on = TestWorld.Best(World.Lockscreen);
            SessionResult off = TestWorld.Sess(World.NoPushOut).MinimalSession();
            Assert.That(on.verdicts.Count, Is.EqualTo(TestWorld.VerdictCount),
                "밀려남이 있으면 아예 못 푼다 — 그건 어려운 게 아니라 고장이다: " + on.Text);
            Assert.That(on.wakes.Count, Is.GreaterThanOrEqualTo(TestWorld.C.minWakesOn),
                "깨우기 " + on.wakes.Count + "번이면 끝난다 — 밀려남이 장식이다");
            Assert.That(on.wakes.Count - off.wakes.Count, Is.GreaterThanOrEqualTo(TestWorld.C.wakeGapMin),
                "밀려남이 늘린 깨우기가 " + (on.wakes.Count - off.wakes.Count) + "번뿐이다");
            TestContext.WriteLine("밀려남o: 깨우기 " + on.wakes.Count + "번 · " + on.Text);
            TestContext.WriteLine("밀려남x: 깨우기 " + off.wakes.Count + "번 · " + off.Text);
        }

        [Test]
        public void 한_번_깨워서는_밀려남이_있을_때_아무것도_못_세운다()
        {
            // ★ 이 검사기의 핵심 수치. 밀려남이 없으면 한 번으로 거의 다 되는데, 있으면 0이다.
            GameData d = TestWorld.Data;
            int bestOn = BestSingleWake(World.Lockscreen);
            int bestOff = BestSingleWake(World.NoPushOut);
            Assert.That(bestOn, Is.LessThanOrEqualTo(TestWorld.C.oneWakeVerdictsMax),
                "한 번만 깨워도 정답 " + bestOn + "개가 선다");
            Assert.That(bestOff, Is.GreaterThan(bestOn),
                "밀려남을 꺼도 한 번 깨우기가 나아지지 않는다 — 밀려남이 원인이 아니다");
            TestContext.WriteLine("한 번 깨워 세우는 정답: 밀려남o " + bestOn + "/" + TestWorld.VerdictCount
                + " · 밀려남x " + bestOff + "/" + TestWorld.VerdictCount);
        }

        [Test]
        public void 한_번에_화면에_보이는_필요_알림_수를_센다()
        {
            GameData d = TestWorld.Data;
            List<string> need = Deduction.AllNeededNotifs(d);
            int on = BestSingleSeen(World.Lockscreen, need);
            int off = BestSingleSeen(World.NoPushOut, need);
            Assert.That(on, Is.LessThan(need.Count),
                "한 번에 필요한 알림이 전부 보인다 — 밀려남이 아무것도 하지 않는다");
            Assert.That(off, Is.EqualTo(need.Count), "대조군에서는 전부 보여야 한다");
            TestContext.WriteLine("한 번에 보이는 필요 알림: 밀려남o 최고 " + on + "/" + need.Count
                + " · 밀려남x " + off + "/" + need.Count);
        }

        [Test]
        public void 소식이_퍼지는_순간_화면이_통째로_쓸린다()
        {
            // 데이터가 의도한 잔인한 자리. 문자가 칸 수만큼 몰려 들어오면 그 앞의 모든 것이 사라진다.
            GameData d = TestWorld.Data;
            Lockscreen s = TestWorld.Screen();
            int cap = d.Phone.stack.capacity;
            int worstDrop = 0, at = 0;
            for (int t = d.Phone.foundAtMin; t < d.Phone.lastMinuteMin - 10; t++)
            {
                int a = s.Visible(t, World.Lockscreen).Count;
                int survivors = 0;
                List<Card> before = s.Visible(t, World.Lockscreen);
                HashSet<string> later = new HashSet<string>();
                foreach (Card c in s.Visible(t + 10, World.Lockscreen)) later.Add(c.def.id);
                foreach (Card c in before) if (later.Contains(c.def.id)) survivors++;
                int drop = a - survivors;
                if (drop > worstDrop) { worstDrop = drop; at = t; }
            }
            Assert.That(worstDrop, Is.GreaterThanOrEqualTo(cap / 2),
                "10분 안에 가장 많이 쓸려 나간 것이 " + worstDrop + "장뿐이다 — 밀려남이 밋밋하다");
            TestContext.WriteLine(TestWorld.Clock(at) + "부터 10분 사이에 화면에서 " + worstDrop
                + "장이 쓸려 나간다 (칸 " + cap + "장 중)");
        }

        [Test]
        public void 늦게_오는_증거가_실제로_있다()
        {
            // 밀려남만으로는 "일찍 봐라"가 답이 된다. 늦게 도착하는 증거가 있어야 결정이 생긴다.
            GameData d = TestWorld.Data;
            List<string> need = Deduction.AllNeededNotifs(d);
            int late = 0, early = 0;
            foreach (string id in need)
            {
                NotifDef n = d.Notif(id);
                if (n.arriveMin > d.Phone.foundAtMin) late++; else early++;
            }
            Assert.That(late, Is.GreaterThan(0), "필요한 증거가 전부 주울 때 이미 와 있다 — 기다릴 이유가 없다");
            Assert.That(early, Is.GreaterThan(0), "필요한 증거가 전부 나중에 온다 — 서두를 이유가 없다");
            NotifDef last = null;
            foreach (string id in need) if (last == null || d.Notif(id).arriveMin > last.arriveMin) last = d.Notif(id);
            TestContext.WriteLine("필요한 알림 " + need.Count + "장 중 주운 뒤에 도착하는 것 " + late
                + "장 · 가장 늦은 것은 " + last.id + " (" + TestWorld.Clock(last.arriveMin) + ")");
        }

        private static int BestSingleWake(World w)
        {
            GameData d = TestWorld.Data;
            Session s = new Session(d, TestWorld.Screen(), w);
            int best = 0;
            for (int t = d.Phone.foundAtMin; t <= d.Phone.lastMinuteMin; t++)
            {
                SessionResult r = s.Run(new List<int> { t });
                if (r.wakes.Count == 0) break;
                if (r.verdicts.Count > best) best = r.verdicts.Count;
            }
            return best;
        }

        private static int BestSingleSeen(World w, List<string> need)
        {
            GameData d = TestWorld.Data;
            Session s = new Session(d, TestWorld.Screen(), w);
            int best = 0;
            for (int t = d.Phone.foundAtMin; t <= d.Phone.lastMinuteMin; t++)
            {
                SessionResult r = s.Run(new List<int> { t });
                if (r.wakes.Count == 0) break;
                int c = 0;
                foreach (string n in need) if (r.seen.Contains(n)) c++;
                if (c > best) best = c;
            }
            return best;
        }
    }
}
