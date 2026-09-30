using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// ★ <b>규칙이 장식이 아닌가.</b>
    ///
    /// 마모를 구현해 놓고 실제로는 아무도 닳을 만큼 쓰지 않는다면, 그 규칙은 화면에 적힌 글자일 뿐이다.
    /// 회차를 <b>정상적으로 끝낼 때</b>(=이겼을 때) 활자가 실제로 녹아 사라지는지를 본다.
    /// 이겼을 때를 보는 이유: 지는 판은 일찍 끝나서 안 닳는 게 당연하고, 그걸 세면 아무것도 판정하지 못한다.
    /// </summary>
    [TestFixture]
    public class AttritionReachableTests
    {
        [Test]
        public void 이긴_회차에서_활자가_실제로_녹는다()
        {
            var bal = Fix.Data.Balance;
            int won = 0, wonWithMelt = 0, meltedTotal = 0, usesOnWin = 0;

            for (int i = 0; i < bal.monteCarloRuns; i++)
            {
                var r = Fix.Run(Fix.Measured(), bal.seedBase + i);
                if (!r.Won) continue;
                won++;
                usesOnWin += r.UsesSpent;
                if (r.Exhausted.Count > 0) { wonWithMelt++; meltedTotal += r.Exhausted.Count; }
            }

            Assert.That(won, Is.GreaterThan(0), "이긴 회차가 하나도 없어 판정할 수 없다");
            int meltPct = wonWithMelt * 100 / won;
            int usesX100 = usesOnWin * 100 / won;

            TestContext.WriteLine("이긴 회차 " + won + "개 · 활자가 녹은 회차 " + meltPct
                                  + "% · 회차당 녹은 활자 " + (meltedTotal * 100 / won) / 100.0
                                  + "종 · 회차당 인쇄 " + usesX100 / 100 + "." + (usesX100 % 100).ToString("00") + "회");

            Assert.That(meltPct, Is.GreaterThanOrEqualTo(bal.attritionExhaustedRunPctMin),
                "회차를 이기고도 활자가 거의 안 녹는다 — 마모가 장식이다");
            Assert.That(usesX100, Is.GreaterThanOrEqualTo(bal.attritionUsesPerRunMin),
                "닳는 활자를 거의 안 쓴다 — 넣어 둔 의미가 없다");
        }

        [Test]
        public void 녹은_활자는_다음_전투에_다시_나타나지_않는다()
        {
            // 기록으로 확인한다. 한 번 melt 된 활자가 뒤에서 다시 play 되면 규칙이 한 판짜리로 돌아간 것이다.
            var bal = Fix.Data.Balance;
            int checkedRuns = 0;

            for (int i = 0; i < 60; i++)
            {
                var r = Fix.Run(Fix.Measured(), bal.seedBase + i);
                if (r.Exhausted.Count == 0) continue;
                checkedRuns++;

                foreach (var gone in r.Exhausted)
                {
                    int meltAt = r.Transcript.IndexOf("melt " + gone);
                    Assert.That(meltAt, Is.GreaterThanOrEqualTo(0));
                    int playAfter = r.Transcript.IndexOf("play " + gone, meltAt);
                    Assert.That(playAfter, Is.LessThan(0),
                        "씨드 " + (bal.seedBase + i) + ": 녹은 뒤에 " + gone + " 를 다시 냈다");
                }
            }

            Assert.That(checkedRuns, Is.GreaterThan(0), "활자가 녹은 회차를 하나도 못 찾았다");
            TestContext.WriteLine("활자가 녹은 회차 " + checkedRuns + "개를 훑어 '녹은 뒤 재사용' 0건");
        }

        [Test]
        public void 회차가_끝날_때_대부분의_활자가_닳아_있다()
        {
            // 남은 횟수가 그대로면 아낀 것이 아니라 쓸 일이 없었던 것이다.
            var bal = Fix.Data.Balance;
            int runs = 0, untouched = 0, total = 0;

            for (int i = 0; i < 100; i++)
            {
                var r = Fix.Run(Fix.Measured(), bal.seedBase + i);
                if (!r.Won) continue;
                runs++;
                foreach (var c in Fix.Data.Cards)
                {
                    if (!c.IsConsumable) continue;
                    total++;
                    if (r.Case.TimesUsed(c.id) == 0) untouched++;
                }
            }

            Assert.That(runs, Is.GreaterThan(0));
            int untouchedPct = untouched * 100 / total;
            TestContext.WriteLine("이긴 회차에서 한 번도 안 쓰인 활자 비율 " + untouchedPct + "%");
            Assert.That(untouchedPct, Is.LessThan(50),
                "이기고도 절반 넘는 활자가 손도 안 탄다 — 상자에 넣어 둔 것이 규칙이 아니라 장식이다");
        }
    }
}
