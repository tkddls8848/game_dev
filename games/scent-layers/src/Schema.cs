using System.Collections.Generic;

namespace Scent.Data
{
    // ── data/notes.json ───────────────────────────────────────────────────────
    public sealed class NotesFile
    {
        public int decayStepMin;        // 감쇠 한 걸음의 길이(분). 방문 시각도 이 격자 위에 있다
        public int perceptionFloor;     // 이 아래의 성분은 **코에 잡히지 않는다**. 합에서 아예 빠진다
        public List<NoteDef> notes;
    }

    /// <summary>향 계열 하나. retainPermille 은 한 걸음마다 남는 천분율이다.</summary>
    public sealed class NoteDef
    {
        public string id;
        public string ko;
        public string klass;            // top(머리) / heart(중간) / base(잔향)
        public int retainPermille;
        public string color;            // 크로마토그램 띠 색. 연출이 읽는다
        public int bandOrder;
    }

    // ── data/house.json ───────────────────────────────────────────────────────
    public sealed class HouseFile
    {
        public string ko;
        public string entry;
        public List<RoomDef> rooms;
        public List<EdgeDef> edges;
    }

    public sealed class RoomDef { public string id; public string ko; }
    public sealed class EdgeDef { public string a; public string b; public int walkMin; }

    // ── data/actors.json ──────────────────────────────────────────────────────
    public sealed class ActorsFile { public List<ActorDef> actors; }

    public sealed class ActorDef
    {
        public string id;
        public string ko;
        public string role;
        public string marker;           // 이 사람만 가진 잔향. 알아보는 근거다
        public List<NoteWeight> profile;
    }

    public sealed class NoteWeight { public string note; public int permille; }

    // ── data/day.json ─────────────────────────────────────────────────────────
    public sealed class DayFile
    {
        public int seed;
        public int depositUnits;        // 방문 한 번이 남기는 총량(100%일 때)
        public int dayStartMin;
        public int dayEndMin;
        public List<VisitDef> visits;
        public AmbientSpec ambient;
    }

    /// <summary>복원해야 할 사실 하나 = 누가 · 어느 방에 · 언제 다녀갔는가.</summary>
    public sealed class VisitDef
    {
        public string id;
        public string actor;
        public string room;
        public int atMin;
        public int strengthPercent;     // 얼마나 진하게 남겼는가. **플레이어는 이 값을 모른다**
        public string ko;
    }

    /// <summary>집 자체의 잡내. 씨드가 만든다 — 사실이 아니라 방해물이다.</summary>
    public sealed class AmbientSpec
    {
        public List<string> notePool;
        public int perRoomMin;
        public int perRoomMax;
        public int strengthMin;
        public int strengthMax;
        public int atMinMin;
        public int atMinMax;
    }

    // ── data/balance.json ─────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public InvestigationDef investigation;
        public PerceptionDef perception;
        public CheckerDef checkers;
    }

    public sealed class InvestigationDef
    {
        public int startMin;
        public string startRoom;
        public int sniffCostMin;
        public int maxSniffs;
        public int endMin;
    }

    public sealed class PerceptionDef
    {
        public int dominancePercent;    // 알아보기: 이 비율 이상을 차지한 계열이 있어야 한다
        public int purityPercent;       // 되짚기: 이 비율 이상 순수해야 그 계열로 시각을 잰다
        public int datingFloor;         // 되짚기에 쓰려면 이만큼은 남아 있어야 한다
        public int ratioTolPermille;    // 비율 맞춤의 허용 오차(천분율)
        public int ratioScale;
        public int maxAgeSteps;
    }

    public sealed class CheckerDef
    {
        public int sweepShortfallMin;       // 한 번 훑기가 못 잡는 사실 수의 하한
        public int revisitRoomsMin;         // 전부 복원하려면 되돌아가야 하는 방 수의 하한
        public int fairWindowSlotsMin;      // 모든 사실이 가져야 할 최소 맡을 수 있는 순간 수
        public int campRecoveryMaxPercent;  // 한 방에 죽치는 정책이 넘어서면 안 되는 복원율
        public int seedsToCheck;
    }
}
