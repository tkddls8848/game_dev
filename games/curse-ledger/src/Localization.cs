using System.Collections.Generic;

namespace CurseLedger
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    ///
    ///   Localization.Text("end.uprising", "마을이 제단을 부쉈다")
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
                        { "end.passedOn",     "The ledger was handed down" },
                        { "end.villageRuin",  "The village emptied" },
                        { "end.houseExtinct", "The house ended" },
                        { "end.uprising",     "The village broke the altar" },
                        { "end.curseUnbound", "The binding gave way" },
                        { "end.curseLifted",  "The pact was undone" },
                        { "end.stalled",      "The ledger stalled" },
                        { "ledger.line",      "Generation {0}, {1}: {2}" },
                        { "victim.struck",    "A red line was drawn over {0} ({1})" },
                        { "bill.arrives",     "A bill from {0} arrives in generation {1}" },
                        { "bill.carried",     "The postponed tribute is added to this generation" },
                        { "clean.none",       "No policy leaves the village alive with no one struck out" },
                        { "extreme.keep",     "Keeping the pact: {0}" },
                        { "extreme.release",  "Undoing the pact: {0}" }
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
