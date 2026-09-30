using System.Collections.Generic;

namespace Secretary
{
    /// <summary>
    /// 뿌리 CLAUDE.md 설계 원칙 6. 한국어가 원문이고 영어가 덮어쓰기다.
    /// 정적 필드에 담지 않는다 — 언어를 바꿔도 첫 값이 굳는다. 전부 속성·메서드다.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "en", new Dictionary<string, string>
                    {
                        { "seal.grant",     "GRANTED" },
                        { "seal.deny",      "REFUSED" },
                        { "seal.defer",     "HELD" },
                        { "ledger.left",    "What was asked" },
                        { "ledger.right",   "What it was taken from" },
                        { "ledger.taken",   "{0} of {1} taken from {2}" },
                        { "ledger.balance", "Granted {0}, drawn {1}. The book balances" },
                        { "ledger.broken",  "Granted {0}, drawn {1}. The book does not balance" },
                        { "grant.partial",  "Only {0} of the {1} asked could be drawn" },
                        { "grant.none",     "There was nothing left to draw" },
                        { "defer.grew",     "{0} was held over. The need grew by {1}" },
                        { "deny.closed",    "{0} was refused. It will not grow" },
                        { "audit.noFree",   "No prayer can be granted without a named cost" },
                        { "god.absent",     "The god does not appear in this ledger" }
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
