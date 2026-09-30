using NUnit.Framework;

namespace Tenants.Tests
{
    /// <summary>뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어는 덮어쓰기다.</summary>
    [TestFixture]
    public class LocalizationTests
    {
        [TearDown]
        public void Restore() { Localization.Language = "ko"; }

        [Test]
        public void 표가_없는_열쇠는_한국어_원문을_그대로_준다()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("없는.열쇠", "이 문에서는 아무 소리도 나지 않는다"),
                        Is.EqualTo("이 문에서는 아무 소리도 나지 않는다"));
        }

        [Test]
        public void 영어를_켜면_표가_원문을_덮는다()
        {
            Assert.That(Localization.Text("notice.title", "철거 고지"), Is.EqualTo("철거 고지"));
            Localization.Language = "en";
            Assert.That(Localization.Text("notice.title", "철거 고지"), Is.EqualTo("NOTICE OF DEMOLITION"));
        }

        [Test]
        public void 자리표시자를_쓰고_조각을_이어_붙이지_않는다()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("know.need", "{0} 에 필요한 것: {1}", "302호", "약과 통원"),
                        Is.EqualTo("302호 에 필요한 것: 약과 통원"));
            Localization.Language = "en";
            Assert.That(Localization.Text("know.need", "{0} 에 필요한 것: {1}", "Unit 302", "medicine"),
                        Is.EqualTo("Unit 302 needs: medicine"));
        }

        [Test]
        public void 언어를_되돌리면_값도_되돌아온다()
        {
            // 정적 필드에 굳으면 이 테스트가 깨진다.
            Localization.Language = "en";
            Assert.That(Localization.Text("help.none", "아무도 돕지 않았다"), Is.EqualTo("No one was helped"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("help.none", "아무도 돕지 않았다"), Is.EqualTo("아무도 돕지 않았다"));
        }
    }
}
