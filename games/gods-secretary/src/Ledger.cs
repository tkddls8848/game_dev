using System;
using System.Collections.Generic;
using Secretary.Data;

namespace Secretary.Sim
{
    /// <summary>장부에서 빠진 한 줄. **자리에는 반드시 이름이 있다.**</summary>
    public struct DebitLine
    {
        public string SoulId;
        public string Domain;
        public int Units;
    }

    /// <summary>
    /// 천상의 장부. 영역마다 **총량이 변하지 않는다** — 들어준 만큼이 정확히 출처에서 빠진다.
    /// 그것이 ConservationHolds 가 매 판정마다 확인하는 불변식이다.
    ///
    /// have 는 need 를 절대 넘지 않는다(허가는 부족분까지만 간다). 그래서 누구에게서든
    /// 한 단위를 빼면 반드시 부족이 생기고, **대가 없는 허가가 구조적으로 불가능하다.**
    /// NoFreeGrant 는 그 사실을 믿지 않고 매번 실제로 뒤진다.
    /// </summary>
    public sealed class Ledger
    {
        private readonly GameData _data;
        private readonly List<string> _souls = new List<string>();
        private readonly List<string> _domains = new List<string>();
        private readonly Dictionary<string, int> _soulIndex = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _domainIndex = new Dictionary<string, int>();
        private readonly int[,] _have;
        private readonly int[,] _need;
        private readonly int[,] _tol;
        private readonly bool[,] _present;
        private readonly int[] _initialTotal;

        /// <summary>false 면 **대조 세계**다 — 허가해도 아무 데서도 빠지지 않는다(신이 없던 것을 만든다).</summary>
        public bool Conservation { get; private set; }

        public IList<string> SoulIds { get { return _souls; } }
        public IList<string> DomainIds { get { return _domains; } }

        public Ledger(GameData data, VolumeDef volume) : this(data, volume, data.Balance.conservation.enabled) { }

        public Ledger(GameData data, VolumeDef volume, bool conservation)
        {
            _data = data;
            Conservation = conservation;
            foreach (SoulDef s in data.AllSouls) { _soulIndex[s.id] = _souls.Count; _souls.Add(s.id); }
            foreach (DomainDef d in data.AllDomains) { _domainIndex[d.id] = _domains.Count; _domains.Add(d.id); }
            _have = new int[_souls.Count, _domains.Count];
            _need = new int[_souls.Count, _domains.Count];
            _tol = new int[_souls.Count, _domains.Count];
            _present = new bool[_souls.Count, _domains.Count];
            foreach (HoldingDef h in volume.holdings)
            {
                int s = _soulIndex[h.soulId], d = _domainIndex[h.domain];
                _have[s, d] = h.have; _need[s, d] = h.need; _tol[s, d] = h.toleranceUnits;
                _present[s, d] = true;
            }
            _initialTotal = new int[_domains.Count];
            for (int d = 0; d < _domains.Count; d++) _initialTotal[d] = TotalOf(d);
        }

        private Ledger(Ledger o)
        {
            _data = o._data; Conservation = o.Conservation;
            _souls = o._souls; _domains = o._domains;
            _soulIndex = o._soulIndex; _domainIndex = o._domainIndex;
            _have = (int[,])o._have.Clone();
            _need = (int[,])o._need.Clone();
            _tol = o._tol; _present = o._present;
            _initialTotal = o._initialTotal;
        }

        public Ledger Clone() { return new Ledger(this); }

        public int S(string soulId) { return _soulIndex[soulId]; }
        public int Dm(string domainId) { return _domainIndex[domainId]; }
        public bool Holds(string soulId, string domainId)
        {
            int s, d;
            if (!_soulIndex.TryGetValue(soulId, out s)) return false;
            if (!_domainIndex.TryGetValue(domainId, out d)) return false;
            return _present[s, d];
        }

        public int Have(string soulId, string domainId) { return _have[S(soulId), Dm(domainId)]; }
        public int Need(string soulId, string domainId) { return _need[S(soulId), Dm(domainId)]; }
        public int Deficit(string soulId, string domainId)
        {
            int s = S(soulId), d = Dm(domainId);
            int x = _need[s, d] - _have[s, d];
            return x > 0 ? x : 0;
        }

        public int TotalOf(int d)
        {
            int t = 0;
            for (int s = 0; s < _souls.Count; s++) if (_present[s, d]) t += _have[s, d];
            return t;
        }

        public int InitialTotal(string domainId) { return _initialTotal[Dm(domainId)]; }

        /// <summary>그 사람 그 영역의 고통. 부족분에 비례하고, 견딜 한도를 넘은 몫에는 값이 더 붙는다(볼록).</summary>
        public int Hardship(string soulId, string domainId)
        {
            int s = S(soulId), d = Dm(domainId);
            if (!_present[s, d]) return 0;
            DomainDef dd = _data.Domain(domainId);
            int deficit = _need[s, d] - _have[s, d];
            if (deficit <= 0) return 0;
            int over = deficit - _tol[s, d];
            if (over < 0) over = 0;
            return deficit * dd.hardshipPerUnit + over * dd.severeExtraPerUnit;
        }

        /// <summary>세상의 고통 전부. 낮을수록 좋다.</summary>
        public int TotalHardship()
        {
            int t = 0;
            for (int s = 0; s < _souls.Count; s++)
                for (int d = 0; d < _domains.Count; d++)
                    if (_present[s, d]) t += Hardship(_souls[s], _domains[d]);
            return t;
        }

        /// <summary>그 사람에게서 units 만큼 뺐을 때 그 사람의 고통이 얼마나 오르는가. **0 이면 공짜 허가다.**</summary>
        public int CostOfDrawing(string soulId, string domainId, int units)
        {
            int before = Hardship(soulId, domainId);
            int s = S(soulId), d = Dm(domainId);
            _have[s, d] -= units;
            int after = Hardship(soulId, domainId);
            _have[s, d] += units;
            return after - before;
        }

        public void Draw(string soulId, string domainId, int units)
        {
            if (!Conservation) return;   // 대조 세계 — 아무 데서도 빠지지 않는다
            int s = S(soulId), d = Dm(domainId);
            if (units > _have[s, d]) throw new InvalidOperationException("없는 것을 뺄 수 없다: " + soulId);
            _have[s, d] -= units;
        }

        public void Give(string soulId, string domainId, int units)
        {
            int s = S(soulId), d = Dm(domainId);
            _have[s, d] += units;
            if (_have[s, d] > _need[s, d])
                throw new InvalidOperationException("필요보다 많이 줄 수 없다: " + soulId);
        }

        /// <summary>보류하면 그 사람의 사정이 커진다. 미룬 기도는 작아지지 않는다.</summary>
        public void GrowNeed(string soulId, string domainId, int units)
        {
            _need[S(soulId), Dm(domainId)] += units;
        }

        /// <summary>영역마다 총량이 처음 그대로인가. 대조 세계에서는 늘어난다.</summary>
        public bool Balanced(out string reason)
        {
            for (int d = 0; d < _domains.Count; d++)
            {
                int now = TotalOf(d);
                if (now != _initialTotal[d])
                {
                    reason = _domains[d] + " 의 총량이 " + _initialTotal[d] + " 에서 " + now + " 로 바뀌었다";
                    return false;
                }
            }
            reason = null;
            return true;
        }
    }
}
