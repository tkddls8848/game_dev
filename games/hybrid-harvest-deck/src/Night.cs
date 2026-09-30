// 겨울 밤: 3전투 + 보스. 덱은 그 해 밭에서 나온 것뿐이다.
using System.Collections.Generic;
using System.Text;

namespace HybridHarvestDeck
{
    public sealed class NightResult
    {
        public bool Won;
        public int BattlesCleared;
        public int BattlesTotal;
        public int PlayerHpLeft;
        public bool EmptyDeck;
    }

    public sealed class NightSim
    {
        readonly GameData _data;
        readonly BattleSim _battle;

        public NightSim(GameData data)
        {
            _data = data;
            _battle = new BattleSim(data);
        }

        /// <summary>
        /// year 는 1부터. 적 체력은 해마다 백분율 정수로 커지고 나머지는 누적한다(부동소수 금지).
        /// </summary>
        public NightResult Run(IReadOnlyList<string> deck, IPlayPolicy policy, Rng rng,
                               int year = 1, StringBuilder transcript = null)
        {
            var order = new List<string>(_data.Night.battles) { _data.Night.boss };
            var result = new NightResult { BattlesTotal = order.Count };

            if (deck.Count == 0)
            {
                result.EmptyDeck = true;
                result.PlayerHpLeft = _data.Night.playerHp;
                transcript?.Append("N:empty\n");
                return result;
            }

            int scalePct = 100 + (year - 1) * _data.Balance.night.hpGrowthPctPerYear;
            if (scalePct > _data.Balance.night.hpScaleMaxPct) scalePct = _data.Balance.night.hpScaleMaxPct;
            var acc = new PercentAccumulator();

            int hp = _data.Night.playerHp;
            foreach (var enemyId in order)
            {
                var baseEnemy = _data.Enemy(enemyId);
                var enemy = scalePct == 100 ? baseEnemy : Scaled(baseEnemy, scalePct, acc);

                var r = _battle.Run(enemy, deck, hp, policy, rng, transcript);
                hp = r.PlayerHpLeft;
                if (!r.PlayerWon)
                {
                    result.PlayerHpLeft = hp;
                    transcript?.Append("N:lose\n");
                    return result;
                }
                result.BattlesCleared++;
                hp += _data.Night.healBetweenBattles;
            }

            result.Won = true;
            result.PlayerHpLeft = hp;
            transcript?.Append("N:win").Append(hp).Append('\n');
            return result;
        }

        static EnemyDef Scaled(EnemyDef src, int pct, PercentAccumulator acc)
        {
            return new EnemyDef
            {
                id = src.id,
                nameKo = src.nameKo,
                nameEn = src.nameEn,
                glyph = src.glyph,
                hp = acc.Apply(src.hp, pct),
                pattern = src.pattern
            };
        }
    }
}
