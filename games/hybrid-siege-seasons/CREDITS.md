# CREDITS — hybrid-siege-seasons

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(폰트)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

받은 날은 전부 **2026-09-27**. SHA-256은 `presentation/assets/` 에 **들어 있는 파일 그대로**의 앞 16자다.
원본은 `AssetDownloads/hybrid/` 에 있다(gitignore).

## 글꼴 — 전부 OFL 1.1

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) | 가공 |
|---|---|---|---|---|---|---|
| `presentation/assets/fonts/DoHyeon-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/dohyeon | Woowahan Brothers | SIL OFL 1.1 | 2026-09-27 | `35644be7f28e0a68` | 원본 그대로 |
| `presentation/assets/fonts/GothicA1-Regular.ttf` | https://github.com/google/fonts/tree/main/ofl/gothica1 | HanYang I&C Co.,Ltd. | SIL OFL 1.1 | 2026-09-27 | `211151bea98098c5` | 원본 그대로 |
| `presentation/assets/fonts/NotoEmoji-VariableFont_wght.ttf` | https://github.com/google/fonts/tree/main/ofl/notoemoji | Google | SIL OFL 1.1 | 2026-09-27 | `de6c18832938afc9` | 원본 그대로 |

> 작물·카드·계절·적의 문양은 그림 파일이 아니라 **Noto Emoji 글리프**다
> (`presentation/palette.json` 의 `glyph` 표가 ID → 글리프 대응이다).
> 글꼴이라 CSS로 색을 입힐 수 있고, 그래서 같은 문양이 저쪽 PoC에서는 잉크로,
> 여기서는 눈빛과 등불빛으로 칠해진다.
>
> 원본을 고치지 않는다. 서브셋을 뜨면 용량이 줄지만 OFL의 Reserved Font Name 조항을
> 따로 따져야 한다 — 용량보다 그쪽이 비싸다.

## 질감 — CC0 1.0

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) | 가공 |
|---|---|---|---|---|---|---|
| `presentation/assets/img/snow-night.jpg` | https://ambientcg.com/a/Snow006 | ambientCG | CC0 1.0 | 2026-09-27 | `272f59e36d16df61` | 원본 `Snow006_1K-JPG_Color.jpg`(sha `bf29965ab1e05abf`)를 512px·JPEG q82로 줄였다 |

라이선스 확인: https://ambientcg.com/license — "All assets are released under the Creative Commons CC0 license."

## 소리 — CC0 1.0

| 파일 | 출처 (URL) | 제작자 | 라이선스 | 받은 날 | SHA-256 (앞 16자) | 가공 |
|---|---|---|---|---|---|---|
| `presentation/assets/audio/card-place.ogg` | https://kenney.nl/assets/rpg-audio | Kenney (Kenney Vleugels) | CC0 1.0 | 2026-09-27 | `ba9ba60b172b3ebc` | 팩의 `metalLatch.ogg`. 이름만 바꿨다 |
| `presentation/assets/audio/harvest.ogg` | https://kenney.nl/assets/rpg-audio | Kenney (Kenney Vleugels) | CC0 1.0 | 2026-09-27 | `d00c2b3c9fff07e3` | 팩의 `chop.ogg`. 이름만 바꿨다 |
| `presentation/assets/audio/gate-knock.ogg` | https://kenney.nl/assets/rpg-audio | Kenney (Kenney Vleugels) | CC0 1.0 | 2026-09-27 | `fd21c0e7a9d03173` | 팩의 `doorClose_4.ogg`. 이름만 바꿨다 |

라이선스 확인: kenney.nl의 에셋 페이지 표기(License: Creative Commons CC0)와
내려받은 팩 안의 `License.txt`("License (Creative Commons Zero, CC0)") 두 곳에서 봤다.

> 목업(`presentation/index.html`)은 소리를 **틀지 않는다.** 어떤 소리를 쓰기로 했는지는
> `presentation/palette.json` 의 `sound` 에 값으로 적어 두었다.
> 이 연출에서 카드 놓는 소리가 빗장(`metalLatch`)인 것은 의도다 — 저쪽은 장부에 얹는 소리다.

## 라이선스 원문 — 에셋과 함께 싣는다

**OFL 1.1은 글꼴을 재배포할 때 라이선스 원문을 같이 싣기를 요구한다.** 그래서 파일로 넣었다.
CC0는 요구하지 않지만, 받은 팩의 원문을 같이 두는 편이 나중에 되짚기 쉽다.

| 파일 | 무엇의 라이선스인가 | 출처 (URL) | SHA-256 (앞 16자) |
|---|---|---|---|
| `presentation/assets/fonts/OFL-DoHyeon.txt` | Do Hyeon (OFL 1.1) | https://github.com/google/fonts/blob/main/ofl/dohyeon/OFL.txt | `de5ff32211a4340b` |
| `presentation/assets/fonts/OFL-GothicA1.txt` | Gothic A1 (OFL 1.1) | https://github.com/google/fonts/blob/main/ofl/gothica1/OFL.txt | `ed95c33f80ccca00` |
| `presentation/assets/fonts/OFL-NotoEmoji.txt` | Noto Emoji (OFL 1.1) | https://github.com/google/fonts/blob/main/ofl/notoemoji/OFL.txt | `500bb1ccf43df7bb` |
| `presentation/assets/audio/LICENSE-Kenney-RPG-Audio.txt` | Kenney RPG Audio (CC0 1.0) | https://kenney.nl/assets/rpg-audio (팩 안의 License.txt) | `5735dfd72cb64cbb` |

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 (재배포 사이트의 표기만 믿지 않았다)
  * 글꼴: `google/fonts` 저장소의 `OFL.txt` 와 `METADATA.pb`(`license: "OFL"`)를 직접 읽었다
  * 질감: `ambientcg.com/license` 본문을 직접 읽었다
  * 소리: kenney.nl 에셋 페이지와 팩 안의 `License.txt` 를 둘 다 읽었다
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] 적지 않은 에셋은 `presentation/assets/` 에 없다 (위 표들이 그 폴더의 전량이다)
* [x] OFL 글꼴마다 라이선스 원문을 같이 실었다 — OFL 1.1 이 재배포에 요구하는 것이다

## AI로 만든 에셋

없음.

`src/` 의 로직과 `data/` 의 수치, `presentation/index.html` 의 코드는 AI로 썼다 —
**Steam 공시 대상은 출하 에셋**이므로 여기에 해당하지 않는다
(`docs/PLAN_GENRES.md §3` §8 마지막 줄).
