// 데이터 스키마. PLAN_GENRES.md §4.6.
//
// 규칙 넷만 지킨다:
//   1. using UnityEngine 금지 (뿌리 CLAUDE.md 설계 원칙 1)
//   2. 수치는 전부 정수. 시각은 ms, 거리는 구역 홉 수, 확률·비율은 백분율 정수
//   3. 배열 + 문자열 ID로 평평하게. Dictionary·다형성·중첩 배열을 쓰지 않는다
//      (JsonUtility로 옮겨도 그대로 읽히게 두려는 것이다)
//   4. 값 없는 int 필드는 -1을 명시한다

namespace Tactics.Data
{
    // ── map.json ────────────────────────────────────────────────────────────
    //
    // 지도는 좌표가 아니라 **구역 + 인접 관계**다. mystery-blackwood의 rooms.json이
    // 벽 맞닿음으로 가청을 판정한 것과 같은 이유 — 좌표를 넣으면 경로 탐색이
    // 먼저 필요해지고 PoC 범위를 넘는다.

    public sealed class MapFile
    {
        public MapDef[] maps;
    }

    public sealed class MapDef
    {
        public string id;
        public string name;
        public string entryZone;         // 분대가 여기서 시작한다
        public string extractionZone;    // 여기로 돌아와야 임무가 닫힌다
        public ZoneDef[] zones;
        public LinkDef[] links;          // 인접 관계. 이동 경로이자 소음 전파 경로
        public SightDef[] sightLines;    // from 구역에 선 사람이 보는 구역들 (차폐 반영)
        public LinkDef[] noiseBlocked;   // 지형 차폐. 이 쌍은 소음이 추가로 깎인다
        public ConcealDef[] concealment; // 은폐 지점
        public string[] reconVantages;   // 정찰 단계에 앉아 볼 수 있는 구역
    }

    public sealed class ZoneDef
    {
        public string id;
        public string name;
        public int coverPercent;         // 엄폐율. stealth + cover >= 100 이면 보이지 않는다
        public string note;
    }

    /// <summary>구역 쌍 하나. 인접 관계와 소음 차폐에 같은 모양을 쓴다.</summary>
    public sealed class LinkDef
    {
        public string a;
        public string b;
        public int extraAttenuation;     // noiseBlocked 에서만 쓴다. 인접 목록에서는 -1
    }

    public sealed class SightDef
    {
        public string from;
        public string[] sees;
    }

    public sealed class ConcealDef
    {
        public string id;
        public string zone;
        public string name;
        public int hideBonusPercent;
        public int settleMs;
    }

    // ── patrols.json ────────────────────────────────────────────────────────

    public sealed class PatrolFile
    {
        public GuardDef[] guards;
        public ShiftChangeDef[] shiftChanges;
    }

    public sealed class GuardDef
    {
        public string id;
        public string name;
        public string mapId;
        public PatrolLegDef[] legs;      // 순서대로 돈다. dwellMs 의 합이 순찰 주기다
        public int[] phaseOptionsMs;     // mission.seed 가 이 중 하나를 고른다 (정찰로 관측된다)
        public int hearingThresholdPercent;
    }

    public sealed class PatrolLegDef
    {
        public string zone;
        public int dwellMs;              // 이 구간에 머무는 시간
    }

    /// <summary>교대 시각. 이 시각에 순찰병의 주기가 newPhaseMs 로 갈아탄다.</summary>
    public sealed class ShiftChangeDef
    {
        public string id;
        public string guardId;
        public int atMs;
        public int newPhaseMs;
        public string note;
    }

    // ── squad.json ──────────────────────────────────────────────────────────

    public sealed class SquadFile
    {
        public MemberDef[] members;
    }

    public sealed class MemberDef
    {
        public string id;
        public string name;
        public string role;
        public int stealthPercent;
        public int moveMsPerZone;        // 구역 하나를 건너는 데 드는 시간
        public int canEngage;            // 1 = 교전 가능. 정찰병은 0
        public MemberActionDef[] actions;
    }

    public sealed class MemberActionDef
    {
        public string kind;              // Shoot · PlantTrap · PlantCharge · Detonate · Infiltrate
        public int durationMs;
        public int exposurePercent;      // 수행 중에는 이만큼 은폐가 깎인다
        public int noiseLoudness;
        public int charges;              // 회차당 횟수. 무제한은 -1
        public int rangeZones;           // 사거리(홉). 같은 구역에서만 되면 0, 안 쓰면 -1
    }

