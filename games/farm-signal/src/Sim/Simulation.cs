using System;
using System.Collections.Generic;
using FarmSignal.Data;

namespace FarmSignal.Sim
{
    public sealed class PlotState
    {
        public string Id;
        public int X, Y;
        public int BaseSoil;      // 본래 토질. 회복의 상한이다
        public int Soil;
        public int Exposure;      // 길에서 보이는 정도(고정값)
        public string CropId;     // null이면 빈 칸
        public int GrowthPoints;
        public int GrowthRemainderWeather;
        public int GrowthRemainderSoil;
        public bool WateredToday;

        public bool HasCrop => CropId != null;
    }

    /// 하루 한 걸음. 모든 판단이 정수와 씨드 고정 난수만 쓰므로 같은 씨드는 같은 결과가 된다.
    ///
    /// 이 PoC가 다른 파밍 PoC와 갈리는 곳은 한 군데다: 하루가 끝날 때 **밭을 남이 본다**.
    /// Observe() 가 그 한 줄이고, 계절이 끝날 때 SignalState.CloseSeason() 이 남의 반응을 만든다.
    public sealed class Simulation
    {
        readonly GameData _d;
        readonly PolicyWeights _w;
        readonly SignalRules _rules;
        readonly WeatherCalendar _weather;
        readonly PriceBook _prices;
        readonly SignalState _signal;
        readonly int _years;

        readonly List<PlotState> _plots = new List<PlotState>();
        readonly Dictionary<string, PlotState> _byId = new Dictionary<string, PlotState>();
        readonly HashSet<string> _firedOnce = new HashSet<string>();
        readonly int[] _screenScratch;

        int _money;
        bool _bankrupt;
        readonly SimResult _r = new SimResult();

        public IReadOnlyList<PlotState> Plots => _plots;
        public SignalState Signal => _signal;
        public int Money => _money;

        public Simulation(GameData d, PolicyWeights w, SignalRules rules, int years)
        {
            _d = d;
            _w = w;
            _rules = rules;
            _years = years;
            _weather = new WeatherCalendar(d, years);
            _prices = new PriceBook(d, years);
            _signal = new SignalState(d, rules);
            _screenScratch = new int[Math.Max(1, d.Plots.screenDepth)];

            foreach (var p in d.Plots.plots)
            {
                var s = new PlotState
                {
                    Id = p.id, X = p.x, Y = p.y,
                    BaseSoil = p.soil, Soil = p.soil, Exposure = p.exposure,
                };
                _plots.Add(s);
                _byId[p.id] = s;
            }
            _money = d.Economy.startMoney;
            _r.PolicyLabel = w.Label;
            _r.SignalOn = rules.On;
        }

        public static SimResult Run(GameData d, PolicyKind kind, int years, bool signalOn = true,
                                    string forcedCropId = null)
        {
            var rules = signalOn ? SignalRules.Full(d) : SignalRules.Off(d);
            var sim = new Simulation(d, PolicyWeights.For(kind, forcedCropId), rules, years);
            return sim.Execute();
        }

