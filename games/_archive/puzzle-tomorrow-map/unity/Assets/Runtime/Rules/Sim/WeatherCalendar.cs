using System;
using FarmErosion.Data;

namespace FarmErosion.Sim
{
    /// 한 해가 아니라 시뮬레이션 전체의 날씨를 미리 뽑아 둔다.
    /// System.Random 하나를 순서대로 소비하므로 같은 씨드는 같은 달력이 된다.
    /// UnityEngine.Random을 한 번이라도 쓰면 이 재현성이 깨지고 100년 검사가 무의미해진다.
    public sealed class WeatherCalendar
    {
        readonly string[] _byDay;
        readonly GameData _d;

        public int Days => _byDay.Length;

        public WeatherCalendar(GameData d, int years)
        {
            _d = d;
            var rng = new Random(d.Config.seed + d.Config.weatherSeedOffset);
            _byDay = new string[years * d.DaysPerYear];
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

        public string IdAt(int day) => _byDay[day];
        public WeatherDef At(int day) => _d.Weather(_byDay[day]);
    }
}
