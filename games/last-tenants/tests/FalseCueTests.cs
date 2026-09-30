using NUnit.Framework;
using Tenants.Data;
using Tenants.Sim;

namespace Tenants.Tests
{
    /// <summary>
    /// 거짓 소리가 규칙으로 살아 있는가. 겹쳐 들으면 드러나고, 겹쳐 듣지 않으면 벌을 받는다.
    /// (이것이 없으면 씨드가 결과를 바꾸지 못해 전수 탐색이 늘 같은 답을 낸다.)
    /// </summary>
    [TestFixture]
    public class FalseCueTests
    {
        [Test]
        public void 겹쳐_들으면_거짓이_드러난다()
        {
            GameData d = TestWorld.Data;
            int cases = 0;
            for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                foreach (string bid in TestWorld.BuildingIds())
                {
                    Night n = Night.Resolve(d, bid, seed);
                    foreach (ResolvedCue lie in n.Lies())
                    {
                        int about = n.IndexOf(lie.AboutHouseholdId);
                        int liarDoor = n.IndexOf(lie.AtHouseholdId);
                        Knowledge only = new Knowledge(n);
                        if (d.IsSilent(lie.AboutHouseholdId)) only.Listen(about, 0);
                        only.Listen(liarDoor, lie.Slot);
                        if (!only.Of(about).Misled) continue;
                        foreach (ResolvedCue t in n.Cues)
                        {
                            if (t.Effect != CueEffects.Need) continue;
                            if (t.AboutHouseholdId != lie.AboutHouseholdId) continue;
                            Knowledge both = new Knowledge(n);
                            if (d.IsSilent(lie.AboutHouseholdId)) both.Listen(about, 0);
                            both.Listen(liarDoor, lie.Slot);
                            both.Listen(n.IndexOf(t.AtHouseholdId), t.Slot);
                            Assert.That(both.Of(about).Misled, Is.False,
                                bid + " 씨드" + seed + ": " + t.CueId + " 를 겹쳐 들었는데도 "
                                + lie.CueId + " 의 거짓이 남아 있다");
                            Assert.That(both.Of(about).NeedId, Is.EqualTo(t.NeedId));
                            cases++;
                        }
                    }
                }
            Assert.That(cases, Is.GreaterThan(0), "거짓을 겹쳐 들어 드러내는 경우가 데이터에 없다");
            TestContext.Out.WriteLine("겹쳐 들어 거짓이 드러난 경우 " + cases + "가지");
        }

        [Test]
        public void 거짓을_믿고_도우면_유예는_빠지고_건지는_것은_거의_없다()
        {
            GameData d = TestWorld.Data;
            int punished = 0;
            for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                foreach (string bid in TestWorld.BuildingIds())
                {
                    Night n = Night.Resolve(d, bid, seed);
                    foreach (ResolvedCue lie in n.Lies())
                    {
                        int about = n.IndexOf(lie.AboutHouseholdId);
                        Knowledge k = new Knowledge(n);
                        if (d.IsSilent(lie.AboutHouseholdId)) k.Listen(about, 0);
                        k.Listen(n.IndexOf(lie.AtHouseholdId), lie.Slot);
                        if (!k.Of(about).Misled) continue;
                        Outcome o = Outcome.Help(n, k, about, 2);
                        if (o == null) continue;
                        Assert.That(o.GraceSpent, Is.GreaterThan(0),
                            "틀린 도움인데 유예가 빠지지 않았다 — 나머지가 물어 주지 않으면 벌이 아니다");
                        Assert.That(o.SpilloverInflicted, Is.GreaterThan(0));
                        int honest = n.Households[about].baseStakes
                                     * d.Balance.help.reliefPercentByLevel[2] / 100;
                        Assert.That(o.ReliefOfChosen, Is.LessThan(honest),
                            "거짓을 믿고 도운 것이 제대로 알고 도운 것과 다르지 않다");
                        punished++;
                    }
                }
            Assert.That(punished, Is.GreaterThan(0), "거짓을 믿고 도우는 경우를 하나도 만들지 못했다");
            TestContext.Out.WriteLine("거짓을 믿고 도와 벌을 받은 경우 " + punished + "가지");
        }
    }
}
