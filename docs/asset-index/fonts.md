> **원본 위치:** `AssetDownloads/tmp_game/assets/fonts/` (gitignore — 바이너리는 추적하지 않는다).
> 이 문서는 목록만 저장소에 남긴 사본이다. 아래 링크는 그 원본을 가리킨다.

# 폰트 (fonts/)

모든 폰트는 원 배포처에서 받은 파일 그대로이며, 폴더마다 `SOURCE.json`(출처·라이선스·SHA-256·용도)과 라이선스 원문 파일이 있다.
Google Fonts 계열은 github.com/google/fonts 저장소에서 가져왔다(`SOURCE.json`의 commit 참조). 가변 폰트(`[wght]` 등)는 Unity TextMeshPro에서 기본 인스턴스만 쓰이므로 필요하면 정적 인스턴스로 잘라 쓴다.

## 라이선스 주의
- **OFL-1.1** (대부분): 게임에 번들·임베드 가능. 폰트 파일 자체를 단독 판매 금지, 수정 시 예약 글꼴명 변경.
- **Apache-2.0**: Homemade Apple, Special Elite. 라이선스 사본 동봉.
- **CC BY-SA 4.0**: `terminal-mono/oldschool-pc-fonts` (int10h.org, VileR). 크레딧에 **"The Ultimate Oldschool PC Font Pack by VileR (int10h.org), CC BY-SA 4.0"** 표기 필수. 폰트 파일을 수정해 배포하면 그 폰트는 같은 라이선스로 공개해야 한다(게임 자체에는 전염되지 않음).

## terminal-mono/ — 터미널·CRT·비트맵 모노스페이스 (1 터미널 유령, 2 라디오 다이얼)

| 폴더 | 이름 | 라이선스 | 제작 | 용도(컨셉 번호) |
|---|---|---|---|---|
| `d2coding` | D2Coding 1.3.3 | OFL-1.1 | NAVER Corp. | 1, 2 |
| `departure-mono` | Departure Mono 1.500 | OFL-1.1 | Helena Zhang | 1, 2 |
| `firacode` | Fira Code | OFL-1.1 | The Mozilla Foundation, Telefonica S.A., Nikita Prokopov | 1, 2 |
| `galmuri` | Galmuri 2.40.4 | OFL-1.1 | Lee Minseo (quiple) | 1, 2, 6, 15 |
| `ibmplexmono` | IBM Plex Mono | OFL-1.1 | Mike Abbink, Bold Monday | 1, 2 |
| `jetbrainsmono` | JetBrains Mono | OFL-1.1 | JetBrains, Philipp Nurullin, Konstantin Bulenkov | 1, 2 |
| `majormonodisplay` | Major Mono Display | OFL-1.1 | Emre Parlak | 1, 2 |
| `monaspace` | Monaspace 1.400 (variable) | OFL-1.1 | GitHub Next | 1 |
| `nanumgothiccoding` | Nanum Gothic Coding | OFL-1.1 | Sandoll Communication | 1, 2 |
| `neodgm` | NeoDunggeunmo 1.601 | OFL-1.1 | Eunbin Jeong (Dalgona.) | 1, 2 |
| `oldschool-pc-fonts` | The Ultimate Oldschool PC Font Pack v2.2 | CC-BY-SA-4.0 | VileR (int10h.org) | 1, 2, 6, 10 |
| `pressstart2p` | Press Start 2P | OFL-1.1 | CodeMan38 | 1, 2 |
| `sharetechmono` | Share Tech Mono | OFL-1.1 | Carrois Apostrophe | 1, 2 |
| `silkscreen` | Silkscreen | OFL-1.1 | Jason Kottke | 1, 2 |
| `spacemono` | Space Mono | OFL-1.1 | Colophon Foundry | 1, 2 |
| `vt323` | VT323 | OFL-1.1 | Peter Hull | 1, 2 |

## handwriting/ — 손글씨 영문·한글 (9 답장 없는 편지)

