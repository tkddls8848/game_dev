> **원본 위치:** `AssetDownloads/tmp_game/assets/images/` (gitignore — 바이너리는 추적하지 않는다).
> 이 문서는 목록만 저장소에 남긴 사본이다. 아래 링크는 그 원본을 가리킨다.

# images/ — 이미지 에셋 (20개 컨셉 공용)

수집일: 2026-09-28. 총 약 0.94GB. 라이선스는 **CC0 / 퍼블릭 도메인이 대부분**이고, CC BY는 17개(출처 표기 필요, 아래 목록)뿐이다.
CC BY-SA, NC, ND, GFDL은 **하나도 넣지 않았다**.

- 사진 묶음은 폴더마다 `sources.json`에 **파일 단위 출처**를 남겼다: title, sourceUrl, downloadUrl, license(+licenseUrl), author, date, acquiredAt, sha256, width/height, usedFor(컨셉 번호), 검색어.
- 텍스처는 재질 폴더마다 `SOURCE.json`을 두었고, 전체 목록은 `textures/index.json`이다.
- `_scripts/`: 수집·정리 스크립트(재현용). `curate.py` + `extra_rm.json`에는 사람이 보고 **제외한 항목의 ID**가 기록돼 있다.
- 긴 변은 3000px 이하다(Commons는 1920/2000px 썸네일, Met는 web-large, AIC는 사용하지 못함).

