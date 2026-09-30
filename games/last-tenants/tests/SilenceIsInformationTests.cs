using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// ★ **핵 검사기 2 — SilenceIsInformation.**
    ///
    /// 소리 없는 집이 「내용 없음」이 아니라 **신호**인가.
    /// 그 집을 고른 결과가 다른 집과 뚜렷이 다르고, **무시했을 때의 결과도 다른가.**
    ///
    /// 세 세계를 같은 씨드로 돌려 비교한다:
    ///
    ///   켠 세계    — 이 PoC. 이웃의 말은 그 문에서 침묵을 확인한 뒤에야 확신이 된다
    ///   문 막은 세계 — 침묵한 문에 귀를 댈 수 없다. 「침묵에는 들을 것이 없다」를 강제한 세계
    ///   소리로 바꾼 세계 — 침묵한 칸이 자기 문에서 자기 사정을 말한다. 침묵이 사라진 세계
    ///
    /// **침묵을 소리 있는 집과 같게 다루면 이 컨셉의 가장 좋은 부분이 죽는다**를 수치로 보이는 것이
    /// 이 검사기의 일이다.
    /// </summary>
    [TestFixture]
    public class SilenceIsInformationTests
    {
        private static List<string> WithUnable()
        {
            List<string> outp = new List<string>();
            foreach (string bid in TestWorld.BuildingIds())
                if (TestWorld.Silent(bid, SilenceKinds.Unable) != null) outp.Add(bid);
            return outp;
        }

        [Test]
        public void A_침묵한_문에_귀를_대지_못하면_그_칸은_구할_수_없다()
        {
            GameData d = TestWorld.Data;
            int need = d.Balance.checkers.silenceIgnoreGapMin;
            StringBuilder sb = new StringBuilder();
            List<string> buildings = WithUnable();
            Assert.That(buildings.Count, Is.GreaterThanOrEqualTo(2),
                "말할 수 없는 집이 있는 건물이 둘은 있어야 이 비교가 우연이 아니다");

            int minGap = int.MaxValue;
            foreach (string bid in buildings)
            {
                HouseholdDef u = TestWorld.Silent(bid, SilenceKinds.Unable);
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceRow on = TestWorld.Matrix(bid, seed).Row(u.id);
                    ChoiceRow off = TestWorld.MatrixNoSilentDoors(bid, seed).Row(u.id);
                    int gap = off.Total - on.Total;
                    if (gap < minGap) minGap = gap;
                    Assert.That(gap, Is.GreaterThanOrEqualTo(need),
                        bid + " 씨드" + seed + ": 침묵한 문을 막아도 " + u.id
                        + " 를 고른 결과가 " + gap + "점만 나빠졌다 (기준 " + need + ")");
                    Assert.That(on.SilentDoorListens, Is.GreaterThan(0),
                        bid + " 씨드" + seed + ": 최선 계획이 침묵한 문에 귀를 대지 않았다 —"
                        + " 그러면 침묵이 규칙이 아니라 장식이다");
                    Assert.That(off.KnowledgeLevel, Is.LessThanOrEqualTo(1),
                        bid + " 씨드" + seed + ": 문을 막았는데도 그 집 사정이 확신까지 갔다");
                }
                sb.AppendLine(bid + " " + u.id + ": 문 막으면 " + u.name + " 를 고른 최선이 "
                              + TestWorld.MatrixNoSilentDoors(bid, 0).Row(u.id).Total
                              + " (켠 세계 " + TestWorld.Matrix(bid, 0).Row(u.id).Total + ")");
            }
            sb.AppendLine("가장 작은 벌어짐 " + minGap + "점 (기준 " + need + ")");
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void B_두_침묵의_결과가_뚜렷이_다르다()
        {
            GameData d = TestWorld.Data;
            int need = d.Balance.checkers.silenceKindGapMin;
            StringBuilder sb = new StringBuilder();
            int compared = 0;
            foreach (string bid in TestWorld.BuildingIds())
            {
                HouseholdDef u = TestWorld.Silent(bid, SilenceKinds.Unable);
                HouseholdDef g = TestWorld.Silent(bid, SilenceKinds.Gone);
                if (u == null || g == null) continue;
                compared++;
                int bestU = int.MaxValue, bestG = int.MaxValue;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    if (m.Row(u.id).Total < bestU) bestU = m.Row(u.id).Total;
                    if (m.Row(g.id).Total < bestG) bestG = m.Row(g.id).Total;
                }
                Assert.That(bestG - bestU, Is.GreaterThanOrEqualTo(need),
                    bid + ": 말할 수 없는 집(" + bestU + ")과 이미 나간 집(" + bestG
                    + ")의 최선이 " + (bestG - bestU) + "점밖에 다르지 않다 — 침묵이 한 덩어리다");
                sb.AppendLine(bid + ": 말할 수 없는 집 최선 " + bestU + " · 이미 나간 집 최선 " + bestG
                              + " · 벌어짐 " + (bestG - bestU));
            }
            Assert.That(compared, Is.GreaterThanOrEqualTo(1));
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void B2_이미_나간_집을_돕는_것은_아무도_돕지_않는_것보다_나쁘다()
        {
            GameData d = TestWorld.Data;
            int need = d.Balance.checkers.silenceGoneVsNothingMin;
            StringBuilder sb = new StringBuilder();
            foreach (string bid in TestWorld.BuildingIds())
            {
                HouseholdDef g = TestWorld.Silent(bid, SilenceKinds.Gone);
                Assert.That(g, Is.Not.Null, bid + " 에 이미 나간 침묵이 없다");
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    int gap = m.Row(g.id).Total - m.NoOne.Total;
                    Assert.That(gap, Is.GreaterThanOrEqualTo(need),
                        bid + " 씨드" + seed + ": 빈 집을 도와도 " + gap
                        + "점만 나빠진다 — 침묵을 맹신하는 것에 값이 물리지 않는다");
                }
                sb.AppendLine(bid + ": 빈 집을 도우면 " + TestWorld.Matrix(bid, 0).Row(g.id).Total
                              + " (아무도 돕지 않으면 " + TestWorld.Matrix(bid, 0).NoOne.Total + ")");
            }
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void B3_말할_수_없는_집을_고르는_것은_아무것도_안_하는_것보다_낫다()
        {
            // 최선(씨드가 맞아떨어진 날)에서 그렇다. 확인하지 못한 날은 오히려 손해라는 것도 같이 적는다.
            StringBuilder sb = new StringBuilder();
            foreach (string bid in WithUnable())
            {
                HouseholdDef u = TestWorld.Silent(bid, SilenceKinds.Unable);
                int best = int.MaxValue, worst = 0, noOne = 0, worseThanNothing = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    int t = m.Row(u.id).Total;
                    noOne = m.NoOne.Total;
                    if (t < best) best = t;
                    if (t > worst) worst = t;
                    if (t > noOne) worseThanNothing++;
                }
                Assert.That(best, Is.LessThan(noOne),
                    bid + ": 말할 수 없는 집을 고르는 것이 어떤 씨드에서도 아무것도 안 하는 것보다 낫지 않다");
                sb.AppendLine(bid + ": 최선 " + best + " · 최악 " + worst + " · 아무것도 안 함 " + noOne
                              + " · 안 하는 것보다 나쁜 날 " + worseThanNothing + "/" + TestWorld.SeedSweep);
            }
            TestContext.Out.WriteLine(sb.ToString());
        }
    }
}
