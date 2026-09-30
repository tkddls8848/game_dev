using System.Collections.Generic;
using RedPen.Data;

namespace RedPen.Sim
{
    /// <summary>
    /// 편집자의 방침들. **정책이지 게임 데이터가 아니다** — 문턱값이 여기 있는 이유다.
    /// 「혹독함은 양날이다」가 이것들을 같은 씨드로 돌려 견준다.
    /// </summary>
    public static class Policies
    {
        /// <summary>이 부호가 이 문장의 흠을 하나라도 고치는가. 헛줄을 긋지 않으려면 이걸 봐야 한다.</summary>
        public static bool Helps(GameData d, string markId, LiveSentence s)
        {
            MarkDef m = d.Mark(markId);
            return m != null && ManuscriptSim.Helps(d, m, s);
        }

        /// <summary>한 부호만 되풀이한다. 지배 전략이 없는지 보려면 이런 것들이 필요하다.</summary>
        public sealed class Single : IPolicy
        {
            private readonly string _mark;
            public Single(string markId) { _mark = markId; }
            public string Id { get { return "p_" + _mark.Substring(2); } }
            public string Name { get { return _mark + " 만 되풀이한다"; } }
            public MarkDef Pick(GameData d, LiveSentence s, AuthorState a, int round) { return d.Mark(_mark); }
        }

        /// <summary>
        /// 늘 혹독하게. 흠이 있으면 가장 거친 부호로 친다. 흠이 없어도 손을 댄다.
        /// </summary>
        public sealed class Harsh : IPolicy
        {
            public string Id { get { return "p_harsh"; } }
            public string Name { get { return Localization.Text("policy.harsh", "늘 혹독하게"); } }

            public MarkDef Pick(GameData d, LiveSentence s, AuthorState a, int round)
            {
                // 흠이 없는 문장에는 **아무 말도 하지 않는다.** 혹독한 편집자는 칭찬하지 않는다 —
                // 헛줄을 긋는 서투름과 구별해야 이 정책이 "혹독함"을 재는 자가 된다.
                if (s.Flaws.Count == 0) return d.Mark("m_leave");
                if (s.Flaws.Count >= 3) return d.Mark("m_delete");
                if (Helps(d, "m_insert", s)) return d.Mark("m_insert");
                if (Helps(d, "m_cut", s)) return d.Mark("m_cut");
                if (Helps(d, "m_move", s)) return d.Mark("m_move");
                return d.Mark("m_delete");
            }
        }

        /// <summary>
        /// 늘 관대하게. 물어보거나 칭찬한다. 절대 도려내지 않는다.
        /// </summary>
        public sealed class Gentle : IPolicy
        {
            public string Id { get { return "p_gentle"; } }
            public string Name { get { return Localization.Text("policy.gentle", "늘 관대하게"); } }

            public MarkDef Pick(GameData d, LiveSentence s, AuthorState a, int round)
            {
                // 물어봐야 소용없는 흠(군더더기·자리)에는 묻지 않는다. 헛줄이 아니라 칭찬을 남긴다.
                if (s.Flaws.Count > 0 && Helps(d, "m_query", s)) return d.Mark("m_query");
                return d.Mark("m_praise");
            }
        }

        /// <summary>
        /// ★ 작가를 읽는다. 상태를 보고 부호를 고른다 —
        /// 신뢰가 없으면 먼저 칭찬으로 벌고, 고집이 서면 물어보고,
        /// 자리만 틀렸으면 화살표 하나로 끝낸다.
        /// </summary>
        public sealed class Middle : IPolicy
        {
            public const int NeedTrust = 46;      // 이 밑이면 먼저 신뢰를 번다
            public const int StubbornWall = 58;   // 이 위면 거친 부호가 튕겨 나온다
            public const int FragileConfidence = 34;
            public const int SelfWriteConfidence = 74;  // 이 위여야 내가 대신 써 넣는다. 목소리를 가장 많이 가져가는 부호다

            public string Id { get { return "p_middle"; } }
            public string Name { get { return Localization.Text("policy.middle", "작가를 읽는다"); } }

            public MarkDef Pick(GameData d, LiveSentence s, AuthorState a, int round)
            {
                if (s.Flaws.Count == 0) return d.Mark("m_praise");
                if (a.Trust < NeedTrust) return d.Mark("m_praise");
                if (a.Confidence <= FragileConfidence) return d.Mark("m_praise");
                if (a.Stubborn > StubbornWall) return d.Mark("m_query");

                bool order = s.Flaws.Contains("f_order");
                bool bloat = s.Flaws.Contains("f_bloat");
                if (order && s.Flaws.Count == 1) return d.Mark("m_move");
                if (s.Flaws.Count >= 3 && s.PrideGuard < 40) return d.Mark("m_delete");
                if (bloat && s.Flaws.Count <= 2) return d.Mark("m_cut");
                if (Helps(d, "m_query", s)) return d.Mark("m_query");
                // 물어도 안 움직이는 흠(움직이지 않는 문장)은 결국 내가 써 넣는 수밖에 없다.
                // 작가가 버틸 만할 때만 쓴다 — 목소리를 가장 많이 가져가는 부호다.
                if (a.Confidence >= SelfWriteConfidence && Helps(d, "m_insert", s)) return d.Mark("m_insert");
                return d.Mark("m_praise");
            }
        }

        /// <summary>대조군. 원고를 그대로 돌려보낸다.</summary>
        public sealed class Untouched : IPolicy
        {
            public string Id { get { return "p_none"; } }
            public string Name { get { return Localization.Text("policy.none", "손대지 않고 돌려보낸다"); } }
            public MarkDef Pick(GameData d, LiveSentence s, AuthorState a, int round) { return d.Mark("m_leave"); }
        }

        /// <summary>탐색이 찾아낸 계획을 다시 돌릴 때 쓴다. (회차, 문장) → 부호.</summary>
        public sealed class Scripted : IPolicy
        {
            private readonly Dictionary<string, string> _plan;
            private readonly IPolicy _fallback;
            public Scripted(Dictionary<string, string> plan, IPolicy fallback) { _plan = plan; _fallback = fallback; }
            public string Id { get { return "p_scripted"; } }
            public string Name { get { return "짜 둔 교정"; } }

            public static string Key(int round, string sentenceId) { return round + "|" + sentenceId; }

            public MarkDef Pick(GameData d, LiveSentence s, AuthorState a, int round)
            {
                string mid;
                if (_plan.TryGetValue(Key(round, s.Id), out mid)) return d.Mark(mid);
                return _fallback.Pick(d, s, a, round);
            }
        }

        public static IPolicy HarshPen { get { return new Harsh(); } }
        public static IPolicy GentlePen { get { return new Gentle(); } }
        public static IPolicy MiddlePen { get { return new Middle(); } }
        public static IPolicy NoPen { get { return new Untouched(); } }

        /// <summary>세 방침 + 대조군. 「혹독함은 양날이다」가 견주는 것들.</summary>
        public static IList<IPolicy> Three()
        {
            return new List<IPolicy> { HarshPen, GentlePen, MiddlePen };
        }

        public static IList<IPolicy> All()
        {
            return new List<IPolicy> { HarshPen, GentlePen, MiddlePen, NoPen };
        }

        /// <summary>한 부호만 되풀이하는 정책 전부. NoDominantStrategy 가 쓴다.</summary>
        public static IList<IPolicy> Singles(GameData d)
        {
            List<IPolicy> list = new List<IPolicy>();
            foreach (MarkDef m in d.AllMarks) list.Add(new Single(m.id));
            return list;
        }
    }
}
