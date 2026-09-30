using System.Collections.Generic;

namespace SilentBaton
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///   Localization.Text("press.lag", "{0}이 내내 늘어졌다", "현")
    ///
    /// **정적 필드에 담지 않는다** — 언어를 바꿔도 첫 값이 굳는다. 전부 속성·메서드다.
    /// 조각을 이어 붙이지 않고 {0} 자리표시자를 쓴다 — 영어는 어순이 다르다.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "en", new Dictionary<string, string>
                    {
                        { "chan.breath",    "breathing" },
                        { "chan.bow",       "bow angle" },
                        { "chan.face",      "expression" },
                        { "chan.none",      "none" },
                        { "cond.visible",   "By sight alone — the silent podium" },
                        { "cond.hearing",   "The world that can hear" },
                        { "cond.immediate", "The world that is told at once" },
                        { "press.lag",      "{0} — dragged throughout" },
                        { "press.rush",     "{0} — ran ahead all evening" },
                        { "press.loud",     "{0} — far too loud" },
                        { "press.soft",     "{0} — barely audible" },
                        { "press.split",    "{0}, {1} — two different beats" },
                        { "press.clean",    "There was nothing to fault" },
                        { "grade.pass",     "Passed at {0} points" },
                        { "grade.fail",     "Failed at {0} points" },
                        { "beat.silent",    "No sound reaches the podium" }
                    }
                }
            };

        /// <summary>현재 언어. "ko" 면 원문을 그대로 쓴다.</summary>
        public static string Language { get; set; } = "ko";

        public static string Text(string key, string korean)
        {
            Dictionary<string, string> table;
            string value;
            if (Language != "ko"
                && Tables.TryGetValue(Language, out table)
                && table.TryGetValue(key, out value)
                && !string.IsNullOrEmpty(value))
                return value;
            return korean;
        }

        public static string Text(string key, string korean, params object[] args)
        {
            return string.Format(Text(key, korean), args);
        }
    }
}
