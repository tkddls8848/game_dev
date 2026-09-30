using System.Collections.Generic;

namespace Scent
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///   Localization.Text("nose.masked", "다른 냄새에 묻혀 언제인지 모르겠다")
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
                        { "sense.blind",    "You cannot see. You cannot hear. Only smell." },
                        { "nose.nothing",   "Nothing here that the nose can hold onto" },
                        { "nose.knownWho",  "{0} was here, but when, the nose cannot say" },
                        { "nose.dated",     "{0} was here at {1}" },
                        { "nose.masked",    "Buried under another smell; the age will not read" },
                        { "nose.faint",     "Too faint now. It should have been caught earlier" },
                        { "nose.doubled",   "{0} came through twice; the two layers have fused" },
                        { "nose.tired",     "The nose has gone dead. Nothing more tonight" },
                        { "note.top",       "top note" },
                        { "note.heart",     "heart note" },
                        { "note.base",      "base note" },
                        { "check.sweep",    "One pass is not enough: {0} of {1} facts" },
                        { "check.revisit",  "Going back recovers all {0}" }
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
