# orca/game_dev — 컨셉 시안 · 에셋 · 음악

**게임을 구상하는 저장소다.** 컨셉 71개, 시안 29개, 컨셉 이미지 451장, 생성 음악, 에셋 목록이
여기 있다. 대부분은 만들어지지 않는다. 그게 정상이고, 접은 것도 이유를 적어 남긴다.

> **깊게 개발 중인 것은 여기 없다.**
> [`tkddls8848/game`](https://github.com/tkddls8848/game) — `farm-erosion`(개발 중) ·
> `mystery-blackwood`(거의 완성). 아래 문서에서 그 둘의 이름이 보이면 **설명이지 링크가 아니다.**

* **작업 규율** → [`CLAUDE.md`](CLAUDE.md) — 순수 C# 분리 · 생성기 · 정수 수치 · 씨드 고정 · 검사기
* **폴더 규약과 작업 순서** → [`docs/POC_FACTORY.md`](docs/POC_FACTORY.md)
* **문서 색인** → [`docs/README.md`](docs/README.md)

---

## 무엇이 있나

| | 수 | 어디 |
|---|---:|---|
| 컨셉 | 71 | [`docs/CONCEPTS.md`](docs/CONCEPTS.md) — 미착수 40 + 구현 31을 한 표에 |
| 시안(코드까지 간 것) | 29 | [`games/`](games/) — 대부분 순수 C# 규칙 + 목업 HTML |
| 컨셉 이미지 | 451 | [`docs/poc-gallery/concepts/`](docs/poc-gallery/concepts/) |
| 장르 계획 | 5 | [`docs/PLAN_GENRES.md`](docs/PLAN_GENRES.md) |
| 에셋 목록 | — | [`docs/asset-index/`](docs/asset-index/) — **목록만.** 바이너리는 추적하지 않는다 |

**화면을 보려면** → [`docs/poc-gallery/shots.html`](docs/poc-gallery/shots.html) (찍어 둔 PNG, 그냥 열린다)
· 살아 있는 목업은 [`docs/poc-gallery/index.html`](docs/poc-gallery/index.html) — **서버로 열어야 한다**
(목업이 `fetch('../data/*.json')`를 쓰는데 `file://`에서는 막힌다. 뿌리에서 `python -m http.server`)

**Unity 셸이 있는 시안은 [`games/_archive/puzzle-tomorrow-map`](games/_archive/puzzle-tomorrow-map/) 하나다.**
나머지 28개는 헤드리스 수직 슬라이스다.

> **전부 "재미 미확인"이다.** 검사기는 고장을 잡을 뿐이고, 재미는 사람이 플레이해야 안다.
> 각 시안의 `README.md` `판정` 칸에 기계가 본 것과 사람만 볼 수 있는 것을 갈라 적었다.

---

## 빠른 시작

```bash
# 헤드리스 테스트 — 대부분 1초 안에 끝난다. 작업의 대부분이 여기서 끝난다
C:/Users/PSI/.dotnet/dotnet.exe test games/<게임>/tests/<이름>.csproj
```

API 키는 저장소 뿌리의 `.env`에서 읽는다(gitignore에 있다). 필요한 목록은 `.env.example`.

## 지도

```
games/         시안 하나 = 폴더 하나 (29개). 각자 README.md 를 갖는다
kit/           게임이 아닌 것 — 에셋 수집기 · 갤러리 생성기 · TTS · Unity 셸
docs/          컨셉 71 · 장르 계획 5 · 에셋 목록 · 갤러리 ← docs/README.md
AssetDownloads/  내려받은 원본 6.9 GB (gitignore). 아래를 읽을 것
```

## ⚠️ 에셋 원본은 git 에 없다

`AssetDownloads/` 는 **6.9 GB**라 GitHub 에 들어가지 않는다(권장 상한 5 GB · LFS 무료 1 GB).
그리고 넣을 필요가 없다 — `docs/asset-index/` 가 목록을 들고 있고
`kit/tools/asset-collect/*.py` 가 Kenney · Quaternius · Poly Haven · itch 에서 다시 내려받는다.

> **`tmp_game` 은 `library` 의 중복본이 아니다.** 구조가 닮았을 뿐 내용의 90% 는 거기에만 있다.
> 근거와 정리 기록은 [`CLAUDE.md`](CLAUDE.md#tmp_game-은-중복본이-아니다-2026-09-30).

그래서 `docs/asset-index/*.md` 의 `../../AssetDownloads/...` 링크는 **깨져 있는 것이 정상이다.**
내려받은 사람의 디스크에서만 열린다.

**다시 만들 수 없는 것은 git 에 있다** — 컨셉 이미지 451장 · 티저 영상 · 생성 음악.
유료 크레딧으로 한 번 만든 것이라 지우면 돈을 다시 써야 한다.
