> **원본 위치:** `AssetDownloads/tmp_game/assets/icons/` (gitignore — 바이너리는 추적하지 않는다).
> 이 문서는 목록만 저장소에 남긴 사본이다. 아래 링크는 그 원본을 가리킨다.

# icons/ — 아이콘·2D 스프라이트

수집일 2026-09-28. 모든 팩 폴더에 `SOURCE.json`(name, sourceUrl, downloadUrl, license, licenseFile, author, acquiredAt, sha256, usedFor)이 있다.
원본 zip/tgz는 20MB 미만일 때만 보관했다. 합계 약 350MB.

## 라이선스 요약 — 크레딧 필요 여부

| 팩 | 라이선스 | 크레딧 |
|---|---|---|
| kenney-2d/* | CC0 1.0 | 불필요 |
| game-icons | CC BY 3.0 (Zeromancer 폴더만 CC0) | **필요** — 작가별. `game-icons/ATTRIBUTION.txt` |
| lucide | ISC | LICENSE 동봉 유지 |
| tabler | MIT | LICENSE 동봉 유지 |
| phosphor | MIT | LICENSE 동봉 유지 |
| openmoji | CC BY-SA 4.0 | **필요 + 동일조건**(수정한 이모지 자체는 같은 라이선스로 공개) |
| twemoji | 그래픽 CC BY 4.0 (코드 MIT) | **필요** |

## 팩 목록

| 폴더 | 내용 | 파일 수 | 포맷 | 컨셉 |
|---|---|---|---|---|
| `game-icons/svg/<작가>/` | game-icons.net 전체 (commit 82d9488) | SVG 4,239 | SVG 512×512, 검정 배경+흰 도형 | **7(아이콘 200)**, 1·2·5·6·9·13·17 UI |
| `game-icons/index.json` | 이름→작가→경로 색인 (검색용) | | JSON | 7 |
| `lucide/icons/` | lucide-static 1.48.0 | SVG 2,118 | 24px 스트로크 | UI 공통, 7(예술판 아이콘 50) |
| `tabler/icons/{outline,filled}/` | @tabler/icons 3.48.0 | SVG 6,220 | 24px 스트로크/채움 | UI 공통, 7 |
| `phosphor/assets/<굵기>/` | @phosphor-icons/core 2.1.1, 6가지 굵기 × 1,512 | SVG 9,072 | 256px | UI 공통, 7, 9, 13 |
| `openmoji/{color,black}/svg/` | OpenMoji 17.0 컬러/흑백 | SVG 8,993 | 72px 격자 | 7, 13, 14(표정) |
| `twemoji/` | @twemoji/svg 15.0 | SVG 3,720 | 36px 격자 | 7, 13, 14 |
| `kenney-2d/<slug>/contents/` | Kenney 2D/UI 40팩 (아래) | PNG 19,256 / SVG 4,636 | PNG(+SVG 벡터 원본 일부) | 아래 |

### kenney-2d (전부 CC0)

| slug | 파일 | 컨셉 | 메모 |
|---|---|---|---|
| game-icons, game-icons-expansion | 434 / 810 | 7 | 흰·검 아이콘 PNG |
| board-game-icons, board-game-info | 774 / 868 | 7, 5 | 주사위·토큰·카드 기호 (SVG 포함) |
| generic-items | 339 | 7, 13 | 사물 아이콘 — 소포 내용물 |
| 1-bit-pack, monochrome-rpg, micro-roguelike, tiny-town, tiny-dungeon, rune-pack, map-pack, flag-pack, animal-pack-remastered | 14~692 | 7 | 픽셀·단색 아이콘/타일 |
| ui-pack, ui-pack-pixel-adventure, ui-pack-rpg-expansion, ui-pack-adventure, ui-pack-sci-fi, fantasy-ui-borders | 94~1,315 | 공통 UI, 1·6(sci-fi), 9·14(테두리·패널) | 버튼·패널·슬라이더, 폰트 포함 |
| input-prompts, input-prompts-pixel, mobile-controls, cursor-pack, cursor-pixel-pack, crosshair-pack | 227~4,667 | 공통 | 키·패드 프롬프트, 커서, 조준점(3·11 시야 표시) |
| emotes-pack | 534 | 14, 5 | 말풍선 감정 기호 |
| splat-pack, scribble-dungeons, sketch-town, background-elements-remastered | 113~356 | 14, 5 | 만화 컷 배경·효과 |
| shape-characters, toon-characters | 219 / 698 | 20, 14 | 2D 인물 파츠·포즈 (실루엣 소스) |
| rpg-urban-pack | 495 | 13, 20 | 탑다운 도시 소품·인물 |
| googly-eyes | 12 | 5 | 인형 눈 |
| playing-cards-pack | 291 | 5 | 카드 소품 |
| monster-builder-pack | 367 | 15 | 괴물 파츠 조합 |
| fish-pack | 387 | 8 | 수중 소품 |
| minimap-pack | 168 | 6 | 층 지도/UI |

## 수집 도구

- Kenney: `../_tools/collect_kenney.py <대상폴더> slug=컨셉,...` (kit/tools/asset_library.py의 검증 로직 이식: 페이지 CC0 링크 + 동봉 라이선스 CC0 확인, zip 경로 안전 검사)
- game-icons: `git clone --depth 1 https://github.com/game-icons/icons` 후 `.git` 제거
- lucide/tabler/phosphor/openmoji/twemoji: npm 레지스트리 tarball에서 SVG와 LICENSE만 추출. tabler `categories/`, openmoji `src/`는 중복이라 제거

## 부족한 것

- **컨셉 7 아이콘의 화풍 통일**: game-icons(4,239)만으로 200개는 충분하지만, "정의 조합으로 새 사물" 결과물이 항상 존재하지는 않는다. 조합 결과를 여러 세트(game-icons + tabler + openmoji)에서 섞으면 화풍이 깨진다. 한 세트로 고정하고 빈칸은 직접 합성(두 SVG 겹치기)해야 한다.
- **실루엣(컨셉 20: 50종, 8: 8종, 15: 승객 20)**: 전용 사람 실루엣 세트가 없다. shape/toon-characters(2D)나 models/의 캐릭터를 검은 단색으로 렌더해 만들어야 한다.
- **종이 인형(컨셉 5)**: 전용 종이 인형 스프라이트 없음. toon-characters 파츠 + googly-eyes로 조합 필요.
- **만화 컷(컨셉 14: 컷 80)**: 완성 컷 그림은 없다(말풍선·효과 기호만). 컷 그림은 별도 제작 필요.
- **교환대 화면(컨셉 10)·다이얼(컨셉 2)** 전용 UI 스킨 없음 — ui-pack-sci-fi/ui-pack 조합 또는 제작.
- OpenMoji는 CC BY-SA라 수정본 배포 시 동일 라이선스가 붙는다. 상업판에서는 CC BY(twemoji)나 CC0/MIT 세트를 우선한다.
