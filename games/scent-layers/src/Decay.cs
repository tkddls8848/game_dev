namespace Scent.Sim
{
    /// <summary>
    /// 감쇠. **전부 정수다** (뿌리 CLAUDE.md 설계 원칙 4).
    /// 배율은 천분율 정수이고, 버려질 나머지를 다음 걸음으로 넘겨 누적한다 —
    /// 부동소수를 쓰면 같은 씨드로 두 번 돌려 다른 값이 나올 수 있고, 그러면 전부가 무의미해진다.
    /// </summary>
    public static class Decay
    {
        /// <summary>처음 세기가 steps 걸음 뒤에 얼마나 남는가.</summary>
        public static int Remain(int initial, int retainPermille, int steps)
        {
            if (steps <= 0) return initial;
            long v = initial;
            long rem = 0;                       // 천분의 몇이 남았는지 — 버리지 않고 이월한다
            for (int i = 0; i < steps; i++)
            {
                if (v <= 0) return 0;
                long num = v * retainPermille + rem;
                v = num / 1000;
                rem = num % 1000;
            }
            return (int)v;
        }

        /// <summary>t 분에 이 층이 몇 걸음 늙었는가. 격자 아래는 버린다(계단 감쇠).</summary>
        public static int Steps(int depositedAtMin, int atMin, int stepMin)
        {
            if (atMin <= depositedAtMin) return 0;
            return (atMin - depositedAtMin) / stepMin;
        }

        /// <summary>
        /// 나이가 steps 걸음이라고 읽었을 때, 그 층이 놓인 격자 시각.
        /// 길이 stepMin 인 반열린 구간에는 격자점이 정확히 하나 들어 있다.
        /// </summary>
        public static int GridTimeFromSteps(int atMin, int steps, int stepMin)
        {
            int x = atMin - steps * stepMin;
            int q = x >= 0 ? x / stepMin : -(((-x) + stepMin - 1) / stepMin);
            return q * stepMin;
        }
    }
}
