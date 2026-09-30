using NUnit.Framework;
using RedPen.Sim;

namespace RedPen.Tests
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
            Assert.That(Localization.Text("policy.harsh", "늘 혹독하게"), Is.EqualTo("늘 혹독하게"));
            Assert.That(Localization.Text("없는.열쇠", "원문이 그대로 나온다"), Is.EqualTo("원문이 그대로 나온다"));
        }

        [Test]
        public void EnglishOverwritesAndDoesNotFreeze()
        {
            Localization.Language = "ko";
            string first = Policies.HarshPen.Name;
            Localization.Language = "en";
            string second = Policies.HarshPen.Name;
            Localization.Language = "ko";
            string third = Policies.HarshPen.Name;

            Assert.That(first, Is.EqualTo("늘 혹독하게"));
            Assert.That(second, Is.EqualTo("Always harsh"));
            Assert.That(third, Is.EqualTo("늘 혹독하게"), "언어를 되돌렸는데 첫 값이 굳었다");
        }

        [Test]
        public void PlaceholdersNotConcatenation()
        {
            Localization.Language = "en";
            string s = Localization.Text("author.withdrew", "{0} 이(가) 원고를 거둬 갔다", "윤소하");
            Assert.That(s, Does.Contain("asked for the manuscript back"));
            Assert.That(s, Does.Contain("윤소하"));
        }

        [Test]
        public void MissingEnglishFallsBackToKorean()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("아직.번역.안.한.것", "한국어 원문"), Is.EqualTo("한국어 원문"));
        }
    }
}
