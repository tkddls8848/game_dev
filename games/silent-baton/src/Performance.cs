using System;
using System.Collections.Generic;
using SilentBaton.Data;

namespace SilentBaton.Sim
{
    /// <summary>
    /// 단원들의 **숨은** 상태. 지휘자는 이것을 볼 수 없다 — `VisibleFrame` 만 본다.
    /// 소리가 들리는 세계(대조군)의 지휘자만 이것을 그대로 읽는다.
    /// </summary>
    public sealed class Players
    {
        public int[] OffsetMs;      // 누적 어긋남. 양수면 늦었다
        public int[] DeltaMs;       // 지난 한 박 사이의 변화
        public int[] Dyn;           // 실제 세기
        public int[] BiasMs;        // 이 회차의 쏠림 (성향 + 그날의 몫)
        public int[] TraitBiasMs;   // 성향만. 시즌 내내 같다
        public bool[] Playing;
        public int[] DriftCarry;
        public int[] NudgeCarry;
        public int[] DynCarry;
        public int[] StrainCarry;

        public Players(int n)
        {
            OffsetMs = new int[n]; DeltaMs = new int[n]; Dyn = new int[n];
            BiasMs = new int[n]; TraitBiasMs = new int[n]; Playing = new bool[n];
            DriftCarry = new int[n]; NudgeCarry = new int[n]; DynCarry = new int[n];
            StrainCarry = new int[n];
        }

        public Players Copy()
        {
            Players p = new Players(OffsetMs.Length);
            OffsetMs.CopyTo(p.OffsetMs, 0); DeltaMs.CopyTo(p.DeltaMs, 0); Dyn.CopyTo(p.Dyn, 0);
            BiasMs.CopyTo(p.BiasMs, 0); TraitBiasMs.CopyTo(p.TraitBiasMs, 0); Playing.CopyTo(p.Playing, 0);
            DriftCarry.CopyTo(p.DriftCarry, 0); NudgeCarry.CopyTo(p.NudgeCarry, 0); DynCarry.CopyTo(p.DynCarry, 0);
            StrainCarry.CopyTo(p.StrainCarry, 0);
            return p;
        }
    }

    /// <summary>한 박에 내린 지휘 하나. 지휘봉은 한 곳을 가리킨다.</summary>
    public struct Baton
    {
        public int SectionIndex;
        public string CueId;
        public Baton(int section, string cueId) { SectionIndex = section; CueId = cueId; }
        public override string ToString() { return SectionIndex + ":" + CueId; }
    }

    /// <summary>한 박의 기록. 목업이 이것을 그대로 그린다.</summary>
    public sealed class BeatRecord
    {
        public int Bar;
        public int Beat;
        public int BeatMs;
        public int RequiredDynamic;
        public VisibleFrame Seen;     // 지휘자가 그 순간 **본 것**
        public Baton Given;
        public int[] OffsetAfter;
        public int[] DynAfter;
        public int SpreadMs;
        public int AbsOffsetSum;
        public int AbsDynSum;
        public int PlayingCount;
    }

    /// <summary>
    /// 연주 한 번. **소리가 없다.**
    ///
    /// 한 박의 순서:
    ///   (1) 지휘자가 `VisibleFrame` 을 본다 — 보이는 셋뿐이다
    ///   (2) 지휘봉을 한 무리에 준다
    ///   (3) 그 무리가 responsiveness 만큼 따라온다
    ///   (4) 모든 무리가 자기 쏠림만큼 벌어지고 세기가 습관으로 돌아간다
    ///   (5) 그 박의 어긋남이 기록된다 — **지휘자에게는 알려 주지 않는다**
    ///
    /// 성적은 연주가 끝나고 **다음 날 신문**으로만 온다.
    /// </summary>
    public sealed class Performance
    {
        private readonly GameData _d;
        private readonly PieceDef _piece;
        private readonly int _seed;

        public Performance(GameData d, PieceDef piece, int seed)
        {
            _d = d; _piece = piece; _seed = seed;
        }

        public GameData Data { get { return _d; } }
        public PieceDef Piece { get { return _piece; } }
        public int Seed { get { return _seed; } }

