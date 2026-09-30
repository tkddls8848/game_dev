using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RedPen.Data;
using RedPen.Sim;

namespace RedPen.Tests
{
    /// <summary>
    /// ★ 핵 검사기 2 — **「혹독함은 양날이다」**
    ///
    /// 「늘 혹독」·「늘 관대」 어느 쪽도 지배하지 않는가.
    /// 한쪽이 이기면 작가의 감정 상태가 의미 없는 장식이라는 뜻이다.
    /// **두 극단과 중간 방침 하나 이상의 결과를 수치로 낸다.**
    /// </summary>
    [TestFixture]
    public class HarshnessCutsBothWaysTests
    {
        private GameData D { get { return TestWorld.Data; } }

        private Dictionary<string, Dictionary<string, int>> Table()
        {
            Dictionary<string, Dictionary<string, int>> t = new Dictionary<string, Dictionary<string, int>>();
            foreach (string oid in Objectives.AllIds(D))
            {
                Dictionary<string, int> row = new Dictionary<string, int>();
                foreach (IPolicy p in Policies.Three()) row[p.Id] = Objectives.TotalOverSeeds(D, oid, p);
                t[oid] = row;
            }
            return t;
        }

        private static string ArgMax(Dictionary<string, int> row)
        {
            string best = null;
            foreach (KeyValuePair<string, int> kv in row) if (best == null || kv.Value > row[best]) best = kv.Key;
            return best;
        }

        [Test]
        public void HarshnessCutsBothWays()
        {
            Dictionary<string, Dictionary<string, int>> t = Table();
            int harshWins = 0, gentleWins = 0, middleWins = 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 두 극단과 중간 (씨드 " + D.AllSeeds().Count + "개 합계) ──────────────────");
            sb.Append(string.Format("{0,-34}", "자 (누가 재는가)"));
            foreach (IPolicy p in Policies.Three()) sb.Append(string.Format("{0,12}", p.Id));
            sb.AppendLine("   이긴 쪽");
            foreach (string oid in Objectives.AllIds(D))
            {
                ObjectiveDef o = D.Objective(oid);
                sb.Append(string.Format("{0,-34}", oid + " (" + o.whose + ")"));
                foreach (IPolicy p in Policies.Three()) sb.Append(string.Format("{0,12}", t[oid][p.Id]));
                string w = ArgMax(t[oid]);
                sb.AppendLine("   " + w);
                if (w == "p_harsh") harshWins++;
                else if (w == "p_gentle") gentleWins++;
                else middleWins++;
            }
            TestContext.Out.WriteLine(sb.ToString());
            TestContext.Out.WriteLine(Localization.Text("audit.bothways",
                "혹독이 {0}개, 관대가 {1}개를 이겼고 어느 쪽도 전부를 가져가지 못했다", harshWins, gentleWins));

            // ★ 「양날」의 뜻은 **두 극단이 서로를 어디선가 이긴다**는 것이다.
            //   한쪽이 다른 쪽을 모든 자에서 이기면 그쪽이 그냥 정답이고, 작가의 감정은 장식이 된다.
            int harshOverGentle = 0, gentleOverHarsh = 0;
            foreach (string oid in Objectives.AllIds(D))
            {
                if (t[oid]["p_harsh"] > t[oid]["p_gentle"]) harshOverGentle++;
                else if (t[oid]["p_gentle"] > t[oid]["p_harsh"]) gentleOverHarsh++;
            }
            TestContext.Out.WriteLine("혹독이 관대를 이긴 자 " + harshOverGentle
                                      + " · 관대가 혹독을 이긴 자 " + gentleOverHarsh);

            int total = Objectives.AllIds(D).Count;
            Assert.That(harshOverGentle, Is.GreaterThanOrEqualTo(1),
                "「늘 혹독」이 「늘 관대」를 자 " + total + "개 어디에서도 이기지 못한다 — 혹독함이 그냥 손해다");
            Assert.That(gentleOverHarsh, Is.GreaterThanOrEqualTo(1),
                "「늘 관대」가 「늘 혹독」을 자 " + total + "개 어디에서도 이기지 못한다 — 다정함이 그냥 손해다");
            Assert.That(harshWins, Is.LessThan(total), "「늘 혹독」이 전부를 가져갔다 — 작가의 감정이 장식이다");
            Assert.That(gentleWins, Is.LessThan(total), "「늘 관대」가 전부를 가져갔다 — 붉은 펜이 장식이다");
            Assert.That(harshWins + gentleWins, Is.GreaterThanOrEqualTo(1),
                "두 극단 모두 중간 방침에 전부 졌다 — 극단을 고를 이유가 한 자에도 없다");
            Assert.That(middleWins, Is.GreaterThanOrEqualTo(1),
                "중간 방침이 어느 자에서도 이기지 못한다 — 섞을 이유가 없다");
        }

