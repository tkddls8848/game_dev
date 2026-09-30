// 순수 C#만. using UnityEngine 금지.
// 스키마는 Unity의 JsonUtility가 읽을 수 있는 모양으로 유지한다:
// Dictionary·다형성 없음 · 배열 + 문자열 ID · 값 없는 int는 -1.
using System;

namespace FarmErosion.Data
{
    /// <summary>
    /// 방문해서 받는 것 하나. **양은 그 사람의 남은 땅에 비례한다**(DIRECTION §2-1).
    ///
    /// 그래서 후반으로 갈수록 받을 것이 준다 — 내가 가장 필요해지는 바로 그때.
    /// 난이도 곡선을 따로 두지 않는 이유가 이것이다.
    /// </summary>
    [Serializable] public class GiftDef
    {
        public string kind;                     // coin | soil | wall | seed | hand
        public int baseAmount;                  // 그 사람의 땅이 처음 그대로일 때의 양
        public int minAmount;                   // 아무리 줄어도 이만큼은 준다 — 고갈되어 사라지지 않는다
        public int scaled;                      // 1이면 남은 땅에 비례. 0이면 고정
        public int onlyWhenTheirPlotsAtMost;    // 그들의 땅이 이 수 이하일 때만. -1이면 언제나
        public int once;                        // 1이면 그 지역에서 한 번만
        public string textKo;
    }

    /// <summary>
    /// 의뢰. **보상 장치가 아니라 이전(移轉)이다**(DIRECTION §4-1).
    ///
    /// 그들이 필요한 것이 내가 필요한 것과 같으므로 주는 쪽이 실제로 줄어야 하고,
    /// 돌려받는 것은 **내가 준 것과 다른 종류**여야 한다. 같은 것을 주고받으면 그냥 상쇄다.
    /// </summary>
    [Serializable] public class ErrandDef
    {
        public string id, regionId, titleKo, textKo;
        public string giveKind;                 // 내가 내놓는 것. coin | soil | wall | seed
        public int giveAmount;
        public int giveDays;                    // 며칠 일해 주는가. 그 며칠만큼 내 땅이 깎인다. 없으면 0
        public string takeKind;                 // 돌려받는 것. giveKind 와 달라야 한다
        public int takeBaseAmount;
        public int takeMinAmount;
        public int reprieveDays;                // 이 의뢰가 **그 사람의 시계를** 며칠 늦추는가. 되돌리지는 않는다
        public int once;                        // 1이면 한 번만
    }

    /// <summary>
    /// 한 곳. **그들도 같은 세계에 산다** — 내가 방문하든 안 하든 자기 시계로 땅이 줄어든다.
    /// </summary>
    [Serializable] public class RegionDef
    {
        public string id, nameKo, nameEn, personKo;
        public int travelDaysOneWay;            // 거리. 지역마다 드는 날이 다르다
        public int stayDays;                    // 머무는 날
        public int startPlots;                  // 그들의 처음 땅
        public int floorPlots;                  // 여기 아래로는 내려가지 않는다 — 끝까지 남는다
        public int decayDaysPerPlot;            // 몇 날마다 한 칸을 잃는가. 그들의 시계
        public GiftDef[] gifts;
        public string[] linesKo;                // 방문 차수별 말. **갈수록 짧아진다**(DIRECTION §4-1)
    }

    /// <summary>검사기가 쓰는 환산표와 한계값. 게임 규칙이 아니라 **기계 판정용 눈금**이다.</summary>
    [Serializable] public class TravelLimits
    {
        public int coinPerSoil, coinPerWallDay, coinPerSeed, coinPerHandDay;
        public int lateVisitDay;                // 이 날 이후를 '후반'으로 본다
        public int lateVisitMaxPercentOfEarly;  // 후반 방문 수익이 초반의 이 백분율 이하여야 한다
    }

    [Serializable] public class TravelDataFile
    {
        public int helpErosionReduction;        // '며칠간의 도움'이 있는 날 하루 침식을 이만큼 줄인다
        public int wagonUpkeepCoinPerDay;       // 떠나 있는 하루마다 드는 값. 이동이 공짜가 아니게 한다
        public int maxReprieveTotalPlotPeriods; // 한 사람에게 평생 사 줄 수 있는 날 = 그의 한 칸 주기 x 이 값
        public RegionDef[] regions;
        public ErrandDef[] errands;
        public TravelLimits limits;
    }
}
