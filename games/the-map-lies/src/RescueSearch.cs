using System;
using System.Collections.Generic;
using MapLies.Data;

namespace MapLies.Sim
{
    /// <summary>잘못 그은 선 하나의 뒷일. 갇힌 사람이 몇이고, 꺼내는 데 얼마가 드는가.</summary>
    public sealed class RescueResult
    {
        public string MistakeId = "";
        public int Seed;

        /// <summary>잘못 그은 선이 실제로 사람을 갇히게 만들었는가. 아니면 이 시나리오는 아무것도 시험하지 않는다.</summary>
        public bool TrappedSomeone;
        public readonly List<string> TrappedAfterMistake = new List<string>();
        /// <summary>사고 전에 이미 갇힌 사람. 하나도 없어야 이 시나리오가 사고 하나만 재는 것이 된다.</summary>
        public readonly List<string> TrappedBeforeMistake = new List<string>();
        public int BasisDays;
        public int BasisCost;
        public int DayTrapped = -1;
        public int MistakeCost;
        public int HarmWhenFound;

        /// <summary>되돌릴 수 있었는가.</summary>
        public bool Rescued;
        public readonly List<Stroke> RescueStrokes = new List<Stroke>();
        /// <summary>되돌리는 데 든 날. 0 이면 공짜로 되돌린 것이다 — `ButNotFree` 가 그것을 실패로 본다.</summary>
        public int RescueDays;
        public int RescueCost;
        /// <summary>갇혀 있던 동안 쌓인 해. **되돌려도 돌아오지 않는다.**</summary>
        public int HarmPaid;

        public override string ToString()
        {
            return MistakeId + ": 갇힘 " + TrappedAfterMistake.Count + "명 (" + DayTrapped + "일)"
                   + (Rescued ? " → " + RescueStrokes.Count + "획 · " + RescueDays + "일 · "
                                + RescueCost + "원 · 해 " + HarmPaid : " → 못 꺼냈다");
        }
    }

    /// <summary>
    /// ★ 핵 검사기 둘과 셋을 기계가 판정하는 자리.
    ///
    ///   `MistakesAreSurvivable` — 잘못 그은 선을 되돌릴 수 있는가. 못 되돌리면 저장·불러오기 게임이 된다
    ///   `ButNotFree`            — 그런데 공짜는 아닌가. 공짜면 잘못 긋는 것에 무게가 없다
    ///
    /// 되돌리는 방법을 **탐색으로 찾는다.** 데이터에 정답을 적어 두면 「내가 답을 알기 때문에 가능하다」가
    /// 되고, 플레이어가 찾을 수 있다는 증거가 되지 않는다.
    ///
    /// 후보를 갇힌 사람 주변으로 좁힌다 — 도시 전체를 다 그어 보는 것은 사람이 하는 일이 아니고,
    /// 좁히는 것이 오히려 **사람이 실제로 할 수 있는 수**만 세는 것이다.
    /// </summary>
    public static class RescueSearch
    {
        /// <summary>
        /// 사고가 나기 전의 지도를 세우고(basisChapterId 를 Targeted 로 풀어 도시가 닿을 때까지 돌린다),
        /// 그 위에 **잘못 그은 선**을 긋고 사람이 갇힐 때까지 돌린다.
        /// </summary>
        public static CitySim ApplyMistake(GameData d, MistakeDef mk, int seed, SimOptions opt, out RescueResult res)
        {
            res = new RescueResult { MistakeId = mk.id, Seed = seed };
            CitySim sim = Basis(d, mk, seed, opt);
            res.TrappedBeforeMistake.AddRange(sim.Trapped());
            res.BasisDays = sim.Day;
            res.BasisCost = sim.Plan.PermitSpent;

            int budget = int.MaxValue / 4;
            int before = sim.Plan.PermitSpent;
            int applied = 0;
            foreach (EditDef e in mk.edits)
            {
                // 이미 그렇게 그려져 있는 칸은 그냥 넘긴다 — 사고를 여러 칸으로 적어 두면
                // 그 중 몇은 이미 같은 값일 수 있고, 그것이 데이터의 잘못은 아니다.
                if (sim.Plan.At(e.x, e.y) == e.kind[0]) continue;
                if (!sim.Plan.Draw(sim.Day, e.x, e.y, e.kind[0], budget))
                    throw new InvalidOperationException(mk.id + ": 잘못 그을 선을 그을 수 없다 (" + e.x + "," + e.y + ")");
                applied++;
            }
            if (applied == 0)
                throw new InvalidOperationException(mk.id + ": 그을 선이 하나도 남지 않았다 — 이 시나리오가 아무것도 시험하지 않는다");
            res.MistakeCost = sim.Plan.PermitSpent - before;

            for (int i = 0; i < d.Balance.convergeDays; i++)
            {
                sim.Step();
                List<string> t = sim.Trapped();
                if (t.Count > 0 && res.DayTrapped < 0)
                {
                    res.DayTrapped = sim.Day;
                    res.TrappedAfterMistake.AddRange(t);
                    res.TrappedSomeone = true;
                    res.HarmWhenFound = sim.TotalHarm;
                    break;
                }
            }
            return sim;
        }

