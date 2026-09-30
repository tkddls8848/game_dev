using SilentBaton.Data;

namespace SilentBaton.Sim
{
    /// <summary>
    /// ★ **게임의 지휘자다.** 청각을 잃었다.
    ///
    /// 이 파일은 `VisibleFrame`(보이는 셋) · `PressKnowledge`(어제 신문) · `DeadReckoning`(머릿속 지도)
    /// 밖에 만지지 않는다. 귀 인터페이스(`IHearsEveryBeat` · `IHearsBarEnd`)를 **구현하지 않는다** —
    /// 그러므로 `Performance` 가 이 지휘자에게 진실을 건네줄 길이 구조적으로 없다.
    ///
    /// 검사기 `VisualCuesSuffice` 가 이 파일과 `DeadReckoning.cs` 의 본문을 글자로 훑어
    /// 숨은 상태를 가리키는 이름이 하나도 없음을 확인한다. 이름을 바꾸기 전에 그 검사기를 먼저 읽는다.
    /// </summary>
    public sealed class VisibleOnlyConductor : IConductor
    {
        private readonly PressKnowledge _press;
        private readonly VisibleChannel _masked;
        private GameData _d;
        private DeadReckoning _map;

        public VisibleOnlyConductor(PressKnowledge press = null, VisibleChannel masked = VisibleChannel.None)
        {
            _press = press;
            _masked = masked;
        }

        public string Name
        {
            get
            {
                string b = Localization.Text("cond.visible", "보이는 것만 — 소리 없는 지휘대");
                if (_masked != VisibleChannel.None)
                    b += " (" + VisibleChannels.Korean(_masked) + " 가림)";
                if (_press != null && _press.ConcertsRead > 0)
                    b += " · 신문 " + _press.ConcertsRead + "회 읽음";
                return b;
            }
        }

        public void Begin(GameData d, PieceDef piece)
        {
            _d = d;
            _map = new DeadReckoning(d, _press);
        }

        public Baton Decide(VisibleFrame seen, BarDef bar, int beat)
        {
            if (_masked != VisibleChannel.None) seen = seen.Masked(_masked);
            _map.Observe(seen);
            Baton baton = BatonPlanner.Plan(_d, seen, _map.EstOffset, _map.EstRate, _map.EstDyn);
            _map.Commanded(baton);
            return baton;
        }
    }
}
