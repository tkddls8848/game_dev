using System.Collections.Generic;
using NUnit.Framework;
using Scent.Data;

namespace Scent.Tests
{
    /// <summary>뿌리 CLAUDE.md 설계 원칙 6이 코드에서 실제로 지켜지는지 본다.</summary>
    [TestFixture]
    public sealed class LocalizationTests
    {
        [TearDown]
        public void Reset() { Localization.Language = "ko"; }

        [Test]
        public void 표가_없으면_한국어_원문이_그대로_나온다()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("nose.masked", "다른 냄새에 묻혀 언제인지 모르겠다"),
                Is.EqualTo("다른 냄새에 묻혀 언제인지 모르겠다"));
            Localization.Language = "fr";     // 표가 없는 언어
            Assert.That(Localization.Text("nose.masked", "다른 냄새에 묻혀 언제인지 모르겠다"),
                Is.EqualTo("다른 냄새에 묻혀 언제인지 모르겠다"), "표가 없을 때 화면이 비면 안 된다");
            Localization.Language = "en";
            Assert.That(Localization.Text("nose.missing.key", "없는 열쇠"), Is.EqualTo("없는 열쇠"),
                "열쇠가 없을 때도 원문으로 떨어져야 한다");
        }

        [Test]
        public void 언어를_바꾸면_값이_따라온다()
        {
            // 정적 필드에 굳으면 여기서 걸린다.
            Localization.Language = "ko";
            string ko = Localization.Text("nose.nothing", "코에 걸리는 것이 없다");
            Localization.Language = "en";
            string en = Localization.Text("nose.nothing", "코에 걸리는 것이 없다");
            Assert.That(en, Is.Not.EqualTo(ko));
            Localization.Language = "ko";
            Assert.That(Localization.Text("nose.nothing", "코에 걸리는 것이 없다"), Is.EqualTo(ko));
        }

        [Test]
        public void 자리표시자를_쓰고_조각을_잇지_않는다()
        {
            Localization.Language = "en";
            string s = Localization.Text("nose.dated", "{0} 이 {1} 에 여기 있었다", "의사", "17:30");
            Assert.That(s, Does.Contain("17:30"));
            Assert.That(s, Does.Not.Contain("{0}"));
            foreach (string key in Localization.KeysFor("en"))
            {
                string v = Localization.Text(key, "");
                Assert.That(v, Is.Not.Null.And.Not.Empty, key + " 의 영어가 비어 있다");
            }
        }

        [Test]
        public void 데이터의_한국어가_비어_있지_않다()
        {
            GameData d = TestWorld.Data;
            foreach (NoteDef n in d.Notes.notes) Assert.That(n.ko, Is.Not.Empty, n.id);
            foreach (RoomDef r in d.House.rooms) Assert.That(r.ko, Is.Not.Empty, r.id);
            foreach (ActorDef a in d.Actors.actors)
            {
                Assert.That(a.ko, Is.Not.Empty, a.id);
                Assert.That(a.role, Is.Not.Empty, a.id);
            }
            foreach (VisitDef v in d.Day.visits) Assert.That(v.ko, Is.Not.Empty, v.id);
            TestContext.WriteLine("데이터의 한국어 원문이 전부 채워져 있다");
        }
    }
}
