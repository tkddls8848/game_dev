using System;
using System.Collections.Generic;
using FarmErosion.Data;

namespace FarmErosion.Sim
{
    public sealed class PlotState
    {
        public string Id;
        public int X, Y;
        public int Soil;
        public int Erosion;
        public bool Active = true;
        public string CropId;          // null이면 빈 칸
        public int GrowthPoints;
        public int GrowthRemainderWeather;
        public int GrowthRemainderSoil;
        public int DefenseDaysLeft;
        /// <summary>사람이 켠 '우선 급수'. 물이 모자랄 때 이 칸부터 적신다.</summary>
        public bool WaterFirst;
        public bool WateredToday;
        public int LostOnDay = -1;

        public bool HasCrop => CropId != null;
    }

    /// 하루 한 걸음. 모든 판단이 정수와 씨드 고정 난수만 쓰므로 같은 씨드는 같은 결과가 된다.
    public sealed class Simulation
    {
        readonly GameData _d;
        readonly DifficultyLevel _lv;
        readonly PolicyWeights _w;
        readonly WeatherCalendar _weather;
        readonly PriceBook _prices;
        readonly int _years;

        readonly List<PlotState> _plots = new List<PlotState>();
        readonly Dictionary<string, PlotState> _byId = new Dictionary<string, PlotState>();
        readonly HashSet<string> _firedOnce = new HashSet<string>();

        int _money;
        int _lostCount;
        int _seedCredit;
        int _helpDaysLeft;
        bool _bankrupt;
        readonly SimResult _r = new SimResult();

        public IReadOnlyList<PlotState> Plots => _plots;
        public int Money => _money;

        // Interactive front ends share the same growth, erosion and economy rules.
        public int CurrentDay { get; private set; }
        /// <summary>
        /// **끝은 하나다 — 땅이 다 사라지는 것.**
        ///
        /// 파산은 끝이 아니다. 파산하면 씨앗도 방벽도 살 수 없어 **아무것도 못 하는 채로
        /// 침식을 보게 되고**, 그 길이 자연스럽게 같은 끝으로 이어진다.
        /// 여기서 끊어 버리면 그 무력감이 생기지 않는다 — 실패를 통보받고 화면이 닫힐 뿐이다.
        ///
        /// 연차 상한은 끝이 아니라 안전장치다. 땅은 어차피 0 으로 간다.
        /// </summary>
        public bool Ended => ActiveCount() == 0 || CurrentDay >= _years * _d.DaysPerYear;

        /// <summary>파산 상태. 끝이 아니라 **아무것도 할 수 없는 상태**다.</summary>
        public bool Bankrupt => _bankrupt;
        public int Income => _r.TotalIncomeCoin;
        public WeatherDef CurrentWeather => _weather.At(Math.Min(CurrentDay, _years * _d.DaysPerYear - 1));

        // ── 바깥에서 들어오고 나가는 것 ──────────────────────────────────
        //
        // **이동(Travel)이 쓰는 유일한 통로다.** 방문해서 얻은 것이 여기로 들어오고,
        // 의뢰로 내놓는 것이 여기로 나간다. 들어오는 것은 전부 **시간**이지 **칸**이 아니다 —
        // 되찾기는 없다(plots.json reclaimCost -1, DIRECTION §2-1).

        /// <summary>남이 준 셈. 이 게임에서 밭 바깥의 돈이 들어오는 유일한 자리다.</summary>
        public void ReceiveCoin(int coin)
        {
            if (coin <= 0) return;
            _money += coin;
            _r.TotalIncomeCoin += coin;
            _r.GiftCoinReceived += coin;
        }

        /// <summary>내가 내놓는 셈. 의뢰는 보상이 아니라 이전이므로 실제로 줄어야 한다.</summary>
        public bool SpendCoin(int coin)
        {
            if (coin <= 0 || _money < coin) return false;
            _money -= coin;
            _r.TotalExpenseCoin += coin;
            _r.GiftCoinGiven += coin;
            return true;
        }

