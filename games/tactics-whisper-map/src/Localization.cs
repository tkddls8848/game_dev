using System.Collections.Generic;

namespace Whisper
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///   Localization.Text("ask.closed", "마을이 등을 돌렸다")
    ///
    /// 표가 없거나 깨져도 화면이 비지 않고, 코드를 읽는 사람이 원문을 그 자리에서 본다.
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
                        { "phase.ask",       "Ask" },
                        { "phase.plan",      "Plan" },
                        { "phase.execute",  "Execute" },
                        { "phase.village",   "Village" },
                        { "ask.closed",      "The village has turned its back" },
                        { "ask.silent",      "{0} has gone quiet for a while" },
                        { "ask.poor",        "There is no trust left to spend" },
                        { "ask.conflict",    "Two answers disagree — this one cannot be trusted" },
                        { "ask.confirmed",   "Two answers agree — this one is true" },
                        { "fail.alarm",      "The alarm went up" },
                        { "fail.lost",       "A partisan went down" },
                        { "fail.objective",  "The objective was not met" },
                        { "fail.extract",    "The squad did not reach the flax field" },
                        { "fail.timeout",    "The shot never came" },
                        { "win.clean",       "Cleared without an alarm" },
                        { "audit.fair",      "Every failure traces to something the village could have told you" },
                        { "audit.hidden",    "A failure traces to something no one could have told you" },
                        { "blind.ok",        "This mission can be taken with no intel bought at all" }
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
