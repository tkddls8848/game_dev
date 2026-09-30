using System;
using System.Collections.Generic;
using Phone.Data;

namespace Phone.Sim
{
    /// <summary>
    /// 켜고 끌 수 있는 세 가지. **핵 검사기는 이것들을 바꿔 가며 같은 사건을 다시 푼다.**
    ///   pushOut  : 넘치면 오래된 알림이 밀려 사라진다 (끄면 전부 남는다)
    ///   preview  : 미리보기 본문·EXIF 가 보인다 (끄면 알림이 왔다는 사실만 남는다)
    ///   unlocked : 잠금이 풀렸다 — 잠긴 쪽(본문 전문·통화 기록·사진첩·건강 앱)까지 본다
    /// </summary>
    public struct World
    {
        public bool pushOut;
        public bool preview;
        public bool unlocked;
        public static World Lockscreen { get { return new World { pushOut = true, preview = true, unlocked = false }; } }
        public static World NoPushOut { get { return new World { pushOut = false, preview = true, unlocked = false }; } }
        public static World NoPreview { get { return new World { pushOut = true, preview = false, unlocked = false }; } }
        public static World Unlocked { get { return new World { pushOut = false, preview = true, unlocked = true }; } }
        public string Name
        {
            get
            {
                return (unlocked ? "잠금품" : "잠김") + "·" + (pushOut ? "밀려남o" : "밀려남x") + "·" + (preview ? "미리보기o" : "미리보기x");
            }
        }
    }

    /// <summary>알림 하나가 실제로 화면에 놓인 자리.</summary>
    public sealed class Card
    {
        public NotifDef def;
        public int index;        // 도착 차례 (0부터)
    }

    /// <summary>
    /// 잠금 화면. 알림은 도착 차례로 쌓이고, capacity 를 넘으면 **오래된 것부터 밀려 사라진다.**
    /// 밀려난 것은 잠금을 풀 수 없으므로 **영영 못 본다** — 이 게임의 시간축이 여기서 나온다.
    /// </summary>
    public sealed class Lockscreen
    {
        public GameData D { get; private set; }
        public int Seed { get; private set; }
        public List<Card> Cards { get; private set; }   // 도착 순으로 정렬

        public static Lockscreen Build(GameData d) { return Build(d, d.Phone.seed); }

        public static Lockscreen Build(GameData d, int seed)
        {
            Lockscreen s = new Lockscreen();
            s.D = d; s.Seed = seed;
            List<NotifDef> all = new List<NotifDef>(d.Notifs.notifications);

            // 조사 중에 끼어드는 잡음. System.Random 만 쓴다(설계 원칙 5).
            NoiseSpec ns = d.Phone.noise;
            Random rng = new Random(seed);
            int count = ns.countMin + rng.Next(ns.countMax - ns.countMin + 1);
            int slots = (ns.toMin - ns.fromMin) / ns.gridMin;
            for (int i = 0; i < count; i++)
            {
                NoiseLine line = ns.lines[rng.Next(ns.lines.Count)];
                int at = ns.fromMin + rng.Next(slots + 1) * ns.gridMin;
                all.Add(new NotifDef
                {
                    id = "n_noise_" + (i + 1).ToString("D2"),
                    kind = "noise", app = line.app, fromKo = line.from, previewKo = line.previewKo,
                    arriveMin = at, exifAtMin = -1, exifPlaceKo = null, investigation = true
                });
            }
            all.Sort(delegate (NotifDef a, NotifDef b)
            {
                int c = a.arriveMin.CompareTo(b.arriveMin);
                return c != 0 ? c : string.CompareOrdinal(a.id, b.id);
            });
            s.Cards = new List<Card>();
            for (int i = 0; i < all.Count; i++) s.Cards.Add(new Card { def = all[i], index = i });
            return s;
        }

        /// <summary>t 분에 화면에 남아 있는 알림들. 새것이 위다.</summary>
        public List<Card> Visible(int atMin, World w)
        {
            List<Card> outp = new List<Card>();
            int cap = D.Phone.stack.capacity;
            for (int i = 0; i < Cards.Count; i++)
            {
                if (Cards[i].def.arriveMin > atMin) continue;
                if (w.pushOut)
                {
                    int newer = 0;
                    for (int j = i + 1; j < Cards.Count; j++)
                        if (Cards[j].def.arriveMin <= atMin) newer++;
                    if (newer >= cap) continue;       // 밀려 사라졌다
                }
                outp.Add(Cards[i]);
            }
            outp.Reverse();
            return outp;
        }

        /// <summary>이 알림이 화면에 있었던 구간 [도착, 밀려남). 밀려나지 않으면 끝은 lastMinuteMin+1.</summary>
        public int[] WindowOf(string notifId, World w)
        {
            int idx = Cards.FindIndex(delegate (Card c) { return c.def.id == notifId; });
            if (idx < 0) return null;
            int from = Cards[idx].def.arriveMin;
            if (!w.pushOut) return new int[] { from, D.Phone.lastMinuteMin + 1 };
            int cap = D.Phone.stack.capacity, newer = 0;
            for (int j = idx + 1; j < Cards.Count; j++)
            {
                newer++;
                if (newer >= cap) return new int[] { from, Cards[j].def.arriveMin };
            }
            return new int[] { from, D.Phone.lastMinuteMin + 1 };
        }

        /// <summary>깨우기를 n번 하고 t 분에 읽은 잔량(천분율). 0 이하면 꺼진 것이다.</summary>
        public int BatteryAt(int atMin, int wakesSoFar)
        {
            BatteryDef b = D.Phone.battery;
            int steps = (atMin - D.Phone.foundAtMin) / b.drainStepMin;
            return b.startPermille - b.idleDrainPermillePerStep * steps - b.wakeCostPermille * wakesSoFar;
        }

        /// <summary>
        /// 앞서 wakesSoFar 번 깨웠을 때, 화면이 아직 켜지는 마지막 순간.
        /// 깨우기를 많이 할수록 앞당겨진다 — **이 함수가 이 게임의 예산 그 자체다.**
        /// </summary>
        public int LastLivingMinute(int wakesSoFar)
        {
            BatteryDef b = D.Phone.battery;
            int slack = b.startPermille - b.wakeCostPermille * wakesSoFar;
            if (slack <= 0) return D.Phone.foundAtMin - 1;
            int maxSteps = (slack - 1) / Math.Max(1, b.idleDrainPermillePerStep);
            int t = D.Phone.foundAtMin + maxSteps * b.drainStepMin + (b.drainStepMin - 1);
            return Math.Min(t, D.Phone.lastMinuteMin);
        }
    }
}
