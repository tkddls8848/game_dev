namespace FarmSignal.Sim
{
    /// 세 단계로 벌려 두었다: 판을 아예 안 본다 / 판은 보지만 자기가 판을 움직이는 줄 모른다 /
    /// 자기가 판을 움직이는 것을 안다. **이 세 단계의 차이가 이 PoC가 만들려는 축이다.**
    public enum PolicyKind
    {
        /// 시세판을 보지 않는다. 상점 기준가만 보고 가장 마진 큰 작물을 심는다.
        /// **신호를 끈 세계에서 최적이고, 켠 세계에서 가장 크게 벌을 받는다.**
        Blind,
        /// 시세판을 읽는다. 값이 내려간 작물은 피한다. 다만 **그 판을 내려가게 한 것이
        /// 자기라는 것은 모른다** — 그래서 늘 한 계절 늦게 움직인다.
        Reactive,
        /// 통로 하나만 쓴다 — **윤작**. 한 작물이 밭의 일정 비율을 넘지 못하게 묶어 관측치를 흩는다.
        Rotate,
        /// 통로 하나만 쓴다 — **가림**. 길가 칸을 키 큰 작물로 버리고 뒤에 돈작물을 숨긴다.
        Screen,
        /// 통로 하나만 쓴다 — **평판**. 길가를 라벤더로 채워 노출을 이득으로 바꾼다.
        Renown,
        /// 통로를 전부 쓴다. 관측치가 다음 계절 시세를 내리는 것을 값으로 셈하고,
        /// 도둑을 피하고, 평판을 쓰고, 길가에 키 큰 것을 세운다.
        SignalAware,
        /// 계절마다 기준마진 1위 하나만 밭 전체에 심는다. NoSingleCropWins 가 이것을 이겨야 한다.
        MonoSeasonBest,
        /// 지정한 한 작물만 심는다(심을 수 없는 계절에는 비운다). 작물별 단작 검사용.
        MonoCrop
    }

    /// 정책 가중치. 게임 규칙이 아니라 테스트용 AI의 판단 기준이라 데이터가 아니라 코드에 둔다.
    /// 전부 정수 — 부동소수를 쓰면 정책이 플랫폼마다 갈릴 수 있다.
    public sealed class PolicyWeights
    {
        public PolicyKind Kind;

        /// 시세판(살아 있는 시세지수·웃돈)을 읽는가. false면 상점 기준가만 보고 정한다.
        public bool ReadsBoard;
        /// 남의 반응을 판단에 넣는가. false면 관측치를 세지만 신경 쓰지 않는다.
        public bool SeeSignal;
        /// 한 작물이 밭에서 차지할 수 있는 최대 비율(%). 100이면 제한 없음.
        public int MaxCropSharePercent;
        /// 길가 칸을 가림막으로 쓰는가.
        public bool UseScreens;
        public int ScreenFrontExposureMin;
        /// 이 평판까지는 평판 작물에 가산점을 준다.
        public int RenownTarget;
        /// 계절의 기준마진 1위만 심는다.
        public bool SeasonBestOnly;
        /// null이 아니면 이것만 심는다.
        public string ForcedCropId;

        // 점수 가중치. 점수는 1/100코인 / 이상일 단위다.
        // 모방·도둑 벌은 **노출 x 그 작물의 값**에 비례한다 — 노출만 보면 가장 잘 보이는
        // 라벤더가 가장 큰 벌을 받는데, 라벤더의 노출은 평판이므로 이득이다.
        public int CopycatWeight;    // 보인 정도 x 작물 값 / 1000 당 장래 손실
        public int TheftWeight;      // 노려진 정도 x 작물 값 / 1000 당 위험
        public int RenownWeight;     // 평판 1점의 값
        public int ScreenWeight;     // 가림 키 1점의 값

        public static PolicyWeights For(PolicyKind kind, string forcedCropId = null)
        {
            switch (kind)
            {
                case PolicyKind.SignalAware:
                    return new PolicyWeights
                    {
                        Kind = kind, ReadsBoard = true, SeeSignal = true, MaxCropSharePercent = 42,
                        UseScreens = true, ScreenFrontExposureMin = 70, RenownTarget = 240,
                        SeasonBestOnly = false, ForcedCropId = null,
                        CopycatWeight = 26, TheftWeight = 40, RenownWeight = 34, ScreenWeight = 26,
                    };
                case PolicyKind.Reactive:
                    return new PolicyWeights
                    {
                        Kind = kind, ReadsBoard = true, SeeSignal = false, MaxCropSharePercent = 100,
                        UseScreens = false, ScreenFrontExposureMin = 999, RenownTarget = 0,
                        SeasonBestOnly = false, ForcedCropId = null,
                        CopycatWeight = 0, TheftWeight = 0, RenownWeight = 0, ScreenWeight = 0,
                    };
                case PolicyKind.Rotate:
                    return new PolicyWeights
                    {
                        Kind = kind, ReadsBoard = true, SeeSignal = false, MaxCropSharePercent = 34,
                        UseScreens = false, ScreenFrontExposureMin = 999, RenownTarget = 0,
                        SeasonBestOnly = false, ForcedCropId = null,
                        CopycatWeight = 0, TheftWeight = 0, RenownWeight = 0, ScreenWeight = 0,
                    };
                case PolicyKind.Screen:
                    return new PolicyWeights
                    {
                        Kind = kind, ReadsBoard = true, SeeSignal = true, MaxCropSharePercent = 100,
                        UseScreens = true, ScreenFrontExposureMin = 70, RenownTarget = 0,
                        SeasonBestOnly = false, ForcedCropId = null,
                        CopycatWeight = 26, TheftWeight = 40, RenownWeight = 0, ScreenWeight = 40,
                    };
                case PolicyKind.Renown:
                    return new PolicyWeights
                    {
                        Kind = kind, ReadsBoard = true, SeeSignal = true, MaxCropSharePercent = 100,
                        UseScreens = false, ScreenFrontExposureMin = 999, RenownTarget = 300,
                        SeasonBestOnly = false, ForcedCropId = null,
                        CopycatWeight = 26, TheftWeight = 40, RenownWeight = 90, ScreenWeight = 0,
                    };
                case PolicyKind.MonoSeasonBest:
                    return new PolicyWeights
                    {
                        Kind = kind, ReadsBoard = false, SeeSignal = false, MaxCropSharePercent = 100,
                        UseScreens = false, ScreenFrontExposureMin = 999, RenownTarget = 0,
                        SeasonBestOnly = true, ForcedCropId = null,
                        CopycatWeight = 0, TheftWeight = 0, RenownWeight = 0, ScreenWeight = 0,
                    };
                case PolicyKind.MonoCrop:
                    return new PolicyWeights
                    {
                        Kind = kind, ReadsBoard = false, SeeSignal = false, MaxCropSharePercent = 100,
                        UseScreens = false, ScreenFrontExposureMin = 999, RenownTarget = 0,
                        SeasonBestOnly = false, ForcedCropId = forcedCropId,
                        CopycatWeight = 0, TheftWeight = 0, RenownWeight = 0, ScreenWeight = 0,
                    };
                default:
                    return new PolicyWeights
                    {
                        Kind = PolicyKind.Blind, ReadsBoard = false, SeeSignal = false,
                        MaxCropSharePercent = 100,
                        UseScreens = false, ScreenFrontExposureMin = 999, RenownTarget = 0,
                        SeasonBestOnly = false, ForcedCropId = null,
                        CopycatWeight = 0, TheftWeight = 0, RenownWeight = 0, ScreenWeight = 0,
                    };
            }
        }

        public string Label
        {
            get
            {
                if (Kind == PolicyKind.MonoCrop) return "MonoCrop:" + ForcedCropId;
                return Kind.ToString();
            }
        }
    }
}
