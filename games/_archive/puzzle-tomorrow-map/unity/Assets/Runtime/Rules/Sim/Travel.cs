// 순수 C#만. using UnityEngine 금지 — 이 파일이 Unity 를 끌어들이면 헤드리스 검사가 컴파일 오류로 터진다.
using System;
using System.Collections.Generic;
using FarmErosion.Data;

namespace FarmErosion.Sim
{
    /// <summary>주고받는 것의 종류. 문자열 ID로 평평하게 둔다(설계 원칙 3).</summary>
    public static class Res
    {
        public const string Coin = "coin";   // 셈
        public const string Soil = "soil";   // 흙 · 거름
        public const string Wall = "wall";   // 방벽 자재. 칸·날 단위
        public const string Seed = "seed";   // 씨앗. 한 번 심을 몫
        public const string Hand = "hand";   // 며칠간의 도움
        public const string Days = "days";   // 내가 내놓는 날. 받을 수는 없다
    }

    /// <summary>
    /// 한 곳의 현재. **그들도 같은 세계에 산다**(DIRECTION §2-1) —
    /// 내가 방문하든 안 하든, 도우든 안 돕든 자기 시계로 땅이 줄어든다.
    ///
    /// 되돌아가지 않는다. 내가 도우면 <see cref="NextLossDay"/> 가 뒤로 밀릴 뿐,
    /// 이미 잃은 칸은 나에게서와 똑같이 돌아오지 않는다.
    /// </summary>
    public sealed class RegionState
    {
        public string Id;
        public int StartPlots;
        public int FloorPlots;
        public int DecayDaysPerPlot;
        public int PlotsLeft;
        public int NextLossDay;
        public int ReprieveDaysGiven;   // 내가 준 유예의 합. 기록이지 되돌림이 아니다
        public int VisitCount;
        public int LastVisitDay = -1;
        public readonly HashSet<string> GiftsTaken = new HashSet<string>();   // once 인 것
        public readonly HashSet<string> ErrandsDone = new HashSet<string>();
    }

    /// <summary>한 번의 방문에서 일어난 일. 화면이 읽고, 검사기가 잰다.</summary>
    public sealed class VisitOutcome
    {
        public bool Ok;
        public string FailKo;
        public string RegionId;
        public int VisitIndex = -1;          // 이 곳을 몇 번째로 찾았는가(1부터)
        public int DaysSpent;
        public int DepartDay = -1, ArriveDay = -1, ReturnDay = -1;
        public int TheirPlotsOnArrival = -1;
        public int MyPlotsBefore = -1, MyPlotsAfter = -1;

        public string[] ReceivedKinds = new string[0];
        public int[] ReceivedAmounts = new int[0];

        public string ErrandId;              // null 이면 의뢰 없이 들렀다
        public string GaveKind;
        public int GaveAmount;
        public int GaveDays;
        public int ReprieveGiven;

        /// <summary>검사기용 환산값. 게임 규칙이 아니다 — 종류가 다른 것을 한 눈금으로 재려고 둔다.</summary>
        public int EstimatedCoinValue;
        /// <summary>그 차수의 말. 갈수록 짧아진다(DIRECTION §4-1).</summary>
        public string LineKo = "";
        /// <summary>돌아오기 전에 땅이 다 사라졌다.</summary>
        public bool EndedOnTheRoad;
    }

    /// <summary>
    /// 이동 · 방문 · 의뢰의 규칙. 화면을 모른다.
    ///
    /// **세 가지가 이 파일의 전부다**(DIRECTION §2-1 · §2-2 · §4-1):
    ///   1. 떠나 있는 동안 내 땅은 계속 깎인다 — 시뮬레이션을 날짜로 돌리므로 공짜로 얻어진다
    ///   2. 거기서 얻는 것이 **그 사람의 남은 땅에 비례한다** — 후반일수록 적다
    ///   3. 의뢰는 **이전**이다 — 내 것이 실제로 줄고, 돌려받는 것은 다른 종류다
    ///
    /// 얻는 것은 전부 **시간**이다. 칸은 어떤 경로로도 돌아오지 않는다.
    /// </summary>
    public sealed class Travel
    {
        readonly GameData _d;
        readonly TravelDataFile _t;
        readonly Simulation _sim;
        readonly List<RegionState> _regions = new List<RegionState>();
        readonly Dictionary<string, RegionState> _byId = new Dictionary<string, RegionState>();
        readonly List<VisitOutcome> _log = new List<VisitOutcome>();

