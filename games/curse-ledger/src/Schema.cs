using Newtonsoft.Json;

namespace CurseLedger.Data
{
    // ── data/kin.json ────────────────────────────────────────────────────────
    public sealed class KinFile
    {
        public string houseName;
        public string curseName;
        public HeirDef[] heirs;
        public VillagerDef[] villagers;
    }

    public sealed class HeirDef
    {
        public string id;
        public string name;
        public int generationIndex;
        public int bornYear;
        public string note;
    }

    /// <summary>
    /// 바칠 수 있는 사람 하나. **이름·나이·사정을 가진다** — 핵 검사기 VictimsHaveNames 가
    /// 이 셋이 비어 있지 않은지를 본다. 비면 도덕이 자원이 아니라 비용이 된다.
    /// </summary>
    public sealed class VillagerDef
    {
        public string id;
        public string name;
        public int age;
        public string tier;        // "elder" | "young"
        public string household;
        public string note;        // 사정. 비어 있으면 안 된다
    }

    public static class Tiers
    {
        public static string Elder { get { return "elder"; } }
        public static string Young { get { return "young"; } }
    }

    // ── data/curse.json ──────────────────────────────────────────────────────
    public sealed class CurseFile
    {
        public string id;
        public string name;
        public string note;
        public int generations;
        public int seed;
        public StartState start;
        public DemandCurve demand;
        public PendingDef[] inheritedPending;
        public PriorLine[] priorLedger;
    }

    public sealed class StartState
    {
        public int prosperity;
        public int binding;
        public int wrath;
        public int houseVitality;
        public int resentment;
        public int releaseProgress;
        public int coffers;
    }

    public sealed class DemandCurve
    {
        [JsonProperty("base")] public int baseAmount;
        public int growthPerGeneration;
        public int wrathPercent;
        public int jitterMs = -1;
        public int leanYearSpanPercent;
    }

    /// <summary>
    /// 지연 청구서 하나. **이 PoC의 기제가 이 클래스다** — 결정한 대가 아니라
    /// atGeneration(또는 delayGenerations 뒤)에 효과가 도착한다.
    /// </summary>
    public sealed class PendingDef
    {
        public int atGeneration = -1;
        public int delayGenerations = -1;
        public string from;
        public string note;
        public int wrath;
        public int binding;
        public int prosperity;
        public int houseVitality;
        public int resentment;
        public int demandBonus;
        public int coffers;
    }

    public sealed class PriorLine
    {
        public int generation = -1;
        public string heirId;
        public string riteId;
        public string[] victimNames;
    }

    // ── data/rites.json ──────────────────────────────────────────────────────
    public sealed class RiteFile { public RiteDef[] rites; }

    public sealed class RiteDef
    {
        public string id;
        public string name;
        public string note;
        public string requiresVictimTier;   // "" 면 사람을 바치지 않는다
        public int requiresCoffers = -1;
        public int payPercent;
        public int releaseProgress;
        /// <summary>
        /// 미납분을 노여움으로 바꾸지 않고 **그 대의 요구를 뒤로 미루는** 제례.
        /// -1 이면 보통대로 노여움·속박 손실로 값을 치른다 (값 없는 int 는 -1).
        /// 검사기 NoDominantStrategy 가 「장부를 덮는다」를 죽은 선택지로 잡아내서 생긴 규칙이다.
        /// </summary>
        public int deferUnpaidGenerations = -1;
        public PendingDef immediate;
        public PendingDef[] deferred;

        public bool TakesVictim { get { return !string.IsNullOrEmpty(requiresVictimTier); } }
    }

    // ── data/balance.json ────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public FlowBalance flow;
        public LimitBalance limits;
        public EndingBalance ending;
        public ScoreBalance score;
        public CheckerBalance checkers;
    }

    public sealed class FlowBalance
    {
        public int blessingPerBindingPercent;
        public int blightPerWrathPercent;
        public int prosperityDecayPerGeneration;
        public int resentmentEasePerGeneration;
        public int wrathPerUnpaidPercent;
        public int bindingLossPerUnpaidPercent;
        public int surplusBindingPercent;
        public int postponeInterestPercent;
    }

    public sealed class LimitBalance
    {
        public int prosperityMax;
        public int bindingMax;
        public int wrathMax;
        public int houseVitalityMax;
        public int resentmentMax;
        public int coffersMax;
    }

    public sealed class EndingBalance
    {
        public int uprisingResentment;
        public int releaseComplete;
        public int releaseFamineDrop;
        public int villageAliveFloor;
    }

    public sealed class ScoreBalance
    {
        public int perSurvivedGeneration;
        public int perProsperity;
        public int perHouseVitality;
        public int perVictim;
        public int perResentment;
        public int villageAliveBonus;
    }

    public sealed class CheckerBalance
    {
        public int exhaustiveGenerations;
        public int[] noCleanExitSeeds;
        public int minSurvivingPoliciesWithVictims;
        public int minDistinctRitesInBestPolicy;
        public int monoDominanceMarginPoints;
        public int deferredDivergenceMinPolicies;
        public int deferredMinChangedEndings;
        public int minAncestorShareInGenerationPercent;
        public int minRitesMajorityDeferred;
        public int minLongestDelayGenerations;
    }
}
