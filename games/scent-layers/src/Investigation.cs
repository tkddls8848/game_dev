using System;
using System.Collections.Generic;
using Scent.Data;

namespace Scent.Sim
{
    /// <summary>한 번의 조사. 방을 골라 맡고, 걷고, 코가 지칠 때까지.</summary>
    public sealed class Investigation
    {
        public readonly GameData D;
        public readonly ScentField F;
        public readonly Nose N;
        public readonly World W;

        public Investigation(GameData d, ScentField f, World w) { D = d; F = f; W = w; N = new Nose(d, f); }

        /// <summary>방 차례 하나를 그대로 걸어 본 결과.</summary>
        public RouteResult Run(IList<string> route)
        {
            InvestigationDef iv = D.Balance.investigation;
            RouteResult res = new RouteResult();
            string at = iv.startRoom;
            int t = iv.startMin;
            HashSet<string> got = new HashSet<string>();

            for (int i = 0; i < route.Count; i++)
            {
                if (i >= iv.maxSniffs) { res.overBudget = true; break; }
                t += D.Walk(at, route[i]);
                at = route[i];
                t += iv.sniffCostMin;
                if (t > iv.endMin) { res.overTime = true; break; }

                Reading r = N.Sniff(at, t, W);
                res.steps.Add(new RouteStep { roomId = at, atMin = t, reading = r });
                int before = got.Count;
                foreach (DatedLayer dl in r.dated)
                {
                    VisitDef v = D.Visit(dl.layerId);
                    if (v.atMin == dl.fittedAtMin) got.Add(dl.layerId);
                }
                res.newPerStep.Add(got.Count - before);
                foreach (string a in r.recognized) res.everRecognized.Add(a + "@" + at);
            }
            res.recovered = got;
            res.endMin = t;
            return res;
        }

        /// <summary>방마다 한 번씩, 어떤 차례로든 — "한 번 훑기" 전수.</summary>
        public SweepReport BestSingleSweep()
        {
            List<string> rooms = new List<string>(D.RoomIds);
            SweepReport best = new SweepReport();
            Permute(rooms, 0, best);
            return best;
        }

        private void Permute(List<string> a, int k, SweepReport best)
        {
            if (k == a.Count)
            {
                RouteResult r = Run(a);
                best.sweeps++;
                best.totalRecovered += r.recovered.Count;
                if (r.recovered.Count > best.bestCount)
                {
                    best.bestCount = r.recovered.Count;
                    best.bestRoute = new List<string>(a);
                    best.bestRecovered = new HashSet<string>(r.recovered);
                }
                foreach (string id in r.recovered) best.everRecoveredBySomeSweep.Add(id);
                return;
            }
            for (int i = k; i < a.Count; i++)
            {
                string t = a[k]; a[k] = a[i]; a[i] = t;
                Permute(a, k + 1, best);
                t = a[k]; a[k] = a[i]; a[i] = t;
            }
        }

        /// <summary>
        /// 되돌아가도 되는 길 찾기. 너비 제한 탐색(빔) — 매 걸음 모든 방을 후보로 둔다.
        /// 씨드는 데이터에서 온다(설계 원칙 5). 동점을 가르는 데만 쓴다.
        /// </summary>
        public RouteResult BestRouteWithRevisits(int seed, int beam)
        {
            InvestigationDef iv = D.Balance.investigation;
            Random rng = new Random(seed);
            List<List<string>> frontier = new List<List<string>> { new List<string>() };
            RouteResult best = Run(new List<string>());

            for (int depth = 0; depth < iv.maxSniffs; depth++)
            {
                List<KeyValuePair<int, List<string>>> scored = new List<KeyValuePair<int, List<string>>>();
                foreach (List<string> path in frontier)
                {
                    foreach (string room in D.RoomIds)
                    {
                        List<string> next = new List<string>(path);
                        next.Add(room);
                        RouteResult r = Run(next);
                        if (r.steps.Count < next.Count) continue;          // 시간이나 횟수를 넘겼다
                        int score = r.recovered.Count * 1000 - (r.endMin - iv.startMin) / 10 + rng.Next(3);
                        scored.Add(new KeyValuePair<int, List<string>>(score, next));
                        if (r.recovered.Count > best.recovered.Count ||
                            (r.recovered.Count == best.recovered.Count && r.endMin < best.endMin)) best = r;
                    }
                }
                if (scored.Count == 0) break;
                scored.Sort(delegate (KeyValuePair<int, List<string>> x, KeyValuePair<int, List<string>> y) { return y.Key.CompareTo(x.Key); });
                frontier = new List<List<string>>();
                for (int i = 0; i < scored.Count && i < beam; i++) frontier.Add(scored[i].Value);
            }
            return best;
        }

        /// <summary>
        /// 사실 하나가 **맡을 수 있는 순간들**. 조사 시간 격자를 전부 훑어
        /// "그 방에 그때 있었다면 이 사실을 복원했는가"를 본다. DecayIsFair 가 이것을 쓴다.
        /// </summary>
        public List<int> WindowOf(string visitId)
        {
            InvestigationDef iv = D.Balance.investigation;
            VisitDef v = D.Visit(visitId);
            List<int> slots = new List<int>();
            for (int t = iv.startMin; t <= iv.endMin; t += 1)
            {
                Reading r = N.Sniff(v.room, t, W);
                foreach (DatedLayer dl in r.dated)
                {
                    if (dl.layerId == visitId && dl.fittedAtMin == v.atMin) { slots.Add(t); break; }
                }
            }
            return slots;
        }

        /// <summary>플레이어가 실제로 그 방에 닿을 수 있는 가장 이른 시각.</summary>
        public int EarliestReach(string roomId)
        {
            InvestigationDef iv = D.Balance.investigation;
            return iv.startMin + D.Walk(iv.startRoom, roomId) + iv.sniffCostMin;
        }
    }

    public sealed class RouteStep { public string roomId; public int atMin; public Reading reading; }

    public sealed class RouteResult
    {
        public List<RouteStep> steps = new List<RouteStep>();
        public List<int> newPerStep = new List<int>();
        public HashSet<string> recovered = new HashSet<string>();
        public HashSet<string> everRecognized = new HashSet<string>();
        public int endMin;
        public bool overBudget;
        public bool overTime;

        public string RouteText
        {
            get
            {
                List<string> p = new List<string>();
                foreach (RouteStep s in steps) p.Add(s.roomId + "@" + s.atMin);
                return string.Join(" > ", p);
            }
        }

        /// <summary>몇 개의 방을 두 번 이상 맡았는가.</summary>
        public int RevisitedRooms
        {
            get
            {
                Dictionary<string, int> c = new Dictionary<string, int>();
                foreach (RouteStep s in steps) c[s.roomId] = (c.ContainsKey(s.roomId) ? c[s.roomId] : 0) + 1;
                int n = 0;
                foreach (KeyValuePair<string, int> kv in c) if (kv.Value > 1) n++;
                return n;
            }
        }
    }

    public sealed class SweepReport
    {
        public int sweeps;
        public int totalRecovered;
        public int bestCount;
        public List<string> bestRoute = new List<string>();
        public HashSet<string> bestRecovered = new HashSet<string>();
        public HashSet<string> everRecoveredBySomeSweep = new HashSet<string>();
    }
}
