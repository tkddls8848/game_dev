using FarmSignal.Data;

namespace FarmSignal.Sim
{
    /// 이 PoC의 규칙 그 자체 — "내가 심은 것을 남이 보고 무엇을 하는가"의 정수 산술.
    ///
    /// 상태가 없는 순수 함수만 둔다. 두 곳에서 불린다:
    ///   1. Simulation   — 하루 한 걸음, 밭이 날마다 바뀌는 전체 시뮬레이션
    ///   2. SignalBoard  — 계절 단위, showcase.json 의 고정 계획으로 시세판을 뽑는 쪽
    /// 그리고 **presentation/index.html 이 이 파일을 JS로 그대로 이식한다.**
    /// 목업의 시세판이 테스트가 돌린 시세판과 같아야 하므로, 여기에 부동소수나
    /// 순회 순서에 기대는 코드가 들어오면 화면과 테스트가 갈라진다.
    public static class SignalMath
    {
        /// 이 칸이 길에서 실제로 보이는 정도.
        /// 길 쪽으로 screenDepth 칸 앞에 키 큰 작물이 서 있으면 그만큼 가려진다.
        /// frontCropScreenHeights[k] = k+1칸 앞에 심긴 작물의 screenHeight (없으면 0).
        public static int EffectiveExposure(GameData d, int baseExposure, int[] frontCropScreenHeights)
        {
            int blocked = 0;
            if (frontCropScreenHeights != null)
                for (int k = 0; k < frontCropScreenHeights.Length; k++)
                    blocked += frontCropScreenHeights[k] * d.Plots.screenEffectPercent / 100;
            return IntMath.Clamp(baseExposure - blocked, d.Plots.exposureFloor, 100);
        }

        /// 하루·한 칸·한 작물의 관측치.
        /// visibility x 유효노출 x 날씨노출% x 길통행% / denominator. 나머지는 작물별로 누적한다.
        public static int DailyObservedPoints(SignalRules r, int visibility, int effectiveExposure,
                                             int weatherExposurePercent, int roadTrafficPercent,
                                             ref int remainder)
        {
            int num = weatherExposurePercent * roadTrafficPercent;   // 최대 120 x 130 = 15,600
            int val = visibility * effectiveExposure;                // 최대 100 x 100 = 10,000
            return IntMath.MulDiv(val, num, r.ObservationDenominator, ref remainder);
        }

        /// 계절 마감. 모방 압력이 시세지수를 내리고, **아무도 심지 않은 작물만** 천장까지 올라간다.
        ///
        /// 통로가 둘로 갈려 있는 것이 이 훅의 절반이다:
        ///   회복(recover)은 100%까지만 되돌린다 — 넘게 두면 열두 작물이 전부 천장에 붙어
        ///               "남이 본다"가 벌이 아니라 상금이 된다(처음에 그렇게 짰다가 고쳤다).
        ///   희소(scarcity)는 관측치가 임계 아래일 때만 붙는다 — 한 해 묶어 둔 값이고, 윤작의 보상이다.
        /// copyAppeal 은 "보고 따라 심을 만한가"다. 순무를 심는 것을 보여도 아무도 순무로 밭을
        /// 덮지 않으므로 시세가 버틴다 — 대신 순무는 수입이 얇다. 그 규칙이 이 한 줄이다.
        public static int NextPriceIndex(SignalRules r, int currentIndexPercent, int observedIndex, int copyAppeal)
        {
            int drop = observedIndex * copyAppeal / 100 * r.CopycatDropPerPointPer1000 / 1000;
            int next = currentIndexPercent - drop;
            if (observedIndex <= r.ScarcityThresholdIndex)
                next += r.ScarcityRisePerSeason;
            else if (next < 100)
                next = System.Math.Min(100, next + r.CopycatRecoverPerSeason);
            return IntMath.Clamp(next, r.PriceFloorPercent, r.PriceCeilPercent);
        }

        /// 계절 마감. 값나가는 것을 남이 본 만큼 도둑이 온다. 평판은 그것을 깎는다(지나는 눈이 많다).
        /// 반환값은 1000분율. 0이면 굴리지 않는다.
        public static int TheftRiskPer1000(GameData d, SignalRules r, int[] observedIndexByCrop, int renown)
        {
            if (r.TheftRiskDivisor <= 0) return 0;
            int raw = 0;
            for (int c = 0; c < d.CropCount; c++)
                raw += observedIndexByCrop[c] * d.Crops.crops[c].theftAppeal;
            int risk = raw / r.TheftRiskDivisor;
            risk -= renown * r.TheftWatchPer100Renown / 100;
            risk = IntMath.Clamp(risk, 0, r.TheftRiskCapPer1000);
            return risk < r.TheftMinRiskToRollPer1000 ? 0 : risk;
        }

        /// 위험도가 클수록 여러 칸을 걷어 간다.
        public static int TheftPlotsHit(SignalRules r, int riskPer1000)
        {
            if (r.TheftSeverityStepPer1000 <= 0) return 1;
            int hits = 1 + riskPer1000 / r.TheftSeverityStepPer1000;
            return IntMath.Clamp(hits, 1, r.TheftMaxPlotsHit);
        }

        /// 계절 마감. 보이는 것이 나쁜 일만 하지는 않는다 — 라벤더는 보인 만큼 평판이 된다.
        public static int NextRenown(GameData d, SignalRules r, int renown, int[] observedIndexByCrop)
        {
            int gain = 0;
            if (r.RenownGainDivisor > 0)
            {
                int raw = 0;
                for (int c = 0; c < d.CropCount; c++)
                    raw += observedIndexByCrop[c] * d.Crops.crops[c].renownGain;
                gain = raw / r.RenownGainDivisor;
            }
            return IntMath.Clamp(renown + gain - r.RenownDecayPerSeason, 0, r.RenownCap);
        }

        public static int VisitorCount(SignalRules r, int renown)
        {
            if (r.RenownPerVisitor <= 0) return 0;
            return renown / r.RenownPerVisitor;
        }

        /// 방문자가 밭에서 바로 사 간다 → 모든 작물 판매가에 얹는 웃돈(백분율). 상한이 있다.
        public static int VisitorPremiumPercent(SignalRules r, int visitors)
        {
            return IntMath.Clamp(visitors * r.PremiumPerVisitor, 0, r.PremiumCapPercent);
        }

        /// 실제 판매단가 = 기준 잡음가 x 시세지수% x (100 + 웃돈%)/100. 나머지는 작물별로 누적한다.
        public static int SellUnit(int basePrice, int priceIndexPercent, int premiumPercent, ref int remainder)
        {
            int factor = priceIndexPercent * (100 + premiumPercent);   // 1/10000 단위
            return IntMath.MulDiv(basePrice, factor, 10000, ref remainder);
        }
    }
}
