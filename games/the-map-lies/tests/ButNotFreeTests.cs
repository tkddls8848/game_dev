using MapLies.Data;
using MapLies.Sim;
using NUnit.Framework;

namespace MapLies.Tests
{
    /// <summary>
    /// ★ 핵 검사기 셋 — **그런데 공짜는 아닌가.**
    ///
    /// 되돌릴 수 있는 것과 되돌리는 것이 공짜인 것은 다르다. 공짜면 잘못 긋는 것에 무게가 없고,
    /// 그러면 「지도를 고치면 도시가 따라온다」가 긴장이 되지 않는다.
    ///
    /// 값이 셋에서 든다:
    ///   ① **날** — 도시가 새 지도를 따라오는 데 걸리는 날
    ///   ② **허가비** — 같은 칸을 다시 그으면 덧값이 붙는다. 지우개 자국은 공짜가 아니다
    ///   ③ **해** — 갇혀 있던 동안 쌓인 것. **되돌려도 돌아오지 않는다.** 이것이 실제 값이다
    ///
    /// 두 세계 비교: 도시가 하루 만에 맞추고 해가 쌓이지 않는 세계에서는 셋이 전부 0이 되어야 한다.
    /// </summary>
    [TestFixture]
    public sealed class ButNotFreeTests
    {
        private static GameData D { get { return TestWorld.Data; } }

        [Test]
        public void 되돌리는_데_날이_든다()
        {
            foreach (string mid in TestWorld.MistakeIds())
                for (int t = 0; t < TestWorld.RescueSeeds; t++)
                {
                    RescueResult r = TestWorld.Rescue(mid, t);
                    Assert.That(r.Rescued, Is.True, mid);
                    Assert.That(r.RescueDays, Is.GreaterThan(0),
                                mid + "/씨드" + t + ": 그은 날 바로 사람이 나왔다 — 도시가 따라오는 데 날이 들지 않는다");
                }
        }

        [Test]
        public void 되돌리는_데_허가비가_든다()
        {
            foreach (string mid in TestWorld.MistakeIds())
            {
                RescueResult r = TestWorld.Rescue(mid, 0);
                Assert.That(r.RescueCost, Is.GreaterThan(0), mid + ": 되돌리는 것이 공짜다");
            }
        }

        /// <summary>
        /// **갇혀 있던 동안의 해는 돌아오지 않는다.** 이것이 이 검사기의 핵이다 —
        /// 날과 돈은 예산이지만 해는 되돌릴 수 없는 값이다.
        /// </summary>
        [Test]
        public void 갇혀_있던_동안의_해가_남는다()
        {
            foreach (string mid in TestWorld.MistakeIds())
                for (int t = 0; t < TestWorld.RescueSeeds; t++)
                {
                    RescueResult r = TestWorld.Rescue(mid, t);
                    Assert.That(r.HarmPaid, Is.GreaterThan(0),
                                mid + "/씨드" + t + ": 꺼낸 뒤에 해가 0이다 — 갇혀 있던 것이 아무 일도 아니게 된다");
                    Assert.That(r.HarmPaid, Is.GreaterThanOrEqualTo(r.HarmWhenFound),
                                mid + ": 해가 줄었다 — 되돌리기가 과거를 지웠다");
                }
        }

