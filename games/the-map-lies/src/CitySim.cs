using System;
using System.Collections.Generic;
using MapLies.Data;

namespace MapLies.Sim
{
    /// <summary>하루의 기록. 트레이싱지 한 겹이 이것이다.</summary>
    public sealed class DayRecord
    {
        public int Day;
        public int Flips;
        public int Disagreement;
        public int Refused;
        public int HarmToday;
        public int HarmTotal;
        public readonly List<string> Trapped = new List<string>();
        public readonly List<string> Entombed = new List<string>();
        /// <summary>그 날의 도시. 목업이 겹쳐 그릴 항공사진이다.</summary>
        public string[] ActualRows;
    }

    /// <summary>음성 대조군이 세계를 바꿔 끼우는 손잡이. 기본값은 balance.json 이 정한다.</summary>
    public sealed class SimOptions
    {
        public int MaxFlipsPerCell;
        /// <summary>참이면 도시가 하루 만에 지도에 맞춘다. 「되돌리기가 공짜인 세계」의 대조군.</summary>
        public bool InstantConform;
        /// <summary>거짓이면 갇혀도 해가 쌓이지 않는다. ButNotFree 의 대조군.</summary>
        public bool HarmAccrues = true;
        public bool CitizensWalk = true;

        public static SimOptions From(GameData d)
        {
            return new SimOptions { MaxFlipsPerCell = d.Balance.maxFlipsPerCell };
        }

        public SimOptions Clone()
        {
            return new SimOptions
            {
                MaxFlipsPerCell = MaxFlipsPerCell, InstantConform = InstantConform,
                HarmAccrues = HarmAccrues, CitizensWalk = CitizensWalk
            };
        }
    }

    /// <summary>
    /// ★ **도시가 지도에 맞춰 스스로를 고치는 규칙 전부가 여기 있다.**
    ///
    /// 하루에 일어나는 일:
    ///   ① 어제의 도시를 찍어 두고, 칸마다 **목표**를 정한다
    ///        · 지도가 그린 칸이면 지도가 목표다 (conform)
    ///        · 미지정 칸이면 어제의 이웃에서 도시가 스스로 짐작한다 (infer)
    ///   ② 목표와 다른 칸에 공사 압력이 쌓이고, 값을 채우면 뒤집힌다
    ///   ③ **한 필지는 maxFlipsPerCell 번만 다시 지을 수 있다.** 다 쓰면 도시가 거부한다
    ///   ④ 사람이 걷는다. 서 있던 칸이 건물이 되면 밀려나고, 밀릴 곳이 없으면 묻힌다
    ///   ⑤ 문에 닿지 못하는 사람은 갇힌 것이고, 갇힌 하루마다 해가 쌓인다
    ///
    /// **③이 수렴을 보장하는 단 하나의 장치다.** 뒤집기의 총수가 칸수 x 상한으로 막혀 있으므로
    /// 고정점이 반드시 있다. 상한을 없애면(-1) 도시는 영원히 진동한다 — `CityConverges` 의
    /// 음성 대조군이 그것을 보인다. 그리고 이 규칙은 게임의 말이기도 하다:
    /// **필지가 닳으면 도시는 지도를 무시하고 그 자리에 그대로 남는다. 지도는 영원히 거짓말이 된다.**
    ///
    /// 갱신은 **어제의 사진에서 동시에** 일어난다. 하루의 공사는 어제의 측량으로 계획되기 때문이다.
    /// 그래서 진동이 실제로 생길 수 있고, 그 진동을 막는 것이 규칙이어야 한다.
    /// </summary>
    public sealed class CitySim
    {
        private readonly GameData _data;
        private readonly SimOptions _opt;
        private CityGrid _actual;
        private readonly MapPlan _plan;
        private readonly int[] _pressure;
        private readonly int[] _flips;
        private readonly Dictionary<string, int[]> _pos = new Dictionary<string, int[]>();
        private readonly Dictionary<string, int> _harm = new Dictionary<string, int>();
        private readonly HashSet<string> _entombed = new HashSet<string>();
        private Random _rng;
        private int _rngDraws;

