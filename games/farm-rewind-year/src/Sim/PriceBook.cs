using System;
using FarmRewindYear.Data;

namespace FarmRewindYear.Sim
{
    /// 판매단가를 priceStepDays 마다 새로 뽑아 **한 해 분량만** 굳혀 둔다.
    /// 되감아도 같은 가격표를 다시 쓴다 — 날씨와 같은 이유다(WeatherCalendar 참고).
    /// 기준가에 (100 - band) ~ (100 + band) 백분율을 걸고 나머지를 작물별로 누적한다.
    public sealed class PriceBook
    {
        readonly GameData _d;
        readonly int[,] _price;   // [cropIndex, step]
        readonly int _stepDays;

        public PriceBook(GameData d)
        {
            _d = d;
            _stepDays = Math.Max(1, d.Economy.priceStepDays);
            int totalDays = d.DaysPerYear;
            int steps = totalDays / _stepDays + 1;
            int n = d.Crops.crops.Length;
            _price = new int[n, steps];

            var rng = new Random(d.Config.seed + d.Config.priceSeedOffset);
            var remainder = new int[n];
            int band = d.Economy.priceBandPercent;

            for (int step = 0; step < steps; step++)
                for (int c = 0; c < n; c++)
                {
                    var shop = d.Shop(d.Crops.crops[c].id);
                    int basePrice = shop == null ? 0 : shop.sellUnit;
                    int percent = 100 - band + rng.Next(band * 2 + 1);
                    _price[c, step] = IntMath.MulPercent(basePrice, percent, ref remainder[c]);
                }
        }

        public int CropIndex(string cropId)
        {
            for (int i = 0; i < _d.Crops.crops.Length; i++)
                if (_d.Crops.crops[i].id == cropId) return i;
            return -1;
        }

        public int SellUnit(string cropId, int dayInYear)
        {
            int c = CropIndex(cropId);
            if (c < 0) return 0;
            return _price[c, dayInYear / _stepDays];
        }
    }
}
