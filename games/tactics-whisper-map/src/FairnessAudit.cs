using System.Collections.Generic;
using Whisper.Data;

namespace Whisper.Sim
{
    /// <summary>실패를 만든 사실 하나를 어떻게 분류했는가.</summary>
    public static class FactVerdicts
    {
        /// <summary>쥐고 있었고 참이었다. 계획 단계에서 피할 수 있었다.</summary>
        public static string Held { get { return "Held"; } }
        /// <summary>쥐고 있지 않았지만 **물으면 살 수 있었다.** 선택의 결과다 — 공정하다.</summary>
        public static string Obtainable { get { return "Obtainable"; } }
        /// <summary>틀린 것을 쥐었다. 다만 **겹쳐 물으면 드러났다** — 공정하다.</summary>
        public static string FalseButCheckable { get { return "FalseButCheckable"; } }
        /// <summary>틀린 것을 쥐었고 **확인할 방법이 없었다.** 불공정하다.</summary>
        public static string FalseAndUncheckable { get { return "FalseAndUncheckable"; } }
        /// <summary>어떤 값을 치러도 알 수 없었다. 불공정하다 — 이것이 숨은 정보다.</summary>
        public static string Unknowable { get { return "Unknowable"; } }
        /// <summary>지형 사실을 몰랐다(대조군만 만든다). 불공정하다.</summary>
        public static string TerrainUnknown { get { return "TerrainUnknown"; } }
        /// <summary>참만 쥐었는데 머릿속 지도가 어긋났다 — 모델 버그다. 감사기의 안전망.</summary>
        public static string ModelMismatch { get { return "ModelMismatch"; } }

        public static bool IsFair(string verdict)
        {
            return verdict == Held || verdict == Obtainable || verdict == FalseButCheckable;
        }
    }

    public sealed class FactCheck
    {
        public int AtMs;
        public string Kind;        // Patrol · SightLine · Cover · NoisePath
        public string Verdict;
        public string Detail;
        public string ItemId = "";

        public bool Fair { get { return FactVerdicts.IsFair(Verdict); } }
        public override string ToString() { return AtMs + "ms " + Kind + "/" + Verdict + ": " + Detail; }
    }

    public sealed class FairnessVerdict
    {
        public string MissionId;
        public int EventsChecked;
        public readonly List<FactCheck> Facts = new List<FactCheck>();

        public bool Fair
        {
            get { foreach (FactCheck f in Facts) if (!f.Fair) return false; return true; }
        }

        public List<FactCheck> Unfair()
        {
            List<FactCheck> bad = new List<FactCheck>();
            foreach (FactCheck f in Facts) if (!f.Fair) bad.Add(f);
            return bad;
        }

        public int Count(string verdict)
        {
            int n = 0;
            foreach (FactCheck f in Facts) if (f.Verdict == verdict) n++;
            return n;
        }
    }

    /// <summary>
    /// ★ **이 PoC가 증명하려는 것을 기계로 판정한다** — 그리고 먼저 만든 PoC보다 한 겹 어렵다.
    ///
    /// 먼저 만든 PoC의 규칙은 "정찰에서 **볼 수 없었던** 것이 실패의 원인이 되지 않는다"였다.
    /// 여기서 정찰은 눈이 아니라 **입**이다. 그래서 규칙이 셋으로 갈라진다:
    ///
    ///   · 쥐고 있던 참으로 죽었다        → 공정 (피할 수 있었다)
    ///   · **안 산 것**으로 죽었다        → 공정 (물을 수 있었다. 안 묻기로 한 것은 선택이다)
    ///   · **틀린 것**으로 죽었다         → **겹쳐 물을 수 있었다면 공정**, 아니면 불공정
    ///   · 아무에게 물어도 알 수 없었다   → 불공정. 이것이 숨은 정보다
    ///
    /// **세 번째 줄이 이 검사기의 어려운 부분이다.** "틀린 정보로 죽는 것"과
    /// "알 길이 없어 죽는 것"을 갈라야 하고, 그 경계는 `Whispers` 의 불변식
    /// (한 항목의 거짓말쟁이는 최대 한 명 · 거짓은 출처가 둘 이상일 때만 섞인다)이 세운다.
    /// 그 불변식이 깨지면 이 검사기가 거짓을 말하게 되므로 `FalseIntelIsCheckable` 이 따로 지킨다.
    /// </summary>
    public static class FairnessAudit
    {
        public static FairnessVerdict Audit(MissionSim sim, IntelKnowledge knowledge,
                                           Whispers whispers, MissionResult result)
        {
            FairnessVerdict v = new FairnessVerdict { MissionId = result.MissionId };

            foreach (SightingEvent s in result.Sightings)
            {
                v.EventsChecked++;
                CheckPatrol(v, sim, knowledge, whispers, s.GuardId, s.GuardZone, s.AtMs);
                CheckSightLine(v, knowledge, s.AtMs, s.GuardZone, s.MemberZone, s.MemberId);
                CheckCover(v, knowledge, s.AtMs, s.MemberZone);
            }

            foreach (MemberLostEvent d in result.MembersLost)
            {
                v.EventsChecked++;
                CheckPatrol(v, sim, knowledge, whispers, d.GuardId, d.GuardZone, d.AtMs);
                CheckSightLine(v, knowledge, d.AtMs, d.GuardZone, d.MemberZone, d.MemberId);
            }

            foreach (NoiseHeardEvent n in result.NoiseHeard)
            {
                v.EventsChecked++;
                CheckPatrol(v, sim, knowledge, whispers, n.GuardId, n.GuardZone, n.AtMs);
                if (!knowledge.PredictsAudible(sim.Graph, n.OriginZone, n.SourceLoudness,
                                               n.GuardZone, n.HearingThreshold))
                    v.Facts.Add(new FactCheck
                    {
                        AtMs = n.AtMs, Kind = "NoisePath", Verdict = FactVerdicts.TerrainUnknown,
                        Detail = n.OriginZone + " 의 소리(" + n.SourceLoudness + ")가 " + n.GuardZone
                                 + " 까지 온다는 것을 지형 지식으로는 예측할 수 없었다"
                    });
                else
                    v.Facts.Add(new FactCheck
                    {
                        AtMs = n.AtMs, Kind = "NoisePath", Verdict = FactVerdicts.Held,
                        Detail = n.SourceKind + " 이 " + n.OriginZone + " 에서 " + n.GuardZone + " 로 "
                                 + n.Hops + "홉 건너갔다 — 인접 관계로 설명된다"
                    });
            }

            return v;
        }

