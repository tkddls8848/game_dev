using System.Collections.Generic;
using NUnit.Framework;
using SilentBaton.Data;
using SilentBaton.Sim;

namespace SilentBaton.Tests
{
    /// <summary>
    /// ★ 핵 검사기 둘 — `DelayedFeedbackMatters`.
    ///
    /// **즉시 피드백을 주면 쉬워지는가.**
    /// 즉시 알려 주는 세계와 다음 날 신문으로만 아는 세계를 **같은 씨드·같은 지휘 규칙**으로 돌려
    /// 성적이 유의하게 달라야 한다. 같으면 "뒤늦게 안다"가 장식이다.
    ///
    /// 두 갈래로 본다:
    ///   ① **벌로서** — 같은 정보 조건(신문 없음)에서 소절 끝마다 알려 주면 총점이 크게 오른다.
    ///      두 지휘자는 같은 `DeadReckoning` 과 같은 `BatonPlanner` 를 쓴다.
    ///      다른 것은 하나뿐이다: 소절이 끝날 때 진실을 듣는가.
    ///   ② **규칙으로서** — 뒤늦은 피드백에도 **쓸모가 있다.** 신문을 읽는 시즌이 버리는 시즌보다
    ///      후반에 유의하게 낫다. 이쪽이 없으면 지연은 그냥 벌이고 게임이 아니다.
    /// </summary>
    [TestFixture]
    public sealed class DelayedFeedbackMattersTests
    {
        [Test]
        public void 소절_끝마다_알려_주면_총점이_크게_오른다()
        {
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            int sumDelayed = 0, sumImmediate = 0, n = 0;
            int errDelayed = 0, errImmediate = 0;
            int worstGap = 9999;
            string worstWhere = null;

            TestContext.WriteLine("곡        씨드     | 다음날  즉시  차 | 어긋남 다음날/즉시");
            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    // 두 세계 모두 **신문이 없다.** 달라지는 것은 "제때 아는가" 하나뿐이다.
                    PerformanceResult later = TestWorld.Visible(pieceId, seed, null, VisibleChannel.None, seed);
                    PerformanceResult now = TestWorld.Immediate(pieceId, seed, seed);
                    int gap = now.Total - later.Total;
                    if (gap < worstGap) { worstGap = gap; worstWhere = pieceId + " 씨드 " + seed; }
                    sumDelayed += later.Total; sumImmediate += now.Total;
                    errDelayed += later.MeanAbsOffsetMs; errImmediate += now.MeanAbsOffsetMs;
                    n++;
                    TestContext.WriteLine(pieceId.PadRight(11) + seed.ToString().PadLeft(8) + " | "
                        + later.Total.ToString().PadLeft(6) + now.Total.ToString().PadLeft(6)
                        + gap.ToString().PadLeft(5) + " | " + later.MeanAbsOffsetMs + "ms / "
                        + now.MeanAbsOffsetMs + "ms");
                }

            int meanGap = (sumImmediate - sumDelayed) / n;
            Assert.That(meanGap, Is.GreaterThanOrEqualTo(cb.minImmediateAdvantagePoints),
                "즉시 알려 줘도 평균 " + meanGap + "점만 오른다 — '뒤늦게 안다'가 장식이라는 뜻이다");

            int dropPercent = errDelayed == 0 ? 0 : (errDelayed - errImmediate) * 100 / errDelayed;
            Assert.That(dropPercent, Is.GreaterThanOrEqualTo(cb.minImmediateErrorDropPercent),
                "즉시 알려 줘도 어긋남이 " + dropPercent + "%만 줄어든다");

            TestContext.WriteLine("── 평균 총점 다음날 " + (sumDelayed / n) + " vs 즉시 " + (sumImmediate / n)
                + " (차 " + meanGap + "점, 기준 " + cb.minImmediateAdvantagePoints + ")");
            TestContext.WriteLine("── 평균 어긋남 다음날 " + (errDelayed / n) + "ms vs 즉시 "
                + (errImmediate / n) + "ms (" + dropPercent + "% 감소, 기준 "
                + cb.minImmediateErrorDropPercent + "%)");
            TestContext.WriteLine("── 가장 작은 차 " + worstGap + "점 (" + worstWhere + ")");
        }

        [Test]
        public void 두_세계는_같은_지휘_규칙을_쓰고_다른_것은_언제_아는가_하나뿐이다()
        {
            // 비교가 성립하는지를 먼저 못 박는다. 규칙이 다르면 무엇을 재는지 알 수 없다.
            GameData d = TestWorld.Data;
            string src = VisualCuesSufficeTests.SrcRoot();
            string immediate = System.IO.File.ReadAllText(System.IO.Path.Combine(src, "Conductor.cs"));
            Assert.That(immediate, Does.Contain("new DeadReckoning"),
                "즉시 피드백 지휘자가 같은 머릿속 지도를 쓰지 않는다");
            Assert.That(immediate, Does.Contain("BatonPlanner.Plan"),
                "즉시 피드백 지휘자가 같은 지휘 규칙을 쓰지 않는다");
            string game = System.IO.File.ReadAllText(System.IO.Path.Combine(src, "VisibleOnlyConductor.cs"));
            Assert.That(game, Does.Contain("new DeadReckoning"));
            Assert.That(game, Does.Contain("BatonPlanner.Plan"));

            // 첫 소절이 끝나기 전에는 두 세계가 완전히 같아야 한다 — 아직 알려 준 것이 없다.
            string pieceId = TestWorld.PieceIds()[0];
            int seed = TestWorld.Seeds[0];
            PerformanceResult later = TestWorld.Visible(pieceId, seed, null, VisibleChannel.None, seed);
            PerformanceResult now = TestWorld.Immediate(pieceId, seed, seed);
            int firstBarBeats = d.Piece(pieceId).bars[0].beats;
            for (int i = 0; i < firstBarBeats; i++)
                Assert.That(now.Log[i].Given.ToString(), Is.EqualTo(later.Log[i].Given.ToString()),
                    "첫 소절 " + (i + 1) + "번째 박에서 두 세계가 갈라졌다 — 아직 알려 준 것이 없는데 다르다");
            Assert.That(now.BatonKey, Is.Not.EqualTo(later.BatonKey),
                "끝까지 지휘가 같다 — 알려 준 것이 아무것도 바꾸지 않았다");
            TestContext.WriteLine("첫 소절 " + firstBarBeats + "박은 두 세계가 같고, 그 뒤부터 갈라진다");
        }

