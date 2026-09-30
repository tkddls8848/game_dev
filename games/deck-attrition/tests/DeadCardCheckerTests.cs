using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 채택되지 않는 카드가 없는가 (`PLAN_GENRES.md` §2.5).
    /// 뽑으면 손해인 카드는 보상이 아니라 벌이다.
    ///
    /// 분모는 "손에 있고 기력으로 낼 수 있었던 턴 수"다. "덱에 있었던 횟수"로 두면
    /// 뽑히지 않아서 안 쓴 것과 뽑혔는데 안 쓴 것이 섞여 아무것도 판정하지 못한다.
    /// </summary>
    [TestFixture]
    public class DeadCardCheckerTests
    {
        [Test]
        public void 모든_카드가_최소한의_채택률을_넘는다()
        {
            var b = Fix.Data.Balance;
            var agent = Fix.Measured();
            var deck = Fix.FullPoolDeck(2);

            for (int i = 0; i < 80; i++) RunEngine.Play(Fix.Data, agent, b.seedBase + i, deck);

            int lowest = 1000; string lowestId = null;
            foreach (var c in Fix.Data.Cards)
            {
                int chances = agent.Stats.Chances(c.id);
                int pct = agent.Stats.AdoptionPct(c.id);
                TestContext.WriteLine(string.Format("{0,-16} 채택 {1,3}%  (냄 {2} / 기회 {3})",
                    c.id, pct, agent.Stats.Plays(c.id), chances));
                Assert.That(chances, Is.GreaterThan(0), c.id + " 가 손에 한 번도 오지 않았다");
                if (pct < lowest) { lowest = pct; lowestId = c.id; }
            }

            TestContext.WriteLine("최저 채택률 " + lowestId + " " + lowest + "% (한계 "
                                  + b.deadCardMinAdoptionPct + "%)");
            Assert.That(lowest, Is.GreaterThanOrEqualTo(b.deadCardMinAdoptionPct),
                lowestId + " 가 " + lowest + "% 로 사실상 안 쓰인다 — 사장 카드다");
        }

        [Test]
        public void 닳는_활자도_전부_쓰인다()
        {
            // 아끼는 것이 옳은 게임이라 닳는 활자의 채택률은 낮은 게 정상이다.
            // 그래도 <b>0 이면 안 된다</b> — 0 이면 아끼는 것이 아니라 없는 것이다.
            var b = Fix.Data.Balance;
            var agent = Fix.Measured();
            for (int i = 0; i < 120; i++) RunEngine.Play(Fix.Data, agent, b.seedBase + i);

            foreach (var c in Fix.Data.Cards)
            {
                if (!c.IsConsumable) continue;
                int plays = agent.Stats.Plays(c.id);
                TestContext.WriteLine(c.id + " 120회차에서 " + plays + "번 인쇄 · 채택률 "
                                      + agent.Stats.AdoptionPct(c.id) + "%");
                Assert.That(plays, Is.GreaterThan(0), c.id + " 를 120회차 동안 한 번도 안 썼다");
            }
        }

        [Test]
        public void 시작_덱에서_카드를_빼는_편이_낫지는_않다()
        {
            // 빼는 편이 나은 카드가 있으면 그 카드는 벌이다.
            var b = Fix.Data.Balance;
            var full = new System.Collections.Generic.List<string>(Fix.Data.Run.startingDeck);
            int basePct = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, b.dominanceRuns, full);

            int worstGain = 0; string worstId = null;
            foreach (var c in Fix.Data.Cards)
            {
                var trimmed = new System.Collections.Generic.List<string>(full);
                if (!trimmed.Remove(c.id)) continue;         // 시작 덱에 없는 카드는 건너뛴다
                int pct = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, b.dominanceRuns, trimmed);
                int gain = pct - basePct;
                if (gain > worstGain) { worstGain = gain; worstId = c.id; }
            }

            TestContext.WriteLine("기본 " + basePct + "% · 한 장을 빼서 얻는 최대 이득 "
                                  + worstGain + "%p" + (worstId == null ? "" : " (" + worstId + ")"));
            Assert.That(worstGain, Is.LessThanOrEqualTo(15),
                worstId + " 를 빼면 승률이 " + worstGain + "%p 오른다 — 그 카드는 벌이다");
        }
    }
}
