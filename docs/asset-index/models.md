> **원본 위치:** `AssetDownloads/tmp_game/assets/models/` (gitignore — 바이너리는 추적하지 않는다).
> 이 문서는 목록만 저장소에 남긴 사본이다. 아래 링크는 그 원본을 가리킨다.

# models/ — 3D 모델·캐릭터·이펙트 텍스처

수집일 2026-09-28. **전부 CC0 1.0**(크레딧 불필요). 합계 약 2.6GB.
모든 팩/모델 폴더에 `SOURCE.json`(name, sourceUrl, downloadUrl, license, licenseFile, author, acquiredAt, sha256, usedFor)이 있다.
원본 zip은 20MB 미만일 때만 보관했다. `usedFor`의 숫자는 `../../plam.md`의 컨셉 번호.

| 폴더 | 출처 | 팩/모델 수 | 용량 | 포맷 |
|---|---|---|---|---|
| `kenney/` | kenney.nl 3D·이펙트 | 38팩 | 622MB | 팩마다 GLB·FBX·OBJ(+일부 DAE) 동시 제공, 컬러맵 PNG |
| `quaternius/` | quaternius.com (Google Drive 공개 폴더 + itch.io 무료 티어) | 23팩 | 685MB | FBX 또는 glTF (팩당 한 포맷만 받음, 메가킷은 FBX+glTF) |
| `polyhaven/` | api.polyhaven.com | 392모델 | 810MB | glTF 2.0 + 1k 텍스처 (PBR, 실사 스케일 mm) |
| `characters-animated/` | Kenney + Quaternius | 12팩 | 541MB | FBX/glTF/GLB, 리깅+애니메이션 |

## kenney/ (38)

| slug | 모델 수(GLB) | 컨셉 |
|---|---|---|
| furniture-kit | 140 | 3, 6, 8, 11, 12, 17, 19 — 방 내부 전반 |
| building-kit / modular-buildings | 79 / 108 | 3, 6, 8, 12, 17, 19, 20 — 벽·문·창·계단으로 방 조립 |
| modular-dungeon-kit / mini-dungeon | 39 / 30 | 3, 6, 11 — 어두운 통로·문 |
| graveyard-kit | 91 | 3, 6 — 공포 소품(관·등·울타리) |
| train-kit | 103 | 15 — 열차·선로 (외형 위주) |
| retro-urban-kit | 124 | 12, 15, 20 — 도시 외벽·표지 |
| city-kit-roads / -commercial / -suburban / -industrial, 3d-road-tiles, car-kit | 40~302 | 20 버스 정류장 주변, 13 창고, 17 건물 외관 |
| mini-market / mini-arcade | 20 / 20 | 13, 17 — 진열대·기계 |
| factory-kit | 143 | 13 — 컨베이어·상자·선반 |
| food-kit | 200 | 13, 19 — 소품 |
| holiday-kit / toy-car-kit / cube-pets / fantasy-town-kit / castle-kit | 24~167 | 5 — 인형극 무대·장난감 소품 |
| mini-characters / blocky-characters | 26 / 18 | 5, 15, 20 — 단순 인물 (실루엣 대용) |
| survival-kit / platformer-kit / nature-kit | 80 / 153 / 329 | 16 (눈·얼음 평원: platformer-kit의 snow 타일, nature-kit), 3, 8, 20 |
| watercraft-kit / pirate-kit | 46 / 72 | 8 — 수중·보물 |
| space-kit / space-station-kit / modular-space-kit | 40~153 | 6 — 금속 복도·문(엘리베이터 대용) |
| prototype-kit | 145 | 전 컨셉 그레이박스 |
| particle-pack / smoke-particles | PNG 193 / 79 | 3, 6, 16 — 먼지·연기·불꽃·파편 |
| light-masks | PNG 457 | 3, 11, 19 — 손전등 원뿔·쿠키 |
| skyboxes | PNG 8 | 15, 16, 20 |

## quaternius/ (23)

