using System.Collections.Generic;

namespace Tactics
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///   Localization.Text("checker.unfair", "정찰로 볼 수 없었던 사실이다")
    ///
    /// 표가 없거나 깨져도 화면이 비지 않고, 코드를 읽는 사람이 원문을 그 자리에서 본다.
    /// **정적 필드에 담지 않는다** — 언어를 바꿔도 첫 값이 굳는다. 전부 속성·메서드다.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "en", new Dictionary<string, string>
                    {
                        { "phase.recon",    "Recon" },
                        { "phase.plan",     "Plan" },
                        { "phase.execute",  "Execute" },
                        { "phase.camp",     "Camp" },
                        { "fail.alarm",     "The alarm went up" },
                        { "fail.downed",    "A partisan went down" },
                        { "fail.objective", "The objective was not met" },
                        { "fail.extract",   "The squad did not reach the treeline" },
                        { "win.clean",      "Cleared without an alarm" },
                        { "audit.explained","Every failure traces to something recon could see" },
                        { "audit.hidden",   "A failure traces to something recon could not see" }
                    }
                }
            };

        /// <summary>현재 언어. "ko" 면 원문을 그대로 쓴다.</summary>
        public static string Language { get; set; } = "ko";

        /// <summary>표에 영어가 있으면 그것을, 없으면 한국어 원문을 돌려준다.</summary>
        public static string Text(string key, string korean)
        {
            Dictionary<string, string> table;
            string value;
            if (Language != "ko"
                && Tables.TryGetValue(Language, out table)
                && table.TryGetValue(key, out value)
                && !string.IsNullOrEmpty(value))
            {
                return value;
            }
            return korean;
        }

        /// <summary>조각을 이어 붙이지 않는다 — 영어는 어순이 다르다. {0} 자리표시자를 쓴다.</summary>
        public static string Text(string key, string korean, params object[] args)
        {
            return string.Format(Text(key, korean), args);
        }
    }
}
