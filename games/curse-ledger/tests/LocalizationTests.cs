using NUnit.Framework;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    /// 표가 없거나 깨져도 화면이 비지 않아야 하고, 언어를 바꾸면 **그 자리에서** 따라와야 한다.
    /// </summary>
    [TestFixture]
    public sealed class LocalizationTests
    {
        [TearDown]
        public void 되돌린다() { Localization.Language = "ko"; }

        [Test]
        public void 표에_없는_키는_한국어_원문을_그대로_낸다()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("없는.키", "누렁물 구덩이"), Is.EqualTo("누렁물 구덩이"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("end.passedOn", "대물림"), Is.EqualTo("대물림"));
        }

        [Test]
        public void 언어를_바꾸면_결말_이름이_그_자리에서_따라온다()
        {
            // 정적 필드에 굳어 있으면 여기서 걸린다.
            Assert.That(Endings.Korean(Ending.Uprising), Is.EqualTo("마을 봉기"));
            Localization.Language = "en";
            Assert.That(Endings.Korean(Ending.Uprising), Is.EqualTo("The village broke the altar"));
            Localization.Language = "ko";
            Assert.That(Endings.Korean(Ending.Uprising), Is.EqualTo("마을 봉기"));
        }

        [Test]
        public void 모든_결말에_이름이_있다()
        {
            foreach (Ending e in System.Enum.GetValues(typeof(Ending)))
            {
                string ko = Endings.Korean(e);
                Assert.That(ko, Is.Not.Null.And.Not.Empty, e.ToString());
                Localization.Language = "en";
                string en = Endings.Korean(e);
                Localization.Language = "ko";
                Assert.That(en, Is.Not.Null.And.Not.Empty, e.ToString() + " 의 영어가 없다");
                TestContext.WriteLine(e + " → " + ko + " / " + en);
            }
        }

        [Test]
        public void 조각을_이어_붙이지_않고_자리표시자를_쓴다()
        {
            Localization.Language = "en";
            string s = Localization.Text("bill.arrives", "{0} 가 남긴 값이 {1}대에 온다", "윤치억", 3);
            Assert.That(s, Is.EqualTo("A bill from 윤치억 arrives in generation 3"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("bill.arrives", "{0} 가 남긴 값이 {1}대에 온다", "윤치억", 3),
                Is.EqualTo("윤치억 가 남긴 값이 3대에 온다"));
        }
    }
}
