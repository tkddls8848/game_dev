using System;
using System.Collections.Generic;
using Graft.Data;

namespace Graft.Sim
{
    /// <summary>
    /// 꿈이 거부하는 이유의 이름. **balance.json 의 statedRules 와 하나씩 짝이 맞아야 한다** —
    /// 여기 없는 이름으로 떨림이 생기면 RejectionIsPredictable 이 RuleUnstated 로 잡는다.
    /// </summary>
    public static class TremorRules
    {
        public static string TimePlaceConflict { get { return "TimePlaceConflict"; } }
        public static string PersonElsewhere { get { return "PersonElsewhere"; } }
        public static string PersonAbsent { get { return "PersonAbsent"; } }
        public static string PlaceWindow { get { return "PlaceWindow"; } }
        public static string MoodClashAnchor { get { return "MoodClashAnchor"; } }
        public static string MoodClashEra { get { return "MoodClashEra"; } }
        public static string IntensityClash { get { return "IntensityClash"; } }

        public static bool IsLogic(string rule)
        {
            return rule == TimePlaceConflict || rule == PersonElsewhere
                || rule == PersonAbsent || rule == PlaceWindow;
        }

        public static bool IsFeeling(string rule)
        {
            return rule == MoodClashAnchor || rule == MoodClashEra || rule == IntensityClash;
        }
    }

    /// <summary>플레이어가 고르는 것 전부. 무엇을 심을지는 의뢰인이 정했고, 이 다섯은 플레이어 몫이다.</summary>
    public sealed class GraftChoice
    {
        public string AnchorMemoryId = "";
        public string PlaceId = "";
        public int DayIndex = -1;
        public string MoodId = "";
        public int IntensityPercent = -1;

        public override string ToString()
        {
            return AnchorMemoryId + " / " + PlaceId + " / " + DayIndex + "일 / " + MoodId + " / " + IntensityPercent + "%";
        }
    }

    /// <summary>어긋난 간선 하나. 목업이 붉게 떨리게 그리는 것이 정확히 이것이다.</summary>
    public sealed class Tremor
    {
        public string Rule = "";
        /// <summary>떨리는 간선의 저쪽 끝. 사람·장소 창 규칙이면 "" 이다(간선이 아니라 의뢰서가 근거다).</summary>
        public string ToMemoryId = "";
        /// <summary>사람·장소 규칙이 가리키는 id.</summary>
        public string RefId = "";
        public int Strength;
        public string Detail = "";

        public override string ToString() { return Rule + "(" + Strength + ") " + Detail; }
    }

    public sealed class GraftVerdict
    {
        public string CommissionId = "";
        public GraftChoice Choice;
        public bool Accepted;
        public int TotalTremor;
        public int Worst;
        public string WorstRule = "";
        /// <summary>한 간선이 상한을 넘었다. 총합과 무관하게 거부된다 — 눈에 보이는 모순이다.</summary>
        public bool Blatant;
        public readonly List<Tremor> Tremors = new List<Tremor>();
    }

