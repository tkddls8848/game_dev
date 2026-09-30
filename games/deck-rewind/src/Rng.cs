using System;
using System.Collections.Generic;

namespace DeckRewind
{
    /// <summary>
    /// 씨드 고정 난수 (설계 원칙 5). System.Random 을 쓰되 호출 횟수를 센다.
    ///
    /// 되감기가 있는 게임이므로 난수도 되돌려야 한다. 그래서 상태를 (씨드, 호출 횟수)
    /// 두 정수로만 표현하고, 복원은 같은 씨드로 새로 만들어 그 횟수만큼 다시 소비한다.
    /// 원시 연산이 Raw() 하나뿐이라 재생이 반드시 같은 지점에 도달한다 —
    /// 호출마다 소비량이 다른 편의 함수를 두면 그 보장이 깨진다.
    /// </summary>
    public sealed class Rng
    {
        readonly int _seed;
        Random _r;
        int _calls;

        public Rng(int seed)
        {
            _seed = seed;
            _r = new Random(seed);
            _calls = 0;
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

        public void RestoreTo(int calls)
        {
            _r = new Random(_seed);
            _calls = 0;
            for (int i = 0; i < calls; i++) Raw();
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
