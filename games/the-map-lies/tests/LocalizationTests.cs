using NUnit.Framework;

namespace MapLies.Tests
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
            Assert.That(Localization.Text("kind.block", "건물"), Is.EqualTo("건물"));
        }

        [Test]
        public void 언어를_바꾸면_값이_따라온다()
        {
            Localization.Language = "ko";
            Assert.That(Localization.Text("kind.plaza", "광장"), Is.EqualTo("광장"));
            Localization.Language = "en";
            Assert.That(Localization.Text("kind.plaza", "광장"), Is.EqualTo("Plaza"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("kind.plaza", "광장"), Is.EqualTo("광장"),
                        "한 번 영어를 본 뒤 값이 굳었다 — 정적 필드에 담은 것이다");
        }

        [Test]
        public void 자리표시자를_쓴다()
        {
            Localization.Language = "en";
            Assert.That(Localization.Text("trap.sealed", "{0} 이 건물 사이에 갇혔다", "조씨"),
                        Is.EqualTo("조씨 is walled in between buildings"));
            Localization.Language = "ko";
            Assert.That(Localization.Text("trap.sealed", "{0} 이 건물 사이에 갇혔다", "조씨"),
                        Is.EqualTo("조씨 이 건물 사이에 갇혔다"));
        }

        [Test]
        public void 이_게임의_말이_전부_번역된다()
        {
            Localization.Language = "en";
            string[] keys =
            {
                "kind.block", "kind.street", "kind.plaza", "kind.unspec",
                "trap.sealed", "trap.entombed", "trap.freed",
                "city.refused", "city.settled", "city.churning", "cost.redraw", "harm.kept"
            };
            foreach (string k in keys)
                Assert.That(Localization.Text(k, "@@원문@@"), Is.Not.EqualTo("@@원문@@"),
                            "영어로 덮어쓰이지 않았다: " + k);
        }

        [Test]
        public void 칸_이름은_한국어_원문이_바로_보인다()
        {
            // 격자 글자를 사람이 읽는 말로 바꾸는 것은 Kinds.Name 이 한다. 코드를 읽는 사람이
            // 원문을 그 자리에서 봐야 하므로 여기는 표를 타지 않는다.
            Assert.That(Sim.Kinds.Name('B'), Is.EqualTo("건물"));
            Assert.That(Sim.Kinds.Name('S'), Is.EqualTo("길"));
            Assert.That(Sim.Kinds.Name('P'), Is.EqualTo("광장"));
            Assert.That(Sim.Kinds.Name('.'), Is.EqualTo("미지정"));
        }
    }
}
