using System.Collections.Generic;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// 공통 검사기 — `NoDominantStrategy`.
    /// **한 가지 수를 여덟 대 되풀이하는 것이 최적이 아닌가.**
    /// 최적이면 이 게임은 결정이 하나뿐이고, 나머지 일곱 대는 장식이다.
    ///
    /// 잣대는 `balance.json` 의 `score` 다. 그 잣대가 게임의 도덕이 아니라는 것을
    /// `CurseSim.ScoreOf` 의 주석에 못 박아 두었다 — 여기서 재는 것은 "고민이 남는가"뿐이다.
    /// </summary>
    [TestFixture]
    public sealed class NoDominantStrategyTests
    {
        [Test]
        public void 한_제례만_되풀이하는_정책은_전부_전수의_최선보다_한참_못하다()
        {
            GameData d = TestWorld.Data;
            int margin = d.Balance.checkers.monoDominanceMarginPoints;
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult full = TestWorld.Full(seed);
                int bestMono = int.MinValue;
                string bestMonoId = null;
                foreach (string riteId in d.RiteIds())
                {
                    LedgerRun r = TestWorld.MonoRun(seed, riteId);
                    if (r.Score > bestMono) { bestMono = r.Score; bestMonoId = riteId; }
                    Assert.That(r.VillageAlive, Is.False,
                        riteId + " 만 되풀이해도 마을이 살아남는다 — 결정이 하나뿐인 게임이 된다");
                }
                Assert.That(full.Best.Score - bestMono, Is.GreaterThanOrEqualTo(margin),
                    "씨드 " + seed + ": 가장 좋은 한 수 되풀이(" + bestMonoId + " " + bestMono
                    + ")가 전수 최선(" + full.Best.Score + ")에 너무 가깝다");
                TestContext.WriteLine("씨드 " + seed + ": 최선 한 수 되풀이 " + bestMonoId + " " + bestMono
                    + " vs 전수 최선 " + full.Best.Score + " (차 " + (full.Best.Score - bestMono) + ")");
            }
        }

        [Test]
        public void 전수의_최선은_제례_셋_이상을_섞는다()
        {
            int min = TestWorld.Data.Balance.checkers.minDistinctRitesInBestPolicy;
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult full = TestWorld.Full(seed);
                Assert.That(full.Best.DistinctRites, Is.GreaterThanOrEqualTo(min),
                    "씨드 " + seed + " 최선이 제례 " + full.Best.DistinctRites + "종류만 쓴다: " + full.Best.PolicyText);
                TestContext.WriteLine("씨드 " + seed + " 최선 " + full.Best.DistinctRites + "종류 · "
                    + full.Best.PolicyText);
            }
        }

        [Test]
        public void 살아남는_정책들도_한_가지_수로_이루어져_있지_않다()
        {
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult full = TestWorld.Full(seed);
                Assert.That(full.Survivors, Is.Not.Empty);
                int minDistinct = int.MaxValue;
                foreach (LedgerRun r in full.Survivors)
                    if (r.DistinctRites < minDistinct) minDistinct = r.DistinctRites;
                Assert.That(minDistinct, Is.GreaterThanOrEqualTo(2),
                    "씨드 " + seed + " 에 한 가지 수만으로 살아남는 정책이 있다");
                TestContext.WriteLine("씨드 " + seed + ": 살아남는 정책 " + full.Survivors.Count
                    + "개 · 가장 단조로운 것도 제례 " + minDistinct + "종류를 쓴다");
            }
        }

        [Test]
        public void 쓸모없는_제례가_없다()
        {
            // 어느 제례도 "고를 이유가 아예 없는" 상태가 되면 안 된다.
            // 살아남는 정책들 안에서 그 수가 한 번이라도 쓰이는지 본다.
            SweepResult full = TestWorld.Full(TestWorld.MainSeed);
            Dictionary<string, int> used = new Dictionary<string, int>();
            foreach (string id in TestWorld.Data.RiteIds()) used[id] = 0;
            foreach (LedgerRun r in full.Survivors)
                foreach (string id in r.Policy) if (id != null) used[id] = used[id] + 1;

            List<string> never = new List<string>();
            foreach (KeyValuePair<string, int> kv in used)
            {
                TestContext.WriteLine(kv.Key.PadRight(16) + " 살아남은 정책에서 " + kv.Value + "번 쓰였다");
                if (kv.Value == 0) never.Add(kv.Key);
            }

            Assert.That(never, Is.Empty, "살아남는 정책에서 한 번도 쓰이지 않는 제례가 있다: " + string.Join(", ", never));

            // 해제 의식은 살아남는 길에도 있지만, 그 수의 고유한 값은 **다른 결말로 가는 유일한 길**이다.
            Assert.That(full.LiftedRuns, Is.Not.Empty, "해제 의식으로 닿는 결말이 전수에 없다 — 그 수는 정말 쓸모없다");
            TestContext.WriteLine("해제 의식은 '해제 완수' 결말 " + full.LiftedRuns.Count
                + "회차로 가는 유일한 길이다");
        }

        [Test]
        public void 미룬다고_공짜가_되지_않는다()
        {
            // 「장부를 덮는다」가 값이 싸면 모두가 그것만 고른다. 지연으로 반드시 값을 치러야 한다.
            LedgerRun close = TestWorld.MonoRun(TestWorld.MainSeed, "close_ledger");
            Assert.That(close.VillageAlive, Is.False);
            Assert.That(close.GenerationsSurvived, Is.LessThan(TestWorld.Generations),
                "장부만 덮어도 여덟 대를 넘긴다 — 미루는 것이 공짜다");
            Assert.That(close.Wrath, Is.GreaterThan(TestWorld.Data.Curse.start.wrath + 40),
                "장부를 덮어도 노여움이 거의 오르지 않는다");
            TestContext.WriteLine("장부만 덮는 정책: " + LedgerAudit.OneLine(close));
        }
    }
}
