using System.Collections.Generic;
using NUnit.Framework;
using Tenants.Data;

namespace Tenants.Tests
{
    /// <summary>
    /// **DataValidator — 참조 무결성.** 이 검사기가 깨지면 나머지 검사기의 수치는 아무 뜻이 없다.
    /// 여기서 보는 것은 전부 기계가 판정할 수 있는 것(고장)뿐이다.
    /// </summary>
    [TestFixture]
    public class DataIntegrityTests
    {
        [Test]
        public void 모든_참조가_실재한다()
        {
            GameData d = TestWorld.Data;
            HashSet<string> unitsSeen = new HashSet<string>();

            foreach (BuildingDef b in d.AllBuildings)
            {
                Assert.That(b.units.Length, Is.GreaterThanOrEqualTo(4), b.id + " 는 칸이 너무 적다");
                Assert.That(b.graceDaysTotal, Is.GreaterThan(0), b.id + " 의 유예가 0 이면 아무도 도울 수 없다");
                foreach (UnitDef u in b.units)
                {
                    Assert.That(unitsSeen.Add(u.id), Is.True, "칸 id 가 겹친다: " + u.id);
                    Assert.That(d.HasHousehold(u.householdId), Is.True,
                        u.id + " 가 없는 세대를 가리킨다: " + u.householdId);
                    Assert.That(d.Household(u.householdId).unitId, Is.EqualTo(u.id),
                        u.householdId + " 와 " + u.id + " 가 서로를 가리키지 않는다");
                    Assert.That(d.Household(u.householdId).buildingId, Is.EqualTo(b.id));
                }
            }

            foreach (HouseholdDef h in d.AllHouseholds)
            {
                Assert.That(d.HasUnit(h.unitId), Is.True, h.id + " 가 없는 칸을 가리킨다");
                Assert.That(d.HasNeed(h.needId), Is.True, h.id + " 가 없는 사정을 가리킨다: " + h.needId);
                Assert.That(h.name, Is.Not.Null.And.Not.Empty, h.id + " 에 이름이 없다");
                Assert.That(h.note, Is.Not.Null.And.Not.Empty, h.id + " 에 사정이 적혀 있지 않다");
                Assert.That(h.baseStakes, Is.GreaterThanOrEqualTo(0));
                Assert.That(h.spilloverWeightPercent, Is.GreaterThan(0),
                    h.id + " 의 가중치가 0 이면 남의 유예를 짊어지지 않는다");
            }

            foreach (CueDef c in d.AllCues)
            {
                Assert.That(d.HasHousehold(c.atHouseholdId), Is.True, c.id + " 가 없는 문을 가리킨다");
                Assert.That(d.HasHousehold(c.aboutHouseholdId), Is.True, c.id + " 가 없는 집 얘기를 한다");
                Assert.That(d.HasNeed(c.needId), Is.True, c.id + " 가 없는 사정을 가리킨다: " + c.needId);
                Assert.That(c.text, Is.Not.Null.And.Not.Empty, c.id + " 에 들리는 내용이 없다");
                Assert.That(c.slotOptions, Is.Not.Null.And.Not.Empty, c.id + " 에 시간대가 없다");
                foreach (int s in c.slotOptions)
                    Assert.That(s, Is.InRange(0, d.Balance.day.slotCount - 1),
                        c.id + " 의 시간대가 하루를 벗어난다");
                Assert.That(d.Household(c.atHouseholdId).buildingId,
                            Is.EqualTo(d.Household(c.aboutHouseholdId).buildingId),
                            c.id + " 가 다른 건물 얘기를 한다");
            }
        }

        [Test]
        public void 사정의_값이_모두_1_이상이다()
        {
            GameData d = TestWorld.Data;
            foreach (NeedDef n in d.AllNeeds)
            {
                Assert.That(n.slotsRequired, Is.GreaterThan(0), n.id + " 가 시간대를 먹지 않는다");
                Assert.That(n.graceCost, Is.GreaterThan(0),
                    n.id + " 의 유예가 0 이다 — 대가 없이 도울 수 있는 사정이 생기면 딜레마가 사라진다");
                Assert.That(n.perceivedWeight, Is.GreaterThanOrEqualTo(0));
                Assert.That(n.name, Is.Not.Null.And.Not.Empty);
            }
            foreach (BuildingDef b in d.AllBuildings)
                foreach (NeedDef n in d.AllNeeds)
                    Assert.That(n.graceCost, Is.LessThanOrEqualTo(b.graceDaysTotal),
                        b.id + " 에서 " + n.id + " 는 유예가 모자라 아예 고를 수 없다");
        }

        [Test]
        public void 이웃의_말은_인접한_칸에서만_들린다()
        {
            GameData d = TestWorld.Data;
            int mentions = 0;
            foreach (CueDef c in d.AllCues)
            {
                if (c.atHouseholdId == c.aboutHouseholdId) continue;
                mentions++;
                Assert.That(d.AreAdjacent(c.atHouseholdId, c.aboutHouseholdId), Is.True,
                    c.id + ": " + c.atHouseholdId + " 와 " + c.aboutHouseholdId
                    + " 는 인접하지 않다. 벽 둘을 넘어 들리는 말은 없다");
            }
            Assert.That(mentions, Is.GreaterThanOrEqualTo(6),
                "이웃의 말이 너무 적다 — 침묵한 칸의 사정이 올 길이 막힌다");
        }
    }
}
