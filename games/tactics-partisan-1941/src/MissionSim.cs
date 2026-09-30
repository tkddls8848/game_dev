using System;
using System.Collections.Generic;
using System.Text;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>순찰병이 분대원을 본 순간. DetectionFairness 가 이 한 줄을 정찰 사실로 설명해야 한다.</summary>
    public struct SightingEvent
    {
        public int AtMs;
        public string GuardId;
        public string GuardZone;
        public string MemberId;
        public string MemberZone;
        public int Concealment;   // 은폐 합계. concealFloor 미만이면 보인다
    }

    /// <summary>순찰병이 소리를 들은 순간.</summary>
    public struct NoiseHeardEvent
    {
        public int AtMs;
        public string GuardId;
        public string GuardZone;
        public string OriginZone;
        public int Loudness;         // 도착 음량
        public int SourceLoudness;   // 난 자리에서의 음량. 공정성 감사가 이걸로 다시 계산한다
        public int HearingThreshold; // 이 순찰병의 귀
        public int Hops;
        public string SourceKind;    // Shoot · Detonate · PlantTrap · Trap ...
    }

    public struct MemberLostEvent
    {
        public int AtMs;
        public string MemberId;
        public string MemberZone;
        public string GuardId;
        public string GuardZone;
    }

    public sealed class MissionResult
    {
        public string MissionId;
        public bool Won;
        public string FailureReason = "";   // AlarmRaised · ObjectiveUnmet · NotExtracted · Infeasible
        public int FailureAtMs = -1;
        public int Alarm;
        public int EndedAtMs;

        public readonly List<SightingEvent> Sightings = new List<SightingEvent>();
        public readonly List<NoiseHeardEvent> NoiseHeard = new List<NoiseHeardEvent>();
        public readonly List<MemberLostEvent> MembersLost = new List<MemberLostEvent>();
        public readonly SortedSet<string> GuardsDown = new SortedSet<string>(StringComparer.Ordinal);
        public readonly SortedSet<string> TargetsDestroyed = new SortedSet<string>(StringComparer.Ordinal);
        public readonly SortedSet<string> IntelSeized = new SortedSet<string>(StringComparer.Ordinal);
        public readonly SortedSet<string> MembersDown = new SortedSet<string>(StringComparer.Ordinal);
        public readonly SortedSet<string> ChargesArmed = new SortedSet<string>(StringComparer.Ordinal);
        public int TrapsArmed;
        public readonly List<string> WastedActions = new List<string>();
        public readonly Dictionary<string, int> ActionCounts = new Dictionary<string, int>();

        /// <summary>회차 전체를 한 줄로 접은 지문. TickDeterminism 이 이것을 비교한다.</summary>
        public string LogHash = "";

        public bool CleanWin { get { return Won && Alarm == 0 && MembersDown.Count == 0; } }
    }

    /// <summary>
    /// 회차 하나를 **정수 틱 위에서 결정적으로** 전개한다.
    ///
    /// PLAN_TACTICS §8 "실시간의 유혹": 진짜 실시간으로 만들면 헤드리스 검증이 무너진다.
    /// 그래서 이 클래스에는 난수가 한 줄도 없다 — 같은 (지도·순찰·계획) 이면 같은 로그가 나온다.
    /// 씨드는 순찰 위상 선택(PatrolModel)과 거점 정산(CampLedger)에만 쓰인다.
    ///
    /// 한 틱의 판정 순서가 규칙의 핵이다:
    ///   ① 명령 완료(사살·설치·폭파·탈취)  ② 함정 작동
    ///   ③ 소음 전파 — 여기서는 **살아 있는** 순찰병만 듣는다
    ///   ④ 시야 — 역시 살아 있는 순찰병만 본다
    ///   ⑤ 교전(계속 보이면 쓰러진다)      ⑥ 경보 임계 확인
    /// ①이 ③④보다 먼저라서 "쏘기 전에 마지막 순찰병을 지웠으면 총성은 아무도 못 듣는다"가 성립한다.
    /// </summary>
    public sealed class MissionSim
    {
        private readonly GameData _data;
        private readonly MissionDef _mission;
        private readonly ZoneGraph _graph;
        private readonly PatrolModel _patrols;
        private readonly NoiseModel _noise;

        public ZoneGraph Graph { get { return _graph; } }
        public PatrolModel Patrols { get { return _patrols; } }
        public NoiseModel Noise { get { return _noise; } }
        public MissionDef Mission { get { return _mission; } }

        // 같은 회차를 수만 번 돌린다(계획 탐색). 틱마다 다시 계산하지 않는다.
        private readonly Dictionary<string, string[]> _guardZoneAt = new Dictionary<string, string[]>();
        private readonly Dictionary<string, List<NoiseArrival>> _noiseCache =
            new Dictionary<string, List<NoiseArrival>>();
        private readonly int _ticks;
        private readonly int _guardCount;
        private readonly string[] _guardIds;
        private readonly int[][] _guardZoneIdxAt;
        private readonly int[] _guardHearing;
        private readonly int _memberCount;
        private readonly string[] _memberIds;
        private readonly int[] _memberStealth;

        public MissionSim(GameData data, MissionDef mission)
        {
            _data = data;
            _mission = mission;
            _graph = new ZoneGraph(data.Map(mission.mapId));
            _patrols = new PatrolModel(data, mission);
            _noise = new NoiseModel(_graph, data.Balance.noise);

            _ticks = mission.lengthMs / data.Balance.tickMs;

            _guardCount = _patrols.GuardIds.Count;
            _guardIds = new string[_guardCount];
            _guardZoneIdxAt = new int[_guardCount][];
            _guardHearing = new int[_guardCount];
            for (int gi = 0; gi < _guardCount; gi++)
            {
                string g = _patrols.GuardIds[gi];
                _guardIds[gi] = g;
                _guardHearing[gi] = _patrols.Def(g).hearingThresholdPercent;
                string[] zones = new string[_ticks];
                int[] idx = new int[_ticks];
                for (int k = 0; k < _ticks; k++)
                {
                    zones[k] = _graph.Canon(_patrols.ZoneAt(g, k * data.Balance.tickMs));
                    idx[k] = _graph.IndexOf(zones[k]);
                }
                _guardZoneAt[g] = zones;
                _guardZoneIdxAt[gi] = idx;
            }

            _memberCount = data.AllMembers.Count;
            _memberIds = new string[_memberCount];
            _memberStealth = new int[_memberCount];
            for (int mi = 0; mi < _memberCount; mi++)
            {
                _memberIds[mi] = data.AllMembers[mi].id;
                _memberStealth[mi] = data.AllMembers[mi].stealthPercent;
            }
        }

        /// <summary>틱 k 에 이 순찰병이 있는 구역. PatrolModel 과 같은 값이고, 미리 펼쳐 둔 것이다.</summary>
        public string GuardZoneAtTick(string guardId, int k) { return _guardZoneAt[guardId][k]; }

        private List<NoiseArrival> Arrivals(string zone, int loudness)
        {
            string key = zone + "|" + loudness;
            List<NoiseArrival> cached;
            if (_noiseCache.TryGetValue(key, out cached)) return cached;
            cached = _noise.Propagate(zone, loudness);
            _noiseCache[key] = cached;
            return cached;
        }

        private struct PendingNoise
        {
            public string Zone;
            public int Loudness;
            public string Kind;
        }

        public MissionResult Run(SquadPlan plan)
        {
            MissionResult r = new MissionResult { MissionId = _mission.id };
            BalanceFile b = _data.Balance;
            int tickMs = b.tickMs;
            int ticks = _mission.lengthMs / tickMs;

            PlanRasterizer.Result raster = PlanRasterizer.Build(_data, _mission, _graph, plan);
            if (!raster.Feasible)
            {
                r.Won = false;
                r.FailureReason = "Infeasible";
                r.LogHash = Fnv("infeasible:" + raster.Reason);
                return r;
            }

            // 순찰병 상태
            Dictionary<string, bool> guardAlive = new Dictionary<string, bool>();
            bool[] alive = new bool[_guardCount];
            for (int gi = 0; gi < _guardCount; gi++) { alive[gi] = true; guardAlive[_guardIds[gi]] = true; }
            bool[] memberDown = new bool[_memberCount];
            MemberTimeline[] lines = new MemberTimeline[_memberCount];
            for (int mi = 0; mi < _memberCount; mi++) lines[mi] = raster.Timelines[_memberIds[mi]];
            bool[] sees = _graph.SeesFlat;
            int[] cover = _graph.CoverByIndex;
            int zoneCount = _graph.ZoneCount;
            int concealFloor = b.vision.concealFloorPercent;
            int sameZonePenalty = b.vision.sameZonePenaltyPercent;

            // 함정 · 폭약
            Dictionary<string, int> trapArmedAt = new Dictionary<string, int>();   // 구역 -> 무장 시각
            Dictionary<string, int> trapLoudness = new Dictionary<string, int>();  // 구역 -> 작동 소음
            Dictionary<string, int> chargeArmedAt = new Dictionary<string, int>(); // 목표 id -> 무장 시각

            Dictionary<string, int> spottedSince = new Dictionary<string, int>();
            foreach (MemberDef m in _data.AllMembers) spottedSince[m.id] = -1;

            StringBuilder log = new StringBuilder();
            List<PendingNoise> noises = new List<PendingNoise>();
            HashSet<string> seenThisTick = new HashSet<string>();

            for (int k = 0; k < ticks; k++)
            {
                int t = k * tickMs;
                noises.Clear();

                // ① 명령 완료
                List<SquadOrder> completions = raster.CompletionsAt[k];
                if (completions != null)
                {
                    foreach (SquadOrder o in completions)
                    {
                        // 쓰러진 사람은 더 이상 아무것도 못 한다. 이걸 빼면 손실이 공짜가 된다.
                        if (r.MembersDown.Contains(o.MemberId)) { r.WastedActions.Add(o + " (쓰러진 분대원)"); continue; }
                        MemberActionDef def = _data.ActionOf(o.MemberId, o.Kind);
                        ResolveAction(r, log, o, def, t, guardAlive, trapArmedAt, trapLoudness,
                                      chargeArmedAt, noises);
                    }
                    for (int gi = 0; gi < _guardCount; gi++) alive[gi] = guardAlive[_guardIds[gi]];
                }

                // ② 함정 작동 — 순찰병이 무장된 함정 구역을 밟는다
                if (trapArmedAt.Count > 0)
                {
                    for (int gi = 0; gi < _guardCount; gi++)
                    {
                        if (!alive[gi]) continue;
                        string g = _guardIds[gi];
                        string gz = _guardZoneAt[g][k];
                        int armedAt;
                        if (!trapArmedAt.TryGetValue(gz, out armedAt) || armedAt > t) continue;
                        alive[gi] = false;
                        guardAlive[g] = false;
                        r.GuardsDown.Add(g);
                        int loud = trapLoudness[gz];
                        trapArmedAt.Remove(gz);
                        trapLoudness.Remove(gz);
                        noises.Add(new PendingNoise { Zone = gz, Loudness = loud, Kind = "Trap" });
                        log.Append("T|").Append(t).Append('|').Append(g).Append('|').Append(gz).Append(';');
                    }
                }

                // ③ 소음 — 살아 있는 순찰병만 듣는다
                for (int ni = 0; ni < noises.Count; ni++)
                {
                    PendingNoise pn = noises[ni];
                    List<NoiseArrival> arrivals = Arrivals(pn.Zone, pn.Loudness);
                    for (int gi = 0; gi < _guardCount; gi++)
                    {
                        if (!alive[gi]) continue;
                        string g = _guardIds[gi];
                        string gz = _guardZoneAt[g][k];
                        int threshold = _guardHearing[gi];
                        foreach (NoiseArrival a in arrivals)
                        {
                            if (a.Zone != gz || a.Loudness < threshold) continue;
                            r.NoiseHeard.Add(new NoiseHeardEvent
                            {
                                AtMs = t, GuardId = g, GuardZone = gz, OriginZone = pn.Zone,
                                Loudness = a.Loudness, SourceLoudness = pn.Loudness,
                                HearingThreshold = threshold, Hops = a.Hops, SourceKind = pn.Kind
                            });
                            r.Alarm += b.alarm.perNoiseHeard;
                            log.Append("N|").Append(t).Append('|').Append(g).Append('|').Append(gz)
                               .Append('|').Append(pn.Zone).Append('|').Append(a.Loudness).Append(';');
                            break;
                        }
                    }
                }

                // ④ 시야
                seenThisTick.Clear();
                for (int gi = 0; gi < _guardCount; gi++)
                {
                    if (!alive[gi]) continue;
                    int gzi = _guardZoneIdxAt[gi][k];
                    int row = gzi * zoneCount;
                    for (int mi = 0; mi < _memberCount; mi++)
                    {
                        if (memberDown[mi]) continue;
                        MemberTimeline tl = lines[mi];
                        int mzi = tl.ZoneIdx[k];
                        if (!sees[row + mzi]) continue;

                        int conceal = _memberStealth[mi] + cover[mzi] - tl.Exposure[k];
                        if (gzi == mzi) conceal -= sameZonePenalty;
                        if (conceal < concealFloor && tl.Act[k] != Activity.Moving)
                            conceal += ConcealmentBonus(tl, k, mzi, tickMs);
                        if (conceal >= concealFloor) continue;

                        string mid = _memberIds[mi];
                        string mz = tl.Zone[k];
                        string g = _guardIds[gi];
                        string gz = _guardZoneAt[g][k];

                        r.Sightings.Add(new SightingEvent
                        {
                            AtMs = t, GuardId = g, GuardZone = gz,
                            MemberId = mid, MemberZone = mz, Concealment = conceal
                        });
                        r.Alarm += b.alarm.perSighting;
                        seenThisTick.Add(mid);
                        log.Append("S|").Append(t).Append('|').Append(g).Append('|').Append(gz)
                           .Append('|').Append(mid).Append('|').Append(mz).Append('|').Append(conceal).Append(';');

                        // ⑤ 교전: 계속 보이면 쓰러진다
                        if (spottedSince[mid] < 0) spottedSince[mid] = t;
                        if (t - spottedSince[mid] >= b.engagement.engageDelayMs)
                        {
                            memberDown[mi] = true;
                            r.MembersDown.Add(mid);
                            r.MembersLost.Add(new MemberLostEvent
                            {
                                AtMs = t, MemberId = mid, MemberZone = mz, GuardId = g, GuardZone = gz
                            });
                            log.Append("D|").Append(t).Append('|').Append(mid).Append('|').Append(mz).Append(';');
                        }
                    }
                }
                for (int mi = 0; mi < _memberCount; mi++)
                    if (!seenThisTick.Contains(_memberIds[mi])) spottedSince[_memberIds[mi]] = -1;

                // ⑥ 경보 임계
                if (r.Alarm >= _mission.alarmThreshold)
                {
                    r.Won = false;
                    r.FailureReason = "AlarmRaised";
                    r.FailureAtMs = t;
                    r.EndedAtMs = t;
                    r.LogHash = Fnv(log.ToString());
                    return r;
                }
            }

            r.EndedAtMs = _mission.lengthMs;
            EvaluateVictory(r, raster);
            r.LogHash = Fnv(log.ToString() + "#" + r.Won + "/" + r.Alarm + "/" + r.FailureReason);
            return r;
        }

        private void ResolveAction(MissionResult r, StringBuilder log, SquadOrder o, MemberActionDef def,
                                   int t, Dictionary<string, bool> guardAlive,
                                   Dictionary<string, int> trapArmedAt, Dictionary<string, int> trapLoudness,
                                   Dictionary<string, int> chargeArmedAt,
                                   List<PendingNoise> noises)
        {
            int n;
            r.ActionCounts[o.Kind] = r.ActionCounts.TryGetValue(o.Kind, out n) ? n + 1 : 1;

            if (o.Kind == ActionKinds.Shoot)
            {
                // 총성은 **사수의 자리**에서 난다. 탄착음은 모델에 없다 (README에 적어 둔다).
                noises.Add(new PendingNoise { Zone = o.Zone, Loudness = def.noiseLoudness, Kind = "Shoot" });

                bool alive = o.TargetId != null && guardAlive.ContainsKey(o.TargetId) && guardAlive[o.TargetId];
                if (!alive) { r.WastedActions.Add(o.ToString() + " (이미 쓰러진 순찰병)"); return; }
                string tz = _patrols.ZoneAt(o.TargetId, t);
                int hops = _graph.Hops(o.Zone, tz);
                if (hops < 0 || hops > def.rangeZones) { r.WastedActions.Add(o.ToString() + " (사거리 밖)"); return; }
                if (!_graph.Sees(o.Zone, tz)) { r.WastedActions.Add(o.ToString() + " (시야에 없다)"); return; }

                guardAlive[o.TargetId] = false;
                r.GuardsDown.Add(o.TargetId);
                log.Append("K|").Append(t).Append('|').Append(o.TargetId).Append('|').Append(tz).Append(';');
                return;
            }

            if (o.Kind == ActionKinds.PlantTrap)
            {
                trapArmedAt[o.Zone] = t;
                trapLoudness[o.Zone] = def.noiseLoudness;
                r.TrapsArmed++;
                noises.Add(new PendingNoise { Zone = o.Zone, Loudness = def.noiseLoudness, Kind = "PlantTrap" });
                log.Append("P|").Append(t).Append('|').Append(o.Zone).Append(';');
                return;
            }

            if (o.Kind == ActionKinds.PlantCharge)
            {
                if (o.TargetId == null || !_data.HasTarget(o.TargetId)) { r.WastedActions.Add(o.ToString() + " (없는 목표)"); return; }
                TargetDef target = _data.Target(o.TargetId);
                if (target.mapId != _mission.mapId || target.zone != o.Zone)
                {
                    r.WastedActions.Add(o.ToString() + " (목표가 이 구역에 없다)");
                    return;
                }
                chargeArmedAt[o.TargetId] = t;
                r.ChargesArmed.Add(o.TargetId);
                noises.Add(new PendingNoise { Zone = o.Zone, Loudness = def.noiseLoudness, Kind = "PlantCharge" });
                log.Append("C|").Append(t).Append('|').Append(o.TargetId).Append(';');
                return;
            }

            if (o.Kind == ActionKinds.Detonate)
            {
                bool any = false;
                List<string> ids = new List<string>(chargeArmedAt.Keys);
                ids.Sort(StringComparer.Ordinal);
                foreach (string id in ids)
                {
                    if (chargeArmedAt[id] > t) continue;
                    TargetDef target = _data.Target(id);
                    int hops = _graph.Hops(o.Zone, target.zone);
                    if (hops < 0 || hops > def.rangeZones) continue;
                    r.TargetsDestroyed.Add(id);
                    noises.Add(new PendingNoise { Zone = target.zone, Loudness = def.noiseLoudness, Kind = "Detonate" });
                    log.Append("X|").Append(t).Append('|').Append(id).Append('|').Append(target.zone).Append(';');
                    any = true;
                }
                if (!any) r.WastedActions.Add(o.ToString() + " (기폭할 폭약이 없다)");
                return;
            }

            if (o.Kind == ActionKinds.Infiltrate)
            {
                if (o.TargetId == null || !_data.HasIntel(o.TargetId)) { r.WastedActions.Add(o.ToString() + " (없는 문서)"); return; }
                IntelDef intel = _data.Intel(o.TargetId);
                if (intel.mapId != _mission.mapId || intel.zone != o.Zone)
                {
                    r.WastedActions.Add(o.ToString() + " (문서가 이 구역에 없다)");
                    return;
                }
                r.IntelSeized.Add(o.TargetId);
                if (def.noiseLoudness >= _noise.HearingThreshold)
                    noises.Add(new PendingNoise { Zone = o.Zone, Loudness = def.noiseLoudness, Kind = "Infiltrate" });
                log.Append("I|").Append(t).Append('|').Append(o.TargetId).Append(';');
                return;
            }

            throw new InvalidOperationException("모르는 수단: " + o.Kind);
        }

        /// <summary>
        /// 은폐 합계 = 개인 은밀 + 구역 엄폐 + (정착한) 은폐 지점 - 수행 중 노출 - 같은 구역 벌점.
        /// 이 값이 concealFloor(100) 이상이면 보이지 않는다. 전부 정수다.
        /// </summary>
        private int ConcealmentBonus(MemberTimeline tl, int k, int zoneIdx, int tickMs)
        {
            int settledMs = k * tickMs - tl.ZoneSinceMs[k];
            int best = 0;
            foreach (ConcealDef c in _graph.ConcealmentIn(_graph.ZoneAt(zoneIdx)))
                if (settledMs >= c.settleMs && c.hideBonusPercent > best) best = c.hideBonusPercent;
            return best;
        }

        private void EvaluateVictory(MissionResult r, PlanRasterizer.Result raster)
        {
            List<string> unmet = new List<string>();

            foreach (string g in _mission.eliminateGuardIds)
                if (!r.GuardsDown.Contains(g)) unmet.Add("순찰병 " + g);
            foreach (string id in _mission.destroyTargetIds)
                if (!r.TargetsDestroyed.Contains(id)) unmet.Add("목표 " + id);
            foreach (string id in _mission.seizeIntelIds)
                if (!r.IntelSeized.Contains(id)) unmet.Add("문서 " + id);

            if (unmet.Count > 0)
            {
                r.Won = false;
                r.FailureReason = "ObjectiveUnmet";
                r.FailureAtMs = _mission.lengthMs;
                return;
            }

            foreach (MemberDef m in _data.AllMembers)
            {
                if (r.MembersDown.Contains(m.id)) continue;
                MemberTimeline tl = raster.Timelines[m.id];
                if (tl.ExtractedAtMs < 0 || tl.ExtractedAtMs > _mission.extractDeadlineMs)
                {
                    r.Won = false;
                    r.FailureReason = "NotExtracted";
                    r.FailureAtMs = _mission.extractDeadlineMs;
                    return;
                }
            }

            if (r.Alarm > _mission.maxAlarm)
            {
                r.Won = false;
                r.FailureReason = "AlarmRaised";
                r.FailureAtMs = r.Sightings.Count > 0 ? r.Sightings[0].AtMs : _mission.lengthMs;
                return;
            }

            r.Won = true;
        }

        /// <summary>FNV-1a. string.GetHashCode 는 프로세스마다 달라서 재현 비교에 쓸 수 없다.</summary>
        public static string Fnv(string s)
        {
            ulong h = 14695981039346656037UL;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= s[i];
                h *= 1099511628211UL;
            }
            return h.ToString("x16");
        }
    }
}
