using System;
using System.Collections.Generic;
using FarmRewindYear.Data;

namespace FarmRewindYear.Sim
{
    public sealed class PlotState
    {
        public string Id;
        public int X, Y;
        public int Soil;
        public bool Active;            // 산 칸인가. 되감으면 처음 상태로 돌아간다
        public string CropId;
        public int GrowthPoints;
        public int GrowthRemainderWeather;
        public int GrowthRemainderSoil;
        public bool WateredToday;

        public bool HasCrop => CropId != null;
    }

    /// 한 해를 살고, 못 넘기면 봄으로 되감는다.
    ///
    /// 되감을 때 되돌아가는 것: 날짜 · 현금 · 산 칸 · 심어 둔 것 · 그 해에 깎인 토질.
    /// 되돌아가지 않는 것: **되감은 횟수만큼 깎인 토질**. 그것만 남는다.
    public sealed class Simulation
    {
        readonly GameData _d;
        readonly PolicyKind _kind;
        readonly PolicyWeights _w;
        readonly WeatherCalendar _weather;
        readonly PriceBook _prices;
        readonly bool _forceAllAttempts;

        readonly List<PlotState> _plots = new List<PlotState>();
        readonly int[] _soilAtAttemptStart;
        /// 계절별 평균 성장률(백분율 정수). 예지 범위 밖의 날은 이 값으로 가정한다.
        /// 100%로 가정하면 모르는 쪽이 낙관 편향을 얻어 되감기가 오히려 손해가 된다 -
        /// "겨울은 느리다"는 되감지 않아도 아는 사실이므로 여기에 넣는다.
        readonly int[] _seasonAvgGrowth;
        /// 계절별 평균 성장 손실(백분율 정수). 예지 밖의 날은 이 값으로 가정한다.
        readonly int[] _seasonAvgLoss;

        int _money;
        int _plotsBought;
        int _rewinds;
        readonly RunResult _r = new RunResult();

        public IReadOnlyList<PlotState> Plots => _plots;
        public WeatherCalendar Weather => _weather;
        public PriceBook Prices => _prices;

        /// <param name="forceAllAttempts">
        /// 할당량을 넘겨도 멈추지 않고 되감기를 계속한다. "되감기가 공짜가 아니다"를
        /// 재려면 성공한 뒤의 시도들도 필요하다.
        /// </param>
        public Simulation(GameData d, PolicyKind kind, bool forceAllAttempts = false)
        {
            _d = d;
            _kind = kind;
            _w = PolicyWeights.For(kind);
            _weather = new WeatherCalendar(d);
            _prices = new PriceBook(d);
            _forceAllAttempts = forceAllAttempts;

            _seasonAvgGrowth = new int[d.Seasons.seasons.Length];
            _seasonAvgLoss = new int[d.Seasons.seasons.Length];
            for (int i = 0; i < d.Seasons.seasons.Length; i++)
            {
                var season = d.Seasons.seasons[i];
                int sum = 0, lossSum = 0, weight = 0;
                foreach (var ww in season.weatherWeights)
                {
                    sum += d.Weather(ww.weatherId).growthPercent * ww.weight;
                    lossSum += d.Weather(ww.weatherId).growthLossPoints * ww.weight;
                    weight += ww.weight;
                }
                _seasonAvgGrowth[i] = weight == 0 ? 100 : sum / weight;
                _seasonAvgLoss[i] = weight == 0 ? 0 : lossSum / weight;
            }

            _soilAtAttemptStart = new int[d.Plots.plots.Length];
            for (int i = 0; i < d.Plots.plots.Length; i++)
            {
                var p = d.Plots.plots[i];
                _plots.Add(new PlotState { Id = p.id, X = p.x, Y = p.y, Soil = p.soil, Active = p.startActive == 1 });
                _soilAtAttemptStart[i] = p.soil;
            }
        }

        public static RunResult Run(GameData d, PolicyKind kind, bool forceAllAttempts = false)
            => new Simulation(d, kind, forceAllAttempts).Execute();

