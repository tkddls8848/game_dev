using System.Collections.Generic;
using MapLies.Data;
using MapLies.Sim;
using NUnit.Framework;

namespace MapLies.Tests
{
    /// <summary>
    /// 공통 검사기 `DataValidator` — 참조 무결성.
    ///
    /// 여기서 잡는 것은 전부 **고장**이다(POC_FACTORY §5). 재미와 섞지 않는다.
    /// 다른 검사기 전부가 이 파일이 통과한다는 것을 전제로 돈다 — 문이 건물 안에 있는 도시에서
    /// 「갇혔는가」를 재는 것은 아무 뜻이 없다.
    /// </summary>
    [TestFixture]
    public sealed class DataIntegrityTests
    {
        private static GameData D { get { return TestWorld.Data; } }

        [Test]
        public void 격자의_모양이_옳다()
        {
            Assert.That(D.City.width, Is.GreaterThan(3));
            Assert.That(D.City.height, Is.GreaterThan(3));
            Assert.That(D.City.actualRows.Length, Is.EqualTo(D.City.height), "actualRows 의 줄 수가 height 와 다르다");
            Assert.That(D.City.planRows.Length, Is.EqualTo(D.City.height), "planRows 의 줄 수가 height 와 다르다");

            for (int y = 0; y < D.City.height; y++)
            {
                Assert.That(D.City.actualRows[y].Length, Is.EqualTo(D.City.width), "actualRows[" + y + "] 의 길이가 다르다");
                Assert.That(D.City.planRows[y].Length, Is.EqualTo(D.City.width), "planRows[" + y + "] 의 길이가 다르다");
                for (int x = 0; x < D.City.width; x++)
                {
                    char a = D.City.actualRows[y][x];
                    char p = D.City.planRows[y][x];
                    Assert.That(Kinds.IsReal(a), Is.True,
                                "actual (" + x + "," + y + ") 에 없는 글자가 있다: " + a + " — 미지정(.)은 지도에만 쓴다");
                    Assert.That(Kinds.IsReal(p) || p == Kinds.Unspecified, Is.True,
                                "plan (" + x + "," + y + ") 에 없는 글자가 있다: " + p);
                }
            }
        }

        [Test]
        public void 지도가_아직_안_그린_칸이_있다()
        {
            CityGrid plan = new CityGrid(D.City.width, D.City.height, D.City.planRows);
            Assert.That(plan.Count(Kinds.Unspecified), Is.GreaterThan(0),
                        "미지정 칸이 하나도 없으면 도시가 스스로 짐작할 일이 없고 진동도 수렴도 시험할 것이 없다");
        }

        [Test]
        public void 문이_밖으로_이어져_있다()
        {
            CityGrid g = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            Assert.That(D.City.exits.Length, Is.GreaterThan(0), "문이 없다");
            foreach (CellRef e in D.City.exits)
            {
                Assert.That(g.Inside(e.x, e.y), Is.True, "문이 격자 밖에 있다: " + e.x + "," + e.y);
                Assert.That(Kinds.Walkable(g.At(e.x, e.y)), Is.True,
                            "문이 걸어갈 수 없는 칸이다: " + e.x + "," + e.y);
                Assert.That(g.OnBorder(e.x, e.y), Is.True,
                            "문이 테두리에 없다 — 도시 밖으로 나가는 문이 아니다: " + e.x + "," + e.y);
            }
        }

        [Test]
        public void 사람이_걸을_수_있는_칸에_있고_처음엔_아무도_갇히지_않는다()
        {
            CityGrid g = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            HashSet<string> ids = new HashSet<string>();
            foreach (CitizenDef c in D.AllCitizens)
            {
                Assert.That(ids.Add(c.id), Is.True, "사람 id 가 겹친다: " + c.id);
                Assert.That(g.Inside(c.x, c.y), Is.True, c.id + " 가 격자 밖에 있다");
                Assert.That(Kinds.Walkable(g.At(c.x, c.y)), Is.True,
                            c.id + " 가 걸을 수 없는 칸에 서 있다 (" + c.x + "," + c.y + ")");
                Assert.That(g.ReachesExit(c.x, c.y, D.City.exits), Is.True,
                            c.id + " 가 처음부터 갇혀 있다 — 사고를 재는 기준선이 서지 않는다");
                Assert.That(c.harmPerDayTrapped, Is.GreaterThan(0), c.id + " 의 해가 0 이하다");
                Assert.That(c.walkRadius, Is.GreaterThanOrEqualTo(0), c.id + " 의 반경이 없다 (-1)");
            }
            Assert.That(D.AllCitizens.Count, Is.GreaterThan(2));
        }

