# kit/templates/poc — 헤드리스 PoC 뼈대

`docs/POC_FACTORY.md`의 최소 구조를 그대로 담은 복사용 뼈대.
`docs/POC_FACTORY.md` §3은 "두 번째 헤드리스 PoC를 만들 때 템플릿을 만든다"고 적었고,
지금 여섯 개를 만들게 되어 그 시점이 됐다.

## 쓰는 법

```bash
# 1. 복사하고 슬러그로 이름을 바꾼다
cp -r kit/templates/poc games/<슬러그>
mv games/<슬러그>/PoC.csproj.template games/<슬러그>/tests/<슬러그>.Tests.csproj

# 2. csproj 안의 __SLUG__ 를 슬러그로 바꾼다 (어셈블리 이름 · RootNamespace)
# 3. src/ 에 로직, data/ 에 JSON, tests/ 에 NUnit 테스트를 쓴다
# 4. 돌린다
C:/Users/PSI/.dotnet/dotnet.exe test games/<슬러그>/tests/<슬러그>.Tests.csproj
```

## 구조

```
games/<슬러그>/
  README.md                  ★ 필수. 여섯 항목 (아래)
  CREDITS.md                 ★ 에셋을 하나라도 쓰면 필수. 출처·라이선스
  src/                       순수 C#. UnityEngine 참조 금지
  data/                      JSON. 수치는 정수
  tests/<슬러그>.Tests.csproj  NUnit. src/ 와 data/ 를 직접 가리킨다
  presentation/
    palette.json             색·크기를 값으로. 눈대중 금지
    index.html               연출 목업. 실제 data/ 를 읽어 그린다
    assets/                  이 PoC가 쓰는 에셋 (CC0/PD/OFL만)
```

## README.md 여섯 항목 (`docs/POC_FACTORY.md` §4)

한 줄 · 장르 표기 · 상태 · 판정 · 검증 · 다음.
`상태: 접음`이면 **왜 접었는지 판정 칸에 반드시 적는다.**

## 지켜야 하는 것 (뿌리 `CLAUDE.md`)

1. `src/`에 `using UnityEngine` 금지 — 헤드리스로 돌아야 한다
2. 수치는 **정수**. 배율은 백분율 정수 + 나머지 누적
3. 난수는 **씨드 고정**. `System.Random`을 직접 쓰고 씨드를 데이터에 적는다
4. 데이터는 JSON. 배열 + 문자열 ID로 평평하게
5. Phase는 **검사기 통과**로 닫는다. "구현했다"가 아니다
6. 기계가 판정하는 것(고장)과 사람만 판정하는 것(재미)을 섞지 않는다
