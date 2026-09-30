using System;
using System.Collections.Generic;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>계획 후보 하나 — "누가 · 무엇을 · 어디서", 시각은 아직 없다.</summary>
    public sealed class ActionCandidate
    {
        public string MemberId;
        public string Kind;
        public string Zone;
        public string TargetId;
        public int DurationMs;
        public readonly List<int> StartTimes = new List<int>();

        public override string ToString() { return MemberId + ":" + Kind + "@" + Zone; }
    }

    public sealed class SearchOptions
    {
        /// <summary>쓸 수 있는 분대원. null 이면 전원.</summary>
        public HashSet<string> AvailableMembers;
        public int BeamWidth = -1;      // -1 = balance.json
        public int MaxActions = -1;
        public int MaxWinners = 16;
    }

    public sealed class SearchResult
    {
        public string MissionId;
        public int PlansSimulated;
        public readonly List<SquadPlan> Winners = new List<SquadPlan>();
        public readonly List<MissionResult> WinnerResults = new List<MissionResult>();

        public SortedSet<string> ActionSignatures()
        {
            SortedSet<string> set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (SquadPlan p in Winners) set.Add(p.ActionSignature());
            return set;
        }

        public SortedSet<string> PlacementSignatures()
        {
            SortedSet<string> set = new SortedSet<string>(StringComparer.Ordinal);
            foreach (SquadPlan p in Winners) set.Add(p.PlacementSignature());
            return set;
        }

        /// <summary>이기는 계획에 **반드시** 들어가는 분대원 — 없으면 이 임무를 못 푼다.</summary>
        public SortedSet<string> IndispensableMembers()
        {
            SortedSet<string> all = null;
            foreach (SquadPlan p in Winners)
            {
                SortedSet<string> used = p.MembersUsed();
                if (all == null) { all = new SortedSet<string>(used, StringComparer.Ordinal); continue; }
                all.IntersectWith(used);
            }
            return all ?? new SortedSet<string>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// 계획 탐색. 검사기가 "이길 수 있는가 · 지배 전략이 없는가"를 묻기 위한 도구다.
    ///
    /// 후보 시각을 **격자로 훑지 않고 순찰 구간 경계에서 뽑는다**. 전술 계획에서 의미 있는
    /// 시각은 "순찰병이 저기서 떠나는 순간"·"저기 닿는 순간"뿐이고, 그 사이 아무 때나는
    /// 같은 결과를 낸다. 격자만 쓰면 같은 답을 찾느라 수십 배를 돈다.
    ///
    /// 탐색은 완전하지 않다(빔 탐색이다). 그래서 검사기는 이렇게 읽는다:
    ///   · "이기는 계획을 찾았다"  → 참. 실제로 이겼다
    ///   · "이기는 계획이 없었다"  → **이 탐색 범위 안에서** 없었다. 그 이상은 말하지 않는다
    /// </summary>
    public static class PlanSearch
    {
        private sealed class State
        {
            public SquadPlan Plan;
            public MissionResult Result;
            public int Score;
            public string Key;
        }

        public static SearchResult Solve(GameData data, MissionSim sim, SearchOptions options = null)
        {
            options = options ?? new SearchOptions();
            SearchBalance sb = data.Balance.search;
            int beamWidth = options.BeamWidth > 0 ? options.BeamWidth : sb.beamWidth;
            int maxActions = options.MaxActions > 0 ? options.MaxActions : sb.maxActions;

            SearchResult sr = new SearchResult { MissionId = sim.Mission.id };
            List<ActionCandidate> candidates = Candidates(data, sim, options.AvailableMembers);

            State root = new State { Plan = new SquadPlan(), Result = sim.Run(new SquadPlan()), Score = 0, Key = "" };
            sr.PlansSimulated++;
            root.Score = Score(sim, root.Plan, root.Result);
            if (root.Result.Won) Collect(sr, root, options);

            List<State> beam = new List<State> { root };
            HashSet<string> visited = new HashSet<string>();

            for (int depth = 0; depth < maxActions; depth++)
            {
                List<State> next = new List<State>();
                foreach (State s in beam)
                {
                    if (sr.Winners.Count >= options.MaxWinners) break;
                    foreach (ActionCandidate c in candidates)
                    {
                        if (sr.Winners.Count >= options.MaxWinners) break;
                        foreach (int startMs in c.StartTimes)
                        {
                            SquadPlan plan = s.Plan.Copy();
                            plan.Add(new SquadOrder(c.MemberId, c.Kind, c.Zone, c.TargetId, startMs));

                            string key = plan.ToString();
                            if (visited.Contains(key)) continue;
                            visited.Add(key);

                            MissionResult res = sim.Run(plan);
                            sr.PlansSimulated++;
                            if (res.FailureReason == "Infeasible") continue;

                            // 경보 0이 승리 조건인 임무에서 이미 경보가 붙었으면 더 볼 값이 없다.
                            if (res.Alarm > sim.Mission.maxAlarm) continue;

                            State ns = new State { Plan = plan, Result = res, Key = key, Score = Score(sim, plan, res) };
                            if (res.Won) { Collect(sr, ns, options); continue; }
                            next.Add(ns);
                        }
                    }
                }

                if (sr.Winners.Count >= options.MaxWinners) break;
                if (next.Count == 0) break;

                next.Sort(delegate (State a, State b)
                {
                    int c = b.Score.CompareTo(a.Score);
                    return c != 0 ? c : string.CompareOrdinal(a.Key, b.Key);
                });
                if (next.Count > beamWidth) next.RemoveRange(beamWidth, next.Count - beamWidth);
                beam = next;
            }

            return sr;
        }

        private static void Collect(SearchResult sr, State s, SearchOptions options)
        {
            if (sr.Winners.Count >= options.MaxWinners) return;
            sr.Winners.Add(s.Plan);
            sr.WinnerResults.Add(s.Result);
        }

        /// <summary>
        /// 빔이 무엇을 붙들지 정하는 점수. 승리 조건만 보면 안 된다 —
        /// "초병을 지웠다"·"폭약을 놓았다"는 그 자체로 승리가 아니지만 승리로 가는 계단이고,
        /// 이 계단을 점수에 넣지 않으면 빔이 아무것도 한 게 없는 계획들로 채워진다.
        /// </summary>
        private static int Score(MissionSim sim, SquadPlan plan, MissionResult r)
        {
            MissionDef m = sim.Mission;
            int done = 0;
            foreach (string g in m.eliminateGuardIds) if (r.GuardsDown.Contains(g)) done++;
            foreach (string t in m.destroyTargetIds) if (r.TargetsDestroyed.Contains(t)) done++;
            foreach (string i in m.seizeIntelIds) if (r.IntelSeized.Contains(i)) done++;

            int score = done * 10000;
            score += r.GuardsDown.Count * 800;        // 순찰병이 줄면 다음 수가 열린다
            score += r.ChargesArmed.Count * 1500;
            score += r.TrapsArmed * 120;
            score -= r.Alarm * 200;
            score -= r.MembersDown.Count * 5000;
            score -= r.WastedActions.Count * 50;
            score -= plan.Orders.Count * 10;

            // 같은 성과면 일찍 끝나는 계획을 앞에 둔다 — 늦게 끝나면 탈출 기한이 막는다.
            int latest = 0;
            foreach (SquadOrder o in plan.Orders) if (o.StartMs > latest) latest = o.StartMs;
            score -= latest / 5000;
            return score;
        }

        /// <summary>
        /// 이 임무에서 의미가 있는 (분대원 · 수단 · 자리) 조합과 그 후보 시각들.
        /// </summary>
        public static List<ActionCandidate> Candidates(GameData data, MissionSim sim,
                                                      HashSet<string> availableMembers)
        {
            MissionDef mission = sim.Mission;
            ZoneGraph graph = sim.Graph;
            PatrolModel patrols = sim.Patrols;
            int tickMs = data.Balance.tickMs;

            // 순찰 구간 경계 — 계획에서 의미 있는 시각은 전부 여기서 나온다.
            SortedSet<int> boundaries = new SortedSet<int> { 0 };
            Dictionary<string, List<PatrolWindow>> windows = new Dictionary<string, List<PatrolWindow>>();
            foreach (string g in patrols.GuardIds)
            {
                List<PatrolWindow> w = patrols.Windows(g);
                windows[g] = w;
                foreach (PatrolWindow pw in w) { boundaries.Add(pw.FromMs); boundaries.Add(pw.ToMs); }
            }
            // 거친 격자를 얹어 경계 사이도 조금은 본다.
            for (int t = 0; t < mission.lengthMs; t += data.Balance.search.gridMs) boundaries.Add(t);

            List<ActionCandidate> list = new List<ActionCandidate>();

            foreach (MemberDef m in data.AllMembers)
            {
                if (availableMembers != null && !availableMembers.Contains(m.id)) continue;
                if (m.actions == null) continue;

                foreach (MemberActionDef a in m.actions)
                {
                    if (a.kind == ActionKinds.Shoot)
                    {
                        foreach (string guardId in mission.guardIds)
                        {
                            foreach (string firing in graph.ZoneIds)
                            {
                                List<int> resolveTimes = new List<int>();
                                foreach (PatrolWindow pw in windows[guardId])
                                {
                                    int hops = graph.Hops(firing, pw.Zone);
                                    if (hops < 0 || hops > a.rangeZones) continue;
                                    if (!graph.Sees(firing, pw.Zone)) continue;
                                    resolveTimes.Add(pw.FromMs);
                                    resolveTimes.Add(pw.FromMs + tickMs);
                                    resolveTimes.Add(pw.ToMs - tickMs);
                                }
                                if (resolveTimes.Count == 0) continue;

                                ActionCandidate c = NewCandidate(m.id, a, firing, guardId);
                                foreach (int res in resolveTimes) AddTime(c, res - a.durationMs, tickMs, mission);
                                if (c.StartTimes.Count > 0) list.Add(c);
                            }
                        }
                        continue;
                    }

                    if (a.kind == ActionKinds.PlantTrap)
                    {
                        // 함정은 순찰병이 **밟을** 구역에만 값이 있다.
                        foreach (string zone in patrols.OccupiedZones())
                        {
                            ActionCandidate c = NewCandidate(m.id, a, zone, null);
                            AddTime(c, 0, tickMs, mission);
                            foreach (string g in patrols.GuardIds)
                                foreach (PatrolWindow pw in windows[g])
                                {
                                    if (pw.Zone != zone) continue;
                                    // 그 순찰병이 닿기 전에 무장돼 있어야 한다
                                    AddTime(c, pw.FromMs - a.durationMs, tickMs, mission);
                                    AddTime(c, pw.FromMs - a.durationMs - 10000, tickMs, mission);
                                    AddTime(c, pw.FromMs - a.durationMs - 30000, tickMs, mission);
                                }
                            foreach (int b in boundaries) AddTime(c, b, tickMs, mission);
                            if (c.StartTimes.Count > 0) list.Add(c);
                        }
                        continue;
                    }

                    if (a.kind == ActionKinds.PlantCharge)
                    {
                        foreach (TargetDef t in data.TargetsOnMap(mission.mapId))
                        {
                            ActionCandidate c = NewCandidate(m.id, a, t.zone, t.id);
                            foreach (int b in boundaries) AddTime(c, b, tickMs, mission);
                            if (c.StartTimes.Count > 0) list.Add(c);
                        }
                        continue;
                    }

                    if (a.kind == ActionKinds.Detonate)
                    {
                        List<TargetDef> targets = data.TargetsOnMap(mission.mapId);
                        if (targets.Count == 0) continue;
                        foreach (string zone in graph.ZoneIds)
                        {
                            bool inRange = false;
                            foreach (TargetDef t in targets)
                            {
                                int hops = graph.Hops(zone, t.zone);
                                if (hops >= 0 && hops <= a.rangeZones) { inRange = true; break; }
                            }
                            if (!inRange) continue;
                            ActionCandidate c = NewCandidate(m.id, a, zone, targets[0].id);
                            foreach (int b in boundaries) AddTime(c, b, tickMs, mission);
                            if (c.StartTimes.Count > 0) list.Add(c);
                        }
                        continue;
                    }

                    if (a.kind == ActionKinds.Infiltrate)
                    {
                        foreach (IntelDef i in data.IntelOnMap(mission.mapId))
                        {
                            ActionCandidate c = NewCandidate(m.id, a, i.zone, i.id);
                            foreach (int b in boundaries) AddTime(c, b, tickMs, mission);
                            if (c.StartTimes.Count > 0) list.Add(c);
                        }
                        continue;
                    }
                }
            }

            // 후보 순서를 고정한다 — 탐색이 재현돼야 한다.
            list.Sort(delegate (ActionCandidate a, ActionCandidate b)
            {
                return string.CompareOrdinal(a.ToString() + "|" + a.TargetId, b.ToString() + "|" + b.TargetId);
            });
            return list;
        }

        private static ActionCandidate NewCandidate(string memberId, MemberActionDef a, string zone, string targetId)
        {
            return new ActionCandidate
            {
                MemberId = memberId, Kind = a.kind, Zone = zone, TargetId = targetId, DurationMs = a.durationMs
            };
        }

        private static void AddTime(ActionCandidate c, int startMs, int tickMs, MissionDef mission)
        {
            if (startMs < 0) return;
            int aligned = (startMs / tickMs) * tickMs;
            if (aligned + c.DurationMs > mission.lengthMs) return;
            if (!c.StartTimes.Contains(aligned)) c.StartTimes.Add(aligned);
        }

        /// <summary>
        /// 씨드로 뽑은 무작위 계획들. 대부분 실패한다 — **그 실패가 DetectionFairness 의 표본이다.**
        /// 이기는 계획만 보면 공정성을 물을 수 없다. 들켜야 감사할 사건이 생긴다.
        /// </summary>
        public static List<SquadPlan> RandomPlans(GameData data, MissionSim sim, int seed, int count)
        {
            List<ActionCandidate> candidates = Candidates(data, sim, null);
            List<SquadPlan> plans = new List<SquadPlan>();
            if (candidates.Count == 0) return plans;

            Random rng = new Random(seed);
            int guard = 0;
            while (plans.Count < count && guard++ < count * 20)
            {
                int n = 1 + rng.Next(data.Balance.search.maxActions);
                SquadPlan p = new SquadPlan();
                for (int i = 0; i < n; i++)
                {
                    ActionCandidate c = candidates[rng.Next(candidates.Count)];
                    if (c.StartTimes.Count == 0) continue;
                    int start = c.StartTimes[rng.Next(c.StartTimes.Count)];
                    p.Add(new SquadOrder(c.MemberId, c.Kind, c.Zone, c.TargetId, start));
                }
                if (p.Orders.Count == 0) continue;
                plans.Add(p);
            }
            return plans;
        }
    }
}
