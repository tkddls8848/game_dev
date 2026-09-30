using System.Collections.Generic;
using NUnit.Framework;
using Secretary.Data;
using Secretary.Sim;

namespace Secretary.Tests
{
    /// <summary>
    /// **DataValidator — 참조 무결성.** 이 검사기가 깨지면 나머지 검사기의 수치는 아무 뜻이 없다.
    /// </summary>
    [TestFixture]
    public class DataIntegrityTests
    {
        [Test]
        public void 모든_참조가_실재한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> soulIds = new HashSet<string>();
            foreach (SoulDef s in d.AllSouls)
            {
                Assert.That(soulIds.Add(s.id), Is.True, "이름 id 가 겹친다: " + s.id);
                Assert.That(s.name, Is.Not.Null.And.Not.Empty, s.id + " 에 이름이 없다");
                Assert.That(s.place, Is.Not.Null.And.Not.Empty, s.id + " 에 사는 곳이 없다");
                Assert.That(s.note, Is.Not.Null.And.Not.Empty, s.id + " 에 사정이 적혀 있지 않다");
            }
            foreach (DomainDef x in d.AllDomains)
            {
                Assert.That(x.hardshipPerUnit, Is.GreaterThan(0),
                    x.id + " 의 단위당 고통이 0 이다 — 그 영역에서는 빼도 아프지 않다");
                Assert.That(x.severeExtraPerUnit, Is.GreaterThan(0),
                    x.id + " 에 한도를 넘는 값이 없다 — 고통이 볼록하지 않으면 옮겨도 총합이 안 바뀐다");
                Assert.That(x.unitName, Is.Not.Null.And.Not.Empty);
            }
            foreach (VolumeDef v in d.AllVolumes)
            {
                Assert.That(v.days, Is.GreaterThan(1), v.id + " 의 날이 너무 적다");
                foreach (HoldingDef h in v.holdings)
                {
                    Assert.That(d.HasSoul(h.soulId), Is.True, v.id + " 가 없는 이름을 가리킨다: " + h.soulId);
                    Assert.That(d.HasDomain(h.domain), Is.True, v.id + " 가 없는 영역을 가리킨다: " + h.domain);
                    Assert.That(h.have, Is.GreaterThanOrEqualTo(0));
                    Assert.That(h.toleranceUnits, Is.GreaterThanOrEqualTo(0));
                }
            }
            foreach (PrayerDef p in d.AllPrayers)
            {
                Assert.That(d.HasSoul(p.fromSoulId), Is.True, p.id + " 가 없는 이름에게서 왔다");
                Assert.That(d.HasDomain(p.domain), Is.True, p.id + " 가 없는 영역을 청한다");
                Assert.That(p.askUnits, Is.GreaterThan(0), p.id + " 가 아무것도 청하지 않는다");
                Assert.That(p.text, Is.Not.Null.And.Not.Empty, p.id + " 에 편지 본문이 없다");
                Assert.That(p.dayOptions, Is.Not.Null.And.Not.Empty, p.id + " 에 도착 날이 없다");
                Assert.That(p.deferPenaltyUnits, Is.GreaterThan(0),
                    p.id + " 는 보류해도 커지지 않는다 — 미루는 것이 공짜면 보류가 지배 전략이 된다");
                VolumeDef v = d.Volume(p.volumeId);
                foreach (int day in p.dayOptions)
                    Assert.That(day, Is.InRange(0, v.days - 1), p.id + " 의 도착 날이 장부 밖이다");
            }
        }

