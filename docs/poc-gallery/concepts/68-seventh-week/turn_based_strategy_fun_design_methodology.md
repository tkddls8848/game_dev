# 문명 V · Heroes of Might and Magic류 전략게임의 재미 설계 방법론

## 목적

이 문서는 **Sid Meier's Civilization V**와 **Heroes of Might and Magic
III** 같은 턴제 전략게임이 왜 강한 몰입과 "한 턴만 더(One More Turn)"
현상을 만드는지 이론적으로 설명하고, 이를 실제 개발 방법론으로 정리한다.

> 핵심 가설: 재미는 개별 보상 하나보다 **불확실성 → 선택 → 행동 → 피드백
> → 성장 → 새로운 불확실성**이 계속 이어지는 시스템에서 발생한다.

## 1. 공통적인 재미 구조

Civilization, Heroes of Might and Magic, Football Manager처럼 장시간
몰입을 유발하는 시스템 게임에는 공통점이 있다.

-   플레이어가 스스로 목표를 만든다.
-   여러 목표가 서로 다른 시간에 완료된다.
-   작은 목표의 완료가 새로운 목표를 만든다.
-   현재의 선택이 장기 결과에 영향을 준다.
-   플레이할수록 시스템을 더 잘 이해한다는 유능감을 준다.
-   완전한 정보보다 **추론 가능한 불확실성**을 제공한다.
-   단순한 시스템들의 상호작용에서 예상하지 못한 사건과 이야기가
    발생한다.

## 2. MDA Framework

Hunicke, LeBlanc, Zubek의 MDA는 게임을 **Mechanics → Dynamics →
Aesthetics**로 나눈다.

-   **Mechanics:** 턴, 타일, 자원, 생산, 연구, 전투, 경험치, 기술트리,
    외교.
-   **Dynamics:** 확장 경쟁, 군비 경쟁, 전문화, 위험 감수, 기회비용,
    세력 균형.
-   **Aesthetics:** 발견, 긴장, 성취, 성장, 전략적 만족, 자기만의 역사.

설계자는 기능에서 출발하기보다 원하는 경험에서 역산할 수 있다.

``` text
원하는 경험: "다음 턴이 궁금하다"
        ↓
Dynamics: 여러 사건이 서로 다른 시점에 완료
        ↓
Mechanics:
연구 5턴 / 성장 3턴 / 생산 7턴 / 이동 2턴 / 정책 9턴
```

## 3. Self-Determination Theory

게임에서는 특히 **Competence(유능감)**와 **Autonomy(자율성)**가
중요하다.

### Competence

``` text
문제 → 전략 → 결과 → 학습 → 더 좋은 전략 → 더 어려운 문제
```

HoMM에서는 `전투 → 경험치 → 레벨업 → 스킬 선택 → 더 강한 적`으로,
Civilization에서는
`도시 운영 → 기술 발전 → 새로운 선택 → 더 큰 제국 운영`으로 나타난다.

### Autonomy

과학/군사, 확장/내정, 현재/미래처럼 플레이어가 자신의 전략을 선택하게
한다. 중요한 것은 선택지의 숫자가 아니라 **선택 → 결과 → 피드백 →
학습**이 이어지는가이다.

## 4. Flow와 GameFlow

Sweetser & Wyeth의 GameFlow는 게임의 즐거움을 Concentration, Challenge,
Player Skills, Control, Clear Goals, Feedback, Immersion, Social
Interaction 등의 요소로 분석한다.

프로토타입에서는 다음을 확인한다.

-   목표가 명확한가?
-   행동 결과를 해석할 수 있는가?
-   플레이어에게 실제 통제권이 있는가?
-   실력이 향상될수록 새로운 문제가 나타나는가?
-   난이도가 너무 쉽거나 불공정하지 않은가?

## 5. 불확실성의 해소

전략게임에서는 보상 자체뿐 아니라 **불확실성을 자신의 행동으로 줄이는
과정**이 중요하다.

``` text
불확실성 → 예측 → 선택 → 행동 → 결과 확인 → 정보 획득 → 새로운 불확실성
```

