using System.Collections.Generic;
using Phone.Data;
using Phone.Sim;

namespace Phone.Tests
{
    /// <summary>테스트가 공유하는 세계. data/ 를 한 번만 읽는다.</summary>
    public static class TestWorld
    {
        private static GameData _data;
        private static readonly Dictionary<int, Lockscreen> _screens = new Dictionary<int, Lockscreen>();

        public static GameData Data { get { if (_data == null) _data = GameData.Load(); return _data; } }

        public static Lockscreen Screen(int seed)
        {
            Lockscreen s;
            if (_screens.TryGetValue(seed, out s)) return s;
            s = Lockscreen.Build(Data, seed);
            _screens[seed] = s;
            return s;
        }

        public static Lockscreen Screen() { return Screen(Data.Phone.seed); }
        public static Session Sess(World w) { return new Session(Data, Screen(), w); }
        public static Session Sess(World w, int seed) { return new Session(Data, Screen(seed), w); }

        public static int VerdictCount { get { return Data.Case.verdicts.Count; } }
        public static CheckerDef C { get { return Data.Balance.checkers; } }
        public static string Clock(int m) { return SessionResult.Clock(m); }

        /// <summary>플레이어가 실제로 쥘 수 있는 최대치 — 예산 안에서 최선을 다한 결과.</summary>
        public static SessionResult Best(World w) { return Sess(w).MinimalSession(); }

        /// <summary>
        /// 예산을 무시하고 "볼 수 있었던 알림 전부"를 쥔 상태.
        /// LockscreenSuffices 는 **논리**를 보는 검사기라 예산이 아니라 정보의 경계를 봐야 한다.
        /// </summary>
        public static HashSet<string> AllEverVisible(World w)
        {
            HashSet<string> seen = new HashSet<string>();
            Lockscreen s = Screen();
            foreach (Card c in s.Cards)
            {
                int[] win = s.WindowOf(c.def.id, w);
                if (win == null) continue;
                if (win[1] > Data.Phone.foundAtMin) seen.Add(c.def.id);
            }
            return seen;
        }
    }
}
