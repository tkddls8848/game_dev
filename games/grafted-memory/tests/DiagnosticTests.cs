using System.Collections.Generic;
using Graft.Data;
using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// 판정하지 않고 **수치만 찍는다.** 검사기가 실패했을 때 기준값을 내리는 대신
    /// 데이터를 고쳐 다시 돌리려면 무엇이 얼마인지부터 보여야 한다.
    /// </summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 정책별_성공률()
        {
            TestContext.WriteLine("씨드 " + TestWorld.Data.Balance.trialSeeds + "개 · 의뢰 " + TestWorld.CommissionIds().Count + "개");
            TestContext.WriteLine("");
            TestContext.WriteLine("정책                의뢰                 성공/전체  성공률  예측빗나감  명료도합");
            foreach (string p in TestWorld.PolicyNames())
            {
                foreach (string cid in TestWorld.CommissionIds())
                {
                    PolicyRun r = TestWorld.Run(cid, p);
                    TestContext.WriteLine(Pad(p, 20) + Pad(cid, 21) + Pad(r.Accepted + "/" + r.Total, 11)
                                          + Pad(r.RatePercent + "%", 8) + Pad(r.Surprises.ToString(), 12) + r.LuciditySpent);
                }
                TestContext.WriteLine(Pad(p, 20) + Pad("(전체)", 21) + Pad("", 11) + TestWorld.OverallRate(p) + "%");
                TestContext.WriteLine("");
            }
        }

        [Test]
        public void 두_세계_성공률()
        {
            TestContext.WriteLine("규칙 켠 세계 / 끈 세계");
            foreach (string p in new[] { "Careful", "Reckless" })
                TestContext.WriteLine(Pad(p, 16) + "on " + TestWorld.OverallRate(p, true) + "%   off "
                                      + TestWorld.OverallRate(p, false) + "%");
        }

        [Test]
        public void 감사_분포()
        {
            Dictionary<string, int> tally = new Dictionary<string, int>();
            int rejected = 0, total = 0;
            foreach (string p in TestWorld.PolicyNames())
                foreach (string cid in TestWorld.CommissionIds())
                    foreach (Attempt a in TestWorld.Run(cid, p).Attempts)
                    {
                        total++;
                        if (!a.Accepted) rejected++;
                        RejectionVerdict v = RejectionAudit.Audit(a);
                        foreach (RejectFact f in v.Facts)
                        {
                            if (!tally.ContainsKey(f.Verdict)) tally[f.Verdict] = 0;
                            tally[f.Verdict]++;
                        }
                    }
            TestContext.WriteLine("시도 " + total + " · 거부 " + rejected);
            List<string> keys = new List<string>(tally.Keys);
            keys.Sort();
            foreach (string k in keys) TestContext.WriteLine(Pad(k, 26) + tally[k]);
        }

        [Test]
        public void 정적_구멍()
        {
            foreach (CommissionDef c in TestWorld.Data.AllCommissions)
            {
                List<string> gaps = RejectionAudit.StaticGaps(TestWorld.Data, c);
                TestContext.WriteLine(c.id + ": 구멍 " + gaps.Count);
                foreach (string g in gaps) TestContext.WriteLine("    " + g);
            }
        }

        [Test]
        public void 자기_기억_잔여떨림()
        {
            foreach (string sid in new[] { "subj-operator", "subj-hanwoo" })
            {
                List<SelfSignature> all = SelfMemory.Scan(TestWorld.Data, TestWorld.Data.Subject(sid));
                TestContext.WriteLine("── " + sid + " (중앙값 " + SelfMemory.MedianSecondary(all) + ")");
                foreach (SelfSignature s in all)
                    TestContext.WriteLine("    " + Pad(s.MemoryId, 24) + Pad(s.Residual.ToString(), 8) + Pad("둘째 " + s.Secondary, 14)
                                          + "간선 " + s.TrembleEdges
                                          + (s.Suspect ? "  <의심>" : "")
                                          + (s.ActuallyGrafted ? "  [심어진 것]" : ""));
                TestContext.WriteLine("");
            }
        }

        [Test]
        public void 한_의뢰의_선택지_분포()
        {
            foreach (CommissionDef c in TestWorld.Data.AllCommissions)
            {
                DreamSession s = DreamSession.Open(TestWorld.Data, c.id, 0);
                List<GraftChoice> all = GraftRules.LegalChoices(c, AllIds(s.Net), TestWorld.Data.Balance.dayStep);
                int ok = 0, blatant = 0, best = int.MaxValue;
                foreach (GraftChoice ch in all)
                {
                    GraftVerdict v = s.Judge(ch);
                    if (v.Accepted) ok++;
                    if (v.Blatant) blatant++;
                    if (v.TotalTremor < best) best = v.TotalTremor;
                }
                TestContext.WriteLine(Pad(c.id, 22) + "선택지 " + Pad(all.Count.ToString(), 7)
                                      + "받아들임 " + Pad(ok.ToString(), 7)
                                      + "노골적 " + Pad(blatant.ToString(), 7)
                                      + "최소떨림 " + best + " (허용 " + c.toleranceBudget + ")");
            }
        }

        private static List<string> AllIds(MemoryNet net)
        {
            List<string> ids = new List<string>();
            foreach (MemoryDef m in net.Memories) ids.Add(m.id);
            return ids;
        }

        private static string Pad(string s, int n)
        {
            while (s.Length < n) s += " ";
            return s;
        }
    }
}