| 폴더 | 이름 | 라이선스 | 제작 | 용도(컨셉 번호) |
|---|---|---|---|---|
| `caveat` | Caveat | OFL-1.1 | Impallari Type | 9 |
| `cedarvillecursive` | Cedarville Cursive | OFL-1.1 | Kimberly Geswein | 9 |
| `cutefont` | Cute Font | OFL-1.1 | TypoDesign Lab. Inc | 9 |
| `dawningofanewday` | Dawning of a New Day | OFL-1.1 | Kimberly Geswein | 9 |
| `dokdo` | Dokdo | OFL-1.1 | FONTRIX | 9 |
| `eastseadokdo` | East Sea Dokdo | OFL-1.1 | YoonDesign Inc | 9 |
| `gaegu` | Gaegu | OFL-1.1 | JIKJI SOFT | 9 |
| `gamjaflower` | Gamja Flower | OFL-1.1 | YoonDesign Inc | 9 |
| `himelody` | Hi Melody | OFL-1.1 | YoonDesign Inc | 9 |
| `homemadeapple` | Homemade Apple | Apache-2.0 | Font Diner | 9 |
| `kiranghaerang` | Kirang Haerang | OFL-1.1 | Woowahan Brothers | 9 |
| `kristi` | Kristi | OFL-1.1 | Birgit Pulk | 9 |
| `labelleaurore` | La Belle Aurore | OFL-1.1 | Kimberly Geswein | 9 |
| `mrssaintdelafield` | Mrs Saint Delafield | OFL-1.1 | Sudtipos | 9 |
| `nanumbrushscript` | Nanum Brush Script | OFL-1.1 | Sandoll Communication | 9 |
| `nanumpenscript` | Nanum Pen Script | OFL-1.1 | Sandoll Communication | 9 |
| `nothingyoucoulddo` | Nothing You Could Do | OFL-1.1 | Kimberly Geswein | 9 |
| `poorstory` | Poor Story | OFL-1.1 | Yoon Design | 9 |
| `reeniebeanie` | Reenie Beanie | OFL-1.1 | James Grieshaber | 9 |
| `singleday` | Single Day | OFL-1.1 | DXKorea Inc | 9 |
| `songmyung` | Song Myung | OFL-1.1 | JIKJI | 9, 4 |
| `stylish` | Stylish | OFL-1.1 | AsiaSoft Inc | 9 |
| `yeonsung` | Yeon Sung | OFL-1.1 | Woowahan brothers | 9 |

## typewriter-serif/ — 타자기·시대감 세리프 (4 사진관, 9 편지, 13 소포 라벨, 17 박물관)

| 폴더 | 이름 | 라이선스 | 제작 | 용도(컨셉 번호) |
|---|---|---|---|---|
| `courierprime` | Courier Prime | OFL-1.1 | Alan Dague-Greene | 4, 9, 13, 17 |
| `cutivemono` | Cutive Mono | OFL-1.1 | Vernon Adams | 4, 9, 13, 17 |
| `ebgaramond` | EB Garamond | OFL-1.1 | Georg Duffner, Octavio Pardo | 4, 9, 13, 17 |
| `gowunbatang` | Gowun Batang | OFL-1.1 | Yanghee Ryu | 4, 9, 13, 17 |
| `hahmlet` | Hahmlet | OFL-1.1 | Hypertype | 4, 9, 13, 17 |
| `imfelldoublepica` | IM Fell Double Pica | OFL-1.1 | Igino Marini | 4, 9, 13, 17 |
| `imfelldwpica` | IM Fell DW Pica | OFL-1.1 | Igino Marini | 4, 9, 13, 17 |
| `imfellenglish` | IM Fell English | OFL-1.1 | Igino Marini | 4, 9, 13, 17 |
| `imfellenglishsc` | IM Fell English SC | OFL-1.1 | Igino Marini | 4, 9, 13, 17 |
| `imfellfrenchcanon` | IM Fell French Canon | OFL-1.1 | Igino Marini | 4, 9, 13, 17 |
| `imfellgreatprimer` | IM Fell Great Primer | OFL-1.1 | Igino Marini | 4, 9, 13, 17 |
| `librebaskerville` | Libre Baskerville | OFL-1.1 | Impallari Type | 4, 9, 13, 17 |
| `nanummyeongjo` | Nanum Myeongjo | OFL-1.1 | Sandoll Communication | 4, 9, 13, 17 |
| `notoserifkr` | Noto Serif KR | OFL-1.1 | Google | 4, 9, 13, 17 |
| `specialelite` | Special Elite | Apache-2.0 | Astigmatic | 4, 9, 13, 17 |

