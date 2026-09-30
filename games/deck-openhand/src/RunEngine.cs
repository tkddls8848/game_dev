using System;
using System.Collections.Generic;
using System.Text;

namespace DeckOpenhand
{
    public sealed class RunResult
    {
        public bool Won;
        public int BattlesCleared;
        public int BattlesTotal;
        public int PlayerHp;
        public int IntentsSkipped;
        public int IntentsSwapped;
        public string Transcript;
    }

    /// <summary>
    /// 회차 하나. 전투 몇 개 + 보스.
    /// 화면이 없어도 끝까지 돈다 — 그게 이 저장소가 하루에 PoC 스무 개를 보는 이유다.
    /// </summary>
    public static class RunEngine
    {
        public static RunResult Play(GameData data, IAgent agent, int seed,
                                     IEnumerable<string> startingDeck = null)
        {
            var run = data.Run;
            var deck = new List<string>(startingDeck ?? run.startingDeck);
            var rng = new Rng(seed);                 // 회차 난수: 보상 선택 · 전투 씨드 파생
            var sb = new StringBuilder();

            var order = new List<string>(run.battles);
            order.Add(run.boss);

            int hp = run.playerMaxHp;
            int cleared = 0, skipped = 0, swapped = 0;

            sb.Append("run ").Append(run.id).Append(" seed=").Append(seed)
              .Append(" deck=").Append(string.Join(",", deck)).Append('\n');

            for (int i = 0; i < order.Count; i++)
            {
                int battleSeed = seed + 1013 * (i + 1) + rng.Range(9973);
                var b = new Battle(data, data.Enemy(order[i]), deck, hp, run.playerMaxHp, battleSeed,
                                   run.handSize, run.energyPerTurn, run.peekWindow, run.maxRoundsPerBattle);
                b.RunToEnd(agent);
                sb.Append(b.Transcript());
                skipped += b.IntentsSkipped;
                swapped += b.IntentsSwapped;

                if (b.Outcome != BattleOutcome.PlayerWon)
                {
                    sb.Append("run lost at ").Append(order[i]).Append('\n');
                    return new RunResult
                    {
                        Won = false, BattlesCleared = cleared, BattlesTotal = order.Count,
                        PlayerHp = Math.Max(0, b.Player.Hp),
                        IntentsSkipped = skipped, IntentsSwapped = swapped,
                        Transcript = sb.ToString()
                    };
                }

                cleared++;
                hp = Math.Min(run.playerMaxHp, b.Player.Hp + run.healAfterBattle);

                if (i < order.Count - 1 && run.rewardPool != null && run.rewardPool.Length > 0)
                {
                    for (int r = 0; r < run.rewardsPerBattle; r++)
                    {
                        string reward = run.rewardPool[rng.Range(run.rewardPool.Length)];
                        deck.Add(reward);
                        sb.Append("reward ").Append(reward).Append('\n');
                    }
                }
            }

            sb.Append("run won hp=").Append(hp).Append('\n');
            return new RunResult
            {
                Won = true, BattlesCleared = cleared, BattlesTotal = order.Count,
                PlayerHp = hp, IntentsSkipped = skipped, IntentsSwapped = swapped,
                Transcript = sb.ToString()
            };
        }

        /// <summary>몬테카를로. 승률을 백분율 정수로 돌려준다 — 부동소수를 남기지 않는다.</summary>
        public static int WinRatePct(GameData data, Func<int, IAgent> makeAgent, int seedBase, int runs,
                                    IEnumerable<string> startingDeck = null)
        {
            int wins = 0;
            for (int i = 0; i < runs; i++)
            {
                var agent = makeAgent(seedBase + i);
                if (Play(data, agent, seedBase + i, startingDeck).Won) wins++;
            }
            return wins * 100 / runs;
        }
    }
}
