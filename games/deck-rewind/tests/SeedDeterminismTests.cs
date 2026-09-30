using NUnit.Framework;

namespace DeckRewind.Tests
{
    /// <summary>
    /// 검사기 1 — `SeedDeterminism` (PLAN_DECKBUILDER §5).
    /// <b>같은 씨드 = 같은 결과.</b> 아래 검사기 전부의 전제다.
    /// 한 번이라도 깨지면 승률·지배·사장 카드 판정이 모두 무의미해진다.
    ///
    /// 되감기가 있는 게임이므로 여기서 재는 것이 하나 더 있다:
    /// 되감기가 난수 상태까지 정확히 되돌리는가. 안 되돌리면 되감기가
    /// "운을 다시 뽑는 버튼"이 되고, 그건 다른 게임이다.
    /// </summary>
    [TestFixture]
    public class SeedDeterminismTests
    {
        [Test]
        public void 같은_씨드로_두_번_돌리면_기록이_한_글자도_다르지_않다()
        {
            for (int s = 0; s < 8; s++)
            {
                int seed = Fix.Data.Balance.seedBase + s;
                var a = RunEngine.Play(Fix.Data, Fix.Greedy(), seed);
                var b = RunEngine.Play(Fix.Data, Fix.Greedy(), seed);
                Assert.That(b.Transcript, Is.EqualTo(a.Transcript), "씨드 " + seed + " 의 기록이 갈렸다");
                Assert.That(b.Won, Is.EqualTo(a.Won));
                Assert.That(b.PlayerHp, Is.EqualTo(a.PlayerHp));
                Assert.That(b.RewindsUsed, Is.EqualTo(a.RewindsUsed));
            }
        }

        [Test]
        public void 무작위_플레이도_씨드가_같으면_같다()
        {
            for (int s = 0; s < 8; s++)
            {
                int seed = Fix.Data.Balance.seedBase + 500 + s;
                var a = RunEngine.Play(Fix.Data, Fix.Random(seed), seed);
                var b = RunEngine.Play(Fix.Data, Fix.Random(seed), seed);
                Assert.That(b.Transcript, Is.EqualTo(a.Transcript), "씨드 " + seed);
            }
        }

        [Test]
        public void 씨드가_다르면_기록도_다르다()
        {
            // 결정적인 것과 씨드를 무시하는 것은 다르다. 후자면 승률 측정이 표본 하나가 된다.
            var first = RunEngine.Play(Fix.Data, Fix.Greedy(), Fix.Data.Balance.seedBase).Transcript;
            int different = 0;
            for (int s = 1; s <= 12; s++)
                if (RunEngine.Play(Fix.Data, Fix.Greedy(), Fix.Data.Balance.seedBase + s).Transcript != first)
                    different++;
            Assert.That(different, Is.GreaterThanOrEqualTo(10), "씨드를 바꿨는데 기록이 거의 그대로다");
        }

        [Test]
        public void 되감기는_난수_상태까지_되돌린다()
        {
            var data = Fix.Data;
            var run = data.Run;

            // 되감지 않는 진행과 되감은 뒤 같은 지점까지 다시 온 진행의 손패를 비교한다.
            var plain = NewBattle(4242);
            plain.BeginPlayerTurn();
            var handT1 = string.Join(",", plain.Hand);
            plain.EndPlayerTurn();
            plain.EnemyTurn();
            plain.BeginPlayerTurn();
            var handT2 = string.Join(",", plain.Hand);
            plain.EndPlayerTurn();
            plain.EnemyTurn();

            var rewound = NewBattle(4242);
            rewound.BeginPlayerTurn();
            rewound.EndPlayerTurn();
            rewound.EnemyTurn();
            rewound.BeginPlayerTurn();
            Assert.That(rewound.CanRewind, Is.True, "두 번째 턴에서는 되감을 수 있어야 한다");
            rewound.Rewind();

            Assert.That(rewound.Round, Is.EqualTo(1), "한 라운드 되감으면 1라운드 시작으로 돌아간다");
            Assert.That(string.Join(",", rewound.Hand), Is.EqualTo(handT1),
                "되감은 뒤 손패가 다르다 — 되감기가 운을 다시 뽑는 버튼이 됐다");
            Assert.That(rewound.Energy, Is.EqualTo(run.energyPerTurn), "기력도 되돌아와야 한다");

            // 다시 같은 길을 가면 손패가 원래와 같아야 한다. 적의 수만 바뀐다.
            rewound.EndPlayerTurn();
            rewound.EnemyTurn();
            rewound.BeginPlayerTurn();
            Assert.That(string.Join(",", rewound.Hand), Is.EqualTo(handT2),
                "되감은 뒤 다음 턴 손패가 갈렸다");
        }

        static Battle NewBattle(int seed)
        {
            var run = Fix.Data.Run;
            return new Battle(Fix.Data, Fix.Data.Enemy(run.battles[0]), run.startingDeck,
                              run.playerMaxHp, run.playerMaxHp, seed,
                              run.handSize, run.energyPerTurn, run.rewindCharges,
                              run.maxRewindsPerBattle, run.maxRoundsPerBattle);
        }
    }
}
