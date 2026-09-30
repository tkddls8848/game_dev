using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 검사기 1 — `TickDeterminism`. **나머지 전부의 전제다.**
    /// 같은 씨드·같은 명령이 같은 결과를 내지 않으면 아래 검사기 일곱이 아무 말도 하지 못한다.
    ///
    /// 이 PoC에서 씨드가 정하는 것은 둘뿐이다: 오늘 밤의 위상, 그리고 **누가 무엇에 대해 거짓을 말하는가.**
    /// 그래서 재현성을 셋으로 나눠 확인한다 — 회차 전개 · 정보 시장 · 계획 탐색.
    /// </summary>
    [TestFixture]
    public sealed class TickDeterminismTests
    {
        [Test]
        public void 같은_계획을_여러_번_돌려도_같은_기록이다()
        {
            GameData d = TestWorld.Data;
            int runs = 0;
            foreach (string missionId in TestWorld.MissionIds())
            {
                SquadPlan plan = ReferencePlans.For(missionId);
                MissionResult first = MissionSim.Tonight(d, missionId).Run(plan);
                for (int i = 0; i < 40; i++)
                {
                    MissionResult again = MissionSim.Tonight(d, missionId).Run(plan);
                    runs++;
                    Assert.That(again.Won, Is.EqualTo(first.Won), missionId);
                    Assert.That(again.AlarmPercent, Is.EqualTo(first.AlarmPercent), missionId);
                    Assert.That(again.ReprisalPoints, Is.EqualTo(first.ReprisalPoints), missionId);
                    Assert.That(again.FieldTicks, Is.EqualTo(first.FieldTicks), missionId);
                    Assert.That(again.EndMs, Is.EqualTo(first.EndMs), missionId);
                    Assert.That(again.Log, Is.EqualTo(first.Log), missionId + " 의 기록이 갈렸다");
                }
            }
            TestContext.WriteLine("회차 " + runs + "번 재실행이 같은 기록을 냈다");
        }

        [Test]
        public void 같은_씨드가_같은_위상을_고른다()
        {
            GameData d = TestWorld.Data;
            foreach (MissionDef m in d.AllMissions)
            {
                string first = PhaseAssignment.Tonight(d, m).ToString();
                for (int i = 0; i < 20; i++)
                    Assert.That(PhaseAssignment.Tonight(d, m).ToString(), Is.EqualTo(first), m.id);
            }
        }

        [Test]
        public void 같은_씨드가_같은_거짓말쟁이를_고른다()
        {
            GameData d = TestWorld.Data;
            foreach (MissionDef m in d.AllMissions)
            {
                PhaseAssignment truth = PhaseAssignment.Tonight(d, m);
                List<string> first = LiarList(new Whispers(d, m, truth), d);
                for (int i = 0; i < 20; i++)
                    Assert.That(LiarList(new Whispers(d, m, truth), d), Is.EqualTo(first), m.id);
                TestContext.WriteLine(m.id + " 거짓: " + (first.Count == 0 ? "없음" : string.Join(" ", first)));
            }
        }

        [Test]
        public void 같은_답이_두_번_같다()
        {
            // 물은 답 자체가 흔들리면 겹쳐 묻기가 무의미해진다.
            GameData d = TestWorld.Data;
            foreach (MissionDef m in d.AllMissions)
            {
                Whispers market = new Whispers(d, m, PhaseAssignment.Tonight(d, m));
                foreach (IntelItemDef item in d.AllItems)
                    foreach (VillagerDef v in market.AvailableSources(item.id))
                    {
                        Rumor a = market.Answer(v.id, item.id);
                        Rumor b = market.Answer(v.id, item.id);
                        Assert.That(a.SameAs(b), item.id + " / " + v.id + " 의 답이 흔들린다");
                        Assert.That(a.IsTrue, Is.EqualTo(b.IsTrue));
                    }
            }
        }

        [Test]
        public void 탐색이_두_번_같은_답을_낸다()
        {
            GameData d = TestWorld.Data;
            string missionId = "m_ledger_theft";
            MissionSim sim = TestWorld.Sim(missionId);
            SearchResult a = PlanSearch.Beam(d, sim, TestWorld.Corroborated(missionId));
            SearchResult b = PlanSearch.Beam(d, sim, TestWorld.Corroborated(missionId));
            Assert.That(b.Runs, Is.EqualTo(a.Runs), "탐색이 돌린 회차 수가 다르다");
            Assert.That(b.Winners.Count, Is.EqualTo(a.Winners.Count));
            Assert.That(new List<string>(b.PlacementSignatures), Is.EqualTo(new List<string>(a.PlacementSignatures)));
        }

        private static List<string> LiarList(Whispers market, GameData d)
        {
            List<string> list = new List<string>();
            foreach (IntelItemDef item in d.AllItems)
            {
                string liar = market.LiarOf(item.id);
                if (liar != null) list.Add(item.id + "←" + liar);
            }
            return list;
        }
    }
}
