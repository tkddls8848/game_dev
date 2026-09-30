using System;
using System.Collections.Generic;
using SilentBaton.Data;

namespace SilentBaton.Sim
{
    /// <summary>시즌 한 회차의 결과.</summary>
    public sealed class ConcertResult
    {
        public int Index;
        public string PieceId;
        public int Seed;
        public PerformanceResult Performance;
        public int Total { get { return Performance.Total; } }
        public bool Passed { get { return Performance.Passed; } }
    }

    /// <summary>
    /// 시즌 하나. 연주 여러 회, 그리고 그 사이에 **다음 날 신문**.
    ///
    /// 이 PoC가 빼앗은 것은 "연주 중에 성적을 아는 일"이다.
    /// 그 빼앗음이 벌이 아니라 규칙이 되려면 **뒤늦은 피드백에도 쓸모가 있어야 한다** —
    /// 신문이 알려 주는 성향이 다음 회차의 사전 지식이 되는 자리가 여기다.
    /// `DelayedFeedbackMatters` 가 신문을 읽는 시즌과 버리는 시즌을 견준다.
    /// </summary>
    public static class Season
    {
        public delegate IConductor ConductorFactory(PressKnowledge press);

        /// <summary>
        /// 시즌을 돌린다. `readPress` 가 false 면 신문을 읽지 않는다(대조군) — 매 회차 백지에서 시작한다.
        /// 곡은 목록을 돌아가며 고른다. 회차 씨드는 시즌 씨드에서 정해진다 — 정책과 무관하게 같다.
        /// </summary>
        public static List<ConcertResult> Run(GameData d, int seasonSeed, ConductorFactory factory,
                                              bool readPress, int[] traitOverrideMs = null)
        {
            List<ConcertResult> list = new List<ConcertResult>();
            PressKnowledge press = PressKnowledge.Empty(d.SectionCount);
            Random seeds = new Random(seasonSeed);
            int concerts = d.Balance.season.concerts;

            for (int c = 1; c <= concerts; c++)
            {
                PieceDef piece = d.AllPieces[(c - 1) % d.AllPieces.Length];
                int nightSeed = piece.seed + seeds.Next(1, 1000000);
                Performance perf = new Performance(d, piece, nightSeed);
                Players players = perf.Cast(seasonSeed, traitOverrideMs);
                IConductor conductor = factory(readPress ? press : PressKnowledge.Empty(d.SectionCount));
                PerformanceResult r = perf.Run(conductor, players);
                list.Add(new ConcertResult { Index = c, PieceId = piece.id, Seed = nightSeed, Performance = r });
                // **다음 날** 신문이 나온다. 연주가 끝난 뒤다.
                if (readPress) press.Read(d, r);
            }
            return list;
        }

        public static int MeanTotal(List<ConcertResult> season, int fromIndex = 1)
        {
            int sum = 0, n = 0;
            foreach (ConcertResult c in season) if (c.Index >= fromIndex) { sum += c.Total; n++; }
            return n == 0 ? 0 : sum / n;
        }

        public static int MeanAbsOffset(List<ConcertResult> season, int fromIndex = 1)
        {
            int sum = 0, n = 0;
            foreach (ConcertResult c in season)
                if (c.Index >= fromIndex) { sum += c.Performance.MeanAbsOffsetMs; n++; }
            return n == 0 ? 0 : sum / n;
        }

        public static int PassCount(List<ConcertResult> season)
        {
            int n = 0;
            foreach (ConcertResult c in season) if (c.Passed) n++;
            return n;
        }

        public static string OneLine(List<ConcertResult> season)
        {
            List<string> parts = new List<string>();
            foreach (ConcertResult c in season) parts.Add(c.Index + "회 " + c.Total + "(" + c.Performance.Review.GradeName + ")");
            return string.Join(" · ", parts);
        }
    }

    /// <summary>
    /// 성향 쏠림의 격자. `VisualCuesSuffice` 의 **등가류 전수**가 이것을 돌린다.
    ///
    /// 보이는 이력이 완전히 같은 숨은 상태들을 묶어서, 그 묶음의 **최악값**도 합격선을 넘는지 본다.
    /// 넘으면 "소리를 들어야만 알 수 있는 정보가 필수가 아니다"가 수치로 증명된다.
    /// 넘지 못하면 눈금을 좁히거나(데이터 수정) 이 컨셉이 성립하지 않는 것이다.
    /// </summary>
    public static class BiasGrid
    {
        /// <summary>
        /// **활이 보이지 않는 무리만** 격자로 훑는다.
        ///
        /// 활이 보이는 무리(현·타)의 쏠림은 3~4ms 눈금의 활 각도로 거의 그대로 드러난다 —
        /// 성향을 1ms 만 흔들어도 지휘자가 본 것이 달라지므로 등가류가 만들어지지 않는다.
        /// **소리로만 구별되는 여지는 활이 없는 무리(목관·금관)에 있다.** 거기를 전수한다.
        /// 활이 보이는 무리는 씨드가 뽑은 값(baseTrait)을 그대로 둔다.
        /// </summary>
        public static List<int[]> BowlessOnly(GameData d, int step, int[] baseTrait)
        {
            List<int[]> list = new List<int[]> { (int[])baseTrait.Clone() };
            for (int i = 0; i < d.SectionCount; i++)
            {
                if (d.AllSections[i].bowVisible) continue;
                int range = d.AllSections[i].biasRangeMs;
                List<int[]> next = new List<int[]>();
                for (int v = -range; v <= range; v += step)
                    foreach (int[] prefix in list)
                    {
                        int[] copy = (int[])prefix.Clone();
                        copy[i] = v;
                        next.Add(copy);
                    }
                list = next;
            }
            return list;
        }

        public static List<int[]> All(GameData d, int step)
        {
            List<int[]> list = new List<int[]> { new int[d.SectionCount] };
            for (int i = 0; i < d.SectionCount; i++)
            {
                int range = d.AllSections[i].biasRangeMs;
                List<int[]> next = new List<int[]>();
                for (int v = -range; v <= range; v += step)
                    foreach (int[] prefix in list)
                    {
                        int[] copy = (int[])prefix.Clone();
                        copy[i] = v;
                        next.Add(copy);
                    }
                list = next;
            }
            return list;
        }

        public static string Key(int[] v)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (int x in v) sb.Append(x).Append(',');
            return sb.ToString();
        }
    }
}
