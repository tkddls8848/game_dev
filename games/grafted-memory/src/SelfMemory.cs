using System.Collections.Generic;
using Graft.Data;

namespace Graft.Sim
{
    /// <summary>한 기억을 뺀 눈. 그 기억이 제 이웃들과 어긋나는 양을 재려면 자기 자신은 빼야 한다.</summary>
    public sealed class ExcludingView : IMemoryView
    {
        private readonly IMemoryView _inner;
        private readonly string _hidden;
        private readonly List<string> _ids = new List<string>();

        public ExcludingView(IMemoryView inner, string hiddenId)
        {
            _inner = inner;
            _hidden = hiddenId;
            foreach (string id in inner.KnownIds) if (id != hiddenId) _ids.Add(id);
        }

        public IList<string> KnownIds { get { return _ids; } }
        public bool Knows(string id) { return id != _hidden && _inner.Knows(id); }
        public int Day(string id) { return _inner.Day(id); }
        public int Span(string id) { return _inner.Span(id); }
        public string Place(string id) { return _inner.Place(id); }
        public string Mood(string id) { return _inner.Mood(id); }
        public int Intensity(string id) { return _inner.Intensity(id); }
        public int Fixity(string id) { return _inner.Fixity(id); }
        public IList<string> People(string id) { return _inner.People(id); }
    }

    public sealed class SelfSignature
    {
        public string MemoryId = "";
        public string Name = "";
        /// <summary>이 기억이 제 이웃들과 어긋나는 총량(세기까지 다 센 것). 보고용이다.</summary>
        public int Residual;
        /// <summary>
        /// 흔적으로 세는 떨림만의 총합. **세기(IntensityClash)는 세지 않는다** —
        /// 기억이 얼마나 큰가는 살면서 저절로 바뀌고, 스크린 값도 없다. 흔적이 아니다.
        /// </summary>
        public int SignatureResidual;
        /// <summary>
        /// 흔적 떨림에서 가장 센 하나를 뺀 나머지. **이것이 흔적이다.**
        /// 가장 가까운 기억과 정서가 다른 것은 삶이 그런 것이다 — 자연스러운 기억망에도 흔하다.
        /// 유별난 것은 **두 번째 어긋남**이다. 심은 기억은 자리와 시절 둘 다에 걸린다.
        /// </summary>
        public int Secondary;
        /// <summary>흔적으로 센 떨림의 개수.</summary>
        public int TrembleEdges;
        public bool Suspect;
        /// <summary>데이터가 알고 있는 진실. 검사기는 이 값을 보지 않고 판정한 뒤 나중에 맞춰 본다.</summary>
        public bool ActuallyGrafted;
        public string AnchorMemoryId = "";
        public readonly List<Tremor> Tremors = new List<Tremor>();
    }

