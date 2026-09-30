namespace Secretary.Data
{
    // ── data/domains.json ────────────────────────────────────────────────────
    public sealed class DomainFile { public DomainDef[] domains; }

    /// <summary>
    /// 신이 나눠 줄 수 있는 것 하나. **세상에 있는 총량이 정해져 있고 늘지 않는다.**
    /// 고통은 볼록하다 — 견딜 수 있는 한도를 넘은 몫에 severeExtraPerUnit 이 더 붙는다.
    /// </summary>
    public sealed class DomainDef
    {
        public string id;
        public string name;
        public string unitName;
        public int hardshipPerUnit;
        public int severeExtraPerUnit;
        public string note;
    }

    // ── data/souls.json ──────────────────────────────────────────────────────
    public sealed class SoulFile { public SoulDef[] souls; }

    /// <summary>이름을 가진 존재 하나. 대가가 빠지는 자리에는 반드시 이 이름이 붙는다.</summary>
    public sealed class SoulDef
    {
        public string id;
        public string name;
        public string place;
        public string note;
    }

    // ── data/ledgers.json ────────────────────────────────────────────────────
    public sealed class LedgerFile { public VolumeDef[] volumes; }

    public sealed class VolumeDef
    {
        public string id;
        public string name;
        public int seed;
        public int days;
        public string note;
        public HoldingDef[] holdings;
    }

    /// <summary>
    /// 장부의 한 줄. **have 는 need 를 넘지 않는다**(DataValidator 가 본다) —
    /// 그래서 누구에게서 한 단위라도 빼면 반드시 부족이 생기고, 대가 없는 허가가 구조적으로 불가능해진다.
    /// </summary>
    public sealed class HoldingDef
    {
        public string soulId;
        public string domain;
        public int have;
        public int need;
        public int toleranceUnits;
    }

    // ── data/prayers.json ────────────────────────────────────────────────────
    public sealed class PrayerFile { public PrayerDef[] prayers; }

    public sealed class PrayerDef
    {
        public string id;
        public string volumeId;
        public string fromSoulId;
        public string domain;
        public int askUnits;
        public int[] dayOptions;
        public int deferPenaltyUnits;
        public string text;
        /// <summary>★ 이 컨셉의 핵. 들어주려면 여기 적힌 **이름들에게서** 빼야 한다.</summary>
        public SourceDef[] sources;
    }

    public sealed class SourceDef
    {
        public string soulId;
        public int maxUnits;
        public string note;
    }

    // ── data/balance.json ────────────────────────────────────────────────────
    public sealed class BalanceFile
    {
        public DayBalance day;
        public ConservationBalance conservation;
        public SearchBalance search;
        public NightBalance night;
        public CheckerBalance checkers;
        public int seed;
    }

    public sealed class DayBalance
    {
        public int stampsPerDay;
        public bool autoDeferUnprocessed;
        public int deferCapUnits;
    }

    public sealed class ConservationBalance { public bool enabled; }
    public sealed class SearchBalance { public int beamWidth; }
    public sealed class NightBalance { public int seedSweep; }

    public sealed class CheckerBalance
    {
        public int minGrantsInBestRun;
        public int minDeniesInBestRun;
        public int freeGrantProbeMin;
        public int conservationWorldGapMin;
        public int policySpreadMin;
        public int minDistinctBestPolicies;
    }

    public static class Verdicts
    {
        public static string Grant { get { return "허가"; } }
        public static string Deny { get { return "기각"; } }
        public static string Defer { get { return "보류"; } }
    }
}