        /// <summary>
        /// 사고 전의 지도.
        ///
        /// basisChapterId 가 있으면 그 장을 Targeted 로 풀고 도시가 닿을 때까지 돌린 뒤의 상태다.
        /// 없으면 **지도를 처음 받은 날** — 아직 아무 선도 긋지 않았고 도시도 움직이지 않았다.
        /// 뒤쪽이 필요한 이유가 있다: 닿을 때까지 돌리면 무른 구역의 필지가 세 번을 다 써 버려서
        /// 그 뒤에 무엇을 그어도 도시가 거부한다. 잘못 그은 선조차 지어지지 않는 것이다.
        /// </summary>
        public static CitySim Basis(GameData d, MistakeDef mk, int seed, SimOptions opt)
        {
            MapPlan plan = new MapPlan(d);
            if (mk.basisChapterId.Length == 0) return new CitySim(d, plan, seed, opt);
            Policies.DrawFor(d, d.Chapter(mk.basisChapterId), plan);
            CitySim sim = new CitySim(d, plan, seed, opt);
            sim.RunToFixedPoint();
            return sim;
        }

        /// <summary>
        /// 갇힌 사람을 꺼내는 가장 싼 길. 값의 순서는 (획 수 → 날 → 허가비)다 —
        /// 사람이 먼저 세는 것이 「몇 번 더 그어야 하나」이기 때문이다.
        /// </summary>
        public static RescueResult Find(GameData d, MistakeDef mk, int seed, SimOptions opt = null)
        {
            opt = opt ?? SimOptions.From(d);
            RescueResult res;
            CitySim after = ApplyMistake(d, mk, seed, opt, out res);
            if (!res.TrappedSomeone) return res;

            List<char> kinds = new List<char> { Kinds.Street, Kinds.Plaza };
            List<int[]> frontier = Frontier(d, after, res.TrappedAfterMistake);

            // 깊이를 늘려 가며 찾는다. 얕은 답이 있으면 그것이 답이다.
            for (int depth = 1; depth <= d.Balance.rescueMaxEdits; depth++)
            {
                RescueResult best = null;
                Search(d, after, res, frontier, kinds, depth, new List<int[]>(), new List<char>(), ref best);
                if (best != null) return best;
            }
            return res;
        }

        private static void Search(GameData d, CitySim after, RescueResult res,
                                   List<int[]> frontier, List<char> kinds, int depth,
                                   List<int[]> cells, List<char> picks, ref RescueResult best)
        {
            if (cells.Count == depth)
            {
                RescueResult tried = Try(d, after, res, cells, picks);
                if (tried == null) return;
                if (best == null
                    || tried.RescueDays < best.RescueDays
                    || (tried.RescueDays == best.RescueDays && tried.RescueCost < best.RescueCost))
                    best = tried;
                return;
            }
            int start = cells.Count == 0 ? 0 : IndexOf(frontier, cells[cells.Count - 1]) + 1;
            for (int i = start; i < frontier.Count; i++)
                foreach (char k in kinds)
                {
                    cells.Add(frontier[i]); picks.Add(k);
                    Search(d, after, res, frontier, kinds, depth, cells, picks, ref best);
                    cells.RemoveAt(cells.Count - 1); picks.RemoveAt(picks.Count - 1);
                }
        }

