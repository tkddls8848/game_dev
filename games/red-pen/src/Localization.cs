using System.Collections.Generic;

namespace RedPen
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///   Localization.Text("mark.delete", "삭제선")
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
                        { "policy.harsh",     "Always harsh" },
                        { "policy.gentle",    "Always gentle" },
                        { "policy.middle",    "Read the writer" },
                        { "policy.none",      "Send it back untouched" },
                        { "author.refused",   "{0} left the mark where it was" },
                        { "author.timid",     "The revision came back shorter and afraid" },
                        { "author.good",      "The revision came back better than the note asked for" },
                        { "author.withdrew",  "{0} asked for the manuscript back" },
                        { "author.silenced",  "{0} has stopped sending pages" },
                        { "masterpiece",      "A sentence arrived that no note had asked for" },
                        { "audit.moves",      "The same manuscript ends {0} points apart under different pens" },
                        { "audit.bothways",   "Harsh wins {0} measures, gentle wins {1}, neither takes all" }
                    }
                }
            };

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