        /// <summary>
        /// 반경이 0인 사람과 큰 사람이 둘 다 있어야 한다. 전부 0이면 씨드가 아무것도 바꾸지 않고,
        /// 전부 크면 누가 갇힐지가 일어나는 일이 아니라 운이 된다.
        /// </summary>
        [Test]
        public void 자리를_뜨지_않는_사람과_돌아다니는_사람이_둘_다_있다()
        {
            int still = 0, roaming = 0;
            foreach (CitizenDef c in D.AllCitizens)
            {
                if (c.walkRadius == 0) still++;
                if (c.walkRadius >= 2) roaming++;
            }
            Assert.That(still, Is.GreaterThan(0), "자리를 뜨지 않는 사람이 없다 — 사고가 씨드 운이 된다");
            Assert.That(roaming, Is.GreaterThan(0), "돌아다니는 사람이 없다 — 씨드가 아무것도 바꾸지 않는다");
        }

        [Test]
        public void 잠긴_칸이_격자_안에_있다()
        {
            CityGrid g = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            foreach (CellRef c in D.City.lockedCells)
                Assert.That(g.Inside(c.x, c.y), Is.True, "잠긴 칸이 격자 밖에 있다: " + c.x + "," + c.y);

            MapPlan plan = new MapPlan(D);
            int editable = plan.EditableCells().Count;
            Assert.That(editable, Is.GreaterThan(20), "고칠 수 있는 칸이 " + editable + "개뿐이다 — 그릴 것이 없다");
            Assert.That(editable, Is.LessThan(g.Width * g.Height), "전부 고칠 수 있으면 테두리가 잠기지 않았다");
        }

        [Test]
        public void 구역이_가리키는_칸이_있다()
        {
            CityGrid g = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            foreach (DistrictDef dd in D.City.districts)
            {
                Assert.That(dd.cells.Length, Is.GreaterThan(0), dd.id + " 에 칸이 없다");
                foreach (CellRef c in dd.cells)
                    Assert.That(g.Inside(c.x, c.y), Is.True, dd.id + " 의 칸이 격자 밖이다: " + c.x + "," + c.y);
            }
        }

        [Test]
        public void 장이_가리키는_것이_전부_있다()
        {
            CityGrid g = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            HashSet<string> ids = new HashSet<string>();
            foreach (ChapterDef ch in D.AllChapters)
            {
                Assert.That(ids.Add(ch.id), Is.True, "장 id 가 겹친다: " + ch.id);
                Assert.That(ch.reachCells.Length, Is.GreaterThan(0), ch.id + " 에 닿아야 하는 칸이 없다");
                foreach (CellRef c in ch.reachCells)
                    Assert.That(g.Inside(c.x, c.y), Is.True, ch.id + " 의 reachCell 이 격자 밖이다");
                if (ch.plazaMin > 0)
                    Assert.That(g.Inside(ch.plazaSeed.x, ch.plazaSeed.y), Is.True,
                                ch.id + " 의 광장 씨앗이 격자 밖이다");
                Assert.That(ch.dayLimit, Is.GreaterThan(0), ch.id + " 의 날 한도가 0 이하다");
                Assert.That(ch.permitBudget, Is.GreaterThan(0), ch.id + " 의 허가 예산이 0 이하다");
                Assert.That(ch.goalReward, Is.GreaterThan(0), ch.id + " 의 보상이 0 이하다");
                Assert.That(ch.seed, Is.GreaterThan(0), ch.id + " 의 씨드가 없다");
                Assert.That(ch.dwellingMin, Is.GreaterThan(0), ch.id + " 의 집 하한이 없다 — 길로 덮는 것이 답이 된다");
            }
        }

