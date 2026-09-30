namespace SilentBaton.Data
{
    // ── data/sections.json ───────────────────────────────────────────────────
    public sealed class SectionFile { public SectionDef[] sections; }

    public sealed class SectionDef
    {
        public string id;
        public string name;
        public string instrument;
        public string seat;
        public string note;
        public int responsivenessPercent;
        /// <summary>그 무리의 **성향** 쏠림 폭(ms). 시즌 내내 같은 값이 뽑힌다.</summary>
        public int biasRangeMs;
        public int dynHabit;
        public int dynDriftPerBeat;
        /// <summary>활(또는 치는 팔)이 보이는가. 목관·금관은 활이 없다 — 속도를 볼 수 없다.</summary>
        public bool bowVisible;
        public bool breathVisible;
        public bool faceVisible;
        /// <summary>
        /// 이 무리의 눈금 덮어쓰기. -1 이면 balance.json 의 공통 눈금을 쓴다 (값 없는 int 는 -1).
        /// **무리마다 읽히는 방식이 다르다** — 현은 활이 크게 보이고 호흡은 거의 안 보이며,
        /// 목관은 활이 없지만 숨이 가장 잘 보이고, 금관은 뒤에 앉아 표정이 잘 안 보인다.
        /// 이 세 줄이 sections.json 의 note 를 규칙으로 옮긴 것이다.
        /// </summary>
        public int breathQuantMsOverride = -1;
        public int bowQuantMsOverride = -1;
        public int faceQuantLevelOverride = -1;
    }

    // ── data/score.json ──────────────────────────────────────────────────────
    public sealed class ScoreFile
    {
        public int referenceBeatMs;
        public int seasonSeed;
        public PieceDef[] pieces;
    }

    public sealed class PieceDef
    {
        public string id;
        public string name;
        public string note;
        public int seed;
        public BarDef[] bars;

        public int TotalBeats
        {
            get { int n = 0; foreach (BarDef b in bars) n += b.beats; return n; }
        }
    }

    public sealed class BarDef
    {
        public int index;
        public int beats;
        public int beatMs;
        public int requiredDynamic;
        public string[] enteringSectionIds;
        public string note;
    }

    // ── data/cues.json ───────────────────────────────────────────────────────
    public sealed class CueFile { public CueDef[] cues; }

    public sealed class CueDef
    {
        public string id;
        public string name;
        public string gesture;
        /// <summary>양수면 당긴다(늦은 무리를 앞으로). 어긋남이 양수면 늦었다는 뜻이다.</summary>
        public int tempoNudgeMs;
        public int dynamicNudge;

        public bool IsTempo { get { return tempoNudgeMs != 0; } }
        public bool IsDynamic { get { return dynamicNudge != 0; } }
    }

    // ── data/press.json ──────────────────────────────────────────────────────
    public sealed class PressFile
    {
        public string paper;
        public string column;
        public string critic;
        public GradeDef[] grades;
        public NoteDef[] notes;
    }

    public sealed class GradeDef
    {
        public string id;
        public string name;
        public int minTotal = -1;
        public string headline;
        public string body;
    }

    public sealed class NoteDef
    {
        public string id;
        public string kind;    // lag · rush · loud · soft · split · clean
        public string text;
    }

    public static class NoteKinds
    {
        public static string Lag { get { return "lag"; } }
        public static string Rush { get { return "rush"; } }
        public static string Loud { get { return "loud"; } }
        public static string Soft { get { return "soft"; } }
        public static string Split { get { return "split"; } }
        public static string Clean { get { return "clean"; } }
    }

    // ── data/balance.json ────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public VisibleBalance visible;
        public DriftBalance drift;
        public ReviewBalance review;
        public ConductorBalance conductor;
        public SeasonBalance season;
        public CheckerBalance checkers;
    }

    /// <summary>
    /// ★ 보이는 단서 셋의 눈금. **이 눈금이 이 게임의 난이도 전부다** —
    /// 눈금을 0으로 만들면 소리가 들리는 것과 같아지고, 크게 만들면 아무것도 알 수 없다.
    /// </summary>
    public sealed class VisibleBalance
    {
        public int breathQuantMs;
        public int breathLevels;
        public int bowQuantMs;
        public int bowLevels;
        public int faceQuantLevel;
        public int faceLevels;
    }

    public sealed class DriftBalance
    {
        public int nightlyBiasRangeMs;
        public int startOffsetRangeMs;
        public int dynStartJitter;
        public int strainPerDynErrorPercent;
    }

    public sealed class ReviewBalance
    {
        public int accuracyPerMsPercent;
        public int ensemblePerMsPercent;
        public int dynPerLevelPercent;
        public int weightAccuracy;
        public int weightEnsemble;
        public int weightDynamics;
        public int passTotal;
        public int noteThresholdMs;
        public int maxTempoNotes;
        public int pressGainPercent;
        public int noteThresholdLevel;
        public int splitThresholdMs;
    }

    public sealed class ConductorBalance
    {
        public int lookaheadBeats;
        public int dynUrgentUnits;
        public int dynMsPerUnitPercent;
        public int dynBeatEveryN;
        public int minTempoActionMs;
        public int pressTrustPercent;
    }

    public sealed class SeasonBalance
    {
        public int concerts;
        public int lateFrom;
    }

    public sealed class CheckerBalance
    {
        public int[] seeds;
        public int biasGridStepMs;
        public int minEquivalenceGroups;
        public int minGroupMembers;
        public int minAblationDropPoints;
        public int minDynAblationDropPoints;
        public int minImmediateAdvantagePoints;
        public int minImmediateErrorDropPercent;
        public int minPressAdvantagePoints;
        public int minPressSeasonWorstGap;
        public int monoDominanceMarginPoints;
        public int maxMonoPassing;
        public int minWarmPassRate;
        public int maxColdPassRate;
    }
}