        /// <summary>
        /// 피할 수 없는 값. **셈이 모자라도 나간다** — 살림비와 같은 성질이다.
        /// 낼 수 있을 때만 나가면 셈이 마른 채로 다니는 것이 공짜가 되고, 거기서 수도꼭지가 열린다.
        /// </summary>
        public void PayUnavoidable(int coin)
        {
            if (coin <= 0 || _bankrupt) return;
            _money -= coin;
            _r.TotalExpenseCoin += coin;
            _r.UpkeepPaidCoin += coin;
        }

        /// <summary>남이 준 씨앗. 심을 때 셈보다 **이것을 먼저 쓴다** — 받은 것이 실제로 들어야 한다.</summary>
        public int SeedCredit => _seedCredit;
        public void ReceiveSeed(int n) { if (n > 0) _seedCredit += n; }
        public bool TakeSeed(int n)
        {
            if (n <= 0 || _seedCredit < n) return false;
            _seedCredit -= n;
            return true;
        }

        /// <summary>며칠간의 도움. 그동안 내 칸의 하루 침식이 줄어든다. **칸은 돌아오지 않는다.**</summary>
        public int HelpDaysLeft => _helpDaysLeft;
        public void ReceiveHelp(int days) { if (days > 0) _helpDaysLeft += days; }

        public bool TryPlant(string plotId, string cropId)
        {
            if (Ended || !_byId.TryGetValue(plotId, out var p)) return false;
            var c = _d.Crop(cropId);
            if (c == null || !p.Active || p.HasCrop || p.Soil < Math.Max(c.minSoil, _d.Plots.soilFloorForPlanting)
                || !_d.CropFitsSeason(c, _d.SeasonOfDay(CurrentDay).id)) return false;
            int cost = _d.Shop(cropId).buySeed;
            if (_seedCredit > 0) { _seedCredit--; cost = 0; }   // 남이 준 씨앗을 먼저 쓴다
            else if (_money < cost) return false;
            _money -= cost;
            _r.TotalExpenseCoin += cost;
            p.CropId = cropId;
            p.GrowthPoints = p.GrowthRemainderWeather = p.GrowthRemainderSoil = 0;
            Bump(_r.PlantCounts, cropId);
            return true;
        }
        /// <summary>
        /// 이 칸에 방벽을 세우는 값. **노출된 변 수에 비례한다.**
        ///
        /// 값이 고정이면 계산이 하나뿐이라 선택이 사라진다. 변경선(노출이 큰 칸)은 비싸고
        /// 안쪽은 싸다 — 그런데 안쪽은 어차피 덜 깎이므로 지킬 이유가 없다.
        /// 그래서 판단이 "비싸게 변경선을 지킬까, 물러나서 좋은 땅을 새 변경선으로 내줄까"가 된다.
        /// </summary>
        public int DefenseCost(PlotState p)
            => IntMath.Percent(_d.Plots.defense.cost + _d.Plots.defense.costPerOpenSide * OpenSides(p),
                               _lv.defenseCostPercent);

        /// <summary>방벽이 버티는 날. 난이도가 수명을 바꾼다.</summary>
        public int DefenseDurationDays
            => Math.Max(1, IntMath.Percent(_d.Plots.defense.durationDays, _lv.defenseDurationPercent));

