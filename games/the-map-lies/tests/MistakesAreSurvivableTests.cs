using System.Collections.Generic;
using MapLies.Data;
using MapLies.Sim;
using NUnit.Framework;

namespace MapLies.Tests
{
    /// <summary>
    /// ★ 핵 검사기 둘 — **잘못 그은 선을 되돌릴 수 있는가.**
    ///
    /// 되돌릴 수 없으면 저장·불러오기 게임이 된다. 플레이어가 선을 긋기 전에 저장하고
    /// 결과를 보고 되감는 것이 최적이 되는 순간 이 컨셉은 죽는다.
    ///
    /// 되돌리는 방법을 **탐색으로 찾는다.** 데이터에 정답을 적어 두면
    /// 「내가 답을 알기 때문에 가능하다」가 되고 플레이어가 찾을 수 있다는 증거가 되지 않는다.
    /// 후보는 갇힌 사람에서 rescueFrontier 걸음 안으로 좁힌다 — 도시 반대편을 그어 보는 것은
    /// 사람이 하는 일이 아니므로, 좁히는 것이 오히려 사람이 실제로 할 수 있는 수만 세는 것이다.
    /// </summary>
    [TestFixture]
    public sealed class MistakesAreSurvivableTests
    {
        private static GameData D { get { return TestWorld.Data; } }

        [Test]
        public void 사고가_실제로_사람을_가둔다()
        {
            foreach (string mid in TestWorld.MistakeIds())
                for (int t = 0; t < TestWorld.RescueSeeds; t++)
                {
                    RescueResult r = TestWorld.Rescue(mid, t);
                    Assert.That(r.TrappedBeforeMistake, Is.Empty,
                                mid + "/씨드" + t + ": 선을 긋기 전에 이미 갇힌 사람이 있다 — 사고 하나를 재는 것이 아니다");
                    Assert.That(r.TrappedSomeone, Is.True,
                                mid + "/씨드" + t + ": 잘못 그었는데 아무도 갇히지 않았다 — 이 시나리오가 아무것도 시험하지 않는다");
                }
        }

        [Test]
        public void 갇힐_것이라_적어_둔_사람이_실제로_갇힌다()
        {
            foreach (MistakeDef mk in D.AllMistakes)
                for (int t = 0; t < TestWorld.RescueSeeds; t++)
                {
                    RescueResult r = TestWorld.Rescue(mk.id, t);
                    foreach (string cid in mk.expectTrapped)
                        Assert.That(r.TrappedAfterMistake, Contains.Item(cid),
                                    mk.id + "/씨드" + t + ": " + cid + " 가 갇힐 것이라 적혀 있는데 갇히지 않았다");
                }
        }

        [Test]
        public void 모든_사고를_되돌릴_수_있다()
        {
            List<string> bad = new List<string>();
            foreach (string mid in TestWorld.MistakeIds())
                for (int t = 0; t < TestWorld.RescueSeeds; t++)
                {
                    RescueResult r = TestWorld.Rescue(mid, t);
                    if (!r.Rescued) { bad.Add(mid + "/씨드" + t); continue; }
                    TestContext.WriteLine(mid + "/씨드" + t + ": " + r.RescueStrokes.Count + "획 · "
                                          + r.RescueDays + "일 · " + r.RescueCost + "원 · 남은 해 " + r.HarmPaid);
                    foreach (Stroke s in r.RescueStrokes) TestContext.WriteLine("        " + s);
                }
            Assert.That(bad, Is.Empty, "되돌릴 길을 찾지 못한 사고가 있다: " + string.Join(" ", bad));
        }

        [Test]
        public void 되돌린_뒤에는_아무도_갇혀_있지_않다()
        {
            foreach (string mid in TestWorld.MistakeIds())
            {
                RescueResult r = TestWorld.Rescue(mid, 0);
                Assert.That(r.Rescued, Is.True, mid);
                // 되돌리는 획을 다시 그어 실제로 아무도 갇히지 않는지 확인한다 (탐색을 믿지 않는다)
                RescueResult fresh;
                CitySim sim = RescueSearch.ApplyMistake(D, D.Mistake(mid), r.Seed, SimOptions.From(D), out fresh);
                foreach (Stroke s in r.RescueStrokes)
                    Assert.That(sim.Plan.Draw(sim.Day, s.X, s.Y, s.Kind, int.MaxValue / 4), Is.True,
                                mid + ": 되돌리는 획을 다시 그을 수 없다");
                sim.Advance(r.RescueDays);
                Assert.That(sim.Trapped(), Is.Empty,
                            mid + ": 탐색이 찾은 획을 그대로 그었는데 아직 갇힌 사람이 있다: "
                            + string.Join(",", sim.Trapped()));
            }
        }

        /// <summary>
        /// 묻힌 사람(설 자리가 사라진 사람)도 꺼낼 수 있어야 한다. 묻히는 것이 영구 사망이면
        /// 이 게임은 잘못 그은 선 하나로 끝난다.
        /// </summary>
        [Test]
        public void 묻힌_사람도_꺼낼_수_있다()
        {
            RescueResult r = TestWorld.Rescue("mk-brick-in", 0);
            Assert.That(r.TrappedSomeone, Is.True);
            RescueResult fresh;
            CitySim sim = RescueSearch.ApplyMistake(D, D.Mistake("mk-brick-in"), r.Seed, SimOptions.From(D), out fresh);
            bool anyEntombed = false;
            foreach (CitizenDef c in D.AllCitizens) if (sim.IsEntombed(c.id)) anyEntombed = true;
            Assert.That(anyEntombed, Is.True, "mk-brick-in 이 아무도 묻지 않았다 — 묻히는 규칙을 시험하지 못했다");
            Assert.That(r.Rescued, Is.True, "묻힌 사람을 꺼낼 수 없다");
        }

