namespace FarmRewindYear.Sim
{
    /// 부동소수를 쓰지 않기 위한 산술. 배율은 백분율 정수 + 나머지 누적으로 처리한다.
    /// 나머지를 버리면 정수판 "0.999일 자란 작물"(영원히 익지 않는 작물)이 생긴다.
    public static class IntMath
    {
        /// value * percent / 100. 버려지는 소수부를 remainder에 쌓아 다음 호출에서 회수한다.
        public static int MulPercent(int value, int percent, ref int remainder)
        {
            int n = value * percent + remainder;
            int q = n / 100;
            int r = n - q * 100;
            if (r < 0) { q -= 1; r += 100; }   // 음수도 내림 한 방향으로 고정한다
            remainder = r;
            return q;
        }

        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static int CeilDiv(int a, int b) => (a + b - 1) / b;
    }
}