        int _syncedThroughDay = -1;

        public IReadOnlyList<RegionState> Regions => _regions;
        public IReadOnlyList<VisitOutcome> Log => _log;
        public int VisitCount { get; private set; }
        public int DaysAway { get; private set; }
        public int WagonUpkeepPaid { get; private set; }

        public Travel(GameData d, Simulation sim)
        {
            _d = d;
            _sim = sim;
            _t = d.Travel;
            if (_t == null || _t.regions == null) return;
            foreach (var r in _t.regions)
            {
                var s = new RegionState
                {
                    Id = r.id,
                    StartPlots = r.startPlots,
                    FloorPlots = r.floorPlots,
                    DecayDaysPerPlot = Math.Max(1, r.decayDaysPerPlot),
                    PlotsLeft = r.startPlots,
                    NextLossDay = Math.Max(1, r.decayDaysPerPlot),
                };
                _regions.Add(s);
                _byId[r.id] = s;
            }
            Sync();
        }

        public RegionState Region(string id) => _byId.TryGetValue(id, out var r) ? r : null;
        public RegionDef Def(string id) => _d.Region(id);

        /// <summary>한 번 다녀오는 데 드는 날. 거리가 다르므로 곳마다 다르다.</summary>
        public int DaysFor(string regionId)
        {
            var def = _d.Region(regionId);
            if (def == null) return -1;
            return def.travelDaysOneWay * 2 + def.stayDays;
        }

        public int DaysFor(string regionId, string errandId)
        {
            int days = DaysFor(regionId);
            if (days < 0) return -1;
            var e = _d.Errand(errandId);
            return e == null ? days : days + Math.Max(0, e.giveDays);
        }

        /// <summary>
        /// **그들의 시계를 지금 날짜까지 돌린다.** 내가 집에만 있어도 저쪽은 줄어든다.
        /// 날짜를 누가 넘겼는지 몰라도 되도록, 바깥에서 부르는 모든 길목에서 먼저 부른다.
        /// 단조롭다 — 여러 번 불러도 결과가 같고, 잃은 칸이 돌아오지 않는다.
        /// </summary>
        public void Sync()
        {
            int day = _sim.CurrentDay;
            if (day <= _syncedThroughDay) return;
            _syncedThroughDay = day;
            foreach (var r in _regions)
                while (r.PlotsLeft > r.FloorPlots && day >= r.NextLossDay)
                {
                    r.PlotsLeft--;
                    r.NextLossDay += r.DecayDaysPerPlot;
                }
        }

        /// <summary>그 사람의 남은 땅으로 깎은 양. **줄 수 있는 것이 남은 땅에 비례한다.**</summary>
        public static int Scaled(int baseAmount, int minAmount, int plotsLeft, int startPlots)
        {
            if (startPlots <= 0) return minAmount;
            int v = baseAmount * plotsLeft / startPlots;      // 정수. 나머지는 버린다(설계 원칙 4)
            return v < minAmount ? minAmount : v;             // 고갈되어 사라지지 않는다 — 끝까지 조금씩 준다
        }

        /// <summary>지금 이 곳에서 받을 수 있는 의뢰. 한 번뿐인 것은 끝났으면 빠진다.</summary>
        public List<ErrandDef> ErrandsAt(string regionId)
        {
            Sync();
            var list = new List<ErrandDef>();
            var r = Region(regionId);
            if (r == null || _t.errands == null) return list;
            foreach (var e in _t.errands)
            {
                if (e.regionId != regionId) continue;
                if (e.once == 1 && r.ErrandsDone.Contains(e.id)) continue;
                list.Add(e);
            }
            return list;
        }

        /// <summary>내가 지금 내놓을 수 있는가. 낼 수 없는 의뢰는 받지 않는다.</summary>
        public bool CanPay(ErrandDef e)
        {
            if (e == null) return false;
            switch (e.giveKind)
            {
                case Res.Coin: return _sim.Money >= e.giveAmount;
                case Res.Seed: return _sim.SeedCredit >= e.giveAmount;
                case Res.Wall: return WallDaysHeld() >= e.giveAmount;
                case Res.Soil: return SoilAboveFloor() >= e.giveAmount;
                case Res.Days: return true;                   // 날은 늘 낼 수 있다. 내 땅이 그만큼 깎일 뿐이다
                default: return false;
            }
        }

