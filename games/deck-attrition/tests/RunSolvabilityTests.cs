using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 이길 수 있는가 (`PLAN_GENRES.md` §2.5 `RunSolvability`).
    /// 깨졌을 때의 증상: 이길 수 없는 회차. 플레이어는 게임이 아니라 자기 실력을 의심한다.
    /// </summary>
    [TestFixture]
    public class RunSolvabilityTests
    {
        [Test]
        public void 준최적_정책으로_회차를_이긴다()
        {
            var b = Fix.Data.Balance;
            int wins = 0, n = 30;
            for (int i = 0; i < n; i++) if (Fix.Run(Fix.Measured(), b.seedBase + i).Won) wins++;
            TestContext.WriteLine("씨드 " + n + "개 중 " + wins + "개 승리");
            Assert.That(wins, Is.GreaterThanOrEqualTo(n / 3), "회차를 거의 못 이긴다");
        }

        [Test]
        public void 전투_하나하나를_가득_찬_체력으로_이길_수_있다()
        {
            // 회차가 어려운 것과 <b>어떤 적이 혼자서 불가능한 것</b>은 다르다.
            var run = Fix.Data.Run;
            var ids = new System.Collections.Generic.List<string>(run.battles) { run.boss };

            foreach (var enemyId in ids)
            {
                int wins = 0, n = 40;
                for (int i = 0; i < n; i++)
                {
                    var tc = new TypeCase(Fix.Data);
                    var battle = new Battle(Fix.Data, tc, Fix.Data.Enemy(enemyId), run.startingDeck,
                                            run.playerMaxHp, run.playerMaxHp, 5000 + i,
                                            run.handSize, run.energyPerTurn, run.maxRoundsPerBattle,
                                            0, 0, true);   // 보스 자리로 두어 아끼지 않게 한다
                    battle.RunToEnd(Fix.Measured());
                    if (battle.Outcome == BattleOutcome.PlayerWon) wins++;
                }
                TestContext.WriteLine(enemyId + " 단독 승률 " + (wins * 100 / n) + "%");
                Assert.That(wins * 100 / n, Is.GreaterThanOrEqualTo(50),
                    enemyId + " 는 가득 찬 체력에 활자를 다 써도 절반을 못 이긴다 — 혼자서 불가능하다");
            }
        }

        [Test]
        public void 보스는_닳는_활자_없이는_거의_이기지_못한다()
        {
            // ★ 이 규칙이 회차 전체를 조이는 이유. 보스가 기본 활자만으로 잡히면
            //   "아껴 둘까"가 질문이 아니게 된다 — 아낀 것을 쓸 데가 없기 때문이다.
            var run = Fix.Data.Run;
            var basics = new System.Collections.Generic.List<string>();
            foreach (var id in run.startingDeck)
                if (!Fix.Data.Card(id).IsConsumable) basics.Add(id);
            // 장 수를 맞춘다. 덱이 얇아져서 진 것이 아니어야 한다.
            while (basics.Count < run.startingDeck.Length) basics.Add("card_sort");

            int wins = 0, n = 60;
            for (int i = 0; i < n; i++)
            {
                var tc = new TypeCase(Fix.Data);
                var battle = new Battle(Fix.Data, tc, Fix.Data.Enemy(run.boss), basics,
                                        run.playerMaxHp, run.playerMaxHp, 6000 + i,
                                        run.handSize, run.energyPerTurn, run.maxRoundsPerBattle,
                                        0, 0, true);
                battle.RunToEnd(Fix.Measured());
                if (battle.Outcome == BattleOutcome.PlayerWon) wins++;
            }

            int pct = wins * 100 / n;
            TestContext.WriteLine("기본 활자만 " + basics.Count + "장으로 보스 단독 승률 " + pct
                                  + "% (가득 찬 체력에서)");
            Assert.That(pct, Is.LessThanOrEqualTo(25),
                "기본 활자만으로 보스를 " + pct + "% 잡는다 — 아껴 둔 활자를 쓸 데가 없다");
        }

        [Test]
        public void 이긴_회차는_마감_안에_끝난다()
        {
            // maxRoundsPerBattle 은 마감이다. 이긴 판이 매번 마감 직전에 끝나면
            // 이긴 것이 아니라 간신히 흘러 넘친 것이고, 데이터를 다시 봐야 한다.
            var b = Fix.Data.Balance;
            int won = 0, timeouts = 0;
            for (int i = 0; i < 60; i++)
            {
                var r = Fix.Run(Fix.Measured(), b.seedBase + i);
                if (r.Won) won++;
                if (r.Transcript.Contains("timeout")) timeouts++;
            }
            TestContext.WriteLine("60회차 중 승리 " + won + " · 마감 초과가 한 번이라도 난 회차 " + timeouts);
            Assert.That(won, Is.GreaterThan(0));
        }
    }
}
