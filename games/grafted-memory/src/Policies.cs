using System;
using System.Collections.Generic;
using Graft.Data;

namespace Graft.Sim
{
    /// <summary>한 번의 시도. 무엇을 쥐고 무엇을 골랐고 꿈이 어떻게 답했는가.</summary>
    public sealed class Attempt
    {
        public string PolicyName = "";
        public string CommissionId = "";
        public int Trial;
        public DreamSession Session;
        public ProbeKnowledge Knowledge;
        public GraftChoice Choice;
        /// <summary>플레이어가 쥔 것으로 미리 잰 결과.</summary>
        public GraftVerdict Predicted;
        /// <summary>꿈이 실제로 낸 결과.</summary>
        public GraftVerdict Truth;

        public bool Accepted { get { return Truth != null && Truth.Accepted; } }
        /// <summary>예측과 실제가 갈렸다. 여기가 감사기가 들여다보는 자리다.</summary>
        public bool Surprised { get { return Predicted != null && Truth != null && Predicted.Accepted != Truth.Accepted; } }
    }

    /// <summary>
    /// 정책들. **정책이 데이터가 아니라 코드인 이유**: 검사기가 「따져 심는 것이 이득인가」를 물으려면
    /// 두 정책이 같은 씨드·같은 세계에서 돌아야 하고, 정책 자체가 비교 대상이기 때문이다.
    ///
    /// 이름                          하는 일
    /// ─────────────────────────────────────────────────────────────────────────
    /// Reckless        아무것도 캐묻지 않고 처음 떠 있는 기억에 아무렇게나 끼운다
    /// Careful         의뢰 창 주변의 기억만 골라 캐묻고, 스크린 후보는 겹쳐 물어 확인한 뒤 가장 덜 떨리는 자리를 고른다
    /// ProbeEverything 싼 것부터 닥치는 대로 캐묻는다 — 명료도가 먼저 마르고 정작 필요한 기억을 못 본다
    /// LiarTrusting    캐묻기는 하지만 **겹쳐 묻지 않는다.** 스크린 기억을 그대로 믿는다
    /// Fixed*          한 가지 수를 늘 반복한다 (NoDominantStrategy 가 비교 대상으로 쓴다)
    /// </summary>
    public static class Policies
    {
        public static Attempt Reckless(DreamSession s, int trial)
        {
            ProbeKnowledge k = s.FreshKnowledge();
            Random rng = new Random(unchecked(s.Seed * 31 + 11));
            CommissionDef c = s.Commission;
            IList<string> anchors = k.KnownIds;

            GraftChoice choice = new GraftChoice
            {
                AnchorMemoryId = anchors[rng.Next(anchors.Count)],
                PlaceId = c.allowedPlaceIds[rng.Next(c.allowedPlaceIds.Length)],
                DayIndex = c.dayWindowStart + rng.Next((c.dayWindowEnd - c.dayWindowStart) / s.Data.Balance.dayStep + 1) * s.Data.Balance.dayStep,
                MoodId = c.allowedMoodIds[rng.Next(c.allowedMoodIds.Length)],
                IntensityPercent = c.intensityMin + rng.Next((c.intensityMax - c.intensityMin) / c.intensityStep + 1) * c.intensityStep
            };
            return Finish("Reckless", s, trial, k, choice);
        }

        public static Attempt Careful(DreamSession s, int trial)
        {
            ProbeKnowledge k = s.FreshKnowledge();
            ProbeRelevant(s, k, true);
            GraftChoice choice = BestChoice(s, k, null, -1, null);
            return Finish("Careful", s, trial, k, choice);
        }

        public static Attempt LiarTrusting(DreamSession s, int trial)
        {
            ProbeKnowledge k = s.FreshKnowledge();
            ProbeRelevant(s, k, false);     // 겹쳐 묻지 않는다
            GraftChoice choice = BestChoice(s, k, null, -1, null);
            return Finish("LiarTrusting", s, trial, k, choice);
        }

