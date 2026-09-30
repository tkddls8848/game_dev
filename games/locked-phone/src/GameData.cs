using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Phone.Data
{
    /// <summary>
    /// data/ 의 JSON 다섯 장을 한 벌로 묶는다.
    /// 테스트 산출물이 games/locked-phone/TestBuild/... 에 놓이므로 실행 폴더에서 위로
    /// 올라가며 data/phone.json 을 찾는다.
    /// </summary>
    public sealed class GameData
    {
        public PhoneFile Phone { get; private set; }
        public NotificationsFile Notifs { get; private set; }
        public LockedFile Locked { get; private set; }
        public CaseFile Case { get; private set; }
        public BalanceFile Balance { get; private set; }
        public string DataRoot { get; private set; }

        private readonly Dictionary<string, NotifDef> _notif = new Dictionary<string, NotifDef>();
        private readonly Dictionary<string, FactDef> _fact = new Dictionary<string, FactDef>();
        private readonly Dictionary<string, VerdictDef> _verdict = new Dictionary<string, VerdictDef>();
        private readonly Dictionary<string, LockedDef> _locked = new Dictionary<string, LockedDef>();

        public static GameData Load() { return Load(FindDataRoot()); }

        public static GameData Load(string dataRoot)
        {
            GameData d = new GameData();
            d.DataRoot = dataRoot;
            d.Phone = Read<PhoneFile>(dataRoot, "phone.json");
            d.Notifs = Read<NotificationsFile>(dataRoot, "notifications.json");
            d.Locked = Read<LockedFile>(dataRoot, "locked.json");
            d.Case = Read<CaseFile>(dataRoot, "case.json");
            d.Balance = Read<BalanceFile>(dataRoot, "balance.json");
            foreach (NotifDef n in d.Notifs.notifications) d._notif[n.id] = n;
            foreach (FactDef f in d.Case.facts) d._fact[f.id] = f;
            foreach (VerdictDef v in d.Case.verdicts) d._verdict[v.id] = v;
            foreach (LockedDef l in d.Locked.locked) d._locked[l.id] = l;
            return d;
        }

        private static T Read<T>(string root, string file)
        {
            string path = Path.Combine(root, file);
            if (!File.Exists(path)) throw new FileNotFoundException("데이터 파일이 없다: " + path);
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
        }

        public static string FindDataRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "data");
                if (File.Exists(Path.Combine(candidate, "phone.json"))) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException("data/phone.json 을 위로 올라가며 찾지 못했다: " + AppContext.BaseDirectory);
        }

        public bool HasNotif(string id) { return _notif.ContainsKey(id); }
        /// <summary>씨드가 만든 잡음 알림은 여기 없다 — 그때는 null 이다(딱지 @card 만 남는다).</summary>
        public NotifDef Notif(string id) { NotifDef n; return _notif.TryGetValue(id, out n) ? n : null; }
        public bool HasFact(string id) { return _fact.ContainsKey(id); }
        public FactDef Fact(string id) { return _fact[id]; }
        public bool HasVerdict(string id) { return _verdict.ContainsKey(id); }
        public VerdictDef Verdict(string id) { return _verdict[id]; }
        public bool HasLocked(string id) { return _locked.ContainsKey(id); }

        /// <summary>증거 딱지에서 알림 id 만 떼어 낸다. "e_x@preview" → "e_x". 배터리면 null.</summary>
        public static string NotifOf(string token)
        {
            int i = token.IndexOf('@');
            string head = i < 0 ? token : token.Substring(0, i);
            return head == "battery" ? null : head;
        }

        public static string GradeOf(string token)
        {
            int i = token.IndexOf('@');
            return i < 0 ? "card" : token.Substring(i + 1);
        }
    }
}
