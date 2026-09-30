# CREDITS — curse-ledger

**이 PoC가 쓰는 에셋은 전부 SIL OFL 1.1(글꼴)뿐이다.** 그 밖의 라이선스는 넣지 않는다 —
PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면 그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/curse-ledger/fonts/`(gitignore)에,
PoC가 실제로 쓰는 것만 `presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 글꼴은 원본을 그대로 싣지 않고 **부분집합으로 줄여서** 실었으므로 원본과 해시가 다르다.
> 무엇을 어떻게 손댔는지는 「손댄 것」 열에 적었고, 원본 해시는 그 아래 따로 표로 남겼다.

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/KirangHaerang-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/kiranghaerang | Woowahan Brothers (Kirang Haerang Project Authors) | SIL OFL 1.1 | **부분집합 773자** (`fontTools.subset`, 5,995,608 → 1,403,452 바이트) | 2026-09-27 | `f2456c60619a8df7` |
| `presentation/assets/fonts/OFL-KirangHaerang.txt` | https://github.com/google/fonts/blob/main/ofl/kiranghaerang/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `7e896665d0863d2a` |
| `presentation/assets/fonts/GowunBatang-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/gowunbatang | Yanghee Ryu (Gowun Batang Project Authors) | SIL OFL 1.1 | **부분집합 773자** (`fontTools.subset`, 8,433,296 → 351,848 바이트) | 2026-09-27 | `c8eb76a7d5b3bc40` |
| `presentation/assets/fonts/OFL-GowunBatang.txt` | https://github.com/google/fonts/blob/main/ofl/gowunbatang/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `49a57cc769fa9aff` |

### 부분집합으로 줄인 방법 (재현 가능하다)

`data/*.json` · `presentation/index.html` · `presentation/palette.json` 에 실제로 나오는 글자 전부에
ASCII 인쇄 가능 문자와 흔히 쓰는 기호·한글 음절을 더해 **773자**를 뽑고, 그 목록으로 잘랐다:

```bash
for f in KirangHaerang-Regular GowunBatang-Regular; do
python -m fontTools.subset AssetDownloads/concepts/curse-ledger/fonts/$f.ttf --text-file=773chars.txt \
       --output-file=games/curse-ledger/presentation/assets/fonts/$f.ttf \
       --layout-features='*' --drop-tables+=DSIG --recalc-bounds
done
```

줄인 이유는 하나뿐이다 — 원본 둘이 **14.4MB**인데 목업이 쓰는 글자는 773자다.
OFL은 부분집합·이름 유지 재배포를 허용하고(§2), **패밀리 이름을 바꾸지 않았다**(§4의 금지 대상이 아니다).

> **잘라 낸 뒤 실린 글꼴이 목업의 글자를 전부 덮는지 `fontTools` 로 확인했다.**
> 남은 것은 `` ` ``(Kirang Haerang 원본에 없다)와 `═` 둘뿐이고, 둘 다 **JS 주석에만 있고 화면에 나오지 않는다.**
> 처음 만든 판에는 `palette.json` 의 설명에 한자 `朱印`·`界線` 이 있었는데 **두 글꼴 모두 한자가 없어서**
> 그 자리가 빈 칸으로 나왔다. 글꼴을 늘리지 않고 **설명을 한글로 고쳤다** — 쓸 수 있는 글자가 연출의 한계다.

### 원본 다운로드의 해시 (참고용 · 실린 파일과 다르다)

| 원본 파일 | 크기 | SHA-256 (앞 16자) |
|---|---|---|
| `AssetDownloads/concepts/curse-ledger/fonts/KirangHaerang-Regular.ttf` | 5,995,608 | `d677d28d46698901` |
| `AssetDownloads/concepts/curse-ledger/fonts/GowunBatang-Regular.ttf` | 8,433,296 | `466c593e7147412e` |
| `AssetDownloads/concepts/curse-ledger/fonts/kiranghaerang-OFL.txt` | 4,353 | `7e896665d0863d2a` |
| `AssetDownloads/concepts/curse-ledger/fonts/gowunbatang-OFL.txt` | 4,397 | `49a57cc769fa9aff` |

라이선스 파일은 손대지 않았으므로 실린 것과 원본의 해시가 같다.

| 받았지만 쓰지 않은 원본 | 크기 | SHA-256 (앞 16자) | 왜 안 썼나 |
|---|---|---|---|
| `AssetDownloads/concepts/curse-ledger/fonts/GowunBatang-Bold.ttf` | 8,178,712 | `dbfcaa646e5831e7` | 족보에 굵은 본문이 들어갈 자리가 없었다. **싣지 않았다** — 쓰지 않는 파일을 PoC 안에 두지 않는다 |

---

## 한지 결 · 계선 · 주묵 인장 · 붉은 줄 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 한지 섬유·접힘 | 겹친 `repeating-linear-gradient` 세 겹. 값은 `palette.json` 의 `paper` | 이 저장소의 저작물 |
| 계선(칸을 나눈 가는 선) | `border-left: 1px solid var(--fiber)`. 굵기는 `palette.json` 의 `ledger.ruleWidthPx` | 이 저장소의 저작물 |
| 주묵 인장 | SVG `<rect>` 를 4×4 칸에 찍어 만든다. 같은 문자열이면 같은 인장이 나오게 FNV 해시로 획을 고른다 | 이 저장소의 저작물 |
| 이름 위의 붉은 줄 | 절대 배치한 3px 세로 막대(`.nm::after`). 세로쓰기이므로 "위에 그은 줄"이 화면에서는 오른쪽의 세로선이다 | 이 저장소의 저작물 |
| 청구서가 지나온 길 | SVG 3차 베지에. 앞 세대 칸에서 이번 칸으로 흐른다 | 이 저장소의 저작물 |
| `presentation/assets/preview.png` · `preview-mobile.png` | **이 저장소의 화면을 Playwright/Chromium 으로 찍은 스크린샷.** 생성 모델이 만든 그림이 아니다 | 이 저장소의 저작물 |

**질감 이미지를 한 장도 받지 않았다.** 한지·계선·인장·붉은 줄은 전부 좌표와 값으로 만들었다.
생성 모델을 쓰지 않았다.

---

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 —
  Google Fonts 웹사이트의 표기가 아니라 **배포 저장소의 `OFL.txt` 원문**(`Copyright 2018 The Kirang Haerang
  Project Authors` · `Copyright 2021 The Gowun Batang Project Authors`)과 `METADATA.pb`
  (`license: "OFL"` · `designer:`)를 받아서 읽었다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 글꼴 옆에 같이 실었다** (OFL이 요구한다)
* [x] 표의 SHA-256이 **`presentation/assets/` 에 실린 파일**의 것이다 (손댄 것은 「손댄 것」 열에 적었다)
* [x] AI로 생성한 에셋이 없다 (아래)
* [x] 라이선스나 연출 때문에 **버린 것**이 있다 (아래)

## AI로 만든 에셋

**없음.** 이 PoC에는 생성 모델로 만든 에셋이 하나도 없다 — 받은 이미지가 아예 없고,
한지 결·인장·붉은 줄은 `palette.json` 의 값에서 CSS/SVG로 계산된다.
`presentation/assets/` 의 PNG 둘은 그 화면을 브라우저로 찍은 스크린샷이다.

> 코드와 문서는 AI 지원으로 작성했다. Steam 의 생성형 AI 공시는 **게임에 실리는 에셋**을 대상으로 하므로
> 공시 대상이 되는 항목은 현재 없다. 출시 단계에서 다시 점검한다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했거나, 연출이 겹쳤다)

| 후보 | 왜 버렸나 |
|---|---|
| 한지·닥종이 질감 사진 | CC0 표기가 분명하고 **원 페이지에서 확인되는** 한지 질감을 찾지 못했다. 받지 않고 CSS 그라디언트로 짰다 — 버리는 것이 정답이다 |
| 전각 인장(印) 이미지·폰트 | 재배포 사이트의 표기뿐이고 원 출처가 끊겨 있었다. SVG 칸에 획을 찍어 대신했다 |
| 한자(漢字) 글꼴 | 족보에 한자를 쓰면 훨씬 그럴듯하지만, OFL 한자 글꼴은 전부 수십 MB이고 목업이 쓰는 글자는 800자 미만이다. **한자를 쓰지 않는 쪽**을 골랐다 |
| `Nanum Myeongjo` · `Song Myung` 등 명조 계열 | 라이선스는 OFL로 문제없지만 **`mystery-blackwood`(명조) · `hybrid-*`(나눔명조) · `hybrid-sown-deck`(송명)의 연출과 겹친다.** 라이선스가 아니라 연출이 이유다 |
| 붓·종이·인장 효과음 | 손에 든 CC0 팩(Kenney)에 맞는 소리가 없었다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
