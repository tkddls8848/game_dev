# 76개 게임 컨셉 — 구동 엔진별 분류

> 2026-10-05 재분류. **2026-10-08: 보관 34를 [보관 (구번호)](#보관-구번호)로 내리고 활성 76개를 새 번호(1~76)로 옮겼다. 70(같은 주소)과 71~76(새 6안)은 Unity 2D 로 넣었다.**
> 번호·제목은 [CONCEPTS.md](CONCEPTS.md)와 [컨셉 원본](poc-gallery/concepts/_tools/concepts.json)을 따른다.
> **제작 시 권장하는 주 엔진**으로 각 안을 정확히 한 번씩 묶었다. 현재 해당 엔진으로 실행되는 게임 76개가 있다는 뜻은 아니다.
> [엔진별 시안 갤러리](poc-gallery/concepts/index.html)에서도 같은 묶음으로 볼 수 있다.

## 분류의 전제

**사용자가 언리얼을 피하려던 이유는 3D 작업의 공수가 너무 컸기 때문이다.** 이전 문서의 “C# 이식 비용 때문이며 3D 공수 때문이 아니었다”는 설명은 잘못된 해석이라 바로잡는다.

이번 분류는 **승인 시안의 품질을 유지하면서 공간·에셋·재질·조명·애니메이션·소리·최적화를 만드는 전체 공수**를 우선한다. 기존 C# 로직의 재사용은 그 비용 중 하나이며, Unreal 후보를 제외하는 절대 조건이 아니다. 미착수 컨셉에 이미 이식할 C# 구현이 있다고 가정하지 않는다.

실사 공간을 주로 경험하는 8개는 Unreal, 디오라마·스타일화 3D 12개는 Unity 3D, 2D·UI·오디오 55개는 Unity 2D, 링크 공유형 1개는 웹으로 권장 분류했다. **개별 엔진 배정과 공수 평가는 컨셉·기존 시각 방향에 따른 판단이며, 제작 시간 실측 결과는 아니다.**

Unreal의 PCG는 절차적 콘텐츠 제작 도구다. 이를 활용한 배경 배치 자동화는 검토할 수 있지만, AI 도입만으로 모델링·인물 애니메이션·음향·최적화 공수가 해소됐다고 보지 않는다. [Epic PCG 공식 문서](https://dev.epicgames.com/documentation/unreal-engine/procedural-content-generation-framework-in-unreal-engine)

Unity의 URP에는 2D 조명·그림자를 위한 2D Renderer가 있다. 2D 묶음도 종이 질감·레이어·조명·움직임·입력 피드백을 시안 수준으로 구현하는 전제다. [Unity URP 공식 안내](https://unity.com/resources/how-to-move-from-built-in-to-urp)

## 전체 집계

| 구동 엔진 | 개수 | 주된 제작 대상 |
|---|---:|---|
| Unreal Engine 5 | 8 | 실사 공간·환경·조명·공간음 |
| Unity · URP 3D | 12 | 디오라마·스타일화 3D·공간 조작 |
| Unity · URP 2D / UI·오디오 | 55 | 서류·카드·격자·이미지·소리 중심 |
| 웹 · TypeScript + PixiJS | 1 | 데일리 챌린지·링크 공유 |
| **합계** | **76** | **중복·누락 없음** |

엔진 제품 단위로는 **Unity 67 · Unreal 8 · 웹 1**다. 웹은 단일 엔진 제품명이 아닌 구동 환경이며, PixiJS 기반 구현을 권장안으로 둔다. Godot은 2D의 대안으로 남기되, 이번에는 기존 Unity/C# 제작 흐름을 활용하는 주 엔진 배정으로 통일했다. Godot의 품질이 낮다는 판정은 아니다.

## Unreal Engine 5 — 8개

실사 공간·조명·환경음 중심. 배경 제작 자동화의 효과를 확인할 묶음.

| # | 컨셉·시안 | 배정 근거 | 남는 제작 공수·확인점 |
|---:|---|---|---|
| 3 | [빛이 닿지 않는 것](poc-gallery/concepts/03-what-light-cannot-reach/index.html) | 방 하나의 빛·그림자와 인물 실루엣 | 빛에 따른 존재 판정과 실제 화면 일치, 실루엣 연출 |
| 8 | [수면 아래 스텔스](poc-gallery/concepts/08-surface-stealth/index.html) | 물속 시점의 굴절·그림자·광선 | 수중 재질·커스틱·40개 스테이지 제작. Unreal도 자동 해결 아님 |
| 15 | [창문 속 괴물](poc-gallery/concepts/15-reflection-monster/index.html) | 객차 조명과 창문 반사가 핵심 단서 | 승객 20종·표정/동작·반사 정확도. 인물 제작 공수 높음 |
| 16 | [빙판 탈출](poc-gallery/concepts/16-ice-escape/index.html) | 얼음 표면·균열·공간음의 현장감 | 균열 경로 규칙과 렌더 일치, 발밑 소리 판독성 |
| 17 | [틀린 그림 순찰](poc-gallery/concepts/17-spot-the-change-patrol/index.html) | 박물관을 직접 순찰하며 전시물 변화 관찰 | 방 5·전시물 40. 조명 변화가 오답 단서가 되지 않게 검증 |
| 19 | [숨소리 도둑](poc-gallery/concepts/19-breath-thief/index.html) | 침실 조명·숨소리·근접 긴장 | 방 10·집주인 동작·숨과 입력 타이밍. 제작 공수 높음 |
| 48 | [폭풍의 등대지기](poc-gallery/concepts/48-storm-lighthouse/index.html) | 등대 회전광·폭풍·바다의 공간 연출 | 파도·폭우·광선·배 가시성·성능. 이 묶음에서도 고공수 |
| 69 | [아무것도 없는 산책](poc-gallery/concepts/69-nothing-walk/index.html) | 실사 숲의 1인칭 산책과 재질별 환경음 | 식생·지형·동선·충돌·발소리·최적화. 전투가 없어도 제작 공수는 남음 |

## Unity · URP 3D — 12개

디오라마·스타일화 3D·공간 조작 중심. 기존 셸과 모듈형 에셋 활용.

| # | 컨셉·시안 | 배정 근거 | 남는 제작 공수·확인점 |
|---:|---|---|---|
| 21 | [밭이 줄어드는 세계](poc-gallery/concepts/21-farm-erosion/index.html) | 기존 Unity 셸. 섬·절벽·수면·안개 | 승인 시안 수준의 수면·침식 연출. 개발 본체는 별도 game 저장소 |
| 42 | [조수 간만 우체부](poc-gallery/concepts/42-tide-postman/index.html) | 갯벌 물골·차오르는 수위선 | **수면** — 51과 같은 위험 |
| 43 | [그림자 경매](poc-gallery/concepts/43-shadow-auction/index.html) | 조명 각도 → 실루엣 = 실시간 그림자 투영 | 규칙이 실루엣을 알아야 함(사전 계산 데이터) |
| 49 | [개미 고고학](poc-gallery/concepts/49-ant-archaeology/index.html) | 흙 단면 디오라마 · 따뜻한 조명 | 2D 레이어로도 가능 — 시안 재확인 |
| 51 | [골목 고양이 정치](poc-gallery/concepts/51-alley-cat-politics/index.html) | 저녁 골목 지붕 부감 | 저녁 조명·다수 소품 |
| 52 | [시계탑 수리공](poc-gallery/concepts/52-clocktower-repair/index.html) | 황동 톱니 · 역광 시계판 · 광장 | 톱니 회전을 정수 비율로 |
| 54 | [이불 요새](poc-gallery/concepts/54-blanket-fort/index.html) | 달빛 방 + 크레용 괴물 | 3D 방 속 2D 스프라이트 혼합 |
| 58 | [인형의 집 탐정](poc-gallery/concepts/58-dollhouse-detective/index.html) | 틸트시프트(DoF) + 벽 떼기 + 핀셋 | 3D 피킹 손맛 |
| 60 | [비행운 서예](poc-gallery/concepts/60-contrail-calligraphy/index.html) | 하늘 전체가 캔버스 · 연기 획 파티클 | 획 판정(헤드리스 ✕) · 비행 조작감 |
| 62 | [방탈출 제작자](poc-gallery/concepts/62-escape-room-maker/index.html) | 방 단면 + CCTV 벽 | 다중 렌더 텍스처 |
| 64 | [거인의 등 위 마을](poc-gallery/concepts/64-giant-back-village/index.html) | 구름 위 거인 · 먼 풍경 | 대기 원근·규모감 |
| 66 | [궤도 청소부](poc-gallery/concepts/66-orbit-sweeper/index.html) | 지구 곡면 · 궤도선 · 관제 HUD | 궤도는 정수 격자 — 엔진 물리 금지 |

## Unity · URP 2D / UI·오디오 — 55개

서류·카드·격자·이미지·소리 중심. 2D 조명과 효과까지 포함.

| # | 컨셉·시안 | 배정 근거 | 남는 제작 공수·확인점 |
|---:|---|---|---|
| 1 | [해킹 탈출 방](poc-gallery/concepts/01-hack-escape-room/index.html) | 터미널 한 화면 + CRT 후처리. 타이핑 반응이 손맛 | CRT 셰이더 이식(자산 다수가 GLSL/Godot) · 입력 지연 · IME |
| 2 | [주파수 사냥](poc-gallery/concepts/02-frequency-hunt/index.html) | 다이얼+노이즈 믹싱은 AudioMixer. 일일 시드는 웹 공유에 유리 | 웹이면 필터 체인 재작성 · 로직 WASM |
| 4 | [사진 속 범인](poc-gallery/concepts/04-culprit-in-photo/index.html) | 고해상 흑백 사진 확대·비교. 100건 캐주얼 | 텍스처 메모리·확대 품질 · 사진 합성은 엔진 무관 병목 |
| 5 | [마지막 공연](poc-gallery/concepts/05-last-performance/index.html) | 손 떨림 = 입력 필터 + 2D 관절. 조작감이 서사 | 엔진 물리는 연출만 · 떨림 파라미터는 정수 |
| 6 | [내려가는 곳](poc-gallery/concepts/06-going-down/index.html) | 층마다 앰비언스·음성, 화면은 버튼 패널 | 바이노럴이면 HRTF 플러그인 |
| 7 | [사라지는 언어의 마지막 화자](poc-gallery/concepts/07-last-speaker/index.html) | 사전 페이지 텍스트 + 먹 연출 | 한글 폰트 아틀라스 |
| 9 | [펜팔 추리](poc-gallery/concepts/09-penpal-deduction/index.html) | 편지·필체 비교 UI. 손글씨 폰트 14종 | 한글 손글씨 폰트 다수의 아틀라스 크기 |
| 10 | [연결하면 안 되는 통화](poc-gallery/concepts/10-call-not-to-connect/index.html) | 같은 자산 재사용. 음성 30줄 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 11 | [시력을 잃는 하루](poc-gallery/concepts/11-day-losing-sight/index.html) | 이미지 30 + 바이노럴 | HRTF 플러그인 (미확인) |
| 12 | [아무도 없는 학교에 말 걸기](poc-gallery/concepts/12-talking-to-empty-school/index.html) | 마이크 실시간 분석 → 복도의 대답 | 마이크 권한·장치 전환·입력 분석·응답 타이밍. 데스크톱 기준 |
| 13 | [분실물 창고](poc-gallery/concepts/13-lost-parcel-depot/index.html) | Papers, Please형 책상 UI. 소품 사진 461 | 드래그·도장 손맛 |
| 14 | [말풍선 퍼즐](poc-gallery/concepts/14-bubble-puzzle/index.html) | 원안이 "모바일 궁합 최고" → 네이티브 모바일 | 터치 드래그 · 다해상도 레이아웃 · 컷 그림 병목 |
| 18 | [테이프 감식](poc-gallery/concepts/18-tape-forensics/index.html) | blackwood 되감기·배속 재사용 + 필터 체인 | 실시간 DSP(`OnAudioFilterRead`) · 웹이면 재작성 |
| 22 | [되돌릴 수 있는 한 해](poc-gallery/concepts/22-farm-rewind-year/index.html) | 계절 다이얼 + 밭 격자 UI | 되감기 전환 연출 |
| 23 | [작물이 정보다](poc-gallery/concepts/23-farm-signal/index.html) | 시세판·밭 격자 | 대량 수치 표시 판독성 |
| 24 | [되감는 전투](poc-gallery/concepts/24-deck-rewind/index.html) | 카드 전투 + 되감기 | 되감기 연출(상태 복원과 화면 일치) |
| 25 | [공개된 다음 수](poc-gallery/concepts/25-deck-openhand/index.html) | 적 다섯 수 공개 UI | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 26 | [소모되는 활자](poc-gallery/concepts/26-deck-attrition/index.html) | 활자 카드 인쇄 횟수 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 27 | [수확하는 덱](poc-gallery/concepts/27-hybrid-harvest-deck/index.html) | 밭 → 덱 전환 두 화면 | 두 루프 전환 연출 |
| 28 | [겨울 요새](poc-gallery/concepts/28-hybrid-siege-seasons/index.html) | 설원 방어 전투 카드 | 눈·안개 파티클 |
| 29 | [덱을 심는다](poc-gallery/concepts/29-hybrid-sown-deck/index.html) | 표본 카드 ↔ 밭 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 30 | [능선 초소](poc-gallery/concepts/30-tactics-partisan-1941/index.html) | 전술 지도 + 순찰 시간표 | 실시간 전술 입력 반응 |
| 31 | [속삭이는 지도](poc-gallery/concepts/31-tactics-whisper-map/index.html) | 손그림 지도 + 신뢰 자원 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 32 | [죽은 사람의 휴대폰](poc-gallery/concepts/32-locked-phone/index.html) | 잠금 화면 하나가 전부 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 33 | [냄새로 푸는 추리](poc-gallery/concepts/33-scent-layers/index.html) | 냄새 층 시각 기호 | 층 벗기기 연출 |
| 34 | [원고 되돌려 보내기](poc-gallery/concepts/34-red-pen/index.html) | 붉은 펜 문장 편집 | 펜 획 손맛 |
| 35 | [소리 없는 오케스트라](poc-gallery/concepts/35-silent-baton/index.html) | 무음 지휘. 이미 JS 이식 + 대조 하네스 보유 | 입력→화면 지연 · JS 이식 갈라짐 |
| 36 | [철거 직전 건물의 마지막 세입자들](poc-gallery/concepts/36-last-tenants/index.html) | 건물 단면 + 시간 제한 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 37 | [번역가의 전쟁](poc-gallery/concepts/37-the-interpreter/index.html) | 협상 장면 + 문장 카드 | 긴 대본 |
| 38 | [신의 비서](poc-gallery/concepts/38-gods-secretary/index.html) | 편지 심사 · 도장 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 39 | [가업으로 물려받은 저주 관리](poc-gallery/concepts/39-curse-ledger/index.html) | 장부 UI | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 40 | [가짜 기억 심기](poc-gallery/concepts/40-grafted-memory/index.html) | 기억 타임라인 + 거부 반응 연출 | 글리치 후처리 |
| 41 | [지도가 거짓말하는 도시](poc-gallery/concepts/41-the-map-lies/index.html) | 도시 도면 격자 | 도시가 스스로 고치는 변화 연출 |
| 44 | [중력 주방](poc-gallery/concepts/44-gravity-kitchen/index.html) | 결정적 격자 물리 + 회전 연출 | 엔진 물리 금지 · 회전 트윈 손맛 |
| 45 | [봉화 네트워크](poc-gallery/concepts/45-beacon-network/index.html) | 산수화 지도 + 불꽃·연기 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 46 | [벌집 도시계획](poc-gallery/concepts/46-hive-planner/index.html) | 육각 격자 + 밀랍·빛 입자 | 반투명 층 정렬 |
| 47 | [유령 부동산](poc-gallery/concepts/47-haunted-realty/index.html) | 평면도 + 계약서 조항 조합 | 반투명 유령 |
| 50 | [지우개 모험](poc-gallery/concepts/50-eraser-platformer/index.html) | 지형을 지우는 플랫포머 | 변형 지형 충돌 직접 구현 · 플랫포머 손맛 |
| 53 | [한 해의 나무](poc-gallery/concepts/53-plant-year/index.html) | 수채 톤 뒷마당 + 타임랩스 조명 | 타임랩스 조명을 2D로 낼지 3D로 갈지 |
| 55 | [향신료 항로](poc-gallery/concepts/55-spice-route/index.html) | 선창 단면 격자 + 색 안개 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 56 | [괴수 세탁소](poc-gallery/concepts/56-monster-laundry/index.html) | 네온·거품 파티클 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 57 | [자판기의 마음](poc-gallery/concepts/57-vending-soul/index.html) | 비 오는 골목 + 버튼 패널 | 비·네온 반사 |
| 59 | [거꾸로 던전](poc-gallery/concepts/59-reverse-dungeon/index.html) | 도트 단면 던전 | 픽셀 퍼펙트 카메라 |
| 61 | [메아리 지도 제작자](poc-gallery/concepts/61-echo-cartographer/index.html) | 메아리 지연·반사 + 음파 링 | 규칙의 ms 지연과 재생 일치 · HRTF |
| 63 | [열차 시간표 교향곡](poc-gallery/concepts/63-timetable-symphony/index.html) | 도착이 음이 된다 — 입력 판정 아닌 재생 예약 | 오디오 예약 정밀도 |
| 65 | [먹물 문어](poc-gallery/concepts/65-ink-octopus/index.html) | 먹물 획 드로잉 + 생물 발광 | 그린 형태 판정(△) · 2D 라이트 |
| 67 | [이름 없는 신당](poc-gallery/concepts/67-lost-name-shrine/index.html) | 안개 산사 · 반투명 신 · 촛불 | 2D 라이트·안개 |
| 68 | [일곱 번째 주](poc-gallery/concepts/68-seventh-week/index.html) | 격자 어드벤처 맵 + 전술 전투. PLAN_GENRES §5 가 Unity 2D 셸로 적어 두었다 | 고전풍 장식 UI · 픽셀 스냅 |
| 70 | [같은 주소](poc-gallery/concepts/70-same-address/index.html) | 작은 게임 둘(목장·던전) + 공유 메모리 256바이트 한 화면. 2D 타일 | 두 화면 동시 갱신·메모리 칸 하이라이트. 대안 웹(링크 공유) |
| 71 | [반동 비행사](poc-gallery/concepts/71-recoil-drifter/index.html) | 탑다운 무중력 정거장. 반동 이동과 탄 피드백이 전부라 2D로 충분 | 조준·표류 손맛(히트스톱·흔들림·탄피). 물리는 규칙 쪽 정수로, 엔진 물리는 연출만 |
| 72 | [10초 전의 나](poc-gallery/concepts/72-ten-seconds-ago/index.html) | 탑다운 아레나 하나. 유령은 입력 로그 재생이라 고정 16ms 틱 | 고정 틱과 렌더 보간 분리 · 대시 베기 조작감 · 유령 다수 동시 표시 |
| 73 | [녹는 창고](poc-gallery/concepts/73-melting-icehouse/index.html) | 빙고 단면 한 장 + 시세·재고 장부. 한지·먹 톤 | 얼음 더미가 줄어드는 연출·물방울. 절기 전환 |
| 74 | [길드 인사과](poc-gallery/concepts/74-guild-hr/index.html) | 서류철·보고서·도장. 전투는 화면에 없다 | 한글 공문 대량 · 타자·도장 손맛 · 촛불 2D 조명 |
| 75 | [소문은 덱이다](poc-gallery/concepts/75-rumor-deck/index.html) | 장터 지도 + 한지 쪽지 카드. 카드가 길을 따라 움직이며 문장이 바뀐다 | 문장 변형 연출(취소선·새 낱말) · 카드 트윈 |
| 76 | [섞지 않는 덱](poc-gallery/concepts/76-unshuffled-train/index.html) | 옆에서 본 열차 한 줄 = 덱. 끌어서 편성 | 칸 드래그·인접 효과 빛 · 열차 진행 연출 |

## 웹 · TypeScript + PixiJS — 1개

설치 없이 링크로 공유하는 데일리 챌린지. 웹 런타임 권장.

| # | 컨셉·시안 | 배정 근거 | 남는 제작 공수·확인점 |
|---:|---|---|---|
| 20 | [버스 정류장 관찰](poc-gallery/concepts/20-bus-stop-watch/index.html) | 실루엣 분류 데일리 → 링크 공유 | 로직 WASM/이식 |

## 보관 (구번호)

2026-10-06 에 목록에서 내린 34개다([CONCEPTS.md 보관 34](CONCEPTS.md#보관-34--특징점이-없어-목록에서-내린-안)). **번호는 구번호**라 위 표의 번호와 겹친다. 엔진 판단은 보관과 무관해 남겨 둔다 — 되살리면 새 번호를 받고 위 표로 올린다. 집계에는 넣지 않는다.

| 구# | 당시 배정 | 컨셉·시안 | 배정 근거 | 남는 제작 공수·확인점 |
|---:|---|---|---|---|
| 2 | Unity 2D | [삭제 직전 로그](poc-gallery/concepts/_archive/02-last-log-before-delete/index.html) | 로그 대화 한 화면. 엔진 요구 최소 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 4 | Unity 2D | [죽은 방송국](poc-gallery/concepts/_archive/04-dead-station/index.html) | 저음질 음성 40분 + 다이얼 하나. 오디오가 전부 | 긴 음성 스트리밍·메모리 (미측정) |
| 5 | Unreal | [배터리 서바이벌](poc-gallery/concepts/_archive/05-battery-survival/index.html) | 손전등·암실·발소리로 공간 공포 구성 | 원안의 저폴리 방을 유지하면 Unity 3D도 적합. 실사 시안 방향일 때 Unreal 우선 |
| 8 | Unity 2D | [찍히지 않은 사람](poc-gallery/concepts/_archive/08-the-one-not-photographed/index.html) | 사진첩 25장 + 필름 노이즈 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 9 | Unity 2D | [인형극 대결](poc-gallery/concepts/_archive/09-puppet-duel/index.html) | 소품 카드 조합 + 종이 인형 무대(2D 라이트·관절) | 판정이 주관적(헤드리스 ✕) — 엔진보다 설계 위험 |
| 11 | Unity 3D | [층 선택 생존](poc-gallery/concepts/_archive/11-floor-choice-survival/index.html) | 고정 시점 엘리베이터 방 하나. 로직은 씨드 로그라이크 | 범위 작음. 2D 페인팅 배경으로 낮추면 시안 미달 |
| 13 | Unity 2D | [단어 연금술](poc-gallery/concepts/_archive/13-word-alchemy/index.html) | Baba 인접 격자 퍼즐. 아이콘 완비 · 완전 헤드리스 | 한글 입력 UI |
| 16 | Unreal | [익사한 집](poc-gallery/concepts/_archive/16-drowned-house/index.html) | 잠긴 방을 올려다보는 수면·빛·공간음 | 수면 아래 시야와 발소리 위치. 기존 셰이더의 엔진 호환 재검토 |
| 18 | Unity 2D | [보내지 못한 것](poc-gallery/concepts/_archive/18-never-sent/index.html) | 답장을 직접 쓰는 행위가 핵심 | 한글 IME 자유 입력 품질 (미검증) |
| 19 | Unity 2D | [교환대 타이쿤](poc-gallery/concepts/_archive/19-switchboard-tycoon/index.html) | blackwood 엿듣기·가청 판정 자산이 Unity에 있다 | 동시 다채널 음성 믹싱 |
| 21 | 웹 | [1초 스냅](poc-gallery/concepts/_archive/21-one-second-snap/index.html) | 정지 이미지 60 + 데일리 챌린지 → 링크가 유통 경로 | 1초 노출이 60Hz 프레임으로 양자화 · 로직 WASM/이식 |
| 23 | Unity 2D | [야간 방송 진행자](poc-gallery/concepts/_archive/23-night-dj/index.html) | 사연 텍스트 선택 + BGM 10 | 사연 300개 — 작가 도구(Ink) 검토 여지 |
| 26 | Unity 2D | [한 사람의 유품](poc-gallery/concepts/_archive/26-one-persons-belongings/index.html) | 상자·사진 타임라인 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 28 | Unity 2D | [마지막 컷](poc-gallery/concepts/_archive/28-last-panel/index.html) | 컷 12장 + 말풍선 이동 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 30 | Unreal | [종점](poc-gallery/concepts/_archive/30-terminus/index.html) | 객차 한 공간의 조명·승객·창밖 어둠 | 승객 8종의 연기·음성. 배경 자동화만으로 끝나지 않음 |
| 32 | Unreal | [얼음 아래 누군가](poc-gallery/concepts/_archive/32-someone-under-ice/index.html) | 얼음 아래 존재와 두드림의 공간감 | 얼음 투과 표현·발밑 소리 정위. #16(구#31)과 환경 공유 가능 |
| 34 | Unreal | [자리를 바꾸는 것들](poc-gallery/concepts/_archive/34-things-that-move/index.html) | 박물관 공간·전시물 교체·조명 | 방 3·전시물 20. #17(구#33)과 환경 공유 가능 |
| 36 | Unity 2D | [늘어진 테이프](poc-gallery/concepts/_archive/36-stretched-tape/index.html) | 재생할수록 늘어지는 목소리 | 음높이 유지 시간 늘이기 기본 지원 (미확인) → 미리 굽기 |
| 38 | Unreal | [간병](poc-gallery/concepts/_archive/38-caregiving/index.html) | 방 하나의 조명·거리·호흡으로 밤샘 간병 표현 | 부모의 호흡 동작과 연기. 고정 시점도 인물 품질 중요 |
| 40 | Unreal | [버스가 오지 않는 저녁](poc-gallery/concepts/_archive/40-bus-never-comes/index.html) | 정류장에서 해가 지는 빛과 시간 변화 | 환경 제작 검증에 적합. 기다림의 체험과 환경음은 사람 판정 |
| 41 | Unity 2D | [잔향 감식실](poc-gallery/concepts/_archive/41-mystery-scent-layers/index.html) | 시간층 전환 UI + 채취 | 층 전환 블렌드 연출 |
| 42 | Unity 2D | [휴전의 문장](poc-gallery/concepts/_archive/42-narrative-armistice/index.html) | 조약문 비교 · 강도 이동 | 긴 대본 현지화 |
| 43 | Unity 2D | [잠금 해제 불가](poc-gallery/concepts/_archive/43-mystery-locked-phone/index.html) | 가상 전화 UI | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 44 | Unity 2D | [기억의 봉합사](poc-gallery/concepts/_archive/44-puzzle-memory-suture/index.html) | 조각 조립 UI | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 45 | Unity 3D | [내일의 지도](poc-gallery/concepts/_archive/45-puzzle-tomorrow-map/index.html) | **Unity 셸 있음**(로우폴리 도시 디오라마) | 기존 Unity 셸은 내장 파이프라인. URP 배정은 신규 제작 방향이며 이전 미실시 |
| 46 | Unity 2D | [저주상속](poc-gallery/concepts/_archive/46-management-curse-inheritance/index.html) | 장부·계약 UI, 촛불 조명 | 2D 라이트 깜빡임 |
| 47 | Unity 2D | [당신의 박자](poc-gallery/concepts/_archive/47-rhythm-your-tempo/index.html) | **무음** 시각 박자 → 입력→화면 지연이 전부 | 브라우저 rAF·vsync 지터(README 기록) · 주사율 |
| 48 | Unity 2D | [붉은 펜으로 남긴 것](poc-gallery/concepts/_archive/48-narrative-redline/index.html) | 원고 삭제·교체·순서 편집 | 텍스트 선택·드래그 UI |
| 49 | Unity 2D | [마지막 이삿날](poc-gallery/concepts/_archive/49-narrative-last-address/index.html) | 복도·문 장면 + 선택 | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |
| 50 | Unity 2D | [기적 배정과](poc-gallery/concepts/_archive/50-puzzle-prayer-office/index.html) | 강 유역 지도 + 시간대 배정 | 하류 인과 시각화 |
| 84 | Unity 3D | [하늘길 관제소](poc-gallery/concepts/_archive/84-drone-control/index.html) | 3층 고도 투시 뷰 + 항적 | 다수 드론 항적 렌더 |
| 92 | Unity 2D | [우산 대여점](poc-gallery/concepts/_archive/92-umbrella-shop/index.html) | 노선도 UI + 비 오는 부감 | 비 파티클 |
| 93 | Unity 3D | [눈사람의 겨울](poc-gallery/concepts/_archive/93-snowman-life/index.html) | 고정 시점 마당 · 계절 조명 · 녹는 몸 | 녹는 메시 변형 |
| 98 | Unity 2D | [마지막 순회 서커스](poc-gallery/concepts/_archive/98-last-circus-tour/index.html) | 로드맵 + 프로그램 카드 UI | 원안 분량의 이미지·대사·음향 제작과 연출 검수 |

## 공수 때문에 따로 살펴볼 경계 사례

- **보관 구#5 배터리 서바이벌:** 원문은 저폴리 방 6개다. 기존 시각 방향의 공간·조명을 따라 Unreal로 묶었지만, 저폴리 원안 기준 제작에서는 Unity가 자연스러운 대안이다. 엔진 선택을 이유로 실사화를 강제하지 않는다.
- **#8 수중 표현(보관 구#16 도 같은 문제), #48 폭풍 바다:** 엔진을 바꿔도 물 표현과 가시성 조정 공수가 크다. 샘플 장면에서 시안 대응 화면과 성능을 먼저 비교한다.
- **#15·19 인물 등장(보관 구#30·38 도 같다):** 배경 배치 자동화와 별개로 인물·호흡·연기가 필요하다. 그 비용을 배경 제작 시간에 숨기지 않는다.
- **#49·51·52·62:** 화면만 보면 2D 대안이 가능하지만, 현재 공간·디오라마 방향으로 Unity 3D에 묶었다. 품질이나 공간 조작을 임의로 생략하지 않는다.
- **#53 한 해의 나무:** 수채 톤과 레이어·조명 변화 기준으로 2D에 배정했다. 실제 가지의 입체 성장과 카메라 회전이 필요해지면 Unity 3D로 재검토한다.
- **#12·61 소리 중심 공간:** 보이는 3D 환경의 밀도보다 마이크·메아리·지도와 음향이 중심이라 UI·오디오 묶음에 둔다. 소리 계산용 공간 모델을 쓰는 것과 3D 배경 제작은 구분한다.

## 착수 시 검증과 기존 구현의 처리

1. **전체 공수 측정:** #69 오솔길 한 구간 또는 #20 정류장 장면(보관 구#40 의 짝)으로 에셋 탐색·배치·재질·조명·음향·충돌·성능 조정 시간을 각각 기록한다. AI가 만든 뒤 사람이 고친 시간도 포함한다. #48은 별도로 물·폭풍 제작 공수를 검증한다.
2. **시안·플레이 검증:** 대응되는 실제 화면뿐 아니라 이동·입력·음향을 확인한다. 기본 도형이나 테스트 통과로 감각 품질을 대체하지 않는다. 분류 때문에 콘텐츠 수나 사용자 지정 범위를 줄이지 않는다.
3. **규칙 재사용:** 기존 순수 C#은 유지한다. Unreal 구현이 필요하면 동일 입력·씨드·JSON 결과를 비교하는 대조 검증, 별도 규칙 테스트 등 적합한 경로를 설계한다. C# 테스트가 Unreal 구현의 정확성을 자동으로 보장하지는 않지만, 엔진 변경이 모든 헤드리스 검증의 상실을 뜻하지도 않는다.
4. **현재 상태와 권장 엔진 구분:** HTML 목업은 출시 런타임이 아니다. 기존 Unity 셸과 외부 `game` 저장소의 개발작을 이번 분류만으로 이식하지 않는다. 에디터 버전·파이프라인 설치/변경도 이번 작업에 포함하지 않았다.

## 관리 기준

엔진별 번호 목록은 [`engine-groups.json`](poc-gallery/concepts/_tools/engine-groups.json)에 둔다. 갤러리 생성기는 이 목록으로 그룹과 개수를 만들고 중복·누락을 검사한다. 이 문서의 표를 수정할 때 같은 파일도 함께 갱신한다.

컨셉의 원문·상태는 [CONCEPTS.md](CONCEPTS.md), 게임별 구현 사실은 해당 README, 공통 제작·품질 규약은 [CLAUDE.md](../CLAUDE.md)와 [POC_FACTORY.md](POC_FACTORY.md)를 따른다. 이전 문서의 엔진 버전·요금·지원 일정 비교는 이번 배정의 근거로 사용하지 않았다. 실제 도입 시 공식 문서에서 다시 확인한다.