        /// <summary>
        /// 이 회차의 단원 상태를 뽑는다.
        /// **성향**(traitBias)은 시즌 씨드가 정하므로 회차가 바뀌어도 같고,
        /// **그날의 몫**은 회차 씨드가 정하므로 매번 다르다.
        /// 신문이 알려 주는 것은 주로 성향이고, 그래서 신문을 읽으면 다음 회차가 낫다.
        /// </summary>
        public Players Cast(int seasonSeed, int[] traitOverrideMs = null)
        {
            Players p = new Players(_d.SectionCount);
            Random trait = new Random(seasonSeed);
            for (int i = 0; i < _d.SectionCount; i++)
            {
                int range = _d.AllSections[i].biasRangeMs;
                p.TraitBiasMs[i] = trait.Next(-range, range + 1);
            }
            if (traitOverrideMs != null)
                for (int i = 0; i < _d.SectionCount && i < traitOverrideMs.Length; i++)
                    p.TraitBiasMs[i] = traitOverrideMs[i];

            Random night = new Random(_seed);
            int nr = _d.Balance.drift.nightlyBiasRangeMs;
            int jitter = _d.Balance.drift.dynStartJitter;
            for (int i = 0; i < _d.SectionCount; i++)
            {
                p.BiasMs[i] = p.TraitBiasMs[i] + night.Next(-nr, nr + 1);
                p.Dyn[i] = _d.AllSections[i].dynHabit + night.Next(-jitter, jitter + 1);
                // 무대에 오른 순간 이미 어긋나 있다. 쏠림을 지워도 이 몫은 남고 **호흡으로만 보인다.**
                int so = _d.Balance.drift.startOffsetRangeMs;
                p.OffsetMs[i] = night.Next(-so, so + 1);
                p.DeltaMs[i] = 0;
                p.Playing[i] = false;
            }
            return p;
        }

