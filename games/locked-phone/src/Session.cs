using System;
using System.Collections.Generic;
using Phone.Data;

namespace Phone.Sim
{
    /// <summary>화면을 깨운 한 순간.</summary>
    public sealed class Wake
    {
        public int atMin;
        public int batteryPermille;
        public List<Card> cards;
    }

    /// <summary>깨우기 일정 하나를 그대로 돌려 본 결과.</summary>
    public sealed class SessionResult
    {
        public List<Wake> wakes = new List<Wake>();
        public HashSet<string> seen = new HashSet<string>();
        public bool batteryRate;
        public HashSet<string> tokens = new HashSet<string>();
        public HashSet<string> facts = new HashSet<string>();
        public HashSet<string> verdicts = new HashSet<string>();
        public bool wentDark;              // 배터리가 먼저 죽었다

        public string Text
        {
            get
            {
                List<string> p = new List<string>();
                foreach (Wake k in wakes) p.Add(Clock(k.atMin) + "(" + (k.batteryPermille / 10) + "%)");
                return string.Join(" > ", p);
            }
        }

        public static string Clock(int m)
        {
            int h = (m / 60) % 24;
            return h.ToString("D2") + ":" + (m % 60).ToString("D2");
        }
    }

    /// <summary>
    /// 조사 한 판. 플레이어가 할 수 있는 일은 **화면을 깨우는 것 하나뿐**이다.
    /// 깨울 때마다 배터리가 줄고, 그동안에도 알림은 들어와 오래된 것을 밀어낸다.
    /// </summary>
    public sealed class Session
    {
        public readonly GameData D;
        public readonly Lockscreen S;
        public readonly World W;

        public Session(GameData d, Lockscreen s, World w) { D = d; S = s; W = w; }


        public SessionResult Run(IList<int> wakeMinutes)
        {
            SessionResult r = new SessionResult();
            int n = 0, lastAt = int.MinValue, firstAt = int.MinValue;
            foreach (int t in wakeMinutes)
            {
                if (t < D.Phone.foundAtMin || t > D.Phone.lastMinuteMin) continue;
                int level = S.BatteryAt(t, n);
                if (level <= 0) { r.wentDark = true; break; }
                n++;
                List<Card> cards = S.Visible(t, W);
                r.wakes.Add(new Wake { atMin = t, batteryPermille = level, cards = cards });
                foreach (Card c in cards) r.seen.Add(c.def.id);
                if (firstAt == int.MinValue) firstAt = t;
                lastAt = t;
            }
            r.batteryRate = r.wakes.Count >= 2 && (lastAt - firstAt) >= D.Phone.battery.rateMinGapMin;
            r.tokens = Deduction.Tokens(D, r.seen, r.batteryRate, W);
            r.facts = Deduction.Facts(D, r.tokens, W);
            r.verdicts = Deduction.Verdicts(D, r.facts);
            return r;
        }

        /// <summary>이 알림을 잡을 수 있는 순간들 (배터리가 살아 있는 동안).</summary>
        public List<int> CatchWindow(string notifId, int wakesBudget)
        {
            int[] w = S.WindowOf(notifId, W);
            List<int> outp = new List<int>();
            if (w == null) return outp;
            int last = S.LastLivingMinute(wakesBudget);
            for (int t = Math.Max(w[0], D.Phone.foundAtMin); t < w[1] && t <= last && t <= D.Phone.lastMinuteMin; t++)
                outp.Add(t);
            return outp;
        }

