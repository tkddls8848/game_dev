# CREDITS — locked-phone

**이 PoC가 쓰는 에셋은 전부 OFL(글꼴)뿐이다.** 그 밖의 라이선스는 넣지 않는다 —
PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면 그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

## 실린 파일 — `presentation/assets/`

SHA-256 은 **실제로 실린 파일**의 것이다.

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | 손댄 것 | SHA-256 |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/GothicA1-Thin.ttf` | https://github.com/google/fonts/tree/main/ofl/gothica1 | Hanken Design Co. | SIL OFL 1.1 | 2026-09-27 | 없음 (바이트 그대로) | `2caee77c0db6afe1cd759c547537361ce66945e7d6fef0f14a9a35fbb4581bb6` |
| `presentation/assets/fonts/GothicA1-Light.ttf` | https://github.com/google/fonts/tree/main/ofl/gothica1 | Hanken Design Co. | SIL OFL 1.1 | 2026-09-27 | 없음 (바이트 그대로) | `927826b1e23366327ebe53d4675a87938ee90b77104cb794087b5138588b53f4` |
| `presentation/assets/fonts/OFL-gothica1.txt` | https://github.com/google/fonts/blob/main/ofl/gothica1/OFL.txt | — (라이선스 전문) | SIL OFL 1.1 | 2026-09-27 | **개명만** (`OFL.txt` → `OFL-gothica1.txt`). 내용 그대로 | `ed95c33f80ccca002e3a360b683c43368f9c5eb024e5b992abb51af3c10b59bc` |

> 굵기 100(Thin)이 이 연출의 전제다. 잠금 화면 시계는 얇아야 하고,
> 굵기가 올라가는 순간 "앱 UI"가 되어 이 화면이 게임이라는 느낌이 사라진다(`presentation/palette.json`).

## 원본 (내려받은 그대로) — `AssetDownloads/concepts/locked-phone/`

`AssetDownloads/` 는 gitignore 대상이다. 위 파일들은 여기서 **바이트 그대로 복사**했으므로
해시가 같다(라이선스 전문은 이름만 바꿨다 — 내용이 같으므로 해시도 같다).

| 원본 파일 | SHA-256 |
|---|---|
| `AssetDownloads/concepts/locked-phone/fonts/GothicA1-Thin.ttf` | `2caee77c0db6afe1cd759c547537361ce66945e7d6fef0f14a9a35fbb4581bb6` |
| `AssetDownloads/concepts/locked-phone/fonts/GothicA1-Light.ttf` | `927826b1e23366327ebe53d4675a87938ee90b77104cb794087b5138588b53f4` |
| `AssetDownloads/concepts/locked-phone/fonts/gothica1-OFL.txt` | `ed95c33f80ccca002e3a360b683c43368f9c5eb024e5b992abb51af3c10b59bc` |

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 — `google/fonts` 의 `ofl/gothica1/`
      폴더에 있는 `OFL.txt` 전문을 글꼴 파일과 같은 요청으로 함께 받아 실었다.
      재배포 사이트의 표기를 믿지 않았다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] OFL 글꼴의 **라이선스 전문을 함께 실었다** (`OFL-gothica1.txt`)
* [x] AI로 생성한 에셋은 없다

## AI로 만든 에셋

**없음.** 이 PoC의 그림은 전부 브라우저가 `data/*.json` 을 읽어 그린 HTML·CSS·SVG다.
이미지 파일이 하나도 없다 — 전화기 몸체도 `border-radius` 이고, 배터리 링도 `<circle>` 두 개이며,
밀려남의 지도도 `<rect>` 들이다.

## 쓰지 않고 버린 것

라이선스를 **원 페이지에서 확인하지 못한 것은 쓰지 않고 버린다.** 여기 적어 둔다.

| 무엇 | 왜 버렸나 |
|---|---|
| 알림음·진동 효과음 | 잠금 화면에 어울리는 CC0 소리를 원 페이지에서 확인하지 못했다. 없는 것을 있는 척 적지 않는다 |
| 앱 아이콘 세트 (Material Symbols · Feather 등) | 라이선스는 대개 Apache-2.0/MIT 라 OFL·CC0·PD 만 쓴다는 이 저장소의 규칙에 맞지 않는다. 카드에서 아이콘을 빼고 **앱 이름 글자**로 대신했고, 그쪽이 오히려 이 연출에 맞았다 |
| 실제 기기 사진·목업 프레임 PNG | CC0 확인이 안 됐고, 애초에 이미지 없이 CSS 로 그리는 편이 데이터와 붙어 있어 낫다 |
| 실존 앱·브랜드 이름 (은행·택배·메신저) | 라이선스가 아니라 상표 문제다. 전부 보통명사(`은행` · `택배` · `메시지` · `건강` · `사진`)로 적었다 |
