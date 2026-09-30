using Tenants.Data;

namespace Tenants.Sim
{
    /// <summary>
    /// 하루가 끝난 뒤의 장부. **누구를 고르든 나머지가 나빠진다**가 여기서 수치가 된다.
    ///
    /// 유예는 건물 하나에 정해진 양뿐이다. 한 집에 유예 하루를 쓰면 나머지 세대의 손실이
    /// 각각 (perGraceFlat x 그 집 가중치%) 만큼 오른다. 백분율은 정수이고 나머지는 누적한다
    /// (뿌리 CLAUDE.md 설계 원칙 3·4 — 부동소수를 쓰면 0.999 짜리 손실이 생겨 검사기를 쓸 수 없다).
    /// </summary>
    public sealed class Outcome
    {
        /// <summary>고른 세대의 index. -1 이면 아무도 돕지 않았다.</summary>
        public int ChoiceIndex;
        public string ChoiceId;
        public int HelpStartSlot;

        public int KnowledgeLevel;
        public bool Misled;
        public string BelievedNeedId;

        public int GraceSpent;
        /// <summary>도움이 먹은 시간대의 수.</summary>
        public int HelpSlots;
        public int ReliefOfChosen;
        public int RemainingOfChosen;
        /// <summary>나머지 세대에게 물린 값의 합. **이 값이 0 이면 딜레마가 아니다.**</summary>
        public int SpilloverInflicted;
        public int OthersDamage;
        public int Total;
        public int[] FinalStakes;

        public int Listens;
        public int SilentDoorListens;

        /// <summary>유예 graceDays 일을 쓸 때 가중치 weightPercent 인 집이 더 짊어지는 손실.
        /// 정수 나눗셈의 나머지를 하루마다 누적한다.</summary>
        public static int Spill(int graceDays, int weightPercent, int perGraceFlat)
        {
            int total = 0, rem = 0;
            for (int g = 0; g < graceDays; g++)
            {
                int num = perGraceFlat * weightPercent + rem;
                total += num / 100;
                rem = num % 100;
            }
            return total;
        }

        /// <summary>아무도 돕지 않은 하루. 비교의 바닥이다.</summary>
        public static Outcome HelpNoOne(Night night)
        {
            GameData d = night.Data;
            Outcome o = new Outcome();
            o.ChoiceIndex = -1;
            o.ChoiceId = null;
            o.HelpStartSlot = -1;
            o.KnowledgeLevel = -1;
            o.BelievedNeedId = null;
            o.FinalStakes = new int[night.Households.Count];
            int total = 0;
            for (int i = 0; i < night.Households.Count; i++)
            {
                o.FinalStakes[i] = night.Households[i].baseStakes;
                total += o.FinalStakes[i];
            }
            o.OthersDamage = total;
            o.Total = total;
            return o;
        }

        /// <summary>
        /// 한 집을 고른 결과. 계획이 하루에 들어가지 않으면 null 을 준다
        /// (아는 것이 많아질수록 도움에 드는 시간대가 길어져서 실제로 안 들어가는 일이 생긴다 —
        /// 「약과 통원」은 시간대 셋을 먹으므로 엿들을 수 있는 칸이 셋밖에 남지 않는다).
        /// </summary>
        public static Outcome Help(Night night, Knowledge k, int choiceIndex, int helpStartSlot)
        {
            GameData d = night.Data;
            HelpBalance hb = d.Balance.help;
            Belief b = k.Of(choiceIndex);

            int slots, grace, reliefPercent;
            string believed = b.NeedId;
            if (b.Level >= 2 && believed != null)
            {
                NeedDef nd = d.Need(believed);
                slots = nd.slotsRequired;
                grace = nd.graceCost;
                reliefPercent = b.Misled ? hb.wrongNeedReliefPercent : hb.reliefPercentByLevel[b.Level];
            }
            else
            {
                // 무엇이 필요한지 모른 채 문을 두드린다. 유예는 그대로 빠진다.
                slots = hb.blindSlotsRequired;
                grace = hb.blindGraceCost;
                reliefPercent = hb.reliefPercentByLevel[b.Level < 0 ? 0 : b.Level];
                believed = null;
            }

            if (helpStartSlot < 0) return null;
            if (helpStartSlot + slots > d.Balance.day.slotCount) return null;
            if (grace > night.Building.graceDaysTotal) return null;

            Outcome o = new Outcome();
            o.ChoiceIndex = choiceIndex;
            o.ChoiceId = night.Households[choiceIndex].id;
            o.HelpStartSlot = helpStartSlot;
            o.KnowledgeLevel = b.Level;
            o.Misled = b.Misled;
            o.BelievedNeedId = believed;
            o.GraceSpent = grace;
            o.HelpSlots = slots;
            o.Listens = k.Listens;
            o.SilentDoorListens = k.SilentDoorListens;

            int flat = d.Balance.spillover.perGraceFlat;
            int n = night.Households.Count;
            o.FinalStakes = new int[n];
            int others = 0, spillSum = 0;
            for (int i = 0; i < n; i++)
            {
                HouseholdDef h = night.Households[i];
                if (i == choiceIndex) continue;
                int spill = Spill(grace, h.spilloverWeightPercent, flat);
                spillSum += spill;
                o.FinalStakes[i] = h.baseStakes + spill;
                others += o.FinalStakes[i];
            }
            HouseholdDef c = night.Households[choiceIndex];
            o.ReliefOfChosen = c.baseStakes * reliefPercent / 100;
            o.RemainingOfChosen = c.baseStakes - o.ReliefOfChosen;
            o.FinalStakes[choiceIndex] = o.RemainingOfChosen;
            o.SpilloverInflicted = spillSum;
            o.OthersDamage = others;
            o.Total = others + o.RemainingOfChosen;
            return o;
        }
    }
}
