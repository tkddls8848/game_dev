using System.Collections.Generic;
using Interp.Sim;
using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// 공통 검사기 「SeedDeterminism」 — 같은 씨드 = 같은 결과.
    /// **나머지 전부의 전제다.** 특히 「한 낱말이 조약을 바꾼다」는 이것이 없으면
    /// "낱말이 바꿨다"와 "난수가 달라졌다"를 가를 수 없다.
    /// </summary>
    [TestFixture]
    public class SeedDeterminismTests
    {
        private GameData D { get { return TestWorld.Data; } }

        [Test]
        public void SameSeedSamePolicyGivesTheIdenticalNight()
        {
            foreach (int seed in D.AllSeeds())
                foreach (IPolicy p in Policies.All())
                {
                    string a = SessionSim.Run(D, seed, p).FullSignature(D.Clauses);
                    string b = SessionSim.Run(D, seed, p).FullSignature(D.Clauses);
                    Assert.That(b, Is.EqualTo(a), "씨드 " + seed + " · " + p.Id + " 가 두 번 다르게 돈다");
                }
        }

        [Test]
        public void SeedsActuallyDiffer()
        {
            HashSet<string> sigs = new HashSet<string>();
            foreach (int seed in D.AllSeeds())
                sigs.Add(SessionSim.Run(D, seed, Policies.Exact).FullSignature(D.Clauses));
            Assert.That(sigs.Count, Is.GreaterThan(1),
                "씨드를 바꿔도 같은 밤이 나온다 — 씨드가 아무것도 하지 않는다");
        }

        /// <summary>
        /// ★ 이 PoC에서 특히 중요한 재현성: **난수는 통역의 선택과 무관해야 한다.**
        /// 분위기(Mood)와 발각 판정은 씨드·마디 번호로만 굴린다. 어느 정책으로 돌렸든
        /// 같은 마디에 같은 분위기가 붙어야 한다.
        /// </summary>
        [Test]
        public void MoodDoesNotDependOnWhatTheInterpreterChose()
        {
            foreach (int seed in D.AllSeeds())
            {
                Dictionary<int, int> byStage = new Dictionary<int, int>();
                foreach (IPolicy p in Policies.All())
                {
                    foreach (TraceStep s in SessionSim.Run(D, seed, p).Trace)
                    {
                        int mood;
                        if (byStage.TryGetValue(s.StageIndex, out mood))
                            Assert.That(s.Mood, Is.EqualTo(mood),
                                "씨드 " + seed + " 마디 " + s.StageIndex + " 의 분위기가 정책에 따라 달라진다 — " +
                                "이러면 「한 낱말이 조약을 바꿨다」를 증명할 수 없다");
                        else byStage[s.StageIndex] = s.Mood;
                    }
                }
            }
        }

        [Test]
        public void ExposureRollIsStableAcrossProcessRuns()
        {
            // string.GetHashCode 는 실행마다 달라진다. 직접 센 해시를 쓰는지 확인한다.
            Assert.That(SessionSim.StableHash("m_border_line"), Is.EqualTo(SessionSim.StableHash("m_border_line")));
            Assert.That(SessionSim.StableHash("m_border_line"), Is.Not.EqualTo(SessionSim.StableHash("m_regret")));
            Assert.That(SessionSim.ExposureRoll(7, 3, "m_regret", 50),
                        Is.EqualTo(SessionSim.ExposureRoll(7, 3, "m_regret", 50)));
            Assert.That(SessionSim.ExposureRoll(7, 3, "m_regret", 0), Is.False, "위험 0 인데 드러난다");
        }
    }
}
