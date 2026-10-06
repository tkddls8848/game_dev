# 당신의 박자 · rhythm-your-tempo

**한 줄**: 들리지 않아도, 함께 시작할 수 있다.
**장르 표기**: baton · 내러티브 · 싱글플레이
**상태**: 슬라이스 — 순수 C# 규칙 + JSON + 플레이 가능한 HTML
**판정**: 2026-09-27. 기계 검증과 사람의 재미 판정은 별개. 사람의 판정은 미실시.
**검증**: `C:/Users/PSI/.dotnet/dotnet.exe test games/_archive/rhythm-your-tempo/tests/rhythm-your-tempo.Tests.csproj`
**다음**: 소리 없이 준비 동작과 박자만으로 합주의 성취를 느낄 수 있는가?

## 플레이

저장소 루트에서 `python -m http.server 8000 --bind 127.0.0.1` 실행 후
`http://127.0.0.1:8000/games/_archive/rhythm-your-tempo/presentation/index.html`.

밝은 구간에 박자가 들어올 때 큐를 보내세요. 무음 시각 리듬입니다. 연습 모드에서는 박자를 직접 한 칸씩 이동할 수 있습니다.

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
재미 질문: **소리 없이 준비 동작과 박자만으로 합주의 성취를 느낄 수 있는가?**
