using System;
using System.Collections.Generic;
using System.Text;

namespace DeckAttrition
{
    public sealed class RunResult
    {
        public bool Won;
        public int BattlesCleared;
        public int BattlesTotal;
        public int PlayerHp;

        /// 회차 전체에서 활자를 찍은 총 횟수.
        public int UsesSpent;
        /// 다 닳아 녹은 활자들. 이게 비어 있으면 규칙이 장식이다 (`AttritionReachable`).
        public List<string> Exhausted = new List<string>();

        public TypeCase Case;
        public string Transcript;

        /// 보스 전투가 시작될 때 상자에 남아 있던 총 횟수. -1 이면 보스까지 못 갔다.
        public int UsesLeftAtBoss = -1;
        /// 보스 전투가 시작될 때의 체력. -1 이면 보스까지 못 갔다.
        public int HpAtBoss = -1;
    }

    /// <summary>
    /// 회차 하나. 전투 몇 개 + 보스.
    ///
    /// <b>이 클래스가 규칙의 시간축이다.</b> <see cref="TypeCase"/> 를 회차 시작에 한 번 만들고
    /// 모든 전투가 그것을 <b>같이</b> 쓴다. 전투마다 새로 만들면 "한 판에서 닳는" 평범한 규칙이 되고,
    /// 그 순간 이 PoC는 존재 이유가 없어진다.
    ///
    /// 체력도 전투를 넘어 이어진다. 이 둘이 함께 있어야 "지금 쓸까 아껴 둘까"가 양쪽에서 조인다 —
    /// 아끼면 체력으로 내고, 쓰면 다음 전투에서 낸다.
    /// </summary>
    public static class RunEngine
    {
        public static RunResult Play(GameData data, IAgent agent, int seed,
                                     IEnumerable<string> startingDeck = null)
        {
            var run = data.Run;
            var deck = new List<string>(startingDeck ?? run.startingDeck);
            var rng = new Rng(seed);                 // 회차 난수: 보상 선택 · 전투 씨드 파생
            var typeCase = new TypeCase(data);       // ★ 회차에 하나. 전투가 아니라 여기 산다
            var sb = new StringBuilder();

            var order = new List<string>(run.battles);
            order.Add(run.boss);

            int hp = run.playerMaxHp;
            int cleared = 0;
            int usesLeftAtBoss = -1, hpAtBoss = -1;

            sb.Append("run ").Append(run.id).Append(" seed=").Append(seed)
              .Append(" deck=").Append(string.Join(",", deck)).Append('\n');

            for (int i = 0; i < order.Count; i++)
            {
                int battleSeed = seed + 1013 * (i + 1) + rng.Range(9973);
                bool isBoss = i == order.Count - 1;
                if (isBoss) { usesLeftAtBoss = UsesLeftIn(data, typeCase); hpAtBoss = hp; }
                var b = new Battle(data, typeCase, data.Enemy(order[i]), deck, hp, run.playerMaxHp,
                                   battleSeed, run.handSize, run.energyPerTurn, run.maxRoundsPerBattle,
                                   i, order.Count - 1 - i, isBoss);
                b.RunToEnd(agent);
                sb.Append(b.Transcript());

                // 녹은 활자는 회차 덱에서도 빠진다. 다음 전투에 다시 나타나면 규칙이 한 판짜리가 된다.
                foreach (var gone in b.SpentHere) deck.RemoveAll(x => x == gone);

                if (b.Outcome != BattleOutcome.PlayerWon)
                {
                    sb.Append("run lost at ").Append(order[i])
                      .Append(" case=").Append(typeCase.Describe()).Append('\n');
                    return Finish(false, cleared, order.Count, Math.Max(0, b.Player.Hp), typeCase, sb,
                                  usesLeftAtBoss, hpAtBoss);
                }

                cleared++;
                hp = Math.Min(run.playerMaxHp, b.Player.Hp + run.healAfterBattle);

                if (!isBoss && run.rewardPool != null && run.rewardPool.Length > 0)
                {
                    for (int r = 0; r < run.rewardsPerBattle; r++)
                        GiveReward(data, typeCase, deck, run.rewardPool[rng.Range(run.rewardPool.Length)], sb);
                }
            }

            sb.Append("run won hp=").Append(hp).Append(" case=").Append(typeCase.Describe()).Append('\n');
            return Finish(true, cleared, order.Count, hp, typeCase, sb, usesLeftAtBoss, hpAtBoss);
        }

        /// <summary>
        /// 보상. <b>덱에 장을 더하는 것이 아니라 활자를 주조하는 것</b>이 기본이다 —
        /// 이 게임에서 모자란 것은 카드 장 수가 아니라 남은 횟수다.
        ///
        /// 이미 녹은 활자는 보상으로도 돌아오지 않는다. 돌아오면 "다 썼다"가 되돌릴 수 있는 일이 되고,
        /// 되돌릴 수 있으면 아끼고 말고가 없다.
        /// </summary>
        static void GiveReward(GameData data, TypeCase tc, List<string> deck, string rewardId, StringBuilder sb)
        {
            var card = data.Card(rewardId);
            if (!card.IsConsumable)
            {
                deck.Add(rewardId);
                sb.Append("reward ").Append(rewardId).Append(" (상용 활자 한 벌)\n");
                return;
            }

            if (tc.IsSpent(rewardId))
            {
                sb.Append("reward ").Append(rewardId).Append(" (이미 녹았다 — 받을 것이 없다)\n");
                return;
            }

            if (deck.Contains(rewardId))
            {
                tc.Recast(rewardId, data.Run.rewardRecastUses);
                sb.Append("reward ").Append(rewardId).Append(" (다시 주조 +")
                  .Append(data.Run.rewardRecastUses).Append("회 · 선명도 회복)\n");
            }
            else
            {
                deck.Add(rewardId);
                sb.Append("reward ").Append(rewardId).Append(" (새 활자 · ")
                  .Append(tc.UsesLeft(rewardId)).Append("회)\n");
            }
        }

