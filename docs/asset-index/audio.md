> **원본 위치:** `AssetDownloads/tmp_game/assets/audio/` (gitignore — 바이너리는 추적하지 않는다).
> 이 문서는 목록만 저장소에 남긴 사본이다. 아래 링크는 그 원본을 가리킨다.

# 오디오 에셋 (tmp_game/assets/audio)

`plam.md`의 20개 컨셉용으로 모은 **무료 라이선스 오디오**. 수집일 2026-09-28 (UTC).  
**팩 276개 · 오디오 파일 5069개 · 총 2.77 GB.** 모든 파일을 `ffprobe`로 열어 봤고 손상 파일 0건.

## 규칙
- 각 팩 폴더에 `SOURCE.json`(출처·다운로드 URL·라이선스·작성자·수집 시각·아카이브 sha256·`usedFor` 컨셉 번호)과 라이선스 파일(`LICENSE.txt` 또는 번들 License.txt)이 있다.
- 허용 라이선스: CC0 / 퍼블릭 도메인 / CC-BY / CC-BY-SA. **CC-NC·GPL·OGA-BY 전용 항목은 받지 않았다** (`nc-reference/` 폴더 없음).
- **CC-BY / CC-BY-SA 팩은 크레딧 표기 의무**가 있다. `SOURCE.json`의 `attribution` 필드(Commons·incompetech는 파일별 `downloads[].attribution`)를 그대로 게임 크레딧에 넣는다. CC-BY-SA 소리를 가공해 배포하면 그 가공본도 BY-SA가 된다.
- OpenGameArt 팩의 `LICENSE.txt`에는 페이지 본문(작가의 크레딧 요청문 포함)을 같이 저장했다. 여러 라이선스가 걸린 항목은 가장 관대한 것(CC0 > CC-BY > CC-BY-SA)을 택했다.
- 20MB 미만 zip/7z는 원본을 같이 두었고, 그 이상은 풀고 지웠다(sha256은 SOURCE.json에 남음).

## 역할별 폴더

| 폴더 | 내용 | 주 컨셉 | 팩 | 파일 | 크기 |
|---|---|---|---:|---:|---:|
| `kenney-audio/` | Kenney 오디오 팩 전체 (원본 구조 그대로) | 1,5,7,9,10,13,14 등 UI·임팩트·징글·보이스 | 10 | 753 | 30 MB |
| `sfx-ui-typing/` | 타건음·타자기·터미널 비프·글리치 | 1, 9 | 14 | 233 | 62 MB |
| `radio-static-noise/` | 노이즈·단파·잡음·무전·신호 | 2, 12, 18 | 9 | 135 | 472 MB |
| `footsteps/` | 여러 재질의 발소리 | 3, 8, 16, 17, 19 | 16 | 330 | 19 MB |
| `breathing-heartbeat/` | 숨소리·심장박동 | 3, 19 | 8 | 44 | 17 MB |
| `ambience/` | 환경음·룸톤·비/바람/밤/물/기계 험 등 루프 | 6, 8, 11, 15, 16, 17, 20 | 73 | 425 | 569 MB |
| `foley-props/` | 문·열쇠·종이·카메라·테이프·전화 신호음·소품 폴리 | 4, 5, 9, 10, 13, 17, 18, 19 | 64 | 1496 | 509 MB |
| `audience/` | 박수·환호·웃음(주로 개별 웃음)·야유 | 5 | 12 | 55 | 60 MB |
| `ice/` | 균열·부서짐·눈 밟기·삐걱임 | 16 | 9 | 181 | 22 MB |
| `horror-drones/` | 긴장 베드·드론·호러 효과 | 3, 6, 8, 11, 15, 16, 17, 18, 19 | 19 | 223 | 176 MB |
| `voice-publicdomain/` | 퍼블릭 도메인 낭독·NASA 교신·초기 녹음 | 2, 9, 10, 12, 18 | 11 | 72 | 214 MB |
| `voice-sfx/` | CC0 캐릭터 보이스 클립(감탄사·짧은 대사·숨/신음) | 3, 5, 10, 19 | 2 | 1039 | 204 MB |
| `music/` | BGM (CC0/CC-BY) | 5, 12, 17, 20 | 29 | 83 | 481 MB |

## 팩 목록

표기: **BY** = 크레딧 표기 필요. 컨셉 = plam.md 번호.

