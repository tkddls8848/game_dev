using System.Collections.Generic;
using NUnit.Framework;
using Whisper.Data;
using Whisper.Sim;

namespace Whisper.Tests
{
    /// <summary>
    /// 검사기 7 — `NoiseReachability`. **소리가 닿는 범위가 `map.json` 의 인접 관계와 맞는가.**
    /// 맞지 않으면 소음이 규칙이 아니라 장식이 된다.
    ///
    /// `NoiseModel` 의 답을 믿지 않는다. **`map.json` 만 읽는 별도 다익스트라로 다시 계산해 대조한다** —
    /// 같은 코드로 같은 답을 두 번 받는 것은 확인이 아니다.
    /// </summary>
    [TestFixture]
    public sealed class NoiseReachabilityTests
    {
        [Test]
        public void 전파_결과를_지도만_읽어_다시_계산해도_같다()
        {
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_dusk_checkpoint");
            MapDef map = d.Map(sim.Mission.mapId);
            int[] loudness = { 10, 25, 90, 100 };
            int compared = 0;

            foreach (int loud in loudness)
                foreach (string origin in sim.Graph.ZoneIds)
                {
                    Dictionary<string, int> mine = Recompute(d, map, origin, loud);
                    Dictionary<string, int> theirs = new Dictionary<string, int>();
                    foreach (NoiseArrival a in sim.Noise.Propagate(origin, loud)) theirs[a.Zone] = a.Loudness;

                    foreach (string z in sim.Graph.ZoneIds)
                    {
                        bool mineHas = mine.ContainsKey(z);
                        bool theirsHas = theirs.ContainsKey(z);
                        Assert.That(theirsHas, Is.EqualTo(mineHas),
                            loud + " @ " + origin + " → " + z + " 의 가청 판정이 다르다");
                        if (mineHas) Assert.That(theirs[z], Is.EqualTo(mine[z]),
                            loud + " @ " + origin + " → " + z + " 의 도착 음량이 다르다");
                        compared++;
                    }
                }
            TestContext.WriteLine("가청 판정 " + compared + "가지를 지도만 읽어 다시 계산해 대조했다");
        }

        [Test]
        public void 이_지도의_소리_규칙이_한_줄로_읽힌다()
        {
            // 이 넷이 이 PoC의 규칙을 손으로 읽게 하는 수치다. 바뀌면 설계가 바뀐 것이다.
            GameData d = TestWorld.Data;
            MissionSim sim = TestWorld.Sim("m_dusk_checkpoint");
            int trap = d.Squad.trapLoudnessPercent;
            int charge = d.Squad.chargeLoudnessPercent;
            int shot = d.ActionOf("m_sniper", ActionKinds.Shoot).noisePercent;
            int plant = d.ActionOf("m_sapper", ActionKinds.PlantCharge).noisePercent;

            // ① 설치(10)는 아무 데도 들리지 않는다 — 문턱 20 밑이다
            Assert.That(sim.Noise.Propagate("z_granary", plant), Is.Empty, "폭약 설치 소리가 들린다");
            // ② 함정(25)은 같은 구역뿐이다
            List<NoiseArrival> trapReach = sim.Noise.Propagate("z_belfry", trap);
            Assert.That(trapReach.Count, Is.EqualTo(1), "함정 소리가 구역을 넘었다");
            Assert.That(trapReach[0].Zone, Is.EqualTo("z_belfry"));
            // ③ 총성(90)은 한 홉을 간다 — 단 **과수원에서는 차폐에 먹혀 제자리에 남는다**
            Assert.That(sim.Noise.Propagate("z_lane", shot).Count, Is.GreaterThan(1), "총성이 한 홉도 못 간다");
            List<NoiseArrival> orchardShot = sim.Noise.Propagate("z_orchard", shot);
            Assert.That(orchardShot.Count, Is.EqualTo(1),
                "과수원에서 쏜 총성이 밖으로 나갔다 — 이 PoC의 눈먼 계획이 여기에 기대고 있다");
            Assert.That(orchardShot[0].Zone, Is.EqualTo("z_orchard"));
            // ④ 폭음(100)은 이 지도의 여섯 구역 전부에 닿는다 — 그래서 곳간 폭파는 경보 30을 피할 수 없다
            Assert.That(sim.Noise.Propagate("z_granary", charge).Count, Is.EqualTo(6),
                "폭음이 여섯 구역 전부에 닿지 않는다 — 임무의 경보 임계 30이 근거를 잃는다");
        }

