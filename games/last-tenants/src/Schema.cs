namespace Tenants.Data
{
    // ── data/needs.json ──────────────────────────────────────────────────────
    public sealed class NeedFile { public NeedDef[] needs; }

    /// <summary>도와야 하는 것 하나. slotsRequired 는 하루 여섯 칸 중 몇 칸을 먹는가이고
    /// graceCost 는 건물에 정해진 유예 일수를 몇 개 먹는가다. 둘 다 1 이상이어야 한다 —
    /// graceCost 가 0 이면 「고르면 다 잘 되는」 선택지가 생겨 딜레마가 사라진다.</summary>
    public sealed class NeedDef
    {
        public string id;
        public string name;
        public int slotsRequired;
        public int graceCost;
        /// <summary>그 사정이 얼마나 크게 보이는가. 실제 손실이 아니다 — 정책 검사기가 짐작으로 쓴다.</summary>
        public int perceivedWeight;
        public string note;
    }

    // ── data/buildings.json ──────────────────────────────────────────────────
    public sealed class BuildingFile { public BuildingDef[] buildings; }

    public sealed class BuildingDef
    {
        public string id;
        public string name;
        public string address;
        public string noticeNo;
        public string noticeDate;
        public string demolitionNote;
        public int seed;
        public int graceDaysTotal;
        public string note;
        public UnitDef[] units;
    }

    /// <summary>칸 하나. 인접은 데이터에 적지 않고 floor·side 에서 끌어낸다(GameData.AreAdjacent).</summary>
    public sealed class UnitDef
    {
        public string id;
        public string unitNo;
        public int floor;
        public string side;
        public string householdId;
    }

    // ── data/households.json ─────────────────────────────────────────────────
    public sealed class HouseholdFile { public HouseholdDef[] households; }

    public sealed class HouseholdDef
    {
        public string id;
        public string buildingId;
        public string unitId;
        public string name;
        /// <summary>audible 또는 silent. silent 면 그 문에서는 cue 가 하나도 나지 않는다.</summary>
        public string voice;
        /// <summary>none · gone · unable. 문 앞에서는 gone 과 unable 이 구별되지 않는다.</summary>
        public string silenceKind;
        public string needId;
        public int baseStakes;
        public int spilloverWeightPercent;
        public int ambientLoudnessPercent;
        public string note;
    }

    public static class Voices
    {
        public static string Audible { get { return "audible"; } }
        public static string Silent { get { return "silent"; } }
    }

    public static class SilenceKinds
    {
        public static string None { get { return "none"; } }
        /// <summary>이미 나갔다. 침묵에 아무것도 없다.</summary>
        public static string Gone { get { return "gone"; } }
        /// <summary>사람이 남았는데 말할 수 없다. 이 건물에서 가장 급하다.</summary>
        public static string Unable { get { return "unable"; } }
    }

    // ── data/cues.json ───────────────────────────────────────────────────────
    public sealed class CueFile { public CueDef[] cues; }

    public sealed class CueDef
    {
        public string id;
        public string atHouseholdId;
        public string aboutHouseholdId;
        public int[] slotOptions;
        public int loudnessPercent;
        public string effect;
        public string needId;
        public bool falsifiable;
        public string misleadNeedId;
        public string text;
    }

    public static class CueEffects
    {
        public static string Hint { get { return "hint"; } }
        public static string Need { get { return "need"; } }
        public static string Stakes { get { return "stakes"; } }
        public static string Vacant { get { return "vacant"; } }
        public static string Mislead { get { return "mislead"; } }
    }

    // ── data/balance.json ────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public DayBalance day;
        public HelpBalance help;
        public SpilloverBalance spillover;
        public SilenceBalance silence;
        public NightBalance night;
        public PolicyBalance policy;
        public CheckerBalance checkers;
        public int seed;
    }

    public sealed class DayBalance
    {
        public int slotCount;
        public int slotMinutes;
        public int dayMinutes;
        public int startHour;
        public string[] slotNames;
    }

    public sealed class HelpBalance
    {
        public int[] reliefPercentByLevel;
        public int wrongNeedReliefPercent;
        public int blindGraceCost;
        public int blindSlotsRequired;
    }

    public sealed class SpilloverBalance { public int perGraceFlat; }

    public sealed class SilenceBalance { public bool gateEnabled; }

    public sealed class NightBalance { public int lieCountPerBuilding; public int seedSweep; }

    public sealed class PolicyBalance { public int hintEstimate; }

    public sealed class CheckerBalance
    {
        public int paretoFrontMin;
        public int paretoFrontMeanTenths;
        public int silenceIgnoreGapMin;
        public int silenceKindGapMin;
        public int silenceGoneVsNothingMin;
        public int silenceWorldGapMin;
        public int silencePolicyGapMin;
        public int minDistinctBestChoices;
    }
}
