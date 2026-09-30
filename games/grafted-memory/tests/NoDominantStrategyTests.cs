using System.Collections.Generic;
using Graft.Data;
using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// 공통 검사기 `NoDominantStrategy` — **한 가지 수를 반복하는 것이 최적이 아닌가.**
    ///
    /// 두 갈래로 본다.
    ///   · 정책 비교: 한 축을 굳힌 정책은 Careful 을 따라오지 못한다
    ///   · 최적 수 비교: 의뢰마다 가장 덜 떨리는 수가 실제로 다르다 (문턱이 필요 없는 판정)
    ///
    /// 「전부 캐묻는다」가 최적이 아닌 것도 여기서 본다 — 명료도가 먼저 마른다.
    /// </summary>
    [TestFixture]
    public sealed class NoDominantStrategyTests
    {
        private const int MinEdgePercentPoints = 5;

        private static readonly string[] Fixed =
        {
            "FixedFirstMood", "FixedMaxIntensity", "FixedEarliestDay"
        };

        [Test]
        public void 한_축을_굳힌_정책은_전부_Careful_아래다()
        {
            int careful = TestWorld.OverallRate("Careful");
            TestContext.WriteLine("Careful " + careful + "%");
            foreach (string p in Fixed)
            {
                int rate = TestWorld.OverallRate(p);
                TestContext.WriteLine("    " + p + " " + rate + "% (차이 " + (careful - rate) + "%p)");
                Assert.That(careful - rate, Is.GreaterThanOrEqualTo(MinEdgePercentPoints),
                            p + " 가 " + rate + "% 로 Careful(" + careful + "%) 과 사실상 같다 — 그 축을 고를 값이 없다");
            }
        }

        [Test]
        public void 굳힌_정책마다_무너지는_의뢰가_따로_있다()
        {
            Dictionary<string, List<string>> fails = new Dictionary<string, List<string>>();
            foreach (string p in Fixed)
            {
                fails[p] = new List<string>();
                foreach (string cid in TestWorld.CommissionIds())
                    if (TestWorld.Run(cid, p).RatePercent < TestWorld.Run(cid, "Careful").RatePercent)
                        fails[p].Add(cid + "(" + TestWorld.Run(cid, p).RatePercent + "%)");
            }
            foreach (string p in Fixed)
            {
                TestContext.WriteLine(p + " 가 무너지는 의뢰: " + string.Join(" ", fails[p]));
                Assert.That(fails[p], Is.Not.Empty, p + " 는 어느 의뢰에서도 손해를 보지 않는다");
            }

            // 셋이 같은 의뢰에서만 무너지면 「의뢰 하나가 특별하다」이지 「축이 여럿이다」가 아니다
            HashSet<string> union = new HashSet<string>();
            foreach (string p in Fixed) foreach (string f in fails[p]) union.Add(f.Split('(')[0]);
            Assert.That(union.Count, Is.GreaterThan(1),
                        "굳힌 정책 셋이 모두 같은 의뢰 하나에서만 무너진다 — 축이 여럿이라는 증거가 아니다");
        }

        [Test]
        public void 전부_캐묻는_것이_최적이_아니다()
        {
            int careful = TestWorld.OverallRate("Careful");
            int all = TestWorld.OverallRate("ProbeEverything");
            int carefulCost = 0, allCost = 0;
            foreach (string cid in TestWorld.CommissionIds())
            {
                carefulCost += TestWorld.Run(cid, "Careful").LuciditySpent;
                allCost += TestWorld.Run(cid, "ProbeEverything").LuciditySpent;
            }
            TestContext.WriteLine("Careful " + careful + "% (명료도 " + carefulCost + ") · "
                                  + "ProbeEverything " + all + "% (명료도 " + allCost + ")");
            Assert.That(all, Is.LessThan(careful),
                        "닥치는 대로 캐묻는 것이 골라 캐묻는 것과 같거나 낫다 — 명료도가 값이 아니다");
            Assert.That(allCost, Is.GreaterThan(carefulCost), "더 많이 묻지도 않았다 — 정책 구현이 잘못됐다");
        }

        [Test]
        public void 겹쳐_묻지_않는_것도_최적이_아니다()
        {
            int careful = TestWorld.OverallRate("Careful");
            int liar = TestWorld.OverallRate("LiarTrusting");
            TestContext.WriteLine("Careful " + careful + "% · LiarTrusting " + liar + "%");
            Assert.That(liar, Is.LessThan(careful),
                        "겹쳐 묻지 않아도 성적이 같다 — 스크린 기억이 장식이다");
        }

        /// <summary>
        /// **문턱이 필요 없는 판정.** 의뢰마다 참값 기준으로 가장 덜 떨리는 수를 뽑아,
        /// 어느 축에서도 네 의뢰에 다 통하는 고정값이 없음을 보인다.
        /// </summary>
        [Test]
        public void 의뢰마다_가장_덜_떨리는_수가_다르다()
        {
            Dictionary<string, GraftChoice> best = new Dictionary<string, GraftChoice>();
            foreach (CommissionDef c in TestWorld.Data.AllCommissions)
            {
                DreamSession s = DreamSession.Open(TestWorld.Data, c.id, 0);
                List<string> anchors = new List<string>();
                foreach (MemoryDef m in s.Net.Memories) anchors.Add(m.id);
                GraftChoice pick = null;
                int low = int.MaxValue;
                string key = null;
                foreach (GraftChoice ch in GraftRules.LegalChoices(c, anchors, TestWorld.Data.Balance.dayStep))
                {
                    GraftVerdict v = s.Judge(ch);
                    if (v.Blatant) continue;
                    string k = ch.ToString();
                    if (pick == null || v.TotalTremor < low
                        || (v.TotalTremor == low && string.CompareOrdinal(k, key) < 0))
                    { pick = ch; low = v.TotalTremor; key = k; }
                }
                Assert.That(pick, Is.Not.Null, c.id + ": 노골적이지 않은 수가 하나도 없다");
                best[c.id] = pick;
                TestContext.WriteLine(c.id + " 최적: " + pick + "  떨림 " + low);
            }

            AssertAxisVaries("이어 붙일 자리", best, ch => ch.AnchorMemoryId);
            AssertAxisVaries("정서", best, ch => ch.MoodId);

            // 날과 세기는 의뢰마다 범위가 다르므로 **창 안에서의 상대 위치**로 본다
            HashSet<string> dayRule = new HashSet<string>();
            HashSet<string> intenRule = new HashSet<string>();
            foreach (CommissionDef c in TestWorld.Data.AllCommissions)
            {
                GraftChoice ch = best[c.id];
                dayRule.Add(ch.DayIndex == c.dayWindowStart ? "earliest"
                            : ch.DayIndex == c.dayWindowEnd ? "latest" : "middle");
                intenRule.Add(ch.IntensityPercent == c.intensityMin ? "min"
                              : ch.IntensityPercent == c.intensityMax ? "max" : "middle");
            }
            TestContext.WriteLine("최적 날의 상대 위치: " + string.Join(",", dayRule));
            TestContext.WriteLine("최적 세기의 상대 위치: " + string.Join(",", intenRule));
            Assert.That(dayRule.Count, Is.GreaterThan(1),
                        "네 의뢰 모두 창의 같은 자리(" + string.Join(",", dayRule) + ")가 최적이다 — 날을 고를 필요가 없다");
        }

        private static void AssertAxisVaries(string axis, Dictionary<string, GraftChoice> best,
                                             System.Func<GraftChoice, string> pick)
        {
            HashSet<string> values = new HashSet<string>();
            foreach (KeyValuePair<string, GraftChoice> kv in best) values.Add(pick(kv.Value));
            TestContext.WriteLine("최적 " + axis + ": " + string.Join(", ", values));
            Assert.That(values.Count, Is.GreaterThan(1),
                        axis + " 축의 최적값이 네 의뢰에서 모두 같다 — 그 축은 고를 것이 없다");
        }
    }
}
