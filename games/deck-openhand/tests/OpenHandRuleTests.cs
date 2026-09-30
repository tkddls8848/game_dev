using NUnit.Framework;

namespace DeckOpenhand.Tests
{
    /// <summary>
    /// 이 PoC의 규칙 자체를 재는 테스트 (PLAN_DECKBUILDER §3 후보 "공개 덱").
    ///
    /// 규칙은 "적의 다음 다섯 수가 늘 보인다"이고, 주장은 <b>"정보가 아니라 순서가 퍼즐이다"</b>다.
    /// 그래서 여기서 재는 것은 둘이다:
    /// 1. 보이는 것이 실제로 오는 것과 같은가 (보이는 것이 거짓이면 규칙이 아니라 속임수다)
    /// 2. 순서를 읽고 바꾸는 것이 <b>값을 하는가</b> (값이 없으면 규칙이 장식이다)
    /// </summary>
    [TestFixture]
    public class OpenHandRuleTests
    {
        [Test]
        public void 보이는_다섯_수가_실제로_오는_순서와_같다()
        {
            var b = Fix.NewBattle("enemy_hierophant", 909);
            var predicted = b.PeekIntents(Fix.Data.Run.peekWindow);

            for (int i = 0; i < predicted.Length; i++)
            {
                b.BeginPlayerTurn();
                var nowNext = b.NextIntent;
                Assert.That(nowNext.type, Is.EqualTo(predicted[i].type),
                    (i + 1) + "번째 수의 종류가 예고와 다르다");
                Assert.That(nowNext.amount, Is.EqualTo(predicted[i].amount),
                    (i + 1) + "번째 수의 값이 예고와 다르다");
                b.EndPlayerTurn();
                b.EnemyTurn();
            }
        }

        [Test]
        public void 창은_언제나_다섯_수만큼_차_있다()
        {
            // 전투가 길어져도 예고가 마르면 안 된다 — 마르는 순간 규칙이 사라진다.
            var b = Fix.NewBattle("enemy_hanged", 4321);
            for (int t = 0; t < 15 && b.Outcome == BattleOutcome.InProgress; t++)
            {
                b.BeginPlayerTurn();
                Assert.That(b.PeekIntents(Fix.Data.Run.peekWindow).Length,
                    Is.EqualTo(Fix.Data.Run.peekWindow), "T" + (t + 1) + " 에서 예고가 짧아졌다");
                b.EndPlayerTurn();
                b.EnemyTurn();
            }
        }

        [Test]
        public void 가림막은_다음_한_수를_지운다()
        {
            var b = Fix.NewBattle("enemy_hierophant", 111, Fix.MonoDeck("card_veil", 10));
            b.BeginPlayerTurn();
            var before = b.PeekIntents(3);
            PlayFirst(b, "card_veil");
            var after = b.PeekIntents(2);

            Assert.That(b.IntentsSkipped, Is.EqualTo(1), "지운 수가 세어지지 않았다");
            Assert.That(after[0].type, Is.EqualTo(before[1].type), "지운 뒤 두 번째 수가 앞으로 오지 않았다");
            Assert.That(after[0].amount, Is.EqualTo(before[1].amount));
            Assert.That(after[1].amount, Is.EqualTo(before[2].amount));
        }

        [Test]
        public void 역순은_다음_두_수의_자리를_바꾼다()
        {
            var b = Fix.NewBattle("enemy_hierophant", 222, Fix.MonoDeck("card_invert", 10));
            b.BeginPlayerTurn();
            var before = b.PeekIntents(2);
            PlayFirst(b, "card_invert");
            var after = b.PeekIntents(2);

            Assert.That(b.IntentsSwapped, Is.EqualTo(1));
            Assert.That(after[0].amount, Is.EqualTo(before[1].amount), "첫 수와 둘째 수가 바뀌지 않았다");
            Assert.That(after[1].amount, Is.EqualTo(before[0].amount));
        }

