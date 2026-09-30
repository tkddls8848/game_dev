using System.Collections.Generic;
using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 참조 무결성과 스키마 (`PLAN_GENRES.md` §2.5 `DataValidator`).
    /// 깨졌을 때의 증상은 "런타임에 빈 카드"다 — 시뮬레이션이 조용히 잘못된 값을 돌린다.
    /// </summary>
    [TestFixture]
    public class DataValidatorTests
    {
        static readonly string[] CardEffectTypes =
            { "damage", "block", "heal", "strength", "smudge", "draw", "energy", "recast" };
        static readonly string[] EnemyEffectTypes = { "attack", "block", "strengthen", "heal" };

        [Test]
        public void 카드_스키마가_온전하다()
        {
            var seen = new HashSet<string>();
            foreach (var c in Fix.Data.Cards)
            {
                Assert.That(c.id, Is.Not.Null.And.Not.Empty);
                Assert.That(seen.Add(c.id), Is.True, "카드 id 중복: " + c.id);
                Assert.That(c.nameKo, Is.Not.Null.And.Not.Empty, c.id + " 에 한국어 이름이 없다");
                Assert.That(c.textKo, Is.Not.Null.And.Not.Empty, c.id + " 에 한국어 설명이 없다");
                Assert.That(c.cost, Is.GreaterThanOrEqualTo(0), c.id + " 의 기력이 비었다(-1)");
                Assert.That(c.rarity, Is.AnyOf("common", "uncommon", "rare"), c.id);
                Assert.That(c.effects, Is.Not.Null.And.Not.Empty, c.id + " 에 효과가 없다");

                foreach (var e in c.effects)
                {
                    Assert.That(e.type, Is.AnyOf(CardEffectTypes), c.id + " 의 모르는 효과: " + e.type);
                    Assert.That(e.amount, Is.GreaterThan(0), c.id + " 의 " + e.type + " 값이 비었다");
                    Assert.That(e.target, Is.AnyOf("self", "enemy"), c.id);
                }
            }
        }

        [Test]
        public void 닳는_활자와_닳지_않는_활자가_스키마에서_갈린다()
        {
            // uses 하나로 판정한다. 따로 플래그를 두면 둘이 어긋나고, 어긋나면
            // "닳는다고 적혀 있는데 안 닳는" 카드가 생긴다.
            foreach (var c in Fix.Data.Cards)
            {
                if (c.IsConsumable)
                {
                    Assert.That(c.wearPerUsePct, Is.GreaterThanOrEqualTo(0),
                        c.id + " 는 닳는 활자인데 wearPerUsePct 가 비었다");
                    Assert.That(c.uses, Is.LessThanOrEqualTo(4),
                        c.id + " 의 횟수가 너무 많다 — 많으면 '아껴 둘까'가 질문이 아니게 된다");
                }
                else
                {
                    Assert.That(c.uses, Is.EqualTo(-1), c.id + " 는 닳지 않는 활자인데 uses 가 -1 이 아니다");
                    Assert.That(c.wearPerUsePct, Is.EqualTo(0), c.id + " 는 닳지 않는데 마모율이 있다");
                }
            }
        }

        [Test]
        public void 적_스키마가_온전하다()
        {
            var seen = new HashSet<string>();
            foreach (var e in Fix.Data.Enemies)
            {
                Assert.That(seen.Add(e.id), Is.True, "적 id 중복: " + e.id);
                Assert.That(e.nameKo, Is.Not.Null.And.Not.Empty);
                Assert.That(e.maxHp, Is.GreaterThan(0), e.id + " 의 체력이 비었다");
                Assert.That(e.pattern, Is.Not.Null.And.Not.Empty, e.id + " 에 행동 패턴이 없다");
                foreach (var p in e.pattern)
                {
                    Assert.That(p.type, Is.AnyOf(EnemyEffectTypes), e.id + " 의 모르는 행동: " + p.type);
                    Assert.That(p.amount, Is.GreaterThan(0), e.id + " 의 " + p.type + " 값이 비었다");
                }
            }
        }

        [Test]
        public void 회차가_가리키는_것이_전부_존재한다()
        {
            var run = Fix.Data.Run;
            foreach (var id in run.startingDeck)
                Assert.That(Fix.Data.HasCard(id), Is.True, "시작 덱의 없는 카드: " + id);
            foreach (var id in run.rewardPool)
                Assert.That(Fix.Data.HasCard(id), Is.True, "보상 목록의 없는 카드: " + id);
            foreach (var id in run.battles)
                Assert.That(Fix.Data.HasEnemy(id), Is.True, "전투 순서의 없는 적: " + id);
            Assert.That(Fix.Data.HasEnemy(run.boss), Is.True, "보스가 없다: " + run.boss);

            Assert.That(run.playerMaxHp, Is.GreaterThan(0));
            Assert.That(run.handSize, Is.GreaterThan(0));
            Assert.That(run.energyPerTurn, Is.GreaterThan(0));
            Assert.That(run.maxRoundsPerBattle, Is.GreaterThan(0));
            Assert.That(run.healAfterBattle, Is.GreaterThanOrEqualTo(0));
            Assert.That(run.rewardsPerBattle, Is.GreaterThanOrEqualTo(0));
            Assert.That(run.rewardRecastUses, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void 시작_덱에_닳는_활자가_들어_있다()
        {
            // 이게 없으면 규칙이 데이터에서 죽는다. `hybrid-*` 의 SeasonFeasibility 가
            // 두 번 잡아냈던 종류의 고장이다 — 코드는 맞는데 데이터가 규칙을 안 쓴다.
            int n = 0;
            foreach (var id in Fix.Data.Run.startingDeck)
                if (Fix.Data.Card(id).IsConsumable) n++;
            Assert.That(n, Is.GreaterThanOrEqualTo(3),
                "시작 덱에 닳는 활자가 " + n + "개뿐이다 — 규칙이 장식이 된다");
        }

        [Test]
        public void 한계값이_전부_채워져_있다()
        {
            var b = Fix.Data.Balance;
            var fields = new Dictionary<string, int>
            {
                ["monteCarloRuns"] = b.monteCarloRuns,
                ["dominanceRuns"] = b.dominanceRuns,
                ["dominanceMaxWinRatePct"] = b.dominanceMaxWinRatePct,
                ["randomWinRatePctMax"] = b.randomWinRatePctMax,
                ["randomBattleWinRatePctMin"] = b.randomBattleWinRatePctMin,
                ["randomBattleWinRatePctMax"] = b.randomBattleWinRatePctMax,
                ["measuredWinRatePctMin"] = b.measuredWinRatePctMin,
                ["measuredWinRatePctMax"] = b.measuredWinRatePctMax,
                ["deadCardMinAdoptionPct"] = b.deadCardMinAdoptionPct,
                ["seedBase"] = b.seedBase,
                ["minSharpnessPct"] = b.minSharpnessPct,
                ["hoardingMarginPct"] = b.hoardingMarginPct,
                ["spendNowMarginPct"] = b.spendNowMarginPct,
                ["measuredHpTriggerPct"] = b.measuredHpTriggerPct,
                ["measuredIncomingTriggerPct"] = b.measuredIncomingTriggerPct,
                ["attritionExhaustedRunPctMin"] = b.attritionExhaustedRunPctMin,
                ["attritionUsesPerRunMin"] = b.attritionUsesPerRunMin,
            };
            foreach (var kv in fields)
                Assert.That(kv.Value, Is.GreaterThan(-1), "balance.json 의 " + kv.Key + " 가 비었다");
            Assert.That(b.measuredEndgameBattlesLeft, Is.GreaterThanOrEqualTo(0));
            Assert.That(b.minSharpnessPct, Is.InRange(1, 100));
        }
    }
}
