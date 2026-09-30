# CREDITS — hybrid-sown-deck

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(폰트)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

받은 날은 전부 **2026-09-27**.
**SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다**(앞 16자).
원본은 `AssetDownloads/hybrid/` 에 있다(gitignore). 손댄 파일은 "손댄 것" 열에 무엇을 했는지 적었다.

`presentation/assets/` 의 전량은 아래 표의 다섯 파일이다. 그 밖에는 없다.

## 글꼴 — 전부 OFL 1.1

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) | 손댄 것 |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/NanumPenScript-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/nanumpenscript | Sandoll Communication (NHN Corporation) | SIL OFL 1.1 | 2026-09-27 | `6f0d1ab29c789401` | 없음 — 원본 그대로 |
| `presentation/assets/fonts/SongMyung-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/songmyung | JIKJI | SIL OFL 1.1 | 2026-09-27 | `7f90ab2025091156` | 없음 — 원본 그대로 |

> **손글씨 라벨은 나눔손글씨 펜, 본문은 송명이다.** 이 연출은 "손으로 적은 표본 라벨"이 핵이라
> 라벨 글꼴이 곧 연출이다. 먼저 만든 하이브리드 둘이 쓴 글꼴(Do Hyeon · Gothic A1 · Nanum Myeongjo · Noto Emoji)과
> 겹치지 않게 골랐다 — 같은 장르의 PoC를 나란히 놓고 볼 때 글꼴이 같으면 연출 비교가 되지 않는다.
>
> **서브셋을 뜨지 않았다.** 용량은 줄지만 OFL의 Reserved Font Name 조항을 따로 따져야 한다
> (나눔 계열은 `Nanum`·`NanumPen`을 예약 이름으로 명시한다). 용량보다 그쪽이 비싸다.
> 그래서 위 해시는 `google/fonts` 저장소의 파일과 그대로 일치한다.

라이선스 확인: 두 글꼴 모두 저장소의 `METADATA.pb` 에서 `license: "OFL"` 을 직접 읽고,
같은 폴더의 `OFL.txt` 원문을 내려받아 확인했다. 재배포 사이트(구글 폰트 웹 UI)의 표기만 믿지 않았다.

## 질감 — CC0 1.0

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) | 손댄 것 |
|---|---|---|---|---|---|---|
| `presentation/assets/img/mount-paper.jpg` | https://ambientcg.com/a/Paper004 | ambientCG (Lennart Demes) | CC0 1.0 | 2026-09-27 | `4714900b3a00ff91` | 원본 `Paper004_1K-JPG_Color.jpg`(sha `bd739ab300202b41`, 1024×1024 RGB)를 **512×512 회색조 · JPEG 품질 82** 로 줄였다. 크림색은 그림이 아니라 CSS(`multiply` 합성 + `palette.json` 의 `mount`)가 낸다 |

라이선스 확인: https://ambientcg.com/license 본문을 직접 읽었다 —
"All ambientCG assets are provided under the Creative Commons CC0 1.0 Universal License.
This applies to the downloadable asset files and the material preview renders shown for each asset on the site."
`api/v2/full_json?id=Paper004` 로 자산이 실재함(`shortLink: https://ambientcg.com/a/Paper004`)도 확인했다.

> 먼저 만든 PoC들이 이미 쓴 ambientCG 자산(Paper001 · Paper003 · Paper006 · Cardboard001 · Cardboard004 · Snow006)과
> 겹치지 않는 것을 골랐다.

## 라이선스 원문 — 에셋과 함께 싣는다

**OFL 1.1은 글꼴을 재배포할 때 라이선스 원문을 같이 싣기를 요구한다.** 그래서 파일로 넣었다.

| 파일 | 무엇의 라이선스인가 | 출처 (URL) | SHA-256 (앞 16자) |
|---|---|---|---|
| `presentation/assets/fonts/OFL-NanumPenScript.txt` | Nanum Pen Script (OFL 1.1) | https://github.com/google/fonts/blob/main/ofl/nanumpenscript/OFL.txt | `eeacf16032901d0e` |
| `presentation/assets/fonts/OFL-SongMyung.txt` | Song Myung (OFL 1.1) | https://github.com/google/fonts/blob/main/ofl/songmyung/OFL.txt | `40bd3f35477284c0` |

CC0는 원문 동봉을 요구하지 않는다. `mount-paper.jpg` 의 근거는 위 라이선스 페이지 인용문이다.

## 버린 것

버리는 것이 정답이고 감점이 아니다. 라이선스를 원 페이지에서 확인하지 못했거나 이 연출에 필요 없어진 것들.

| 후보 | 왜 버렸나 |
|---|---|
| Gowun Batang (OFL 1.1, 확인됨) | 라이선스는 문제없었다. **8.4MB** 라 이 저장소의 다른 PoC 에셋(최대 7.2MB)을 혼자 넘어선다. 같은 역할을 2.0MB 의 Song Myung 이 한다 |
| 눌러 말린 꽃 사진 · 식물 도판 스캔 | 표본 그림을 **사진으로 쓰지 않기로 했다.** 표본을 `data/` 의 세대(generation) 수치로 SVG 로 그려야 "카드가 자란다"가 그림이 아니라 데이터에서 나온다. 사진을 쓰면 세대마다 그림을 따로 구해야 하고, 그 순간 목업이 손그림이 된다 |
| 문양 글꼴(Noto Emoji 등) | 위와 같은 이유로 필요 없어졌다. 먼저 만든 하이브리드 둘이 이미 쓴 방식이기도 하다 |
| 소리(Kenney RPG Audio 등) | CC0 로 확인돼 있고 받아 둔 것도 있지만, 이 목업은 소리를 틀지 않는다. **쓰지 않는 파일을 싣지 않는다** — `presentation/assets/` 는 실제로 쓰는 것만 담는다 |

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다
  * 글꼴: `google/fonts` 의 `METADATA.pb`(`license: "OFL"`)와 같은 폴더 `OFL.txt` 원문
  * 질감: `ambientcg.com/license` 본문
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] 표에 적지 않은 에셋은 `presentation/assets/` 에 없다 (위 표들이 그 폴더의 전량이다)
* [x] OFL 글꼴마다 라이선스 원문을 같이 실었다 — OFL 1.1 이 재배포에 요구하는 것이다
* [x] **SHA-256은 실린 파일을 해시한 값이다.** 손댄 파일(`mount-paper.jpg`)은 원본 해시를 따로 적고
      무엇을 했는지 "손댄 것" 열에 남겼다 — 먼저 만든 PoC에서 원본 해시를 적어 표가 그 경로의 파일을
      식별하지 못한 일이 있었다

## AI로 만든 에셋

**없음.** `presentation/assets/` 의 다섯 파일 가운데 AI로 만든 것은 하나도 없다.

표본 그림은 AI 생성 이미지가 아니라 `presentation/index.html` 이 `palette.json` 의 수치로 그리는 **SVG 도형**이다
(코드이지 에셋이 아니다).

`src/` 의 로직과 `data/` 의 수치, `presentation/index.html` 의 코드는 AI로 썼다 —
**Steam 공시 대상은 출하 에셋**이므로 여기에 해당하지 않는다
(`docs/PLAN_GENRES.md §3` §8 마지막 줄).
