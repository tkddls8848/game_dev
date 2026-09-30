namespace Interp.Data
{
    // 뿌리 CLAUDE.md 설계 원칙 3: 배열 + 문자열 ID로 평평하게. 값 없는 int 는 -1 을 명시한다.
    // 뿌리 CLAUDE.md 설계 원칙 4: 수치는 전부 정수. 배율은 백분율 정수다.

    // ── data/nations.json ────────────────────────────────────────────────────
    public sealed class NationFile { public NationDef[] nations; }

    public sealed class NationDef
    {
        public string id;
        public string name;
        public string shortName;
        public string tongue;     // 이 나라가 쓰는 말의 이름
        public string note;
    }

    // ── data/treaty.json ─────────────────────────────────────────────────────
    public sealed class TreatyFile { public ClauseDef[] clauses; }

    /// <summary>조약 조항 하나. 어느 변형으로 굳느냐가 이 PoC의 결과물이다.</summary>
    public sealed class ClauseDef
    {
        public string id;
        public int article;          // 제 n 조
        public string name;
        public string unsetText;     // 아직 아무것도 정해지지 않았을 때 조약문에 적히는 말
        public VariantDef[] variants;
    }

    public sealed class VariantDef
    {
        public string id;
        public string text;          // 조약문에 실제로 적히는 문구
        public int favorA;           // 하란에 얼마나 유리한가
        public int favorB;           // 케리아에 얼마나 유리한가
        public string note;
    }

    // ── data/session.json ────────────────────────────────────────────────────
    public sealed class SessionFile
    {
        public string title;
        public string place;
        public StageDef[] stages;
        public MisunderstandingDef[] misunderstandings;
    }

    /// <summary>
    /// 회담의 한 마디. 후보 발화가 여럿이면 **조건에 처음 맞는 것**이 실제로 나온다 —
    /// 앞에서 고른 역어가 뒤에 무슨 말이 나오는지까지 바꾼다.
    /// </summary>
    public sealed class StageDef
    {
        public string id;
        public int index;
        public string label;
        public RoundDef[] rounds;
    }

    public sealed class RoundDef
    {
        public string id;
        public string speaker;       // nation id
        public string listener;      // nation id
        public string topicClause;   // "" 면 어느 조항과도 무관하다
        public string[] revisits;    // 이 마디에서 함께 다시 읽히는 조항들. 조인 직전 낭독이 그렇다
        public string sourceText;    // 말한 사람이 실제로 한 말 (왼쪽 단)
        public string literalGloss;  // 직역. 통역이 무엇을 비틀었는지 보이는 자
        public CondDef condition;    // null 이면 무조건
        public RenderDef[] renderings;
    }

    /// <summary>발화가 나올 조건. -1 은 "따지지 않는다"는 뜻이다.</summary>
    public sealed class CondDef
    {
        public int tensionMin = -1;
        public int tensionMax = -1;
        public int suspicionMin = -1;
        public string clauseId = "";
        public string variantId = "";     // 이 변형으로 굳어 있어야 한다
        public string notVariantId = "";  // 이 변형이 아니어야 한다
    }

    /// <summary>
    /// 후보 역어 하나. register 는 exact(정확) · soft(완곡) · hard(강경) · false(오역).
    /// </summary>
    public sealed class RenderDef
    {
        public string id;
        public string register;
        public string text;          // 통역이 입 밖에 낸 말 (오른쪽 단)
        public string heardAs;       // 듣는 쪽이 받아들인 뜻. 여기가 어긋나면 오해가 남는다
        public int tensionDelta;
        public int trustADelta;
        public int trustBDelta;
        public int suspicionDelta;
        public string setsClause = "";           // "" 면 조항을 건드리지 않는다
        public string setsVariant = "";
        public string seedsMisunderstanding = ""; // "" 면 오해를 심지 않는다
        public int exposureRiskPercent = -1;      // -1 이면 드러날 것이 없다
        public int revealBonusPercent;            // 이 마디에서 앞선 거짓이 드러날 확률에 더한다. 음수면 덮는다
        public string note;
    }

    /// <summary>두 나라가 같은 낱말을 다르게 믿게 된 자리.</summary>
    public sealed class MisunderstandingDef
    {
        public string id;
        public string clauseId;      // "" 면 조항이 아니라 분위기의 문제다
        public string word;          // 어긋난 낱말 그 자체
        public string aBelieves;
        public string bBelieves;
        public string consequence;   // 이것이 남은 채 조인되면 무슨 일이 생기는가
    }

    // ── data/endings.json ────────────────────────────────────────────────────
    public sealed class EndingFile { public EndingDef[] endings; }

    /// <summary>priority 오름차순으로 훑어 **처음 맞는 것**이 결말이다.</summary>
    public sealed class EndingDef
    {
        public string id;
        public int priority;
        public string name;
        public string text;
        public int requiresSigned = -1;     // 1 = 조인됐어야 한다 · 0 = 아니어야 한다 · -1 = 따지지 않는다
        public int tensionMin = -1;
        public int tensionMax = -1;
        public int suspicionMin = -1;
        public int suspicionMax = -1;
        public int standingMin = -1;        // 남은 오해 수
        public int standingMax = -1;
        public int exposedMin = -1;         // 드러난 오역 수
        public int favorGapMin = -1;        // |favorA - favorB|
        public int unsetClauseMin = -1;     // 비워 둔 조항 수
    }

    // ── data/balance.json ────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public int seed;
        public int[] extraSeeds;

        public int startTension;
        public int startTrustA;
        public int startTrustB;
        public int startSuspicion;

        public int collapseTension;     // 이 값 이상이면 그 자리에서 결렬이다
        public int signTrustMin;        // 양쪽 신뢰가 이 밑이면 서명하지 않는다

        public int moodJitterMin;       // 마디마다 붙는 분위기. 씨드와 마디 번호만으로 정해진다
        public int moodJitterMax;

        public int exposureSuspicion;   // 오역이 드러났을 때
        public int exposureTension;
        public int exposureTrust;

        public int misunderstandingFavorLoss;  // 남은 오해 하나가 양쪽 실익에서 깎는 값
        public int misunderstandingTension;    // 조인 때 오해 하나가 올리는 긴장

        public ObjectiveDef[] objectives;
    }

    /// <summary>
    /// 결말을 재는 자. 여럿이어야 한다 — 하나뿐이면 "최선의 정책"이 하나로 정해지고
    /// NoSafeWord 가 물을 것이 없어진다.
    /// </summary>
    public sealed class ObjectiveDef
    {
        public string id;
        public string name;
        public string whose;             // 누가 이 자로 재는가
        public int signBonus;
        public int peaceWeight;          // (100 - tension) 에 곱할 백분율
        public int favorAWeight;         // 백분율
        public int favorBWeight;
        public int suspicionWeight;
        public int standingWeight;
        public int exposedWeight;
        public int unsetWeight;
    }
}
