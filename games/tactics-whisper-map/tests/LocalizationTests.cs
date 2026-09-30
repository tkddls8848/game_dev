using NUnit.Framework;

namespace Whisper.Tests
{
    /// <summary>
    /// 설계 원칙 6 — 한국어가 원문, 영어가 덮어쓰기. **정적 필드에 굳지 않는다.**
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
            Assert.That(Localization.Text("ask.closed", "마을이 등을 돌렸다"), Is.EqualTo("마을이 등을 돌렸다"));
            Localization.Language = "de";   // 없는 언어
            Assert.That(Localization.Text("ask.closed", "마을이 등을 돌렸다"), Is.EqualTo("마을이 등을 돌렸다"));
            Localization.Language = "en";
            Assert.That(Localization.Text("없는.키", "빠진 열"), Is.EqualTo("빠진 열"));
        }

        [Test]
        public void 언어를_바꾸면_값이_따라온다()
        {
            // 정적 필드(const · static readonly)에 담았다면 여기서 첫 값이 굳어 실패한다.
            Localization.Language = "ko";
            string ko = Localization.Text("blind.ok", "이 임무는 한 마디도 묻지 않고 나갈 수 있다");
            Localization.Language = "en";
            string en = Localization.Text("blind.ok", "이 임무는 한 마디도 묻지 않고 나갈 수 있다");
            Assert.That(en, Is.Not.EqualTo(ko));
            Localization.Language = "ko";
            Assert.That(Localization.Text("blind.ok", "이 임무는 한 마디도 묻지 않고 나갈 수 있다"), Is.EqualTo(ko));
        }

        [Test]
        public void 조각을_이어_붙이지_않고_자리표시자를_쓴다()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("ask.silent", "{0} 가 얼마간 입을 닫았다", "종지기 아이"),
                Is.EqualTo("종지기 아이 has gone quiet for a while"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("ask.silent", "{0} 가 얼마간 입을 닫았다", "짐수레꾼"),
                Is.EqualTo("짐수레꾼 가 얼마간 입을 닫았다"));
        }
    }
}
