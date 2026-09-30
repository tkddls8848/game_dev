// 카드 놓기 정책.
//
// 이 PoC 의 축은 하나다: **얼마나 아끼는가**(wearWeight).
//   0   아무것도 아끼지 않는다 — 좋은 카드를 첫 봄에 다 태운다
//   10  준최적 — 귀한 카드를 값이 클 때만 쓴다
//   140 아끼기만 한다 — 강한 카드를 끝내 쓰지 않는다. 계획서가 경고한 '지루한 최적해'
// DominanceChecker 가 이 축을 훑어 극단이 이기지 않는지 본다.
using System.Collections.Generic;

namespace HybridSiegeSeasons
{
    public sealed class RandomPlayPolicy : IPlayPolicy
    {
        readonly Rng _rng;
        public RandomPlayPolicy(Rng rng) { _rng = rng; }

        public int ChooseCard(BattleView v)
        {
            var playable = new List<int>();
            for (int i = 0; i < v.Hand.Count; i++)
                if (v.Data.Card(v.Hand[i].CardId).cost <= v.Energy) playable.Add(i);
            if (playable.Count == 0) return -1;
            if (_rng.Next(4) == 0) return -1;
            return playable[_rng.Next(playable.Count)];
        }

        public int ChooseExhaust(BattleView v) => v.Hand.Count == 0 ? -1 : _rng.Next(v.Hand.Count);
    }

    /// <summary>
    /// 규칙 기반의 결정적 플레이. wearWeight 로 "아끼는 정도"를 바꾼다.
    /// 귀한 카드(durability 가 작은 카드)일수록 한 번 쓰는 값이 비싸다.
    /// </summary>
    public sealed class WearAwarePlayPolicy : IPlayPolicy
    {
        readonly int _burnTurns;
        readonly int _wearWeight;

        public WearAwarePlayPolicy(GameData data, int wearWeight)
        {
            _burnTurns = data.Balance.battle.burnTurnsAssumed;
            _wearWeight = wearWeight;
        }

        public static WearAwarePlayPolicy Skilled(GameData d) => new WearAwarePlayPolicy(d, d.Balance.battle.wearWeightSkilled);
        public static WearAwarePlayPolicy Hoarding(GameData d) => new WearAwarePlayPolicy(d, d.Balance.battle.wearWeightHoard);
        public static WearAwarePlayPolicy Reckless(GameData d) => new WearAwarePlayPolicy(d, d.Balance.battle.wearWeightReckless);

        public int ChooseCard(BattleView v)
        {
            int best = -1, bestScore = 0;
            for (int i = 0; i < v.Hand.Count; i++)
            {
                var card = v.Data.Card(v.Hand[i].CardId);
                if (card.cost > v.Energy) continue;
                int score = Score(card, v.Hand[i], v);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        int Score(CardDef card, CardInstance inst, BattleView v)
        {
            int score = 0;
            bool draws = false;
            bool lethal = false;
            int threat = v.IncomingDamage - v.PlayerBlock;

            foreach (var e in card.effects)
            {
                switch (e.type)
                {
                    case "damage":
                        if (Kill(e.amount, v)) { lethal = true; score += 1000; } else score += e.amount * 10;
                        break;
                    case "damage_per_remaining":
                    {
                        int amount = e.amount * (v.RemainingCards - 1);
                        if (Kill(amount, v)) { lethal = true; score += 1000; } else score += amount * 9;
                        break;
                    }
                    case "block":
                        if (threat <= 0) score += 1;
                        else score += System.Math.Min(e.amount, threat) * (v.PlayerHp <= threat ? 60 : 11);
                        break;
                    case "burn":
                        score += e.amount * _burnTurns * (v.EnemyBurn > 0 ? 2 : 4);
                        break;
                    case "draw":
                        draws = true;
                        score += e.amount * 12;
                        break;
                    case "repair":
                    {
                        // 되살릴 수 있는 횟수만큼만 값이 있다. 성한 카드뿐이면 손질은 값이 없다
                        int mendable = 0;
                        foreach (var h in v.Hand)
                        {
                            if (h.CardId == card.id) continue;
                            int gap = h.MaxUses - h.Uses;
                            if (gap > mendable) mendable = gap;
                        }
                        score += (mendable < e.amount ? mendable : e.amount) * 14;
                        break;
                    }
                    case "exhaust":
                        score += e.amount * 8;
                        break;
                }
            }
            if (draws && v.Energy - card.cost >= 1) score += 10;

            // ── 닳는 값 ────────────────────────────────────────────────
            // 한 번 쓰면 그 카드 수명의 1/내구도를 태운다. 그래서 값은 **절대값이 아니라 비율**이다 —
            // 절대값으로 깎으면 귀한 카드가 어떤 상황에서도 안 나오고, 그러면 죽은 카드가 된다.
            // 죽을 판이거나 지금 끝낼 수 있으면 아끼지 않는다. 아껴서 지면 아무 소용이 없다.
            if (!lethal && v.PlayerHp > threat && score > 0)
            {
                int dur = card.durability < 1 ? 1 : card.durability;
                int keepPct = 100 * dur - _wearWeight;
                // 그 카드가 덱에 얼마 남지 않았으면 더 아낀다
                int left = v.DeckUsesByCard.TryGetValue(card.id, out var u) ? u : 0;
                if (left <= dur) keepPct -= _wearWeight / 2;
                score = keepPct <= 0 ? 0 : score * keepPct / (100 * dur);
            }

            if (score <= 0) return 0;
            return score * 2 / (card.cost + 1);
        }

        static bool Kill(int amount, BattleView v) => amount >= v.EnemyHp + v.EnemyBlock;

        /// <summary>
        /// 없앨 카드: **가장 닳은 것부터.** 어차피 곧 부서질 카드를 지금 빼면 손해가 거의 없고,
        /// 덱은 그만큼 얇아진다. 성한 카드를 없애면 '정리'가 덫이 된다 — 검사기가 그것을 잡았다.
        /// </summary>
        public int ChooseExhaust(BattleView v)
        {
            int best = -1, bestKey = int.MaxValue;
            for (int i = 0; i < v.Hand.Count; i++)
            {
                var card = v.Data.Card(v.Hand[i].CardId);
                int key = v.Hand[i].Uses * 100 + card.sortWeight;   // 남은 횟수가 먼저, 값이 나중
                if (key < bestKey) { bestKey = key; best = i; }
            }
            return best;
        }
    }
}
