using System.Collections.Generic;
using Tenants.Data;

namespace Tenants.Sim
{
    /// <summary>선택지 하나의 전체 결과. 「그 집이 건진 것」과 「나머지가 물어 준 것」을 나란히 둔다.</summary>
    public sealed class ChoiceRow
    {
        public int Index;
        public string Id;
        public string Name;
        public string UnitNo;
        public string SilenceKind;

        public int KnowledgeLevel;
        public bool Misled;
        public string BelievedNeedId;
        public int GraceSpent;

        /// <summary>그 집이 건진 값. 높을수록 좋다.</summary>
        public int Relief;
        /// <summary>나머지 세대에게 물린 값의 합. 낮을수록 좋다.</summary>
        public int Spill;
        public int OthersDamage;
        public int Total;
        public int[] FinalStakes;
        public int Listens;
        public int SilentDoorListens;
        public int[] Plan;
    }

    /// <summary>
    /// **EveryChoiceCostsTheOthers 가 보는 표.**
    ///
    /// 선택지마다 하루를 전수 탐색해 최선을 찾고, 그 결과를 두 축으로 늘어놓는다.
    /// 두 축을 고른 이유가 이 PoC의 설계 전부다:
    ///
    ///   Relief — 그 집이 건진 값. 크게 건지려면 비싼 사정을 골라야 한다
    ///   Spill  — 나머지 다섯이 물어 준 값. 비싼 사정은 유예를 많이 먹는다
    ///
    /// 이 둘이 같은 방향으로 움직이면 지배적 최선이 생기고 딜레마가 사라진다.
    /// (총 손실 Total 을 축으로 쓰면 안 된다 — 가장 큰 사정을 고르는 것만으로
    ///  그 값이 「나머지」에서 빠져서, 물린 값과 무관하게 유리해 보인다.)
    /// </summary>
    public sealed class ChoiceMatrix
    {
        public Night Night { get; private set; }
        public List<ChoiceRow> Rows { get; private set; }
        public Outcome NoOne { get; private set; }
        public int Nodes { get; private set; }

        public static ChoiceMatrix Build(Night night) { return Build(night, new SearchOptions()); }

        public static ChoiceMatrix Build(Night night, SearchOptions opt)
        {
            PlanSearch.Result r = PlanSearch.Exhaustive(night, opt);
            ChoiceMatrix m = new ChoiceMatrix();
            m.Night = night;
            m.NoOne = r.NoOne;
            m.Nodes = r.Nodes;
            m.Rows = new List<ChoiceRow>();
            for (int i = 0; i < r.ByChoice.Length; i++)
            {
                if (r.ByChoice[i] == null) continue;
                Outcome o = r.ByChoice[i].Result;
                HouseholdDef h = night.Households[i];
                ChoiceRow row = new ChoiceRow();
                row.Index = i;
                row.Id = h.id;
                row.Name = h.name;
                row.UnitNo = night.Data.Unit(h.unitId).unitNo;
                row.SilenceKind = h.silenceKind;
                row.KnowledgeLevel = o.KnowledgeLevel;
                row.Misled = o.Misled;
                row.BelievedNeedId = o.BelievedNeedId;
                row.GraceSpent = o.GraceSpent;
                row.Relief = o.ReliefOfChosen;
                row.Spill = o.SpilloverInflicted;
                row.OthersDamage = o.OthersDamage;
                row.Total = o.Total;
                row.FinalStakes = o.FinalStakes;
                row.Listens = o.Listens;
                row.SilentDoorListens = o.SilentDoorListens;
                row.Plan = r.ByChoice[i].Plan();
                m.Rows.Add(row);
            }
            return m;
        }

        public ChoiceRow Row(string householdId)
        {
            foreach (ChoiceRow r in Rows) if (r.Id == householdId) return r;
            return null;
        }

        /// <summary>두 축에서 지배되지 않는 선택지들. 하나뿐이면 지배적 최선이 있다는 뜻이다.</summary>
        public List<ChoiceRow> ParetoFront()
        {
            List<ChoiceRow> front = new List<ChoiceRow>();
            foreach (ChoiceRow a in Rows)
            {
                bool dominated = false;
                foreach (ChoiceRow b in Rows)
                {
                    if (a == b) continue;
                    bool notWorse = b.Relief >= a.Relief && b.Spill <= a.Spill;
                    bool strictly = b.Relief > a.Relief || b.Spill < a.Spill;
                    if (notWorse && strictly) { dominated = true; break; }
                }
                if (!dominated) front.Add(a);
            }
            return front;
        }

        /// <summary>그 선택에서 나머지 세대 **하나하나가** 더 나빠졌는가. 합계로 뭉개지 않는다.</summary>
        public bool EachOtherStrictlyWorse(ChoiceRow row)
        {
            for (int i = 0; i < Night.Households.Count; i++)
            {
                if (i == row.Index) continue;
                if (row.FinalStakes[i] <= Night.Households[i].baseStakes) return false;
            }
            return true;
        }

        public ChoiceRow BestByTotal()
        {
            ChoiceRow best = null;
            foreach (ChoiceRow r in Rows) if (best == null || r.Total < best.Total) best = r;
            return best;
        }

        public ChoiceRow WorstByTotal()
        {
            ChoiceRow worst = null;
            foreach (ChoiceRow r in Rows) if (worst == null || r.Total > worst.Total) worst = r;
            return worst;
        }

        public ChoiceRow OfSilenceKind(string kind)
        {
            foreach (ChoiceRow r in Rows) if (r.SilenceKind == kind) return r;
            return null;
        }
    }
}