        /// <summary>
        /// **감사기의 어려운 갈래를 일부러 켜는 정책.**
        ///
        /// 스크린 후보를 한 길로만 띄운 뒤 **그 기억을 이어 붙일 자리로 고른다.** 겉값을 그대로 믿는다.
        /// 이런 정책이 없으면 DistortedButCheckable 판정이 한 번도 일어나지 않고,
        /// 그러면 「스크린 기억으로 죽는 것은 공정한가」가 통과했는지 손대지 않았는지 알 수 없다.
        /// tactics-whisper-map 의 AskedLiarsFirst 가 같은 일을 한다.
        /// </summary>
        public static Attempt TrustDistorted(DreamSession s, int trial)
        {
            ProbeKnowledge k = s.FreshKnowledge();
            ProbeRelevant(s, k, false);

            string mark = "";
            foreach (MemoryDef m in s.Net.Memories)
            {
                if (!m.distortable || !k.Knows(m.id) || k.Corroborated(m.id)) continue;
                if (mark.Length == 0 || string.CompareOrdinal(m.id, mark) < 0) mark = m.id;
            }

            GraftChoice choice = mark.Length == 0
                ? BestChoice(s, k, null, -1, null)
                : BestChoiceAnchoredOn(s, k, mark);
            return Finish("TrustDistorted", s, trial, k, choice);
        }

        /// <summary>이어 붙일 자리를 못 박고 나머지만 고른다.</summary>
        private static GraftChoice BestChoiceAnchoredOn(DreamSession s, ProbeKnowledge k, string anchorId)
        {
            CommissionDef c = s.Commission;
            List<GraftChoice> all = GraftRules.LegalChoices(c, new List<string> { anchorId }, s.Data.Balance.dayStep);
            GraftChoice best = null;
            int bestScore = int.MaxValue;
            string bestKey = null;
            foreach (GraftChoice ch in all)
            {
                GraftVerdict v = s.Predict(k, ch);
                int score = v.Blatant ? 1000000000 + v.TotalTremor : v.TotalTremor;
                string key = ch.ToString();
                if (best == null || score < bestScore || (score == bestScore && string.CompareOrdinal(key, bestKey) < 0))
                { best = ch; bestScore = score; bestKey = key; }
            }
            return best;
        }

        public static Attempt ProbeEverything(DreamSession s, int trial)
        {
            ProbeKnowledge k = s.FreshKnowledge();
            while (true)
            {
                Hop h = CheapestHop(s.Net, k, null);
                if (h == null || h.Cost > k.LucidityLeft) break;
                if (!k.Probe(h.From, h.To)) break;
            }
            GraftChoice choice = BestChoice(s, k, null, -1, null);
            return Finish("ProbeEverything", s, trial, k, choice);
        }

        /// <summary>
        /// 한 가지 수를 늘 반복하는 정책. 캐묻기는 Careful 과 똑같이 하고 **고르는 것만** 굳힌다 —
        /// 그래야 NoDominantStrategy 가 「고르는 일」 자체의 값을 재는 것이 된다.
        /// </summary>
        public static Attempt Fixed(DreamSession s, int trial, string name,
                                    string forcedMoodId, int forcedIntensity, string dayRule)
        {
            ProbeKnowledge k = s.FreshKnowledge();
            ProbeRelevant(s, k, true);
            GraftChoice choice = BestChoice(s, k, forcedMoodId, forcedIntensity, dayRule);
            return Finish(name, s, trial, k, choice);
        }

