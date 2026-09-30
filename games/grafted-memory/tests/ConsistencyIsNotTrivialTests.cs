using System.Collections.Generic;
using Graft.Data;
using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// ★ 핵 검사기 둘 — **일관성 규칙이 장식이 아닌가.**
    ///
    /// 아무렇게나 심는 정책은 실패하고 따져 심는 정책은 성공하는가. 차이를 수치로 낸다.
    /// 차이가 없으면 규칙이 있으나 없으나 같다는 뜻이고, 그러면 이 컨셉이 죽는다.
    ///
    /// 판정은 **두 세계 비교**다 — 같은 씨드·같은 정책을
    ///   · 켠 세계: 논리·정서 규칙이 떨림을 낸다 (실제 설계)
    ///   · 끈 세계: 떨림이 하나도 생기지 않는다 (규칙을 뺀 세계)
    /// 로 각각 돌려, 정책 사이의 차이가 **켠 세계에서만** 벌어져야 통과다.
    /// </summary>
    [TestFixture]
    public sealed class ConsistencyIsNotTrivialTests
    {
        /// <summary>따져 심는 것과 아무렇게나 심는 것의 성공률 차이. 이 값이 이 PoC의 생사다.</summary>
        private const int MinGapPercentPoints = 40;

        [Test]
        public void 켠_세계에서_따져_심는_것이_크게_낫다()
        {
            int careful = TestWorld.OverallRate("Careful");
            int reckless = TestWorld.OverallRate("Reckless");
            int gap = careful - reckless;

            TestContext.WriteLine("Careful " + careful + "% · Reckless " + reckless + "% → 차이 " + gap + "%p");
            foreach (string cid in TestWorld.CommissionIds())
                TestContext.WriteLine("    " + cid + ": "
                                      + TestWorld.Run(cid, "Careful").RatePercent + "% vs "
                                      + TestWorld.Run(cid, "Reckless").RatePercent + "%");

            Assert.That(gap, Is.GreaterThanOrEqualTo(MinGapPercentPoints),
                        "따져 심는 것과 아무렇게나 심는 것의 차이가 " + gap + "%p 뿐이다 — 일관성 규칙이 장식이다");
        }

        /// <summary>
        /// **끈 세계에서는 차이가 사라져야 한다.** 사라지지 않으면 위의 차이가 일관성 규칙이 아니라
        /// 다른 무언가(탐색량·명료도 소비 같은 것)에서 온 것이다.
        /// </summary>
        [Test]
        public void 끈_세계에서는_차이가_사라진다()
        {
            int carefulOff = TestWorld.OverallRate("Careful", false);
            int recklessOff = TestWorld.OverallRate("Reckless", false);
            int gapOff = carefulOff - recklessOff;

            TestContext.WriteLine("규칙 끈 세계: Careful " + carefulOff + "% · Reckless " + recklessOff
                                  + "% → 차이 " + gapOff + "%p");
            Assert.That(carefulOff, Is.EqualTo(100), "규칙을 껐는데도 따져 심는 쪽이 실패한다");
            Assert.That(recklessOff, Is.EqualTo(100), "규칙을 껐는데도 아무렇게나 심는 쪽이 실패한다");
            Assert.That(gapOff, Is.Zero, "규칙을 껐는데도 정책 사이에 차이가 남았다 — 차이의 원인이 규칙이 아니다");
        }

        [Test]
        public void 두_세계의_차이가_유의하게_다르다()
        {
            int gapOn = TestWorld.OverallRate("Careful") - TestWorld.OverallRate("Reckless");
            int gapOff = TestWorld.OverallRate("Careful", false) - TestWorld.OverallRate("Reckless", false);
            TestContext.WriteLine("정책 차이: 켠 세계 " + gapOn + "%p · 끈 세계 " + gapOff + "%p");
            Assert.That(gapOn - gapOff, Is.GreaterThanOrEqualTo(MinGapPercentPoints),
                        "두 세계의 차이가 " + (gapOn - gapOff) + "%p 뿐이다");
        }

        /// <summary>
        /// 의뢰마다 따로 본다. 하나가 전부를 끌어올리는 것이면 「규칙이 산다」가 아니라
        /// 「의뢰 하나가 특별하다」다.
        /// </summary>
        [Test]
        public void 의뢰마다_따로_봐도_차이가_있다()
        {
            foreach (string cid in TestWorld.CommissionIds())
            {
                int careful = TestWorld.Run(cid, "Careful").RatePercent;
                int reckless = TestWorld.Run(cid, "Reckless").RatePercent;
                Assert.That(careful - reckless, Is.GreaterThanOrEqualTo(MinGapPercentPoints),
                            cid + ": 차이가 " + (careful - reckless) + "%p 뿐이다");
            }
        }

        /// <summary>
        /// 규칙이 켜진 세계에서 **아무렇게나 심는 것이 대부분 거부된다.** 그리고 거부의 이유가
        /// 논리·정서 양쪽에서 나와야 한다 — 한쪽만 나오면 다른 쪽 규칙이 죽어 있는 것이다.
        /// </summary>
        [Test]
        public void 논리와_정서가_둘_다_거부를_만든다()
        {
            int logic = 0, feeling = 0;
            Dictionary<string, int> byRule = new Dictionary<string, int>();
            foreach (string p in TestWorld.PolicyNames())
                foreach (string cid in TestWorld.CommissionIds())
                    foreach (Attempt a in TestWorld.Run(cid, p).Attempts)
                    {
                        if (a.Accepted) continue;
                        foreach (Tremor t in a.Truth.Tremors)
                        {
                            if (TremorRules.IsLogic(t.Rule)) logic++;
                            if (TremorRules.IsFeeling(t.Rule)) feeling++;
                            if (!byRule.ContainsKey(t.Rule)) byRule[t.Rule] = 0;
                            byRule[t.Rule]++;
                        }
                    }
            List<string> keys = new List<string>(byRule.Keys);
            keys.Sort();
            foreach (string k in keys) TestContext.WriteLine("    " + k + ": " + byRule[k]);
            TestContext.WriteLine("논리 " + logic + " · 정서 " + feeling);

            Assert.That(logic, Is.GreaterThan(0), "논리 규칙이 거부를 한 번도 만들지 않았다");
            Assert.That(feeling, Is.GreaterThan(0), "정서 규칙이 거부를 한 번도 만들지 않았다");
            foreach (string r in RejectionAudit.EngineRules())
                Assert.That(byRule.ContainsKey(r), Is.True,
                            r + " 규칙이 한 번도 거부를 만들지 않았다 — 데이터에서 그 규칙이 죽어 있다");
        }

        /// <summary>
        /// 규칙이 켜진 세계에서 **자리가 남아 있어야 한다.** 받아들여지는 선택이 하나도 없으면
        /// 성공률 비교가 「아무도 못 한다」를 재는 것이 되고, 반대로 다 받아들여지면 규칙이 없는 것과 같다.
        /// </summary>
        [Test]
        public void 받아들여지는_자리가_있고_동시에_드물다()
        {
            foreach (CommissionDef c in TestWorld.Data.AllCommissions)
            {
                DreamSession s = DreamSession.Open(TestWorld.Data, c.id, 0);
                List<string> anchors = new List<string>();
                foreach (MemoryDef m in s.Net.Memories) anchors.Add(m.id);
                List<GraftChoice> all = GraftRules.LegalChoices(c, anchors, TestWorld.Data.Balance.dayStep);
                int ok = 0;
                foreach (GraftChoice ch in all) if (s.Judge(ch).Accepted) ok++;
                int percent = ok * 100 / all.Count;
                TestContext.WriteLine(c.id + ": " + ok + "/" + all.Count + " (" + percent + "%) 가 자리를 잡는다");
                Assert.That(ok, Is.GreaterThan(0), c.id + ": 받아들여지는 자리가 하나도 없다 — 풀 수 없는 의뢰다");
                Assert.That(percent, Is.LessThan(25),
                            c.id + ": 선택의 " + percent + "%가 통과한다 — 아무렇게나 골라도 되는 의뢰다");
            }
        }
    }
}