        /// <summary>
        /// 두 극단의 **모양이 서로 달라야** 한다. 혹독은 원고를 올리고 목소리를 지우며,
        /// 관대는 작가를 살리고 원고를 세운다. 방향이 같으면 자를 잘못 고른 것이다.
        /// </summary>
        [Test]
        public void TheTwoExtremesFailInOppositeDirections()
        {
            int hq = 0, hv = 0, hc = 0, gq = 0, gv = 0, gc = 0;
            foreach (int seed in D.AllSeeds())
            {
                RunResult h = ManuscriptSim.Run(D, seed, Policies.HarshPen);
                RunResult g = ManuscriptSim.Run(D, seed, Policies.GentlePen);
                hq += h.Quality; hv += h.Voice; hc += h.Confidence;
                gq += g.Quality; gv += g.Voice; gc += g.Confidence;
            }
            int n = D.AllSeeds().Count;
            TestContext.Out.WriteLine(string.Format(
                "늘 혹독 — 질 {0} 목소리 {1} 자신감 {2}   /   늘 관대 — 질 {3} 목소리 {4} 자신감 {5}",
                hq / n, hv / n, hc / n, gq / n, gv / n, gc / n));

            Assert.That(hq / n, Is.GreaterThan(gq / n + 5),
                "혹독한 펜이 원고를 더 좋게 만들지 못한다 — 혹독함에 값이 없다");
            Assert.That(gv / n, Is.GreaterThan(hv / n + 15),
                "다정한 펜이 목소리를 더 남기지 못한다 — 목소리가 장식이다");
            Assert.That(gc / n, Is.GreaterThan(hc / n + 15),
                "다정한 펜이 작가를 더 성하게 남기지 못한다 — 작가 상태가 장식이다");
        }

        /// <summary>
        /// **걸작은 어느 극단으로도 닿지 못해야 한다.** 네 문턱(자신감·신뢰·목소리·원고)을
        /// 한꺼번에 넘겨야 오는 것이라, 혹독은 앞의 셋에서 막히고 관대는 원고에서 막힌다.
        /// </summary>
        [Test]
        public void NeitherExtremeReachesTheMasterpiece()
        {
            int harsh = 0, gentle = 0, middle = 0;
            foreach (int seed in D.AllSeeds())
            {
                harsh += ManuscriptSim.Run(D, seed, Policies.HarshPen).Masterpieces;
                gentle += ManuscriptSim.Run(D, seed, Policies.GentlePen).Masterpieces;
                middle += ManuscriptSim.Run(D, seed, Policies.MiddlePen).Masterpieces;
            }
            TestContext.Out.WriteLine("걸작 — 늘 혹독 " + harsh + " · 늘 관대 " + gentle + " · 작가를 읽는다 " + middle
                                      + " (씨드 " + D.AllSeeds().Count + "개)");
            Assert.That(harsh, Is.Zero, "혹독하기만 해도 걸작이 온다 — 작가의 상태가 걸작의 조건이 아니라는 뜻이다");
            Assert.That(middle, Is.GreaterThan(gentle),
                "섞어 쓴 펜이 다정하기만 한 펜보다 걸작에 덜 닿는다 — 원고 문턱이 일하지 않는다");
            Assert.That(middle, Is.GreaterThan(0));
        }

        /// <summary>
        /// **작가가 버티는 일이 실제로 일어나야 한다.** 고집이 서면 거친 표시가 튕겨 나온다 —
        /// 그게 없으면 작가의 감정은 점수판일 뿐 규칙이 아니다.
        /// </summary>
        [Test]
        public void TheWriterActuallyDigsIn()
        {
            int refused = 0, withdrawn = 0, silenced = 0;
            List<IPolicy> all = new List<IPolicy>(Policies.All());
            all.AddRange(Policies.Singles(D));
            foreach (IPolicy p in all)
                foreach (int seed in D.AllSeeds())
                {
                    RunResult r = ManuscriptSim.Run(D, seed, p);
                    foreach (RoundLog l in r.Rounds) refused += l.Refused.Count;
                    if (r.Withdrawn) withdrawn++;
                    if (r.Silenced) silenced++;
                }
            TestContext.Out.WriteLine("작가가 버틴 표시 " + refused + "건 · 원고를 거둬 간 회차 " + withdrawn
                                      + " · 더 쓰지 못한 회차 " + silenced);
            Assert.That(refused, Is.GreaterThan(0), "작가가 한 번도 버티지 않는다 — 고집이 장식이다");
            Assert.That(withdrawn + silenced, Is.GreaterThan(0), "작가가 무너지는 길이 하나도 없다");
        }

        /// <summary>
        /// **다정함도 공짜가 아니어야 한다.** 관대하기만 하면 원고가 실을 수준에 못 미치는 밤이 있어야 한다.
        /// </summary>
        [Test]
        public void BeingKindAloneIsNotEnough()
        {
            int notPublishable = 0;
            foreach (int seed in D.AllSeeds())
                if (!ManuscriptSim.Run(D, seed, Policies.GentlePen).Publishable) notPublishable++;
            TestContext.Out.WriteLine("「늘 관대」가 실을 수 없었던 밤 " + notPublishable + " / " + D.AllSeeds().Count);
            Assert.That(notPublishable, Is.GreaterThan(0), "다정하기만 해도 늘 실을 수 있다 — 붉은 펜이 필요 없다");
            Assert.That(notPublishable, Is.LessThan(D.AllSeeds().Count), "다정하면 반드시 실패한다 — 그것도 답이 하나인 것이다");
        }
    }
}