        public VisitOutcome Visit(string regionId) => Visit(regionId, null);

        /// <summary>
        /// 다녀온다. 떠나는 날 · 머무는 날 · 돌아오는 날이 전부 실제 날짜로 지나간다.
        ///
        /// 순서가 뜻을 만든다: **싣고 가서, 주고, 받아서, 돌아와 푼다.**
        /// 받을 양은 **만난 날** 그들의 땅으로 정해지고, 물건은 **돌아온 날** 내 손에 들어온다.
        /// 그래서 멀수록 값이 크고 멀수록 늦다.
        /// </summary>
        public VisitOutcome Visit(string regionId, string errandId)
        {
            Sync();
            var o = new VisitOutcome { RegionId = regionId, ErrandId = errandId };
            var def = _d.Region(regionId);
            var st = Region(regionId);
            if (def == null || st == null) { o.FailKo = "그런 곳이 없다."; return o; }
            if (_sim.Ended) { o.FailKo = "남은 땅이 없다."; return o; }

            ErrandDef er = null;
            if (errandId != null)
            {
                er = _d.Errand(errandId);
                if (er == null || er.regionId != regionId) { o.FailKo = "그 의뢰는 여기 것이 아니다."; return o; }
                if (er.once == 1 && st.ErrandsDone.Contains(er.id)) { o.FailKo = "이미 한 일이다."; return o; }
                if (!CanPay(er)) { o.FailKo = "내놓을 것이 없다."; return o; }
            }

            o.DepartDay = _sim.CurrentDay;
            o.MyPlotsBefore = _sim.ActiveCount();

            // 1. 싣는다. **의뢰의 값은 떠나기 전에 치른다** — 수레에 실어 가는 것이다.
            if (er != null)
            {
                PayGive(er, o);
                st.ErrandsDone.Add(er.id);
            }

            // 2. 간다. 이 날들에 내 땅은 계속 깎인다 — 시뮬레이션이 날짜로 도니 공짜로 얻어진다.
            if (!Travelled(def.travelDaysOneWay, o)) return Finish(o, st, def, null, 0);

            Sync();
            o.ArriveDay = _sim.CurrentDay;
            o.TheirPlotsOnArrival = st.PlotsLeft;

            // 3. 내가 준 것이 그 사람의 시계를 늦춘다. **되돌리지는 않는다.**
            //
            // **총량 상한이 있다.** 예전에는 그냥 더했는데, 그러면 같은 사람에게 계속 찾아가는 것만으로
            // 그의 시계를 **멈출 수 있었다** — 한 판을 돌려 보니 22칸이 21칸이 되고 끝났다.
            // 그러면 도움이 '조금이라도'가 아니라 구원이 되고, "모두 같은 결말에 닿는다"가 깨진다
            // (DIRECTION §0 · §2-1).
            //
            // **거리로 거는 상한은 소용이 없었다.** '오늘로부터 며칠 앞까지'로 막았더니 왕복 11일마다
            // 다시 찾아가 그 앞을 매번 새로 세웠고, 결국 시계가 한 번도 울리지 않았다.
            // 막아야 하는 것은 한 번의 크기가 아니라 **한 사람에게 사 줄 수 있는 날의 총합**이다.
            if (er != null && er.reprieveDays > 0)
            {
                int budget = st.DecayDaysPerPlot * Math.Max(1, _t.maxReprieveTotalPlotPeriods);
                int given = Math.Max(0, Math.Min(er.reprieveDays, budget - st.ReprieveDaysGiven));
                st.NextLossDay += given;
                st.ReprieveDaysGiven += given;
                o.ReprieveGiven = given;
            }

            // 4. 머문다. 일손을 주기로 했다면 그 며칠도 여기서 간다.
            int stay = def.stayDays + (er != null ? Math.Max(0, er.giveDays) : 0);
            if (er != null) o.GaveDays = Math.Max(0, er.giveDays);
            int theirPlots = st.PlotsLeft;
            if (!Travelled(stay, o)) return Finish(o, st, def, er, theirPlots);

            // 5. 돌아온다.
            Travelled(def.travelDaysOneWay, o);

            return Finish(o, st, def, er, theirPlots);
        }

        // ── 조각 ────────────────────────────────────────────────────────────

