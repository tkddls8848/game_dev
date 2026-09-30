using System.Collections.Generic;

namespace MapLies
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///     Localization.Text("trap.sealed", "{0} 이 건물 사이에 갇혔다", 이름)
    ///
    /// 표가 없거나 깨져도 화면이 비지 않고, 코드를 읽는 사람이 원문을 그 자리에서 본다.
    /// 정적 필드에 담지 않는다 — 언어를 바꿔도 첫 값이 굳는다. 전부 속성·메서드다.
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
                        { "kind.block",    "Block" },
                        { "kind.street",   "Street" },
                        { "kind.plaza",    "Plaza" },
                        { "kind.unspec",   "Undrawn" },
                        { "phase.draw",    "Draw" },
                        { "phase.build",   "Build" },
                        { "phase.survey",  "Survey" },
                        { "trap.sealed",   "{0} is walled in between buildings" },
                        { "trap.entombed", "{0} has nowhere left to stand" },
                        { "trap.freed",    "{0} can reach the gate again" },
                        { "city.refused",  "The city has rebuilt this plot as often as it can. It stays as it is" },
                        { "city.settled",  "The city has caught up with the map" },
                        { "city.churning", "The city is still moving" },
                        { "cost.redraw",   "This plot was drawn before. The eraser is not free" },
                        { "harm.kept",     "{0} days of harm do not come back" }
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
