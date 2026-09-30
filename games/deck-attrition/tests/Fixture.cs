using System.Collections.Generic;
using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 테스트가 공유하는 데이터. 파일을 한 번만 읽는다.
    /// 에이전트는 매번 새로 만든다 — 채택 통계와 정책 상태를 들고 있기 때문이다.
    /// </summary>
    public static class Fix
    {
        static GameData _data;

        public static GameData Data => _data ?? (_data = GameData.Load(TestContext.CurrentContext.TestDirectory));

        public static TypesetterAgent Measured() => new TypesetterAgent(Data, AttritionPolicy.Measured);
        public static TypesetterAgent Hoard() => new TypesetterAgent(Data, AttritionPolicy.Hoard);
        public static TypesetterAgent SpendNow() => new TypesetterAgent(Data, AttritionPolicy.SpendNow);
        public static IAgent Random(int seed) => new RandomAgent(seed);

        /// 한 카드만 넣은 덱. `DominanceChecker` 가 쓴다.
        public static List<string> MonoDeck(string cardId, int size)
        {
            var d = new List<string>();
            for (int i = 0; i < size; i++) d.Add(cardId);
            return d;
        }

        /// <summary>
        /// 시작 덱의 <b>기본 활자</b>에 한 카드를 잔뜩 쌓은 덱. `DominanceChecker` 가 쓴다.
        /// 한 카드만 넣은 덱은 어떤 카드로도 회차를 못 넘겨 전부 0%가 되고, 그러면
        /// "지배 전략이 없다"가 아니라 "아무것도 재지 않았다"가 된다.
        /// </summary>
        public static List<string> StackedDeck(string cardId, int copies)
        {
            var d = new List<string>();
            foreach (var id in Data.Run.startingDeck)
                if (!Data.Card(id).IsConsumable) d.Add(id);
            for (int i = 0; i < copies; i++) d.Add(cardId);
            return d;
        }

        /// <summary>
        /// 모든 카드를 <paramref name="copies"/> 장씩 넣은 덱. `DeadCardChecker` 가 쓴다 —
        /// 카드가 손에 올 기회를 고르게 주지 않으면 "채택되지 않았다"와 "뽑히지 않았다"가 섞인다.
        /// </summary>
        public static List<string> FullPoolDeck(int copies)
        {
            var d = new List<string>();
            foreach (var c in Data.Cards)
                for (int i = 0; i < copies; i++) d.Add(c.id);
            return d;
        }

        /// 회차 하나를 돌린다. 편의용.
        public static RunResult Run(IAgent agent, int seed, IEnumerable<string> deck = null) =>
            RunEngine.Play(Data, agent, seed, deck);
    }
}
