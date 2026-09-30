using System;
using FarmRewindYear.Data;

namespace FarmRewindYear.Sim
{
    /// **한 해만** 뽑는다. 되감아도 같은 달력을 다시 쓴다 —
    /// 그것이 이 PoC의 규칙이다. 되감기가 새 날씨를 주면 그건 다시 하기가 아니라 다른 해다.
    /// 날씨가 같아야 플레이어의 기억이 값을 갖고, 그 값의 대가로 흔적이 남는다.
    public sealed class WeatherCalendar
    {
        readonly string[] _byDay;
        readonly GameData _d;

        public int Days => _byDay.Length;

        public WeatherCalendar(GameData d)
        {
            _d = d;
            var rng = new Random(d.Config.seed + d.Config.weatherSeedOffset);
            _byDay = new string[d.DaysPerYear];
            for (int day = 0; day < _byDay.Length; day++)
                _byDay[day] = Pick(d.SeasonOfDay(day), rng);
        }

        static string Pick(SeasonDef season, Random rng)
        {
            int total = 0;
            for (int i = 0; i < season.weatherWeights.Length; i++) total += season.weatherWeights[i].weight;
            if (total <= 0) return season.weatherWeights[0].weatherId;
            int roll = rng.Next(total);
            for (int i = 0; i < season.weatherWeights.Length; i++)
            {
                roll -= season.weatherWeights[i].weight;
                if (roll < 0) return season.weatherWeights[i].weatherId;
            }
            return season.weatherWeights[season.weatherWeights.Length - 1].weatherId;
        }

        public string IdAt(int dayInYear) => _byDay[dayInYear];
        public WeatherDef At(int dayInYear) => _d.Weather(_byDay[dayInYear]);
    }
}
