# CREDITS — grafted-memory

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(글꼴)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

내려받은 원본은 `AssetDownloads/concepts/grafted-memory/fonts/`(gitignore)에,
PoC가 실제로 쓰는 것만 `presentation/assets/`에 둔다. 받은 날 **2026-09-27**.

> **표의 SHA-256은 `presentation/assets/` 에 실제로 실린 파일의 것이다.**
> 글꼴은 원본을 그대로 싣지 않고 **부분집합으로 줄여서** 실었으므로 원본과 해시가 다르다.
> 무엇을 어떻게 손댔는지는 「손댄 것」 열에 적었고, 원본 해시는 그 아래 따로 적었다.

---

## 글꼴 (SIL OFL 1.1)

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 손댄 것 | 받은 날 | SHA-256 (앞 16자) |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/Orbit-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/orbit | Sooun Cho, JAMO (Orbit Project Authors) | SIL OFL 1.1 | **부분집합 567자** (`pyftsubset`, 784,480 → 120,304 바이트) | 2026-09-27 | `3f76b0a7428831f5` |
| `presentation/assets/fonts/OFL-orbit.txt` | https://github.com/google/fonts/blob/main/ofl/orbit/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `00f1783a4e7a52d9` |
| `presentation/assets/fonts/Stylish-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/stylish | AsiaSoft Inc. (Stylish Project Authors) | SIL OFL 1.1 | **부분집합 567자** (`pyftsubset`, 10,854,004 → 309,676 바이트) | 2026-09-27 | `4f477c8c1f750433` |
| `presentation/assets/fonts/OFL-stylish.txt` | https://github.com/google/fonts/blob/main/ofl/stylish/OFL.txt | — (라이선스 원문) | SIL OFL 1.1 | 손대지 않았다 | 2026-09-27 | `a9b3e7e1cf5735dc` |

### 왜 이 두 글꼴인가

`Orbit` 은 한 굵기뿐인 **단선 기하 글꼴**이다. 획이 균일해서 표제를 쓰면 글자 자체가
배선도의 선처럼 보인다 — 이 연출이 필요한 것이 정확히 그것이다.
`Stylish` 는 가늘고 군더더기 없는 본문 글꼴이라 「임상 기록」의 결이 난다.

먼저 만든 PoC와 겹치지 않는 것도 고른 이유다 — `mystery-blackwood`(명조),
`tactics-whisper-map`(Gaegu · GowunDodum) 과 아무것도 겹치지 않는다.

### 부분집합으로 줄인 방법 (재현 가능하다)

`data/*.json` · `presentation/index.html` · `presentation/palette.json` 에 실제로 나오는 글자 전부에
ASCII 인쇄 가능 문자와 흔히 쓰는 기호를 더해 **567자**를 뽑고, 그 목록으로 잘랐다:

```bash
python -m fontTools.subset <원본.ttf> --text-file=<567자 목록> \
       --output-file=presentation/assets/fonts/Orbit-Regular.ttf --layout-features='*' --drop-tables+=DSIG
```

줄인 이유는 하나뿐이다 — 원본 두 개가 **11.1MB**인데 목업이 쓰는 글자는 567자다.
OFL은 부분집합·이름 유지 재배포를 허용하고(§2), **패밀리 이름을 바꾸지 않았다**(§4의 금지 대상이 아니다).

> **글자 수를 세는 대상에 `README.md` 와 `CREDITS.md` 는 넣지 않았다.**
> 문서를 한 줄 고칠 때마다 글꼴이 바뀌면 이 표의 해시가 매번 어긋난다.
> 화면(`index.html`)이 쓰는 글자만 센다.

### 원본 다운로드의 해시 (참고용 · 실린 파일과 다르다)

| 원본 파일 | 크기 | SHA-256 (앞 16자) |
|---|---|---|
| `AssetDownloads/concepts/grafted-memory/fonts/orbit-Orbit-Regular.ttf` | 784,480 | `5d0206fb0a9e3eea` |
| `AssetDownloads/concepts/grafted-memory/fonts/stylish-Stylish-Regular.ttf` | 10,854,004 | `3ea2e4c9d0183fdc` |
| `AssetDownloads/concepts/grafted-memory/fonts/orbit-OFL.txt` | 4,478 | `00f1783a4e7a52d9` |
| `AssetDownloads/concepts/grafted-memory/fonts/stylish-OFL.txt` | 4,374 | `a9b3e7e1cf5735dc` |

라이선스 파일은 손대지 않았으므로 실린 것과 원본의 해시가 같다.

---

## 유리 · 인광 · 번짐 · 떨림 (자작)

| 무엇 | 만든 방법 | 라이선스 |
|---|---|---|
| 유리판의 주사선 | `repeating-linear-gradient` 한 겹. 값은 `palette.json` 의 `grade.scanlineAlpha` | 이 저장소의 저작물 |
| 인광 번짐 | SVG `feGaussianBlur` + `feMerge` 한 겹. 값은 `palette.json` 의 `glow` | 이 저장소의 저작물 |
| 기억 노드 · 배선 | SVG `circle` · 3차 베지에 `path`. 굽힘 비율은 `palette.json` 의 `wire.curve` | 이 저장소의 저작물 |
| 연상의 종류 | `stroke-dasharray` 넷(장소·사람·시간·인과). 값은 `palette.json` 의 `wire.kindDash` | 이 저장소의 저작물 |
| 붉은 떨림 | CSS `@keyframes` 2px 진동. 값은 `palette.json` 의 `tremble` | 이 저장소의 저작물 |

**질감 이미지를 한 장도 받지 않았다.** 유리·인광·번짐은 전부 좌표와 값으로 만들었다.
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
유리·인광·배선·떨림은 `palette.json` 의 값에서 SVG/CSS로 계산된다.

## 버린 것 (라이선스를 확인하지 못했거나, 맞는 것을 찾지 못했다)

| 후보 | 왜 버렸나 |
|---|---|
| 유리 건판 · 필름 질감 사진 | CC0 표기가 분명하고 원 페이지에서 확인되는 것을 찾지 못했다. **받지 않고 CSS 그라디언트로 짰다** — 버리는 것이 정답이다 |
| 오실로스코프 · 진공관 배선도 스캔 | 대부분 재배포 사이트의 표기뿐이고 원 출처가 끊겨 있었다. SVG 베지에로 대신했다 |
| 유리 · 심장 · 전기 효과음 | 손에 든 CC0 팩(Kenney rpg-audio)에 이 연출에 맞는 소리가 없었다. **없는 것을 있는 척 적지 않는다** — `palette.json` 의 `sfx._없는_것` 에도 같은 사실을 적었다 |
| `IBM Plex Sans KR` | 라이선스는 OFL로 문제없지만 같은 회차의 다른 PoC(`the-interpreter`)가 쓰고 있어 갈랐다. 라이선스가 아니라 **연출**이 이유다 |
| `Nanum Myeongjo` 등 명조 계열 | 라이선스는 OFL로 문제없지만 **`mystery-blackwood` 의 연출(명조체)과 겹친다** |
