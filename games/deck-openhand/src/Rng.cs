using System.Collections.Generic;

namespace DeckOpenhand
{
    /// <summary>
    /// 씨드 고정 난수 (설계 원칙 5). System.Random 을 씨드로 만들어 직접 쓴다.
    ///
    /// 이 PoC에서 난수가 닿는 곳은 <b>카드를 뽑는 순서 하나뿐</b>이다.
    /// 적의 행동은 난수가 아니라 고정된 순서고, 그 순서가 플레이어에게 늘 보인다 —
    /// 그것이 이 PoC의 규칙이다. 그래서 운의 비중이 작고, 대신 순서가 퍼즐이 된다.
    ///
    /// 원시 연산을 Raw() 하나로 좁혀 둔다. 호출마다 소비량이 다른 편의 함수를 두면
    /// 씨드가 같아도 소비 지점이 갈려 재현이 깨진다.
    /// </summary>
    public sealed class Rng
    {
        readonly int _seed;
        readonly System.Random _r;
        int _calls;

        public Rng(int seed)
        {
            _seed = seed;
            _r = new System.Random(seed);
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
