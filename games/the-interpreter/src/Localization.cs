using System.Collections.Generic;

namespace Interp
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///   Localization.Text("register.false", "오역")
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
                        { "register.exact",   "Faithful" },
                        { "register.soft",    "Softened" },
                        { "register.hard",    "Hardened" },
                        { "register.false",   "Mistranslated" },
                        { "clause.unset",     "Article {0} was left unwritten" },
                        { "misread.standing", "“{0}” — one side heard {1}, the other heard {2}" },
                        { "misread.exposed",  "“{0}” was caught. The room went quiet" },
                        { "end.signed",       "Signed" },
                        { "end.collapsed",    "The talks broke off" },
                        { "policy.exact",     "Render everything faithfully" },
                        { "policy.soft",      "Soften everything" },
                        { "policy.hard",      "Harden everything" },
                        { "policy.false",     "Mistranslate wherever possible" },
                        { "policy.situational", "Read the room" },
                        { "policy.shade",     "Tilt toward the paymaster" },
                        { "audit.oneword",    "{0} single-word changes move the treaty" },
                        { "audit.nosafe",     "Faithful rendering is not the best policy under {0} of {1} measures" }
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

        /// <summary>역어의 결. 데이터의 register 문자열을 사람이 읽는 말로.</summary>
        public static string Register(string register)
        {
            switch (register)
            {
                case "exact": return Text("register.exact", "정확");
                case "soft":  return Text("register.soft", "완곡");
                case "hard":  return Text("register.hard", "강경");
                case "false": return Text("register.false", "오역");
            }
            return register;
        }
    }
}
