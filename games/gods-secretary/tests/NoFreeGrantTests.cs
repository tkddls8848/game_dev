using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Secretary.Data;
using Secretary.Sim;

namespace Secretary.Tests
{
    /// <summary>
    /// ★ **핵 검사기 2 — NoFreeGrant.**
    ///
    /// **대가 없이 들어줄 수 있는 기도가 하나도 없는가.**
    /// 하나라도 있으면 최적 전략이 「그것만 들어준다」가 되어 게임이 끝난다.
    ///
    /// 「구조적으로 불가능하다」를 믿지 않는다 — 정책마다 · 씨드마다 · 날마다 · 도장마다
    /// 접수함의 모든 편지 x 모든 출처 x 모든 양을 실제로 빼 보고 그 자리의 고통이 오르는지 센다.
    ///
    /// 그리고 **이 탐색기에 이가 있는지**를 대조군으로 확인한다: 장부 한 줄을 고쳐
    /// 누구 하나에게 여유를 만들면 탐색기가 **반드시** 그것을 찾아내야 한다.
    /// 찾아내지 못하면 「하나도 없다」는 말이 공허하다.
    /// </summary>
    [TestFixture]
    public class NoFreeGrantTests
    {
        private static FreeGrantFinder.Report ProbeRun(GameData d, string vid, int seed, Policy p)
        {
            return ProbeRun(d, d.Volume(vid), seed, p);
        }

        private static FreeGrantFinder.Report ProbeRun(GameData d, VolumeDef v, int seed, Policy p)
        {
            SimState st = Office.Begin(d, v, seed, d.Balance.conservation.enabled);
            FreeGrantFinder.Report r = new FreeGrantFinder.Report();
            while (!st.Finished)
            {
                FreeGrantFinder.Probe(st, r);
                while (st.StampsLeft > 0)
                {
                    Verdict verdict = p.Decide(st);
                    if (verdict == null) break;
                    if (!Office.Stamp(st, verdict)) break;
                    FreeGrantFinder.Probe(st, r);
                }
                Office.EndDay(st);
            }
            FreeGrantFinder.Probe(st, r);
            return r;
        }

        [Test]
        public void 대가_없이_들어줄_수_있는_기도가_하나도_없다()
        {
            GameData d = TestWorld.Data;
            int need = d.Balance.checkers.freeGrantProbeMin;
            int probes = 0, cheapest = int.MaxValue;
            string where = null;
            foreach (string vid in TestWorld.VolumeIds())
                foreach (Policy p in Policy.All())
                    for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                    {
                        FreeGrantFinder.Report r = ProbeRun(d, vid, seed, p);
                        Assert.That(r.Found, Is.Empty,
                            vid + " " + p.Name + " 씨드" + seed + ": "
                            + (r.Found.Count > 0 ? r.Found[0].ToString() : ""));
                        probes += r.Probes;
                        if (r.CheapestCost < cheapest) { cheapest = r.CheapestCost; where = r.CheapestWhere; }
                    }
            Assert.That(probes, Is.GreaterThanOrEqualTo(need),
                "뒤진 횟수가 " + probes + "번뿐이다 (기준 " + need + ") — 「하나도 없다」가 공허하다");
            Assert.That(cheapest, Is.GreaterThan(0),
                "가장 싼 허가의 값이 " + cheapest + " 이다 — 공짜 허가가 있다: " + where);
            TestContext.Out.WriteLine("뒤진 수 " + probes + "번 · 공짜 허가 0건 · 가장 싼 허가의 값 "
                                      + cheapest + " (" + where + ")");
        }

