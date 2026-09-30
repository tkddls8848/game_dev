using System.Collections.Generic;
using Tenants.Data;

namespace Tenants.Sim
{
    /// <summary>한 세대에 대해 지금 아는 것.</summary>
    public struct Belief
    {
        /// <summary>0 모른다 · 1 뭔가 있다 · 2 필요한 것을 안다 · 3 얼마나 급한지까지 안다</summary>
        public int Level;
        public string NeedId;
        /// <summary>거짓을 믿고 있다. Level 은 2 지만 들고 가면 거의 아무것도 못 건진다.</summary>
        public bool Misled;
        /// <summary>그 문에 직접 귀를 대 침묵을 확인했다.</summary>
        public bool SilenceConfirmed;
    }

    /// <summary>
    /// 엿들어 모은 것. **더하고 뺄 수 있게** 셈으로만 들고 있다 —
    /// 계획 탐색이 같은 상태를 수만 번 지나가므로 되돌릴 수 있어야 한다.
    ///
    /// 들은 순서는 결론에 영향을 주지 않는다. 이웃의 말을 먼저 듣고 나중에 그 문에서 침묵을
    /// 확인해도 같다 — 사람은 그렇게 확신이 서고, 순서를 따지면 탐색이 배로 커진다.
    ///
    /// **침묵의 게이트가 이 클래스의 핵이다.** 소리 없는 칸에 대한 이웃의 말은 그 문에서 침묵을
    /// 확인하기 전에는 need/stakes/vacant 로 올라가지 못하고 소문(Level 1)에 그친다.
    /// </summary>
    public sealed class Knowledge
    {
        private readonly Night _night;
        private readonly GameData _data;
        private readonly int _h;   // 세대 수
        private readonly int _nd;  // need 종류 수
        private readonly Dictionary<string, int> _needIndex = new Dictionary<string, int>();
        private readonly string[] _needIds;

        private readonly int[,] _trueNeed;   // [세대, need] 참된 need 소리를 몇 번 들었나
        private readonly int[,] _lieNeed;    // [세대, need] 거짓을 몇 번 들었나
        private readonly int[] _hint;
        private readonly int[] _stakes;
        private readonly int[] _vacant;
        private readonly int[] _silence;     // 그 문에서 침묵을 확인한 횟수
        private readonly bool[] _isSilent;

        public int Listens { get; private set; }
        public int SilentDoorListens { get; private set; }

        public Knowledge(Night night)
        {
            _night = night;
            _data = night.Data;
            _h = night.Households.Count;
            IList<NeedDef> needs = _data.AllNeeds;
            _nd = needs.Count;
            _needIds = new string[_nd];
            for (int i = 0; i < _nd; i++) { _needIndex[needs[i].id] = i; _needIds[i] = needs[i].id; }

            _trueNeed = new int[_h, _nd];
            _lieNeed = new int[_h, _nd];
            _hint = new int[_h];
            _stakes = new int[_h];
            _vacant = new int[_h];
            _silence = new int[_h];
            _isSilent = new bool[_h];
            for (int i = 0; i < _h; i++) _isSilent[i] = night.Households[i].voice == Voices.Silent;
        }

        /// <summary>그 문에 그 시간대에 귀를 댄다. 되돌릴 수 있다(Unlisten).</summary>
        public void Listen(int doorIndex, int slot) { Touch(doorIndex, slot, +1); }
        public void Unlisten(int doorIndex, int slot) { Touch(doorIndex, slot, -1); }

        private void Touch(int doorIndex, int slot, int sign)
        {
            Listens += sign;
            if (_isSilent[doorIndex])
            {
                _silence[doorIndex] += sign;
                SilentDoorListens += sign;
            }
            List<ResolvedCue> cues = _night.Heard[doorIndex][slot];
            for (int i = 0; i < cues.Count; i++)
            {
                ResolvedCue c = cues[i];
                int about = _night.IndexOf(c.AboutHouseholdId);
                if (about < 0) continue;
                if (c.Effect == CueEffects.Need) _trueNeed[about, _needIndex[c.NeedId]] += sign;
                else if (c.Effect == CueEffects.Mislead) _lieNeed[about, _needIndex[c.NeedId]] += sign;
                else if (c.Effect == CueEffects.Stakes) _stakes[about] += sign;
                else if (c.Effect == CueEffects.Vacant) _vacant[about] += sign;
                else _hint[about] += sign;
            }
        }

        public Belief Of(int householdIndex)
        {
            int i = householdIndex;
            Belief b = new Belief();
            b.SilenceConfirmed = _silence[i] > 0;
            b.NeedId = null;
            b.Misled = false;

            int trueTotal = 0, firstTrue = -1, lieTotal = 0, firstLie = -1;
            for (int n = 0; n < _nd; n++)
            {
                if (_trueNeed[i, n] > 0) { trueTotal += _trueNeed[i, n]; if (firstTrue < 0) firstTrue = n; }
                if (_lieNeed[i, n] > 0) { lieTotal += _lieNeed[i, n]; if (firstLie < 0) firstLie = n; }
            }

            bool gated = _night.SilenceGate && _isSilent[i] && _silence[i] == 0;
            if (gated)
            {
                // 문 앞에 서 보지 않았다. 이웃의 말은 소문에 그친다.
                bool anything = _hint[i] > 0 || trueTotal > 0 || lieTotal > 0
                                || _vacant[i] > 0 || _stakes[i] > 0;
                b.Level = anything ? 1 : 0;
                return b;
            }

            if (_vacant[i] > 0)
            {
                // 이미 나갔다. 이것도 확실한 지식이고, 그래서 헛되게 돕는 것을 막아 준다.
                b.Level = 3;
                b.NeedId = "need_none";
                return b;
            }
            if (trueTotal > 0)
            {
                // 겹쳐 들으면 거짓이 드러난다 — 참된 소리가 있으면 거짓은 무시된다.
                b.Level = _stakes[i] > 0 ? 3 : 2;
                b.NeedId = _needIds[firstTrue];
                return b;
            }
            if (lieTotal > 0)
            {
                b.Level = 2;
                b.NeedId = _needIds[firstLie];
                b.Misled = true;
                return b;
            }
            bool some = _hint[i] > 0 || _stakes[i] > 0 || _silence[i] > 0;
            b.Level = some ? 1 : 0;
            return b;
        }

        /// <summary>그 문에서 아직 얻을 것이 있는가. 없으면 그 칸에 귀를 대는 것은 쉬는 것과 같다 —
        /// 탐색이 그 가지를 지운다(최선을 잃지 않는다: 쉬는 가지가 같은 결과를 낸다).</summary>
        public bool WorthListening(int doorIndex, int slot)
        {
            if (_night.Heard[doorIndex][slot].Count > 0) return true;
            return _isSilent[doorIndex] && _silence[doorIndex] == 0;
        }
    }
}