        public SimResult Execute()
        {
            int daysPerSeason = _d.Seasons.daysPerSeason;
            int totalDays = _years * _d.DaysPerYear;
            _r.Years = _years;
            _r.DaysSimulated = totalDays;
            _r.PlayableDays = totalDays;
            _r.PeakMoneyCoin = _money;
            _r.PeakMoneyDay = 0;

            int yearIncome = 0, yearExpense = 0, yearTheftPlots = 0, yearTheftSeed = 0, yearObserved = 0;

            for (int day = 0; day < totalDays; day++)
            {
                var season = _d.SeasonOfDay(day);
                int year = _d.YearOfDay(day);
                int dayInSeason = _d.DayInSeason(day);
                int seasonNumber = _d.SeasonNumberOfDay(day);
                var weather = _weather.At(day);
                Bump(_r.WeatherCounts, weather.id);

                if (dayInSeason == 1 && day > 0) ApplySeasonTransition(season);

                HarvestReady(day, season, ref yearIncome);
                if (!_bankrupt) PlantAll(day, season, ref yearExpense);
                AllocateWater(weather);
                Grow(weather);
                ObserveField(season, weather);
                FireEvents(year, season, dayInSeason);

                if (dayInSeason == daysPerSeason)
                {
                    PayLivingCost(season, ref yearExpense);
                    var rec = CloseSeasonSignal(seasonNumber, year, season);
                    yearObserved += rec.ObservedTotal;
                    int seedLost = ApplyTheft(rec);
                    yearTheftPlots += rec.TheftPlotsHit;
                    yearTheftSeed += seedLost;
                }

                CheckBankruptcy(year, day);
                if (_money > _r.PeakMoneyCoin) { _r.PeakMoneyCoin = _money; _r.PeakMoneyDay = day + 1; }

                if (dayInSeason == daysPerSeason && season.order == _d.SeasonsPerYear - 1)
                {
                    int tax = _bankrupt ? 0 : _plots.Count * _d.Economy.taxPerPlotPerYear;
                    if (tax > 0)
                    {
                        _money -= tax; yearExpense += tax;
                        _r.TotalExpenseCoin += tax; _r.UpkeepPaidCoin += tax;
                    }
                    CheckBankruptcy(year, day);
                    _r.YearRecords.Add(new YearRecord
                    {
                        Year = year,
                        IncomeCoin = yearIncome,
                        ExpenseCoin = yearExpense,
                        MoneyAtYearEnd = _money,
                        TheftPlotsLost = yearTheftPlots,
                        TheftSeedLossCoin = yearTheftSeed,
                        ObservedIndexSum = yearObserved,
                        RenownAtYearEnd = _signal.Renown,
                        MinPriceIndexAtYearEnd = MinPriceIndexNow(),
                    });
                    yearIncome = 0; yearExpense = 0; yearTheftPlots = 0; yearTheftSeed = 0; yearObserved = 0;
                }
            }

            _r.FinalMoneyCoin = _money;
            _r.SignalHistory.AddRange(_signal.History);
            _r.FirstWarnSeason = _signal.FirstWarnSeason;
            _r.FirstCollapseSeason = _signal.FirstCollapseSeason;
            _r.FirstTheftSeason = _signal.FirstTheftSeason;
            _r.FirstVisitorSeason = _signal.FirstVisitorSeason;
            _r.TheftCount = _signal.TheftCount;
            _r.TheftPlotsLost = _signal.TheftPlotsLost;
            _r.MaxRenown = _signal.MaxRenown;
            _r.MinPriceIndexSeen = _signal.MinPriceIndexSeen == 1000 ? -1 : _signal.MinPriceIndexSeen;
            _r.MaxPriceIndexSeen = _signal.MaxPriceIndexSeen == 0 ? -1 : _signal.MaxPriceIndexSeen;
            return _r;
        }

        // ── 한 걸음의 조각들 ────────────────────────────────────────────────

        void ApplySeasonTransition(SeasonDef season)
        {
            int regen = _d.Plots.soilRegenPerSeason;
            foreach (var p in _plots)
            {
                p.Soil = IntMath.Clamp(p.Soil - season.transitionSoilLoss, 0, 100);
                if (p.Soil < p.BaseSoil) p.Soil = Math.Min(p.Soil + regen, p.BaseSoil);
            }
        }

        void PayLivingCost(SeasonDef season, ref int yearExpense)
        {
            if (_bankrupt) return;
            int cost = _d.Economy.livingCostPerSeason;
            if (cost <= 0) return;
            _money -= cost;
            yearExpense += cost;
            _r.TotalExpenseCoin += cost;
            _r.UpkeepPaidCoin += cost;
            Bump(_r.SeasonExpense, season.id, cost);
        }

        void CheckBankruptcy(int year, int day)
        {
            if (_bankrupt || _money >= _d.Economy.bankruptcyAtCoin) return;
            _bankrupt = true;
            _r.BankruptYear = year;
            _r.BankruptDay = day + 1;
            if (_r.PlayableDays > day + 1) _r.PlayableDays = day + 1;
            foreach (var p in _plots) { p.CropId = null; p.GrowthPoints = 0; }
        }