        /// <summary>며칠을 보낸다. 도중에 땅이 다 사라지면 거기서 멈춘다.</summary>
        bool Travelled(int days, VisitOutcome o)
        {
            for (int i = 0; i < days; i++)
            {
                if (_sim.Ended) { o.EndedOnTheRoad = true; return false; }
                _sim.AdvanceDay();
                o.DaysSpent++;
                DaysAway++;
                int upkeep = _t.wagonUpkeepCoinPerDay;
                if (upkeep > 0) { _sim.PayUnavoidable(upkeep); WagonUpkeepPaid += upkeep; }
            }
            if (_sim.Ended) o.EndedOnTheRoad = true;
            return !_sim.Ended;
        }

        VisitOutcome Finish(VisitOutcome o, RegionState st, RegionDef def, ErrandDef er, int theirPlots)
        {
            o.ReturnDay = _sim.CurrentDay;
            o.MyPlotsAfter = _sim.ActiveCount();
            st.VisitCount++;
            st.LastVisitDay = o.ArriveDay;
            VisitCount++;
            o.VisitIndex = st.VisitCount;
            o.LineKo = LineFor(def, st.VisitCount);

            if (o.ArriveDay < 0)
            {
                // 가는 길에 끝났다. 받은 것은 없다.
                o.Ok = false;
                o.FailKo = "돌아올 땅이 없었다.";
                _log.Add(o);
                return o;
            }

            var kinds = new List<string>();
            var amounts = new List<int>();

            // 그냥 들러도 받는 것이 있다. **그들도 없지만 그래도 뭔가를 준다**(DIRECTION §2-1).
            if (def.gifts != null)
                foreach (var g in def.gifts)
                {
                    string key = def.id + "/" + g.kind + "/" + g.baseAmount;
                    if (g.once == 1 && st.GiftsTaken.Contains(key)) continue;
                    if (g.onlyWhenTheirPlotsAtMost != -1 && theirPlots > g.onlyWhenTheirPlotsAtMost) continue;
                    int amount = g.scaled == 1
                        ? Scaled(g.baseAmount, g.minAmount, theirPlots, st.StartPlots)
                        : g.baseAmount;
                    if (amount <= 0) continue;
                    if (g.once == 1) st.GiftsTaken.Add(key);
                    Receive(g.kind, amount);
                    kinds.Add(g.kind);
                    amounts.Add(amount);
                }

            // 의뢰의 몫. **내가 준 것과 다른 종류**이고, 이것도 남은 땅에 비례한다.
            if (er != null)
            {
                int amount = Scaled(er.takeBaseAmount, er.takeMinAmount, theirPlots, st.StartPlots);
                if (amount > 0)
                {
                    Receive(er.takeKind, amount);
                    kinds.Add(er.takeKind);
                    amounts.Add(amount);
                }
            }

            o.ReceivedKinds = kinds.ToArray();
            o.ReceivedAmounts = amounts.ToArray();
            o.EstimatedCoinValue = ValueOf(kinds, amounts);
            o.Ok = true;
            _log.Add(o);
            return o;
        }

        static string LineFor(RegionDef def, int visitIndex)
        {
            if (def.linesKo == null || def.linesKo.Length == 0) return "";
            int i = visitIndex - 1;
            if (i < 0) i = 0;
            if (i >= def.linesKo.Length) i = def.linesKo.Length - 1;   // 마지막 말이 계속된다. 더 줄지는 않는다
            return def.linesKo[i];
        }

        void Receive(string kind, int amount)
        {
            switch (kind)
            {
                case Res.Coin: _sim.ReceiveCoin(amount); break;
                case Res.Seed: _sim.ReceiveSeed(amount); break;
                case Res.Hand: _sim.ReceiveHelp(amount); break;
                case Res.Soil: SpreadSoil(amount); break;
                case Res.Wall: SpreadWall(amount); break;
            }
        }

        void PayGive(ErrandDef e, VisitOutcome o)
        {
            o.GaveKind = e.giveKind;
            o.GaveAmount = e.giveAmount;
            switch (e.giveKind)
            {
                case Res.Coin: _sim.SpendCoin(e.giveAmount); break;
                case Res.Seed: _sim.TakeSeed(e.giveAmount); break;
                case Res.Wall: TakeWall(e.giveAmount); break;
                case Res.Soil: TakeSoil(e.giveAmount); break;
                case Res.Days: break;                          // 값은 날로 낸다. Visit 이 그만큼 더 머문다
            }
        }

