// 표본 하나의 값, 그리고 "심으면 얼마나 이득인가".
//
// 정책이 카드 이름을 외우지 않게 하려고 값을 **데이터에서 계산한다.**
// cards.json 의 effects 와 sown 만 보고 셈하므로, 카드를 고치면 정책이 따라 바뀐다.
// 전부 정수다 — 부동소수를 쓰면 같은 씨드에서 다른 결정이 나올 수 있다.
using System.Collections.Generic;

namespace HybridSownDeck
{
    public static class CardValue
    {
        /// <summary>전투에서 대략 얼마나 쓸모 있는가. 정확할 필요는 없다 — 순서만 맞으면 된다.</summary>
        public static int Combat(GameData d, CardDef c)
        {
            int burnTurns = d.Balance.battle.burnTurnsAssumed;
            int v = 0;
            foreach (var e in c.effects)
            {
                switch (e.type)
                {
                    case "damage": v += e.amount * 10; break;
                    case "block":  v += e.amount * 9;  break;
                    case "burn":   v += e.amount * burnTurns * 8; break;
                }
            }
            return v;
        }

        /// <summary>심었을 때 다음 해에 돌아오는 값. seedfall 은 씨앗을 값으로 환산한다.</summary>
        public static int SownReturn(GameData d, CardDef c)
        {
            var rule = c.sown;
            if (rule == null) return 0;
            if (rule.YieldsSeeds) return (rule.seeds > 0 ? rule.seeds : 0) * d.Balance.sowing.seedValueForSowing;
            if (!rule.YieldsCard || string.IsNullOrEmpty(rule.becomes) || !d.HasCard(rule.becomes)) return 0;
            int n = rule.amount < 1 ? 1 : rule.amount;
            return Combat(d, d.Card(rule.becomes)) * n;
        }

        /// <summary>
        /// 심어서 얻는 것 − 지금 쥐고 있는 것. 양수면 "안 쓰고 심는 편이 낫다"는 뜻이다.
        /// 이 값이 이 PoC 의 결정을 만든다: 어린싹은 크게 양수(자란다), 가시덤불은 음수(갈라져 흩어진다).
        /// </summary>
        public static int SowGain(GameData d, CardDef c) => SownReturn(d, c) - Combat(d, c);

        /// <summary>심을 표본을 고르는 순서. 이득이 큰 것 먼저, 같으면 sortWeight 로 가른다(결정적).</summary>
        public static List<Specimen> RankForSowing(GameData d, List<Specimen> unplayed)
        {
            var list = new List<Specimen>(unplayed);
            list.Sort((a, b) =>
            {
                int ga = SowGain(d, d.Card(a.CardId)), gb = SowGain(d, d.Card(b.CardId));
                if (ga != gb) return gb.CompareTo(ga);
                var ca = d.Card(a.CardId); var cb = d.Card(b.CardId);
                if (ca.sortWeight != cb.sortWeight) return ca.sortWeight.CompareTo(cb.sortWeight);
                return string.CompareOrdinal(a.LineageId, b.LineageId);
            });
            return list;
        }
    }
}
