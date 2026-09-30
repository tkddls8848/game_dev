namespace Graft.Data
{
    // ── data/net.json ────────────────────────────────────────────────────────
    // 뿌리 CLAUDE.md 설계 원칙 3: 배열 + 문자열 ID 로 평평하게. 값 없는 int 는 -1 을 명시한다.

    public sealed class NetFile
    {
        public PersonDef[] people;
        public PlaceDef[] places;
        public MoodDef[] moods;
        /// <summary>정서 거리 행렬. moods.Length^2 를 평평하게 편 것. 0~100, 대각은 0, 대칭.</summary>
        public int[] moodDistance;
        public SubjectDef[] subjects;
    }

    /// <summary>
    /// 사람. presentFromDay~presentUntilDay 밖에서는 그 사람이 있을 수 없다.
    /// **이 창은 의뢰서에 적혀 있다** — 플레이어가 늘 안다. 그래서 이것으로 죽는 것은 공정하다.
    /// </summary>
    public sealed class PersonDef
    {
        public string id;
        public string name;
        public int presentFromDay = -1;
        public int presentUntilDay = -1;   // -1 이면 지금도 있다
        public string note = "";
    }

    /// <summary>장소. 생기기 전에도 헐린 뒤에도 그 자리에 기억을 둘 수 없다. 창은 공개 정보다.</summary>
    public sealed class PlaceDef
    {
        public string id;
        public string name;
        public int existsFromDay = -1;
        public int existsUntilDay = -1;    // -1 이면 지금도 있다
        public string note = "";
    }

    public sealed class MoodDef { public string id; public string name; }

    public sealed class SubjectDef
    {
        public string id;
        public string name;
        public string note = "";
        /// <summary>꿈에 들어가자마자 떠 있는 기억. 나머지는 캐물어야(probe) 뜬다.</summary>
        public string[] surfacedAtStart;
        /// <summary>한 회차에 실제로 스크린 기억이 되는 개수. distortable 후보 중에서 씨드가 고른다.</summary>
        public int distortedPerSession = -1;
        public MemoryDef[] memories;
        public LinkDef[] links;
    }

    /// <summary>
    /// 기억 하나.
    ///
    /// **겉값과 참값이 다를 수 있다** — 스크린 기억(screen memory)이다. 겉값은 한 번 캐물으면 보이고,
    /// 참값은 서로 다른 간선 둘로 겹쳐 캐물어야 드러난다. 거부 반응은 **참값**으로 일어난다.
    /// 그래서 겉값만 믿고 심으면 예측이 어긋나고, 그 어긋남은 「겹쳐 물 수 있었다」로 공정해진다.
    /// </summary>
    public sealed class MemoryDef
    {
        public string id;
        public string name;
        public int dayIndex = -1;
        public int spanDays = -1;
        public string placeId = "";
        public string[] peopleIds;
        public string moodId = "";
        public int intensityPercent = -1;
        /// <summary>굳기. 이 기억과 어긋났을 때 떨림이 얼마나 센가.</summary>
        public int fixityPercent = -1;

        /// <summary>이 회차에 스크린 기억이 될 수 있는가. 되면 아래 true* 가 참값이다.</summary>
        public bool distortable = false;
        public int trueDayIndex = -1;          // -1 이면 겉값과 같다
        public string truePlaceId = "";        // "" 이면 겉값과 같다
        public string trueMoodId = "";         // "" 이면 겉값과 같다

        /// <summary>누가 심은 기억이면 심은 날. -1 이면 제 기억이다. selfnet.json 에서 쓴다.</summary>
        public int graftedOnDay = -1;
        public string note = "";
    }

    /// <summary>
    /// 기억 사이의 연상. 캐묻는 길이면서 동시에 떨림이 타고 흐르는 줄이다.
    /// kind: place · person · time · cause
    /// </summary>
    public sealed class LinkDef
    {
        public string a;
        public string b;
        public string kind;
        /// <summary>이 줄을 타고 건너가 저쪽 기억을 띄우는 값(명료도).</summary>
        public int probeCost = -1;
    }

    // ── data/commissions.json ────────────────────────────────────────────────

    public sealed class CommissionFile { public CommissionDef[] commissions; }

    /// <summary>
    /// 의뢰 하나. **무엇을 심을지는 의뢰인이 정하고, 어디에 어떻게 끼울지는 플레이어가 고른다.**
    /// 고를 수 있는 것: 이어 붙일 기억(anchor) · 장소 · 날 · 정서 · 세기.
    /// </summary>
    public sealed class CommissionDef
    {
        public string id;
        public string name;
        public string subjectId;
        public string clientNote = "";
        public string graftName = "";

        public string[] requiredPeopleIds;   // 의뢰인이 반드시 넣으라고 한 사람
        public string[] allowedPlaceIds;
        public int dayWindowStart = -1;
        public int dayWindowEnd = -1;
        public string[] allowedMoodIds;
        public int intensityMin = -1;
        public int intensityMax = -1;
        public int intensityStep = -1;
        public int spanDays = -1;

        /// <summary>캐묻는 데 쓸 수 있는 명료도 총량.</summary>
        public int lucidityBudget = -1;
        /// <summary>떨림 총합 허용치. 넘으면 꿈이 거부한다.</summary>
        public int toleranceBudget = -1;
        /// <summary>간선 하나의 떨림 상한. 하나라도 넘으면 총합과 무관하게 거부한다.</summary>
        public int singleEdgeCap = -1;
        public int seed = -1;
    }

    // ── data/balance.json ────────────────────────────────────────────────────

    public sealed class BalanceFile
    {
        public int seed = -1;
        public int trialSeeds = -1;

        /// <summary>날을 고를 때의 격자. 하루 단위로 다 보면 탐색이 쓸데없이 커진다.</summary>
        public int dayStep = -1;

        /// <summary>겹쳐 물어 참값이 드러나는 데 필요한 서로 다른 간선 수.</summary>
        public int corroborateRoutes = -1;

        // 규칙 저울. 떨림 = weight * magnitude. **나눗셈이 한 번도 없다** (설계 원칙 4).
        public int timePlaceWeight = -1;
        public int personElsewhereWeight = -1;
        public int personAbsentWeight = -1;
        public int placeWindowWeight = -1;
        public int moodAnchorWeight = -1;
        public int eraWeight = -1;
        public int intensityWeight = -1;

        public int moodTolerance = -1;       // 이어 붙인 기억과의 정서 거리 허용
        public int eraTolerance = -1;        // 그 시절 전체와의 정서 거리 허용
        public int eraWindowDays = -1;       // 그 시절의 폭
        public int intensityTolerance = -1;

        /// <summary>엔진이 낼 수 있는 규칙 이름 전부. 여기 없는 규칙이 떨림을 내면 불공정이다.</summary>
        public string[] statedRules;

        public SelfSignatureDef selfSignature;
    }

    /// <summary>
    /// 심은 기억이 남기는 흔적. **플레이어 자신의 기억을 같은 자로 재는 문턱이다.**
    ///
    /// 잔여 떨림 **총량**을 문턱으로 두면 안 된다는 것을 한 번 배웠다 — 자연스러운 기억망도
    /// 가장 가까운 기억과 정서가 반대인 짝을 흔히 갖는다(운동장에서 넘어진 일 ↔ 선화를 처음 본 일: 3725).
    /// 그래서 **가장 센 떨림 하나를 뺀 나머지**를 본다. 둘째 어긋남부터가 흔적이다.
    /// </summary>
    public sealed class SelfSignatureDef
    {
        /// <summary>가장 센 떨림을 뺀 나머지가 이 값을 넘어야 흔적으로 본다.</summary>
        public int secondaryFloor = -1;
        /// <summary>서로 다른 이유가 몇 개는 있어야 한다. 하나뿐이면 그냥 삶이 그런 것이다.</summary>
        public int minTrembleEdges = -1;
    }

    // ── data/selfnet.json ────────────────────────────────────────────────────
    // 같은 스키마(NetFile)를 쓴다. 다른 것은 memories 에 graftedOnDay 가 박혀 있다는 점뿐이다.
}
