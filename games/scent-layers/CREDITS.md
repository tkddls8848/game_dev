# CREDITS — scent-layers

**이 PoC가 쓰는 에셋은 전부 OFL(글꼴)뿐이다.** 그 밖의 라이선스는 넣지 않는다 —
PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면 그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

## 실린 파일 — `presentation/assets/`

SHA-256 은 **실제로 실린 파일**의 것이다.

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | 손댄 것 | SHA-256 |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/NanumMyeongjo-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/nanummyeongjo | Sandoll Communication | SIL OFL 1.1 | 2026-09-27 | 없음 (바이트 그대로) | `7ed9e8653a8ed04285d51dc343ffea6eb3d9c73afc27383ea8929ee4ffd03205` |
| `presentation/assets/fonts/IBMPlexMono-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/ibmplexmono | Mike Abbink · Bold Monday (IBM) | SIL OFL 1.1 | 2026-09-27 | 없음 (바이트 그대로) | `6a3412f058c7d8dfd9170c41e85ade48e5156ecb89356110ca57a0a27734af46` |
| `presentation/assets/fonts/OFL-nanummyeongjo.txt` | https://github.com/google/fonts/blob/main/ofl/nanummyeongjo/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 2026-09-27 | **개명만** (`OFL.txt` → `OFL-nanummyeongjo.txt`). 내용 그대로 | `8eb1c1019fe7fe6d0b6e7d7bbbba1d9cbdd969d8c5f26455708f6cfb8a77284c` |
| `presentation/assets/fonts/OFL-ibmplexmono.txt` | https://github.com/google/fonts/blob/main/ofl/ibmplexmono/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 2026-09-27 | **개명만** (`OFL.txt` → `OFL-ibmplexmono.txt`). 내용 그대로 | `7e6b2818edbd8f6a01ae80641cc8f16a51080d08fb4e532be3a0b6f74adb07da` |

## 원본 (내려받은 그대로) — `AssetDownloads/concepts/scent-layers/`

`AssetDownloads/` 는 gitignore 대상이다. 위 파일들은 여기서 **바이트 그대로 복사**했으므로
해시가 같다(라이선스 전문은 이름만 바꿨다 — 내용이 같으므로 해시도 같다).

| 원본 파일 | SHA-256 |
|---|---|
| `AssetDownloads/concepts/scent-layers/fonts/NanumMyeongjo-Regular.ttf` | `7ed9e8653a8ed04285d51dc343ffea6eb3d9c73afc27383ea8929ee4ffd03205` |
| `AssetDownloads/concepts/scent-layers/fonts/IBMPlexMono-Regular.ttf` | `6a3412f058c7d8dfd9170c41e85ade48e5156ecb89356110ca57a0a27734af46` |
| `AssetDownloads/concepts/scent-layers/fonts/nanummyeongjo-OFL.txt` | `8eb1c1019fe7fe6d0b6e7d7bbbba1d9cbdd969d8c5f26455708f6cfb8a77284c` |
| `AssetDownloads/concepts/scent-layers/fonts/ibmplexmono-OFL.txt` | `7e6b2818edbd8f6a01ae80641cc8f16a51080d08fb4e532be3a0b6f74adb07da` |

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 — 두 글꼴 모두 `google/fonts` 의
      해당 폴더에 있는 `OFL.txt` 전문을 같은 요청으로 함께 받아 실었다. 재배포 사이트의 표기를 믿지 않았다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 함께 실었다** (위 표의 `OFL-*.txt` 둘)
* [x] AI로 생성한 에셋은 없다

## AI로 만든 에셋

**없음.** 이 PoC의 그림은 전부 브라우저가 `data/*.json` 을 읽어 그린 SVG·CSS다.
이미지 파일이 하나도 없다 — 종이 결도 겹친 그라디언트이고, 크로마토그램의 띠도 `<rect>` 다.

## 쓰지 않고 버린 것

라이선스를 **원 페이지에서 확인하지 못한 것은 쓰지 않고 버린다.** 여기 적어 둔다.

| 무엇 | 왜 버렸나 |
|---|---|
| 냄새·실험실 효과음 | 후각만 남은 게임에 소리를 붙이는 것 자체가 규칙을 어긴다. 대신 쓸 CC0 소리도 찾지 않았다 — 없는 것을 있는 척 적지 않는다 |
| 종이 질감 이미지 (CC0 texture 사이트들) | 원 페이지에서 CC0 를 직접 확인하지 못했고, 겹친 그라디언트로 충분했다. 확인 못 한 것은 쓰지 않는다 |
| 손글씨 표제 글꼴 | 실험 보고서는 활자로 찍혀 있었다. 손글씨는 이 연출의 거짓말이 된다 — 라이선스 문제가 아니라 설계로 버렸다 |
