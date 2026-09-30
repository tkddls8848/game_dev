using NUnit.Framework;

namespace DeckRewind.Tests
{
    /// <summary>
    /// 이 PoC의 규칙 자체를 재는 테스트 (PLAN_DECKBUILDER §3 후보 1순위 "되감기").
    ///
    /// 위험은 하나다: <b>되감기가 공짜면 규칙이 아니라 편의가 된다.</b>
    /// 그래서 여기서 재는 것은 "되감기가 동작한다"가 아니라
    /// "되감기에 값이 있고, 동시에 대가가 있다" 두 가지다.
    /// </summary>
    [TestFixture]
    public class RewindRuleTests
    {
        [Test]
        public void 되감으면_적의_기억이_남고_같은_수를_다시_두지_않는다()
        {
            var b = NewBattle(777);
            b.BeginPlayerTurn();
            // 1라운드에 적이 둘 수. 되감기가 취소하는 것이 이 수다.
            int undoneIntent = b.EffectiveIntentIndex;
            b.EndPlayerTurn();
            b.EnemyTurn();
            b.BeginPlayerTurn();

            b.Rewind();

            Assert.That(b.Memory, Is.EqualTo(1), "되감았는데 적이 기억하지 않는다");
            Assert.That(b.RewindCharges, Is.EqualTo(Fix.Data.Run.rewindCharges - 1), "충전이 줄지 않았다");
            Assert.That(b.Round, Is.EqualTo(1), "1라운드 시작으로 돌아가야 한다");
            Assert.That(b.EffectiveIntentIndex, Is.Not.EqualTo(undoneIntent),
                "되감았는데 적이 방금 둔 수를 그대로 다시 둔다 — 대비하지 않는다");
        }

        [Test]
        public void 기억한_적은_같은_공격으로_더_세게_때린다()
        {
            // 되감으면 패턴이 밀려 다음 수가 달라지므로, "다음에 맞는 값"을 비교하면
            // 기억의 효과와 패턴 이동이 섞인다. 같은 공격량 하나를 고정해 놓고 잰다.
            const int probe = 10;
            var b = NewBattle(2468);
            b.BeginPlayerTurn();
            int at0 = b.PreviewEnemyDamage(probe);

            b.EndPlayerTurn(); b.EnemyTurn(); b.BeginPlayerTurn();
            b.Rewind();
            int at1 = b.PreviewEnemyDamage(probe);

            b.EndPlayerTurn(); b.EnemyTurn(); b.BeginPlayerTurn();
            b.Rewind();
            int at2 = b.PreviewEnemyDamage(probe);

            TestContext.WriteLine("기억 0/1/2 에서 공격 " + probe + " 의 실제 피해: "
                                  + at0 + " / " + at1 + " / " + at2);
            Assert.That(at1, Is.GreaterThan(at0), "한 번 되감았는데 피해가 그대로다 — 되감기가 공짜다");
            Assert.That(at2, Is.GreaterThan(at1), "기억이 쌓이는데 피해가 늘지 않는다");
        }

        [Test]
        public void 전투를_통틀어도_기억한_적이_더_아프다()
        {
            // 위가 단위 계산이라면 이것은 전투 전체의 합이다. 백분율 나머지 누적이 총합을 맞추는지도 본다.
            int plain = HpLostOverRounds(0);
            int remembered = HpLostOverRounds(2);
            TestContext.WriteLine("되감기 0회 -> 누적 " + plain + " 피해 · 2회 -> 누적 " + remembered + " 피해");
            Assert.That(remembered, Is.GreaterThan(plain), "되감기를 써도 전투 전체 피해가 늘지 않는다");
        }

        [Test]
        public void 되감기는_라운드_하나와_그_안의_수를_모두_되돌린다()
        {
            var b = NewBattle(1234);
            b.BeginPlayerTurn();
            int handAtStart = b.Hand.Count;
            // 낼 수 있는 것을 하나 낸다.
            for (int i = 0; i < b.Hand.Count; i++)
                if (b.CanPlay(i)) { b.PlayCard(i); break; }
            b.EndPlayerTurn();
            b.EnemyTurn();

            b.BeginPlayerTurn();
            b.Rewind();

            Assert.That(b.Round, Is.EqualTo(1));
            Assert.That(b.Hand.Count, Is.EqualTo(handAtStart), "낸 카드가 손으로 돌아오지 않았다");
            Assert.That(b.Enemy.Hp, Is.EqualTo(b.EnemyDef.maxHp), "적 체력이 되돌아오지 않았다");
            Assert.That(b.Player.Hp, Is.EqualTo(Fix.Data.Run.playerMaxHp), "플레이어 체력이 되돌아오지 않았다");
        }

        [Test]
        public void 첫_라운드에서는_되감을_수_없다()
        {
            var b = NewBattle(99);
            b.BeginPlayerTurn();
            Assert.That(b.CanRewind, Is.False, "되돌릴 과거가 없는데 되감을 수 있다");
        }

