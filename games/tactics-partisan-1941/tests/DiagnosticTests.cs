using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기가 아니다. 데이터를 손으로 읽을 수 있게 펼쳐 보여 주는 진단이다.
    /// (검사기가 실패했을 때 어디가 어긋났는지 보려고 둔다 — 늘 통과한다)
    /// </summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 순찰_시간축을_펼쳐_본다()
        {
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                Console.WriteLine("=== " + missionId + " (" + sim.Mission.mapId + ") ===");
                foreach (string g in sim.Patrols.GuardIds)
                {
                    Console.WriteLine("  " + g + " 주기 " + sim.Patrols.CycleMs(g)
                                      + "ms 위상 " + sim.Patrols.BasePhaseMs(g) + "ms");
                    foreach (PatrolWindow w in sim.Patrols.Windows(g))
                    {
                        if (w.FromMs > 360000) break;
                        Console.WriteLine("      [" + w.FromMs + "," + w.ToMs + ") " + w.Zone);
                    }
                }
                ReconReport recon = TestWorld.Recon(missionId);
                Console.WriteLine("  정찰 가능 구역: " + string.Join(", ", recon.ObservableZones));
                Console.WriteLine("  정찰 사각 구간: " + recon.HiddenWindows.Count);
            }
            Assert.Pass();
        }

        [Test]
        public void 참조_계획을_돌려_본다()
        {
            foreach (KeyValuePair<string, SquadPlan> kv in ReferencePlans.ByMission())
            {
                MissionSim sim = TestWorld.Sim(kv.Key);
                MissionResult r = sim.Run(kv.Value);
                Console.WriteLine("=== " + kv.Key + " ===");
                Console.WriteLine("  " + kv.Value);
                Console.WriteLine("  Won=" + r.Won + " reason=" + r.FailureReason + " alarm=" + r.Alarm
                                  + " guardsDown=" + string.Join(",", r.GuardsDown)
                                  + " targets=" + string.Join(",", r.TargetsDestroyed)
                                  + " intel=" + string.Join(",", r.IntelSeized)
                                  + " membersDown=" + string.Join(",", r.MembersDown));
                foreach (SightingEvent s in r.Sightings)
                    Console.WriteLine("    S " + s.AtMs + " " + s.GuardId + "@" + s.GuardZone
                                      + " -> " + s.MemberId + "@" + s.MemberZone + " conceal=" + s.Concealment);
                foreach (NoiseHeardEvent n in r.NoiseHeard)
                    Console.WriteLine("    N " + n.AtMs + " " + n.GuardId + "@" + n.GuardZone
                                      + " <- " + n.OriginZone + " " + n.SourceKind + " " + n.Loudness);
                foreach (string w in r.WastedActions) Console.WriteLine("    ! " + w);
            }
            Assert.Pass();
        }

        [Test]
        public void 저격_참조_계획도_돌려_본다()
        {
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            MissionResult r = sim.Run(ReferencePlans.RidgeDuskShots());
            Console.WriteLine("저격 변형: Won=" + r.Won + " reason=" + r.FailureReason + " alarm=" + r.Alarm
                              + " down=" + string.Join(",", r.GuardsDown));
            foreach (SightingEvent s in r.Sightings)
                Console.WriteLine("    S " + s.AtMs + " " + s.GuardId + "@" + s.GuardZone
                                  + " -> " + s.MemberId + "@" + s.MemberZone + " conceal=" + s.Concealment);
            foreach (NoiseHeardEvent n in r.NoiseHeard)
                Console.WriteLine("    N " + n.AtMs + " " + n.GuardId + "@" + n.GuardZone + " <- " + n.OriginZone);
            foreach (string w in r.WastedActions) Console.WriteLine("    ! " + w);
            Assert.Pass();
        }

        [Test]
        public void 탐색_결과를_펼쳐_본()
        {
            foreach (string missionId in TestWorld.MissionIds())
            {
                DateTime t0 = DateTime.UtcNow;
                SearchResult sr = TestWorld.Search(missionId);
                Console.WriteLine("=== " + missionId + " 계획 " + sr.PlansSimulated + "개 돌렸다, "
                                  + (int)(DateTime.UtcNow - t0).TotalMilliseconds + "ms, 이기는 계획 "
                                  + sr.Winners.Count + "개");
                int n = 0;
                foreach (SquadPlan p in sr.Winners)
                {
                    Console.WriteLine("    " + p);
                    if (++n >= 8) break;
                }
                Console.WriteLine("  절차: " + string.Join(" // ", sr.ActionSignatures()));
                Console.WriteLine("  없으면 못 푸는 사람: " + string.Join(",", sr.IndispensableMembers()));
            }
            Assert.Pass();
        }

    }
}
