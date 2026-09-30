using System;
using SilentBaton.Data;

namespace SilentBaton.Sim
{
    /// <summary>
    /// ★ 보이는 셋으로 **머릿속에 그리는 지도**. 청각을 잃은 지휘자가 하는 일 전부가 이것이다.
    ///
    /// 이 형은 `VisibleFrame`(보이는 셋) 과 `PressKnowledge`(어제 신문) 밖에 만지지 않는다 —
    /// 숨은 상태를 가리키는 이름이 이 파일에 하나도 없다. 검사기가 글자로 확인한다.
    ///
    /// 세 채널이 서로 다른 일을 한다. **하나라도 빼면 지도가 무너진다** —
    /// 그것이 `VisualCuesSuffice` 의 음영 대조군이 재는 것이다.
    ///
    ///   활(Bow)   → **얼마나 빠르게 벌어지는가.** 4ms 눈금. 이것으로 한 박씩 앞을 이어 간다(추측 항법)
    ///   호흡(Breath) → **지금 어디까지 벌어졌는가.** 30ms 눈금. 추측이 눈금 밖으로 나가면 눈을 믿고 고친다
    ///   표정(Face) → **세기가 얼마나 어긋났는가.** 9칸 눈금
    ///
    /// 활이 없는 무리(목관·금관)의 속도는 어제 신문이 알려 준 성향으로 메우고,
    /// 신문이 없으면 호흡 눈금이 한 칸 움직이는 것을 기다려 **뒤늦게** 안다.
    /// </summary>
    public sealed class DeadReckoning
    {
        private readonly GameData _d;
        private readonly PressKnowledge _press;
        private readonly int[] _estOffset;
        private readonly int[] _estRate;
        private readonly int[] _estDyn;
        private readonly int[] _prevBreath;
        private readonly bool[] _seen;
        private readonly int[] _lastCommandedMs;
        private readonly int[] _pressCarry;
        private int[] _rateHint;

        public DeadReckoning(GameData d, PressKnowledge press)
        {
            _d = d; _press = press;
            int n = d.SectionCount;
            _estOffset = new int[n]; _estRate = new int[n]; _estDyn = new int[n];
            _prevBreath = new int[n]; _seen = new bool[n];
            _lastCommandedMs = new int[n]; _pressCarry = new int[n];
        }

        public int[] EstOffset { get { return _estOffset; } }
        public int[] EstRate { get { return _estRate; } }
        public int[] EstDyn { get { return _estDyn; } }

        /// <summary>이 지도를 정확한 값으로 덮어쓴다. 즉시 피드백 세계만 부를 수 있다(대조군).</summary>
        public void Overwrite(int[] exactOffsetMs)
        {
            for (int i = 0; i < _estOffset.Length && i < exactOffsetMs.Length; i++)
                _estOffset[i] = exactOffsetMs[i];
        }

        /// <summary>
        /// 활이 보이지 않는 무리의 **속도**를 알려 주는 쪽지. 어제 신문이 주면 늦게 오고,
        /// 즉시 피드백 세계에서는 첫 소절 끝에 온다. 어느 쪽이든 **귀로 들은 것이 아니다.**
        /// 여기가 이 PoC의 승부처다 — 빼앗긴 것은 위치가 아니라 **속도를 언제 아는가**다.
        /// </summary>
        public void SetRateHint(int[] perBeatMs) { _rateHint = perBeatMs; }

        /// <summary>보이는 프레임 하나를 읽어 지도를 갱신한다.</summary>
        public void Observe(VisibleFrame seen)
        {
            VisibleBalance v = _d.Balance.visible;
            ConductorBalance cb = _d.Balance.conductor;

            for (int i = 0; i < _d.SectionCount; i++)
            {
                if (!seen.Playing[i])
                {
                    _estOffset[i] = 0; _estRate[i] = 0; _estDyn[i] = 0;
                    _lastCommandedMs[i] = 0;
                    continue;
                }

                // ① 얼마나 빠르게 벌어지는가
                if (seen.BowReadable[i])
                {
                    _estRate[i] = seen.Bow[i] * seen.BowQuantMs[i];
                }
                else if (_rateHint != null)
                {
                    _estRate[i] = _rateHint[i];
                }
                else if (_press != null && _press.ConcertsRead > 0)
                {
                    int c = _pressCarry[i];
                    _estRate[i] = Ratio.Scale(_press.TraitEstimateMs[i], cb.pressTrustPercent, ref c);
                    _pressCarry[i] = c;
                }
                else if (_seen[i])
                {
                    _estRate[i] = (seen.Breath[i] - _prevBreath[i]) * seen.BreathQuantMs[i];
                }
                else
                {
                    _estRate[i] = 0;
                }

                // ② 추측 항법: 지난 박에 벌어진 만큼 더하고, 내가 준 지휘가 먹은 만큼 뺀다.
                //    자기 손이 무엇을 했는지는 안다 — 소리를 듣는 것이 아니다.
                _estOffset[i] += _estRate[i] - _lastCommandedMs[i];
                _lastCommandedMs[i] = 0;

                // ③ 눈으로 고친다. 호흡 눈금과 내 짐작이 한 눈금 넘게 다르면 **눈을 믿는다.**
                //    이 한 줄이 호흡 채널이 짐을 지는 자리다.
                int breathMs = seen.Breath[i] * seen.BreathQuantMs[i];
                bool saturated = Math.Abs(seen.Breath[i]) >= v.breathLevels;
                if (!saturated && Math.Abs(_estOffset[i] - breathMs) > seen.BreathQuantMs[i])
                    _estOffset[i] = breathMs;

                // ④ 세기
                _estDyn[i] = seen.Face[i] * seen.FaceQuantLevel[i];

                _prevBreath[i] = seen.Breath[i];
                _seen[i] = true;
            }
        }

        /// <summary>내가 준 지휘가 다음 박에 얼마나 먹을지 적어 둔다. 추측 항법이 이것을 뺀다.</summary>
        public void Commanded(Baton baton)
        {
            int t = baton.SectionIndex;
            if (t < 0 || t >= _d.SectionCount) return;
            CueDef c = _d.Cue(baton.CueId);
            if (c.tempoNudgeMs == 0) return;
            int carry = 0;
            _lastCommandedMs[t] = Ratio.Scale(c.tempoNudgeMs, _d.AllSections[t].responsivenessPercent, ref carry);
        }
    }
}
