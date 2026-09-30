using NUnit.Framework;

namespace Secretary.Tests
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
            Assert.That(Localization.Text("없는.열쇠", "신은 이 장부에 나타나지 않는다"),
                        Is.EqualTo("신은 이 장부에 나타나지 않는다"));
        }

        [Test]
        public void 영어를_켜면_표가_원문을_덮는다()
        {
            Assert.That(Localization.Text("seal.grant", "허가"), Is.EqualTo("허가"));
            Localization.Language = "en";
            Assert.That(Localization.Text("seal.grant", "허가"), Is.EqualTo("GRANTED"));
        }

        [Test]
        public void 자리표시자를_쓰고_조각을_이어_붙이지_않는다()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("ledger.taken", "{2} 에게서 {0} {1} 을 뺐다", 3, "동이", "우물지기 오렌"),
                        Is.EqualTo("우물지기 오렌 에게서 3 동이 을 뺐다"));
            Localization.Language = "en";
            Assert.That(Localization.Text("ledger.taken", "{2} 에게서 {0} {1} 을 뺐다", 3, "jars", "Oren the well-keeper"),
                        Is.EqualTo("3 of jars taken from Oren the well-keeper"));
        }

        [Test]
        public void 언어를_되돌리면_값도_되돌아온다()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("seal.deny", "기각"), Is.EqualTo("REFUSED"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("seal.deny", "기각"), Is.EqualTo("기각"));
        }
    }
}
