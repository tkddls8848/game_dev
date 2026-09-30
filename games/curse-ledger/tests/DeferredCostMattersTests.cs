using System.Collections.Generic;
using NUnit.Framework;
using CurseLedger.Data;
using CurseLedger.Sim;

namespace CurseLedger.Tests
{
    /// <summary>
    /// ★ 이 묶음의 공통 기제를 재는 검사기 — `DeferredCostMatters`.
    ///
    /// 이 PoC가 빼앗은 것은 **"내 결정의 결과를 제때 보는 일"**이다.
    /// 그것이 장식이 아니라는 것은 **두 세계 비교**로만 보인다:
    ///
    ///   세계 A (게임) : 제례의 값이 한두 대 뒤에 청구서로 도착한다
    ///   세계 B (대조군): 똑같은 값이 결정한 그 대에 즉시 붙는다
    ///
    /// 같은 씨드·같은 정책으로 둘을 돌려 결말이 유의하게 달라야 통과한다.
    /// 같으면 "세대를 건너 온다"는 말이 문장일 뿐이다.
    /// </summary>
    [TestFixture]
    public sealed class DeferredCostMattersTests
    {
        [Test]
        public void 지연을_끄면_같은_정책이_다른_결말을_맞는다()
        {
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            int seed = TestWorld.MainSeed;
            SweepResult full = TestWorld.Full(seed);

            // 전수에서 나온 정책 전부를 두 세계에 그대로 넣는다.
            List<string[]> policies = full.FullLengthPolicies;
            Assert.That(policies.Count, Is.GreaterThanOrEqualTo(cb.deferredDivergenceMinPolicies),
                "견줄 정책이 " + policies.Count + "개뿐이다");

            CurseSim on = new CurseSim(d, seed, TestWorld.Generations, true);
            CurseSim off = new CurseSim(d, seed, TestWorld.Generations, false);

            int comparable = 0, changedEnding = 0, changedAlive = 0, changedVictims = 0;
            int sumGenOn = 0, sumGenOff = 0, sumProspOn = 0, sumProspOff = 0;
            foreach (string[] p in policies)
            {
                LedgerRun a = on.Run(p);
                LedgerRun b = off.Run(p);
                if (a == null || b == null) continue;    // 두 세계에서 고를 수 있는 수가 다르면 견줄 수 없다
                comparable++;
                if (a.Ending != b.Ending) changedEnding++;
                if (a.VillageAlive != b.VillageAlive) changedAlive++;
                if (a.VictimCount != b.VictimCount) changedVictims++;
                sumGenOn += a.GenerationsSurvived; sumGenOff += b.GenerationsSurvived;
                sumProspOn += a.Prosperity; sumProspOff += b.Prosperity;
            }

            Assert.That(comparable, Is.GreaterThan(0));
            Assert.That(changedEnding, Is.GreaterThanOrEqualTo(cb.deferredMinChangedEndings),
                "지연을 껐는데 결말이 바뀐 정책이 " + changedEnding + "개뿐이다 (견준 " + comparable
                + "개) — '세대를 건너 온다'가 장식이라는 뜻이다");

            TestContext.WriteLine("견준 정책 " + comparable + "개");
            TestContext.WriteLine("  결말이 바뀐 정책      " + changedEnding
                + "개 (" + (changedEnding * 100 / comparable) + "%)");
            TestContext.WriteLine("  마을 생사가 바뀐 정책 " + changedAlive
                + "개 (" + (changedAlive * 100 / comparable) + "%)");
            TestContext.WriteLine("  희생자 수가 바뀐 정책 " + changedVictims + "개");
            TestContext.WriteLine("  평균 버틴 대   지연 " + (sumGenOn * 100 / comparable) + "/100"
                + " vs 즉시 " + (sumGenOff * 100 / comparable) + "/100");
            TestContext.WriteLine("  평균 끝 번영   지연 " + (sumProspOn * 100 / comparable) + "/100"
                + " vs 즉시 " + (sumProspOff * 100 / comparable) + "/100");
        }

        [Test]
        public void 두_극단도_지연을_끄면_다르게_끝난다()
        {
            GameData d = TestWorld.Data;
            int g = TestWorld.Generations;
            int seed = TestWorld.MainSeed;

            CurseSim on = new CurseSim(d, seed, g, true);
            CurseSim off = new CurseSim(d, seed, g, false);
            LedgerRun keepOn = ExtremePolicies.Keep(d, seed, g);
            LedgerRun keepOff = off.Run(keepOn.Policy);
            Assert.That(keepOff, Is.Not.Null);

            TestContext.WriteLine("「계속 유지」 지연 세계: " + LedgerAudit.OneLine(keepOn));
            TestContext.WriteLine("「계속 유지」 즉시 세계: " + LedgerAudit.OneLine(keepOff));
            bool same = keepOff.GenerationsSurvived == keepOn.GenerationsSurvived
                        && keepOff.Ending == keepOn.Ending
                        && keepOff.Prosperity == keepOn.Prosperity;
            Assert.That(same, Is.False, "「계속 유지」가 두 세계에서 똑같이 끝났다");

            // 즉시 세계에서는 청구서가 하나도 남지 않는다 — 그게 대조군의 정의다.
            Assert.That(LedgerAudit.BillsScheduled(keepOff), Is.Zero, "즉시 세계에 예약된 청구서가 남아 있다");
            Assert.That(LedgerAudit.BillsScheduled(keepOn), Is.GreaterThan(0), "지연 세계에 예약된 청구서가 없다");
            Assert.That(on.DeferralOn, Is.True);
            Assert.That(off.DeferralOn, Is.False);
        }