        // ── 캐묻기 ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 의뢰 창에 닿는 기억만 골라 캐묻는다. 무엇이 「닿는가」는 적힌 규칙에서 바로 나온다 —
        /// 시간이 겹칠 수 있거나 그 시절(eraWindowDays) 안에 있는 기억이다.
        /// corroborate 가 켜지면 스크린 후보를 서로 다른 길로 한 번 더 물어 참값을 본다.
        /// </summary>
        public static void ProbeRelevant(DreamSession s, ProbeKnowledge k, bool corroborate)
        {
            List<string> want = RelevantMemories(s);
            if (corroborate)
            {
                // **스크린 후보의 이웃까지 길에 넣는다.** 넣지 않으면 겹쳐 물을 둘째 이웃이 영원히 안 뜨고,
                // 겹쳐 묻는 정책이 겹쳐 묻지 못한 채로 「따져 심었다」고 주장하게 된다.
                // 진단이 잡아낸 것: com-hospital-vow 에서 병원 복도를 한 번도 확인할 수 없어 Careful 이 31% 였다.
                List<string> extra = new List<string>();
                foreach (string id in want)
                {
                    if (!s.Net.Memory(id).distortable) continue;
                    foreach (string nb in s.Net.NeighboursOf(id))
                        if (!want.Contains(nb) && !extra.Contains(nb)) extra.Add(nb);
                }
                want.AddRange(extra);
            }
            while (true)
            {
                Hop h = CheapestHop(s.Net, k, want);
                if (h != null && h.Cost <= k.LucidityLeft) { if (k.Probe(h.From, h.To)) continue; }
                if (!corroborate) break;

                Hop c = CheapestCorroboration(s, k, want);
                if (c == null || c.Cost > k.LucidityLeft) break;
                if (!k.Probe(c.From, c.To)) break;
            }
        }

        /// <summary>규칙이 실제로 들여다볼 수 있는 기억. 이 바깥의 기억은 떨림에 끼지 못한다.</summary>
        public static List<string> RelevantMemories(DreamSession s)
        {
            CommissionDef c = s.Commission;
            int era = s.Data.Balance.eraWindowDays;
            List<string> ids = new List<string>();
            foreach (MemoryDef m in s.Net.Memories)
            {
                int mEnd = m.dayIndex + m.spanDays - 1;
                if (mEnd < c.dayWindowStart - era) continue;
                if (m.dayIndex > c.dayWindowEnd + era) continue;
                ids.Add(m.id);
            }
            return ids;
        }

        public sealed class Hop { public string From = ""; public string To = ""; public int Cost; }

        /// <summary>
        /// 아직 안 뜬 기억 하나를 향한 **다음 한 걸음**. 여러 홉 떨어져 있으면 디딤돌부터 밟는다.
        /// want 가 null 이면 아무 기억이나 — ProbeEverything 이 쓴다.
        /// </summary>
        public static Hop CheapestHop(MemoryNet net, ProbeKnowledge k, IList<string> want)
        {
            Dictionary<string, ProbeRoute> routes = net.CheapestRoutes(k.KnownIds);
            Hop best = null;
            List<string> targets = new List<string>();
            foreach (MemoryDef m in net.Memories)
            {
                if (k.Knows(m.id)) continue;
                if (want != null && !want.Contains(m.id)) continue;
                targets.Add(m.id);
            }
            targets.Sort(StringComparer.Ordinal);

            foreach (string t in targets)
            {
                ProbeRoute r;
                if (!routes.TryGetValue(t, out r)) continue;
                Hop h = FirstUnknownHop(net, k, routes, t);
                if (h == null) continue;
                if (best == null || h.Cost < best.Cost) best = h;
            }
            return best;
        }

        private static Hop FirstUnknownHop(MemoryNet net, ProbeKnowledge k,
                                          Dictionary<string, ProbeRoute> routes, string target)
        {
            List<string> path = new List<string> { target };
            string cur = target;
            while (true)
            {
                ProbeRoute r;
                if (!routes.TryGetValue(cur, out r) || r.ViaMemoryId.Length == 0) break;
                cur = r.ViaMemoryId;
                path.Insert(0, cur);
                if (path.Count > 64) break;    // 고리 안전망
            }
            for (int i = 1; i < path.Count; i++)
            {
                if (k.Knows(path[i])) continue;
                int cost = LinkCost(net, path[i - 1], path[i]);
                if (cost < 0) return null;
                return new Hop { From = path[i - 1], To = path[i], Cost = cost };
            }
            return null;
        }

