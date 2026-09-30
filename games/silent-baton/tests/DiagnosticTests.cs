using System.Collections.Generic;
using NUnit.Framework;
using SilentBaton.Data;
using SilentBaton.Sim;

namespace SilentBaton.Tests
{
    /// <summary>아무것도 단정하지 않고 **수치만 찍는다.** 데이터를 고칠 때 보는 창이다.</summary>
    [TestFixture]
    public sealed class DiagnosticTests
    {
        [Test]
        public void 세_세계의_성적을_곡별로_찍는다()
        {
            GameData d = TestWorld.Data;
            foreach (string pieceId in TestWorld.PieceIds())
                foreach (int seed in TestWorld.Seeds)
                {
                    PerformanceResult v = TestWorld.Visible(pieceId, seed);
                    PerformanceResult h = TestWorld.Hearing(pieceId, seed);
                    PerformanceResult i = TestWorld.Immediate(pieceId, seed);
                    TestContext.WriteLine(pieceId + " 씨드 " + seed
                        + "\n   보임 " + v.Total + "(" + v.Review.GradeName + ") 어긋남 " + v.MeanAbsOffsetMs
                        + "ms 벌어짐 " + v.MeanSpreadMs + "ms 세기 " + v.MeanAbsDynError
                        + "\n   들림 " + h.Total + "(" + h.Review.GradeName + ") 어긋남 " + h.MeanAbsOffsetMs
                        + "ms 벌어짐 " + h.MeanSpreadMs + "ms 세기 " + h.MeanAbsDynError
                        + "\n   즉시 " + i.Total + "(" + i.Review.GradeName + ") 어긋남 " + i.MeanAbsOffsetMs
                        + "ms 벌어짐 " + i.MeanSpreadMs + "ms 세기 " + i.MeanAbsDynError);
                }
        }

        [Test]
        public void 채널을_하나씩_가려_본다()
        {
            foreach (string pieceId in TestWorld.PieceIds())
            {
                int seed = TestWorld.Seeds[0];
                PerformanceResult full = TestWorld.Visible(pieceId, seed);
                string line = pieceId + " 전부 " + full.Total;
                foreach (VisibleChannel c in VisibleChannels.All)
                    line += " · " + VisibleChannels.Korean(c) + " 가림 "
                        + TestWorld.Visible(pieceId, seed, null, c).Total;
                TestContext.WriteLine(line);
            }
        }

        [Test]
        public void 한_무리_한_동작만_되풀이해_본다()
        {
            GameData d = TestWorld.Data;
            string pieceId = TestWorld.PieceIds()[0];
            int seed = TestWorld.Seeds[0];
            int best = -1; string bestLabel = null;
            for (int s = 0; s < d.SectionCount; s++)
                foreach (CueDef c in d.AllCues)
                {
                    string label = d.AllSections[s].name + "/" + c.name;
                    PerformanceResult r = TestWorld.Play(pieceId, seed, new MonoConductor(s, c.id, label));
                    if (r.Total > best) { best = r.Total; bestLabel = label; }
                }
            TestContext.WriteLine("한 동작 되풀이의 최고: " + bestLabel + " " + best
                + "점 (합격선 " + d.Balance.review.passTotal + ")");
        }

        [Test]
        public void 시즌_둘을_나란히_돌려_본다()
        {
            GameData d = TestWorld.Data;
            foreach (int seed in TestWorld.Seeds)
            {
                List<ConcertResult> read = Season.Run(d, seed, p => new VisibleOnlyConductor(p), true);
                List<ConcertResult> blind = Season.Run(d, seed, p => new VisibleOnlyConductor(p), false);
                TestContext.WriteLine("시즌 씨드 " + seed);
                TestContext.WriteLine("   신문 읽음: " + Season.OneLine(read)
                    + " → 평균 " + Season.MeanTotal(read) + " · 후반 " + Season.MeanTotal(read, d.Balance.season.lateFrom));
                TestContext.WriteLine("   신문 버림: " + Season.OneLine(blind)
                    + " → 평균 " + Season.MeanTotal(blind) + " · 후반 " + Season.MeanTotal(blind, d.Balance.season.lateFrom));
            }
        }

        [Test]
        public void 한_연주의_박을_줄줄이_찍는다()
        {
            GameData d = TestWorld.Data;
            PerformanceResult r = TestWorld.Visible(TestWorld.PieceIds()[0], TestWorld.Seeds[0]);
            TestContext.WriteLine("지휘자: " + r.ConductorName + " → " + r.Review.OneLine);
            foreach (BeatRecord b in r.Log)
            {
                string seen = "";
                for (int i = 0; i < d.SectionCount; i++)
                    if (b.Seen.Playing[i])
                        seen += d.AllSections[i].name + "[숨" + b.Seen.Breath[i] + " 활"
                            + (b.Seen.BowReadable[i] ? b.Seen.Bow[i].ToString() : "-")
                            + " 낯" + b.Seen.Face[i] + "] ";
                TestContext.WriteLine(b.Bar + "-" + b.Beat + " " + seen + "→ "
                    + d.AllSections[b.Given.SectionIndex].name + " " + d.Cue(b.Given.CueId).name
                    + " · 벌어짐 " + b.SpreadMs + "ms");
            }
            TestContext.WriteLine("쏠림(숨은 값): " + string.Join(", ", r.Final.BiasMs));
        }

        [Test]
        public void 신문이_되짚은_성향을_참값과_견준다()
        {
            GameData d = TestWorld.Data;
            foreach (int seed in TestWorld.Seeds)
            {
                PressKnowledge press = PressKnowledge.Empty(d.SectionCount);
                Performance probe = new Performance(d, d.AllPieces[0], d.AllPieces[0].seed);
                Players cast = probe.Cast(seed);
                TestContext.WriteLine("시즌 씨드 " + seed + " 참 성향 " + string.Join(", ", cast.TraitBiasMs));
                System.Random seeds = new System.Random(seed);
                for (int c = 1; c <= d.Balance.season.concerts; c++)
                {
                    PieceDef piece = d.AllPieces[(c - 1) % d.AllPieces.Length];
                    int night = piece.seed + seeds.Next(1, 1000000);
                    Performance perf = new Performance(d, piece, night);
                    PerformanceResult r = perf.Run(new VisibleOnlyConductor(press), perf.Cast(seed));
                    press.Read(d, r);
                    TestContext.WriteLine("   " + c + "회 뒤 추정 " + string.Join(", ", press.TraitEstimateMs)
                        + "  (적힌 횟수 " + press.NotesAbout(0) + "," + press.NotesAbout(1) + ","
                        + press.NotesAbout(2) + "," + press.NotesAbout(3) + ")");
                }
            }
        }
    }
}