        [Test]
        public void 충전이_떨어지면_되감을_수_없다()
        {
            var run = Fix.Data.Run;
            var b = new Battle(Fix.Data, Fix.Data.Enemy(run.battles[0]), run.startingDeck,
                               run.playerMaxHp, run.playerMaxHp, 55,
                               run.handSize, run.energyPerTurn, 1, run.maxRewindsPerBattle,
                               run.maxRoundsPerBattle);
            b.BeginPlayerTurn(); b.EndPlayerTurn(); b.EnemyTurn();
            b.BeginPlayerTurn();
            Assert.That(b.CanRewind, Is.True);
            b.Rewind();
            b.EndPlayerTurn(); b.EnemyTurn();
            b.BeginPlayerTurn(); b.EndPlayerTurn(); b.EnemyTurn();
            b.BeginPlayerTurn();
            Assert.That(b.RewindCharges, Is.EqualTo(0));
            Assert.That(b.CanRewind, Is.False, "충전이 0인데 되감을 수 있다");
        }

        [Test]
        public void 되감기를_쓰는_쪽이_안_쓰는_쪽보다_낫다()
        {
            // 규칙의 값. 대가가 값을 넘으면 아무도 되감지 않고, 그러면 규칙이 없는 것과 같다.
            var bal = Fix.Data.Balance;
            int with = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 30000, 150);
            int without = RunEngine.WinRatePct(Fix.Data, _ => Fix.GreedyNoRewind(),
                                               bal.seedBase + 30000, 150);
            TestContext.WriteLine("되감기 씀 " + with + "% · 안 씀 " + without + "%");
            Assert.That(with, Is.GreaterThan(without),
                "되감기를 쓰는 쪽이 낫지 않다 — 대가가 값을 넘었다. 규칙이 죽어 있다");
        }

        [Test]
        public void 성급하게_되감으면_손해다()
        {
            // 이 PoC가 측정으로 알아낸 것. 되감기는 여유가 아니라 마지막 수단이다.
            // 적의 기억이 전투 내내 남으므로 선불로 되감으면 남은 전투 전부가 비싸진다.
            var bal = Fix.Data.Balance;
            int onDeath = RunEngine.WinRatePct(Fix.Data, _ => Fix.Greedy(), bal.seedBase + 32000, 150);
            int eager = RunEngine.WinRatePct(Fix.Data, _ => Fix.GreedyEagerRewind(), bal.seedBase + 32000, 150);
            TestContext.WriteLine("죽을 때만 되감기 " + onDeath + "% · 크게 맞으면 바로 되감기 " + eager + "%");
            Assert.That(onDeath, Is.GreaterThan(eager),
                "성급한 되감기가 더 낫다 — 기억의 대가가 너무 싸다");
        }

        [Test]
        public void 되감기가_실제로_쓰인다()
        {
            int used = 0, runs = 60;
            for (int s = 0; s < runs; s++)
                used += RunEngine.Play(Fix.Data, Fix.Greedy(), Fix.Data.Balance.seedBase + 31000 + s).RewindsUsed;
            TestContext.WriteLine(runs + "판에서 되감기 " + used + "회");
            Assert.That(used, Is.GreaterThan(0), "준최적 플레이가 되감기를 한 번도 쓰지 않는다");
        }

        /// 카드를 한 장도 내지 않고 여덟 라운드를 흘려보내며 누적 피해를 잰다.
        /// 되감기를 먼저 소비하면 기억이 그만큼 쌓인 상태로 같은 라운드 수를 돈다.
        static int HpLostOverRounds(int rewinds)
        {
            var run = Fix.Data.Run;
            var b = new Battle(Fix.Data, Fix.Data.Enemy("enemy_sentry"), run.startingDeck,
                               400, 400, 2468,
                               run.handSize, run.energyPerTurn, 9, 9, 99);
            b.BeginPlayerTurn(); b.EndPlayerTurn(); b.EnemyTurn();
            for (int i = 0; i < rewinds; i++)
            {
                b.BeginPlayerTurn();
                b.Rewind();
                b.EndPlayerTurn(); b.EnemyTurn();
            }
            // 감시기의 패턴 길이는 4다. 그 배수만큼 돌리면 어느 오프셋에서 시작해도
            // 패턴의 모든 수가 같은 횟수로 나온다 — 그래서 총합에는 기억의 효과만 남는다.
            int rounds = b.EnemyDef.pattern.Length * 3;
            int before = b.Player.Hp;
            for (int i = 0; i < rounds; i++) { b.BeginPlayerTurn(); b.EndPlayerTurn(); b.EnemyTurn(); }
            return before - b.Player.Hp;
        }

        static Battle NewBattle(int seed)
        {
            var run = Fix.Data.Run;
            return new Battle(Fix.Data, Fix.Data.Enemy(run.battles[0]), run.startingDeck,
                              run.playerMaxHp, run.playerMaxHp, seed,
                              run.handSize, run.energyPerTurn, run.rewindCharges,
                              run.maxRewindsPerBattle, run.maxRoundsPerBattle);
        }
    }
}
