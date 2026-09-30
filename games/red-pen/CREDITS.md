# CREDITS — red-pen

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(글꼴)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/red-pen/`(gitignore)에, PoC가 실제로 쓰는 것만
`presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 이 PoC는 글꼴을 **하나도 손대지 않았다** — 부분집합으로 줄이지도, 이름을 바꾸지도 않았다.
> 그래서 실린 파일의 해시와 내려받은 원본의 해시가 같다. 아래 두 표를 견주면 확인된다.
>
> **왜 줄이지 않았나.** `Nanum Gothic Coding` 의 OFL 고지에는 예약 글꼴 이름(Reserved Font Name)
> `Nanum` · `NanumGothic` 등이 붙어 있다. OFL 1.1 §5 는 **수정본이 예약 이름을 쓰는 것을 금지**하고,
> 글리프를 지우는 부분집합은 §1 의 정의상 수정본이다. 줄이려면 패밀리 이름을 바꿔야 하고
> 그러면 CREDITS 의 이름과 파일 안의 이름이 어긋난다. **원본 그대로 싣는 쪽을 골랐다.**
> `Dokdo` 에는 예약 이름이 없어 줄여도 되지만, 두 글꼴의 처리를 다르게 하면 표가 헷갈린다 —
> 둘 다 원본 그대로 실었다.

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/NanumGothicCoding-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/nanumgothiccoding | Sandoll Communication (NHN Corporation) | SIL OFL 1.1 (예약 이름 `Nanum` 외) | **손대지 않았다** (2,315,924 바이트 그대로) | 2026-09-27 | `787effd7efed2abc` |
| `presentation/assets/fonts/OFL-nanumgothiccoding.txt` | https://github.com/google/fonts/blob/main/ofl/nanumgothiccoding/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `eeacf16032901d0e` |
| `presentation/assets/fonts/Dokdo-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/dokdo | FONTRIX | SIL OFL 1.1 | **손대지 않았다** (2,150,860 바이트 그대로) | 2026-09-27 | `5b3a3d8d28af31fa` |
| `presentation/assets/fonts/OFL-dokdo.txt` | https://github.com/google/fonts/blob/main/ofl/dokdo/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `dcc832bd71ea62cb` |

### 내려받은 원본의 해시 (실린 파일과 **같아야 한다**)

| 원본 파일 | SHA-256 (앞 16자) |
|---|---|
| `AssetDownloads/concepts/red-pen/fonts/NanumGothicCoding-Regular.ttf` | `787effd7efed2abc` |
| `AssetDownloads/concepts/red-pen/fonts/nanumgothiccoding-OFL.txt` | `eeacf16032901d0e` |
| `AssetDownloads/concepts/red-pen/fonts/Dokdo-Regular.ttf` | `5b3a3d8d28af31fa` |
| `AssetDownloads/concepts/red-pen/fonts/dokdo-OFL.txt` | `dcc832bd71ea62cb` |

손대지 않았으므로 네 줄 전부 위 표와 일치한다. 어긋나면 누군가 파일을 건드린 것이다.

> `AssetDownloads/concepts/red-pen/fonts/NanumGothicCoding-Bold.ttf` 도 받아 두었으나
> **쓰지 않아 싣지 않았다.** 쓰지 않는 파일을 `presentation/assets/` 에 넣지 않는다.

### 왜 이 둘인가

* **Nanum Gothic Coding** — 원고 본문. 폭이 고른 글꼴이라 **원고지 칸 위에 앉는다.**
  비례 글꼴을 쓰면 괘선과 글자가 어긋나 원고지가 그냥 배경 무늬가 된다.
* **Dokdo** — 붉은 펜의 여백 메모와 물음표. 거친 손글씨라 **사람이 그은 것**으로 읽힌다.
  이 둘의 대비가 이 연출의 전부다 — 인쇄된 원고 / 손으로 그은 교정.

> `Gaegu`(`games/tactics-whisper-map`)나 `Nanum Pen Script`(`games/hybrid-sown-deck`)도 손글씨지만
> 둘 다 둥글고 다정하다. 편집자의 붉은 펜은 급하고 각져야 해서 `Dokdo` 를 골랐다.

---

## 원고지 칸 · 붉은 획 · 커피 자국 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 원고지 칸 | 겹친 `repeating-linear-gradient` 두 겹(세로 칸선·가로 줄선). 값은 `palette.json` 의 `grid` | 이 저장소의 저작물 |
| 삭제선 · 삽입 갈고리 · 이동 화살표 · 도려내기 꺾쇠 | SVG `line`/`path`. 모양·굵기는 `palette.json` 의 `marks` · `pen` | 이 저장소의 저작물 |
| 회차별 잉크 | `palette.json` 의 `pen.roundInk` 다섯 색과 `roundAlpha`. **1회차는 바래고 5회차는 새 잉크다** | 이 저장소의 저작물 |
| 손떨림 | 획 끝점을 1px 안에서 민다. **문장 id 에서 계산한 고정값**이라 다시 그려도 같은 자리다(난수가 아니다) | 이 저장소의 저작물 |
| 커피 잔 자국 | SVG 원 두 개(테두리 + 옅은 속). 자리·크기는 `palette.json` 의 `coffee` | 이 저장소의 저작물 |
| 버틴 표시의 점선 | `stroke-dasharray`. 값은 `palette.json` 의 `pen.refusedDash` | 이 저장소의 저작물 |

**질감 이미지를 한 장도 받지 않았다.** 종이·칸·획·얼룩은 전부 좌표와 값으로 만들었다.

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 —
  Google Fonts 웹사이트의 표기가 아니라 **배포 저장소의 `OFL.txt` 원문**과 `METADATA.pb`
  (`license: "OFL"` · `designer`)를 내려받아 읽었다
  (`AssetDownloads/concepts/red-pen/fonts/*-METADATA.pb`)
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 글꼴 옆에 같이 실었다** (OFL이 요구한다)
* [x] 예약 글꼴 이름이 붙은 글꼴을 **수정하지 않았다** (OFL §5)
* [x] 표의 SHA-256이 **`presentation/assets/` 에 실린 파일**의 것이다
* [x] 쓰지 않는 파일을 `presentation/assets/` 에 넣지 않았다
* [x] AI로 생성한 에셋이 없다 (아래)
* [x] 라이선스를 확인하지 못해 **버린 것**이 있다 (아래)

## AI로 만든 에셋

**없음.** 이 PoC에는 생성 모델로 만든 에셋이 하나도 없다 — 이미지 파일이 아예 없고,
원고지 칸·붉은 획·커피 자국은 `palette.json` 의 값에서 CSS/SVG로 계산된다.

원고 본문(「눈이 오면 개가 짖는다」 아홉 문장)은 이 PoC를 위해 쓴 것이고
`data/manuscript.json` 안에 있다. 외부에서 가져온 글이 아니다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| 원고지 스캔 이미지 | 퍼블릭 도메인 표기가 원 출처까지 이어지는 것을 찾지 못했다. **받지 않고 CSS 그라디언트로 칸을 그었다** — 버리는 것이 정답이다 |
| 커피 얼룩 질감 PNG | CC0 표기가 재배포 사이트에만 있었다. SVG 원 두 개로 대신했다 |
| 붉은 펜 브러시 이미지 | 같은 이유. SVG `path` 로 그었고, 그래야 `palette.json` 에서 굵기를 고칠 수 있다 |
| 종이 넘기는 소리 · 펜 긁는 소리 | 손에 든 CC0 팩(Kenney)에 맞는 소리가 없었다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
| `Nanum Pen Script` · `Gaegu` | 라이선스는 OFL로 문제없지만 **이미 다른 PoC가 쓰는 손글씨**이고 결이 다정하다. 라이선스가 아니라 연출이 이유다 |
| `Nanum Myeongjo` 등 명조 계열 | `mystery-blackwood` 의 연출(명조체)과 겹친다 |
