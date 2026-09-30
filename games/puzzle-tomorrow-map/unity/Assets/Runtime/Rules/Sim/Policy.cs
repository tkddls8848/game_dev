namespace FarmErosion.Sim
{
    public enum PolicyKind
    {
        /// 사람이 낼 만한 수. 좋은 칸은 제방으로 지키고, 나쁜 칸은 포기하고 클로버로 시간을 번다.
        Balanced,
        /// 착취형. 마진이 큰 작물만 심고 제방도 클로버도 쓰지 않는다. EconomyChecker의 반대편.
        Greedy,
        /// 방어만. 클로버만 심고 계속 제방을 쌓는다. "지키기만 하면 굶는다"를 확인한다.
        Turtle
    }

    /// 정책 가중치. 게임 규칙이 아니라 테스트용 AI의 판단 기준이라 데이터가 아니라 코드에 둔다.
    /// 전부 정수 — 부동소수를 쓰면 정책이 플랫폼마다 갈릴 수 있다.
    public sealed class PolicyWeights
    {
        public int SoilCoinPerPoint;      // 토질 1점의 값. 작물의 soilDrain을 돈으로 환산할 때 쓴다
        public int GuardCoinPerPoint;     // erosionGuard 1점의 값(하루치)
        public int CloverSoilTrigger;     // 토질이 이 아래면 돈보다 회복을 고른다
        public int DefendMoneyReserve;    // 제방을 쌓고도 남겨 둘 현금
        public int DefendSoilMin;         // 이 토질 이상인 칸만 지킨다
        public int DefendErosionMin;      // 이 침식 이상일 때부터 지킨다
        public bool AllowDefense;
        public bool AllowClover;
        public bool ForceClover;

        public static PolicyWeights For(PolicyKind kind)
        {
            switch (kind)
            {
                case PolicyKind.Greedy:
                    return new PolicyWeights
                    {
                        SoilCoinPerPoint = 0, GuardCoinPerPoint = 0, CloverSoilTrigger = -1,
                        DefendMoneyReserve = 0, DefendSoilMin = 999, DefendErosionMin = 999,
                        AllowDefense = false, AllowClover = false, ForceClover = false,
                    };
                case PolicyKind.Turtle:
                    return new PolicyWeights
                    {
                        SoilCoinPerPoint = 6, GuardCoinPerPoint = 4, CloverSoilTrigger = 101,
                        DefendMoneyReserve = 0, DefendSoilMin = 0, DefendErosionMin = 400,
                        AllowDefense = true, AllowClover = true, ForceClover = true,
                    };
                default:
                    return new PolicyWeights
                    {
                        SoilCoinPerPoint = 6, GuardCoinPerPoint = 4, CloverSoilTrigger = 34,
                        DefendMoneyReserve = 500, DefendSoilMin = 38, DefendErosionMin = 1000,
                        AllowDefense = true, AllowClover = true, ForceClover = false,
                    };
            }
        }
    }
}