        [Test]
        public void 빠지는_자리마다_이름이_있고_익명의_세계가_없다()
        {
            // 「세계」나 「모두」 같은 자리에서 빼면 대가가 추상이 되어 아무 무게가 없다.
            GameData d = TestWorld.Data;
            string[] forbidden = { "세계", "world", "모두", "everyone", "익명", "anonymous", "누군가" };
            foreach (SoulDef s in d.AllSouls)
                foreach (string bad in forbidden)
                {
                    Assert.That(s.id.ToLowerInvariant().Contains(bad.ToLowerInvariant()), Is.False,
                        "익명의 자리가 있다: " + s.id);
                    Assert.That(s.name.Contains(bad), Is.False, "익명의 자리가 있다: " + s.name);
                }

            foreach (PrayerDef p in d.AllPrayers)
            {
                Assert.That(p.sources, Is.Not.Null.And.Not.Empty,
                    p.id + " 에 빠질 자리가 없다 — 대가 없이 들어줄 수 있는 기도가 된다");
                HashSet<string> seen = new HashSet<string>();
                foreach (SourceDef s in p.sources)
                {
                    Assert.That(d.HasSoul(s.soulId), Is.True, p.id + " 가 없는 이름에서 뺀다: " + s.soulId);
                    Assert.That(seen.Add(s.soulId), Is.True, p.id + " 에 같은 이름이 두 번 적혀 있다");
                    Assert.That(s.soulId, Is.Not.EqualTo(p.fromSoulId),
                        p.id + " 가 자기에게서 뺀다 — 그것은 들어준 것이 아니다");
                    Assert.That(s.maxUnits, Is.GreaterThan(0), p.id + " 의 " + s.soulId + " 에서 뺄 수 있는 양이 0 이다");
                    Assert.That(s.note, Is.Not.Null.And.Not.Empty,
                        p.id + " 의 " + s.soulId + " 자리에 「어디서」가 적혀 있지 않다");
                }
            }
        }

        [Test]
        public void 가진_것이_필요한_것을_넘지_않는다()
        {
            // 이 불변식이 NoFreeGrant 의 바닥이다. 누구 하나라도 여유가 있으면 그 자리는 공짜 출처가 된다.
            GameData d = TestWorld.Data;
            foreach (VolumeDef v in d.AllVolumes)
                foreach (HoldingDef h in v.holdings)
                    Assert.That(h.have, Is.LessThanOrEqualTo(h.need),
                        v.id + " 의 " + h.soulId + "/" + h.domain + " 가 필요보다 많이 가졌다 ("
                        + h.have + " > " + h.need + ") — 그 자리에서는 대가 없이 뺄 수 있다");
        }

        [Test]
        public void 모든_편지가_받은_날_실제로_들어줄_수_있다()
        {
            GameData d = TestWorld.Data;
            foreach (VolumeDef v in d.AllVolumes)
            {
                Ledger l = new Ledger(d, v);
                foreach (PrayerDef p in d.PrayersOf(v.id))
                {
                    Assert.That(l.Holds(p.fromSoulId, p.domain), Is.True,
                        p.id + ": 청원자가 그 영역의 장부 줄을 갖고 있지 않다");
                    Assert.That(l.Deficit(p.fromSoulId, p.domain), Is.GreaterThan(0),
                        p.id + ": 청원자에게 부족이 없다 — 들어줄 것이 없는 편지다");
                    int reach = 0;
                    foreach (SourceDef s in p.sources)
                    {
                        Assert.That(l.Holds(s.soulId, p.domain), Is.True,
                            p.id + ": " + s.soulId + " 은 그 영역을 갖고 있지 않다");
                        int can = s.maxUnits < l.Have(s.soulId, p.domain) ? s.maxUnits : l.Have(s.soulId, p.domain);
                        reach += can;
                    }
                    Assert.That(reach, Is.GreaterThan(0), p.id + ": 어디서도 한 단위를 뺄 수 없다");
                }
            }
        }

        [Test]
        public void 장부마다_봉랍이_편지보다_모자라다()
        {
            // 다 볼 수 있으면 고르는 일이 없다.
            GameData d = TestWorld.Data;
            foreach (VolumeDef v in d.AllVolumes)
            {
                int stamps = v.days * d.Balance.day.stampsPerDay;
                int letters = d.PrayersOf(v.id).Count;
                Assert.That(stamps, Is.LessThan(letters),
                    v.id + ": 봉랍 " + stamps + "번으로 편지 " + letters + "장을 다 볼 수 있다 — 고를 것이 없다");
            }
        }
    }
}
