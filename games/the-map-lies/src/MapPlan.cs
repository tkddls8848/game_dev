using System.Collections.Generic;
using MapLies.Data;

namespace MapLies.Sim
{
    /// <summary>지도에 그은 선 하나. 몇 번째 겹인지가 트레이싱지의 두께가 된다.</summary>
    public sealed class Stroke
    {
        public int Day;
        public int X;
        public int Y;
        public char Kind;
        public char Was;
        public int Cost;
        /// <summary>이 칸을 다시 그은 것인가. 지우개 자국은 공짜가 아니다.</summary>
        public bool Redraw;
        public override string ToString()
        {
            return Day + "일 (" + X + "," + Y + ") " + Kinds.Name(Was) + "→" + Kinds.Name(Kind)
                   + " " + Cost + "원" + (Redraw ? " (다시 그음)" : "");
        }
    }

    /// <summary>
    /// 지도. **완벽하다.** 틀린 것은 도시 쪽이고, 도시가 매일 조금씩 지도에 맞춰 스스로를 고친다.
    ///
    /// 플레이어가 만지는 것은 이것뿐이다 — 도시를 직접 건드릴 수단은 없다.
    /// 그래서 잘못 그은 선을 되돌리는 것도 **지도를 다시 그리는 것**이고, 값과 날이 든다.
    /// </summary>
    public sealed class MapPlan
    {
        private readonly GameData _data;
        private readonly CityGrid _plan;
        private readonly HashSet<int> _locked = new HashSet<int>();
        private readonly Dictionary<int, int> _drawnCount = new Dictionary<int, int>();

        public readonly List<Stroke> Strokes = new List<Stroke>();
        public int PermitSpent { get; private set; }

        public MapPlan(GameData data)
        {
            _data = data;
            _plan = new CityGrid(data.City.width, data.City.height, data.City.planRows);

            foreach (CellRef c in data.City.lockedCells) _locked.Add(_plan.Index(c.x, c.y));
            foreach (CellRef e in data.City.exits) _locked.Add(_plan.Index(e.x, e.y));
            if (data.Balance.borderLocked)
                for (int y = 0; y < _plan.Height; y++)
                    for (int x = 0; x < _plan.Width; x++)
                        if (_plan.OnBorder(x, y)) _locked.Add(_plan.Index(x, y));
        }

        public CityGrid Grid { get { return _plan; } }
        public char At(int x, int y) { return _plan.At(x, y); }

        public bool Editable(int x, int y)
        {
            return _plan.Inside(x, y) && !_locked.Contains(_plan.Index(x, y));
        }

        /// <summary>이 칸을 몇 번 그었는가. 트레이싱지 겹의 수다.</summary>
        public int DrawnCount(int x, int y)
        {
            int n;
            return _drawnCount.TryGetValue(_plan.Index(x, y), out n) ? n : 0;
        }

        /// <summary>
        /// 한 칸을 그린다. 값은 종류값 + (다시 그으면) 덧값.
        /// 예산을 넘거나 잠긴 칸이면 아무 일도 일어나지 않는다 — false 를 돌려준다.
        /// </summary>
        public bool Draw(int day, int x, int y, char kind, int permitBudget)
        {
            if (!Editable(x, y)) return false;
            if (!Kinds.IsReal(kind)) return false;
            char was = _plan.At(x, y);
            if (was == kind) return false;

            int idx = _plan.Index(x, y);
            bool redraw = DrawnCount(x, y) > 0;
            int cost = _data.EditCost(kind) + (redraw ? _data.Balance.redrawSurcharge : 0);
            if (PermitSpent + cost > permitBudget) return false;

            _plan.Set(x, y, kind);
            PermitSpent += cost;
            _drawnCount[idx] = DrawnCount(x, y) + 1;
            Strokes.Add(new Stroke
            {
                Day = day, X = x, Y = y, Kind = kind, Was = was, Cost = cost, Redraw = redraw
            });
            return true;
        }

        /// <summary>미리 값을 재 본다. 되돌리는 값이 처음 그은 값보다 큰지를 ButNotFree 가 여기로 본다.</summary>
        public int CostOf(int x, int y, char kind)
        {
            return _data.EditCost(kind) + (DrawnCount(x, y) > 0 ? _data.Balance.redrawSurcharge : 0);
        }

        public MapPlan Fork()
        {
            MapPlan copy = new MapPlan(_data);
            for (int y = 0; y < _plan.Height; y++)
                for (int x = 0; x < _plan.Width; x++)
                    copy._plan.Set(x, y, _plan.At(x, y));
            foreach (KeyValuePair<int, int> kv in _drawnCount) copy._drawnCount[kv.Key] = kv.Value;
            copy.PermitSpent = PermitSpent;
            copy.Strokes.AddRange(Strokes);
            return copy;
        }

        /// <summary>고칠 수 있는 칸 전부. 정책과 구조 탐색이 여기서 후보를 뽑는다.</summary>
        public List<int[]> EditableCells()
        {
            List<int[]> cells = new List<int[]>();
            for (int y = 0; y < _plan.Height; y++)
                for (int x = 0; x < _plan.Width; x++)
                    if (Editable(x, y)) cells.Add(new[] { x, y });
            return cells;
        }
    }
}
