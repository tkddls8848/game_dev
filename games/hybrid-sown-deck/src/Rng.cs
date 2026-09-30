// 난수는 씨드 고정. System.Random 을 직접 쓰고 씨드는 데이터(balance.json)에서 온다.
// UnityEngine.Random 은 한 번만 써도 헤드리스 재현이 깨지고 시뮬레이션 검증 전부가 무의미해진다.
using System;
using System.Collections.Generic;

namespace HybridSownDeck
{
    public sealed class Rng
    {
        readonly Random _r;
        public int Seed { get; }

        public Rng(int seed)
        {
            Seed = seed;
            _r = new Random(seed);
        }

        /// <summary>0 이상 maxExclusive 미만.</summary>
        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : _r.Next(maxExclusive);

        /// <summary>제자리 피셔-예이츠. 같은 씨드면 같은 순서다.</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _r.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    /// <summary>
    /// 백분율 정수 곱셈 + 나머지 누적. 부동소수를 쓰지 않고도 드리프트가 없다.
    /// 예: 수확량 2 에 비옥도 120% 를 세 번 → 2,3,2 (평균 2.4)이지 2.4 가 아니다.
    /// </summary>
    public sealed class PercentAccumulator
    {
        int _remainder;

        public int Apply(int baseValue, int percent)
        {
            int scaled = baseValue * percent + _remainder;
            int whole = scaled / 100;
            _remainder = scaled - whole * 100;
            return whole;
        }

        public int Remainder => _remainder;
        public void Reset() => _remainder = 0;
    }
}