        /// 상자에 남아 있는 총 횟수. 덱에 없어도 상자에 있으면 센다 — 보상으로 들어올 수 있다.
        static int UsesLeftIn(GameData data, TypeCase tc)
        {
            int n = 0;
            foreach (var c in data.Cards) if (c.IsConsumable) n += Math.Max(0, tc.UsesLeft(c.id));
            return n;
        }

        static RunResult Finish(bool won, int cleared, int total, int hp, TypeCase tc, StringBuilder sb,
                                int usesLeftAtBoss = -1, int hpAtBoss = -1)
        {
            var r = new RunResult
            {
                Won = won, BattlesCleared = cleared, BattlesTotal = total, PlayerHp = hp,
                UsesSpent = tc.TotalUsesSpent, Case = tc, Transcript = sb.ToString(),
                UsesLeftAtBoss = usesLeftAtBoss, HpAtBoss = hpAtBoss
            };
            r.Exhausted.AddRange(tc.Exhausted);
            return r;
        }

        /// <summary>몬테카를로. 승률을 백분율 정수로 돌려준다 — 부동소수를 남기지 않는다.</summary>
        public static int WinRatePct(GameData data, Func<int, IAgent> makeAgent, int seedBase, int runs,
                                     IEnumerable<string> startingDeck = null)
        {
            int wins = 0;
            for (int i = 0; i < runs; i++)
                if (Play(data, makeAgent(seedBase + i), seedBase + i, startingDeck).Won) wins++;
            return wins * 100 / runs;
        }

        /// <summary>
        /// 정책 하나를 여러 씨드로 돌려 모은 값. `HoardingIsNotOptimal` 과
        /// `AttritionReachable` 이 같은 것을 두 번 재지 않도록 한 번에 뽑는다.
        /// </summary>
        public sealed class PolicyReport
        {
            public string Policy;
            public int Runs;
            public int Wins;
            public int WinRatePct;
            /// 회차당 활자를 찍은 평균 횟수 ×100 (정수로 유지하려고 백 배로 들고 있다).
            public int UsesPerRunX100;
            /// 활자가 최소 하나 녹은 회차의 비율.
            public int ExhaustedRunPct;
            /// 이긴 회차의 평균 남은 체력.
            public int AvgHpOnWin;
            /// 회차당 깬 전투 수 ×100.
            public int ClearedX100;
            /// 진 회차 중 <b>보스에서</b> 진 비율. 어디서 무너지는지가 정책의 지문이다.
            public int LostAtBossPct;
            /// 보스 앞에 섰을 때 상자에 남아 있던 횟수 ×100. <b>정책을 가르는 실제 값이다.</b>
            public int UsesLeftAtBossX100;
            /// 보스 앞에 섰을 때의 체력 ×100.
            public int HpAtBossX100;
            /// 보스까지 간 회차의 비율.
            public int ReachedBossPct;
        }

        public static PolicyReport Measure(GameData data, AttritionPolicy policy, int seedBase, int runs,
                                           IEnumerable<string> startingDeck = null)
        {
            int wins = 0, uses = 0, exhaustedRuns = 0, hpSum = 0, cleared = 0, losses = 0, lostAtBoss = 0;
            int reached = 0, leftAtBoss = 0, hpAtBoss = 0;
            for (int i = 0; i < runs; i++)
            {
                var agent = new TypesetterAgent(data, policy);
                var r = Play(data, agent, seedBase + i, startingDeck);
                if (r.Won) { wins++; hpSum += r.PlayerHp; }
                else { losses++; if (r.BattlesCleared == r.BattlesTotal - 1) lostAtBoss++; }
                cleared += r.BattlesCleared;
                uses += r.UsesSpent;
                if (r.UsesLeftAtBoss >= 0) { reached++; leftAtBoss += r.UsesLeftAtBoss; hpAtBoss += r.HpAtBoss; }
                if (r.Exhausted.Count > 0) exhaustedRuns++;
            }
            return new PolicyReport
            {
                Policy = policy.ToString(),
                Runs = runs,
                Wins = wins,
                WinRatePct = wins * 100 / runs,
                UsesPerRunX100 = uses * 100 / runs,
                ExhaustedRunPct = exhaustedRuns * 100 / runs,
                AvgHpOnWin = wins == 0 ? -1 : hpSum / wins,
                ClearedX100 = cleared * 100 / runs,
                LostAtBossPct = losses == 0 ? -1 : lostAtBoss * 100 / losses,
                ReachedBossPct = reached * 100 / runs,
                UsesLeftAtBossX100 = reached == 0 ? -1 : leftAtBoss * 100 / reached,
                HpAtBossX100 = reached == 0 ? -1 : hpAtBoss * 100 / reached
            };
        }
    }
}
