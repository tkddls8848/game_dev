using System.Collections.Generic;
using Phone.Data;

namespace Phone.Sim
{
    /// <summary>
    /// 증거 → 사실 → 정답. 이 파일 하나가 "무엇을 알 수 있는가"의 전부다.
    ///
    /// **증거 딱지**는 넷뿐이다:
    ///   e_xxx@card    알림이 왔다는 것 — 미리보기를 꺼도 남는다
    ///   e_xxx@preview 미리보기 본문
    ///   e_xxx@exif    사진의 촬영 시각·장소
    ///   battery@rate  잔량을 충분히 벌려 두 번 읽어야 나온다
    ///
    /// **사실**은 needs 를 전부 쥐어야 성립한다(논리곱). 잠금을 푼 세계에서는 altNeedsUnlocked 로도 성립한다.
    /// **정답**은 supports 를 전부 쥐고, 나머지 선택지가 **전부** 지워져야 선다.
    /// 선택지 하나를 지우려면 그 선택지의 refutedBy 를 **전부** 쥐어야 한다 —
    /// 그래야 "어느 하나를 빼면 부족해진다"가 말 그대로 참이 된다.
    /// </summary>
    public static class Deduction
    {
        public const string Battery = "battery@rate";

        /// <summary>본 알림들과 잔량 측정 여부에서 증거 딱지를 만든다.</summary>
        public static HashSet<string> Tokens(GameData d, IEnumerable<string> seenNotifIds, bool batteryRate, World w)
        {
            HashSet<string> t = new HashSet<string>();
            foreach (string id in seenNotifIds)
            {
                t.Add(id + "@card");
                if (!w.preview) continue;
                NotifDef n = d.Notif(id);
                if (n == null) continue;            // 씨드가 만든 잡음 — 사실을 주지 않는다
                if (!string.IsNullOrEmpty(n.previewKo)) t.Add(id + "@preview");
                if (n.exifAtMin >= 0) t.Add(id + "@exif");
            }
            if (batteryRate) t.Add(Battery);
            if (w.unlocked) foreach (LockedDef l in d.Locked.locked) t.Add(l.id);
            return t;
        }

        public static HashSet<string> Facts(GameData d, HashSet<string> tokens, World w)
        {
            HashSet<string> got = new HashSet<string>();
            foreach (FactDef f in d.Case.facts)
            {
                if (Covers(tokens, f.needs)) { got.Add(f.id); continue; }
                if (w.unlocked && f.altNeedsUnlocked != null && f.altNeedsUnlocked.Count > 0
                    && Covers(tokens, f.altNeedsUnlocked)) got.Add(f.id);
            }
            return got;
        }

        private static bool Covers(HashSet<string> have, List<string> need)
        {
            if (need == null || need.Count == 0) return false;   // 빈 요구는 '공짜로 성립'이 아니다
            foreach (string x in need) if (!have.Contains(x)) return false;
            return true;
        }

        /// <summary>이 사실 집합으로 세워지는 정답들.</summary>
        public static HashSet<string> Verdicts(GameData d, HashSet<string> facts)
        {
            HashSet<string> got = new HashSet<string>();
            foreach (VerdictDef v in d.Case.verdicts) if (Stands(d, v, facts)) got.Add(v.id);
            return got;
        }

        public static bool Stands(GameData d, VerdictDef v, HashSet<string> facts)
        {
            foreach (string s in v.supports) if (!facts.Contains(s)) return false;
            foreach (OptionDef o in v.options)
            {
                if (o.id == v.correct) continue;
                if (o.refutedBy == null || o.refutedBy.Count == 0) return false;
                foreach (string r in o.refutedBy) if (!facts.Contains(r)) return false;
            }
            return true;
        }

        /// <summary>이 정답 하나를 세우는 데 쓰이는 사실 전부 (근거 + 반박).</summary>
        public static List<string> FactFootprint(GameData d, VerdictDef v)
        {
            List<string> outp = new List<string>();
            foreach (string s in v.supports) if (!outp.Contains(s)) outp.Add(s);
            foreach (OptionDef o in v.options)
            {
                if (o.id == v.correct || o.refutedBy == null) continue;
                foreach (string r in o.refutedBy) if (!outp.Contains(r)) outp.Add(r);
            }
            return outp;
        }

        /// <summary>이 정답 하나를 세우는 데 필요한 **알림** 전부. 배터리는 여기 안 든다.</summary>
        public static List<string> NotifFootprint(GameData d, VerdictDef v)
        {
            List<string> outp = new List<string>();
            foreach (string fid in FactFootprint(d, v))
                foreach (string tok in d.Fact(fid).needs)
                {
                    string n = GameData.NotifOf(tok);
                    if (n != null && !outp.Contains(n)) outp.Add(n);
                }
            outp.Sort();
            return outp;
        }

        public static bool NeedsBattery(GameData d, VerdictDef v)
        {
            foreach (string fid in FactFootprint(d, v))
                foreach (string tok in d.Fact(fid).needs) if (tok == Battery) return true;
            return false;
        }

        /// <summary>사건 전체를 세우는 데 필요한 알림 전부 (합집합).</summary>
        public static List<string> AllNeededNotifs(GameData d)
        {
            List<string> outp = new List<string>();
            foreach (VerdictDef v in d.Case.verdicts)
                foreach (string n in NotifFootprint(d, v)) if (!outp.Contains(n)) outp.Add(n);
            outp.Sort();
            return outp;
        }
    }
}
