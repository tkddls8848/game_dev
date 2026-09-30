# CREDITS — tactics-whisper-map

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(글꼴)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/tactics/fonts/`(gitignore)에, PoC가 실제로 쓰는 것만
`presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 글꼴은 원본을 그대로 싣지 않고 **부분집합으로 줄여서** 실었으므로 원본과 해시가 다르다.
> 무엇을 어떻게 손댔는지는 「손댄 것」 열에 적었고, 원본 해시는 그 아래 따로 적었다.
> (먼저 만든 PoC에서 이걸 틀렸다 — 파일을 손댄 뒤 **원본 다운로드의 해시**를 적어서
> 표가 그 경로의 파일을 식별하지 못했다.)

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/Gaegu-Bold.ttf` | https://github.com/google/fonts/tree/main/ofl/gaegu | JIKJI SOFT (Gaegu Project Authors) | SIL OFL 1.1 | **부분집합 585자** (`pyftsubset`, 3,165,816 → 552,504 바이트) | 2026-09-27 | `5eb48ffec514a1fc` |
| `presentation/assets/fonts/OFL-gaegu.txt` | https://github.com/google/fonts/blob/main/ofl/gaegu/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `53a9ce47085d9fef` |
| `presentation/assets/fonts/GowunDodum-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/gowundodum | Yanghee Ryu (Gowun Dodum Project Authors) | SIL OFL 1.1 | **부분집합 585자** (`pyftsubset`, 7,229,088 → 223,788 바이트) | 2026-09-27 | `79de9af853c67f21` |
| `presentation/assets/fonts/OFL-gowundodum.txt` | https://github.com/google/fonts/blob/main/ofl/gowundodum/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `a7c73f9521cd646b` |

### 부분집합으로 줄인 방법 (재현 가능하다)

`data/*.json` · `presentation/index.html` · `presentation/palette.json` 에 실제로 나오는 글자 전부에
ASCII 인쇄 가능 문자와 흔히 쓰는 기호·자모를 더해 **585자**를 뽑고, 그 목록으로 잘랐다:

```bash
python -m fontTools.subset <원본.ttf> --text-file=<585자 목록> \
       --output-file=presentation/assets/fonts/<이름>.ttf --layout-features='*' --drop-tables+=DSIG
```

줄인 이유는 하나뿐이다 — 원본 두 개가 **10.3MB**인데 목업이 쓰는 글자는 585자다.
OFL은 부분집합·이름 유지 재배포를 허용하고(§2), **패밀리 이름을 바꾸지 않았다**(§4의 금지 대상이 아니다).

> **`Gaegu` 에는 가운뎃점(·)·줄표(—)·말줄임(…)이 없다.** 손글씨 글꼴이라 원본에 아예 없다.
> 그래서 표제의 글꼴 스택을 `"Gaegu", "GowunDodum", cursive` 로 두어 빠진 기호를 본문 글꼴이 메운다.
> 부분집합이 지운 것이 아니다 — 자르기 전 원본에서도 없다(`fontTools` 로 확인했다).

### 원본 다운로드의 해시 (참고용 · 실린 파일과 다르다)

| 원본 파일 | SHA-256 (앞 16자) |
|---|---|
| `AssetDownloads/tactics/fonts/gaegu-Gaegu-Bold.ttf` | `cc38a4af9506a452` |
| `AssetDownloads/tactics/fonts/gowundodum-GowunDodum-Regular.ttf` | `a6e457933227483a` |
| `AssetDownloads/tactics/fonts/gaegu-OFL.txt` | `53a9ce47085d9fef` |
| `AssetDownloads/tactics/fonts/gowundodum-OFL.txt` | `a7c73f9521cd646b` |

라이선스 파일은 손대지 않았으므로 실린 것과 원본의 해시가 같다.

---

## 천 결 · 바늘땀 · 매듭 · 기운 자리 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 아마천 결 | 겹친 `repeating-linear-gradient` 세 겹(날실·씨실·슬럽). 값은 `palette.json` 의 `weave` | 이 저장소의 저작물 |
| 바늘땀 | SVG `stroke-dasharray`. 밀도 값 다섯은 `palette.json` 의 `stitch` | 이 저장소의 저작물 |
| 뒷면 매듭 | 매듭 원을 `knotBackOffsetPx` 만큼 밀고 `feGaussianBlur` 로 흐리게 깐 것 | 이 저장소의 저작물 |
| 기워 덮인 자리 | SVG `<pattern>` 사선 해칭. 각도·간격은 `palette.json` 의 `patch` | 이 저장소의 저작물 |

**질감 이미지를 한 장도 받지 않았다.** 천·실·매듭은 전부 좌표와 값으로 만들었다.
생성 모델을 쓰지 않았다.

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 —
  Google Fonts 웹사이트의 표기가 아니라 **배포 저장소의 `OFL.txt` 원문**과 `METADATA.pb`
  (`license: "OFL"` · `designer`)를 받아서 읽었다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 글꼴 옆에 같이 실었다** (OFL이 요구한다)
* [x] 표의 SHA-256이 **`presentation/assets/` 에 실린 파일**의 것이다 (손댄 것은 「손댄 것」 열에 적었다)
* [x] AI로 생성한 에셋이 없다 (아래)
* [x] 라이선스를 확인하지 못해 **버린 것**이 있다 (아래)

## AI로 만든 에셋

**없음.** 이 PoC에는 생성 모델로 만든 에셋이 하나도 없다 — 이미지가 아예 없고,
천 결·바늘땀·매듭은 `palette.json` 의 값에서 SVG/CSS로 계산된다.

> 먼저 만든 `games/tactics-partisan-1941` 은 종이 질감 한 장이 AI 생성물(Commons `{{PD-algorithm}}`)이라
> 따로 공시해야 했다. 여기서는 그 한 장을 **아예 만들지 않는 쪽**을 골랐다 — 결이 절차적으로 만들어지면
> 출시 때 바꿀 것도 없다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| 아마천·리넨 질감 사진 | CC0 표기가 분명하고 원 페이지에서 확인되는 리넨 질감을 찾지 못했다. **받지 않고 CSS 그라디언트로 짰다** — 버리는 것이 정답이다 |
| 자수·바늘땀 브러시/패턴 이미지 | 대부분 재배포 사이트의 표기뿐이고 원 출처가 끊겨 있었다. SVG `dasharray` 로 대신했다 |
| 천·바늘·실 효과음 | 손에 든 CC0 팩(Kenney rpg-audio)에 맞는 소리가 없었다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
| `Nanum Myeongjo` 등 명조 계열 | 라이선스는 OFL로 문제없지만 **`mystery-blackwood` 의 연출(명조체)과 겹친다.** 라이선스가 아니라 연출이 이유다 |
