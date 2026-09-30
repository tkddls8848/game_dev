using System.Collections.Generic;
using System.Text;
using Interp.Data;
using Interp.Sim;
using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// ★ 핵 검사기 1 — **「한 낱말이 조약을 바꾼다」**
    ///
    /// 한 발화의 역어 **하나만** 바꿨을 때 최종 조약이 달라지는 지점이 있는가.
    /// 없으면 이 PoC의 한 줄이 거짓이다. **몇 개 지점에서 그러한지 수를 낸다.**
    ///
    /// 두 세계 비교로 이 검사기에 이가 있음을 함께 보인다:
    ///   · 켠 세계 — 고른 역어가 조문에 반영된다
    ///   · 끈 세계 — 무엇을 고르든 정확한 역어가 나간 것으로 친다(통역은 도관일 뿐이다)
    /// 끈 세계에서 단 하나라도 달라지면 이 검사기는 역어가 아니라 다른 것을 재고 있는 것이다.
    /// </summary>
    [TestFixture]
    public class OneWordChangesTheTreatyTests
    {
        private GameData D { get { return TestWorld.Data; } }

        private static DivergenceReport _on;
        private static DivergenceReport _off;

        [OneTimeSetUp]
        public void Analyse()
        {
            _on = Divergence.Analyse(TestWorld.Data, TestWorld.Data.AllSeeds(), Policies.All(), true);
            _off = Divergence.Analyse(TestWorld.Data, TestWorld.Data.AllSeeds(), Policies.All(), false);
        }

        [Test]
        public void OneWordChangesTheTreaty()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 한 낱말만 바꿔 보았다 ──────────────────────────────────");
            sb.AppendLine("돌린 밤            " + _on.Runs);
            sb.AppendLine("바꿔 본 낱말        " + _on.FlipsTried);
            sb.AppendLine("조약이 달라진 것    " + _on.FlipsChangingTreaty);
            sb.AppendLine("결말이 달라진 것    " + _on.FlipsChangingEnding);
            sb.AppendLine("오해 목록이 달라진 것 " + _on.FlipsChangingMisreadings);
            sb.AppendLine("★ 조약이 달라지는 지점 (씨드×정책×마디) " + _on.PointsChangingTreaty.Count);
            sb.AppendLine("★ 그런 마디의 가짓수                     " + _on.StagesChangingTreaty.Count
                          + " / " + D.Stages.Count);
            sb.AppendLine("대조군(역어가 장식인 세계)에서 달라진 것  " + _off.FlipsChangingTreaty);
            TestContext.Out.WriteLine(sb.ToString());

            Assert.That(_on.FlipsTried, Is.GreaterThan(200), "바꿔 본 낱말이 너무 적다");
            Assert.That(_on.PointsChangingTreaty.Count, Is.GreaterThanOrEqualTo(60),
                "한 낱말이 조약을 바꾸는 지점이 " + _on.PointsChangingTreaty.Count + "곳뿐이다");
            Assert.That(_on.StagesChangingTreaty.Count, Is.GreaterThanOrEqualTo(8),
                "조약을 바꿀 수 있는 마디가 " + _on.StagesChangingTreaty.Count + "개뿐이다 — 나머지 마디는 장식이다");
            Assert.That(_on.FlipsChangingEnding, Is.GreaterThan(0), "결말까지 바뀌는 낱말이 하나도 없다");
        }

        /// <summary>
        /// 대조군. 역어가 조문에 닿지 않는 세계에서는 **아무것도** 달라지지 않아야 한다.
        /// 여기서 0이 아니면 위 숫자는 역어가 아니라 다른 것을 센 것이다.
        /// </summary>
        [Test]
        public void ControlWorldWhereRenderingIsDecorationChangesNothing()
        {
            Assert.That(_off.FlipsTried, Is.GreaterThan(200));
            Assert.That(_off.FlipsChangingTreaty, Is.Zero, "역어가 장식인 세계에서 조약이 달라졌다");
            Assert.That(_off.FlipsChangingEnding, Is.Zero, "역어가 장식인 세계에서 결말이 달라졌다");
            Assert.That(_off.FlipsChangingMisreadings, Is.Zero, "역어가 장식인 세계에서 오해가 달라졌다");
        }

        /// <summary>
        /// **조항 하나만** 갈아 끼우는 낱말이 있는가.
        /// 전부 뒤엎는 낱말만 있다면 "한 낱말이 조약을 바꾼다"가 아니라 "한 낱말이 판을 바꾼다"다.
        /// </summary>
        [Test]
        public void SomeWordsMoveExactlyOneArticle()
        {
            int single = 0, multi = 0;
            foreach (Flip f in _on.Changed)
            {
                if (!f.TreatyChanged) continue;
                if (f.ClausesChanged == 1) single++; else multi++;
            }
            TestContext.Out.WriteLine("조항 하나만 갈린 낱말 " + single + " · 여럿이 갈린 낱말 " + multi);
            Assert.That(single, Is.GreaterThanOrEqualTo(20),
                "조항 하나만 갈아 끼우는 낱말이 " + single + "개뿐이다");
            Assert.That(multi, Is.GreaterThan(0),
                "한 낱말이 조항 여럿을 끌고 가는 자리가 없다 — 뒤가 앞에 매여 있지 않다는 뜻이다");
        }

        /// <summary>
        /// **거짓말 없이도** 조약이 바뀌는가. 정확·완곡·강경 사이의 갈아타기만으로도
        /// 조문이 움직여야 한다. 오역만 조약을 바꾼다면 이 게임은 '거짓말 게임'이지
        /// '번역 게임'이 아니다.
        /// </summary>
        [Test]
        public void TruthfulRegistersAloneAlreadyMoveTheTreaty()
        {
            int honest = 0;
            HashSet<string> stages = new HashSet<string>();
            foreach (Flip f in _on.Changed)
            {
                if (!f.TreatyChanged) continue;
                if (f.FromRegister == "false" || f.ToRegister == "false") continue;
                honest++;
                stages.Add(f.StageId);
            }
            TestContext.Out.WriteLine("거짓 없이(정확·완곡·강경 사이) 조약을 바꾼 낱말 " + honest
                                      + " · 그런 마디 " + stages.Count);
            Assert.That(honest, Is.GreaterThanOrEqualTo(40),
                "거짓말을 빼면 조약을 바꿀 길이 " + honest + "개뿐이다");
            Assert.That(stages.Count, Is.GreaterThanOrEqualTo(6),
                "거짓말 없이 조약을 바꿀 수 있는 마디가 " + stages.Count + "개뿐이다");
        }

        [Test]
        public void PrintTheSharpestWords()
        {
            List<Flip> best = new List<Flip>();
            foreach (Flip f in _on.Changed)
                if (f.TreatyChanged && f.EndingChanged && f.Seed == D.Balance.seed) best.Add(f);
            best.Sort((a, b) => b.ClausesChanged.CompareTo(a.ClausesChanged));

            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("── 씨드 " + D.Balance.seed + " 에서 결말까지 바꾼 낱말 (앞 12개) ─────────");
            for (int i = 0; i < best.Count && i < 12; i++)
            {
                Flip f = best[i];
                sb.AppendLine(string.Format("{0,-16} {1,-10} {2}→{3}  조항 {4}개  {5} → {6}",
                    f.StageId, f.BasePolicy, Localization.Register(f.FromRegister),
                    Localization.Register(f.ToRegister), f.ClausesChanged, f.BaseEnding, f.FlippedEnding));
            }
            sb.AppendLine("(결말까지 바뀐 낱말 " + best.Count + "개)");
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(best.Count, Is.GreaterThan(0));
        }
    }
}
