using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 6 — `SupplyEconomy`.
    /// **N회 임무 시뮬레이션에서 보급이 발산하거나 고갈하지 않는가.**
    /// 깨지면 3회차에 자원이 무의미해지거나 진행이 막힌다 (PLAN_TACTICS §5).
    /// </summary>
    [TestFixture]
    public sealed class SupplyEconomyTests
    {
        [Test]
        public void N회를_돌려도_발산하지도_고갈하지도_않는다()
        {
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            CampTrace trace = CampSim.Run(cb.economyMissions);

            foreach (SupplyDef s in d.Camp.supplies)
            {
                int start = trace.Start[s.id];
                int final = trace.Final[s.id];
                int drift = final > start ? final - start : start - final;

                TestContext.WriteLine(s.name + ": 시작 " + start + " → 끝 " + final
                                      + " (최소 " + trace.Min[s.id] + " 최대 " + trace.Max[s.id]
                                      + ", 상한 " + trace.CeilingClampRounds[s.id] + "회 · 바닥 "
                                      + trace.FloorClampRounds[s.id] + "회)");

                Assert.That(trace.CeilingClampRounds[s.id], Is.LessThanOrEqualTo(cb.maxCeilingClampRounds),
                    s.name + " 이 저장 한계에 " + trace.CeilingClampRounds[s.id]
                    + "회 붙었다 — 넘치면 자원이 무의미해진다");
                Assert.That(trace.FloorClampRounds[s.id], Is.LessThanOrEqualTo(cb.maxFloorClampRounds),
                    s.name + " 이 바닥을 " + trace.FloorClampRounds[s.id] + "회 뚫었다 — 진행이 막힌다");
                Assert.That(drift, Is.LessThanOrEqualTo(cb.supplyDriftTolerance),
                    s.name + " 이 " + drift + " 만큼 한쪽으로 쏠렸다 (허용 " + cb.supplyDriftTolerance + ")");
                Assert.That(trace.Min[s.id], Is.GreaterThanOrEqualTo(s.floor), s.name);
                Assert.That(trace.Max[s.id], Is.LessThanOrEqualTo(s.ceiling), s.name);
            }
        }

        [Test]
        public void 진행이_막히지_않는다()
        {
            GameData d = TestWorld.Data;
            int rounds = d.Balance.checkers.economyMissions;
            CampTrace trace = CampSim.Run(rounds);

            TestContext.WriteLine("임무 " + trace.MissionsAttempted + "회 시도 · " + trace.MissionsSucceeded
                                  + "회 성공 · 쉰 회차 " + trace.RestRounds
                                  + " · 손실 " + trace.MembersLost + " · 복귀 " + trace.MembersRecovered);

            Assert.That(trace.MissionsAttempted, Is.GreaterThanOrEqualTo(rounds * 2 / 3),
                "임무를 나갈 수 있었던 회차가 " + trace.MissionsAttempted + "/" + rounds + "뿐이다");
            Assert.That(trace.BlockedRounds, Is.LessThanOrEqualTo(rounds / 3),
                "아무 임무도 나갈 수 없었던 회차가 " + trace.BlockedRounds + "회다");

            // 세 임무가 다 쓰이는가 — 하나가 늘 최적이면 다른 둘은 장식이다.
            Dictionary<string, int> used = new Dictionary<string, int>();
            foreach (MissionDef m in d.AllMissions) used[m.id] = 0;
            foreach (CampRound r in trace.Rounds) if (r.Attempted) used[r.MissionId]++;
            foreach (MissionDef m in d.AllMissions)
                Assert.That(used[m.id], Is.GreaterThan(0), m.id + " 가 " + rounds + "회 동안 한 번도 쓰이지 않았다");
            TestContext.WriteLine("임무별 횟수: " + string.Join(" · ", Format(used)));
        }

        [Test]
        public void 고칠_것을_사면_보급이_흘러나간다()
        {
            // 발산을 막는 밸브가 실제로 돈다는 확인. 아무것도 사지 않으면 보급은 위로만 쌓인다.
            CampTrace trace = CampSim.Run(TestWorld.Data.Balance.checkers.economyMissions);
            int bought = 0;
            foreach (CampRound r in trace.Rounds) if (r.BoughtRepairId != "") bought++;
            Assert.That(bought, Is.GreaterThan(0), "거점에서 아무것도 고치지 않았다 — 남는 보급이 흘러나갈 데가 없다");
            Assert.That(bought, Is.LessThanOrEqualTo(TestWorld.Data.Camp.repairs.Length),
                "같은 것을 여러 번 고쳤다");
        }

        [Test]
        public void 거점_정산도_재현된다()
        {
            int rounds = TestWorld.Data.Balance.checkers.economyMissions;
            Assert.That(CampSim.Run(rounds).LedgerHash, Is.EqualTo(CampSim.Run(rounds).LedgerHash),
                "같은 씨드로 두 번 돌렸는데 장부가 다르다");
        }

        [Test]
        public void 임무마다_값이_다르다()
        {
            // 값이 같으면 경제가 선택을 만들지 않는다.
            HashSet<string> costs = new HashSet<string>();
            foreach (string missionId in TestWorld.MissionIds())
            {
                Dictionary<string, int> cost = CampSim.CheapestCost(missionId);
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, int> kv in cost) parts.Add(kv.Key + ":" + kv.Value);
                parts.Sort();
                string key = string.Join(",", parts);
                costs.Add(key);
                TestContext.WriteLine(missionId + " 최저 비용 " + key);
            }
            Assert.That(costs.Count, Is.GreaterThan(1), "모든 임무의 값이 같다");
        }

        private static List<string> Format(Dictionary<string, int> used)
        {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> kv in used) parts.Add(kv.Key + " " + kv.Value + "회");
            parts.Sort();
            return parts;
        }
    }

    /// <summary>
    /// 거점 시뮬레이션을 검사기 둘(SupplyEconomy · AttritionRecoverable)이 나눠 쓴다.
    /// 임무 비용은 **탐색이 찾은 가장 싼 승리안**에서 뽑는다 — 사람이 정한 값이 아니다.
    /// </summary>
    public static class CampSim
    {
        private static readonly Dictionary<string, Dictionary<string, int>> CostCache =
            new Dictionary<string, Dictionary<string, int>>();

        public static Dictionary<string, int> CheapestCost(string missionId)
        {
            Dictionary<string, int> cached;
            if (CostCache.TryGetValue(missionId, out cached)) return cached;

            CampLedger ledger = new CampLedger(TestWorld.Data);
            Dictionary<string, int> best = null;
            int bestTotal = int.MaxValue;
            foreach (SquadPlan p in TestWorld.Search(missionId).Winners)
            {
                Dictionary<string, int> cost = ledger.CostOfPlan(p);
                int total = 0;
                foreach (KeyValuePair<string, int> kv in cost) total += kv.Value;
                if (total < bestTotal) { bestTotal = total; best = cost; }
            }
            Assert.That(best, Is.Not.Null, missionId + " 에 승리안이 없어 비용을 뽑을 수 없다");
            CostCache[missionId] = best;
            return best;
        }

        public static CampTrace Run(int rounds)
        {
            GameData d = TestWorld.Data;
            CampLedger ledger = new CampLedger(d);
            return ledger.Simulate(rounds, CheapestCost, Solvable);
        }

        /// <summary>이 분대원들만으로 이 임무를 풀 수 있는가 — 탐색이 답한다.</summary>
        public static bool Solvable(string missionId, HashSet<string> available)
        {
            if (available.Count >= TestWorld.Data.AllMembers.Count)
                return TestWorld.Search(missionId).Winners.Count > 0;
            return TestWorld.Search(missionId, available).Winners.Count > 0;
        }
    }
}
