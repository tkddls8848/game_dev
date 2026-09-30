using System;
using System.Collections.Generic;
using Secretary.Data;

namespace Secretary.Sim
{
    /// <summary>도착한 편지 하나. Grown 은 보류로 커진 몫이다.</summary>
    public sealed class Letter
    {
        public PrayerDef Def;
        public int ArrivedDay;
        public int Grown;
        public Letter Copy() { return new Letter { Def = Def, ArrivedDay = ArrivedDay, Grown = Grown }; }
    }

    /// <summary>봉랍 도장 한 번. 허가면 어느 이름에서 뺄지 순서까지 정해야 한다.</summary>
    public sealed class Verdict
    {
        public string Kind;
        public string PrayerId;
        /// <summary>허가일 때만. 이 순서대로 뺀다 — 앞에 적힌 이름이 먼저 잃는다.</summary>
        public string[] SourceOrder;
    }

    /// <summary>장부에 남은 한 줄. **빠진 자리마다 이름이 있다.**</summary>
    public sealed class VerdictRecord
    {
        public int Day;
        public string PrayerId;
        public string Kind;
        public string FromSoulId;
        public string Domain;
        public int Granted;
        public int Drawn;
        public int CostAtSources;
        public int NeedGrew;
        public List<DebitLine> Debits = new List<DebitLine>();
    }

    public sealed class SimState
    {
        public GameData Data;
        public VolumeDef Volume;
        public Ledger L;
        public List<Letter> Docket = new List<Letter>();
        public List<Letter> Pending = new List<Letter>();
        public int Day;
        public int StampsLeft;
        /// <summary>날마다 쌓은 고통의 합. 미루면 그만큼 더 쌓인다 — 보류의 값이 여기서 나온다.</summary>
        public int Accrued;
        public int Grants, Denies, Defers, AutoDefers;
        public int GrantedUnits, DrawnUnits;
        public List<VerdictRecord> Log = new List<VerdictRecord>();
        public List<string> ConservationBreaks = new List<string>();

        public bool Finished { get { return Day >= Volume.days; } }

        public SimState Clone()
        {
            SimState s = new SimState();
            s.Data = Data; s.Volume = Volume; s.L = L.Clone();
            foreach (Letter x in Docket) s.Docket.Add(x.Copy());
            foreach (Letter x in Pending) s.Pending.Add(x.Copy());
            s.Day = Day; s.StampsLeft = StampsLeft; s.Accrued = Accrued;
            s.Grants = Grants; s.Denies = Denies; s.Defers = Defers; s.AutoDefers = AutoDefers;
            s.GrantedUnits = GrantedUnits; s.DrawnUnits = DrawnUnits;
            s.Log.AddRange(Log);
            s.ConservationBreaks.AddRange(ConservationBreaks);
            return s;
        }

        public Letter Find(string prayerId)
        {
            foreach (Letter x in Docket) if (x.Def.id == prayerId) return x;
            return null;
        }
    }
}
