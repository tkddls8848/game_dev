using NUnit.Framework;

namespace FarmRewindYear.Tests
{
    /// 이 PoC의 가설을 기계가 판정할 수 있는 만큼만 본다:
    /// "되감을 수 있지만 되감은 횟수가 흔적을 남긴다 — 그래서 되감기가 편의가 아니라 규칙이다."
    ///
    /// 기계가 볼 수 없는 것: **사람의 학습이 흔적을 이기는가.** 여기 쓰인 탐욕 정책은 이기지 못한다
    /// (예지가 늘어도 연말 현금이 계속 줄어든다). 사람은 한 해의 윤작을 계획하지만 이 정책은 못 한다.
    /// 그 차이가 이 PoC의 가장 큰 미확인 사항이고, README 판정 칸에 그대로 적었다.
    [TestFixture]
    public class RewindHypothesisTests
    {
        [Test]
        public void 되감기는_공짜가_아니다()
        {
            var d = Support.Data;
            Assert.That(d.Plots.rewind.soilLossPerRewind, Is.GreaterThan(0));
            var ladder = Support.Ladder.Attempts;
            Assert.That(ladder[ladder.Count - 1].SoilSumAtStart,
                Is.LessThan(ladder[0].SoilSumAtStart),
                "스무 번을 되감아도 봄의 땅이 그대로다 — 되감기가 편의다.");
        }

        [Test]
        public void 되감아도_사라지는_것과_남는_것이_정확히_갈린다()
        {
            // 되돌아가는 것: 현금 · 산 칸 · 심어 둔 것. 남는 것: 되감기 흔적뿐.
            var ladder = Support.Ladder.Attempts;
            foreach (var a in ladder)
            {
                Assert.That(a.SoilSumAtStart, Is.GreaterThan(0));
                // 산 칸은 시도마다 다시 사야 한다 — 이월되지 않는다.
                Assert.That(a.ActivePlots, Is.LessThanOrEqualTo(
                    Support.Data.Plots.startActivePlots + Support.Data.Plots.expansion.maxExtraPlots));
            }
            // 흔적은 단조롭게만 쌓인다(중간에 회복되지 않는다).
            for (int i = 1; i < ladder.Count; i++)
                Assert.That(ladder[i].SoilSumAtStart, Is.LessThanOrEqualTo(ladder[i - 1].SoilSumAtStart),
                    $"되감기 {ladder[i].RewindsBefore}회에서 봄 토질이 회복됐다 — 흔적이 지워졌다.");
        }

        [Test]
        public void 지식의_값을_실측해_남긴다()
        {
            // 판정하지 않고 기록만 한다. 이 저장소의 탐욕 정책에서는 완전한 지식이 무지보다
            // 덜 남긴다 — 정책이 한 해의 윤작을 계획하지 못해서다. 사람이 그것을 할 수 있는지가
            // 이 PoC의 가장 큰 미확인 사항이고, 기계는 그것을 판정할 수 없다(POC_FACTORY §5).
            int blind = Support.StubbornLadder.Attempts[0].FinalMoneyCoin;
            int knowing = Support.Ladder.Attempts[0].FinalMoneyCoin;
            Support.Line($"같은 해 · 같은 땅 — 지식 없음 {blind} vs 완전한 지식 {knowing} (차이 {knowing - blind})");
            Support.Line("이 차이의 부호는 검사기가 판정하지 않는다. 사람이 플레이해서 판정할 것.");
            Assert.That(blind, Is.GreaterThan(0));
            Assert.That(knowing, Is.GreaterThan(0));
        }

        [Test]
        public void 배우지_않는_되감기는_순손실이다()
        {
            var atts = Support.StubbornLadder.Attempts;
            Assert.That(atts[atts.Count - 1].FinalMoneyCoin, Is.LessThan(atts[0].FinalMoneyCoin),
                "배우지 않고 스무 번 되감았는데 손해가 아니다.");
            Support.Line($"배우지 않는 되감기: 첫 해 {atts[0].FinalMoneyCoin} → 스무 번째 {atts[atts.Count - 1].FinalMoneyCoin}");
        }

        [Test]
        public void 한_해보다_긴_시간_축이_없다()
        {
            // 이 PoC의 범위. 해를 넘겨 쌓이는 것이 있으면 그것은 다른 게임이다.
            var d = Support.Data;
            foreach (var c in d.Crops.crops)
                Assert.That(c.growDays, Is.LessThanOrEqualTo(d.DaysPerYear),
                    $"{c.nameKo}: 한 해보다 오래 걸린다 — 되감기가 성립하지 않는다.");
            foreach (var a in Support.Ladder.Attempts)
                Assert.That(a.SoilSumAtEnd, Is.LessThanOrEqualTo(a.SoilSumAtStart),
                    "한 해 안에서 토질이 늘었다 — 이 PoC에는 회복 수단이 없다.");
        }

        [Test]
        public void 해를_넘기거나_계절을_못_견딘_작물이_실제로_죽는다()
        {
            // 마감이 없으면 '언제 심는가'가 판단이 아니고, 그러면 날씨 기억도 값이 없다.
            int lost = 0;
            foreach (var a in Support.Ladder.Attempts) lost += a.CropsLost;
            Assert.That(lost, Is.GreaterThan(0),
                "사다리를 끝까지 돌려도 죽은 작물이 하나도 없다 — 마감이 압력이 아니다.");
            Support.Line($"사다리 전체에서 계절·연말 마감에 죽은 작물 {lost}개");
        }
    }
}
