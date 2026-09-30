using System.Collections.Generic;
using MapLies.Data;

namespace MapLies.Sim
{
    /// <summary>
    /// 정책들. 시의회의 요구 하나를 서로 다른 방식으로 그려 본다.
    ///
    /// 이름               하는 일
    /// ─────────────────────────────────────────────────────────────────────────
    /// Targeted     닿아야 하는 칸까지 **가장 적게 파서** 길을 내고, 광장은 필요한 만큼만 넓힌다
    /// PaveEverything  고칠 수 있는 칸을 전부 길로 덮는다 — 닿기는 되는데 집이 사라진다
    /// BlockEverything 전부 건물로 덮는다 — 집은 늘지만 사람이 갇힌다
    /// PlazaEverything 전부 광장으로 덮는다
    /// NoEdit       아무것도 그리지 않는다 (아래쪽 대조군)
    ///
    /// **한 종류로 덮는 정책이 반드시 어딘가에서 무너져야** `NoDominantStrategy` 가 통과한다.
    /// </summary>
    public static class Policies
    {
        public static string[] Names()
        {
            return new[] { "Targeted", "PaveEverything", "BlockEverything", "PlazaEverything", "NoEdit" };
        }

        public static ChapterResult Run(GameData d, string chapterId, string policy, int trial,
                                        SimOptions opt = null)
        {
            ChapterDef ch = d.Chapter(chapterId);
            int seed = unchecked(ch.seed + trial * 7919);
            MapPlan plan = new MapPlan(d);

            switch (policy)
            {
                case "Targeted": DrawTargeted(d, ch, plan); break;
                case "PaveEverything": Blanket(d, ch, plan, Kinds.Street); break;
                case "BlockEverything": Blanket(d, ch, plan, Kinds.Block); break;
                case "PlazaEverything": Blanket(d, ch, plan, Kinds.Plaza); break;
                case "NoEdit": break;
                default: throw new KeyNotFoundException("없는 정책: " + policy);
            }

            CitySim sim = new CitySim(d, plan, seed, opt);
            sim.RunToFixedPoint(ch.dayLimit);
            return Goals.Evaluate(d, ch, sim, policy);
        }

        private static void Blanket(GameData d, ChapterDef ch, MapPlan plan, char kind)
        {
            foreach (int[] c in plan.EditableCells())
                plan.Draw(0, c[0], c[1], kind, ch.permitBudget);
        }

        // ── 골라 그리는 정책 ─────────────────────────────────────────────────

        /// <summary>외부에서도 쓴다 — 사고 시나리오가 「사고 전의 지도」를 세울 때.</summary>
        public static void DrawFor(GameData d, ChapterDef ch, MapPlan plan) { DrawTargeted(d, ch, plan); }

        /// <summary>
        /// 골라 그린다. 한 번 그리고 끝이 아니라 **그린 뒤에 도시가 어떻게 될지 다시 내다보고** 또 그린다 —
        /// 미지정 칸을 도시가 메우면서 방금 낸 길이 막히는 일이 실제로 일어난다.
        /// </summary>
        private static void DrawTargeted(GameData d, ChapterDef ch, MapPlan plan)
        {
            for (int round = 0; round < 6; round++)
            {
                CityGrid pred = Predict(d, plan);
                bool progress = false;

                // ① 광장을 먼저 넓힌다. 광장도 걸을 수 있으므로 ②의 길이 짧아진다
                if (ch.plazaMin > 0 && pred.PlazaComponentSize(ch.plazaSeed.x, ch.plazaSeed.y) < ch.plazaMin)
                { GrowPlaza(d, ch, plan, pred); progress = true; }

                // ② 닿아야 하는 칸마다 **가장 적게 파는 길**을 내고 그 칸만 길로 그린다
                foreach (CellRef t in ReachTargets(d, ch))
                {
                    if (pred.ReachesExit(t.x, t.y, d.City.exits)) continue;
                    if (CarveTo(d, ch, plan, t, pred)) progress = true;
                }
                if (!progress) break;
            }
        }

        /// <summary>
        /// 닿아야 하는 칸. 시의회가 적은 것에 **사람들이 서 있는 칸**을 더한다 —
        /// requireNoTrapped 가 켜져 있으면 그것도 요구 사항이기 때문이다.
        /// </summary>
        private static List<CellRef> ReachTargets(GameData d, ChapterDef ch)
        {
            List<CellRef> all = new List<CellRef>(ch.reachCells);
            if (!ch.requireNoTrapped) return all;
            foreach (CitizenDef c in d.AllCitizens)
            {
                bool dup = false;
                foreach (CellRef r in all) if (r.x == c.x && r.y == c.y) dup = true;
                if (!dup) all.Add(new CellRef { x = c.x, y = c.y });
            }
            return all;
        }

