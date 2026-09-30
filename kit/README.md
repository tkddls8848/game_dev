# kit — 게임이 아닌 것

여러 PoC가 같이 쓰는 도구·틀·공유 코드. **게임 코드는 여기 두지 않는다** — 게임은 `games/<슬러그>/`다.

```
kit/
  tools/          도구 (아래 표)
  templates/      새 PoC 뼈대 · 갤러리와 에셋 목록 HTML 틀
  unity-lowpoly/  Unity 셸 (로우폴리 PoC 두 개가 공유) ← kit/unity-lowpoly/README.md
  logic/          PoC 사이에 공유하는 순수 C#
  presentation/   목업 HTML 이 공유하는 CSS/JS
```

---

## 도구

### 만드는 것

| 도구 | 하는 일 | 게임 지정 |
|---|---|---|
| `tools/sync_shared_logic.py` | `Assets/Scripts`에서 `using UnityEngine`이 **없는** 파일을 골라 테스트 프로젝트의 `.props`를 만든다 | `DEFAULT_GAME` |
| `tools/build_lowpoly_projects.py` | `kit/unity-lowpoly/`와 게임의 순수 C#·JSON을 Unity 프로젝트 둘로 복사한다. **kit에서 지운 파일은 프로젝트에서도 지운다** | 인자 없음 (둘 다) |
| `tools/scaffold_revised_concepts.py` | 수정 컨셉 PoC들의 시나리오·진입점·메타데이터를 생성한다 | 인자 없음 |
| `tools/build_poc_gallery.py` | `games/`를 훑어 PoC 목록 갤러리를 만든다 | 인자 없음 |
| `tools/build_revised_gallery.py` | `file://`로 열어도 동작하는 갤러리를 만든다 | 인자 없음 |
| `tools/asset_library.py` | 공개 CC0 팩을 모아 재현 가능한 에셋 목록을 만든다 | 인자 없음 |
| `tools/asset-collect/*.py` | 수집기 — Kenney · Quaternius · Poly Haven · itch 무료, 그리고 컨셉별 색인 생성 | 인자 없음 |

### 소리

| 도구 | 하는 일 | 게임 지정 |
|---|---|---|
| `tools/tts_generate.py` | 대본(`script.json`)을 ElevenLabs `eleven_v3`로 합성해 파일로 굽는다 | `--game` |
| `tools/measure_voice_clips.py` | 구운 음성의 **실제 길이**를 MPEG 프레임을 세어 재고 JSON으로 남긴다 | `--game` |
| `tools/build_voice_audition.py` | 이미 있는 샘플을 오디션 페이지로 묶는다. **오프라인 전용 — 합성하지 않는다** | 인자 없음 |

### 찍고 검사하는 것

| 도구 | 하는 일 | 게임 지정 |
|---|---|---|
| `tools/shoot_mocks.js` | 모든 PoC의 연출 목업을 PNG로 찍는다 → `docs/poc-gallery/png/` + `shots.html` | 인자로 슬러그 |
| `tools/verify_poc_gallery.js` · `verify_revised_pocs.js` | 갤러리와 수정 컨셉 PoC의 링크·상태를 검사 | 인자 없음 |
| `tools/verify_asset_library.py` · `verify_asset_catalog.js` | 에셋 목록의 경로·서명·glTF 의존을 오프라인 감사 | 인자 없음 |
| `tools/verify_takeover.js` | 인계 문서의 주장과 실제 상태를 대조 | 인자 없음 |

`--game`이 있는 도구의 기본값은 `games/mystery-blackwood`다. 새 게임에서는 인자로 준다.

---

## 두 가지 함정 — 도구가 대신 피해 준다

`shoot_mocks.js`:

* **정적 서버를 띄우고 http로 방문한다.** 목업은 `fetch('../data/*.json')`로 실제 데이터를
  읽는데 `file://`에서는 Chromium이 그 fetch를 막는다 — 그냥 열면 빈 화면이 나온다
* **게임 화면에 해당하는 절로 스크롤한 뒤 뷰포트를 찍는다.** 목업은 문서 꼴이라 머리를 찍으면
  데이터 시트가 나온다. 절만 잘라 찍으면 폭이 제각각이라 비교가 안 되고 옆 칸이 잘린다
  (한 번은 '밭과 덱을 나란히'가 핵인데 '밭'만 521px로 잘렸다)

내용이 없는 목업(아직 만들고 있는 PoC)은 **찍지 않는다** — 빈 이미지를 남기면 목록에서
'연출이 이렇다'로 읽힌다. playwright는 npx 캐시에서 찾아 쓴다(devDependency로 두지 않는다).

`build_lowpoly_projects.py`:

* **복사만 하지 않고 지운다.** kit에서 파일을 쪼개거나 지웠을 때 프로젝트에 옛 사본이 남으면
  같은 `partial class`가 두 번 정의되어 `error CS0111`로 터진다. 한 번 겪었다 —
  `Art.cs`를 구역별로 쪼갰더니 옛 `Art.cs`가 남아 중복 정의 15건이 났다
* **정리 범위는 `Assets/Runtime`·`Assets/Editor` 바로 아래 한 단계뿐이다.** 하위 폴더는
  프로젝트 전용이다(예: `puzzle-tomorrow-map`의 `ReferenceEdition/`). 재귀로 훑었다가
  추적 중이던 프로젝트 전용 파일을 지운 적이 있다

---

## 생성물은 손으로 고치지 않는다

`sync_shared_logic.py`가 만드는 `.props`는 **생성물이다.**
그 파일이 설계 원칙 1(로직에서 UnityEngine 분리)을 강제하는 장치다 — 로직 파일에 UnityEngine이
끼어들면 목록에서 빠지고 테스트 빌드가 컴파일 오류로 터진다.

`games/farm-erosion/data/map.json`처럼 생성기가 있는 데이터도 마찬가지다.

---

## templates

`templates/poc/` — 새 헤드리스 PoC 뼈대. `PoC.csproj.template` · `src/` · `tests/` · `data/` ·
`presentation/` · `README.md` · `CREDITS.md`.
슬러그를 정하고 복사한 뒤 `__SLUG__`를 바꾼다. 절차는 [`docs/POC_FACTORY.md`](../docs/POC_FACTORY.md) §3.

`templates/gallery.html` · `templates/asset-library.html` — 생성 도구들이 쓰는 HTML 틀.

## logic · presentation

`logic/ConceptGraph.cs` — 상태 그래프. 수정 컨셉 PoC들이 공유한다(테스트 포함).
`presentation/concept.css` · `concept.js` — 목업 HTML 이 공유하는 스타일과 동작.

**여기 있는 것은 두 개 이상의 PoC가 실제로 쓰는 것뿐이다.** 하나만 쓰는 코드는
그 게임 폴더에 둔다 — 꺼내는 시점은 **두 번째 게임이 같은 것을 필요로 할 때**다.
