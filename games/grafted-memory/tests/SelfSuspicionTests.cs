using System.Collections.Generic;
using Graft.Data;
using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// **마지막 장면을 기계가 판정하는 자리.**
    ///
    /// 「플레이어 자신의 기억도 누가 심은 것인지 의심하게 된다」를 연출 문구로 두지 않았다.
    /// 의뢰에 쓰는 것과 **똑같은 규칙**으로 제 기억망을 훑는다. 남을 재던 자가 자기를 재는 자가 된다.
    ///
    /// 여기서 정직하게 적어야 하는 것이 하나 있다 — **검사기는 자리를 셋 찾고 심어진 것은 둘이다.**
    /// 심은 기억은 이웃까지 떨게 만들기 때문에 「이 근처에 심어진 것이 있다」까지만 판정된다.
    /// 어느 것인지는 기계가 말해 주지 않는다. 그것을 통과 조건에서 빼는 대신 **수치로 적어 둔다.**
    /// </summary>
    [TestFixture]
    public sealed class SelfSuspicionTests
    {
        private static List<SelfSignature> Scan(string subjectId)
        {
            return SelfMemory.Scan(TestWorld.Data, TestWorld.Data.Subject(subjectId));
        }

        [Test]
        public void 심어진_기억을_하나도_놓치지_않는다()
        {
            List<SelfSignature> all = Scan("subj-operator");
            List<string> missed = new List<string>();
            foreach (SelfSignature s in all)
                if (s.ActuallyGrafted && !s.Suspect)
                    missed.Add(s.MemoryId + "(둘째 " + s.Secondary + " · 간선 " + s.TrembleEdges + ")");

            foreach (SelfSignature s in all)
                TestContext.WriteLine("    " + s.MemoryId + "  잔여 " + s.Residual
                                      + " · 흔적 " + s.SignatureResidual + " · 둘째 " + s.Secondary
                                      + " · 간선 " + s.TrembleEdges
                                      + (s.Suspect ? "  <의심>" : "") + (s.ActuallyGrafted ? "  [심어진 것]" : ""));

            Assert.That(missed, Is.Empty, "심어진 기억을 놓쳤다: " + string.Join(" ", missed));
        }

        /// <summary>
        /// **음성 대조군.** 심어진 것이 없는 기억망(의뢰인의 것)에서는 하나도 걸리지 않아야 한다.
        /// 걸리면 이 검사기는 「떠는 자리」가 아니라 「아무 자리」를 찾고 있는 것이다.
        /// </summary>
        [Test]
        public void 심어진_것이_없는_기억망에서는_하나도_걸리지_않는다()
        {
            List<SelfSignature> all = Scan("subj-hanwoo");
            List<string> wrong = new List<string>();
            int maxSecondary = 0;
            foreach (SelfSignature s in all)
            {
                if (s.Secondary > maxSecondary) maxSecondary = s.Secondary;
                if (s.Suspect) wrong.Add(s.MemoryId + "(둘째 " + s.Secondary + ")");
            }
            TestContext.WriteLine("의뢰인 기억망 " + all.Count + "자리 · 둘째 떨림 최대 " + maxSecondary
                                  + " · 문턱 " + TestWorld.Data.Balance.selfSignature.secondaryFloor);
            Assert.That(wrong, Is.Empty, "심어진 것이 없는데 의심으로 걸렸다: " + string.Join(" ", wrong));
        }

        [Test]
        public void 문턱이_두_기억망_사이에_실제로_들어간다()
        {
            int clientMax = 0;
            foreach (SelfSignature s in Scan("subj-hanwoo"))
                if (s.Secondary > clientMax) clientMax = s.Secondary;

            int graftMin = int.MaxValue;
            foreach (SelfSignature s in Scan("subj-operator"))
                if (s.ActuallyGrafted && s.Secondary < graftMin) graftMin = s.Secondary;

            int floor = TestWorld.Data.Balance.selfSignature.secondaryFloor;
            TestContext.WriteLine("의뢰인 최대 " + clientMax + " < 문턱 " + floor + " <= 심어진 것 최소 " + graftMin);
            Assert.That(clientMax, Is.LessThan(floor), "문턱이 의뢰인 기억망의 잡음보다 낮다");
            Assert.That(graftMin, Is.GreaterThanOrEqualTo(floor), "문턱이 심어진 기억의 흔적보다 높다");
            Assert.That(graftMin - clientMax, Is.GreaterThan(1000),
                        "두 기억망 사이의 틈이 " + (graftMin - clientMax) + " 뿐이다 — 문턱 하나 옮기면 판정이 뒤집힌다");
        }

        /// <summary>
        /// 떠는 자리가 **심어진 것과 그 이웃 안에 머문다.** 엉뚱한 자리가 떨면
        /// 검사기가 심은 흔적이 아니라 데이터의 잡음을 보고 있는 것이다.
        /// </summary>
        [Test]
        public void 떠는_자리가_심어진_것의_이웃_안에_머문다()
        {
            SubjectDef subj = TestWorld.Data.Subject("subj-operator");
            MemoryNet net = new MemoryNet(subj);

            HashSet<string> allowed = new HashSet<string>();
            foreach (MemoryDef m in subj.memories)
            {
                if (m.graftedOnDay < 0) continue;
                allowed.Add(m.id);
                foreach (string nb in net.NeighboursOf(m.id)) allowed.Add(nb);
            }

            List<string> stray = new List<string>();
            foreach (SelfSignature s in Scan("subj-operator"))
                if (s.Suspect && !allowed.Contains(s.MemoryId)) stray.Add(s.MemoryId);

            Assert.That(stray, Is.Empty,
                        "심어진 것도 아니고 그 이웃도 아닌 자리가 떨었다: " + string.Join(" ", stray));
        }

        /// <summary>
        /// **기계가 판정하지 못한 것을 수치로 남긴다.** 정확도를 통과 조건으로 걸지 않는다 —
        /// 이 컨셉에서는 「어느 것인지 모른다」가 결함이 아니라 끝맺음이기 때문이다.
        /// </summary>
        [Test]
        public void 정확도를_수치로_남긴다()
        {
            List<SelfSignature> all = Scan("subj-operator");
            int suspects = 0, grafted = 0, hits = 0;
            foreach (SelfSignature s in all)
            {
                if (s.Suspect) suspects++;
                if (s.ActuallyGrafted) grafted++;
                if (s.Suspect && s.ActuallyGrafted) hits++;
            }
            TestContext.WriteLine("떠는 자리 " + suspects + " · 심어진 것 " + grafted
                                  + " · 맞춘 것 " + hits
                                  + " → 재현율 " + (hits * 100 / grafted) + "% · 정확도 " + (hits * 100 / suspects) + "%");
            TestContext.WriteLine("정확도는 통과 조건이 아니다. 「이 근처에 심어진 것이 있다」까지가 기계의 몫이다.");
            Assert.That(hits, Is.EqualTo(grafted), "재현율이 100% 가 아니다");
            Assert.That(suspects, Is.LessThan(all.Count),
                        "기억망 전체가 떨면 아무것도 가리킨 것이 아니다");
        }

        /// <summary>
        /// **같은 자를 쓴다는 것의 증거.** 제 기억망을 훑을 때 나오는 이유의 이름이
        /// 의뢰에 쓰는 규칙표 안에 있어야 한다. 밖에 있으면 자가 두 개인 것이다.
        /// </summary>
        [Test]
        public void 자기_기억을_재는_이유도_같은_규칙표에_있다()
        {
            List<string> stated = new List<string>(TestWorld.Data.Balance.statedRules);
            int seen = 0;
            foreach (string sid in new[] { "subj-operator", "subj-hanwoo" })
                foreach (SelfSignature s in Scan(sid))
                    foreach (Tremor t in s.Tremors)
                    {
                        Assert.That(stated, Contains.Item(t.Rule),
                                    sid + "/" + s.MemoryId + ": 규칙표에 없는 이유로 떨었다: " + t.Rule);
                        seen++;
                    }
            Assert.That(seen, Is.GreaterThan(0), "제 기억망에서 떨림이 하나도 나오지 않았다");
            TestContext.WriteLine("제 기억망에서 나온 떨림 " + seen + "건 전부가 의뢰에 쓰는 규칙표 안에 있다");
        }
    }
}