        [Test]
        public void 회차에서_들린_소리가_전파_규칙과_맞는다()
        {
            // 실제 회차가 남긴 NoiseHeard 를 전파 계산으로 다시 확인한다.
            GameData d = TestWorld.Data;
            int checked_ = 0;
            foreach (string missionId in TestWorld.MissionIds())
            {
                MissionSim sim = TestWorld.Sim(missionId);
                foreach (SquadPlan plan in PlanSearch.RandomPlans(d, sim, TestWorld.Corroborated(missionId), 777, 120))
                {
                    MissionResult r = sim.Run(plan);
                    foreach (NoiseHeardEvent n in r.NoiseHeard)
                    {
                        Assert.That(sim.Noise.Hears(n.OriginZone, n.SourceLoudness, n.GuardZone, n.HearingThreshold),
                            n.OriginZone + " 의 " + n.SourceLoudness + " 가 " + n.GuardZone
                            + " 에 들렸다고 기록됐는데 전파 규칙으로는 들리지 않는다");
                        Assert.That(n.ArrivedLoudness, Is.GreaterThanOrEqualTo(n.HearingThreshold));
                        Assert.That(n.Hops, Is.LessThanOrEqualTo(sim.Noise.MaxHops(n.SourceLoudness)),
                            "차폐를 무시한 상한보다 더 멀리 갔다");
                        checked_++;
                    }
                }
            }
            Assert.That(checked_, Is.GreaterThan(0), "확인할 소리 사건이 없었다");
            TestContext.WriteLine("회차에서 들린 소리 " + checked_ + "건을 전파 규칙으로 다시 확인했다");
        }

        /// <summary>map.json 만 읽는 다익스트라. NoiseModel 을 한 줄도 쓰지 않는다.</summary>
        private static Dictionary<string, int> Recompute(GameData d, MapDef map, string origin, int loudness)
        {
            NoiseBalance nb = d.Balance.noise;
            Dictionary<string, List<string>> adj = new Dictionary<string, List<string>>();
            Dictionary<string, int> extra = new Dictionary<string, int>();
            foreach (ZoneDef z in map.zones) adj[z.id] = new List<string>();
            foreach (LinkDef l in map.links) { adj[l.a].Add(l.b); adj[l.b].Add(l.a); }
            if (map.noiseBlocked != null)
                foreach (LinkDef b in map.noiseBlocked) extra[Key(b.a, b.b)] = b.extraAttenuation;

            Dictionary<string, int> cost = new Dictionary<string, int> { { origin, 0 } };
            HashSet<string> done = new HashSet<string>();
            Dictionary<string, int> reached = new Dictionary<string, int>();
            if (loudness < nb.hearingThresholdPercent) return reached;

            while (true)
            {
                string cur = null; int best = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in cost)
                    if (!done.Contains(kv.Key) && kv.Value < best) { cur = kv.Key; best = kv.Value; }
                if (cur == null) break;
                done.Add(cur);
                int arrival = loudness - best;
                if (arrival < nb.hearingThresholdPercent) continue;
                reached[cur] = arrival;
                foreach (string n in adj[cur])
                {
                    int e; extra.TryGetValue(Key(cur, n), out e);
                    int step = best + nb.hopAttenuation + e;
                    int known;
                    if (!cost.TryGetValue(n, out known) || step < known) cost[n] = step;
                }
            }
            return reached;
        }

        private static string Key(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? a + "|" + b : b + "|" + a;
        }
    }
}
