# CREDITS — gods-secretary

**이 PoC가 쓰는 에셋은 전부 SIL OFL(글꼴)뿐이다.** 그 밖의 라이선스는 넣지 않는다 —
PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면 그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/gods-secretary/`(gitignore)에 있고,
PoC가 실제로 쓰는 것만 `presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 글꼴은 원본을 그대로 싣지 않고 **부분집합으로 줄여서** 실었으므로 원본과 해시가 다르다.
> 무엇을 어떻게 손댔는지는 「손댄 것」 열에 적었고, 원본 해시는 그 아래 따로 표로 남겼다.

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/Hahmlet.ttf` | https://github.com/google/fonts/tree/main/ofl/hahmlet | Hypertype (Hahmlet Project Authors) | SIL OFL 1.1 | **부분집합 672자** (`pyftsubset`, 3,559,144 → 614,972 바이트) · 원본 파일명 `Hahmlet[wght].ttf` 를 `Hahmlet.ttf` 로 **개명**(대괄호가 URL에서 인코딩되어 `@font-face` 가 깨진다). 가변 축(wght)은 그대로 살아 있다 | 2026-09-27 | `5cc478b99b4b601e` |
| `presentation/assets/fonts/OFL-hahmlet.txt` | https://github.com/google/fonts/blob/main/ofl/hahmlet/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `ddadb10d28a303e3` |
| `presentation/assets/fonts/NanumPenScript-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/nanumpenscript | Sandoll Communication (NHN Corporation) | SIL OFL 1.1 | **부분집합 672자** (`pyftsubset`, 3,201,664 → 367,960 바이트) | 2026-09-27 | `47af84cbe5aea310` |
| `presentation/assets/fonts/OFL-nanumpenscript.txt` | https://github.com/google/fonts/blob/main/ofl/nanumpenscript/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `eeacf16032901d0e` |

### 부분집합으로 줄인 방법 (재현 가능하다)

`data/*.json` · `presentation/index.html` · `presentation/palette.json` 에 실제로 나오는 글자 전부에
ASCII 인쇄 가능 문자, 흔히 쓰는 기호, 한글 자모, 봉랍의 한자 셋(許·却·留)을 더해 **672자**를 뽑고 그 목록으로 잘랐다:

```bash
python -m fontTools.subset <원본.ttf> --text-file=<672자 목록> \
       --output-file=presentation/assets/fonts/<이름>.ttf --layout-features='*' --drop-tables+=DSIG
```

줄인 이유는 하나뿐이다 — 원본 둘이 **6.4MB**인데 목업이 쓰는 글자는 672자다.
OFL은 부분집합·개명 없는 재배포를 허용하고(§2), **패밀리 이름을 바꾸지 않았다**(§4의 금지 대상은
패밀리 이름이지 파일 이름이 아니다 — 파일 이름만 `Hahmlet[wght].ttf` → `Hahmlet.ttf` 로 바꿨다).

### 원본 다운로드의 해시 (참고용 · 실린 파일과 다르다)

| 원본 파일 | SHA-256 (앞 16자) |
|---|---|
| `AssetDownloads/concepts/gods-secretary/fonts/hahmlet-Hahmlet-wght.ttf` | `892bffe530255770` |
| `AssetDownloads/concepts/gods-secretary/fonts/nanumpenscript-Regular.ttf` | `6f0d1ab29c789401` |
| `AssetDownloads/concepts/gods-secretary/fonts/hahmlet-OFL.txt` | `ddadb10d28a303e3` |
| `AssetDownloads/concepts/gods-secretary/fonts/nanumpenscript-OFL.txt` | `eeacf16032901d0e` |

라이선스 파일은 손대지 않았으므로 실린 것과 원본의 해시가 같다.

---

## 대리석 · 음각 · 봉랍 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 대리석 결 | 각도가 다른 `repeating-linear-gradient` 두 겹 + 아주 옅은 가로 결 한 겹. 값은 `palette.json` 의 `marble` | 이 저장소의 저작물 |
| 음각 글자 | `text-shadow` 두 줄(위는 흰 테, 아래는 옅은 그늘). 값은 `palette.json` 의 `engrave` | 이 저장소의 저작물 |
| 봉랍 도장 셋 | SVG `<polygon>` — 원을 44 조각으로 나눠 **사인 두 겹으로** 반지름을 흔든다(난수를 쓰지 않는다: 씨드 고정이 이 저장소의 규칙이고, 화면이 매번 달라지면 눈으로 대조할 수 없다). 값은 `palette.json` 의 `wax` | 이 저장소의 저작물 |
| 봉랍의 글자 許·却·留 | 유니코드 한자. `Hahmlet` 부분집합에 포함시켰다 | 글자는 저작 대상이 아니다 · 글꼴은 위 표의 OFL |
| 장부의 괘선 | CSS `border-bottom`. 값은 `palette.json` 의 `engrave.ruleWidthPx` | 이 저장소의 저작물 |

**이미지를 한 장도 받지 않았다.** 돌 · 음각 · 밀랍은 전부 좌표와 값으로 만든다.
생성 모델을 쓰지 않았다.

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 —
  Google Fonts 웹사이트의 표기가 아니라 **배포 저장소의 `OFL.txt` 원문**과 `METADATA.pb`
  (`license: "OFL"` · `designer`)를 받아서 읽었다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 글꼴 옆에 같이 실었다** (OFL이 요구한다)
* [x] 표의 SHA-256이 **`presentation/assets/` 에 실린 파일**의 것이다 (손댄 것은 「손댄 것」 열에 적었다 —
  이 PoC는 부분집합에 더해 **개명**도 했으므로 그것도 적었다)
* [x] AI로 생성한 에셋이 없다 (아래)
* [x] 라이선스를 확인하지 못해 **버린 것**이 있다 (아래)

## AI로 만든 에셋

**없음.** 이 PoC에는 생성 모델로 만든 에셋이 하나도 없다 — 이미지가 아예 없고,
대리석 · 음각 · 봉랍은 `palette.json` 의 값에서 SVG/CSS로 계산된다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| 밀랍 인장 사진 · 인장 브러시 | CC0 표기가 분명하고 **원 페이지에서 확인되는** 것을 찾지 못했다. 대부분 재배포 사이트의 표기뿐이고 원 출처가 끊겨 있었다. **받지 않고 SVG로 그렸다** |
| 대리석 질감 사진 | 같은 이유. CSS 그라디언트 두 겹으로 대신했다 — 돌은 결 두 겹이면 읽힌다 |
| 밀랍 눌리는 소리 · 종이 스치는 소리 | 손에 든 CC0 팩(Kenney)에 맞는 소리가 없었다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
| 실재하는 종교의 문양 · 문서 서식 | 라이선스와 별개로, **실재하는 신앙의 표장을 빌리면 그 신앙을 말하는 작품이 된다.** 이 PoC는 신이 등장하지 않는 것이 요점이라 어느 신앙의 기호도 쓰지 않았다 |
| `Nanum Myeongjo` · `Gowun Batang` 등 명조 계열 | 라이선스는 OFL로 문제없지만 **`mystery-blackwood` 의 연출(명조체)과 겹친다.** `Hahmlet` 은 세리프지만 명조가 아니라 획이 각진 슬래브라 골랐다 |
| `Gaegu` (손글씨) | `tactics-whisper-map`(자수 지도)이 이미 쓴다. 편지 글꼴은 `Nanum Pen Script` 로 갈랐다 |
