using System;
using FarmSignal.Data;

namespace FarmSignal.Sim
{
    /// 기준 판매단가의 **잡음**만 담당한다. priceStepDays 마다 새로 뽑아 미리 굳혀 둔다.
    ///
    /// 남의 반응(모방 → 시세지수)은 여기 없다. SignalState.PriceIndexPercent 가 이 위에 곱해진다.
    /// **가르는 이유:** 검사기가 "시세가 내려간 것이 잡음인가 남의 반응인가"를 구별해야 한다.
    /// 한 수에 섞어 두면 SignalMatters 가 무엇을 봤는지 말할 수 없다.
    public sealed class PriceBook
    {
        readonly GameData _d;
        readonly int[,] _price;   // [cropIndex, step]
        readonly int _stepDays;

        public PriceBook(GameData d, int years)
        {
            _d = d;
            _stepDays = Math.Max(1, d.Economy.priceStepDays);
            int totalDays = years * d.DaysPerYear;
            int steps = totalDays / _stepDays + 1;
            int n = d.CropCount;
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

        public int BaseUnit(string cropId, int day)
        {
            int c = _d.CropIndex(cropId);
            if (c < 0) return 0;
            return _price[c, day / _stepDays];
        }
    }
}
