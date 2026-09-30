using System.Collections.Generic;
using NUnit.Framework;
using Scent.Data;
using Scent.Sim;

namespace Scent.Tests
{
    /// <summary>
    /// 판정하지 않고 **수치를 찍는다**. 데이터를 고칠 때 보는 창이다.
    /// 검사기가 막혔을 때 기준값을 내리는 대신 여기 숫자를 보고 데이터를 고친다.
    /// </summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 사실마다_맡을_수_있는_창을_찍는다()
        {
            GameData d = TestWorld.Data;
            Investigation on = TestWorld.Inv(World.On);
            TestContext.WriteLine("사실 14개 · 섞임o·감쇠o 세계 · 조사 " +
                TestWorld.Clock(d.Balance.investigation.startMin) + "~" + TestWorld.Clock(d.Balance.investigation.endMin));
            TestContext.WriteLine("id   방            사람          방문     창(열림~닫힘)    분  가장이른도착");
            foreach (VisitDef v in d.Day.visits)
            {
                List<int> w = on.WindowOf(v.id);
                string win = w.Count == 0 ? "       없다      " :
                    TestWorld.Clock(w[0]) + "~" + TestWorld.Clock(w[w.Count - 1]);
                TestContext.WriteLine(v.id + "  " + d.Room(v.room).id.PadRight(13) + d.Actor(v.actor).id.PadRight(15)
                    + TestWorld.Clock(v.atMin) + "  " + win.PadRight(16) + w.Count.ToString().PadLeft(4)
                    + "  " + TestWorld.Clock(on.EarliestReach(v.room)));
            }
        }

        [Test]
        public void 네_세계의_한_번_훑기를_견준다()
        {
            World[] worlds = { World.Off, World.MixingOnly, World.DecayOnly, World.On };
            foreach (World w in worlds)
            {
                SweepReport s = TestWorld.Sweep(w);
                RouteResult r = TestWorld.Revisit(w);
                TestContext.WriteLine(w.Name + " | 한 번 훑기 최고 " + s.bestCount + "/" + TestWorld.FactCount
                    + " · 평균 " + (s.totalRecovered / s.sweeps) + " · 순열 " + s.sweeps
                    + " | 되짚기 " + r.recovered.Count + "/" + TestWorld.FactCount
                    + " (맡은 횟수 " + r.steps.Count + " · 되돌아간 방 " + r.RevisitedRooms + " · 끝 " + TestWorld.Clock(r.endMin) + ")");
                TestContext.WriteLine("    되짚기 길: " + r.RouteText);
            }
        }

        [Test]
        public void 방마다_층을_찍는다()
        {
            GameData d = TestWorld.Data;
            ScentField f = TestWorld.Field();
            foreach (string room in d.RoomIds)
            {
                List<string> parts = new List<string>();
                foreach (Layer L in f.InRoom(room))
                    parts.Add((L.IsVisit ? d.Actor(L.actorId).id : "잡내") + "@" + TestWorld.Clock(L.atMin));
                TestContext.WriteLine(room.PadRight(14) + string.Join("  ", parts));
            }
        }
    }
}
