using NUnit.Framework;

namespace FarmRewindYear.Tests
{
    /// 검사기 6 — ProgressionPacing. 주요 단계 도달이 설계 구간 안인가.
    /// 이 PoC의 단계는 연차가 아니라 **되감기 횟수**다.
    [TestFixture]
    public class ProgressionPacingTests
    {
        [Test]
        public void 첫_해는_넘길_수_있지만_여유가_없다()
        {
            // 못 넘기면 불공정하고, 넉넉히 넘기면 되감기 버튼이 화면에 나올 일이 없다.
            var lim = Support.Data.Economy.limits;
            int quota = Support.Data.Economy.quotaCoin;
            var first = Support.StubbornLadder.Attempts[0];
            int margin = first.FinalMoneyCoin - quota;
            Support.Line($"첫 해: 수입 {first.IncomeCoin} · 연말 현금 {first.FinalMoneyCoin} · 할당량 {quota} · 여유 {margin}");
            Assert.That(first.QuotaMet, Is.True, "첫 해에 할당량을 넘길 수 없다 — 되감기가 의무가 되면 선택이 아니다.");
            Assert.That(margin, Is.LessThanOrEqualTo(lim.firstYearMarginMaxCoin),
                $"첫 해 여유가 {margin}이다. 이만큼 남으면 되감을 이유가 없다.");
        }

        [Test]
        public void 한_번만_되감아도_그_해는_더_어려워진다()
        {
            // 흔적이 바로 다음 해에 수치로 나타나야 플레이어가 되감기의 값을 읽을 수 있다.
            foreach (var r in Support.AllLadders)
            {
                var a0 = r.Attempts[0];
                var a1 = r.Attempts[1];
                Assert.That(a1.SoilSumAtStart, Is.LessThan(a0.SoilSumAtStart));
                Assert.That(a1.FinalMoneyCoin, Is.LessThan(a0.FinalMoneyCoin),
                    $"한 번 되감았는데 연말 현금이 {a0.FinalMoneyCoin} → {a1.FinalMoneyCoin}. 어려워지지 않았다.");
            }
        }

        [Test]
        public void 벽이_설계_구간_안에_있다()
        {
            var lim = Support.Data.Economy.limits;
            int wall = -1;
            foreach (var a in Support.Ladder.Attempts)
                if (!a.QuotaMet) { wall = a.RewindsBefore; break; }
            Support.Line($"할당량 {Support.Data.Economy.quotaCoin} · 벽은 되감기 {wall}회에서 처음 나타난다");
            Assert.That(wall, Is.InRange(lim.wallMinRewinds, lim.wallWithinRewinds),
                $"벽이 되감기 {wall}회에 있다. 너무 이르면 되감기가 쓸모없고, 너무 늦으면 흔적이 느껴지지 않는다.");
        }

        [Test]
        public void 되감기_한도가_실제로_소진된다()
        {
            // 한도가 닿지 않으면 그 수는 데이터에만 있는 수다.
            Assert.That(Support.StubbornLadder.RewindsUsed, Is.EqualTo(Support.Data.Plots.rewind.maxRewinds));
            Assert.That(Support.StubbornLadder.Attempts.Count, Is.EqualTo(Support.Data.Plots.rewind.maxRewinds + 1));
        }

        [Test]
        public void 되감기마다_봄의_토질이_반드시_줄어든다_바닥까지()
        {
            // 흔적의 정의 그 자체. 한 번이라도 줄지 않으면 그 되감기는 공짜였다.
            var d = Support.Data;
            var ladder = Support.Ladder.Attempts;
            int floorSum = d.Plots.plots.Length * d.Plots.rewind.soilFloor;
            for (int i = 1; i < ladder.Count; i++)
            {
                int prev = ladder[i - 1].SoilSumAtStart;
                int now = ladder[i].SoilSumAtStart;
                if (prev <= floorSum) { Assert.That(now, Is.EqualTo(prev)); continue; }
                Assert.That(now, Is.LessThan(prev),
                    $"되감기 {ladder[i].RewindsBefore}회: 봄 토질이 {prev} → {now}. 줄지 않았다 — 되감기가 공짜다.");
            }
            Support.Line($"봄 토질 {ladder[0].SoilSumAtStart} → {ladder[ladder.Count - 1].SoilSumAtStart} (바닥 합 {floorSum})");
        }

        [Test]
        public void 흔적이_작물_사다리를_한_칸씩_내려가게_만든다()
        {
            // 값비싼 작물을 받을 수 있는 칸 수가 되감기마다 줄어야 흔적이 화면에서 읽힌다.
            var d = Support.Data;
            Data.CropDef top = null;
            foreach (var c in d.Crops.crops) if (top == null || c.minSoil > top.minSoil) top = c;

            int firstCount = -1, lastCount = -1;
            foreach (var a in Support.Ladder.Attempts)
            {
                // 봄 토질 합만 기록되어 있으므로, 칸별 토질은 시작값에서 되감기 손실을 뺀 값으로 다시 센다.
                int loss = a.RewindsBefore * d.Plots.rewind.soilLossPerRewind;
                int n = 0;
                foreach (var p in d.Plots.plots)
                {
                    int soil = p.soil - loss;
                    if (soil < d.Plots.rewind.soilFloor) soil = d.Plots.rewind.soilFloor;
                    if (soil >= top.minSoil) n++;
                }
                if (firstCount < 0) firstCount = n;
                lastCount = n;
            }
            Support.Line($"{top.nameKo}(minSoil {top.minSoil})를 받을 수 있는 칸: {firstCount} → {lastCount}");
            Assert.That(lastCount, Is.LessThan(firstCount),
                $"{top.nameKo}를 받을 수 있는 칸 수가 줄지 않았다 — 흔적이 선택지를 좁히지 못한다.");
            Assert.That(lastCount, Is.Zero, $"사다리 끝에서도 {top.nameKo}를 심을 수 있다 — 흔적이 바닥에 닿지 않는다.");
        }
    }
}