HoMM의
`미탐험 지역 → 이동 → 몬스터 발견 → 위험 판단 → 전투 → 보상 → 새로운 지역`과
Civilization의 `정찰 → 자원/문명 발견 → 정착/외교 판단 → 새로운 문제`가
대표적이다.

따라서 **탐험은 단순히 지도를 밝히는 기능이 아니라 새로운 의사결정
문제를 생성하는 시스템**이다.

## 6. Core Loop와 다중 시간축

공통 Core Loop:

``` text
Explore → Discover → Decide → Act → Feedback → Reward → Grow → Explore
```

추상화하면:

``` text
Uncertainty → Decision → Action → Feedback → Progression → New Uncertainty
```

동시에 여러 시간 규모의 루프를 둔다.

-   **Micro (1\~3분):** 이동 → 발견 → 판단 → 행동/전투 → 보상
-   **Meso (10\~30분):** 도시/영웅 성장 → 능력 해금 → 전략 변화
-   **Macro (수십 분\~수 시간):** 세력 확장 → 경쟁 → 외교/전쟁 → 승리
    조건

## 7. One More Turn: 중첩된 Progress Clocks

Civilization류의 중요한 장치는 여러 미래 사건의 완료 시점을 엇갈리게
만드는 것이다.

``` text
현재
1턴 후  정찰병 도착
2턴 후  도시 성장
4턴 후  기술 연구
6턴 후  유닛 생산
9턴 후  정책 획득
13턴 후 불가사의 완성
```

그래서 `Goal → Reward → Stop`이 아니라 다음 구조가 된다.

``` text
Goal A 완료
→ B가 거의 완료
→ B 확인
→ 새로운 Goal C 발생
→ 그 사이 D가 거의 완료
→ ...
```

**보상의 크기뿐 아니라 보상의 시간적 배치가 중요하다.** 모든 시스템이
동시에 끝나면 종료점이 생기므로, 3/5/7/9/13턴처럼 완료 시점을
비동기화하는 것이 효과적이다.

## 8. 다층 보상과 Progression

보상은 여러 시간축에 배치한다.

-   **즉시:** 전투 승리, 자원/아이템, 타일 발견.
-   **단기:** 레벨업, 생산, 도시 성장, 연구.
-   **중기:** 도시 전문화, 영웅 빌드, 시대 진입, 세력 제압.
-   **장기:** 세계 정복, 과학 승리, 캠페인 완수.

Progression은 단순한 `공격력 10 → 11 → 12`보다 **새로운 능력 → 새로운
선택 → 새로운 전략**을 열어야 한다. 즉 성장의 핵심은 **Decision Space의
확장**이다.

## 9. 의미 있는 선택과 Opportunity Cost

좋은 전략적 선택은 서로 다른 방향을 만든다.

``` text
빠른 확장 / 강한 방어 / 연구 집중 / 군사 압박
```

그리고 동시에 얻을 수 없게 한다.

``` text
군대 생산 ↔ 경제 건물 생산
확장 ↔ 내정
현재 이득 ↔ 미래 이득
안전 ↔ 위험
```

턴, 생산력, 이동력, 골드, 연구력, 영웅 행동, 정보가 제한되어 있기 때문에
전략게임의 핵심 질문은 **"지금 무엇을 하지 않을 것인가?"**가 된다.

## 10. 정보 설계

정보는 **Known / Unknown / Predictable**로 나눌 수 있다. 전략적으로 특히
흥미로운 것은 Predictable 영역이다.

적 병력의 정확한 수는 모르지만 정찰, 도시 규모, 최근 전투, 경제력으로
대략 추론할 수 있다면 **추론 능력 자체가 플레이어의 실력**이 된다.

좋은 불확실성은 순수 랜덤이 아니라 플레이어가 정보를 수집해 더 나은
판단을 할 수 있는 형태다.

## 11. Emergence와 플레이어가 만드는 이야기

경제 + 지형 + 외교 + 군사 + AI + 자원 같은 단순 시스템을 연결하면
개발자가 직접 스크립트하지 않은 사건이 발생한다.