        public int Day { get; private set; }
        public int QuietRun { get; private set; }
        public bool Converged { get; private set; }
        public int ConvergedOnDay { get; private set; }
        public readonly List<DayRecord> Chronicle = new List<DayRecord>();
        public int Seed { get; private set; }

        public CitySim(GameData data, MapPlan plan, int seed, SimOptions opt = null)
        {
            _data = data;
            _plan = plan;
            _opt = opt ?? SimOptions.From(data);
            _actual = new CityGrid(data.City.width, data.City.height, data.City.actualRows);
            _pressure = new int[_actual.Width * _actual.Height];
            _flips = new int[_actual.Width * _actual.Height];
            Seed = seed;
            _rng = new Random(seed);
            ConvergedOnDay = -1;
            foreach (CitizenDef c in data.AllCitizens)
            {
                _pos[c.id] = new[] { c.x, c.y };
                _harm[c.id] = 0;
            }
            Record(0);
        }

        private CitySim(CitySim src)
        {
            _data = src._data;
            _opt = src._opt.Clone();
            _plan = src._plan.Fork();
            _actual = src._actual.Clone();
            _pressure = (int[])src._pressure.Clone();
            _flips = (int[])src._flips.Clone();
            foreach (KeyValuePair<string, int[]> kv in src._pos) _pos[kv.Key] = new[] { kv.Value[0], kv.Value[1] };
            foreach (KeyValuePair<string, int> kv in src._harm) _harm[kv.Key] = kv.Value;
            foreach (string e in src._entombed) _entombed.Add(e);
            Day = src.Day; QuietRun = src.QuietRun; Converged = src.Converged;
            ConvergedOnDay = src.ConvergedOnDay; Seed = src.Seed;
            // 난수 상태를 이어 가려면 같은 씨드에서 같은 횟수를 다시 뽑는다 — 결정적이다
            _rng = new Random(src.Seed);
            for (int i = 0; i < src._rngDraws; i++) _rng.Next();
            _rngDraws = src._rngDraws;
            Chronicle.AddRange(src.Chronicle);
        }

        /// <summary>이 순간에서 갈라진 세계. 되돌릴 길을 찾는 탐색이 이것을 쓴다.</summary>
        public CitySim Fork() { return new CitySim(this); }

        public CityGrid Actual { get { return _actual; } }
        public MapPlan Plan { get { return _plan; } }
        public SimOptions Options { get { return _opt; } }
        public int[] PositionOf(string citizenId) { return _pos[citizenId]; }
        public int HarmOf(string citizenId) { return _harm[citizenId]; }
        public bool IsEntombed(string citizenId) { return _entombed.Contains(citizenId); }
        public int FlipsAt(int x, int y) { return _flips[_actual.Index(x, y)]; }

        public int TotalHarm
        {
            get { int n = 0; foreach (KeyValuePair<string, int> kv in _harm) n += kv.Value; return n; }
        }

        public List<string> Trapped()
        {
            List<string> t = new List<string>();
            foreach (CitizenDef c in _data.AllCitizens)
            {
                int[] p = _pos[c.id];
                if (!_actual.ReachesExit(p[0], p[1], _data.City.exits)) t.Add(c.id);
            }
            return t;
        }

        /// <summary>
        /// 지도가 무슨 말을 해도 도시가 끝내 듣지 않는 칸. **거부다.**
        /// 목업이 「일그러진 자리」로 그리는 것이 정확히 이것이다.
        /// </summary>
        public List<int[]> RefusedCells()
        {
            List<int[]> bad = new List<int[]>();
            if (_opt.MaxFlipsPerCell < 0) return bad;
            for (int y = 0; y < _actual.Height; y++)
                for (int x = 0; x < _actual.Width; x++)
                {
                    char p = _plan.At(x, y);
                    if (p == Kinds.Unspecified || _actual.At(x, y) == p) continue;
                    if (_flips[_actual.Index(x, y)] >= _opt.MaxFlipsPerCell) bad.Add(new[] { x, y });
                }
            return bad;
        }

