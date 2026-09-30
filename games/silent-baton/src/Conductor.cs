using System;
using SilentBaton.Data;

namespace SilentBaton.Sim
{
    /// <summary>
    /// 지휘자. **`Decide` 는 `VisibleFrame` 만 받는다** — 그게 이 게임의 규칙이다.
    /// 숨은 상태를 받고 싶은 지휘자는 아래 두 귀 인터페이스 중 하나를 따로 구현해야 하고,
    /// 그것을 구현하지 않은 지휘자에게는 `Performance` 가 진실을 건네줄 길이 없다.
    /// 검사기 `VisualCuesSuffice` 가 기대는 구조가 이것이다.
    /// </summary>
    public interface IConductor
    {
        string Name { get; }
        void Begin(GameData d, PieceDef piece);
        Baton Decide(VisibleFrame seen, BarDef bar, int beat);
    }

    /// <summary>소리가 들리는 세계의 지휘자만 구현한다 — 매 박 진실을 듣는다.</summary>
    public interface IHearsEveryBeat { void HearBeat(Players truth, BarDef bar, int beat); }

    /// <summary>즉시 피드백 세계의 지휘자만 구현한다 — 소절 끝마다 정확한 값을 듣는다.</summary>
    public interface IHearsBarEnd { void HearBarEnd(Players truth, BarDef bar); }

    /// <summary>
    /// 추정값 셋을 받아 지휘봉을 어디에 줄지 고른다.
    /// **모든 지휘자가 이 함수를 공유한다** — 그래야 세계를 견줄 때
    /// 달라지는 것이 "무엇을 알 수 있었나" 하나로 좁혀진다.
    /// </summary>
    public static class BatonPlanner
    {
        public static Baton Plan(GameData d, VisibleFrame seen,
                                 int[] estOffsetMs, int[] estRateMs, int[] estDynError)
        {
            ConductorBalance cb = d.Balance.conductor;
            int tempoTarget = -1, tempoUrgency = -1, predictAt = 0;
            int dynTarget = -1, dynUrgency = -1, dynAt = 0;

            for (int i = 0; i < d.SectionCount; i++)
            {
                if (!seen.Playing[i]) continue;
                int predict = estOffsetMs[i] + estRateMs[i] * cb.lookaheadBeats;
                int urg = Math.Abs(predict);
                if (urg > tempoUrgency) { tempoUrgency = urg; tempoTarget = i; predictAt = predict; }
                int dynErr = estDynError[i];
                if (Math.Abs(dynErr) > dynUrgency) { dynUrgency = Math.Abs(dynErr); dynTarget = i; dynAt = dynErr; }
            }
            if (tempoTarget < 0) return new Baton(0, "c_hold");

            int carry = 0;
            int dynAsMs = Ratio.Scale(dynUrgency, cb.dynMsPerUnitPercent, ref carry);
            // 몇 박마다 한 번은 세기를 먼저 본다. 이 규칙이 없으면 급한 것만 쫓다가
            // 표정 채널을 한 번도 쓰지 않는다 — 음영 대조군이 그것을 잡아냈다.
            bool dynTurn = cb.dynBeatEveryN > 0 && seen.Beat % cb.dynBeatEveryN == 0;
            if (dynTarget >= 0 && dynUrgency >= cb.dynUrgentUnits && (dynTurn || dynAsMs > tempoUrgency))
                return new Baton(dynTarget, dynAt > 0 ? "c_hush" : "c_swell");

            if (tempoUrgency < cb.minTempoActionMs) return new Baton(tempoTarget, "c_hold");

            // 실제로 움직이는 양은 nudge * responsiveness / 100 이다. 그러므로 필요한 nudge 는 그 역이다.
            int resp = d.AllSections[tempoTarget].responsivenessPercent;
            int wanted = resp == 0 ? predictAt : predictAt * 100 / resp;
            string best = "c_hold";
            int bestGap = int.MaxValue;
            foreach (CueDef c in d.AllCues)
            {
                if (c.IsDynamic) continue;
                int gap = Math.Abs(c.tempoNudgeMs - wanted);
                if (gap < bestGap) { bestGap = gap; best = c.id; }
            }
            return new Baton(tempoTarget, best);
        }
    }

    /// <summary>
    /// 소리가 들리는 세계의 지휘자 — **대조군이다.**
    /// 정확한 어긋남과 정확한 쏠림, 정확한 세기를 안다. 게임에는 이 지휘자가 없다.
    /// </summary>
    public sealed class HearingConductor : IConductor, IHearsEveryBeat
    {
        private GameData _d;
        private Players _truth;
        private int _scale = 100;
        private int[] _off, _rate, _dyn;

