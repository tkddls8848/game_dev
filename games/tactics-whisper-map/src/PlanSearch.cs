using System;
using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
{
    public sealed class SearchResult
    {
        public string MissionId;
        public int Runs;
        public readonly List<SquadPlan> Winners = new List<SquadPlan>();
        public readonly SortedSet<string> ActionSignatures = new SortedSet<string>(StringComparer.Ordinal);
        public readonly SortedSet<string> PlacementSignatures = new SortedSet<string>(StringComparer.Ordinal);

        /// <summary>가장 적은 틱을 지도 위에서 보낸 승리안. 정보의 값을 이 수치로 잰다.</summary>
        public int BestFieldTicks = int.MaxValue;
    }

    /// <summary>
    /// 계획 탐색(빔 탐색). **완전하지 않다** — 검사기는 이렇게 읽어야 한다:
    ///
    ///   · "이기는 계획을 찾았다"   → 참. 실제로 돌려서 이겼다
    ///   · "이기는 계획이 없었다"   → **이 탐색 범위 안에서** 없었다. 그 이상은 말하지 않는다
    ///
    /// 후보 시각은 **믿고 있는** 순찰 구간 경계에서 뽑고 격자를 얹는다.
    /// 믿는 것이 틀렸으면 틀린 계획이 나온다 — 그게 이 PoC에서 실패 표본이 생기는 방식이고,
    /// DetectionFairness 가 감사할 재료다.
    /// </summary>
    public static class PlanSearch
    {
        /// <summary>후보 명령 하나하나. 지도·임무·분대에서만 뽑는다(손으로 적은 자리가 없다).</summary>
        public static List<SquadOrder> Candidates(GameData d, MissionSim sim, IntelKnowledge knowledge, int gridMs,
                                                  ICollection<string> allowedMembers = null)
        {
            MissionDef mission = sim.Mission;
            ZoneGraph g = sim.Graph;
            List<int> times = CandidateTimes(d, sim, knowledge, gridMs);
            HashSet<string> occupied = sim.Patrols.OccupiedZones();
            List<SquadOrder> list = new List<SquadOrder>();

            foreach (MemberDef m in d.AllMembers)
            {
                if (m.actions == null) continue;
                if (allowedMembers != null && !allowedMembers.Contains(m.id)) continue;
                foreach (MemberActionDef a in m.actions)
                {
                    List<string> zones = new List<string>();
                    List<string> targets = new List<string>();

                    if (a.kind == ActionKinds.Shoot)
                    {
                        foreach (string z in g.ZoneIds)
                        {
                            bool useful = false;
                            foreach (string gz in occupied)
                                if (g.Sees(z, gz) && g.Hops(z, gz) >= 0 && g.Hops(z, gz) <= a.rangeHops)
                                { useful = true; break; }
                            if (useful) zones.Add(z);
                        }
                        foreach (string guardId in mission.guardIds) targets.Add(guardId);
                    }
                    else if (a.kind == ActionKinds.PlantTrap)
                    {
                        foreach (string z in g.ZoneIds) if (occupied.Contains(z)) zones.Add(z);
                        targets.Add("");
                    }
                    else if (a.kind == ActionKinds.PlantCharge)
                    {
                        if (!d.HasTarget(mission.destroyTargetId)) continue;
                        zones.Add(g.Canon(d.Target(mission.destroyTargetId).zone));
                        targets.Add(mission.destroyTargetId);
                    }
                    else if (a.kind == ActionKinds.Detonate)
                    {
                        if (!d.HasTarget(mission.destroyTargetId)) continue;
                        string chargeZone = g.Canon(d.Target(mission.destroyTargetId).zone);
                        foreach (string z in g.ZoneIds)
                        {
                            int hops = g.Hops(z, chargeZone);
                            if (hops >= 0 && hops <= a.rangeHops) zones.Add(z);
                        }
                        targets.Add(mission.destroyTargetId);
                    }
                    else if (a.kind == ActionKinds.Infiltrate)
                    {
                        if (!d.HasDocument(mission.stealDocumentId)) continue;
                        zones.Add(g.Canon(d.Document(mission.stealDocumentId).zone));
                        targets.Add(mission.stealDocumentId);
                    }

                    foreach (string z in zones)
                        foreach (string targetId in targets)
                            foreach (int t in times)
                            {
                                list.Add(new SquadOrder(m.id, a.kind, z, targetId, t, false));
                                // 은폐 지점이 있는 자리에서는 "몸을 묻는" 변형도 후보다.
                                if (g.BestConcealmentSettleMs(z) > 0)
                                    list.Add(new SquadOrder(m.id, a.kind, z, targetId, t, true));
                            }
                }
            }
            return list;
        }

        /// <summary>
        /// 후보 시각. **믿고 있는** 순찰 구간 경계 + 격자.
        /// 아무것도 묻지 않았으면 경계가 없고 격자만 남는다 — 그래서 눈먼 계획은 넉넉하게 짜인다.
        /// </summary>
        public static List<int> CandidateTimes(GameData d, MissionSim sim, IntelKnowledge knowledge, int gridMs)
        {
            SortedSet<int> set = new SortedSet<int>();
            int length = sim.Mission.lengthMs;
            for (int t = 0; t <= length; t += gridMs) set.Add(t);

            PatrolModel believed = knowledge == null ? null : knowledge.BelievedModel();
            if (believed != null)
                foreach (string guardId in knowledge.PredictableGuardIds())
                    foreach (PatrolWindow w in believed.Windows(guardId))
                    {
                        if (w.FromMs >= 0 && w.FromMs <= length) set.Add(w.FromMs);
                        if (w.ToMs >= 0 && w.ToMs <= length) set.Add(w.ToMs);
                    }

            List<int> times = new List<int>(set);
            return times;
        }

        /// <summary>
        /// 회차 하나를 점수로 환산한다. 정수만 쓴다.
        ///
        /// **임무가 무엇을 요구하는지만 센다.** 이것을 틀리면 빔이 엉뚱한 진척을 쫓는다 —
        /// 실제로 한 번 틀렸다: 순찰 제거를 무조건 점수로 주자 폭파 임무의 빔이 **폭약을 놓는 계획을 전부 버리고**
        /// 사살 계획만 남겨서 해를 하나도 못 찾았다. 임무마다 진척의 정의가 다르다.
        /// </summary>
        public static int Score(MissionResult r, MissionDef mission)
        {
            if (r.Infeasible) return int.MinValue;
            if (r.Won) return 1000000 - r.FieldTicks;
            int s = 0;
            if (mission.downAllGuards) s += r.GuardsDowned.Count * 1200;
            if (!string.IsNullOrEmpty(mission.destroyTargetId))
            {
                if (r.ChargePlanted) s += 1200;
                if (r.TargetDestroyed) s += 3600;
            }
            if (!string.IsNullOrEmpty(mission.stealDocumentId) && r.DocumentStolen) s += 3600;
            s -= r.AlarmPercent * 50;
            s -= r.Sightings.Count * 20;
            s -= r.MembersLost.Count * 2000;
            if (mission.noGuardsDowned) s -= r.GuardsDowned.Count * 4000;
            return s;
        }

        /// <summary>
        /// 빔 탐색. 깊이마다 후보 하나를 덧붙이고 상위 beamWidth 개만 남긴다.
        /// 동률은 계획 문자열로 깬다 — 같은 데이터면 늘 같은 답이 나와야 한다.
        /// </summary>
        public static SearchResult Beam(GameData d, MissionSim sim, IntelKnowledge knowledge,
                                        int gridMs = -1, int beamWidth = -1, int maxActions = -1,
                                        ICollection<string> allowedMembers = null)
        {
            SearchBalance sb = d.Balance.search;
            if (gridMs < 0) gridMs = sb.gridMs;
            if (beamWidth < 0) beamWidth = sb.beamWidth;
            if (maxActions < 0) maxActions = sb.maxActions;

            SearchResult result = new SearchResult { MissionId = sim.Mission.id };
            List<SquadOrder> candidates = Candidates(d, sim, knowledge, gridMs, allowedMembers);
            List<SquadPlan> beam = new List<SquadPlan> { new SquadPlan() };
            HashSet<string> seen = new HashSet<string>();

            for (int depth = 0; depth < maxActions; depth++)
            {
                List<SquadPlan> grown = new List<SquadPlan>();
                List<int> scores = new List<int>();

                foreach (SquadPlan basePlan in beam)
                    foreach (SquadOrder cand in candidates)
                    {
                        SquadPlan plan = basePlan.Copy().Add(cand.Copy());
                        string key = plan.ToString();
                        if (!seen.Add(key)) continue;

                        MissionResult r = sim.Run(plan);
                        result.Runs++;
                        int score = Score(r, sim.Mission);
                        if (score == int.MinValue) continue;

                        if (r.Won)
                        {
                            result.Winners.Add(plan);
                            result.ActionSignatures.Add(plan.ActionSignature());
                            result.PlacementSignatures.Add(plan.PlacementSignature());
                            if (r.FieldTicks < result.BestFieldTicks) result.BestFieldTicks = r.FieldTicks;
                            continue;   // 이긴 계획은 더 늘리지 않는다
                        }
                        grown.Add(plan);
                        scores.Add(score);
                    }

                beam = TopN(grown, scores, beamWidth);
                if (beam.Count == 0) break;
            }

            return result;
        }

        /// <summary>
        /// 상위 n 개. 다만 **한 절차가 빔을 다 먹지 못하게 한다.**
        ///
        /// 실제로 한 번 물렸다: 초소 제거 임무에서 함정 계획과 저격 계획이 **같은 점수**(순찰 1인 제거)로
        /// 묶이자 문자열 순서로 동률이 깨지면서 `m_sapper:...` 가 빔 16칸을 전부 차지했고,
        /// **저격 두 번으로 푸는 해(참조 계획)를 탐색이 못 찾았다.** 점수가 같은 해를 다양성으로 남긴다.
        /// </summary>
        private static List<SquadPlan> TopN(List<SquadPlan> plans, List<int> scores, int n)
        {
            List<int> order = new List<int>();
            for (int i = 0; i < plans.Count; i++) order.Add(i);
            order.Sort(delegate (int a, int b)
            {
                int c = scores[b].CompareTo(scores[a]);
                if (c != 0) return c;
                return string.CompareOrdinal(plans[a].ToString(), plans[b].ToString());
            });

            int quota = n / 3 < 1 ? 1 : n / 3;
            Dictionary<string, int> used = new Dictionary<string, int>();
            List<SquadPlan> top = new List<SquadPlan>();
            List<SquadPlan> overflow = new List<SquadPlan>();
            foreach (int i in order)
            {
                string sig = plans[i].ActionSignature();
                int taken;
                used.TryGetValue(sig, out taken);
                if (taken >= quota) { overflow.Add(plans[i]); continue; }
                used[sig] = taken + 1;
                top.Add(plans[i]);
                if (top.Count >= n) return top;
            }
            foreach (SquadPlan p in overflow)
            {
                if (top.Count >= n) break;
                top.Add(p);
            }
            return top;
        }

        /// <summary>
        /// ★ **눈먼 계획 — 아홉 위상 전부에서 이기는 계획.**
        ///
        /// 아무것도 묻지 않은 플레이어는 오늘 밤이 아홉 세계 중 어느 것인지 모른다.
        /// 그러므로 "정보를 하나도 사지 않고 깬다"는 **아홉 전부에서 이긴다**는 뜻이고,
        /// 이 메서드가 그것을 그대로 돌린다. BlindSolvability 의 정의가 여기 있다.
        /// </summary>
        public static List<SquadPlan> BlindRobustWinners(GameData d, MissionDef mission,
                                                         int gridMs = 30000, int beamWidth = 10,
                                                         int maxActions = 2, int maxKeep = 8)
        {
            List<PhaseAssignment> all = PhaseAssignment.AllCombinations(d, mission);
            IntelKnowledge blind = IntelKnowledge.Blind(d, mission);

            // ① 위상마다 이기는 계획을 모은다 (눈먼 후보 집합에서).
            Dictionary<string, SquadPlan> pool = new Dictionary<string, SquadPlan>();
            List<string> poolOrder = new List<string>();
            foreach (PhaseAssignment p in all)
            {
                MissionSim sim = new MissionSim(d, mission, p);
                SearchResult sr = Beam(d, sim, blind, gridMs, beamWidth, maxActions);
                foreach (SquadPlan w in sr.Winners)
                {
                    string key = w.ToString();
                    if (pool.ContainsKey(key)) continue;
                    pool[key] = w;
                    poolOrder.Add(key);
                }
                if (pool.Count > 400) break;
            }

            // ② 아홉 전부에서 이기는 것만 남긴다.
            List<SquadPlan> robust = new List<SquadPlan>();
            poolOrder.Sort(StringComparer.Ordinal);
            foreach (string key in poolOrder)
            {
                if (IsRobust(d, mission, pool[key], all)) robust.Add(pool[key]);
                if (robust.Count >= maxKeep) break;
            }
            return robust;
        }

        /// <summary>이 계획이 위상 전부에서 이기는가.</summary>
        public static bool IsRobust(GameData d, MissionDef mission, SquadPlan plan,
                                    List<PhaseAssignment> phases = null)
        {
            if (phases == null) phases = PhaseAssignment.AllCombinations(d, mission);
            foreach (PhaseAssignment p in phases)
            {
                MissionResult r = new MissionSim(d, mission, p).Run(plan);
                if (!r.Won) return false;
            }
            return true;
        }

        /// <summary>위상 전부에서 돌렸을 때 가장 나쁜(가장 늦은) 목표 달성 시각. 눈먼 계획의 값이다.</summary>
        public static int WorstFieldTicks(GameData d, MissionDef mission, SquadPlan plan)
        {
            int worst = 0;
            foreach (PhaseAssignment p in PhaseAssignment.AllCombinations(d, mission))
            {
                MissionResult r = new MissionSim(d, mission, p).Run(plan);
                if (r.FieldTicks > worst) worst = r.FieldTicks;
            }
            return worst;
        }

        /// <summary>
        /// 무작위 계획. DetectionFairness 의 **실패 표본**을 만드는 자리다 —
        /// 이기는 계획만 보면 감사할 실패가 없다.
        /// </summary>
        public static List<SquadPlan> RandomPlans(GameData d, MissionSim sim, IntelKnowledge knowledge,
                                                  int seed, int count, int gridMs = -1)
        {
            if (gridMs < 0) gridMs = d.Balance.search.gridMs;
            List<SquadOrder> candidates = Candidates(d, sim, knowledge, gridMs);
            Random rng = new Random(seed);
            HashSet<string> seen = new HashSet<string>();
            List<SquadPlan> plans = new List<SquadPlan>();
            int guard = 0;

            while (plans.Count < count && guard++ < count * 40)
            {
                int n = 1 + rng.Next(d.Balance.search.maxActions);
                SquadPlan plan = new SquadPlan();
                for (int i = 0; i < n; i++) plan.Add(candidates[rng.Next(candidates.Count)].Copy());
                string key = plan.ToString();
                if (!seen.Add(key)) continue;
                plans.Add(plan);
            }
            return plans;
        }
    }
}