| slug | 파일 | 포맷 | 컨셉 |
|---|---|---|---|
| ultimatehomeinterior | FBX 123 | FBX | 3, 8, 11, 17, 19 — 집 내부(가구·가전) |
| ultimatefurniture / furniture | FBX 20 / 23 | FBX | 3, 11, 17, 19 |
| modulartrain | FBX 14 | FBX | 15 — 객차(외형) |
| publictransport | FBX 12 | FBX | 15, 20 — 버스·지하철·트램 |
| modularstreets / simplebuildings / downtown-city-megakit | 25 / 10 / 153 | FBX (+glTF, 메가킷 PBR 텍스처) | 20 — 거리·정류장 주변 |
| backgroundposedhumans | FBX 28 | FBX (포즈 고정 인물) | 15, 20 — **승객·정류장 실루엣용** |
| zombieapocalypsekit | glTF 64 | glTF | 3, 6 — 폐허·공포 소품 |
| fantasy-props-megakit | 94 | FBX+glTF, PBR | 17 (전시물), 5, 13 |
| sci-fi-essentials-kit / ultimatemodularscifi / cyberpunkgamekit | 37 / 91 / 71 | FBX(+glTF) | 6 (금속 문·복도), 1 |
| cutefish / animatedfish | 52 / 7 | FBX | 8 |
| ultimateanimatedanimals / farmanimal | 12 / 7 | glTF/FBX (애니) | 17, 5 |
| ultimatemonsters / cutemonsters | 50 / 21 | glTF | 15 (창문 속 괴물), 3, 5 |
| survival | 53 | FBX | 16, 3 |
| junkfood / ultimatefood | 16 / 103 | FBX | 13 |

메가킷 3종(itch)은 **무료 Standard 티어만** 받았다. 중복 포맷(OBJ, Unreal용 FBX/노멀)은 삭제하고 `SOURCE.json`의 `trimmed`에 기록했다.

## polyhaven/ (392)

카테고리 props·furniture·decorative·tools·containers·seating·electronics·lighting·table·appliances·food·shelves·dishes·office·vases·wall decoration·instrument·bed·books·rigged 전부. 각 폴더에 `<id>_1k.gltf` + `.bin` + `textures/`.
`usedFor`는 이름·태그 키워드로 자동 분류했다(근사치).

컨셉별 대표 모델:
- **2·12·18 라디오/방송/녹음**: vintage_radio_transceiver, boombox, cassette_player, portable_cassette_player, Megaphone_01, Television_01, television_02
- **10 교환수**: korean_public_payphone_01, vintage_telephone_wall_clock, power_box_01
- **3·11·19 어두운 방**: vintage_flashlight, signal_flashlight, Lantern_01, vintage_oil_lamp, desk_lamp_arm_01, GothicBed_01, old_bed_frame, vintage_day_bed, alarm_clock_01, wall_clock, vintage_grandfather_clock_01
- **4 사진관**: Camera_01, vintage_video_camera, 각종 picture_frame, magnifying_glass_01
- **13 소포**: cardboard_box_01, wooden_crate_01/02, plastic_crate_01~03, vintage_suitcase, medical_box, metal_toolbox
- **17 박물관 전시물**: marble_bust_01, gothic_statue, horse_statue_01, bronze_*_statue, 각종 vase, chess_set, mantel_clock_01, brass_candleholders
- **9 편지**: binder_notebook, book_encyclopedia_set_01, wooden_bookshelf_worn
- **20 정류장**: painted_wooden_bench, street_lamp_01/02

## characters-animated/ (12)

