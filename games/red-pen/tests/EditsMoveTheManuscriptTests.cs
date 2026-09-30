using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RedPen.Sim;

namespace RedPen.Tests
{
    /// <summary>
    /// ★ 핵 검사기 1 — **「교정이 원고를 움직인다」**
    ///
    /// 같은 원고가 **다른 교정 방침** 아래 측정 가능하게 다른 상태로 끝나는가
    /// (원고 품질 · 작가 상태 · 걸작 도달 여부). 같으면 교정이 장식이다.
    ///
    /// 두 세계 비교로 이 검사기에 이가 있음을 함께 보인다:
    ///   · 켠 세계 — 붉은 펜이 원고와 작가에 닿는다
    ///   · 끈 세계 — 표시는 남지만 원고도 작가도 달라지지 않는다(작가가 전부 무시한다)
    /// 끈 세계에서 방침 사이에 차이가 나면 이 검사기는 붉은 펜이 아닌 다른 것을 재고 있는 것이다.
    /// </summary>
    [TestFixture]
    public class EditsMoveTheManuscriptTests
    {
        private GameData D { get { return TestWorld.Data; } }

        private IList<IPolicy> Everything()
        {
            List<IPolicy> all = new List<IPolicy>(Policies.All());
            all.AddRange(Policies.Singles(D));
            return all;
        }

        private sealed class Spread
        {
            public int Quality, Voice, Confidence, Trust, Masterpiece, Lost, Endings;
            public override string ToString()
            {
                return "질 " + Quality + " · 목소리 " + Voice + " · 자신감 " + Confidence
                     + " · 신뢰 " + Trust + " · 걸작 " + Masterpiece + " · 지운 문장 " + Lost
                     + " · 결말 " + Endings + "가지";
            }
        }

        private Spread Measure(bool editsMatter)
        {
            int qMin = int.MaxValue, qMax = int.MinValue, vMin = int.MaxValue, vMax = int.MinValue;
            int cMin = int.MaxValue, cMax = int.MinValue, tMin = int.MaxValue, tMax = int.MinValue;
            int mMin = int.MaxValue, mMax = int.MinValue, lMin = int.MaxValue, lMax = int.MinValue;
            HashSet<string> endings = new HashSet<string>();
            foreach (IPolicy p in Everything())
            {
                int q = 0, v = 0, c = 0, t = 0, m = 0, l = 0;
                foreach (int seed in D.AllSeeds())
                {
                    RunResult r = ManuscriptSim.Run(D, seed, p, editsMatter);
                    q += r.Quality; v += r.Voice; c += r.Confidence; t += r.Trust;
                    m += r.Masterpieces; l += r.LostSentences;
                    endings.Add(r.EndingId);
                }
                int n = D.AllSeeds().Count;
                q /= n; v /= n; c /= n; t /= n;
                qMin = Math.Min(qMin, q); qMax = Math.Max(qMax, q);
                vMin = Math.Min(vMin, v); vMax = Math.Max(vMax, v);
                cMin = Math.Min(cMin, c); cMax = Math.Max(cMax, c);
                tMin = Math.Min(tMin, t); tMax = Math.Max(tMax, t);
                mMin = Math.Min(mMin, m); mMax = Math.Max(mMax, m);
                lMin = Math.Min(lMin, l); lMax = Math.Max(lMax, l);
            }
            return new Spread
            {
                Quality = qMax - qMin, Voice = vMax - vMin, Confidence = cMax - cMin,
                Trust = tMax - tMin, Masterpiece = mMax - mMin, Lost = lMax - lMin,
                Endings = endings.Count
            };
        }

        [Test]
        public void EditsMoveTheManuscript()
        {
            Spread on = Measure(true);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 같은 원고, 다른 붉은 펜 (방침 " + Everything().Count
                          + "가지 × 씨드 " + D.AllSeeds().Count + "개) ──");
            sb.AppendLine("방침        평균질 평균목소리 자신감 신뢰 걸작합 지움합 결말");
            foreach (IPolicy p in Everything())
            {
                int q = 0, v = 0, c = 0, t = 0, m = 0, l = 0;
                HashSet<string> es = new HashSet<string>();
                foreach (int seed in D.AllSeeds())
                {
                    RunResult r = ManuscriptSim.Run(D, seed, p);
                    q += r.Quality; v += r.Voice; c += r.Confidence; t += r.Trust;
                    m += r.Masterpieces; l += r.LostSentences; es.Add(r.EndingId);
                }
                int n = D.AllSeeds().Count;
                sb.AppendLine(string.Format("{0,-11}{1,6}{2,10}{3,7}{4,6}{5,7}{6,7}  {7}",
                    p.Id, q / n, v / n, c / n, t / n, m, l, string.Join(",", es)));
            }
            sb.AppendLine("★ 벌어진 폭 — " + on);
            TestContext.Out.WriteLine(sb.ToString());

            Assert.That(on.Quality, Is.GreaterThanOrEqualTo(30),
                "가장 좋은 펜과 가장 나쁜 펜의 원고 품질 차이가 " + on.Quality + "뿐이다");
            Assert.That(on.Voice, Is.GreaterThanOrEqualTo(40),
                "목소리 차이가 " + on.Voice + "뿐이다 — 붉은 펜이 목소리를 건드리지 못한다");
            Assert.That(on.Confidence, Is.GreaterThanOrEqualTo(40),
                "작가의 자신감 차이가 " + on.Confidence + "뿐이다");
            Assert.That(on.Trust, Is.GreaterThanOrEqualTo(40),
                "작가의 신뢰 차이가 " + on.Trust + "뿐이다");
            Assert.That(on.Masterpiece, Is.GreaterThanOrEqualTo(3),
                "걸작에 닿는 펜과 닿지 못하는 펜의 차이가 " + on.Masterpiece + "뿐이다");
            Assert.That(on.Endings, Is.GreaterThanOrEqualTo(5),
                "닿는 결말이 " + on.Endings + "가지뿐이다");
        }

