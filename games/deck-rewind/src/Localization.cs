using System.Collections.Generic;

namespace DeckRewind
{
    /// <summary>
    /// 한국어가 원문, 영어는 덮어쓰기 (뿌리 CLAUDE.md 설계 원칙 6).
    /// 문자열을 static 필드에 굳히지 않는다 — 언어를 바꿔도 첫 값이 남기 때문이다.
    /// 전부 이 함수를 지나가고, 표가 없거나 깨져도 한국어 원문이 그대로 보인다.
    /// </summary>
    public static class Localization
    {
        static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>();

        public static string Language { get; set; } = "ko";

        public static void SetOverride(string language, string key, string text)
        {
            if (!Tables.TryGetValue(language, out var t))
            {
                t = new Dictionary<string, string>();
                Tables[language] = t;
            }
            t[key] = text;
        }

        public static void Reset()
        {
            Tables.Clear();
            Language = "ko";
        }

        /// 조각을 이어 붙이지 않는다. 어순이 다른 언어가 있으므로 {0} 자리표시자를 쓴다.
        public static string Text(string key, string koOriginal, params object[] args)
        {
            string s = koOriginal;
            if (Language != "ko" && Tables.TryGetValue(Language, out var t)
                && t.TryGetValue(key, out var over) && !string.IsNullOrEmpty(over))
            {
                s = over;
            }
            return args == null || args.Length == 0 ? s : string.Format(s, args);
        }
    }
}
