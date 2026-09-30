using System.Collections.Generic;
using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 규칙 자체가 실제로 그렇게 도는가. 밸런스가 아니라 <b>기계 장치</b>를 본다.
    /// 여기가 깨지면 위의 승률 측정은 전부 다른 게임을 잰 것이 된다.
    /// </summary>
    [TestFixture]
    public class AttritionRuleTests
    {
        static CardData FirstConsumable()
        {
            foreach (var c in Fix.Data.Cards) if (c.IsConsumable) return c;
            Assert.Fail("닳는 활자가 데이터에 하나도 없다");
            return null;
        }

        [Test]
        public void 상용_활자는_닳지_않는다()
        {
            var tc = new TypeCase(Fix.Data);
            foreach (var c in Fix.Data.Cards)
            {
                if (c.IsConsumable) continue;
                Assert.That(tc.UsesLeft(c.id), Is.EqualTo(-1), c.id);
                Assert.That(tc.SharpnessPct(c.id), Is.EqualTo(100), c.id);
                Assert.That(tc.Spend(c.id), Is.False, c.id + " 가 닳았다");
                Assert.That(tc.Print(c.id, "damage", 10), Is.EqualTo(10), c.id);
            }
        }

        [Test]
        public void 찍을수록_선명도가_떨어지고_값이_줄어든다()
        {
            var c = FirstConsumable();
            var tc = new TypeCase(Fix.Data);

            int first = tc.Print(c.id, "damage", 20);
            tc.Spend(c.id);
            int second = tc.Print(c.id, "damage", 20);

            TestContext.WriteLine(c.id + ": 첫 인쇄 " + first + " → 두 번째 " + second
                                  + " (선명도 100% → " + tc.SharpnessPct(c.id) + "%)");
            Assert.That(first, Is.EqualTo(20), "첫 인쇄는 설계값 그대로여야 한다");
            Assert.That(second, Is.LessThan(first), "두 번째 인쇄가 안 약해졌다 — 마모가 값에 닿지 않는다");
        }

        [Test]
        public void 선명도는_바닥_아래로_내려가지_않는다()
        {
            var c = FirstConsumable();
            var tc = new TypeCase(Fix.Data);
            // 다시 부어 가며 계속 찍어 마모만 쌓는다.
            for (int i = 0; i < 40; i++)
            {
                if (tc.UsesLeft(c.id) <= 1) tc.Recast(c.id, 1);
                tc.Spend(c.id);
                if (tc.IsSpent(c.id)) break;
            }
            Assert.That(tc.SharpnessPct(c.id),
                Is.GreaterThanOrEqualTo(Fix.Data.Balance.minSharpnessPct),
                "선명도가 바닥을 뚫었다 — '남았는데 아무 일도 안 일어나는 카드'가 생긴다");
        }

        [Test]
        public void 백분율_나머지가_누적돼_총합이_맞는다()
        {
            // 설계 원칙 4. 버림만 하면 열 번 찍은 총합이 설계값과 어긋난다.
            var c = FirstConsumable();
            var tc = new TypeCase(Fix.Data);
            tc.Spend(c.id);                       // 선명도를 100 아래로 떨어뜨린다
            int pct = tc.SharpnessPct(c.id);
            Assert.That(pct, Is.LessThan(100));

            const int amount = 7, times = 20;
            int sum = 0;
            for (int i = 0; i < times; i++) sum += tc.Print(c.id, "damage", amount);

            int ideal = amount * pct * times / 100;
            TestContext.WriteLine("선명도 " + pct + "% 로 " + amount + " 을 " + times + "번: 합 "
                                  + sum + " · 이상적 합 " + ideal);
            Assert.That(sum, Is.InRange(ideal - 1, ideal + 1),
                "나머지가 누적되지 않아 총합이 흘렀다");
        }

        [Test]
        public void 다_쓰면_덱과_손과_버림에서_전부_사라진다()
        {
            var data = Fix.Data;
            var c = FirstConsumable();
            var tc = new TypeCase(data);

            // 같은 활자를 세 장 넣은 덱. 횟수는 장 수가 아니라 활자 하나에 붙어 있으므로
            // 다 닳는 순간 세 장이 한꺼번에 사라져야 한다.
            var deck = new List<string> { c.id, c.id, c.id };
            for (int i = 0; i < 6; i++) deck.Add("card_sort");

            var b = new Battle(data, tc, data.Enemy(data.Run.battles[0]), deck,
                               data.Run.playerMaxHp, data.Run.playerMaxHp, 99,
                               data.Run.handSize, data.Run.energyPerTurn, data.Run.maxRoundsPerBattle);

            int guard = 0;
            while (!tc.IsSpent(c.id) && guard++ < 60)
            {
                b.BeginPlayerTurn();
                int idx = b.Hand.IndexOf(c.id);
                if (idx >= 0 && b.CanPlay(idx)) b.PlayCard(idx);
                if (b.Outcome != BattleOutcome.InProgress) break;
                b.EndPlayerTurn();
                b.EnemyTurn();
            }

            Assert.That(tc.IsSpent(c.id), Is.True, "활자를 다 쓰지 못했다 — 검사 자체가 성립하지 않았다");
            Assert.That(b.Deck, Does.Not.Contain(c.id), "덱에 녹은 활자가 남았다");
            Assert.That(b.Hand, Does.Not.Contain(c.id), "손에 녹은 활자가 남았다");
            Assert.That(b.Discard, Does.Not.Contain(c.id), "버림에 녹은 활자가 남았다");
            Assert.That(b.SpentHere, Does.Contain(c.id));
        }

        [Test]
        public void 마모는_전투를_넘어_이어진다()
        {
            // ★ 이 PoC의 규칙 그 자체. 전투마다 새로 차면 평범한 소모품이 된다.
            var data = Fix.Data;
            var c = FirstConsumable();
            var tc = new TypeCase(data);
            var deck = new List<string> { c.id, "card_sort", "card_sort", "card_sort", "card_sort" };

            var b1 = new Battle(data, tc, data.Enemy(data.Run.battles[0]), deck,
                                data.Run.playerMaxHp, data.Run.playerMaxHp, 11,
                                data.Run.handSize, data.Run.energyPerTurn, data.Run.maxRoundsPerBattle);
            b1.BeginPlayerTurn();
            int idx = b1.Hand.IndexOf(c.id);
            Assert.That(idx, Is.GreaterThanOrEqualTo(0), "첫 턴에 그 활자가 손에 오지 않았다");
            b1.PlayCard(idx);

            int afterBattle1 = tc.UsesLeft(c.id);
            int sharpAfter1 = tc.SharpnessPct(c.id);

            // 다음 전투. 같은 상자를 넘겨준다 — 그게 규칙이다.
            var b2 = new Battle(data, tc, data.Enemy(data.Run.battles[1]), deck,
                                data.Run.playerMaxHp, data.Run.playerMaxHp, 12,
                                data.Run.handSize, data.Run.energyPerTurn, data.Run.maxRoundsPerBattle);

            TestContext.WriteLine(c.id + " · 전투1 뒤 남은 횟수 " + afterBattle1
                                  + " · 선명도 " + sharpAfter1 + "% · 전투2 시작 시 "
                                  + b2.Case.UsesLeft(c.id) + "회 " + b2.Case.SharpnessPct(c.id) + "%");

            Assert.That(afterBattle1, Is.EqualTo(c.uses - 1), "첫 전투에서 횟수가 줄지 않았다");
            Assert.That(b2.Case.UsesLeft(c.id), Is.EqualTo(afterBattle1),
                "전투가 바뀌자 횟수가 되돌아왔다 — 회차 단위 마모가 아니다");
            Assert.That(b2.Case.SharpnessPct(c.id), Is.EqualTo(sharpAfter1),
                "전투가 바뀌자 선명도가 되돌아왔다 — 회차 단위 마모가 아니다");
        }

        [Test]
        public void 녹은_활자는_재주조로도_돌아오지_않는다()
        {
            var c = FirstConsumable();
            var tc = new TypeCase(Fix.Data);
            for (int i = 0; i < c.uses; i++) tc.Spend(c.id);

            Assert.That(tc.IsSpent(c.id), Is.True);
            Assert.That(tc.Recast(c.id, 2), Is.False, "녹은 활자가 되살아났다");
            Assert.That(tc.UsesLeft(c.id), Is.EqualTo(0));
            Assert.That(tc.MostWorn(), Is.Not.EqualTo(c.id), "녹은 활자가 재주조 대상으로 뽑힌다");
        }

        [Test]
        public void 재주조는_선명도도_되돌린다()
        {
            var c = FirstConsumable();
            var tc = new TypeCase(Fix.Data);
            tc.Spend(c.id);
            int worn = tc.SharpnessPct(c.id);
            int left = tc.UsesLeft(c.id);

            Assert.That(tc.Recast(c.id, 1), Is.True);
            Assert.That(tc.UsesLeft(c.id), Is.EqualTo(left + 1));
            Assert.That(tc.SharpnessPct(c.id), Is.GreaterThan(worn),
                "다시 부었는데 여전히 뭉개져 있다 — 납을 다시 부은 것이 아니다");
        }

        [Test]
        public void 가장_뭉개진_활자를_고르는_순서가_데이터_순서로_고정된다()
        {
            // Dictionary 순회 순서로 자르면 같은 씨드가 다른 결과를 낸다.
            // 동점을 만들어 두고 열 번 물어 같은 답이 나오는지 본다.
            var tc = new TypeCase(Fix.Data);
            var consumables = new List<string>();
            foreach (var c in Fix.Data.Cards) if (c.IsConsumable) consumables.Add(c.id);
            Assert.That(consumables.Count, Is.GreaterThanOrEqualTo(2));

            foreach (var id in consumables) tc.Spend(id);
            string first = tc.MostWorn();
            for (int i = 0; i < 10; i++)
                Assert.That(tc.MostWorn(), Is.EqualTo(first), "재주조 대상이 호출마다 흔들린다");
            Assert.That(first, Is.Not.Null);
        }
    }
}
