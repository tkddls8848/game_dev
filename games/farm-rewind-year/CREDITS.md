# CREDITS — farm-rewind-year

**이 PoC가 쓰는 에셋은 전부 CC0 · 퍼블릭 도메인 · OFL(폰트)뿐이다.**
그 밖의 라이선스는 넣지 않는다 — PoC는 버려지거나 출시로 가는데, 출시에서 되돌리려면
그 에셋을 쓴 모든 연출을 다시 만들어야 한다.

받은 날: **2026-09-27**. 원본은 `AssetDownloads/farming/`(gitignore)에 그대로 두었고,
아래 SHA-256은 **원본 파일**의 것이다(가공본이 아니라 출처를 되짚을 수 있는 값이어야 한다).

---

## 글꼴 — SIL Open Font License 1.1

라이선스 전문을 `presentation/assets/OFL-*.txt`로 같이 넣었다(OFL이 요구한다).

| 파일 | 출처 (URL) | 제작자 | 라이선스 | SHA-256 (앞 16자) |
|---|---|---|---|---|
| `presentation/assets/EBGaramond.ttf` | [google/fonts · ofl/ebgaramond](https://github.com/google/fonts/tree/main/ofl/ebgaramond) | Georg Duffner, Octavio Pardo | OFL 1.1 | `b8f3dd1fbc5bf88a` |
| `presentation/assets/SongMyung-Regular.subset.ttf` | [google/fonts · ofl/songmyung](https://github.com/google/fonts/tree/main/ofl/songmyung) | Eunyoung Yang (양은영) | OFL 1.1 | `81807a2e63afb559` |

**가공:** 둘 다 `fontTools.subset`으로 줄였다 — EB Garamond는 라틴·숫자·문장부호만
(831 KB → 255 KB, 표제와 숫자에만 쓴다), 송명체는 이 목업이 실제로 쓰는 글자만
(1,988 KB → 170 KB). OFL은 파생·재배포를 허용하고, 예약된 글꼴 이름이 없어 이름을 그대로 쓴다.

**왜 이 둘인가:** EB Garamond는 Claude Garamond의 활자를 되살린 휴머니스트 세리프로
**필사본 뒤의 첫 인쇄체** 자리에 있다. 송명체는 옛 활자 느낌의 한글 명조로 그 시대감을 맞췄다.
연감은 "베껴 쓴 것"처럼 읽혀야 하므로 기하학적인 글꼴을 쓰면 안 된다.

## 질감 — Creative Commons CC0 1.0 Universal

| 파일 | 출처 (URL) | 제작자 | 라이선스 | SHA-256 (앞 16자) |
|---|---|---|---|---|
| `presentation/assets/paper.jpg` | [ambientCG · Paper003](https://ambientcg.com/view?id=Paper003) (`Paper003_1K-JPG_Color.jpg`) | Lennart Demes (ambientCG) | CC0 1.0 | `cfae0fa9326ef76c` |

**가공:** Color 맵만 받아 512×512 회색조로 줄이고 JPEG 품질 72로 다시 저장했다(114 KB → 4 KB).
`overlay`로 옅게 겹쳐 짙은 남색 위에 **양피지 결**만 남긴다 — 종이색은 쓰지 않는다.
밤하늘·별·금박은 전부 CSS와 인라인 SVG로 그린다(아래 "코드가 만든 것").

**라이선스 확인:** [docs.ambientcg.com/license](https://docs.ambientcg.com/license/) 원 페이지에서
직접 확인했다 — "All ambientCG assets are provided under the Creative Commons CC0 1.0 Universal License."
(API v2의 `license` 필드는 비어 있어 믿을 수 없었다. 그래서 라이선스 페이지를 읽었다.)

## 소리 — Creative Commons CC0 1.0 Universal

| 파일 | 출처 (URL) | 제작자 | 라이선스 | SHA-256 (앞 16자) |
|---|---|---|---|---|
| `presentation/assets/handleSmallLeather.ogg` | [Kenney · RPG Audio](https://kenney.nl/assets/rpg-audio) | Kenney Vleugels | CC0 1.0 | `ae3cbf695aa0a8b9` |
| `presentation/assets/chop.ogg` | [Kenney · RPG Audio](https://kenney.nl/assets/rpg-audio) | Kenney Vleugels | CC0 1.0 | `d00c2b3c9fff07e3` |
| `presentation/assets/bookFlip1.ogg` | [Kenney · RPG Audio](https://kenney.nl/assets/rpg-audio) | Kenney Vleugels | CC0 1.0 | `fa81ac2fedc8c641` |
| `presentation/assets/bookClose.ogg` | [Kenney · RPG Audio](https://kenney.nl/assets/rpg-audio) | Kenney Vleugels | CC0 1.0 | `81e976532565f437` |
| `presentation/assets/handleCoins.ogg` | [Kenney · RPG Audio](https://kenney.nl/assets/rpg-audio) | Kenney Vleugels | CC0 1.0 | `8a91f969e932df70` |

쓰이는 곳: 씨앗 주머니(심기) · 수확 · 책장 넘기기(날짜) · **책 덮기(되감기)** · 동전(셈하는 날).
가공하지 않고 그대로 넣었다. 되감기에 `bookClose`를 고른 이유는 한 해를 닫는 소리여야 하기 때문이다.

**라이선스 확인:** 공식 배포 zip 안의 `License.txt`를 직접 읽었다
(`presentation/assets/License-Kenney-RPGAudio.txt`로 같이 넣었다):
"License (Creative Commons Zero, CC0) … You may use these assets in personal and commercial projects."

## 아이콘 — Creative Commons CC0 1.0 Universal

| 파일 | 출처 (URL) | 제작자 | 라이선스 | SHA-256 (앞 16자) |
|---|---|---|---|---|
| `presentation/assets/rewind.png` | [Kenney · Game Icons](https://kenney.nl/assets/game-icons) | Kenney Vleugels | CC0 1.0 | `b518b4e90b117f8c` |

라이선스 전문은 `presentation/assets/License-Kenney-GameIcons.txt`.

---

## 코드가 만든 것 (외부 에셋이 아니다)

아래는 내려받은 것이 아니라 `presentation/index.html`이 **`data/`의 수치로 그리는 것**이라
출처를 적을 대상이 아니다. 뿌리 `CLAUDE.md` 설계 원칙 2("손으로만 만들 수 있는 에셋을 만들지 않는다").

* **달의 위상 112개** — `seasons.json`의 `moonPhases`에서 조명 비율을 읽어 SVG 원호 두 개로 그린다.
  삭·초승·상현·망·하현·그믐이 한 식에서 나온다. 그림 파일이 아니므로 주기를 바꾸면 그림도 따라온다
* 밤하늘 · 별 · 비네팅 — CSS 그라디언트
* 금박 — 사선 그라디언트를 글자에 `background-clip:text`로 물린다(평면 노란색은 금박이 아니다)
* 되감기 사다리의 막대그래프 · 날씨 문양 — CSS와 문자
* 한 해의 날씨 — `.NET System.Random(seed)`를 JS로 이식해 C#과 같은 수열을 뽑는다.
  이 PoC에서는 그것이 규칙이기도 하다 — 되감은 해의 달력이 같아야 기억이 값을 갖는다

작물·계절 아이콘은 넣지 않았다. CC0/PD/OFL로 확인된 농작물 아이콘 세트를 찾지 못했고
(대표적인 game-icons.net은 CC BY 3.0으로 이 저장소의 허용 목록에 없다),
**확인하지 못한 에셋은 넣지 않는다**는 규칙이 아이콘을 넣는 것보다 중요하다.

## 확인한 것

* [x] 모든 항목의 라이선스를 **원 페이지에서 직접** 확인했다 (재배포 사이트의 표기만 믿지 않았다)
  * 글꼴: `google/fonts` 저장소의 각 `ofl/<이름>/OFL.txt`를 같이 받아 확인
  * 질감: ambientCG 공식 라이선스 문서 페이지
  * 소리·아이콘: Kenney 공식 zip 안의 `License.txt`
* [x] CC0/PD/OFL이 아닌 것은 하나도 없다
* [x] 라이선스를 확인하지 못해 **버린 것**: game-icons.net(CC BY 3.0) 작물·달·날씨 아이콘 세트
* [x] AI로 생성한 에셋은 없다

## 표의 SHA-256 은 **실린 파일**의 것이다 (2026-09-27 코디네이터 정정)

폰트는 쓰는 글자만 남겨 부분집합(subset)으로 줄였고 `paper.jpg` 는 원본
(`Paper006_1K-JPG_Color.jpg`)에서 이름과 형식을 바꿨다. 그래서 **원본 내려받은 파일과
해시가 다르다.**

처음 표에는 원본의 해시가 적혀 있었다. 그러면 표가 그 경로의 파일을 식별하지 못한다 —
해시를 적는 이유가 "이 파일이 그 파일인가"를 나중에 확인하는 것이므로, **실린 파일의
해시로 바꿨다.** 원 출처는 같은 행의 URL 이 가리킨다.

폰트의 `OFL-*.txt` 는 목업이 참조하지 않는다. **OFL 1.1 이 라이선스 전문 동봉을
요구하기 때문에** 함께 두는 것이고, 지우면 안 된다.

## AI로 만든 에셋

없음. (로직·데이터·검사기는 AI로 썼지만 출하 에셋이 아니므로 Steam 공시 대상이 아니다 —
`docs/PLAN_GENRES.md §1` §8 참고.)