        /// <summary>스크린 후보를 **다른 길로** 한 번 더 묻는 가장 싼 걸음.</summary>
        public static Hop CheapestCorroboration(DreamSession s, ProbeKnowledge k, IList<string> want)
        {
            MemoryNet net = s.Net;
            Hop best = null;
            List<string> targets = new List<string>();
            foreach (MemoryDef m in net.Memories)
            {
                if (!m.distortable) continue;
                if (!k.Knows(m.id) || k.Corroborated(m.id)) continue;
                if (want != null && !want.Contains(m.id)) continue;
                targets.Add(m.id);
            }
            targets.Sort(StringComparer.Ordinal);

            foreach (string t in targets)
            {
                IList<string> used = k.ViaOf(t);
                foreach (string nb in net.NeighboursOf(t))
                {
                    if (!k.Knows(nb)) continue;
                    if (used.Contains(nb)) continue;
                    int cost = LinkCost(net, nb, t);
                    if (cost < 0) continue;
                    if (best == null || cost < best.Cost) best = new Hop { From = nb, To = t, Cost = cost };
                }
            }
            return best;
        }

        public static int LinkCost(MemoryNet net, string a, string b)
        {
            foreach (LinkDef l in net.LinksOf(a)) if (net.Other(l, a) == b) return l.probeCost;
            return -1;
        }

        // ── 고르기 ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 쥔 것으로 재서 가장 덜 떨리는 자리를 고른다. 같은 값이면 문자열 순으로 — 결정적이어야 한다.
        /// forcedMoodId / forcedIntensity / dayRule 이 주어지면 그 칸을 굳힌다(Fixed 정책).
        /// </summary>
        public static GraftChoice BestChoice(DreamSession s, ProbeKnowledge k,
                                             string forcedMoodId, int forcedIntensity, string dayRule)
        {
            CommissionDef c = s.Commission;
            List<GraftChoice> all = GraftRules.LegalChoices(c, k.KnownIds, s.Data.Balance.dayStep);
            GraftChoice best = null;
            int bestScore = int.MaxValue;
            string bestKey = null;

            foreach (GraftChoice ch in all)
            {
                if (forcedMoodId != null && ch.MoodId != forcedMoodId) continue;
                if (forcedIntensity >= 0 && ch.IntensityPercent != forcedIntensity) continue;
                if (dayRule == "earliest" && ch.DayIndex != c.dayWindowStart) continue;
                if (dayRule == "latest" && ch.DayIndex > c.dayWindowEnd - s.Data.Balance.dayStep) continue;

                GraftVerdict v = s.Predict(k, ch);
                int score = v.Blatant ? 1000000000 + v.TotalTremor : v.TotalTremor;
                string key = ch.ToString();
                if (best == null || score < bestScore || (score == bestScore && string.CompareOrdinal(key, bestKey) < 0))
                {
                    best = ch; bestScore = score; bestKey = key;
                }
            }
            if (best == null)   // 굳힌 칸 때문에 고를 것이 없으면 아무 합법 선택 하나
                best = all.Count > 0 ? all[0] : new GraftChoice
                {
                    AnchorMemoryId = k.KnownIds[0], PlaceId = c.allowedPlaceIds[0],
                    DayIndex = c.dayWindowStart, MoodId = c.allowedMoodIds[0], IntensityPercent = c.intensityMin
                };
            return best;
        }

        private static Attempt Finish(string name, DreamSession s, int trial, ProbeKnowledge k, GraftChoice choice)
        {
            return new Attempt
            {
                PolicyName = name, CommissionId = s.Commission.id, Trial = trial, Session = s,
                Knowledge = k, Choice = choice,
                Predicted = s.Predict(k, choice),
                Truth = s.Judge(choice)
            };
        }
    }
}
