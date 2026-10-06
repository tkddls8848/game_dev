// 순수 C#만. using UnityEngine 금지.
using System;
using System.Collections.Generic;
using System.Text;

namespace FarmErosion.Sim
{
    /// <summary>다른 사람이 남긴 한 줄과, 그가 어디까지 갔는지.</summary>
    public sealed class LedgerLine
    {
        public string TextKo = "";
        public string ContextKo = "";      // "5년차 봄까지" — 이것 말고는 아무것도 붙이지 않는다
    }

    /// <summary>
    /// **다른 플레이어의 말.** DIRECTION.md §2-7 의 마지막 연출이 읽는 자리다.
    ///
    /// 전제 조건이 하나 있고 그게 전부다: **지어낸 것을 남의 말이라고 보여 주지 않는다.**
    /// NieR 도 항아리 게임도 진짜를 써서 그 장면이 작동한다. 우리가 작가가 쓴 문장을
    /// "다른 플레이어의 말"로 띄우면 들통났을 때 **게임 전체가 거짓말이 된다** —
    /// 여기가 이 게임이 가장 진심인 자리라서 손해가 가장 크다.
    ///
    /// 그래서 이 파일에는 **씨앗 문장이 하나도 없다.** 비어 있으면 비어 있는 채로 끝난다.
    /// 수가 적으면 적은 채로 보여 준다 — 세 줄만 올라와도 그것이 진짜면 작동한다.
    ///
    /// PoC 단계에서 이것이 담는 것은 **이 기기에서 지난 회차에 남긴 말**이다.
    /// 서버도 동봉분도 아직 없다(§2-7 '남은 문제'). 구조만 잡아 두고 자리를 비워 둔다.
    ///
    /// 형식은 JsonUtility 가 그대로 읽을 수 있게 맞춘다(배열 + 문자열, 설계 원칙 3).
    /// 쓰는 쪽은 직접 굽는다 — 헤드리스에서 JsonUtility 를 쓸 수 없고, 사람이 친 글이라
    /// 이스케이프를 우리가 책임져야 한다.
    /// </summary>
    public static class Ledger
    {
        /// <summary>한 줄이다. 문단이 아니다 — 길어지면 다음 줄을 덮어 목록이 된다.</summary>
        public const int MaxChars = 44;
        /// <summary>파일에 쌓아 두는 최대치. 넘으면 오래된 것부터 버린다.</summary>
        public const int Keep = 200;
        /// <summary>엔딩에 올리는 줄 수. 쏟아지면 목록이 된다(§2-7 '천천히, 한 줄씩').</summary>
        public const int ShowCount = 5;

        /// <summary>
        /// 사람이 친 글을 한 줄로 만든다. 걸러지면 null 이다.
        /// 뜻은 바꾸지 않는다 — 줄바꿈·제어문자를 지우고 길이만 자른다.
        /// </summary>
        public static string Clean(string raw)
        {
            if (raw == null) return null;
            var sb = new StringBuilder(raw.Length);
            bool space = true;                      // 앞쪽 공백부터 먹는다
            foreach (char c in raw)
            {
                char ch = c;
                if (ch == '\n' || ch == '\r' || ch == '\t') ch = ' ';
                if (char.IsControl(ch)) continue;
                if (ch == ' ') { if (space) continue; space = true; sb.Append(' '); continue; }
                space = false;
                sb.Append(ch);
            }
            string s = sb.ToString().TrimEnd();
            if (s.Length == 0) return null;
            if (s.Length > MaxChars) s = s.Substring(0, MaxChars).TrimEnd();
            return s.Length == 0 ? null : s;
        }

        public static List<LedgerLine> Parse(string json)
        {
            var list = new List<LedgerLine>();
            if (string.IsNullOrEmpty(json)) return list;
            int i = 0;
            while (true)
            {
                string text = NextString(json, "\"textKo\"", ref i);
                if (text == null) break;
                int j = i;
                string context = NextString(json, "\"contextKo\"", ref j) ?? "";
                // 다음 textKo 보다 뒤에 있는 contextKo 는 남의 것이다.
                int peek = i;
                if (NextString(json, "\"textKo\"", ref peek) != null && j > peek) context = "";
                list.Add(new LedgerLine { TextKo = text, ContextKo = context });
            }
            return list;
        }

        public static string Write(IList<LedgerLine> lines)
        {
            var sb = new StringBuilder();
            sb.Append("{\"lines\":[");
            int start = Math.Max(0, lines.Count - Keep);
            for (int k = start; k < lines.Count; k++)
            {
                if (k > start) sb.Append(',');
                sb.Append("\n {\"textKo\":\"").Append(Escape(lines[k].TextKo))
                  .Append("\",\"contextKo\":\"").Append(Escape(lines[k].ContextKo)).Append("\"}");
            }
            sb.Append("\n]}\n");
            return sb.ToString();
        }

        /// <summary>내 한 줄을 뒤에 붙인 파일 내용을 돌려준다. 걸러지면 원본 그대로다.</summary>
        public static string Append(string json, string rawText, string contextKo)
        {
            string text = Clean(rawText);
            if (text == null) return json ?? Write(new List<LedgerLine>());
            var lines = Parse(json);
            lines.Add(new LedgerLine { TextKo = text, ContextKo = Clean(contextKo) ?? "" });
            return Write(lines);
        }

        /// <summary>
        /// 엔딩에 올릴 줄. **가장 최근 것부터** 거슬러 올라가며 고르고, 올릴 때는 다시 시간 순이다.
        /// 내 이번 회차의 말은 여기 없다 — 내가 방금 쓴 것을 남의 말로 보여 주지 않는다.
        /// </summary>
        public static List<LedgerLine> Recent(IList<LedgerLine> all, int count)
        {
            var take = new List<LedgerLine>();
            for (int k = all.Count - 1; k >= 0 && take.Count < count; k--) take.Add(all[k]);
            take.Reverse();
            return take;
        }

        static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (char.IsControl(c)) sb.Append(' ');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>key 뒤의 첫 문자열 값을 읽고 i 를 그 뒤로 옮긴다. 없으면 null.</summary>
        static string NextString(string s, string key, ref int i)
        {
            int at = s.IndexOf(key, i, StringComparison.Ordinal);
            if (at < 0) return null;
            at = s.IndexOf('"', at + key.Length);          // 값을 여는 따옴표
            if (at < 0) return null;
            var sb = new StringBuilder();
            for (int k = at + 1; k < s.Length; k++)
            {
                char c = s[k];
                if (c == '\\' && k + 1 < s.Length) { sb.Append(s[++k]); continue; }
                if (c == '"') { i = k + 1; return sb.ToString(); }
                sb.Append(c);
            }
            return null;
        }
    }
}
