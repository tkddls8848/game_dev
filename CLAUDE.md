# CLAUDE.md — 컨셉 시안 저장소

**여기는 게임을 완성하는 곳이 아니라 게임을 구상하는 곳이다.**
컨셉, 시안, 목업, 에셋, 생성 음악이 여기 있다. 대부분은 만들어지지 않는다. 그게 정상이다.

깊게 개발 중인 작업물은 **다른 저장소**에 있다:
[`tkddls8848/game`](https://github.com/tkddls8848/game) — `farm-erosion`(개발 중) ·
`mystery-blackwood`(거의 완성). 여기서 그 둘의 이름이 보이면 **설명이지 링크가 아니다.**

---

## 지도

```
games/                     시안 29개. 대부분 순수 C# 규칙 + 목업 HTML 이다
  puzzle-tomorrow-map/     유일하게 Unity 셸이 있는 시안 (아래 주의)
kit/
  unity-lowpoly/           로우폴리 Unity 셸. 지금은 puzzle-tomorrow-map 만 쓴다
  tools/                   에셋 수집기 · 갤러리 생성기 · TTS · 공유 로직 목록 생성기
docs/
  README.md                ★ 문서 색인
  CONCEPTS.md              컨셉 목록 — 40안 + 수정 컨셉 10종
  PLAN_GENRES.md           장르 계획 다섯
  ENGINE_FIT.md            엔진 선택
  MUSIC_DIRECTION.md       음악 방향
  ASSET_LIBRARY.md         에셋 쓰는 법 · 라이선스
  POC_FACTORY.md           폴더 규약 · 새 PoC 시작
  asset-index/             에셋 목록 (바이너리는 추적하지 않고 목록만 남긴다)
  poc-gallery/             컨셉 이미지 451장 · 티저 영상 · 목업
AssetDownloads/            내려받은 원본 (gitignore). 아래를 읽을 것
```

---

## ⚠️ AssetDownloads 는 git 에 넣지 않는다

**6.9 GB 다.** GitHub 저장소 권장 상한이 5 GB이고 LFS 무료 용량은 1 GB이므로 들어가지 않는다.
그리고 **넣을 필요가 없다** — 이 저장소가 그 문제를 이미 풀어 두었다:

* `docs/asset-index/` 가 **목록**을 들고 있다 (audio · models · icons · fonts · images · shaders)
* `kit/tools/asset-collect/*.py` 가 Kenney · Quaternius · Poly Haven · itch 에서 **다시 내려받는다**

그러니 `docs/asset-index/*.md` 안의 `../../AssetDownloads/...` 링크는
**git 에서 깨져 있는 것이 정상이다.** 내려받은 사람의 디스크에서만 열린다.

**대신 다시 만들 수 없는 것은 git 에 있다.** 돈을 내고 한 번 만든 것들이다:

| 무엇 | 어디 | 도구 |
|---|---|---|
| 컨셉 이미지 451장 | `docs/poc-gallery/concepts/` | Seedream 5.0 Pro (Artlist) |
| 티저 영상 | `docs/poc-gallery/video/` | — |
| 생성 음악 | `docs/poc-gallery/music/` | Lyria 3 Pro (Artlist) |

**이것들은 지우지 않는다.** 크레딧을 다시 써야 복구된다.

### tmp_game 은 중복본이 아니다 (2026-09-30 확인)

`AssetDownloads/tmp_game/assets/` 는 `library/` 와 폴더 구조가 닮아서 **중복 덤프처럼 보인다.**
한 번 그렇게 보고 "지우면 8 GB 가 1 GB 가 된다"고 적었는데, **틀렸다.**
126,169개를 전부 sha256 으로 견줘 보니 이렇다:

```
tmp_game 6,891 MB
  밖에 사본이 있다      711 MB  (23,300개)
  tmp_game 에만 있다  6,180 MB  (72,073개)   ← 통째로 지우면 이만큼 사라진다
```

둘은 **정리 방식이 다른 별개의 수집물**이다 — `library/` 는 Kenney 팩 단위로,
`tmp_game/assets/` 는 audio·models·icons 처럼 종류별로 모아 두었고 출처도 더 넓다.
**구조가 닮았다는 것으로 내용이 같다고 판단하지 않는다.**

내용이 같은 사본은 2026-09-30 에 정리했다 — 33,029개 · 1,038 MB 를 지웠고,
지운 것은 전부 **같은 sha256 의 파일이 다른 자리에 남아 있는 것**뿐이다.
남길 쪽은 tmp_game 밖을 먼저 골랐다(`library/` 가 asset-index 가 가리키는 정규 자리다).
`asset-index` 가 링크하는 폴더 275개 중 비어 버린 것은 없다.

---

## 환경

| 항목 | 값 |
|---|---|
| .NET SDK | `C:\Users\PSI\.dotnet\dotnet.exe` (8.0.425, 사용자 설치라 PATH에 없다) |
| Unity 에디터 | `C:\Users\PSI\Unity\Hub\Editor\6000.0.81f1\Editor\Unity.exe` |
| 저장소 루트 | `C:\Users\PSI\orca\game_dev` |

**환경변수 파일은 저장소 최상위의 `.env` 한 개로만 관리한다.** 하위 폴더별 `.env`를 만들거나
루트 `.env`를 복사하지 않는다. Unity Assets·Resources·StreamingAssets 및 빌드에도 넣지 않는다.
`.env` 내용은 로그·문서·도구 출력·스크린샷에 노출하지 않는다.
`.env.example`은 값 없는 템플릿이다.

---

## 검증 — 엔진 없이 (0.3초)

**여기 있는 것은 거의 전부 이 길로 끝난다.**

```bash
C:/Users/PSI/.dotnet/dotnet.exe test games/<게임>/tests/<이름>.csproj
```

**목록을 만드는 단계가 없다.** 여기 시안들의 `.csproj` 는 `src/**/*.cs` 를 통째로 컴파일한다.
Unity 를 참조하지 않으므로 로직 파일에 `using UnityEngine` 을 끌어들이면 **그 자리에서 터진다** —
설계 원칙 1이 문서상의 약속이 아니라 게이트가 된다.

(파일 목록을 만드는 `sync_shared_logic.py` 는 `unity/Assets/Scripts` 가 있는 게임에만 쓰는데,
그런 게임은 여기 없어서 이 저장소에는 두지 않았다.)

### puzzle-tomorrow-map 만 Unity 가 필요하다

**그리고 이 시안은 혼자 다시 만들 수 없다.** `kit/tools/build_lowpoly_projects.py` 가
Unity 프로젝트를 구울 때 규칙 코드를 `games/farm-erosion/src/` 에서 가져왔는데,
farm-erosion 은 [다른 저장소](https://github.com/tkddls8848/game)로 갔다.

지금 `unity/Assets/Runtime/Rules/` 에 있는 것은 **갈라서던 시점의 사본**이다.
컴파일도 실행도 되지만 **갱신되지 않는다.** 규칙을 고쳐야 하면 두 저장소를 나란히 두고
그쪽 `src/` 를 가리키게 해야 한다.

Unity batchmode 는 반드시 `Start-Process -Wait` 로 실행한다(`Unity.exe` 는 GUI 서브시스템이라
`&` 로 부르면 종료를 기다리지 않는다). 로그는 파일로 받는다. PowerShell 스크립트는 ASCII 로만 쓴다.

---

## 설계 원칙 — **이게 이 저장소의 실제 자산이다**

시안을 많이 찍어 낼 수 있는 이유가 코드 재사용이 아니라 **이 규율**이다.

1. **로직에서 UnityEngine 의존을 뺀다.** 시간·판정·채점·시뮬레이션은 순수 C#
   → GUI 없이 검증 가능. **다작의 전제조건이다**
2. **스크립트로 만드는 것이 기본값이다. 다만 도구를 막지는 않는다.**
   씬·빌드·시나리오는 되도록 그것을 만드는 스크립트를 먼저 쓴다. 그러나 이것은 금지가 아니다 —
   유기적인 형태나 손으로 잡은 타이밍의 애니메이션은 코드로 정점을 찍어서는 나오지 않는다.
   손으로 만들 때는 **원본 파일(.blend 등)을 같이 남기고** 출처·라이선스를 `CREDITS.md` 에 적는다
3. **데이터는 JSON.** ScriptableObject(.asset)는 GUID YAML이라 텍스트로 안전하게 못 쓴다.
   `JsonUtility`는 Dictionary·다형성 불가 → **배열 + 문자열 ID로 평평하게**.
   빠진 int를 0으로 채우므로 **값 없는 int 필드는 `-1`을 명시**한다
4. **수치는 정수.** 시각은 ms, 배율은 백분율 정수 + 나머지 누적
5. **난수는 씨드 고정.** `UnityEngine.Random` 금지 — 한 번만 써도 헤드리스 재현이 깨진다
6. **한국어가 원문, 영어는 덮어쓰기.** 조각을 이어 붙이지 말고 `{0}` 자리표시자를 쓴다
7. **검사기 통과는 기술 완료의 필수 조건이지 재미의 증거가 아니다.**
   기계가 판정할 수 있는 것(고장), 화면으로 확인할 것(연출), 사람이 판정할 것(재미)을 **구별한다**

---

## 시안을 판정할 때 — 여기서만 다르다

**저쪽 저장소(`game`)는 감각까지 구현하는 것이 완료 기준이다. 여기는 아니다.**

여기 있는 것은 *만들 가치가 있는가*를 묻는 단계다. 목업 HTML 과 검사기로
"규칙이 실제로 굴러가는가"와 "콘텐츠 청구서가 얼마인가"를 본다.
**그 둘이 통과했다고 게임이 된 것은 아니다.**

시안이 살아남아 실제 게임이 되기로 하면 그때 [`game`](https://github.com/tkddls8848/game)
저장소로 옮기고, 거기서부터 그래픽·사운드·조작감에 타협하지 않는 기준을 적용한다.
졸업 기준은 [`docs/POC_FACTORY.md`](docs/POC_FACTORY.md).

---

## kit 도구

| 도구 | 하는 일 |
|---|---|
| `kit/tools/asset-collect/*.py` | 에셋 수집기(Kenney·Quaternius·itch·Poly Haven)와 컨셉별 색인 생성기 |
| `kit/tools/build_poc_gallery.py` · `build_revised_gallery.py` | 목업들을 모아 갤러리 HTML 을 만든다 |
| `kit/tools/asset_library.py` · `verify_asset_library.py` | 에셋 색인 생성·검증 |
| `kit/tools/shoot_mocks.js` · `verify_*.js` | 목업 스크린샷과 갤러리 검증 |
| `kit/tools/tts_generate.py` · `measure_voice_clips.py` · `build_voice_audition.py` | 음성 합성과 길이 측정 |
| `kit/tools/build_lowpoly_projects.py` | 로우폴리 Unity 프로젝트를 굽는다 (지금은 지도 퍼즐 하나뿐) |

**음성 세 도구의 기본 대상 게임은 다른 저장소로 갔다**(`games/mystery-blackwood`).
파일 맨 위에 표시해 두었으니 쓸 때 대상을 바꾼다.