        /// <summary>
        /// **미리 접는다.** 무너지기를 기다리지 않고 지금 내주고 자재를 회수한다.
        ///
        /// 그냥 두고 무너지면 0 이다. 그래서 "버틸까 지금 뜯을까"가 실제 선택이 된다.
        /// 흙의 일부는 이웃 칸 토질로 간다 — 물러나는 것이 손실만은 아니게 만든다.
        /// 되찾기는 여전히 없다(reclaimCost -1). **접는 것은 되돌릴 수 없다.**
        /// </summary>
        public bool TryAbandon(string plotId)
        {
            if (Ended || _d.Plots.salvage == null || _d.Plots.salvage.enabled == 0) return false;
            if (!_byId.TryGetValue(plotId, out var p) || !p.Active) return false;
            if (ActiveCount() <= 1) return false;          // 마지막 한 칸은 접을 수 없다 — 끝은 침식이어야 한다

            var s = _d.Plots.salvage;
            int coin = s.coinPerPlot + s.coinPerDefenseDay * Math.Max(0, p.DefenseDaysLeft);
            _money += coin;
            _r.TotalIncomeCoin += coin;

            int share = p.Soil * s.soilShareToNeighbors / 100;
            var kin = new List<PlotState>();
            foreach (var n in Neighbors(p)) if (n.Active) kin.Add(n);
            if (kin.Count > 0)
            {
                int each = share / kin.Count;              // 정수. 나머지는 버린다(설계 원칙 4)
                foreach (var n in kin) n.Soil += each;
            }

            // 손실 처리는 ResolveLosses 와 같아야 한다 — 접은 칸도 사라진 칸이다.
            p.Active = false;
            p.CropId = null;
            p.LostOnDay = CurrentDay + 1;
            p.DefenseDaysLeft = 0;
            _lostCount++;
            if (_r.FirstLossYear < 0) _r.FirstLossYear = _d.YearOfDay(CurrentDay);
            if (_r.HalfLandYear < 0 && _lostCount * 2 >= _d.Plots.plots.Length) _r.HalfLandYear = _d.YearOfDay(CurrentDay);
            if (_r.AllLostYear < 0 && _lostCount >= _d.Plots.plots.Length) _r.AllLostYear = _d.YearOfDay(CurrentDay);
            // **이웃에 전파한다.** 무너져서 사라지든 접어서 사라지든 옆은 똑같이 변경선이 된다.
            foreach (var n in Neighbors(p)) if (n.Active) n.Erosion += _d.Plots.neighborLossErosion;
            _r.PlotsAbandoned++;
            return true;
        }

        public bool TryDefend(string plotId)
        {
            if (Ended || !_byId.TryGetValue(plotId, out var p) || !p.Active || p.DefenseDaysLeft > 0) return false;
            int cost = DefenseCost(p);
            if (_money < cost) return false;
            _money -= cost;
            _r.TotalExpenseCoin += cost;
            _r.DefensesBuilt++;
            p.DefenseDaysLeft = DefenseDurationDays;
            return true;
        }
        public bool AdvanceDay()
        {
            if (Ended) return false;
            int day = CurrentDay, year = _d.YearOfDay(day), income = 0, expense = 0, lost = 0;
            var season = _d.SeasonOfDay(day);
            if (day > 0 && _d.DayInSeason(day) == 1) ApplySeasonTransition(season);
            Harvest(day, season, ref income);
            ResolveLosses(day, year, ref lost);
            AllocateWater(CurrentWeather);
            Grow(CurrentWeather);
            Erode(season, year, CurrentWeather);
            FireEvents(year, season, _d.DayInSeason(day));
            if (_d.DayInSeason(day) == _d.Seasons.daysPerSeason) PayLivingCost(season, ref expense);
            if ((day + 1) % _d.DaysPerYear == 0 && !_bankrupt)
                _money -= ActiveCount() * _d.Economy.taxPerActivePlotPerYear;
            CheckBankruptcy(year, day);
            CurrentDay++;
            return true;
        }

        public Simulation(GameData d, PolicyKind kind, int years)
            : this(d, kind, years, d.Difficulty != null ? d.Difficulty.defaultId : null) { }