        public RunResult Execute()
        {
            int maxAttempts = Math.Min(_d.Config.simAttempts, _d.Plots.rewind.maxRewinds + 1);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var rec = PlayOneYear(attempt);
                _r.Attempts.Add(rec);
                if (rec.IncomeCoin > _r.BestIncomeCoin) { _r.BestIncomeCoin = rec.IncomeCoin; _r.BestIncomeAttempt = attempt; }

                if (rec.QuotaMet)
                {
                    if (_r.SuccessAttempt < 0) _r.SuccessAttempt = attempt;
                    if (!_forceAllAttempts) break;
                }
                if (attempt == maxAttempts) { if (_r.SuccessAttempt < 0) _r.Exhausted = true; break; }
                Rewind();
            }

            _r.RewindsUsed = _rewinds;
            return _r;
        }

        // ── 한 해 ────────────────────────────────────────────────────────────

        AttemptRecord PlayOneYear(int attempt)
        {
            ResetYear();
            int foresight = PolicyWeights.ForesightDays(_kind, _rewinds, _d);
            var rec = new AttemptRecord
            {
                Attempt = attempt,
                RewindsBefore = _rewinds,
                ForesightDays = foresight,
                SoilSumAtStart = SoilSum(),
            };

            for (int day = 0; day < _d.DaysPerYear; day++)
            {
                var season = _d.SeasonOfDay(day);
                int dayInSeason = _d.DayInSeason(day);
                var weather = _weather.At(day);
                Bump(_r.WeatherCounts, weather.id);

                if (dayInSeason == 1 && day > 0) ApplySeasonTransition(season, rec);

                Harvest(day, season, rec);
                if (_w.AllowExpansion) BuyPlots(season, day, rec);
                PlantAll(day, season, foresight, rec);
                AllocateWater(weather);
                Grow(weather);
                FireEvents(season, dayInSeason);

                if (dayInSeason == _d.Seasons.daysPerSeason) PayLivingCost(season, rec);
                if (_money > _r.PeakAssetCoin) _r.PeakAssetCoin = _money;
            }

            // 해를 넘긴 작물은 죽는다. 씨앗값과 그 칸의 한 해가 같이 사라진다 -
            // 예지가 막아 주는 손해가 이것이다.
            foreach (var p in _plots)
                if (p.Active && p.HasCrop) { rec.CropsLost++; Bump(_r.LostCounts, p.CropId); }

            rec.FinalMoneyCoin = _money;
            rec.SoilSumAtEnd = SoilSum();
            rec.PlotsBought = _plotsBought;
            rec.ActivePlots = ActiveCount();
            rec.QuotaMet = _money >= _d.Economy.quotaCoin;
            return rec;
        }

        void ResetYear()
        {
            _money = _d.Economy.startMoney;
            _plotsBought = 0;
            for (int i = 0; i < _plots.Count; i++)
            {
                var p = _plots[i];
                p.Soil = _soilAtAttemptStart[i];
                p.Active = _d.Plots.plots[i].startActive == 1;
                p.CropId = null;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
                p.WateredToday = false;
            }
        }

        /// 되감기. 봄으로 돌아가되 모든 칸의 토질이 깎이고, 그 값은 되돌지 않는다.
        void Rewind()
        {
            _rewinds++;
            int loss = _d.Plots.rewind.soilLossPerRewind;
            int floor = _d.Plots.rewind.soilFloor;
            for (int i = 0; i < _soilAtAttemptStart.Length; i++)
            {
                int next = _soilAtAttemptStart[i] - loss;
                _soilAtAttemptStart[i] = next < floor ? floor : next;
            }
        }