        // ── 하루 ─────────────────────────────────────────────────────────────

        public void Step()
        {
            CityGrid yesterday = _actual;              // 동시 갱신 — 어제의 사진에서 목표를 정한다
            CityGrid today = yesterday.Clone();
            int flips = 0;

            for (int y = 0; y < yesterday.Height; y++)
                for (int x = 0; x < yesterday.Width; x++)
                {
                    char planned = _plan.At(x, y);
                    bool specified = planned != Kinds.Unspecified;
                    char target = specified ? planned : Infer(yesterday, x, y);
                    int i = yesterday.Index(x, y);

                    if (target == yesterday.At(x, y)) { _pressure[i] = 0; continue; }

                    int push = specified ? _data.Balance.conformPressurePerDay : _data.Balance.inferPressurePerDay;
                    if (_opt.InstantConform && specified) push = _data.FlipCost(target);
                    _pressure[i] += push;

                    if (_pressure[i] < _data.FlipCost(target)) continue;
                    if (_opt.MaxFlipsPerCell >= 0 && _flips[i] >= _opt.MaxFlipsPerCell)
                    {
                        _pressure[i] = _data.FlipCost(target);    // 넘치지 않게 눌러 둔다
                        continue;                                  // 도시가 거부했다
                    }
                    today.Set(x, y, target);
                    _pressure[i] = 0;
                    _flips[i]++;
                    flips++;
                }

            _actual = today;
            Day++;
            MoveCitizens();
            AccrueHarm();

            if (flips == 0) QuietRun++; else QuietRun = 0;
            if (!Converged && QuietRun >= _data.Balance.quietDays)
            {
                Converged = true;
                ConvergedOnDay = Day - _data.Balance.quietDays;
            }
            Record(flips);
        }

        /// <summary>
        /// 지도가 안 그린 칸을 도시가 스스로 메운다.
        ///
        ///   길 이웃이 너무 많으면 **건물**을 세운다 — 교차로 가운데를 비워 두지 않는다
        ///   길 이웃이 둘 이상이면 길을 뚫는다 — 끊긴 길을 잇는다
        ///   광장 이웃이 둘 이상이면 광장으로 넓힌다
        ///   그 밖에는 건물
        ///
        /// **첫 줄이 진동의 씨앗이다.** 길이 생기면 옆 칸은 건물을 원하고, 건물이 생기면 다시 길을 원한다.
        /// 필지 상한이 없으면 이 짝이 영원히 뒤집힌다.
        /// </summary>
        public char Infer(CityGrid from, int x, int y)
        {
            int s = from.CountNeighbours(x, y, Kinds.Street);
            int p = from.CountNeighbours(x, y, Kinds.Plaza);
            if (s >= _data.Balance.inferBlockOverStreets) return Kinds.Block;
            if (s >= _data.Balance.inferStreetMin) return Kinds.Street;
            if (p >= _data.Balance.inferPlazaMin) return Kinds.Plaza;
            return Kinds.Block;
        }

