using System.Collections.Generic;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// ★ 핵 검사기 둘 — `BothExtremesFail`.
    ///
    /// **「계속 유지」와 「즉시 해제」가 각각 다른 방식으로 나쁘게 끝나는가.**
    /// 한쪽이 그냥 낫다면 고민이 사라진다 — 나은 쪽을 고르면 되는 문제가 된다.
    ///
    /// 두 극단을 정책 하나가 아니라 **가족**으로 본다:
    ///   「유지」 = 사람을 바치는 제례만으로 내려가는 전수
    ///   「해제」 = 해제 의식과 장부 덮기만으로 내려가는 전수
    /// 그러고 나서 두 가족의 **결말 집합이 서로 겹치지 않는 자리**를 못 박는다 —
    /// 유지에는 봉기가 있고 해제에는 없다, 해제에는 기근이 있고 유지에는 없다.
    /// </summary>
    [TestFixture]
    public sealed class BothExtremesFailTests
    {
        private static readonly string[] KeepFamily = { "offer_elder", "offer_young" };
        private static readonly string[] ReleaseFamily = { "rite_release", "close_ledger" };

        private static SweepResult Keep(int seed)
        {
            return PolicySweep.Restricted(TestWorld.Data, seed, TestWorld.Generations, KeepFamily);
        }

        private static SweepResult Release(int seed)
        {
            return PolicySweep.Restricted(TestWorld.Data, seed, TestWorld.Generations, ReleaseFamily);
        }

        [Test]
        public void 두_극단의_대표_회차를_수치로_나란히_낸다()
        {
            GameData d = TestWorld.Data;
            int g = TestWorld.Generations;
            foreach (int seed in TestWorld.Seeds)
            {
                LedgerRun keep = ExtremePolicies.Keep(d, seed, g);
                LedgerRun rel = ExtremePolicies.Release(d, seed, g);

                Assert.That(keep.VillageAlive, Is.False, "「계속 유지」가 마을을 살린 채로 끝났다 — 극단이 답이 된다");
                Assert.That(rel.VillageAlive, Is.False, "「즉시 해제」가 마을을 살린 채로 끝났다 — 극단이 답이 된다");
                Assert.That(keep.Ending, Is.Not.EqualTo(rel.Ending),
                    "두 극단이 같은 방식으로 끝났다 (" + keep.EndingKorean + ") — 다른 방식으로 나빠야 고민이 남는다");

                TestContext.WriteLine("── 씨드 " + seed);
                TestContext.WriteLine("   계속 유지  : " + LedgerAudit.OneLine(keep));
                TestContext.WriteLine("   즉시 해제  : " + LedgerAudit.OneLine(rel));
                TestContext.WriteLine("   갈라지는 자리: 유지는 희생자 " + keep.VictimCount + "명에 원한 "
                    + keep.Resentment + " (부유한 채로 등을 돌린다) · "
                    + "해제는 희생자 " + rel.VictimCount + "명에 번영 " + rel.Prosperity
                    + " (아무도 바치지 않았고 굶는다)");
            }
        }

        [Test]
        public void 유지_가족_전수에_살아남는_회차가_없다()
        {
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult k = Keep(seed);
                Assert.That(k.Survivors, Is.Empty,
                    "씨드 " + seed + " 의 「유지」 가족에서 " + k.Survivors.Count + "회차가 살아남았다");
                Assert.That(k.CleanExits, Is.Empty);
                TestContext.WriteLine("씨드 " + seed + " 유지 전수: " + Distribution(k)
                    + " · 가장 깊은 대 " + k.DeepestGeneration + "/" + TestWorld.Generations);
            }
        }

        [Test]
        public void 해제_가족_전수에_살아남는_회차가_없다()
        {
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult r = Release(seed);
                Assert.That(r.Survivors, Is.Empty,
                    "씨드 " + seed + " 의 「해제」 가족에서 " + r.Survivors.Count + "회차가 살아남았다 — "
                    + "저주를 풀고도 마을이 사는 길이 있으면 이 게임의 전제가 무너진다");
                TestContext.WriteLine("씨드 " + seed + " 해제 전수: " + Distribution(r)
                    + " · 가장 깊은 대 " + r.DeepestGeneration + "/" + TestWorld.Generations);
            }
        }

        [Test]
        public void 두_가족은_서로에게_없는_결말을_가진다()
        {
            // ★ 여기가 이 검사기의 핵이다. "둘 다 나쁘다"가 아니라 **다르게 나쁘다**를 본다.
            foreach (int seed in TestWorld.Seeds)
            {
                SweepResult k = Keep(seed);
                SweepResult r = Release(seed);

                Assert.That(k.EndingKinds, Contains.Item(Ending.Uprising),
                    "「유지」에 마을 봉기가 없다 — 사람을 계속 바쳐도 마을이 등을 돌리지 않는다는 뜻이다");
                Assert.That(k.EndingKinds, Does.Not.Contain(Ending.CurseLifted),
                    "「유지」에서 저주가 풀렸다 — 바치는 것만으로 해제가 된다면 해제 의식이 쓸모없다");

                Assert.That(r.EndingKinds, Contains.Item(Ending.CurseLifted),
                    "「해제」에 해제 완수가 없다 — 극단을 밀어붙여도 저주가 풀리지 않으면 선택지가 아니다");
                Assert.That(r.EndingKinds, Does.Not.Contain(Ending.Uprising),
                    "「해제」에서 봉기가 났다 — 두 극단의 실패 방식이 같아진다");

                // 해제 완수는 반드시 기근을 얹고 온다. 그 낙차가 규칙의 전부다.
                int lifted = 0, liftedDead = 0;
                foreach (LedgerRun run in r.LiftedRuns) { lifted++; if (!run.VillageAlive) liftedDead++; }
                Assert.That(lifted, Is.GreaterThan(0));
                Assert.That(liftedDead, Is.EqualTo(lifted),
                    "저주를 풀고도 마을이 살아남은 회차가 " + (lifted - liftedDead) + "개 있다");

                TestContext.WriteLine("씨드 " + seed + ": 유지 결말 {" + Kinds(k) + "} · 해제 결말 {" + Kinds(r) + "}"
                    + " · 해제 완수 " + lifted + "회차 전부 마을이 굶었다");
            }
        }

        [Test]
        public void 유지의_값은_이름이고_해제의_값은_굶주림이다()
        {
            // 두 극단이 **다른 자원을 태운다**는 것을 수치로 남긴다.
            GameData d = TestWorld.Data;
            int g = TestWorld.Generations;
            LedgerRun keep = ExtremePolicies.Keep(d, TestWorld.MainSeed, g);
            LedgerRun rel = ExtremePolicies.Release(d, TestWorld.MainSeed, g);

            Assert.That(keep.VictimCount, Is.GreaterThan(rel.VictimCount),
                "「유지」가 「해제」보다 사람을 덜 썼다");
            Assert.That(rel.Prosperity, Is.LessThan(keep.Prosperity),
                "「해제」가 「유지」보다 마을을 덜 굶겼다");
            Assert.That(keep.Resentment, Is.GreaterThan(rel.Resentment),
                "「유지」가 「해제」보다 원한을 덜 쌓았다");

            TestContext.WriteLine("유지: 이름 " + keep.VictimCount + "개 · 원한 " + keep.Resentment
                + " · 번영 " + keep.Prosperity);
            TestContext.WriteLine("해제: 이름 " + rel.VictimCount + "개 · 원한 " + rel.Resentment
                + " · 번영 " + rel.Prosperity + " (기근 " + d.Balance.ending.releaseFamineDrop + " 낙차 포함)");
            foreach (VictimRecord v in keep.Victims)
                TestContext.WriteLine("   유지가 지운 이름: " + v.Name + " (" + v.Age + ") — " + v.Note);
        }

        private static string Distribution(SweepResult s)
        {
            List<Ending> keys = new List<Ending>(s.EndingCounts.Keys);
            keys.Sort();
            List<string> parts = new List<string>();
            foreach (Ending e in keys) parts.Add(Endings.Korean(e) + " " + s.EndingCounts[e]);
            return "결말 " + s.RunsSettled + "개 (" + string.Join(" · ", parts) + ")";
        }

        private static string Kinds(SweepResult s)
        {
            List<Ending> keys = new List<Ending>(s.EndingKinds);
            keys.Sort();
            List<string> parts = new List<string>();
            foreach (Ending e in keys) parts.Add(Endings.Korean(e));
            return string.Join(", ", parts);
        }
    }
}
