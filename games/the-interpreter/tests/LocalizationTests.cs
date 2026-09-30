using NUnit.Framework;

namespace Interp.Tests
{
    /// <summary>
    /// 설계 원칙 6. 한국어가 원문, 영어는 덮어쓰기.
    /// 정적 필드에 굳지 않는지, 없는 열쇠에도 화면이 비지 않는지.
    /// </summary>
    [TestFixture]
    public class LocalizationTests
    {
        [TearDown]
        public void Reset() { Localization.Language = "ko"; }

        [Test]
        public void KoreanIsTheSource()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("register.exact", "정확"), Is.EqualTo("정확"));
            Assert.That(Localization.Text("없는.열쇠", "원문이 그대로 나온다"), Is.EqualTo("원문이 그대로 나온다"));
        }

        [Test]
        public void EnglishOverwritesAndDoesNotFreeze()
        {
            Localization.Language = "ko";
            string first = Localization.Register("false");
            Localization.Language = "en";
            string second = Localization.Register("false");
            Localization.Language = "ko";
            string third = Localization.Register("false");

            Assert.That(first, Is.EqualTo("오역"));
            Assert.That(second, Is.EqualTo("Mistranslated"));
            Assert.That(third, Is.EqualTo("오역"), "언어를 되돌렸는데 첫 값이 굳었다");
        }

        [Test]
        public void PlaceholdersNotConcatenation()
        {
            Localization.Language = "en";
            string s = Localization.Text("misread.standing", "「{0}」 — 하란: {1} / 케리아: {2}", "유감", "사죄", "유감");
            Assert.That(s, Does.Contain("one side heard"));
            Assert.That(s, Does.Contain("유감"));
        }

        [Test]
        public void MissingEnglishFallsBackToKorean()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("아직.번역.안.한.것", "한국어 원문"), Is.EqualTo("한국어 원문"));
        }
    }
}
