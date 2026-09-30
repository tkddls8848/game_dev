using System.Collections.Generic;
using NUnit.Framework;
using Tactics.Data;
using Tactics.Sim;

namespace Tactics.Tests
{
    /// <summary>
    /// 검사기 5 — `NoiseReachability`.
    /// **총성·폭발이 닿는 범위가 `map.json` 의 인접 관계와 맞는가.**
    /// 깨지면 소음이 규칙이 아니라 장식이 된다 (PLAN_TACTICS §5).
    ///
    /// 여기서 재계산을 **테스트 안에서 독립적으로** 한다. 같은 함수를 두 번 부르면
    /// 아무것도 확인하지 않는 것이라서, `map.json` 의 links 만 읽는 별도 다익스트라를 쓴다.
    /// </summary>
    [TestFixture]
    public sealed class NoiseReachabilityTests
    {
        private static readonly int[] Loudnesses = { 5, 10, 25, 90, 100 };

        [Test]
        public void 전파_결과가_인접_관계로_다시_계산된다()
        {
            GameData d = TestWorld.Data;
            NoiseBalance nb = d.Balance.noise;
            int cases = 0;

            foreach (MapDef map in d.Maps.maps)
            {
                ZoneGraph graph = new ZoneGraph(map);
                NoiseModel model = new NoiseModel(graph, nb);

                foreach (ZoneDef origin in map.zones)
                {
                    foreach (int loudness in Loudnesses)
                    {
                        Dictionary<string, int> expected = RawDijkstra(map, nb, origin.id, loudness);
                        List<NoiseArrival> actual = model.Propagate(origin.id, loudness);

                        Assert.That(actual.Count, Is.EqualTo(expected.Count),
                            map.id + " " + origin.id + " 음량 " + loudness + " 의 도달 구역 수가 다르다: 모델 "
                            + Join(actual) + " vs 재계산 " + string.Join(",", expected.Keys));

                        foreach (NoiseArrival a in actual)
                        {
                            Assert.That(expected.ContainsKey(a.Zone),
                                map.id + " " + origin.id + " → " + a.Zone + " 이 재계산에 없다");
                            Assert.That(a.Loudness, Is.EqualTo(expected[a.Zone]),
                                map.id + " " + origin.id + " → " + a.Zone + " 의 도착 음량이 다르다");
                        }
                        cases++;
                    }
                }
            }
            Assert.That(cases, Is.GreaterThanOrEqualTo(60), "표본이 너무 적다");
        }

        [Test]
        public void 소리가_인접하지_않은_구역으로_뛰지_않는다()
        {
            GameData d = TestWorld.Data;
            NoiseBalance nb = d.Balance.noise;

            foreach (MapDef map in d.Maps.maps)
            {
                ZoneGraph graph = new ZoneGraph(map);
                NoiseModel model = new NoiseModel(graph, nb);

                foreach (ZoneDef origin in map.zones)
                {
                    foreach (int loudness in Loudnesses)
                    {
                        int hopBound = model.MaxHops(loudness);
                        foreach (NoiseArrival a in model.Propagate(origin.id, loudness))
                        {
                            int hops = graph.Hops(origin.id, a.Zone);
                            Assert.That(hops, Is.GreaterThanOrEqualTo(0),
                                map.id + ": " + origin.id + " 의 소리가 인접으로 닿지 않는 " + a.Zone + " 까지 갔다");
                            Assert.That(hops, Is.LessThanOrEqualTo(hopBound),
                                map.id + ": 음량 " + loudness + " 가 " + hops + "홉을 갔다 (상한 " + hopBound + ")");
                            Assert.That(a.Hops, Is.GreaterThanOrEqualTo(hops),
                                map.id + ": 보고된 홉 수가 최단 경로보다 짧다");
                        }
                    }
                }
            }
        }

        [Test]
        public void 총성은_한_홉_폭음은_두_홉이다()
        {
            // 규칙이 손으로 읽히는지 못 박는다. 이 수치가 임무 설계의 축이다.
            GameData d = TestWorld.Data;
            NoiseModel model = new NoiseModel(new ZoneGraph(d.Map("map_ridge_post")), d.Balance.noise);

            MemberActionDef shot = d.ActionOf("sniper", ActionKinds.Shoot);
            MemberActionDef blast = d.ActionOf("sapper", ActionKinds.Detonate);
            MemberActionDef trap = d.ActionOf("sapper", ActionKinds.PlantTrap);
            MemberActionDef charge = d.ActionOf("sapper", ActionKinds.PlantCharge);

            Assert.That(model.MaxHops(shot.noiseLoudness), Is.EqualTo(1), "총성이 한 홉이 아니다");
            Assert.That(model.MaxHops(blast.noiseLoudness), Is.EqualTo(2), "폭음이 두 홉이 아니다");
            Assert.That(model.MaxHops(trap.noiseLoudness), Is.EqualTo(0), "함정 소리가 같은 구역을 넘는다");
            Assert.That(model.MaxHops(charge.noiseLoudness), Is.LessThan(0), "폭약 설치 소리가 들린다");
        }

