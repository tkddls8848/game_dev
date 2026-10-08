# docs — 문서 색인

주제별로 한 문서씩이다. **같은 주제가 여러 파일로 갈라지면 합친다** — 서로를 반복해서
링크하고 있다는 것은 한 문서여야 한다는 뜻이다(2026-09-28에 9개를 5개로 줄였다).

| 문서 | 무엇이 있나 | 언제 보나 |
|---|---|---|
| [`POC_FACTORY.md`](POC_FACTORY.md) | 폴더 규약 · 새 PoC 시작 절차 · 졸업 기준 · 현재 PoC 표 | **새 PoC를 시작할 때 먼저** |
| [`CONCEPTS.md`](CONCEPTS.md) | **컨셉 76개를 한 목록에 동등하게.** 미착수 55 + 구현 21. 특징점이 없는 34개는 '보관'으로 내렸고(2026-10-06), 2026-10-08 에 1~76으로 다시 매겼다(대응표 포함). 상태·에셋·헤드리스 판정 가능성·착수 우선순위를 열로 비교 | 다음에 무엇을 만들지 고를 때 |
| [`PLAN_GENRES.md`](PLAN_GENRES.md) | 장르 계획 다섯 — 파밍 · 로그라이크 덱빌더 · 둘의 결합 · 실시간 전술 · **턴제 전략(HoMM 계열)**. 시장 근거 · 검사기 설계 · Phase · 리스크 | 장르를 정한 뒤 설계할 때 |
| [`ENGINE_FIT.md`](ENGINE_FIT.md) | 컨셉 76개의 엔진별 분류 — Unreal 8 · Unity 3D 12 · Unity 2D/UI 55 · 웹 1, 제작 공수와 검증 기준 (보관 34는 구번호로 따로) | 셸(`unity/` 등)을 붙이기 전, 엔진·파이프라인을 정할 때 |
| [`MUSIC_DIRECTION.md`](MUSIC_DIRECTION.md) | 컨셉(보관 전 101개 기준)을 음악 계열 27개로 묶은 방향 — 악기·BPM·적응형 레이어·쓰지 말 것 · 기존 라이브러리 매칭과 공백 · 참고곡 · 생성 프롬프트 초안 (기계용 `poc-gallery/music/families.json`) | 소리·음악을 붙이기 전 |
| [`ASSET_LIBRARY.md`](ASSET_LIBRARY.md) | 에셋 쓰는 법 · 라이선스 · 출처 표기 의무 | 에셋을 게임에 넣기 전 |
| [`asset-index/`](asset-index/INDEX.md) | 실제 파일 목록 4,208개. 오디오 · 폰트 · 아이콘 · 이미지 · 모델 · 셰이더 | 쓸 수 있는 파일을 찾을 때 |
| [`poc-gallery/`](poc-gallery/) | **실제 엔진이 그린 화면**과 컨셉 티저 영상 | 시안 대비 검증할 때 |
| [`STEAM_RELEASE.md`](https://github.com/tkddls8848/game/blob/main/docs/STEAM_RELEASE.md) | Steam 등록 절차 · 태그 · 가격 · 위시리스트. **다른 저장소에 있다** — 출시를 검토하는 것은 개발 중인 게임뿐이다 | 출시를 검토할 때 |
| [`poc-gallery/concepts/`](poc-gallery/concepts/index.html) | **시안**: 컨셉 76개 각각 HTML 시안(보관 34개의 폴더도 남아 있다)(화면 A/B) + AI 목업 이미지 2장 · 네 열까지 모은 한 장(`all.html`) · 비판 리뷰 전후 3안(`_critique/`) · 이미지 감사 기록(`_tools/audit-2026-09-29.json`) | 컨셉을 고르거나 시안 기준을 잡을 때 |
| [`poc-gallery/fx/`](poc-gallery/fx/index.html) | 효과 구현 샘플 24종(변형 3~4개씩, 조작·소리·수치·Unity 이식 메모) | 연출·피드백을 구현할 때 참고 |

## 여기 없는 것

* **게임별 사실** — 각 게임 폴더의 `README.md`·`CLAUDE.md`·`CREDITS.md`에 있다.
  예: 포지셔닝·문구 규칙은 `games/mystery-blackwood/docs/POSITIONING.md`
* **kit 사용법** — `kit/unity-lowpoly/README.md`(로우폴리 Unity 셸, 작업 기록 포함),
  `kit/tools/`(각 도구의 `--help`)
* **공통 규약과 환경** — 저장소 뿌리의 [`CLAUDE.md`](../CLAUDE.md). 품질 기준·검증 두 갈래·설계 원칙

## 구분해서 읽을 것

`poc-gallery/lowpoly/`는 **엔진 실제 화면**이고 `poc-gallery/video/`·`art-concepts/`·`concepts/`는 **시안**이다(`concepts/`의 `mock-*.jpg`는 AI 생성 이미지).
뿌리 `CLAUDE.md`의 기준대로, 시안은 실제 게임 화면의 증거가 되지 못한다.
