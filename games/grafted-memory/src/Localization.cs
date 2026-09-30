using System.Collections.Generic;

namespace Graft
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///     Localization.Text("reject.timePlace", "몸이 두 곳에 있을 수 없다: {0}", 이름)
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
                        { "phase.probe",        "Probe" },
                        { "phase.graft",        "Graft" },
                        { "phase.settle",       "Settle" },
                        { "reject.timePlace",   "The body cannot be in two places: {0}" },
                        { "reject.personGone",  "{0} was not there on that day" },
                        { "reject.personElse",  "{0} was elsewhere that day: {1}" },
                        { "reject.placeWindow", "{0} did not stand on that day" },
                        { "reject.moodAnchor",  "The seam does not feel like {0}" },
                        { "reject.era",         "Nothing in that season felt like this: {0}" },
                        { "reject.intensity",   "Too loud for what it is sewn to: {0}" },
                        { "accept.settled",     "It settled. He will remember it as his own" },
                        { "audit.fair",         "Every rejection traces to something the dream could have shown you" },
                        { "audit.hidden",       "A rejection traces to something nothing could have shown you" },
                        { "probe.corroborated", "Two routes agree, so this is the real shape" },
                        { "probe.single",       "One route only, so this shape may be a screen" },
                        { "self.suspect",       "{0} of your own memories tremble the same way" }
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
