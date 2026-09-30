# CREDITS — deck-attrition

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(폰트)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

넣을 때마다 한 줄 적는다. 적지 않은 에셋은 **없는 것으로 취급하고 지운다.**
원본은 `AssetDownloads/deckbuilder/` 에 받은 그대로 두었다(gitignore 대상).

> **SHA-256 은 이 폴더에 실제로 실린 파일의 해시다** — 원본 다운로드의 해시가 아니다.
> 손댄 파일은 "손댄 것" 열에 무엇을 했는지 적었다. (먼저 만든 PoC에서 이걸 틀려서
> 표가 그 경로의 파일을 식별하지 못한 적이 있다.)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) | 손댄 것 |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/Hahmlet-OFL.txt` | https://github.com/google/fonts/tree/main/ofl/hahmlet | The Hahmlet Project Authors | OFL 1.1 | 2026-09-27 | `ddadb10d28a303e3` | 라이선스 전문. 원본 그대로 (동봉 의무) |
| `presentation/assets/fonts/Hahmlet-Variable.ttf` | https://github.com/google/fonts/tree/main/ofl/hahmlet | The Hahmlet Project Authors | OFL 1.1 | 2026-09-27 | `892bffe530255770` | `Hahmlet[wght].ttf` 를 **파일명만** 바꿈. 부분집합·변환 없음 |
| `presentation/assets/fonts/LibreCaslonText-OFL.txt` | https://github.com/google/fonts/tree/main/ofl/librecaslontext | The Libre Caslon Text Project Authors | OFL 1.1 | 2026-09-27 | `a294245c822c5aa9` | 라이선스 전문. 원본 그대로 (동봉 의무) |
| `presentation/assets/fonts/LibreCaslonText-Variable.ttf` | https://github.com/google/fonts/tree/main/ofl/librecaslontext | The Libre Caslon Text Project Authors | OFL 1.1 | 2026-09-27 | `c11809dbfd544588` | `LibreCaslonText[wght].ttf` 를 **파일명만** 바꿈. 부분집합·변환 없음 |
| `presentation/assets/img/paper.jpg` | https://ambientcg.com/view?id=Paper002 | ambientCG (Lennart Demes) | CC0 1.0 | 2026-09-27 | `e538d9218231e0cb` | `Paper002_1K-JPG_Color.jpg` 를 **폭 700px 로 축소 · JPEG 품질 80 재압축**. 자르거나 색을 바꾸지 않음 |
| `presentation/assets/img/lead.jpg` | https://ambientcg.com/view?id=Metal032 | ambientCG (Lennart Demes) | CC0 1.0 | 2026-09-27 | `4f0df34838b25156` | `Metal032_1K-JPG_Color.jpg` 를 **폭 700px 로 축소 · JPEG 품질 80 재압축**. 납회색 탈색은 파일이 아니라 CSS 필터로 입힌다 |
| `presentation/assets/sfx/press.ogg` | https://kenney.nl/assets/interface-sounds | Kenney (Kenney Vleugels) | CC0 1.0 | 2026-09-27 | `d21d0f0b782445db` | `bong_001.ogg` 를 **파일명만** 바꿈. 소리는 원본 |
| `presentation/assets/sfx/sort_place.ogg` | https://kenney.nl/assets/rpg-audio | Kenney (Kenney Vleugels) | CC0 1.0 | 2026-09-27 | `9851a69d0c613e13` | `metalClick.ogg` 를 **파일명만** 바꿈. 소리는 원본 |
| `presentation/assets/sfx/sort_spent.ogg` | https://kenney.nl/assets/rpg-audio | Kenney (Kenney Vleugels) | CC0 1.0 | 2026-09-27 | `159def979e8e386c` | `metalPot1.ogg` 를 **파일명만** 바꿈. 소리는 원본 |

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 (재배포 사이트의 표기만 믿지 않았다)
  * **글꼴** — `google/fonts` 의 해당 폴더에 `OFL.txt` 가 폰트 파일과 나란히 있고, 그 전문을 받아
    첫 줄까지 읽었다: *"Copyright 2020 The Hahmlet Project Authors … SIL Open Font License, Version 1.1"*,
    *"Copyright 2018 The Libre Caslon Text Project Authors … SIL Open Font License, Version 1.1"*.
    두 전문을 `presentation/assets/fonts/` 에 같이 실었다 — **OFL 은 라이선스 동봉이 의무다**
  * **질감** — `ambientcg.com/view?id=Paper002` · `?id=Metal032` 페이지에서
    *"All assets are released under the Creative Commons CC0 license, making them free to use
    without attribution - even in commercial circumstances."* 를 직접 확인했다.
    두 에셋 모두 `creationMethodName` 이 *Height field photogrammetry* 로, **AI 생성이 아니다**
  * **효과음** — 받은 zip 안의 `License.txt` 에 *"License: (Creative Commons Zero, CC0)"* 가 적혀 있다
    (Interface Sounds 1.0 · RPG Audio, 둘 다 Kenney)
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] SHA-256 은 **이 폴더에 들어 있는 파일**을 다시 해시한 값이다 (원본이 아니라 실제 실린 것)
* [x] AI로 생성한 에셋은 없다

## 버린 것

확인이 끝나지 않았거나 이 연출에 맞지 않아 **쓰지 않고 버린 것**도 적는다 — 버리는 것이 정답이다.

| 후보 | 왜 버렸나 |
|---|---|
| Kenney `board-game-icons` (CC0, 손에 있었다) | 라이선스는 문제없지만 **연출이 거부했다.** 활판 인쇄에서 뜻은 아이콘이 아니라 활자가 나른다. 아이콘을 얹는 순간 "정보가 장식보다 먼저"라는 이 연출의 전제가 무너지고, `games/deck-rewind` 가 이미 같은 팩의 SVG를 쓰고 있어 겹치기도 했다 |
| ambientCG `Paper005` | 크라프트 계열 황갈색이라 `games/hybrid-harvest-deck` 의 갈색 장부와 겹쳤다 |
| ambientCG `Metal035` | 구릿빛이라 "검정·납회색 두 색" 규칙을 깼다 |
| 인쇄기 작동음 | **손에 든 CC0 팩에 없었다.** 없는 것을 있는 척 적지 않는다 — `palette.json` 의 `sfx._없는_것` 에도 같이 적어 두었다 |

## 손대지 않은 것에 대해

이 PoC의 연출 색조(검정·납회색·주홍)는 **에셋을 고쳐서 만든 것이 아니다.**
`index.html` 이 `palette.json` 의 `print.leadTextureSaturation` 값으로 CSS 필터를 건다.
그래야 값 하나를 고쳐 다시 볼 수 있고, 표의 해시도 원본과 한 번만 대조하면 된다.

## AI로 만든 에셋

없음.

Steam 공시 대상이 되는 생성 에셋은 하나도 쓰지 않았다.
로직·밸런스·문서 작성에 AI를 쓴 것은 공시 대상이 아니다 (`docs/PLAN_GENRES.md §2` §8).