        /// <summary>
        /// **난이도는 이기느냐를 바꾸지 않는다.** 어느 쪽을 골라도 땅은 0 이 되고 끝의 모양도 같다.
        /// 바뀌는 것은 시작 자원 · 산출 · 방벽 값과 수명 · 파산 임계, 그리고 그 여지를 준 만큼
        /// 되돌려 받는 하루 침식뿐이다(DIRECTION §2-6). 연차 상한은 건드리지 않는다.
        /// </summary>
        public Simulation(GameData d, PolicyKind kind, int years, string difficultyId)
        {
            _d = d;
            _lv = d.Level(difficultyId);
            _w = PolicyWeights.For(kind);
            _years = years;
            _weather = new WeatherCalendar(d, years);
            _prices = new PriceBook(d, years);

            foreach (var p in d.Plots.plots)
            {
                var s = new PlotState { Id = p.id, X = p.x, Y = p.y, Soil = p.soil, Erosion = p.startErosion };
                _plots.Add(s);
                _byId[p.id] = s;
            }
            _money = IntMath.Percent(d.Economy.startMoney, _lv.startCoinPercent);
        }

        /// <summary>고른 난이도. 화면과 검사기가 이름을 읽는다.</summary>
        public DifficultyLevel Level => _lv;

        public static SimResult Run(GameData d, PolicyKind kind, int years)
        {
            var sim = new Simulation(d, kind, years);
            return sim.Execute();
        }

        public SimResult Execute()
        {
            int daysPerYear = _d.DaysPerYear;
            int totalDays = _years * daysPerYear;
            _r.Years = _years;
            _r.DaysSimulated = totalDays;
            _r.PlayableDays = totalDays;
            _r.PeakAssetCoin = Asset();
            _r.PeakAssetDay = 0;

            int yearIncome = 0, yearExpense = 0, yearLost = 0;

            for (int day = 0; day < totalDays; day++)
            {
                var season = _d.SeasonOfDay(day);
                int year = _d.YearOfDay(day);
                int dayInSeason = _d.DayInSeason(day);
                var weather = _weather.At(day);
                Bump(_r.WeatherCounts, weather.id);

                if (dayInSeason == 1 && day > 0) ApplySeasonTransition(season);

                Harvest(day, season, ref yearIncome);
                ResolveLosses(day, year, ref yearLost);
                if (!_bankrupt && _w.AllowDefense) BuildDefenses(ref yearExpense, season, year, weather);
                if (!_bankrupt) Plant(day, season, year, weather, ref yearExpense);
                AllocateWater(weather);
                Grow(weather);
                Erode(season, year, weather);
                FireEvents(year, season, dayInSeason);
                // 살림비는 계절 끝에 셈한다. 땅이 줄어도 살림은 줄지 않는다 - 그것이 이 훅의 압력이다.
                if (dayInSeason == _d.Seasons.daysPerSeason) PayLivingCost(season, ref yearExpense);
                CheckBankruptcy(year, day);

                int asset = Asset();
                if (asset > _r.PeakAssetCoin) { _r.PeakAssetCoin = asset; _r.PeakAssetDay = day + 1; }

                if (dayInSeason == _d.Seasons.daysPerSeason && season.order == _d.Seasons.seasons.Length - 1)
                {
                    int tax = _bankrupt ? 0 : ActiveCount() * _d.Economy.taxPerActivePlotPerYear;
                    if (tax > 0) { _money -= tax; yearExpense += tax; _r.TotalExpenseCoin += tax; _r.UpkeepPaidCoin += tax; }
                    CheckBankruptcy(year, day);
                    _r.YearRecords.Add(new YearRecord
                    {
                        Year = year,
                        IncomeCoin = yearIncome,
                        ExpenseCoin = yearExpense,
                        MoneyAtYearEnd = _money,
                        ActivePlotsAtYearEnd = ActiveCount(),
                        PlotsLostThisYear = yearLost,
                        AssetAtYearEnd = asset,
                        CapacityAtYearEnd = Capacity(),
                    });
                    if (yearIncome > _r.PeakIncomeCoin) { _r.PeakIncomeCoin = yearIncome; _r.PeakIncomeYear = year; }
                    yearIncome = 0; yearExpense = 0; yearLost = 0;
                }
            }

            _r.FinalMoneyCoin = _money;
            return _r;
        }