        // ── 음성 대조군 ──────────────────────────────────────────────────────

        /// <summary>
        /// **날을 한 날로 줄이면 못 꺼내야 한다.** 도시가 지도를 따라오는 데 날이 드는 것이
        /// 판정에 실제로 들어가는지 보는 것이다. 여전히 꺼낼 수 있으면 「날」이 장식이다.
        /// </summary>
        [Test]
        public void 대조군_날을_빼앗으면_못_꺼낸다()
        {
            GameData d = TestWorld.CloneData();
            d.Balance.rescueMaxDays = 1;
            int failed = 0;
            foreach (MistakeDef mk in d.AllMistakes)
            {
                RescueResult r = RescueSearch.Find(d, mk, mk.seed);
                if (!r.Rescued) failed++;
            }
            TestContext.WriteLine("되돌릴 날을 하루만 주면 " + failed + "/" + d.AllMistakes.Count + " 가 실패한다");
            Assert.That(failed, Is.EqualTo(d.AllMistakes.Count),
                        "하루 만에 되돌릴 수 있는 사고가 있다 — 도시가 지도를 따라오는 데 날이 들지 않는 것이다");
        }

        /// <summary>
        /// **딴 길이 없고 필지도 닳았으면 못 꺼내야 한다.**
        ///
        /// 이 대조군이 이 검사기의 이가 어디에 있는지를 말해 준다. 실제 도시에서 사고를 되돌릴 수 있는
        /// 이유는 「되돌리기가 원래 되는 것」이 아니라 **안마당 옆에 파고 들어갈 필지가 남아 있기 때문**이다.
        /// 그 필지를 전부 잠그고 목의 필지도 닳게 하면 탐색은 답을 찾지 못한다.
        ///
        /// (한 번 틀렸다: 필지 상한만 1로 줄이면 여전히 전부 꺼낼 수 있었다 — 탐색이 목이 아니라
        ///  옆 필지를 파고 들어갔기 때문이다. 그것이 이 도시의 설계이고, 대조군은 그것을 없애야 했다.)
        /// </summary>
        [Test]
        public void 대조군_딴_길을_막고_필지도_닳으면_못_꺼낸다()
        {
            GameData d = TestWorld.CloneData();

            // 안마당 옆의 모든 필지를 잠근다 — 목(7,4) 하나만 남는다
            List<CellRef> locked = new List<CellRef>(d.City.lockedCells);
            foreach (int[] c in new[]
            {
                new[] {6, 4}, new[] {8, 4}, new[] {6, 5}, new[] {9, 5},
                new[] {7, 6}, new[] {8, 6}, new[] {6, 6}, new[] {9, 6},
                new[] {6, 3}, new[] {8, 3}, new[] {9, 4}, new[] {7, 5}, new[] {8, 5}
            })
                locked.Add(new CellRef { x = c[0], y = c[1] });
            d.City.lockedCells = locked.ToArray();

            SimOptions worn = SimOptions.From(d);
            worn.MaxFlipsPerCell = 1;      // 사고가 목의 필지를 다 쓴다

            RescueResult r = RescueSearch.Find(d, d.Mistake("mk-seal-neck"), d.Mistake("mk-seal-neck").seed, worn);
            TestContext.WriteLine("딴 길을 막고 필지도 닳게 하면: 갇힘 " + r.TrappedAfterMistake.Count
                                  + " · " + (r.Rescued ? "꺼냈다" : "**못 꺼냈다**"));
            Assert.That(r.TrappedSomeone, Is.True, "대조군에서 아무도 갇히지 않았다");
            Assert.That(r.Rescued, Is.False,
                        "딴 길도 없고 필지도 닳았는데 꺼냈다 — 탐색이 규칙을 지키지 않고 있다");

            // 같은 사고를 보통 도시에서 보면 꺼낼 수 있다. 차이가 **딴 길이 있다는 것** 하나다
            Assert.That(TestWorld.Rescue("mk-seal-neck", 0).Rescued, Is.True);
        }

        /// <summary>
        /// 그을 획 수를 0으로 줄이면 못 꺼내야 한다. 당연해 보이지만, 이것이 실패하면
        /// 「되돌렸다」가 사람이 걸어서 스스로 빠져나온 것을 잘못 센 것이라는 뜻이다.
        /// </summary>
        [Test]
        public void 대조군_아무_획도_못_그으면_못_꺼낸다()
        {
            GameData d = TestWorld.CloneData();
            d.Balance.rescueMaxEdits = 0;
            foreach (MistakeDef mk in d.AllMistakes)
            {
                RescueResult r = RescueSearch.Find(d, mk, mk.seed);
                Assert.That(r.Rescued, Is.False,
                            mk.id + ": 아무 선도 안 그었는데 사람이 빠져나왔다 — 갇힘 판정이 잘못됐다");
            }
        }
    }
}
