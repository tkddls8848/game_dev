using System.Collections.Generic;
using NUnit.Framework;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>
    /// ★ 핵 검사기 하나 — `LockscreenSuffices`.
    /// **정답이 잠금 화면에 뜬 것만으로 도출되는가.**
    ///
    /// 잠긴 내용을 알아야 풀리면 이 게임은 성립하지 않는다. 그런데 이 주장은
    /// **잠긴 쪽이 비어 있으면 공짜로 참이 된다** — 그래서 잠긴 쪽(사진첩·통화 기록·메시지 전문·
    /// 건강 앱·메모·은행 앱)에 **답이 통째로** 들어 있게 만들어 두고, 네 세계를 견준다:
    ///
    ///   잠금 풀림   — 모든 것을 본다                → 3/3 서야 한다 (답이 애초에 있다는 확인)
    ///   잠긴 쪽만   — 알림을 하나도 못 본다          → 3/3 서야 한다 (**대조군이 비어 있지 않다**)
    ///   잠김        — 잠금 화면만 본다 (이 게임)     → 3/3 서야 한다 ★
    ///   미리보기 끔 — 알림이 왔다는 것만 본다        → 0/3 이어야 한다 (**표면 내용이 일하고 있다**)
    ///
    /// 마지막 줄이 이 검사기의 이빨이다. 그것이 없으면 "잠금 화면으로 충분하다"는 말이
    /// "알림이 있다는 사실만으로 충분하다"는 훨씬 약한 주장과 구별되지 않는다.
    /// </summary>
    [TestFixture]
    public sealed class LockscreenSufficesTests
    {
        private static HashSet<string> Maximal(World w)
        {
            GameData d = TestWorld.Data;
            HashSet<string> tok = Deduction.Tokens(d, TestWorld.AllEverVisible(w), true, w);
            return Deduction.Facts(d, tok, w);
        }

        [Test]
        public void 잠금_화면만으로_정답_셋이_전부_선다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> facts = Maximal(World.Lockscreen);
            foreach (VerdictDef v in d.Case.verdicts)
            {
                Assert.That(Deduction.Stands(d, v, facts), Is.True,
                    v.id + " (" + v.questionKo + ") 이 잠금 화면만으로는 서지 않는다");
                TestContext.WriteLine(v.id + " → " + Option(v, v.correct).ko + "  (근거 " + v.supports.Count + " · 알림 "
                    + Deduction.NotifFootprint(d, v).Count + "장"
                    + (Deduction.NeedsBattery(d, v) ? " + 잔량" : "") + ")");
            }
        }

        [Test]
        public void 예산_안에서_실제로_그_셋을_쥘_수_있다()
        {
            // 위 검사기는 **논리**를 본다. 여기서는 배터리와 밀려남을 쥔 채 실제로 도달하는지를 본다.
            SessionResult r = TestWorld.Best(World.Lockscreen);
            Assert.That(r.verdicts.Count, Is.EqualTo(TestWorld.VerdictCount),
                "예산 안에서는 " + r.verdicts.Count + "/" + TestWorld.VerdictCount + " 밖에 못 세운다: " + r.Text);
            Assert.That(r.wentDark, Is.False, "배터리가 먼저 죽었다: " + r.Text);
            TestContext.WriteLine("깨우기 " + r.wakes.Count + "번으로 " + r.verdicts.Count + "/" + TestWorld.VerdictCount + " · " + r.Text);
        }

        [Test]
        public void 정답의_근거에_잠긴_것이_하나도_섞여_있지_않다()
        {
            GameData d = TestWorld.Data;
            foreach (VerdictDef v in d.Case.verdicts)
                foreach (string fid in Deduction.FactFootprint(d, v))
                    foreach (string tok in d.Fact(fid).needs)
                    {
                        Assert.That(d.HasLocked(tok), Is.False,
                            v.id + " 의 근거 " + fid + " 가 잠긴 것 " + tok + " 을 쓴다");
                        string n = GameData.NotifOf(tok);
                        if (n != null) Assert.That(d.HasNotif(n), Is.True, fid + " 가 없는 알림을 가리킨다: " + n);
                    }
            TestContext.WriteLine("정답 " + d.Case.verdicts.Count + "개의 근거가 전부 잠금 화면 쪽에 있다");
        }

        [Test]
        public void 대조군_하나_잠긴_쪽에도_답이_통째로_들어_있다()
        {
            // 이것이 없으면 "잠금 화면으로 충분하다"가 "잠긴 쪽이 비어서 충분하다"와 구별되지 않는다.
            GameData d = TestWorld.Data;
            HashSet<string> tok = Deduction.Tokens(d, new List<string>(), false, World.Unlocked);
            HashSet<string> facts = Deduction.Facts(d, tok, World.Unlocked);
            HashSet<string> stood = Deduction.Verdicts(d, facts);
            Assert.That(stood.Count, Is.GreaterThanOrEqualTo(TestWorld.C.lockedOnlyVerdictsMin),
                "잠긴 쪽만으로는 " + stood.Count + "개밖에 서지 않는다 — 대조군이 비어 있어 비교가 공짜가 된다");
            Assert.That(facts.Count, Is.EqualTo(d.Case.facts.Count),
                "잠긴 쪽이 사실 " + facts.Count + "/" + d.Case.facts.Count + " 만 낸다");
            TestContext.WriteLine("잠긴 쪽만: 사실 " + facts.Count + "/" + d.Case.facts.Count
                + " · 정답 " + stood.Count + "/" + TestWorld.VerdictCount + " — 답은 잠긴 쪽에도 통째로 있다");
        }

        [Test]
        public void 대조군_둘_잠금을_풀어도_답이_바뀌지_않는다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> lockScreen = Deduction.Verdicts(d, Maximal(World.Lockscreen));
            HashSet<string> unlocked = Deduction.Verdicts(d, Maximal(World.Unlocked));
            Assert.That(unlocked.Count, Is.EqualTo(TestWorld.VerdictCount), "잠금을 풀었는데도 다 서지 않는다");
            foreach (string id in unlocked)
                Assert.That(lockScreen.Contains(id), Is.True, id + " 은 잠금을 풀어야만 선다");
            foreach (string id in lockScreen)
                Assert.That(unlocked.Contains(id), Is.True, id + " 이 잠금을 풀면 무너진다 — 모형이 어긋났다");
            TestContext.WriteLine("잠금 화면 " + lockScreen.Count + "/" + TestWorld.VerdictCount
                + " = 잠금 풀림 " + unlocked.Count + "/" + TestWorld.VerdictCount + " — 풀어도 얻는 것이 없다");
        }

        [Test]
        public void 대조군_셋_미리보기를_끄면_아무것도_서지_않는다()
        {
            // ★ 이빨. 일하고 있는 것이 '알림의 존재'가 아니라 **표면에 뜬 내용**임을 보인다.
            GameData d = TestWorld.Data;
            HashSet<string> facts = Maximal(World.NoPreview);
            HashSet<string> stood = Deduction.Verdicts(d, facts);
            Assert.That(stood.Count, Is.LessThanOrEqualTo(TestWorld.C.noPreviewVerdictsMax),
                "미리보기를 껐는데도 정답 " + stood.Count + "개가 선다 — 이 게임은 알림 개수 세기다");
            TestContext.WriteLine("미리보기 끔: 사실 " + facts.Count + "/" + d.Case.facts.Count
                + " · 정답 " + stood.Count + "/" + TestWorld.VerdictCount);
        }

        [Test]
        public void 필요한_알림은_전부_예산_안에서_잡을_수_있다()
        {
            // 공정성. 아무리 잘해도 닿을 수 없는 증거가 있으면 그것은 고장이다.
            GameData d = TestWorld.Data;
            Session s = TestWorld.Sess(World.Lockscreen);
            SessionResult plan = TestWorld.Best(World.Lockscreen);
            int budget = plan.wakes.Count - 1;          // 실제로 짤 수 있는 일정에서 마지막 깨우기의 처지
            int narrowest = int.MaxValue; string worst = "";
            foreach (string id in Deduction.AllNeededNotifs(d))
            {
                Assert.That(plan.seen.Contains(id), Is.True,
                    id + " 은 예산 안에서 짤 수 있는 어떤 일정에도 걸리지 않는다 — 풀 수 없는 사건이다");
                List<int> cw = s.CatchWindow(id, budget);
                Assert.That(cw.Count, Is.GreaterThan(0),
                    id + " 은 마지막 깨우기 처지에서는 배터리가 닿지 않는다");
                if (cw.Count < narrowest) { narrowest = cw.Count; worst = id; }
            }
            TestContext.WriteLine("필요한 알림 " + Deduction.AllNeededNotifs(d).Count
                + "장 전부 잡을 수 있다 · 가장 좁은 창 " + narrowest + "분 (" + worst + ")");
        }

        private static OptionDef Option(VerdictDef v, string id)
        {
            foreach (OptionDef o in v.options) if (o.id == id) return o;
            return null;
        }
    }
}
