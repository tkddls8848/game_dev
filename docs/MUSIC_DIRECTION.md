# 음악 연출 방향 — 컨셉 101개를 음악 가족 27개로

> 2026-09-29. 대상: [`CONCEPTS.md`](CONCEPTS.md)의 컨셉 101개 전부.
> 기계 판독용 사본: [`poc-gallery/music/families.json`](poc-gallery/music/families.json) — 이 문서의 가족 절(§3)과 같은 원본에서 만들었다.
> **2026-10-08: 컨셉 번호를 재정렬 뒤 번호(1~76)로 바꿨다.** 보관된 안은 `구#N` 으로 남겼다(가족에서 빼지 않았다 — 되살리면 쓴다). 새 6안(71~76)은 아직 어느 가족에도 넣지 않았다. 대응표는 [`CONCEPTS.md`](CONCEPTS.md#번호-대응표-2026-10-08-재정렬).
> **음악은 아직 한 곡도 생성하지 않았다.** §5의 크레딧 계산은 제안이고, 생성 여부는 사람이 정한다.
>
> **결정(2026-09-29, 사용자): AI 음악은 생성하지 않는다.** 당분간 기존 무료 라이브러리(§2, 표기 의무 확인)와
> Artlist 참고곡(청취용)으로만 방향을 잡는다. 라이브러리가 비는 가족(특히 F18 국악)은 공공 음원 수집 등 다른 경로가 정해질 때까지 공백으로 둔다.

## 0. 이 문서가 지키는 원칙

뿌리 [`CLAUDE.md`](../CLAUDE.md)의 "게임은 감각까지 구현한다"를 음악에 적용한다.

- **음악은 행동·환경·상태에 반응해야 한다.** 가족마다 `적응형 레이어` 칸을 두었다. 상태 변수(위험도·남은 시간·진행도·게이지)가 어느 레이어·템포·필터를 움직이는지 적었다. 루프 한 곡을 처음부터 끝까지 틀어 놓는 것은 완료 기준이 아니다.
- **임시 삐 소리나 아무 곡으로 채워 두고 완료라고 하지 않는다.** 라이브러리 곡을 쓰더라도 분위기·템포가 그 가족 방향에 맞는지 청취로 확인한 뒤에 쓴다(§3의 라이브러리 후보는 **색인 기준**이다. 바이너리가 이 머신에 없어 들어 보지 않았다).
- **음악을 줄이거나 빼는 것도 연출이다.** 음악이 정보를 가리는 컨셉(§3 F02)과 음악이 없어야 성립하는 컨셉(§3 F03)은 음악을 빼고 **대체 감각**을 구체적으로 적었다. 무음은 "안 만듦"이 아니라 설계다.
- **엔진 쪽 규율.** 수치는 정수(볼륨은 dB×10이나 백분율 정수, 템포는 BPM 정수, 박 위치는 ms), 레이어 전환은 박 경계에 양자화한다. 음악 상태 결정(어느 레이어를 켤지)은 순수 C#으로 두면 헤드리스 테스트가 된다 — 설계 원칙 1·4를 음악에도 적용한다.

## 1. 한눈에 보기

27개 가족. 한 컨셉은 한 가족에만 속한다(중복 없음·빠짐 없음 — 생성 스크립트가 1~101을 전부 확인했다).
`라이브러리` 열은 현재 무료 라이브러리(`docs/asset-index/audio.md`)로 그 가족을 얼마나 채울 수 있는지다.

| ID | 가족 | 컨셉 | BPM | 라이브러리 | 생성 순위 |
|---|---|---|---|---|:-:|
| F01 | 호러 드론·저음 긴장 | 구5, 3, 구11, 6, 구21, 15 | 무박~70 | 부분 | 7 |
| F02 | 소리가 정보 — 음악 최소·덕킹 | 12, 16, 구32, 19, 61 | 무박 | 음악 거의 불필요 | — |
| F03 | 무음 지향·다이제틱 대체 | 구40, 구47, 33, 35 | — | 음악 불필요 | — |
| F04 | 아날로그 라디오·다이제틱 방송 | 2, 구4, 구23 | 70–110 | 없음 | — |
| F05 | 터미널 글리치·사이버 신스 | 1, 구2 | 120–140 / 무박 | 부분 | — |
| F06 | 모던 수사·미니멀 전자 펄스 | 18, 구41, 구43, 32 | 90–110 | 없음 | 3 |
| F07 | 테이프 루프·열화 노스탤지어 | 구8, 구26, 구30, 구36 | 55–75 | 부분 | — |
| F08 | 상실·돌봄 미니멀 피아노 | 구18, 11, 구28, 구38, 구49, 36 | 60–80 | 부분 | 8 |
| F09 | 서재·편지 실내악 (펜과 활자) | 9, 구48, 26, 34 | 72–96 | 없음 | 4 |
| F10 | 천상 관료제 (하프·첼레스타·허밍) | 구50, 38 | 84–100 | 빈약 | — |
| F11 | 노이르 재즈·야간 순찰 | 4, 10, 17, 구34, 43 | 60–90 스윙 | 대부분 있음(BY) | — |
| F12 | 보드빌·래그타임·서커스 칼리오페 | 구9, 구19, 구98 | 100–140 | 대부분 있음(BY) | — |
| F13 | 오르골·태엽·미니어처 | 5, 49, 52, 58 | 84–110 (3/4) | 없음 | 6 |
| F14 | 동화 어쿠스틱 (연필·이불·눈사람) | 50, 54, 구93 | 80–120 | 부분 | — |
| F15 | 목가 어쿠스틱 파밍 (계절 한 벌) | 21, 22, 23, 27, 29, 64 | 80–110 | 빈약 | 2 |
| F16 | 바다·항해 포크 (물때·폭풍·향신료) | 42, 48, 55 | 84–120 (6/8) | 없음 | 11 |
| F17 | 수중 앰비언트 | 8, 구16, 65 | 무박~70 | 없음 | 12 |
| F18 | 국악·조선 산수 | 7, 45, 67 | 40–90 (장단) | 없음 | 9 |
| F19 | 전쟁과 조약 — 저현 오스티나토 | 구42, 30, 31, 37 | 70–100 | 없음 | 5 |
| F20 | 다크 판타지·오컬트 (촛불과 장부) | 구46, 25, 28, 39 | 60–90 | 빈약 | 10 |
| F21 | 몽환 기억 앰비언트 | 구44, 40 | 무박~75 | 부분 | — |
| F22 | 하늘·계절 앰비언트 | 53, 60 | 60–90 | 부분 | — |
| F23 | 시스템 미니멀리즘 (반복 패턴) | 구45, 41, 46, 구84, 63 | 100–130 | 없음 | 1 |
| F24 | 경쾌 퍼즐 (피치카토·마림바) | 구13, 14, 62 | 110–135 | 부분 | — |
| F25 | 유쾌한 괴물 (테레민·튜바 / 칩튠 변주) | 47, 56, 59 | 96–140 | 부분 | — |
| F26 | 로파이 재즈·빗속 도시 (일상 경영) | 13, 20, 51, 57, 구92 | 70–90 | 충분 | — |
| F27 | 하드 SF 신스 (궤도·정거장) | 24, 44, 66 | 80–120 | 부분 | — |

**음악이 의도적으로 적거나 없는 컨셉 9개.** F02(12·16·구32·19·61: 소리가 곧 정보라 음악은 서브 드론 한 줄 + 덕킹)와
F03(구40·구47·33·35: 음악 없음). 특히 **#35 소리 없는 오케스트라와 구#47 당신의 박자는 청각을 잃은 지휘자가 시각 박으로 지휘하는 게임이라 배경음악이 규칙 위반이다.**
대체는 시각 박(단원 호흡·활 각도·조명 펄스)·화면 진동·60Hz 이하 촉각형 저음 한 번이다. #63 열차 시간표 교향곡은 반대로 **음악이 곧 게임플레이**라 배경곡이 아니라 음색 샘플 세트가 필요하다(F23).

## 2. 라이브러리에 대해 먼저 알아 둘 것

- 현재 라이브러리: 팩 276개, `music/` 29팩 83파일. **바이너리는 이 머신에 없고 목록([`asset-index/audio.md`](asset-index/audio.md))만 있다.** 아래 후보는 전부 청취 확인 전이다.
- `audio.md`의 `컨셉` 열 번호는 이 문서의 컨셉 번호가 **아니다.** 옛 `plam.md` 주제 번호(1~20)다. 주제 n = 구번호 컨셉 2n−1, 2n(예: 주제 16 얼음 아래 목소리 = 구#31·구#32 → 지금 #16 · 보관 구#32).
- **BY 표기 의무가 있는 음악 팩**: Matthew Pablo 7팩(a-cloudy-morning-jazz, a-conversation-with-saul, circus-dilemma, deliciously-sour, snowland-town, soliloquy, trouble-makers — CC-BY 3.0), Kevin MacLeod 26곡(CC-BY 4.0, 파일별 attribution), calm-bgm, death-is-just-another-path, in-the-circus-psg-version, mystical-theme, one, school-of-quirks. 게임에 넣으면 `SOURCE.json`의 `attribution`을 그 게임 `CREDITS.md`로 옮긴다.
- **BY-SA 주의**: `ambience/water-harp`, `horror-drones/is-anybody-home`·`realization`·`soled-bad-memory`, `ambience/gull-sounds` 등은 가공해 배포하면 가공본도 BY-SA가 된다.
- **CC0 음악은 적다**: bossa-nova, chill-lofi-inspired, lofi-compilation(9), childrens-march-theme, cyberpunk-moonlight-sonata, free-music-pack(7), forest-ambience, mysterious-ambience-song21, november-snow, rain-and-thunders, shop-theme, sleep-talking-loop, snowfall, talking-cute-chiptune, the-field-of-dreams. 로파이·파밍 일부를 빼면 가족별로 한두 곡이다.
- **모든 라이브러리 곡은 스테레오 단일 믹스다.** 적응형 레이어에 필요한 스템(악기별 분리)은 없다. 레이어 설계는 (a) 곡 하나 + 필터·볼륨·템포 자동화, (b) 같은 키·템포의 곡을 따로 만들어 겹치기, 둘 중 하나로 한다.

## 3. 가족별 방향

각 가족: 분위기 · 편성 · 템포 · 루프 구조(인트로/루프/스팅어) · 적응형 레이어 · 쓰지 말 것 · 라이브러리 후보와 빈 곳 · Artlist 청취 참고 · 생성 프롬프트 초안.

### F01 · 호러 드론·저음 긴장

**컨셉.** 구#5 배터리 서바이벌 · #3 빛이 닿지 않는 것 · #11 층 선택 생존 · #6 내려가는 곳 · 구#21 1초 스냅 · #15 창문 속 괴물

| 항목 | 방향 |
|---|---|
| 분위기 | 숨 막힘, 좁은 공간, 보이지 않는 것의 존재감, 끝나지 않는 하강 |
| 편성 | 서브베이스 드론, 활로 긁은 금속(bowed cymbal/waterphone), 저역 현 클러스터, 먼 금속성 타격, 역재생 피아노 한 음 |
| 템포 | 무박(프리 템포) 드론 + 위험 레이어에서 60~70 BPM 심장 박동형 펄스 |
| 루프 구조 | 인트로 없음(환경음에서 스며듦) → 60~90초 드론 루프(2~3개 교대) → 스팅어(발견·접촉 0.5~2초, 곡이 아니라 효과음 층) |
| 적응형 레이어 | L0 룸톤/환경음만 → L1 드론(위험도 1) → L2 저역 펄스(적 근접) → L3 고역 현 트레몰로(시야 안 위협). 손전등 배터리(구5)·남은 시간(구21)·층수(구11,6)에 L2 템포를 5~10% 연동. 발견 순간 전 레이어 200ms 덕킹 후 스팅어. |
| 쓰지 말 것 | 점프스케어용 오케스트라 히트 남발, 선율이 분명한 테마(공포가 '음악'이 되어 안전해짐), 4/4 드럼 비트, 에픽 트레일러 브라스 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`horror-drones/dreamscape-drone` (CC-BY 3.0 · **BY**), `horror-drones/horror-ambient` (CC-BY 3.0 · **BY**), `horror-drones/is-anybody-home` (CC-BY-SA 3.0 · **BY**), `horror-drones/realization` (CC-BY-SA 3.0 · **BY**), `horror-drones/soled-bad-memory` (CC-BY-SA 4.0 · **BY**), `horror-drones/dark-factory` (CC-BY 3.0 · **BY**), `ambience/dark-ambiences` (CC0), `ambience/4-atmospheric-ghostly-loops` (CC0), `ambience/upside-down-grin-freaky-ambient` (CC0), `ambience/loopable-dungeon-ambience` (CC0), `music/mysterious-ambience-song21` (CC0), `music/kevin-macleod-incompetech` (CC-BY 4.0 · **BY**)

**빈 곳.** 드론은 넉넉하지만 전부 스테레오 단일 믹스라 레이어 분리(stem)가 없다. 위험도 레이어용 저역 펄스·고역 트레몰로를 따로 만들어야 한다. 엘리베이터(구11,6)·지하철(15) 운행 럼블 루프도 없다(audio.md '부족한 것').

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Dark Tension](https://artlist.io/royalty-free-music/song/dark-tension/3132) — Kyle Preston, 122 BPM
- [Horror in Your Eyes](https://artlist.io/royalty-free-music/song/horror-in-your-eyes/38155) — G-Yerro, 111 BPM
- [Fire and Ice](https://artlist.io/royalty-free-music/song/fire-and-ice/80862) — Alon Peretz, 89 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Dark ambient horror underscore, instrumental. Sub-bass drone, bowed metal and waterphone scrapes, low string clusters, a distant reversed piano note, sparse metallic hits far away. No melody, no drums, no orchestral hits. Tense, claustrophobic, slowly breathing. Free tempo with a faint 64 BPM heartbeat-like low pulse. Seamless loop, 30 seconds.
```

### F02 · 소리가 정보 — 음악 최소·덕킹

**컨셉.** #12 아무도 없는 학교에 말 걸기 · #16 빙판 탈출 · 구#32 얼음 아래 누군가 · #19 숨소리 도둑 · #61 메아리 지도 제작자

> **음악을 줄이거나 빼는 이유와 대체.** 의도적 음악 최소 가족. 대체 피드백: 16·구32 얼음 균열 핑·두드림(ice/*), 19 숨 3단계 루프(breathing-heartbeat/* 가공), 94 클릭·메아리(합성 IR), 24 마이크 입력 후 복도 잔향. 정보 소리가 곧 음악이다.

| 항목 | 방향 |
|---|---|
| 분위기 | 귀를 기울이게 하는 정적, 긴장된 청취 |
| 편성 | 음악은 거의 쓰지 않는다. 쓰더라도 20Hz~150Hz 서브 드론 한 줄(정보 대역 500Hz~8kHz를 비워 둠) |
| 템포 | 무박. 19번만 숨 리듬(분당 12~20회) 자체가 템포 |
| 루프 구조 | 루프 음악 없음. 결과 화면·장 전환에만 5~10초 스팅어 |
| 적응형 레이어 | 정보 소리(균열 16, 두드림 구32, 숨 19, 메아리 61, 복도 대답 12)가 날 때 드론 -12dB 사이드체인. 위험이 높아질수록 음악을 더하는 것이 아니라 환경음을 빼서 정보 소리를 드러낸다(역-레이어링). |
| 쓰지 말 것 | 중고역을 채우는 패드·선율, 리버브가 긴 음악(메아리·균열 판독을 망침), 리듬(19번 숨 리듬과 충돌) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`ice/ice-breakingshattering` (CC0), `ice/cracking-sounds` (CC-BY 4.0 · **BY**), `ice/41-snow-shoe-steps` (CC0), `breathing-heartbeat/breathing-tired` (CC0), `breathing-heartbeat/heartbeat-sounds` (CC0), `ambience/dripping-water-loop` (CC0), `ambience/wind1` (CC0), `horror-drones/wind` (CC0), `ambience/ambient-pulse-noise` (CC-BY-SA 3.0 · **BY**)

**빈 곳.** 음악 갭은 작다. 실제 갭은 소리 자산: 호수 얼음 '핑' 울림, 잔잔한 수면 호흡 루프, 동굴 IR, 학교 복도 룸톤(모두 audio.md '부족한 것').

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [The Unknown - Creative Cut - Minimal](https://artlist.io/royalty-free-music/song/the-unknown-creative-cut-minimal/137898) — KeMi, 60 BPM
- [In the Deep](https://artlist.io/royalty-free-music/song/in-the-deep/128988) — DaniHaDani, 60 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Extremely sparse sub-bass drone for a listening-based game, instrumental. Only frequencies below 150 Hz, a single slowly evolving low tone with subtle air noise, no melody, no rhythm, no mid or high frequencies, no reverb tails. Cold, attentive, quiet. Free tempo. Seamless loop, 30 seconds.
```

### F03 · 무음 지향·다이제틱 대체

**컨셉.** 구#40 버스가 오지 않는 저녁 · 구#47 당신의 박자(구현) · #33 냄새로 푸는 추리(구현) · #35 소리 없는 오케스트라(구현)

> **음악을 줄이거나 빼는 이유와 대체.** 구#47·#35 청각을 잃은 지휘자 — 음악이 없는 것이 규칙이다. #33 보이지도 들리지도 않는 방. 구#40 앉아 있기만 하는 저녁 — 환경음 체험. 대체: 시각 박·진동·폴리·환경음 레이어.

| 항목 | 방향 |
|---|---|
| 분위기 | 고요, 부재, 감각 박탈의 체험 그 자체 |
| 편성 | 음악 없음. 환경음·다이제틱 소리·(35·구47) 저역 진동감만 |
| 템포 | 해당 없음 — 구47·35는 화면 속 시각 박자가 템포(단원 호흡·지휘봉)이고 소리로 내지 않는다 |
| 루프 구조 | 루프 음악 없음. 엔딩 크레딧 1곡만 허용(선택) |
| 적응형 레이어 | 음악 대신 감각 치환: 35·구47은 박을 맞추면 화면 진동·조명 펄스·(선택) 60Hz 이하 촉각형 저음 한 번, 틀리면 단원 표정·활 흔들림. 33은 냄새 층을 벗길 때 들숨 폴리와 방 룸톤만 필터가 열리듯 변함. 구40은 시간대별 환경음(매미→저녁 새→귀뚜라미→버스 없는 도로)이 곧 진행 표시. |
| 쓰지 말 것 | 어떤 형태든 배경음악(이 컨셉의 핵심 규칙을 깬다), '감동' 피아노 삽입, 효과음으로 박자 알려 주기(구47·35) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`ambience/crickets-ambient-noise-loopable` (CC0), `ambience/high-traffic-road-sounds` (CC0), `ambience/wind1` (CC0), `ambience/forest-bird-sounds` (CC0), `ambience/rain-loopable` (CC0), `breathing-heartbeat/breathing-tired` (CC0), `audience/applause-in-a-large-hall-or-church` (CC0)

**빈 곳.** 음악 갭 없음(생성 불필요). 소리 갭: 콘서트홀 룸톤(35), 시골 정류장 저녁 필드 레코딩(구40), 들숨/냄새 맡기 폴리(33).

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Reset](https://artlist.io/royalty-free-music/song/reset/119902) — sero, 50 BPM
- [Distant Embers](https://artlist.io/royalty-free-music/song/distant-embers/97171) — Adi Goldstein, 82 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
(Not recommended to generate — this family is intentionally music-free.) Optional end-credits only: warm, almost silent ambient piece, single sustained felt-piano chord dissolving into room tone and distant evening field recording, no rhythm, no melody hook, 50 BPM feel. Instrumental, seamless loop, 30 seconds.
```

### F04 · 아날로그 라디오·다이제틱 방송

**컨셉.** #3 주파수 사냥 · 구#4 죽은 방송국 · 구#23 야간 방송 진행자

| 항목 | 방향 |
|---|---|
| 분위기 | 밤의 고독, 전파 너머의 사람, 흘러간 시대 |
| 편성 | 다이제틱 곡(1970~90s 가요풍 이지리스닝·재즈 발라드·방송 징글) + AM 필터·잡음·페이딩. 비다이제틱은 쓰지 않는다 |
| 템포 | 곡마다 70~110 BPM |
| 루프 구조 | 라디오 곡 2~3분 여러 개(루프 아님) + 방송 징글 3~5초 + 튜닝 사이 정적. 구4번은 20년 전 테이프처럼 열화된 판본 |
| 적응형 레이어 | 다이얼 위치 = 믹스: 주파수 일치도에 따라 곡의 대역폭(300Hz~3kHz → 전대역)과 잡음 비율이 연속 변화. 2번은 조난 신호 근처에서 곡이 사라지고 모스·음성이 드러남. 구23번은 사연 분위기에 따라 선곡이 바뀜(선곡 자체가 조작). |
| 쓰지 말 것 | 현대적 풀레인지 믹스를 필터 없이 틀기, 실존 가요 샘플(권리), 비다이제틱 BGM을 라디오 위에 덧씌우기 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`radio-static-noise/frequency-static-sound-effects` (CC0), `radio-static-noise/static` (CC0), `radio-static-noise/mysterious-radio-signal` (CC-BY 3.0 · **BY**), `radio-static-noise/radio-call` (CC-BY 4.0 · **BY**), `voice-publicdomain/mladytele1915` (Public Domain), `music/bossa-nova` (CC0), `music/a-cloudy-morning-jazz` (CC-BY 3.0 · **BY**), `music/kevin-macleod-incompetech` (CC-BY 4.0 · **BY**)

**빈 곳.** 잡음·신호는 충분. 비어 있는 것은 '방송에서 나오는 곡' 자체 — 한국 심야 라디오 분위기 곡과 방송국 징글이 없다. 곡 수가 많이 필요해 30초 루프 1개로는 해결되지 않는다(별도 배치 필요).

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Let the Good Times Roll - No Lead Vocals](https://artlist.io/royalty-free-music/song/let-the-good-times-roll-no-lead-vocals/64806) — Ofer Koren, 89 BPM
- [Don't Let the Summer End](https://artlist.io/royalty-free-music/song/dont-let-the-summer-end/93954) — The Magnetic Buzz, 93 BPM
- [Always Gallant Polka](https://artlist.io/royalty-free-music/song/always-gallant-polka/114931) — Victor Dance Orchestra, 123 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Late-night 1980s Asian city radio easy-listening ballad, instrumental. Electric piano, soft nylon guitar, gentle fretless bass, brushed drums, warm saxophone phrase. Nostalgic, lonely, intimate, as if heard through an old AM radio. 84 BPM. Seamless loop, 30 seconds.
```

### F05 · 터미널 글리치·사이버 신스

**컨셉.** #1 해킹 탈출 방 · 구#2 삭제 직전 로그

| 항목 | 방향 |
|---|---|
| 분위기 | 압박, 시간 초과, 기계 속 무언가가 되받아치는 불쾌함(1) / 꺼져 가는 존재와의 대화(구2) |
| 편성 | 비트크러시 신스 베이스, 16분음 아르페지에이터, 글리치 퍼커션(데이터 잡음), CRT 험, 구2번은 사인파 패드와 느린 벨 |
| 템포 | 1: 120~140 BPM / 구2: 무박~70 BPM |
| 루프 구조 | 1: 8마디 인트로 → 16마디 루프 A/B → 성공/실패 스팅어(2초) / 구2: 90초 패드 루프 → 삭제 카운트다운 구간 변주 |
| 적응형 레이어 | 1: 남은 시간 30%마다 레이어 추가(하이햇→베이스 옥타브→리드). 유령이 명령을 되받아칠 때 템포 순간 역재생/스터터. 구2: 남은 시간에 따라 비트레이트를 낮춰 음악이 점점 부서짐(샘플레이트 44.1k→8k). |
| 쓰지 말 것 | 신스웨이브 향수(밝은 80s 리드), EDM 드롭, 할리우드 해커 클리셰 보컬 찹 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`sfx-ui-typing/glitch-music` (CC0), `music/cyberpunk-moonlight-sonata` (CC0), `ambience/sci-fi-drone-loop` (CC-BY 3.0 · **BY**), `ambience/sci-fi-background-noise` (CC0), `kenney-audio/sci-fi-sounds` (CC0-1.0), `kenney-audio/digital-audio` (CC0-1.0)

**빈 곳.** 글리치 곡 1개(CC0)와 사이버펑크 편곡 1개뿐 — 템포가 맞는 레이어 세트가 없다. 구2번용 느린 패드도 없다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Glitch Flop](https://artlist.io/royalty-free-music/song/glitch-flop/126766) — Flint, 110 BPM
- [Broken Radios](https://artlist.io/royalty-free-music/song/broken-radios/603) — Stanley Gurvich, 96 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Tense glitch electronic hacking underscore, instrumental. Bitcrushed synth bass, fast 16th-note arpeggiator, glitchy data-noise percussion, CRT hum, stuttering edits. Pressured, cold, digital, no vocals, no EDM drop. 128 BPM. Seamless loop, 30 seconds.
```

### F06 · 모던 수사·미니멀 전자 펄스

**컨셉.** #18 테이프 감식 · 구#41 잔향 감식실(구현) · 구#43 잠금 해제 불가(구현) · #32 죽은 사람의 휴대폰(구현)

| 항목 | 방향 |
|---|---|
| 분위기 | 집중, 냉정한 분석, 드러나는 진실의 무게 |
| 편성 | 뮤트 피아노 반복음, 아날로그 신스 펄스(8분음), 저역 패드, 가벼운 틱 퍼커션(시계·로그), 18번은 테이프 모터음을 리듬 요소로 |
| 템포 | 90~110 BPM |
| 루프 구조 | 인트로 4마디 → 루프 A(조사) 16마디 / 루프 B(단서 결합) 16마디 → 결론 스팅어(판정 맞음/틀림 두 벌) |
| 적응형 레이어 | 증거 수에 따라 레이어 추가(펄스→베이스→피아노 모티프). 잘못된 추론·모순 발견 시 펄스가 반박자 어긋남. 구43·32는 전화 알림이 올 때 음악 전체 로우패스. |
| 쓰지 말 것 | 트루크라임 다큐 클리셰(과장된 붐), 밝은 코퍼릿 피아노, 공포 드론(장르가 호러로 기움) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/cyberpunk-moonlight-sonata` (CC0), `music/sleep-talking-loop-fantasy-rpg-sci-fi` (CC0), `music/mysterious-ambience-song21` (CC0), `music/kevin-macleod-incompetech` (CC-BY 4.0 · **BY**), `foley-props/tape-recorder-opening-and-closing-sound-effects` (CC0), `foley-props/equipment-clicks-iii` (CC0)

**빈 곳.** 수사물용 펄스 곡이 없다. 구현된 구41·구43·32가 이 가족이라 격차 체감이 가장 크다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Empty Rooms](https://artlist.io/royalty-free-music/song/empty-rooms/123460) — Gal Lev, 102 BPM
- [Free Radicals](https://artlist.io/royalty-free-music/song/free-radicals/36010) — Stanley Gurvich, 127 BPM
- [Was It All a Dream?](https://artlist.io/royalty-free-music/song/was-it-all-a-dream/8649) — Jimmy Svensson, 90 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Minimal modern investigation underscore, instrumental. Muted repeating piano notes, analog synth eighth-note pulse, low warm pad, light ticking percussion like a clock. Focused, cool, analytical, slowly building. No big hits, no horror. 100 BPM. Seamless loop, 30 seconds.
```

### F07 · 테이프 루프·열화 노스탤지어

**컨셉.** 구#8 찍히지 않은 사람 · 구#26 한 사람의 유품 · #30 종점 · 구#36 늘어진 테이프

| 항목 | 방향 |
|---|---|
| 분위기 | 바랜 사진, 사라진 사람, 기억이 닳는 소리 |
| 편성 | 업라이트 피아노/일렉트릭 피아노 짧은 프레이즈를 테이프 루프로(워우·플러터·히스), 멀리 있는 현 패드, 빈티지 오르간 한 음 |
| 템포 | 55~75 BPM(루프 길이가 템포) |
| 루프 구조 | 4~8마디 테이프 루프 하나를 반복하며 열화 단계 3~4개(원본→약한 워우→심한 드롭아웃→거의 잡음) |
| 적응형 레이어 | 진행도 = 열화도. 구36번은 재생할수록 루프가 늘어져 사라짐(피치 -1~-3% 누적, 드롭아웃 증가). 구8번은 빠진 한 사람에 다가갈수록 루프에서 한 음이 빠짐. 구30번 종점은 역 정차마다 루프가 조금씩 느려짐. |
| 쓰지 말 것 | 깨끗한 하이파이 피아노, 로파이 힙합 드럼(구26·구30의 슬픔을 '칠'로 바꿈), 감상적 현악 크레셴도 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/november-snow` (CC0), `music/calm-bgm` (CC-BY 3.0 · **BY**), `music/soliloquy` (CC-BY 3.0 · **BY**), `foley-props/vinyl` (CC0), `foley-props/commons-projector-camera-tape` (mixed per file: CC BY 4.0, CC BY-SA 3.0, Public domain · **BY**), `ambience/reversing-time-stuck-in-time` (CC0)

**빈 곳.** 원곡(피아노 짧은 루프)은 라이브러리 곡에서 잘라 쓸 수 있고 열화는 DSP로 만든다 — 생성 필요도가 중간. 다만 BY 곡을 가공하면 크레딧 의무는 남는다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Lost Tapes](https://artlist.io/royalty-free-music/song/lost-tapes/97169) — Adi Goldstein, 58 BPM
- [Vapor Fingers](https://artlist.io/royalty-free-music/song/vapor-fingers/80356) — Tamuz Dekel, 68 BPM
- [Dots](https://artlist.io/royalty-free-music/song/dots/116729) — Adi Goldstein, 59 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Nostalgic degraded tape-loop ambient, instrumental. A short upright piano phrase repeating on worn cassette tape with wow and flutter, tape hiss, occasional dropouts, a distant soft string pad. Melancholic, faded, tender, memory slowly disappearing. 64 BPM. Seamless loop, 30 seconds.
```

### F08 · 상실·돌봄 미니멀 피아노

**컨셉.** 구#18 보내지 못한 것 · #11 시력을 잃는 하루 · 구#28 마지막 컷 · 구#38 간병 · 구#49 마지막 이삿날(구현) · #36 철거 직전 건물의 마지막 세입자들(구현)

> **음악을 줄이거나 빼는 이유와 대체.** 구38번 간병은 긴 무음 구간을 허용(숨소리·시계·냉장고 험이 주인공). 음악은 새벽 전환에만.

| 항목 | 방향 |
|---|---|
| 분위기 | 조용한 슬픔, 곁에 있음, 이별 직전의 일상 |
| 편성 | 펠트 피아노(해머 소리·페달 노이즈 포함), 첼로 한 줄, 가끔 어쿠스틱 기타 하모닉스, 창밖 비·방 룸톤과 섞이는 믹스 |
| 템포 | 60~80 BPM |
| 루프 구조 | 인트로 없음 → 60~90초 루프(선율 A) + 변주 A' → 장 끝 2~4마디 코다(결말 분기별 조성 다름) |
| 적응형 레이어 | 11번: 눈 뜨는 시간이 줄수록 악기가 하나씩 빠지고 마지막엔 피아노 한 음. 구38번: 부모의 숨이 고르면 첼로 등장, 불안정하면 피아노만. 구49·36번: 도운 집 수만큼 화성이 열림(단조→병행장조). 구18번: 40일 진행에 따라 선율이 짧아짐. |
| 쓰지 말 것 | 웅장한 현악 오케스트라, 드럼, '감동 유도' 크레셴도, 과한 리버브 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/soliloquy` (CC-BY 3.0 · **BY**), `music/calm-bgm` (CC-BY 3.0 · **BY**), `music/november-snow` (CC0), `music/snowfall` (CC0), `music/one` (CC-BY 3.0 · **BY**), `music/death-is-just-another-path` (CC-BY 3.0 · **BY**), `ambience/rain-loopable` (CC0), `ambience/clock-tick-0` (CC0), `ambience/fridge-loop-1` (CC0)

**빈 곳.** 잔잔한 피아노 곡 후보는 있지만 CC0는 2곡(november-snow, snowfall)뿐이고 악기 단위 레이어가 없다. 결말 분기 코다도 없다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Gnossienne No. 1](https://artlist.io/royalty-free-music/song/gnossienne-no-1/87674) — Bishara Haroni, 78 BPM
- [Frozen Lake](https://artlist.io/royalty-free-music/song/frozen-lake/102468) — Nsee, 123 BPM
- [Goodbye](https://artlist.io/royalty-free-music/song/goodbye/81740) — 8opus, 123 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Intimate minimal felt piano, instrumental. Soft felt-muted upright piano with audible hammers and pedal noise, a single sustained cello line entering halfway, lots of space and silence between phrases. Quiet grief, tenderness, late night at home. No drums, no strings swell. 68 BPM. Seamless loop, 30 seconds.
```

### F09 · 서재·편지 실내악 (펜과 활자)

**컨셉.** #9 펜팔 추리 · 구#48 붉은 펜으로 남긴 것(구현) · #26 소모되는 활자(구현) · #34 원고 되돌려 보내기(구현)

| 항목 | 방향 |
|---|---|
| 분위기 | 사색, 문장을 고르는 긴장, 종이 냄새 |
| 편성 | 현악 3~4중주(피치카토+레가토), 클라리넷 또는 바순, 업라이트 피아노, 26번은 인쇄기·타자기 리듬을 퍼커션으로 |
| 템포 | 72~96 BPM |
| 루프 구조 | 인트로 2마디 → 루프 A(읽기) / 루프 B(고치기·인쇄, 피치카토 밀도↑) → 반송·완성 스팅어 |
| 적응형 레이어 | 편집 조작(삭제·삽입·이동, 구48·34)마다 피치카토 한 음을 박자에 양자화해 붙임 — 손맛이 음악에 섞임. 26번은 남은 인쇄 횟수가 줄수록 현이 하나씩 빠짐. 9번은 상대 거짓말 판정에 따라 조성 흔들림. |
| 쓰지 말 것 | 바로크 패스티시 과잉, 코믹 피치카토(가볍게 무너짐), 전자음 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/calm-bgm` (CC-BY 3.0 · **BY**), `music/soliloquy` (CC-BY 3.0 · **BY**), `foley-props/pencil-sounds` (CC0), `foley-props/writing-scribbles` (CC-BY-SA 4.0 · **BY**), `foley-props/page-turning-sfx-sound-effect` (CC-BY 4.0 · **BY**), `sfx-ui-typing/typewriter-sounds` (CC0), `foley-props/100-cc0-metal-and-wood-sfx` (CC0)

**빈 곳.** 실내악 곡이 전혀 없다. 구현된 구48·26·34 세 개가 이 가족.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Morning Dance](https://artlist.io/royalty-free-music/song/morning-dance/7704) — Yoav Ilan, 111 BPM
- [Life's Sweetness - Strings & Piano](https://artlist.io/royalty-free-music/song/lifes-sweetness-strings-piano/37304) — Patrick Ussher, 96 BPM
- [My Rose](https://artlist.io/royalty-free-music/song/my-rose/129110) — Asi Mandel, 103 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Intimate chamber music for a writing desk, instrumental. String quartet alternating gentle pizzicato and legato phrases, a warm clarinet line, soft upright piano. Thoughtful, literary, quietly tense, like choosing the right sentence. 84 BPM. Seamless loop, 30 seconds.
```

### F10 · 천상 관료제 (하프·첼레스타·허밍)

**컨셉.** 구#50 기적 배정과(구현) · #38 신의 비서(구현)

| 항목 | 방향 |
|---|---|
| 분위기 | 신성하지만 사무적인, 은근한 유머와 대가의 무게 |
| 편성 | 콘서트 하프, 첼레스타, 허밍 합창 패드(가사 없음), 피치카토 현, 작은 핸드벨, 도장 소리를 퍼커션으로 |
| 템포 | 84~100 BPM |
| 루프 구조 | 루프 A(서류 처리) 16마디 → 루프 B(대가 발생, 저역 추가) → 결재 스팅어(종 한 번) |
| 적응형 레이어 | 들어준 기도 수만큼 합창 레이어 두꺼워짐, 다른 곳에서 대가가 빠질 때 저역 현이 반음 내려감(구50번 하류 확인 시). |
| 쓰지 말 것 | 교회 오르간·그레고리안 성가(특정 종교 연상), 에픽 합창 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/mystical-theme` (CC-BY 3.0 · **BY**), `music/the-field-of-dreams` (CC0), `ambience/water-harp` (CC-BY-SA 3.0 · **BY**), `foley-props/church-bell` (CC-BY-SA 3.0 · **BY**), `foley-props/point-bell` (CC0)

**빈 곳.** 하프·합창 루프 없음. water-harp는 BY-SA라 가공 시 파생물도 BY-SA.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Fairy Dreams](https://artlist.io/royalty-free-music/song/fairy-dreams/12559) — Ian Post, 70 BPM
- [La Source, Op. 44](https://artlist.io/royalty-free-music/song/la-source-op-44/116818) — Ada Ragimov, 74 BPM
- [Magical Tales](https://artlist.io/royalty-free-music/song/magical-tales/135556) — Ian Post, 121 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Celestial bureaucracy underscore, instrumental. Concert harp arpeggios, celesta, wordless humming choir pad, light pizzicato strings, small handbells. Heavenly yet orderly and slightly playful, like an office in the clouds. No organ, no epic choir. 92 BPM. Seamless loop, 30 seconds.
```

### F11 · 노이르 재즈·야간 순찰

**컨셉.** #4 사진 속 범인 · #10 연결하면 안 되는 통화 · #17 틀린 그림 순찰 · 구#34 자리를 바꾸는 것들 · #43 그림자 경매

| 항목 | 방향 |
|---|---|
| 분위기 | 담배 연기, 흑백 사진, 밤의 전시실, 속삭이는 경매장 |
| 편성 | 뮤트 트럼펫/테너 색소폰, 업라이트 베이스 워킹, 브러시 드럼, 재즈 피아노 보이싱, 17·구34는 비브라폰·피치카토 '살금살금' |
| 템포 | 60~90 BPM(스윙 필) |
| 루프 구조 | 인트로 4마디 → 루프 A(조사·순찰) 16마디 → 루프 B(용의자 좁힘·경매 호가) → 지목/낙찰 스팅어 |
| 적응형 레이어 | 17·구34: 바뀐 전시물을 놓칠수록 베이스만 남기고 드럼 빠짐 → 구34번 예술안은 점점 조가 틀어짐(4분의 1음 디튠). 43: 호가가 오를수록 피아노 컴핑 밀도↑. 10: 금지 통화 연결 시 음악 전체 전화선 필터. |
| 쓰지 말 것 | 빅밴드 스윙(너무 밝음), 스무스 재즈, 전자 비트 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/a-conversation-with-saul-jazzblues-shuffle` (CC-BY 3.0 · **BY**), `music/trouble-makers-coolriff-jazz` (CC-BY 3.0 · **BY**), `music/a-cloudy-morning-jazz` (CC-BY 3.0 · **BY**), `music/deliciously-sour` (CC-BY 3.0 · **BY**), `music/kevin-macleod-incompetech` (CC-BY 4.0 · **BY**), `ambience/ventilation-version2` (CC-BY 3.0 · **BY**), `ambience/clock-ticking` (CC-BY 3.0 · **BY**)

**빈 곳.** 재즈 곡은 있으나 전부 CC-BY(Matthew Pablo 3곡 포함) — 크레딧 의무. 살금살금 순찰용 비브라폰·피치카토 곡은 없다. 생성 우선순위 낮음.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Keep Your Head Down](https://artlist.io/royalty-free-music/song/keep-your-head-down/127840) — The Magnetic Buzz, 68 BPM
- [Dusk](https://artlist.io/royalty-free-music/song/dusk/122475) — Rotem Cinamon, 69 BPM
- [Creeping up on Tiptoes](https://artlist.io/royalty-free-music/song/creeping-up-on-tiptoes/109457) — Veaceslav Draganov, 100 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Film noir detective jazz, instrumental. Muted trumpet, walking upright bass, brushed drums, smoky jazz piano voicings, soft vibraphone. Night-time, mysterious, slow swing feel, cigarette smoke and black-and-white photographs. 72 BPM. Seamless loop, 30 seconds.
```

### F12 · 보드빌·래그타임·서커스 칼리오페

**컨셉.** 구#9 인형극 대결 · 구#19 교환대 타이쿤 · 구#98 마지막 순회 서커스

| 항목 | 방향 |
|---|---|
| 분위기 | 왁자한 무대, 분주한 교환대, 마지막 순회의 쓸쓸한 흥 |
| 편성 | 래그타임 피아노, 튜바·트롬본, 스네어·심벌, 칼리오페/증기 오르간(구98), 밴조, 클라리넷 |
| 템포 | 100~140 BPM |
| 루프 구조 | 인트로 2마디 → 루프 A 16마디 → 루프 B(고조) → 박수·실패 스팅어. 구98은 도시별 변주(같은 선율, 편성만 교체) |
| 적응형 레이어 | 구9: 관객 호응 게이지가 오르면 편성 추가·템포 +5%. 구19: 대기 통화 수 = 피아노 오른손 밀도, 잘못 연결 시 한 박 삐끗(오음). 구98: 단원이 빠질 때마다 악기가 하나씩 사라지고 마지막 공연은 칼리오페 혼자. |
| 쓰지 말 것 | 공포 서커스 클리셰(디튠 오르골), 현대 팝 드럼 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/circus-dilemma` (CC-BY 3.0 · **BY**), `music/in-the-circus-psg-version` (CC-BY 3.0 · **BY**), `music/kevin-macleod-incompetech` (CC-BY 4.0 · **BY**), `music/childrens-march-theme` (CC0), `audience/applause-in-a-large-hall-or-church` (CC0), `audience/free-crowd-cheering-sounds` (CC-BY 4.0 · **BY**), `foley-props/commons-telephone` (mixed per file: CC BY 3.0, CC BY 4.0, CC BY-SA 3.0, CC BY-SA 4.0, CC0, Public Domain, Public domain · **BY**)

**빈 곳.** 서커스·코미디 곡은 충분(대부분 BY). 교환대 시대 래그타임과 칼리오페 단독 판본이 없다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [It's a Slippery Slope](https://artlist.io/royalty-free-music/song/its-a-slippery-slope/118475) — Yonatan Riklis, 90 BPM
- [That Rag Really Tied the Room Together - Piano Version](https://artlist.io/royalty-free-music/song/that-rag-really-tied-the-room-together-alternative-piano-version/76947) — Ziv Grinberg, 108 BPM
- [Can't Look Down](https://artlist.io/royalty-free-music/song/cant-look-down/50982) — Ty Simon, 70 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Vintage vaudeville circus ragtime, instrumental. Bouncy ragtime piano, oom-pah tuba, steam calliope organ melody, clarinet runs, snare and cymbal hits. Lively, theatrical, a little bittersweet. 120 BPM. Seamless loop, 30 seconds.
```

### F13 · 오르골·태엽·미니어처

**컨셉.** #5 마지막 공연 · #79 개미 고고학 · #52 시계탑 수리공 · #58 인형의 집 탐정

| 항목 | 방향 |
|---|---|
| 분위기 | 작고 정교한 세계, 태엽의 시간, 늙은 손의 떨림 |
| 편성 | 오르골(뮤직 박스), 첼레스타, 피치카토 현, 클라비코드, 시계 틱·톱니를 퍼커션으로(52), 칼림바(49) |
| 템포 | 3/4 왈츠 84~110 BPM(52는 60 BPM 초침과 동기) |
| 루프 구조 | 루프 A 16마디(왈츠) → 태엽 풀림 구간(템포 하강) → 태엽 감기 스팅어 |
| 적응형 레이어 | 5: 인형사의 손 떨림 = 오르골 음정 흔들림·박 흔들림. 52: 시계가 틀린 만큼 음악 템포가 어긋나고, 톱니를 맞추면 초침과 박이 정확히 동기. 58: 벽을 떼면 방별로 다른 악기(부엌=칼림바 등). 49: 지하 깊이만큼 저역 추가. |
| 쓰지 말 것 | 공포 오르골(디튠·역재생, F01과 혼동), 신스 패드 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/sleep-talking-loop-fantasy-rpg-sci-fi` (CC0), `music/school-of-quirks` (CC-BY 3.0 · **BY**), `ambience/clock-wind-sounds` (CC0), `ambience/ticking-clock` (CC0), `ambience/tick-and-tock` (CC0), `ambience/steam-boiler-sound-loop` (CC0), `foley-props/4-metal-dingsrings` (CC0)

**빈 곳.** 오르골·왈츠 곡이 없다. 시계 소리는 넉넉하다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Music Box Waltz](https://artlist.io/royalty-free-music/song/music-box-waltz/9460) — Alon Peretz, 110 BPM
- [Dance of the Sugar Plum Fairy](https://artlist.io/royalty-free-music/song/dance-of-the-sugar-plum-fairy/120139) — Kashido, 96 BPM
- [The House with the Purple Windows](https://artlist.io/royalty-free-music/song/the-house-with-the-purple-windows/108532) — Roie Shpigler, 83 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Delicate clockwork miniature waltz, instrumental. Music box melody, celesta, light pizzicato strings, clavichord, soft clock ticks and gear clicks as percussion. Intricate, tender, tiny world, gently nostalgic. 3/4 time, 96 BPM. Seamless loop, 30 seconds.
```

### F14 · 동화 어쿠스틱 (연필·이불·눈사람)

**컨셉.** #50 지우개 모험 · #54 이불 요새 · 구#93 눈사람의 겨울

| 항목 | 방향 |
|---|---|
| 분위기 | 아이의 상상, 포근함, 조금 무서운 밤과 녹는 봄 |
| 편성 | 글로켄슈필, 첼레스타, 목관(플루트·바순), 우쿨렐레, 토이 피아노, 54는 밤 전투용 팀파니·저역 현 추가 |
| 템포 | 80~120 BPM |
| 루프 구조 | 낮(설계/놀이) 루프 → 밤(방어·위기) 루프 같은 선율 단조 편곡 → 승리·아침 스팅어 |
| 적응형 레이어 | 54: 괴물 수에 따라 팀파니·저역 현 추가, 요새 붕괴 시 토이피아노만. 구93: 녹을수록(기온) 템포가 느려지고 음정이 아래로 흐름. 50: 지운 지형만큼 선율 음이 빠짐(지운 것은 돌아오지 않는다 = 음도 돌아오지 않는다). |
| 쓰지 말 것 | 디즈니식 풀 오케스트라, 크리스마스 캐럴 클리셰(구93), 칩튠 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/childrens-march-theme` (CC0), `music/snowland-town` (CC-BY 3.0 · **BY**), `music/snowfall` (CC0), `music/the-field-of-dreams` (CC0), `footsteps/walking-on-snow-sound` (CC0), `foley-props/pencil-sounds` (CC0)

**빈 곳.** 동화풍 CC0 곡은 childrens-march-theme 정도. 낮/밤 한 쌍 편곡이 없다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Secret Garden](https://artlist.io/royalty-free-music/song/secret-garden/12558) — Ian Post, 92 BPM
- [The Kid and the Bird](https://artlist.io/royalty-free-music/song/the-kid-and-the-bird/132824) — SEA, 69 BPM
- [A Christmas Adventure](https://artlist.io/royalty-free-music/song/a-christmas-adventure/76366) — Zac Nelson, 89 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Cozy storybook acoustic, instrumental. Glockenspiel, celesta, toy piano, gentle flute and bassoon, soft ukulele strums. Warm, childlike imagination, bedtime adventure, a hint of mischief. No big orchestra. 100 BPM. Seamless loop, 30 seconds.
```

### F15 · 목가 어쿠스틱 파밍 (계절 한 벌)

**컨셉.** #21 밭이 줄어드는 세계(구현) · #22 되돌릴 수 있는 한 해(구현) · #23 작물이 정보다(구현) · #27 수확하는 덱(구현) · #29 덱을 심는다(구현) · #64 거인의 등 위 마을

| 항목 | 방향 |
|---|---|
| 분위기 | 흙냄새, 계절의 순환, 잃어 가는 땅에 대한 애틋함(21), 거인의 걸음(64) |
| 편성 | 어쿠스틱 기타 핑거피킹, 만돌린, 피들, 틴휘슬/플루트, 업라이트 베이스, 64은 저역 팀파니·호른으로 '걸음' |
| 템포 | 80~110 BPM |
| 루프 구조 | 계절 4벌(봄·여름·가을·겨울) 같은 선율 다른 편성, 각 60~90초 루프 + 수확·결산 스팅어 + 겨울 밤 전투(27·28 연결) |
| 적응형 레이어 | 21: 침식이 진행될수록 악기 하나씩 빠지고 파도·균열 효과음이 들어옴(세계가 줄면 음악도 준다). 22: 되감을 때마다 테이프 역재생 스윕, 토질이 깎인 만큼 기타 줄 하나가 디튠. 23: 시세 급등락 시 피들 스타카토. 64: 거인 걸음 박(40~50 BPM 저역)에 곡 템포 동기. |
| 쓰지 말 것 | 컨트리 팝 드럼, 신스, 과한 켈틱 지그(모든 계절이 축제가 됨) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/the-field-of-dreams` (CC0), `music/forest-ambience` (CC0), `music/free-music-pack` (CC0), `music/shop-theme` (CC0), `ambience/forest-bird-sounds` (CC0), `ambience/birdcricketfrog-and-mosquito-sounds` (CC0), `ambience/nature-sounds-pack` (CC-BY 4.0 · **BY**), `ambience/gull-sounds` (CC-BY-SA 3.0 · **BY**)

**빈 곳.** 파밍 곡으로 쓸 CC0 곡이 몇 개 있지만 계절 한 벌·단계적 레이어가 없다. 구현 6개 중 21은 유일하게 Unity 셸이 있어 가장 먼저 필요하다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Lantern Light](https://artlist.io/royalty-free-music/song/lantern-light/136706) — Ziv Moran, 124 BPM
- [Keep Me in Mind](https://artlist.io/royalty-free-music/song/keep-me-in-mind/56942) — Elad Perez, 95 BPM
- [Far - Instrumental version](https://artlist.io/royalty-free-music/song/far-instrumental-version/14935) — The Hunts, 105 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Pastoral acoustic farming music, instrumental. Fingerpicked acoustic guitar, mandolin, gentle fiddle, tin whistle melody, upright bass. Warm, earthy, hopeful with a touch of melancholy, early autumn fields. No drums kit, no synths. 96 BPM. Seamless loop, 30 seconds.
```

### F16 · 바다·항해 포크 (물때·폭풍·향신료)

**컨셉.** #42 조수 간만 우체부 · #48 폭풍의 등대지기 · #55 향신료 항로

| 항목 | 방향 |
|---|---|
| 분위기 | 짠바람, 물때를 기다리는 시간, 폭풍 속 등대의 긴장 |
| 편성 | 아코디언, 틴휘슬, 어쿠스틱 기타, 콘트라베이스, 프레임 드럼. 55은 우드(oud)·다르부카 색채, 48 폭풍 레이어는 저역 현·팀파니 |
| 템포 | 84~120 BPM(6/8 가능) |
| 루프 구조 | 잔잔 루프(42 썰물·55 항해) / 폭풍 루프(48) — 같은 조성, 폭풍은 추가 레이어로 교차 페이드 |
| 적응형 레이어 | 42: 물때표의 수위 = 음악 필터(만조일수록 저역 먹먹). 48: 파고·암초 근접도에 따라 현 트레몰로·팀파니, 배를 무사히 인도하면 해소 코드. 55: 선창의 향 섞임에 따라 악기 색이 바뀜(향 = 음색). |
| 쓰지 말 것 | 해적 영화 에픽 브라스, 레게/열대풍, 과한 뱃노래 합창 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`ambience/gull-sounds` (CC-BY-SA 3.0 · **BY**), `ambience/storm-arwen-2022` (CC-BY 4.0 · **BY**), `ambience/rain-and-thunder-loop` (CC-BY 3.0 · **BY**), `ambience/ship-sinking` (CC0), `ambience/wind1` (CC0), `music/rain-and-thunders` (CC0), `ambience/skippy-fish-water-sound-collection` (CC0)

**빈 곳.** 바다 환경음은 있으나 항해 음악은 0곡.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [The Seven Seas](https://artlist.io/royalty-free-music/song/the-seven-seas/97388) — Zac Nelson, 84 BPM
- [Drunken Sailor - Instrumental version](https://artlist.io/royalty-free-music/song/drunken-sailor-instrumental-version/114678) — HillTopTrio, 115 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Coastal seafaring folk, instrumental. Accordion melody, tin whistle, fingerpicked acoustic guitar, double bass, soft frame drum, gentle 6/8 sway. Salty breeze, patient tide, quiet island life with distant adventure. No epic brass, no vocals. 92 BPM. Seamless loop, 30 seconds.
```

### F17 · 수중 앰비언트

**컨셉.** #8 수면 아래 스텔스 · 구#16 익사한 집 · #65 먹물 문어

| 항목 | 방향 |
|---|---|
| 분위기 | 먹먹함, 굴절된 빛, 위에서 내려다보는 그림자 |
| 편성 | 로우패스 신스 패드, 물 하프·유리 하모닉스, 느린 벨, 고래 울음 같은 저역 글라이드, 65는 마림바·피치카토로 '먹물 그리기' 리듬 |
| 템포 | 무박~70 BPM(65는 100~110 BPM) |
| 루프 구조 | 90초 패드 루프 2벌(얕은 물·깊은 물) + 발각/포식자 스팅어 |
| 적응형 레이어 | 8: 위층 경비 그림자가 가까울수록 고역이 더 깎이고 저역 펄스 추가. 구16: 수면에 가까울수록 필터가 열림. 65: 포식자 거리 = 퍼커션 밀도, 속임 성공 시 벨 한 음. |
| 쓰지 말 것 | 해양 다큐 에픽, 밝은 트로피컬, 고역 반짝이 과다(물속 느낌이 깨짐) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`ambience/bubble-sound-effects` (CC0), `ambience/underwater-or-space-engine-rumble` (CC0), `ambience/water-harp` (CC-BY-SA 3.0 · **BY**), `ambience/dripping-water-loop` (CC0), `ambience/skippy-fish-water-sound-collection` (CC0)

**빈 곳.** 수중 음악 0곡, 실제 하이드로폰 녹음도 없음.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Unconscious Mind (Delta)](https://artlist.io/royalty-free-music/song/unconscious-mind-delta/80888) — Yotam Agam, 120 BPM
- [Gazing Wide](https://artlist.io/royalty-free-music/song/gazing-wide/67575) — Tamuz Dekel, 66 BPM
- [Deep Blue](https://artlist.io/royalty-free-music/song/deep-blue/137135) — YahavK, 134 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Submerged underwater ambient, instrumental. Heavily low-passed warm synth pads, glass harmonics and water-harp tones, slow distant bells, low whale-like gliding tones, muffled and refracted. Calm but uneasy, light filtering from above. No drums. Free tempo, 60 BPM feel. Seamless loop, 30 seconds.
```

### F18 · 국악·조선 산수

**컨셉.** #7 사라지는 언어의 마지막 화자 · #45 봉화 네트워크 · #67 이름 없는 신당

| 항목 | 방향 |
|---|---|
| 분위기 | 수묵의 여백, 산 능선의 봉화, 잊힌 신들의 신당 |
| 편성 | 가야금, 거문고, 대금·단소, 해금, 장구·북, 67은 징·방울·구음(무가풍, 가사 없음), 45는 대북·나각 |
| 템포 | 진양조~중모리 느낌 40~90 BPM(장단은 12/8·6/8 계열) |
| 루프 구조 | 7: 여백 많은 산조풍 즉흥 루프 / 45: 봉화 연결 단계마다 북 패턴 추가 / 67: 참배 시간대별 3벌(새벽·낮·밤) + 신 소멸 스팅어 |
| 적응형 레이어 | 7: 뜻을 하나 잊을 때마다 선율에서 한 음(시김새)이 사라짐. 45: 봉화가 수도로 전달될수록 북 레이어가 쌓이고, 적 침입 경보는 나각. 67: 참배객이 기억하는 만큼 해당 신의 악기가 크게 들림(신 = 악기), 잊히면 그 악기가 빠짐. |
| 쓰지 말 것 | 중국 5음계 클리셰(얼후·구정 사운드), 일본 고토·샤쿠하치 색, 퓨전 EDM 국악, 무속을 호러로 소비하는 연출 |

**라이브러리 후보.** 없음.

**빈 곳.** 라이브러리에 국악이 0곡. Artlist 카탈로그에서도 한국 전통음악은 찾지 못했다('korean' 검색 = K-pop, 'gayageum' 검색 = 중국·일본 곡). 생성 또는 국립국악원 등 공공 음원(라이선스 확인 필요)이 유일한 경로.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Xiang Fei Lei (질감 참고만 — 중국 구정)](https://artlist.io/royalty-free-music/song/xiang-fei-lei/60293) — Annie Zhou, 86 BPM
- [Tamura's Journey (질감 참고만 — 일본풍)](https://artlist.io/royalty-free-music/song/tamuras-journey/114581) — Borden Lulu, 64 BPM
- [Mountainous Coast Planning (산수 공간감 참고)](https://artlist.io/royalty-free-music/song/mountainous-coast-planning/136952) — Inon Zur, 106 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Korean traditional gugak ambient, instrumental. Gayageum plucked melody with expressive bends, daegeum bamboo flute breathy phrases, haegeum long tones, soft janggu hourglass drum in a slow 12/8 jinyang-style rhythm, lots of empty space like an ink-wash landscape. Serene, ancient, contemplative. Avoid Chinese or Japanese instruments. 60 BPM. Seamless loop, 30 seconds.
```

### F19 · 전쟁과 조약 — 저현 오스티나토

**컨셉.** 구#42 휴전의 문장(구현) · #30 능선 초소(구현) · #31 속삭이는 지도(구현) · #37 번역가의 전쟁(구현)

| 항목 | 방향 |
|---|---|
| 분위기 | 말 한 마디의 무게, 매복 직전의 정적, 해질 무렵 능선 |
| 편성 | 저역 현 오스티나토(첼로·콘트라베이스), 스네어 롤·군악 림샷(30·31), 피아노 저음, 솔로 바이올린/비올라, 37은 해금·대금 한 줄로 동서 양측 색 |
| 템포 | 70~100 BPM |
| 루프 구조 | 정찰/협상 루프(오스티나토만) → 실행/결렬 루프(타악 추가) → 조약 체결·매복 성공/실패 스팅어 |
| 적응형 레이어 | 구42·37: 상대 긴장도 게이지 = 오스티나토 밀도·음역, 오역이 확정되면 불협 한 화음. 30: 순찰 감시 공백 타임라인과 스네어 패턴 동기(감시 중 = 림샷, 공백 = 무음). 31: 신뢰를 쓸수록 선율이 가늘어짐. |
| 쓰지 말 것 | 할리우드 전쟁 에픽(합창·브라스 팡파르), 영웅적 행진곡, 특정 국가 국가/군가 인용 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/death-is-just-another-path` (CC-BY 3.0 · **BY**), `music/kevin-macleod-incompetech` (CC-BY 4.0 · **BY**), `ambience/wind1` (CC0), `ambience/crickets-ambient-noise-loopable` (CC0), `ambience/clock-ticking` (CC-BY 3.0 · **BY**), `footsteps/42-snow-and-gravel-footsteps` (CC0)

**빈 곳.** 긴장 오스티나토 곡이 없다. 구현 4개 모두 이 가족.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [A Dangerous Plan](https://artlist.io/royalty-free-music/song/a-dangerous-plan/34082) — Martin Puehringer, 102 BPM
- [Final Confrontation](https://artlist.io/royalty-free-music/song/final-confrontation/124304) — Matthias Förster, 63 BPM
- [Untouched Valley](https://artlist.io/royalty-free-music/song/untouched-valley/136945) — Inon Zur, 68 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Tense political and tactical drama underscore, instrumental. Low cello and double bass ostinato, restrained snare rolls and rim clicks, sparse low piano notes, a lonely solo viola line. Suspenseful, restrained, dusk before an ambush or a treaty signing. No heroic brass, no choir. 88 BPM. Seamless loop, 30 seconds.
```

### F20 · 다크 판타지·오컬트 (촛불과 장부)

**컨셉.** 구#46 저주상속(구현) · #25 공개된 다음 수(구현) · #28 겨울 요새(구현) · #39 가업으로 물려받은 저주 관리(구현)

| 항목 | 방향 |
|---|---|
| 분위기 | 촛불, 계약서의 피, 타로, 눈 덮인 요새의 밤 |
| 편성 | 첼로 솔로, 하프시코드, 낮은 남성 허밍 합창, 파이프 오르간 저음(절제), 종, 28은 전투 팀파니·프레임드럼 |
| 템포 | 60~90 BPM |
| 루프 구조 | 장부/계약 루프 → 결산 스팅어(도덕 대가) / 58 전투 루프(카드 턴과 동기) |
| 적응형 레이어 | 구46·39: 도덕 자원이 줄수록 조성이 어두워지고(단조→프리지안) 합창이 가까워짐. 25: 공개된 적 수 5칸 = 5음 모티프, 받은 순서대로 음이 재생. 28: 요새 내구도 = 드럼 레이어. |
| 쓰지 말 것 | 메탈 기타, 에픽 트레일러 합창, 코믹 할로윈(F25와 구별) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/death-is-just-another-path` (CC-BY 3.0 · **BY**), `music/mystical-theme` (CC-BY 3.0 · **BY**), `horror-drones/the-chaos-has-risen` (CC-BY 3.0 · **BY**), `ambience/fireplace-sound-loop` (CC0), `ambience/fire-crackling` (CC0), `foley-props/church-bell` (CC-BY-SA 3.0 · **BY**)

**빈 곳.** 어두운 판타지 곡 후보는 BY 2곡뿐, 하프시코드·첼로 루프 없음.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Darkness](https://artlist.io/royalty-free-music/song/darkness/126311) — John Dada & the Weathermen, 70 BPM
- [The Council of the Titans](https://artlist.io/royalty-free-music/song/the-council-of-the-titans/103052) — Shahead Mostafafar, 68 BPM
- [Tumannoye Ozero (The Misty Lake)](https://artlist.io/royalty-free-music/song/tumannoye-ozero-the-misty-lake/109414) — Ian Post, 109 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Dark fantasy occult chamber piece, instrumental. Solo cello melody, harpsichord arpeggios, low male humming choir, restrained low organ pedal, a distant bell. Candlelit, ominous, contractual, elegant decay. No metal guitars, no trailer hits. 72 BPM. Seamless loop, 30 seconds.
```

### F21 · 몽환 기억 앰비언트

**컨셉.** 구#44 기억의 봉합사(구현) · #40 가짜 기억 심기(구현)

| 항목 | 방향 |
|---|---|
| 분위기 | 기억의 조각, 꿈의 봉합선, 거부 반응 |
| 편성 | 역재생 피아노, 그래뉼러 패드, 멀리서 들리는 오르골, 테이프 딜레이, 40은 불협 현 글리산도 |
| 템포 | 무박~75 BPM |
| 루프 구조 | 루프 A(꿈 안정) / 루프 B(모순·거부 반응: 피치 흔들림) → 봉합 성공 스팅어 |
| 적응형 레이어 | 구44: 조각이 맞아 들수록 역재생 요소가 정방향으로 돌아옴. 40: 가짜 기억의 어긋남 수치 = 디튠·그래뉼러 파편화, 한계 넘으면 F01급 드론으로 붕괴. |
| 쓰지 말 것 | 명상 음악 클리셰, 뉴에이지 싱잉볼 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/sleep-talking-loop-fantasy-rpg-sci-fi` (CC0), `horror-drones/dreamscape-drone` (CC-BY 3.0 · **BY**), `breathing-heartbeat/dreaming` (CC-BY 3.0 · **BY**), `ambience/reversing-time-stuck-in-time` (CC0)

**빈 곳.** 부분 확보(드론·루프 몇 개). 구현 2개.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Standing on the Shore of a Memory](https://artlist.io/royalty-free-music/song/standing-on-the-shore-of-a-memory/119820) — We Dream of Eden, 74 BPM
- [Glistening Ripples Lullaby - Creative Cut - Dreamy](https://artlist.io/royalty-free-music/song/glistening-ripples-lullaby-creative-cut-dreamy/136482) — Lior Soltz, 156 BPM
- [Through Valleys](https://artlist.io/royalty-free-music/song/through-valleys/111618) — Flint, 90 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Dreamy memory ambient, instrumental. Reversed piano phrases, granular shimmering pads, a faint distant music box, tape delay echoes, occasional pitch wobble. Hazy, intimate, fragile, like stitching memories together. No drums. 70 BPM. Seamless loop, 30 seconds.
```

### F22 · 하늘·계절 앰비언트

**컨셉.** #53 한 해의 나무 · #60 비행운 서예

| 항목 | 방향 |
|---|---|
| 분위기 | 햇빛, 바람, 긴 시간의 흐름, 하늘에 쓰는 글씨 |
| 편성 | 스트링 패드, 따뜻한 신스 패드, 피아노 하모닉스, 어쿠스틱 기타 스웰, 새소리·바람과 섞임 |
| 템포 | 60~90 BPM |
| 루프 구조 | 53: 계절 4벌 크로스페이드(10분에 1년) / 60: 비행 루프 + 획 완성 스팅어 |
| 적응형 레이어 | 53: 햇빛 받는 양 = 화성 밝기(장조 텐션), 계절 경계에서 자동 크로스페이드. 60: 바람 세기 = 패드 필터 스윕, 획이 번질수록 리버브 증가. |
| 쓰지 말 것 | 코퍼릿 업리프팅(박수 비트), 에픽 오케스트라 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/the-field-of-dreams` (CC0), `music/november-snow` (CC0), `ambience/wind1` (CC0), `ambience/forest-bird-sounds` (CC0), `ambience/nature-sounds-pack` (CC-BY 4.0 · **BY**), `ambience/ambient-bird-sounds` (CC0)

**빈 곳.** 부분 확보. 계절 한 벌 없음.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Transitions](https://artlist.io/royalty-free-music/song/transitions/126447) — Liquid Memoirs, 70 BPM
- [Beholding - Instrumental version](https://artlist.io/royalty-free-music/song/beholding-instrumental-version/102506) — Marshall Usinger, 57 BPM
- [The Returning](https://artlist.io/royalty-free-music/song/the-returning/97184) — Adi Goldstein, 74 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Airy open-sky ambient, instrumental. Warm evolving string and synth pads, soft piano harmonics, swelling acoustic guitar, gentle wind texture. Spacious, hopeful, sunlight slowly moving across a season. No drums. 72 BPM. Seamless loop, 30 seconds.
```

### F23 · 시스템 미니멀리즘 (반복 패턴)

**컨셉.** 구#45 내일의 지도(구현) · #41 지도가 거짓말하는 도시(구현) · #76 벌집 도시계획 · 구#84 하늘길 관제소 · #63 열차 시간표 교향곡

> **음악을 줄이거나 빼는 이유와 대체.** 63은 배경 음악을 깔지 않는다 — 플레이어가 만든 선로가 곡이다. 필요한 것은 곡이 아니라 음색 샘플 세트(마림바·벨·현 피치카토 한 음씩).

| 항목 | 방향 |
|---|---|
| 분위기 | 질서, 흐름, 교통망·벌집·도시가 스스로 움직이는 감각 |
| 편성 | 마림바·비브라폰 반복 패턴(스티브 라이히식 위상), 신스 아르페지오, 피아노 오스티나토, 41은 디튠된 피아노로 언캐니, 63은 열차 도착음 = 음표(프로시저럴) |
| 템포 | 100~130 BPM(모든 가족 중 템포 고정성이 가장 중요) |
| 루프 구조 | 모듈식: 1~2마디 셀 8~12개를 상태에 따라 켜고 끄는 스템 구조. 선형 루프보다 '셀 라이브러리' |
| 적응형 레이어 | 구45·41: 유효 경로 수 = 활성 셀 수, 41은 도시가 지도에 맞춰 스스로 고칠 때 한 셀이 반박자 밀림(위상 이동). 46: 8자 춤 정보 = 마림바 패턴. 구84: 층별 드론 교통량 = 옥타브별 아르페지오 밀도, 충돌 위험 시 불협. 63: 음악이 곧 게임플레이 — 열차 도착 시각을 박에 양자화해 음 발생(스케일 고정으로 오답도 음악적으로). |
| 쓰지 말 것 | EDM 킥, 감정적 선율 테마, 긴 리버브(패턴이 흐려짐) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`ambience/scifi-city-ambient-loop` (CC0), `ambience/clock-ticking` (CC-BY 3.0 · **BY**), `kenney-audio/music-jingles` (CC0-1.0), `music/free-music-pack` (CC0)

**빈 곳.** 패턴 음악 0곡. 96용 단음 샘플 세트도 없다(Kenney 징글은 조성이 섞임).

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Minimal Truth](https://artlist.io/royalty-free-music/song/minimal-truth/6001695) — Xavi Morató, 110 BPM
- [Always Ready](https://artlist.io/royalty-free-music/song/always-ready/11202) — Rhythm Scott, 95 BPM
- [Held in Motion - Short version](https://artlist.io/royalty-free-music/song/held-in-motion-short-version/6002012) — Roie Shpigler, 90 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Minimalist systems music in the style of phasing pattern pieces, instrumental. Interlocking marimba and vibraphone patterns, a soft synth arpeggio, repeating piano ostinato, gradually shifting phase. Orderly, flowing, like a city network running itself. No kick drum, no big melody. 116 BPM. Seamless loop, 30 seconds.
```

### F24 · 경쾌 퍼즐 (피치카토·마림바)

**컨셉.** #13 단어 연금술 · #14 말풍선 퍼즐 · #62 방탈출 제작자

| 항목 | 방향 |
|---|---|
| 분위기 | 번뜩임, 가벼운 장난기, 조합의 쾌감 |
| 편성 | 피치카토 현, 마림바, 우쿨렐레, 핑거스냅, 글로켄, 14은 만화풍 브라스 스탭, 62는 방탈출 설계용 약간의 미스터리 비브라폰 |
| 템포 | 110~135 BPM |
| 루프 구조 | 루프 A(설계·조합) 16마디 → 정답 스팅어(0.5~1.5초, 조성 맞춤) / 오답 스팅어(부드러운 하강) |
| 적응형 레이어 | 구13: 조합한 정의 수만큼 레이어 추가. 14: 컷 순서가 맞을수록 편성이 풀 밴드로. 62: 시뮬레이션 손님의 막힘 = 템포 하강, 풀리는 순간 원래 템포 복귀. |
| 쓰지 말 것 | 어린이 교육 앱 같은 과한 밝음, 칩튠(F25와 구별), 반복 피로를 부르는 짧은 루프(30초 미만 반복 금지) |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/school-of-quirks` (CC-BY 3.0 · **BY**), `music/deliciously-sour` (CC-BY 3.0 · **BY**), `music/shop-theme` (CC0), `kenney-audio/music-jingles` (CC0-1.0), `kenney-audio/interface-sounds` (CC0-1.0), `music/talking-cute-chiptune` (CC0)

**빈 곳.** 부분 확보(BY 2곡, CC0 shop-theme). 정답·오답 스팅어는 Kenney 징글로 가능.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Quirky Quest](https://artlist.io/royalty-free-music/song/quirky-quest/122417) — Solis, 135 BPM
- [Creeping up on Tiptoes](https://artlist.io/royalty-free-music/song/creeping-up-on-tiptoes/109457) — Veaceslav Draganov, 100 BPM
- [Wag the Tail](https://artlist.io/royalty-free-music/song/wag-the-tail/85914) — Roie Shpigler, 135 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Light clever puzzle music, instrumental. Playful pizzicato strings, marimba, ukulele, finger snaps, glockenspiel accents. Bright, curious, witty, satisfying to think along with. No chiptune, not childish. 120 BPM. Seamless loop, 30 seconds.
```

### F25 · 유쾌한 괴물 (테레민·튜바 / 칩튠 변주)

**컨셉.** #47 유령 부동산 · #56 괴수 세탁소 · #59 거꾸로 던전

| 항목 | 방향 |
|---|---|
| 분위기 | 무섭지 않은 유령, 투덜대는 괴수 손님, 모험가를 살려 보내는 던전 |
| 편성 | 테레민, 튜바, 바순, 하프시코드, 핑거스냅, 오르간 콤보. 59은 같은 선율의 16비트 칩튠(펄스파·삼각파·노이즈) 판본 |
| 템포 | 96~140 BPM |
| 루프 구조 | 루프 A(영업·설계) → 루프 B(손님 화남·모험가 입장) → 계약·리뷰 스팅어 |
| 적응형 레이어 | 47: 유령과 새 집주인 서명 여부에 따라 테레민(유령)과 피아노(사람) 볼륨 교차. 56: 괴수 짜증 게이지 = 튜바 음역 하강·템포 상승. 59: 모험가 스릴 곡선(시안의 그래프)을 그대로 음악 강도 커브로 사용. |
| 쓰지 말 것 | 진짜 공포 드론(F01), 할로윈 클리셰 비명 샘플, 59의 칩튠을 다른 두 컨셉에 섞기 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/kevin-macleod-incompetech` (CC-BY 4.0 · **BY**), `music/in-the-circus-psg-version` (CC-BY 3.0 · **BY**), `music/talking-cute-chiptune` (CC0), `audience/evil-laugh` (CC0), `audience/witch-cackle` (CC0), `kenney-audio/music-jingles` (CC0-1.0)

**빈 곳.** 코믹 유령·칩튠 곡은 부분 확보(talking-cute-chiptune CC0, PSG 서커스 BY).

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Ghost Waltz - Theremin, Pads & Bells](https://artlist.io/royalty-free-music/song/ghost-waltz-theremin-pads-bells/8803) — Ziv Moran, 95 BPM
- [Moonlit Mischief - Creative Cut - Sped Up](https://artlist.io/royalty-free-music/song/moonlit-mischief-creative-cut-sped-up/6000058) — Adam Dib, 140 BPM
- [Asteroid Rebellion 5 (90 칩튠 판본 참고)](https://artlist.io/royalty-free-music/song/asteroid-rebellion-5/71710) — T. Bless, 106 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Playful spooky comedy, instrumental. Theremin melody, bouncy tuba, bassoon, harpsichord, finger snaps, light combo organ. Mischievous, friendly monsters, funny rather than scary. 112 BPM. Seamless loop, 30 seconds.
```

### F26 · 로파이 재즈·빗속 도시 (일상 경영)

**컨셉.** #25 분실물 창고 · #20 버스 정류장 관찰 · #51 골목 고양이 정치 · #57 자판기의 마음 · 구#92 우산 대여점

| 항목 | 방향 |
|---|---|
| 분위기 | 퇴근길 노을, 비 오는 골목, 자판기 불빛, 고양이 정치 |
| 편성 | 로즈 피아노, 재즈 기타, 업라이트 베이스, 브러시/로파이 드럼, 비닐 크래클, 빗소리와 믹스 |
| 템포 | 70~90 BPM |
| 루프 구조 | 2~3분 루프 3~4개 순환(하루 근무 반복 피로 방지) + 하루 결산 스팅어 |
| 적응형 레이어 | 구92: 실제 비 세기 = 빗소리 대 음악 비율, 예보가 틀릴 때 코드 하나 비틀림. 13: 하루 20건 진행도에 따라 드럼 밀도. 51: 파벌별 모티프(콘트라베이스 피치카토=골목 대장 등). 57: 손님별 자판기 버튼음이 곡 조성에 맞춰 조율됨. |
| 쓰지 말 것 | 스터디 로파이 1시간 틀기(정보 없음), 힙합 보컬 찹, 과한 비닐 잡음 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/lofi-compilation` (CC0), `music/chill-lofi-inspired` (CC0), `music/bossa-nova` (CC0), `music/a-cloudy-morning-jazz` (CC-BY 3.0 · **BY**), `ambience/rain-gutter-loop` (CC0), `ambience/rain-loopable` (CC0), `ambience/high-traffic-road-sounds` (CC0)

**빈 곳.** 가장 잘 채워진 가족 — CC0 로파이 11곡 + 보사노바. 생성 필요 낮음.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [2 AM](https://artlist.io/royalty-free-music/song/2-am/63057) — FewDoors, 90 BPM
- [Roasted](https://artlist.io/royalty-free-music/song/roasted/43473) — Lalinea, 85 BPM
- [Webster Soul (feat. Bad Room Producer)](https://artlist.io/royalty-free-music/song/webster-soul-feat-bad-room-producer/104163) — Yestalgia, 84 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Rainy city lo-fi jazz, instrumental. Rhodes electric piano chords, soft jazz guitar licks, upright bass, dusty brushed lo-fi drums, light vinyl crackle, gentle rain in the background. Cozy, end-of-day, neon reflections on wet streets. 82 BPM. Seamless loop, 30 seconds.
```

### F27 · 하드 SF 신스 (궤도·정거장)

**컨셉.** #24 되감는 전투(구현) · #44 중력 주방 · #66 궤도 청소부

| 항목 | 방향 |
|---|---|
| 분위기 | 진공의 고요, 궤도 역학, 무중력 주방의 소동(44) |
| 편성 | 아날로그 신스 아르페지오, 저역 드론, 스페이스 패드, 텔레메트리 삑 소리를 리듬으로, 44는 일렉트로 펑크 베이스·클라비넷 |
| 템포 | 80~120 BPM |
| 루프 구조 | 66: 90초 궤도 루프 + 케슬러 연쇄 경보 레이어 / 24: 전투 루프(되감기 3회 = 3단계 변주) / 44: 주방 러시 루프 |
| 적응형 레이어 | 66: 잔해 밀도 = 아르페지오 음 수, 연쇄 충돌 위험 시 불협 저역·경보. 24: 되감을 때마다 테이프 역재생 스윕 후 한 레이어 추가(적이 기억한다 = 음악도 기억한다). 44: 중력 방향 전환 = 스테레오 패닝 회전·피치 벤드. |
| 쓰지 말 것 | 스타워즈식 오케스트라, 신스웨이브 향수 리드, 과한 SF 효과음을 음악에 섞기 |

**라이브러리 후보** (`AssetDownloads/tmp_game/assets/audio/…`, 색인 기준 — 청취 확인 전):

`music/cyberpunk-moonlight-sonata` (CC0), `ambience/ambient-spaceship-hums` (CC-BY 3.0 · **BY**), `ambience/sci-fi-drone-loop` (CC-BY 3.0 · **BY**), `ambience/sci-fi-ambience-sfx` (CC0), `kenney-audio/sci-fi-sounds` (CC0-1.0), `voice-publicdomain/Apollo11Audio` (Public Domain (PDM 1.0))

**빈 곳.** SF 환경음은 있지만 음악은 CC0 1곡. Apollo 교신(PD)은 66번 분위기용 다이제틱 소스로 좋다.

**Artlist 청취 참고** (참고용 — 게임 사용은 라이선스 확인 전 금지):

- [Outer Limits](https://artlist.io/royalty-free-music/song/outer-limits/115167) — Theatre of Delays, 75 BPM
- [First Step on Mars](https://artlist.io/royalty-free-music/song/first-step-on-mars/89486) — Matooma, 80 BPM
- [End Game](https://artlist.io/royalty-free-music/song/end-game/99997) — Downtown Binary, 118 BPM

**생성 프롬프트 초안** (생성하지 않았다):

```text
Hard science fiction orbital ambient, instrumental. Precise analog synth arpeggio, deep sub drone, wide cold space pads, subtle telemetry blips used as rhythm. Vast, calm, mathematically precise, quietly dangerous. No orchestra, no retro synthwave lead. 96 BPM. Seamless loop, 30 seconds.
```

## 4. Artlist 참고곡의 라이선스 — 사람이 답해야 할 질문

§3의 Artlist 곡(가족당 2~3곡, 총 77곡)은 **청취 참고용**으로만 골랐다. 방향을 귀로 맞추기 위한 것이고, 게임에 넣기로 한 곡이 아니다.

- Artlist 카탈로그 곡을 **출시하는 게임에 넣을 수 있는지는 사용자의 Artlist 구독 종류와 약관에 달렸다.** 이 저장소는 그 정보를 가지고 있지 않다. 이 문서는 사용 가능하다고 **가정하지 않는다.**
- 질문: (1) 현재 Artlist 구독이 있는가, 있다면 어떤 플랜인가? (2) 그 플랜이 게임(인터랙티브 미디어) 배포와 루프 편집을 허용하는가? (3) 구독 종료 후에도 이미 출시한 빌드에 남아 있어도 되는가? — 셋 다 약관 원문으로 확인해야 한다.
- 확인 전까지 규칙: **Artlist 음원 파일을 저장소·`AssetDownloads/`·빌드에 넣지 않는다.** 링크만 남긴다.
- 같은 질문이 **AI 생성 음원**에도 있다. 생성 서비스의 상업 이용 조건(출력물 권리·크레딧 표기·재배포)을 생성 전에 확인해 각 게임 `CREDITS.md`에 적는다.
- 참고곡 링크는 2026-09-29 Artlist `search_music`(무료·읽기 전용) 결과다. BPM은 카탈로그 표기값이며, 일부는 두 배/절반 템포로 표기돼 있다(예: Glistening Ripples Lullaby 156 = 체감 78).
- **국악(F18)은 Artlist에서 찾지 못했다.** "korean"은 K-pop, "gayageum"은 중국 구정·일본풍 곡만 나왔다. F18의 참고곡 3개는 "질감·공간감만 참고, 선법은 따르지 말 것"으로 표시했다.

## 5. 생성 우선순위 — 예산이 제한될 때

**점수 = 가족에 속한 컨셉 수 × 라이브러리 격차**(없음 2 · 빈약 1.5 · 부분 1 · 대부분 있음 0.5 · 충분/불필요 0).
동점과 경계는 (a) 이미 구현된 컨셉(41~71)이 있는가, (b) 대체 경로가 전혀 없는가(F18 국악)로 정했다.

| 순위 | 가족 | 컨셉 수 | 격차 | 점수 | 이유 |
|:-:|---|:-:|:-:|:-:|---|
| 1 | F23 시스템 미니멀리즘 (반복 패턴) | 5 | 없음 | 10 | 구현 구45·41 + 미착수 46·구84·63. 패턴 음악 0곡. 63은 음색 샘플 세트도 같이 필요 |
| 2 | F15 목가 어쿠스틱 파밍 (계절 한 벌) | 6 | 빈약 | 9 | 파밍 6개(구현 21·22·23·27·29). 21은 유일한 Unity 셸 — 가장 먼저 실제 빌드에 들어간다 |
| 3 | F06 모던 수사·미니멀 전자 펄스 | 4 | 없음 | 8 | 구현 구41·구43·32. 수사 펄스 곡 0곡 |
| 4 | F09 서재·편지 실내악 (펜과 활자) | 4 | 없음 | 8 | 구현 구48·26·34. 실내악 0곡 |
| 5 | F19 전쟁과 조약 — 저현 오스티나토 | 4 | 없음 | 8 | 구현 구42·30·31·37 네 개 전부. 긴장 오스티나토 0곡 |
| 6 | F13 오르골·태엽·미니어처 | 4 | 없음 | 8 | 미착수 4개. 오르골·왈츠 0곡(시계 소리는 넉넉) |
| 7 | F01 호러 드론·저음 긴장 | 6 | 부분 | 6 | 컨셉 6개로 최다 공동. 드론은 있으나 레이어용 펄스가 없다 |
| 8 | F08 상실·돌봄 미니멀 피아노 | 6 | 부분 | 6 | 컨셉 6개(구현 구49·36). CC0 피아노 2곡뿐 |
| 9 | F18 국악·조선 산수 | 3 | 없음 | 6 | 대체 경로가 없다 — 라이브러리 0곡, Artlist에도 없음 |
| 10 | F20 다크 판타지·오컬트 (촛불과 장부) | 4 | 빈약 | 6 | 구현 구46·25·28·39. BY 후보 2곡뿐 |
| 11 | F16 바다·항해 포크 (물때·폭풍·향신료) | 3 | 없음 | 6 | 항해 음악 0곡, 환경음만 있음 |
| 12 | F17 수중 앰비언트 | 3 | 없음 | 6 | 수중 음악 0곡 |

**제외한 것과 이유.**
- **F04 라디오**(점수 6): 방송에서 나오는 곡이 여러 개(선곡이 게임 조작) 필요해 30초 루프 1개로는 방향 검증이 안 된다. 곡 목록·길이를 먼저 정한 뒤 별도 배치로 잡는다.
- **F26 로파이**(0): CC0 로파이 11곡으로 이미 채워진다. **F03 무음**(0): 음악을 만들지 않는다. **F02 소리가 정보**(1.5): 필요한 것은 음악이 아니라 얼음·숨·동굴 소리다.
- **F11 노이르 재즈·F12 서커스**: BY 곡이 이미 있다(크레딧 의무만 지키면 된다).

**크레딧 계산**(가족당 30초 트랙 1개, 트랙당 150 크레딧 — 요청에 주어진 단가 기준):

| 범위 | 트랙 수 | 크레딧 |
|---|:-:|:-:|
| 최소(1~8위) | 8 | 1,200 |
| 권장(1~12위) | 12 | **1,800** |

- 150 크레딧은 요청에 주어진 가정이다. **실제 단가는 생성 직전에 `get_generation_cost`로 모델·길이·설정을 넣어 확인한다**(이 문서에서는 호출하지 않았다).
- 30초 1개는 **방향 검증용**이다. 실제 게임에 쓰려면 적응형 레이어(기본/긴장 두 벌 이상), 스팅어, 계절·결말 변주가 필요해 가족당 3~6개가 된다. 12가족 전부를 완성하면 대략 36~72트랙(5,400~10,800 크레딧, 같은 단가 가정)이다. **가족 방향을 30초로 먼저 듣고 합격한 가족만 확장**하는 순서를 권한다.

## 6. 생성할 때 주의

- **"seamless loop"는 모델이 보장하지 않는다.** 생성 후 루프 지점을 박 경계로 잘라 확인하고, 안 맞으면 엔진에서 1~2박 크로스페이드로 잇는다. 30초는 게임 루프로는 짧다 — 반복 피로가 큰 가족(F24 퍼즐, F26 로파이, F15 파밍)은 같은 프롬프트로 A/B 두 벌을 만들어 교대하는 편이 낫다.
- **레이어용으로 쓰려면 키·템포를 고정한다.** 프롬프트의 BPM을 바꾸지 말고, "same key, same tempo, add …" 식으로 변주를 요청한다.
- **같은 조건으로 비교 검증한다.** 생성곡은 실제 빌드에서 상태 변화(위험도 상승, 계절 전환 등)를 일으키며 녹화해 참고곡·시안과 나란히 들어 본다. 음악 단독 청취만으로 완료라고 하지 않는다(뿌리 CLAUDE.md).
- 프롬프트에는 실존 작곡가·곡 이름을 넣지 않았다(F23의 "phasing pattern pieces"는 양식 기술). 참고곡 제목도 프롬프트에 넣지 않는다.

## 7. 컨셉 → 가족 색인

| # | 컨셉 | 가족 |
|---|---|---|
| 1 | 해킹 탈출 방 | F05 터미널 글리치·사이버 신스 |
| 2 | 삭제 직전 로그 | F05 터미널 글리치·사이버 신스 |
| 3 | 주파수 사냥 | F04 아날로그 라디오·다이제틱 방송 |
| 4 | 죽은 방송국 | F04 아날로그 라디오·다이제틱 방송 |
| 5 | 배터리 서바이벌 | F01 호러 드론·저음 긴장 |
| 6 | 빛이 닿지 않는 것 | F01 호러 드론·저음 긴장 |
| 7 | 사진 속 범인 | F11 노이르 재즈·야간 순찰 |
| 8 | 찍히지 않은 사람 | F07 테이프 루프·열화 노스탤지어 |
| 9 | 인형극 대결 | F12 보드빌·래그타임·서커스 칼리오페 |
| 10 | 마지막 공연 | F13 오르골·태엽·미니어처 |
| 11 | 층 선택 생존 | F01 호러 드론·저음 긴장 |
| 12 | 내려가는 곳 | F01 호러 드론·저음 긴장 |
| 13 | 단어 연금술 | F24 경쾌 퍼즐 (피치카토·마림바) |
| 14 | 사라지는 언어의 마지막 화자 | F18 국악·조선 산수 |
| 15 | 수면 아래 스텔스 | F17 수중 앰비언트 |
| 16 | 익사한 집 | F17 수중 앰비언트 |
| 17 | 펜팔 추리 | F09 서재·편지 실내악 (펜과 활자) |
| 18 | 보내지 못한 것 | F08 상실·돌봄 미니멀 피아노 |
| 19 | 교환대 타이쿤 | F12 보드빌·래그타임·서커스 칼리오페 |
| 20 | 연결하면 안 되는 통화 | F11 노이르 재즈·야간 순찰 |
| 21 | 1초 스냅 | F01 호러 드론·저음 긴장 |
| 22 | 시력을 잃는 하루 | F08 상실·돌봄 미니멀 피아노 |
| 23 | 야간 방송 진행자 | F04 아날로그 라디오·다이제틱 방송 |
| 24 | 아무도 없는 학교에 말 걸기 | F02 소리가 정보 — 음악 최소·덕킹 |
| 25 | 분실물 창고 | F26 로파이 재즈·빗속 도시 (일상 경영) |
| 26 | 한 사람의 유품 | F07 테이프 루프·열화 노스탤지어 |
| 27 | 말풍선 퍼즐 | F24 경쾌 퍼즐 (피치카토·마림바) |
| 28 | 마지막 컷 | F08 상실·돌봄 미니멀 피아노 |
| 29 | 창문 속 괴물 | F01 호러 드론·저음 긴장 |
| 30 | 종점 | F07 테이프 루프·열화 노스탤지어 |
| 31 | 빙판 탈출 | F02 소리가 정보 — 음악 최소·덕킹 |
| 32 | 얼음 아래 누군가 | F02 소리가 정보 — 음악 최소·덕킹 |
| 33 | 틀린 그림 순찰 | F11 노이르 재즈·야간 순찰 |
| 34 | 자리를 바꾸는 것들 | F11 노이르 재즈·야간 순찰 |
| 35 | 테이프 감식 | F06 모던 수사·미니멀 전자 펄스 |
| 36 | 늘어진 테이프 | F07 테이프 루프·열화 노스탤지어 |
| 37 | 숨소리 도둑 | F02 소리가 정보 — 음악 최소·덕킹 |
| 38 | 간병 | F08 상실·돌봄 미니멀 피아노 |
| 39 | 버스 정류장 관찰 | F26 로파이 재즈·빗속 도시 (일상 경영) |
| 40 | 버스가 오지 않는 저녁 | F03 무음 지향·다이제틱 대체 |
| 41 | 잔향 감식실 ·구현 | F06 모던 수사·미니멀 전자 펄스 |
| 42 | 휴전의 문장 ·구현 | F19 전쟁과 조약 — 저현 오스티나토 |
| 43 | 잠금 해제 불가 ·구현 | F06 모던 수사·미니멀 전자 펄스 |
| 44 | 기억의 봉합사 ·구현 | F21 몽환 기억 앰비언트 |
| 45 | 내일의 지도 ·구현 | F23 시스템 미니멀리즘 (반복 패턴) |
| 46 | 저주상속 ·구현 | F20 다크 판타지·오컬트 (촛불과 장부) |
| 47 | 당신의 박자 ·구현 | F03 무음 지향·다이제틱 대체 |
| 48 | 붉은 펜으로 남긴 것 ·구현 | F09 서재·편지 실내악 (펜과 활자) |
| 49 | 마지막 이삿날 ·구현 | F08 상실·돌봄 미니멀 피아노 |
| 50 | 기적 배정과 ·구현 | F10 천상 관료제 (하프·첼레스타·허밍) |
| 51 | 밭이 줄어드는 세계 ·구현 | F15 목가 어쿠스틱 파밍 (계절 한 벌) |
| 52 | 되돌릴 수 있는 한 해 ·구현 | F15 목가 어쿠스틱 파밍 (계절 한 벌) |
| 53 | 작물이 정보다 ·구현 | F15 목가 어쿠스틱 파밍 (계절 한 벌) |
| 54 | 되감는 전투 ·구현 | F27 하드 SF 신스 (궤도·정거장) |
| 55 | 공개된 다음 수 ·구현 | F20 다크 판타지·오컬트 (촛불과 장부) |
| 56 | 소모되는 활자 ·구현 | F09 서재·편지 실내악 (펜과 활자) |
| 57 | 수확하는 덱 ·구현 | F15 목가 어쿠스틱 파밍 (계절 한 벌) |
| 58 | 겨울 요새 ·구현 | F20 다크 판타지·오컬트 (촛불과 장부) |
| 59 | 덱을 심는다 ·구현 | F15 목가 어쿠스틱 파밍 (계절 한 벌) |
| 60 | 능선 초소 ·구현 | F19 전쟁과 조약 — 저현 오스티나토 |
| 61 | 속삭이는 지도 ·구현 | F19 전쟁과 조약 — 저현 오스티나토 |
| 62 | 죽은 사람의 휴대폰 ·구현 | F06 모던 수사·미니멀 전자 펄스 |
| 63 | 냄새로 푸는 추리 ·구현 | F03 무음 지향·다이제틱 대체 |
| 64 | 원고 되돌려 보내기 ·구현 | F09 서재·편지 실내악 (펜과 활자) |
| 65 | 소리 없는 오케스트라 ·구현 | F03 무음 지향·다이제틱 대체 |
| 66 | 철거 직전 건물의 마지막 세입자들 ·구현 | F08 상실·돌봄 미니멀 피아노 |
| 67 | 번역가의 전쟁 ·구현 | F19 전쟁과 조약 — 저현 오스티나토 |
| 68 | 신의 비서 ·구현 | F10 천상 관료제 (하프·첼레스타·허밍) |
| 69 | 가업으로 물려받은 저주 관리 ·구현 | F20 다크 판타지·오컬트 (촛불과 장부) |
| 70 | 가짜 기억 심기 ·구현 | F21 몽환 기억 앰비언트 |
| 71 | 지도가 거짓말하는 도시 ·구현 | F23 시스템 미니멀리즘 (반복 패턴) |
| 72 | 조수 간만 우체부 | F16 바다·항해 포크 (물때·폭풍·향신료) |
| 73 | 그림자 경매 | F11 노이르 재즈·야간 순찰 |
| 74 | 중력 주방 | F27 하드 SF 신스 (궤도·정거장) |
| 75 | 봉화 네트워크 | F18 국악·조선 산수 |
| 76 | 벌집 도시계획 | F23 시스템 미니멀리즘 (반복 패턴) |
| 77 | 유령 부동산 | F25 유쾌한 괴물 (테레민·튜바 / 칩튠 변주) |
| 78 | 폭풍의 등대지기 | F16 바다·항해 포크 (물때·폭풍·향신료) |
| 79 | 개미 고고학 | F13 오르골·태엽·미니어처 |
| 80 | 지우개 모험 | F14 동화 어쿠스틱 (연필·이불·눈사람) |
| 81 | 골목 고양이 정치 | F26 로파이 재즈·빗속 도시 (일상 경영) |
| 82 | 시계탑 수리공 | F13 오르골·태엽·미니어처 |
| 83 | 한 해의 나무 | F22 하늘·계절 앰비언트 |
| 84 | 하늘길 관제소 | F23 시스템 미니멀리즘 (반복 패턴) |
| 85 | 이불 요새 | F14 동화 어쿠스틱 (연필·이불·눈사람) |
| 86 | 향신료 항로 | F16 바다·항해 포크 (물때·폭풍·향신료) |
| 87 | 괴수 세탁소 | F25 유쾌한 괴물 (테레민·튜바 / 칩튠 변주) |
| 88 | 자판기의 마음 | F26 로파이 재즈·빗속 도시 (일상 경영) |
| 89 | 인형의 집 탐정 | F13 오르골·태엽·미니어처 |
| 90 | 거꾸로 던전 | F25 유쾌한 괴물 (테레민·튜바 / 칩튠 변주) |
| 91 | 비행운 서예 | F22 하늘·계절 앰비언트 |
| 92 | 우산 대여점 | F26 로파이 재즈·빗속 도시 (일상 경영) |
| 93 | 눈사람의 겨울 | F14 동화 어쿠스틱 (연필·이불·눈사람) |
| 94 | 메아리 지도 제작자 | F02 소리가 정보 — 음악 최소·덕킹 |
| 95 | 방탈출 제작자 | F24 경쾌 퍼즐 (피치카토·마림바) |
| 96 | 열차 시간표 교향곡 | F23 시스템 미니멀리즘 (반복 패턴) |
| 97 | 거인의 등 위 마을 | F15 목가 어쿠스틱 파밍 (계절 한 벌) |
| 98 | 마지막 순회 서커스 | F12 보드빌·래그타임·서커스 칼리오페 |
| 99 | 먹물 문어 | F17 수중 앰비언트 |
| 100 | 궤도 청소부 | F27 하드 SF 신스 (궤도·정거장) |
| 101 | 이름 없는 신당 | F18 국악·조선 산수 |

## 8. 열린 질문

1. Artlist 구독 여부·플랜·게임 사용 허용 범위(§4).
2. AI 생성 음원의 상업 이용 조건과 크레딧 표기 방식.
3. §5 권장 12개(1,800 크레딧)로 갈지, 최소 8개(1,200)로 갈지.
4. F18 국악: 생성 외에 국립국악원 등 공공 음원을 찾아볼지(라이선스 확인 필요).
5. F04 라디오: 방송 곡을 몇 곡·몇 분으로 할지(곡 목록 확정 후 별도 배치).