        private static void CheckSightLine(FairnessVerdict v, IntelKnowledge knowledge,
                                           int atMs, string guardZone, string memberZone, string memberId)
        {
            if (guardZone == memberZone) return;   // 같은 구역이면 시선이 필요 없다
            if (knowledge.KnowsSightLine(guardZone, memberZone))
                v.Facts.Add(new FactCheck
                {
                    AtMs = atMs, Kind = "SightLine", Verdict = FactVerdicts.Held,
                    Detail = guardZone + " 에서 " + memberZone + " 가 보인다 — 지형이다"
                });
            else
                v.Facts.Add(new FactCheck
                {
                    AtMs = atMs, Kind = "SightLine", Verdict = FactVerdicts.TerrainUnknown,
                    Detail = guardZone + " 에서 " + memberZone + " 가 보인다는 것을 알 수 없었다 (" + memberId + ")"
                });
        }

        private static void CheckCover(FairnessVerdict v, IntelKnowledge knowledge, int atMs, string zone)
        {
            if (knowledge.KnowsCover(zone)) return;
            v.Facts.Add(new FactCheck
            {
                AtMs = atMs, Kind = "Cover", Verdict = FactVerdicts.TerrainUnknown,
                Detail = zone + " 의 엄폐율을 알 수 없었다"
            });
        }

        /// <summary>
        /// "그 시각 그 순찰병이 그 구역에 있다" 하나를 분류한다. **이 메서드가 이 검사기의 핵이다.**
        /// </summary>
        private static void CheckPatrol(FairnessVerdict v, MissionSim sim, IntelKnowledge knowledge,
                                        Whispers whispers, string guardId, string guardZone, int atMs)
        {
            GameData d = sim.Data;
            IntelItemDef route = d.ItemFor(IntelKinds.Route, guardId);
            IntelItemDef post = d.ItemFor(IntelKinds.Post, guardId);

            if (route == null || post == null)
            {
                Add(v, atMs, FactVerdicts.Unknowable, "",
                    guardId + " 의 " + (route == null ? "순찰 경로" : "오늘의 배치") + "를 아는 사람이 마을에 없다");
                return;
            }

            if (!knowledge.CanPredict(guardId))
            {
                bool buyable = whispers.SourceCount(route.id) > 0 && whispers.SourceCount(post.id) > 0;
                if (buyable)
                    Add(v, atMs, FactVerdicts.Obtainable, route.id,
                        guardId + " 가 " + atMs + "ms 에 " + guardZone + " 에 있는 것은 물으면 알 수 있었다");
                else
                    Add(v, atMs, FactVerdicts.Unknowable, route.id,
                        guardId + " 를 아는 사람이 이번 회차에 아무도 입을 열지 않았다");
                return;
            }

            string believed = knowledge.BelievedZoneAt(guardId, atMs);
            if (believed == guardZone)
            {
                Add(v, atMs, FactVerdicts.Held, route.id,
                    guardId + " 가 " + atMs + "ms 에 " + guardZone + " 에 있다는 것을 알고 있었다");
                return;
            }

            // 머릿속 지도가 어긋났다. 무엇이 어긋나게 했는가.
            List<IntelItemDef> falseHeld = new List<IntelItemDef>();
            bool missingShift = false;
            if (knowledge.HoldsFalse(route.id)) falseHeld.Add(route);
            if (knowledge.HoldsFalse(post.id)) falseHeld.Add(post);
            foreach (ShiftChangeDef sc in d.ShiftChangesOf(sim.Mission))
            {
                if (sc.guardId != guardId) continue;
                IntelItemDef shiftItem = d.ItemFor(IntelKinds.Shift, sc.id);
                if (shiftItem == null)
                {
                    Add(v, atMs, FactVerdicts.Unknowable, "",
                        sc.id + " 의 교대 시각을 아는 사람이 마을에 없다");
                    return;
                }
                if (knowledge.HoldsFalse(shiftItem.id)) falseHeld.Add(shiftItem);
                else if (!knowledge.Holds(shiftItem.id)) missingShift = true;
            }

            if (falseHeld.Count > 0)
            {
                // **여기가 갈림길이다.** 겹쳐 물어 확인할 수 있었던 거짓인가?
                foreach (IntelItemDef bad in falseHeld)
                {
                    if (whispers.SourceCount(bad.id) >= 2)
                        Add(v, atMs, FactVerdicts.FalseButCheckable, bad.id,
                            bad.id + " 에 대해 틀린 답을 쥐었다 — 출처가 " + whispers.SourceCount(bad.id)
                            + " 사람이니 겹쳐 물으면 드러났다 (" + guardId + " 는 " + atMs + "ms 에 실제로 "
                            + guardZone + ", 믿은 곳은 " + believed + ")");
                    else
                        Add(v, atMs, FactVerdicts.FalseAndUncheckable, bad.id,
                            bad.id + " 에 대해 틀린 답을 쥐었는데 출처가 " + whispers.SourceCount(bad.id)
                            + " 사람뿐이어서 확인할 방법이 없었다");
                }
                return;
            }

            if (missingShift)
            {
                Add(v, atMs, FactVerdicts.Obtainable, "",
                    "교대 시각을 사지 않아 " + atMs + "ms 뒤의 구간이 어긋났다 (실제 " + guardZone
                    + ", 믿은 곳 " + believed + ")");
                return;
            }

            Add(v, atMs, FactVerdicts.ModelMismatch, "",
                "참만 쥐었는데 머릿속 지도가 어긋났다: " + guardId + " 실제 " + guardZone + " · 믿은 곳 " + believed);
        }

