# CREDITS — deck-openhand

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(폰트)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

넣을 때마다 한 줄 적는다. 적지 않은 에셋은 **없는 것으로 취급하고 지운다.**
원본은 `AssetDownloads/deckbuilder/` 에 받은 그대로 두었다(gitignore 대상).

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) | 손댄 것 |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/EBGaramond-Italic-Variable.ttf` | https://github.com/google/fonts/tree/main/ofl/ebgaramond | The EB Garamond Project Authors | OFL 1.1 | 2026-09-27 | `bba2c4499c93c961` | 가변 폰트 원본 그대로 |
| `presentation/assets/fonts/EBGaramond-OFL.txt` | https://github.com/google/fonts/tree/main/ofl/ebgaramond | The EB Garamond Project Authors | OFL 1.1 | 2026-09-27 | `0985066662eb755e` | 라이선스 전문 (동봉 의무) |
| `presentation/assets/fonts/EBGaramond-Variable.ttf` | https://github.com/google/fonts/tree/main/ofl/ebgaramond | The EB Garamond Project Authors | OFL 1.1 | 2026-09-27 | `ef9512f92f6d579e` | 가변 폰트 원본 그대로 |
| `presentation/assets/icons/card_target.svg` | https://kenney.nl/assets/board-game-icons | Kenney | CC0 1.0 | 2026-09-27 | `834f7b45a5c7d93b` | 원본 그대로 (Vector/Icons 의 SVG) |
| `presentation/assets/icons/crown_a.svg` | https://kenney.nl/assets/board-game-icons | Kenney | CC0 1.0 | 2026-09-27 | `d0bbefc14de18624` | 원본 그대로 (Vector/Icons 의 SVG) |
| `presentation/assets/icons/flask_full.svg` | https://kenney.nl/assets/board-game-icons | Kenney | CC0 1.0 | 2026-09-27 | `7ddb1e52bf26c19e` | 원본 그대로 (Vector/Icons 의 SVG) |
| `presentation/assets/icons/hourglass.svg` | https://kenney.nl/assets/board-game-icons | Kenney | CC0 1.0 | 2026-09-27 | `5abed8d1c98b26dc` | 원본 그대로 (Vector/Icons 의 SVG) |
| `presentation/assets/icons/shield.svg` | https://kenney.nl/assets/board-game-icons | Kenney | CC0 1.0 | 2026-09-27 | `377da5c6c7bac6cf` | 원본 그대로 (Vector/Icons 의 SVG) |
| `presentation/assets/icons/sword.svg` | https://kenney.nl/assets/board-game-icons | Kenney | CC0 1.0 | 2026-09-27 | `65c51c8d87d04ee3` | 원본 그대로 (Vector/Icons 의 SVG) |
| `presentation/assets/img/paper.jpg` | https://ambientcg.com/view?id=Cardboard004 | ambientCG (Lennart Demes) | CC0 1.0 | 2026-09-27 | `9d5dd8a4b09f9da4` | `Cardboard004_1K-JPG_Color.jpg` 를 폭 700px 로 축소 · JPEG 품질 80 재압축 |
| `presentation/assets/img/tarot-hanged.jpg` | https://commons.wikimedia.org/wiki/File:RWS_Tarot_12_Hanged_Man.jpg | Pamela Colman Smith (1909) | 퍼블릭 도메인 (PD-old / PD-US) | 2026-09-27 | `2c0bf65b46ceee2b` | 폭 460px 로 축소 · JPEG 품질 82 재압축 |
| `presentation/assets/img/tarot-hierophant.jpg` | https://commons.wikimedia.org/wiki/File:RWS_Tarot_05_Hierophant.jpg | Pamela Colman Smith (1909) | 퍼블릭 도메인 (PD-old / PD-US) | 2026-09-27 | `1df705f36b24b564` | 폭 460px 로 축소 · JPEG 품질 82 재압축 |
| `presentation/assets/img/tarot-tower.jpg` | https://commons.wikimedia.org/wiki/File:RWS_Tarot_16_Tower.jpg | Pamela Colman Smith (1909) | 퍼블릭 도메인 (PD-old / PD-US) | 2026-09-27 | `9a634663536f56b0` | 폭 460px 로 축소 · JPEG 품질 82 재압축 |
| `presentation/assets/sfx/card_place.ogg` | https://kenney.nl/assets/interface-sounds | Kenney | CC0 1.0 | 2026-09-27 | `4ac4d1cef7e93696` | `drop_002.ogg` 를 파일명만 바꿈 |
| `presentation/assets/sfx/damage.ogg` | https://kenney.nl/assets/rpg-audio | Kenney | CC0 1.0 | 2026-09-27 | `6c2064d0ef988d1e` | `knifeSlice2.ogg` 를 파일명만 바꿈 |
| `presentation/assets/sfx/reveal.ogg` | https://kenney.nl/assets/rpg-audio | Kenney | CC0 1.0 | 2026-09-27 | `fa81ac2fedc8c641` | `bookFlip1.ogg` 를 파일명만 바꿈 |

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 (재배포 사이트의 표기만 믿지 않았다)
  * `kenney.nl` 의 각 에셋 페이지에 `License: Creative Commons CC0` 이 적혀 있고,
    받은 zip 안의 `License.txt` 에도 같은 문구가 있다
  * `ambientcg.com/view?id=...` 페이지에 "All assets are released under the Creative Commons CC0
    license" 가 적혀 있다
  * 폰트는 `google/fonts` 의 해당 폴더에 `OFL.txt` 가 폰트 파일과 함께 들어 있고, 그 전문을
    `presentation/assets/fonts/` 에 같이 두었다 (OFL 은 라이선스 동봉이 의무다)
  * Rider-Waite 타로(1909, Pamela Colman Smith)는 Wikimedia Commons 의 파일 페이지가
    `Public domain` 으로 표시한다
    (미국에서 1929년 이전 출판물이고, 영국에서도 제작자 사망 1951년 + 70년이 지났다).
    **다만 이것은 파일 페이지의 표기를 읽은 것이고 법률 검토는 아니다** — 출시 전에 확인할 목록에 남긴다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] SHA-256 은 **이 폴더에 들어 있는 파일**을 다시 해시한 값이다 (원본이 아니라 실제 쓰는 것)
* [x] AI로 생성한 에셋은 없다

## 손댄 것에 대해

이미지는 목업에 맞게 **축소·재압축만** 했다. 잘라내거나 색을 바꾸지 않았다.
연출의 색조(청사진 톤 · 크림 톤)는 에셋을 고친 것이 아니라 `index.html` 의 CSS 필터로 입힌다 —
그래야 `presentation/palette.json` 의 값 하나를 고쳐 다시 볼 수 있다.

효과음은 **파일명만** 바꿨다. 소리 자체는 원본이다.

## AI로 만든 에셋

없음.

Steam 공시 대상이 되는 생성 에셋은 하나도 쓰지 않았다.
로직·밸런스·문서 작성에 AI를 쓴 것은 공시 대상이 아니다 (`docs/PLAN_GENRES.md §2` §8).
