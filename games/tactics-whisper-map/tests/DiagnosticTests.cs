using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 판정하지 않고 **수치를 찍는다.** 설계를 고칠 때 이 출력을 먼저 본다.
    /// POC_FACTORY §5: 기계가 보는 것과 사람이 보는 것을 섞지 않는다 — 여기는 기계가 본 것을 사람에게 넘기는 자리다.
    /// </summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 지도와_순찰을_찍는다()
        {
            GameData d = TestWorld.Data;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                TestContext.WriteLine("── " + missionId + " (" + sim.Mission.name + ") 위상 " + sim.Phases);
                foreach (string g in sim.Patrols.GuardIds)
                {
                    List<string> parts = new List<string>();
                    foreach (PatrolWindow w in sim.Patrols.Windows(g))
                        parts.Add(w.Zone + " " + w.FromMs / 1000 + "-" + w.ToMs / 1000 + "s");
                    TestContext.WriteLine("  " + g + ": " + string.Join(" · ", parts));
                    TestContext.WriteLine("    골목을 비우는 최장 구간: "
                        + sim.Patrols.LongestAbsenceMs(g, "z_lane") / 1000 + "s");
                }
            }
            Assert.Pass();
        }

        [Test]
        public void 은폐_셈을_찍는다()
        {
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_ledger_theft");
            foreach (MemberDef m in d.AllMembers)
                foreach (string z in sim.Graph.ZoneIds)
                {
                    int bare = m.stealthPercent + sim.Graph.CoverPercent(z);
                    int settled = bare + sim.Graph.BestConcealmentBonus(z);
                    TestContext.WriteLine(m.id + " @ " + z + " : 맨몸 " + bare + " · 묻고 " + settled
                        + (settled >= d.Balance.vision.concealFloorPercent ? "  ← 묻으면 보이지 않는다" : ""));
                }
            Assert.Pass();
        }

        [Test]
        public void 소음_전파를_찍는다()
        {
            MissionSim sim = TestWorld.Sim("m_dusk_checkpoint");
            int[] loudness = { 90, 100, 25, 10 };
            string[] names = { "총성", "폭음", "함정", "설치" };
            for (int i = 0; i < loudness.Length; i++)
                foreach (string z in sim.Graph.ZoneIds)
                {
                    List<string> reach = new List<string>();
                    foreach (NoiseArrival a in sim.Noise.Propagate(z, loudness[i]))
                        reach.Add(a.Zone + ":" + a.Loudness + "(" + a.Hops + "홉)");
                    TestContext.WriteLine(names[i] + " " + loudness[i] + " @ " + z + " → "
                        + (reach.Count == 0 ? "아무 데도 안 들린다" : string.Join(" ", reach)));
                }
            Assert.Pass();
        }
    }
}
