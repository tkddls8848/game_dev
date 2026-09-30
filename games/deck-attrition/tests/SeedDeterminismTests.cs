using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 같은 씨드 = 같은 결과 (`PLAN_GENRES.md` §2.5). <b>아래 검사기 전부의 전제다</b> —
    /// 한 번이라도 깨지면 승률·지배·마모 측정이 통째로 의미를 잃는다.
    ///
    /// 이 PoC에서 특히 위험한 자리가 하나 있다: <see cref="TypeCase"/> 가 카드 id 로 색인한
    /// Dictionary 를 들고 있다는 것. 거기를 순회하면 .NET 버전에 따라 순서가 달라지고
    /// "가장 뭉개진 활자" 가 달라진다. 그래서 순서가 필요한 자리는 전부
    /// <c>data/cards.json</c> 의 배열 순서를 쓴다 — 그 규율이 지켜지는지를 여기서 본다.
    /// </summary>
    [TestFixture]
    public class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드는_같은_기록을_남긴다()
        {
            var a = Fix.Run(Fix.Measured(), 4242);
            var b = Fix.Run(Fix.Measured(), 4242);
            Assert.That(b.Transcript, Is.EqualTo(a.Transcript), "같은 씨드가 다른 기록을 냈다");
            Assert.That(b.Won, Is.EqualTo(a.Won));
            Assert.That(b.PlayerHp, Is.EqualTo(a.PlayerHp));
            Assert.That(b.UsesSpent, Is.EqualTo(a.UsesSpent));
            Assert.That(string.Join(",", b.Exhausted), Is.EqualTo(string.Join(",", a.Exhausted)));
        }

        [Test]
        public void 마모_상태까지_같은_씨드에서_같다()
        {
            var a = Fix.Run(Fix.Measured(), 777);
            var b = Fix.Run(Fix.Measured(), 777);
            Assert.That(b.Case.Describe(), Is.EqualTo(a.Case.Describe()),
                "회차가 끝났을 때 활자 상자의 상태가 다르다 — 마모가 재현되지 않는다");
        }

        [Test]
        public void 씨드가_바뀌면_결과도_갈린다()
        {
            // 재현성만 보면 "늘 같은 값을 돌려준다"도 통과한다. 갈리는 것도 확인해야 한다.
            int different = 0;
            var first = Fix.Run(Fix.Measured(), 1000).Transcript;
            for (int i = 1; i <= 20; i++)
                if (Fix.Run(Fix.Measured(), 1000 + i).Transcript != first) different++;
            Assert.That(different, Is.GreaterThanOrEqualTo(18),
                "씨드를 바꿔도 결과가 거의 안 갈린다 — 난수가 실제로 쓰이고 있지 않다");
        }

        [Test]
        public void 몬테카를로_승률이_두_번_같다()
        {
            var b = Fix.Data.Balance;
            int a1 = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, 60);
            int a2 = RunEngine.WinRatePct(Fix.Data, s => Fix.Measured(), b.seedBase, 60);
            Assert.That(a2, Is.EqualTo(a1), "같은 씨드에서 승률이 두 번 다르게 나왔다");
        }

        [Test]
        public void 전투_하나도_씨드로_재현된다()
        {
            string Play(int seed)
            {
                var tc = new TypeCase(Fix.Data);
                var run = Fix.Data.Run;
                var battle = new Battle(Fix.Data, tc, Fix.Data.Enemy(run.battles[0]), run.startingDeck,
                                        run.playerMaxHp, run.playerMaxHp, seed,
                                        run.handSize, run.energyPerTurn, run.maxRoundsPerBattle);
                battle.RunToEnd(Fix.Measured());
                return battle.Transcript() + tc.Describe();
            }
            Assert.That(Play(31337), Is.EqualTo(Play(31337)));
        }
    }
}
