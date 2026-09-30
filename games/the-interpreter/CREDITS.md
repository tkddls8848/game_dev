# CREDITS — the-interpreter

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(글꼴)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/the-interpreter/`(gitignore)에, PoC가 실제로 쓰는 것만
`presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 이 PoC는 글꼴을 **하나도 손대지 않았다** — 부분집합으로 줄이지도, 이름을 바꾸지도 않았다.
> 그래서 실린 파일의 해시와 내려받은 원본의 해시가 같다. 아래 두 표를 견주면 확인된다.
>
> **왜 줄이지 않았나.** `IBM Plex Sans KR` 의 OFL 고지에는 예약 글꼴 이름(Reserved Font Name)
> `"Plex"` 가 붙어 있다. OFL 1.1 §5 는 **수정본이 예약 이름을 쓰는 것을 금지**하고,
> 글리프를 지우는 부분집합은 §1 의 정의상 수정본이다. 줄이려면 패밀리 이름을 바꿔야 하는데
> 그러면 CREDITS 의 이름과 파일 안의 이름이 어긋난다. **원본 그대로 싣는 쪽을 골랐다.**
> (먼저 만든 `games/tactics-whisper-map` 은 예약 이름이 **없는** 글꼴만 줄였다 — 그래서 거기서는
> 줄이는 것이 맞았고 여기서는 틀리다. 라이선스 전문을 읽고 갈라야 하는 자리다.)

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/CutiveMono-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/cutivemono | Vernon Adams (The Cutive Project Authors) | SIL OFL 1.1 | **손대지 않았다** (80,004 바이트 그대로) | 2026-09-27 | `96a36a0007905868` |
| `presentation/assets/fonts/OFL-cutivemono.txt` | https://github.com/google/fonts/blob/main/ofl/cutivemono/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `ca83490f9c203cda` |
| `presentation/assets/fonts/IBMPlexSansKR-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/ibmplexsanskr | Mike Abbink · Bold Monday (IBM Corp.) | SIL OFL 1.1 (예약 이름 `Plex`) | **손대지 않았다** (2,797,100 바이트 그대로) | 2026-09-27 | `5375037927031236` |
| `presentation/assets/fonts/OFL-ibmplexsanskr.txt` | https://github.com/google/fonts/blob/main/ofl/ibmplexsanskr/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `7e6b2818edbd8f6a` |

### 내려받은 원본의 해시 (실린 파일과 **같아야 한다**)

| 원본 파일 | SHA-256 (앞 16자) |
|---|---|
| `AssetDownloads/concepts/the-interpreter/fonts/CutiveMono-Regular.ttf` | `96a36a0007905868` |
| `AssetDownloads/concepts/the-interpreter/fonts/cutivemono-OFL.txt` | `ca83490f9c203cda` |
| `AssetDownloads/concepts/the-interpreter/fonts/IBMPlexSansKR-Regular.ttf` | `5375037927031236` |
| `AssetDownloads/concepts/the-interpreter/fonts/ibmplexsanskr-OFL.txt` | `7e6b2818edbd8f6a` |

손대지 않았으므로 네 줄 전부 위 표와 일치한다. 어긋나면 누군가 파일을 건드린 것이다.

### 왜 이 둘인가

* **Cutive Mono** — 표제·조항 번호·수치. 수동 타자기 활자를 본뜬 글꼴이라 **정서본(clean copy)** 으로 읽힌다.
  라틴 전용이므로 한국어는 아래 글꼴이 받는다.
* **IBM Plex Sans KR** — 한국어 본문. 획이 곧고 폭이 고르다. 타자면 위에서 흔들리지 않고,
  두 단을 나란히 놓았을 때 줄이 맞는다.

**둘 다 이 저장소의 다른 PoC가 쓰지 않는 글꼴이다**(`Gaegu` · `Gowun Dodum` · `Nanum Myeongjo` ·
`Gothic A1` · `Song Myung` · `Hahmlet` · `EB Garamond` · `JetBrains Mono` · `Do Hyeon` ·
`Black Han Sans` · `Stardos Stencil` 과 겹치지 않는다).

---

## 종이 · 번짐 · 도장 · 괘선 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 아마지 결 | 겹친 `repeating-linear-gradient` 두 겹(가로 섬유·세로 섬유). 값은 `palette.json` 의 `sheet.grainOpacity` | 이 저장소의 저작물 |
| 두 단 가운데 괘선 | CSS 그리드의 1px 칸. 굵기·색은 `palette.json` 의 `sheet.ruleWidthPx` · `color.rule` | 이 저장소의 저작물 |
| 붉은 번짐 | `radial-gradient` 두 겹. 반지름·퍼짐·농도는 `palette.json` 의 `bleedMark` | 이 저장소의 저작물 |
| 조항 도장 | 원형 테두리 + `rotate(-7deg)`. 값은 `palette.json` 의 `bleedMark.stampRotateDeg` | 이 저장소의 저작물 |
| 갈라진 자리의 빗금 | `repeating-linear-gradient(45deg, …)`. 각도·간격은 `palette.json` 의 `split` | 이 저장소의 저작물 |
| 서류철 접힌 자국 | 안쪽 테두리 한 줄. `sheet.edgeFoldPx` | 이 저장소의 저작물 |

**질감 이미지를 한 장도 받지 않았다.** 종이·번짐·도장은 전부 좌표와 값으로 만들었다.

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 —
  Google Fonts 웹사이트의 표기가 아니라 **배포 저장소의 `OFL.txt` 원문**과 `METADATA.pb`
  (`license: "OFL"` · `designer`)를 내려받아 읽었다
  (`AssetDownloads/concepts/the-interpreter/fonts/*-METADATA.pb`)
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 글꼴 옆에 같이 실었다** (OFL이 요구한다)
* [x] 예약 글꼴 이름(`Plex`)이 붙은 글꼴을 **수정하지 않았다** (OFL §5)
* [x] 표의 SHA-256이 **`presentation/assets/` 에 실린 파일**의 것이다
* [x] AI로 생성한 에셋이 없다 (아래)
* [x] 라이선스를 확인하지 못해 **버린 것**이 있다 (아래)

## AI로 만든 에셋

**없음.** 이 PoC에는 생성 모델로 만든 에셋이 하나도 없다 — 이미지 파일이 아예 없고,
종이·번짐·도장·괘선은 `palette.json` 의 값에서 CSS로 계산된다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| 타자기 종이·먹지 질감 사진 | 원 페이지에서 CC0 임이 분명한 것을 찾지 못했다. **받지 않고 CSS 그라디언트로 짰다** — 버리는 것이 정답이다 |
| `Special Elite` (타자기 글꼴) | 타자기 느낌은 가장 좋았지만 **Apache 2.0** 이라 이 저장소의 규칙(글꼴은 OFL만)에 맞지 않는다. `Cutive Mono` 로 바꿨다 |
| 조약 도장·봉랍 이미지 | 퍼블릭 도메인 표기가 원 출처까지 이어지는 것을 찾지 못했다. CSS 원형 테두리로 대신했다 |
| 타자기 키 소리 · 종이 넘기는 소리 | 손에 든 CC0 팩(Kenney)에 맞는 소리가 없었다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
| `Nanum Myeongjo` 등 명조 계열 | 라이선스는 OFL로 문제없지만 **`mystery-blackwood` 의 연출(명조체)과 겹친다.** 라이선스가 아니라 연출이 이유다 |
