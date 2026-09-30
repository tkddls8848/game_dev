using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DeckRewind.Tests
{
    /// <summary>
    /// 테스트가 공유하는 데이터. 파일을 한 번만 읽는다.
    /// 여기서 만드는 에이전트는 매번 새로 만든다 — 상태(채택 통계 · 자기 난수)를 들고 있기 때문이다.
    /// </summary>
    public static class Fix
    {
        static GameData _data;

        public static GameData Data => _data ?? (_data = GameData.Load(TestContext.CurrentContext.TestDirectory));

        public static IAgent Greedy() => new GreedyAgent(Data);
        public static IAgent GreedyNoRewind() => new GreedyAgent(Data, RewindPolicy.Never);
        public static IAgent GreedyEagerRewind() => new GreedyAgent(Data, RewindPolicy.Eager);
        public static IAgent Random(int seed) => new RandomAgent(seed);

        /// 한 카드만 넣은 덱. `DominanceChecker` 가 쓴다.
        public static List<string> MonoDeck(string cardId, int size)
        {
            var d = new List<string>();
            for (int i = 0; i < size; i++) d.Add(cardId);
            return d;
        }

        /// 모든 카드를 두 장씩 넣은 덱. `DeadCardChecker` 가 쓴다 —
        /// 카드가 손에 올 기회를 고르게 주지 않으면 "채택되지 않았다"와 "뽑히지 않았다"가 섞인다.
        public static List<string> FullPoolDeck(int copies)
        {
            var d = new List<string>();
            foreach (var c in Data.Cards)
                for (int i = 0; i < copies; i++) d.Add(c.id);
            return d;
        }
    }
}
