using System;
using System.Collections.Generic;
using SilentBaton.Data;

namespace SilentBaton.Sim
{
    /// <summary>
    /// 다음 날 신문에 실린 단평 하나. **연주 중에는 이 중 아무것도 알 수 없다.**
    /// </summary>
    public sealed class ReviewResult
    {
        public int Accuracy;
        public int Ensemble;
        public int Dynamics;
        public int Total;
        public string GradeId;
        public string GradeName;
        public string Headline;
        public string Body;
        public bool Passed;
        /// <summary>지적한 것들. 이것이 다음 회차의 사전 지식이 된다.</summary>
        public List<PressNote> Notes = new List<PressNote>();

        public string OneLine
        {
            get
            {
                string s = GradeName + " " + Total + "점 (정확 " + Accuracy + " · 합주 " + Ensemble
                           + " · 세기 " + Dynamics + ")";
                foreach (PressNote n in Notes) s += " · " + n.Text;
                return s;
            }
        }
    }

    public sealed class PressNote
    {
        public string Kind;
        public string SectionId;
        public string SecondSectionId;
        public string Text;
        /// <summary>그 무리가 늘어졌는지(+) 앞질렀는지(-). 다음 회차가 이 부호를 쓴다.</summary>
        public int SignedMs;
        public int SignedLevel;
    }

    /// <summary>
    /// 평론을 쓴다. **연주가 끝난 뒤에만 부를 수 있다** — 그게 이 PoC의 규칙이다.
    /// 지휘자가 연주 중에 이 함수를 부를 길은 없다(`IConductor` 가 결과를 받지 않는다).
    /// </summary>
    public static class Review
    {
        public static ReviewResult Write(GameData d, PerformanceResult r)
        {
            ReviewBalance b = d.Balance.review;
            int carry = 0;
            int accPenalty = Ratio.Scale(r.MeanAbsOffsetMs, b.accuracyPerMsPercent, ref carry);
            int ensPenalty = Ratio.Scale(r.MeanSpreadMs, b.ensemblePerMsPercent, ref carry);
            int dynPenalty = Ratio.Scale(r.MeanAbsDynError, b.dynPerLevelPercent, ref carry);

            ReviewResult v = new ReviewResult
            {
                Accuracy = Ratio.Clamp(100 - accPenalty, 0, 100),
                Ensemble = Ratio.Clamp(100 - ensPenalty, 0, 100),
                Dynamics = Ratio.Clamp(100 - dynPenalty, 0, 100)
            };
            int wc = 0;
            v.Total = Ratio.Scale(v.Accuracy, b.weightAccuracy, ref wc)
                      + Ratio.Scale(v.Ensemble, b.weightEnsemble, ref wc)
                      + Ratio.Scale(v.Dynamics, b.weightDynamics, ref wc);
            v.Passed = v.Total >= b.passTotal;

            GradeDef g = d.GradeOf(v.Total);
            v.GradeId = g.id; v.GradeName = g.name; v.Headline = g.headline; v.Body = g.body;
            v.Notes = Notes(d, r);
            return v;
        }

        /// <summary>
        /// 지적을 뽑는다. 회차당 최대 셋 — 신문 한 단이 그 이상 실리지 않는다.
        /// 이 목록이 **다음 회차의 유일한 사전 지식**이다.
        /// </summary>
        private static List<PressNote> Notes(GameData d, PerformanceResult r)
        {
            ReviewBalance b = d.Balance.review;
            List<PressNote> notes = new List<PressNote>();
            int n = d.SectionCount;
            int samplesPerSection = r.Samples == 0 ? 1 : Math.Max(1, r.Beats);

            // 템포 지적: 부호가 있는 평균 어긋남이 큰 무리를 위에서부터 최대 maxTempoNotes 개
            List<int> order = new List<int>();
            for (int i = 0; i < n; i++) order.Add(i);
            order.Sort(delegate (int x, int y)
            {
                int mx = Math.Abs(r.SignedOffsetSum[x] / samplesPerSection);
                int my = Math.Abs(r.SignedOffsetSum[y] / samplesPerSection);
                if (mx != my) return my - mx;
                return x - y;                       // 같으면 선언 순서 — 결정적이어야 한다
            });
            int taken = 0;
            foreach (int i in order)
            {
                if (taken >= b.maxTempoNotes) break;
                int mean = r.SignedOffsetSum[i] / samplesPerSection;
                if (Math.Abs(mean) < b.noteThresholdMs) continue;
                NoteDef def = d.NoteOfKind(mean > 0 ? NoteKinds.Lag : NoteKinds.Rush);
                notes.Add(new PressNote
                {
                    Kind = def.kind, SectionId = d.AllSections[i].id, SignedMs = mean,
                    Text = Localization.Text("press." + def.kind, def.text, d.AllSections[i].name)
                });
                taken++;
            }

            // 세기 지적
            int dWorst = -1, dWorstAbs = 0, dWorstMean = 0;
            for (int i = 0; i < n; i++)
            {
                int mean = r.SignedDynSum[i] / samplesPerSection;
                if (Math.Abs(mean) > dWorstAbs) { dWorstAbs = Math.Abs(mean); dWorst = i; dWorstMean = mean; }
            }
            if (dWorst >= 0 && dWorstAbs >= b.noteThresholdLevel)
            {
                NoteDef def = d.NoteOfKind(dWorstMean > 0 ? NoteKinds.Loud : NoteKinds.Soft);
                notes.Add(new PressNote
                {
                    Kind = def.kind, SectionId = d.AllSections[dWorst].id, SignedLevel = dWorstMean,
                    Text = Localization.Text("press." + def.kind, def.text, d.AllSections[dWorst].name)
                });
            }

            // 갈라짐 지적: 가장 멀리 떨어진 두 무리
            if (r.MeanSpreadMs >= b.splitThresholdMs)
            {
                int hi = -1, lo = -1, hiV = int.MinValue, loV = int.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    int mean = r.SignedOffsetSum[i] / samplesPerSection;
                    if (mean > hiV) { hiV = mean; hi = i; }
                    if (mean < loV) { loV = mean; lo = i; }
                }
                if (hi >= 0 && lo >= 0 && hi != lo)
                {
                    NoteDef def = d.NoteOfKind(NoteKinds.Split);
                    notes.Add(new PressNote
                    {
                        Kind = def.kind, SectionId = d.AllSections[hi].id, SecondSectionId = d.AllSections[lo].id,
                        Text = Localization.Text("press." + def.kind, def.text,
                                                 d.AllSections[hi].name, d.AllSections[lo].name)
                    });
                }
            }