        // ── 한 걸음의 조각들 ────────────────────────────────────────────────

        void ApplySeasonTransition(SeasonDef season)
        {
            foreach (var p in _plots)
            {
                if (!p.Active) continue;
                p.Erosion += season.transitionErosion;
                p.Soil = IntMath.Clamp(p.Soil - season.transitionSoilLoss, 0, 100);
            }
        }

        void PayLivingCost(SeasonDef season, ref int yearExpense)
        {
            if (_bankrupt) return;   // 파산 뒤에는 농장을 놓았으므로 더 걷지 않는다
            int cost = _d.Economy.livingCostPerSeason;
            if (cost <= 0) return;
            _money -= cost;
            yearExpense += cost;
            _r.TotalExpenseCoin += cost;
            _r.UpkeepPaidCoin += cost;
            Bump(_r.SeasonExpense, season.id, cost);
        }

        /// 파산은 패배 조건이 아니다. 살림이 먼저 무너지면 씨앗도 방벽도 못 사고,
        /// **손을 놓은 채로 땅이 깎이는 것을 보게 된다.** 끝은 여전히 침식이다.
        void CheckBankruptcy(int year, int day)
        {
            if (_bankrupt || _money >= _lv.bankruptcyAtCoin) return;   // 난이도가 정하는 임계(§2-6)
            _bankrupt = true;
            _r.BankruptYear = year;
            _r.BankruptDay = day + 1;
            if (_r.PlayableDays > day + 1) _r.PlayableDays = day + 1;
            foreach (var p in _plots) p.CropId = null;   // 씨앗값을 못 내면 밭을 놓는다
        }

