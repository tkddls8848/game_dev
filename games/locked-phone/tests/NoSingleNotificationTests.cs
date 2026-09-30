using System.Collections.Generic;
using NUnit.Framework;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>
    /// ★ 핵 검사기 둘 — `NoSingleNotificationGivesItAway`.
    /// **알림 하나만으로 정답이 나오지 않는가.** 하나로 끝나면 퍼즐이 아니다.
    ///
    /// 알림을 하나씩 지워 보며 셋을 확인한다:
    ///   ① 어느 알림도 **단독으로** 정답을 세우지 못한다 (둘씩 짝지어도 안 된다)
    ///   ② 필요한 알림은 **어느 하나를 빼도** 정답이 무너진다 (군더더기가 없다)
    ///   ③ 정답 하나를 세우는 데 드는 알림이 최소 몇 장인지 실제로 센다
    ///
    /// ②가 중요하다. 같은 사실로 가는 길이 두 개면 하나를 빼도 안 무너지고,
    /// 그러면 "밀려 사라졌다"가 아프지 않다 — 이 게임의 시계가 멈춘다.
    /// </summary>
    [TestFixture]
    public sealed class NoSingleNotificationTests
    {
        private static HashSet<string> StandsWith(GameData d, IEnumerable<string> notifs, bool battery)
        {
            HashSet<string> tok = Deduction.Tokens(d, notifs, battery, World.Lockscreen);
            return Deduction.Verdicts(d, Deduction.Facts(d, tok, World.Lockscreen));
        }

        [Test]
        public void 어떤_알림_한_장도_단독으로_정답을_세우지_못한다()
        {
            GameData d = TestWorld.Data;
            foreach (Card c in TestWorld.Screen().Cards)
            {
                // 잔량까지 공짜로 얹어 준다 — 가장 유리한 조건에서도 못 세워야 한다
                HashSet<string> stood = StandsWith(d, new List<string> { c.def.id }, true);
                Assert.That(stood.Count, Is.EqualTo(0),
                    c.def.id + " 한 장만으로 정답 " + string.Join(",", stood) + " 이 선다 — 퍼즐이 아니다");
            }
            TestContext.WriteLine("알림 " + TestWorld.Screen().Cards.Count + "장 각각 단독 → 정답 0개");
        }

        [Test]
        public void 알림_두_장으로도_정답이_서지_않는다()
        {
            GameData d = TestWorld.Data;
            List<string> need = Deduction.AllNeededNotifs(d);
            int pairs = 0;
            for (int i = 0; i < need.Count; i++)
                for (int j = i + 1; j < need.Count; j++)
                {
                    pairs++;
                    HashSet<string> stood = StandsWith(d, new List<string> { need[i], need[j] }, true);
                    Assert.That(stood.Count, Is.EqualTo(0),
                        need[i] + " + " + need[j] + " 둘만으로 " + string.Join(",", stood) + " 이 선다");
                }
            TestContext.WriteLine("필요한 알림 " + need.Count + "장의 짝 " + pairs + "가지 전부 → 정답 0개");
        }

        [Test]
        public void 필요한_알림은_어느_하나를_빼도_정답이_무너진다()
        {
            GameData d = TestWorld.Data;
            List<string> all = new List<string>(TestWorld.AllEverVisible(World.Lockscreen));
            HashSet<string> full = StandsWith(d, all, true);
            Assert.That(full.Count, Is.EqualTo(TestWorld.VerdictCount), "전부 있어도 다 서지 않는다");

            List<string> need = Deduction.AllNeededNotifs(d);
            foreach (string drop in need)
            {
                List<string> less = new List<string>(all);
                less.Remove(drop);
                HashSet<string> stood = StandsWith(d, less, true);
                Assert.That(stood.Count, Is.LessThan(TestWorld.VerdictCount),
                    drop + " 을 빼도 정답 셋이 그대로 선다 — 군더더기다. 같은 사실로 가는 길이 두 개다");
            }

            // 반대쪽도 확인한다: 필요하지 않다고 적은 알림은 정말로 빼도 아무 일이 없어야 한다
            int spare = 0;
            foreach (Card c in TestWorld.Screen().Cards)
            {
                if (need.Contains(c.def.id)) continue;
                List<string> less = new List<string>(all);
                less.Remove(c.def.id);
                Assert.That(StandsWith(d, less, true).Count, Is.EqualTo(TestWorld.VerdictCount),
                    c.def.id + " 은 필요 목록에 없는데 빼면 무너진다 — 목록이 틀렸다");
                spare++;
            }
            TestContext.WriteLine("필요한 " + need.Count + "장은 전부 하나만 빠져도 무너지고, 나머지 " + spare + "장은 빠져도 그대로다");
        }

        [Test]
        public void 잔량_한_번만_읽어서는_시각을_못_정한다()
        {
            // 배터리도 '알림 한 장'과 같은 취급을 받아야 한다 — 두 번 벌려 읽어야만 나온다.
            GameData d = TestWorld.Data;
            List<string> all = new List<string>(TestWorld.AllEverVisible(World.Lockscreen));
            HashSet<string> withoutRate = StandsWith(d, all, false);
            Assert.That(withoutRate.Count, Is.LessThan(TestWorld.VerdictCount),
                "잔량 감소율 없이도 정답 셋이 다 선다 — 배터리가 장식이다");
            TestContext.WriteLine("잔량을 재지 못하면 " + withoutRate.Count + "/" + TestWorld.VerdictCount
                + " (" + string.Join(",", withoutRate) + ")");

            // 한 번만 깨우면 감소율이 안 나온다는 것도 실제로 확인한다
            Session s = TestWorld.Sess(World.Lockscreen);
            Assert.That(s.Run(new List<int> { d.Phone.foundAtMin }).batteryRate, Is.False);
            int gap = d.Phone.battery.rateMinGapMin;
            Assert.That(s.Run(new List<int> { d.Phone.foundAtMin, d.Phone.foundAtMin + gap - 1 }).batteryRate, Is.False,
                "너무 붙여 읽었는데 감소율이 나왔다");
            Assert.That(s.Run(new List<int> { d.Phone.foundAtMin, d.Phone.foundAtMin + gap }).batteryRate, Is.True);
        }

        [Test]
        public void 정답_하나에_드는_알림_수를_실제로_센다()
        {
            GameData d = TestWorld.Data;
            foreach (VerdictDef v in d.Case.verdicts)
            {
                List<string> foot = Deduction.NotifFootprint(d, v);
                int min = MinimalSubsetSize(d, v, foot);
                Assert.That(min, Is.GreaterThanOrEqualTo(TestWorld.C.minNotificationsPerVerdict),
                    v.id + " 은 알림 " + min + "장이면 선다 — 너무 싸다");
                Assert.That(min, Is.EqualTo(foot.Count),
                    v.id + " 의 필요 목록 " + foot.Count + "장 중 " + min + "장만 실제로 쓰인다 — 목록에 군더더기가 있다");
                TestContext.WriteLine(v.id + ": 알림 " + min + "장"
                    + (Deduction.NeedsBattery(d, v) ? " + 잔량 두 번" : "") + " — " + string.Join(", ", foot));
            }
        }

        /// <summary>이 정답을 세우는 가장 작은 알림 부분집합의 크기. 목록이 작아 전수로 센다.</summary>
        private static int MinimalSubsetSize(GameData d, VerdictDef v, List<string> foot)
        {
            int n = foot.Count;
            Assert.That(n, Is.LessThanOrEqualTo(20), "전수로 세기엔 너무 크다");
            int best = n + 1;
            for (int mask = 0; mask < (1 << n); mask++)
            {
                int bits = System.Numerics.BitOperations.PopCount((uint)mask);
                if (bits >= best) continue;
                List<string> subset = new List<string>();
                for (int i = 0; i < n; i++) if ((mask & (1 << i)) != 0) subset.Add(foot[i]);
                HashSet<string> tok = Deduction.Tokens(d, subset, true, World.Lockscreen);
                if (Deduction.Stands(d, v, Deduction.Facts(d, tok, World.Lockscreen))) best = bits;
            }
            return best;
        }
    }
}
