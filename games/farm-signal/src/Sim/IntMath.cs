namespace FarmSignal.Sim
{
    /// 부동소수를 쓰지 않기 위한 산술. 배율은 백분율 정수 + 나머지 누적으로 처리한다.
    /// 나머지를 버리면 정수판 "0.999일 자란 작물"(영원히 익지 않는 작물)이 생긴다.
    public static class IntMath
    {
        /// value * numerator / denominator. 버려지는 소수부를 remainder에 쌓아 다음 호출에서 회수한다.
        /// 관측치는 백분율 넷을 곱해 분모가 1,000,000이라 int 를 넘을 수 있다 → 중간을 long 으로 받는다.
        public static int MulDiv(int value, int numerator, int denominator, ref int remainder)
        {
            long n = (long)value * numerator + remainder;
            long q = n / denominator;
            long r = n - q * denominator;
            if (r < 0) { q -= 1; r += denominator; }   // 음수도 내림 한 방향으로 고정한다
            remainder = (int)r;
            return (int)q;
        }

        public static int MulPercent(int value, int percent, ref int remainder) =>
            MulDiv(value, percent, 100, ref remainder);

        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static int CeilDiv(int a, int b) => (a + b - 1) / b;
    }
}
