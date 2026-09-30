using System.Collections.Generic;
using System.Text;
using Graft.Data;
using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// 공통 검사기 `SeedDeterminism` — **나머지 전부의 전제다.**
    ///
    /// 같은 씨드가 같은 결과를 내지 않으면 성공률 비교(ConsistencyIsNotTrivial)와
    /// 감사(RejectionIsPredictable)가 전부 뜻을 잃는다. 그래서 이 파일이 먼저 통과해야 한다.
    ///
    /// 뿌리 CLAUDE.md 설계 원칙 5: 난수는 System.Random 하나뿐이고 씨드는 데이터에서 온다.
    /// 시각 · 배율 · 떨림이 전부 정수이므로(원칙 4) 재현에 부동소수 오차가 끼어들 자리가 없다.
    /// </summary>
    [TestFixture]
    public sealed class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드가_같은_스크린_기억을_뽑는다()
        {
            foreach (string cid in TestWorld.CommissionIds())
                for (int trial = 0; trial < 12; trial++)
                {
                    DreamSession a = DreamSession.Open(TestWorld.Data, cid, trial);
                    DreamSession b = DreamSession.Open(TestWorld.Data, cid, trial);
                    Assert.That(Join(b.Distorted), Is.EqualTo(Join(a.Distorted)),
                                cid + " 시행 " + trial + ": 같은 씨드가 다른 스크린 기억을 뽑았다");
                    Assert.That(b.Seed, Is.EqualTo(a.Seed));
                }
        }

        [Test]
        public void 다른_씨드는_실제로_다른_밤을_만든다()
        {
            // 씨드가 아무것도 바꾸지 않으면 씨드 예순 개를 도는 것이 시행 하나를 예순 번 도는 것과 같다.
            foreach (string cid in TestWorld.CommissionIds())
            {
                HashSet<string> seen = new HashSet<string>();
                for (int trial = 0; trial < TestWorld.Data.Balance.trialSeeds; trial++)
                    seen.Add(Join(DreamSession.Open(TestWorld.Data, cid, trial).Distorted));
                Assert.That(seen.Count, Is.GreaterThan(1),
                            cid + ": 씨드를 예순 개 돌렸는데 밤이 한 가지뿐이다");
            }
        }

        [Test]
        public void 같은_씨드가_같은_시도를_만든다()
        {
            foreach (string cid in TestWorld.CommissionIds())
                foreach (string p in TestWorld.PolicyNames())
                    for (int trial = 0; trial < 6; trial++)
                    {
                        string one = Fingerprint(cid, p, trial);
                        string two = Fingerprint(cid, p, trial);
                        Assert.That(two, Is.EqualTo(one),
                                    cid + "/" + p + " 시행 " + trial + ": 같은 씨드가 다른 시도를 만들었다");
                    }
        }

        [Test]
        public void 회차_전체를_두_번_돌리면_지문이_같다()
        {
            StringBuilder first = new StringBuilder();
            foreach (string cid in TestWorld.CommissionIds())
                foreach (Attempt a in TestWorld.Run(cid, "Careful").Attempts)
                    first.Append(Line(a));

            // 캐시를 거치지 않고 처음부터 다시 만든다
            StringBuilder again = new StringBuilder();
            foreach (string cid in TestWorld.CommissionIds())
                for (int trial = 0; trial < TestWorld.Data.Balance.trialSeeds; trial++)
                {
                    DreamSession s = DreamSession.Open(TestWorld.Data, cid, trial);
                    again.Append(Line(Policies.Careful(s, trial)));
                }

            Assert.That(again.ToString(), Is.EqualTo(first.ToString()),
                        "같은 데이터를 두 번 돌렸는데 회차 지문이 달라졌다");
        }

        [Test]
        public void 자기_기억_훑기가_결정적이다()
        {
            foreach (string sid in new[] { "subj-operator", "subj-hanwoo" })
            {
                string a = SelfFingerprint(sid);
                string b = SelfFingerprint(sid);
                Assert.That(b, Is.EqualTo(a), sid + ": 같은 데이터에서 훑기 결과가 달라졌다");
            }
        }

        private static string Fingerprint(string cid, string policy, int trial)
        {
            DreamSession s = DreamSession.Open(TestWorld.Data, cid, trial);
            Attempt a = Call(policy, s, trial);
            return Line(a);
        }

        private static Attempt Call(string policy, DreamSession s, int trial)
        {
            CommissionDef c = s.Commission;
            switch (policy)
            {
                case "Careful": return Policies.Careful(s, trial);
                case "Reckless": return Policies.Reckless(s, trial);
                case "ProbeEverything": return Policies.ProbeEverything(s, trial);
                case "LiarTrusting": return Policies.LiarTrusting(s, trial);
                case "TrustDistorted": return Policies.TrustDistorted(s, trial);
                case "FixedFirstMood": return Policies.Fixed(s, trial, policy, c.allowedMoodIds[0], -1, null);
                case "FixedMaxIntensity": return Policies.Fixed(s, trial, policy, null, c.intensityMax, null);
                case "FixedEarliestDay": return Policies.Fixed(s, trial, policy, null, -1, "earliest");
                default: throw new KeyNotFoundException(policy);
            }
        }

        private static string Line(Attempt a)
        {
            return a.CommissionId + "|" + a.Trial + "|" + a.Choice + "|" + a.Truth.TotalTremor
                   + "|" + (a.Accepted ? "1" : "0") + "|" + a.Knowledge.LuciditySpent
                   + "|" + a.Truth.Tremors.Count + ";";
        }

        private static string SelfFingerprint(string subjectId)
        {
            StringBuilder sb = new StringBuilder();
            foreach (SelfSignature s in SelfMemory.Scan(TestWorld.Data, TestWorld.Data.Subject(subjectId)))
                sb.Append(s.MemoryId).Append(':').Append(s.Residual).Append('/').Append(s.Secondary)
                  .Append('/').Append(s.TrembleEdges).Append(s.Suspect ? "!" : "").Append(';');
            return sb.ToString();
        }

        private static string Join(IList<string> xs)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string x in xs) sb.Append(x).Append(',');
            return sb.ToString();
        }
    }
}
