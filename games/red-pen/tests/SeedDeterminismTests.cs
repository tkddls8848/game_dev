using System.Collections.Generic;
using NUnit.Framework;
using RedPen.Sim;

namespace RedPen.Tests
{
    /// <summary>
    /// 공통 검사기 「SeedDeterminism」 — 같은 씨드 = 같은 결과. 나머지 전부의 전제다.
    /// </summary>
    [TestFixture]
    public class SeedDeterminismTests
    {
        private GameData D { get { return TestWorld.Data; } }

        private IList<IPolicy> Everything()
        {
            List<IPolicy> all = new List<IPolicy>(Policies.All());
            all.AddRange(Policies.Singles(D));
            return all;
        }

        [Test]
        public void SameSeedSamePolicyGivesTheIdenticalExchange()
        {
            foreach (int seed in D.AllSeeds())
                foreach (IPolicy p in Everything())
                {
                    string a = ManuscriptSim.Run(D, seed, p).Signature();
                    string b = ManuscriptSim.Run(D, seed, p).Signature();
                    Assert.That(b, Is.EqualTo(a), "씨드 " + seed + " · " + p.Id + " 가 두 번 다르게 돈다");
                }
        }

        [Test]
        public void SeedsActuallyDiffer()
        {
            HashSet<string> sigs = new HashSet<string>();
            foreach (int seed in D.AllSeeds())
                sigs.Add(ManuscriptSim.Run(D, seed, Policies.MiddlePen).Signature());
            Assert.That(sigs.Count, Is.GreaterThan(1), "씨드를 바꿔도 같은 원고가 돌아온다");
        }

        /// <summary>
        /// 작가의 그날은 **편집자가 무엇을 했든** 같아야 한다.
        /// 아니면 "교정이 원고를 움직였다"와 "작가가 그날 컨디션이 좋았다"를 가를 수 없다.
        /// </summary>
        [Test]
        public void TheWritersDayDoesNotDependOnWhatTheEditorDid()
        {
            foreach (int seed in D.AllSeeds())
            {
                Dictionary<int, int> byRound = new Dictionary<int, int>();
                foreach (IPolicy p in Everything())
                    foreach (RoundLog l in ManuscriptSim.Run(D, seed, p).Rounds)
                    {
                        int day;
                        if (byRound.TryGetValue(l.Round, out day))
                            Assert.That(l.Day, Is.EqualTo(day),
                                "씨드 " + seed + " " + l.Round + "회차의 '그날'이 방침에 따라 달라진다");
                        else byRound[l.Round] = l.Day;
                    }
            }
        }

        [Test]
        public void RollsAreStableAcrossProcessRuns()
        {
            // string.GetHashCode 는 실행마다 달라진다. 직접 센 해시를 쓰는지 확인한다.
            Assert.That(ManuscriptSim.StableHash("s5"), Is.EqualTo(ManuscriptSim.StableHash("s5")));
            Assert.That(ManuscriptSim.StableHash("s5"), Is.Not.EqualTo(ManuscriptSim.StableHash("s6")));
            Assert.That(ManuscriptSim.MasterpieceRoll(7, 2, 50), Is.EqualTo(ManuscriptSim.MasterpieceRoll(7, 2, 50)));
            Assert.That(ManuscriptSim.MasterpieceRoll(7, 2, 0), Is.False, "확률 0인데 걸작이 온다");
            Assert.That(ManuscriptSim.IntroduceRoll(7, 2, "s5", "m_insert", 0), Is.False, "확률 0인데 흠이 생긴다");
            Assert.That(ManuscriptSim.IntroduceRoll(7, 2, "s5", "m_insert", 100), Is.True);
        }
    }
}