| slug | 내용 | 애니메이션 | 포맷 |
|---|---|---|---|
| universal-animation-library (Standard) | 마네킹 + 휴머노이드 애니 43개 | Walk_Loop, Walk_Formal_Loop, Idle_Loop, Idle_Talking_Loop, **Idle_Torch_Loop**(3), Sitting_Enter/Idle/Talking/Exit(20·12), Crouch, Swim(8), Interact, PickUp_Table(13, 19), Death 등. 루트모션판(_RM) 별도 | FBX(Unity), GLB |
| universal-animation-library-2 (Standard) | 남녀 마네킹 + 추가 애니 43개 | **Idle_Rail_Loop**(15 손잡이), **Idle_TalkingPhone_Loop**(10), **Idle_Lantern_Loop**(3), LayToIdle(19), Idle_FoldArms_Loop(20), Walk_Carry_Loop(13), Chest_Open, Zombie_Walk 등 | FBX, GLB |
| universal-base-characters (Standard) | 기본 인체(남/여, 체형) + 헤어 | 위 라이브러리와 같은 리그 | glTF, FBX |
| ultimatemodularcharacters / ultimatemodularwomen | 모듈식 인물(의상 교체) | 리깅+기본 애니 | glTF, FBX |
| animatedmen / animatedwomen / ultimatedanimatedcharacter | 일상복 인물 | idle·walk·run 등 | FBX / glTF |
| animatedzombie | 좀비 1종 | 걷기·공격 | FBX |
| animated-characters-protagonists / -retro / -survivors (Kenney) | 공용 리그 1개 + 스킨 PNG 4~5장 | idle·jump·run만 | FBX |

## 수집 도구 (`../_tools/`)

- `collect_kenney.py <대상> slug=컨셉,...` — kit/tools/asset_library.py `kenney()` 이식. 페이지 CC0 링크·동봉 License.txt CC0 확인, zip 경로 안전 검사
- `collect_quaternius.py <대상> pack=컨셉,...` — 팩 페이지 CC0 확인 → 공개 Drive 폴더 목록(gdown, venv 필요) → `drive.usercontent.google.com` 직접 다운로드. 포맷 하나만(glTF>FBX>OBJ)
- `collect_itch_free.py <대상> url=slug=컨셉` — itch.io 공개 페이지에서 **$0 업로드만** (계정 없음)
- `collect_polyhaven.py <대상>` — 공식 API, md5 검증

## 부족한 것

- **엘리베이터(6)**: 전용 엘리베이터 칸·문·층 버튼 모델 없음. space-station/sci-fi 문 + building-kit 벽으로 조립해야 한다. 층 표시기·버튼 패널은 제작 필요.
- **지하철 내부(15)**: modulartrain·publictransport·train-kit는 외형 위주 저폴리라 **객차 내부(좌석 열·손잡이·창문 반사면)가 없다**. 내부는 prototype/building-kit + polyhaven 좌석으로 조립 또는 제작.
- **교환대(10)**: 전화 교환대(잭 패널·코드) 모델 없음 — 제작 필요.
- **방송실·스튜디오(2, 12)**: 마이크 스탠드·콘솔·믹서 전용 모델 없음 (radio_transceiver, boombox, megaphone만).
- **인형극(5)**: 종이 인형·꼭두각시·무대 커튼 전용 모델 없음. castle/holiday/toy 소품 + 평면 스프라이트로 대체 필요.
- **얼음·균열(16)**: 얼음판·균열 데칼 전용 에셋 없음 (platformer-kit 눈 타일, nature-kit 정도). 균열은 셰이더/데칼 제작 필요.
- **수중 집(8)**: 물에 잠긴 실내 전용 없음 — 가구 + 굴절 셰이더 조합.
- **걷기·앉기 애니**: Kenney 캐릭터는 idle/jump/run만. 걷기·앉기는 Quaternius Universal Animation Library(같은 리그의 universal-base-characters)에 있다. **누워 잠든 채 숨쉬는 루프(19)는 없다**(LayToIdle 일어나기만). 인형 떨림(5)용 손 애니도 없음 — 절차적으로 만든다.
- **실사 인물 실루엣(20: 50종)**: 포즈 고정 인물 28(backgroundposedhumans) + 모듈 캐릭터 조합이 전부. 50종은 의상·체형 조합으로 만들어야 한다.
- poly.pizza는 Cloudflare 차단(403)으로, Smithsonian 3D는 요청 거부로 받지 못했다. Mixamo(로그인)는 제외.
- Poly Haven `decorative_book_set_01`은 glTF가 없어(blend/fbx만) 빠졌다.
- Quaternius 팩 다수(ultimatemodularcharacters 등)는 하위 폴더 구조상 FBX·glTF가 함께 들어왔고, cyberpunkgamekit은 OBJ도 섞여 있다(중복 포맷).