        [Test]
        public void 신문을_읽는_시즌이_버리는_시즌보다_후반에_낫다()
        {
            // ★ 이쪽이 없으면 지연은 벌이고 규칙이 아니다.
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            int late = d.Balance.season.lateFrom;
            int sumRead = 0, sumBlind = 0, seasons = 0, worst = 9999;

            foreach (int seed in TestWorld.Seeds)
            {
                List<ConcertResult> read = Season.Run(d, seed, p => new VisibleOnlyConductor(p), true);
                List<ConcertResult> blind = Season.Run(d, seed, p => new VisibleOnlyConductor(p), false);

                // 1회차는 읽을 신문이 없으므로 두 시즌이 반드시 같아야 한다 — 비교의 기준점이다.
                Assert.That(read[0].Total, Is.EqualTo(blind[0].Total),
                    "1회차에 이미 다르다 — 신문을 읽기 전인데 무엇인가 새고 있다");

                int r = Season.MeanTotal(read, late), b = Season.MeanTotal(blind, late);
                if (r - b < worst) worst = r - b;
                sumRead += r; sumBlind += b; seasons++;
                TestContext.WriteLine("시즌 " + seed + " 후반(" + late + "회차 이후) 신문 " + r
                    + " vs 버림 " + b + " (차 " + (r - b) + ")");
                TestContext.WriteLine("   읽음: " + Season.OneLine(read));
                TestContext.WriteLine("   버림: " + Season.OneLine(blind));
            }

            int mean = (sumRead - sumBlind) / seasons;
            Assert.That(mean, Is.GreaterThanOrEqualTo(cb.minPressAdvantagePoints),
                "신문을 읽어도 후반 평균이 " + mean + "점만 오른다 — 뒤늦은 피드백에 쓸모가 없다");
            Assert.That(worst, Is.GreaterThanOrEqualTo(cb.minPressSeasonWorstGap),
                "어떤 시즌에서는 신문을 읽어도 " + worst + "점만 오른다");
            TestContext.WriteLine("── 후반 평균 차 " + mean + "점 (기준 " + cb.minPressAdvantagePoints
                + ") · 가장 나쁜 시즌도 " + worst + "점 (기준 " + cb.minPressSeasonWorstGap + ")");
        }

        [Test]
        public void 신문은_회차가_지날수록_단원의_성향에_가까워진다()
        {
            // 뒤늦은 피드백이 **무엇을** 알려 주는지 못 박는다. 이것이 흐리면 위 검사가 왜 통과했는지 모른다.
            GameData d = TestWorld.Data;
            foreach (int seed in TestWorld.Seeds)
            {
                Performance probe = new Performance(d, d.AllPieces[0], d.AllPieces[0].seed);
                int[] truth = probe.Cast(seed).TraitBiasMs;
                PressKnowledge press = PressKnowledge.Empty(d.SectionCount);
                System.Random seeds = new System.Random(seed);
                int firstError = -1, lastError = -1;

                for (int c = 1; c <= d.Balance.season.concerts; c++)
                {
                    PieceDef piece = d.AllPieces[(c - 1) % d.AllPieces.Length];
                    int night = piece.seed + seeds.Next(1, 1000000);
                    Performance perf = new Performance(d, piece, night);
                    press.Read(d, perf.Run(new VisibleOnlyConductor(press), perf.Cast(seed)));

                    // 활이 없는 무리(신문이 알려 줄 수 있는 유일한 대상)의 추정 오차만 센다.
                    int err = 0, counted = 0;
                    for (int i = 0; i < d.SectionCount; i++)
                    {
                        if (d.AllSections[i].bowVisible) continue;
                        if (press.NotesAbout(i) == 0) continue;
                        err += System.Math.Abs(press.TraitEstimateMs[i] - truth[i]);
                        counted++;
                    }
                    if (counted == 0) continue;
                    int mean = err / counted;
                    if (firstError < 0) firstError = mean;
                    lastError = mean;
                }

                Assert.That(firstError, Is.GreaterThanOrEqualTo(0),
                    "시즌 " + seed + " 에서 신문이 활 없는 무리를 한 번도 지적하지 않았다");
                Assert.That(lastError, Is.LessThanOrEqualTo(d.Balance.drift.nightlyBiasRangeMs + 3),
                    "시즌 " + seed + " 끝에도 추정 오차가 " + lastError + "ms 다 — 신문이 성향을 알려 주지 못한다");
                TestContext.WriteLine("시즌 " + seed + ": 참 성향 " + string.Join(", ", truth)
                    + " → 첫 지적 뒤 오차 " + firstError + "ms · 시즌 끝 오차 " + lastError + "ms");
            }
        }
    }
}