### kenney-audio/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`casino-audio`](kenney-audio/casino-audio/) | [Kenney casino-audio](https://kenney.nl/assets/casino-audio) | CC0-1.0 | Kenney (kenney.nl) | 55 | 1.7 | 5,13 |
| [`digital-audio`](kenney-audio/digital-audio/) | [Kenney digital-audio](https://kenney.nl/assets/digital-audio) | CC0-1.0 | Kenney (kenney.nl) | 63 | 2.0 | 1,7,14 |
| [`impact-sounds`](kenney-audio/impact-sounds/) | [Kenney impact-sounds](https://kenney.nl/assets/impact-sounds) | CC0-1.0 | Kenney (kenney.nl) | 130 | 1.8 | 3,5,6,13,17,19 |
| [`interface-sounds`](kenney-audio/interface-sounds/) | [Kenney interface-sounds](https://kenney.nl/assets/interface-sounds) | CC0-1.0 | Kenney (kenney.nl) | 100 | 1.7 | 1,7,9,10,14 |
| [`music-jingles`](kenney-audio/music-jingles/) | [Kenney music-jingles](https://kenney.nl/assets/music-jingles) | CC0-1.0 | Kenney (kenney.nl) | 86 | 2.5 | 5,12 |
| [`rpg-audio`](kenney-audio/rpg-audio/) | [Kenney rpg-audio](https://kenney.nl/assets/rpg-audio) | CC0-1.0 | Kenney (kenney.nl) | 52 | 1.9 | 3,4,5,9,13,17,19 |
| [`sci-fi-sounds`](kenney-audio/sci-fi-sounds/) | [Kenney sci-fi-sounds](https://kenney.nl/assets/sci-fi-sounds) | CC0-1.0 | Kenney (kenney.nl) | 73 | 11.3 | 1,2 |
| [`ui-audio`](kenney-audio/ui-audio/) | [Kenney ui-audio](https://kenney.nl/assets/ui-audio) | CC0-1.0 | Kenney (kenney.nl) | 52 | 0.9 | 1,7,10,14 |
| [`voiceover-pack`](kenney-audio/voiceover-pack/) | [Kenney voiceover-pack](https://kenney.nl/assets/voiceover-pack) | CC0-1.0 | Kenney (kenney.nl) | 95 | 3.7 | 5 |
| [`voiceover-pack-fighter`](kenney-audio/voiceover-pack-fighter/) | [Kenney voiceover-pack-fighter](https://kenney.nl/assets/voiceover-pack-fighter) | CC0-1.0 | Kenney (kenney.nl) | 47 | 2.4 | 5 |

### sfx-ui-typing/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`50-cc0-sci-fi-sfx`](sfx-ui-typing/50-cc0-sci-fi-sfx/) | [50 CC0 Sci-Fi SFX](https://opengameart.org/content/50-cc0-sci-fi-sfx) | CC0 | rubberduck | 50 | 4.7 | 1,9 |
| [`60-cc0-sci-fi-sfx`](sfx-ui-typing/60-cc0-sci-fi-sfx/) | [60 CC0 Sci-Fi SFX](https://opengameart.org/content/60-cc0-sci-fi-sfx) | CC0 | rubberduck | 60 | 17.6 | 1,9 |
| [`9-sci-fi-computer-sounds-and-beeps`](sfx-ui-typing/9-sci-fi-computer-sounds-and-beeps/) | [9 sci-fi computer sounds and beeps](https://opengameart.org/content/9-sci-fi-computer-sounds-and-beeps) | CC-BY 3.0 **BY** | qubodup | 9 | 2.9 | 1,9 |
| [`beep-tone-sound-sfx`](sfx-ui-typing/beep-tone-sound-sfx/) | [Beep Tone Sound SFX](https://opengameart.org/content/beep-tone-sound-sfx) | CC0 | qubodup | 1 | 0.0 | 1,9 |
| [`commons-typewriter-keyboard`](sfx-ui-typing/commons-typewriter-keyboard/) | [Wikimedia Commons audio: commons-typewriter-keyboard](https://commons.wikimedia.org/) | mixed per file: CC0 **BY** | various (per file) | 1 | 0.1 | 1,9 |
| [`glitch-music`](sfx-ui-typing/glitch-music/) | [glitch music](https://opengameart.org/content/glitch-music) | CC0 | celestialghost8 | 1 | 1.5 | 1,9 |
| [`keyboard-soundpack-1-typing-and-single-keystrokes`](sfx-ui-typing/keyboard-soundpack-1-typing-and-single-keystrokes/) | [Keyboard Soundpack #1 [Typing and Single Keystrokes]](https://opengameart.org/content/keyboard-soundpack-1-typing-and-single-keystrokes) | CC0 | unicaegames | 50 | 19.5 | 1,9 |
| [`keyboard-typing`](sfx-ui-typing/keyboard-typing/) | [Keyboard Typing](https://opengameart.org/content/keyboard-typing) | CC-BY-SA 4.0 **BY** | Wandering Door Games | 10 | 0.9 | 1,9 |
| [`mechanical-keyboard-sound`](sfx-ui-typing/mechanical-keyboard-sound/) | [Mechanical keyboard sound](https://opengameart.org/content/mechanical-keyboard-sound) | CC-BY 3.0 **BY** | bluszcz | 2 | 0.0 | 1,9 |
| [`short-alarm`](sfx-ui-typing/short-alarm/) | [Short alarm](https://opengameart.org/content/short-alarm) | CC0 | yd | 1 | 0.0 | 1,9 |
| [`single-key-press-sounds`](sfx-ui-typing/single-key-press-sounds/) | [Single Key Press Sounds](https://opengameart.org/content/single-key-press-sounds) | CC-BY 3.0 **BY** | qubodup | 28 | 5.9 | 1,9 |
| [`typewriter-sounds`](sfx-ui-typing/typewriter-sounds/) | [Typewriter sounds](https://opengameart.org/content/typewriter-sounds) | CC0 | Cassie-OrbitGames | 8 | 0.3 | 1,9 |
| [`typing-soundeffect`](sfx-ui-typing/typing-soundeffect/) | [typing soundeffect](https://opengameart.org/content/typing-soundeffect) | CC0 | johndekale | 1 | 0.2 | 1,9 |
| [`ui-sound-effects-pack`](sfx-ui-typing/ui-sound-effects-pack/) | [UI Sound effects pack](https://opengameart.org/content/ui-sound-effects-pack) | CC-BY 3.0 **BY** | ViRiX | 11 | 8.0 | 1,9 |

### radio-static-noise/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`100-cc0-sfx`](radio-static-noise/100-cc0-sfx/) | [100 CC0 SFX](https://opengameart.org/content/100-cc0-sfx) | CC0 | rubberduck | 100 | 5.8 | 2,12,18 |
| [`commons-radio`](radio-static-noise/commons-radio/) | [Wikimedia Commons audio: commons-radio](https://commons.wikimedia.org/) | mixed per file: CC0 **BY** | various (per file) | 1 | 1.5 | 2,12,18 |
| [`dark-ambience-soundscapes`](radio-static-noise/dark-ambience-soundscapes/) | [Dark Ambience Soundscapes](https://opengameart.org/content/dark-ambience-soundscapes) | CC-BY-SA 3.0 **BY** | qubodup | 8 | 54.2 | 2,12,18 |
| [`female-soldier-voice`](radio-static-noise/female-soldier-voice/) | [Female Soldier Voice](https://opengameart.org/content/female-soldier-voice) | CC-BY 3.0 **BY** | qubodup | 12 | 3.5 | 2,12,18 |
| [`frequency-static-sound-effects`](radio-static-noise/frequency-static-sound-effects/) | [Frequency Static Sound Effects](https://opengameart.org/content/frequency-static-sound-effects) | CC0 | bretbernhoft | 10 | 403.8 | 2,12,18 |
| [`mysterious-radio-signal`](radio-static-noise/mysterious-radio-signal/) | [Mysterious Radio Signal](https://opengameart.org/content/mysterious-radio-signal) | CC-BY 3.0 **BY** | samuncle | 1 | 1.2 | 2,12,18 |
| [`radio-call`](radio-static-noise/radio-call/) | [Radio call](https://opengameart.org/content/radio-call) | CC-BY 4.0 **BY** | Reemax | 1 | 0.4 | 2,12,18 |
| [`static`](radio-static-noise/static/) | [Static](https://opengameart.org/content/static) | CC0 | xhunterko | 1 | 0.5 | 2,12,18 |
| [`zombie-news-in-radio`](radio-static-noise/zombie-news-in-radio/) | [Zombie news in Radio](https://opengameart.org/content/zombie-news-in-radio) | CC0 | zisongbr | 1 | 1.1 | 2,12,18 |

### footsteps/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`100-cc0-sfx-2`](footsteps/100-cc0-sfx-2/) | [100 CC0 SFX #2](https://opengameart.org/content/100-cc0-sfx-2) | CC0 | rubberduck | 100 | 4.7 | 3,8,16,17,19 |
| [`42-snow-and-gravel-footsteps`](footsteps/42-snow-and-gravel-footsteps/) | [42 Snow and Gravel Footsteps](https://opengameart.org/content/42-snow-and-gravel-footsteps) | CC0 | qubodup | 42 | 7.7 | 3,8,16,17,19 |
| [`different-steps-on-wood-stone-leaves-gravel-and-mud`](footsteps/different-steps-on-wood-stone-leaves-gravel-and-mud/) | [Different steps on wood, stone, leaves, gravel and mud](https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud) | CC0 | TinyWorlds | 8 | 0.2 | 3,8,16,17,19 |
| [`fantozzis-footsteps-grasssand-stone`](footsteps/fantozzis-footsteps-grasssand-stone/) | [Fantozzi's Footsteps (Grass/Sand & Stone)](https://opengameart.org/content/fantozzis-footsteps-grasssand-stone) | CC0 | qubodup | 24 | 0.9 | 3,8,16,17,19 |
| [`footsteps`](footsteps/footsteps/) | [Footsteps](https://opengameart.org/content/footsteps) | CC-BY 3.0 **BY** | spookymodem | 1 | 0.7 | 3,8,16,17,19 |
| [`footsteps-0`](footsteps/footsteps-0/) | [Footsteps](https://opengameart.org/content/footsteps-0) | CC0 | GboxMikeFozzy | 6 | 0.1 | 3,8,16,17,19 |
| [`footsteps-leather-cloth-armor`](footsteps/footsteps-leather-cloth-armor/) | [Footsteps Leather, Cloth, Armor](https://opengameart.org/content/footsteps-leather-cloth-armor) | CC0 | HaelDB | 12 | 0.2 | 3,8,16,17,19 |
| [`footsteps-on-different-surfaces`](footsteps/footsteps-on-different-surfaces/) | [Footsteps on different surfaces](https://opengameart.org/content/footsteps-on-different-surfaces) | CC-BY 3.0 **BY** | congusbongus | 78 | 0.8 | 3,8,16,17,19 |
| [`grass-foot-step-sounds-yo-frankie`](footsteps/grass-foot-step-sounds-yo-frankie/) | [Grass Foot Step Sounds (Yo Frankie!)](https://opengameart.org/content/grass-foot-step-sounds-yo-frankie) | CC-BY 3.0 **BY** | Lamoot | 2 | 0.0 | 3,8,16,17,19 |
| [`metal-footsteps-on-concrete`](footsteps/metal-footsteps-on-concrete/) | [Metal footsteps on concrete](https://opengameart.org/content/metal-footsteps-on-concrete) | CC0 | Thimras | 25 | 1.6 | 3,8,16,17,19 |
| [`platformer-sounds-terminal-interaction-door-shots-bang-and-footsteps`](footsteps/platformer-sounds-terminal-interaction-door-shots-bang-and-footsteps/) | [Platformer Sounds: Terminal, Interaction, Door, Shots, Bang ](https://opengameart.org/content/platformer-sounds-terminal-interaction-door-shots-bang-and-footsteps) | CC0 | yd | 12 | 0.3 | 3,8,16,17,19 |
| [`stepping-sounds`](footsteps/stepping-sounds/) | [Stepping Sounds](https://opengameart.org/content/stepping-sounds) | CC-BY 3.0 **BY** | dklon | 9 | 0.1 | 3,8,16,17,19 |
| [`stone-stair-steps`](footsteps/stone-stair-steps/) | [Stone stair steps](https://opengameart.org/content/stone-stair-steps) | CC-BY-SA 3.0 **BY** | dorkster | 1 | 0.0 | 3,8,16,17,19 |
| [`walking-in-and-out-through-wooden-stairs`](footsteps/walking-in-and-out-through-wooden-stairs/) | [Walking in and out through wooden stairs](https://opengameart.org/content/walking-in-and-out-through-wooden-stairs) | CC-BY-SA 3.0 **BY** | Gallaecio | 4 | 0.5 | 3,8,16,17,19 |
| [`walking-on-snow-sound`](footsteps/walking-on-snow-sound/) | [Walking on snow sound](https://opengameart.org/content/walking-on-snow-sound) | CC0 | IgnasD | 2 | 0.0 | 3,8,16,17,19 |
| [`water-splash-and-sand-footsteps`](footsteps/water-splash-and-sand-footsteps/) | [Water Splash and sand footsteps](https://opengameart.org/content/water-splash-and-sand-footsteps) | CC0 | Peludo | 4 | 1.3 | 3,8,16,17,19 |

### breathing-heartbeat/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`breathing-tired`](breathing-heartbeat/breathing-tired/) | [Breathing Tired](https://opengameart.org/content/breathing-tired) | CC0 | mikeask | 1 | 0.5 | 3,19 |
| [`dreaming`](breathing-heartbeat/dreaming/) | [Dreaming](https://opengameart.org/content/dreaming) | CC-BY 3.0 **BY** | marcelofg55 | 1 | 0.2 | 3,19 |
| [`ghost-breath`](breathing-heartbeat/ghost-breath/) | [Ghost breath](https://opengameart.org/content/ghost-breath) | CC0 | qubodup | 1 | 0.7 | 3,19 |
| [`goblin-breathing`](breathing-heartbeat/goblin-breathing/) | [Goblin Breathing](https://opengameart.org/content/goblin-breathing) | CC-BY 3.0 **BY** | spookymodem | 1 | 0.7 | 3,19 |
| [`heartbeat-single-sound`](breathing-heartbeat/heartbeat-single-sound/) | [Heartbeat (single sound)](https://opengameart.org/content/heartbeat-single-sound) | CC0 | qubodup | 1 | 0.1 | 3,19 |
| [`heartbeat-sounds`](breathing-heartbeat/heartbeat-sounds/) | [Heartbeat sounds](https://opengameart.org/content/heartbeat-sounds) | CC0 | bart | 4 | 2.0 | 3,19 |
| [`nhfea-sound`](breathing-heartbeat/nhfea-sound/) | [NHFEA: Sound](https://opengameart.org/content/nhfea-sound) | CC0 | Roars Games | 34 | 12.2 | 3,19 |
| [`silly-me`](breathing-heartbeat/silly-me/) | [Silly me](https://opengameart.org/content/silly-me) | CC0 | Nocturnal_Vanguard | 1 | 0.1 | 3,19 |

### ambience/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`30-cc0-sfx-loops`](ambience/30-cc0-sfx-loops/) | [30 CC0 SFX loops](https://opengameart.org/content/30-cc0-sfx-loops) | CC0 | rubberduck | 30 | 6.9 | 6,8,11,15,16,17,20 |
| [`4-atmospheric-ghostly-loops`](ambience/4-atmospheric-ghostly-loops/) | [4 Atmospheric ghostly loops](https://opengameart.org/content/4-atmospheric-ghostly-loops) | CC0 | qubodup | 4 | 6.5 | 6,8,11,15,16,17,20 |
| [`68-workshop-sounds`](ambience/68-workshop-sounds/) | [68 Workshop Sounds](https://opengameart.org/content/68-workshop-sounds) | CC0 | bart | 68 | 81.0 | 6,8,11,15,16,17,20 |
| [`accident-%F0%9F%94%89`](ambience/accident-%F0%9F%94%89/) | [Accident 🔉](https://opengameart.org/content/accident-%F0%9F%94%89) | CC-BY-SA 4.0 **BY** | Modanung | 1 | 0.2 | 6,8,11,15,16,17,20 |
| [`ambient-bird-cricket-and-frog`](ambience/ambient-bird-cricket-and-frog/) | [Ambient Bird, Cricket and Frog](https://opengameart.org/content/ambient-bird-cricket-and-frog) | CC-BY 3.0 **BY** | Lamoot | 4 | 0.5 | 6,8,11,15,16,17,20 |
| [`ambient-bird-sounds`](ambience/ambient-bird-sounds/) | [Ambient Bird Sounds](https://opengameart.org/content/ambient-bird-sounds) | CC0 | isaiah658 | 1 | 0.5 | 6,8,11,15,16,17,20 |
| [`ambient-mountain-river-wind-and-forest-and-waterfall`](ambience/ambient-mountain-river-wind-and-forest-and-waterfall/) | [Ambient Mountain, River, Wind and Forest and Waterfall](https://opengameart.org/content/ambient-mountain-river-wind-and-forest-and-waterfall) | CC-BY 3.0 **BY** | Lamoot | 6 | 7.6 | 6,8,11,15,16,17,20 |
| [`ambient-pulse-noise`](ambience/ambient-pulse-noise/) | [Ambient Pulse Noise](https://opengameart.org/content/ambient-pulse-noise) | CC-BY-SA 3.0 **BY** | Gobusto | 1 | 0.1 | 6,8,11,15,16,17,20 |
| [`ambient-spaceship-hums`](ambience/ambient-spaceship-hums/) | [Ambient Spaceship Hums](https://opengameart.org/content/ambient-spaceship-hums) | CC-BY 3.0 **BY** | dklon | 2 | 0.2 | 6,8,11,15,16,17,20 |
| [`atmospheric-interaction-sound-pack`](ambience/atmospheric-interaction-sound-pack/) | [Atmospheric Interaction Sound Pack](https://opengameart.org/content/atmospheric-interaction-sound-pack) | CC0 | qubodup | 43 | 22.1 | 6,8,11,15,16,17,20 |
| [`background-rumble-noise`](ambience/background-rumble-noise/) | [Background Rumble Noise](https://opengameart.org/content/background-rumble-noise) | CC-BY 3.0 **BY** | gryc | 3 | 0.5 | 6,8,11,15,16,17,20 |
| [`bird-chirping-sounds`](ambience/bird-chirping-sounds/) | [Bird chirping sounds](https://opengameart.org/content/bird-chirping-sounds) | CC0 | syncopika | 2 | 0.6 | 6,8,11,15,16,17,20 |
| [`birdcricketfrog-and-mosquito-sounds`](ambience/birdcricketfrog-and-mosquito-sounds/) | [Bird,cricket,frog and mosquito sounds](https://opengameart.org/content/birdcricketfrog-and-mosquito-sounds) | CC0 | Aj_ | 7 | 0.9 | 6,8,11,15,16,17,20 |
| [`bubble-sound-effects`](ambience/bubble-sound-effects/) | [Bubble Sound Effects](https://opengameart.org/content/bubble-sound-effects) | CC0 | BMacZero | 5 | 1.1 | 6,8,11,15,16,17,20 |
| [`bubbles-pop`](ambience/bubbles-pop/) | [bubbles 'pop'](https://opengameart.org/content/bubbles-pop) | CC0 | farfadet46 | 1 | 0.0 | 6,8,11,15,16,17,20 |
| [`car-engine-start-01`](ambience/car-engine-start-01/) | [Car engine start 01](https://opengameart.org/content/car-engine-start-01) | CC0 | looneybits | 2 | 0.4 | 6,8,11,15,16,17,20 |
| [`chain-winch-sounds`](ambience/chain-winch-sounds/) | [Chain winch sounds](https://opengameart.org/content/chain-winch-sounds) | CC0 | bart | 9 | 8.8 | 6,8,11,15,16,17,20 |
| [`chirp-loop`](ambience/chirp-loop/) | [Chirp loop](https://opengameart.org/content/chirp-loop) | CC-BY 3.0 **BY** | qubodup | 1 | 0.2 | 6,8,11,15,16,17,20 |
| [`clock-tick-0`](ambience/clock-tick-0/) | [Clock Tick](https://opengameart.org/content/clock-tick-0) | CC0 | IgnasD | 1 | 0.0 | 6,8,11,15,16,17,20 |
| [`clock-ticking`](ambience/clock-ticking/) | [Clock ticking](https://opengameart.org/content/clock-ticking) | CC-BY 3.0 **BY** | christoph | 1 | 0.7 | 6,8,11,15,16,17,20 |
| [`clock-wind-sounds`](ambience/clock-wind-sounds/) | [Clock Wind Sounds](https://opengameart.org/content/clock-wind-sounds) | CC0 | BMacZero | 6 | 0.3 | 6,8,11,15,16,17,20 |
| [`cricket-chirping-loopable`](ambience/cricket-chirping-loopable/) | [cricket chirping (loopable)](https://opengameart.org/content/cricket-chirping-loopable) | CC-BY-SA 4.0 **BY** | sinny | 5 | 0.6 | 6,8,11,15,16,17,20 |
| [`crickets`](ambience/crickets/) | [Crickets](https://opengameart.org/content/crickets) | CC-BY 3.0 **BY** | dklon | 1 | 0.4 | 6,8,11,15,16,17,20 |
| [`crickets-ambient-noise-loopable`](ambience/crickets-ambient-noise-loopable/) | [Crickets Ambient Noise - loopable](https://opengameart.org/content/crickets-ambient-noise-loopable) | CC0 | Wolfgang_ | 1 | 0.2 | 6,8,11,15,16,17,20 |
| [`dark-ambiences`](ambience/dark-ambiences/) | [Dark Ambiences](https://opengameart.org/content/dark-ambiences) | CC0 | Ogrebane | 5 | 6.3 | 6,8,11,15,16,17,20 |
| [`dripping-water`](ambience/dripping-water/) | [Dripping Water](https://opengameart.org/content/dripping-water) | CC-BY 3.0 **BY** | spookymodem | 1 | 0.6 | 6,8,11,15,16,17,20 |
| [`dripping-water-loop`](ambience/dripping-water-loop/) | [Dripping water loop](https://opengameart.org/content/dripping-water-loop) | CC0 | qubodup | 1 | 1.3 | 6,8,11,15,16,17,20 |
| [`dry-bushes`](ambience/dry-bushes/) | [dry bushes](https://opengameart.org/content/dry-bushes) | CC-BY-SA 3.0 **BY** | sinny | 4 | 0.2 | 6,8,11,15,16,17,20 |
| [`engine-sound`](ambience/engine-sound/) | [Engine Sound](https://opengameart.org/content/engine-sound) | CC-BY 3.0 **BY** | kurt | 2 | 1.1 | 6,8,11,15,16,17,20 |
| [`equipment-clicks-ii`](ambience/equipment-clicks-ii/) | [Equipment Clicks II](https://opengameart.org/content/equipment-clicks-ii) | CC0 | LFA | 1 | 0.5 | 6,8,11,15,16,17,20 |
| [`fire-crackling`](ambience/fire-crackling/) | [Fire Crackling](https://opengameart.org/content/fire-crackling) | CC0 | AntumDeluge | 2 | 0.3 | 6,8,11,15,16,17,20 |
| [`fireplace-sound-loop`](ambience/fireplace-sound-loop/) | [Fireplace Sound loop](https://opengameart.org/content/fireplace-sound-loop) | CC0 | PagDev | 1 | 9.8 | 6,8,11,15,16,17,20 |
| [`force-field-electric-hum`](ambience/force-field-electric-hum/) | [Force field electric hum](https://opengameart.org/content/force-field-electric-hum) | CC-BY 4.0 **BY** | Varkalandar | 1 | 0.8 | 6,8,11,15,16,17,20 |
| [`forest-bird-sounds`](ambience/forest-bird-sounds/) | [Forest bird sounds](https://opengameart.org/content/forest-bird-sounds) | CC0 | pauliuw | 15 | 6.4 | 6,8,11,15,16,17,20 |
| [`free-general-ambience-sounds`](ambience/free-general-ambience-sounds/) | [Free General Ambience Sounds](https://opengameart.org/content/free-general-ambience-sounds) | CC-BY-SA 4.0 **BY** | Gregor Quendel | 45 | 117.6 | 6,8,11,15,16,17,20 |
| [`fridge-loop-1`](ambience/fridge-loop-1/) | [Fridge Loop 1](https://opengameart.org/content/fridge-loop-1) | CC0 | Kresiek The Furry | 3 | 0.9 | 6,8,11,15,16,17,20 |
| [`ghost`](ambience/ghost/) | [Ghost](https://opengameart.org/content/ghost) | CC0 | Ogrebane | 1 | 0.8 | 6,8,11,15,16,17,20 |
| [`ghostly-humming`](ambience/ghostly-humming/) | [Ghostly Humming](https://opengameart.org/content/ghostly-humming) | CC0 | Nocturnal_Vanguard | 1 | 1.3 | 6,8,11,15,16,17,20 |
| [`gull-sounds`](ambience/gull-sounds/) | [Gull Sounds](https://opengameart.org/content/gull-sounds) | CC-BY-SA 3.0 **BY** | AntumDeluge | 6 | 0.8 | 6,8,11,15,16,17,20 |
| [`high-traffic-road-sounds`](ambience/high-traffic-road-sounds/) | [High traffic road sounds](https://opengameart.org/content/high-traffic-road-sounds) | CC0 | IgnasD | 1 | 0.5 | 6,8,11,15,16,17,20 |
| [`kitchen-ambience-sfx`](ambience/kitchen-ambience-sfx/) | [Kitchen Ambience, SFX](https://opengameart.org/content/kitchen-ambience-sfx) | CC-BY 4.0 **BY** | DavidW | 5 | 53.2 | 6,8,11,15,16,17,20 |
| [`loopable-dungeon-ambience`](ambience/loopable-dungeon-ambience/) | [Loopable Dungeon Ambience](https://opengameart.org/content/loopable-dungeon-ambience) | CC0 | JaggedStone | 1 | 1.6 | 6,8,11,15,16,17,20 |
| [`nature-sounds-pack`](ambience/nature-sounds-pack/) | [Nature Sounds Pack](https://opengameart.org/content/nature-sounds-pack) | CC-BY 4.0 **BY** | Antoinemax | 23 | 21.2 | 6,8,11,15,16,17,20 |
| [`rain-and-thunder-loop`](ambience/rain-and-thunder-loop/) | [Rain and Thunder Loop](https://opengameart.org/content/rain-and-thunder-loop) | CC-BY 3.0 **BY** | DoKashiteru | 1 | 4.3 | 6,8,11,15,16,17,20 |
| [`rain-gutter-loop`](ambience/rain-gutter-loop/) | [Rain in the Gutter Loop](https://opengameart.org/content/rain-gutter-loop) | CC0 | Ogrebane | 1 | 1.4 | 6,8,11,15,16,17,20 |
| [`rain-long-thunder`](ambience/rain-long-thunder/) | [Rain + Long Thunder](https://opengameart.org/content/rain-long-thunder) | CC0 | WuxiaScrub | 1 | 0.8 | 6,8,11,15,16,17,20 |
| [`rain-loopable`](ambience/rain-loopable/) | [Rain (loopable)](https://opengameart.org/content/rain-loopable) | CC0 | Ylmir | 8 | 11.4 | 6,8,11,15,16,17,20 |
| [`reversing-time-stuck-in-time`](ambience/reversing-time-stuck-in-time/) | [Reversing Time / Stuck in Time](https://opengameart.org/content/reversing-time-stuck-in-time) | CC0 | isaiah658 | 2 | 4.4 | 6,8,11,15,16,17,20 |
| [`scary-echoey-horn-esque-sound`](ambience/scary-echoey-horn-esque-sound/) | [Scary Echoey Horn Esque Sound](https://opengameart.org/content/scary-echoey-horn-esque-sound) | CC-BY 4.0 **BY** | Kat | 3 | 13.2 | 6,8,11,15,16,17,20 |
| [`sci-fi-ambience-sfx`](ambience/sci-fi-ambience-sfx/) | [Sci-fi ambience SFX](https://opengameart.org/content/sci-fi-ambience-sfx) | CC0 | Fun Gi Development | 2 | 0.4 | 6,8,11,15,16,17,20 |
| [`sci-fi-background-noise`](ambience/sci-fi-background-noise/) | [Sci-Fi Background noise](https://opengameart.org/content/sci-fi-background-noise) | CC0 | Spring Spring | 1 | 1.5 | 6,8,11,15,16,17,20 |
| [`sci-fi-drone-loop`](ambience/sci-fi-drone-loop/) | [Sci-Fi Drone Loop](https://opengameart.org/content/sci-fi-drone-loop) | CC-BY 3.0 **BY** | jdagenet | 1 | 1.3 | 6,8,11,15,16,17,20 |
| [`scifi-city-ambient-loop`](ambience/scifi-city-ambient-loop/) | [Scifi City - Ambient Loop](https://opengameart.org/content/scifi-city-ambient-loop) | CC0 | TinyWorlds | 2 | 0.6 | 6,8,11,15,16,17,20 |
| [`ship-sinking`](ambience/ship-sinking/) | [Ship sinking](https://opengameart.org/content/ship-sinking) | CC0 | Thimras | 1 | 1.1 | 6,8,11,15,16,17,20 |
| [`sirens-and-alarm-noise`](ambience/sirens-and-alarm-noise/) | [Sirens and Alarm Noise](https://opengameart.org/content/sirens-and-alarm-noise) | CC0 | aquinn | 1 | 0.6 | 6,8,11,15,16,17,20 |
| [`skippy-fish-water-sound-collection`](ambience/skippy-fish-water-sound-collection/) | [Skippy Fish Water Sound Collection](https://opengameart.org/content/skippy-fish-water-sound-collection) | CC0 | jcpmcdonald | 18 | 1.2 | 6,8,11,15,16,17,20 |
| [`slow-clock-ticking-seamless-looping-sfx-sound-effect`](ambience/slow-clock-ticking-seamless-looping-sfx-sound-effect/) | [Slow Clock Ticking, Seamless Looping, Sfx Sound Effect](https://opengameart.org/content/slow-clock-ticking-seamless-looping-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 2.0 | 6,8,11,15,16,17,20 |
| [`steam-boiler-sound-loop`](ambience/steam-boiler-sound-loop/) | [Steam boiler sound loop](https://opengameart.org/content/steam-boiler-sound-loop) | CC0 | bart | 1 | 3.7 | 6,8,11,15,16,17,20 |
| [`storm-arwen-2022`](ambience/storm-arwen-2022/) | [Storm Arwen 2022](https://opengameart.org/content/storm-arwen-2022) | CC-BY 4.0 **BY** | Tsorthan Grove | 1 | 21.7 | 6,8,11,15,16,17,20 |
| [`storm-siren`](ambience/storm-siren/) | [Storm & Siren](https://opengameart.org/content/storm-siren) | CC0 | TinyWorlds | 2 | 2.3 | 6,8,11,15,16,17,20 |
| [`swamp-environment-audio`](ambience/swamp-environment-audio/) | [Swamp Environment Audio](https://opengameart.org/content/swamp-environment-audio) | CC0 | LokiF | 20 | 20.4 | 6,8,11,15,16,17,20 |
| [`the-shop`](ambience/the-shop/) | [The Shop](https://opengameart.org/content/the-shop) | CC0 | LEGIT Audio | 4 | 48.5 | 6,8,11,15,16,17,20 |
| [`thunder-lightning-ambience-field-recording`](ambience/thunder-lightning-ambience-field-recording/) | [Thunder / Lightning Ambience - Field Recording](https://opengameart.org/content/thunder-lightning-ambience-field-recording) | CC-BY 4.0 **BY** | Gregor Quendel | 1 | 25.9 | 6,8,11,15,16,17,20 |
| [`thunder-very-close-rain-01`](ambience/thunder-very-close-rain-01/) | [Thunder, Very Close, Rain, 01](https://opengameart.org/content/thunder-very-close-rain-01) | CC-BY 3.0 **BY** | InspectorJ | 1 | 2.2 | 6,8,11,15,16,17,20 |
| [`tick-and-tock`](ambience/tick-and-tock/) | [Tick and Tock](https://opengameart.org/content/tick-and-tock) | CC0 | cemkalyoncu | 2 | 0.0 | 6,8,11,15,16,17,20 |
| [`ticking-clock`](ambience/ticking-clock/) | [Ticking clock.](https://opengameart.org/content/ticking-clock) | CC0 | bart | 5 | 3.2 | 6,8,11,15,16,17,20 |
| [`ticking-clock-0`](ambience/ticking-clock-0/) | [Ticking Clock](https://opengameart.org/content/ticking-clock-0) | CC0 | AntumDeluge | 2 | 0.4 | 6,8,11,15,16,17,20 |
| [`underwater-or-space-engine-rumble`](ambience/underwater-or-space-engine-rumble/) | [underwater or space engine rumble](https://opengameart.org/content/underwater-or-space-engine-rumble) | CC0 | gmason | 4 | 1.9 | 6,8,11,15,16,17,20 |
| [`upside-down-grin-freaky-ambient`](ambience/upside-down-grin-freaky-ambient/) | [Upside Down Grin (freaky Ambient)](https://opengameart.org/content/upside-down-grin-freaky-ambient) | CC0 | HaelDB | 1 | 1.5 | 6,8,11,15,16,17,20 |
| [`ventilation-version2`](ambience/ventilation-version2/) | [Ventilation version2](https://opengameart.org/content/ventilation-version2) | CC-BY 3.0 **BY** | MidFag | 1 | 0.1 | 6,8,11,15,16,17,20 |
| [`ventilationvariant1`](ambience/ventilationvariant1/) | [Ventilation_variant1](https://opengameart.org/content/ventilationvariant1) | CC-BY 3.0 **BY** | MidFag | 1 | 0.1 | 6,8,11,15,16,17,20 |
| [`water-harp`](ambience/water-harp/) | [Water harp](https://opengameart.org/content/water-harp) | CC-BY-SA 3.0 **BY** | qubodup | 7 | 1.9 | 6,8,11,15,16,17,20 |
| [`wind1`](ambience/wind1/) | [wind1](https://opengameart.org/content/wind1) | CC0 | Luke.RUSTLTD | 5 | 25.2 | 6,8,11,15,16,17,20 |

### foley-props/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`10-book-page-flips`](foley-props/10-book-page-flips/) | [10 Book Page Flips](https://opengameart.org/content/10-book-page-flips) | CC0 | StarNinjas | 10 | 0.3 | 4,5,9,10,13,17,18,19 |
| [`100-cc0-metal-and-wood-sfx`](foley-props/100-cc0-metal-and-wood-sfx/) | [100 CC0 metal and wood SFX](https://opengameart.org/content/100-cc0-metal-and-wood-sfx) | CC0 | rubberduck | 100 | 4.0 | 4,5,9,10,13,17,18,19 |
| [`16-button-clicks`](foley-props/16-button-clicks/) | [16 button clicks](https://opengameart.org/content/16-button-clicks) | CC0 | qubodup | 16 | 0.8 | 4,5,9,10,13,17,18,19 |
| [`202-more-sound-effects`](foley-props/202-more-sound-effects/) | [202 More Sound Effects](https://opengameart.org/content/202-more-sound-effects) | CC0 | OwlishMedia | 202 | 37.7 | 4,5,9,10,13,17,18,19 |
| [`4-door-closes`](foley-props/4-door-closes/) | [4 Door Closes](https://opengameart.org/content/4-door-closes) | CC0 | StarNinjas | 4 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`4-metal-dingsrings`](foley-props/4-metal-dingsrings/) | [4 Metal Dings/Rings](https://opengameart.org/content/4-metal-dingsrings) | CC0 | StarNinjas | 4 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`51-ui-sound-effects-buttons-switches-and-clicks`](foley-props/51-ui-sound-effects-buttons-switches-and-clicks/) | [51 UI sound effects (buttons, switches and clicks)](https://opengameart.org/content/51-ui-sound-effects-buttons-switches-and-clicks) | CC0 | Kenney | 51 | 3.6 | 4,5,9,10,13,17,18,19 |
| [`75-cc0-breaking-falling-hit-sfx`](foley-props/75-cc0-breaking-falling-hit-sfx/) | [75 CC0 breaking / falling / hit sfx](https://opengameart.org/content/75-cc0-breaking-falling-hit-sfx) | CC0 | rubberduck | 75 | 3.3 | 4,5,9,10,13,17,18,19 |
| [`80-cc0-rpg-sfx`](foley-props/80-cc0-rpg-sfx/) | [80 CC0 RPG SFX](https://opengameart.org/content/80-cc0-rpg-sfx) | CC0 | rubberduck | 80 | 3.7 | 4,5,9,10,13,17,18,19 |
| [`beep-sound`](foley-props/beep-sound/) | [Beep Sound](https://opengameart.org/content/beep-sound) | CC0 | Test User | 1 | 0.0 | 4,5,9,10,13,17,18,19 |
| [`breaking-bottle`](foley-props/breaking-bottle/) | [Breaking Bottle](https://opengameart.org/content/breaking-bottle) | CC-BY 3.0 **BY** | spookymodem | 1 | 0.2 | 4,5,9,10,13,17,18,19 |
| [`cabinet-lock-sfx-sound-effect`](foley-props/cabinet-lock-sfx-sound-effect/) | [Cabinet Lock Sfx Sound Effect](https://opengameart.org/content/cabinet-lock-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.2 | 4,5,9,10,13,17,18,19 |
| [`camera`](foley-props/camera/) | [Camera](https://opengameart.org/content/camera) | CC0 | themightyglider | 1 | 0.0 | 4,5,9,10,13,17,18,19 |
| [`camerashudder`](foley-props/camerashudder/) | [CameraShudder](https://opengameart.org/content/camerashudder) | CC0 | FacadeGaikan | 1 | 0.0 | 4,5,9,10,13,17,18,19 |
| [`cardoorsfx`](foley-props/cardoorsfx/) | [Car_door_SFX](https://opengameart.org/content/cardoorsfx) | CC0 | looneybits | 2 | 0.3 | 4,5,9,10,13,17,18,19 |
| [`church-bell`](foley-props/church-bell/) | [Church Bell](https://opengameart.org/content/church-bell) | CC-BY-SA 3.0 **BY** | qubodup | 1 | 0.6 | 4,5,9,10,13,17,18,19 |
| [`commons-projector-camera-tape`](foley-props/commons-projector-camera-tape/) | [Wikimedia Commons audio: commons-projector-camera-tape](https://commons.wikimedia.org/) | mixed per file: CC BY 4.0, CC BY-SA 3.0, Public domain **BY** | various (per file) | 3 | 0.9 | 4,18 |
| [`commons-telephone`](foley-props/commons-telephone/) | [Wikimedia Commons audio: commons-telephone](https://commons.wikimedia.org/) | mixed per file: CC BY 3.0, CC BY 4.0, CC BY-SA 3.0, CC BY-SA 4.0, CC0, Public Domain, Public domain **BY** | various (per file) | 37 | 21.6 | 10 |
| [`crank-movie-telephone-ringtone`](foley-props/crank-movie-telephone-ringtone/) | [Crank movie telephone ringtone](https://opengameart.org/content/crank-movie-telephone-ringtone) | CC0 | cyberdyne | 1 | 0.2 | 4,5,9,10,13,17,18,19 |
| [`creaky-light-wooden-door`](foley-props/creaky-light-wooden-door/) | [Creaky light wooden door](https://opengameart.org/content/creaky-light-wooden-door) | CC-BY 3.0 **BY** | fractilegames | 1 | 1.2 | 4,5,9,10,13,17,18,19 |
| [`cup-on-table-sfx-sound-effect`](foley-props/cup-on-table-sfx-sound-effect/) | [Cup On Table Sfx Sound Effect](https://opengameart.org/content/cup-on-table-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.3 | 4,5,9,10,13,17,18,19 |
| [`dialog-vocal-samples`](foley-props/dialog-vocal-samples/) | [Dialog vocal samples](https://opengameart.org/content/dialog-vocal-samples) | CC0 | MirceaKitsune | 32 | 0.4 | 4,5,9,10,13,17,18,19 |
| [`door-open-door-close-set`](foley-props/door-open-door-close-set/) | [Door Open, Door Close Set](https://opengameart.org/content/door-open-door-close-set) | CC0 | qubodup | 36 | 6.5 | 4,5,9,10,13,17,18,19 |
| [`doorbell-ring`](foley-props/doorbell-ring/) | [Doorbell ring](https://opengameart.org/content/doorbell-ring) | CC0 | qubodup | 1 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`double-click-mouse-sfx-sound-effect`](foley-props/double-click-mouse-sfx-sound-effect/) | [Double Click Mouse Sfx Sound Effect](https://opengameart.org/content/double-click-mouse-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.3 | 4,5,9,10,13,17,18,19 |
| [`elevator-ding`](foley-props/elevator-ding/) | [Elevator ding](https://opengameart.org/content/elevator-ding) | CC0 | fvcalderan | 1 | 0.0 | 4,5,9,10,13,17,18,19 |
| [`elevatordoor`](foley-props/elevatordoor/) | [Elevator_Door](https://opengameart.org/content/elevatordoor) | CC0 | PagDev | 1 | 1.2 | 4,5,9,10,13,17,18,19 |
| [`equipment-clicks-iii`](foley-props/equipment-clicks-iii/) | [equipment clicks III](https://opengameart.org/content/equipment-clicks-iii) | CC0 | LFA | 1 | 1.9 | 4,5,9,10,13,17,18,19 |
| [`fantasy-accessory-sfx-library`](foley-props/fantasy-accessory-sfx-library/) | [Fantasy Accessory SFX Library](https://opengameart.org/content/fantasy-accessory-sfx-library) | CC0 | Vehicle | 156 | 42.5 | 4,5,9,10,13,17,18,19 |
| [`fantasy-sound-effects-tinysized-sfx`](foley-props/fantasy-sound-effects-tinysized-sfx/) | [Fantasy Sound Effects (Tinysized SFX)](https://opengameart.org/content/fantasy-sound-effects-tinysized-sfx) | CC0 | Vehicle | 97 | 45.8 | 4,5,9,10,13,17,18,19 |
| [`glass-break`](foley-props/glass-break/) | [Glass Break](https://opengameart.org/content/glass-break) | CC0 | TinyWorlds | 1 | 0.2 | 4,5,9,10,13,17,18,19 |
| [`gui-sound-effects`](foley-props/gui-sound-effects/) | [GUI Sound Effects](https://opengameart.org/content/gui-sound-effects) | CC0 | LokiF | 13 | 6.5 | 4,5,9,10,13,17,18,19 |
| [`horror-cinema-8`](foley-props/horror-cinema-8/) | [Horror Cinema 8](https://opengameart.org/content/horror-cinema-8) | CC-BY-SA 3.0 **BY** | Cadere Sounds | 1 | 4.0 | 4,5,9,10,13,17,18,19 |
| [`impact`](foley-props/impact/) | [Impact](https://opengameart.org/content/impact) | CC0 | qubodup | 10 | 0.3 | 4,5,9,10,13,17,18,19 |
| [`interface-sounds`](foley-props/interface-sounds/) | [Interface Sounds](https://opengameart.org/content/interface-sounds) | CC0 | Kenney | 100 | 1.7 | 4,5,9,10,13,17,18,19 |
| [`inventory-sound-effects`](foley-props/inventory-sound-effects/) | [Inventory Sound Effects](https://opengameart.org/content/inventory-sound-effects) | CC-BY 3.0 **BY** | Ogrebane | 6 | 1.2 | 4,5,9,10,13,17,18,19 |
| [`item-handling`](foley-props/item-handling/) | [Item Handling](https://opengameart.org/content/item-handling) | CC-BY 3.0 **BY** | qubodup | 10 | 1.3 | 4,5,9,10,13,17,18,19 |
| [`light-switch-on-sfx-sound-effect`](foley-props/light-switch-on-sfx-sound-effect/) | [Light Switch On Sfx Sound Effect](https://opengameart.org/content/light-switch-on-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`menu-selection-click`](foley-props/menu-selection-click/) | [Menu Selection Click](https://opengameart.org/content/menu-selection-click) | CC-BY 3.0 **BY** | NenadSimic | 1 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`metal-interactions`](foley-props/metal-interactions/) | [Metal Interactions](https://opengameart.org/content/metal-interactions) | CC0 | qubodup | 5 | 0.4 | 4,5,9,10,13,17,18,19 |
| [`modern-ringtone-chirptone`](foley-props/modern-ringtone-chirptone/) | [Modern Ringtone [Chirptone]](https://opengameart.org/content/modern-ringtone-chirptone) | CC0 | Zane Little Music | 1 | 0.8 | 4,5,9,10,13,17,18,19 |
| [`office-chair-roll-sfx-sound-effect`](foley-props/office-chair-roll-sfx-sound-effect/) | [Office Chair Roll Sfx Sound Effect](https://opengameart.org/content/office-chair-roll-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.5 | 4,5,9,10,13,17,18,19 |
| [`old-elevator-door`](foley-props/old-elevator-door/) | [old elevator door](https://opengameart.org/content/old-elevator-door) | CC0 | sinny | 1 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`opening-and-closing-a-map-sounds`](foley-props/opening-and-closing-a-map-sounds/) | [Opening and Closing a Map Sounds](https://opengameart.org/content/opening-and-closing-a-map-sounds) | CC0 | Spring Spring | 2 | 1.2 | 4,5,9,10,13,17,18,19 |
| [`page-turning-sfx-sound-effect`](foley-props/page-turning-sfx-sound-effect/) | [Page Turning Sfx Sound Effect](https://opengameart.org/content/page-turning-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.2 | 4,5,9,10,13,17,18,19 |
| [`paper-crumple-sfx-sound-effect`](foley-props/paper-crumple-sfx-sound-effect/) | [Paper Crumple Sfx Sound Effect](https://opengameart.org/content/paper-crumple-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.3 | 4,5,9,10,13,17,18,19 |
| [`pen-click-sfx-sound-effect`](foley-props/pen-click-sfx-sound-effect/) | [Pen Click Sfx Sound Effect](https://opengameart.org/content/pen-click-sfx-sound-effect) | CC-BY 4.0 **BY** | unknown | 1 | 0.2 | 4,5,9,10,13,17,18,19 |
| [`pencil-sounds`](foley-props/pencil-sounds/) | [Pencil Sounds](https://opengameart.org/content/pencil-sounds) | CC0 | AntumDeluge | 4 | 0.5 | 4,5,9,10,13,17,18,19 |
| [`point-bell`](foley-props/point-bell/) | [Point bell](https://opengameart.org/content/point-bell) | CC0 | HaelDB | 1 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`random-sfx`](foley-props/random-sfx/) | [Random SFX](https://opengameart.org/content/random-sfx) | CC0 | Écrivain | 54 | 14.2 | 4,5,9,10,13,17,18,19 |
| [`random-sound-effects`](foley-props/random-sound-effects/) | [Random Sound effects](https://opengameart.org/content/random-sound-effects) | CC0 | HaelDB | 1 | 1.0 | 4,5,9,10,13,17,18,19 |
| [`rpg-sound-pack`](foley-props/rpg-sound-pack/) | [RPG Sound Pack](https://opengameart.org/content/rpg-sound-pack) | CC0 | artisticdude | 96 | 27.2 | 4,5,9,10,13,17,18,19 |
| [`scissors`](foley-props/scissors/) | [Scissors](https://opengameart.org/content/scissors) | CC0 | themightyglider | 1 | 0.0 | 4,5,9,10,13,17,18,19 |
| [`shears`](foley-props/shears/) | [Shears](https://opengameart.org/content/shears) | CC-BY 3.0 **BY** | AntumDeluge | 4 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`sound-effects-pack`](foley-props/sound-effects-pack/) | [Sound Effects Pack](https://opengameart.org/content/sound-effects-pack) | CC0 | OwlishMedia | 161 | 201.8 | 4,5,9,10,13,17,18,19 |
| [`super-foley-pack`](foley-props/super-foley-pack/) | [Super Foley Pack](https://opengameart.org/content/super-foley-pack) | CC-BY 3.0 **BY** | dklon | 12 | 45.4 | 4,5,9,10,13,17,18,19 |
| [`tape-recorder-opening-and-closing-sound-effects`](foley-props/tape-recorder-opening-and-closing-sound-effects/) | [Tape Recorder Opening and Closing Sound Effects](https://opengameart.org/content/tape-recorder-opening-and-closing-sound-effects) | CC0 | Spring Spring | 2 | 0.8 | 4,5,9,10,13,17,18,19 |
| [`thunder`](foley-props/thunder/) | [Thunder](https://opengameart.org/content/thunder) | CC-BY 3.0 **BY** | Jerimee | 2 | 11.2 | 4,5,9,10,13,17,18,19 |
| [`various-scissors`](foley-props/various-scissors/) | [various scissors](https://opengameart.org/content/various-scissors) | CC0 | sinny | 5 | 0.1 | 4,5,9,10,13,17,18,19 |
| [`various-sound-effects`](foley-props/various-sound-effects/) | [Various Sound Effects](https://opengameart.org/content/various-sound-effects) | CC0 | laleksic | 27 | 6.2 | 4,5,9,10,13,17,18,19 |
| [`vinyl`](foley-props/vinyl/) | [Vinyl](https://opengameart.org/content/vinyl) | CC0 | Aidan_Walker | 1 | 0.0 | 4,5,9,10,13,17,18,19 |
| [`writing-scribbles`](foley-props/writing-scribbles/) | [Writing Scribbles](https://opengameart.org/content/writing-scribbles) | CC-BY-SA 4.0 **BY** | Wandering Door Games | 16 | 1.3 | 4,5,9,10,13,17,18,19 |
| [`yucchis-assorted-sounds-1`](foley-props/yucchis-assorted-sounds-1/) | [Yucchi's Assorted Sounds 1](https://opengameart.org/content/yucchis-assorted-sounds-1) | CC-BY 3.0 **BY** | YucchiNyan | 32 | 2.2 | 4,5,9,10,13,17,18,19 |
| [`zipper`](foley-props/zipper/) | [Zipper](https://opengameart.org/content/zipper) | CC0 | AntumDeluge | 2 | 0.1 | 4,5,9,10,13,17,18,19 |

### audience/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`applause`](audience/applause/) | [Applause](https://opengameart.org/content/applause) | CC-BY 3.0 **BY** | LeeZH | 1 | 1.4 | 5 |
| [`applause-in-a-large-hall-or-church`](audience/applause-in-a-large-hall-or-church/) | [Applause in a large hall or church](https://opengameart.org/content/applause-in-a-large-hall-or-church) | CC0 | eXpl0it3r | 1 | 6.6 | 5 |
| [`boo-voice-pack-female-tomboyish-low-tone-voice-over-vocal-sound-for-character`](audience/boo-voice-pack-female-tomboyish-low-tone-voice-over-vocal-sound-for-character/) | ["Boo!" voice pack Female tomboyish low-tone voice over](https://opengameart.org/content/boo-voice-pack-female-tomboyish-low-tone-voice-over-vocal-sound-for-character) | CC-BY 3.0 **BY** | JeanMyna_VA | 11 | 5.1 | 5 |
| [`character-quotes`](audience/character-quotes/) | [Character quotes](https://opengameart.org/content/character-quotes) | CC-BY 3.0 **BY** | HaelDB | 22 | 3.7 | 5 |
| [`commons-audience`](audience/commons-audience/) | [Wikimedia Commons audio: commons-audience](https://commons.wikimedia.org/) | mixed per file: CC0, Public domain **BY** | various (per file) | 2 | 1.3 | 5 |
| [`evil-laugh`](audience/evil-laugh/) | [Evil Laugh](https://opengameart.org/content/evil-laugh) | CC0 | AntumDeluge | 1 | 0.0 | 5 |
| [`evil-laughter-0`](audience/evil-laughter-0/) | [evil laughter](https://opengameart.org/content/evil-laughter-0) | CC0 | VennStone | 1 | 0.2 | 5 |
| [`fireworks-with-applause-happy-people`](audience/fireworks-with-applause-happy-people/) | [Fireworks With Applause Happy People](https://opengameart.org/content/fireworks-with-applause-happy-people) | CC0 | Almitory | 1 | 3.6 | 5 |
| [`free-crowd-cheering-sounds`](audience/free-crowd-cheering-sounds/) | [Free Crowd Cheering Sounds](https://opengameart.org/content/free-crowd-cheering-sounds) | CC-BY 4.0 **BY** | Gregor Quendel | 11 | 37.7 | 5 |
| [`happy-halloween-group`](audience/happy-halloween-group/) | [Happy Halloween! (Group)](https://opengameart.org/content/happy-halloween-group) | CC0 | Nocturnal_Vanguard | 1 | 0.0 | 5 |
| [`well-done`](audience/well-done/) | [Well Done](https://opengameart.org/content/well-done) | CC0 | qubodup | 2 | 0.3 | 5 |
| [`witch-cackle`](audience/witch-cackle/) | [Witch Cackle](https://opengameart.org/content/witch-cackle) | CC0 | AntumDeluge | 1 | 0.0 | 5 |

### ice/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`35-wooden-crackshitsdestructions`](ice/35-wooden-crackshitsdestructions/) | [35 wooden cracks/hits/destructions](https://opengameart.org/content/35-wooden-crackshitsdestructions) | CC0 | qubodup | 35 | 8.2 | 16 |
| [`4-dry-snow-steps`](ice/4-dry-snow-steps/) | [4 dry snow steps](https://opengameart.org/content/4-dry-snow-steps) | CC0 | qubodup | 4 | 0.6 | 16 |
| [`41-snow-shoe-steps`](ice/41-snow-shoe-steps/) | [41 snow shoe steps](https://opengameart.org/content/41-snow-shoe-steps) | CC0 | qubodup | 41 | 8.7 | 16 |
| [`5-break-crunch-impacts`](ice/5-break-crunch-impacts/) | [5 break, crunch impacts](https://opengameart.org/content/5-break-crunch-impacts) | CC0 | qubodup | 5 | 0.6 | 16 |
| [`9-wet-snow-steps`](ice/9-wet-snow-steps/) | [9 wet snow steps](https://opengameart.org/content/9-wet-snow-steps) | CC0 | qubodup | 9 | 2.2 | 16 |
| [`cracking-sounds`](ice/cracking-sounds/) | [Cracking Sounds](https://opengameart.org/content/cracking-sounds) | CC-BY 4.0 **BY** | Varkalandar | 2 | 0.3 | 16 |
| [`ice-breakingshattering`](ice/ice-breakingshattering/) | [Ice breaking/shattering](https://opengameart.org/content/ice-breakingshattering) | CC0 | IgnasD | 5 | 0.1 | 16 |
| [`leaves-cracking-and-crumbling`](ice/leaves-cracking-and-crumbling/) | [leaves cracking and crumbling](https://opengameart.org/content/leaves-cracking-and-crumbling) | CC-BY 3.0 **BY** | qubodup | 79 | 1.1 | 16 |
| [`water-flowing-sound`](ice/water-flowing-sound/) | [Water flowing sound](https://opengameart.org/content/water-flowing-sound) | CC-BY 3.0 **BY** | erxer1 | 1 | 0.5 | 16 |

### horror-drones/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`25-spooky-sound-effects`](horror-drones/25-spooky-sound-effects/) | [25 spooky sound effects](https://opengameart.org/content/25-spooky-sound-effects) | CC-BY 3.0 **BY** | bart | 26 | 27.2 | 3,6,8,11,15,16,17,18,19 |
| [`a-kinda-cool-sound-effect`](horror-drones/a-kinda-cool-sound-effect/) | [A Kinda Cool Sound Effect](https://opengameart.org/content/a-kinda-cool-sound-effect) | CC0 | Spring Spring | 1 | 0.1 | 3,6,8,11,15,16,17,18,19 |
| [`a-lotta-bones-sound-fx`](horror-drones/a-lotta-bones-sound-fx/) | [A Lotta Bones Sound FX](https://opengameart.org/content/a-lotta-bones-sound-fx) | CC-BY 3.0 **BY** | Nerdzmasterz | 1 | 0.0 | 3,6,8,11,15,16,17,18,19 |
| [`ambientguitar001`](horror-drones/ambientguitar001/) | [AmbientGuitar_001](https://opengameart.org/content/ambientguitar001) | CC-BY 3.0 **BY** | jdagenet | 1 | 4.0 | 3,6,8,11,15,16,17,18,19 |
| [`dark-factory`](horror-drones/dark-factory/) | [Dark Factory](https://opengameart.org/content/dark-factory) | CC-BY 3.0 **BY** | bart | 1 | 2.9 | 3,6,8,11,15,16,17,18,19 |
| [`day-1-cinematic-transition-sound`](horror-drones/day-1-cinematic-transition-sound/) | ["Day 1" Cinematic transition sound](https://opengameart.org/content/day-1-cinematic-transition-sound) | CC-BY 3.0 **BY** | copyc4t | 3 | 0.8 | 3,6,8,11,15,16,17,18,19 |
| [`dreamscape-drone`](horror-drones/dreamscape-drone/) | [Dreamscape Drone](https://opengameart.org/content/dreamscape-drone) | CC-BY 3.0 **BY** | jdagenet | 1 | 5.3 | 3,6,8,11,15,16,17,18,19 |
| [`ghost-monster-voice-moaning-growling`](horror-drones/ghost-monster-voice-moaning-growling/) | [Ghost Monster Voice Moaning & Growling](https://opengameart.org/content/ghost-monster-voice-moaning-growling) | CC0 | qubodup | 10 | 8.1 | 3,6,8,11,15,16,17,18,19 |
| [`horror-ambient`](horror-drones/horror-ambient/) | [Horror Ambient](https://opengameart.org/content/horror-ambient) | CC-BY 3.0 **BY** | Vinrax | 3 | 3.2 | 3,6,8,11,15,16,17,18,19 |
| [`horror-scream1`](horror-drones/horror-scream1/) | [Horror scream1](https://opengameart.org/content/horror-scream1) | CC0 | Vinrax | 1 | 0.2 | 3,6,8,11,15,16,17,18,19 |
| [`horror-screams-drone`](horror-drones/horror-screams-drone/) | [Horror Screams Drone](https://opengameart.org/content/horror-screams-drone) | CC-BY-SA 4.0 **BY** | LogLogGames | 1 | 8.9 | 3,6,8,11,15,16,17,18,19 |
| [`horror-sound-effects-library`](horror-drones/horror-sound-effects-library/) | [Horror Sound Effects Library](https://opengameart.org/content/horror-sound-effects-library) | CC-BY 3.0 **BY** | Little Robot Sound Factory | 138 | 75.4 | 3,6,8,11,15,16,17,18,19 |
| [`i-see-you-voice`](horror-drones/i-see-you-voice/) | ["I see you" Voice](https://opengameart.org/content/i-see-you-voice) | CC0 | Vinrax | 1 | 0.1 | 3,6,8,11,15,16,17,18,19 |
| [`is-anybody-home`](horror-drones/is-anybody-home/) | [Is Anybody Home?](https://opengameart.org/content/is-anybody-home) | CC-BY-SA 3.0 **BY** | jdagenet | 1 | 5.3 | 3,6,8,11,15,16,17,18,19 |
| [`realization`](horror-drones/realization/) | [Realization](https://opengameart.org/content/realization) | CC-BY-SA 3.0 **BY** | jdagenet | 1 | 0.8 | 3,6,8,11,15,16,17,18,19 |
| [`soled-bad-memory`](horror-drones/soled-bad-memory/) | [soled - bad memory](https://opengameart.org/content/soled-bad-memory) | CC-BY-SA 4.0 **BY** | soled | 1 | 9.3 | 3,6,8,11,15,16,17,18,19 |
| [`the-chaos-has-risen`](horror-drones/the-chaos-has-risen/) | [The Chaos Has Risen](https://opengameart.org/content/the-chaos-has-risen) | CC-BY 3.0 **BY** | jdagenet | 2 | 14.4 | 3,6,8,11,15,16,17,18,19 |
| [`wind`](horror-drones/wind/) | [Wind](https://opengameart.org/content/wind) | CC0 | IgnasD | 6 | 0.6 | 3,6,8,11,15,16,17,18,19 |
| [`zombies-sound-pack`](horror-drones/zombies-sound-pack/) | [Zombies Sound Pack](https://opengameart.org/content/zombies-sound-pack) | CC0 | artisticdude | 24 | 9.3 | 3,6,8,11,15,16,17,18,19 |

### voice-publicdomain/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`Apollo11Audio`](voice-publicdomain/Apollo11Audio/) | [Apollo 11](https://archive.org/details/Apollo11Audio) | Public Domain (PDM 1.0) | NASA | 6 | 48.3 | 2,10,12,18 |
| [`Apollo13Audio`](voice-publicdomain/Apollo13Audio/) | [Apollo 13](https://archive.org/details/Apollo13Audio) | Public Domain (PDM 1.0) | John Stoll | 1 | 30.9 | 2,10,12,18 |
| [`EDIS-SRP-0199-05`](voice-publicdomain/EDIS-SRP-0199-05/) | [The birth of the telephone](https://archive.org/details/EDIS-SRP-0199-05) | Public Domain | Edison | 1 | 3.7 | 2,10,12,18 |
| [`EDIS-SRP-0206-01`](voice-publicdomain/EDIS-SRP-0206-01/) | [Transcontinental telephone address to Thomas A. Edison](https://archive.org/details/EDIS-SRP-0206-01) | Public Domain | Miller Reese Hutchinson | 1 | 2.8 | 2,10,12,18 |
| [`eves_diary_librivox`](voice-publicdomain/eves_diary_librivox/) | [Eve's Diary](https://archive.org/details/eves_diary_librivox) | Public Domain | Mark Twain | 3 | 17.4 | 2,9,10,12,18 |
| [`extracts_adams_diary`](voice-publicdomain/extracts_adams_diary/) | [Extracts from Adam's Diary](https://archive.org/details/extracts_adams_diary) | Public Domain | Mark Twain | 5 | 11.4 | 2,9,10,12,18 |
| [`lettersfromacat_1309_librivox`](voice-publicdomain/lettersfromacat_1309_librivox/) | [Letters from a Cat](https://archive.org/details/lettersfromacat_1309_librivox) | Public Domain (PDM 1.0) | Helen Hunt Jackson | 4 | 27.0 | 2,9,10,12,18 |
| [`mladytele1915`](voice-publicdomain/mladytele1915/) | [ My Lady Of The Telephone](https://archive.org/details/mladytele1915) | Public Domain | Joseph Phillips and Chorus | 3 | 6.0 | 2,10,12,18 |
| [`radiocop_2502_librivox`](voice-publicdomain/radiocop_2502_librivox/) | [The Radio Cop](https://archive.org/details/radiocop_2502_librivox) | Public Domain (PDM 1.0) | Vic Whitman | 6 | 25.2 | 2,9,10,12,18 |
| [`shortpoetry_002_librivox`](voice-publicdomain/shortpoetry_002_librivox/) | [Short Poetry Collection 002](https://archive.org/details/shortpoetry_002_librivox) | Public Domain | Various | 22 | 26.6 | 2,9,10,12,18 |
| [`shortpoetry_024_librivox`](voice-publicdomain/shortpoetry_024_librivox/) | [Short Poetry Collection 024](https://archive.org/details/shortpoetry_024_librivox) | Public Domain |  | 20 | 14.6 | 2,9,10,12,18 |

### voice-sfx/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`voice-clip-packs-for-visual-novels-and-rpgs`](voice-sfx/voice-clip-packs-for-visual-novels-and-rpgs/) | [Voice Clip Packs for Visual Novels and RPGs](https://opengameart.org/content/voice-clip-packs-for-visual-novels-and-rpgs) | CC0 | cicifyre | 129 | 99.1 | 3,5,10,19 |
| [`voices-sound-effects-library`](voice-sfx/voices-sound-effects-library/) | [Voices Sound Effects Library](https://opengameart.org/content/voices-sound-effects-library) | CC-BY 3.0 **BY** | Little Robot Sound Factory | 910 | 104.9 | 3,5,10,19 |

### music/

| 팩 폴더 | 이름 | 라이선스 | 작성자 | 파일 | MB | 컨셉 |
|---|---|---|---|---:|---:|---|
| [`a-cloudy-morning-jazz`](music/a-cloudy-morning-jazz/) | [A Cloudy Morning (Jazz)](https://opengameart.org/content/a-cloudy-morning-jazz) | CC-BY 3.0 **BY** | Matthew Pablo | 1 | 5.6 | 5,12,17,20 |
| [`a-conversation-with-saul-jazzblues-shuffle`](music/a-conversation-with-saul-jazzblues-shuffle/) | [A Conversation with Saul (Jazz/Blues Shuffle)](https://opengameart.org/content/a-conversation-with-saul-jazzblues-shuffle) | CC-BY 3.0 **BY** | Matthew Pablo | 1 | 3.7 | 5,12,17,20 |
| [`bossa-nova`](music/bossa-nova/) | [Bossa Nova](https://opengameart.org/content/bossa-nova) | CC0 | Joth | 1 | 1.1 | 5,12,17,20 |
| [`calm-bgm`](music/calm-bgm/) | [calm bgm](https://opengameart.org/content/calm-bgm) | CC-BY 3.0 **BY** | syncopika | 2 | 14.5 | 5,12,17,20 |
| [`childrens-march-theme`](music/childrens-march-theme/) | [Children's March Theme](https://opengameart.org/content/childrens-march-theme) | CC0 | CleytonKauffman | 3 | 30.4 | 5,12,17,20 |
| [`chill-lofi-inspired`](music/chill-lofi-inspired/) | [Chill lofi inspired](https://opengameart.org/content/chill-lofi-inspired) | CC0 | omfgdude | 2 | 8.8 | 5,12,17,20 |
| [`circus-dilemma`](music/circus-dilemma/) | [Circus Dilemma](https://opengameart.org/content/circus-dilemma) | CC-BY 3.0 **BY** | Matthew Pablo | 1 | 3.2 | 5,12,17,20 |
| [`cyberpunk-moonlight-sonata`](music/cyberpunk-moonlight-sonata/) | [Cyberpunk Moonlight Sonata](https://opengameart.org/content/cyberpunk-moonlight-sonata) | CC0 | Joth | 2 | 4.5 | 5,12,17,20 |
| [`death-is-just-another-path`](music/death-is-just-another-path/) | [Death Is Just Another Path](https://opengameart.org/content/death-is-just-another-path) | CC-BY 3.0 **BY** | Otto Halmén | 3 | 33.0 | 5,12,17,20 |
| [`deliciously-sour`](music/deliciously-sour/) | [Deliciously Sour](https://opengameart.org/content/deliciously-sour) | CC-BY 3.0 **BY** | Matthew Pablo | 1 | 3.6 | 5,12,17,20 |
| [`forest-ambience`](music/forest-ambience/) | [Forest Ambience](https://opengameart.org/content/forest-ambience) | CC0 | TinyWorlds | 1 | 0.7 | 5,12,17,20 |
| [`free-music-pack`](music/free-music-pack/) | [Free Music Pack](https://opengameart.org/content/free-music-pack) | CC0 | tricksntraps | 7 | 32.8 | 5,12,17,20 |
| [`in-the-circus-psg-version`](music/in-the-circus-psg-version/) | [In the circus (PSG Version)](https://opengameart.org/content/in-the-circus-psg-version) | CC-BY 3.0 **BY** | Snabisch | 1 | 1.0 | 5,12,17,20 |
| [`kevin-macleod-incompetech`](music/kevin-macleod-incompetech/) | [Kevin MacLeod selected tracks](https://incompetech.com/music/royalty-free/music.html) | CC-BY 4.0 **BY** | Kevin MacLeod (incompetech.com) | 26 | 178.3 | 3,4,5,6,9,11,12,15,17,18,19,20 |
| [`lofi-compilation`](music/lofi-compilation/) | [lofi Compilation](https://opengameart.org/content/lofi-compilation) | CC0 | TAD | 9 | 25.2 | 5,12,17,20 |
| [`mysterious-ambience-song21`](music/mysterious-ambience-song21/) | [Mysterious Ambience (song21)](https://opengameart.org/content/mysterious-ambience-song21) | CC0 | cynicmusic | 1 | 0.4 | 5,12,17,20 |
| [`mystical-theme`](music/mystical-theme/) | [Mystical Theme](https://opengameart.org/content/mystical-theme) | CC-BY 3.0 **BY** | Alexandr Zhelanov | 1 | 1.6 | 5,12,17,20 |
| [`november-snow`](music/november-snow/) | [November Snow](https://opengameart.org/content/november-snow) | CC0 | cynicmusic | 1 | 13.3 | 5,12,17,20 |
| [`one`](music/one/) | [One](https://opengameart.org/content/one) | CC-BY 3.0 **BY** | pheonton | 1 | 1.9 | 5,12,17,20 |
| [`rain-and-thunders`](music/rain-and-thunders/) | [rain and thunders](https://opengameart.org/content/rain-and-thunders) | CC0 | kindland | 1 | 6.5 | 5,12,17,20 |
| [`school-of-quirks`](music/school-of-quirks/) | [School of Quirks](https://opengameart.org/content/school-of-quirks) | CC-BY 3.0 **BY** | Zander Noriega | 1 | 5.9 | 5,12,17,20 |
| [`shop-theme`](music/shop-theme/) | [Shop Theme](https://opengameart.org/content/shop-theme) | CC0 | CleytonKauffman | 4 | 26.8 | 5,12,17,20 |
| [`sleep-talking-loop-fantasy-rpg-sci-fi`](music/sleep-talking-loop-fantasy-rpg-sci-fi/) | [Sleep Talking / Loop / Fantasy / RPG / Sci-Fi](https://opengameart.org/content/sleep-talking-loop-fantasy-rpg-sci-fi) | CC0 | KiluaBoy | 2 | 5.8 | 5,12,17,20 |
| [`snowfall`](music/snowfall/) | [Snowfall](https://opengameart.org/content/snowfall) | CC0 | Kistol | 2 | 1.9 | 5,12,17,20 |
| [`snowland-town`](music/snowland-town/) | [Snowland Town](https://opengameart.org/content/snowland-town) | CC-BY 3.0 **BY** | Matthew Pablo | 3 | 37.1 | 5,12,17,20 |
| [`soliloquy`](music/soliloquy/) | [Soliloquy](https://opengameart.org/content/soliloquy) | CC-BY 3.0 **BY** | Matthew Pablo | 1 | 5.3 | 5,12,17,20 |
| [`talking-cute-chiptune`](music/talking-cute-chiptune/) | [Talking Cute (Chiptune)](https://opengameart.org/content/talking-cute-chiptune) | CC0 | Pro Sensory | 1 | 3.5 | 5,12,17,20 |
| [`the-field-of-dreams`](music/the-field-of-dreams/) | [The Field Of Dreams](https://opengameart.org/content/the-field-of-dreams) | CC0 | pauliuw | 1 | 3.3 | 5,12,17,20 |
| [`trouble-makers-coolriff-jazz`](music/trouble-makers-coolriff-jazz/) | [Trouble Makers (Cool/Riff-Jazz)](https://opengameart.org/content/trouble-makers-coolriff-jazz) | CC-BY 3.0 **BY** | Matthew Pablo | 2 | 21.7 | 5,12,17,20 |

## 컨셉별 바로가기

| # | 컨셉 | 먼저 볼 곳 |
|---|---|---|
| 1 | 터미널 유령 | `sfx-ui-typing/keyboard-*`, `mechanical-keyboard-sound`, `single-key-press-sounds`, `typewriter-sounds`, `kenney-audio/interface-sounds`·`digital-audio`·`sci-fi-sounds`, `sfx-ui-typing/glitch-music` |
| 2 | 라디오 수색 | `radio-static-noise/frequency-static-sound-effects`(CC0 대용량 정적 잡음), `static`, `mysterious-radio-signal`, `radio-call`, `voice-publicdomain/Apollo11Audio`·`Apollo13Audio`(실제 무선 교신) |
| 3 | 손전등 하나 | `footsteps/*`, `breathing-heartbeat/*`, `horror-drones/*`, `foley-props/door-*`·`creaky-light-wooden-door` |
| 4 | 박제된 사진관 | `foley-props/camera`·`camerashudder`, `commons-projector-camera-tape`, `vinyl` |
| 5 | 탁상 인형극 | `audience/*`, `music/circus-dilemma`·`in-the-circus-psg-version`·`kevin-macleod-incompetech`(Monkeys Spinning Monkeys, Fluffing a Duck 등), `kenney-audio/impact-sounds`(나무 두드림) |
| 6 | 엘리베이터 | `foley-props/elevator-ding`·`elevatordoor`·`old-elevator-door`, `ambience/*`(루프 100개 이상), Kevin MacLeod "Local Forecast - Elevator"·"Lobby Time" |
| 7 | 오답 사전 | `kenney-audio/ui-audio`·`interface-sounds` |
| 8 | 뒤집힌 수족관 | `ambience/*water*`·`bubble*`·`dripping-*`, `footsteps/*`(천장 위 발소리용) |
| 9 | 답장 없는 편지 | `foley-props/pencil-sounds`·`writing-scribbles`·`10-book-page-flips`·`page-turning-*`·`paper-crumple-*`·`opening-and-closing-a-map-sounds`, `voice-publicdomain/lettersfromacat_*`·`eves_diary_*` |
| 10 | 전화 교환수 | `foley-props/commons-telephone`(다이얼톤·통화중·링백·DTMF·회전 다이얼), `crank-movie-telephone-ringtone`, `modern-ringtone-chirptone`, `voice-publicdomain/mladytele1915` |
| 11 | 눈 감은 기록 | `ambience/*`, `horror-drones/*` (바이노럴 소스는 없음 → 부족한 것) |
| 12 | 폐교 방송실 | `music/*`(BGM 10곡 이상), `voice-publicdomain/*`, `radio-static-noise/*` |
| 13 | 잘못 배송된 소포 | `foley-props/sound-effects-pack`·`super-foley-pack`·`item-handling`·`scissors`·`various-scissors`, `kenney-audio/rpg-audio` |
| 14 | 한 프레임 만화 | `kenney-audio/ui-audio`·`interface-sounds` |
| 15 | 지하철 마지막 열차 | `ambience/*`(험·환풍·기계음), `horror-drones/*` — 실제 지하철 차내 녹음은 부족 |
| 16 | 얼음 아래 목소리 | `ice/*`, `footsteps/42-snow-and-gravel-footsteps`·`walking-on-snow-sound` |
| 17 | 박물관 야간 경비 | `footsteps/*`(돌·타일), `ambience/clock-*`·`ventilation*`·`fridge-loop-1`, `horror-drones/*` |
| 18 | 녹음기 되감기 | `foley-props/tape-recorder-opening-and-closing-sound-effects`·`equipment-clicks-*`, `voice-publicdomain/*`(음성 수 시간) |
| 19 | 잠든 사람 옆에서 | `breathing-heartbeat/*`, `voice-sfx/*`(숨·신음 클립), `foley-props/*` |
| 20 | 이름 없는 정거장 | `ambience/high-traffic-road-sounds`·`rain-*`·`crickets*`·`wind1`, Kevin MacLeod "Lightless Dawn"·"Peaceful Desolation" |

## 부족한 것

- **컨셉 전용 대사 전부.** 2(조난 음성 60줄), 6(음성 20줄), 10(전화 음성 200줄/30줄), 12(응답 음성 20줄), 18(진술 음성 60분/10분)의 **한국어 대사는 공개 라이선스 녹음으로 대체할 수 없다.** `kit/tools/tts_generate.py`(ElevenLabs)로 대본을 만든 뒤 합성해야 한다. 여기 있는 LibriVox·NASA·에디슨 녹음은 전부 영어이며, 라디오/테이프 질감 테스트·배경 웅얼거림·필터 체인 개발용이다.
- **관객 "웃음" 군중 녹음.** 박수·환호(`free-crowd-cheering-sounds`, `applause*`)는 있으나 여러 사람이 동시에 웃는 CC0 녹음은 거의 없다(개별 웃음·악당 웃음뿐). 5번에는 개별 웃음을 겹쳐 합성하거나 Sonniss GDC 번들에서 찾을 것.
- **얼음 균열.** 빙판이 갈라지는 실제 녹음(호수 얼음의 '핑' 울림 포함)이 부족하다. 지금은 `ice/ice-breakingshattering`, `cracking-sounds`, `35-wooden-crackshitsdestructions`, `5-break-crunch-impacts`, 눈 밟기 소리뿐. 16번의 핵심 감각이라 Sonniss 번들 또는 직접 녹음/합성(얼음 조각 + 피치다운 + 스프링 리버브)이 필요하다.
- **지하철·엘리베이터 차내 환경음 루프.** 벨·문 소리는 있지만 운행 중 차내 럼블 루프는 없다(15, 6).
- **박물관 대형 홀 룸톤, 학교 복도 룸톤, 방송실 룸톤**(17, 12). 일반 험/환풍 루프로 대체 가능하나 공간감(긴 잔향)은 IR 리버브로 만들어야 한다.
- **바이노럴(11) 녹음**, 수중 청음(8)의 실제 하이드로폰 녹음.
- **교환대 플러그 꽂는 소리, 필름 영사기 구동음, 카세트 되감기/빨리감기 모터음**(10, 4, 18) — 일반 클릭·기계음으로 대체 중.
- **종이 인형 막대/나무 인형 딸각 소리**(5) — `kenney-audio/impact-sounds`의 나무 계열과 `foley-props/100-cc0-metal-and-wood-sfx`로 대체.
- 숨소리 3단계 루프(19)는 `breathing-heartbeat/breathing-tired`·`voice-sfx` 조각으로 편집해 만들어야 한다. 잔잔한 수면 호흡 루프는 없다.

## 수동 다운로드 권장 (자동 수집 불가)

- **Sonniss GDC Game Audio Bundle** (https://sonniss.com/gameaudiogdc) — 알려진 조건은 로열티 프리·상업 사용 가능·크레딧 불필요·원본 재배포 금지이나, 이번에는 사이트가 403을 돌려 **라이선스 페이지를 직접 확인하지 못했다.** 받을 때 반드시 재확인. 해마다 10–30GB 단일 번들이며 사이트가 자동화 요청을 403으로 막아 받지 않았다. 얼음 균열·지하철·엘리베이터·군중 웃음·영사기 같은 위 부족분의 대부분이 여기 있다. 사람이 브라우저로 받은 뒤 필요한 파일만 골라 넣을 것. 받은 날의 라이선스 페이지를 함께 저장할 것.
- **Freesound CC0** — 다운로드에 로그인 필요라 건너뜀. 계정이 있으면 "ice crack", "subway interior", "laughing crowd" CC0 필터로 보충 가능.

## 확인 필요 (라이선스가 모호해 받지 않음)

- **The Conet Project (숫자 방송국 녹음)** — archive.org 항목에 CC0 표기가 있으나 업로더가 붙인 표기이고, 원 배포사 Irdial-Discs의 라이선스 문구와 녹음 자체의 권리 관계가 불분명.
- **archive.org의 단파 해적방송 모음**(ShortwavePirateRadio 등) — 업로더가 PD/CC0로 표기했지만 방송 내용의 권리자가 따로 있다.
- **archive.org 올드타임 라디오(OTR) 방송**(Speed Gibson, D-Day 방송일 전체 녹음 등) — "public domain" 표기는 업로더 주장이며 미국 저작권 갱신 여부가 개별로 확인되지 않음.
- **BBC Sound Effects** — RemArc 라이선스(비상업·개인 용도)라 제외.
- **NASA SoundCloud 클립** — NASA 음원 자체는 퍼블릭 도메인이나 SoundCloud는 직접 다운로드 경로가 없어 받지 않음. 대신 archive.org의 NASA JSC 원본(Apollo 11/13)을 받았다.
- OpenGameArt에서 **GPL 전용** 항목(예: `foot-walking-step-sounds-on-stone-water-snow-wood-and-dirt`, `static-noise-similar-untuned-tv-also-known-white-noise`)은 규칙상 제외.
- Wikimedia Commons 다운로드 서버(upload.wikimedia.org)가 이 IP에 429(요청 과다)를 계속 돌려 상당수 파일을 받지 못했다. 특히 **`Sound of cracking ice.ogg`(Amada44, CC BY-SA 3.0)**, **`72844 lonemonk approx-800-laughter-and-clapter-1.wav`(lonemonk, CC BY 3.0)** 는 16·5번에 꼭 필요한데 실패했으니 나중에 수동으로 받을 것. `commons-*` 폴더는 받은 파일만 SOURCE.json에 기록되어 있다. 나중에 같은 검색어로 다시 돌리면 채워진다.

## 수집 도구

수집 스크립트는 세션 스크래치 디렉터리에만 있었다(저장소에 넣지 않음). 재수집 시 방식: OpenGameArt `art-search-advanced` 인기순 상위 N개 → 노드 페이지의 라이선스 필드로 CC0/CC-BY/CC-BY-SA만 통과 → 첨부 파일 다운로드·압축 해제 → SOURCE.json 기록. archive.org는 `/metadata/<id>` API, Commons는 `generator=search` + `imageinfo.extmetadata.LicenseShortName`으로 파일별 라이선스를 판정했다.


## 부록: 검색에 걸렸지만 주제와 무관해 지운 팩

OpenGameArt 인기순 검색이 판타지 전투·마법·동물 소리 같은 무관한 팩도 끌어와서 아래는 받은 뒤 삭제했다(라이선스 문제 아님). 필요하면 같은 슬러그로 `https://opengameart.org/content/<슬러그>`에서 다시 받으면 된다.

`8-bit-pickup-1`, `alien-animal-sounds`, `battle-at-sea`, `bubbling-acid`, `cannon-hit-wall`, `explosion-0`, `farm-animals`, `female-rpg-voice-starter-pack`, `fire-staff-sound-effects`, `giraffe-hum`, `horse-gallop-on-different-surfaces`, `jc-sounds-pirate-pack-vol-1`, `jc-sounds-sci-fi-pack-vol-1`, `kluna-vns-eating`, `knife-sharpening-slice-2`, `levelup-sound-atmospheric`, `punches-hits-swords-and-squishes`, `quail-imitation`, `rabbit-eating`, `rage-mode`, `rockbreaking`, `rumbleexplosion`, `slime-jump-effect`, `spell-sounds-starter-pack`, `swish-bamboo-stick-weapon-swhoshes`, `vulture-imitation`, `some-animal-noise`, `sci-fi-sound-effects-library`, `red-eclipse-sounds`, `underwater-like-fanfare`, `ui-and-item-sounds-sample-1`, `various-sound-effects-from-rubiks-race`, `literal-dummy-game-dev-placeholder-sounds`, `40-cc0-water-splash-slime-sfx`, `evil-creature`, `female-warrior-cheer`, `girly-scream`, `stunt-rally-sounds`, `20-sword-sound-effects-attacks-and-clashes`, `8-heals-and-buffs-sfx`, `action-sounds`, `chimey-ui-sounds`, `80-cc0-creature-sfx`, `80-cc0-creture-sfx-2`, `10-impactshield-blocks`, `8-wet-squish-slurp-impacts`, `fleshy-bone-breaksnap-sfx`, `level-up-power-up-coin-get-13-sounds`, `picked-coin-echo-2`, `shimmer-glitter-magic`, `supertuxkart-sound-effects`, `car-sound-effects-pack-low-quality`, `50-rpg-sound-effects`, `completion-sound`, `fantasy-sound-effects-library`, `fps-placeholder-sounds`, `angry-robot-bird`, `freeze-spell-0`, `ice-electricity-magic`, `magic-sfx-sample`, `open-chest`, `thwack-sounds`, `54-casino-sound-effects-cards-dice-chips`, `bird-song-1-second`, `compressed-gas-leak-sfx`, `high-pitch-hitted`, `sci-fi-rts-war-unit-sounds`, `swishes-sound-pack`, `miscmenu-sci-fi-sounds`, `action-shooter-soundset-wwvi`, `generic-8-bit-jrpg-soundtrack`, `techno-space`, `background-space-track`, `space-orchestral`, `space-boss-battle-theme`, `game-music-loop-intense`, `battle-theme-a`, `another-space-background-track`, `happy-adventure-loop`, `high-tech-lab`, `town-theme-rpg`, `crystal-cave-song18`, `rpg-costal-town-background-music`, `medieval-the-old-tower-inn`, `spell-sounds`, `8-magic-attacks`, `jc-sounds-fantasy-sfx-pack-vol-1`, `7-eating-crunches`, `tree-chop-fall-thud`, `goblin-cackle`, `tiny-naval-battle-sounds-set`, `dog-running-0`, `kelvin-shadewings-sound-pack-2`, `ambient-alien-light-sfx`, `8-bit-sound-effects-library`, `3-ping-pong-sounds-8-bit-style`, `game-over-soundold-school`, `robot-this-will-be-the-end-of-you`, `2-wooden-squish-splatter-sequences`, `ui-accept-or-forward`, `hi-tech-button-sound-pack-i-non-themed`, `commons-ice`
