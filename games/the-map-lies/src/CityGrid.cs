using System.Collections.Generic;
using MapLies.Data;

namespace MapLies.Sim
{
    /// <summary>칸의 종류. 문자 하나로 데이터에 적는다 — 사람이 도시를 열어서 볼 수 있어야 한다.</summary>
    public static class Kinds
    {
        public static char Block { get { return 'B'; } }
        public static char Street { get { return 'S'; } }
        public static char Plaza { get { return 'P'; } }
        /// <summary>지도가 아직 안 그린 칸. plan 에만 나온다.</summary>
        public static char Unspecified { get { return '.'; } }

        public static bool Walkable(char k) { return k == Street || k == Plaza; }
        public static bool IsReal(char k) { return k == Block || k == Street || k == Plaza; }

        public static string Name(char k)
        {
            if (k == Block) return "건물";
            if (k == Street) return "길";
            if (k == Plaza) return "광장";
            if (k == Unspecified) return "미지정";
            return "?";
        }
    }

    /// <summary>
    /// 격자 하나. 실제 도시(항공사진)도 이것이고 지도(트레이싱지)도 이것이다 —
    /// **둘이 같은 자료구조라는 것이 이 PoC의 전부다.** 지도가 세계의 모형이고 도시가 그 모형을 따라간다.
    /// </summary>
    public sealed class CityGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private readonly char[] _cells;

        public CityGrid(int width, int height, string[] rows)
        {
            Width = width; Height = height;
            _cells = new char[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    _cells[y * width + x] = rows[y][x];
        }

        private CityGrid(int width, int height, char[] cells)
        {
            Width = width; Height = height; _cells = cells;
        }

        public CityGrid Clone() { return new CityGrid(Width, Height, (char[])_cells.Clone()); }

        public bool Inside(int x, int y) { return x >= 0 && y >= 0 && x < Width && y < Height; }
        public char At(int x, int y) { return _cells[y * Width + x]; }
        public void Set(int x, int y, char k) { _cells[y * Width + x] = k; }
        public int Index(int x, int y) { return y * Width + x; }

        /// <summary>네 방향. 순서를 못 박는다 — 갇힌 사람을 밀어낼 방향도 이 순서를 따른다.</summary>
        private static readonly int[] DX = { 0, 1, 0, -1 };
        private static readonly int[] DY = { -1, 0, 1, 0 };

        public void ForEachNeighbour(int x, int y, System.Action<int, int> f)
        {
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], ny = y + DY[d];
                if (Inside(nx, ny)) f(nx, ny);
            }
        }

        public List<int[]> Neighbours(int x, int y)
        {
            List<int[]> ns = new List<int[]>();
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], ny = y + DY[d];
                if (Inside(nx, ny)) ns.Add(new[] { nx, ny });
            }
            return ns;
        }

        public int CountNeighbours(int x, int y, char kind)
        {
            int n = 0;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], ny = y + DY[d];
                if (Inside(nx, ny) && At(nx, ny) == kind) n++;
            }
            return n;
        }

        public bool OnBorder(int x, int y) { return x == 0 || y == 0 || x == Width - 1 || y == Height - 1; }

        /// <summary>걸어 다닐 수 있는 칸들 중 여기서 닿을 수 있는 것 전부.</summary>
        public HashSet<int> WalkableComponent(int x, int y)
        {
            HashSet<int> seen = new HashSet<int>();
            if (!Inside(x, y) || !Kinds.Walkable(At(x, y))) return seen;
            Queue<int[]> q = new Queue<int[]>();
            q.Enqueue(new[] { x, y });
            seen.Add(Index(x, y));
            while (q.Count > 0)
            {
                int[] c = q.Dequeue();
                foreach (int[] n in Neighbours(c[0], c[1]))
                {
                    if (!Kinds.Walkable(At(n[0], n[1]))) continue;
                    int i = Index(n[0], n[1]);
                    if (!seen.Add(i)) continue;
                    q.Enqueue(n);
                }
            }
            return seen;
        }

        /// <summary>이 칸에서 문 하나에라도 닿는가. 이것이 「갇히지 않았다」의 정의다.</summary>
        public bool ReachesExit(int x, int y, IList<CellRef> exits)
        {
            HashSet<int> comp = WalkableComponent(x, y);
            if (comp.Count == 0) return false;
            foreach (CellRef e in exits) if (comp.Contains(Index(e.x, e.y))) return true;
            return false;
        }

        /// <summary>길에 붙은 건물 칸의 수. 시의회가 「집」으로 세는 것이 이것이다.</summary>
        public int Dwellings()
        {
            int n = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (At(x, y) == Kinds.Block && CountNeighbours(x, y, Kinds.Street) > 0) n++;
            return n;
        }

        /// <summary>이 칸이 든 광장 덩어리의 크기. 광장만 세고 길로는 건너가지 않는다.</summary>
        public int PlazaComponentSize(int x, int y)
        {
            if (!Inside(x, y) || At(x, y) != Kinds.Plaza) return 0;
            HashSet<int> seen = new HashSet<int> { Index(x, y) };
            Queue<int[]> q = new Queue<int[]>();
            q.Enqueue(new[] { x, y });
            while (q.Count > 0)
            {
                int[] c = q.Dequeue();
                foreach (int[] n in Neighbours(c[0], c[1]))
                {
                    if (At(n[0], n[1]) != Kinds.Plaza) continue;
                    if (!seen.Add(Index(n[0], n[1]))) continue;
                    q.Enqueue(n);
                }
            }
            return seen.Count;
        }

        public int Count(char kind)
        {
            int n = 0;
            foreach (char c in _cells) if (c == kind) n++;
            return n;
        }

        public string[] Rows()
        {
            string[] rows = new string[Height];
            for (int y = 0; y < Height; y++)
            {
                char[] row = new char[Width];
                for (int x = 0; x < Width; x++) row[x] = At(x, y);
                rows[y] = new string(row);
            }
            return rows;
        }

        /// <summary>두 격자가 다른 칸의 수. 도시가 지도에 얼마나 어긋나 있는가.</summary>
        public int Disagreement(CityGrid plan)
        {
            int n = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    char p = plan.At(x, y);
                    if (p == Kinds.Unspecified) continue;
                    if (At(x, y) != p) n++;
                }
            return n;
        }
    }
}
