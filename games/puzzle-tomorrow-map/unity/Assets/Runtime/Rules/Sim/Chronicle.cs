// 순수 C#만. using UnityEngine 금지 — 이 파일이 Unity 를 끌어들이면 헤드리스 검사가 컴파일 오류로 터진다.
using System;
using System.Collections.Generic;
using FarmErosion.Data;

namespace FarmErosion.Sim
{
    /// <summary>
    /// **끝난 뒤에 남는 것.** DIRECTION.md §2-7 을 문장으로 굽는 자리다.
    ///
    /// 땅은 전부 사라진다. 그런데 **남에게 준 것은 일어난 일이라 침식이 가져갈 수 없다.**
    /// 그래서 끝에 남는 것은 그것뿐이고, 이 클래스가 만드는 것은 그 목록이다.
    ///
    /// 문장을 여기서 굽는 이유가 있다 — 화면(Unity)에서 조립하면 헤드리스로 읽어 볼 수가 없다.
    /// 이 연출은 **한 번 읽어 보면 느끼한지 아닌지 바로 아는** 종류라(§2-7 '위험'),
    /// 검사기가 실제 문장을 눈으로 볼 수 있어야 한다.
    ///
    /// 규칙 넷은 검사기가 지킨다(EndingTests):
    ///   1. **평가하지 않는다** — 점수·등급·"훌륭했습니다"가 없다
    ///   2. **주어를 바꾼다** — "내가 주었다"가 아니라 "그는 12일을 더 버텼다"
    ///   3. **사실이 질문보다 앞이다** — 질문만 뜨면 상담이 된다
    ///   4. **질문은 드물다** — 기록 전부에 붙이면 설문이 된다
    /// </summary>
    public sealed class Memory
    {
        public int Day;
        public string RegionId;        // null 이면 사람이 아니라 내 땅에 대한 기록
        public string FactKo;          // "3년차 가을, 당신은 여울목의 노인에게 목재를 주었습니다."
        public string EffectKo;        // "그는 12일을 더 버텼습니다."  — 주어가 그 사람이다
        public string AfterKo;         // "그 사이 그의 밭은 18칸에서 3칸이 되었습니다."
        public string QuestionKo;      // "왜 그랬나요?"  — 붙는 기록은 하나뿐이다
        public int CostCoin;           // 내가 치른 값(환산). 어디에 질문을 붙일지 고르는 데만 쓴다

        /// <summary>화면이 한 덩어리로 그릴 때 쓰는 본문. 질문은 뒤에 따로 둔다.</summary>
        public string BodyKo
        {
            get
            {
                string s = FactKo;
                if (!string.IsNullOrEmpty(EffectKo)) s += "\n" + EffectKo;
                if (!string.IsNullOrEmpty(AfterKo)) s += "\n" + AfterKo;
                return s;
            }
        }
    }

    public static class Chronicle
    {
        /// <summary>맨 마지막에 딱 한 번. 남발하면 죽는다(§2-7).</summary>
        public const string ClosingQuestionKo = "무엇을 느꼈나요?";
        public const string ClosingHintKo = "한 줄을 남길 수 있습니다 · 비워 두어도 됩니다";
        const string WhyKo = "왜 그랬나요?";

        /// <summary>내가 내놓은 것의 이름. 받은 것이 아니라 **준 것**만 부른다.</summary>
        static string GaveNameKo(string kind) => kind == Res.Wall ? "목재"
                                               : kind == Res.Soil ? "흙"
                                               : kind == Res.Seed ? "씨앗"
                                               : kind == Res.Coin ? "셈" : null;

        /// <summary>"6년차 겨울" 꼴. 다른 사람의 엔딩에 올라갈 때도 이 맥락 하나만 붙는다.</summary>
        public static string WhenKo(GameData d, int day)
        {
            int clamped = Math.Max(0, day);
            return d.YearOfDay(clamped) + "년차 " + d.SeasonOfDay(clamped).nameKo;
        }

        /// <summary>"6년차 겨울까지". 순위표가 되지 않도록 여기에 날짜·점수를 더하지 않는다(§2-7).</summary>
        public static string SurvivedKo(GameData d, Simulation sim)
            => WhenKo(d, Math.Max(0, sim.CurrentDay - 1)) + "까지";

