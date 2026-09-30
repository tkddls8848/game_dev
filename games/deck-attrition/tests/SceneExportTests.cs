using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 연출 목업이 읽을 장면을 <b>실제 시뮬레이션에서 뽑아</b> `presentation/scene.json` 으로 내보낸다.
    ///
    /// 설계 원칙 2: 손으로만 만들 수 있는 에셋을 만들지 않는다.
    /// 목업의 숫자를 손으로 적으면 데이터를 고칠 때마다 목업이 거짓이 되고,
    /// 거짓이 되는 순간 연출 비교가 아니라 그림 감상이 된다. 그래서 테스트가 굽는다.
    ///
    /// 장면은 <b>규칙이 실제로 일어난 판</b>이어야 한다. 활자가 한 번도 안 닳은 판을 그리면
    /// 목업이 이 PoC를 설명하지 못한다. 그래서 씨드를 순서대로 훑어
    /// <b>이기면서 활자가 녹는</b> 회차를 고른다 (훑는 순서가 고정이라 결과도 고정이다).
    /// </summary>
    [TestFixture]
    public class SceneExportTests
    {
        const int SeedFrom = 5150;
        const int SeedTries = 200;

        [Test]
        public void 연출_목업용_장면을_내보낸다()
        {
            Scene best = null;
            for (int i = 0; i < SeedTries; i++)
            {
                var s = Capture(SeedFrom + i);
                if (!s.Won) continue;
                if (s.Melted.Count == 0) continue;
                if (s.Focus.Turns.Count < 5) continue;
                // 보스 판에서 활자가 녹은 회차가 가장 좋다 — 아껴 둔 것을 쓰는 자리가 이 규칙의 그림이다.
                if (s.FocusMelted.Count > 0) { best = s; break; }
                if (best == null) best = s;
            }

            Assert.That(best, Is.Not.Null, "이기면서 활자가 녹는 회차를 하나도 못 찾았다");
            Assert.That(best.Melted.Count, Is.GreaterThan(0));

            string pocRoot = Directory.GetParent(GameData.FindDataDir(
                TestContext.CurrentContext.TestDirectory)).FullName;
            string outPath = Path.Combine(pocRoot, "presentation", "scene.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            var doc = new Dictionary<string, object>
            {
                ["_"] = "손으로 쓰지 말 것. tests/SceneExportTests.cs 가 실제 시뮬레이션에서 굽는다.",
                ["generatedBy"] = "deck-attrition.Tests / SceneExportTests",
                ["rule"] = "소모되는 카드 — 활자는 한 판이 아니라 회차 전체에서 닳는다. "
                         + "찍을수록 뭉개지고, 다 쓰면 녹여 버린다.",
                ["policy"] = "Measured (적절히 쓴다)",
                ["seed"] = best.Seed,
                ["won"] = best.Won,
                ["playerMaxHp"] = Fix.Data.Run.playerMaxHp,
                ["deadlineRounds"] = Fix.Data.Run.maxRoundsPerBattle,
                ["battles"] = best.Battles,
                ["focus"] = new
                {
                    battleIndex = best.Focus.Index,
                    enemyId = best.Focus.EnemyId,
                    enemyNameKo = best.Focus.EnemyNameKo,
                    enemyMaxHp = best.Focus.EnemyMaxHp,
                    isBoss = best.Focus.IsBoss,
                    turns = best.Focus.Turns
                },
                ["meltedInFocus"] = best.FocusMelted,
                ["melted"] = best.Melted,
                ["caseAtEnd"] = best.CaseAtEnd
            };
            File.WriteAllText(outPath, JsonConvert.SerializeObject(doc, Formatting.Indented) + "\n");

            TestContext.WriteLine("장면: 씨드 " + best.Seed + " · " + best.Focus.EnemyNameKo
                                  + " · " + best.Focus.Turns.Count + "턴 · 녹은 활자 "
                                  + string.Join(",", best.Melted));
            Assert.That(new FileInfo(outPath).Length, Is.GreaterThan(1500));
        }

        // ---------- 한 회차를 찍어 장면으로 만든다 ----------

        Scene Capture(int seed)
        {
            var data = Fix.Data;
            var run = data.Run;
            var agent = Fix.Measured();
            var tc = new TypeCase(data);
            var deck = new List<string>(run.startingDeck);
            var rng = new Rng(seed);

            var order = new List<string>(run.battles) { run.boss };
            var scene = new Scene { Seed = seed };

            int hp = run.playerMaxHp;

            for (int i = 0; i < order.Count; i++)
            {
                int battleSeed = seed + 1013 * (i + 1) + rng.Range(9973);
                bool isBoss = i == order.Count - 1;
                var enemy = data.Enemy(order[i]);

                var b = new Battle(data, tc, enemy, deck, hp, run.playerMaxHp, battleSeed,
                                   run.handSize, run.energyPerTurn, run.maxRoundsPerBattle,
                                   i, order.Count - 1 - i, isBoss);

                var caseAtStart = SnapshotCase(tc);
                var turns = PlayAndRecord(b, agent);

                scene.Battles.Add(new
                {
                    index = i,
                    enemyId = enemy.id,
                    enemyNameKo = enemy.nameKo,
                    enemyMaxHp = enemy.maxHp,
                    isBoss,
                    hpAtStart = hp,
                    caseAtStart,
                    melted = new List<string>(b.SpentHere),
                    outcome = b.Outcome.ToString()
                });

                if (isBoss || scene.Focus == null)
                    scene.Focus = new FocusBattle
                    {
                        Index = i, EnemyId = enemy.id, EnemyNameKo = enemy.nameKo,
                        EnemyMaxHp = enemy.maxHp, IsBoss = isBoss, Turns = turns
                    };
                if (isBoss) scene.FocusMelted.AddRange(b.SpentHere);

                foreach (var gone in b.SpentHere) deck.RemoveAll(x => x == gone);

                if (b.Outcome != BattleOutcome.PlayerWon) { scene.Won = false; break; }
                hp = Math.Min(run.playerMaxHp, b.Player.Hp + run.healAfterBattle);
                scene.Won = isBoss;

                if (!isBoss && run.rewardPool.Length > 0)
                    for (int r = 0; r < run.rewardsPerBattle; r++)
                    {
                        string reward = run.rewardPool[rng.Range(run.rewardPool.Length)];
                        var card = data.Card(reward);
                        if (!card.IsConsumable) deck.Add(reward);
                        else if (tc.IsSpent(reward)) { /* 녹은 활자는 돌아오지 않는다 */ }
                        else if (deck.Contains(reward)) tc.Recast(reward, run.rewardRecastUses);
                        else deck.Add(reward);
                    }
            }

            scene.Melted.AddRange(tc.Exhausted);
            scene.CaseAtEnd = SnapshotCase(tc);
            return scene;
        }

        /// <summary>
        /// 한 전투를 돌면서 턴마다 손패의 <b>선명도</b>까지 찍는다.
        /// 목업은 남은 횟수를 숫자로 적지 않고 이 값으로 그린다 — 활자가 닳는 그림이 카드가 닳는 그림이다.
        /// </summary>
        List<object> PlayAndRecord(Battle b, IAgent agent)
        {
            var turns = new List<object>();
            int guard = 0;

            while (b.Outcome == BattleOutcome.InProgress && guard++ < 40)
            {
                b.BeginPlayerTurn();

                var handBefore = new List<object>();
                foreach (var id in b.Hand) handBefore.Add(CardShot(b, id));

                int hpBefore = b.Player.Hp, enemyHpBefore = b.Enemy.Hp, energy = b.Energy;
                int deckBefore = b.Deck.Count, discardBefore = b.Discard.Count, round = b.Round;
                string intentType = b.NextIntent.type;
                int intentAmount = b.NextIntent.type == "attack" ? b.IncomingDamage : b.NextIntent.amount;

                int logMark = b.Log.Count;
                int meltMark = b.SpentHere.Count;
                agent.TakePlayerTurn(b);
                var played = PlaysSince(b, logMark);
                var meltedNow = b.SpentHere.GetRange(meltMark, b.SpentHere.Count - meltMark);

                int enemyHpAfterPlayer = b.Enemy.Hp;
                int blockAfterPlayer = b.Player.Block;

                if (b.Outcome == BattleOutcome.InProgress) b.EndPlayerTurn();

                string enemyAction = "(없음)";
                if (b.Outcome == BattleOutcome.InProgress)
                {
                    int mark = b.Log.Count;
                    b.EnemyTurn();
                    enemyAction = FirstLineStartingWith(b, mark, "  enemy ", "  smudge ");
                }

                turns.Add(new
                {
                    round,
                    playerHp = hpBefore,
                    playerHpAfter = Math.Max(0, b.Player.Hp),
                    playerBlock = blockAfterPlayer,
                    enemyHp = enemyHpBefore,
                    enemyHpAfter = enemyHpAfterPlayer,
                    energy,
                    deck = deckBefore,
                    discard = discardBefore,
                    hand = handBefore,
                    played,
                    meltedNow,
                    intentType,
                    intentAmount,
                    enemyAction
                });
            }
            return turns;
        }

        object CardShot(Battle b, string id)
        {
            var c = Fix.Data.Card(id);
            return new
            {
                id = c.id,
                nameKo = c.nameKo,
                textKo = c.textKo,
                cost = c.cost,
                rarity = c.rarity,
                consumable = c.IsConsumable,
                usesLeft = b.Case.UsesLeft(c.id),
                usesTotal = c.uses,
                sharpnessPct = b.Case.SharpnessPct(c.id)
            };
        }

        List<object> SnapshotCase(TypeCase tc)
        {
            var list = new List<object>();
            foreach (var c in Fix.Data.Cards)
            {
                if (!c.IsConsumable) continue;
                list.Add(new
                {
                    id = c.id,
                    nameKo = c.nameKo,
                    usesLeft = tc.UsesLeft(c.id),
                    usesTotal = c.uses,
                    timesUsed = tc.TimesUsed(c.id),
                    sharpnessPct = tc.SharpnessPct(c.id)
                });
            }
            return list;
        }

        static List<object> PlaysSince(Battle b, int logIndex)
        {
            var played = new List<object>();
            for (int i = logIndex; i < b.Log.Count; i++)
            {
                if (!b.Log[i].StartsWith("  play ")) continue;
                var parts = b.Log[i].Substring("  play ".Length).Split(' ');
                string id = parts[0];
                int sharp = 100;
                if (parts.Length > 1 && parts[1].StartsWith("sharp="))
                    int.TryParse(parts[1].Substring(6).TrimEnd('%'), out sharp);
                var c = Fix.Data.Card(id);
                played.Add(new { id, nameKo = c.nameKo, cost = c.cost, consumable = c.IsConsumable, sharpnessPct = sharp });
            }
            return played;
        }

        static string FirstLineStartingWith(Battle b, int logIndex, params string[] prefixes)
        {
            foreach (var prefix in prefixes)
                for (int i = logIndex; i < b.Log.Count; i++)
                    if (b.Log[i].StartsWith(prefix)) return b.Log[i].Trim();
            return "(없음)";
        }

        sealed class FocusBattle
        {
            public int Index;
            public string EnemyId, EnemyNameKo;
            public int EnemyMaxHp;
            public bool IsBoss;
            public List<object> Turns = new List<object>();
        }

        sealed class Scene
        {
            public int Seed;
            public bool Won;
            public List<object> Battles = new List<object>();
            public FocusBattle Focus;
            public List<string> FocusMelted = new List<string>();
            public List<string> Melted = new List<string>();
            public List<object> CaseAtEnd = new List<object>();
        }
    }
}