| 폴더 | 수량 | 라이선스 구성 | 출처 | 용도(컨셉) |
|---|---|---|---|---|
| `photos-bw-vintage/` | 266장 | CC0 145 · PD 120 · No known restrictions 1 | Met Open Access(사진부) 136, Wikimedia Commons 130(LoC Bain/Harris & Ewing, State Library QLD, Nationaal Archief 등) | 4 사진 속 범인 / 찍히지 않은 사람, 13 유품 |
| `photos-objects/` | 461장 | CC0 459 · PD 2 | Met 275(박물관 소장품, 회색 배경), Commons CC0 186(`everyday_*`, 현대 생활용품) | 13 분실물 창고 / 유품, 17 박물관 전시물 |
| `photos-rooms-places/` | 248장 | PD 154 · CC0 87 · CC BY 6 · No known restrictions 1 | Wikimedia Commons(LoC, NARA, MOHAI, HABS 등 PD 사진 포함) | 6 엘리베이터, 10 전화 교환수, 11 눈 감은 기록, 12 폐교 방송실, 15 지하철, 16 얼음, 17 박물관, 19 침실, 20 정류장 |
| `textures/` | 44개 재질, 맵 129장 | CC0 전부 | Poly Haven 2k 25개, ambientCG 2K-JPG 19개 | 3·6·8·11·12·15·16·17·19 (3D 방·바닥·벽) |
| `hdri/` | 9개(.hdr 1k) | CC0 | Poly Haven | 야외 밤 4(moonless_golf, dikhololo_night, street_lamp, cobblestone_street_night), 실내·어두움 5(metro_noord, creepy_bathroom, small_empty_room_2, debris_basement_corridor, childrens_hospital) |
| `overlays/` | 40장 | 자체 생성 CC0 34 · Commons CC0/PD 6 | numpy/PIL 절차 생성(씨드 고정), Commons 스캔 6(TMAX100 필름 그레인 3, 오래된 종이·양피지 3) | 4·9·11·13·14·15·20 (필름 노이즈·먼지·종이·빛샘·하프톤·비네트·스캔라인) |
| `silhouettes/` | 54개(SVG + 800px PNG 미리보기) | CC0 | Openclipart(퍼블릭 도메인 헌정) | 3·8·15·20. standing 32 · walking 11 · sitting 9 · waiting 2 |
| `comics-panels/` | 페이지·스트립 135장 + 자동 추출 컷 269장 | PD 403 · CC0 1 | Wikimedia Commons (Little Nemo, Dream of the Rarebit Fiend, Krazy Kat, Mutt and Jeff, Happy Hooligan, Katzenjammer Kids, Buster Brown, 1940년대 PD 코믹북 내지) | 14 한 프레임 만화 |
| `paper-puppets-reference/` | 103장 | CC0 53 · PD 39 · CC BY 11 | Met 43(Pollock's 장난감 극장 인물·배경 시트, 스페인 그림자극 인물 시트 *ombres chinoises*, 종이 인형), Commons 60(토이 시어터, 와양 쿨릿, 중국·이집트 그림자 인형, 펀치와 주디) | 5 탁상 인형극 |

## 폴더 상세

### photos-bw-vintage
하위 폴더: `family` 90 · `portrait` 67 · `street` 52 · `group` 27 · `interior` 17 · `crime` 13.
- Met 사진 중 1880~90년대 파리 경찰 **베르티용 머그샷**(met_306xxx·307xxx)이 다수 있다. 4번(범인 지목)에 바로 쓸 수 있다.
- 세피아 톤인 원본이 많다. 흑백으로 통일하려면 채도를 빼면 된다(원본 그대로 두었다).
- LoC 스테레오 사진은 좌우 중복을 dHash로 대부분 걸렀다.
- ⚠ 실존 유명인(버펄로 빌, 벅시 시겔, 로버트 E. 리 등)이 일부 있다. 퍼블릭 도메인이지만 게임 속 '범인'으로 쓸 때는 빼는 게 안전하다.

### photos-objects
- `keys` `watches` `glasses` `toys` `tools` `bottles` `shoes` `books` `letters` `boxes` `jewelry` `clothing` `desk` `household` `misc` → Met 소장품(골동품 느낌, 회색 스튜디오 배경). 17번 전시물에 적합하다.
- `everyday_*` → Commons CC0 현대 생활용품(열쇠, 손목시계, 안경, 지갑, 우산, 카세트, 휴대폰, 라디오, 손전등, 머그, 보온병, 신발 등). 13번 소포 내용물에 적합하다.
- 배경이 제각각이다. 게임에 넣을 때 누끼(배경 제거)가 필요하다.

### photos-rooms-places
`switchboard` 37(교환대·교환수, 1900~1960년대) · `subway_train` 34 · `bus_stop` 34 · `interior` 30 · `school` 28(1910년대 교실·복도 다수) · `radio_station` 20 · `abandoned` 18 · `museum` 16 · `elevator` 13 · `corridor` 8 · `flooded` 6 · `ice` 4.

### textures
재질: concrete 3, plaster/plaster-concrete 3, tiles 5(마블·라미네이트 포함), wood 3, metal 4, brick 1, asphalt 1, cobblestone 1, snow 5, ice 3, fabric 4, carpet 3, wallpaper 2, paper 3, cardboard 2, wet_surface 1(SurfaceImperfections).
맵은 color / normal_gl(OpenGL Y+) / roughness만 남겼다. ambientCG의 Wallpaper는 opacity 맵이 함께 있다. Unity에서 쓰려면 normal의 G 채널을 뒤집는다(DirectX 방식).

### overlays
`film_grain_{fine,medium,coarse}_1024`(타일링, 중간 회색) · `film_grain_anim_00~07_512`(애니메이션 프레임) · `dust_scratches_00~03`(RGBA 투명) · `paper_{cream,aged,white}` · `light_leak_00~04`(검은 바탕, screen/add 블렌드) · `halftone_screen/gradient_p6/p10/p16` · `noise_fbm*`(타일링) · `noise_white` · `vignette` · `scanlines` · `scanned_film/`(실제 TMAX100 그레인 스캔) · `scanned_paper/`.
블렌드 모드 제안은 `sources.json`의 `suggestedBlend`에 있다.

### silhouettes
SVG가 원본이고 PNG는 Openclipart에서 받은 800px 렌더다. 서 있는 인물(경찰, 탐정, 신사, 커플, 아이, 노인, 서류가방 든 남자 등)과 걷는 인물, 앉은 뒷모습(father_and_son_sitting 2종), 신문 읽는 사람, 우산 쓴 커플 등이 있다.

### comics-panels
- 페이지 원본은 작품별 폴더(`little_nemo/`, `krazy_kat/`, `golden_age_pages/`, `newspaper_strips/` …)에 있다.
- `panels/`: 흰 거터 검출로 **자동으로 잘라낸 컷**이다. `sources.json`에 `derivedFrom`(원본 페이지)과 `crop` 좌표가 있다. 사람이 훑어보고 광고·표지 조각은 뺐지만 **2~3컷이 붙어 나온 것도 있다**(거터가 좁거나 색 배경인 페이지).
- `golden_age/`의 13장은 **표지**다(컷 추출에서 제외).
- 제외한 것: 인종차별적 캐리커처 스트립(It Happened in Birdland, Inbad the Tailor), 저작권 표기가 있는 현대 스트립(Mandrake 벵골어판), 캐릭터 로고(Felix), 신문 전면, 광고·전단.

### paper-puppets-reference
- `toy_theatre/` 38: Pollock's *Jack the Giant Killer* 인물 시트 5장과 배경 시트 10장(Met), Redington *Oliver Twist* 시트, 독일 Papiertheater.
- `shadow_puppets/` 42: 1880년대 바르셀로나 *Nueva colección de figuras para sombras chinescas* 인물 시트 13장(Met, 오려 쓰기 좋은 검은 실루엣), 와양 쿨릿, 중국 허베이 그림자 인형.
- `paper_cutouts/` 10: 19세기 종이 인형·옷 시트.
- `puppet_theatre/` 13: 펀치와 주디, 분라쿠 인형 머리.

## CC BY — 출처 표기 필요 (17개)
| 폴더/파일 | 라이선스 | 저작자 |
|---|---|---|
| paper-puppets-reference/toy_theatre/wc_51712867_* | CC BY 2.0 | Thomas Quine |
| paper-puppets-reference/shadow_puppets/wc_51712871_* | CC BY 2.0 | Thomas Quine |
| paper-puppets-reference/shadow_puppets/wc_24487709_* | CC BY 2.0 | Arian Zwegers |
| paper-puppets-reference/shadow_puppets/wc_30137081_* | CC BY 2.0 | j bizzie |
| paper-puppets-reference/shadow_puppets/wc_5887781_* | CC BY 3.0 | Karthickbala |
| paper-puppets-reference/shadow_puppets/wc_30259963~66_* (4장) | CC BY 3.0 | Sailko |
| paper-puppets-reference/shadow_puppets/wc_94895366_* | CC BY 4.0 | Nationaal Museum van Wereldculturen |
| paper-puppets-reference/shadow_puppets/wc_147357844_* | CC BY 4.0 | Sekolah Pedalangan Wayang Sasak, Koalisi Seni |
| photos-rooms-places/abandoned/wc_40584531_*, wc_40584544_* | CC BY 2.0 | Tiffany Bailey |
| photos-rooms-places/abandoned/wc_36497251_*, wc_36497474_* | CC BY 2.0 | SuSanA Secretariat |
| photos-rooms-places/abandoned/wc_4338311_* | CC BY 3.0 | Wordbuilder |
| photos-rooms-places/abandoned/wc_166191285_* | CC BY 4.0 | 9yz |

정확한 URL은 각 폴더의 `sources.json`에서 `license`가 `CC BY`로 시작하는 항목을 보면 된다.

## 부족한 것
- **Art Institute of Chicago**: API 검색은 되지만 IIIF 이미지 서버(artic.edu)가 Cloudflare 봇 차단(403)을 걸어 받지 못했다. 우회하지 않았다.
- **Library of Congress(loc.gov)**: JSON API가 Cloudflare 챌린지를 걸어 직접 쓰지 못했다. 대신 Commons에 올라 있는 LoC PD 사진(Bain, Harris & Ewing, NYWT 등)을 썼다.
- **Smithsonian**(API 키 필요)과 **Rijksmuseum**(Linked Art 다단계 조회)은 시도하지 않았다.
- **실루엣**: *잠자는·누운 인물 0개*, *정류장에서 기다리는 사람 2개*, *뒷모습 전용은 적다*. 8·15·20번에 필요한 "직업이 읽히는 뒷모습" 50종은 부족하다. PD 사진에서 인물을 따서 만들거나 직접 그려야 한다.
- **물(water) 텍스처**: Poly Haven과 ambientCG 모두 CC0 물 표면 재질이 없다. 셰이더로 만들어야 한다(wet_surface만 있다).
- **만화 컷**: 자동 컷 269장 중 일부는 여러 컷이 붙어 있거나 여백이 불균일하다. 14번의 "한 컷에 말풍선만 옮기는" 용도에는 **말풍선을 지운 깨끗한 컷이 따로 필요**하다(현재는 말풍선·글씨가 포함된 원본).
- **흑백 사진 중 '범죄 현장 같은 실내'**: 인테리어 17장, crime 13장뿐이다. 4번 100건을 채우려면 합성 편집이 필요하다.
- **분실물 소품**: 배경이 통일돼 있지 않아 누끼 작업이 필요하다. 소포 상자(`everyday_parcels`)는 4장뿐이다.
- **HDRI**: "지하철 차량 안"이나 "엘리베이터 안" 같은 좁은 실내 CC0 HDRI는 없다. metro_noord(역사)와 small_empty_room_2가 가장 가깝다.
- 사람이 전수 검수하지는 않았다. 컨택트 시트로 훑어 명백한 이탈(그림, 로고, 현대 뉴스 사진, 민감한 사진)만 뺐으므로 개별 부적합 항목이 남아 있을 수 있다.
