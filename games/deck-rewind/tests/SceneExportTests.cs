using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DeckRewind.Tests
{
    /// <summary>
    /// 연출 목업이 읽을 장면을 <b>실제 시뮬레이션에서 뽑아</b> `presentation/scene.json` 으로 내보낸다.
    ///
    /// 설계 원칙 2: 손으로만 만들 수 있는 에셋을 만들지 않는다.
    /// 목업의 숫자를 손으로 적으면 데이터를 고칠 때마다 목업이 거짓이 되고,
    /// 거짓이 되는 순간 연출 비교가 아니라 그림 감상이 된다. 그래서 테스트가 굽는다.
    ///
    /// 장면은 <b>규칙이 실제로 일어난 판</b>이어야 한다. 되감기가 한 번도 안 나온 판을 그리면
    /// 목업이 이 PoC를 설명하지 못한다. 그래서 씨드를 순서대로 훑어 되감기가 나온 판을 고른다
    /// (훑는 순서가 고정이라 결과도 고정이다).
    /// </summary>
    [TestFixture]
    public class SceneExportTests
    {
        const int SeedFrom = 4242;
        const int SeedTries = 120;

        [Test]
        public void 연출_목업용_장면을_내보낸다()
        {
            var run = Fix.Data.Run;
            // 보스부터 본다. 되감기가 값을 하는 자리는 가장 아픈 전투다.
            var candidates = new List<string> { run.boss };
            candidates.AddRange(run.battles);

            Scene best = null;
            foreach (var enemyId in candidates)
            {
                for (int i = 0; i < SeedTries; i++)
                {
                    var s = Simulate(enemyId, SeedFrom + i);
                    if (s.TurnCount < 4) continue;
                    if (best == null || s.RewindsUsed > best.RewindsUsed) best = s;
                    if (s.RewindsUsed > 0 && s.Outcome == "PlayerWon") { best = s; break; }
                }
                if (best != null && best.RewindsUsed > 0 && best.Outcome == "PlayerWon") break;
            }

            Assert.That(best, Is.Not.Null, "쓸 만한 장면을 하나도 못 찾았다");
            Assert.That(best.RewindsUsed, Is.GreaterThan(0),
                "씨드 " + SeedTries + "개를 훑어도 되감기가 나오는 판이 없다 — 목업이 규칙을 못 보여 준다");

            string pocRoot = Directory.GetParent(GameData.FindDataDir(
                TestContext.CurrentContext.TestDirectory)).FullName;
            string outPath = Path.Combine(pocRoot, "presentation", "scene.json");

            var doc = new Dictionary<string, object>
            {
                ["_"] = "손으로 쓰지 말 것. tests/SceneExportTests.cs 가 실제 시뮬레이션에서 굽는다.",
                ["generatedBy"] = "deck-rewind.Tests / SceneExportTests",
                ["rule"] = "되감기 — 적의 의도는 보이지 않는다. 되감으면 적이 기억하고 더 세게 때린다.",
                ["seed"] = best.Seed,
                ["enemyId"] = best.EnemyId,
                ["enemyNameKo"] = best.EnemyNameKo,
                ["enemyPatternLength"] = best.EnemyPatternLength,
                ["memoryDamagePctPerStack"] = best.MemoryDamagePctPerStack,
                ["outcome"] = best.Outcome,
                ["rewindsUsed"] = best.RewindsUsed,
                ["finalMemory"] = best.FinalMemory,
                ["turns"] = best.Turns
            };
            File.WriteAllText(outPath, JsonConvert.SerializeObject(doc, Formatting.Indented) + "\n");

            TestContext.WriteLine("장면: " + best.EnemyNameKo + " · 씨드 " + best.Seed
                                  + " · " + best.TurnCount + "턴 · 되감기 " + best.RewindsUsed
                                  + "회 · " + best.Outcome);
            Assert.That(new FileInfo(outPath).Length, Is.GreaterThan(500));
        }

        // ---------- 한 판을 찍어 장면으로 만든다 ----------

        Scene Simulate(string enemyId, int seed)
        {
            var data = Fix.Data;
            var run = data.Run;
            var agent = Fix.Greedy();
            var battle = new Battle(data, data.Enemy(enemyId), run.startingDeck,
                                    run.playerMaxHp, run.playerMaxHp, seed,
                                    run.handSize, run.energyPerTurn, run.rewindCharges,
                                    run.maxRewindsPerBattle, run.maxRoundsPerBattle);

            var turns = new List<object>();
            bool turnAlreadyStarted = false;
            int guard = 0;

            while (battle.Outcome == BattleOutcome.InProgress && ++guard < 60)
            {
                if (!turnAlreadyStarted) battle.BeginPlayerTurn();
                turnAlreadyStarted = false;

                var handBefore = new List<string>(battle.Hand);
                int hpBefore = battle.Player.Hp;
                int enemyHpBefore = battle.Enemy.Hp;
                bool known = battle.TryPeekKnownIntent(out var knownType, out var knownAmount);
                int deckBefore = battle.Deck.Count, discardBefore = battle.Discard.Count;
                int round = battle.Round, memory = battle.Memory, charges = battle.RewindCharges;
                int energy = battle.Energy;

                int logMark = battle.Log.Count;
                agent.TakePlayerTurn(battle);
                var played = PlaysSince(battle, logMark);

                battle.EndPlayerTurn();
                int enemyHpAfterPlayer = battle.Enemy.Hp;
                int blockAfterPlayer = battle.Player.Block;

                string enemyAction = "(없음)";
                if (battle.Outcome == BattleOutcome.InProgress)
                {
                    int mark = battle.Log.Count;
                    battle.EnemyTurn();
                    enemyAction = FirstEnemyLine(battle, mark);
                }
                // 되감기 전의 값을 찍는다. 되감으면 체력이 되돌아오므로 나중에 읽으면 맞지 않는다.
                int hpAfterEnemy = Math.Max(0, battle.Player.Hp);

                bool rewoundHere = false;
                if (battle.Outcome == BattleOutcome.PlayerLost && battle.CanRewind)
                {
                    battle.Rewind();
                    turnAlreadyStarted = true;
                    rewoundHere = true;
                }

                turns.Add(new
                {
                    round,
                    playerHp = hpBefore,
                    playerHpAfter = hpAfterEnemy,
                    playerMaxHp = run.playerMaxHp,
                    playerBlock = blockAfterPlayer,
                    enemyHp = enemyHpBefore,
                    enemyHpAfter = enemyHpAfterPlayer,
                    enemyMaxHp = battle.Enemy.MaxHp,
                    energy,
                    deck = deckBefore,
                    discard = discardBefore,
                    hand = handBefore,
                    played,
                    // 되감기 PoC의 핵심: 적의 의도는 본 적이 있을 때만 알 수 있다.
                    knownIntentType = known ? knownType : null,
                    knownIntentAmount = known ? knownAmount : -1,
                    memory,
                    rewindCharges = charges,
                    enemyAction,
                    // 이 턴 끝에 되감았는가. 목업이 여기에 표식을 찍는다.
                    rewoundAfter = rewoundHere
                });
            }

            return new Scene
            {
                Seed = seed,
                EnemyId = battle.EnemyDef.id,
                EnemyNameKo = battle.EnemyDef.nameKo,
                EnemyPatternLength = battle.EnemyDef.pattern.Length,
                MemoryDamagePctPerStack = battle.EnemyDef.memoryDamagePctPerStack,
                Outcome = battle.Outcome.ToString(),
                RewindsUsed = battle.RewindsUsed,
                FinalMemory = battle.Memory,
                Turns = turns
            };
        }

        static List<string> PlaysSince(Battle b, int logIndex)
        {
            var played = new List<string>();
            for (int i = logIndex; i < b.Log.Count; i++)
                if (b.Log[i].StartsWith("  play ")) played.Add(b.Log[i].Substring("  play ".Length));
            return played;
        }

        static string FirstEnemyLine(Battle b, int logIndex)
        {
            for (int i = logIndex; i < b.Log.Count; i++)
                if (b.Log[i].StartsWith("  enemy ")) return b.Log[i].Trim();
            for (int i = logIndex; i < b.Log.Count; i++)
                if (b.Log[i].StartsWith("  burn ")) return b.Log[i].Trim();
            return "(없음)";
        }

        sealed class Scene
        {
            public int Seed;
            public string EnemyId;
            public string EnemyNameKo;
            public int EnemyPatternLength;
            public int MemoryDamagePctPerStack;
            public string Outcome;
            public int RewindsUsed;
            public int FinalMemory;
            public List<object> Turns;
            public int TurnCount => Turns.Count;
        }
    }
}