``` text
자원 부족 → 확장 → 국경 충돌 → 외교 악화 → 군비 경쟁 → 전쟁
```

이런 과정은 플레이어가 "내 게임에서 일어난 역사"를 기억하게 한다. 즉
시스템이 **이야기 생성기** 역할을 한다.

## 12. 실제 설계 절차

### Step 1 --- Desired Experience

탐험의 기대, 성장의 만족, 전략적 우월감, 위험한 결정의 긴장, 장기 계획의
성공 같은 목표 경험을 먼저 정의한다.

### Step 2 --- Core Loop

`Explore → Discover → Decide → Act → Gain → Grow → Explore`처럼 한
문장으로 설명한다.

### Step 3 --- Resources

Gold, Food, Production, Science, Army, Time, Information 각각에 대해
획득처, 사용처, 부족 시 문제, 다른 자원과의 변환 관계를 정의한다.

### Step 4 --- Decision Space

확장/내정, 경제/군사, 현재/미래, 안전/위험처럼 반복적으로 고민할 핵심
대립축을 만든다.

### Step 5 --- Progression

초기에는 시스템 학습, 중기에는 전략 분화, 후기에는 대규모 시스템 충돌을
만든다.

### Step 6 --- Progress Clocks

1\~3턴, 3\~8턴, 8\~20턴, 20\~50턴, 전체 게임 등 여러 시간축에 목표를
배치한다.

### Step 7 --- Uncertainty

지도, 적 의도, 전투 결과, 보상, 미래 자원, 외교 반응 등 플레이어가
추론할 대상을 만든다.

### Step 8 --- Feedback

`행동 → 즉각적 피드백 → 시스템 변화 → 장기 결과`가 읽히도록 한다.

## 13. 프로토타입 개발 방법

처음부터 거대한 Civilization을 만들지 않는다.

### Prototype 1 --- Core Loop

20×20 Grid, 자원 3종, 유닛 3종, 도시 1개, 적 1세력, 기술 10개 정도로
시작한다. 그래픽은 최소화하고 **"턴을 한 번 더 넘기고 싶은가?"**만
검증한다.

### Prototype 2 --- Progress Clocks

연구, 생산, 성장, 탐험을 추가하고 완료 시점을 엇갈리게 한다. 플레이어가
스스로 **"이것까지만 보고..."**라고 생각하는지 관찰한다.

### Prototype 3 --- Trade-offs

경제/군사, 확장/발전, 현재/미래, 안전/위험의 opportunity cost를 넣는다.

### Prototype 4 --- Emergence

경제·지형·세력 관계·군사를 연결하고 스크립트 없이 흥미로운 사건이
생기는지 확인한다.

## 14. 플레이테스트 지표

단순히 "재미있었나요?"만 묻지 않는다.

-   예상보다 몇 턴 더 플레이했는가?
-   미래 계획을 스스로 세우는가?
-   가까운 미래 사건을 기다리는가?
-   실패 원인을 설명할 수 있는가?
-   다음 플레이에서 다른 전략을 시험하려 하는가?
-   선택 앞에서 실제로 고민하는가?
-   자신의 플레이를 이야기 형태로 설명하는가?

특히 **"다음에는 이렇게 해봐야겠다"**는 반응은 `학습 → 가설 → 재도전`
루프가 작동한다는 강한 신호다.

## 15. 흔한 실패

-   **콘텐츠 양으로 해결:** 유닛 500종보다 의미 있는 상호작용이
    중요하다.
-   **의미 없는 선택:** 결과가 비슷한 선택지는 복잡성만 높인다.
-   **순수 랜덤 과다:** 플레이어가 결과에 영향을 줄 수 없다면 전략성이
    약해진다.
-   **숫자만 증가하는 성장:** 가능하면 새로운 행동과 전략을 해금한다.
-   **보상 완료 시점의 동기화:** 자연스러운 종료점을 지나치게 많이
    만든다.
-   **후반 반복 작업:** 규모는 커졌는데 의사결정의 질은 낮아지는 문제를
    피해야 한다.