        /// <summary>
        /// 모든 정답을 세우는 **가장 적은 깨우기 수**. 필요한 알림들의 구간을 찌르는 문제라
        /// 탐욕(가장 일찍 닫히는 구간부터)으로 최소를 얻고, 배터리로 다시 걸러 확인한다.
        /// </summary>
        /// <summary>
        /// 모든 정답을 세우는 **가장 적은 깨우기 수**를 찾는다.
        /// 예산이 깨우기 수에 딸려 있어 순환이 생긴다 — 몇 번 깨울지가 마지막 깨울 수 있는 시각을 정하고,
        /// 그 시각이 다시 몇 번 깨워야 하는지를 정한다. 그래서 k=1,2,3… 로 **바깥에서 감는다.**
        /// </summary>
        public SessionResult MinimalSession()
        {
            SessionResult fallback = null;
            for (int k = 1; k <= D.Balance.checkers.maxPlanWakes; k++)
            {
                SessionResult r = PlanWithin(k);
                if (fallback == null || r.verdicts.Count > fallback.verdicts.Count) fallback = r;
                if (r.wakes.Count <= k && r.verdicts.Count == D.Case.verdicts.Count) return r;
            }
            return fallback;
        }

        /// <summary>깨우기를 k번 쓴다고 가정하고 구간 찌르기로 일정을 짠다.</summary>
        public SessionResult PlanWithin(int k)
        {
            List<string> need = Deduction.AllNeededNotifs(D);
            // 배터리가 허락하는 마지막 순간으로 구간을 자른다 — 계획을 짤 때 예산을 무시하면
            // "이론상 잡을 수 있다"는 답이 나오고, 그것은 이 게임의 답이 아니다.
            int ceiling = S.LastLivingMinute(k - 1);
            List<int[]> spans = new List<int[]>();
            foreach (string id in need)
            {
                int[] w = S.WindowOf(id, W);
                if (w == null) continue;
                int from = Math.Max(w[0], D.Phone.foundAtMin);
                int to = Math.Min(w[1] - 1, ceiling);
                if (to < from) continue;              // 예산 안에서는 닿을 수 없다 — 공정성 검사기가 따로 잡는다
                spans.Add(new int[] { from, to });
            }
            spans.Sort(delegate (int[] a, int[] b) { return a[1].CompareTo(b[1]); });

            List<int> picks = new List<int>();
            foreach (int[] sp in spans)
            {
                bool covered = false;
                foreach (int p in picks) if (p >= sp[0] && p <= sp[1]) { covered = true; break; }
                if (!covered) picks.Add(sp[1]);
            }
            picks.Sort();

            // 잔량 감소율을 재려면 충분히 벌어진 두 번이 있어야 한다
            int gap = D.Phone.battery.rateMinGapMin;
            if (picks.Count == 0) picks.Add(D.Phone.foundAtMin);
            if (picks.Count == 1 || picks[picks.Count - 1] - picks[0] < gap)
            {
                // 뒤로 더 미룰 수는 없다(구간이 이미 끝에 붙어 있다) — 앞쪽에 한 번을 더 둔다
                int extra = Math.Max(D.Phone.foundAtMin, picks[0] - gap);
                if (picks.Contains(extra)) extra = Math.Min(D.Phone.lastMinuteMin, picks[picks.Count - 1] + gap);
                if (!picks.Contains(extra)) picks.Add(extra);
                picks.Sort();
            }
            return Run(picks);
        }

        /// <summary>정해진 간격으로 기계적으로 깨우는 정책. NoDominantStrategy 가 견준다.</summary>
        public SessionResult Metronome(int everyMin, int startOffset)
        {
            List<int> ts = new List<int>();
            for (int t = D.Phone.foundAtMin + startOffset; t <= D.Phone.lastMinuteMin; t += everyMin) ts.Add(t);
            return Run(ts);
        }

        /// <summary>처음부터 배터리가 다할 때까지 쉬지 않고 깨우는 정책.</summary>
        public SessionResult BurnEarly(int everyMin)
        {
            List<int> ts = new List<int>();
            for (int t = D.Phone.foundAtMin; t <= D.Phone.lastMinuteMin; t += everyMin) ts.Add(t);
            return Run(ts);
        }

        /// <summary>끝까지 참았다가 마지막에 몰아 깨우는 정책.</summary>
        public SessionResult WaitThenBurn(int everyMin, int wakes)
        {
            int last = S.LastLivingMinute(wakes);
            List<int> ts = new List<int>();
            for (int i = wakes - 1; i >= 0; i--) ts.Add(last - i * everyMin);
            ts.Sort();
            return Run(ts);
        }
    }
}
