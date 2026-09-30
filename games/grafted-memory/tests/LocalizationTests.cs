using Graft.Sim;
using NUnit.Framework;

namespace Graft.Tests
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문, 영어가 덮어쓰기.
    /// 정적 필드에 담으면 언어를 바꿔도 첫 값이 굳는다 — 그게 안 굳는지를 본다.
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
            Assert.That(Localization.Text("없는.키", "한국어 원문"), Is.EqualTo("한국어 원문"));
            Localization.Language = "fr";
            Assert.That(Localization.Text("phase.probe", "캐묻기"), Is.EqualTo("캐묻기"));
        }

        [Test]
        public void 언어를_바꾸면_값이_따라온다()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("phase.graft", "심기"), Is.EqualTo("심기"));
            Localization.Language = "en";
            Assert.That(Localization.Text("phase.graft", "심기"), Is.EqualTo("Graft"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("phase.graft", "심기"), Is.EqualTo("심기"),
                        "한 번 영어를 본 뒤 값이 굳었다 — 정적 필드에 담은 것이다");
        }

        [Test]
        public void 자리표시자를_쓴다()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("reject.personGone", "{0} 는 그 날 거기 있을 수 없다", "선화"),
                        Is.EqualTo("선화 was not there on that day"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("reject.personGone", "{0} 는 그 날 거기 있을 수 없다", "선화"),
                        Is.EqualTo("선화 는 그 날 거기 있을 수 없다"));
        }

        [Test]
        public void 거부_이유가_전부_번역된다()
        {
            Localization.Language = "en";
            string[] keys =
            {
                "reject.timePlace", "reject.personGone", "reject.personElse", "reject.placeWindow",
                "reject.moodAnchor", "reject.era", "reject.intensity"
            };
            foreach (string k in keys)
                Assert.That(Localization.Text(k, "@@원문@@"), Is.Not.EqualTo("@@원문@@"),
                            "거부 이유가 영어로 덮어쓰이지 않았다: " + k);
        }
    }
}