        /// <summary>
        /// **이 지도로 두면 도시가 무엇이 되는가.** 플레이어는 규칙을 알고 있으므로 스스로 돌려 볼 수 있다.
        /// 미지정 칸을 도시가 어떻게 메울지까지 여기서 나온다 — 그래서 「그리지 않는 것」도 하나의 선택이 된다.
        /// </summary>
        public static CityGrid Predict(GameData d, MapPlan plan)
        {
            SimOptions quiet = SimOptions.From(d);
            quiet.CitizensWalk = false;
            quiet.HarmAccrues = false;
            CitySim sim = new CitySim(d, plan.Fork(), 0, quiet);
            sim.RunToFixedPoint();
            return sim.Actual;
        }

        private static void GrowPlaza(GameData d, ChapterDef ch, MapPlan plan, CityGrid pred)
        {
            CityGrid g = pred.Clone();
            CellRef seed = ch.plazaSeed;
            if (g.At(seed.x, seed.y) != Kinds.Plaza && plan.Editable(seed.x, seed.y))
            { plan.Draw(0, seed.x, seed.y, Kinds.Plaza, ch.permitBudget); g.Set(seed.x, seed.y, Kinds.Plaza); }

            int guard = 0;
            while (g.PlazaComponentSize(seed.x, seed.y) < ch.plazaMin && guard++ < 200)
            {
                // 광장 덩어리에 붙은 칸 중 고칠 수 있는 가장 가까운 것 하나
                int[] pick = null;
                HashSet<int> comp = PlazaCells(g, seed.x, seed.y);
                foreach (int i in comp)
                {
                    int cx = i % g.Width, cy = i / g.Width;
                    foreach (int[] n in g.Neighbours(cx, cy))
                    {
                        if (g.At(n[0], n[1]) == Kinds.Plaza) continue;
                        if (!plan.Editable(n[0], n[1])) continue;
                        if (pick == null || n[1] < pick[1] || (n[1] == pick[1] && n[0] < pick[0])) pick = n;
                    }
                }
                if (pick == null) break;
                if (!plan.Draw(0, pick[0], pick[1], Kinds.Plaza, ch.permitBudget)) break;
                g.Set(pick[0], pick[1], Kinds.Plaza);
            }
        }

        private static HashSet<int> PlazaCells(CityGrid g, int x, int y)
        {
            HashSet<int> seen = new HashSet<int>();
            if (g.At(x, y) != Kinds.Plaza) return seen;
            Queue<int[]> q = new Queue<int[]>();
            q.Enqueue(new[] { x, y }); seen.Add(g.Index(x, y));
            while (q.Count > 0)
            {
                int[] c = q.Dequeue();
                foreach (int[] n in g.Neighbours(c[0], c[1]))
                    if (g.At(n[0], n[1]) == Kinds.Plaza && seen.Add(g.Index(n[0], n[1]))) q.Enqueue(n);
            }
            return seen;
        }

        /// <summary>
        /// 이 칸에서 문까지 **파야 하는 칸이 가장 적은 길**. 걸을 수 있는 칸은 값 0,
        /// 고칠 수 있는 막힌 칸은 값 1, 못 고치는 막힌 칸은 지나갈 수 없다.
        /// 다익스트라이고 값이 0/1 이므로 앞뒤로 넣는 큐 하나로 충분하다.
        /// </summary>
        private static bool CarveTo(GameData d, ChapterDef ch, MapPlan plan, CellRef from, CityGrid pred)
        {
            CityGrid g = pred;
            if (g.ReachesExit(from.x, from.y, d.City.exits)) return false;

            int n = g.Width * g.Height;
            int[] dist = new int[n];
            int[] prev = new int[n];
            for (int i = 0; i < n; i++) { dist[i] = int.MaxValue; prev[i] = -1; }

            int start = g.Index(from.x, from.y);
            dist[start] = Kinds.Walkable(g.At(from.x, from.y)) ? 0 : 1;
            LinkedList<int> dq = new LinkedList<int>();
            dq.AddFirst(start);

            while (dq.Count > 0)
            {
                int cur = dq.First.Value; dq.RemoveFirst();
                int cx = cur % g.Width, cy = cur / g.Width;
                foreach (int[] nb in g.Neighbours(cx, cy))
                {
                    int i = g.Index(nb[0], nb[1]);
                    bool walk = Kinds.Walkable(g.At(nb[0], nb[1]));
                    if (!walk && !plan.Editable(nb[0], nb[1])) continue;
                    int w = walk ? 0 : 1;
                    if (dist[cur] + w >= dist[i]) continue;
                    dist[i] = dist[cur] + w;
                    prev[i] = cur;
                    if (w == 0) dq.AddFirst(i); else dq.AddLast(i);
                }
            }

            int bestExit = -1;
            foreach (CellRef e in d.City.exits)
            {
                int i = g.Index(e.x, e.y);
                if (dist[i] == int.MaxValue) continue;
                if (bestExit < 0 || dist[i] < dist[bestExit]) bestExit = i;
            }
            if (bestExit < 0) return false;

            bool drew = false;
            for (int at = bestExit; at >= 0; at = prev[at])
            {
                int x = at % g.Width, y = at / g.Width;
                if (Kinds.Walkable(g.At(x, y))) continue;
                if (!plan.Editable(x, y)) continue;
                if (plan.Draw(0, x, y, Kinds.Street, ch.permitBudget)) drew = true;
            }
            return drew;
        }
    }
}
