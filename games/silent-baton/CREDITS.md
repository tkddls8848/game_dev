# CREDITS — silent-baton

**이 PoC가 쓰는 에셋은 전부 SIL OFL 1.1(글꼴)뿐이다.** 그 밖의 라이선스는 넣지 않는다 —
PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면 그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/silent-baton/fonts/`(gitignore)에,
PoC가 실제로 쓰는 것만 `presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 글꼴은 원본을 그대로 싣지 않고 **부분집합으로 줄여서** 실었으므로 원본과 해시가 다르다.
> 무엇을 어떻게 손댔는지는 「손댄 것」 열에 적었고, 원본 해시는 그 아래 따로 표로 남겼다.

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/NotoSerifKR-Subset.ttf` | https://github.com/google/fonts/tree/main/ofl/notoserifkr | Google (Noto Project Authors) | SIL OFL 1.1 | **부분집합 797자** (`fontTools.subset`, 23,795,420 → 748,400 바이트. 가변 축 `wght` 유지) | 2026-09-27 | `8befc718912a57da` |
| `presentation/assets/fonts/OFL-NotoSerifKR.txt` | https://github.com/google/fonts/blob/main/ofl/notoserifkr/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `5e0da210fb04058a` |
| `presentation/assets/fonts/BodoniModa-Subset.ttf` | https://github.com/google/fonts/tree/main/ofl/bodonimoda | Owen Earl (Bodoni Moda Project Authors) | SIL OFL 1.1 | **부분집합 797자** (`fontTools.subset`, 162,104 → 85,252 바이트. 가변 축 `opsz,wght` 유지) | 2026-09-27 | `cceb30cbb7af012a` |
| `presentation/assets/fonts/BodoniModa-Italic-Subset.ttf` | https://github.com/google/fonts/tree/main/ofl/bodonimoda | Owen Earl (Bodoni Moda Project Authors) | SIL OFL 1.1 | **부분집합 797자** (`fontTools.subset`, 176,300 → 97,896 바이트) | 2026-09-27 | `d16d7833e451da86` |
| `presentation/assets/fonts/OFL-BodoniModa.txt` | https://github.com/google/fonts/blob/main/ofl/bodonimoda/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `931dfe2e0cd3c944` |

### 부분집합으로 줄인 방법 (재현 가능하다)

`data/*.json` · `presentation/*.html` · `presentation/*.js` · `presentation/palette.json` ·
`presentation/style.css` 에 실제로 나오는 글자 전부에 ASCII 인쇄 가능 문자와 흔히 쓰는 기호·한글 음절을
더해 **797자**를 뽑고, 그 목록으로 잘랐다:

```bash
for f in NotoSerifKR BodoniModa BodoniModa-Italic; do
  python -m fontTools.subset AssetDownloads/concepts/silent-baton/fonts/$f-var.ttf \
         --text-file=797chars.txt \
         --output-file=games/silent-baton/presentation/assets/fonts/$f-Subset.ttf \
         --layout-features='*' --drop-tables+=DSIG --recalc-bounds
done
```

줄인 이유는 하나뿐이다 — 원본 셋이 **24.1MB**인데 화면이 쓰는 글자는 797자다.
OFL은 부분집합·이름 유지 재배포를 허용하고(§2), **패밀리 이름을 바꾸지 않았다**(§4의 금지 대상이 아니다).

> **잘라 낸 뒤 실린 글꼴이 화면의 글자를 전부 덮는지 `fontTools` 로 확인했다** — Noto Serif KR 은
> **0자 누락**이다. Bodoni Moda 는 라틴 전용이라 한글이 없고(464자), 그래서 CSS 글꼴 스택을
> `"BodoniModa","NotoSerifKR",Georgia,serif` 로 두어 한글이 곧바로 Noto Serif KR 로 떨어지게 했다.
> Bodoni Moda 는 제호(`The Silent Season / 01`)와 라틴 표제에만 쓴다.
> 실제 브라우저에서 두 글꼴이 `loaded` 로 올라오는 것까지 확인했다(아래 「화면으로 확인한 것」).

### 원본 다운로드의 해시 (참고용 · 실린 파일과 다르다)

| 원본 파일 | 크기 | SHA-256 (앞 16자) |
|---|---|---|
| `AssetDownloads/concepts/silent-baton/fonts/NotoSerifKR-var.ttf` | 23,795,420 | `11f8d5de6f1b7919` |
| `AssetDownloads/concepts/silent-baton/fonts/BodoniModa-var.ttf` | 162,104 | `550f5e34ee0a828d` |
| `AssetDownloads/concepts/silent-baton/fonts/BodoniModa-Italic-var.ttf` | 176,300 | `dfff1619f8f6871c` |
| `AssetDownloads/concepts/silent-baton/fonts/notoserifkr-OFL.txt` | 4,350 | `5e0da210fb04058a` |
| `AssetDownloads/concepts/silent-baton/fonts/bodonimoda-OFL.txt` | 4,400 | `931dfe2e0cd3c944` |

라이선스 파일은 손대지 않았으므로 실린 것과 원본의 해시가 같다.

---

## 오선지 · 음표 · 신문 조각 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 미색 오선지 | 겹친 `repeating-linear-gradient` 두 겹(종이 결) + SVG `<line>` 다섯 줄 × 무리 넷. 값은 `palette.json` 의 `staff` | 이 저장소의 저작물 |
| 음표 (단원) | SVG `<ellipse>`+`<line>`. **머리 크기=호흡 · 기울기=활 · 짧은 획=표정.** 값은 `palette.json` 의 `glyph` | 이 저장소의 저작물 |
| 잔상 (궤적) | 지난 여섯 박의 음표를 `opacity` 를 낮춰 겹쳐 그린다 | 이 저장소의 저작물 |
| 인쇄 망점 | `radial-gradient` 한 겹을 `background-size` 로 타일링. 점 2px · 간격 6px | 이 저장소의 저작물 |
| 신문 조각 · 테이프 · 접힌 자국 | CSS `transform: rotate` + 그라디언트 두 겹. 값은 `palette.json` 의 `clipping` | 이 저장소의 저작물 |
| `presentation/assets/preview*.png` | **이 저장소의 화면을 Playwright 로 찍은 스크린샷.** 생성 모델이 만든 그림이 아니다 | 이 저장소의 저작물 |

**질감 이미지를 한 장도 받지 않았다.** 종이·오선·음표·망점은 전부 좌표와 값으로 만들었다.

---

## 소리 — **넣지 않았다. 그것이 컨셉이다**

이 게임은 **무음**이다. 청각을 잃은 지휘자가 주인공이므로 플레이어도 듣지 못한다.
뿌리 `CLAUDE.md` 가 요구하는 「의도적인 무음인 경우의 대체 피드백」은 화면이 여섯 갈래로 짊어진다:

1. 음표 머리의 **크기** — 호흡(누적 어긋남)
2. 머리를 지나는 선의 **기울기** — 활(벌어지는 속도)
3. 머리 위의 **획** — 표정(세기 어긋남)
4. **잔상** — 지난 여섯 박의 궤적
5. 지휘봉 **고리** — 지금 보고 있는 무리
6. **신문 조각** — 결과 (연주가 끝난 뒤에만)

빈 칸이나 임시 삐 소리로 때우지 않았고, 대체 피드백을 실제 브라우저에서 확인했다
(잔상 25개가 실제로 그려지는 것, `<audio>` 가 0개인 것).

## 화면으로 확인한 것 (실제 실행 브라우저)

`node kit/tools/verify_takeover.js` 와 같은 방식으로 정적 서버를 띄우고 Playwright/Chromium 으로 확인했다.
써 넣은 곳은 이 PoC 폴더 안뿐이다.

* 글꼴 둘이 `loaded` 로 올라온다 · `<audio>` 0개 (무음)
* 오선·음표·잔상이 실제로 그려진다 (잔상 음표 25개)
* 곡 셋을 끝까지 지휘할 수 있고 신문 조각이 연주가 끝난 뒤에만 나타난다
* 390×844(휴대폭)에서 가로 스크롤이 생기지 않는다
* 페이지 오류·콘솔 오류·요청 실패 **0건**

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 — 배포 저장소의 `OFL.txt` 원문
  (`Copyright 2012 Google Inc.` · `Copyright 2020 The Bodoni Moda Project Authors`)과
  `METADATA.pb`(`license: "OFL"` · `designer:`)를 받아서 읽었다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 글꼴 옆에 같이 실었다** (OFL이 요구한다)
* [x] 표의 SHA-256이 **`presentation/assets/` 에 실린 파일**의 것이다
* [x] AI로 생성한 에셋이 없다 (아래)
* [x] 버린 것을 적었다 (아래)

## AI로 만든 에셋

**없음.** 이미지·음성·음악을 생성 모델로 만들지 않았다. 화면의 모든 형태는
`palette.json` 의 값에서 CSS/SVG로 계산되고, `preview*.png` 는 그 화면을 그대로 찍은 것이다.

> 코드와 문서는 AI 지원으로 작성했다. Steam 의 생성형 AI 공시는 **게임에 실리는 에셋**을 대상으로 하므로
> 공시 대상이 되는 항목은 현재 없다. 출시 단계에서 다시 점검한다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했거나, 연출이 겹쳤다)

| 후보 | 왜 버렸나 |
|---|---|
| 오선지·악보 스캔 이미지 | CC0 표기가 분명하고 **원 페이지에서 확인되는** 것을 찾지 못했다. 받지 않고 SVG 선으로 그었다 |
| 신문 지면 스캔·망점 텍스처 | 대부분 재배포 사이트의 표기뿐이고 원 출처가 끊겨 있었다. `radial-gradient` 타일로 대신했다 |
| 관현악 음원 · 메트로놈 소리 | **일부러 넣지 않았다.** 무음이 컨셉이라 소리를 넣으면 규칙이 깨진다. 「없는 것을 있는 척」이 아니라 「있으면 안 되는 것」이다 |
| `Nanum Myeongjo` · `Song Myung` 등 명조 계열 | 라이선스는 OFL로 문제없지만 **`mystery-blackwood`(명조) · `hybrid-*`(나눔명조) · `hybrid-sown-deck`(송명)의 연출과 겹친다.** 라이선스가 아니라 연출이 이유다 |
| 시스템 글꼴(Georgia / 맑은 고딕) 지정 | 인계본은 글꼴 파일을 싣지 않고 시스템 글꼴을 요청했다. 재배포 문제는 없지만 **보는 사람마다 화면이 달라져 연출을 견줄 수 없다.** OFL 글꼴을 부분집합으로 실어 고정했다 |
