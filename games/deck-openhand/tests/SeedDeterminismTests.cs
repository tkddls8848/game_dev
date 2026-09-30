using NUnit.Framework;

namespace DeckOpenhand.Tests
{
    /// <summary>
    /// 검사기 1 — `SeedDeterminism` (PLAN_DECKBUILDER §5).
    /// <b>같은 씨드 = 같은 결과.</b> 아래 검사기 전부의 전제다.
    /// 한 번이라도 깨지면 승률·지배·사장 카드 판정이 모두 무의미해진다.
    /// </summary>
    [TestFixture]
    public class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드로_두_번_돌리면_기록이_한_글자도_다르지_않다()
        {
            for (int s = 0; s < 8; s++)
            {
                int seed = Fix.Data.Balance.seedBase + s;
                var a = RunEngine.Play(Fix.Data, Fix.Greedy(), seed);
                var b = RunEngine.Play(Fix.Data, Fix.Greedy(), seed);
                Assert.That(b.Transcript, Is.EqualTo(a.Transcript), "씨드 " + seed + " 의 기록이 갈렸다");
                Assert.That(b.Won, Is.EqualTo(a.Won));
                Assert.That(b.PlayerHp, Is.EqualTo(a.PlayerHp));
                Assert.That(b.IntentsSkipped, Is.EqualTo(a.IntentsSkipped));
                Assert.That(b.IntentsSwapped, Is.EqualTo(a.IntentsSwapped));
            }
        }

        [Test]
        public void 무작위_플레이도_씨드가_같으면_같다()
        {
            for (int s = 0; s < 8; s++)
            {
                int seed = Fix.Data.Balance.seedBase + 500 + s;
                var a = RunEngine.Play(Fix.Data, Fix.Random(seed), seed);
                var b = RunEngine.Play(Fix.Data, Fix.Random(seed), seed);
                Assert.That(b.Transcript, Is.EqualTo(a.Transcript), "씨드 " + seed);
            }
        }

        [Test]
        public void 씨드가_다르면_기록도_다르다()
        {
            // 결정적인 것과 씨드를 무시하는 것은 다르다. 후자면 승률 측정이 표본 하나가 된다.
            var first = RunEngine.Play(Fix.Data, Fix.Greedy(), Fix.Data.Balance.seedBase).Transcript;
            int different = 0;
            for (int s = 1; s <= 12; s++)
                if (RunEngine.Play(Fix.Data, Fix.Greedy(), Fix.Data.Balance.seedBase + s).Transcript != first)
                    different++;
            Assert.That(different, Is.GreaterThanOrEqualTo(10), "씨드를 바꿨는데 기록이 거의 그대로다");
        }

        [Test]
        public void 카드를_뽑는_순서만_난수에_걸려_있다()
        {
            // 이 PoC의 설계: 난수는 손패에만 닿는다. 그래서 운의 비중이 작다.
            // 같은 씨드로 만든 두 전투는 손패가 같고, 씨드를 바꾸면 손패는 달라진다.
            var a = Fix.NewBattle("enemy_hierophant", 7777);
            var b = Fix.NewBattle("enemy_hierophant", 7777);
            a.BeginPlayerTurn(); b.BeginPlayerTurn();
            Assert.That(string.Join(",", b.Hand), Is.EqualTo(string.Join(",", a.Hand)));

            int changed = 0;
            for (int s = 1; s <= 10; s++)
            {
                var c = Fix.NewBattle("enemy_hierophant", 7777 + s);
                c.BeginPlayerTurn();
                if (string.Join(",", c.Hand) != string.Join(",", a.Hand)) changed++;
            }
            Assert.That(changed, Is.GreaterThanOrEqualTo(7), "씨드를 바꿨는데 손패가 거의 그대로다");
        }
    }
}
