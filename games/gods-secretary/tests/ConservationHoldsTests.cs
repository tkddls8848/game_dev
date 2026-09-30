using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Secretary.Data;
using Secretary.Sim;

namespace Secretary.Tests
{
    /// <summary>
    /// ★ **핵 검사기 1 — ConservationHolds.**
    ///
    /// 모든 응답이 **장부에서 균형이 맞는가.**
    /// 들어준 만큼이 정확히 어딘가에서 빠지고, **그 빠진 자리가 이름을 가지는가.**
    /// (익명의 「세계」로 퉁치면 대가가 추상이 되어 아무 무게가 없다.)
    ///
    /// 한 판정마다 확인하고, 하루마다 확인하고, 마지막에 총량을 다시 센다.
    /// 그리고 **보존을 끈 대조 세계**를 같은 씨드로 돌려 값이 실제로 다른 것을 보인다 —
    /// 값이 같으면 보존이 규칙이 아니라 장식이다.
    /// </summary>
    [TestFixture]
    public class ConservationHoldsTests
    {
        private static List<Policy> Everyone { get { return Policy.All(); } }

        [Test]
        public void 모든_판정_뒤에_영역의_총량이_그대로다()
        {
            GameData d = TestWorld.Data;
            int runs = 0;
            foreach (string vid in TestWorld.VolumeIds())
                foreach (Policy p in Everyone)
                    for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                    {
                        SimState st = TestWorld.RunPolicy(vid, seed, p);
                        Assert.That(st.ConservationBreaks, Is.Empty,
                            vid + " " + p.Name + " 씨드" + seed + ": " + string.Join(" / ", st.ConservationBreaks));
                        string why;
                        Assert.That(st.L.Balanced(out why), Is.True, vid + " " + p.Name + ": " + why);
                        runs++;
                    }
            TestContext.Out.WriteLine("회차 " + runs + "번 전부 장부가 맞는다");
        }

        [Test]
        public void 들어준_만큼이_정확히_빠지고_그_자리에_이름이_있다()
        {
            GameData d = TestWorld.Data;
            int grants = 0, lines = 0;
            foreach (string vid in TestWorld.VolumeIds())
                foreach (Policy p in Everyone)
                    for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                    {
                        SimState st = TestWorld.RunPolicy(vid, seed, p);
                        foreach (VerdictRecord r in st.Log)
                        {
                            if (r.Kind != Verdicts.Grant) { Assert.That(r.Debits, Is.Empty); continue; }
                            grants++;
                            int drawn = 0;
                            foreach (DebitLine dl in r.Debits)
                            {
                                Assert.That(d.HasSoul(dl.SoulId), Is.True, "빠진 자리에 실재하지 않는 이름이 있다");
                                Assert.That(d.Soul(dl.SoulId).name, Is.Not.Null.And.Not.Empty);
                                Assert.That(dl.SoulId, Is.Not.EqualTo(r.FromSoulId), "자기에게서 뺐다");
                                Assert.That(dl.Domain, Is.EqualTo(r.Domain), "다른 영역에서 뺐다");
                                Assert.That(dl.Units, Is.GreaterThan(0));
                                drawn += dl.Units;
                                lines++;
                            }
                            Assert.That(r.Debits.Count, Is.GreaterThan(0),
                                r.PrayerId + " 를 들어줬는데 아무 데서도 빠지지 않았다");
                            Assert.That(drawn, Is.EqualTo(r.Granted),
                                r.PrayerId + ": 건너간 " + r.Granted + " 과 빠진 " + drawn + " 이 다르다");
                        }
                        Assert.That(st.GrantedUnits, Is.EqualTo(st.DrawnUnits),
                            vid + " " + p.Name + ": 회차 전체에서 건너간 합과 빠진 합이 다르다");
                    }
            Assert.That(grants, Is.GreaterThan(100), "허가가 너무 적어 이 검사가 공허하다");
            TestContext.Out.WriteLine("허가 " + grants + "건 · 이름이 붙은 차변 " + lines + "줄 전부 균형이 맞는다");
        }

