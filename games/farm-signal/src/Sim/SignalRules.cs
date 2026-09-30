using FarmSignal.Data;

namespace FarmSignal.Sim
{
    /// 신호 세 갈래(모방 · 도둑 · 평판)를 한 번에 켜고 끄는 손잡이.
    ///
    /// **이것이 핵 검사기 SignalMatters 의 전부다.** 같은 정책을 Off 세계와 On 세계에서
    /// 돌려 결과가 유의하게 달라야 한다. 같으면 신호가 장식이라는 뜻이다.
    /// 그래서 끄는 방법이 코드에 흩어져 있으면 안 되고, 여기 한 곳이어야 한다.
    public sealed class SignalRules
    {
        public bool On;

        // 모방 → 시세
        public int CopycatDropPerPointPer1000;
        public int CopycatRecoverPerSeason;
        public int ScarcityThresholdIndex;
        public int ScarcityRisePerSeason;
        public int PriceFloorPercent;
        public int PriceCeilPercent;
        public int PriceWarnPercent;
        public int PriceCollapsePercent;

        // 도둑
        public int TheftRiskDivisor;
        public int TheftRiskCapPer1000;
        public int TheftWatchPer100Renown;
        public int TheftSeverityStepPer1000;
        public int TheftMaxPlotsHit;
        public int TheftMinRiskToRollPer1000;

        // 평판 → 방문자 → 판매 웃돈
        public int RenownGainDivisor;
        public int RenownDecayPerSeason;
        public int RenownCap;
        public int RenownPerVisitor;
        public int PremiumPerVisitor;
        public int PremiumCapPercent;

        public int ObservationDenominator;

        public static SignalRules Full(GameData d)
        {
            var c = d.Signal.copycat;
            var t = d.Signal.theft;
            var r = d.Signal.renown;
            return new SignalRules
            {
                On = true,
                CopycatDropPerPointPer1000 = c.dropPerPointPer1000,
                CopycatRecoverPerSeason = c.recoverPerSeason,
                ScarcityThresholdIndex = c.scarcityThresholdIndex,
                ScarcityRisePerSeason = c.scarcityRisePerSeason,
                PriceFloorPercent = c.floorPercent,
                PriceCeilPercent = c.ceilPercent,
                PriceWarnPercent = c.warnPercent,
                PriceCollapsePercent = c.collapsePercent,
                TheftRiskDivisor = t.riskDivisor,
                TheftRiskCapPer1000 = t.riskCapPer1000,
                TheftWatchPer100Renown = t.watchPer100Renown,
                TheftSeverityStepPer1000 = t.severityStepPer1000,
                TheftMaxPlotsHit = t.maxPlotsHit,
                TheftMinRiskToRollPer1000 = t.minRiskToRollPer1000,
                RenownGainDivisor = r.gainDivisor,
                RenownDecayPerSeason = r.decayPerSeason,
                RenownCap = r.cap,
                RenownPerVisitor = r.perVisitor,
                PremiumPerVisitor = r.premiumPerVisitor,
                PremiumCapPercent = r.premiumCapPercent,
                ObservationDenominator = d.Signal.observation.denominator,
            };
        }

        /// 남이 내 밭을 보지 않는 세계. 관측치는 그대로 세지만(그래야 두 세계를 견줄 수 있다)
        /// 아무 결과도 만들지 않는다: 시세는 100%에 고정, 도둑은 오지 않고, 평판은 쌓이지 않는다.
        public static SignalRules Off(GameData d)
        {
            var r = Full(d);
            r.On = false;
            r.CopycatDropPerPointPer1000 = 0;
            r.CopycatRecoverPerSeason = 0;
            r.ScarcityRisePerSeason = 0;
            r.TheftRiskDivisor = 0;          // 0 = 위험도를 셈하지 않는다
            r.TheftWatchPer100Renown = 0;
            r.RenownGainDivisor = 0;         // 0 = 평판이 쌓이지 않는다
            return r;
        }
    }
}
