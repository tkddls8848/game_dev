# CREDITS — farm-signal

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(폰트)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

받은 날: **2026-09-27**. 원본은 `AssetDownloads/farming/`(gitignore)에 그대로 두었다.

> ## 표의 SHA-256 은 **`presentation/assets/` 에 실제로 실린 파일**의 것이다
>
> 먼저 만든 PoC에서 이걸 틀렸다: 폰트를 부분집합으로 줄이고 이미지를 변환한 뒤
> **원본 다운로드의 해시를 적어서** 표가 그 경로의 파일을 식별하지 못했다.
> 해시를 적는 이유는 "이 경로의 이 파일이 그 파일인가"를 나중에 확인하는 것이므로,
> 해시는 실린 파일의 것이어야 한다. 원 출처는 같은 행의 URL 이 가리킨다.
> **손댄 것** 열에 무엇을 했는지 적었다.

---

## 글꼴 — SIL Open Font License 1.1

라이선스 전문을 `presentation/assets/OFL-*.txt` 로 같이 넣었다(**OFL 1.1 §2 가 요구한다**).
목업이 참조하지 않는 파일이지만 지우면 안 된다.

| 파일 (`presentation/assets/`) | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 바이트 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `IBMPlexMono-Regular.ttf` | [google/fonts · ofl/ibmplexmono](https://github.com/google/fonts/tree/main/ofl/ibmplexmono) · 상류 [IBM/plex](https://github.com/IBM/plex) | IBM Corp. (Mike Abbink, Bold Monday) | OFL 1.1 (예약 글꼴 이름 `Plex`) | **없음 — 원본 그대로** | 135,580 | `6a3412f058c7d8df` |
| `SinhoMono-Regular.subset.ttf` | [google/fonts · ofl/nanumgothiccoding](https://github.com/google/fonts/tree/main/ofl/nanumgothiccoding) | NHN Corporation · 디자인 Sandoll Communications | OFL 1.1 (예약 글꼴 이름 `Nanum`·`NanumGothic`) | **부분집합 + 개명** (아래) | 212,084 | `8e344672c92a6fc4` |
| `OFL-IBMPlexMono.txt` | 같은 저장소의 `OFL.txt` | — | OFL 1.1 전문 | 없음 | 4,456 | `7e6b2818edbd8f6a` |
| `OFL-NanumGothicCoding.txt` | 같은 저장소의 `OFL.txt` | — | OFL 1.1 전문 | 없음 | 4,534 | `eeacf16032901d0e` |

### 한글 글꼴을 왜 개명했는가 — OFL 의 예약 글꼴 이름

Nanum Gothic Coding 의 `OFL.txt` 머리에 **Reserved Font Name** 으로
`Nanum`·`NanumGothic` 등이 걸려 있다. 이 목업은 실제로 쓰는 글자만 남겨 부분집합으로 줄였고
(2,315,924 → 212,084 바이트), 글리프를 지운 것은 **OFL 이 말하는 Modified Version** 이다.
OFL 1.1 §3 은 수정본이 예약 글꼴 이름을 쓰지 못하게 한다 — 그래서 파일명과 글꼴 이름표
(name ID 1·3·4·6·16)를 **`Sinho Mono`** 로 바꿨다. 이름을 바꾸는 것이 OFL 이 정한 절차이고,
원 출처는 위 URL 과 동봉한 전문이 가리킨다.

IBM Plex Mono 에도 예약 글꼴 이름(`Plex`)이 있지만 **손대지 않고 원본을 그대로 실었다**
(135 KB 로 작다). 수정본이 아니므로 이름을 그대로 쓰는 것이 맞다.

**왜 이 둘인가:** 둘 다 **등폭**이다. 시세판은 숫자가 칸에 맞아떨어져야 하고, 관측 등록부는
한글 열이 맞아야 읽힌다. 비례 글꼴을 쓰면 딸깍거리는 칸이 만들어지지 않는다.
IBM Plex Mono 는 1960년대 IBM 활자 계보의 등폭체로 전신국·표시관 쪽에 붙고,
Nanum Gothic Coding 은 그 자리에 맞는 유일한 OFL 한글 등폭체였다.

## 소리 — Creative Commons CC0 1.0 Universal

| 파일 (`presentation/assets/`) | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 바이트 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `switch_002.ogg` | [Kenney · Interface Sounds](https://kenney.nl/assets/interface-sounds) | Kenney Vleugels | CC0 1.0 | 없음 | 7,097 | `6fc395be2ad1f99c` |
| `tick_002.ogg` | [Kenney · Interface Sounds](https://kenney.nl/assets/interface-sounds) | Kenney Vleugels | CC0 1.0 | 없음 | 4,514 | `869442f54214be90` |
| `confirmation_002.ogg` | [Kenney · Interface Sounds](https://kenney.nl/assets/interface-sounds) | Kenney Vleugels | CC0 1.0 | 없음 | 14,169 | `33b17a9a9a2397c6` |
| `error_003.ogg` | [Kenney · Interface Sounds](https://kenney.nl/assets/interface-sounds) | Kenney Vleugels | CC0 1.0 | 없음 | 12,256 | `885b28175c7b5111` |
| `bong_001.ogg` | [Kenney · Interface Sounds](https://kenney.nl/assets/interface-sounds) | Kenney Vleugels | CC0 1.0 | 없음 | 4,848 | `d21d0f0b782445db` |
| `License-Kenney-InterfaceSounds.txt` | 같은 배포 zip 안의 `License.txt` | — | CC0 전문 | 없음 | 574 | `f7966c773bbed0ec` |

쓰이는 곳 (`presentation/palette.json → sfx`):
`switch_002` = 시세 칸이 뒤집힘 · `tick_002` = 전신 키(계절을 고를 때) ·
`confirmation_002` = 전문 도착 · `error_003` = 도둑 · `bong_001` = 계절 장 마감.

**라이선스 확인:** 공식 배포 zip 안의 `License.txt` 를 직접 읽었다 (그 파일을 그대로 동봉했다):
"License: (Creative Commons Zero, CC0) … This content is free to use in personal, educational and commercial projects."

## 질감 — Creative Commons CC0 1.0 Universal

| 파일 (`presentation/assets/`) | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 바이트 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `tape.jpg` | [ambientCG · Paper003](https://ambientcg.com/view?id=Paper003) (`Paper003_1K-JPG_Color.jpg`) | Lennart Demes (ambientCG) | CC0 1.0 | **256×256 회색조 · JPEG 품질 70** (116,443 → 1,680 바이트) | 1,680 | `44a76bd81f7945ca` |

종이 테이프에만 `multiply` 로 옅게 겹친다 — 화면에서 빛나지 않는 것이 테이프뿐이라
그 종이 결이 "밖에서 들어온 소식"을 만든다. 나머지 질감(주사선·인광 번짐·비네팅·천공 구멍)은
전부 CSS다(아래 "코드가 만든 것").

**라이선스 확인:** [docs.ambientcg.com/license](https://docs.ambientcg.com/license/) 원 페이지를
직접 읽었다 — "All ambientCG assets are provided under the Creative Commons CC0 1.0 Universal License.
This applies to the downloadable asset files…"

---

## 코드가 만든 것 (외부 에셋이 아니다)

아래는 내려받은 것이 아니라 `presentation/index.html` 이 **`data/` 의 수치로 그리는 것**이라
출처를 적을 대상이 아니다. 뿌리 `CLAUDE.md` 설계 원칙 2("손으로만 만들 수 있는 에셋을 만들지 않는다").

* **시세판 96칸** — 작물 8종 × 계절 12개. 숫자·색·접힘선·뒤집히는 순서가 전부
  `data/` 와 `palette.json` 에서 나온다. 그림 파일이 하나도 없다
* **스플릿플랩의 접힘선** — 칸 가운데의 1px 선(CSS). 이것 하나가 "판이 뒤집힌다"를 만든다
* **종이 테이프의 천공 구멍** — `radial-gradient` 를 `repeat-x` 로 깐다. 구멍 크기·간격은 `palette.json`
* **인광 번짐 · 주사선 · 비네팅 · 표시관 광택** — CSS 그라디언트와 `text-shadow`
* **관측 등록부의 막대** — 관측치에 비례한 `div` 폭. 차트 그림이 아니다
* **밭 그림** — **없다. 일부러 없다.** 이 PoC의 규칙이 "내가 심은 것이 남에게 정보로 간다"이므로,
  밭을 그리면 플레이어가 자기 눈으로 밭을 보게 되고 규칙과 그림이 어긋난다
* **3년치 날씨·기준가·도둑 주사위** — `.NET System.Random(seed)` 를 JS로 이식해
  C#과 같은 수열을 뽑는다. 그래서 화면의 3년이 테스트가 돌린 3년과 같다.
  대조는 눈대중이 아니라 기계로 했다: C# `SignalBoard` 와 JS `computeBoard` 의 출력
  **39줄(계획 3 × 계절 12 + 합계 3)이 완전히 일치**한다
  (`tests/SignalBoardTests.cs` 의 `목업이_맞춰_볼_표본값을_찍는다` 가 그 표를 찍는다)

작물 아이콘은 넣지 않았다. 이 연출에서는 아이콘이 있으면 안 된다 — 밭이 그림으로 나타나는
순간 "숫자와 전문으로만 밭을 본다"가 깨진다. CC0/PD 농작물 아이콘을 못 찾아서가 아니라
**쓰지 않는 것이 연출이다.**

## 쓰지 않은 것 — 그리고 정확히 왜

**확인하지 않은 것을 "확인했더니 안 되더라"로 적지 않는다.** 아래는 실제로 한 일만 적은 것이다.

| 후보 | 왜 쓰지 않았나 |
|---|---|
| 작물·저울·눈(目) 아이콘 일반 | **연출이 거부한다.** 밭이 그림으로 나타나면 "숫자와 전문으로만 본다"가 깨진다. 그래서 아이콘 출처를 찾는 일 자체를 하지 않았다 — 라이선스 판정이 아니라 설계 판정이다 |
| 7세그먼트 표시관 글꼴(DSEG 등) | **라이선스를 확인하지 않았다.** 확인하지 않은 것은 쓰지 않는다. 시세판의 숫자는 IBM Plex Mono 로 찍고 칸·접힘선을 CSS로 그려 같은 인상을 냈다 — 숫자만을 위해 셋째 글꼴을 들이지 않는 편이 낫다 |
| Kenney 밖의 전신 키 녹음 | **라이선스를 확인하지 않았다.** Kenney Interface Sounds(CC0, 원 zip 의 `License.txt` 로 확인함)의 `tick_002` 로 대신했다 |

**확인하지 않은 것을 버리는 것이 정답이고 감점이 아니다.** 확인 못 한 에셋 하나가
출시에서 그 에셋을 쓴 연출 전체를 다시 만들게 한다.

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 (재배포 사이트의 표기만 믿지 않았다)
  * 글꼴: `google/fonts` 의 `ofl/ibmplexmono/OFL.txt` 와 `ofl/nanumgothiccoding/OFL.txt` 를 받아 읽고, IBM Plex 는 상류
    `github.com/IBM/plex` 의 `LICENSE.txt` 도 직접 읽어 예약 글꼴 이름을 확인했다
  * 소리: Kenney 공식 zip 안의 `License.txt`
  * 질감: ambientCG 공식 라이선스 문서 페이지
* [x] CC0/PD/OFL 이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 라이선스 전문을 `presentation/assets/` 에 함께 실었다
* [x] 부분집합으로 줄인 글꼴은 **예약 글꼴 이름을 피해 개명**했다
* [x] 표의 SHA-256 이 **실린 파일**의 것이다 (원본 다운로드의 것이 아니다)
* [x] 확인하지 못한 것은 **버렸고** 위에 적었다
* [x] AI로 생성한 출하 에셋은 없다

## AI로 만든 에셋

**없음.**

로직·데이터·검사기·목업 코드는 AI로 썼지만 **출하 에셋이 아니므로 Steam 공시 대상이 아니다**
(`docs/PLAN_GENRES.md §1` §8: "로직·검사기에는 AI를 마음껏 쓴다(공시 대상 아님). 출하 에셋만 판단한다").
이 PoC에는 그림 파일이 하나도 없고, 화면은 CSS와 `data/` 의 수치로만 그려진다.