        [Test]
        public void 대조_세계_보존을_끄면_값이_무너진다()
        {
            // 「들어줘도 아무 데서도 빠지지 않는 세계」를 같은 씨드로 돌린다.
            GameData d = TestWorld.Data;
            int need = d.Balance.checkers.conservationWorldGapMin;
            StringBuilder sb = new StringBuilder();
            int minGap = int.MaxValue;
            foreach (string vid in TestWorld.VolumeIds())
            {
                int on = 0, off = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    SimState a = TestWorld.Best(vid, seed);
                    SimState b = TestWorld.BestNoConservation(vid, seed);
                    on += a.Accrued; off += b.Accrued;
                    Assert.That(b.Accrued, Is.LessThan(a.Accrued),
                        vid + " 씨드" + seed + ": 보존을 껐는데 고통이 줄지 않았다 — 대가가 무게를 갖지 않는다");
                    string why;
                    Assert.That(b.L.Balanced(out why), Is.False,
                        vid + " 씨드" + seed + ": 보존을 껐는데 총량이 그대로다 — 대조 세계가 만들어지지 않았다");
                }
                int gap = (on - off) / TestWorld.SeedSweep;
                if (gap < minGap) minGap = gap;
                sb.AppendLine(d.Volume(vid).name + ": 켠 세계 평균 " + (on / TestWorld.SeedSweep)
                              + " · 끈 세계 평균 " + (off / TestWorld.SeedSweep) + " · 벌어짐 " + gap);
            }
            Assert.That(minGap, Is.GreaterThanOrEqualTo(need),
                "보존을 꺼도 고통이 " + minGap + "밖에 안 줄었다 (기준 " + need + ") — 대가가 장식이다");
            sb.AppendLine("가장 작은 벌어짐 " + minGap + " (기준 " + need + ")");
            TestContext.Out.WriteLine(sb.ToString());
        }

        [Test]
        public void 대조_세계에서는_기각_도장이_죽는다()
        {
            // 대가가 없으면 들어주는 것이 언제나 기각보다 낫다 — 봉랍 셋 중 둘이 죽고 게임이 끝난다.
            GameData d = TestWorld.Data;
            StringBuilder sb = new StringBuilder();
            int onDenies = 0, offDenies = 0, onGrants = 0, offGrants = 0;
            foreach (string vid in TestWorld.VolumeIds())
            {
                int vOnDeny = 0, vOffDeny = 0, vOnGrant = 0, vOffGrant = 0;
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    SimState a = TestWorld.Best(vid, seed);
                    SimState b = TestWorld.BestNoConservation(vid, seed);
                    vOnDeny += a.Denies; vOffDeny += b.Denies;
                    vOnGrant += a.Grants; vOffGrant += b.Grants;
                    Assert.That(b.Denies, Is.EqualTo(0),
                        vid + " 씨드" + seed + ": 보존을 껐는데도 알려진 최선이 기각을 쓴다 ("
                        + b.Denies + "번) — 대가 말고 다른 것이 기각을 살려 두고 있다");
                }
                Assert.That(vOnDeny, Is.GreaterThan(0),
                    vid + ": 켠 세계의 알려진 최선이 기각을 한 번도 쓰지 않는다 — 기각 도장이 장식이다");
                onDenies += vOnDeny; offDenies += vOffDeny; onGrants += vOnGrant; offGrants += vOffGrant;
                sb.AppendLine(d.Volume(vid).name + ": 켠 세계 최선 = 허가 " + vOnGrant + " 기각 " + vOnDeny
                              + " · 끈 세계 최선 = 허가 " + vOffGrant + " 기각 " + vOffDeny);
            }
            sb.AppendLine("합계: 켠 세계 허가 " + onGrants + " 기각 " + onDenies
                          + " · 끈 세계 허가 " + offGrants + " 기각 " + offDenies);
            TestContext.Out.WriteLine(sb.ToString());
            Assert.That(offDenies, Is.EqualTo(0));
            Assert.That(offGrants, Is.GreaterThan(onGrants),
                "보존을 껐는데 허가가 늘지 않았다 — 대가가 허가를 막고 있던 것이 아니다");
        }
    }
}