-   **피드백 부족:** 왜 성공/실패했는지 모르면 학습과 유능감이 약해진다.

## 16. 통합 설계 모델

이 장르의 재미를 하나의 식처럼 표현하면 다음과 같이 볼 수 있다.

``` text
재미 있는 전략 플레이
=
의미 있는 선택
× 불확실성
× 해석 가능한 피드백
× 성장
× 장기 계획
× 중첩된 목표
× Emergence
```

그리고 세션 지속성은 다음 순환에서 나온다.

``` text
현재의 작은 문제 해결
        ↓
즉시 보상
        ↓
장기 성장에 기여
        ↓
새로운 능력/정보
        ↓
더 큰 문제 등장
        ↓
새로운 계획
        ↓
"한 턴만 더"
```

핵심 설계 원칙은 **보상이 플레이를 끝내게 하지 않고 다음 의사결정을
생성하도록 만드는 것**이다.

## 17. 추천 자료와 논문

### 핵심 논문

1.  Hunicke, R., LeBlanc, M., & Zubek, R. (2004). **MDA: A Formal
    Approach to Game Design and Game Research.**
2.  Ryan, R. M., Rigby, C. S., & Przybylski, A. (2006). **The
    Motivational Pull of Video Games: A Self-Determination Theory
    Approach.** *Motivation and Emotion*.
3.  Sweetser, P., & Wyeth, P. (2005). **GameFlow: A Model for Evaluating
    Player Enjoyment in Games.** *Computers in Entertainment*.
4.  Caldwell, N. (2004). **Theoretical Frameworks for Analysing
    Turn-Based Computer Strategy Games.**
5.  Voorhees, G. (2009). **I Play Therefore I Am: Sid Meier's
    Civilization, Turn-Based Strategy Games and the Cogito.**
6.  **Mastering uncertainty: A predictive processing account of enjoying
    uncertain success in video game play** (2022).

### 책

-   Katie Salen & Eric Zimmerman, **Rules of Play: Game Design
    Fundamentals**
-   Jesse Schell, **The Art of Game Design: A Book of Lenses**
-   Tynan Sylvester, **Designing Games: A Guide to Engineering
    Experiences**

### 연구할 때 사용할 키워드

`game design MDA`, `self determination theory video games`, `GameFlow`,
`uncertainty video games`, `turn-based strategy game design`,
`emergent gameplay`, `meaningful choice game design`,
`progression systems`, `player motivation`, `one more turn game design`.

## 18. 개발용 체크리스트

새 시스템을 추가할 때 다음을 확인한다.

-   이 시스템은 어떤 **플레이어 경험**을 만들기 위한 것인가?
-   새로운 **의미 있는 선택**을 만드는가?
-   다른 시스템과 **trade-off**가 있는가?
-   플레이어가 결과를 **예측하고 학습**할 수 있는가?
-   결과에 대한 **피드백**이 명확한가?
-   progression이 단순 수치가 아니라 **새로운 전략**을 여는가?
-   다른 progress clock과 완료 시점이 적절히 엇갈리는가?
-   보상을 받았을 때 **새로운 목표나 문제**가 생기는가?
-   시스템 간 상호작용에서 **emergent event**가 발생할 수 있는가?
-   플레이어가 끝난 뒤 자신의 플레이를 **이야기로 기억할 가능성**이
    있는가?

------------------------------------------------------------------------

## 결론

Civilization V와 HoMM III의 강한 지속성은 단순한 보상이나 "중독성"
하나로 설명하기 어렵다. 핵심은 **자율적인 선택, 유능감의 축적, 추론
가능한 불확실성, 다층 progression, opportunity cost, 비동기적인 progress
clocks, 그리고 시스템 간 emergence**가 서로 맞물리는 데 있다.

실제 개발에서는 거대한 콘텐츠부터 만들기보다 가장 작은 프로토타입에서
**"플레이어가 자발적으로 다음 턴을 누르는가?"**를 먼저 검증하고, 이후
중첩 목표·trade-off·정보·성장·emergence를 단계적으로 추가하는 접근이
효율적이다.
