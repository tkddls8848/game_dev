using NUnit.Framework;
using Phone.Data;

namespace Phone.Tests
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
            Assert.That(Localization.Text("lock.pushed", "밀려 사라졌다. 돌아오지 않는다"),
                Is.EqualTo("밀려 사라졌다. 돌아오지 않는다"));
            Localization.Language = "fr";     // 표가 없는 언어
            Assert.That(Localization.Text("lock.pushed", "밀려 사라졌다. 돌아오지 않는다"),
                Is.EqualTo("밀려 사라졌다. 돌아오지 않는다"), "표가 없을 때 화면이 비면 안 된다");
            Localization.Language = "en";
            Assert.That(Localization.Text("lock.missing.key", "없는 열쇠"), Is.EqualTo("없는 열쇠"));
        }

        [Test]
        public void 언어를_바꾸면_값이_따라온다()
        {
            // 정적 필드에 굳으면 여기서 걸린다.
            Localization.Language = "ko";
            string ko = Localization.Text("lock.locked", "잠겨 있다. 비밀번호는 오지 않는다");
            Localization.Language = "en";
            string en = Localization.Text("lock.locked", "잠겨 있다. 비밀번호는 오지 않는다");
            Assert.That(en, Is.Not.EqualTo(ko));
            Localization.Language = "ko";
            Assert.That(Localization.Text("lock.locked", "잠겨 있다. 비밀번호는 오지 않는다"), Is.EqualTo(ko));
        }

        [Test]
        public void 자리표시자를_쓰고_조각을_잇지_않는다()
        {
            Localization.Language = "en";
            string s = Localization.Text("exif.taken", "{0}에 {1}에서 찍었다", "20:06", "자택 계단참");
            Assert.That(s, Does.Contain("20:06"));
            Assert.That(s, Does.Not.Contain("{0}"));
            foreach (string key in Localization.KeysFor("en"))
                Assert.That(Localization.Text(key, ""), Is.Not.Null.And.Not.Empty, key + " 의 영어가 비어 있다");
        }

        [Test]
        public void 데이터의_한국어가_비어_있지_않다()
        {
            GameData d = TestWorld.Data;
            foreach (NotifDef n in d.Notifs.notifications)
            {
                Assert.That(n.previewKo, Is.Not.Empty, n.id);
                Assert.That(n.fromKo, Is.Not.Empty, n.id);
            }
            foreach (FactDef f in d.Case.facts) Assert.That(f.ko, Is.Not.Empty, f.id);
            foreach (VerdictDef v in d.Case.verdicts)
            {
                Assert.That(v.questionKo, Is.Not.Empty, v.id);
                foreach (OptionDef o in v.options) Assert.That(o.ko, Is.Not.Empty, v.id + "·" + o.id);
            }
            foreach (LockedDef l in d.Locked.locked)
            {
                Assert.That(l.ko, Is.Not.Empty, l.id);
                Assert.That(l.whyKo, Is.Not.Empty, l.id);
            }
            TestContext.WriteLine("데이터의 한국어 원문이 전부 채워져 있다");
        }
    }
}
