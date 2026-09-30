using System;
using System.Collections.Generic;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>거점 정산 한 회차.</summary>
    public sealed class CampRound
    {
        public int Index;
        public string MissionId = "";        // 빈 문자열 = 쉬었다
        public bool Attempted;
        public bool Succeeded;
        public string RestReason = "";
        public string LostMemberId = "";
        public string BoughtRepairId = "";
        public readonly Dictionary<string, int> SuppliesAfter = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Clamped = new Dictionary<string, int>();
        public readonly List<string> Unavailable = new List<string>();
    }

    public sealed class CampTrace
    {
        public readonly List<CampRound> Rounds = new List<CampRound>();
        public readonly Dictionary<string, int> Start = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Final = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Min = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Max = new Dictionary<string, int>();
        public readonly Dictionary<string, int> CeilingClampRounds = new Dictionary<string, int>();
        public readonly Dictionary<string, int> FloorClampRounds = new Dictionary<string, int>();
        public int MissionsAttempted;
        public int MissionsSucceeded;
        public int RestRounds;
        public int MembersLost;
        public int MembersRecovered;
        public int BlockedRounds;            // 아무 임무도 시도할 수 없었던 회차
        public string LedgerHash = "";
    }

    /// <summary>
    /// 거점 층. 임무 사이에 빼앗은 보급으로 무엇을 고칠지 고르는 자리다.
    ///
    /// 여기가 이 PoC에서 **난수를 실제로 쓰는 유일한 곳**이다(camp.json 의 seed).
    /// 회차 시뮬레이션은 난수가 없다 — 전술이 운으로 결판나면 정찰이 의미를 잃기 때문이다.
    /// 거점은 반대다. "이번에 누가 다쳤는가"는 운이어야 회차마다 다른 선택이 나온다.
    ///
    /// 두 검사기가 이 클래스를 본다:
    ///   · SupplyEconomy      — N회에서 보급이 발산하거나 고갈하지 않는가
    ///   · AttritionRecoverable — 분대원을 잃어도 되돌릴 수 있고, 잃는 것이 공짜가 아닌가
    /// </summary>
    public sealed class CampLedger
    {
        private readonly GameData _data;

        /// <summary>임무 하나를 치르는 데 드는 보급. (보급 id -> 양)</summary>
        public delegate Dictionary<string, int> MissionCostFn(string missionId);

        /// <summary>이 분대원들만으로 이 임무를 풀 수 있는가.</summary>
        public delegate bool SolvableFn(string missionId, HashSet<string> availableMembers);

        public CampLedger(GameData data) { _data = data; }

        public CampTrace Simulate(int rounds, MissionCostFn costOf, SolvableFn solvable)
        {
            CampFile camp = _data.Camp;
            CampTrace trace = new CampTrace();
            Random rng = new Random(camp.seed);

            Dictionary<string, int> supplies = new Dictionary<string, int>();
            foreach (SupplyDef s in camp.supplies)
            {
                supplies[s.id] = s.startAmount;
                trace.Start[s.id] = s.startAmount;
                trace.Min[s.id] = s.startAmount;
                trace.Max[s.id] = s.startAmount;
                trace.CeilingClampRounds[s.id] = 0;
                trace.FloorClampRounds[s.id] = 0;
            }

            // 분대원 복귀 회차. 0 이면 지금 나갈 수 있다.
            Dictionary<string, int> downUntil = new Dictionary<string, int>();
            foreach (MemberDef m in _data.AllMembers) downUntil[m.id] = 0;

            HashSet<string> repairsOwned = new HashSet<string>();
            System.Text.StringBuilder log = new System.Text.StringBuilder();

            for (int i = 0; i < rounds; i++)
            {
                CampRound round = new CampRound { Index = i };

                HashSet<string> available = new HashSet<string>();
                foreach (MemberDef m in _data.AllMembers)
                {
                    if (downUntil[m.id] <= i) available.Add(m.id);
                    else round.Unavailable.Add(m.id);
                }

                // 고친 것이 매 회차 보급을 보태 준다
                foreach (RepairDef rp in camp.repairs)
                    if (repairsOwned.Contains(rp.id)) supplies[rp.bonusSupplyId] += rp.bonusPerMission;

                // 임무 선택: 순서대로 돌면서 지금 인원으로 풀리고 값을 치를 수 있는 첫 임무
                string chosen = null;
                Dictionary<string, int> chosenCost = null;
                int n = _data.AllMissions.Count;
                for (int k = 0; k < n; k++)
                {
                    MissionDef candidate = _data.AllMissions[(i + k) % n];
                    if (!solvable(candidate.id, available)) continue;
                    Dictionary<string, int> cost = costOf(candidate.id);
                    if (!CanAfford(supplies, cost)) continue;
                    chosen = candidate.id;
                    chosenCost = cost;
                    break;
                }

                if (chosen == null)
                {
                    round.RestReason = available.Count < _data.AllMembers.Count
                        ? "분대원이 회복 중이다"
                        : "값을 치를 보급이 없다";
                    trace.RestRounds++;
                    trace.BlockedRounds++;
                    // 쉬는 회차에도 마을은 조금씩 내준다 — 진행이 완전히 막히지 않게 하는 밸브다
                    if (camp.restRewards != null)
                        foreach (RestRewardDef rr in camp.restRewards) supplies[rr.supplyId] += rr.amount;
                }
                else
                {
                    round.MissionId = chosen;
                    round.Attempted = true;
                    trace.MissionsAttempted++;
                    foreach (KeyValuePair<string, int> kv in chosenCost) supplies[kv.Key] -= kv.Value;

                    round.Succeeded = true;   // 이기는 계획만 쓴다. 검사기가 보는 것은 경제다
                    trace.MissionsSucceeded++;
                    foreach (MissionRewardDef mr in camp.missionRewards)
                        if (mr.missionId == chosen) supplies[mr.supplyId] += mr.onSuccess;

                    // 손실 — 여기만 운이다
                    if (available.Count > 1 && rng.Next(100) < camp.recovery.lossChancePercent)
                    {
                        List<string> pool = new List<string>(available);
                        pool.Sort(StringComparer.Ordinal);
                        string lost = pool[rng.Next(pool.Count)];
                        downUntil[lost] = i + 1 + camp.recovery.woundedRecoverMissions;
                        round.LostMemberId = lost;
                        trace.MembersLost++;
                        supplies[camp.recovery.medicineSupplyId] -= camp.recovery.medicineCost;
                        supplies[camp.recovery.trustSupplyId] -= camp.recovery.trustPenaltyOnLoss;
                    }
                }

                // 복귀한 사람 세기
                foreach (MemberDef m in _data.AllMembers)
                    if (downUntil[m.id] == i + 1) trace.MembersRecovered++;

                // 고칠 것을 산다 — 남는 보급을 흘려보내는 자리다(발산을 막는 밸브)
                foreach (RepairDef rp in camp.repairs)
                {
                    if (repairsOwned.Contains(rp.id)) continue;
                    SupplyDef sd = _data.Supply(rp.supplyId);
                    if (supplies[rp.supplyId] - rp.cost < sd.floor) continue;
                    if (rng.Next(100) >= 50) continue;      // 거점에서 매번 사지는 않는다
                    supplies[rp.supplyId] -= rp.cost;
                    repairsOwned.Add(rp.id);
                    round.BoughtRepairId = rp.id;
                    break;
                }

                // 거점 저장 한계 · 바닥
                foreach (SupplyDef s in camp.supplies)
                {
                    if (supplies[s.id] > s.ceiling)
                    {
                        supplies[s.id] = s.ceiling;
                        trace.CeilingClampRounds[s.id]++;
                        round.Clamped[s.id] = 1;
                    }
                    if (supplies[s.id] < s.floor)
                    {
                        // 손실 정산이 바닥을 뚫을 수 있다. 뚫은 회차를 세어 SupplyEconomy 가 본다.
                        supplies[s.id] = s.floor;
                        trace.FloorClampRounds[s.id]++;
                        round.Clamped[s.id] = -1;
                    }
                    if (supplies[s.id] < trace.Min[s.id]) trace.Min[s.id] = supplies[s.id];
                    if (supplies[s.id] > trace.Max[s.id]) trace.Max[s.id] = supplies[s.id];
                    round.SuppliesAfter[s.id] = supplies[s.id];
                    log.Append(s.id).Append('=').Append(supplies[s.id]).Append(',');
                }
                log.Append(round.MissionId).Append('/').Append(round.LostMemberId).Append(';');

                trace.Rounds.Add(round);
            }

            foreach (SupplyDef s in camp.supplies) trace.Final[s.id] = supplies[s.id];
            trace.LedgerHash = MissionSim.Fnv(log.ToString());
            return trace;
        }

        private static bool CanAfford(Dictionary<string, int> supplies, Dictionary<string, int> cost)
        {
            foreach (KeyValuePair<string, int> kv in cost)
                if (supplies[kv.Key] - kv.Value < 0) return false;
            return true;
        }

        /// <summary>계획 하나가 먹는 보급. camp.json 의 actionCosts 를 그대로 더한다.</summary>
        public Dictionary<string, int> CostOfPlan(SquadPlan plan)
        {
            Dictionary<string, int> cost = new Dictionary<string, int>();
            foreach (SupplyDef s in _data.Camp.supplies) cost[s.id] = 0;
            foreach (SquadOrder o in plan.Orders)
                foreach (ActionCostDef ac in _data.Camp.actionCosts)
                    if (ac.actionKind == o.Kind) cost[ac.supplyId] += ac.amount;
            return cost;
        }
    }
}
