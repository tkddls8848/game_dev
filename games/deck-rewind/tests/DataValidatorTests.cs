using System.Collections.Generic;
using NUnit.Framework;

namespace DeckRewind.Tests
{
    /// <summary>
    /// 검사기 0 — `DataValidator` (PLAN_DECKBUILDER §5).
    /// 참조 무결성. 깨지면 런타임에 빈 카드가 나온다.
    /// </summary>
    [TestFixture]
    public class DataValidatorTests
    {
        static readonly string[] CardEffectTypes =
            { "damage", "block", "strength", "draw", "energy", "heal", "weak", "burn", "rewind_charge" };
        static readonly string[] IntentTypes =
            { "attack", "block", "weak", "strengthen", "heal" };
        static readonly string[] Rarities = { "common", "uncommon", "rare" };

        [Test]
        public void 카드는_5종에서_12종_사이다()
        {
            // PLAN §7 Phase 1~2 의 규모. 수직 슬라이스가 이 PoC의 범위다.
            Assert.That(Fix.Data.Cards.Length, Is.InRange(5, 12));
        }

        [Test]
        public void 카드_id는_겹치지_않고_필드가_채워져_있다()
        {
            var seen = new List<string>();
            foreach (var c in Fix.Data.Cards)
            {
                Assert.That(c.id, Is.Not.Null.And.Not.Empty);
                Assert.That(seen, Does.Not.Contain(c.id), "카드 id 중복: " + c.id);
                seen.Add(c.id);
                Assert.That(c.nameKo, Is.Not.Null.And.Not.Empty, c.id + " 의 한국어 이름이 없다");
                Assert.That(c.textKo, Is.Not.Null.And.Not.Empty, c.id + " 의 한국어 설명이 없다");
                Assert.That(c.cost, Is.InRange(0, 5), c.id + " 의 코스트");
                Assert.That(Rarities, Does.Contain(c.rarity), c.id + " 의 희귀도: " + c.rarity);
                Assert.That(c.effects, Is.Not.Null.And.Not.Empty, c.id + " 에 효과가 없다");
            }
        }

        [Test]
        public void 카드_효과의_type과_target이_알려진_값이다()
        {
            foreach (var c in Fix.Data.Cards)
                foreach (var e in c.effects)
                {
                    Assert.That(CardEffectTypes, Does.Contain(e.type), c.id + " 의 효과 type: " + e.type);
                    Assert.That(new[] { "self", "enemy" }, Does.Contain(e.target),
                        c.id + " 의 효과 target: " + e.target);
                    // 값 없는 int 를 0 으로 채우면 "피해 0" 이 조용히 생긴다. -1 도 남기지 않는다.
                    Assert.That(e.amount, Is.GreaterThan(0), c.id + " 의 " + e.type + " amount");
                }
        }

        [Test]
        public void 적의_패턴이_알려진_의도로만_이루어져_있다()
        {
            foreach (var en in Fix.Data.Enemies)
            {
                Assert.That(en.maxHp, Is.GreaterThan(0), en.id);
                Assert.That(en.memoryDamagePctPerStack, Is.InRange(1, 100), en.id + " 의 기억 배율");
                Assert.That(en.pattern, Is.Not.Null.And.Not.Empty, en.id + " 에 패턴이 없다");
                bool hasAttack = false;
                foreach (var p in en.pattern)
                {
                    Assert.That(IntentTypes, Does.Contain(p.type), en.id + " 의 의도: " + p.type);
                    Assert.That(p.amount, Is.GreaterThan(0), en.id + " 의 " + p.type + " amount");
                    if (p.type == "attack") hasAttack = true;
                }
                Assert.That(hasAttack, Is.True, en.id + " 은 한 번도 공격하지 않는다 — 이길 수 없는 대신 질 수도 없다");
            }
        }

        [Test]
        public void 회차가_가리키는_카드와_적이_모두_존재한다()
        {
            var run = Fix.Data.Run;
            foreach (var id in run.startingDeck)
                Assert.That(Fix.Data.HasCard(id), Is.True, "시작 덱의 없는 카드: " + id);
            foreach (var id in run.rewardPool)
                Assert.That(Fix.Data.HasCard(id), Is.True, "보상 풀의 없는 카드: " + id);
            foreach (var id in run.battles)
                Assert.That(Fix.Data.HasEnemy(id), Is.True, "전투 순서의 없는 적: " + id);
            Assert.That(Fix.Data.HasEnemy(run.boss), Is.True, "보스가 없다: " + run.boss);

            Assert.That(run.seed, Is.GreaterThan(0), "씨드가 데이터에 적혀 있어야 한다");
            Assert.That(run.playerMaxHp, Is.GreaterThan(0));
            Assert.That(run.handSize, Is.InRange(1, 10));
            Assert.That(run.energyPerTurn, Is.InRange(1, 10));
            Assert.That(run.rewindCharges, Is.GreaterThan(0), "되감기 충전이 0이면 이 PoC의 규칙이 없다");
            Assert.That(run.maxRoundsPerBattle, Is.GreaterThan(1));
            Assert.That(run.startingDeck.Length, Is.GreaterThanOrEqualTo(run.handSize));
        }

        [Test]
        public void 모든_카드가_보상_풀이나_시작_덱에_들어_있다()
        {
            // 어디에도 나오지 않는 카드는 데이터에만 있고 게임에는 없는 카드다.
            var reachable = new List<string>(Fix.Data.Run.startingDeck);
            reachable.AddRange(Fix.Data.Run.rewardPool);
            foreach (var c in Fix.Data.Cards)
                Assert.That(reachable, Does.Contain(c.id),
                    c.id + " 은 시작 덱에도 보상 풀에도 없다 — 회차에서 만날 수 없는 카드다");
        }

        [Test]
        public void 밸런스_한계값이_모두_채워져_있다()
        {
            var b = Fix.Data.Balance;
            Assert.That(b.monteCarloRuns, Is.GreaterThan(0));
            Assert.That(b.dominanceRuns, Is.GreaterThan(0));
            Assert.That(b.dominanceMaxWinRatePct, Is.InRange(1, 100));
            Assert.That(b.randomWinRatePctMax, Is.GreaterThan(b.randomWinRatePctMin));
            Assert.That(b.randomBattleWinRatePctMax, Is.GreaterThan(b.randomBattleWinRatePctMin));
            Assert.That(b.greedyWinRatePctMax, Is.GreaterThan(b.greedyWinRatePctMin));
            Assert.That(b.greedyWinRatePctMin, Is.GreaterThan(b.randomWinRatePctMax),
                "준최적이 무작위보다 확실히 나아야 한다 — 아니면 실력이 값을 하지 않는 게임이다");
            Assert.That(b.rewardPenaltyMaxPct, Is.InRange(1, 100));
            Assert.That(b.weakDamagePct, Is.InRange(1, 99));
            Assert.That(b.seedBase, Is.GreaterThan(0));
        }
    }
}