        private void MoveCitizens()
        {
            foreach (CitizenDef c in _data.AllCitizens)
            {
                int[] p = _pos[c.id];

                // 서 있던 칸이 건물이 됐다. 밀려난다 — 밀릴 곳이 없으면 묻힌다
                if (!Kinds.Walkable(_actual.At(p[0], p[1])))
                {
                    bool pushed = false;
                    foreach (int[] n in _actual.Neighbours(p[0], p[1]))
                        if (Kinds.Walkable(_actual.At(n[0], n[1])))
                        { _pos[c.id] = n; _entombed.Remove(c.id); pushed = true; break; }
                    if (!pushed) { _entombed.Add(c.id); continue; }
                    p = _pos[c.id];
                }
                else _entombed.Remove(c.id);

                if (!_opt.CitizensWalk) continue;
                for (int step = 0; step < _data.Balance.walkStepsPerDay; step++)
                {
                    List<int[]> open = new List<int[]>();
                    foreach (int[] n in _actual.Neighbours(p[0], p[1]))
                    {
                        if (!Kinds.Walkable(_actual.At(n[0], n[1]))) continue;
                        // 사람은 사는 데서 산다. 집에서 반경 밖으로는 가지 않는다
                        if (Math.Abs(n[0] - c.x) + Math.Abs(n[1] - c.y) > c.walkRadius) continue;
                        open.Add(n);
                    }
                    open.Add(new[] { p[0], p[1] });        // 그 자리에 머무는 것도 한 수다
                    int pick = _rng.Next(open.Count);
                    _rngDraws++;
                    p = open[pick];
                    _pos[c.id] = p;
                }
            }
        }

        private void AccrueHarm()
        {
            if (!_opt.HarmAccrues) return;
            foreach (string id in Trapped())
            {
                CitizenDef c = _data.Citizen(id);
                _harm[id] += c.harmPerDayTrapped + (_entombed.Contains(id) ? _data.Balance.entombExtraHarm : 0);
            }
        }

        private void Record(int flips)
        {
            DayRecord r = new DayRecord
            {
                Day = Day, Flips = flips,
                Disagreement = _actual.Disagreement(_plan.Grid),
                Refused = RefusedCells().Count,
                HarmTotal = TotalHarm,
                ActualRows = _actual.Rows()
            };
            foreach (string id in Trapped()) r.Trapped.Add(id);
            foreach (string id in _entombed) r.Entombed.Add(id);
            r.Entombed.Sort(StringComparer.Ordinal);
            if (Chronicle.Count > 0) r.HarmToday = r.HarmTotal - Chronicle[Chronicle.Count - 1].HarmTotal;
            Chronicle.Add(r);
        }

        public void Advance(int days) { for (int i = 0; i < days; i++) Step(); }

        /// <summary>고정점까지 돌린다. 못 닿으면 false — `CityConverges` 가 그것을 실패로 본다.</summary>
        public bool RunToFixedPoint(int maxDays = -1)
        {
            int limit = maxDays < 0 ? _data.Balance.convergeDays : maxDays;
            for (int i = 0; i < limit && !Converged; i++) Step();
            return Converged;
        }

        /// <summary>
        /// 진동을 이름으로 잡는다 — 도시의 모습이 전에 나온 모습으로 되돌아오면 주기다.
        /// 고정점(주기 1)은 세지 않는다.
        ///
        /// **tailDays 가 필수인 이유가 있다.** 연대기 전체를 보면 멈추는 도시도 주기가 잡힌다 —
        /// 무른 구역이 필지를 다 쓰기 **전까지** 실제로 몇 번 뒤집히고, 그 사이에 같은 모습이
        /// 두 번 나오기 때문이다. 「지금도 진동하는가」를 묻는 것이므로 **끝 며칠만** 본다.
        /// (한 번 틀렸다: 40일에 멈춘 도시가 주기 9로 잡혔다.)
        /// </summary>
        public int DetectCyclePeriod(int tailDays)
        {
            Dictionary<string, int> first = new Dictionary<string, int>();
            string prev = null;
            int from = tailDays <= 0 ? 0 : System.Math.Max(0, Chronicle.Count - tailDays);
            for (int i = from; i < Chronicle.Count; i++)
            {
                string key = string.Join("/", Chronicle[i].ActualRows);
                // 고정점은 주기가 아니다. 어제와 같은 모습이면 넘긴다 — 안 그러면 멈춘 도시가
                // 「주기 2」로 잡힌다(어제와 같고 그제와도 같으니까).
                if (key != prev)
                {
                    int before;
                    if (first.TryGetValue(key, out before) && i - before >= 2) return i - before;
                    if (!first.ContainsKey(key)) first[key] = i;
                }
                prev = key;
            }
            return 0;
        }
    }
}