        /// <summary>
        /// 기록을 시간 순으로 굽는다. **사람별로 한 줄씩**, 그리고 마지막에 내 땅 한 줄.
        ///
        /// 여러 번 도운 사람은 **가장 크게 치른 한 번**만 적는다 — 같은 사람이 네 번 나오면
        /// 목록이 되고, 목록이 되면 총계 화면이 된다(§2-7 '평가가 아니라 기록이다').
        /// </summary>
        public static List<Memory> Build(GameData d, Simulation sim, Travel travel)
        {
            var best = new Dictionary<string, Memory>();
            var order = new List<string>();

            if (travel != null)
            {
                foreach (var v in travel.Log)
                {
                    if (!v.Ok || v.ErrandId == null) continue;      // 의뢰 없이 들른 것은 준 것이 없다
                    var def = d.Region(v.RegionId);
                    if (def == null) continue;

                    int cost = v.GaveDays > 0
                        ? v.GaveDays * d.Travel.limits.coinPerHandDay
                        : travel.ValueOf(new[] { v.GaveKind }, new[] { v.GaveAmount });

                    string what = v.GaveDays > 0
                        ? "며칠 손을 보탰습니다"
                        : GaveNameKo(v.GaveKind) is string name ? name + "를 주었습니다" : null;
                    if (what == null) continue;

                    var m = new Memory
                    {
                        Day = v.ArriveDay,
                        RegionId = v.RegionId,
                        CostCoin = cost,
                        FactKo = WhenKo(d, v.ArriveDay) + ", 당신은 " + def.personKo + "에게 " + what + ".",
                        // **주어가 그 사람이다.** 내 업적이 아니라 그에게 일어난 일로 적는다(§2-7).
                        EffectKo = v.ReprieveGiven > 0 ? HeKo(def) + " " + v.ReprieveGiven + "일을 더 버텼습니다." : null,
                    };

                    if (!best.TryGetValue(v.RegionId, out var kept)) { best[v.RegionId] = m; order.Add(v.RegionId); }
                    // **실제로 무언가 일어난 쪽을 먼저 남긴다.** 유예에 상한이 있어서 거듭 찾아가면
                    // 치르기만 하고 그의 시계는 안 밀리는 방문이 생긴다. 그런 것을 대표로 적으면
                    // 기록이 "주었다"에서 끝나 버린다.
                    else if (Better(m, kept)) best[v.RegionId] = m;
                }
            }

            var list = new List<Memory>();
            foreach (var id in order) list.Add(best[id]);
            list.Sort((a, b) => a.Day != b.Day ? a.Day.CompareTo(b.Day) : string.CompareOrdinal(a.RegionId, b.RegionId));

            // **결말은 같다.** 도운 사람의 밭도 줄었다는 것을 같은 톤으로 덧붙인다.
            // 이 두 줄이 나란히 있을 때가 이 게임이 가장 정확해지는 순간이다(§2-7).
            if (travel != null)
                foreach (var m in list)
                {
                    var def = d.Region(m.RegionId);
                    var state = travel.Region(m.RegionId);
                    if (def == null || state == null || state.PlotsLeft >= def.startPlots) continue;
                    m.AfterKo = "그 사이 " + His(def) + " 밭은 " + def.startPlots + "칸에서 " + state.PlotsLeft + "칸이 되었습니다.";
                }

            // **질문은 하나뿐이다.** 가장 크게 치른 자리에만 붙인다 — 전부 물으면 설문이 된다(§2-7).
            Memory costliest = null;
            foreach (var m in list) if (costliest == null || m.CostCoin > costliest.CostCoin) costliest = m;
            if (costliest != null) costliest.QuestionKo = WhyKo;

            // 마지막은 언제나 내 땅이다. 평가하지 않고 사실만 적는다(§2-4 '쓸려가버렸다').
            int last = Math.Max(0, sim.CurrentDay - 1);
            list.Add(new Memory
            {
                Day = last,
                FactKo = WhenKo(d, last) + ", 마지막 한 칸이 사라졌습니다.",
            });
            return list;
        }

        /// <summary>대표로 남길 방문 고르기. 그의 시계가 실제로 밀린 쪽이 먼저고, 그 다음이 내가 치른 값이다.</summary>
        static bool Better(Memory candidate, Memory kept)
        {
            bool a = candidate.EffectKo != null, b = kept.EffectKo != null;
            if (a != b) return a;
            return candidate.CostCoin > kept.CostCoin;
        }

        // 사람이 하나인지 여럿인지에 따라 대명사가 달라진다. 데이터에 성별을 넣지 않으려고
        // 이름 끝으로 고른다 — '부부'·'아이들'은 '그들', 나머지는 '그'.
        //
        // **조사를 붙여서 돌려준다.** '그'+'는' 과 '그들'+'는' 을 같은 자리에서 이으면
        // "그들는" 이 나온다. 조각을 이어 붙이지 말라는 규약(설계 원칙 6)이 한국어에서
        // 걸리는 지점이 정확히 이것이다.
        static bool Plural(RegionDef def)
            => def.personKo != null && (def.personKo.EndsWith("부부") || def.personKo.EndsWith("아이들") || def.personKo.EndsWith("들"));
        static string HeKo(RegionDef def) => Plural(def) ? "그들은" : "그는";
        static string His(RegionDef def) => Plural(def) ? "그들의" : "그의";
    }
}