        public string Name { get { return Localization.Text("cond.hearing", "들리는 세계"); } }

        public void Begin(GameData d, PieceDef piece)
        {
            _d = d;
            _off = new int[d.SectionCount]; _rate = new int[d.SectionCount]; _dyn = new int[d.SectionCount];
        }

        public void HearBeat(Players truth, BarDef bar, int beat)
        {
            _truth = truth;
            _scale = _d.BeatScalePercent(bar);
        }

        public Baton Decide(VisibleFrame seen, BarDef bar, int beat)
        {
            int carry = 0;
            for (int i = 0; i < _d.SectionCount; i++)
            {
                _off[i] = _truth.OffsetMs[i];
                _rate[i] = Ratio.Scale(_truth.BiasMs[i], _scale, ref carry);
                _dyn[i] = _truth.Dyn[i] - bar.requiredDynamic;
            }
            return BatonPlanner.Plan(_d, seen, _off, _rate, _dyn);
        }
    }

    /// <summary>
    /// 즉시 피드백 세계의 지휘자 — **대조군이다.**
    ///
    /// 보이는 것은 게임의 지휘자와 **똑같이** 보고, 같은 머릿속 지도(`DeadReckoning`)를 쓴다.
    /// 다른 것은 하나뿐이다: **소절이 끝날 때마다 정확한 어긋남을 듣고 지도를 덮어쓴다.**
    /// 그래서 이 세계와 게임의 세계를 견주면 달라지는 것이 "제때 아는가" 하나로 좁혀진다 —
    /// `DelayedFeedbackMatters` 가 그 차이를 수치로 낸다.
    /// </summary>
    public sealed class ImmediateFeedbackConductor : IConductor, IHearsBarEnd
    {
        private GameData _d;
        private DeadReckoning _map;
        private int[] _barStart;
        private int[] _commandedInBar;
        private int[] _knownRate;

        public string Name { get { return Localization.Text("cond.immediate", "즉시 피드백 세계"); } }

        public void Begin(GameData d, PieceDef piece)
        {
            _d = d;
            _map = new DeadReckoning(d, null);
            _barStart = new int[d.SectionCount];
            _commandedInBar = new int[d.SectionCount];
            _knownRate = null;
        }

        /// <summary>
        /// 소절이 끝나자마자 듣는다: 정확한 어긋남과, 그 소절에서 각 무리가 **한 박에 얼마씩 쏠렸는지.**
        /// 뒤의 값이 신문이 다음 날 알려 주는 것과 같은 것이다 — 여기서는 그것을 **오늘** 안다.
        /// </summary>
        public void HearBarEnd(Players truth, BarDef bar)
        {
            if (_knownRate == null) _knownRate = new int[_d.SectionCount];
            for (int i = 0; i < _d.SectionCount; i++)
            {
                int moved = truth.OffsetMs[i] - _barStart[i] + _commandedInBar[i];
                _knownRate[i] = bar.beats == 0 ? 0 : moved / bar.beats;
                _barStart[i] = truth.OffsetMs[i];
                _commandedInBar[i] = 0;
            }
            _map.Overwrite(truth.OffsetMs);
            _map.SetRateHint(_knownRate);
        }

        public Baton Decide(VisibleFrame seen, BarDef bar, int beat)
        {
            _map.Observe(seen);
            Baton baton = BatonPlanner.Plan(_d, seen, _map.EstOffset, _map.EstRate, _map.EstDyn);
            _map.Commanded(baton);
            int t = baton.SectionIndex;
            CueDef c = _d.Cue(baton.CueId);
            if (t >= 0 && t < _d.SectionCount && c.tempoNudgeMs != 0)
            {
                int carry = 0;
                _commandedInBar[t] += Ratio.Scale(c.tempoNudgeMs, _d.AllSections[t].responsivenessPercent, ref carry);
            }
            return baton;
        }
    }

    /// <summary>
    /// 한 무리에게 한 동작만 되풀이하는 지휘자. `NoDominantStrategy` 가 이것들을 전부 돌린다.
    /// 하나라도 합격하면 이 게임은 지휘가 아니라 한 번의 결심이다.
    /// </summary>
    public sealed class MonoConductor : IConductor
    {
        private readonly int _section;
        private readonly string _cueId;
        private readonly string _label;

        public MonoConductor(int section, string cueId, string label)
        {
            _section = section; _cueId = cueId; _label = label;
        }

        public string Name { get { return _label; } }
        public void Begin(GameData d, PieceDef piece) { }
        public Baton Decide(VisibleFrame seen, BarDef bar, int beat) { return new Baton(_section, _cueId); }
    }
}
