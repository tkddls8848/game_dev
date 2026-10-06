// 순수 C#만. using UnityEngine 금지.
using System;

namespace FarmErosion.Data
{
    /// <summary>
    /// 난이도 한 칸. **이기느냐를 바꾸지 않는다** — 어느 쪽을 골라도 땅은 0 이 된다.
    /// 바꾸는 것은 가는 길의 숨통뿐이다(DIRECTION.md §2-6).
    /// </summary>
    [Serializable] public class DifficultyLevel
    {
        public string id, nameKo, nameEn, captionKo;
        public int startCoinPercent;            // 시작 자원
        public int yieldPercent;                // 수확량. 침식 속도 대비 산출
        public int defenseCostPercent;          // 방벽 값
        public int defenseDurationPercent;      // 방벽 수명
        public int bankruptcyAtCoin;            // 파산 임계. 음수다
        // **하루 침식은 난이도로 밀지 않는다.** 한 번 그렇게 했다가 쉬움에서 최적 플레이와
        // 서툰 플레이의 차이가 3일이 됐다 — 여지가 많아진 것이 아니라 플레이어가 무엇을 하든
        // 결과가 같아진 것이었다. 지금 세 난이도 모두 0 이고, 길이는 **산출의 천장**으로 맞춘다.
        public int erosionPerDayBonus;
    }

    [Serializable] public class DifficultyDataFile
    {
        public int maxLengthRatioPercent;       // 가장 긴 난이도 / 가장 짧은 난이도의 상한
        public int minSkillGapDays;             // 쉬움에서도 최적과 서툰 플레이가 이만큼은 갈려야 한다
        public string defaultId;
        public DifficultyLevel[] levels;
        public string honestLineKo;             // 선택 화면에 그대로 쓴다. 이기는 길이 없다고 말한다
    }
}
