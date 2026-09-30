namespace RedPen.Data
{
    // 뿌리 CLAUDE.md 설계 원칙 3: 배열 + 문자열 ID로 평평하게. 값 없는 int 는 -1 을 명시한다.
    // 뿌리 CLAUDE.md 설계 원칙 4: 수치는 전부 정수. 배율은 백분율 정수다.

    // ── data/manuscript.json ─────────────────────────────────────────────────
    public sealed class ManuscriptFile
    {
        public string title;
        public string author;
        public string note;
        public FlawDef[] flaws;
        public SentenceDef[] sentences;
    }

    /// <summary>문장이 앓고 있는 것. weight 만큼 그 문장의 값을 누른다.</summary>
    public sealed class FlawDef
    {
        public string id;
        public string name;
        public string symbol;     // 교정지 여백에 그려지는 표시
        public int weight;
        public string note;
    }

    public sealed class SentenceDef
    {
        public string id;
        public int order;
        public string kind;        // k_open · k_body · k_turn · k_close
        public string text;
        public int quality;        // 0..100. 흠을 빼기 전의 값
        public int voice;          // 0..100. 이 문장에서 작가의 목소리가 들리는 정도
        public int prideGuard;     // 0..100. 작가가 이 문장에 얼마나 붙어 있는가
        public string[] flaws;
        public string note;
    }

    // ── data/marks.json ──────────────────────────────────────────────────────
    public sealed class MarkFile { public MarkDef[] marks; }

    /// <summary>
    /// 붉은 펜 하나. 조작은 대화 선택지가 아니라 **이것을 문장에 얹는 것**이다.
    /// </summary>
    public sealed class MarkDef
    {
        public string id;
        public string name;
        public string symbol;        // 삭제선 · 삽입 갈고리 · 이동 화살표 · 물음표
        public int harshness;        // 0..10
        public string[] fixes;       // 고치는 흠. ["*"] 이면 전부
        public int maxFixes = 1;     // 한 번에 고쳐지는 흠의 수. 한 번에 다 낫지 않는다
        public string introducesFlaw = "";  // ★ 고치면서 새로 생기는 흠. "" 면 없다
        public int introducesPercent = -1;  // -1 이면 생기지 않는다
        public int qualityGain;
        public int voiceDelta;
        public int confidenceDelta;
        public int trustDelta;
        public int stubbornDelta;
        public int removesSentence;  // 1 이면 문장을 지운다
        public int needsTrustMin = -1;      // 이 밑이면 작가가 하지 않는다
        public int needsStubbornMax = -1;   // 이 위면 작가가 버틴다
        public string note;
    }

    // ── data/author.json ─────────────────────────────────────────────────────
    public sealed class AuthorFile
    {
        public string name;
        public string note;
        public int startConfidence;
        public int startTrust;
        public int startStubborn;

        public int stubbornPerHarsh;      // 혹독함 100당 오르는 고집 (백분율 정수)
        public int trustCalmsStubborn;    // 신뢰 100당 내리는 고집
        public int prideAmplifyPercent;   // 아끼는 문장에 손대면 상처가 몇 % 커지는가

        public int ignoreStubborn;        // 고집이 이 위면 거친 표시(harshness>=hardMarkFrom)를 무시한다
        public int hardMarkFrom;

        public int goodConfidence;        // 이 위 + 신뢰도 위면 '좋은 고쳐 쓰기'
        public int goodTrust;
        public int timidConfidence;       // 이 밑이면 '겁먹은 고쳐 쓰기'

        public int reviseGoodQuality;
        public int reviseGoodVoice;
        public int reviseNormalQuality;
        public int reviseNormalVoice;
        public int reviseTimidQuality;
        public int reviseTimidVoice;

        public int dayJitterMin;          // 작가의 그날. 씨드와 회차로만 정한다
        public int dayJitterMax;

        public int wasteConfidence;       // 고칠 것 없는 문장에 그은 줄. 전부 헛수고면 이 값 그대로 깎인다
        public int wasteTrust;
        public int wasteStubborn;
        public int hollowTrust;           // 흠 있는 문장에 얹은 빈 칭찬. 작가도 자기 글의 흠을 안다

        public int quitTrust;             // 이 밑이면 원고를 거둬 간다
        public int silenceConfidence;     // 이 밑이면 더 쓰지 못한다
    }

    // ── data/balance.json ────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public int seed;
        public int[] extraSeeds;
        public int rounds;

        public int masterpieceConfidence;
        public int masterpieceTrust;
        public int masterpieceVoice;
        public int masterpieceQuality;
        public int masterpieceChancePercent;
        public int masterpieceSentenceQuality;
        public int masterpieceSentenceVoice;
        public int masterpieceSentencePride;

        public int publishQuality;
        public int publishVoice;
        public int publishMinSentences;

        public ObjectiveDef[] objectives;
    }

    /// <summary>
    /// 결과를 재는 자. 여럿이어야 한다 — 하나뿐이면 「혹독함은 양날이다」가 물을 것이 없어진다.
    /// </summary>
    public sealed class ObjectiveDef
    {
        public string id;
        public string name;
        public string whose;
        public int qualityWeight;      // 백분율
        public int voiceWeight;
        public int confidenceWeight;
        public int trustWeight;
        public int stubbornPenalty;
        public int masterpieceBonus;
        public int publishBonus;
        public int lostSentencePenalty;  // 지워 버린 문장 하나당
        public int withdrawnPenalty;     // 작가가 원고를 거둬 갔을 때
    }

    // ── data/endings.json ────────────────────────────────────────────────────
    public sealed class EndingFile { public EndingDef[] endings; }

    public sealed class EndingDef
    {
        public string id;
        public int priority;
        public string name;
        public string text;
        public int requiresWithdrawn = -1;   // 1 · 0 · -1
        public int requiresSilenced = -1;
        public int requiresPublishable = -1;
        public int masterpieceMin = -1;
        public int qualityMin = -1;
        public int qualityMax = -1;
        public int voiceMin = -1;
        public int voiceMax = -1;
        public int trustMax = -1;
        public int confidenceMax = -1;
        public int lostSentenceMin = -1;
    }
}