            if (notes.Count == 0)
            {
                NoteDef def = d.NoteOfKind(NoteKinds.Clean);
                notes.Add(new PressNote { Kind = def.kind, Text = Localization.Text("press.clean", def.text) });
            }
            return notes;
        }
    }

    /// <summary>
    /// 지난 회차의 신문에서 얻은 사전 지식. **연주 중에 얻을 수 없는 유일한 정보다.**
    /// 지휘자는 이것을 첫 박부터 쓸 수 있다 — 그래서 뒤늦은 피드백이 벌이 아니라 규칙이 된다.
    /// </summary>
    public sealed class PressKnowledge
    {
        public int[] TraitEstimateMs;
        public int[] DynEstimate;
        public int ConcertsRead;
        private readonly int[] _sum;
        private readonly int[] _count;

        public PressKnowledge(int n)
        {
            TraitEstimateMs = new int[n];
            DynEstimate = new int[n];
            _sum = new int[n];
            _count = new int[n];
        }

        /// <summary>그 무리에 대해 신문이 몇 번 적었나. 0이면 아직 아무것도 모른다.</summary>
        public int NotesAbout(int sectionIndex) { return _count[sectionIndex]; }

        public static PressKnowledge Empty(int n) { return new PressKnowledge(n); }

        public PressKnowledge Copy()
        {
            PressKnowledge k = new PressKnowledge(TraitEstimateMs.Length) { ConcertsRead = ConcertsRead };
            TraitEstimateMs.CopyTo(k.TraitEstimateMs, 0);
            DynEstimate.CopyTo(k.DynEstimate, 0);
            _sum.CopyTo(k._sum, 0);
            _count.CopyTo(k._count, 0);
            return k;
        }

        /// <summary>
        /// 신문을 읽는다. 지적된 무리의 성향을 추정에 얹는다 —
        /// 평론은 "얼마나"를 ms 로 적어 주지 않으므로 **누적 어긋남에서 박 수를 나눠 되짚는다.**
        /// </summary>
        public void Read(GameData d, PerformanceResult r)
        {
            ConcertsRead++;
            int beats = Math.Max(1, r.Beats);
            foreach (PressNote note in r.Review.Notes)
            {
                if (string.IsNullOrEmpty(note.SectionId)) continue;
                int i = d.SectionIndex(note.SectionId);
                if (note.Kind == NoteKinds.Lag || note.Kind == NoteKinds.Rush)
                {
                    // 평균 어긋남에서 한 박 몫의 쏠림을 되짚는다. 지휘로 눌러 온 만큼
                    // 평균이 작게 나오므로 pressGainPercent 로 되돌린다.
                    int carry = 0;
                    int perBeat = Ratio.Scale(note.SignedMs, d.Balance.review.pressGainPercent, ref carry) / beats;
                    _sum[i] += perBeat;
                    _count[i]++;
                    TraitEstimateMs[i] = _sum[i] / _count[i];
                }
                else if (note.Kind == NoteKinds.Loud || note.Kind == NoteKinds.Soft)
                {
                    DynEstimate[i] = note.SignedLevel;
                }
            }
        }
    }
}
