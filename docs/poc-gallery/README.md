# PoC 통합 갤러리

게임 폴더 32개를 자동 검색해 목록을 생성한다. 원안과 수정안은 독립된 구현이므로 삭제하거나 합치지 않는다.

- [전체 목록](index.html): 검색, 장르, 원안/수정안, 화면 유형, 정렬, 관련 버전 이동.
- [PNG만 보기](shots.html): 파일로 바로 열리는 캡처 목록.
- [수정 컨셉 10종](revised.html): 기존의 전용 큐레이션 페이지.
- [기계가 읽는 목록](catalog.json): games/와 실제 파일 존재 여부에서 생성.

## 파일 규칙

- `png/<slug>.png`: 게임별 대표 화면, 1600×1000.
- `png/<slug>-full.png`: 긴 목업의 전체 화면. 필요할 때 재생성.
- `png/revised-gallery.png`: 개별 게임이 아닌 갤러리 소개 화면. 게임 수에 포함하지 않음.
- `revised-*.json`: 수정안 10종의 메타데이터와 별도 플레이 검증 기록.
- `capture-report.json`: 최근 캡처의 성공/실패/건너뜀 기록. 게임 규칙 테스트 결과가 아님.

게임 실행 파일·데이터는 `games/<slug>/`가 원본이다. 이 폴더에 복제하지 않는다.
HTML 목업은 게임 전체가 플레이 가능하다는 뜻이 아니다. 브라우저 PoC 표시는
수정안의 상태 그래프와 진입 페이지가 함께 존재하는 경우에만 붙인다.
미리보기 목록은 서버 없이 열리지만, JSON을 읽는 게임 화면은 HTTP 서버가 필요하다.

## 갱신

```powershell
python kit/tools/build_poc_gallery.py
node kit/tools/shoot_mocks.js --missing
```

특정 게임만 다시 찍기: `node kit/tools/shoot_mocks.js deck-attrition`.
일부만 찍어도 갤러리 전체를 재검색하므로 다른 게임 카드가 사라지지 않는다.
브라우저 실행: 저장소 루트에서 `python -m http.server 0 --bind 127.0.0.1`을 실행하고,
표시된 포트의 `/docs/poc-gallery/index.html`에 접속한다.

## 목록

| 제목 | 폴더 | 화면 유형 | PNG | 관련 버전 |
|---|---|---|---|---|
| 가업으로 물려받은 저주 관리 | `curse-ledger` | HTML 목업 | 있음 | management-curse-inheritance |
| 소모되는 활자 | `deck-attrition` | HTML 목업 | 있음 | — |
| 공개된 다음 수 | `deck-openhand` | HTML 목업 | 있음 | — |
| 되감는 전투 | `deck-rewind` | HTML 목업 | 있음 | — |
| 밭이 줄어드는 세계 | `farm-erosion` | HTML 목업 | 있음 | — |
| 되돌릴 수 있는 한 해 | `farm-rewind-year` | HTML 목업 | 있음 | — |
| 작물이 정보다 | `farm-signal` | HTML 목업 | 있음 | — |
| 신의 비서 | `gods-secretary` | HTML 목업 | 있음 | puzzle-prayer-office |
| 가짜 기억 심기 | `grafted-memory` | HTML 목업 | 있음 | puzzle-memory-suture |
| 수확하는 덱 | `hybrid-harvest-deck` | HTML 목업 | 있음 | — |
| 겨울 요새 | `hybrid-siege-seasons` | HTML 목업 | 있음 | — |
| 덱을 심는다 | `hybrid-sown-deck` | HTML 목업 | 있음 | — |
| 철거 직전 건물의 마지막 세입자들 | `last-tenants` | HTML 목업 | 있음 | narrative-last-address |
| 죽은 사람의 휴대폰 | `locked-phone` | HTML 목업 | 있음 | mystery-locked-phone |
| 저주상속 | `management-curse-inheritance` | 브라우저 PoC | 있음 | curse-ledger |
| 블랙우드 저택 | `mystery-blackwood` | Unity 전용 | 없음 | — |
| 잠금 해제 불가 | `mystery-locked-phone` | 브라우저 PoC | 있음 | locked-phone |
| 잔향 감식실 | `mystery-scent-layers` | 브라우저 PoC | 있음 | scent-layers |
| 휴전의 문장 | `narrative-armistice` | 브라우저 PoC | 있음 | the-interpreter |
| 마지막 이삿날 | `narrative-last-address` | 브라우저 PoC | 있음 | last-tenants |
| 붉은 펜으로 남긴 것 | `narrative-redline` | 브라우저 PoC | 있음 | red-pen |
| 기억의 봉합사 | `puzzle-memory-suture` | 브라우저 PoC | 있음 | grafted-memory |
| 기적 배정과 | `puzzle-prayer-office` | 브라우저 PoC | 있음 | gods-secretary |
| 내일의 지도 | `puzzle-tomorrow-map` | 브라우저 PoC | 있음 | the-map-lies |
| 원고 되돌려 보내기 | `red-pen` | HTML 목업 | 있음 | narrative-redline |
| 당신의 박자 | `rhythm-your-tempo` | 브라우저 PoC | 있음 | silent-baton |
| 냄새로 푸는 추리 | `scent-layers` | HTML 목업 | 있음 | mystery-scent-layers |
| 소리 없는 오케스트라 | `silent-baton` | HTML 목업 | 있음 | rhythm-your-tempo |
| 능선 초소 | `tactics-partisan-1941` | HTML 목업 | 있음 | — |
| 속삭이는 지도 | `tactics-whisper-map` | HTML 목업 | 있음 | — |
| 번역가의 전쟁 | `the-interpreter` | HTML 목업 | 있음 | narrative-armistice |
| 지도가 거짓말하는 도시 | `the-map-lies` | HTML 목업 | 있음 | puzzle-tomorrow-map |
