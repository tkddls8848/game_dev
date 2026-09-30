# CREDITS — tactics-partisan-1941

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(폰트)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/tactics/`(gitignore)에, PoC가 실제로 쓰는 것만
`presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

---

## 글꼴 (OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|
| `presentation/assets/fonts/BlackHanSans-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/blackhansans | Zess Type (Black Han Sans Project) | SIL OFL 1.1 | 2026-09-27 | `3196080928402668` |
| `presentation/assets/fonts/OFL-blackhansans.txt` | https://github.com/google/fonts/blob/main/ofl/blackhansans/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 2026-09-27 | `c324192c8b3a988b` |
| `presentation/assets/fonts/StardosStencil-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/stardosstencil | Vernon Adams · Impallari Type | SIL OFL 1.1 | 2026-09-27 | `208b13d15387c282` |
| `presentation/assets/fonts/OFL-stardosstencil.txt` | https://github.com/google/fonts/blob/main/ofl/stardosstencil/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 2026-09-27 | `88d3abd47414e791` |
| `presentation/assets/fonts/NanumGothic-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/nanumgothic | NAVER Corporation | SIL OFL 1.1 | 2026-09-27 | `76f45ef4a6bcff34` |
| `presentation/assets/fonts/OFL-nanumgothic.txt` | https://github.com/google/fonts/blob/main/ofl/nanumgothic/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 2026-09-27 | `eeacf16032901d0e` |

세 글꼴 모두 **각 패밀리 폴더의 `OFL.txt` 원문을 직접 받아** 확인했고, 그 파일을 글꼴 옆에 같이 둔다.
Google Fonts 웹사이트의 표기가 아니라 배포 저장소의 라이선스 파일이 근거다.

## 질감 (CC0)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|
| `presentation/assets/textures/paper-grain.jpg` | https://commons.wikimedia.org/wiki/File:Old_Paper_texture.jpg | Wikimedia 사용자 Arka09185 (leonardoai) | CC0 1.0 (`{{self\|cc-zero}}`) + PD-algorithm | 2026-09-27 | `a82d3b9453b4bf43` |
| (원본) `AssetDownloads/tactics/textures/Old_Paper_texture.jpg` | 위와 같음 | 위와 같음 | 위와 같음 | 2026-09-27 | `d846ac0d1c82b8ff` |

`presentation/` 쪽은 원본(2048px 컬러)을 **1024px 흑백으로 줄인 파생본**이다. 그래서 SHA-256이 다르다.
라이선스는 Commons 파일 페이지의 위키텍스트를 직접 읽어 `{{self|cc-zero}}`와 `{{PD-algorithm}}`을 확인했다.
**이 파일은 AI로 만들어진 이미지다 — 아래 "AI로 만든 에셋"에 다시 적는다.**

## 효과음 (CC0)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|
| `presentation/assets/sfx/footstep00.ogg` | https://kenney.nl/assets/rpg-audio | Kenney Vleugels (kenney.nl) | CC0 1.0 | 2026-09-27 | `6fe61ef1fc3bcf0e` |
| `presentation/assets/sfx/footstep03.ogg` | https://kenney.nl/assets/rpg-audio | Kenney Vleugels (kenney.nl) | CC0 1.0 | 2026-09-27 | `a86756b9af9f3704` |
| `presentation/assets/sfx/metalLatch.ogg` | https://kenney.nl/assets/rpg-audio | Kenney Vleugels (kenney.nl) | CC0 1.0 | 2026-09-27 | `ba9ba60b172b3ebc` |
| `presentation/assets/sfx/kenney-License.txt` | 위 팩에 들어 있는 원문 | Kenney Vleugels | CC0 1.0 | 2026-09-27 | `5735dfd72cb64cbb` |

팩 안의 `License.txt`가 "Creative Commons Zero, CC0"를 명시한다 — 그 파일을 같이 둔다.
받은 날짜는 이 PoC가 팩에서 꺼낸 날이다(팩 자체는 저장소가 전에 받아 둔 `AssetDownloads/Kenney/rpg-audio.zip`).

> **총성·폭발 효과음은 없다.** 손에 든 CC0 팩에 맞는 소리가 없었고, 없는 것을 있는 척 적지 않는다.
> `presentation/palette.json`의 `sfx._없는_것`에도 같은 사실을 적어 두었다.

## 문양·아이콘 (자작)

| 파일 | 만든 방법 | 라이선스 |
|---|---|---|
| `presentation/assets/icons/star.svg` · `gear.svg` · `arrow.svg` · `eye.svg` · `wave.svg` · `trap.svg` | 이 PoC에서 직접 그린 SVG 경로 (구성주의 기본 도형: 별·톱니·화살표·눈·음파·삼각) | 이 저장소의 저작물 |

생성 모델을 쓰지 않았다. 좌표를 직접 적은 경로 여섯 개다.

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다
  (글꼴: 배포 저장소의 `OFL.txt` · 질감: Commons 파일 페이지의 라이선스 틀 · 효과음: 팩 안의 `License.txt`)
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] AI로 생성한 에셋을 아래에 따로 적었다 (Steam 공시 대상이다)
* [x] 라이선스를 확인하지 못해 **지운 에셋**이 있다 — 아래 "버린 것"

## AI로 만든 에셋

| 파일 | 도구·모델 | 만든 날 | 프롬프트 요지 |
|---|---|---|---|
| `presentation/assets/textures/paper-grain.jpg` | 불명 (업로더 계정명이 `leonardoai`, Commons가 `{{PD-algorithm}}`으로 분류) | 2024-04-23 (업로드일) | 불명 — "old paper texture" |

업로더가 CC0로 내놓았고 Commons가 기계 생성물로 분류했다. 쓰는 데 문제는 없지만
**출시로 간다면 스캔한 종이로 바꾸는 것이 낫다** — 이 한 장을 바꾸면 되도록 연출에서 종이 결은
`palette.json`의 `print.paperTexture` 한 줄로만 참조한다.

## 버린 것 (라이선스를 확인하지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| Commons `File:Old_paper2.jpg` · `Old_paper6.jpg` · `Old_paper7.jpg` | `{{PD-author}}`인데 **저자가 unknown**이고, 출처로 적힌 원 사이트(thedigitalyardsale.com)가 살아 있지 않아 원 페이지에서 확인할 수 없었다 |
| 망점·미스레지스터 질감 이미지 | 라이선스가 분명한 것을 찾지 못했다. 대신 **CSS/SVG로 직접 만들었다**(`index.html`의 `body::after` 망점, `h1`의 미스레지스터 그림자) |
