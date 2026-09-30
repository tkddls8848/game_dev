using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using SilentBaton.Data;
using SilentBaton.Sim;

namespace SilentBaton.Tests
{
    /// <summary>
    /// 인계본이 남긴 수용 검증. **핵 검사기는 `VisualCuesSufficeTests` · `DelayedFeedbackMattersTests` 에 있고,**
    /// 이 파일은 그 둘이 기대는 **불변식**과 브라우저 대조용 붙박이(fixture) 내보내기를 맡는다.
    ///
    /// 인계 시점의 두 단정은 지금 데이터에서 거짓이 되어 고쳤다. 무엇을 왜 고쳤는지 남긴다:
    ///   ① 「백지 상태로도 모든 곡에서 합격한다」 — 이제 거짓이다.
    ///      시작 어긋남(startOffsetRangeMs)과 무리별 눈금을 넣은 뒤 **신문을 읽어야** 넘는다.
    ///      그게 `DelayedFeedbackMatters` 가 재는 것이므로 여기서는 **신문을 읽은 뒤**로 고쳤다.
    ///   ② `VisibleFrame` 의 칸 목록 — 무리별 눈금 세 칸이 늘었다. 셋 다 보이는 것이다
    ///      (지휘자는 자기 악단이 어떻게 읽히는지 안다). 목록을 갱신했다.
    /// </summary>
    public class AcceptanceTests
    {
        [TestCase(20260927)] [TestCase(91)] [TestCase(4404)] [TestCase(77713)]
        public void VisualCuesSuffice(int seed)
        {
            GameData d = TestWorld.Data;
            PressKnowledge press = TuningTests.Warmed(d, seed);
            foreach (var id in TestWorld.PieceIds())
                Assert.That(TestWorld.Visible(id, seed, press, VisibleChannel.None, seed).Passed, Is.True, id);
            Assert.That(typeof(IHearsEveryBeat).IsAssignableFrom(typeof(VisibleOnlyConductor)), Is.False);
            Assert.That(typeof(IHearsBarEnd).IsAssignableFrom(typeof(VisibleOnlyConductor)), Is.False);
        }

        [TestCase(20260927)] [TestCase(91)] [TestCase(4404)] [TestCase(77713)]
        public void DelayedFeedbackMatters(int seed)
        {
            var d = TestWorld.Data;
            var read = Season.Run(d, seed, p => new VisibleOnlyConductor(p), true);
            var blind = Season.Run(d, seed, p => new VisibleOnlyConductor(p), false);
            Assert.That(read[0].Total, Is.EqualTo(blind[0].Total), "First concert has no newspaper yet");
            Assert.That(Season.MeanTotal(read, d.Balance.season.lateFrom),
                Is.GreaterThan(Season.MeanTotal(blind, d.Balance.season.lateFrom)));
        }

        [Test]
        public void SeenFrameContainsOnlyVisibleInformation()
        {
            // 무리별 눈금 셋이 늘었다. 지휘자는 자기 악단이 어떻게 읽히는지 알고 있다 — 소리가 아니다.
            CollectionAssert.AreEquivalent(new[]{"Beat","Bar","RequiredDynamic","Breath","Bow","Face",
                    "Playing","BowReadable","BreathQuantMs","BowQuantMs","FaceQuantLevel"},
                typeof(VisibleFrame).GetFields().Select(f=>f.Name));
            foreach(var id in TestWorld.PieceIds()) {
                var r=TestWorld.Visible(id,91);
                foreach(var b in r.Log) for(int i=0;i<TestWorld.Data.SectionCount;i++) {
                    Assert.That(Math.Abs(b.Seen.Breath[i]),Is.LessThanOrEqualTo(TestWorld.Data.Balance.visible.breathLevels));
                    Assert.That(Math.Abs(b.Seen.Bow[i]),Is.LessThanOrEqualTo(TestWorld.Data.Balance.visible.bowLevels));
                    Assert.That(Math.Abs(b.Seen.Face[i]),Is.LessThanOrEqualTo(TestWorld.Data.Balance.visible.faceLevels));
                    if(!b.Seen.BowReadable[i]) Assert.That(b.Seen.Bow[i],Is.Zero);
                    if(!b.Seen.Playing[i]) {
                        Assert.That(b.Seen.Breath[i],Is.Zero);
                        Assert.That(b.Seen.Face[i],Is.Zero);
                    }
                }
            }
        }

        [Test]
        public void ReplayIsDeterministicAndDoesNotMutateCast()
        {
            var d=TestWorld.Data; var piece=d.AllPieces[0]; var p=new Performance(d,piece,91); var cast=p.Cast(91);
            string before=JsonConvert.SerializeObject(cast);
            var a=p.Run(new VisibleOnlyConductor(),cast);var b=p.Run(new VisibleOnlyConductor(),cast);
            Assert.That(a.SeenKey,Is.EqualTo(b.SeenKey));Assert.That(a.Total,Is.EqualTo(b.Total));
            Assert.That(JsonConvert.SerializeObject(cast),Is.EqualTo(before));
        }

        /// <summary>
        /// 브라우저가 C# 과 **같은 수**를 내는지 대조할 붙박이를 내보낸다.
        /// `node tests/browser-parity.js` 가 이것을 읽어 `presentation/engine.js` 와 박 단위로 견준다.
        /// 목업이 규칙을 따로 구현하므로 이 대조가 없으면 화면이 조용히 거짓이 된다.
        /// </summary>
        [Test]
        public void ExportBrowserFixtures()
        {
            var d=TestWorld.Data;var fixtures=new List<object>();var casts=new List<object>();
            foreach(var piece in d.AllPieces) foreach(int seed in TestWorld.Seeds) {
                var p=new Performance(d,piece,seed);var cast=p.Cast(d.Score.seasonSeed);
                if(seed==TestWorld.Seeds[0])casts.Add(new{pieceId=piece.id,seed,cast});
                var conductors=new List<IConductor>{new VisibleOnlyConductor(),new HearingConductor(),
                                                    new ImmediateFeedbackConductor()};
                foreach(var cue in d.AllCues) for(int s=0;s<d.SectionCount;s++) conductors.Add(new MonoConductor(s,cue.id,cue.name));
                foreach(var c in conductors) {
                    var r=p.Run(c,cast);
                    Assert.That(r.Log.Count,Is.EqualTo(piece.TotalBeats));
                    Assert.That(r.Total,Is.InRange(0,100));
                    fixtures.Add(new{pieceId=piece.id,seed,cast,log=r.Log,total=r.Total,review=r.Review});
                }
            }
            string outDir=Path.Combine(d.DataRoot,"..","TestBuild");
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir,"browser-fixtures.json"),JsonConvert.SerializeObject(fixtures));
            File.WriteAllText(Path.Combine(d.DataRoot,"browser-casts.json"),JsonConvert.SerializeObject(casts,Formatting.Indented));
            TestContext.WriteLine("붙박이 "+fixtures.Count+"회 연주를 TestBuild/browser-fixtures.json 에 내보냈다 — "
                +"node tests/browser-parity.js 가 이것으로 브라우저를 대조한다");
        }
    }
}
