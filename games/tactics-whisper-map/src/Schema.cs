namespace Whisper.Data
{
    // ── data/map.json ────────────────────────────────────────────────────────
    public sealed class MapFile { public MapDef[] maps; }

    public sealed class MapDef
    {
        public string id;
        public string name;
        public string entryZone;
        public string extractionZone;
        public ZoneDef[] zones;
        public LinkDef[] links;
        public SightDef[] sightLines;
        public LinkDef[] noiseBlocked;
        public ConcealDef[] concealment;
    }

    public sealed class ZoneDef
    {
        public string id;
        public string name;
        public int coverPercent;
        public int reprisalWeightPercent;
        public string note;
    }

    /// <summary>인접 한 쌍. extraAttenuation 이 -1 이면 "없다"는 뜻이다 (설계 원칙 3: 값 없는 int 는 -1).</summary>
    public sealed class LinkDef { public string a; public string b; public int extraAttenuation = -1; }

    public sealed class SightDef { public string from; public string[] sees; }

    public sealed class ConcealDef
    {
        public string id;
        public string zone;
        public string name;
        public int hideBonusPercent;
        public int settleMs;
    }

    // ── data/patrols.json ────────────────────────────────────────────────────
    public sealed class PatrolFile { public GuardDef[] guards; public ShiftChangeDef[] shiftChanges; }

    public sealed class GuardDef
    {
        public string id;
        public string name;
        public string mapId;
        public int hearingThresholdPercent;
        public int[] phaseOptionsMs;
        public LegDef[] legs;
    }

    public sealed class LegDef { public string zone; public int dwellMs; }

    public sealed class ShiftChangeDef
    {
        public string id;
        public string guardId;
        public int atMs;
        public int newPhaseMs;
        public string note;
    }

    // ── data/squad.json ──────────────────────────────────────────────────────
    public sealed class SquadFile
    {
        public MemberDef[] members;
        public int trapLoudnessPercent;
        public int chargeLoudnessPercent;
    }

    public sealed class MemberDef
    {
        public string id;
        public string name;
        public int stealthPercent;
        public int moveMsPerZone;
        public int maxTraps;
        public int maxCharges;
        public string note;
        public MemberActionDef[] actions;
    }

    public sealed class MemberActionDef
    {
        public string kind;
        public int durationMs;
        public int noisePercent;
        public int exposurePercent;
        /// <summary>저격수만 쓴다. 자리를 잡고 표적이 보일 때까지 기다리는 상한.</summary>
        public int holdWindowMs;
        public int rangeHops;
    }

    // ── data/village.json ────────────────────────────────────────────────────
    public sealed class VillageFile { public VillagerDef[] villagers; public IntelItemDef[] items; }

    public sealed class VillagerDef
    {
        public string id;
        public string name;
        public string note;
        public int baseCostPercent;
        public int exposureRiskPercent;
        public string[] knows;
    }

    /// <summary>물어서 알 수 있는 사실 하나. kind 는 route · post · shift 셋뿐이다.</summary>
    public sealed class IntelItemDef
    {
        public string id;
        public string kind;
        public string subjectId;   // route/post → guardId · shift → shiftChangeId
        public string name;
        public bool falsifiable;
    }

    public static class IntelKinds
    {
        public static string Route { get { return "route"; } }
        public static string Post { get { return "post"; } }
        public static string Shift { get { return "shift"; } }
    }

    // ── data/mission.json ────────────────────────────────────────────────────
    public sealed class MissionFile
    {
        public MissionDef[] missions;
        public TargetDef[] targets;
        public DocumentDef[] documents;
    }

    public sealed class MissionDef
    {
        public string id;
        public string name;
        public string mapId;
        public int lengthMs;
        public int seed;
        public int alarmLimit;
        public string[] guardIds;
        public string[] shiftChangeIds;
        public bool requireExtraction;
        public bool downAllGuards;
        public string destroyTargetId;
        public string stealDocumentId;
        public bool noGuardsDowned;
        public string note;
    }

    public sealed class TargetDef { public string id; public string mapId; public string zone; public string name; }
    public sealed class DocumentDef { public string id; public string mapId; public string zone; public string name; }

    // ── data/balance.json ────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public int tickMs;
        public NoiseBalance noise;
        public VisionBalance vision;
        public AlarmBalance alarm;
        public EngagementBalance engagement;
        public ReprisalBalance reprisal;
        public TrustBalance trust;
        public AskBalance ask;
        public SearchBalance search;
        public CheckerBalance checkers;
    }

    public sealed class NoiseBalance
    {
        public int hopAttenuation;
        public int hearingThresholdPercent;
        public int blockedExtraAttenuation;
    }

    public sealed class VisionBalance { public int concealFloorPercent; public int sameZonePenaltyPercent; }
    public sealed class AlarmBalance { public int perSighting; public int perNoiseHeard; }
    public sealed class EngagementBalance { public int engageDelayMs; }

    public sealed class ReprisalBalance
    {
        public int perSightingBasePercent;
        public int onFailureFlat;
        public int onAlarmBreachFlat;
        public int trustPerHundredPoints;
    }

    public sealed class TrustBalance
    {
        public int startPercent;
        public int ceilingPercent;
        public int floorPercent;
        public int askFloorPercent;
        public int gainOnSuccess;
        public int lossOnFailure;
    }

    public sealed class AskBalance
    {
        public int[] repeatSurchargePercent;
        public int repeatSurchargeBeyondPercent;
        public int falseChancePercent;
        public int shiftLieDeltaMs;
        public int exposureDecayPerMission;
        public int silenceThresholdPercent;
        public int silenceMissions;
    }

    public sealed class SearchBalance { public int gridMs; public int beamWidth; public int maxActions; public int seed; }

    public sealed class CheckerBalance
    {
        public int minWinningPlansPerMission;
        public int minDistinctSignaturesPerMission;
        public int failureSampleTarget;
        public int falseIntelSeedSweep;
        public int economyMissions;
        public int trustDriftTolerance;
        public int maxCeilingClampRounds;
        public int maxFloorRecoveryMissions;
        public int blindPhaseCombinationsMin;
    }
}
