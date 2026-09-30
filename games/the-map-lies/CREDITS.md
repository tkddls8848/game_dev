# CREDITS — the-map-lies

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(글꼴)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/the-map-lies/fonts/`(gitignore)에,
PoC가 실제로 쓰는 것만 `presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 글꼴은 원본을 그대로 싣지 않고 **부분집합으로 줄여서** 실었으므로 원본과 해시가 다르다.
> 무엇을 어떻게 손댔는지는 「손댄 것」 열에 적었고, 원본 해시는 그 아래 따로 적었다.

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/Sunflower-Light.ttf` | https://github.com/google/fonts/tree/main/ofl/sunflower | JIKJISOFT (Sunflower Project Authors) | SIL OFL 1.1 | **부분집합 538자** (`pyftsubset`, 758,332 → 132,864 바이트) | 2026-09-27 | `ce85bfe8bd48353c` |
| `presentation/assets/fonts/OFL-sunflower.txt` | https://github.com/google/fonts/blob/main/ofl/sunflower/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `a9b40759b5821a0c` |
| `presentation/assets/fonts/GamjaFlower-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/gamjaflower | YoonDesign Inc. (Gamja Flower Project Authors) | SIL OFL 1.1 | **부분집합 538자** (`pyftsubset`, 12,615,444 → 372,232 바이트) | 2026-09-27 | `8a336fe5d07ace94` |
| `presentation/assets/fonts/OFL-gamjaflower.txt` | https://github.com/google/fonts/blob/main/ofl/gamjaflower/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `39de3de5f1873f89` |

### 왜 이 두 글꼴인가

`Sunflower Light` 는 가늘고 기하적인 한글 산세리프라 **제도용 레터링**의 결이 난다 —
도면의 본문과 수치가 이 글꼴이어야 도면으로 읽힌다.
`GamjaFlower` 는 도면 위에 **연필로 쓴 주석**에만 쓴다. 본문에 쓰면 도면이 낙서가 되므로
`palette.json` 의 `type._손글씨` 에 그 규칙을 값으로 적어 두었다.

먼저 만든 PoC와 겹치지 않는 것도 고른 이유다 — `mystery-blackwood`(명조),
`tactics-whisper-map`(Gaegu · GowunDodum) 과 아무것도 겹치지 않는다.

### 부분집합으로 줄인 방법 (재현 가능하다)

`data/*.json` · `presentation/index.html` · `presentation/palette.json` 에 실제로 나오는 글자 전부에
ASCII 인쇄 가능 문자와 흔히 쓰는 기호를 더해 **538자**를 뽑고, 그 목록으로 잘랐다:

```bash
python -m fontTools.subset <원본.ttf> --text-file=<538자 목록> \
       --output-file=presentation/assets/fonts/Sunflower-Light.ttf --layout-features='*' --drop-tables+=DSIG
```

줄인 이유는 하나뿐이다 — 원본 두 개가 **13.4MB**인데 목업이 쓰는 글자는 538자다.
OFL은 부분집합·이름 유지 재배포를 허용하고(§2), **패밀리 이름을 바꾸지 않았다**(§4의 금지 대상이 아니다).

> **글자 수를 세는 대상에 `README.md` 와 `CREDITS.md` 는 넣지 않았다.**
> 문서를 한 줄 고칠 때마다 글꼴이 바뀌면 이 표의 해시가 매번 어긋난다.
> 화면(`index.html`)이 쓰는 글자만 센다.

### 원본 다운로드의 해시 (참고용 · 실린 파일과 다르다)

| 원본 파일 | 크기 | SHA-256 (앞 16자) |
|---|---|---|
| `AssetDownloads/concepts/the-map-lies/fonts/sunflower-Sunflower-Light.ttf` | 758,332 | `dd9fb97aa9ec1fdb` |
| `AssetDownloads/concepts/the-map-lies/fonts/gamjaflower-GamjaFlower-Regular.ttf` | 12,615,444 | `ece32819ed585363` |
| `AssetDownloads/concepts/the-map-lies/fonts/sunflower-OFL.txt` | 4,348 | `a9b40759b5821a0c` |
| `AssetDownloads/concepts/the-map-lies/fonts/gamjaflower-OFL.txt` | 4,354 | `39de3de5f1873f89` |

라이선스 파일은 손대지 않았으므로 실린 것과 원본의 해시가 같다.

---