        /// <summary>
        /// **잘못 그었다가 되돌린 쪽이 처음부터 잘 그은 쪽보다 점수가 낮다.**
        ///
        /// 이것이 「공짜가 아니다」의 가장 곧은 형태다 — 값 항목 셋을 따로 보는 것이 아니라
        /// 게임이 실제로 세는 점수로 두 세계를 견준다.
        ///
        /// (한 번 틀렸다: 「되돌리는 허가비가 잘못 그은 허가비보다 크다」로 걸었더니 네 칸을 잘못 그은
        ///  시나리오에서 실패했다 — 한 칸을 새로 파는 것이 네 칸을 지우는 것보다 싼 것은 당연하다.
        ///  값 항목을 견주는 것이 아니라 결과를 견뤼야 했다.)
        /// </summary>
        [Test]
        public void 되돌려도_처음부터_잘_그은_것보다_손해다()
        {
            int compared = 0;
            foreach (MistakeDef mk in D.AllMistakes)
            {
                if (mk.basisChapterId.Length == 0) continue;   // 기준이 될 장이 없으면 견줄 수 없다
                ChapterDef ch = D.Chapter(mk.basisChapterId);
                RescueResult r = TestWorld.Rescue(mk.id, 0);
                Assert.That(r.Rescued, Is.True, mk.id);

                RescueResult fresh;
                CitySim sim = RescueSearch.ApplyMistake(D, mk, r.Seed, SimOptions.From(D), out fresh);
                foreach (Stroke s in r.RescueStrokes)
                    Assert.That(sim.Plan.Draw(sim.Day, s.X, s.Y, s.Kind, int.MaxValue / 4), Is.True);
                sim.Advance(D.Balance.rescueMaxDays);

                ChapterResult after = Goals.Evaluate(D, ch, sim, "잘못 그었다가 되돌림");
                ChapterResult clean = Policies.Run(D, ch.id, "Targeted", 0);
                TestContext.WriteLine(mk.id + " → " + ch.id + ": 처음부터 잘 그음 " + clean.Score
                                      + " · 잘못 그었다가 되돌림 " + after.Score
                                      + " (해 " + after.Harm + " · 허가 " + after.PermitSpent
                                      + " · 날 " + after.Days + ")");
                Assert.That(after.GoalMet, Is.True, mk.id + ": 되돌렸는데도 시의회의 요구를 못 채웠다");
                Assert.That(after.Score, Is.LessThan(clean.Score),
                            mk.id + ": 잘못 그었다가 되돌린 쪽이 처음부터 잘 그은 쪽과 같거나 낫다");
                compared++;
            }
            Assert.That(compared, Is.GreaterThan(0), "견줄 수 있는 사고가 하나도 없다");
        }

        /// <summary>같은 칸을 다시 그으면 덧값이 붙는다. 트레이싱지가 한 겹 더 쌓인다.</summary>
        [Test]
        public void 같은_칸을_다시_그으면_더_비싸다()
        {
            MapPlan plan = new MapPlan(D);
            int first = plan.CostOf(7, 4, Kinds.Block);
            Assert.That(plan.Draw(0, 7, 4, Kinds.Block, 1000), Is.True);
            int second = plan.CostOf(7, 4, Kinds.Street);
            TestContext.WriteLine("처음 그을 때 " + first + "원 · 같은 칸을 다시 그을 때 "
                                  + second + "원 (덧값 " + D.Balance.redrawSurcharge + ")");
            Assert.That(plan.DrawnCount(7, 4), Is.EqualTo(1));
            Assert.That(second, Is.GreaterThan(D.EditCost(Kinds.Street)),
                        "다시 그어도 처음과 같은 값이면 지우개가 공짜다");

            Assert.That(plan.Draw(0, 7, 4, Kinds.Street, 1000), Is.True);
            Assert.That(plan.DrawnCount(7, 4), Is.EqualTo(2), "겹이 쌓이지 않았다");
            Assert.That(plan.Strokes.Count, Is.EqualTo(2));
            Assert.That(plan.Strokes[1].Redraw, Is.True);
        }

        // ── 음성 대조군 ──────────────────────────────────────────────────────

