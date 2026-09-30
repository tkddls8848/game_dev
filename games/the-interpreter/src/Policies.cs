using System.Collections.Generic;
using Interp.Data;

namespace Interp.Sim
{
    /// <summary>
    /// 통역사의 방침들. **정책이지 게임 데이터가 아니다** — 문턱값이 여기 있는 이유다.
    /// NoSafeWord 는 이것들을 같은 씨드로 돌려 견준다.
    /// </summary>
    public static class Policies
    {
        /// <summary>한 결만 고른다. 그 결이 없는 마디에서는 정확으로 떨어진다.</summary>
        public sealed class Pure : IPolicy
        {
            private readonly string _register;
            public Pure(string register) { _register = register; }
            public string Id { get { return "p_" + _register; } }

            public string Name
            {
                get
                {
                    switch (_register)
                    {
                        case "exact": return Localization.Text("policy.exact", "늘 정확하게 옮긴다");
                        case "soft":  return Localization.Text("policy.soft", "늘 완곡하게 옮긴다");
                        case "hard":  return Localization.Text("policy.hard", "늘 강경하게 옮긴다");
                        case "false": return Localization.Text("policy.false", "옮길 수 있는 곳마다 틀리게 옮긴다");
                    }
                    return _register;
                }
            }

            public RenderDef Pick(GameData d, RoundDef round, SessionState st)
            {
                foreach (RenderDef x in round.renderings) if (x.register == _register) return x;
                return SessionSim.Exact(round);
            }
        }

        /// <summary>
        /// 방을 읽는다. 순수 정책 넷을 섞는다 —
        /// 긴장이 높으면 누그러뜨리고, 낮으면 고용주(하란) 쪽으로 밀고,
        /// 아무도 나를 의심하지 않을 때만 한 번 거짓을 섞는다.
        /// </summary>
        public sealed class Situational : IPolicy
        {
            public const int CalmTension = 34;    // 이 밑이면 여유가 있다
            public const int TenseTension = 58;   // 이 위면 불을 꺼야 한다
            public const int SafeSuspicion = 18;  // 이 밑이면 아직 나를 안 본다

            public string Id { get { return "p_situational"; } }
            public string Name { get { return Localization.Text("policy.situational", "방을 읽는다"); } }

            public RenderDef Pick(GameData d, RoundDef round, SessionState st)
            {
                RenderDef exact = null, soft = null, hard = null, lie = null;
                foreach (RenderDef x in round.renderings)
                {
                    if (x.register == "exact" && exact == null) exact = x;
                    else if (x.register == "soft" && soft == null) soft = x;
                    else if (x.register == "hard" && hard == null) hard = x;
                    else if (x.register == "false" && lie == null) lie = x;
                }
                if (st.Tension >= TenseTension) return soft ?? exact;
                if (st.Suspicion < SafeSuspicion && lie != null && st.Tension < TenseTension) return lie;
                if (st.Tension <= CalmTension && hard != null) return hard;
                return exact;
            }
        }

        /// <summary>
        /// ★ 고용주 편을 든다. **거짓말은 한마디도 하지 않는다.**
        /// 하란이 말하면 강하게, 케리아가 말하면 눌러서 옮긴다 —
        /// 그것만으로 조문이 한쪽으로 기운다. 이 정책이 있는 이유는
        /// 「오역만이 조약을 바꾼다」가 거짓임을 검사기가 보게 하려는 것이다.
        /// </summary>
        public sealed class Shade : IPolicy
        {
            public string Id { get { return "p_shade"; } }
            public string Name { get { return Localization.Text("policy.shade", "고용주 쪽으로 기울여 옮긴다"); } }

            public RenderDef Pick(GameData d, RoundDef round, SessionState st)
            {
                RenderDef exact = null, soft = null, hard = null;
                foreach (RenderDef x in round.renderings)
                {
                    if (x.register == "exact" && exact == null) exact = x;
                    else if (x.register == "soft" && soft == null) soft = x;
                    else if (x.register == "hard" && hard == null) hard = x;
                }
                // 조인 직전 낭독은 매끄럽게 넘긴다. 짚고 넘어가면 내가 눌러 둔 것이 들린다.
                bool reading = round.revisits != null && round.revisits.Length > 0;
                if (reading) return soft ?? exact;
                if (round.speaker == "n_haran") return (st.Tension < 70 ? hard : exact) ?? exact;
                return soft ?? exact;
            }
        }

        /// <summary>미리 정해 둔 역어 목록대로만 간다. 탐색이 찾아낸 계획을 다시 돌릴 때 쓴다.</summary>
        public sealed class Scripted : IPolicy
        {
            private readonly Dictionary<string, string> _byStage;
            private readonly IPolicy _fallback;
            public Scripted(Dictionary<string, string> byStage, IPolicy fallback)
            {
                _byStage = byStage;
                _fallback = fallback;
            }
            public string Id { get { return "p_scripted"; } }
            public string Name { get { return "짜 둔 계획"; } }

            public RenderDef Pick(GameData d, RoundDef round, SessionState st)
            {
                string want;
                if (_byStage.TryGetValue(d.Stages[st.StageIndex].id, out want))
                    foreach (RenderDef x in round.renderings) if (x.id == want) return x;
                return _fallback.Pick(d, round, st);
            }
        }

        public static IPolicy Exact { get { return new Pure("exact"); } }
        public static IPolicy Soft { get { return new Pure("soft"); } }
        public static IPolicy Hard { get { return new Pure("hard"); } }
        public static IPolicy False { get { return new Pure("false"); } }
        public static IPolicy Room { get { return new Situational(); } }
        public static IPolicy Tilt { get { return new Shade(); } }

        /// <summary>검사기들이 견주는 여섯. 순서가 표의 순서다.</summary>
        public static IList<IPolicy> All()
        {
            return new List<IPolicy> { Exact, Soft, Hard, False, Room, Tilt };
        }
    }
}
