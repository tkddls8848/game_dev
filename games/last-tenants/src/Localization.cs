using System.Collections.Generic;

namespace Tenants
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///   Localization.Text("silence.confirmed", "이 문에서는 아무 소리도 나지 않는다")
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
                        { "notice.title",        "NOTICE OF DEMOLITION" },
                        { "notice.seal",         "SEALED" },
                        { "slot.listen",         "Ear to the door of {0}" },
                        { "slot.help",           "Helping {0}" },
                        { "slot.rest",           "Nothing" },
                        { "silence.confirmed",   "No sound at all from this door" },
                        { "silence.rumour",      "Someone spoke of {0}, but the door has not been checked" },
                        { "silence.gone",        "{0} left last week. The silence holds nothing" },
                        { "silence.unable",      "{0} is still in there and cannot speak" },
                        { "know.none",           "Nothing is known about {0}" },
                        { "know.hint",           "Something is wrong in {0}, but not what" },
                        { "know.need",           "{0} needs: {1}" },
                        { "know.stakes",         "{0} needs {1}, and how badly is known too" },
                        { "know.wrong",          "{0} was taken to need {1}. It was not that" },
                        { "help.spent",          "Grace spent: {0} days. The other households carry it" },
                        { "help.none",           "No one was helped" },
                        { "choice.cost",         "Choosing {0} raised the loss of the other five by {1}" },
                        { "audit.noFreeChoice",  "Every choice costs the others something measurable" },
                        { "audit.silencePays",   "The silent unit cannot be read from its own door alone" }
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
