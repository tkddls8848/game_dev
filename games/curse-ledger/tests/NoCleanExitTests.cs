using System.Collections.Generic;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// ★ 핵 검사기 하나 — `NoCleanExit`.
    ///
    /// **"마을도 살고 아무도 희생되지 않는" 결말에 닿는 정책이 하나도 없는가.**
    /// 하나라도 있으면 이것은 딜레마가 아니라 그냥 풀 수 있는 문제다.
    ///
    /// **없음의 주장이라 표본으로는 못 보인다.** 그래서 정책 공간을 전수로 내려간다 —
    /// 대마다 고를 수 있는 제례 전부로 갈라지고, 끝난 회차는 그 자리에서 결말로 센다
    /// (끝난 뒤의 미래는 존재하지 않는다). 씨드도 하나로는 증명이 아니므로 셋을 돌린다.
    ///
    /// 그리고 **반대쪽도 같이 못 박는다**: 이름을 지우면 살아남는 정책이 있어야 한다.
    /// 전부 죽는 게임은 딜레마가 아니라 그냥 못 이기는 게임이다.
    /// </summary>
    [TestFixture]
    public sealed class NoCleanExitTests
    {
        [Test]
        public void 아무도_바치지_않고_마을을_살리는_정책이_전수에_없다()
        {
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult full = TestWorld.Full(seed);
                Assert.That(full.CleanExits, Is.Empty,
                    "씨드 " + seed + " 에서 깨끗한 출구가 " + full.CleanExits.Count + "개 나왔다 — 딜레마가 아니다. "
                    + (full.CleanExits.Count > 0 ? "첫째: " + full.CleanExits[0].PolicyText : ""));

                TestContext.WriteLine("씨드 " + seed + ": 마디 " + full.NodesVisited
                    + " · 결말 " + full.RunsSettled + " · 깨끗한 출구 0"
                    + " · 가장 깊은 대 " + full.DeepestGeneration);
            }
        }

        [Test]
        public void 사람을_바치는_제례를_아예_뺀_전수에도_살아남는_회차가_없다()
        {
            // 위 검사와 겹쳐 보이지만 다른 것을 본다: 위는 "희생자 0인 결말"을 걸러 낸 것이고
            // 이것은 **바칠 수 있다는 사실 자체를 지운 세계**를 전수로 훑는다.
            // 이쪽이 더 좁은 정책 공간이라 전수의 깊이를 확실히 볼 수 있다.
            List<string> bloodless = TestWorld.BloodlessRiteIds();
            Assert.That(bloodless.Count, Is.EqualTo(4), "사람을 바치지 않는 제례는 넷이다");

            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult b = TestWorld.Bloodless(seed);
                Assert.That(b.Survivors, Is.Empty,
                    "씨드 " + seed + " 의 무혈 세계에서 " + b.Survivors.Count + "회차가 살아남았다");
                foreach (LedgerRun r in b.BestByVictimCount.Values)
                    Assert.That(r.VictimCount, Is.Zero, "무혈 세계에서 희생자가 나왔다");

                List<Ending> kinds = new List<Ending>(b.EndingCounts.Keys);
                kinds.Sort();
                List<string> parts = new List<string>();
                foreach (Ending e in kinds) parts.Add(Endings.Korean(e) + " " + b.EndingCounts[e]);
                TestContext.WriteLine("씨드 " + seed + " 무혈 전수: 마디 " + b.NodesVisited
                    + " · 결말 " + b.RunsSettled + " (" + string.Join(" · ", parts) + ")"
                    + " · 가장 깊은 대 " + b.DeepestGeneration + "/" + TestWorld.Generations);
            }
        }

        [Test]
        public void 무혈_최선은_대를_끝까지_잇지_못하고_어떻게_죽는지_수치로_남는다()
        {
            // "없다"만 적으면 왜 없는지가 사라진다. 가장 깨끗한 출구에 가까웠던 시도를 기록으로 남긴다.
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult b = TestWorld.Bloodless(seed);
                LedgerRun best = b.BloodlessBest;
                Assert.That(best, Is.Not.Null);
                Assert.That(best.VillageAlive, Is.False);
                Assert.That(best.CleanExit, Is.False);
                TestContext.WriteLine("씨드 " + seed + " 무혈 최선: " + LedgerAudit.OneLine(best)
                    + "\n            " + best.PolicyText);
            }
        }

        [Test]
        public void 그러나_이름을_지우면_살아남는_정책이_있다()
        {
            // ★ 이 검사가 없으면 위의 전부가 "그냥 못 이기는 게임"이라는 뜻이 된다.
            int min = TestWorld.Data.Balance.checkers.minSurvivingPoliciesWithVictims;
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult full = TestWorld.Full(seed);
                Assert.That(full.AliveWithVictims.Count, Is.GreaterThanOrEqualTo(min),
                    "씨드 " + seed + " 에서 마을이 살아남는 정책이 " + full.AliveWithVictims.Count
                    + "개뿐이다 — 딜레마가 아니라 그냥 못 이기는 게임이다");

                // 살아남으려면 이름이 몇 개나 필요한가. 이 수가 이 PoC의 값이다.
                int cheapest = int.MaxValue;
                foreach (LedgerRun r in full.AliveWithVictims)
                    if (r.VictimCount < cheapest) cheapest = r.VictimCount;
                Assert.That(cheapest, Is.GreaterThan(0), "희생자 0으로 살아남았다 — 위 검사와 어긋난다");

                TestContext.WriteLine("씨드 " + seed + ": 살아남는 정책 " + full.AliveWithVictims.Count
                    + "개 · **마을을 살리는 가장 싼 값은 이름 " + cheapest + "개**");
            }
        }

        [Test]
        public void 희생자_수를_한_명씩_올려_가며_어디서부터_살아남는지_본다()
        {
            // 도덕이 자원이라는 말이 수치가 되는 자리. 이름을 하나 더 지울 때마다 무엇이 바뀌는가.
            SweepResult full = TestWorld.Full(TestWorld.MainSeed);
            List<int> counts = new List<int>(full.BestByVictimCount.Keys);
            counts.Sort();
            bool sawDead = false, sawAlive = false;
            foreach (int vc in counts)
            {
                LedgerRun r = full.BestByVictimCount[vc];
                if (r.VillageAlive) sawAlive = true; else if (vc == 0) sawDead = true;
                TestContext.WriteLine("희생자 " + vc + "명 최선 → " + LedgerAudit.OneLine(r));
            }
            Assert.That(sawDead, "희생자 0명에서 마을이 살아남았다");
            Assert.That(sawAlive, "희생자를 늘려도 마을이 살아남는 길이 없다");
        }
    }
}