    // ── mission.json ────────────────────────────────────────────────────────

    public sealed class MissionFile
    {
        public MissionDef[] missions;
        public TargetDef[] targets;
        public IntelDef[] intel;
    }

    public sealed class MissionDef
    {
        public string id;
        public string name;
        public string mapId;
        public int seed;
        public int lengthMs;
        public int extractDeadlineMs;
        public string[] guardIds;
        public string[] shiftChangeIds;
        public string[] eliminateGuardIds;
        public string[] destroyTargetIds;
        public string[] seizeIntelIds;
        public int alarmThreshold;       // 이 값에 닿으면 즉시 실패한다
        public int maxAlarm;             // 승리로 인정되는 최대 경보치
        public string brief;
    }

    public sealed class TargetDef
    {
        public string id;
        public string mapId;
        public string zone;
        public string name;
    }

    public sealed class IntelDef
    {
        public string id;
        public string mapId;
        public string zone;
        public string name;
    }

    // ── camp.json ───────────────────────────────────────────────────────────

    public sealed class CampFile
    {
        public int seed;
        public SupplyDef[] supplies;
        public ActionCostDef[] actionCosts;
        public MissionRewardDef[] missionRewards;
        public RepairDef[] repairs;
        public RecoveryDef recovery;
        public RestRewardDef[] restRewards;
    }

    public sealed class SupplyDef
    {
        public string id;
        public string name;
        public int startAmount;
        public int floor;                // 이 밑으로 내려가면 진행이 막힌다
        public int ceiling;              // 거점 저장 한계. 넘치면 버린다
    }

    public sealed class ActionCostDef
    {
        public string actionKind;
        public string supplyId;
        public int amount;
    }

    public sealed class MissionRewardDef
    {
        public string missionId;
        public string supplyId;
        public int onSuccess;
        public int onFailure;
    }

    public sealed class RepairDef
    {
        public string id;
        public string name;
        public string supplyId;          // 무엇으로 값을 치르는가
        public int cost;
        public string bonusSupplyId;     // 무엇이 매 임무 늘어나는가
        public int bonusPerMission;
    }

    public sealed class RecoveryDef
    {
        public int woundedRecoverMissions;   // 몇 회차를 쉬어야 돌아오는가
        public string medicineSupplyId;
        public int medicineCost;             // 회복에 드는 값. 0이면 손실이 공짜가 된다
        public string trustSupplyId;
        public int trustPenaltyOnLoss;
        public int lossChancePercent;        // 성공한 임무에서도 누군가 다칠 확률(백분율 정수)
    }

    /// <summary>임무를 못 나간 회차에 마을이 내주는 것. 진행이 완전히 막히지 않게 하는 밸브다.</summary>
    public sealed class RestRewardDef
    {
        public string supplyId;
        public int amount;
    }

    // ── balance.json ────────────────────────────────────────────────────────

    public sealed class BalanceFile
    {
        public int tickMs;
        public NoiseBalance noise;
        public VisionBalance vision;
        public AlarmBalance alarm;
        public EngagementBalance engagement;
        public SearchBalance search;
        public CheckerBalance checkers;
    }

    public sealed class NoiseBalance
    {
        public int hopAttenuation;
        public int hearingThresholdPercent;
        public int blockedExtraAttenuation;
    }

    public sealed class VisionBalance
    {
        public int concealFloorPercent;      // stealth+cover-노출 이 이 값 이상이면 보이지 않는다
        public int sameZonePenaltyPercent;   // 같은 구역에 순찰병이 있으면 깎이는 양
    }

    public sealed class AlarmBalance
    {
        public int perSighting;
        public int perNoiseHeard;
    }

    public sealed class EngagementBalance
    {
        public int engageDelayMs;            // 이만큼 계속 보이면 쓰러진다
    }

    public sealed class SearchBalance
    {
        public int gridMs;
        public int beamWidth;
        public int maxActions;
        public int seed;
    }

    public sealed class CheckerBalance
    {
        public int minWinningPlansPerMission;
        public int minDistinctSignaturesPerMission;
        public int failureSampleTarget;
        public int economyMissions;
        public int supplyDriftTolerance;
        public int maxCeilingClampRounds;
        public int maxFloorClampRounds;
    }
}
