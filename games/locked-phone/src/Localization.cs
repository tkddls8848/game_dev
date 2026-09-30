using System.Collections.Generic;

namespace Phone
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///   Localization.Text("lock.pushed", "밀려 사라졌다")
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
                        { "lock.locked",   "Locked. The passcode is not coming." },
                        { "lock.wake",     "Wake the screen" },
                        { "lock.pushed",   "Pushed off the screen. It is not coming back." },
                        { "lock.more",     "{0} more" },
                        { "lock.dead",     "The phone is dead. That is all there will ever be." },
                        { "batt.level",    "{0}%" },
                        { "batt.rate",     "Down {0} per {1} minutes while nobody touches it" },
                        { "batt.needTwo",  "Read it twice, far enough apart, to know the rate" },
                        { "exif.taken",    "Taken {0} at {1}" },
                        { "fact.held",     "You can say this much: {0}" },
                        { "verdict.stands", "{0} — and nothing else fits" },
                        { "verdict.short", "Not enough to rule the others out" }
                    }
                }
            };

        /// <summary>현재 언어. "ko" 면 원문을 그대로 쓴다.</summary>
        public static string Language { get; set; } = "ko";

        public static string Text(string key, string korean)
        {
            Dictionary<string, string> table;
            string value;
            if (Language != "ko" && Tables.TryGetValue(Language, out table) && table.TryGetValue(key, out value))
                return value;
            return korean;
        }

        public static string Text(string key, string korean, params object[] args)
        {
            return string.Format(Text(key, korean), args);
        }

        public static IEnumerable<string> KeysFor(string language)
        {
            Dictionary<string, string> table;
            if (Tables.TryGetValue(language, out table)) return table.Keys;
            return new List<string>();
        }
    }
}
