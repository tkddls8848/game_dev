namespace FarmRewindYear.Sim
{
    public enum PolicyKind
    {
        /// 되감을 때마다 기억이 쌓인다. 의도한 플레이 — 지식은 늘고 땅은 깎인다.
        Learner,
        /// 되감아도 배우지 않는다. 되감기를 편의로만 쓰는 수 — 흔적만 남고 얻는 것이 없다.
        Stubborn,
        /// 첫 시도부터 한 해를 다 안다. 달성 가능한 상한이고, 흔적의 값을 재는 자.
        Omniscient
    }

    /// 정책 가중치. 게임 규칙이 아니라 테스트용 AI의 판단 기준이라 데이터가 아니라 코드에 둔다.
    public sealed class PolicyWeights
    {
        public int SoilCoinPerPoint;     // 토질 1점의 값. 작물의 soilDrain을 돈으로 환산할 때 쓴다
        public int ExpandMoneyReserve;   // 칸을 사고도 남겨 둘 현금
        public bool AllowExpansion;

        public static PolicyWeights For(PolicyKind kind)
        {
            return new PolicyWeights
            {
                SoilCoinPerPoint = 5,
                ExpandMoneyReserve = 150,
                AllowExpansion = true,
            };
        }

        /// 이 시도에서 플레이어가 미리 아는 날 수. 되감기의 값이 전부 여기서 나온다.
        public static int ForesightDays(PolicyKind kind, int rewindsBefore, Data.GameData d)
        {
            switch (kind)
            {
                case PolicyKind.Stubborn:   return 0;
                case PolicyKind.Omniscient: return d.DaysPerYear;
                default:
                    int days = rewindsBefore * d.Config.foresightGainPerRewind;
                    return days > d.DaysPerYear ? d.DaysPerYear : days;
            }
        }
    }
}