        void ApplySeasonTransition(SeasonDef season, AttemptRecord rec)
        {
            foreach (var p in _plots)
            {
                if (!p.Active) continue;
                p.Soil = IntMath.Clamp(p.Soil - season.transitionSoilLoss, 0, 100);

                // 계절이 바뀌면 그 계절을 못 견디는 작물은 죽는다. 씨앗값과 그 칸의 시간이 같이 사라진다.
                // 이 마감이 있어야 '언제 심는가'가 판단이 된다 — 그리고 그 판단에 필요한 것이 날씨 기억이다.
                if (!p.HasCrop) continue;
                if (_d.CropFitsSeason(_d.Crop(p.CropId), season.id)) continue;
                rec.CropsLost++;
                Bump(_r.LostCounts, p.CropId);
                p.CropId = null;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
        }

        void PayLivingCost(SeasonDef season, AttemptRecord rec)
        {
            int cost = _d.Economy.livingCostPerSeason;
            if (cost <= 0) return;
            _money -= cost;
            rec.ExpenseCoin += cost;
            Bump(_r.SeasonExpense, season.id, cost);
        }

        void Harvest(int day, SeasonDef season, AttemptRecord rec)
        {
            foreach (var p in _plots)
            {
                if (!p.Active || !p.HasCrop) continue;
                var crop = _d.Crop(p.CropId);
                if (p.GrowthPoints < crop.growDays * 100) continue;

                int gain = crop.yieldUnits * _prices.SellUnit(crop.id, day);
                _money += gain;
                rec.IncomeCoin += gain;
                Bump(_r.SeasonIncome, season.id, gain);
                Bump(_r.HarvestCounts, crop.id);

                p.Soil = IntMath.Clamp(p.Soil - crop.soilDrain, 0, 100);
                p.CropId = null;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
        }

        void BuyPlots(SeasonDef season, int day, AttemptRecord rec)
        {
            var ex = _d.Plots.expansion;
            if (ex.enabled == 0) return;
            if (ex.deadlineDayInYear != -1 && day + 1 > ex.deadlineDayInYear) return;
            while (_plotsBought < ex.maxExtraPlots)
            {
                var target = NextBuyable();
                if (target == null) return;
                int cost = ex.costBase + ex.costPerPlot * _plotsBought;
                if (_money - cost < _w.ExpandMoneyReserve) return;
                _money -= cost;
                rec.ExpenseCoin += cost;
                Bump(_r.SeasonExpense, season.id, cost);
                target.Active = true;
                _plotsBought++;
            }
        }

        /// 살 수 있는 칸 중 토질이 가장 좋은 것. 동점은 id로 갈라 결정적으로 만든다.
        PlotState NextBuyable()
        {
            PlotState best = null;
            for (int i = 0; i < _plots.Count; i++)
            {
                if (_plots[i].Active) continue;
                if (_d.Plots.plots[i].startActive == 1) continue;
                if (best == null || _plots[i].Soil > best.Soil ||
                    (_plots[i].Soil == best.Soil && string.CompareOrdinal(_plots[i].Id, best.Id) < 0))
                    best = _plots[i];
            }
            return best;
        }

        void PlantAll(int day, SeasonDef season, int foresight, AttemptRecord rec)
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
                var pick = Choose(p, day, season, foresight);
                if (pick == null) continue;
                var shop = _d.Shop(pick.id);
                if (_money < shop.buySeed) continue;

                _money -= shop.buySeed;
                rec.ExpenseCoin += shop.buySeed;
                rec.PlantingsTotal++;
                Bump(_r.SeasonExpense, season.id, shop.buySeed);
                Bump(_r.PlantCounts, pick.id);

                p.CropId = pick.id;
                p.GrowthPoints = 0;
                p.GrowthRemainderWeather = 0;
                p.GrowthRemainderSoil = 0;
            }
        }

        /// 이 칸에 무엇을 심을지. 예지 범위 안의 날씨와 시세는 실제 값을, 밖은 계절 평균과
        /// 기준가로 가정한다. 그 차이가 이 PoC가 모형화한 "되감아서 얻는 것"의 전부다.
        ///
        /// 여기에 '기다리기'를 넣어 봤다가 뺐다. 하루 점수 x 남은 일수로 견주면 먼 미래의
        /// 좋은 날이 과대평가되어 정책이 밭을 비워 두고, 지식이 있는 쪽이 오히려 덜 벌었다.
        /// 사람은 한 해의 윤작을 계획하지만 이 탐욕 정책은 그러지 못한다 —
        /// 그래서 "사람의 학습이 흔적을 이기는가"는 이 검사기가 판정할 수 없다. README의 판정 칸 참고.
        public CropDef Choose(PlotState p, int day, SeasonDef season, int foresight)
        {
            CropDef best = null;
            int bestScore = int.MinValue;

            foreach (var crop in _d.Crops.crops)
            {
                if (!_d.CropFitsSeason(crop, season.id)) continue;
                if (p.Soil < crop.minSoil) continue;
                if (p.Soil < _d.Plots.soilFloorForPlanting) continue;

                int days = EstimateRipenDays(crop, p.Soil, day, foresight);
                if (days < 0) continue;   // 해를 넘기거나 계절을 못 견딘다

                var shop = _d.Shop(crop.id);
                int harvestDay = day + days - 1;
                int unit = harvestDay < foresight ? _prices.SellUnit(crop.id, harvestDay) : shop.sellUnit;
                int net = crop.yieldUnits * unit - shop.buySeed;
                int score = net * 100 / days - crop.soilDrain * _w.SoilCoinPerPoint * 100 / days;

                if (score > bestScore || (score == bestScore && best != null &&
                                          string.CompareOrdinal(crop.id, best.id) < 0))
                {
                    bestScore = score;
                    best = crop;
                }
            }
            return best;
        }

