using System.Collections.Generic;
using NUnit.Framework;
using RedPen.Data;
using RedPen.Sim;

namespace RedPen.Tests
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
            HashSet<string> ids = new HashSet<string>();
            foreach (FlawDef f in D.AllFlaws)
            {
                Assert.That(ids.Add(f.id), Is.True, "흠 id 가 겹친다: " + f.id);
                Assert.That(f.weight, Is.GreaterThan(0), f.id + " 의 무게가 0이다 — 있으나 마나 한 흠이다");
                Assert.That(f.symbol, Is.Not.Null.And.Not.Empty, f.id + " 에 교정 부호가 없다");
                Assert.That(f.note, Is.Not.Null.And.Not.Empty, f.id + " 에 설명이 없다");
            }

            foreach (SentenceDef s in D.AllSentences)
            {
                Assert.That(ids.Add(s.id), Is.True, "문장 id 가 겹친다: " + s.id);
                Assert.That(s.text, Is.Not.Null.And.Not.Empty, s.id + " 에 글이 없다");
                Assert.That(s.quality, Is.InRange(0, 100), s.id + " 의 quality");
                Assert.That(s.voice, Is.InRange(0, 100), s.id + " 의 voice");
                Assert.That(s.prideGuard, Is.InRange(0, 100), s.id + " 의 prideGuard");
                Assert.That(s.kind, Is.AnyOf("k_open", "k_body", "k_turn", "k_close"), s.id + " 의 kind");
                if (s.flaws != null)
                    foreach (string f in s.flaws)
                        Assert.That(D.Flaw(f), Is.Not.Null, s.id + " 의 흠 " + f + " 가 데이터에 없다");
            }

            foreach (MarkDef m in D.AllMarks)
            {
                Assert.That(ids.Add(m.id), Is.True, "부호 id 가 겹친다: " + m.id);
                Assert.That(m.harshness, Is.InRange(0, 10), m.id + " 의 harshness");
                Assert.That(m.note, Is.Not.Null.And.Not.Empty, m.id + " 에 설명이 없다");
                if (m.fixes != null)
                    foreach (string f in m.fixes)
                        if (f != "*") Assert.That(D.Flaw(f), Is.Not.Null, m.id + " 가 고친다는 흠 " + f);
                if (!string.IsNullOrEmpty(m.introducesFlaw))
                {
                    Assert.That(D.Flaw(m.introducesFlaw), Is.Not.Null, m.id + " 가 만든다는 흠 " + m.introducesFlaw);
                    Assert.That(m.introducesPercent, Is.InRange(1, 100), m.id + " 의 introducesPercent");
                }
            }
        }

        [Test]
        public void EveryFlawIsFixableAndEveryMarkCanDoSomething()
        {
            foreach (FlawDef f in D.AllFlaws)
            {
                bool fixable = false;
                foreach (MarkDef m in D.AllMarks)
                {
                    if (m.fixes == null) continue;
                    foreach (string x in m.fixes) if (x == "*" || x == f.id) fixable = true;
                }
                Assert.That(fixable, Is.True, f.id + " 를 고칠 수 있는 부호가 하나도 없다 — 붉은 펜이 못 닿는 흠이다");
            }

            foreach (FlawDef f in D.AllFlaws)
            {
                bool present = false;
                foreach (SentenceDef s in D.AllSentences)
                    if (s.flaws != null) foreach (string x in s.flaws) if (x == f.id) present = true;
                Assert.That(present, Is.True, f.id + " 를 가진 문장이 원고에 하나도 없다 — 쓰이지 않는 흠이다");
            }

            foreach (MarkDef m in D.AllMarks)
            {
                if (m.harshness == 0) continue;   // 칭찬·그대로 두기는 고치는 것이 일이 아니다
                Assert.That(m.fixes, Is.Not.Null.And.Not.Empty, m.id + " 는 거칠기만 하고 고치는 것이 없다");
            }
        }

        [Test]
        public void TheManuscriptIsNotAlreadyGoodAndNotHopeless()
        {
            RunResult untouched = ManuscriptSim.Run(D, D.Balance.seed, Policies.NoPen);
            TestContext.Out.WriteLine("손대지 않은 원고: 질 " + untouched.Quality + " · 목소리 " + untouched.Voice
                                      + " · 실을 수 " + (untouched.Publishable ? "있다" : "없다"));
            Assert.That(untouched.Publishable, Is.False,
                "손대지 않아도 실을 수 있는 원고다 — 편집자가 할 일이 없다");
            Assert.That(untouched.Quality, Is.GreaterThan(20),
                "손대지 않은 원고가 너무 나쁘다 — 무엇을 해도 올라오지 못한다");

            int flawed = 0, clean = 0;
            foreach (SentenceDef s in D.AllSentences)
                if (s.flaws != null && s.flaws.Length > 0) flawed++; else clean++;
            TestContext.Out.WriteLine("흠 있는 문장 " + flawed + " · 흠 없는 문장 " + clean);
            Assert.That(flawed, Is.GreaterThanOrEqualTo(4), "고칠 문장이 너무 적다");
            Assert.That(clean, Is.GreaterThanOrEqualTo(2),
                "흠 없는 문장이 둘 미만이다 — 붉은 펜을 참는 선택이 없어진다");
        }

        [Test]
        public void ThereIsASentenceWorthKeepingAndOneWorthCutting()
        {
            SentenceDef best = null, worst = null;
            foreach (SentenceDef s in D.AllSentences)
            {
                if (best == null || s.voice > best.voice) best = s;
                int burden = 0;
                if (s.flaws != null) foreach (string f in s.flaws) burden += D.Flaw(f).weight;
                int eff = s.quality - burden;
                if (worst == null) worst = s;
                else
                {
                    int wb = 0;
                    if (worst.flaws != null) foreach (string f in worst.flaws) wb += D.Flaw(f).weight;
                    if (eff < worst.quality - wb) worst = s;
                }
            }
            TestContext.Out.WriteLine("아껴야 하는 문장 " + best.id + " (목소리 " + best.voice
                                      + " · 애착 " + best.prideGuard + ") · 지워야 하는 문장 " + worst.id
                                      + " (애착 " + worst.prideGuard + ")");
            Assert.That(best.prideGuard, Is.GreaterThanOrEqualTo(70),
                "가장 좋은 문장에 작가가 붙어 있지 않다 — 삭제선이 위험하지 않다");
            Assert.That(worst.prideGuard, Is.LessThanOrEqualTo(30),
                "가장 나쁜 문장에 작가가 붙어 있다 — 지우기가 언제나 벌이 된다");
        }

        [Test]
        public void BalanceNumbersAreSaneIntegers()
        {
            BalanceFile b = D.Balance;
            AuthorFile a = D.Author;
            Assert.That(b.seed, Is.Not.Zero);
            Assert.That(b.extraSeeds, Is.Not.Null.And.Not.Empty, "씨드 하나로 방침을 견주면 안 된다");
            Assert.That(D.AllSeeds().Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(b.rounds, Is.GreaterThanOrEqualTo(3), "원고가 세 번은 오가야 관계가 쌓인다");
            Assert.That(b.objectives.Length, Is.GreaterThanOrEqualTo(3),
                "자가 둘 이하면 「혹독함은 양날이다」가 물을 것이 없다");
            HashSet<string> oids = new HashSet<string>();
            foreach (ObjectiveDef o in b.objectives)
            {
                Assert.That(oids.Add(o.id), Is.True, "자 id 가 겹친다: " + o.id);
                Assert.That(o.whose, Is.Not.Null.And.Not.Empty, o.id + " 를 누가 재는지 적혀 있지 않다");
            }
            Assert.That(a.startConfidence, Is.InRange(1, 99));
            Assert.That(a.startTrust, Is.InRange(1, 99));
            Assert.That(a.timidConfidence, Is.LessThan(a.goodConfidence));
            Assert.That(a.dayJitterMin, Is.LessThanOrEqualTo(a.dayJitterMax));
            Assert.That(a.quitTrust, Is.LessThan(a.startTrust));
        }

        [Test]
        public void EveryEndingUsedByAPolicyExistsAndEnoughEndingsAreReachable()
        {
            HashSet<string> seen = new HashSet<string>();
            List<IPolicy> all = new List<IPolicy>(Policies.All());
            all.AddRange(Policies.Singles(D));
            foreach (IPolicy p in all)
                foreach (int seed in D.AllSeeds())
                    seen.Add(ManuscriptSim.Run(D, seed, p).EndingId);
            foreach (string s in seen)
            {
                bool known = false;
                foreach (EndingDef e in D.Endings.endings) if (e.id == s) known = true;
                Assert.That(known, Is.True, "방침들이 데이터에 없는 결말 " + s + " 에 닿았다");
            }
            TestContext.Out.WriteLine("방침들이 닿은 결말 " + seen.Count + "가지: " + string.Join(", ", seen));
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(5), "결말이 " + seen.Count + "가지뿐이다");
        }
    }
}