        [Test]
        public void 한_대의_상태_변화_중_앞_세대_몫이_과반에_가깝다()
        {
            GameData d = TestWorld.Data;
            int min = d.Balance.checkers.minAncestorShareInGenerationPercent;
            SweepResult full = TestWorld.Full(TestWorld.MainSeed);

            int worst = 100, sum = 0, n = 0;
            List<LedgerRun> sample = new List<LedgerRun>(full.Survivors);
            if (sample.Count > 40) sample.RemoveRange(40, sample.Count - 40);
            sample.Add(full.Best);
            sample.Add(full.BloodlessBest);
            foreach (LedgerRun r in sample)
            {
                int share = LedgerAudit.AncestorSharePercent(d, r);
                if (share < worst) worst = share;
                sum += share; n++;
            }
            Assert.That(worst, Is.GreaterThanOrEqualTo(min),
                "어떤 회차에서는 앞 세대 몫이 " + worst + "% 뿐이다 — 그 회차에서 이 기제는 없는 것과 같다");
            TestContext.WriteLine("회차 " + n + "개 · 앞 세대 몫 평균 " + (sum / n) + "% · 최저 " + worst + "%"
                + " (기준 " + min + "%)");
        }

        [Test]
        public void 모든_제례가_지연_청구서를_갖고_가장_먼_값은_세_대_뒤에_온다()
        {
            GameData d = TestWorld.Data;
            CheckerBalance cb = d.Balance.checkers;
            int majority = 0, longest = 0;
            foreach (RiteDef r in d.AllRites)
            {
                Assert.That(r.deferred, Is.Not.Null.And.Not.Empty,
                    r.id + " 에 지연 청구서가 없다 — 그 수는 결과를 제때 보여 준다");
                foreach (PendingDef p in r.deferred)
                    Assert.That(p.delayGenerations, Is.GreaterThanOrEqualTo(1),
                        r.id + " 의 청구서가 같은 대에 도착한다");

                int pct = ConsequenceWeight.DeferredPercent(r);
                int delay = ConsequenceWeight.LongestDelay(r);
                if (pct >= 50) majority++;
                if (delay > longest) longest = delay;
                TestContext.WriteLine(r.id.PadRight(16) + " 지연 비중 " + pct + "% · 가장 먼 값 " + delay + "대 뒤");
            }
            Assert.That(longest, Is.GreaterThanOrEqualTo(cb.minLongestDelayGenerations),
                "가장 먼 청구서가 " + longest + "대 뒤에 온다 — 손자 대까지 가지 않으면 '가업'이 아니다");
            Assert.That(majority, Is.GreaterThanOrEqualTo(cb.minRitesMajorityDeferred),
                "값의 과반이 미래에 있는 제례가 " + majority + "개뿐이다");
            TestContext.WriteLine("값의 과반이 미래에 있는 제례 " + majority + "/" + d.AllRites.Count
                + " · 가장 먼 값 " + longest + "대 뒤");
        }

        [Test]
        public void 앞_세대가_남긴_청구서가_첫_대의_선택_이전에_이미_놓여_있다()
        {
            // 이 PoC의 첫인상이다. 1대가 아무것도 하지 않았는데 이미 빚이 있다.
            GameData d = TestWorld.Data;
            Assert.That(d.Curse.inheritedPending, Is.Not.Null.And.Not.Empty, "물려받은 청구서가 없다");

            CurseSim sim = TestWorld.Sim(TestWorld.MainSeed);
            LedgerState s = sim.NewState();
            Assert.That(s.Bills.Count, Is.EqualTo(d.Curse.inheritedPending.Length),
                "첫 대가 시작되기 전인데 청구서가 놓여 있지 않다");

            int atFirst = 0;
            foreach (Bill b in s.Bills)
            {
                Assert.That(b.FromGeneration, Is.EqualTo(-1), "물려받은 청구서인데 출처가 이번 대다");
                Assert.That(b.Note, Is.Not.Null.And.Not.Empty, "청구서에 사연이 없다 — 화면에 쓸 것이 없다");
                d.Heir(b.Source);   // 앞 세대의 누가 남긴 것인지 족보에서 이어져야 한다
                if (b.AtGeneration == 1) atFirst++;
            }
            Assert.That(atFirst, Is.GreaterThan(0), "1대에 도착하는 청구서가 없다");

            sim.Step(s, "close_ledger");
            Assert.That(s.Lines[0].BillsArrived.Count, Is.EqualTo(atFirst),
                "1대에 도착해야 할 청구서가 도착하지 않았다");
            TestContext.WriteLine("물려받은 청구서 " + d.Curse.inheritedPending.Length + "장 중 "
                + atFirst + "장이 1대에 도착한다:");
            foreach (Bill b in s.Lines[0].BillsArrived)
                TestContext.WriteLine("   " + d.Heir(b.Source).name + " 의 값: " + b.Note);
        }
    }
}
