using System.Collections.Generic;
using NUnit.Framework;
using Tenants.Data;

namespace Tenants.Tests
{
    /// <summary>DataValidator 의 뒤쪽 절반 — 침묵과 거짓의 무결성.</summary>
    [TestFixture]
    public class DataIntegritySilenceTests
    {
        [Test]
        public void 침묵한_문에서는_아무_소리도_나지_않는다()
        {
            GameData d = TestWorld.Data;
            foreach (CueDef c in d.AllCues)
                Assert.That(d.IsSilent(c.atHouseholdId), Is.False,
                    c.id + " 가 침묵한 칸(" + c.atHouseholdId + ") 의 문에서 난다. 침묵은 소리가 없다는 뜻이다");

            foreach (HouseholdDef h in d.AllHouseholds)
            {
                if (h.voice == Voices.Silent)
                {
                    Assert.That(h.silenceKind, Is.Not.EqualTo(SilenceKinds.None),
                        h.id + " 는 침묵인데 침묵의 종류가 없다");
                    Assert.That(h.ambientLoudnessPercent, Is.EqualTo(0),
                        h.id + " 는 침묵인데 복도에서 소리가 난다");
                }
                else
                {
                    Assert.That(h.voice, Is.EqualTo(Voices.Audible), h.id + " 의 voice 가 이상하다: " + h.voice);
                    Assert.That(h.silenceKind, Is.EqualTo(SilenceKinds.None));
                    Assert.That(h.ambientLoudnessPercent, Is.GreaterThan(0),
                        h.id + " 는 소리가 난다는데 복도에서 0 이다");
                }
                if (h.silenceKind == SilenceKinds.Gone)
                    Assert.That(h.needId, Is.EqualTo("need_none"),
                        h.id + " 는 이미 나갔는데 도울 것이 있다고 적혀 있다");
            }
        }

        [Test]
        public void 건물마다_침묵한_칸이_있고_한_건물에는_두_종류가_같이_있다()
        {
            GameData d = TestWorld.Data;
            int bothKinds = 0;
            foreach (BuildingDef b in d.AllBuildings)
            {
                List<HouseholdDef> silent = d.SilentOf(b.id);
                Assert.That(silent.Count, Is.GreaterThanOrEqualTo(1),
                    b.id + " 에 침묵한 칸이 없다 — 이 PoC의 핵이 그 건물에서는 없는 것이 된다");
                if (d.SilentOfKind(b.id, SilenceKinds.Gone) != null
                    && d.SilentOfKind(b.id, SilenceKinds.Unable) != null) bothKinds++;
            }
            Assert.That(bothKinds, Is.GreaterThanOrEqualTo(1),
                "한 건물 안에 gone 과 unable 이 같이 있어야 「문 앞에서는 구별되지 않는다」가 실제 문제가 된다");
        }

        [Test]
        public void 침묵한_칸마다_이웃의_말이_있다()
        {
            GameData d = TestWorld.Data;
            foreach (HouseholdDef h in d.AllHouseholds)
            {
                if (h.voice != Voices.Silent) continue;
                int mentions = 0;
                foreach (CueDef c in d.AllCues)
                    if (c.aboutHouseholdId == h.id && c.atHouseholdId != h.id) mentions++;
                Assert.That(mentions, Is.GreaterThanOrEqualTo(1),
                    h.id + " 는 침묵인데 그 얘기를 하는 이웃이 없다 — 알 길이 아예 없는 칸이 된다");
            }
        }

        [Test]
        public void 거짓이_될_수_있는_소리는_엉뚱한_사정을_가리킨다()
        {
            GameData d = TestWorld.Data;
            int falsifiable = 0;
            foreach (CueDef c in d.AllCues)
            {
                if (!c.falsifiable)
                {
                    Assert.That(c.misleadNeedId, Is.Empty,
                        c.id + " 는 거짓이 될 수 없는데 엉뚱한 사정을 들고 있다");
                    continue;
                }
                falsifiable++;
                Assert.That(c.misleadNeedId, Is.Not.Empty,
                    c.id + " 는 거짓이 될 수 있는데 무엇으로 속이는지가 없다");
                Assert.That(d.HasNeed(c.misleadNeedId), Is.True, c.id + " 가 없는 사정으로 속인다");
                Assert.That(c.misleadNeedId, Is.Not.EqualTo(c.needId),
                    c.id + " 의 거짓이 참과 같다 — 속이는 것이 아니다");
            }
            foreach (BuildingDef b in d.AllBuildings)
            {
                int n = 0;
                foreach (CueDef c in d.CuesOf(b.id)) if (c.falsifiable) n++;
                Assert.That(n, Is.GreaterThan(d.Balance.night.lieCountPerBuilding),
                    b.id + " 에서 거짓 후보가 뽑을 개수보다 많지 않다 — 씨드가 바꿀 것이 없다");
            }
            Assert.That(falsifiable, Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void 모든_사정이_적어도_한_세대에_쓰인다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> used = new HashSet<string>();
            foreach (HouseholdDef h in d.AllHouseholds) used.Add(h.needId);
            foreach (NeedDef n in d.AllNeeds)
                Assert.That(used.Contains(n.id), Is.True, n.id + " 를 쓰는 세대가 없다 — 죽은 데이터다");
        }

        [Test]
        public void 경감_백분율표가_단계마다_오른다()
        {
            HelpBalance h = TestWorld.Data.Balance.help;
            Assert.That(h.reliefPercentByLevel.Length, Is.EqualTo(4));
            Assert.That(h.reliefPercentByLevel[0], Is.EqualTo(0), "아무것도 모르면 건지는 것이 없어야 한다");
            for (int i = 1; i < 4; i++)
                Assert.That(h.reliefPercentByLevel[i], Is.GreaterThan(h.reliefPercentByLevel[i - 1]),
                    "더 아는 것이 손해가 되면 엿듣기가 벌이 된다");
            Assert.That(h.reliefPercentByLevel[3], Is.EqualTo(100));
            Assert.That(h.wrongNeedReliefPercent, Is.LessThan(h.reliefPercentByLevel[1]),
                "틀린 것을 확신하는 것이 아무것도 모르는 것보다 나아서는 안 된다");
            Assert.That(TestWorld.Data.Balance.spillover.perGraceFlat, Is.GreaterThan(0),
                "유예가 나머지에게 아무것도 물리지 않으면 이 PoC의 규칙이 사라진다");
        }
    }
}
