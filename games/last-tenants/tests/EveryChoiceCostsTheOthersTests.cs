using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// ★ **핵 검사기 1 — EveryChoiceCostsTheOthers.**
    ///
    /// 누구를 고르든 나머지가 **측정 가능하게** 나빠지는가.
    /// 「고르면 다 잘 되는」 선택지가 하나라도 있으면 딜레마가 아니다.
    ///
    /// 선택지마다 하루를 **전수 탐색**해 최선을 찾고(표본이 아니다), 그 결과를 두 축에 늘어놓는다:
    ///
    ///   건짐(Relief) — 그 집이 건진 값. 크게 건지려면 비싼 사정을 골라야 한다
    ///   물림(Spill)  — 나머지 다섯이 물어 준 값. 비싼 사정은 유예를 많이 먹는다
    ///
    /// 지배되지 않는 선택지가 하나뿐이면 지배적 최선이 있다는 뜻이고, 그러면 이 컨셉은 죽는다.
    /// </summary>
    [TestFixture]
    public class EveryChoiceCostsTheOthersTests
    {
        [Test]
        public void 어떤_선택도_나머지에게_물리는_값이_0이_아니다()
        {
            StringBuilder sb = new StringBuilder();
            int minSpill = int.MaxValue;
            foreach (string bid in TestWorld.BuildingIds())
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    foreach (ChoiceRow r in m.Rows)
                    {
                        Assert.That(r.GraceSpent, Is.GreaterThan(0),
                            bid + " " + r.Id + " 를 고르는 데 유예가 한 푼도 들지 않았다 — 공짜 선택이다");
                        Assert.That(r.Spill, Is.GreaterThan(0),
                            bid + " " + r.Id + " 를 골랐는데 나머지가 아무것도 물지 않았다 — 딜레마가 아니다");
                        if (r.Spill < minSpill) minSpill = r.Spill;
                    }
                }
            sb.AppendLine("가장 싼 선택이 나머지에게 물린 값: " + minSpill);
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 나머지_세대_하나하나가_더_나빠진다()
        {
            // 합계로 뭉개지 않는다. 다섯 집 각각의 손실이 올라야 「나머지가 나빠졌다」고 말할 수 있다.
            foreach (string bid in TestWorld.BuildingIds())
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    foreach (ChoiceRow r in m.Rows)
                    {
                        Assert.That(m.EachOtherStrictlyWorse(r), Is.True,
                            bid + " " + r.Id + " 를 골랐을 때 손실이 그대로인 세대가 있다");
                        int noOneOthers = 0;
                        for (int i = 0; i < m.Night.Households.Count; i++)
                            if (i != r.Index) noOneOthers += m.Night.Households[i].baseStakes;
                        Assert.That(r.OthersDamage, Is.GreaterThan(noOneOthers),
                            bid + " " + r.Id + ": 나머지 합이 아무도 돕지 않을 때보다 크지 않다");
                    }
                }
        }

        [Test]
        public void 지배적_최선이_없다()
        {
            GameData d = TestWorld.Data;
            int min = d.Balance.checkers.paretoFrontMin;
            int sumFront = 0, count = 0, maxFront = 0;
            StringBuilder sb = new StringBuilder();
            foreach (string bid in TestWorld.BuildingIds())
            {
                int bMin = int.MaxValue, bMax = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    List<ChoiceRow> front = m.ParetoFront();
                    Assert.That(front.Count, Is.GreaterThanOrEqualTo(min),
                        bid + " 씨드" + seed + ": 지배되지 않는 선택지가 " + front.Count
                        + "개뿐이다 — 고르면 다 잘 되는 선택지가 있다는 뜻이다");
                    sumFront += front.Count; count++;
                    if (front.Count < bMin) bMin = front.Count;
                    if (front.Count > bMax) bMax = front.Count;
                    if (front.Count > maxFront) maxFront = front.Count;
                }
                sb.AppendLine(bid + ": 파레토 앞면 " + bMin + "~" + bMax + "개");
            }
            sb.AppendLine("앞면 크기 평균 x10 = " + (sumFront * 10 / count) + " (기준 "
                          + d.Balance.checkers.paretoFrontMeanTenths + " 이상)");
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(sumFront * 10, Is.GreaterThanOrEqualTo(d.Balance.checkers.paretoFrontMeanTenths * count),
                "앞면이 겨우 둘씩만 나오면 딜레마가 한 쌍뿐이라는 뜻이다");
            Assert.That(maxFront, Is.GreaterThanOrEqualTo(4),
                "어느 건물에서도 네 갈래가 서지 않으면 선택의 폭이 사실상 둘이다");
        }

        [Test]
        public void 가장_크게_건지는_선택과_가장_덜_물리는_선택이_다르다()
        {
            // 이 둘이 같은 집이면 그 집이 모든 축에서 최선이 되어 고민할 것이 없다.
            int same = 0, total = 0;
            StringBuilder sb = new StringBuilder();
            foreach (string bid in TestWorld.BuildingIds())
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    ChoiceRow maxRelief = null, minSpill = null;
                    foreach (ChoiceRow r in m.Rows)
                    {
                        if (maxRelief == null || r.Relief > maxRelief.Relief) maxRelief = r;
                        if (minSpill == null || r.Spill < minSpill.Spill) minSpill = r;
                    }
                    total++;
                    if (maxRelief.Id == minSpill.Id) same++;
                    Assert.That(maxRelief.Id, Is.Not.EqualTo(minSpill.Id),
                        bid + " 씨드" + seed + ": " + maxRelief.Id
                        + " 가 가장 크게 건지면서 가장 덜 물린다 — 지배적 최선이다");
                    if (seed == 0)
                        sb.AppendLine(bid + ": 가장 크게 건짐 " + maxRelief.UnitNo + "(" + maxRelief.Relief
                                      + ", 물림 " + maxRelief.Spill + ") · 가장 덜 물림 " + minSpill.UnitNo
                                      + "(" + minSpill.Relief + ", 물림 " + minSpill.Spill + ")");
                }
            sb.AppendLine("둘이 같은 경우 " + same + "/" + total);
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 아무도_돕지_않는_것보다_나쁜_선택이_반드시_있다()
        {
            // 「돕는 것은 언제나 이득」이면 고르는 일에 무게가 없다. 고른다는 것 자체가 위험이어야 한다.
            StringBuilder sb = new StringBuilder();
            foreach (string bid in TestWorld.BuildingIds())
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    ChoiceMatrix m = TestWorld.Matrix(bid, seed);
                    int worse = 0;
                    foreach (ChoiceRow r in m.Rows) if (r.Total > m.NoOne.Total) worse++;
                    Assert.That(worse, Is.GreaterThan(0),
                        bid + " 씨드" + seed + ": 아무도 돕지 않는 것보다 나쁜 선택이 하나도 없다");
                    if (seed == 0)
                        sb.AppendLine(bid + ": 여섯 선택 중 " + worse
                                      + "개가 아무도 돕지 않는 것(" + m.NoOne.Total + ")보다 나쁘다");
                }
            TestContext.Out.WriteLine(sb.ToString());
        }
    }
}
