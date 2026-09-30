namespace MapLies.Data
{
    // ── data/city.json ───────────────────────────────────────────────────────
    // 설계 원칙 3: 배열 + 문자열 ID 로 평평하게. 값 없는 int 는 -1 을 명시한다.
    // 격자는 줄 문자열로 적는다 — 사람이 열어서 도시를 볼 수 있어야 데이터를 고칠 수 있다.
    //   B 건물(Block) · S 길(Street) · P 광장(Plaza) · . 미지정(지도가 아직 안 그린 곳, plan 에만)

    public sealed class CityFile
    {
        public int width = -1;
        public int height = -1;
        /// <summary>지금 실제로 서 있는 도시. 흑백 항공사진에 해당한다.</summary>
        public string[] actualRows;
        /// <summary>지도(트레이싱지)가 그려 둔 것. `.` 은 아직 안 그린 칸이다.</summary>
        public string[] planRows;
        /// <summary>도시 밖으로 나가는 문. 사람이 여기에 닿을 수 있어야 갇히지 않은 것이다.</summary>
        public CellRef[] exits;
        /// <summary>고칠 수 없는 칸(문·간선도로). 테두리는 balance.borderLocked 가 따로 잠근다.</summary>
        public CellRef[] lockedCells;
        public CitizenDef[] citizens;
        public DistrictDef[] districts;
    }

    public sealed class CellRef { public int x = -1; public int y = -1; }

    public sealed class CitizenDef
    {
        public string id;
        public string name;
        public int x = -1;
        public int y = -1;
        /// <summary>갇힌 하루에 쌓이는 해. 정수다 — 되돌릴 수 없는 값이므로 반올림을 두지 않는다.</summary>
        public int harmPerDayTrapped = -1;
        /// <summary>
        /// 집에서 몇 칸까지 가는가(맨해튼 거리). 사람은 사는 데서 산다 —
        /// 도시 전역을 돌아다니게 두면 누가 갇힐지가 일어나는 일이 아니라 운이 된다.
        /// 0 이면 자리를 뜨지 않는다. 건물이 덮으면 밀려나는 것은 이 값을 보지 않는다.
        /// </summary>
        public int walkRadius = -1;
        public string note = "";
    }

    /// <summary>이름 붙은 구역. 목업이 「무른 구역」을 가리킬 수 있어야 한다.</summary>
    public sealed class DistrictDef
    {
        public string id;
        public string name;
        public CellRef[] cells;
        public string note = "";
    }

    // ── data/chapters.json ───────────────────────────────────────────────────

    public sealed class ChapterFile { public ChapterDef[] chapters; }

    /// <summary>
    /// 한 장(章). 시의회가 이번에 요구하는 것.
    /// 목표는 셋을 겹쳐 쓴다 — 하나만 두면 「전부 길로 덮기」가 늘 답이 된다.
    /// </summary>
    public sealed class ChapterDef
    {
        public string id;
        public string name;
        public string councilNote = "";

        /// <summary>이 칸들이 문에 닿아야 한다.</summary>
        public CellRef[] reachCells;
        /// <summary>길에 붙은 건물 칸이 이만큼은 있어야 한다. -1 이면 보지 않는다.</summary>
        public int dwellingMin = -1;
        /// <summary>이어진 광장 덩어리가 이만큼은 되어야 한다. -1 이면 보지 않는다.</summary>
        public int plazaMin = -1;
        /// <summary>그 광장 덩어리에 반드시 들어가야 하는 칸.</summary>
        public CellRef plazaSeed;
        /// <summary>한 사람이라도 갇히면 실패다.</summary>
        public bool requireNoTrapped = true;

        public int dayLimit = -1;
        public int permitBudget = -1;
        public int goalReward = -1;
        public int seed = -1;
    }

    // ── data/scenarios.json ──────────────────────────────────────────────────

    public sealed class ScenarioFile { public MistakeDef[] mistakes; }

    /// <summary>
    /// 잘못 그은 선 하나. **MistakesAreSurvivable 이 되돌릴 수 있는지를 보는 대상이다.**
    /// expectTrapped 에 적은 사람이 실제로 갇혀야 한다 — 갇히지 않으면 이 시나리오가 아무것도 시험하지 않는다.
    /// </summary>
    public sealed class MistakeDef
    {
        public string id;
        public string name;
        public string note = "";
        /// <summary>
        /// 이 사고가 일어나기 전의 지도. 이 장을 Targeted 로 해결해 도시가 수렴한 다음에
        /// 아래 edits 를 그어 보는 것이다. 보댈발 지도에서 시작하면 **어느 선이 사람을 가뇐는지**가
        /// 섞여 이 시나리오가 아무것도 시험하지 않게 된다.
        /// </summary>
        public string basisChapterId = "";
        public EditDef[] edits;
        public string[] expectTrapped;
        public int seed = -1;
    }

    public sealed class EditDef
    {
        public int x = -1;
        public int y = -1;
        /// <summary>B · S · P 중 하나. 미지정으로 되돌리는 것은 없다 — 한 번 그린 선은 지워도 자국이 남는다.</summary>
        public string kind = "";
    }

    // ── data/balance.json ────────────────────────────────────────────────────

    public sealed class BalanceFile
    {
        public int seed = -1;
        public int trialSeeds = -1;

        // 도시가 지도에 맞추는 규칙
        /// <summary>지도가 그린 칸에 하루치로 쌓이는 공사 압력.</summary>
        public int conformPressurePerDay = -1;
        /// <summary>미지정 칸에 도시가 스스로 붓는 압력. 낮다 — 시가 제 돈으로 하는 일이다.</summary>
        public int inferPressurePerDay = -1;
        public int flipCostBlock = -1;
        public int flipCostStreet = -1;
        public int flipCostPlaza = -1;

        // 도시가 미지정 칸을 스스로 메우는 규칙
        /// <summary>길 이웃이 이만큼 이상이면 **건물**을 세운다 (교차로 가운데를 비워 두지 않는다).</summary>
        public int inferBlockOverStreets = -1;
        /// <summary>길 이웃이 이만큼 이상이면 길을 뚫는다.</summary>
        public int inferStreetMin = -1;
        public int inferPlazaMin = -1;

        /// <summary>
        /// **수렴을 보장하는 단 하나의 장치.** 한 필지를 이만큼만 다시 지을 수 있다.
        /// 다 쓰면 도시가 거부한다 — 지도가 무슨 말을 해도 그 자리는 그대로 남는다.
        /// -1 이면 무한(음성 대조군. 진동한다).
        /// </summary>
        public int maxFlipsPerCell = -1;

        public int convergeDays = -1;
        /// <summary>이 날 수만큼 아무 칸도 바뀌지 않으면 닿은 것으로 본다.</summary>
        public int quietDays = -1;

        // 값
        public int editCostBlock = -1;
        public int editCostStreet = -1;
        public int editCostPlaza = -1;
        /// <summary>같은 칸을 다시 고치면 붙는 값. 지우개 자국은 공짜가 아니다.</summary>
        public int redrawSurcharge = -1;
        public int entombExtraHarm = -1;
        public int harmPenaltyPerPoint = -1;

        // 구조
        public bool borderLocked = true;
        public int rescueMaxDays = -1;
        public int rescueMaxEdits = -1;
        /// <summary>갇힌 사람에서 몇 걸음 이안을 그어 볼 것인가. 도시 반대편을 그어 보는 것은 사람이 하는 일이 아니다.</summary>
        public int rescueFrontier = -1;
        /// <summary>사람이 하루에 걸어 옮기는 칸 수.</summary>
        public int walkStepsPerDay = -1;
    }
}
