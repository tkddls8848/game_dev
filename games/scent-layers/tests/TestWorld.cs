using System.Collections.Generic;
using Scent.Data;
using Scent.Sim;

namespace Scent.Tests
{
    /// <summary>테스트가 공유하는 세계. data/ 를 한 번만 읽고 무거운 탐색을 재사용한다.</summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<string, ScentField> _fields = new Dictionary<string, ScentField>();
        private static readonly Dictionary<string, SweepReport> _sweeps = new Dictionary<string, SweepReport>();
        private static readonly Dictionary<string, RouteResult> _routes = new Dictionary<string, RouteResult>();

        public static GameData Data
        {
            get { if (_data == null) _data = GameData.Load(); return _data; }
        }

        public static ScentField Field(int seed)
        {
            string k = seed.ToString();
            ScentField f;
            if (_fields.TryGetValue(k, out f)) return f;
            f = ScentField.Build(Data, seed);
            _fields[k] = f;
            return f;
        }

        public static ScentField Field() { return Field(Data.Day.seed); }

        public static Investigation Inv(World w) { return new Investigation(Data, Field(), w); }
        public static Investigation Inv(World w, int seed) { return new Investigation(Data, Field(seed), w); }

        /// <summary>이 세계에서 "한 번 훑기" 전수(방 순열 720가지).</summary>
        public static SweepReport Sweep(World w)
        {
            SweepReport r;
            if (_sweeps.TryGetValue(w.Name, out r)) return r;
            r = Inv(w).BestSingleSweep();
            _sweeps[w.Name] = r;
            return r;
        }

        /// <summary>되돌아가도 되는 최선의 길.</summary>
        public static RouteResult Revisit(World w)
        {
            RouteResult r;
            if (_routes.TryGetValue(w.Name, out r)) return r;
            r = Inv(w).BestRouteWithRevisits(Data.Day.seed, 14);
            _routes[w.Name] = r;
            return r;
        }

        public static int FactCount { get { return Data.Day.visits.Count; } }

        public static List<string> FactIds()
        {
            List<string> ids = new List<string>();
            foreach (VisitDef v in Data.Day.visits) ids.Add(v.id);
            return ids;
        }

        public static string Clock(int min)
        {
            int h = (min / 60) % 24, m = min % 60;
            return h.ToString("D2") + ":" + m.ToString("D2");
        }
    }
}
