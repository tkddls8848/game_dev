using NUnit.Framework;

namespace Tactics.Tests
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문, 영어가 덮어쓰기.
    /// **정적 필드에 담지 않는다** — 언어를 바꿔도 첫 값이 굳는다. 그걸 여기서 못 박는다.
    /// </summary>
    [TestFixture]
    public sealed class LocalizationTests
    {
        [TearDown]
        public void 되돌린다() { Localization.Language = "ko"; }

        [Test]
        public void 표가_없으면_한국어_원문이_나온다()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("fail.alarm", "경보가 올랐다"), Is.EqualTo("경보가 올랐다"));
            Localization.Language = "fr";
            Assert.That(Localization.Text("fail.alarm", "경보가 올랐다"), Is.EqualTo("경보가 올랐다"),
                "표가 없는 언어에서 화면이 비었다");
            Localization.Language = "en";
            Assert.That(Localization.Text("no.such.key", "없는 열쇠"), Is.EqualTo("없는 열쇠"));
        }

        [Test]
        public void 언어를_바꾸면_같은_호출이_다른_값을_낸다()
        {
            Localization.Language = "ko";
            string ko = Localization.Text("fail.alarm", "경보가 올랐다");
            Localization.Language = "en";
            string en = Localization.Text("fail.alarm", "경보가 올랐다");
            Localization.Language = "ko";
            string again = Localization.Text("fail.alarm", "경보가 올랐다");

            Assert.That(en, Is.Not.EqualTo(ko), "영어 덮어쓰기가 적용되지 않았다");
            Assert.That(again, Is.EqualTo(ko), "첫 값이 굳었다 — 정적 필드에 담긴 것이다");
        }

        [Test]
        public void 조각을_잇지_않고_자리표시자를_쓴다()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("x", "{0} 가 {1} 에서 들켰다", "정찰병", "수레길"),
                Is.EqualTo("정찰병 가 수레길 에서 들켰다"));
        }
    }
}
