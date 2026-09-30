using System.Collections.Generic;
using Tactics.Data;

namespace Tactics.Sim
{
    /// <summary>설명하지 못한 사실 하나. 이게 하나라도 있으면 숨은 정보로 죽은 것이다.</summary>
    public sealed class UnexplainedFact
    {
        public int AtMs;
        public string Kind;      // Patrol · SightLine · Cover · NoisePath
        public string Detail;

        public override string ToString() { return AtMs + "ms " + Kind + ": " + Detail; }
    }

    public sealed class FairnessVerdict
    {
        public string MissionId;
        public bool Fair { get { return Unexplained.Count == 0; } }
        public int EventsChecked;
        public readonly List<string> Explanation = new List<string>();
        public readonly List<UnexplainedFact> Unexplained = new List<UnexplainedFact>();
    }

    /// <summary>
    /// **이 PoC가 증명하려는 것 하나를 기계로 판정한다:**
    /// "정찰에서 볼 수 없었던 것이 실패의 원인이 되지 않는다."
    ///
    /// 회차가 실패하면 그 실패를 만든 사건 하나하나(들켰다 · 소리를 들켰다 · 쓰러졌다)를 집어
    /// 그것이 기대는 사실을 나열한다. 사실 하나라도 ReconReport 에 없으면 숨은 정보다.
    ///
    /// 들킨 사건 하나가 기대는 사실 셋:
    ///   ① 그 시각 그 순찰병이 그 구역에 있다   (정찰로 관측된 순찰 구간인가)
    ///   ② 그 구역에서 내가 있던 구역이 보인다   (정찰로 관측된 시선인가)
    ///   ③ 내가 있던 구역의 엄폐율               (정찰로 관측된 구역인가)
    /// 소리를 들킨 사건은 ① + 소음이 온 길(인접 관계)이다.
    ///
    /// 셋 다 정찰 보고서에 있으면 플레이어는 **계획 단계에서 이 실패를 피할 수 있었다.**
    /// 그게 이 검사기가 통과했을 때의 의미다 — 재미를 보장하지는 않는다(README 판정 칸).
    /// </summary>
    public static class FairnessAudit
    {
        public static FairnessVerdict Audit(ZoneGraph graph, ReconReport recon, MissionResult result)
        {
            FairnessVerdict v = new FairnessVerdict { MissionId = result.MissionId };

            foreach (SightingEvent s in result.Sightings)
            {
                v.EventsChecked++;
                CheckPatrol(v, recon, s.GuardId, s.GuardZone, s.AtMs);
                if (!recon.KnowsSightLine(s.GuardZone, s.MemberZone))
                    v.Unexplained.Add(new UnexplainedFact
                    {
                        AtMs = s.AtMs, Kind = "SightLine",
                        Detail = s.GuardZone + " 에서 " + s.MemberZone + " 가 보인다는 것을 정찰로 알 수 없었다"
                    });
                else
                    v.Explanation.Add(s.AtMs + "ms " + s.GuardId + "(" + s.GuardZone + ") 의 시선이 "
                                      + s.MemberZone + " 를 덮는다 — 정찰에서 보였다");

                if (!recon.KnowsCover(s.MemberZone))
                    v.Unexplained.Add(new UnexplainedFact
                    {
                        AtMs = s.AtMs, Kind = "Cover",
                        Detail = s.MemberZone + " 의 엄폐율을 정찰로 알 수 없었다"
                    });
            }

            foreach (MemberLostEvent d in result.MembersLost)
            {
                v.EventsChecked++;
                CheckPatrol(v, recon, d.GuardId, d.GuardZone, d.AtMs);
                if (!recon.KnowsSightLine(d.GuardZone, d.MemberZone))
                    v.Unexplained.Add(new UnexplainedFact
                    {
                        AtMs = d.AtMs, Kind = "SightLine",
                        Detail = d.MemberId + " 를 쓰러뜨린 시선(" + d.GuardZone + "→" + d.MemberZone
                                 + ")을 정찰로 알 수 없었다"
                    });
            }

            foreach (NoiseHeardEvent n in result.NoiseHeard)
            {
                v.EventsChecked++;
                CheckPatrol(v, recon, n.GuardId, n.GuardZone, n.AtMs);
                if (!recon.PredictsAudible(graph, n.OriginZone, n.SourceLoudness,
                                           n.GuardZone, n.HearingThreshold))
                    v.Unexplained.Add(new UnexplainedFact
                    {
                        AtMs = n.AtMs, Kind = "NoisePath",
                        Detail = n.OriginZone + " 의 소리(" + n.SourceLoudness + ")가 " + n.GuardZone
                                 + " 까지 온다는 것을 정찰 지식으로는 예측할 수 없었다"
                    });
                else
                    v.Explanation.Add(n.AtMs + "ms " + n.SourceKind + " 소리가 " + n.OriginZone + " 에서 "
                                      + n.GuardZone + " 로 " + n.Hops + "홉 건너갔다 — 인접 관계로 설명된다");
            }

            return v;
        }

        private static void CheckPatrol(FairnessVerdict v, ReconReport recon,
                                        string guardId, string guardZone, int atMs)
        {
            if (recon.KnowsPatrol(guardId, guardZone, atMs))
            {
                v.Explanation.Add(atMs + "ms " + guardId + " 가 " + guardZone
                                  + " 에 있다는 것은 정찰로 관측된 순찰 구간이다");
                return;
            }
            v.Unexplained.Add(new UnexplainedFact
            {
                AtMs = atMs, Kind = "Patrol",
                Detail = guardId + " 가 " + atMs + "ms 에 " + guardZone + " 에 있는 것을 정찰로 알 수 없었다"
            });
        }

        /// <summary>
        /// 설계 단계의 정적 보장. 동적 감사(위)가 표본에 기대는 반면 이건 전수다:
        /// **순찰병이 한 번이라도 밟는 구역은 전부 정찰로 보여야 한다.**
        /// 이게 깨지면 아직 아무도 그 구역에서 죽지 않았을 뿐이다.
        /// </summary>
        public static List<string> StaticGaps(ReconReport recon, PatrolModel patrols, ZoneGraph graph)
        {
            List<string> gaps = new List<string>();

            foreach (PatrolWindow w in recon.HiddenWindows)
                gaps.Add("순찰 구간이 정찰 사각이다: " + w.GuardId + " @ " + w.Zone
                         + " [" + w.FromMs + "," + w.ToMs + ")");

            foreach (string id in recon.HiddenShiftChangeIds)
                gaps.Add("교대 시각을 정찰로 볼 수 없다: " + id);

            // 순찰병이 서는 구역에서 뻗는 시선은 전부 알아야 한다 — 그게 위험의 정의이기 때문이다.
            foreach (string gz in patrols.OccupiedZones())
            {
                foreach (string seen in graph.SeenFrom(gz))
                    if (!recon.KnowsSightLine(gz, seen))
                        gaps.Add("순찰병의 시선을 정찰로 알 수 없다: " + gz + " → " + seen);
                if (!recon.KnowsCover(gz))
                    gaps.Add("순찰병이 서는 구역의 엄폐율을 알 수 없다: " + gz);
            }

            return gaps;
        }
    }
}
