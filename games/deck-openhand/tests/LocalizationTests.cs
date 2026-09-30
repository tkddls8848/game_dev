using NUnit.Framework;

namespace DeckOpenhand.Tests
{
    /// <summary>
    /// 설계 원칙 6 — 한국어가 원문, 영어는 덮어쓰기.
    ///
    /// 여기서 재는 것은 번역 품질이 아니라 <b>구조</b>다.
    /// (1) 표가 없거나 깨져도 화면이 비지 않는가 (한국어 원문이 그대로 나오는가)
    /// (2) 언어를 바꾸면 <b>실제로 바뀌는가</b> — 정적 필드에 굳으면 첫 값이 남는다.
    ///     이게 이 저장소가 한 번 겪은 함정이고, 그래서 테스트로 못 박는다.
    /// </summary>
    [TestFixture]
    public class LocalizationTests
    {
        [TearDown]
        public void 되돌린다() => Localization.Reset();

        [Test]
        public void 표가_없으면_한국어_원문이_나온다()
        {
            Localization.Reset();
            foreach (var c in Fix.Data.Cards)
            {
                Assert.That(c.Name, Is.EqualTo(c.nameKo), c.id);
                Assert.That(c.Text, Is.EqualTo(c.textKo), c.id);
            }
            foreach (var e in Fix.Data.Enemies)
                Assert.That(e.Name, Is.EqualTo(e.nameKo), e.id);
        }

        [Test]
        public void 영어를_덮어쓰면_바뀌고_되돌리면_돌아온다()
        {
            var card = Fix.Data.Cards[0];
            string ko = card.nameKo;

            Localization.Reset();
            Localization.SetOverride("en", "card." + card.id + ".name", "Overridden");

            Assert.That(card.Name, Is.EqualTo(ko), "표를 넣었을 뿐인데 한국어가 바뀌었다");

            Localization.Language = "en";
            Assert.That(card.Name, Is.EqualTo("Overridden"), "언어를 바꿨는데 값이 굳어 있다");

            Localization.Language = "ko";
            Assert.That(card.Name, Is.EqualTo(ko), "되돌렸는데 영어가 남아 있다 — 어딘가 static 에 담겼다");
        }

        [Test]
        public void 덮어쓰기가_없는_항목은_영어에서도_한국어로_보인다()
        {
            // 부분 번역 상태에서 화면이 비지 않아야 한다. 빈 칸보다 한국어가 낫다.
            Localization.Reset();
            Localization.Language = "en";
            foreach (var c in Fix.Data.Cards)
                Assert.That(c.Name, Is.EqualTo(c.nameKo), c.id + " 이 영어 모드에서 비었다");
        }

        [Test]
        public void 자리표시자를_쓴다_조각을_이어_붙이지_않는다()
        {
            // 영어는 어순이 다르다. "{0} 피해" + 숫자 식으로 이으면 번역이 불가능해진다.
            Localization.Reset();
            Assert.That(Localization.Text("test.deal", "{0} 피해를 준다", 7), Is.EqualTo("7 피해를 준다"));
            Localization.SetOverride("en", "test.deal", "Deal {0} damage");
            Localization.Language = "en";
            Assert.That(Localization.Text("test.deal", "{0} 피해를 준다", 7), Is.EqualTo("Deal 7 damage"));
        }
    }
}
