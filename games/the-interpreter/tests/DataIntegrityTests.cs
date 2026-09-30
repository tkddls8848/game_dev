using System.Collections.Generic;
using Interp.Data;
using Interp.Sim;
using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// 공통 검사기 「DataValidator」 — 참조 무결성.
    /// 나머지 검사기 전부가 이것을 전제한다. 여기서 터지면 아래 수치는 전부 의미가 없다.
    /// </summary>
    [TestFixture]
    public class DataIntegrityTests
    {
        private GameData D { get { return TestWorld.Data; } }

        [Test]
        public void EveryReferenceResolves()
        {
            HashSet<string> nationIds = new HashSet<string>();
            foreach (NationDef n in D.Nations.nations) nationIds.Add(n.id);

            foreach (StageDef st in D.Stages)
            {
                Assert.That(st.rounds, Is.Not.Null.And.Not.Empty, st.id + " 에 발화가 없다");
                foreach (RoundDef r in st.rounds)
                {
                    Assert.That(nationIds, Does.Contain(r.speaker), r.id + " 의 speaker");
                    Assert.That(nationIds, Does.Contain(r.listener), r.id + " 의 listener");
                    Assert.That(r.speaker, Is.Not.EqualTo(r.listener), r.id + " 은 혼잣말이다");
                    if (!string.IsNullOrEmpty(r.topicClause))
                        Assert.That(D.Clause(r.topicClause), Is.Not.Null, r.id + " 의 topicClause " + r.topicClause);
                    if (r.revisits != null)
                        foreach (string c in r.revisits)
                            Assert.That(D.Clause(c), Is.Not.Null, r.id + " 의 revisits " + c);
                    if (r.condition != null && !string.IsNullOrEmpty(r.condition.clauseId))
                    {
                        Assert.That(D.Clause(r.condition.clauseId), Is.Not.Null, r.id + " 조건의 clauseId");
                        if (!string.IsNullOrEmpty(r.condition.variantId))
                            Assert.That(D.ClauseOfVariant(r.condition.variantId), Is.EqualTo(r.condition.clauseId),
                                r.id + " 조건의 variantId 가 다른 조항의 것이다");
                        if (!string.IsNullOrEmpty(r.condition.notVariantId))
                            Assert.That(D.ClauseOfVariant(r.condition.notVariantId), Is.EqualTo(r.condition.clauseId),
                                r.id + " 조건의 notVariantId 가 다른 조항의 것이다");
                    }

                    foreach (RenderDef g in r.renderings)
                    {
                        Assert.That(g.register, Is.AnyOf("exact", "soft", "hard", "false"), g.id + " 의 결");
                        Assert.That(g.text, Is.Not.Null.And.Not.Empty, g.id + " 에 옮긴 말이 없다");
                        Assert.That(g.heardAs, Is.Not.Null.And.Not.Empty, g.id + " 에 '어떻게 들렸는가'가 없다");
                        bool hasClause = !string.IsNullOrEmpty(g.setsClause);
                        bool hasVariant = !string.IsNullOrEmpty(g.setsVariant);
                        Assert.That(hasClause, Is.EqualTo(hasVariant), g.id + " 의 setsClause/setsVariant 가 한쪽만 있다");
                        if (hasClause)
                        {
                            Assert.That(D.Clause(g.setsClause), Is.Not.Null, g.id + " 의 setsClause");
                            Assert.That(D.ClauseOfVariant(g.setsVariant), Is.EqualTo(g.setsClause),
                                g.id + " 의 setsVariant 가 그 조항의 것이 아니다");
                        }
                        if (!string.IsNullOrEmpty(g.seedsMisunderstanding))
                        {
                            MisunderstandingDef m = D.Misread(g.seedsMisunderstanding);
                            Assert.That(m, Is.Not.Null, g.id + " 가 심는 오해 " + g.seedsMisunderstanding);
                            Assert.That(g.exposureRiskPercent, Is.InRange(0, 100),
                                g.id + " 가 오해를 심으면서 드러날 확률을 적지 않았다");
                        }
                        Assert.That(g.revealBonusPercent, Is.InRange(-100, 100), g.id + " 의 revealBonusPercent");
                    }
                }
            }

            foreach (MisunderstandingDef m in D.Misunderstandings)
            {
                Assert.That(D.Clause(m.clauseId), Is.Not.Null, m.id + " 의 clauseId — 조항에 붙지 않은 오해는 영영 드러나지 않는다");
                Assert.That(m.aBelieves, Is.Not.Null.And.Not.Empty, m.id + " 에 하란이 믿는 것이 없다");
                Assert.That(m.bBelieves, Is.Not.Null.And.Not.Empty, m.id + " 에 케리아가 믿는 것이 없다");
                Assert.That(m.aBelieves, Is.Not.EqualTo(m.bBelieves), m.id + " 은 오해가 아니다 — 양쪽이 같은 것을 믿는다");
                Assert.That(m.consequence, Is.Not.Null.And.Not.Empty, m.id + " 에 결과가 없다");
            }
        }

        [Test]
        public void EveryClauseHasAtLeastTwoVariantsAndEveryVariantIsReachable()
        {
            HashSet<string> reachable = new HashSet<string>();
            foreach (StageDef st in D.Stages)
                foreach (RoundDef r in st.rounds)
                    foreach (RenderDef g in r.renderings)
                        if (!string.IsNullOrEmpty(g.setsVariant)) reachable.Add(g.setsVariant);

            foreach (ClauseDef c in D.Clauses)
            {
                Assert.That(c.variants.Length, Is.GreaterThanOrEqualTo(2), c.id + " 의 변형이 둘 미만이다 — 고를 것이 없는 조항이다");
                Assert.That(c.unsetText, Is.Not.Null.And.Not.Empty, c.id + " 에 '비워 둔 경우'의 문구가 없다");
                foreach (VariantDef v in c.variants)
                    Assert.That(reachable, Does.Contain(v.id),
                        v.id + " 는 어떤 역어로도 닿을 수 없다 — 쓸모없는 선택지다");
            }
        }

        [Test]
        public void EveryRoundHasAFaithfulRenderingAndNoDuplicateIds()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (StageDef st in D.Stages)
            {
                foreach (RoundDef r in st.rounds)
                {
                    Assert.That(seen.Add(r.id), Is.True, "발화 id 가 겹친다: " + r.id);
                    int exact = 0;
                    Assert.That(r.renderings.Length, Is.InRange(3, 5), r.id + " 의 후보 역어는 3~5개여야 한다");
                    foreach (RenderDef g in r.renderings)
                    {
                        Assert.That(seen.Add(g.id), Is.True, "역어 id 가 겹친다: " + g.id);
                        if (g.register == "exact") exact++;
                    }
                    Assert.That(exact, Is.EqualTo(1), r.id + " 에 정확한 역어가 하나여야 한다 (있는 것: " + exact + ")");
                }
                // 마디마다 조건 없는 발화가 하나는 있어야 한다. 없으면 상태에 따라 아무 말도 나오지 않는다.
                bool unconditional = false;
                foreach (RoundDef r in st.rounds) if (r.condition == null) unconditional = true;
                Assert.That(unconditional, Is.True, st.id + " 에 조건 없는 발화가 없다");
                Assert.That(st.rounds[st.rounds.Length - 1].condition, Is.Null,
                    st.id + " 의 마지막 발화에 조건이 붙어 있다 — 떨어질 자리가 없다");
            }
        }

        [Test]
        public void StageIndicesAreContiguousAndOrdered()
        {
            for (int i = 0; i < D.Stages.Count; i++)
                Assert.That(D.Stages[i].index, Is.EqualTo(i), D.Stages[i].id + " 의 index");
        }

        [Test]
        public void EveryEndingIsReachableUnderSomePolicyOrIsDocumented()
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (int seed in D.AllSeeds())
                foreach (IPolicy p in Policies.All())
                    seen.Add(SessionSim.Run(D, seed, p).EndingId);
            foreach (string s in seen)
            {
                bool known = false;
                foreach (EndingDef e in D.Endings.endings) if (e.id == s) known = true;
                Assert.That(known, Is.True, "정책들이 데이터에 없는 결말 " + s + " 에 닿았다");
            }
            TestContext.Out.WriteLine("정책 여섯이 닿은 결말: " + string.Join(", ", seen));
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(4), "결말이 " + seen.Count + "가지뿐이다 — 결말이 갈리지 않는다");
        }

        [Test]
        public void BalanceNumbersAreSaneIntegers()
        {
            BalanceFile b = D.Balance;
            Assert.That(b.seed, Is.Not.Zero);
            Assert.That(b.extraSeeds, Is.Not.Null.And.Not.Empty, "씨드 하나로 정책을 견주면 안 된다");
            Assert.That(D.AllSeeds().Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(b.startTension, Is.InRange(0, 100));
            Assert.That(b.collapseTension, Is.InRange(b.startTension + 1, 100));
            Assert.That(b.signTrustMin, Is.InRange(1, 99));
            Assert.That(b.moodJitterMin, Is.LessThanOrEqualTo(b.moodJitterMax));
            Assert.That(b.objectives.Length, Is.GreaterThanOrEqualTo(3), "자가 둘 이하면 「안전한 낱말이 없다」가 물을 것이 없다");
            HashSet<string> oids = new HashSet<string>();
            foreach (ObjectiveDef o in b.objectives)
            {
                Assert.That(oids.Add(o.id), Is.True, "목표 id 가 겹친다: " + o.id);
                Assert.That(o.whose, Is.Not.Null.And.Not.Empty, o.id + " 를 누가 재는지 적혀 있지 않다");
            }
        }
    }
}