## 항공사진 · 트레이싱지 · 연필 · 지우개 자국 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 흑백 항공사진 | **도시 데이터에서 그린다.** 칸 종류마다 회색값(`palette.json` 의 `color.photoDark/photoHaze/photoLight`)을 주고 SVG `rect` 로 깐다. 사진이 아니라 **그 순간의 도시**다 | 이 저장소의 저작물 |
| 사진 입자 | SVG `<pattern>` 두 점(검정 한 점 · 흰 한 점). 값은 `palette.json` 의 `photo.grainAlpha` · `grainPitchPx` | 이 저장소의 저작물 |
| 종이 결 | 겹친 `repeating-linear-gradient` 두 겹 | 이 저장소의 저작물 |
| 트레이싱지 겹 | 반투명 `rect` 를 `vellumSheet.offsetPx` 만큼 밀어 쌓은 것. **장 수가 고친 날 수다** | 이 저장소의 저작물 |
| 연필선 | SVG `rect`/`line`. 굵기·점선은 `palette.json` 의 `pencil` | 이 저장소의 저작물 |
| 지우개 자국 | 반투명 `rect` 의 진하기가 그 칸을 다시 그은 횟수다. 값은 `pencil.eraserAlpha` | 이 저장소의 저작물 |
| 도시가 거부한 칸 | SVG `<pattern>` 사선 해칭(38도). 붉은색은 `color.redDot` | 이 저장소의 저작물 |
| 갇힌 사람 | SVG `circle`. 크기는 `palette.json` 의 `dot` — 크게 그리면 캐릭터가 되고 도면이 아니게 된다 | 이 저장소의 저작물 |

**질감 이미지를 한 장도 받지 않았다.** 항공사진조차 받지 않았다 —
회색 칸이 곧 도시의 상태이므로, 사진을 받으면 데이터와 그림이 갈라진다.
생성 모델을 쓰지 않았다.

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 —
  Google Fonts 웹사이트의 표기가 아니라 **배포 저장소의 `OFL.txt` 원문**과 `METADATA.pb`
  (`license: "OFL"` · `designer:`)를 받아서 읽었다. 두 파일 모두 SIL OFL 1.1 전문을 담고 있다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 글꼴 옆에 같이 실었다** (OFL이 요구한다)
* [x] 표의 SHA-256이 **`presentation/assets/` 에 실린 파일**의 것이다 (손댄 것은 「손댄 것」 열에 적었다)
* [x] AI로 생성한 에셋이 없다 (아래)
* [x] 라이선스를 확인하지 못해 **버린 것**이 있다 (아래)

## AI로 만든 에셋

**없음.** 이 PoC에는 생성 모델로 만든 에셋이 하나도 없다 — 이미지가 아예 없고,
항공사진·입자·트레이싱지·연필·지우개 자국은 전부 `data/` 와 `palette.json` 의 값에서
SVG/CSS로 계산된다.

> 먼저 만든 `games/tactics-partisan-1941` 은 종이 질감 한 장이 AI 생성물(Commons `{{PD-algorithm}}`)이라
> 따로 공시해야 했다. 여기서는 그 한 장을 **아예 만들지 않는 쪽**을 골랐다 —
> 결이 절차적으로 만들어지면 출시 때 바꿀 것도 없다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| 실제 흑백 항공사진 | 퍼블릭 도메인인 것(미 정부 측량 사진 등)이 있기는 하지만, **받으면 안 되는 것이었다** — 사진은 고정 이미지이고 이 PoC의 사진은 매일 바뀌는 도시 상태다. 데이터에서 그리는 쪽이 옳다 |
| 트레이싱지 · 제도지 질감 사진 | CC0 표기가 분명하고 원 페이지에서 확인되는 것을 찾지 못했다. **받지 않고 CSS 그라디언트로 짰다** — 버리는 것이 정답이다 |
| 연필 · 지우개 브러시 이미지 | 대부분 재배포 사이트의 표기뿐이고 원 출처가 끊겨 있었다. SVG `rect` 와 투명도로 대신했다 |
| 연필 · 종이 넘기는 효과음 | 손에 든 CC0 팩(Kenney rpg-audio)에 이 연출에 맞는 소리가 없었다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
| `Gothic A1` · `Nanum Gothic Coding` | 라이선스는 OFL로 문제없지만 같은 회차의 다른 PoC(`last-tenants` · `red-pen`)가 쓰고 있어 갈랐다. 라이선스가 아니라 **연출**이 이유다 |
| 청색 계열 · 세피아 계열 색 | 라이선스와 무관하다. **이미 쓴 연출(청사진 모눈종이 · 바랜 토지 측량도)과 겹치므로 팔레트에서 아예 뺐다** — `palette.json` 의 `_겹치지않게` 에 그 결정을 적어 두었다 |
