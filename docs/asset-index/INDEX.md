# tmp_game 에셋 인덱스 — 컨셉별

> 수집 2026-09-28. 대상 계획: [`../CONCEPTS.md`](../CONCEPTS.md#컨셉-71) (미착수 40안 = 20주제 × 상업·예술).
> 전체 7.0 GB · **파일 95,372개** · 출처 기록(`SOURCE.json`·`sources.json`) 1,019개.
>
> **실물은 `AssetDownloads/tmp_game/`(gitignore)에만 있다.** 원격 수집 폴더
> (`orca:/home/ubuntu/orca/workspaces/game/tmp_game`)는 2026-09-29에 삭제했다 — 지우기 전에
> 경로·크기를 전수 대조하고(불일치 0), 20MB 넘는 파일 25개를 포함한 표본 325개(796MB)의
> SHA-256 을 양쪽에서 재어 일치를 확인했다. **이제 사본은 로컬 하나뿐이다.**
>
> 그때 대소문자 충돌 한 쌍을 발견했다 — `interface-sounds/` 에 내용이 다른 `License.txt`(Kenney 원본)와
> `LICENSE.txt`(OpenGameArt 메타데이터)가 함께 있었고 **Windows 는 둘을 같은 파일로 본다.**
> 뒤엣것을 `LICENSE.opengameart.txt` 로 이름을 갈라 받아 둘 다 살렸다
> (경위는 그 폴더의 `CASE_COLLISION.md`).
> 이 문서는 **사람이 고른 요약**이다. `usedFor` 기준 전체 목록은 [`CONCEPT_PACKS.md`](CONCEPT_PACKS.md)
> (`python3 _tools/build_concept_index.py`로 재생성, 기계용은 `concept_index.json`).

## 폴더

| 폴더 | 크기 | 내용 | 안내 |
|---|---|---|---|
| `fonts/` | 321 MB | 76 패밀리 — 터미널·픽셀·한글 모노, 손글씨(영9·한14), 타자기·세리프, UI, 만화, 7세그먼트 | [README](fonts.md) |
| `shaders/` | 46 MB | 105종 — CRT, 수중 굴절·커스틱, 반사, 필름그레인·VHS, 손전등·어둠, 얼음·균열 | [README](shaders.md) |
| `audio/` | 2.8 GB | 276팩·5,069파일 — Kenney 전체, 앰비언스, 폴리, 타건, 라디오 노이즈, 발소리, 숨, 관객, 얼음, 드론, PD 음성 6시간, BGM | [README](audio.md) |
| `images/` | 936 MB | 흑백 PD 사진 266, 사물 461, 장소 248, PBR 재질 44, HDRI 9, 오버레이 40, 실루엣 54, 만화 404, 종이인형 참고 103 | [README](images.md) |
| `models/` | 2.6 GB | Kenney 38팩, Quaternius 23팩, Poly Haven 392개, 애니메이션 캐릭터 12팩 | [README](models.md) |
| `icons/` | 347 MB | game-icons.net 4,239, Kenney 2D 40팩, Lucide·Tabler·Phosphor·OpenMoji·Twemoji | [README](icons.md) |
| `kit/tools/asset-collect/` | — | 수집 스크립트(Kenney·Quaternius·itch·Poly Haven), 인덱스 생성기 | |

## 라이선스 — 쓰기 전에 확인

대부분 **CC0 / 퍼블릭 도메인 / OFL**. 아래는 **표기 의무**가 있다. 게임에 넣을 때 해당 폴더의 `SOURCE.json`의 `attribution`을 `CREDITS.md`로 옮긴다.

- **CC BY**: game-icons.net(작가별, `icons/game-icons/ATTRIBUTION.txt`), Twemoji, 오디오 82팩(Kevin MacLeod 26곡 포함), 이미지 17장, int10h 픽셀폰트(CC BY-SA)
- **CC BY-SA** (파생물도 같은 조건): 오디오 18팩, OpenMoji, int10h 폰트
- **셰이더 BSD-3 / BSL-1.0 / CC BY**: `crt/Unity_CRTEffect`, `flashlight-darkness/VolumetricLights`, `crt/Cathode-Retro`, `film-grain.slang`
- **빌드 반입 금지**: `shaders/crt/gpl-reference/` (GPL, 참고용)
- **실존 인물**: 일부 PD 사진에 유명인이 찍혀 있다(images README 표시). 범인·용의자 역할로 쓰지 말 것

## 엔진 적합성

셰이더 대부분은 **Godot·URP·libretro GLSL**이다. Unity 게임은 내장 파이프라인을 쓰므로 이식이 필요하다.
바로 쓰기 가까운 것: `Simple-CRT-Shader`, `Unity_CRTEffect`, `KinoGlitch/Fringe/Bloom`, `AdamPlaneReflection`, `VolumetricLights`, `unity-frosted-glass`.
**어떤 셰이더도 Unity에서 컴파일·실행해 보지 않았다.** 모델은 GLB·FBX·glTF 혼재 — 같은 모델을 여러 형식으로 동시에 임포트하지 말 것.

---

## 컨셉별

표기: ✅ 확보 · ⚠️ 부분 확보/가공 필요 · ❌ 없음(제작·생성 필요)

### 1. 터미널 유령 — 해킹 탈출 / 삭제 직전 로그
| 필요 | 상태 | 위치 |
|---|---|---|
| 터미널 폰트 | ✅ | `fonts/terminal-mono/` — VT323, IBM Plex Mono, Px437(oldschool-pc-fonts), **한글: D2Coding·Galmuri·NeoDunggeunmo** |
| CRT 셰이더 | ✅ | `shaders/crt/` (Unity: Simple-CRT-Shader, Unity_CRTEffect · Godot/GLSL: crt-lottes) · `film-grain-vhs/KinoGlitch` |
| 타건음 | ✅ | `audio/sfx-ui-typing/` (mechanical-keyboard, keyboard-soundpack-1, single-key-press) · `kenney-audio/interface-sounds` |
| 컴퓨터 비프·글리치 | ✅ | `audio/sfx-ui-typing/9-sci-fi-computer-sounds-and-beeps`, `glitch-music` · `kenney-audio/digital-audio` |
| 서버실 험 | ✅ | `audio/ambience/ambient-spaceship-hums`, `force-field-electric-hum`, `fridge-loop-1` |
| 스테이지 30개 | ❌ | 텍스트 — 직접 작성 |

### 2. 라디오 수색 — 주파수 사냥 / 죽은 방송국
| 필요 | 상태 | 위치 |
|---|---|---|
| 노이즈·튜닝 | ✅ | `audio/radio-static-noise/` (frequency-static 404 MB, static, mysterious-radio-signal, commons-radio) |
| 무전 교신 질감 | ✅ | `audio/voice-publicdomain/Apollo11Audio`, `Apollo13Audio` · `radio-static-noise/radio-call` |
| 저음질 옛 녹음 | ✅ | `audio/voice-publicdomain/EDIS-*` (1925년 이전 에디슨), LibriVox 5종 — **영어, 질감·필터 시험용** |
| 한국어 음성 60줄 / 40분 | ❌ | `kit/tools/tts_generate.py`로 생성 + 대역 필터 |
| 다이얼 UI·모델 | ⚠️ | `models/polyhaven/vintage_radio_transceiver`, `boombox` · `fonts/signage/dseg` (주파수 표시) |

### 3. 손전등 하나 — 배터리 서바이벌 / 빛이 닿지 않는 것
| 필요 | 상태 | 위치 |
|---|---|---|
| 저폴리 방 6개 | ✅ | `models/quaternius/ultimatehomeinterior`, `ultimatefurniture` · `models/kenney/furniture-kit`, `building-kit`, `modular-dungeon-kit` |
| 손전등·랜턴 | ✅ | `models/polyhaven/vintage_flashlight`, `signal_flashlight`, `Lantern_01`, `vintage_oil_lamp` |
| 빛·어둠 셰이더 | ✅ | `shaders/flashlight-darkness/` (VolumetricLights, 콘 마스크) · `models/kenney/light-masks` |
| 발소리 5종 | ✅ | `audio/footsteps/` (footsteps-on-different-surfaces, wood-stone-leaves-gravel-and-mud, stone-stair-steps) |
| 숨소리 | ⚠️ | `audio/breathing-heartbeat/` (breathing-tired, ghost-breath) — 가까운 숨 전용은 부족 |
| 실루엣 하나 | ⚠️ | `images/silhouettes/standing` · `models/quaternius/backgroundposedhumans` |
| 재질·HDRI | ✅ | `images/textures/` (wallpaper·wood·concrete) · `images/hdri/` (어두운 실내 5) |

### 4. 박제된 사진관 — 사진 속 범인 / 찍히지 않은 사람
| 필요 | 상태 | 위치 |
|---|---|---|
| PD 흑백 사진 | ✅ | `images/photos-bw-vintage/` — family 90, portrait 67, street 52, group 27, interior 17, **crime 13 + Bertillon 머그샷 약 40** |
| 필름 노이즈 | ✅ | `images/overlays/` (그레인 정지·8프레임 애니, 먼지·스크래치, 빛번짐) · `shaders/film-grain-vhs/` |
| 사진관 소품 | ✅ | `models/polyhaven/Camera_01`, `fancy_picture_frame_*`, `standing_picture_frame_*`, `filmstrip_projector_8mm`, `projector_screen` |
| 셔터·영사기 소리 | ⚠️ | `audio/foley-props/camera`, `camerashudder`, `commons-projector-camera-tape` — 영사기 모터 루프는 부족 |
| 확대 비교용 합성 | ❌ | 100건 사건 사진 합성은 직접 제작 |
| 폰트 | ✅ | `fonts/typewriter-serif/` (Special Elite, IM Fell) |

### 5. 탁상 인형극 — 인형극 대결 / 마지막 공연
| 필요 | 상태 | 위치 |
|---|---|---|
| 종이 인형 30 | ⚠️ | `images/paper-puppets-reference/` — toy_theatre(Pollock 캐릭터·장면 시트), shadow_puppets, paper_cutouts. **오려 내기 필요** |
| 소품 40 | ⚠️ | 아이콘으로: `icons/game-icons/`, `icons/kenney-2d/` · 사진으로: `images/photos-objects/` |
| 관객 효과음 | ⚠️ | `audio/audience/` (applause, applause-in-a-large-hall, crowd-cheering, boo) — **여럿이 웃는 소리 부족** |
| 막대·나무 딸깍 | ✅ | `audio/foley-props/100-cc0-metal-and-wood-sfx` |
| 무대·막 3D | ❌ | 없음 — 2D로 가면 불필요 |
| 음악 | ✅ | `audio/music/circus-dilemma`, `in-the-circus-psg-version`, kevin-macleod 코미디곡 |

### 6. 엘리베이터 — 층 선택 생존 / 내려가는 곳
| 필요 | 상태 | 위치 |
|---|---|---|
| 방 하나·문 하나 | ⚠️ | **엘리베이터 카·버튼 패널 모델 없음.** `models/kenney/building-kit`·`prototype-kit`으로 조립, 참고 사진 `images/photos-rooms-places/elevator` 13장 |
| 도착음·문 | ✅ | `audio/foley-props/elevator-ding`, `elevatordoor`, `door-open-door-close-set` |
| 앰비언스 30개 | ⚠️ | `audio/ambience/` 73팩(10초 이상 142개) — 층 이벤트용으로 충분. **엘리베이터 주행 루프는 없음** |
| 층 표시 | ✅ | `fonts/signage/dseg` (7·14세그먼트) |
| 음성 20줄 | ❌ | TTS |
| 음악 | ✅ | kevin-macleod 라운지곡 (Airport Lounge 등) (`audio/music/kevin-macleod-incompetech`) |

### 7. 오답 사전 — 단어 연금술 / 마지막 화자
| 필요 | 상태 | 위치 |
|---|---|---|
| 아이콘 200 / 50 | ✅ | `icons/game-icons/` 4,239 (**CC BY, 작가 표기**) · CC0로만 가려면 `icons/kenney-2d/` + `tabler`·`phosphor`(MIT) |
| 사전 폰트 | ✅ | `fonts/typewriter-serif/` (EB Garamond, 함렛, 나눔명조) |
| 종이 질감 | ✅ | `images/overlays/` paper · `images/textures/` paper |
| UI 효과음 | ✅ | `audio/kenney-audio/ui-audio`, `interface-sounds` · `foley-props/10-book-page-flips` |

### 8. 뒤집힌 수족관 — 수면 아래 스텔스 / 익사한 집
| 필요 | 상태 | 위치 |
|---|---|---|
| 굴절 셰이더 | ✅ | `shaders/water-refraction/` — URPUnderwaterEffects(굴절·커스틱·안개·빛줄기), **godotshaders Snell's Window**(수면 올려다보기) |
| 실루엣 8종 / 3 | ⚠️ | `images/silhouettes/` 54 (서기32·걷기11) — 위에서 본 그림자는 **셰이더로 투영** |
| 물 속 앰비언스 | ⚠️ | `audio/ambience/bubble-sound-effects`, `dripping-water*` · `ice/water-flowing-sound` — **하이드로폰 녹음 없음** |
| 천장 위 발소리 | ✅ | `audio/footsteps/` (wood) — 로우패스로 가공 |
| 집 한 채 | ✅ | `models/quaternius/ultimatehomeinterior` · 참고 `images/photos-rooms-places/flooded` |
| 물고기 | ✅ | `models/quaternius/animatedfish`, `cutefish` |
| 물 재질 | ❌ | CC0 물 텍스처 없음 → 셰이더로 |

### 9. 답장 없는 편지 — 펜팔 추리 / 보내지 못한 것
| 필요 | 상태 | 위치 |
|---|---|---|
| 손글씨 폰트 5 / 2 | ✅ | `fonts/handwriting/` — 영어 9, **한글 14** (나눔손글씨 펜·붓, 개구, 하이멜로디, 감자꽃, 동해독도 …) |
| 편지 80 / 40 | ❌ | 텍스트 직접 작성 |
| 종이·편지 사진 | ✅ | `images/photos-objects/letters`, `everyday_letters` · `images/overlays/` paper |
| 펜·종이 소리 | ⚠️ | `audio/foley-props/10-book-page-flips` · `sfx-ui-typing/typewriter-sounds` — **펜 긁는 소리 전용은 부족** |
| 참고 음성 | ✅ | `audio/voice-publicdomain/lettersfromacat_1309_librivox`, `eves_diary`, `extracts_adams_diary` (영어 서간·일기 낭독) |

### 10. 전화 교환수 — 교환대 타이쿤 / 연결하면 안 되는 통화
| 필요 | 상태 | 위치 |
|---|---|---|
| 교환대 화면 | ⚠️ | 참고 사진 `images/photos-rooms-places/switchboard` **37장**(PD). **3D 교환대 모델 없음** → 2D 제작 |
| 전화 신호음 | ✅ | `audio/foley-props/commons-telephone` 41개 (다이얼톤·통화중·링백·DTMF·로터리), `crank-movie-telephone-ringtone` |
| 플러그 꽂는 소리 | ❌ | 없음 — `100-cc0-metal-and-wood-sfx` 등에서 대체 가공 |
| 전화 음성 200 / 30 | ❌ | TTS + 전화 대역 필터. 질감 참고 `voice-publicdomain/mladytele1915` |
| 전화기 모델 | ✅ | `models/polyhaven/korean_public_payphone_01`, `vintage_telephone_wall_clock` |

### 11. 눈 감은 기록 — 1초 스냅 / 시력을 잃는 하루
| 필요 | 상태 | 위치 |
|---|---|---|
| 정지 이미지 60 / 30 | ⚠️ | 실내 사진 `images/photos-rooms-places/interior`·`corridor`·`abandoned` + 3D 렌더로 보충 |
| 앰비언스 | ✅ | `audio/ambience/`, `horror-drones/` |
| 바이노럴 | ❌ | 녹음 없음 — 엔진 HRTF 공간화로 대체 |
| 암전·잔상 | ✅ | `shaders/film-grain-vhs/` (비네트), `images/overlays/vignette*` |

### 12. 폐교 방송실 — 야간 방송 진행자 / 아무도 없는 학교에 말 걸기
| 필요 | 상태 | 위치 |
|---|---|---|
| 방 하나 | ⚠️ | `models/polyhaven/SchoolDesk_01`, `SchoolChair_01`, `Megaphone_01`, `boombox`, `cassette_player` · 참고 `images/photos-rooms-places/school` 28, `radio_station` 20. **방송 콘솔·마이크 스탠드 없음** |
| BGM 10 | ✅ | `audio/music/` 29팩 — lofi-compilation, a-cloudy-morning-jazz, november-snow, kevin-macleod … |
| 사연 300 | ❌ | 텍스트 |
| 복도 응답 음성 20 | ❌ | TTS + 잔향. 복도 룸톤도 부족 |
| 마이크 입력 처리 | ❌ | 코드 |

### 13. 잘못 배송된 소포 — 분실물 창고 / 한 사람의 유품
| 필요 | 상태 | 위치 |
|---|---|---|
| 소품 사진 300 / 100 | ✅ | `images/photos-objects/` **461장** (열쇠·시계·안경·장난감·공구·편지·소포·병·신발·책·보석). **배경 제각각 → 누끼 필요** |
| 상자 | ✅ | `models/polyhaven/cardboard_box_01`, `wooden_crate_*`, `vintage_suitcase` · `images/textures/ambientcg_Cardboard*` |
| 라벨 폰트 | ✅ | `fonts/typewriter-serif/` (Courier Prime, Special Elite) |
| 박스·테이프 소리 | ⚠️ | `audio/foley-props/item-handling`, `inventory-sound-effects` |

### 14. 한 프레임 만화 — 말풍선 퍼즐 / 마지막 컷
| 필요 | 상태 | 위치 |
|---|---|---|
| 컷 80 / 12 | ⚠️ | `images/comics-panels/panels` **269컷**(자동 분할) + 원본 페이지 135. **일부 컷에 2~3칸이 섞임, 말풍선 미제거** |
| 말풍선 폰트 | ✅ | `fonts/comic/` (Bangers, Comic Neue, 한글: 주아·도현·블랙한산스·동글) |
| 효과음 | ✅ | `audio/kenney-audio/ui-audio`, `foley-props/10-book-page-flips` |

### 15. 지하철 마지막 열차 — 창문 속 괴물 / 종점
| 필요 | 상태 | 위치 |
|---|---|---|
| 열차 하나 | ⚠️ | `models/quaternius/modulartrain`, `publictransport`, `models/kenney/train-kit` — **모두 외형. 객차 내부 없음** · 참고 `images/photos-rooms-places/subway_train` 34 |
| 승객 20 / 8 | ✅ | `models/characters-animated/universal-animation-library` (**Idle_Rail** 손잡이 잡기, Sitting) + `ultimatemodularcharacters`, `ultimatemodularwomen` |
| 반사 셰이더 | ✅ | `shaders/reflection/` (AdamPlaneReflection 내장 파이프라인, kMirrors, window-raindrop) |
| 주행 소리 | ❌ | 지하철 주행 루프 없음 — `ambience/background-rumble-noise`로 대체 가공 |

### 16. 얼음 아래 목소리 — 빙판 탈출 / 얼음 아래 누군가
| 필요 | 상태 | 위치 |
|---|---|---|
| 흰 평면·얼음 재질 | ✅ | `images/textures/ambientcg_Ice002~004` · `shaders/ice-crack/` (cracked-ice 시차 깊이, frosted-glass) |
| 균열 데칼 | ❌ | 없음 — `shaders/ice-crack/Godot-Glass-Break-Effect` 참고해 절차적으로 |
| 얼음 소리 | ⚠️ | `audio/ice/` (cracking-sounds, ice-breakingshattering, 눈 발소리) — **진짜 빙판 갈라지는 녹음 부족**. 수동 다운로드 후보: Commons `Sound of cracking ice.ogg` (CC BY-SA) |
| 두드림 | ✅ | `audio/foley-props/` impact 계열 → 로우패스 |
| HDRI | ✅ | `images/hdri/` 야외 야경 4 |

### 17. 박물관 야간 경비 — 틀린 그림 순찰 / 자리를 바꾸는 것들
| 필요 | 상태 | 위치 |
|---|---|---|
| 방 5 / 3 | ⚠️ | `models/kenney/building-kit`, `modular-buildings` · 참고 `images/photos-rooms-places/museum` 16 |
| 전시물 40 / 20 | ✅ | 3D: `models/polyhaven/marble_bust_01`, `gothic_statue`, `horse_statue_01`, `bronze_*_statue`, `ceramic_vase_*`, `vintage_grandfather_clock_01`, `vintage_microscope` · 2D: `images/photos-objects/` Met 소장품 275 |
| 경비 도구 | ✅ | `models/polyhaven/security_camera_*`, `vintage_flashlight` |
| 앰비언스 | ⚠️ | `audio/ambience/clock-ticking`, `horror-drones/` — **넓은 홀 룸톤 부족** |

### 18. 녹음기 되감기 — 테이프 감식 / 늘어진 테이프
| 필요 | 상태 | 위치 |
|---|---|---|
| 음성 60분 / 10분 | ⚠️ | `audio/voice-publicdomain/` 약 6시간(영어) — 필터·복원 시험용으로 충분. **한국어 진술은 TTS** |
| 테이프 기계음 | ⚠️ | `audio/foley-props/commons-projector-camera-tape` · 버튼 `16-button-clicks` — **되감기 모터 소리 부족** |
| 이펙트 체인 | ❌ | 코드(대역 필터·피치·와우플러터) |
| 녹음기 모델 | ✅ | `models/polyhaven/cassette_player`, `portable_cassette_player`, `boombox` |

### 19. 잠든 사람 옆에서 — 숨소리 도둑 / 간병
| 필요 | 상태 | 위치 |
|---|---|---|
| 방 10 / 1 | ✅ | `models/quaternius/ultimatehomeinterior`, `ultimatefurniture` · `models/polyhaven/old_bed_frame`, `GothicBed_01`, `vintage_day_bed` |
| 숨 루프 3단계 | ⚠️ | `audio/breathing-heartbeat/` (breathing-tired, dreaming) · `music/sleep-talking-loop` — **잠자는 숨 3단계 루프 없음 → 녹음·가공 필요** |
| 소품 소리 | ✅ | `audio/foley-props/` (cabinet-lock, creaky-light-wooden-door, item-handling) |
| 누운 캐릭터 | ❌ | 잠자는·숨쉬는 애니메이션 없음 — 정지 포즈 + 흉부 스케일 절차 애니로 |

### 20. 이름 없는 정거장 — 버스 정류장 관찰 / 버스가 오지 않는 저녁
| 필요 | 상태 | 위치 |
|---|---|---|
| 실루엣 50 / 10 | ⚠️ | `images/silhouettes/` 54 (서기32·걷기11·앉기9·기다리기2) + `models/quaternius/backgroundposedhumans` 28 포즈. **직업별 뒷모습 부족** |
| 정류장 | ✅ | `models/quaternius/modularstreets`, `downtown-city-megakit` · `models/kenney/city-kit-*` · 참고 `images/photos-rooms-places/bus_stop` 34 |
| 조명 곡선 | ✅ | `images/hdri/` 야경 4 · `models/polyhaven/street_lamp_*` |
| 거리 앰비언스 | ✅ | `audio/ambience/high-traffic-road-sounds`, `rain-and-thunder*`, crickets |

---

## 공통 부족 목록 — 제작·생성·수동 다운로드

| 무엇 | 컨셉 | 제안 |
|---|---|---|
| 한국어 대사 전부 | 2, 6, 10, 12, 18 | `kit/tools/tts_generate.py --game ...` (루트 `.env`의 키 필요 — 이 머신엔 `.env` 없음) |
| 엘리베이터 카, 지하철 객차 내부, 교환대, 방송 콘솔 | 6, 15, 10, 12 | 모듈 키트로 조립하거나 직접 모델링 |
| 얼음 균열 데칼, 물 재질 | 16, 8 | 셰이더·절차 생성 |
| 잠자는 숨 3단계, 주행 루프(지하철·엘리베이터), 하이드로폰, 바이노럴, 넓은 홀·복도 룸톤 | 19, 15, 6, 8, 11, 17, 12 | 녹음 또는 Sonniss GDC 번들(수동 확인) |
| 교환대 플러그, 영사기 모터, 테이프 되감기 모터 | 10, 4, 18 | 녹음 또는 가공 |
| 여럿이 웃는 소리 | 5 | 수동 다운로드 후보: Commons `72844 lonemonk approx-800-laughter-and-clapter-1.wav` (CC BY 3.0) |
| 직업별 뒷모습 실루엣, 누운 실루엣 | 20, 19 | 3D 캐릭터 렌더 → 실루엣 추출 |
| 만화 컷 정리·말풍선 제거 | 14 | 수작업 또는 반자동 |
| 사물 사진 누끼 | 13, 17 | 배경 제거 처리 |

수집이 막힌 출처: Freesound(로그인), poly.pizza·AIC·LoC(Cloudflare 차단), Sonniss(스크립트 403), Smithsonian(API 키), Wikimedia 일부(429 속도 제한).