        void HarvestReady(int day, SeasonDef season, ref int yearIncome)
        {
            foreach (var p in _plots)
            {
                if (!p.HasCrop) continue;
                var crop = _d.Crop(p.CropId);
                if (p.GrowthPoints < crop.growDays * 100) continue;

                int unit = _signal.SellUnit(crop.id, _prices.BaseUnit(crop.id, day));
                int gain = crop.yieldUnits * unit;
                _money += gain;
                yearIncome += gain;
                _r.TotalIncomeCoin += gain;
                Bump(_r.SeasonIncome, season.id, gain);
                Bump(_r.HarvestCounts, crop.id);
                Bump(_r.CropRevenue, crop.id, gain);

                p.Soil = IntMath.Clamp(p.Soil - crop.soilDrain, 0, 100);
                if (p.Soil > p.BaseSoil) p.Soil = p.BaseSoil;   // 겨울호밀이 본래 토질을 넘지는 못한다
                p.CropId = null;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
        }

        void PlantAll(int day, SeasonDef season, ref int yearExpense)
        {
            // 심는 순서: 노출이 낮은 칸부터. 뒤 칸을 먼저 채우면 가림막 판단이 앞 칸에서 뒤를 보고 정해진다.
            var empty = new List<PlotState>();
            foreach (var p in _plots) if (!p.HasCrop) empty.Add(p);
            empty.Sort((a, b) =>
            {
                int c = a.Exposure.CompareTo(b.Exposure);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Id, b.Id);
            });

            int cap = CapPerCrop(season);
            foreach (var p in empty)
            {
                var pick = Choose(p, season, day, cap);
                if (pick == null) { _r.EmptyPlotDays++; continue; }
                var shop = _d.Shop(pick.id);
                if (_money < shop.buySeed) { _r.EmptyPlotDays++; continue; }

                _money -= shop.buySeed;
                yearExpense += shop.buySeed;
                _r.TotalExpenseCoin += shop.buySeed;
                _r.SeedSpentCoin += shop.buySeed;
                Bump(_r.SeasonExpense, season.id, shop.buySeed);
                Bump(_r.PlantCounts, pick.id);

                p.CropId = pick.id;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
        }

        /// 한 작물이 밭에서 차지할 수 있는 칸 수. 고른 분배보다 좁아지지는 않는다 —
        /// 겨울처럼 심을 수 있는 작물이 둘뿐인 계절에 밭을 비우면 안 된다.
        int CapPerCrop(SeasonDef season)
        {
            if (_w.MaxCropSharePercent >= 100) return _plots.Count;
            int available = 0;
            foreach (var c in _d.Crops.crops) if (_d.CropFitsSeason(c, season.id)) available++;
            int cap = _plots.Count * _w.MaxCropSharePercent / 100;
            int even = IntMath.CeilDiv(_plots.Count, Math.Max(1, available));
            return Math.Max(cap, even);
        }

        int CountPlanted(string cropId)
        {
            int n = 0;
            foreach (var p in _plots) if (p.CropId == cropId) n++;
            return n;
        }

        /// 이 칸에 무엇을 심을지. 점수는 1/100코인 / 이상일 단위로 맞춘다.
        public CropDef Choose(PlotState p, SeasonDef season, int day, int capPerCrop)
        {
            if (p.Soil < _d.Plots.soilFloorForPlanting) return null;
            int effExp = EffectiveExposure(p);
            bool isFrontRow = p.Y == 0 && HasPlotBehind(p);
            string seasonBest = _w.SeasonBestOnly ? SeasonBestCrop(season) : null;

            CropDef best = null;
            int bestScore = int.MinValue;

            foreach (var crop in _d.Crops.crops)
            {
                if (_w.ForcedCropId != null && crop.id != _w.ForcedCropId) continue;
                if (seasonBest != null && crop.id != seasonBest) continue;
                if (!_d.CropFitsSeason(crop, season.id)) continue;
                if (p.Soil < crop.minSoil) continue;
                if (capPerCrop < _plots.Count && CountPlanted(crop.id) >= capPerCrop) continue;

                var shop = _d.Shop(crop.id);
                // 시세판을 읽는 정책만 살아 있는 시세를 본다. 안 읽는 정책은 상점 기준가로 정한다 —
                // 사프란이 25%로 내려앉아도 계속 사프란을 심는다. 그게 "자기 밭만 본다"는 뜻이다.
                int unit = _w.ReadsBoard
                    ? _signal.PeekSellUnit(crop.id, _prices.BaseUnit(crop.id, day))
                    : shop.sellUnit;
                int net = crop.yieldUnits * unit - shop.buySeed;
                int score = net * 100 / crop.growDays;

                if (_w.SeeSignal)
                {
                    // 벌은 "얼마나 보이는가" 곱하기 "그 작물이 얼마짜리인가"다.
                    // 값을 빼고 노출만 보면 가장 잘 보이는 라벤더가 가장 큰 벌을 받는데,
                    // 라벤더의 노출은 평판이라 이득이다. 처음에 그 실수를 했고 라벤더가 한 번도 안 심겼다.
                    int worth = crop.yieldUnits * shop.sellUnit;
                    int seen = crop.visibility * effExp / 100;                 // 0~100
                    int copied = seen * crop.copyAppeal / 100;                 // 따라 심힐 만큼만
                    int noticed = crop.theftAppeal * effExp / 100;             // 0~100
                    score -= copied * _w.CopycatWeight * worth / 1000;
                    score -= noticed * _w.TheftWeight * worth / 1000;
                    if (_signal.Renown < _w.RenownTarget)
                        score += crop.renownGain * effExp / 100 * _w.RenownWeight;
                }
                if (_w.UseScreens && isFrontRow && p.Exposure >= _w.ScreenFrontExposureMin)
                    score += crop.screenHeight * _w.ScreenWeight;

                if (score > bestScore ||
                    (score == bestScore && best != null && string.CompareOrdinal(crop.id, best.id) < 0))
                {
                    bestScore = score;
                    best = crop;
                }
            }
            return best;
        }

        /// 기준가(데이터)만 보고 고른 계절 1위. 남의 반응을 셈하지 않는다 — 그게 단작 정책의 정의다.
        string SeasonBestCrop(SeasonDef season)
        {
            string best = null;
            int bestScore = int.MinValue;
            foreach (var crop in _d.Crops.crops)
            {
                if (!_d.CropFitsSeason(crop, season.id)) continue;
                var shop = _d.Shop(crop.id);
                int score = (crop.yieldUnits * shop.sellUnit - shop.buySeed) * 100 / crop.growDays;
                if (score > bestScore || (score == bestScore && best != null && string.CompareOrdinal(crop.id, best) < 0))
                {
                    bestScore = score;
                    best = crop.id;
                }
            }
            return best;
        }

        void AllocateWater(WeatherDef weather)
        {
            int available = Math.Max(0, _d.Economy.wellWaterPerDay + weather.waterDelta);
            var growing = new List<PlotState>();
            foreach (var p in _plots)
            {
                p.WateredToday = false;
                if (p.HasCrop) growing.Add(p);
            }
            growing.Sort((a, b) =>
            {
                int c = WaterPriority(b).CompareTo(WaterPriority(a));
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
            if (_w.ReadsBoard) gross = gross * _signal.PriceIndexOf(crop.id) / 100;
            return gross * 100 / Math.Max(1, crop.waterPerDay);
        }

        void Grow(WeatherDef weather)
        {
            foreach (var p in _plots)
            {
                if (!p.HasCrop) continue;
                int basePoints = p.WateredToday ? 100 : 40;
                int afterWeather = IntMath.MulPercent(basePoints, weather.growthPercent, ref p.GrowthRemainderWeather);
                int soilFactor = 50 + p.Soil / 2;   // 토질 0 → 50%, 100 → 100%
                p.GrowthPoints += IntMath.MulPercent(afterWeather, soilFactor, ref p.GrowthRemainderSoil);
            }
        }

        /// **이 PoC의 한 줄.** 하루가 끝나면 밭이 남에게 읽힌다.
        /// 순회는 반드시 plots[] 배열 순서로 — 나머지 누적이 순서에 걸려 있다.
        void ObserveField(SeasonDef season, WeatherDef weather)
        {
            if (weather.id == "fog") _signal.CountFogDay();
            foreach (var p in _plots)
            {
                if (!p.HasCrop) continue;
                _signal.Observe(p.CropId, EffectiveExposure(p), weather.exposurePercent, season.roadTrafficPercent);
            }
        }

        SeasonSignal CloseSeasonSignal(int seasonNumber, int year, SeasonDef season)
        {
            var candidates = new List<TheftCandidate>();
            foreach (var p in _plots)
            {
                if (!p.HasCrop) continue;
                var crop = _d.Crop(p.CropId);
                candidates.Add(new TheftCandidate
                {
                    PlotId = p.Id, CropId = p.CropId,
                    EffectiveExposure = EffectiveExposure(p), Appeal = crop.theftAppeal,
                });
            }
            var rec = _signal.CloseSeason(seasonNumber, year, season.id, candidates);
            if (_signal.Visitors > _r.MaxVisitors) _r.MaxVisitors = _signal.Visitors;
            if (_signal.PremiumPercent > _r.MaxPremiumPercent) _r.MaxPremiumPercent = _signal.PremiumPercent;
            return rec;
        }

        /// 털린 칸의 작물이 사라진다. 씨앗값과 그 칸의 시간이 같이 사라진다.
        int ApplyTheft(SeasonSignal rec)
        {
            if (!rec.TheftStruck) return 0;
            int seedLost = 0;
            foreach (var target in rec.TheftTargets)
            {
                int sep = target.IndexOf(':');
                string plotId = target.Substring(0, sep);
                if (!_byId.TryGetValue(plotId, out var p) || !p.HasCrop) continue;
                var shop = _d.Shop(p.CropId);
                seedLost += shop.buySeed;
                p.CropId = null;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
            _r.TheftSeedLossCoin += seedLost;
            return seedLost;
        }

        void FireEvents(int year, SeasonDef season, int dayInSeason)
        {
            int observed = _signal.ObservedTotalThisSeason();
            int visitorLevel = _signal.Visitors;
            foreach (var e in _d.Events.events)
            {
                if (e.onYear != -1 && e.onYear != year) continue;
                if (!string.IsNullOrEmpty(e.onSeason) && e.onSeason != season.id) continue;
                if (e.onDay != -1 && e.onDay != dayInSeason) continue;
                if (e.minObserved != -1 && observed < e.minObserved) continue;
                if (e.minRenownLevel != -1 && visitorLevel < e.minRenownLevel) continue;
                if (e.once == 1 && _firedOnce.Contains(e.id)) continue;
                if (e.once == 1) _firedOnce.Add(e.id);
                Bump(_r.EventFireCounts, e.id);
            }
        }

        // ── 조회 ────────────────────────────────────────────────────────────

        /// 길에서 이 칸이 실제로 보이는 정도. 앞(길 쪽) 칸의 키 큰 작물이 가려 준다.
        public int EffectiveExposure(PlotState p)
        {
            int depth = Math.Max(1, _d.Plots.screenDepth);
            for (int k = 0; k < depth; k++)
            {
                var front = At(p.X, p.Y - (k + 1));
                _screenScratch[k] = (front != null && front.HasCrop) ? _d.Crop(front.CropId).screenHeight : 0;
            }
            return SignalMath.EffectiveExposure(_d, p.Exposure, _screenScratch);
        }

        public PlotState At(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _d.Plots.gridWidth || y >= _d.Plots.gridHeight) return null;
            foreach (var p in _plots) if (p.X == x && p.Y == y) return p;
            return null;
        }

        bool HasPlotBehind(PlotState p) => At(p.X, p.Y + 1) != null;

        int MinPriceIndexNow()
        {
            int m = int.MaxValue;
            for (int c = 0; c < _signal.PriceIndexPercent.Length; c++)
                if (_signal.PriceIndexPercent[c] < m) m = _signal.PriceIndexPercent[c];
            return m == int.MaxValue ? -1 : m;
        }

        static void Bump(Dictionary<string, int> map, string key, int by = 1)
        {
            map.TryGetValue(key, out var v);
            map[key] = v + by;
        }
    }
}