        [Test]
        public void 대비는_다음_수가_공격일_때_그_값만큼_막는다()
        {
            // 사제의 첫 수는 공격 9다. 대비는 9 + 2 = 11 을 막아야 한다.
            var b = Fix.NewBattle("enemy_hierophant", 333, Fix.MonoDeck("card_brace", 10));
            b.BeginPlayerTurn();
            var next = b.NextIntent;
            Assume.That(next.type, Is.EqualTo("attack"), "이 테스트는 첫 수가 공격인 적을 전제한다");

            PlayFirst(b, "card_brace");
            Assert.That(b.Player.Block, Is.EqualTo(next.amount + 2),
                "다음 수의 값만큼 막지 못했다 — 순서를 읽은 값이 수치가 되지 않는다");

            // 그 공격을 그대로 받으면 체력이 줄지 않는다.
            int hp = b.Player.Hp;
            b.EndPlayerTurn();
            b.EnemyTurn();
            Assert.That(b.Player.Hp, Is.EqualTo(hp), "정확히 막았는데 체력이 줄었다");
        }

        [Test]
        public void 메아리는_보이는_공격의_수만큼_때린다()
        {
            var b = Fix.NewBattle("enemy_hierophant", 444, Fix.MonoDeck("card_echo", 10));
            b.BeginPlayerTurn();
            int attacks = b.AttacksInWindow;
            Assume.That(attacks, Is.GreaterThan(0));
            int ehp = b.Enemy.Hp;
            PlayFirst(b, "card_echo");
            Assert.That(ehp - b.Enemy.Hp, Is.EqualTo(3 * attacks),
                "보이는 공격 " + attacks + "개에 대해 피해가 " + (ehp - b.Enemy.Hp) + " 다");
        }

        [Test]
        public void 순서를_읽는_쪽이_읽지_않는_쪽보다_낫다()
        {
            // 이 PoC의 주장이 통째로 걸려 있는 검사. 읽어도 안 읽어도 같으면 규칙이 장식이다.
            var bal = Fix.Data.Balance;
            int open = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 30000, 200);
            int blind = RunEngine.WinRatePct(Fix.Data, _ => Fix.GreedyBlind(), bal.seedBase + 30000, 200);
            TestContext.WriteLine("순서를 읽음 " + open + "% · 읽지 않음 " + blind + "%");
            Assert.That(open - blind, Is.GreaterThanOrEqualTo(10),
                "순서를 읽어도 " + (open - blind) + "%p 밖에 낫지 않다 — 규칙이 장식이다");
        }

        [Test]
        public void 순서를_바꾸는_카드가_실제로_쓰인다()
        {
            int skipped = 0, swapped = 0, runs = 80;
            for (int s = 0; s < runs; s++)
            {
                var r = RunEngine.Play(Fix.Data, Fix.Greedy(), Fix.Data.Balance.seedBase + 31000 + s,
                                       Fix.FullPoolDeck(2));
                skipped += r.IntentsSkipped;
                swapped += r.IntentsSwapped;
            }
            TestContext.WriteLine(runs + "판에서 가림막 " + skipped + "회 · 역순 " + swapped + "회");
            Assert.That(skipped, Is.GreaterThan(0), "가림막을 한 번도 쓰지 않는다");
            Assert.That(swapped, Is.GreaterThan(0), "역순을 한 번도 쓰지 않는다");
        }

        [Test]
        public void 적의_행동에는_난수가_없다()
        {
            // 공개 덱의 전제. 적이 난수를 쓰면 "보인다"가 성립하지 않는다.
            // 씨드를 바꿔도 적의 행동 순서는 같아야 한다.
            string First(int seed)
            {
                var b = Fix.NewBattle("enemy_tower", seed);
                var sb = new System.Text.StringBuilder();
                for (int t = 0; t < 10 && b.Outcome == BattleOutcome.InProgress; t++)
                {
                    b.BeginPlayerTurn();
                    sb.Append(b.NextIntent.type).Append(b.NextIntent.amount).Append(';');
                    b.EndPlayerTurn();
                    b.EnemyTurn();
                }
                return sb.ToString();
            }
            var baseline = First(1);
            for (int s = 2; s <= 6; s++)
                Assert.That(First(s), Is.EqualTo(baseline),
                    "씨드 " + s + " 에서 적의 행동 순서가 달라졌다");
        }

        static void PlayFirst(Battle b, string cardId)
        {
            for (int i = 0; i < b.Hand.Count; i++)
                if (b.Hand[i] == cardId && b.CanPlay(i)) { b.PlayCard(i); return; }
            Assert.Fail(cardId + " 을 손에서 찾지 못했다");
        }
    }
}
