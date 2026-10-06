# 수정 컨셉 10종 PoC

## 보기

- [PNG 비교 갤러리](revised.html): 파일로 바로 열 수 있다.
- 플레이: 저장소 루트에서 `python -m http.server 8000 --bind 127.0.0.1` 실행 후
  `http://127.0.0.1:8000/docs/poc-gallery/revised.html`.
- [브라우저 검사 결과](revised-verification.json).
- 기존 `index.html`과 `shots.html`에도 수정안 갤러리 링크를 추가했다.

| 제목 | games 폴더 | 실제 슬라이스 |
|---|---|---|
| 잔향 감식실 | mystery-scent-layers | 세 냄새 시간층, 채취, 외투의 이동 추론 |
| 휴전의 문장 | narrative-armistice | 세 회담 의제와 약속 강도의 누적 |
| 잠금 해제 불가 | mystery-locked-phone | 전화 대응, 후속 알림, 위치 단서, 마지막 연락 |
| 기억의 봉합사 | puzzle-memory-suture | 인물·행동·감정 조각 교체, 모순과 후유증 |
| 내일의 지도 | puzzle-tomorrow-map | 도로·병원 입구·주민 출입구 편집 및 승인 |
| 저주상속 | management-curse-inheritance | 세 결산, 장부 조사, 협상, 자원과 기억의 대가 |
| 당신의 박자 | rhythm-your-tempo | 네 번의 무음 진입, 실시간/연습 박자 |
| 붉은 펜으로 남긴 것 | narrative-redline | 삭제선, 문단 이동, 표현 교체, 작가의 답장 |
| 마지막 이삿날 | narrative-last-address | 세 집 조사, 시간 배분, 작은 도움과 마지막 차 |
| 기적 배정과 | puzzle-prayer-office | 오전·오후 날씨와 하류 수문 배정 |

## 검증과 재생성

각 게임의 `tests/<slug>.Tests.csproj`를 .NET 8로 실행한다.
공유 검증은 프로젝트당 8개, 총 80개다. 무결성뿐 아니라 의도한 해결 경로,
반대 선택의 결과, 단서/자원 조건, 편집 취소, 박자 판정 경계 등을 확인한다.
게임 로직을 검사한 뒤 `data/graph.json`을 재생성한다. HTML은 이 그래프를 따른다.

```powershell
& C:/Users/PSI/.dotnet/dotnet.exe test games/_archive/management-curse-inheritance/tests/management-curse-inheritance.Tests.csproj
node kit/tools/verify_revised_pocs.js
python kit/tools/build_revised_gallery.py
```

브라우저 검사 도구는 실제 버튼 조작으로 각 게임의 서로 다른 두 결말과 재시작을
검사하고, C# 그래프와 화면의 전이 결과를 비교한다. 1600 / 768 / 390px 뷰포트와
데이터 누락 안내도 확인한다. PNG는 `png/<slug>.png`에 저장한다.

`python kit/tools/scaffold_revised_concepts.py`는 시나리오·README·HTML 진입점·프로젝트 파일을
재생성한다. 이미 해당 파일을 직접 편집했다면 덮어쓰므로 필요한 경우에만 실행한다.
수작업 `src/Rules.cs`와 기존의 다른 PoC는 건드리지 않는다.

## 범위와 남은 판단

완성된 상업 게임이나 앞서 제안한 전체 캠페인은 아니다. 각 컨셉에서 핵심 조작 하나와
결과의 차이를 짧게 검토하는 버전이다. 기존 프로젝트의 리스킨도 아니다.
음성·환경음 에셋, 자유 문장 입력, 자유 도시 건설, 세이브는 구현 범위에 넣지 않았다.
따라서 마지막 이삿날의 벽 너머 소리는 현재 텍스트 단서로 표현된다.
호흡 기반 지휘는 추상적인 인물 도형과 시각 박자로 표현된다.
기계 검증 통과는 재미·시장성·상업적 완성도를 증명하지 않는다.