        void Harvest(int day, SeasonDef season, ref int yearIncome)
        {
            foreach (var p in _plots)
            {
                if (!p.Active || !p.HasCrop) continue;
                var crop = _d.Crop(p.CropId);
                if (p.GrowthPoints < crop.growDays * 100) continue;

                int unit = _prices.SellUnit(crop.id, day);
                int gain = IntMath.Percent(crop.yieldUnits * unit, _lv.yieldPercent);
                _money += gain;
                yearIncome += gain;
                _r.TotalIncomeCoin += gain;
                Bump(_r.SeasonIncome, season.id, gain);
                Bump(_r.HarvestCounts, crop.id);

                p.Soil = IntMath.Clamp(p.Soil - crop.soilDrain, 0, 100);
                p.CropId = null;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
        }

        void ResolveLosses(int day, int year, ref int yearLost)
        {
            // 한 날에 여러 칸이 동시에 무너질 수 있다. 전파는 그 날 한 번만 적용한다.
            var lostToday = new List<PlotState>();
            foreach (var p in _plots)
            {
                if (!p.Active) continue;
                if (p.Erosion < _d.Plots.lossThresholdErosion) continue;
                p.Active = false;
                p.CropId = null;
                p.LostOnDay = day + 1;
                lostToday.Add(p);
            }
            if (lostToday.Count == 0) return;

            foreach (var p in lostToday)
            {
                _lostCount++;
                yearLost++;
                if (_r.FirstLossYear < 0) _r.FirstLossYear = year;
                if (_r.HalfLandYear < 0 && _lostCount * 2 >= _d.Plots.plots.Length) _r.HalfLandYear = year;
                if (_r.AllLostYear < 0 && _lostCount >= _d.Plots.plots.Length)
                {
                    _r.AllLostYear = year;
                    _r.PlayableDays = day + 1;
                }
            }

            foreach (var p in lostToday)
                foreach (var n in Neighbors(p))
                    if (n.Active) n.Erosion += _d.Plots.neighborLossErosion;
        }

        void BuildDefenses(ref int yearExpense, SeasonDef season, int year, WeatherDef weather)
        {
            var def = _d.Plots.defense;
            var candidates = new List<PlotState>();
            foreach (var p in _plots)
            {
                if (!p.Active || p.DefenseDaysLeft > 0) continue;
                if (p.Soil < _w.DefendSoilMin) continue;
                if (p.Erosion < _w.DefendErosionMin) continue;
                candidates.Add(p);
            }
            // 토질이 높은 칸부터. 동점은 id로 갈라 결정적으로 만든다.
            candidates.Sort((a, b) =>
            {
                int c = b.Soil.CompareTo(a.Soil);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });

            foreach (var p in candidates)
            {
                if (_money - def.cost < _w.DefendMoneyReserve) break;
                _money -= def.cost;
                yearExpense += def.cost;
                _r.TotalExpenseCoin += def.cost;
                _r.DefensesBuilt++;
                Bump(_r.SeasonExpense, season.id, def.cost);
                p.DefenseDaysLeft = def.durationDays;
            }
        }

        void Plant(int day, SeasonDef season, int year, WeatherDef weather, ref int yearExpense)
        {
            var empty = new List<PlotState>();
            foreach (var p in _plots) if (p.Active && !p.HasCrop) empty.Add(p);
            empty.Sort((a, b) =>
            {
                int c = b.Soil.CompareTo(a.Soil);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });

            foreach (var p in empty)
            {
                var pick = Choose(p, season, year, weather);
                if (pick == null) continue;
                var shop = _d.Shop(pick.id);
                int seedCost = shop.buySeed;
                if (_seedCredit > 0) { _seedCredit--; seedCost = 0; }   // 남이 준 씨앗을 먼저 쓴다
                else if (_money < seedCost) continue;

                _money -= seedCost;
                yearExpense += seedCost;
                _r.TotalExpenseCoin += seedCost;
                Bump(_r.SeasonExpense, season.id, seedCost);
                Bump(_r.PlantCounts, pick.id);

                p.CropId = pick.id;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
        }

        /// 이 칸에 무엇을 심을지. 돈 · 토질 · 침식 방어를 정수 점수 하나로 합친다.
        public CropDef Choose(PlotState p, SeasonDef season, int year, WeatherDef weather)
        {
            int life = EstimatedDaysLeft(p, season, year, 0);
            CropDef best = null;
            int bestScore = int.MinValue;

            foreach (var crop in _d.Crops.crops)
            {
                if (!_d.CropFitsSeason(crop, season.id)) continue;
                if (p.Soil < crop.minSoil) continue;
                if (p.Soil < _d.Plots.soilFloorForPlanting) continue;
                bool isClover = crop.yieldUnits == 0;
                if (isClover && !_w.AllowClover) continue;
                if (!isClover && _w.ForceClover) continue;

                // 덮개가 있으면 그 칸은 더 오래 버틴다 — 수명을 작물별로 다시 잰다
                int lifeWithCrop = EstimatedDaysLeft(p, season, year, crop.erosionGuard);
                if (lifeWithCrop < crop.growDays) continue;

                var shop = _d.Shop(crop.id);
                int gross = crop.yieldUnits * shop.sellUnit;
                int net = gross - shop.buySeed;
                int perDay = net * 100 / crop.growDays;                       // 하루 수익(1/100 코인)
                int soilCost = crop.soilDrain * _w.SoilCoinPerPoint * 100 / crop.growDays;
                int guardGain = crop.erosionGuard * _w.GuardCoinPerPoint * 100 / 100;
                int score = perDay - soilCost + guardGain;

                // 토질이 바닥이면 회복이 먼저다 — 회복작물에 가산점을 준다
                if (crop.soilDrain < 0 && p.Soil <= _w.CloverSoilTrigger) score += 600;

                if (score > bestScore || (score == bestScore && best != null &&
                                          string.CompareOrdinal(crop.id, best.id) < 0))
                {
                    bestScore = score;
                    best = crop;
                }
            }
            return best;
        }

        /// 지금 추세로 이 칸이 몇 날 더 버티는가. 날씨는 평균을 쓰지 않고 계절 기본값 + 1로 잡는다.
        public int EstimatedDaysLeft(PlotState p, SeasonDef season, int year, int guard)
        {
            int perDay = season.erosionPerDay
                       + (year - 1) * _d.Plots.erosionPressurePerYear
                       + 3   // 평균 날씨 침식. 계절마다 다르지만 정책 추정에는 한 값으로 충분하다
                       + _d.Plots.edgeErosionBonus * OpenSides(p)
                       + (guard > 0 ? 0 : _d.Plots.bareErosionBonus)
                       - guard
                       - (p.DefenseDaysLeft > 0 ? _d.Plots.defense.erosionReduction : 0);
            if (perDay <= 0) return int.MaxValue / 4;
            int left = _d.Plots.lossThresholdErosion - p.Erosion;
            if (left <= 0) return 0;
            return left / perDay;
        }

        void AllocateWater(WeatherDef weather)
        {
            int available = Math.Max(0, _d.Economy.wellWaterPerDay + weather.waterDelta);
            var growing = new List<PlotState>();
            foreach (var p in _plots)
            {
                p.WateredToday = false;
                if (p.Active && p.HasCrop) growing.Add(p);
            }
            // **사람이 켠 칸을 먼저 적신다.** 물은 모자라서 24칸을 다 못 굴린다 —
            // 어느 칸이 굴러갈지를 사람이 정해야 그 부족함이 선택이 된다.
            // 켜지 않았으면 예전처럼 작물 값 순이다. 동점은 id로 갈라 결정적으로 만든다.
            bool manual = _d.Plots.waterPriority != null && _d.Plots.waterPriority.enabled != 0;
            growing.Sort((a, b) =>
            {
                if (manual && a.WaterFirst != b.WaterFirst) return a.WaterFirst ? -1 : 1;
                int va = WaterPriority(a), vb = WaterPriority(b);
                int c = vb.CompareTo(va);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });
            foreach (var p in growing)
            {
                var crop = _d.Crop(p.CropId);
                if (available < crop.waterPerDay) continue;
                available -= crop.waterPerDay;
                p.WateredToday = true;
            }
        }

        int WaterPriority(PlotState p)
        {
            var crop = _d.Crop(p.CropId);
            var shop = _d.Shop(crop.id);
            int gross = crop.yieldUnits * shop.sellUnit;
            return (gross + crop.erosionGuard * _w.GuardCoinPerPoint) * 100 / Math.Max(1, crop.waterPerDay);
        }

        void Grow(WeatherDef weather)
        {
            foreach (var p in _plots)
            {
                if (!p.Active || !p.HasCrop) continue;
                int basePoints = p.WateredToday ? 100 : 40;
                int afterWeather = IntMath.MulPercent(basePoints, weather.growthPercent, ref p.GrowthRemainderWeather);
                int soilFactor = 50 + p.Soil / 2;   // 토질 0 → 50%, 100 → 100%
                int add = IntMath.MulPercent(afterWeather, soilFactor, ref p.GrowthRemainderSoil);
                p.GrowthPoints += add;
            }
        }

        void Erode(SeasonDef season, int year, WeatherDef weather)
        {
            // 남이 와서 거들어 주는 날. 며칠뿐이고, 그 며칠이 지나면 그대로다.
            int help = _helpDaysLeft > 0 && _d.Travel != null ? _d.Travel.helpErosionReduction : 0;
            foreach (var p in _plots)
            {
                if (!p.Active) continue;
                var crop = p.HasCrop ? _d.Crop(p.CropId) : null;
                int e = season.erosionPerDay
                      + _lv.erosionPerDayBonus      // 여지를 준 쪽은 여기서 되돌려 받는다(§2-6 함정)
                      + (year - 1) * _d.Plots.erosionPressurePerYear
                      + weather.erosionDelta
                      + _d.Plots.edgeErosionBonus * OpenSides(p)
                      + (crop == null ? _d.Plots.bareErosionBonus : 0)
                      - (crop == null ? 0 : crop.erosionGuard)
                      - (p.DefenseDaysLeft > 0 ? _d.Plots.defense.erosionReduction : 0)
                      - help;
                if (e < 0) e = 0;                      // 침식은 되돌지 않는다. 막는 것이 최선이다
                p.Erosion += e;
                if (p.DefenseDaysLeft > 0) p.DefenseDaysLeft--;
            }
            if (_helpDaysLeft > 0) _helpDaysLeft--;    // 하루에 한 번. 칸마다가 아니다
        }

        void FireEvents(int year, SeasonDef season, int dayInSeason)
        {
            foreach (var e in _d.Events.events)
            {
                if (e.onYear != -1 && e.onYear != year) continue;
                if (!string.IsNullOrEmpty(e.onSeason) && e.onSeason != season.id) continue;
                if (e.onDay != -1 && e.onDay != dayInSeason) continue;
                if (e.minPlotsLost != -1 && _lostCount < e.minPlotsLost) continue;
                if (e.minMoney != -1 && _money < e.minMoney) continue;
                if (e.once == 1 && _firedOnce.Contains(e.id)) continue;
                if (e.once == 1) _firedOnce.Add(e.id);
                Bump(_r.EventFireCounts, e.id);
            }
        }

        // ── 조회 ────────────────────────────────────────────────────────────

        /// 격자 밖이거나 이미 잃은 이웃의 수. 전선이 넓은 칸이 빨리 깎인다.
        public int OpenSides(PlotState p)
        {
            int open = 0;
            if (!IsHeld(p.X - 1, p.Y)) open++;
            if (!IsHeld(p.X + 1, p.Y)) open++;
            if (!IsHeld(p.X, p.Y - 1)) open++;
            if (!IsHeld(p.X, p.Y + 1)) open++;
            return open;
        }

        bool IsHeld(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _d.Plots.gridWidth || y >= _d.Plots.gridHeight) return false;
            var p = At(x, y);
            return p != null && p.Active;
        }

        public PlotState At(int x, int y)
        {
            foreach (var p in _plots) if (p.X == x && p.Y == y) return p;
            return null;
        }

        IEnumerable<PlotState> Neighbors(PlotState p)
        {
            var a = At(p.X - 1, p.Y); if (a != null) yield return a;
            var b = At(p.X + 1, p.Y); if (b != null) yield return b;
            var c = At(p.X, p.Y - 1); if (c != null) yield return c;
            var e = At(p.X, p.Y + 1); if (e != null) yield return e;
        }

        public int ActiveCount()
        {
            int n = 0;
            foreach (var p in _plots) if (p.Active) n++;
            return n;
        }

        /// 남은 칸의 토질 합. 이 게임에서 실제로 줄어드는 것이고, 어떤 정책으로도 늘 수 없다.
        public int Capacity()
        {
            int c = 0;
            foreach (var p in _plots) if (p.Active) c += p.Soil;
            return c;
        }

        /// 성장 곡선이 보는 값: 현금 + 남은 땅의 값. 땅을 잃으면 자산이 실제로 줄어든다.
        public int Asset()
        {
            int land = 0;
            foreach (var p in _plots) if (p.Active) land += p.Soil * _d.Economy.landValueCoinPerSoil;
            return _money + land;
        }

        static void Bump(Dictionary<string, int> map, string key, int by = 1)
        {
            map.TryGetValue(key, out var v);
            map[key] = v + by;
        }
    }
}
