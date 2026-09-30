// 카드 놓기 정책. 검사기가 "잘 두는 사람"과 "아무렇게나 두는 사람"을 나눠 재기 위한 축이다.
using System.Collections.Generic;

namespace HybridHarvestDeck
{
    /// <summary>무작위 플레이. WinRateBand 의 아래쪽 기준선.</summary>
    public sealed class RandomPlayPolicy : IPlayPolicy
    {
        readonly Rng _rng;
        public RandomPlayPolicy(Rng rng) { _rng = rng; }

        public int ChooseCard(BattleView v)
        {
            var playable = new List<int>();
            for (int i = 0; i < v.Hand.Count; i++)
                if (v.Data.Card(v.Hand[i]).cost <= v.Energy) playable.Add(i);
            if (playable.Count == 0) return -1;
            // 4분의 1 확률로 그냥 턴을 끝낸다 — 서투른 플레이를 흉내 낸다
            if (_rng.Next(4) == 0) return -1;
            return playable[_rng.Next(playable.Count)];
        }

        public int ChooseDiscard(BattleView v) => v.Hand.Count == 0 ? -1 : _rng.Next(v.Hand.Count);
    }

    /// <summary>
    /// 규칙 기반의 준최적 플레이. 결정적이다(난수를 쓰지 않는다).
    /// 치명타 → 방어 → 화상 → 수확 → 그 밖의 순으로 값을 매기고 가장 높은 것을 놓는다.
    /// </summary>
    public sealed class SkilledPlayPolicy : IPlayPolicy
    {
        readonly int _burnTurns;
        public SkilledPlayPolicy(GameData data) { _burnTurns = data.Balance.battle.burnTurnsAssumed; }

        public int ChooseCard(BattleView v)
        {
            int best = -1, bestScore = 0;
            for (int i = 0; i < v.Hand.Count; i++)
            {
                var card = v.Data.Card(v.Hand[i]);
                if (card.cost > v.Energy) continue;
                int score = Score(card, v);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        int Score(CardDef card, BattleView v)
        {
            int score = 0;
            bool draws = false;
            int threat = v.IncomingDamage - v.PlayerBlock;
            foreach (var e in card.effects)
            {
                switch (e.type)
                {
                    case "damage":
                        score += Kill(e.amount, v) ? 1000 : e.amount * 10;
                        break;
                    case "damage_per_remaining":
                    {
                        int amount = e.amount * (v.RemainingCards - 1);
                        score += Kill(amount, v) ? 1000 : amount * 9;
                        break;
                    }
                    case "block":
                        // 막을 것이 없으면 방벽은 값이 없다. 죽을 판이면 무엇보다 값이 크다
                        if (threat <= 0) score += 1;
                        else score += System.Math.Min(e.amount, threat) * (v.PlayerHp <= threat ? 60 : 11);
                        break;
                    case "burn":
                        // 화상은 남은 턴 동안 계속 들어온다. 이미 붙어 있으면 값이 준다
                        score += e.amount * _burnTurns * (v.EnemyBurn > 0 ? 2 : 4);
                        break;
                    case "draw":
                        draws = true;
                        score += e.amount * 12;
                        break;
                    case "exhaust":
                        // 덱을 얇게 만든다. 다만 '수확'은 남은 장수를 먹으므로 공짜가 아니다
                        score += e.amount * 5;
                        break;
                    case "discard":
                        score -= e.amount * 3;
                        break;
                }
            }
            // 에너지가 남는 동안 돌리는 것이 순서상 이득이다
            if (draws && v.Energy - card.cost >= 1) score += 10;
            return score * 2 / (card.cost + 1);
        }

        static bool Kill(int amount, BattleView v) => amount >= v.EnemyHp + v.EnemyBlock;

        public int ChooseDiscard(BattleView v)
        {
            int best = -1, bestWeight = int.MaxValue;
            for (int i = 0; i < v.Hand.Count; i++)
            {
                var card = v.Data.Card(v.Hand[i]);
                // 지금 놓을 수 있는 0코스트 카드는 버리지 않는다
                int weight = card.sortWeight;
                if (weight < bestWeight) { bestWeight = weight; best = i; }
            }
            return best;
        }
    }
}