        [Test]
        public void 소음_차폐가_실제로_범위를_줄인다()
        {
            // noiseBlocked 가 장식이 아니어야 한다. 차폐를 지운 사본과 비교한다.
            GameData d = TestWorld.Data;
            int shot = d.ActionOf("sniper", ActionKinds.Shoot).noiseLoudness;
            int reducedPairs = 0;

            foreach (MapDef map in d.Maps.maps)
            {
                Assert.That(map.noiseBlocked, Is.Not.Null.And.Length.GreaterThan(0),
                    map.id + " 에 소음 차폐가 없다 — 지형이 소리에 아무 영향을 주지 않는다");

                NoiseModel withBlocks = new NoiseModel(new ZoneGraph(map), d.Balance.noise);
                MapDef open = CopyWithoutBlocks(map);
                NoiseModel without = new NoiseModel(new ZoneGraph(open), d.Balance.noise);

                foreach (LinkDef b in map.noiseBlocked)
                {
                    Assert.That(b.extraAttenuation, Is.GreaterThan(0), map.id + " 의 차폐 감쇠가 0이다");
                    bool hearsWith = withBlocks.Hears(b.a, shot, b.b);
                    bool hearsWithout = without.Hears(b.a, shot, b.b);
                    if (hearsWithout && !hearsWith) reducedPairs++;
                }
            }
            Assert.That(reducedPairs, Is.GreaterThan(0),
                "차폐를 지워도 총성이 닿는 범위가 같다 — 차폐가 규칙에 들어 있지 않다");
            TestContext.WriteLine("차폐가 총성을 막은 인접 쌍: " + reducedPairs + "개");
        }

        [Test]
        public void 회차에_적힌_들은_사건이_전파_모델과_맞는다()
        {
            // 시뮬레이터가 적은 NoiseHeard 가 모델과 어긋나면 로그를 신뢰할 수 없다.
            MissionSim sim = TestWorld.Sim("m_ridge_dusk");
            MissionResult r = sim.Run(new SquadPlan(new[]
            {
                new SquadOrder("sniper", ActionKinds.Shoot, "z_ridge", "g_rover", 62000)
            }));
            Assert.That(r.NoiseHeard.Count, Is.GreaterThan(0), "총성을 아무도 듣지 않았다");

            foreach (NoiseHeardEvent n in r.NoiseHeard)
            {
                bool found = false;
                foreach (NoiseArrival a in sim.Noise.Propagate(n.OriginZone, ExpectedLoudness(n)))
                    if (a.Zone == n.GuardZone && a.Loudness == n.Loudness) { found = true; break; }
                Assert.That(found, "들은 사건이 전파 모델에 없다: " + n.OriginZone + " → " + n.GuardZone
                                   + " " + n.Loudness + " (" + n.SourceKind + ")");
                Assert.That(sim.Patrols.ZoneAt(n.GuardId, n.AtMs), Is.EqualTo(n.GuardZone),
                    "들은 순찰병의 자리가 순찰 표와 다르다");
            }
        }

        private static int ExpectedLoudness(NoiseHeardEvent n)
        {
            GameData d = TestWorld.Data;
            if (n.SourceKind == "Shoot") return d.ActionOf("sniper", ActionKinds.Shoot).noiseLoudness;
            if (n.SourceKind == "Detonate") return d.ActionOf("sapper", ActionKinds.Detonate).noiseLoudness;
            return d.ActionOf("sapper", ActionKinds.PlantTrap).noiseLoudness;
        }

        /// <summary>
        /// `map.json` 의 links · noiseBlocked 만 읽어 감쇠 합이 가장 작은 경로를 찾는다.
        /// NoiseModel 과 코드를 나누지 않는다 — 나누면 같은 버그가 양쪽에 있어도 통과한다.
        /// </summary>
        private static Dictionary<string, int> RawDijkstra(MapDef map, NoiseBalance nb, string origin, int loudness)
        {
            Dictionary<string, int> reach = new Dictionary<string, int>();
            if (loudness < nb.hearingThresholdPercent) return reach;

            Dictionary<string, List<string>> adj = new Dictionary<string, List<string>>();
            foreach (ZoneDef z in map.zones) adj[z.id] = new List<string>();
            foreach (LinkDef l in map.links) { adj[l.a].Add(l.b); adj[l.b].Add(l.a); }

            Dictionary<string, int> extra = new Dictionary<string, int>();
            if (map.noiseBlocked != null)
                foreach (LinkDef b in map.noiseBlocked)
                {
                    extra[b.a + "|" + b.b] = b.extraAttenuation;
                    extra[b.b + "|" + b.a] = b.extraAttenuation;
                }

            Dictionary<string, int> cost = new Dictionary<string, int> { { origin, 0 } };
            HashSet<string> done = new HashSet<string>();
            while (true)
            {
                string cur = null;
                int best = int.MaxValue;
                foreach (KeyValuePair<string, int> kv in cost)
                    if (!done.Contains(kv.Key) && kv.Value < best) { cur = kv.Key; best = kv.Value; }
                if (cur == null) break;
                done.Add(cur);

                int arrival = loudness - best;
                if (arrival < nb.hearingThresholdPercent) continue;
                reach[cur] = arrival;

                foreach (string n in adj[cur])
                {
                    int step = nb.hopAttenuation;
                    int e;
                    if (extra.TryGetValue(cur + "|" + n, out e)) step += e;
                    int next = best + step;
                    int known;
                    if (!cost.TryGetValue(n, out known) || next < known) cost[n] = next;
                }
            }
            return reach;
        }

        private static MapDef CopyWithoutBlocks(MapDef map)
        {
            return new MapDef
            {
                id = map.id + "_open", name = map.name,
                entryZone = map.entryZone, extractionZone = map.extractionZone,
                zones = map.zones, links = map.links, sightLines = map.sightLines,
                noiseBlocked = new LinkDef[0], concealment = map.concealment,
                reconVantages = map.reconVantages
            };
        }

        private static string Join(List<NoiseArrival> arrivals)
        {
            List<string> parts = new List<string>();
            foreach (NoiseArrival a in arrivals) parts.Add(a.Zone + ":" + a.Loudness);
            return string.Join(",", parts);
        }
    }
}
