# 내일의 지도 — 시안 재현 작업

이 게임은 승인 시안 `docs/poc-gallery/lowpoly/tomorrow-map-v1.png`의 구도·재질·조명·UI를 실제 3D 장면으로 재현한다. 그래픽·사운드·조작감 기준을 낮추지 않는다. 테스트·빌드 성공만으로 시안 구현 완료를 선언하지 않는다.

## 현재 제작 경로

- `art/reference-city.blend`: 사용자가 직접 편집할 원본. 자동 생성기로 덮어쓰지 않는다.
- `art/reference-city.generated.blend`: 생성기를 다시 실행했을 때의 별도 결과. 편집 원본과 구별한다.
- `tools/build_reference_city.py`: 최초 모델·재질·조명을 만드는 Blender 스크립트.
- `tools/bake_editable.py`: 현재 열어 둔 편집 원본을 Unity용으로 다시 굽는다. 원본을 변경·저장하지 않는다.
- `tools/publish_city.py`: 조명 텍스처의 표시 색 변환과 메시 압축. 비밀 설정을 사용하지 않는다.
- `presentation/`: 지도 전용 런타임·셰이더·Unity 빌드 원본.
- `tools/sync_reference_project.py`: 지도 프로젝트만 동기화한다. 농장 파일을 수정하지 않는다.
- `unity/Assets/Resources/ReferenceCity`: 실제 3D 메시 `.bytes`, 조명 텍스처 `.png`, 폰트와 CC0 조작음.
- 빌드 함수: **`ReferenceMapBuild.Build`**. 이전 공용 `PocBuild.Build`는 이전 화면을 생성하므로 사용하지 않는다.

시안 이미지를 배경으로 사용하는 방식이 아니다. Cycles의 조명 결과를 3D 메시 UV에 굽고 Unity에서 렌더한다. 조명이 구워진 정적 메시를 이동하면 기존 그림자가 남으므로 구조·배치 변경 후에는 다시 베이크한다. 동적 도면 표시·문·차량·UI는 Unity에서 처리한다.

실행 캡처는 `unity/Build/EvidenceReference`에 기록한다. Blender 확인 렌더는 에셋 제작 중간 결과이며 Unity 실행 증거로 제시하지 않는다. `.env`는 저장소 루트 한 개만 사용한다.