    /// <summary>
    /// **마지막 장면을 기계가 판정할 수 있게 만든 것.**
    ///
    /// 「플레이어 자신의 기억도 누가 심은 것인지 의심하게 된다」를 연출 문구로 두지 않았다.
    /// 심은 기억은 흔적을 남긴다 — 제 이웃들과 논리·정서가 어긋난 채로 굳어 있다.
    /// 그 흔적을 **의뢰에 쓰는 것과 똑같은 규칙**으로 잰다. 자로 남을 재던 것이 자기를 재는 자가 된다.
    ///
    /// 검사기(SelfSuspicionTests)가 보는 것:
    ///   · 내 기억망에서 심어진 노드를 정확히 골라내는가 (graftedOnDay 는 보지 않고 판정한다)
    ///   · 의뢰인의 기억망(심어진 것이 없다)에서는 하나도 고르지 않는가  ← 음성 대조군
    /// </summary>
    public static class SelfMemory
    {
        public static List<SelfSignature> Scan(GameData d, SubjectDef subject)
        {
            MemoryNet net = new MemoryNet(subject);
            MoodModel moods = new MoodModel(d);
            TruthView truth = new TruthView(net, new List<string>());   // 제 기억망에는 스크린을 가정하지 않는다
            SelfSignatureDef sig = d.Balance.selfSignature;
            List<SelfSignature> all = new List<SelfSignature>();

            foreach (MemoryDef m in subject.memories)
            {
                string anchor = StrongestNeighbour(net, m.id);
                SelfSignature s = new SelfSignature
                {
                    MemoryId = m.id, Name = m.name, ActuallyGrafted = m.graftedOnDay >= 0, AnchorMemoryId = anchor
                };
                if (anchor.Length == 0) { all.Add(s); continue; }

                CommissionDef asIf = AsIfCommission(m);
                GraftChoice choice = new GraftChoice
                {
                    AnchorMemoryId = anchor, PlaceId = m.placeId, DayIndex = m.dayIndex,
                    MoodId = m.moodId, IntensityPercent = m.intensityPercent
                };
                GraftVerdict v = GraftRules.Evaluate(d, net, moods, asIf, choice,
                                                     new ExcludingView(truth, m.id), true);
                s.Residual = v.TotalTremor;
                s.Tremors.AddRange(v.Tremors);

                int worst = 0;
                foreach (Tremor t in v.Tremors)
                {
                    if (t.Rule == TremorRules.IntensityClash) continue;   // 세기는 흔적이 아니다
                    s.SignatureResidual += t.Strength;
                    s.TrembleEdges++;
                    if (t.Strength > worst) worst = t.Strength;
                }
                s.Secondary = s.SignatureResidual - worst;
                all.Add(s);
            }

            foreach (SelfSignature s in all)
                s.Suspect = s.TrembleEdges >= sig.minTrembleEdges && s.Secondary >= sig.secondaryFloor;
            return all;
        }

        /// <summary>둘째 떨림의 중앙값. 진단용 — 판정에는 쓰지 않는다.</summary>
        public static int MedianSecondary(List<SelfSignature> all)
        {
            List<int> xs = new List<int>();
            foreach (SelfSignature s in all) xs.Add(s.Secondary);
            if (xs.Count == 0) return 0;
            xs.Sort();
            return xs[(xs.Count - 1) / 2];
        }

        /// <summary>
        /// 이 기억을 「지금 심는다면」의 의뢰서. 규칙 코드를 한 줄도 베끼지 않기 위한 껍데기다 —
        /// 같은 자를 써야 「같은 규칙으로 나를 재고 있다」가 성립한다.
        /// </summary>
        private static CommissionDef AsIfCommission(MemoryDef m)
        {
            return new CommissionDef
            {
                id = "self:" + m.id,
                spanDays = m.spanDays,
                requiredPeopleIds = m.peopleIds ?? new string[0],
                allowedPlaceIds = new[] { m.placeId },
                allowedMoodIds = new[] { m.moodId },
                dayWindowStart = m.dayIndex, dayWindowEnd = m.dayIndex,
                intensityMin = m.intensityPercent, intensityMax = m.intensityPercent, intensityStep = 1,
                toleranceBudget = int.MaxValue / 4, singleEdgeCap = int.MaxValue / 4,
                lucidityBudget = 0, seed = 0
            };
        }

        /// <summary>가장 센 연상 — 값이 가장 싼 간선. 같으면 id 순으로 (결정적이어야 한다).</summary>
        public static string StrongestNeighbour(MemoryNet net, string memoryId)
        {
            string best = "";
            int bestCost = int.MaxValue;
            foreach (LinkDef l in net.LinksOf(memoryId))
            {
                string o = net.Other(l, memoryId);
                if (l.probeCost < bestCost || (l.probeCost == bestCost && string.CompareOrdinal(o, best) < 0))
                { best = o; bestCost = l.probeCost; }
            }
            return best;
        }

        public static List<SelfSignature> Suspects(List<SelfSignature> all)
        {
            List<SelfSignature> s = new List<SelfSignature>();
            foreach (SelfSignature x in all) if (x.Suspect) s.Add(x);
            return s;
        }
    }
}