        private static RescueResult Try(GameData d, CitySim after, RescueResult res,
                                        List<int[]> cells, List<char> picks)
        {
            CitySim sim = after.Fork();
            int budget = int.MaxValue / 4;
            int before = sim.Plan.PermitSpent;
            int strokesBefore = sim.Plan.Strokes.Count;
            for (int i = 0; i < cells.Count; i++)
                if (!sim.Plan.Draw(sim.Day, cells[i][0], cells[i][1], picks[i], budget)) return null;
            int cost = sim.Plan.PermitSpent - before;

            int day0 = sim.Day;
            for (int i = 0; i < d.Balance.rescueMaxDays; i++)
            {
                sim.Step();
                if (sim.Trapped().Count != 0) continue;

                RescueResult ok = new RescueResult
                {
                    MistakeId = res.MistakeId, Seed = res.Seed, TrappedSomeone = true,
                    DayTrapped = res.DayTrapped, MistakeCost = res.MistakeCost,
                    HarmWhenFound = res.HarmWhenFound,
                    BasisDays = res.BasisDays, BasisCost = res.BasisCost,
                    Rescued = true, RescueDays = sim.Day - day0, RescueCost = cost,
                    HarmPaid = sim.TotalHarm
                };
                ok.TrappedAfterMistake.AddRange(res.TrappedAfterMistake);
                // 지도가 실제로 적어 둔 획을 그대로 쓴다. 다시 계산하면 지우개 덧값이 두 번 붙는다
                for (int s = strokesBefore; s < sim.Plan.Strokes.Count; s++)
                    ok.RescueStrokes.Add(sim.Plan.Strokes[s]);
                return ok;
            }
            return null;
        }

        /// <summary>
        /// 그어 볼 만한 칸. 갇힌 사람이 선 자리에서 `rescueFrontier` 걸음 안에 있는 칸 전부다.
        /// 도시 반대편을 그어 보는 것은 사람이 하는 일이 아니다.
        /// </summary>
        public static List<int[]> Frontier(GameData d, CitySim sim, IList<string> trapped)
        {
            HashSet<int> seen = new HashSet<int>();
            List<int[]> out_ = new List<int[]>();
            CityGrid g = sim.Actual;

            foreach (string id in trapped)
            {
                int[] p = sim.PositionOf(id);
                Queue<int[]> q = new Queue<int[]>();
                Dictionary<int, int> dist = new Dictionary<int, int> { { g.Index(p[0], p[1]), 0 } };
                q.Enqueue(p);
                while (q.Count > 0)
                {
                    int[] c = q.Dequeue();
                    int dc = dist[g.Index(c[0], c[1])];
                    if (dc >= d.Balance.rescueFrontier) continue;
                    foreach (int[] n in g.Neighbours(c[0], c[1]))
                    {
                        int i = g.Index(n[0], n[1]);
                        if (dist.ContainsKey(i)) continue;
                        dist[i] = dc + 1;
                        q.Enqueue(n);
                    }
                }
                foreach (KeyValuePair<int, int> kv in dist)
                {
                    int x = kv.Key % g.Width, y = kv.Key / g.Width;
                    if (!sim.Plan.Editable(x, y)) continue;
                    if (!seen.Add(kv.Key)) continue;
                    out_.Add(new[] { x, y });
                }
            }
            out_.Sort((a, b) => a[1] != b[1] ? a[1] - b[1] : a[0] - b[0]);   // 결정적 순서
            return out_;
        }

        private static int IndexOf(List<int[]> list, int[] cell)
        {
            for (int i = 0; i < list.Count; i++) if (list[i][0] == cell[0] && list[i][1] == cell[1]) return i;
            return -1;
        }
    }
}
