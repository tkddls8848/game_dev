using System;
using System.Collections.Generic;

namespace DeckAttrition
{
    /// <summary>
    /// 씨드 고정 난수 (설계 원칙 5). <c>System.Random</c> 만 쓰고 씨드는 데이터에 적는다.
    ///
    /// 원시 연산은 <see cref="Raw"/> 하나뿐이다. 호출마다 소비량이 다른 편의 함수를 두면
    /// "같은 씨드 = 같은 결과"가 조용히 깨진다 — 이 PoC의 검사기 전부가 그 보장 위에 서 있다.
    /// </summary>
    public sealed class Rng
    {
        readonly int _seed;
        readonly Random _r;
        int _calls;

        public Rng(int seed)
        {
            _seed = seed;
            _r = new Random(seed);
        }

        public int Seed => _seed;
        public int Calls => _calls;

        int Raw()
        {
            _calls++;
            return _r.Next(int.MaxValue);
        }

        /// [0, maxExclusive) 정수. 부동소수를 지나가지 않는다.
        public int Range(int maxExclusive)
        {
            int raw = Raw();
            if (maxExclusive <= 1) return 0;
            return raw % maxExclusive;
        }

        /// 제자리 Fisher-Yates. 목록 순서가 결과를 정하므로 Dictionary 순회를 섞지 않는다.
        public void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
