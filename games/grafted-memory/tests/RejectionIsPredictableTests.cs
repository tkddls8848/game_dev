using System.Collections.Generic;
using Graft.Data;
using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// ★ 핵 검사기 하나 — **거부가 적힌 규칙으로 설명되는가.**
    ///
    /// 플레이어가 알 수 있었던 것만으로 거부를 예측할 수 있어야 한다.
    /// 설명되지 않는 거부가 있으면 그건 규칙이 아니라 운이다.
    ///
    /// games/tactics-partisan-1941 · games/tactics-whisper-map 의 DetectionFairness 가
    /// 같은 문제를 푼 모양을 그대로 따랐다 — **세 겹이다.**
    ///
    ///   ① 정적 보장   표본이 아니라 전수. 아직 아무도 당하지 않았을 뿐인 구멍을 설계 단계에서 막는다
    ///   ② 동적 감사   실제로 일어난 떨림 하나하나를 여섯 판정 중 하나로 분류한다
    ///   ③ 음성 대조군 데이터를 일부러 망가뜨리면 **감사기가 반드시 실패해야 한다**
    ///
    /// ③이 이 파일의 절반을 차지한다. ①②만 있으면 통과가
    /// 「설계가 옳다」인지 「감사기에 이가 없다」인지 가릴 수 없다.
    /// </summary>
    [TestFixture]
    public sealed class RejectionIsPredictableTests
    {
        // ── ① 정적 보장 (전수) ───────────────────────────────────────────────

        [Test]
        public void 정적_보장_구멍이_없다()
        {
            foreach (CommissionDef c in TestWorld.Data.AllCommissions)
            {
                List<string> gaps = RejectionAudit.StaticGaps(TestWorld.Data, c);
                Assert.That(gaps, Is.Empty, c.id + " 의 구멍: " + string.Join(" / ", gaps));
            }
        }

        // ── ② 동적 감사 ──────────────────────────────────────────────────────

        [Test]
        public void 일어난_모든_떨림이_설명된다()
        {
            int checked_ = 0, rejected = 0;
            List<string> bad = new List<string>();
            foreach (string p in TestWorld.PolicyNames())
                foreach (string cid in TestWorld.CommissionIds())
                    foreach (Attempt a in TestWorld.Run(cid, p).Attempts)
                    {
                        RejectionVerdict v = RejectionAudit.Audit(a);
                        checked_ += v.TremorsChecked;
                        if (!a.Accepted) rejected++;
                        foreach (RejectFact f in v.Unfair())
                            bad.Add(cid + "/" + p + "/" + a.Trial + ": " + f);
                    }

            TestContext.WriteLine("떨림 " + checked_ + "개 감사 · 거부된 시도 " + rejected + "개");
            Assert.That(bad, Is.Empty, "설명되지 않는 떨림이 있다:\n" + string.Join("\n", bad));
            Assert.That(checked_, Is.GreaterThan(0), "감사할 떨림이 하나도 없었다 — 검사기가 헛돈다");
        }

        /// <summary>
        /// 감사기의 **어려운 갈래가 실제로 켜지는가.** 스크린 기억으로 죽는 경우가 한 번도 없으면
        /// 「겹쳐 물을 수 있었으니 공정하다」는 판정이 통과했는지 손대지 않았는지 알 수 없다.
        /// </summary>
        [Test]
        public void 스크린_기억으로_죽는_경우가_실제로_생긴다()
        {
            int distorted = 0;
            foreach (string cid in TestWorld.CommissionIds())
                foreach (Attempt a in TestWorld.Run(cid, "TrustDistorted").Attempts)
                    distorted += RejectionAudit.Audit(a).Count(RejectVerdicts.DistortedButCheckable);

            TestContext.WriteLine("DistortedButCheckable " + distorted + "건 (TrustDistorted 정책)");
            Assert.That(distorted, Is.GreaterThan(0),
                        "스크린 기억으로 죽은 일이 한 번도 없다 — 감사기의 어려운 갈래를 아무도 밟지 않았다");
        }

        [Test]
        public void 안_물어서_죽는_경우도_실제로_생긴다()
        {
            int probeable = 0;
            foreach (string cid in TestWorld.CommissionIds())
                foreach (Attempt a in TestWorld.Run(cid, "Reckless").Attempts)
                    probeable += RejectionAudit.Audit(a).Count(RejectVerdicts.Probeable);
            TestContext.WriteLine("Probeable " + probeable + "건 (Reckless 정책)");
            Assert.That(probeable, Is.GreaterThan(0), "안 물어서 죽은 일이 한 번도 없다");
        }

        /// <summary>
        /// 따져 심으면 **예측이 빗나가지 않는다.** 이것이 공정의 다른 면이다 —
        /// 알 수 있는 것을 다 알아 낸 플레이어는 꿈이 무엇을 거부할지 미리 안다.
        /// </summary>
        [Test]
        public void 겹쳐_물은_플레이어는_예측이_빗나가지_않는다()
        {
            foreach (string cid in TestWorld.CommissionIds())
            {
                PolicyRun r = TestWorld.Run(cid, "Careful");
                Assert.That(r.Surprises, Is.Zero,
                            cid + ": 겹쳐 묻고 따져 심었는데도 예측이 " + r.Surprises + "번 빗나갔다");
            }
        }

        // ── ③ 음성 대조군 — 망가뜨리면 반드시 실패해야 한다 ────────────────────

        [Test]
        public void 대조군_연상을_끊으면_정적_보장이_잡는다()
        {
            GameData d = TestWorld.CloneData();
            SubjectDef s = d.Subject("subj-hanwoo");
            s.links = Without(s.links, "m-hospital-hall");   // 병원 복도로 가는 길을 전부 끊는다

            foreach (CommissionDef c in d.Commissions.commissions)
            {
                List<string> gaps = RejectionAudit.StaticGaps(d, c);
                Assert.That(gaps, Is.Not.Empty, c.id + ": 길을 끊었는데도 구멍을 못 봤다");
                Assert.That(string.Join(" ", gaps), Does.Contain("m-hospital-hall"));
            }
            TestContext.WriteLine("길을 끊자 정적 보장이 즉시 구멍을 보고했다 (전수라서 씨드 운에 기대지 않는다)");
        }

        [Test]
        public void 대조군_닿지_않는_기억으로_죽으면_Unknowable_이_된다()
        {
            GameData d = TestWorld.CloneData();
            SubjectDef s = d.Subject("subj-hanwoo");
            s.links = Without(s.links, "m-hospital-hall");

            // 병원 복도(9600~9603, 시립 병원)와 시간·장소가 어긋나게 심는다.
            // 이제 그 기억에는 어떤 길로도 닿을 수 없으므로 이 거부는 **알 수 없었던 것**이다.
            DreamSession sess = DreamSession.Open(d, "com-hospital-vow", 0);
            ProbeKnowledge k = sess.FreshKnowledge();
            Attempt a = Hand(sess, k, new GraftChoice
            {
                AnchorMemoryId = "m-fathers-death", PlaceId = "pl-pier", DayIndex = 9600,
                MoodId = "mood-dread", IntensityPercent = 90
            });

            RejectionVerdict v = RejectionAudit.Audit(a);
            Assert.That(v.Count(RejectVerdicts.Unknowable), Is.GreaterThan(0),
                        "닿을 수 없는 기억으로 죽었는데 Unknowable 이 하나도 없다");
            Assert.That(v.Fair, Is.False, "불공정한 거부인데 감사기가 공정하다고 했다");
        }

        [Test]
        public void 대조군_겹쳐_물을_길을_없애면_DistortedUncheckable_이_된다()
        {
            GameData d = TestWorld.CloneData();
            SubjectDef s = d.Subject("subj-hanwoo");

            // 부두의 냄새에 이르는 길을 하나만 남긴다 — 이제 겹쳐 물어 확인할 수 없다.
            List<LinkDef> keep = new List<LinkDef>();
            bool kept = false;
            foreach (LinkDef l in s.links)
            {
                bool touches = l.a == "m-pier-smell" || l.b == "m-pier-smell";
                if (!touches) { keep.Add(l); continue; }
                bool viaStart = l.a == "m-fathers-death" || l.b == "m-fathers-death";
                if (viaStart && !kept) { keep.Add(l); kept = true; }
            }
            s.links = keep.ToArray();
            Assert.That(kept, Is.True, "남길 길을 찾지 못했다 — 대조군을 세우지 못했다");

            // 이 회차에 스크린이 되는 기억을 부두의 냄새로 못 박는다
            foreach (MemoryDef m in s.memories) m.distortable = m.id == "m-pier-smell";
            s.distortedPerSession = 1;

            // 정적 보장이 먼저 잡아야 한다
            List<string> gaps = RejectionAudit.StaticGaps(d, d.Commission("com-pier-promise"));
            Assert.That(string.Join(" ", gaps), Does.Contain("m-pier-smell"),
                        "겹쳐 물 길을 없앴는데 정적 보장이 못 봤다");

            // 동적으로도 DistortedUncheckable 이 나와야 한다.
            // 겉값(3부두 창고)을 믿고 그 자리에 심는다. 참값은 개항로 집이라 시간·장소가 어긋난다.
            DreamSession sess = DreamSession.Open(d, "com-pier-promise", 0);
            Assert.That(sess.Distorted, Contains.Item("m-pier-smell"));
            ProbeKnowledge k = sess.FreshKnowledge();
            Assert.That(k.Probe("m-fathers-death", "m-pier-smell"), Is.True, "한 길로도 띄우지 못했다");
            Assert.That(k.Corroborated("m-pier-smell"), Is.False, "길이 하나뿐인데 확인됐다고 한다");

            Attempt a = Hand(sess, k, new GraftChoice
            {
                AnchorMemoryId = "m-pier-smell", PlaceId = "pl-pier", DayIndex = 4800,
                MoodId = "mood-dread", IntensityPercent = 55
            });
            RejectionVerdict v = RejectionAudit.Audit(a);
            Assert.That(v.Count(RejectVerdicts.DistortedUncheckable), Is.GreaterThan(0),
                        "겹쳐 물 길이 없는 스크린 기억으로 죽었는데 DistortedUncheckable 이 없다");
            Assert.That(v.Fair, Is.False);
        }

        [Test]
        public void 대조군_규칙표에서_규칙을_빼면_RuleUnstated_가_된다()
        {
            GameData d = TestWorld.CloneData();
            List<string> stated = new List<string>();
            foreach (string r in d.Balance.statedRules)
                if (r != TremorRules.MoodClashAnchor) stated.Add(r);
            d.Balance.statedRules = stated.ToArray();

            // 정서가 크게 어긋나는 자리에 심어 MoodClashAnchor 를 일부러 낸다
            DreamSession sess = DreamSession.Open(d, "com-inheritance", 0);
            ProbeKnowledge k = sess.FreshKnowledge();
            Attempt a = Hand(sess, k, new GraftChoice
            {
                AnchorMemoryId = "m-fathers-death", PlaceId = "pl-house", DayIndex = 13010,
                MoodId = "mood-longing", IntensityPercent = 55
            });
            RejectionVerdict v = RejectionAudit.Audit(a);
            Assert.That(v.Count(RejectVerdicts.RuleUnstated), Is.GreaterThan(0),
                        "규칙표에 없는 이유로 떨렸는데 RuleUnstated 가 없다");
            Assert.That(v.Fair, Is.False);

            // 정적 보장도 같은 것을 본다
            Assert.That(string.Join(" ", RejectionAudit.StaticGaps(d, d.Commission("com-inheritance"))),
                        Does.Contain(TremorRules.MoodClashAnchor));
        }

        [Test]
        public void 대조군_명료도를_빼앗으면_물을_수_있던_것이_Unknowable_이_된다()
        {
            GameData d = TestWorld.CloneData();
            foreach (CommissionDef c in d.Commissions.commissions) c.lucidityBudget = 1;

            int unknowable = 0;
            for (int trial = 0; trial < 20; trial++)
            {
                DreamSession sess = DreamSession.Open(d, "com-house-farewell", trial);
                Attempt a = Policies.Reckless(sess, trial);
                unknowable += RejectionAudit.Audit(a).Count(RejectVerdicts.Unknowable);
            }
            Assert.That(unknowable, Is.GreaterThan(0),
                        "명료도를 1로 줄였는데도 「물을 수 있었다」로 남았다 — 감사기가 예산을 보지 않는다");
            TestContext.WriteLine("명료도 1: Unknowable " + unknowable + "건. 예산이 판정에 실제로 들어간다");
        }

        /// <summary>
        /// **감사기의 안전망이 산다는 증거.** 눈을 참값으로 갈아 끼우면 예측과 실제가 절대 갈리지 않아야 하고,
        /// 그래도 갈리면 ModelMismatch 가 잡는다. 여기서는 갈리지 않는 쪽을 확인한다.
        /// </summary>
        [Test]
        public void 참값을_다_쥐면_예측이_절대_갈리지_않는다()
        {
            foreach (string cid in TestWorld.CommissionIds())
                for (int trial = 0; trial < 10; trial++)
                {
                    DreamSession s = DreamSession.Open(TestWorld.Data, cid, trial);
                    List<GraftChoice> all = GraftRules.LegalChoices(
                        s.Commission, s.Net.StartSurfaced(), TestWorld.Data.Balance.dayStep);
                    foreach (GraftChoice ch in all)
                    {
                        GraftVerdict truth = s.Judge(ch);
                        GraftVerdict sameEye = GraftRules.Evaluate(TestWorld.Data, s.Net, s.Moods,
                                                                   s.Commission, ch, s.Truth, true);
                        Assert.That(sameEye.TotalTremor, Is.EqualTo(truth.TotalTremor));
                        Assert.That(sameEye.Accepted, Is.EqualTo(truth.Accepted));
                    }
                }
        }

        // ── 도움 ─────────────────────────────────────────────────────────────

        private static Attempt Hand(DreamSession s, ProbeKnowledge k, GraftChoice c)
        {
            return new Attempt
            {
                PolicyName = "Hand", CommissionId = s.Commission.id, Trial = 0, Session = s,
                Knowledge = k, Choice = c, Predicted = s.Predict(k, c), Truth = s.Judge(c)
            };
        }

        private static LinkDef[] Without(LinkDef[] links, string memoryId)
        {
            List<LinkDef> keep = new List<LinkDef>();
            foreach (LinkDef l in links)
                if (l.a != memoryId && l.b != memoryId) keep.Add(l);
            return keep.ToArray();
        }
    }
}
