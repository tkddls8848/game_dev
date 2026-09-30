using System.Collections.Generic;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// ★ 곁들인 핵 검사기 — `VictimsHaveNames`.
    ///
    /// **희생자가 숫자가 아니라 이름과 사정을 가진 목록인가.**
    /// 숫자면 도덕이 자원이 되지 못하고 그냥 비용이 된다 — "3명"은 장부의 한 칸이고
    /// "노단이(14), 오탁수의 손녀, 글을 배우고 싶다고 종가에 세 번 왔다"는 한 칸이 아니다.
    ///
    /// 이 검사기는 **구조를 강제한다.** 시뮬레이션이 희생자를 정수로 셀 수 있는 길을
    /// 남겨 두면 다음 사람이 그 길로 간다. 그래서 회차의 결과에 `VictimCount` 만 있고
    /// 이름이 없는 상태를 만들 수 없게 만들어 두었고, 그것을 여기서 확인한다.
    /// </summary>
    [TestFixture]
    public sealed class VictimsHaveNamesTests
    {
        [Test]
        public void 명단의_모든_사람이_이름_나이_집안_사정을_가진다()
        {
            GameData d = TestWorld.Data;
            Assert.That(d.AllVillagers.Count, Is.GreaterThanOrEqualTo(TestWorld.Generations + 2),
                "바칠 수 있는 사람이 대의 수보다 적으면 명단이 곧 한계가 된다 — 그건 다른 게임이다");

            HashSet<string> names = new HashSet<string>();
            foreach (VillagerDef v in d.AllVillagers)
            {
                Assert.That(v.name, Is.Not.Null.And.Not.Empty, v.id + " 에 이름이 없다");
                Assert.That(v.note, Is.Not.Null.And.Not.Empty, v.name + " 에 사정이 없다 — 숫자와 같아진다");
                Assert.That(v.note.Length, Is.GreaterThanOrEqualTo(12),
                    v.name + " 의 사정이 너무 짧다 (" + v.note.Length + "자): " + v.note);
                Assert.That(v.household, Is.Not.Null.And.Not.Empty, v.name + " 에 집안이 없다");
                Assert.That(v.age, Is.InRange(1, 120), v.name + " 의 나이가 " + v.age + " 다");
                Assert.That(v.tier, Is.AnyOf(Tiers.Elder, Tiers.Young), v.name + " 의 계층이 이상하다: " + v.tier);
                Assert.That(names.Add(v.name), "이름이 겹친다: " + v.name + " — 붉은 줄이 누구에게 그어졌는지 알 수 없다");
            }
            TestContext.WriteLine("명단 " + d.AllVillagers.Count + "명 (늙은 이 "
                + d.TierRoster(Tiers.Elder).Count + " · 젊은 이 " + d.TierRoster(Tiers.Young).Count
                + ") 전부 이름·나이·집안·사정을 가진다");
        }

        [Test]
        public void 바쳐진_사람은_회차의_결과에서도_이름과_사정을_들고_있다()
        {
            GameData d = TestWorld.Data;
            LedgerRun keep = ExtremePolicies.Keep(d, TestWorld.MainSeed, TestWorld.Generations);
            Assert.That(keep.Victims, Is.Not.Empty, "「계속 유지」에서 아무도 바치지 않았다");

            HashSet<string> seen = new HashSet<string>();
            foreach (VictimRecord v in keep.Victims)
            {
                Assert.That(v.Name, Is.Not.Null.And.Not.Empty);
                Assert.That(v.Note, Is.Not.Null.And.Not.Empty, v.Name + " 의 사정이 결과에서 사라졌다");
                Assert.That(v.Household, Is.Not.Null.And.Not.Empty);
                Assert.That(v.Age, Is.GreaterThan(0));
                Assert.That(v.Generation, Is.InRange(1, TestWorld.Generations), v.Name + " 가 어느 대에 바쳐졌는지 없다");
                Assert.That(v.HeirId, Is.Not.Null.And.Not.Empty, v.Name + " 를 누가 골랐는지 없다");
                Assert.That(v.RiteId, Is.Not.Null.And.Not.Empty);
                Assert.That(v.LedgerLine, Is.GreaterThan(0), v.Name + " 에 족보의 붉은 줄 자리가 없다");
                Assert.That(seen.Add(v.VillagerId), "같은 사람이 두 번 바쳐졌다: " + v.Name);

                // 명단의 원본과 어긋나면 결과가 거짓을 들고 다니는 것이다.
                VillagerDef src = d.Villager(v.VillagerId);
                Assert.That(v.Name, Is.EqualTo(src.name));
                Assert.That(v.Note, Is.EqualTo(src.note));
                Assert.That(v.Age, Is.EqualTo(src.age));
            }

            for (int i = 0; i < keep.Victims.Count; i++)
                Assert.That(keep.Victims[i].LedgerLine, Is.EqualTo(i + 1), "붉은 줄 번호가 이어지지 않는다");

            TestContext.WriteLine("「계속 유지」가 지운 이름 " + keep.Victims.Count + "개:");
            foreach (VictimRecord v in keep.Victims)
                TestContext.WriteLine("   " + v.Generation + "대 · 붉은 줄 " + v.LedgerLine + " · "
                    + v.Name + "(" + v.Age + ") " + v.Household + " — " + v.Note);
        }

        [Test]
        public void 누구를_고르는가는_씨드가_정하고_정책이_바꾸지_못한다()
        {
            // 이름이 진짜라면 **정책이 사람을 고르는 순서를 흔들지 못해야** 한다 —
            // 그래야 "이 대에 노단이가 남아 있다"가 세계의 사실이 되고 플레이어의 계산이 아니다.
            GameData d = TestWorld.Data;
            CurseSim a = TestWorld.Sim(TestWorld.MainSeed);
            CurseSim b = TestWorld.Sim(TestWorld.MainSeed);
            Assert.That(NamesOf(a.YoungOrder), Is.EqualTo(NamesOf(b.YoungOrder)));
            Assert.That(NamesOf(a.ElderOrder), Is.EqualTo(NamesOf(b.ElderOrder)));

            CurseSim other = TestWorld.Sim(TestWorld.Seeds[1]);
            Assert.That(NamesOf(other.YoungOrder), Is.Not.EqualTo(NamesOf(a.YoungOrder)),
                "씨드를 바꿔도 부르는 순서가 같다 — 씨드가 아무것도 정하지 않는다");

            TestContext.WriteLine("씨드 " + TestWorld.MainSeed + " 의 젊은 이 부름 순서: "
                + string.Join(" > ", NamesOf(a.YoungOrder)));
            TestContext.WriteLine("씨드 " + TestWorld.Seeds[1] + " 의 젊은 이 부름 순서: "
                + string.Join(" > ", NamesOf(other.YoungOrder)));
        }

        [Test]
        public void 이름_하나를_지우면_마을이_반드시_반응한다()
        {
            // "도덕이 자원이다"의 반대말은 "도덕이 공짜다"다.
            // 살아남는 정책에서 「집안이 대신 치른다」를 「젊은 이를 바친다」로 딱 한 칸 바꿔 보면
            // 희생자가 하나 늘고 원한이 반드시 오른다. 이 인과가 끊기면 이름이 숫자가 된다.
            GameData d = TestWorld.Data;
            SweepResult full = TestWorld.Full(TestWorld.MainSeed);
            CurseSim sim = TestWorld.Sim(TestWorld.MainSeed);

            int compared = 0;
            foreach (LedgerRun baseRun in full.Survivors)
            {
                int at = System.Array.IndexOf(baseRun.Policy, "house_pays");
                if (at < 0) continue;
                string[] swapped = (string[])baseRun.Policy.Clone();
                swapped[at] = "offer_young";
                LedgerRun a = sim.Run(baseRun.Policy);
                LedgerRun b = sim.Run(swapped);
                if (a == null || b == null) continue;
                // 바꾼 쪽이 중간에 끝나면 뒤의 결정이 아예 일어나지 않는다 — 견줄 수 없는 쌍이다.
                if (a.GenerationsSurvived != TestWorld.Generations) continue;
                if (b.GenerationsSurvived != TestWorld.Generations) continue;

                Assert.That(b.VictimCount, Is.EqualTo(a.VictimCount + 1),
                    "한 칸을 바꿨는데 희생자 수가 " + a.VictimCount + " → " + b.VictimCount + " 다");
                Assert.That(b.Resentment, Is.GreaterThan(a.Resentment),
                    "이름 하나를 더 지웠는데 원한이 " + a.Resentment + " → " + b.Resentment
                    + " 다 — 이름이 공짜가 된다: " + string.Join(" > ", swapped));
                compared++;
                if (compared >= 40) break;
            }
            Assert.That(compared, Is.GreaterThanOrEqualTo(10), "견줄 쌍이 " + compared + "개뿐이다");
            TestContext.WriteLine("살아남는 정책 " + compared + "개에서 한 칸을 바꿔 보았다 — "
                + "전부 희생자 +1, 원한 상승");
        }

        [Test]
        public void 마을을_살리는_가장_싼_값이_이름_몇_개인지_적는다()
        {
            // 이 수가 이 PoC의 값이다. 0이면 딜레마가 없고, 명단 전체면 게임이 아니다.
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult full = TestWorld.Full(seed);
                int cheapest = int.MaxValue, dearest = 0;
                foreach (LedgerRun r in full.Survivors)
                {
                    if (r.VictimCount < cheapest) cheapest = r.VictimCount;
                    if (r.VictimCount > dearest) dearest = r.VictimCount;
                }
                Assert.That(cheapest, Is.InRange(1, TestWorld.Data.AllVillagers.Count - 1));
                LedgerRun best = null;
                foreach (LedgerRun r in full.Survivors)
                    if (r.VictimCount == cheapest && (best == null || r.Score > best.Score)) best = r;

                TestContext.WriteLine("씨드 " + seed + ": 가장 싼 값 이름 " + cheapest
                    + "개 · 가장 비싼 살아남기 이름 " + dearest + "개");
                foreach (VictimRecord v in best.Victims)
                    TestContext.WriteLine("   그 한 명: " + v.Generation + "대 · " + v.Name
                        + "(" + v.Age + ") " + v.Household + " — " + v.Note);
            }
        }

        private static List<string> NamesOf(IList<VillagerDef> list)
        {
            List<string> names = new List<string>();
            foreach (VillagerDef v in list) names.Add(v.name);
            return names;
        }
    }
}
