using System.Collections.Generic;
using System.Text;
using RedPen.Data;
using RedPen.Sim;
using NUnit.Framework;

namespace RedPen.Tests
{
    /// <summary>
    /// 판정하지 않고 **보여 준다.** 데이터를 고칠 때 여기 표를 먼저 본다.
    /// 검사기가 아니므로 이 파일에는 Assert 가 거의 없다.
    /// </summary>
    [TestFixture]
    public class DiagnosticTests
    {
        private GameData D { get { return TestWorld.Data; } }

        [Test]
        public void PrintPolicyTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 방침 × 씨드 ────────────────────────────────────────────────");
            sb.AppendLine("방침        씨드      질  목소리 자신감 신뢰 고집 걸작 지움 붉기 실을수 결말");
            List<IPolicy> all = new List<IPolicy>(Policies.All());
            all.AddRange(Policies.Singles(D));
            foreach (IPolicy p in all)
                foreach (int seed in D.AllSeeds())
                {
                    RunResult r = ManuscriptSim.Run(D, seed, p);
                    sb.AppendLine(string.Format("{0,-11} {1,7} {2,5} {3,5} {4,5} {5,5} {6,4} {7,4} {8,4} {9,5} {10,5}  {11}",
                        p.Id, seed, r.Quality, r.Voice, r.Confidence, r.Trust, r.Stubborn,
                        r.Masterpieces, r.LostSentences, r.Redness, r.Publishable ? "O" : "X", r.EndingId));
                }
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void PrintObjectiveTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 자별 합계 (씨드 " + D.AllSeeds().Count + "개) ───────────────────────────");
            List<IPolicy> all = new List<IPolicy>(Policies.All());
            all.AddRange(Policies.Singles(D));
            sb.Append(string.Format("{0,-12}", "방침"));
            foreach (string o in Objectives.AllIds(D)) sb.Append(string.Format("{0,15}", o));
            sb.AppendLine();
            foreach (IPolicy p in all)
            {
                sb.Append(string.Format("{0,-12}", p.Id));
                foreach (string o in Objectives.AllIds(D))
                    sb.Append(string.Format("{0,15}", Objectives.TotalOverSeeds(D, o, p)));
                sb.AppendLine();
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        [TestCase("p_harsh")]
        [TestCase("p_gentle")]
        [TestCase("p_middle")]
        public void PrintTrace(string which)
        {
            IPolicy pol = which == "p_harsh" ? Policies.HarshPen
                        : which == "p_gentle" ? Policies.GentlePen : Policies.MiddlePen;
            RunResult r = ManuscriptSim.Run(D, D.Balance.seed, pol);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── " + pol.Name + " (씨드 " + D.Balance.seed + ") ─────────────────");
            foreach (RoundLog l in r.Rounds)
                sb.AppendLine(string.Format("{0}회차 혹독 {1,3} 자신감 {2,3} 신뢰 {3,3} 고집 {4,3} 질 {5,3} 목소리 {6,3} [{7}]{8} 버팀 {9}",
                    l.Round + 1, l.Harshness, l.ConfidenceAfter, l.TrustAfter, l.StubbornAfter,
                    l.QualityAfter, l.VoiceAfter, l.ReviseBand, l.Masterpiece ? " ★걸작" : "", l.Refused.Count + "/헛" + l.Wasted.Count));
            foreach (LiveSentence s in r.Sentences)
                sb.AppendLine(string.Format("  {0,-10} {1} q{2,3} v{3,3} [{4}] 표시 {5}",
                    s.Id, s.Deleted ? "지움" : "남음", s.Quality, s.Voice,
                    string.Join(",", s.Flaws), string.Join(" ", s.MarkHistory)));
            sb.AppendLine("결말 " + r.EndingId);
            TestContext.Out.WriteLine(sb.ToString());
        }
    }
}
