using System.Collections.Generic;

namespace Phone.Data
{
    // ── data/phone.json ───────────────────────────────────────────────────────
    public sealed class PhoneFile
    {
        public string ko;
        public int seed;
        public int foundAtMin;              // 휴대폰을 주운 시각
        public int lastMinuteMin;           // 이 뒤로는 아무 일도 없다(계산 상한)
        public StackDef stack;
        public BatteryDef battery;
        public NoiseSpec noise;
    }

    public sealed class StackDef
    {
        public int capacity;                // 잠금 화면이 붙들고 있는 알림 수. 넘치면 **오래된 것부터 사라진다**
        public bool previewOn;              // 미리보기(본문·EXIF)가 켜져 있는가
    }

    public sealed class BatteryDef
    {
        public int startPermille;            // 주운 순간의 잔량(천분율)
        public int drainStepMin;             // 잔량이 한 칸 주는 데 걸리는 시간
        public int idleDrainPermillePerStep; // 가만히 둬도 한 칸마다 주는 양
        public int wakeCostPermille;         // 화면을 깨울 때마다 더 주는 양
        public int rateMinGapMin;            // 잔량 두 번을 이만큼 벌려 읽어야 감소율을 잰다
    }

    /// <summary>조사 중에 끼어드는 쓸모없는 알림. 씨드가 만든다 — **밀어내는 것이 일이다.**</summary>
    public sealed class NoiseSpec
    {
        public int countMin;
        public int countMax;
        public int fromMin;
        public int toMin;
        public int gridMin;
        public List<NoiseLine> lines;
    }

    public sealed class NoiseLine { public string app; public string from; public string previewKo; }

    // ── data/notifications.json ───────────────────────────────────────────────
    public sealed class NotificationsFile { public List<NotifDef> notifications; }

    /// <summary>
    /// 알림 하나. **잠금 화면에 보이는 것만** 여기 있다.
    /// 잠금을 풀어야 보이는 것은 data/locked.json 에 따로 있다 — 그 경계가 이 게임의 전부다.
    /// </summary>
    public sealed class NotifDef
    {
        public string id;
        public string kind;                 // message · call · photo · card · system · health · delivery · noise
        public string app;
        public string fromKo;
        public string previewKo;            // 미리보기를 껐다면 이것이 안 보인다
        public int arriveMin;
        public int exifAtMin;               // 사진이 아니면 -1 (설계 원칙 3: 값 없는 int 는 -1 을 명시한다)
        public string exifPlaceKo;          // 사진이 아니면 null
        public bool investigation;          // 조사가 시작된 뒤에 도착하는가 (사람이 적은 것. 검증한다)
    }

    // ── data/locked.json ──────────────────────────────────────────────────────
    public sealed class LockedFile { public List<LockedDef> locked; }

    /// <summary>잠금을 풀어야 보이는 것. **대조군 전용이다** — 이 게임에서는 절대 못 본다.</summary>
    public sealed class LockedDef
    {
        public string id;
        public string ko;
        public string whyKo;
    }

    // ── data/case.json ────────────────────────────────────────────────────────
    public sealed class CaseFile
    {
        public string ko;
        public List<FactDef> facts;
        public List<VerdictDef> verdicts;
    }

    /// <summary>
    /// 증거에서 곧바로 나오는 사실 하나. needs 를 **전부** 쥐어야 성립한다.
    /// 증거 딱지는 셋 중 하나다:
    ///   e_xxx@card    — 알림이 있었다는 것 (앱·보낸이·시각). 미리보기를 꺼도 보인다
    ///   e_xxx@preview — 미리보기 본문. 미리보기를 끄면 사라진다
    ///   e_xxx@exif    — 사진의 촬영 시각·장소. 미리보기를 끄면 사라진다
    ///   battery@rate  — 잔량을 충분히 벌려 두 번 읽어야 나온다
    /// </summary>
    public sealed class FactDef
    {
        public string id;
        public string ko;
        public List<string> needs;
        public List<string> altNeedsUnlocked;   // 잠금을 풀었다면 이 길로도 나온다 (대조군)
    }

    public sealed class VerdictDef
    {
        public string id;
        public string questionKo;
        public string correct;
        public List<string> supports;           // 정답을 세우는 사실들. 전부 있어야 한다
        public List<OptionDef> options;
    }

    public sealed class OptionDef
    {
        public string id;
        public string ko;
        public List<string> refutedBy;          // 이 선택지를 지우는 사실들. **전부** 있어야 지워진다
    }

    // ── data/balance.json ─────────────────────────────────────────────────────
    public sealed class BalanceFile { public CheckerDef checkers; }

    public sealed class CheckerDef
    {
        public int minNotificationsPerVerdict;  // 정답 하나에 알림이 최소 몇 장 필요한가
        public int minWakesOn;                  // 밀려남이 있을 때 필요한 최소 깨우기 수
        public int wakeGapMin;                  // 밀려남이 없는 세계보다 이만큼은 더 깨워야 한다
        public int lockedOnlyVerdictsMin;       // 잠긴 쪽만으로 세워져야 하는 정답 수 (대조군이 비어 있지 않다는 증거)
        public int noPreviewVerdictsMax;        // 미리보기를 끈 세계가 세울 수 있는 정답 수의 상한
        public int oneWakeVerdictsMax;          // 한 번만 깨워서 세울 수 있는 정답 수의 상한
        public int metronomeSuccessMaxPercent;  // 고정 박자 정책 중 전부 세우는 것의 비율 상한
        public int maxPlanWakes;                // 계획을 짤 때 가정하는 깨우기 수 상한(배터리 여유 계산용)
        public int seedsToCheck;
    }
}
