using System.Collections.Generic;
using CurseLedger.Data;

namespace CurseLedger.Sim
{
    /// <summary>
    /// 백분율 배율의 나머지를 버리지 않고 누적한다 (설계 원칙 4).
    /// 부동소수를 쓰면 "0.999 대 남은 속박"이 생기고 검사기를 쓸 수 없다.
    /// </summary>
    public static class Ratio
    {
        public static int Scale(int amount, int percent, ref int carry)
        {
            long t = (long)amount * percent + carry;
            int result = (int)(t / 100);
            carry = (int)(t % 100);
            return result;
        }
    }

    /// <summary>어떻게 끝났는가. 우선순위 순서가 곧 판정 순서다.</summary>
    public enum Ending
    {
        Running = 0,
        VillageRuin,    // 마을 몰락 — 번영이 0
        HouseExtinct,   // 가문 단절 — 종가의 기력이 0
        Uprising,       // 마을 봉기 — 원한이 100, 제단이 부서진다
        CurseUnbound,   // 저주 창궐 — 속박이 0
        CurseLifted,    // 해제 완수 — 약조가 사라진다. 그리고 풍요도 사라진다
        Stalled,        // 장부가 멈췄다 — 고를 제례가 없어 대를 끝까지 잇지 못했다
        PassedOn        // 대물림 — 마지막 대를 넘겼다. 아무것도 끝나지 않았다
    }

    public static class Endings
    {
        public static string Korean(Ending e)
        {
            switch (e)
            {
                case Ending.VillageRuin:  return Localization.Text("end.villageRuin",  "마을 몰락");
                case Ending.HouseExtinct: return Localization.Text("end.houseExtinct", "가문 단절");
                case Ending.Uprising:     return Localization.Text("end.uprising",     "마을 봉기");
                case Ending.CurseUnbound: return Localization.Text("end.curseUnbound", "저주 창궐");
                case Ending.CurseLifted:  return Localization.Text("end.curseLifted",  "해제 완수");
                case Ending.Stalled:      return Localization.Text("end.stalled",      "장부가 멈췄다");
                case Ending.PassedOn:     return Localization.Text("end.passedOn",     "대물림");
                default:                  return "진행 중";
            }
        }
    }

    /// <summary>희생자 한 줄. **이름과 사정을 들고 다닌다** — 숫자로 줄어들지 않는다.</summary>
    public sealed class VictimRecord
    {
        public string VillagerId;
        public string Name;
        public int Age;
        public string Tier;
        public string Household;
        public string Note;
        public int Generation;
        public string HeirId;
        public string RiteId;
        /// <summary>족보에서 붉은 줄이 그어지는 줄 번호. 연출이 이 값으로 줄을 긋는다.</summary>
        public int LedgerLine;
    }

    /// <summary>도착을 기다리는 청구서 하나. 이 목록이 이 PoC의 기제다.</summary>
    public sealed class Bill
    {
        public int AtGeneration;
        public int FromGeneration;   // -1 이면 앞 세대가 남긴 것
        public string Source;        // riteId 또는 heirId
        public string Note;
        public int Wrath, Binding, Prosperity, HouseVitality, Resentment, DemandBonus, Coffers;
        /// <summary>다음 대의 요구에 **한 번만** 얹히는 몫. 미룬 공물이 이 칸으로 온다.</summary>
        public int DemandOnce;

        public static Bill From(PendingDef p, int atGeneration, int fromGeneration, string source)
        {
            return new Bill
            {
                AtGeneration = atGeneration,
                FromGeneration = fromGeneration,
                Source = source,
                Note = p.note,
                Wrath = p.wrath, Binding = p.binding, Prosperity = p.prosperity,
                HouseVitality = p.houseVitality, Resentment = p.resentment,
                DemandBonus = p.demandBonus, Coffers = p.coffers
            };
        }

        /// <summary>이 청구서가 움직이는 총량. ConsequenceWeight 가 이것으로 지연 비중을 잰다.</summary>
        public int Magnitude
        {
            get
            {
                return Abs(Wrath) + Abs(Binding) + Abs(Prosperity) + Abs(HouseVitality)
                       + Abs(Resentment) + Abs(DemandBonus) + Abs(DemandOnce) + Abs(Coffers);
            }
        }

        private static int Abs(int v) { return v < 0 ? -v : v; }
    }

    /// <summary>한 대의 장부 한 줄. 목업이 이것을 그대로 그린다.</summary>
    public sealed class LedgerLine
    {
        public int Generation;
        public string HeirId;
        public string HeirName;
        public string RiteId;
        public string RiteName;
        public int DemandDue;
        public int Paid;
        public int Unpaid;
        public bool LeanYear;
        /// <summary>청구서가 도착한 순간 끝나 제례를 올리지 못한 대. 목업이 이 줄을 다르게 그린다.</summary>
        public bool Aborted;
        /// <summary>미뤄서 다음 대로 넘긴 공물(이자 붙은 뒤의 값). 0 이면 미루지 않았다.</summary>
        public int Postponed;
        public List<string> VictimIds = new List<string>();
        public List<Bill> BillsArrived = new List<Bill>();
        public List<Bill> BillsScheduled = new List<Bill>();
        public int Prosperity, Binding, Wrath, HouseVitality, Resentment, ReleaseProgress, Coffers;
    }

    /// <summary>회차 하나의 상태. 전부 정수다.</summary>
    public sealed class LedgerState
    {
        public int Generation;
        public int Prosperity, Binding, Wrath, HouseVitality, Resentment, ReleaseProgress, Coffers;
        public int DemandBonus;
        /// <summary>미룬 공물. 다음 대의 요구에 한 번 얹히고 비워진다.</summary>
        public int DemandOnce;
        public Ending Ending;   // 기본값 0 = Running

        // 나머지 누적 채널 셋. 부동소수 대신 이것을 쓴다.
        public int ProsperityCarry, BindingCarry, DemandCarry;

        public readonly List<VictimRecord> Victims = new List<VictimRecord>();
        public readonly List<Bill> Bills = new List<Bill>();
        public readonly List<LedgerLine> Lines = new List<LedgerLine>();
        public readonly HashSet<string> TakenVillagers = new HashSet<string>();

        public bool Ended { get { return Ending != Ending.Running; } }
    }
}
