using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;

namespace DeckOpenhand.Tests
{
    /// <summary>
    /// 연출 목업이 읽을 장면을 <b>실제 시뮬레이션에서 뽑아</b> `presentation/scene.json` 으로 내보낸다.
    ///
    /// 설계 원칙 2: 손으로만 만들 수 있는 에셋을 만들지 않는다.
    /// 목업의 숫자를 손으로 적으면 데이터를 고칠 때마다 목업이 거짓이 되고,
    /// 거짓이 되는 순간 연출 비교가 아니라 그림 감상이 된다. 그래서 테스트가 굽는다.
    ///
    /// 이 PoC에서는 특히 <b>예고된 다섯 수</b>가 진짜여야 한다 — 손으로 적으면
    /// "늘 보인다"는 규칙 자체가 목업에서 거짓말이 된다.
    /// </summary>
    [TestFixture]
    public class SceneExportTests
    {
        const int SeedFrom = 4242;
        const int SeedTries = 120;

        [Test]
        public void 연출_목업용_장면을_내보낸다()
        {
            // 장면은 규칙이 실제로 일어난 판이어야 한다. 순서를 한 번도 건드리지 않은 판을 그리면
            // 목업이 "정보가 아니라 순서가 퍼즐이다"를 보여 주지 못한다.
            // 씨드를 고정 순서로 훑어 가림막·역순이 나온 판을 고른다 (훑는 순서가 고정이라 결과도 고정이다).
            var run0 = Fix.Data.Run;
            int chosen = -1;
            for (int i = 0; i < SeedTries; i++)
            {
                // 훑을 때도 장면과 같은 덱을 써야 한다. 시작 덱에는 가림막·역순이 없어
                // 다른 덱으로 훑으면 영원히 못 찾는다.
                var probe = Fix.NewBattle(run0.boss, SeedFrom + i, Fix.FullPoolDeck(1));
                probe.RunToEnd(Fix.Greedy());
                if (probe.IntentsSkipped + probe.IntentsSwapped > 0
                    && probe.Outcome == BattleOutcome.PlayerWon
                    && probe.Round >= 5)
                { chosen = SeedFrom + i; break; }
            }
            Assert.That(chosen, Is.GreaterThan(0),
                "씨드 " + SeedTries + "개를 훑어도 순서를 건드리는 판이 없다 — 목업이 규칙을 못 보여 준다");

            var data = Fix.Data;
            var run = data.Run;
            var agent = Fix.Greedy();
            var battle = Fix.NewBattle(run.boss, chosen, Fix.FullPoolDeck(1));

            var turns = new List<object>();
            int guard = 0;

            while (battle.Outcome == BattleOutcome.InProgress && ++guard < 40)
            {
                battle.BeginPlayerTurn();

                var handBefore = new List<string>(battle.Hand);
                int hpBefore = battle.Player.Hp;
                int enemyHpBefore = battle.Enemy.Hp;
                int deckBefore = battle.Deck.Count, discardBefore = battle.Discard.Count;
                int round = battle.Round, energy = battle.Energy;
                int braceValue = battle.BraceValue();
                int attacksAhead = battle.AttacksInWindow;
                var peekBefore = Peek(battle);

                int logBefore = battle.Log.Count;
                agent.TakePlayerTurn(battle);
                var played = PlaysSince(battle, logBefore);
                var peekAfter = Peek(battle);

                battle.EndPlayerTurn();
                int enemyHpAfterPlayer = battle.Enemy.Hp;
                int blockAfterPlayer = battle.Player.Block;

                string enemyAction = "(없음)";
                if (battle.Outcome == BattleOutcome.InProgress)
                {
                    int logMark = battle.Log.Count;
                    battle.EnemyTurn();
                    enemyAction = FirstEnemyLine(battle, logMark);
                }

                turns.Add(new
                {
                    round,
                    playerHp = hpBefore,
                    playerHpAfter = battle.Player.Hp,
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
                    // 공개 덱 PoC의 핵심: 앞으로 오는 수가 늘 보인다.
                    // peekAfter 가 peekBefore 와 다르면 그 턴에 순서를 건드린 것이다.
                    peek = peekBefore,
                    peekAfterPlay = peekAfter,
                    braceValue,
                    attacksAhead,
                    enemyAction
                });
            }

            string pocRoot = Directory.GetParent(GameData.FindDataDir(
                TestContext.CurrentContext.TestDirectory)).FullName;
            string outPath = Path.Combine(pocRoot, "presentation", "scene.json");

            var scene = new
            {
                _ = "손으로 쓰지 말 것. tests/SceneExportTests.cs 가 실제 시뮬레이션에서 굽는다.",
                generatedBy = "deck-openhand.Tests / SceneExportTests",
                rule = "공개 덱 — 적의 다음 다섯 수가 늘 보인다. 정보가 아니라 순서가 퍼즐이다.",
                seed = chosen,
                peekWindow = run.peekWindow,
                enemyId = battle.EnemyDef.id,
                enemyNameKo = battle.EnemyDef.nameKo,
                enemyPatternLength = battle.EnemyDef.pattern.Length,
                outcome = battle.Outcome.ToString(),
                intentsSkipped = battle.IntentsSkipped,
                intentsSwapped = battle.IntentsSwapped,
                turns
            };

            File.WriteAllText(outPath, JsonConvert.SerializeObject(scene, Formatting.Indented) + "\n");
            TestContext.WriteLine("장면을 내보냈다: " + outPath + " (" + turns.Count + "턴)");

            Assert.That(File.Exists(outPath), Is.True);
            Assert.That(turns.Count, Is.GreaterThanOrEqualTo(3), "장면이 너무 짧아 연출을 볼 수 없다");
            Assert.That(new FileInfo(outPath).Length, Is.GreaterThan(500));
        }

        static List<object> Peek(Battle b)
        {
            var list = new List<object>();
            foreach (var i in b.PeekIntents(b.PeekWindow))
                list.Add(new { type = i.type, amount = i.amount, target = i.target });
            return list;
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
            return "(없음)";
        }
    }
}