    /// <summary>
    /// ★ **적힌 규칙 전부가 여기 있다.** 꿈이 거부하는 이유는 이 일곱 가지뿐이고,
    /// 떨림의 세기는 언제나 `저울 x 크기` 형태의 **정수 곱**이다 — 나눗셈이 한 번도 없다(설계 원칙 4).
    ///
    /// 논리(몸은 두 곳에 없다 · 없는 사람 · 없는 장소)와 정서(이어 붙인 자리 · 그 시절 · 세기)를
    /// 이름으로 갈라 둔 이유는 하나다. 목업이 붉은 떨림 옆에 **왜** 를 적을 수 있어야 한다.
    ///
    /// 같은 함수를 꿈(TruthView)과 플레이어(ProbeKnowledge)가 각자의 눈으로 돌린다.
    /// 예측이 어긋났다면 그것은 규칙이 아니라 **눈**이 다른 것이고, 감사기가 그 차이에 이름을 붙인다.
    /// </summary>
    public static class GraftRules
    {
        /// <summary>
        /// 이 자리에 이렇게 심으면 꿈이 어떻게 떨리는가.
        /// rulesEnabled 가 false 면 떨림이 하나도 안 생긴다 — ConsistencyIsNotTrivial 의 **끈 세계**다.
        /// </summary>
        public static GraftVerdict Evaluate(GameData data, MemoryNet net, MoodModel moods,
                                            CommissionDef com, GraftChoice choice,
                                            IMemoryView view, bool rulesEnabled)
        {
            BalanceFile b = data.Balance;
            GraftVerdict v = new GraftVerdict { CommissionId = com.id, Choice = choice };
            if (!rulesEnabled)
            {
                v.Accepted = true;
                return v;
            }

            int gStart = choice.DayIndex;
            int gEnd = choice.DayIndex + com.spanDays - 1;

            // ── 논리 ① 장소가 그 날 서 있지 않았다 ────────────────────────────────
            if (!data.PlaceExists(choice.PlaceId, gStart) || !data.PlaceExists(choice.PlaceId, gEnd))
                Add(v, TremorRules.PlaceWindow, "", choice.PlaceId,
                    b.placeWindowWeight * 100,
                    Localization.Text("reject.placeWindow", "{0} 은 그 날 그 자리에 없었다",
                                      data.Place(choice.PlaceId).name));

            // ── 논리 ② 의뢰인이 넣으라 한 사람이 그 날 없었다 ──────────────────────
            foreach (string pid in com.requiredPeopleIds)
            {
                if (data.PersonPresent(pid, gStart) && data.PersonPresent(pid, gEnd)) continue;
                Add(v, TremorRules.PersonAbsent, "", pid,
                    b.personAbsentWeight * 100,
                    Localization.Text("reject.personGone", "{0} 는 그 날 거기 있을 수 없다", data.Person(pid).name));
            }

            // ── 기억 하나하나와 겹쳐 본다 ─────────────────────────────────────────
            foreach (string mid in view.KnownIds)
            {
                if (mid == choice.AnchorMemoryId) { }   // 자리 규칙은 아래에서 따로 본다

                int mStart = view.Day(mid);
                int mEnd = mStart + view.Span(mid) - 1;
                bool overlap = mStart <= gEnd && gStart <= mEnd;
                string mPlace = view.Place(mid);
                int fix = view.Fixity(mid);
                string mName = net.Memory(mid).name;

                // 논리 ③ 몸이 두 곳에 있을 수 없다
                if (overlap && mPlace != choice.PlaceId)
                {
                    Add(v, TremorRules.TimePlaceConflict, mid, "",
                        b.timePlaceWeight * fix,
                        Localization.Text("reject.timePlace", "몸이 두 곳에 있을 수 없다: {0}", mName));

                    // 논리 ④ 그 사람도 그 날 다른 곳에 있었다
                    foreach (string pid in com.requiredPeopleIds)
                    {
                        if (!Contains(view.People(mid), pid)) continue;
                        Add(v, TremorRules.PersonElsewhere, mid, pid,
                            b.personElsewhereWeight * fix,
                            Localization.Text("reject.personElse", "{0} 는 그 날 다른 곳에 있었다: {1}",
                                              data.Person(pid).name, mName));
                    }
                }

                // 정서 ② 그 시절 전체와 어긋난다 (이어 붙인 자리는 아래에서 더 무겁게 본다)
                if (mid == choice.AnchorMemoryId) continue;
                if (Math.Abs(mStart - choice.DayIndex) > b.eraWindowDays) continue;
                int eraDist = moods.Distance(choice.MoodId, view.Mood(mid));
                if (eraDist <= b.eraTolerance) continue;
                Add(v, TremorRules.MoodClashEra, mid, "",
                    b.eraWeight * (eraDist - b.eraTolerance) * fix,
                    Localization.Text("reject.era", "그 무렵에 이런 것은 하나도 없었다: {0}", mName));
            }

            // ── 정서 ① 이어 붙인 자리가 다르게 느껴진다 ───────────────────────────
            if (view.Knows(choice.AnchorMemoryId))
            {
                int aFix = view.Fixity(choice.AnchorMemoryId);
                string aName = net.Memory(choice.AnchorMemoryId).name;
                int dist = moods.Distance(choice.MoodId, view.Mood(choice.AnchorMemoryId));
                if (dist > b.moodTolerance)
                    Add(v, TremorRules.MoodClashAnchor, choice.AnchorMemoryId, "",
                        b.moodAnchorWeight * (dist - b.moodTolerance) * aFix,
                        Localization.Text("reject.moodAnchor", "이어 붙인 자리가 {0} 와 다르게 느껴진다", aName));

                // ── 정서 ③ 세기가 맞지 않는다 ────────────────────────────────────
                int diff = Math.Abs(choice.IntensityPercent - view.Intensity(choice.AnchorMemoryId));
                if (diff > b.intensityTolerance)
                    Add(v, TremorRules.IntensityClash, choice.AnchorMemoryId, "",
                        b.intensityWeight * (diff - b.intensityTolerance) * aFix,
                        Localization.Text("reject.intensity", "이어 붙인 자리에 비해 너무 크다: {0}", aName));
            }

            foreach (Tremor t in v.Tremors)
            {
                v.TotalTremor += t.Strength;
                if (t.Strength > v.Worst) { v.Worst = t.Strength; v.WorstRule = t.Rule; }
            }
            v.Blatant = v.Worst > com.singleEdgeCap;
            v.Accepted = !v.Blatant && v.TotalTremor <= com.toleranceBudget;
            return v;
        }

        /// <summary>의뢰가 허락한 선택 전부. 여기 없는 선택은 애초에 고를 수 없다.</summary>
        public static List<GraftChoice> LegalChoices(CommissionDef com, IList<string> anchorIds, int dayStep)
        {
            List<GraftChoice> all = new List<GraftChoice>();
            foreach (string anchor in anchorIds)
                foreach (string place in com.allowedPlaceIds)
                    for (int day = com.dayWindowStart; day <= com.dayWindowEnd; day += dayStep)
                        foreach (string mood in com.allowedMoodIds)
                            for (int inten = com.intensityMin; inten <= com.intensityMax; inten += com.intensityStep)
                                all.Add(new GraftChoice
                                {
                                    AnchorMemoryId = anchor, PlaceId = place, DayIndex = day,
                                    MoodId = mood, IntensityPercent = inten
                                });
            return all;
        }

        private static void Add(GraftVerdict v, string rule, string toMemoryId, string refId, int strength, string detail)
        {
            v.Tremors.Add(new Tremor
            {
                Rule = rule, ToMemoryId = toMemoryId, RefId = refId, Strength = strength, Detail = detail
            });
        }

        private static bool Contains(IList<string> list, string s)
        {
            if (list == null) return false;
            for (int i = 0; i < list.Count; i++) if (list[i] == s) return true;
            return false;
        }
    }
}