        [Test]
        public void 알려진_최선의_길에서도_공짜_허가가_없다()
        {
            // 정책이 가는 길뿐 아니라 빔 탐색이 실제로 고른 길에서도 뒤진다.
            GameData d = TestWorld.Data;
            int probes = 0;
            foreach (string vid in TestWorld.VolumeIds())
                for (int seed = 0; seed < TestWorld.SeedSweep; seed++)
                {
                    SimState best = TestWorld.Best(vid, seed);
                    SimState st = Office.Begin(d, vid, seed);
                    FreeGrantFinder.Report r = new FreeGrantFinder.Report();
                    int k = 0;
                    while (!st.Finished)
                    {
                        FreeGrantFinder.Probe(st, r);
                        while (st.StampsLeft > 0 && k < best.Log.Count)
                        {
                            VerdictRecord rec = best.Log[k];
                            if (rec.Day != st.Day) break;
                            string[] order = new string[rec.Debits.Count];
                            for (int i = 0; i < rec.Debits.Count; i++) order[i] = rec.Debits[i].SoulId;
                            if (!Office.Stamp(st, new Verdict { Kind = rec.Kind, PrayerId = rec.PrayerId, SourceOrder = order }))
                                break;
                            k++;
                            FreeGrantFinder.Probe(st, r);
                        }
                        Office.EndDay(st);
                    }
                    Assert.That(r.Found, Is.Empty, vid + " 씨드" + seed + " 알려진 최선의 길: "
                        + (r.Found.Count > 0 ? r.Found[0].ToString() : ""));
                    probes += r.Probes;
                }
            Assert.That(probes, Is.GreaterThan(0));
            TestContext.Out.WriteLine("알려진 최선의 길에서 뒤진 수 " + probes + "번 · 공짜 허가 0건");
        }

        [Test]
        public void 검사기에_이가_있다_장부를_한_줄_고치면_찾아낸다()
        {
            // 대조군. 누구 하나에게 필요보다 많은 것을 쥐여 주면 그 자리는 공짜 출처가 된다.
            // 탐색기가 그것을 못 찾으면 위의 「하나도 없다」는 아무 뜻이 없다.
            // **원본 data/ 는 건드리지 않는다** — 메모리에서 장부 한 줄만 바꿔 돌린다.
            GameData d = TestWorld.Data;
            int found = 0, checkedVolumes = 0;
            StringBuilder sb = new StringBuilder();
            foreach (string vid in TestWorld.VolumeIds())
            {
                VolumeDef broken = WithSlack(d.Volume(vid), 4);
                if (broken == null) continue;
                checkedVolumes++;
                for (int seed = 0; seed < 3; seed++)
                {
                    FreeGrantFinder.Report r = ProbeRun(d, broken, seed, new RefuseEverything());
                    found += r.Found.Count;
                    if (r.Found.Count > 0 && sb.Length < 500) sb.AppendLine("  " + r.Found[0]);
                }
            }
            Assert.That(checkedVolumes, Is.EqualTo(TestWorld.VolumeIds().Count));
            Assert.That(found, Is.GreaterThan(0),
                "장부에 일부러 여유를 만들었는데도 탐색기가 공짜 허가를 찾지 못했다 — 탐색기에 이가 없다");
            TestContext.Out.WriteLine("일부러 고친 장부에서 공짜 허가 " + found
                                      + "건을 찾아냈다:" + System.Environment.NewLine + sb);
        }

        /// <summary>장부를 베껴 **출처로 쓰이는 줄 하나**에 여유를 만든다(have 를 need 위로 올린다).</summary>
        private static VolumeDef WithSlack(VolumeDef v, int extra)
        {
            GameData d = TestWorld.Data;
            HashSet<string> sourceKeys = new HashSet<string>();
            foreach (PrayerDef p in d.PrayersOf(v.id))
                foreach (SourceDef s in p.sources) sourceKeys.Add(s.soulId + "/" + p.domain);

            List<HoldingDef> copy = new List<HoldingDef>();
            bool patched = false;
            foreach (HoldingDef h in v.holdings)
            {
                HoldingDef c = new HoldingDef
                {
                    soulId = h.soulId, domain = h.domain,
                    have = h.have, need = h.need, toleranceUnits = h.toleranceUnits
                };
                if (!patched && sourceKeys.Contains(h.soulId + "/" + h.domain) && h.have == h.need)
                {
                    c.have = h.have + extra;   // 필요보다 많이 쥐고 있다 — 여기서 빼면 아무도 아프지 않다
                    patched = true;
                }
                copy.Add(c);
            }
            if (!patched) return null;
            return new VolumeDef
            {
                id = v.id, name = v.name, seed = v.seed, days = v.days, note = v.note,
                holdings = copy.ToArray()
            };
        }
    }
}