        // ── 흙과 방벽은 밭 위에 있다. 나누고 걷는 순서를 정해 두어야 결정적이다 ──

        /// <summary>남은 칸에 고루 뿌린다. 토질은 100 을 넘지 않는다 — **칸이 늘지는 않는다.**</summary>
        void SpreadSoil(int points)
        {
            var open = ActiveSortedById();
            if (open.Count == 0) return;
            int left = points;
            int step = Math.Max(1, points / open.Count);
            for (int pass = 0; pass < 4 && left > 0; pass++)
                foreach (var p in open)
                {
                    if (left <= 0) break;
                    if (p.Soil >= 100) continue;
                    int add = Math.Min(Math.Min(left, step), 100 - p.Soil);
                    p.Soil += add;
                    left -= add;
                }
        }

        /// <summary>받아 온 널을 가장 헐벗은 칸부터 덮는다. 한 칸의 방벽은 규정 일수를 넘지 않는다.</summary>
        void SpreadWall(int plotDays)
        {
            var open = ActiveSortedById();
            if (open.Count == 0) return;
            int cap = _d.Plots.defense.durationDays;
            int left = plotDays;
            for (int pass = 0; pass < 4 && left > 0; pass++)
            {
                open.Sort((a, b) =>
                {
                    int c = a.DefenseDaysLeft.CompareTo(b.DefenseDaysLeft);
                    if (c != 0) return c;
                    return string.CompareOrdinal(a.Id, b.Id);
                });
                bool moved = false;
                foreach (var p in open)
                {
                    if (left <= 0) break;
                    int room = cap - p.DefenseDaysLeft;
                    if (room <= 0) continue;
                    int add = Math.Min(left, room);
                    p.DefenseDaysLeft += add;
                    left -= add;
                    moved = true;
                }
                if (!moved) break;
            }
        }

        /// <summary>목재를 주면 **내 방벽이 줄어든다.** 가장 두터운 칸부터 뜯는다.</summary>
        void TakeWall(int plotDays)
        {
            var open = ActiveSortedById();
            open.Sort((a, b) =>
            {
                int c = b.DefenseDaysLeft.CompareTo(a.DefenseDaysLeft);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });
            int left = plotDays;
            foreach (var p in open)
            {
                if (left <= 0) break;
                int take = Math.Min(left, p.DefenseDaysLeft);
                p.DefenseDaysLeft -= take;
                left -= take;
            }
        }

        /// <summary>흙을 떠 주면 **내 토질이 줄어든다.** 심을 수 있는 바닥 아래로는 내려가지 않는다.</summary>
        void TakeSoil(int points)
        {
            var open = ActiveSortedById();
            int floor = _d.Plots.soilFloorForPlanting;
            open.Sort((a, b) =>
            {
                int c = b.Soil.CompareTo(a.Soil);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });
            int left = points;
            foreach (var p in open)
            {
                if (left <= 0) break;
                int take = Math.Min(left, Math.Max(0, p.Soil - floor));
                p.Soil -= take;
                left -= take;
            }
        }

        public int WallDaysHeld()
        {
            int n = 0;
            foreach (var p in _sim.Plots) if (p.Active) n += p.DefenseDaysLeft;
            return n;
        }

        public int SoilAboveFloor()
        {
            int n = 0, floor = _d.Plots.soilFloorForPlanting;
            foreach (var p in _sim.Plots) if (p.Active && p.Soil > floor) n += p.Soil - floor;
            return n;
        }

        List<PlotState> ActiveSortedById()
        {
            var list = new List<PlotState>();
            foreach (var p in _sim.Plots) if (p.Active) list.Add(p);
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return list;
        }

        /// <summary>검사기용 환산. 종류가 다른 것을 한 눈금으로 재려는 것일 뿐 게임 규칙이 아니다.</summary>
        public int ValueOf(IList<string> kinds, IList<int> amounts)
        {
            var L = _t.limits;
            int v = 0;
            for (int i = 0; i < kinds.Count; i++)
            {
                switch (kinds[i])
                {
                    case Res.Coin: v += amounts[i]; break;
                    case Res.Soil: v += amounts[i] * L.coinPerSoil; break;
                    case Res.Wall: v += amounts[i] * L.coinPerWallDay; break;
                    case Res.Seed: v += amounts[i] * L.coinPerSeed; break;
                    case Res.Hand: v += amounts[i] * L.coinPerHandDay; break;
                }
            }
            return v;
        }
    }
}