## ui-sans/ — UI 산세리프 (전 컨셉 공통)

| 폴더 | 이름 | 라이선스 | 제작 | 용도(컨셉 번호) |
|---|---|---|---|---|
| `gowundodum` | Gowun Dodum | OFL-1.1 | Yanghee Ryu | 전체 |
| `ibmplexsanskr` | IBM Plex Sans KR | OFL-1.1 | Mike Abbink, Bold Monday | 전체 |
| `inter` | Inter | OFL-1.1 | Rasmus Andersson | 전체 |
| `nanumgothic` | Nanum Gothic | OFL-1.1 | Sandoll Communication | 전체 |
| `notosanskr` | Noto Sans KR | OFL-1.1 | Google | 전체 |
| `orbit` | Orbit | OFL-1.1 | Sooun Cho, JAMO | 전체 |
| `pretendard` | Pretendard 1.3.9 | OFL-1.1 | Kil Hyung-jin (orioncactus) | 전체 |
| `sunflower` | Sunflower | OFL-1.1 | JIKJISOFT | 전체 |

## comic/ — 만화 말풍선 (14 한 프레임 만화)

| 폴더 | 이름 | 라이선스 | 제작 | 용도(컨셉 번호) |
|---|---|---|---|---|
| `bangers` | Bangers | OFL-1.1 | Vernon Adams | 14 |
| `blackhansans` | Black Han Sans | OFL-1.1 | Zess Type | 14 |
| `comicneue` | Comic Neue | OFL-1.1 | Craig Rozynski, Hrant Papazian | 14 |
| `dohyeon` | Do Hyeon | OFL-1.1 | Woowahan Brothers | 14 |
| `dongle` | Dongle | OFL-1.1 | Yanghee Ryu | 14 |
| `gugi` | Gugi | OFL-1.1 | TAE System & Typefaces Co. | 14 |
| `jua` | Jua | OFL-1.1 | Woowahan Brothers | 14 |

## signage/ — 표지판·세그먼트 디스플레이 (6 엘리베이터 층 표시, 10 교환대, 15 지하철)

| 폴더 | 이름 | 라이선스 | 제작 | 용도(컨셉 번호) |
|---|---|---|---|---|
| `b612` | B612 | OFL-1.1 | Nicolas Chauveau, Thomas Paillot, Jonathan Favre-Lamarine, Jean-Luc Vinot | 6, 10, 15 |
| `b612mono` | B612 Mono | OFL-1.1 | Nicolas Chauveau, Thomas Paillot, Jonathan Favre-Lamarine, Jean-Luc Vinot | 6, 10, 15 |
| `dseg` | DSEG 0.46 (7/14-segment) | OFL-1.1 | keshikan | 2, 6, 10, 15, 18 |
| `orbitron` | Orbitron | OFL-1.1 | Matt McInerney | 6, 10, 15 |
| `oswald` | Oswald | OFL-1.1 | Vernon Adams, Kalapi Gajjar, Cyreal | 6, 10, 15 |
| `overpass` | Overpass | OFL-1.1 | Delve Withrington, Dave Bailey, Thomas Jockin | 6, 10, 15 |
| `overpassmono` | Overpass Mono | OFL-1.1 | Delve Withrington, Dave Bailey, Thomas Jockin | 6, 10, 15 |

## 받지 못했거나 뺀 것
- **Komika 계열**: 명확한 자유 라이선스 배포처를 확인할 수 없어 제외. 대신 Bangers(OFL)·Comic Neue(OFL)와 한글 Jua·Do Hyeon·Black Han Sans·Dongle·Gugi를 넣었다.
- **Pretendard / D2Coding / Galmuri / int10h 원본 zip**: 20MB 이상이거나 불필요한 웹폰트·BDF가 많아 zip은 보관하지 않고 데스크톱용 TTF/OTF만 풀었다(해시는 원본 zip 기준).
- int10h 팩은 `ttf - Px`(픽셀 윤곽)와 `ttf - Ac`(종횡비 보정)만 풀었다. `Mx`(비트맵 혼합)·`otb`(리눅스 비트맵)는 Unity에서 쓸 일이 없어 생략.
- Monaspace는 가변 TTF만(정적·Nerd Font 판 생략).

총 76개 패밀리 폴더, 용량 321M.
