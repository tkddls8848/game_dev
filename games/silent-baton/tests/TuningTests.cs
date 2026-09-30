using System;
using System.Collections.Generic;
using NUnit.Framework;
using SilentBaton.Data;
using SilentBaton.Sim;

namespace SilentBaton.Tests
{
    /// <summary>
    /// 데이터를 고칠 때 보는 한 장. **아무것도 단정하지 않는다.**
    /// 검사기가 요구하는 수치 전부를 한 표로 찍어 어디를 고쳐야 하는지 보여 준다.
    /// </summary>
    [TestFixture]
    public sealed class TuningTests
    {
        /// <summary>신문을 두 번 읽은 상태를 만든다 — 서곡·느린 악장을 먼저 치른 지휘자.</summary>
        public static PressKnowledge Warmed(GameData d, int seasonSeed, int papers = 2)
        {
            PressKnowledge press = PressKnowledge.Empty(d.SectionCount);
            Random seeds = new Random(seasonSeed);
            for (int c = 1; c <= papers; c++)
            {
                PieceDef piece = d.AllPieces[(c - 1) % d.AllPieces.Length];
                int night = piece.seed + seeds.Next(1, 1000000);
                Performance perf = new Performance(d, piece, night);
                press.Read(d, perf.Run(new VisibleOnlyConductor(press), perf.Cast(seasonSeed)));
            }
            return press;
        }

        [Test]
        public void 튜닝판()
        {
            GameData d = TestWorld.Data;
            int pass = d.Balance.review.passTotal;
            TestContext.WriteLine("합격선 " + pass);
            TestContext.WriteLine("곡        씨드  | 보임(백지) 보임(신문) 들림 즉시 | 숨가림 활가림 낯가림 | 한동작최고");

            int coldFail = 0, warmFail = 0, monoPass = 0;
            int sumVis = 0, sumImm = 0, sumHear = 0, sumCold = 0, n = 0;
            int[] ablDrop = new int[3];
            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    PressKnowledge press = Warmed(d, seed);
                    int cold = TestWorld.Visible(pieceId, seed, null, VisibleChannel.None, seed).Total;
                    int warm = TestWorld.Visible(pieceId, seed, press, VisibleChannel.None, seed).Total;
                    int hear = TestWorld.Hearing(pieceId, seed, seed).Total;
                    int imm = TestWorld.Immediate(pieceId, seed, seed).Total;
                    int[] abl = new int[3];
                    for (int k = 0; k < 3; k++)
                        abl[k] = TestWorld.Visible(pieceId, seed, press, VisibleChannels.All[k], seed).Total;
                    int mono = MonoBest(d, pieceId, seed);
                    if (cold < pass) coldFail++;
                    if (warm < pass) warmFail++;
                    if (mono >= pass) monoPass++;
                    sumVis += warm; sumImm += imm; sumHear += hear; sumCold += cold; n++;
                    for (int k = 0; k < 3; k++) ablDrop[k] += warm - abl[k];

                    TestContext.WriteLine(pieceId.PadRight(11) + seed.ToString().PadLeft(8) + " | "
                        + cold.ToString().PadLeft(9) + warm.ToString().PadLeft(10)
                        + hear.ToString().PadLeft(5) + imm.ToString().PadLeft(5) + " | "
                        + abl[0].ToString().PadLeft(6) + abl[1].ToString().PadLeft(7)
                        + abl[2].ToString().PadLeft(7) + " | " + mono.ToString().PadLeft(6));
                }

            TestContext.WriteLine("── 백지 낙방 " + coldFail + "/" + n + " · 신문 낙방 " + warmFail + "/" + n
                + " · 한 동작 합격 " + monoPass + "/" + n);
            // 즉시 세계도 신문은 못 본다 — 그러므로 견줄 짝은 **백지**다. 같은 정보 조건에서 늦게 아는 것뿐이다.
            TestContext.WriteLine("── 평균 보임(백지) " + (sumCold / n) + " · 즉시 " + (sumImm / n)
                + " (차 " + ((sumImm - sumCold) / n) + ")  ← DelayedFeedbackMatters 가 보는 짝");
            TestContext.WriteLine("── 평균 보임(신문) " + (sumVis / n) + " · 들림 " + (sumHear / n)
                + " (차 " + ((sumHear - sumVis) / n) + ")");
            TestContext.WriteLine("── 채널 가림 평균 낙폭: 호흡 " + (ablDrop[0] / n) + " · 활 " + (ablDrop[1] / n)
                + " · 표정 " + (ablDrop[2] / n));

            int readSum = 0, blindSum = 0, readLate = 0, blindLate = 0, seeds = 0;
            int worstSeedGap = 999;
            foreach (int seed in TestWorld.Seeds)
            {
                List<ConcertResult> read = Season.Run(d, seed, pp => new VisibleOnlyConductor(pp), true);
                List<ConcertResult> blind = Season.Run(d, seed, pp => new VisibleOnlyConductor(pp), false);
                int late = d.Balance.season.lateFrom;
                readSum += Season.MeanTotal(read); blindSum += Season.MeanTotal(blind);
                readLate += Season.MeanTotal(read, late); blindLate += Season.MeanTotal(blind, late);
                int gap = Season.MeanTotal(read, late) - Season.MeanTotal(blind, late);
                if (gap < worstSeedGap) worstSeedGap = gap;
                seeds++;
            }
            TestContext.WriteLine("── 시즌 평균 신문 " + (readSum / seeds) + " vs 버림 " + (blindSum / seeds)
                + " · 후반 " + (readLate / seeds) + " vs " + (blindLate / seeds)
                + " (차 " + ((readLate - blindLate) / seeds) + ", 최악 시즌 차 " + worstSeedGap + ")");
        }

        public static int MonoBest(GameData d, string pieceId, int seed)
        {
            int best = -1;
            for (int s = 0; s < d.SectionCount; s++)
                foreach (CueDef c in d.AllCues)
                {
                    int t = TestWorld.Play(pieceId, seed,
                        new MonoConductor(s, c.id, d.AllSections[s].name + "/" + c.name), seed).Total;
                    if (t > best) best = t;
                }
            return best;
        }
    }
}
