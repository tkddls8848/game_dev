# CREDITS — last-tenants

**이 PoC가 쓰는 에셋은 전부 SIL OFL(글꼴)뿐이다.** 그 밖의 라이선스는 넣지 않는다 —
PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면 그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/last-tenants/`(gitignore)에 있고,
PoC가 실제로 쓰는 것만 `presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 글꼴은 원본을 그대로 싣지 않고 **부분집합으로 줄여서** 실었으므로 원본과 해시가 다르다.
> 무엇을 어떻게 손댔는지는 「손댄 것」 열에 적었고, 원본 해시는 그 아래 따로 표로 남겼다.

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/GothicA1-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/gothica1 | HanYang I&C Co | SIL OFL 1.1 | **부분집합 644자** (`pyftsubset`, 2,295,400 → 262,384 바이트) | 2026-09-27 | `f0a0bda1de81fcac` |
| `presentation/assets/fonts/GothicA1-Bold.ttf` | https://github.com/google/fonts/tree/main/ofl/gothica1 | HanYang I&C Co | SIL OFL 1.1 | **부분집합 644자** (`pyftsubset`, 2,287,068 → 262,688 바이트) | 2026-09-27 | `d744177bc95b934c` |
| `presentation/assets/fonts/OFL-gothica1.txt` | https://github.com/google/fonts/blob/main/ofl/gothica1/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `ed95c33f80ccca00` |
| `presentation/assets/fonts/NanumGothicCoding-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/nanumgothiccoding | Sandoll Communication (NHN Corporation) | SIL OFL 1.1 | **부분집합 644자** (`pyftsubset`, 2,315,924 → 241,740 바이트) | 2026-09-27 | `1392918dd3c32c83` |
| `presentation/assets/fonts/OFL-nanumgothiccoding.txt` | https://github.com/google/fonts/blob/main/ofl/nanumgothiccoding/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `eeacf16032901d0e` |

### 부분집합으로 줄인 방법 (재현 가능하다)

`data/*.json` · `presentation/index.html` · `presentation/palette.json` 에 실제로 나오는 글자 전부에
ASCII 인쇄 가능 문자, 흔히 쓰는 기호, 한글 자모를 더해 **644자**를 뽑고 그 목록으로 잘랐다:

```bash
python -m fontTools.subset <원본.ttf> --text-file=<644자 목록> \
       --output-file=presentation/assets/fonts/<이름>.ttf --layout-features='*' --drop-tables+=DSIG
```

줄인 이유는 하나뿐이다 — 원본 셋이 **6.8MB**인데 목업이 쓰는 글자는 644자다.
OFL은 부분집합·이름 유지 재배포를 허용하고(§2), **패밀리 이름을 바꾸지 않았다**(§4의 금지 대상이 아니다).

### 원본 다운로드의 해시 (참고용 · 실린 파일과 다르다)

| 원본 파일 | SHA-256 (앞 16자) |
|---|---|
| `AssetDownloads/concepts/last-tenants/fonts/gothica1-GothicA1-Regular.ttf` | `211151bea98098c5` |
| `AssetDownloads/concepts/last-tenants/fonts/gothica1-GothicA1-Bold.ttf` | `2e883fa0ae548000` |
| `AssetDownloads/concepts/last-tenants/fonts/nanumgothiccoding-Regular.ttf` | `787effd7efed2abc` |
| `AssetDownloads/concepts/last-tenants/fonts/gothica1-OFL.txt` | `ed95c33f80ccca00` |
| `AssetDownloads/concepts/last-tenants/fonts/nanumgothiccoding-OFL.txt` | `eeacf16032901d0e` |

라이선스 파일은 손대지 않았으므로 실린 것과 원본의 해시가 같다.

---

## 갱지 · 파형 · 직인 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 갱지 결 | 겹친 `repeating-linear-gradient` 두 겹(세로 섬유·가로 섬유). 값은 `palette.json` 의 `paper` | 이 저장소의 저작물 |
| 접힌 자국 | `paper.foldLineXPercent` 두 줄을 얇은 그림자로 깐 것 | 이 저장소의 저작물 |
| 파형 | SVG `<rect>` 26개. 높이는 **데이터의 `loudnessPercent` 에서 계산**하고 손으로 그리지 않는다. 무늬는 사인 두 겹(고정) | 이 저장소의 저작물 |
| 붉은 원형 직인 | SVG `<circle>` 둘 + 글자. 각도·굵기는 `palette.json` 의 `seal` | 이 저장소의 저작물 |
| 배치도 격자 | CSS 표. 칸 크기는 `palette.json` 의 `grid` | 이 저장소의 저작물 |

**이미지를 한 장도 받지 않았다.** 종이 · 파형 · 직인은 전부 좌표와 값으로 만든다.
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
갱지 · 파형 · 직인은 `palette.json` 의 값에서 SVG/CSS로 계산된다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| 문 두드리는 소리 · 복도 발소리 (효과음) | **엿듣기가 주제인 PoC에 소리가 없는 것이 가장 아프다.** 손에 든 CC0 팩(Kenney)에 맞는 소리가 없었고, 원 페이지에서 CC0 를 확인할 수 있는 「문에 귀를 댄 소리」를 찾지 못했다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
| 갱지 · 등사 질감 사진 | CC0 표기가 분명하고 원 페이지에서 확인되는 것을 찾지 못했다. **받지 않고 CSS 그라디언트로 짰다** |
| 관공서 서식 스캔 이미지 | 실재하는 관청 문서는 저작권과 별개로 **실제 기관을 사칭하는 모양**이 된다. 쓰지 않고 서식을 직접 짰다(구청 이름도 실재 문서를 베끼지 않았다) |
| `Nanum Myeongjo` 등 명조 계열 | 라이선스는 OFL로 문제없지만 **`mystery-blackwood` 의 연출(명조체)과 겹친다.** 라이선스가 아니라 연출이 이유다 |
| `Gaegu` · `Gowun Dodum` | 같은 이유. `tactics-whisper-map`(자수 지도)이 이미 쓴다 |