        /// <summary>
        /// 대조군. 붉은 펜이 닿지 않는 세계에서는 **아무 방침도 서로 다르지 않아야 한다.**
        /// 여기서 0이 아니면 위 숫자는 붉은 펜이 아니라 다른 것을 잰 것이다.
        /// </summary>
        [Test]
        public void ControlWorldWhereTheRedPenIsDecorationChangesNothing()
        {
            Spread off = Measure(false);
            TestContext.Out.WriteLine("붉은 펜이 장식인 세계에서 벌어진 폭 — " + off);
            Assert.That(off.Quality, Is.Zero, "장식인 세계에서 원고 품질이 갈렸다");
            Assert.That(off.Voice, Is.Zero, "장식인 세계에서 목소리가 갈렸다");
            Assert.That(off.Confidence, Is.Zero, "장식인 세계에서 작가의 자신감이 갈렸다");
            Assert.That(off.Trust, Is.Zero, "장식인 세계에서 작가의 신뢰가 갈렸다");
            Assert.That(off.Masterpiece, Is.Zero, "장식인 세계에서 걸작이 갈렸다");
            Assert.That(off.Endings, Is.EqualTo(1), "장식인 세계에서 결말이 갈렸다");
        }

        /// <summary>
        /// **문장 하나하나가 실제로 달라져 있어야 한다.** 평균만 갈리고 본문이 같으면
        /// 그건 원고가 움직인 것이 아니라 점수판이 움직인 것이다.
        /// </summary>
        [Test]
        public void IndividualSentencesEndUpInDifferentStates()
        {
            int seed = D.Balance.seed;
            Dictionary<string, HashSet<string>> perSentence = new Dictionary<string, HashSet<string>>();
            foreach (IPolicy p in Everything())
            {
                RunResult r = ManuscriptSim.Run(D, seed, p);
                foreach (LiveSentence s in r.Sentences)
                {
                    if (s.IsMasterpiece) continue;
                    HashSet<string> set;
                    if (!perSentence.TryGetValue(s.Id, out set)) { set = new HashSet<string>(); perSentence[s.Id] = set; }
                    set.Add((s.Deleted ? "X" : "O") + s.Quality + "/" + s.Voice + "[" + string.Join(",", s.Flaws) + "]");
                }
            }
            int moved = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 문장마다 끝난 자리가 몇 가지인가 (씨드 " + seed + ") ──");
            foreach (KeyValuePair<string, HashSet<string>> kv in perSentence)
            {
                sb.AppendLine("  " + kv.Key + " — " + kv.Value.Count + "가지");
                if (kv.Value.Count >= 4) moved++;
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(moved, Is.GreaterThanOrEqualTo(6),
                "네 가지 이상으로 갈리는 문장이 " + moved + "개뿐이다 — 본문이 움직이지 않는다");
        }

        /// <summary>
        /// **고친 흔적이 쌓여야 한다.** 회차마다 표시가 누적되고, 방침에 따라
        /// 원고가 얼마나 붉어졌는지가 달라야 한다 — 그 붉기가 이 관계의 역사다.
        /// </summary>
        [Test]
        public void TheRednessAccumulatesAndDiffersByPen()
        {
            int min = int.MaxValue, max = int.MinValue;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 원고가 얼마나 붉어졌나 (씨드 " + D.Balance.seed + ") ──");
            foreach (IPolicy p in Everything())
            {
                RunResult r = ManuscriptSim.Run(D, D.Balance.seed, p);
                int marks = 0;
                foreach (LiveSentence s in r.Sentences) marks += s.MarkHistory.Count;
                sb.AppendLine(string.Format("{0,-11} 붉기 {1,4} · 얹힌 표시 {2,3}개", p.Id, r.Redness, marks));
                min = Math.Min(min, r.Redness); max = Math.Max(max, r.Redness);
            }
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(max - min, Is.GreaterThanOrEqualTo(80),
                "가장 붉은 원고와 가장 흰 원고의 차이가 " + (max - min) + "뿐이다");

            // 한 문장에 회차를 건너 표시가 쌓이는가
            RunResult harsh = ManuscriptSim.Run(D, D.Balance.seed, Policies.HarshPen);
            int layered = 0;
            foreach (LiveSentence s in harsh.Sentences)
            {
                HashSet<int> rounds = new HashSet<int>(s.MarkRound);
                if (rounds.Count >= 2) layered++;
            }
            Assert.That(layered, Is.GreaterThanOrEqualTo(3),
                "회차를 건너 표시가 쌓인 문장이 " + layered + "개뿐이다 — 오간 흔적이 남지 않는다");
        }
    }
}
