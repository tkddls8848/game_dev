// 한국어가 원문, 영어는 덮어쓰기. Text("key", "한국어 원문") 꼴로 부른다.
// 표가 없거나 깨져도 화면이 비지 않고, 코드를 읽는 사람이 원문을 그 자리에서 본다.
// 정적 필드에 담지 않는다 — 언어를 바꿔도 첫 값이 굳는다. 전부 메서드/속성으로 둔다.
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace HybridSownDeck
{
    public enum Language { Korean, English }

    public static class Localization
    {
        static readonly Dictionary<string, string> Overlay = new Dictionary<string, string>();

        public static Language Current { get; set; } = Language.Korean;

        public static void LoadOverlay(string path)
        {
            Overlay.Clear();
            if (!File.Exists(path)) return;
            var file = JsonConvert.DeserializeObject<OverlayFile>(File.ReadAllText(path));
            if (file?.entries == null) return;
            foreach (var e in file.entries)
                if (!string.IsNullOrEmpty(e.key)) Overlay[e.key] = e.text;
        }

        /// <summary>원문(한국어)을 그 자리에 적는다. 영어일 때만 표를 찾아 덮어쓴다.</summary>
        public static string Text(string key, string korean)
        {
            if (Current == Language.English && Overlay.TryGetValue(key, out var en) && !string.IsNullOrEmpty(en))
                return en;
            return korean;
        }

        /// <summary>조각을 이어 붙이지 않는다 — 영어는 어순이 다르다. {0} 자리표시자를 쓴다.</summary>
        public static string Text(string key, string korean, params object[] args)
            => string.Format(Text(key, korean), args);

        public static bool HasOverlay(string key) => Overlay.ContainsKey(key);
        public static IReadOnlyCollection<string> OverlayKeys => Overlay.Keys;

        sealed class OverlayFile { public List<Entry> entries = new List<Entry>(); }
        sealed class Entry { public string key = ""; public string text = ""; }
    }
}
