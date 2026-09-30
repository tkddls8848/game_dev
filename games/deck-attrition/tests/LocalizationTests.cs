using NUnit.Framework;

namespace DeckAttrition.Tests
{
    /// <summary>
    /// 설계 원칙 6. 한국어가 원문, 영어는 덮어쓰기.
    /// 표가 없거나 깨져도 화면이 비지 않아야 하고, 언어를 바꿔도 첫 값이 굳으면 안 된다.
    /// </summary>
    [TestFixture]
    public class LocalizationTests
    {
        [TearDown]
        public void Cleanup() => Localization.Reset();

        [Test]
        public void 표가_없어도_한국어_원문이_나온다()
        {
            Localization.Reset();
            foreach (var c in Fix.Data.Cards)
            {
                Assert.That(c.Name, Is.EqualTo(c.nameKo));
                Assert.That(c.Text, Is.EqualTo(c.textKo));
            }
            foreach (var e in Fix.Data.Enemies)
                Assert.That(e.Name, Is.EqualTo(e.nameKo));
        }

        [Test]
        public void 영어_덮어쓰기가_먹는다()
        {
            var c = Fix.Data.Cards[0];
            Localization.Reset();
            Localization.SetOverride("en", "card." + c.id + ".name", "Common Sort");
            Localization.Language = "en";
            Assert.That(c.Name, Is.EqualTo("Common Sort"));
        }

        [Test]
        public void 덮어쓰기가_없으면_영어에서도_한국어가_나온다()
        {
            var c = Fix.Data.Cards[0];
            Localization.Reset();
            Localization.Language = "en";
            Assert.That(c.Name, Is.EqualTo(c.nameKo), "표에 없는 키가 빈 문자열이 됐다");
        }

        [Test]
        public void 언어를_바꿔도_첫_값이_굳지_않는다()
        {
            // 정적 필드에 담으면 여기서 걸린다. 속성으로 둔 이유가 이것이다.
            var c = Fix.Data.Cards[0];
            Localization.Reset();
            string ko = c.Name;
            Localization.SetOverride("en", "card." + c.id + ".name", "Common Sort");
            Localization.Language = "en";
            Assert.That(c.Name, Is.EqualTo("Common Sort"));
            Localization.Language = "ko";
            Assert.That(c.Name, Is.EqualTo(ko), "한국어로 돌아오지 않는다 — 어딘가에 값이 굳었다");
        }

        [Test]
        public void 자리표시자로_이어_붙인다()
        {
            // 조각을 문자열 덧셈으로 잇지 않는다. 영어는 어순이 다르다.
            Localization.Reset();
            Assert.That(Localization.Text("case.left", "{0} 는 {1}번 더 찍을 수 있다", "대형 목활자", 2),
                Is.EqualTo("대형 목활자 는 2번 더 찍을 수 있다"));
            Localization.SetOverride("en", "case.left", "{0} can print {1} more time(s)");
            Localization.Language = "en";
            Assert.That(Localization.Text("case.left", "{0} 는 {1}번 더 찍을 수 있다", "Wood Type", 2),
                Is.EqualTo("Wood Type can print 2 more time(s)"));
        }
    }
}
