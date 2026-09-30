using System.Collections.Generic;
using NUnit.Framework;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>공통 검사기 — `DataValidator`. 참조 무결성과 이 PoC가 기대는 구조 불변식.</summary>
    [TestFixture]
    public sealed class DataIntegrityTests
    {
        [Test]
        public void 알림이_성립한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> ids = new HashSet<string>();
            foreach (NotifDef n in d.Notifs.notifications)
            {
                Assert.That(ids.Add(n.id), "알림 id가 겹친다: " + n.id);
                Assert.That(n.kind, Is.AnyOf("message", "call", "photo", "card", "system", "health", "delivery"), n.id);
                Assert.That(n.app, Is.Not.Null.And.Not.Empty, n.id);
                Assert.That(n.fromKo, Is.Not.Null.And.Not.Empty, n.id + " 에 보낸이가 없다");
                Assert.That(n.previewKo, Is.Not.Null.And.Not.Empty, n.id + " 에 한국어 원문이 없다");
                Assert.That(n.arriveMin, Is.GreaterThan(0), n.id);
                Assert.That(n.investigation, Is.EqualTo(n.arriveMin > d.Phone.foundAtMin),
                    n.id + " 의 investigation 표시가 도착 시각과 어긋난다");
                if (n.kind == "photo" || n.exifAtMin >= 0)
                {
                    Assert.That(n.exifAtMin, Is.GreaterThan(0), n.id + " 은 사진인데 촬영 시각이 없다");
                    Assert.That(n.exifPlaceKo, Is.Not.Null.And.Not.Empty, n.id + " 은 사진인데 장소가 없다");
                    Assert.That(n.exifAtMin, Is.LessThanOrEqualTo(n.arriveMin),
                        n.id + " 의 촬영 시각이 알림 도착보다 늦다 — 시간이 거꾸로 간다");
                }
                else Assert.That(n.exifAtMin, Is.EqualTo(-1), n.id + " 은 사진이 아닌데 exifAtMin 이 -1 이 아니다");
            }
            TestContext.WriteLine("알림 " + d.Notifs.notifications.Count + "장 · 사람이 적은 것 전부 성립");
        }

        [Test]
        public void 사실과_정답이_성립한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> fids = new HashSet<string>();
            foreach (FactDef f in d.Case.facts)
            {
                Assert.That(fids.Add(f.id), "사실 id가 겹친다: " + f.id);
                Assert.That(f.ko, Is.Not.Null.And.Not.Empty, f.id + " 에 한국어 원문이 없다");
                Assert.That(f.needs, Is.Not.Null.And.Not.Empty, f.id + " 이 아무것도 요구하지 않는다 — 공짜 사실이다");
                foreach (string tok in f.needs)
                {
                    string grade = GameData.GradeOf(tok);
                    Assert.That(grade, Is.AnyOf("card", "preview", "exif", "rate"), f.id + " 의 딱지 등급이 이상하다: " + tok);
                    string n = GameData.NotifOf(tok);
                    if (n == null) { Assert.That(tok, Is.EqualTo(Deduction.Battery), f.id); continue; }
                    Assert.That(d.HasNotif(n), f.id + " 이 없는 알림을 가리킨다: " + n);
                    if (grade == "exif") Assert.That(d.Notif(n).exifAtMin, Is.GreaterThan(0),
                        f.id + " 이 사진이 아닌 " + n + " 의 EXIF 를 요구한다");
                    if (grade == "preview") Assert.That(d.Notif(n).previewKo, Is.Not.Empty, f.id);
                }
                Assert.That(f.altNeedsUnlocked, Is.Not.Null.And.Not.Empty,
                    f.id + " 에 잠긴 쪽 경로가 없다 — 대조군이 비어 버린다");
                foreach (string x in f.altNeedsUnlocked)
                    Assert.That(d.HasLocked(x), f.id + " 이 없는 잠긴 항목을 가리킨다: " + x);
            }

            HashSet<string> vids = new HashSet<string>();
            foreach (VerdictDef v in d.Case.verdicts)
            {
                Assert.That(vids.Add(v.id), "정답 id가 겹친다: " + v.id);
                Assert.That(v.questionKo, Is.Not.Null.And.Not.Empty, v.id);
                Assert.That(v.options.Count, Is.GreaterThanOrEqualTo(3), v.id + " 의 선택지가 너무 적다");
                Assert.That(v.supports, Is.Not.Null.And.Not.Empty, v.id);
                foreach (string s in v.supports) Assert.That(d.HasFact(s), v.id + " 이 없는 사실을 가리킨다: " + s);
                int correct = 0;
                HashSet<string> oids = new HashSet<string>();
                foreach (OptionDef o in v.options)
                {
                    Assert.That(oids.Add(o.id), v.id + " 의 선택지 id가 겹친다: " + o.id);
                    Assert.That(o.ko, Is.Not.Null.And.Not.Empty, v.id + "·" + o.id);
                    if (o.id == v.correct) { correct++; continue; }
                    Assert.That(o.refutedBy, Is.Not.Null.And.Not.Empty,
                        v.id + " 의 " + o.id + " 를 지울 방법이 없다 — 정답이 영영 서지 못한다");
                    foreach (string r in o.refutedBy) Assert.That(d.HasFact(r), v.id + "·" + o.id + " 가 없는 사실을 가리킨다: " + r);
                }
                Assert.That(correct, Is.EqualTo(1), v.id + " 의 정답 선택지가 정확히 하나가 아니다");
            }
            TestContext.WriteLine("사실 " + d.Case.facts.Count + "개 · 정답 " + d.Case.verdicts.Count + "개 성립");
        }

        [Test]
        public void 기기와_예산이_성립한다()
        {
            GameData d = TestWorld.Data;
            Assert.That(d.Phone.stack.capacity, Is.GreaterThan(0));
            Assert.That(d.Phone.lastMinuteMin, Is.GreaterThan(d.Phone.foundAtMin));
            BatteryDef b = d.Phone.battery;
            Assert.That(b.startPermille, Is.InRange(1, 1000));
            Assert.That(b.drainStepMin, Is.GreaterThan(0));
            Assert.That(b.idleDrainPermillePerStep, Is.GreaterThan(0));
            Assert.That(b.wakeCostPermille, Is.GreaterThan(0));
            Assert.That(b.rateMinGapMin, Is.GreaterThan(0));

            Lockscreen s = TestWorld.Screen();
            Assert.That(s.BatteryAt(d.Phone.foundAtMin, 0), Is.EqualTo(b.startPermille));
            int prev = int.MaxValue;
            for (int t = d.Phone.foundAtMin; t <= d.Phone.lastMinuteMin; t++)
            {
                int v = s.BatteryAt(t, 0);
                Assert.That(v, Is.LessThanOrEqualTo(prev), "잔량이 " + t + " 에서 늘었다");
                prev = v;
            }
            for (int n = 0; n < 8; n++)
                Assert.That(s.LastLivingMinute(n + 1), Is.LessThanOrEqualTo(s.LastLivingMinute(n)),
                    "깨우기를 더 했는데 더 오래 산다");

            NoiseSpec ns = d.Phone.noise;
            Assert.That(ns.countMin, Is.GreaterThanOrEqualTo(0));
            Assert.That(ns.countMax, Is.GreaterThanOrEqualTo(ns.countMin));
            Assert.That(ns.toMin, Is.GreaterThan(ns.fromMin));
            Assert.That(ns.fromMin, Is.GreaterThan(d.Phone.foundAtMin), "잡음이 주운 시각보다 먼저 온다");
            Assert.That(ns.lines.Count, Is.GreaterThan(0));
            foreach (NoiseLine l in ns.lines) Assert.That(l.previewKo, Is.Not.Empty);
            TestContext.WriteLine("주운 시각 " + TestWorld.Clock(d.Phone.foundAtMin) + " · 칸 " + d.Phone.stack.capacity
                + "장 · 잔량 " + b.startPermille + "‰ · 깨우기 " + b.wakeCostPermille + "‰");
        }

        [Test]
        public void 저전력_알림과_잔량이_서로_맞는다()
        {
            // f_untouched_since_2012 의 논리가 데이터에서 실제로 성립하는지 본다.
            // 저전력 알림이 말한 20% 에서 대기 감소분만큼만 줄어 있어야 "화면을 켜지 않았다"가 참이 된다.
            GameData d = TestWorld.Data;
            NotifDef low = d.Notif("e_lowpower");
            Assert.That(low, Is.Not.Null, "저전력 알림이 없다 — 배터리 추론의 닻이 사라졌다");
            BatteryDef b = d.Phone.battery;
            int steps = (d.Phone.foundAtMin - low.arriveMin) / b.drainStepMin;
            int expected = 200 - b.idleDrainPermillePerStep * steps;
            Assert.That(b.startPermille, Is.EqualTo(expected),
                "저전력 알림의 20%(200‰)에서 " + steps + "칸 내려오면 " + expected + "‰ 인데 주운 잔량은 "
                + b.startPermille + "‰ 다 — 추론이 데이터와 어긋난다");
            TestContext.WriteLine("저전력 " + TestWorld.Clock(low.arriveMin) + " 200‰ → 주운 시각 "
                + TestWorld.Clock(d.Phone.foundAtMin) + " " + b.startPermille + "‰ (대기 " + steps + "칸)");
        }

        [Test]
        public void 밀려남은_되돌릴_수_없다()
        {
            // 한 번 사라진 알림이 다시 보이면 이 게임의 시계가 거꾸로 간다.
            GameData d = TestWorld.Data;
            Lockscreen s = TestWorld.Screen();
            foreach (Card c in s.Cards)
            {
                bool wasVisible = false, goneForGood = false;
                for (int t = c.def.arriveMin; t <= d.Phone.lastMinuteMin; t++)
                {
                    bool now = false;
                    foreach (Card x in s.Visible(t, World.Lockscreen)) if (x.def.id == c.def.id) { now = true; break; }
                    if (now)
                    {
                        Assert.That(goneForGood, Is.False, c.def.id + " 이 사라졌다가 " + t + " 에 되살아났다");
                        wasVisible = true;
                    }
                    else if (wasVisible) goneForGood = true;
                }
            }
            TestContext.WriteLine("알림 " + s.Cards.Count + "장 전부: 한 번 밀려나면 돌아오지 않는다");
        }
    }
}
