using System.Collections.Generic;
using System.Text;
using Interp.Data;
using Interp.Sim;
using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// 판정하지 않고 **보여 준다.** 데이터를 고칠 때 여기 표를 먼저 본다.
    /// 검사기가 아니므로 이 파일에는 Assert 가 거의 없다.
    /// </summary>
    [TestFixture]
    public class DiagnosticTests
    {
        [Test]
        public void PrintPolicyTable()
        {
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 정책 × 씨드 ────────────────────────────────────────────────");
            sb.AppendLine("정책            씨드     조인 긴장 의심 남은오해 드러남 favorA favorB 결말");
            foreach (IPolicy p in Policies.All())
            {
                foreach (int seed in d.AllSeeds())
                {
                    SessionResult r = SessionSim.Run(d, seed, p);
                    sb.AppendLine(string.Format("{0,-14} {1,6}  {2,4} {3,4} {4,4} {5,6} {6,6} {7,6} {8,6}  {9}",
                        p.Id, seed, r.Signed ? "O" : "X", r.Tension, r.Suspicion,
                        r.Standing.Count, r.Exposed.Count, r.FavorA, r.FavorB, r.EndingId));
                }
            }
            sb.AppendLine();
            sb.AppendLine("── 목표별 합계 (씨드 " + d.AllSeeds().Count + "개) ─────────────────────────");
            sb.Append(string.Format("{0,-14}", "정책"));
            foreach (string o in Objectives.AllIds(d)) sb.Append(string.Format("{0,12}", o));
            sb.AppendLine();
            foreach (IPolicy p in Policies.All())
            {
                sb.Append(string.Format("{0,-14}", p.Id));
                foreach (string o in Objectives.AllIds(d))
                    sb.Append(string.Format("{0,12}", Objectives.TotalOverSeeds(d, o, p)));
                sb.AppendLine();
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.Pass();
        }

        [Test]
        public void PrintExactTrace()
        {
            GameData d = TestWorld.Data;
            SessionResult r = SessionSim.Run(d, d.Balance.seed, Policies.Exact);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 늘 정확하게 옮긴 밤 (씨드 " + d.Balance.seed + ") ──────────────");
            foreach (TraceStep s in r.Trace)
                sb.AppendLine(string.Format("{0,2} {1,-16} {2,-9} 긴장{3,3} 하란{4,3} 케리아{5,3} 의심{6,3} {7}{8}",
                    s.StageIndex, s.StageId, s.RenderingId, s.TensionAfter, s.TrustAAfter, s.TrustBAfter,
                    s.SuspicionAfter,
                    string.IsNullOrEmpty(s.SetVariant) ? "" : "→" + s.SetVariant,
                    string.IsNullOrEmpty(s.ExposedMisread) ? "" : " ✱드러남:" + s.ExposedMisread));
            sb.AppendLine("조약: " + r.TreatySignature(d.Clauses));
            sb.AppendLine("결말: " + r.EndingId + " · 남은 오해 " + string.Join(",", r.Standing));
            TestContext.Out.WriteLine(sb.ToString());
            Assert.Pass();
        }
    }
}
