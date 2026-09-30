// 표본을 놓는 정책.
//
// 이 PoC 의 축은 하나다: **지금 쓸까, 남겨 두어 심을까**(sowWeight).
//   0    아무것도 남기지 않는다 — 이번 밤에 쓸 수 있는 것은 다 쓴다
//   60   준최적 — 심으면 크게 자라는 표본만, 심을 칸만큼만 남긴다
//   600  아끼기만 한다 — 자랄 표본을 끝내 쓰지 않는다. 그래서 밤에 진다
//
// hybrid-siege-seasons 의 wearWeight 와 겉모양이 비슷하나 뜻이 다르다.
// 저쪽은 '쓰면 닳아 없어진다'(손실 회피)이고, 이쪽은 '안 쓰면 더 좋은 것이 된다'(투자)다.
// 그래서 여기서는 **심을 칸이 없으면 아끼는 값이 0 이 된다** — 저쪽에는 그런 조건이 없다.
using System.Collections.Generic;

namespace HybridSownDeck
{
    public sealed class RandomPlayPolicy : IPlayPolicy
    {
        readonly Rng _rng;
        public RandomPlayPolicy(Rng rng) { _rng = rng; }

        public int ChooseCard(NightView v)
        {
            var playable = new List<int>();
            for (int i = 0; i < v.Hand.Count; i++)
                if (v.Data.Card(v.Hand[i].CardId).cost <= v.Energy) playable.Add(i);
            if (playable.Count == 0) return -1;
            if (_rng.Next(4) == 0) return -1;
            return playable[_rng.Next(playable.Count)];
        }
    }

    /// <summary>규칙 기반의 결정적 플레이. sowWeight 로 "남겨서 심는 정도"를 바꾼다.</summary>
    public sealed class SowAwarePlayPolicy : IPlayPolicy
    {
        readonly GameData _d;
        readonly int _burnTurns;
        readonly int _sowWeight;

        public SowAwarePlayPolicy(GameData data, int sowWeight)
        {
            _d = data;
            _burnTurns = data.Balance.battle.burnTurnsAssumed;
            _sowWeight = sowWeight;
        }

        public static SowAwarePlayPolicy Skilled(GameData d) => new SowAwarePlayPolicy(d, d.Balance.battle.sowWeightSkilled);
        public static SowAwarePlayPolicy Spender(GameData d) => new SowAwarePlayPolicy(d, d.Balance.battle.sowWeightSpender);
        public static SowAwarePlayPolicy Hoarder(GameData d) => new SowAwarePlayPolicy(d, d.Balance.battle.sowWeightHoarder);

        public int ChooseCard(NightView v)
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

        int Score(CardDef card, Specimen inst, NightView v)
        {
            int score = 0;
            bool lethal = false;
            int threat = v.IncomingDamage - v.PlayerBlock;

            foreach (var e in card.effects)
            {
                switch (e.type)
                {
                    case "damage":
                        if (Kill(e.amount, v)) { lethal = true; score += 1000; } else score += e.amount * 10;
                        break;
                    case "block":
                        if (threat <= 0) score += 1;
                        else score += System.Math.Min(e.amount, threat) * (v.PlayerHp <= threat ? 60 : 11);
                        break;
                    case "burn":
                        score += e.amount * _burnTurns * (v.EnemyBurn > 0 ? 2 : 4);
                        break;
                }
            }

            // ── 남겨서 심을 값 ──────────────────────────────────────────
            // 이미 놓은 표본은 밭으로 돌아갈 수 없으므로 아낄 이유가 없다.
            // 죽을 판이거나 지금 끝낼 수 있으면 아끼지 않는다 — 아껴서 지면 아무 소용이 없다.
            if (!inst.Played && !lethal && v.PlayerHp > threat && score > 0 && v.SowPlotsAvailable > 0)
            {
                int gain = CardValue.SowGain(_d, card);
                if (gain > 0)
                {
                    // 심을 칸만큼만 남긴다 — **종류별이 아니라 전부 합해서**, 그리고 **좋은 것부터**.
                    // 종류마다 칸 수만큼 남기면 칸이 셋인데 아홉 장을 쥐고 밤에 진다.
                    // 그래서 세는 것은 "이만큼 좋거나 더 좋은 표본이 이미 몇 장 남아 있는가"다.
                    // 그 수가 칸 수에 닿았으면 이 표본은 심을 자리가 없으니 아끼지 않고 쓴다 —
                    // 반대로 지금까지 남은 것이 시시한 것들뿐이면 좋은 표본은 아껴 둔다.
                    int spareAtLeastAsGood = 0;
                    foreach (var kv in v.UnplayedByCard)
                        if (CardValue.SowGain(_d, _d.Card(kv.Key)) >= gain) spareAtLeastAsGood += kv.Value;
                    if (spareAtLeastAsGood < v.SowPlotsAvailable)
                    {
                        int denom = 100 + _sowWeight * gain / 100;
                        score = denom <= 0 ? score : score * 100 / denom;
                    }
                }
            }

            if (score <= 0) return 0;
            return score * 2 / (card.cost + 1);
        }

        static bool Kill(int amount, NightView v) => amount >= v.EnemyHp + v.EnemyBlock;
    }
}