        private static void Add(FairnessVerdict v, int atMs, string verdict, string itemId, string detail)
        {
            v.Facts.Add(new FactCheck
            {
                AtMs = atMs, Kind = "Patrol", Verdict = verdict, ItemId = itemId, Detail = detail
            });
        }

        /// <summary>
        /// **설계 단계의 정적 보장.** 동적 감사는 표본에 기대지만 이건 전수다.
        ///
        ///   ① 순찰병마다 경로·배치를 아는 사람이 있는가
        ///   ② 교대마다 그 시각을 아는 사람이 있는가
        ///   ③ 틀릴 수 있는 항목은 **출처가 둘 이상**인가 (겹쳐 물을 수 있는가)
        ///   ④ 순찰병이 밟는 구역의 지형(엄폐·시선)을 아는가
        ///
        /// ③이 깨지면 아직 아무도 그 거짓으로 죽지 않았을 뿐이다.
        /// </summary>
        public static List<string> StaticGaps(MissionSim sim, IntelKnowledge knowledge, Whispers whispers)
        {
            GameData d = sim.Data;
            List<string> gaps = new List<string>();

            foreach (string guardId in sim.Patrols.GuardIds)
            {
                IntelItemDef route = d.ItemFor(IntelKinds.Route, guardId);
                IntelItemDef post = d.ItemFor(IntelKinds.Post, guardId);
                if (route == null) { gaps.Add("순찰 경로를 아는 사람이 없다: " + guardId); continue; }
                if (post == null) { gaps.Add("오늘의 배치를 아는 사람이 없다: " + guardId); continue; }
                CheckSources(d, whispers, route, gaps);
                CheckSources(d, whispers, post, gaps);
            }

            foreach (ShiftChangeDef sc in d.ShiftChangesOf(sim.Mission))
            {
                IntelItemDef shiftItem = d.ItemFor(IntelKinds.Shift, sc.id);
                if (shiftItem == null) { gaps.Add("교대 시각을 아는 사람이 없다: " + sc.id); continue; }
                CheckSources(d, whispers, shiftItem, gaps);
            }

            foreach (string gz in sim.Patrols.OccupiedZones())
            {
                if (!knowledge.KnowsCover(gz)) gaps.Add("순찰병이 서는 구역의 엄폐율을 모른다: " + gz);
                foreach (string seen in sim.Graph.SeenFrom(gz))
                    if (!knowledge.KnowsSightLine(gz, seen))
                        gaps.Add("순찰병의 시선을 모른다: " + gz + " → " + seen);
            }

            return gaps;
        }

        private static void CheckSources(GameData d, Whispers whispers, IntelItemDef item, List<string> gaps)
        {
            int declared = d.SourcesOf(item.id).Count;
            if (declared < 1) gaps.Add("아는 사람이 없는 항목: " + item.id);
            if (item.falsifiable && declared < 2)
                gaps.Add("틀릴 수 있는데 출처가 하나뿐이다 — 겹쳐 물을 수 없다: " + item.id);
            if (whispers != null && whispers.SourceCount(item.id) < 1)
                gaps.Add("이번 회차에 이 항목을 말해 줄 사람이 없다: " + item.id);
        }
    }
}