        [Test]
        public void 사고가_가리키는_것이_전부_있다()
        {
            CityGrid g = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            HashSet<string> ids = new HashSet<string>();
            MapPlan plan = new MapPlan(D);
            foreach (MistakeDef mk in D.AllMistakes)
            {
                Assert.That(ids.Add(mk.id), Is.True, "사고 id 가 겹친다: " + mk.id);
                Assert.That(mk.edits.Length, Is.GreaterThan(0), mk.id + " 에 그은 선이 없다");
                foreach (EditDef e in mk.edits)
                {
                    Assert.That(g.Inside(e.x, e.y), Is.True, mk.id + " 의 선이 격자 밖이다: " + e.x + "," + e.y);
                    Assert.That(e.kind.Length, Is.EqualTo(1), mk.id + " 의 종류가 한 글자가 아니다: " + e.kind);
                    Assert.That(Kinds.IsReal(e.kind[0]), Is.True, mk.id + " 의 종류가 없는 글자다: " + e.kind);
                    Assert.That(plan.Editable(e.x, e.y), Is.True,
                                mk.id + ": 잠긴 칸에 선을 그으려 한다 (" + e.x + "," + e.y + ")");
                }
                Assert.That(mk.expectTrapped.Length, Is.GreaterThan(0), mk.id + " 에 갇힐 사람이 적혀 있지 않다");
                foreach (string cid in mk.expectTrapped)
                    Assert.That(D.HasCitizen(cid), Is.True, mk.id + " 가 없는 사람을 가리킨다: " + cid);
                if (mk.basisChapterId.Length > 0)
                    Assert.DoesNotThrow(() => D.Chapter(mk.basisChapterId), mk.id + " 의 basisChapterId 가 없는 장이다");
                Assert.That(mk.seed, Is.GreaterThan(0), mk.id + " 의 씨드가 없다");
            }
        }

        [Test]
        public void 저울이_옳다()
        {
            BalanceFile b = D.Balance;
            Assert.That(b.trialSeeds, Is.GreaterThan(0));
            Assert.That(b.conformPressurePerDay, Is.GreaterThan(0));
            Assert.That(b.inferPressurePerDay, Is.GreaterThan(0));
            Assert.That(b.inferPressurePerDay, Is.LessThan(b.conformPressurePerDay),
                        "도시가 제 짐작으로 하는 공사가 지도가 시킨 공사보다 빠르면 지도가 뜻을 잃는다");
            foreach (int c in new[] { b.flipCostBlock, b.flipCostStreet, b.flipCostPlaza })
                Assert.That(c, Is.GreaterThan(b.conformPressurePerDay),
                            "하루치 압력으로 칸이 뒤집히면 날짜가 값이 아니다");
            Assert.That(b.inferBlockOverStreets, Is.GreaterThan(b.inferStreetMin),
                        "길 이웃이 많을 때 건물을 세우는 문턱이 길을 뚫는 문턱보다 낮으면 짐작 규칙이 뒤집힌다");
            Assert.That(b.maxFlipsPerCell, Is.GreaterThan(0),
                        "필지 상한이 없으면 수렴이 보장되지 않는다. 대조군은 테스트가 -1 로 바꿔 끼운다");
            Assert.That(b.convergeDays, Is.GreaterThan(b.quietDays * 2));
            Assert.That(b.quietDays * b.inferPressurePerDay, Is.GreaterThanOrEqualTo(b.flipCostBlock),
                        "조용한 날 수가 짐작 공사 한 번보다 짧으면 진동하는 도시를 「닿았다」고 볼 수 있다");
            Assert.That(b.redrawSurcharge, Is.GreaterThan(0), "지우개가 공짜면 ButNotFree 가 성립하지 않는다");
            Assert.That(b.entombExtraHarm, Is.GreaterThan(0));
            Assert.That(b.harmPenaltyPerPoint, Is.GreaterThan(0));
            Assert.That(b.rescueMaxDays, Is.GreaterThan(0));
            Assert.That(b.rescueMaxEdits, Is.GreaterThan(0));
            Assert.That(b.rescueFrontier, Is.GreaterThan(0));
            Assert.That(b.walkStepsPerDay, Is.GreaterThan(0));
        }

        /// <summary>
        /// **안마당이 실제로 목 하나로만 붙어 있는가.** 이 사실이 깨지면 사고 시나리오 둘이
        /// 아무도 가두지 못하고, 그러면 MistakesAreSurvivable 이 헛돈다.
        /// </summary>
        [Test]
        public void 안마당이_목_하나로만_붙어_있다()
        {
            CityGrid g = new CityGrid(D.City.width, D.City.height, D.City.actualRows);
            DistrictDef court = D.District("d-court");

            List<int> inside = new List<int>();
            foreach (CellRef c in court.cells) inside.Add(g.Index(c.x, c.y));

            int necks = 0;
            foreach (CellRef c in court.cells)
                foreach (int[] n in g.Neighbours(c.x, c.y))
                {
                    if (inside.Contains(g.Index(n[0], n[1]))) continue;
                    if (Kinds.Walkable(g.At(n[0], n[1]))) necks++;
                }
            Assert.That(necks, Is.EqualTo(1), "안마당이 걸어 나갈 수 있는 자리가 " + necks + "군데다 — 하나여야 한다");
        }
    }
}
