using System;
using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
{
    public struct SightingEvent
    {
        public int AtMs; public string GuardId; public string GuardZone;
        public string MemberId; public string MemberZone; public int ReprisalPoints;
    }

    public struct NoiseHeardEvent
    {
        public int AtMs; public string GuardId; public string GuardZone;
        public string OriginZone; public string SourceKind;
        public int SourceLoudness; public int ArrivedLoudness; public int Hops; public int HearingThreshold;
    }

    public struct MemberLostEvent
    {
        public int AtMs; public string MemberId; public string MemberZone;
        public string GuardId; public string GuardZone;
    }

    public sealed class MissionResult
    {
        public string MissionId;
        public string PhaseLabel;
        public bool Won;
        public string FailureReason = "";
        public int AlarmPercent;
        public int ReprisalPoints;
        public bool ChargePlanted;
        public bool TargetDestroyed;
        public bool DocumentStolen;
        public int ObjectiveDoneMs = -1;
        public int EndMs;
        /// <summary>분대원이 지도 위에 있던 틱의 합. **정보가 사는 것이 이 값이다** — 묻지 않으면 기다려야 한다.</summary>
        public int FieldTicks;
        public readonly SortedSet<string> GuardsDowned = new SortedSet<string>(StringComparer.Ordinal);
        public readonly List<SightingEvent> Sightings = new List<SightingEvent>();
        public readonly List<NoiseHeardEvent> NoiseHeard = new List<NoiseHeardEvent>();
        public readonly List<MemberLostEvent> MembersLost = new List<MemberLostEvent>();
        public readonly List<string> Log = new List<string>();

        public bool Infeasible { get { return FailureReason == FailureReasons.Infeasible; } }
    }

    public static class FailureReasons
    {
        public static string Infeasible { get { return "Infeasible"; } }
        public static string AlarmBreach { get { return "AlarmBreach"; } }
        public static string MemberLost { get { return "MemberLost"; } }
        public static string ObjectiveNotMet { get { return "ObjectiveNotMet"; } }
        public static string NoExtraction { get { return "NoExtraction"; } }
        public static string OrderTimedOut { get { return "OrderTimedOut"; } }
        public static string OrderCollision { get { return "OrderCollision"; } }
        public static string GuardDowned { get { return "GuardDowned"; } }
    }

    /// <summary>
    /// 회차 전개. **난수가 한 줄도 없다** — 씨드는 위상과 거짓 배분에만 쓰이고, 여기까지 오지 않는다.
    /// `DataIntegrityTests.회차_전개에는_난수가_없다` 가 이것을 파일 단위로 지킨다.
    ///
    /// 한 틱의 판정 순서가 규칙의 핵이다:
    ///   ① 분대원 진행 (이동·정착·대기·수행·완료: 사살·설치·폭파·탈취)
    ///   ② 함정 작동
    ///   ③ 소음 전파 — **살아 있는** 순찰병만 듣는다
    ///   ④ 시야     — 역시 살아 있는 순찰병만 본다 → 목격 + 보복 점수
    ///   ⑤ 교전 (engageDelayMs 동안 계속 보이면 쓰러진다)
    ///   ⑥ 경보 임계 확인 — 넘으면 그 자리에서 끝난다
    ///
    /// ①이 ③④보다 먼저다. 그래서 **쏘기 전에 마지막 순찰병을 지웠으면 총성은 아무도 못 듣는다**가 성립한다.
    /// </summary>
    public sealed class MissionSim
    {
        private sealed class MemberRun
        {
            public MemberDef Def;
            public List<SquadOrder> Queue = new List<SquadOrder>();
            public int Next;
            public string Zone;              // null = 지도 위에 없다
            public int State;                // 0 대기 1 이동 2 정착 3 사선대기 4 수행 5 복귀 6 빠져나감
            public List<string> Path;
            public int PathIdx;
            public int StepEndMs;
            public int PhaseEndMs;
            public int HoldDeadlineMs;
            public bool Settled;
            public bool Alive = true;
            public int SeenSinceMs = -1;
            public MemberActionDef Action;
            public int EnterFieldMs = -1;
        }

        private const int StWaiting = 0, StMoving = 1, StSettling = 2, StHolding = 3, StReady = 7,
                          StActing = 4, StReturning = 5, StDone = 6;

        private readonly GameData _data;
        private readonly MissionDef _mission;
        private readonly ZoneGraph _graph;
        private readonly PatrolModel _patrols;
        private readonly NoiseModel _noise;
        private readonly PhaseAssignment _phases;
        private readonly int _tickMs;

        public GameData Data { get { return _data; } }
        public MissionDef Mission { get { return _mission; } }
        public ZoneGraph Graph { get { return _graph; } }
        public PatrolModel Patrols { get { return _patrols; } }
        public NoiseModel Noise { get { return _noise; } }
        public PhaseAssignment Phases { get { return _phases; } }

        public MissionSim(GameData data, MissionDef mission, PhaseAssignment phases)
        {
            _data = data;
            _mission = mission;
            _phases = phases;
            _graph = new ZoneGraph(data.Map(mission.mapId));
            _patrols = new PatrolModel(data, mission, phases);
            _noise = new NoiseModel(_graph, data.Balance.noise);
            _tickMs = data.Balance.tickMs;
        }

        /// <summary>오늘 밤(씨드가 고른 위상)의 회차.</summary>
        public static MissionSim Tonight(GameData data, string missionId)
        {
            MissionDef m = data.Mission(missionId);
            return new MissionSim(data, m, PhaseAssignment.Tonight(data, m));
        }

        private struct NoiseSource { public string Zone; public int Loudness; public string Kind; }

        public MissionResult Run(SquadPlan plan)
        {
            MissionResult r = new MissionResult { MissionId = _mission.id, PhaseLabel = _phases.ToString() };
            VisionBalance vis = _data.Balance.vision;
            AlarmBalance alarmBal = _data.Balance.alarm;
            ReprisalBalance rep = _data.Balance.reprisal;

            // ── 계획을 분대원별 큐로 나눈다 ────────────────────────────────────
            Dictionary<string, MemberRun> runs = new Dictionary<string, MemberRun>();
            List<string> memberOrder = new List<string>();
            foreach (MemberDef md in _data.AllMembers)
            {
                runs[md.id] = new MemberRun { Def = md, Zone = null };
                memberOrder.Add(md.id);
            }
            foreach (SquadOrder o in plan.SortedOrders())
            {
                if (!runs.ContainsKey(o.MemberId)) return Fail(r, FailureReasons.Infeasible, "없는 분대원: " + o.MemberId);
                if (!_graph.HasZone(o.Zone)) return Fail(r, FailureReasons.Infeasible, "없는 구역: " + o.Zone);
                if (_data.ActionOf(o.MemberId, o.Kind) == null)
                    return Fail(r, FailureReasons.Infeasible, o.MemberId + " 는 " + o.Kind + " 를 못 한다");
                if (o.StartMs < 0 || o.StartMs > _mission.lengthMs)
                    return Fail(r, FailureReasons.Infeasible, "회차 밖의 시각: " + o.StartMs);
                runs[o.MemberId].Queue.Add(o.Copy());
            }
            foreach (string id in memberOrder)
            {
                int traps = 0, charges = 0;
                foreach (SquadOrder o in runs[id].Queue)
                {
                    if (o.Kind == ActionKinds.PlantTrap) traps++;
                    if (o.Kind == ActionKinds.PlantCharge) charges++;
                }
                if (traps > runs[id].Def.maxTraps) return Fail(r, FailureReasons.Infeasible, "함정 한도 초과: " + id);
                if (charges > runs[id].Def.maxCharges) return Fail(r, FailureReasons.Infeasible, "폭약 한도 초과: " + id);
            }

            Dictionary<string, bool> guardAlive = new Dictionary<string, bool>();
            foreach (string g in _patrols.GuardIds) guardAlive[g] = true;
            List<string> trapZones = new List<string>();
            string chargeZone = null;
            bool chargeArmed = false;
            int alarm = 0;
            int reprisal = 0;
            int length = _mission.lengthMs;
            List<NoiseSource> noises = new List<NoiseSource>();

            for (int t = 0; t <= length; t += _tickMs)
            {
                noises.Clear();

                // ── ① 분대원 진행 ───────────────────────────────────────────
                foreach (string id in memberOrder)
                {
                    MemberRun m = runs[id];
                    if (!m.Alive || m.State == StDone || m.Queue.Count == 0) continue;

                    if (m.State == StWaiting)
                    {
                        if (m.Next >= m.Queue.Count)
                        {
                            if (m.Zone == null) { m.State = StDone; continue; }
                            BeginMove(m, m.Zone, _graph.ExtractionZone, t);
                            m.State = StReturning;
                            if (m.Path.Count == 1) { m.State = StDone; m.Zone = null; }
                            continue;
                        }
                        SquadOrder o = m.Queue[m.Next];
                        string from = m.Zone ?? _graph.EntryZone;
                        int hops = _graph.Hops(from, o.Zone);
                        if (hops < 0) return Fail(r, FailureReasons.Infeasible, "닿을 수 없는 자리: " + o.Zone);
                        int settleMs = o.Settle ? _graph.BestConcealmentSettleMs(o.Zone) : 0;
                        int departMs = o.StartMs - settleMs - hops * m.Def.moveMsPerZone;
                        // 명령이 밀렸으면 늦게라도 간다 — 계획이 무효가 되지는 않는다. 대신 늦어진다.
                        if (t < departMs) continue;
                        if (m.EnterFieldMs < 0) { m.EnterFieldMs = t; m.Zone = _graph.EntryZone; from = _graph.EntryZone; }
                        BeginMove(m, from, o.Zone, t);
                        if (m.Path.Count == 1) ArriveAtOrderZone(m, t);
                        continue;
                    }

                    if (m.State == StMoving && t >= m.StepEndMs)
                    {
                        m.PathIdx++;
                        m.Zone = m.Path[m.PathIdx];
                        if (m.PathIdx >= m.Path.Count - 1) ArriveAtOrderZone(m, t);
                        else m.StepEndMs = t + m.Def.moveMsPerZone;
                    }

                    if (m.State == StSettling && t >= m.PhaseEndMs) { m.Settled = true; m.State = StReady; }

                    if (m.State == StReady)
                    {
                        SquadOrder o = m.Queue[m.Next];
                        m.Action = _data.ActionOf(m.Def.id, o.Kind);
                        if (t >= o.StartMs)
                        {
                            if (o.Kind == ActionKinds.Shoot)
                            {
                                m.State = StHolding;
                                m.HoldDeadlineMs = Math.Max(o.StartMs, t) + m.Action.holdWindowMs;
                            }
                            else { m.State = StActing; m.PhaseEndMs = t + m.Action.durationMs; }
                        }
                    }

                    if (m.State == StHolding)
                    {
                        SquadOrder o = m.Queue[m.Next];
                        string tz = AliveGuardZone(o.TargetId, guardAlive, t);
                        bool inSight = tz != null && _graph.Sees(o.Zone, tz)
                                       && _graph.Hops(o.Zone, tz) <= m.Action.rangeHops;
                        if (inSight) { m.State = StActing; m.PhaseEndMs = t + m.Action.durationMs; }
                        else if (t > m.HoldDeadlineMs)
                            return Fail(r, FailureReasons.OrderTimedOut,
                                o + " 가 " + m.Action.holdWindowMs + "ms 를 기다려도 표적이 사선에 들어오지 않았다");
                    }

                    if (m.State == StActing && t >= m.PhaseEndMs)
                    {
                        SquadOrder o = m.Queue[m.Next];
                        MissionResult bad = Resolve(r, m, o, t, guardAlive, trapZones, noises,
                                                    ref chargeZone, ref chargeArmed);
                        if (bad != null) return bad;
                        m.Next++;
                        m.Settled = false;
                        m.State = StWaiting;
                    }

                    if (m.State == StReturning && t >= m.StepEndMs)
                    {
                        m.PathIdx++;
                        m.Zone = m.Path[m.PathIdx];
                        if (m.PathIdx >= m.Path.Count - 1) { m.State = StDone; m.Zone = null; }
                        else m.StepEndMs = t + m.Def.moveMsPerZone;
                    }
                }

                // 지도 위에 있던 틱을 센다. **정보가 사는 것이 이 값이다.**
                foreach (string id in memberOrder)
                    if (runs[id].Zone != null && runs[id].Alive) r.FieldTicks++;

                // ── ② 함정 작동 ─────────────────────────────────────────────
                for (int i = trapZones.Count - 1; i >= 0; i--)
                {
                    string tz = trapZones[i];
                    string victim = null;
                    foreach (string g in _patrols.GuardIds)
                        if (guardAlive[g] && _graph.Canon(_patrols.ZoneAt(g, t)) == tz) { victim = g; break; }
                    if (victim == null) continue;
                    guardAlive[victim] = false;
                    r.GuardsDowned.Add(victim);
                    r.Log.Add(t + "ms 함정작동 " + victim + "@" + tz);
                    trapZones.RemoveAt(i);
                    noises.Add(new NoiseSource { Zone = tz, Loudness = _data.Squad.trapLoudnessPercent, Kind = "함정" });
                }

                // ── ③ 소음 전파 — 살아 있는 순찰병만 듣는다 ────────────────────
                foreach (NoiseSource ns in noises)
                {
                    if (ns.Loudness < _noise.HearingThreshold) continue;
                    List<NoiseArrival> arrivals = _noise.Propagate(_graph.Canon(ns.Zone), ns.Loudness);
                    foreach (string g in _patrols.GuardIds)
                    {
                        if (!guardAlive[g]) continue;
                        string gz = _graph.Canon(_patrols.ZoneAt(g, t));
                        int threshold = _patrols.Def(g).hearingThresholdPercent;
                        foreach (NoiseArrival a in arrivals)
                        {
                            if (a.Zone != gz || a.Loudness < threshold) continue;
                            r.NoiseHeard.Add(new NoiseHeardEvent
                            {
                                AtMs = t, GuardId = g, GuardZone = gz,
                                OriginZone = _graph.Canon(ns.Zone), SourceKind = ns.Kind,
                                SourceLoudness = ns.Loudness, ArrivedLoudness = a.Loudness,
                                Hops = a.Hops, HearingThreshold = threshold
                            });
                            alarm += alarmBal.perNoiseHeard;
                            r.Log.Add(t + "ms 소리 " + g + " 가 " + ns.Kind + " 을 들었다 (" + a.Loudness + ")");
                            break;
                        }
                    }
                }

                // ── ④ 시야 — 살아 있는 순찰병만 본다 ──────────────────────────
                // 은밀 + 엄폐 + 은폐지점(묻었을 때) - 노출(수행 중) - 같은구역벌점 >= 100 이면 보이지 않는다.
                foreach (string id in memberOrder)
                {
                    MemberRun m = runs[id];
                    if (!m.Alive || m.Zone == null) continue;
                    int conceal = m.Def.stealthPercent + _graph.CoverPercent(m.Zone);
                    if (m.Settled) conceal += _graph.BestConcealmentBonus(m.Zone);
                    if (m.State == StActing && m.Action != null) conceal -= m.Action.exposurePercent;

                    bool seenThisTick = false;
                    foreach (string g in _patrols.GuardIds)
                    {
                        if (!guardAlive[g]) continue;
                        string gz = _graph.Canon(_patrols.ZoneAt(g, t));
                        int total;
                        if (gz == m.Zone) total = conceal - vis.sameZonePenaltyPercent;
                        else if (_graph.Sees(gz, m.Zone)) total = conceal;
                        else continue;
                        if (total >= vis.concealFloorPercent) continue;

                        seenThisTick = true;
                        int points = _graph.ReprisalWeight(m.Zone) * rep.perSightingBasePercent / 100;
                        reprisal += points;
                        alarm += alarmBal.perSighting;
                        r.Sightings.Add(new SightingEvent
                        {
                            AtMs = t, GuardId = g, GuardZone = gz,
                            MemberId = m.Def.id, MemberZone = m.Zone, ReprisalPoints = points
                        });
                        r.Log.Add(t + "ms 목격 " + g + "(" + gz + ") 가 " + m.Def.id + "(" + m.Zone + ") 를 봤다");

                        // ── ⑤ 교전 — 계속 보이면 쓰러진다 ───────────────────
                        if (m.SeenSinceMs < 0) m.SeenSinceMs = t;
                        else if (t - m.SeenSinceMs >= _data.Balance.engagement.engageDelayMs)
                        {
                            string lostAt = m.Zone;
                            m.Alive = false;
                            m.Zone = null;
                            r.MembersLost.Add(new MemberLostEvent
                            {
                                AtMs = t, MemberId = m.Def.id, MemberZone = lostAt,
                                GuardId = g, GuardZone = gz
                            });
                            r.Log.Add(t + "ms 손실 " + m.Def.id);
                        }
                        break;
                    }
                    if (!seenThisTick) m.SeenSinceMs = -1;
                }

                // ── ⑥ 경보 임계 확인 ────────────────────────────────────────
                if (alarm > _mission.alarmLimit)
                {
                    r.AlarmPercent = alarm;
                    reprisal += rep.onAlarmBreachFlat;
                    r.ReprisalPoints = reprisal + rep.onFailureFlat;
                    r.EndMs = t;
                    return Fail(r, FailureReasons.AlarmBreach,
                        "경보 " + alarm + " 이 임계 " + _mission.alarmLimit + " 를 넘었다");
                }
                if (r.MembersLost.Count > 0)
                {
                    r.AlarmPercent = alarm;
                    r.ReprisalPoints = reprisal + rep.onFailureFlat;
                    r.EndMs = t;
                    return Fail(r, FailureReasons.MemberLost, "분대원을 잃었다");
                }

                if (r.ObjectiveDoneMs < 0 && _mission.downAllGuards
                    && r.GuardsDowned.Count >= _patrols.GuardIds.Count) r.ObjectiveDoneMs = t;
                if (AllExtracted(runs, memberOrder) && ObjectiveMet(r)) { r.EndMs = t; break; }
                r.EndMs = t;
            }

            r.AlarmPercent = alarm;

            // ── 승리 판정 ───────────────────────────────────────────────────
            if (!ObjectiveMet(r))
            {
                r.ReprisalPoints = reprisal + rep.onFailureFlat;
                return Fail(r, FailureReasons.ObjectiveNotMet, "목표를 이루지 못했다");
            }
            if (_mission.noGuardsDowned && r.GuardsDowned.Count > 0)
            {
                r.ReprisalPoints = reprisal + rep.onFailureFlat;
                return Fail(r, FailureReasons.GuardDowned, "아무도 쓰러뜨리지 않아야 했다");
            }
            if (_mission.requireExtraction)
            {
                foreach (string id in memberOrder)
                {
                    MemberRun m = runs[id];
                    if (m.Queue.Count == 0) continue;
                    if (m.State != StDone)
                    {
                        r.ReprisalPoints = reprisal + rep.onFailureFlat;
                        return Fail(r, FailureReasons.NoExtraction, m.Def.id + " 가 아마밭으로 돌아오지 못했다");
                    }
                }
            }

            r.Won = true;
            r.ReprisalPoints = reprisal;
            return r;
        }

        private MissionResult Resolve(MissionResult r, MemberRun m, SquadOrder o, int t,
                                      Dictionary<string, bool> guardAlive, List<string> trapZones,
                                      List<NoiseSource> noises, ref string chargeZone, ref bool chargeArmed)
        {
            if (o.Kind == ActionKinds.Shoot)
            {
                if (guardAlive.ContainsKey(o.TargetId) && guardAlive[o.TargetId])
                {
                    guardAlive[o.TargetId] = false;
                    r.GuardsDowned.Add(o.TargetId);
                    r.Log.Add(t + "ms 사살 " + o.TargetId);
                }
                noises.Add(new NoiseSource { Zone = m.Zone, Loudness = m.Action.noisePercent, Kind = "총성" });
            }
            else if (o.Kind == ActionKinds.PlantTrap)
            {
                trapZones.Add(_graph.Canon(o.Zone));
                noises.Add(new NoiseSource { Zone = m.Zone, Loudness = m.Action.noisePercent, Kind = "설치" });
                r.Log.Add(t + "ms 함정 " + o.Zone);
            }
            else if (o.Kind == ActionKinds.PlantCharge)
            {
                TargetDef target = _data.HasTarget(o.TargetId) ? _data.Target(o.TargetId) : null;
                if (target == null || _graph.Canon(target.zone) != _graph.Canon(o.Zone))
                    return Fail(r, FailureReasons.Infeasible, "폭약을 목표가 없는 자리에 놓았다: " + o.Zone);
                chargeZone = _graph.Canon(o.Zone);
                chargeArmed = true;
                r.ChargePlanted = true;
                noises.Add(new NoiseSource { Zone = m.Zone, Loudness = m.Action.noisePercent, Kind = "설치" });
                r.Log.Add(t + "ms 폭약 " + o.Zone);
            }
            else if (o.Kind == ActionKinds.Detonate)
            {
                if (!chargeArmed) return Fail(r, FailureReasons.Infeasible, "놓지 않은 폭약을 터뜨렸다");
                int hops = _graph.Hops(m.Zone, chargeZone);
                // 제 발밑의 폭약은 터뜨리지 않는다. 그래서 폭파에는 **물러나는 시간**이 든다.
                if (hops < 1) return Fail(r, FailureReasons.Infeasible, "폭약 위에 서서 터뜨릴 수는 없다");
                if (hops > m.Action.rangeHops)
                    return Fail(r, FailureReasons.Infeasible, "폭약이 사거리 밖이다: " + hops + "홉");
                chargeArmed = false;
                r.TargetDestroyed = true;
                if (r.ObjectiveDoneMs < 0) r.ObjectiveDoneMs = t;
                noises.Add(new NoiseSource
                {
                    Zone = chargeZone, Loudness = _data.Squad.chargeLoudnessPercent, Kind = "폭음"
                });
                r.Log.Add(t + "ms 폭파 " + chargeZone);
            }
            else if (o.Kind == ActionKinds.Infiltrate)
            {
                DocumentDef doc = _data.HasDocument(o.TargetId) ? _data.Document(o.TargetId) : null;
                if (doc == null || _graph.Canon(doc.zone) != _graph.Canon(o.Zone))
                    return Fail(r, FailureReasons.Infeasible, "문서가 없는 자리를 털었다: " + o.Zone);
                r.DocumentStolen = true;
                if (r.ObjectiveDoneMs < 0) r.ObjectiveDoneMs = t;
                r.Log.Add(t + "ms 탈취 " + doc.id);
            }
            return null;
        }

        private void BeginMove(MemberRun m, string from, string to, int t)
        {
            m.Path = _graph.Path(from, to);
            m.PathIdx = 0;
            m.Zone = m.Path[0];
            m.Settled = false;
            m.StepEndMs = t + m.Def.moveMsPerZone;
            m.State = m.State == StReturning ? StReturning : StMoving;
        }

        private void ArriveAtOrderZone(MemberRun m, int t)
        {
            SquadOrder o = m.Queue[m.Next];
            int settleMs = o.Settle ? _graph.BestConcealmentSettleMs(o.Zone) : 0;
            if (settleMs > 0) { m.State = StSettling; m.PhaseEndMs = t + settleMs; }
            else { m.Settled = false; m.State = StReady; }
        }

        private string AliveGuardZone(string guardId, Dictionary<string, bool> guardAlive, int t)
        {
            if (guardId == null || !guardAlive.ContainsKey(guardId) || !guardAlive[guardId]) return null;
            return _graph.Canon(_patrols.ZoneAt(guardId, t));
        }

        private bool ObjectiveMet(MissionResult r)
        {
            if (_mission.downAllGuards && r.GuardsDowned.Count < _patrols.GuardIds.Count) return false;
            if (_data.HasTarget(_mission.destroyTargetId) && !r.TargetDestroyed) return false;
            if (_data.HasDocument(_mission.stealDocumentId) && !r.DocumentStolen) return false;
            return true;
        }

        private static bool AllExtracted(Dictionary<string, MemberRun> runs, List<string> order)
        {
            foreach (string id in order)
            {
                MemberRun m = runs[id];
                if (m.Queue.Count == 0) continue;
                if (m.State != StDone) return false;
            }
            return true;
        }

        private static MissionResult Fail(MissionResult r, string reason, string detail)
        {
            r.Won = false;
            r.FailureReason = reason;
            r.Log.Add("실패: " + reason + " — " + detail);
            return r;
        }
    }
}
