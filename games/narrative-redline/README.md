# 붉은 펜으로 남긴 것 · narrative-redline

**한 줄**: 문장을 고치는 일은, 누군가를 남기는 일.
**장르 표기**: editor · 내러티브 · 싱글플레이
**상태**: 슬라이스 — 순수 C# 규칙 + JSON + 플레이 가능한 HTML
**판정**: 2026-09-27. 기계 검증과 사람의 재미 판정은 별개. 사람의 판정은 미실시.
**검증**: `C:/Users/PSI/.dotnet/dotnet.exe test games/narrative-redline/tests/narrative-redline.Tests.csproj`
**다음**: 문장의 이해도와 작가의 목소리를 서로 다른 가치로 느끼는가?

## 플레이

저장소 루트에서 `python -m http.server 8000 --bind 127.0.0.1` 실행 후
`http://127.0.0.1:8000/games/narrative-redline/presentation/index.html`.

원고 위의 편집 도구로 문장을 지우고 문단을 옮기고 표현을 교체하세요. 원고를 돌려보내 작가의 답장을 읽습니다.

## 실제 구현 범위

짧은 한 장면의 핵심 조작과 복수 결과. 앞선 기획의 전체 캠페인이 아니다.
게임 규칙은 `src/Rules.cs`, 문구·초기값은 `data/scenario.json`에 있다.
테스트가 모든 도달 가능한 상태를 검사하고 `data/graph.json`으로 내보낸다.
브라우저는 이 그래프를 따라가므로 규칙을 JavaScript로 재구현하지 않는다.
난수·네트워크 API·외부 계정·음성 합성·유료 에셋을 사용하지 않는다.
`다시 시작`은 모든 진행을 초기화하며 세이브는 제공하지 않는다.

## 재생성

`python kit/tools/scaffold_revised_concepts.py` — 시나리오·엔트리·메타데이터 재생성.
그 뒤 위 테스트 명령으로 상태 그래프를 재생성한다.
`node kit/tools/verify_revised_pocs.js` — 브라우저 조작 검증과 갤러리 PNG 재생성.
공유 UI는 `kit/presentation/concept.js`와 `concept.css`.
재미 질문: **문장의 이해도와 작가의 목소리를 서로 다른 가치로 느끼는가?**