        /// <summary>
        /// **공짜로 되돌려지는 세계를 만들어 보고 값이 사라지는지 본다.**
        /// 도시가 하루 만에 맞추고 해가 쌓이지 않으면 날과 해가 둘 다 0이 되어야 한다.
        /// 그렇지 않으면 위의 수치가 다른 데서 온 것이다.
        /// </summary>
        [Test]
        public void 대조군_공짜_세계에서는_값이_사라진다()
        {
            SimOptions free = SimOptions.From(D);
            free.InstantConform = true;
            free.HarmAccrues = false;

            foreach (MistakeDef mk in D.AllMistakes)
            {
                RescueResult r = RescueSearch.Find(D, mk, mk.seed, free);
                TestContext.WriteLine(mk.id + " (공짜 세계): 갇힘 " + r.TrappedAfterMistake.Count
                                      + " · " + (r.Rescued ? r.RescueDays + "일 · 해 " + r.HarmPaid : "못 꺼냈다"));
                if (!r.TrappedSomeone) continue;
                Assert.That(r.Rescued, Is.True, mk.id + ": 공짜 세계에서도 못 꺼냈다");
                Assert.That(r.HarmPaid, Is.Zero, mk.id + ": 해가 쌓이지 않는 세계인데 해가 남았다");
                Assert.That(r.RescueDays, Is.EqualTo(1),
                            mk.id + ": 하루에 맞추는 세계인데 " + r.RescueDays + "일이 걸렸다");
            }
        }

        /// <summary>덧값을 0으로 두면 다시 그리는 것이 처음 그리는 것과 같아져야 한다.</summary>
        [Test]
        public void 대조군_덧값을_없애면_지우개가_공짜가_된다()
        {
            GameData d = TestWorld.CloneData();
            d.Balance.redrawSurcharge = 0;
            MapPlan plan = new MapPlan(d);
            Assert.That(plan.Draw(0, 7, 4, Kinds.Block, 1000), Is.True);
            Assert.That(plan.CostOf(7, 4, Kinds.Street), Is.EqualTo(d.EditCost(Kinds.Street)),
                        "덧값을 0으로 뒀는데도 값이 다르다 — 덧값이 아닌 다른 것이 붙어 있다");
        }

        /// <summary>
        /// 묻힌 사람(설 자리가 사라진 사람)의 해가 더 빠르게 쌓인다.
        ///
        /// **같은 사람을 두 사고에서 견준다.** mk-seal-neck 은 목만 막으므로 남씨가 길 위에 갇히고,
        /// mk-brick-in 은 안마당을 통째로 덮으므로 남씨가 묻힌다. 같은 기준 지도 · 같은 씨드 ·
        /// 같은 사람이므로 차이는 묻혔는지 하나뿐이다.
        /// (한 번 틀렸다: 한 사고 안에서 둘을 찾으려 했는데 mk-brick-in 은 갇힌 사람 전부를 묻는다.)
        /// </summary>
        [Test]
        public void 묻힌_사람의_해가_더_빠르게_쌓인다()
        {
            const int days = 4;
            int trapped = Harm("mk-seal-neck", "cz-nam", days, false);
            int entombed = Harm("mk-brick-in", "cz-nam", days, true);
            TestContext.WriteLine("남씨 " + days + "일: 길 위에 갇혔을 때 " + trapped
                                  + " · 묻혔을 때 " + entombed
                                  + " (하루치 " + D.Citizen("cz-nam").harmPerDayTrapped
                                  + " + 묻힘 " + D.Balance.entombExtraHarm + ")");
            Assert.That(trapped, Is.GreaterThan(0), "길 위에 갇혔는데 해가 0이다");
            Assert.That(entombed, Is.GreaterThan(trapped),
                        "묻힌 사람과 갇힌 사람의 해가 같다 — entombExtraHarm 이 장식이다");
        }

        private static int Harm(string mistakeId, string citizenId, int days, bool expectEntombed)
        {
            RescueResult r;
            MistakeDef mk = D.Mistake(mistakeId);
            CitySim sim = RescueSearch.ApplyMistake(D, mk, mk.seed, SimOptions.From(D), out r);
            int before = sim.HarmOf(citizenId);
            sim.Advance(days);
            Assert.That(sim.IsEntombed(citizenId), Is.EqualTo(expectEntombed),
                        mistakeId + ": " + citizenId + " 의 묻힘 상태가 예상과 다르다");
            return sim.HarmOf(citizenId) - before;
        }
    }
}