        /// <summary>연주를 끝까지 돌린다. 지휘자는 `IConductor` 다 — 무엇을 볼 수 있는지가 그것으로 갈린다.</summary>
        public PerformanceResult Run(IConductor conductor, Players players)
        {
            Players p = players.Copy();
            conductor.Begin(_d, _piece);
            List<BeatRecord> log = new List<BeatRecord>();
            int totalAbsOffset = 0, totalAbsDyn = 0, totalSpread = 0, samples = 0, beatCount = 0;
            int[] signedOffsetSum = new int[_d.SectionCount];
            int[] signedDynSum = new int[_d.SectionCount];
            int[] absOffsetSum = new int[_d.SectionCount];

            foreach (BarDef bar in _piece.bars)
            {
                if (bar.enteringSectionIds != null)
                    foreach (string id in bar.enteringSectionIds) p.Playing[_d.SectionIndex(id)] = true;
                int scale = _d.BeatScalePercent(bar);

                for (int beat = 1; beat <= bar.beats; beat++)
                {
                    beatCount++;
                    // (1) 지휘자가 본다 — **보이는 셋뿐이다**
                    VisibleFrame seen = VisibleFrame.From(_d, p, bar, beat);
                    // 귀를 구현한 지휘자에게만 진실이 건네진다 (대조군). 게임의 지휘자는 이 인터페이스가 없다.
                    IHearsEveryBeat ears = conductor as IHearsEveryBeat;
                    if (ears != null) ears.HearBeat(p, bar, beat);
                    // (2) 지휘봉
                    Baton baton = conductor.Decide(seen, bar, beat);
                    CueDef cue = _d.Cue(baton.CueId);
                    // (3) 그 무리가 따라온다
                    int t = baton.SectionIndex;
                    if (t >= 0 && t < _d.SectionCount && p.Playing[t])
                    {
                        SectionDef sec = _d.AllSections[t];
                        if (cue.tempoNudgeMs != 0)
                        {
                            int c = p.NudgeCarry[t];
                            p.OffsetMs[t] -= Ratio.Scale(cue.tempoNudgeMs, sec.responsivenessPercent, ref c);
                            p.NudgeCarry[t] = c;
                        }
                        if (cue.dynamicNudge != 0)
                        {
                            int c = p.DynCarry[t];
                            p.Dyn[t] += Ratio.Scale(cue.dynamicNudge, sec.responsivenessPercent, ref c);
                            p.DynCarry[t] = c;
                        }
                    }
                    // (4) 몸이 벌어지고 세기가 습관으로 돌아간다
                    for (int i = 0; i < _d.SectionCount; i++)
                    {
                        if (!p.Playing[i]) { p.DeltaMs[i] = 0; continue; }
                        int before = p.OffsetMs[i];
                        int c = p.DriftCarry[i];
                        p.OffsetMs[i] += Ratio.Scale(p.BiasMs[i], scale, ref c);
                        p.DriftCarry[i] = c;
                        // 무리하면 늘어진다. 세기가 어긋난 만큼 박이 뒤로 밀린다 —
                        // 그래서 표정을 보지 않으면 세기만 틀리는 것이 아니라 템포까지 무너진다.
                        int strain = Math.Abs(p.Dyn[i] - bar.requiredDynamic);
                        if (strain > 0)
                        {
                            int sc = p.StrainCarry[i];
                            p.OffsetMs[i] += Ratio.Scale(strain, _d.Balance.drift.strainPerDynErrorPercent, ref sc);
                            p.StrainCarry[i] = sc;
                        }
                        SectionDef sec = _d.AllSections[i];
                        int toward = sec.dynHabit;
                        if (p.Dyn[i] < toward) p.Dyn[i] = Math.Min(toward, p.Dyn[i] + sec.dynDriftPerBeat);
                        else if (p.Dyn[i] > toward) p.Dyn[i] = Math.Max(toward, p.Dyn[i] - sec.dynDriftPerBeat);
                        p.Dyn[i] = Ratio.Clamp(p.Dyn[i], 0, 100);
                        p.DeltaMs[i] = p.OffsetMs[i] - before;
                    }
                    // (5) 기록. **지휘자에게 알려 주지 않는다**
                    BeatRecord rec = new BeatRecord
                    {
                        Bar = bar.index, Beat = beat, BeatMs = bar.beatMs,
                        RequiredDynamic = bar.requiredDynamic, Seen = seen, Given = baton,
                        OffsetAfter = (int[])p.OffsetMs.Clone(), DynAfter = (int[])p.Dyn.Clone()
                    };
                    int lo = int.MaxValue, hi = int.MinValue, playing = 0;
                    for (int i = 0; i < _d.SectionCount; i++)
                    {
                        if (!p.Playing[i]) continue;
                        playing++;
                        int abs = Math.Abs(p.OffsetMs[i]);
                        int dynErr = Math.Abs(p.Dyn[i] - bar.requiredDynamic);
                        rec.AbsOffsetSum += abs; rec.AbsDynSum += dynErr;
                        totalAbsOffset += abs; totalAbsDyn += dynErr;
                        signedOffsetSum[i] += p.OffsetMs[i];
                        signedDynSum[i] += p.Dyn[i] - bar.requiredDynamic;
                        absOffsetSum[i] += abs;
                        samples++;
                        if (p.OffsetMs[i] < lo) lo = p.OffsetMs[i];
                        if (p.OffsetMs[i] > hi) hi = p.OffsetMs[i];
                    }
                    rec.PlayingCount = playing;
                    rec.SpreadMs = playing > 1 ? hi - lo : 0;
                    totalSpread += rec.SpreadMs;
                    log.Add(rec);
                }
                IHearsBarEnd barEars = conductor as IHearsBarEnd;
                if (barEars != null) barEars.HearBarEnd(p, bar);
            }

            PerformanceResult r = new PerformanceResult
            {
                PieceId = _piece.id, Seed = _seed, Beats = beatCount, Samples = samples,
                Log = log, Final = p, ConductorName = conductor.Name,
                MeanAbsOffsetMs = samples == 0 ? 0 : totalAbsOffset / samples,
                MeanAbsDynError = samples == 0 ? 0 : totalAbsDyn / samples,
                MeanSpreadMs = beatCount == 0 ? 0 : totalSpread / beatCount,
                SignedOffsetSum = signedOffsetSum, SignedDynSum = signedDynSum, AbsOffsetSum = absOffsetSum,
                TotalAbsOffsetMs = totalAbsOffset
            };
            r.Review = Review.Write(_d, r);
            return r;
        }
    }

    public sealed class PerformanceResult
    {
        public string PieceId;
        public int Seed;
        public int Beats;
        public int Samples;
        public List<BeatRecord> Log;
        public Players Final;
        public string ConductorName;
        public int MeanAbsOffsetMs;
        public int MeanAbsDynError;
        public int MeanSpreadMs;
        public int TotalAbsOffsetMs;
        public int[] SignedOffsetSum;
        public int[] SignedDynSum;
        public int[] AbsOffsetSum;
        public ReviewResult Review;

        public int Total { get { return Review.Total; } }
        public bool Passed { get { return Review.Passed; } }

        /// <summary>지휘자가 본 것 전부를 이어 붙인 열쇠. 등가류를 묶는 데 쓴다.</summary>
        public string SeenKey
        {
            get
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (BeatRecord b in Log) b.Seen.AppendKey(sb);
                return sb.ToString();
            }
        }

        /// <summary>지휘자가 내린 지휘 전부. 보이는 것이 같으면 이것도 같아야 한다.</summary>
        public string BatonKey
        {
            get
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (BeatRecord b in Log) sb.Append(b.Given).Append('/');
                return sb.ToString();
            }
        }
    }
}
