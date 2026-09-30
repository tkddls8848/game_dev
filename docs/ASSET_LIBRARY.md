# 에셋 라이브러리

원본 위치: `C:/Users/PSI/orca/game/AssetDownloads`.

2026-09-27 정리 결과: 신규 공식 CC0 팩 54개 다운로드 성공, 실패 0개. 기존 보유분을 포함해 30,782개 파일 / 1.03 GiB를 색인했다. 동일 내용 중복 577그룹(중복 사본 56.2 MiB)은 보존했다. 빈 파일은 없으며, 기존 파일 141개는 파일별 라이선스 매핑을 추가 확인해야 한다.

검증: glTF/GLB 4,704개 형식·의존 파일 확인, 오류 0개. 이 중 애니메이션 포함 파일 150개에서 클립 항목 1,928개를 색인했다(중복 포함). 검색·필터·페이지 확장·54개 미리보기·모바일 표시 검사를 통과했다.

**[에셋 검색·미리보기](../AssetDownloads/index.html)**에서 시작한다. HTML 파일을 직접 열어도 동작하며 검색·종류별 필터·전체 파일 보기를 제공한다. 원본 ZIP·압축 해제 폴더·공식 출처·라이선스 근거로 이동할 수 있다.

## 2026-09-28 추가 — tmp_game 컬렉션 (7.0 GB)

원격 서버에서 가져온 별도 컬렉션이다. **기존 `AssetDownloads/` 내용과 섞지 않고**
`AssetDownloads/tmp_game/assets/` 아래에 따로 둔다 — 두 컬렉션의 색인 체계가 다르고,
기존 `index.html`이 자기 경로를 쓰기 때문이다.

* **목록**: [`asset-index/INDEX.md`](asset-index/INDEX.md) — 컨셉 20주제별로 무엇이 확보됐고
  무엇이 비었는지 표로 정리돼 있다(✅/⚠️/❌)
* **전체 목록**: [`asset-index/CONCEPT_PACKS.md`](asset-index/CONCEPT_PACKS.md) — 4,208항목,
  `kit/tools/asset-collect/build_concept_index.py`가 각 `SOURCE.json`의 `usedFor`에서 생성
* **컨셉안**: [`CONCEPTS.md`](CONCEPTS.md#컨셉-71) — 컨셉 61개를 한 목록에 두고, 에셋 확보 상태를 열로 달았다

구성: 오디오 2.8 GB(276팩·5,069파일) · 모델 2.6 GB(Kenney 38·Quaternius 23·Poly Haven 392) ·
이미지 936 MB · 아이콘 347 MB(game-icons 4,239 포함) · 폰트 321 MB(76패밀리) · 셰이더 46 MB(105종).
출처 기록 1,019개.

**바이너리는 git에 넣지 않고 목록만 넣는다.** 7 GB를 추적할 수 없지만, 저장소가 무엇이
있는지는 알아야 한다 — `docs/asset-index/`가 그 역할이다.

## 정리 원칙

기존 `audition`, `concepts`, `deckbuilder`, `farming`, `hybrid`, `Kenney`, `tactics`의 파일과 경로는 보존한다. 게임 문서와 음성 청취실이 해당 경로를 사용하므로 목록을 통합하는 방식으로 정리했다. 중복 사본은 해시로 식별하고 보고서에 남겼으며 삭제하지 않았다.

신규 미니폴리·로우폴리·연출 팩은 다음 구조를 따른다.

```text
AssetDownloads/
  index.html                        검색·미리보기
  README.md                         폴더 안내·실측 통계
  library/kenney/<pack>/
    kenney_<pack>.zip                공식 원본
    contents/                       압축 해제한 실제 에셋·동봉 라이선스
    source.html                     배포 페이지 확인용 사본
    preview.png                     카탈로그 확인용 이미지
    provenance.json                 URL·취득 시각·라이선스·SHA-256
  _catalog/
    inventory.csv                   Excel용 UTF-8 BOM 파일 목록
    inventory.json                  파일별 종류·크기·출처·라이선스 근거·해시
    packs.json                      신규 팩 목록
    duplicates.json                 내용이 같은 중복 그룹
    review-needed.json              빈 파일·라이선스 매핑 미확인 목록
    model-index.json                glTF/GLB 모델·애니메이션 이름
    animations.csv                  애니메이션을 찾는 모델 경로
    collection-run.json             다운로드 성공·실패 기록
    verification.json               원본 해시·라이선스 파일·모델 의존 파일 검사
    browser-verification.json       검색 화면 검사
```

## 가져다 쓰기

- 미니폴리 통일감: `mini-characters`, `mini-forest`, `mini-dungeon`, `mini-market`, `mini-arena`, `mini-arcade`, `mini-skate`.
- 마을·실내: `city-kit-*`, `furniture-kit`, `building-kit`, `fantasy-town-kit`.
- 자연·던전: `nature-kit`, `modular-cave-kit`, `modular-dungeon-kit`, `graveyard-kit`.
- 장면 연출 보조: `particle-pack`, `smoke-particles`, `light-masks`, `skyboxes`.
- 애니메이션 후보: `animated-characters-*`, `blocky-characters`, `cube-pets`, 미니 캐릭터. 실제 클립 이름은 `_catalog/animations.csv`에서 확인한다.

게임에 쓸 때는 `contents` 안의 필요한 모델과 종속 텍스처만 해당 게임 에셋 폴더에 복사하고 `CREDITS.md`에 출처·라이선스·해시를 적는다. 카탈로그의 preview와 source.html은 출처 확인용이며 게임 에셋으로 분류하지 않는다. FBX/GLTF/GLB 등 여러 형식의 같은 모델을 동시에 임포트할 필요는 없다.

신규 팩은 공식 페이지 CC0 링크와 동봉 라이선스를 확인했다. 기존 파일은 근거를 찾은 범위까지만 표시한다. 라이선스 파일이 주변에 있다는 이유만으로 모든 파일에 같은 라이선스를 부여하지 않는다. 생성 음성 청취본의 공급자 약관·상업 이용 검토는 별도 상태다.

파일 수에는 형식 변형·색상 변형·렌더 이미지·라이선스가 포함되며 고유 모델 수가 아니다. 애니메이션 항목 수도 여러 모델의 동일 클립을 중복 포함한다. Unity 임포트·리타기팅·장면 연출 품질은 이번 수집·정리 검사에 포함하지 않는다.

## 유지보수

```powershell
python kit/tools/asset_library.py collect-kenney
python kit/tools/asset_library.py index
python kit/tools/verify_asset_library.py
node kit/tools/verify_asset_catalog.js
```

이미 받은 팩은 원본 해시를 확인하고 재다운로드하지 않는다. 새 파일을 추가했으면 index로 목록을 갱신한다. `AssetDownloads`는 기존 gitignore 대상이므로 원본은 별도로 백업한다. 목록 생성 도구와 이 안내서는 저장소에 보존된다.