        /// 이 칸에서 그 작물을 심을 수 있는가(계절·토질만 본다). 흔적이 사다리를 내려가는지 세는 데 쓴다.
        public bool CanHost(PlotState p, CropDef crop) => p.Active && p.Soil >= crop.minSoil;

        /// 지금 심으면 며칠에 익는가. -1이면 해를 넘긴다.
        public int EstimateRipenDays(CropDef crop, int soil, int startDay, int foresight)
        {
            int need = crop.growDays * 100;
            int soilFactor = 50 + soil / 2;
            int points = 0, remW = 0, remS = 0;
            for (int k = 0; startDay + k < _d.DaysPerYear; k++)
            {
                int at = startDay + k;
                int seasonIndex = at / _d.Seasons.daysPerSeason;
                // 그 날의 계절을 못 견디면 익기 전에 죽는다. 예지가 있으면 이 마감을 정확히 본다.
                if (!_d.CropFitsSeason(crop, _d.Seasons.seasons[seasonIndex].id)) return -1;
                bool known = at < foresight;
                int gp = known ? _weather.At(at).growthPercent : _seasonAvgGrowth[seasonIndex];
                int loss = known ? _weather.At(at).growthLossPoints : _seasonAvgLoss[seasonIndex];
                if (loss > 0) { points -= loss; if (points < 0) points = 0; }
                int afterWeather = IntMath.MulPercent(100, gp, ref remW);
                points += IntMath.MulPercent(afterWeather, soilFactor, ref remS);
                if (points >= need) return k + 1;
            }
            return -1;
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
            return crop.yieldUnits * shop.sellUnit * 100 / Math.Max(1, crop.waterPerDay);
        }

        void Grow(WeatherDef weather)
        {
            foreach (var p in _plots)
            {
                if (!p.Active || !p.HasCrop) continue;
                // 폭풍·서리는 쌓아 둔 진행도를 정액으로 깎는다(하루 이상적 성장 = 100점).
                if (weather.growthLossPoints > 0)
                {
                    p.GrowthPoints -= weather.growthLossPoints;
                    if (p.GrowthPoints < 0) p.GrowthPoints = 0;
                }
                int basePoints = p.WateredToday ? 100 : 40;
                int afterWeather = IntMath.MulPercent(basePoints, weather.growthPercent, ref p.GrowthRemainderWeather);
                int soilFactor = 50 + p.Soil / 2;   // 토질 0 → 50%, 100 → 100%
                p.GrowthPoints += IntMath.MulPercent(afterWeather, soilFactor, ref p.GrowthRemainderSoil);
            }
        }

        void FireEvents(SeasonDef season, int dayInSeason)
        {
            foreach (var e in _d.Events.events)
            {
                if (!string.IsNullOrEmpty(e.onSeason) && e.onSeason != season.id) continue;
                if (e.onDay != -1 && e.onDay != dayInSeason) continue;
                if (e.minRewinds != -1 && _rewinds < e.minRewinds) continue;
                if (e.minMoney != -1 && _money < e.minMoney) continue;
                Bump(_r.EventFireCounts, e.id);
            }
        }

        // ── 조회 ────────────────────────────────────────────────────────────

        public int ActiveCount()
        {
            int n = 0;
            foreach (var p in _plots) if (p.Active) n++;
            return n;
        }

        public int SoilSum()
        {
            int s = 0;
            foreach (var p in _plots) s += p.Soil;
            return s;
        }

        /// 되감기 단계별로 봄에 남아 있는 토질 합. 흔적을 그대로 보여 준다.
        public int SoilSumAtSpring()
        {
            int s = 0;
            foreach (var v in _soilAtAttemptStart) s += v;
            return s;
        }

        static void Bump(Dictionary<string, int> map, string key, int by = 1)
        {
            map.TryGetValue(key, out var v);
            map[key] = v + by;
        }
    }
}
