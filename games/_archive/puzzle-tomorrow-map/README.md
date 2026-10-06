# 내일의 지도 · puzzle-tomorrow-map

**한 줄**: 이 선을 지우면, 내일 누군가의 문이 사라진다.
**장르 표기**: map · 내러티브 · 싱글플레이
**상태**: Unity 6 로우폴리 Windows PoC + 순수 C# 규칙 + JSON + 플레이 가능한 HTML

**Unity 실행**: [TomorrowMap.exe](unity/Build/TomorrowMap.exe). Build 폴더 전체가 필요합니다.
우측 도면 항목으로 골목·병원 입구를 수정하고, 민원을 읽어 지하 출입구를 이전한 뒤 승인합니다.
단일 퍼즐에서 세 결말을 확인할 수 있습니다. Q/E 회전, 휠 확대·축소, R 재시작, Esc 도움말.
Unity 프로젝트: `unity/`. 재생성·빌드 방법: [공통 안내](../../../kit/unity-lowpoly/README.md).
실제 화면: [Unity 갤러리](../../../docs/poc-gallery/lowpoly/playable.html) · 승인 시안 [`tomorrow-map-v1.png`](../../../docs/poc-gallery/lowpoly/tomorrow-map-v1.png).
**판정**: 2026-09-27. 기계 검증과 사람의 재미 판정은 별개. 사람의 판정은 미실시.
**검증**: `C:/Users/PSI/.dotnet/dotnet.exe test games/_archive/puzzle-tomorrow-map/tests/puzzle-tomorrow-map.Tests.csproj`
**다음**: 통행 퍼즐을 해결하면서 지도에서 누락된 주민도 살피게 되는가?

## 플레이

저장소 루트에서 `python -m http.server 8000 --bind 127.0.0.1` 실행 후
`http://127.0.0.1:8000/games/_archive/puzzle-tomorrow-map/presentation/index.html`.

골목을 넓히고 병원 입구를 연결하세요. 누락된 민원을 읽고 지하 세입자의 출입구를 이전한 뒤 도면을 승인합니다.

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
재미 질문: **통행 퍼즐을 해결하면서 지도에서 누락된 주민도 살피게 되는가?**
