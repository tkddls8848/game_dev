"""Build the complete PoC catalogue from games/, preserving independent variants."""
from pathlib import Path
from html import escape
import json
import re

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "docs/poc-gallery"
PAIRS = {
    "scent-layers": "mystery-scent-layers", "the-interpreter": "narrative-armistice",
    "locked-phone": "mystery-locked-phone", "grafted-memory": "puzzle-memory-suture",
    "the-map-lies": "puzzle-tomorrow-map", "curse-ledger": "management-curse-inheritance",
    "silent-baton": "rhythm-your-tempo", "red-pen": "narrative-redline",
    "last-tenants": "narrative-last-address", "gods-secretary": "puzzle-prayer-office",
}
NAMES = {
    "deck-rewind": "되감는 전투", "deck-openhand": "공개된 다음 수", "deck-attrition": "소모되는 활자",
    "farm-erosion": "밭이 줄어드는 세계", "farm-rewind-year": "되돌릴 수 있는 한 해", "farm-signal": "작물이 정보다",
    "hybrid-harvest-deck": "수확하는 덱", "hybrid-siege-seasons": "겨울 요새", "hybrid-sown-deck": "덱을 심는다",
    "tactics-partisan-1941": "능선 초소", "tactics-whisper-map": "속삭이는 지도",
    "mystery-blackwood": "블랙우드 저택", "scent-layers": "냄새로 푸는 추리", "the-interpreter": "번역가의 전쟁",
    "locked-phone": "죽은 사람의 휴대폰", "grafted-memory": "가짜 기억 심기", "the-map-lies": "지도가 거짓말하는 도시",
    "curse-ledger": "가업으로 물려받은 저주 관리", "silent-baton": "소리 없는 오케스트라",
    "red-pen": "원고 되돌려 보내기", "last-tenants": "철거 직전 건물의 마지막 세입자들", "gods-secretary": "신의 비서",
}
GENRES = {"deck": "덱빌더", "farm": "파밍", "hybrid": "하이브리드", "tactics": "전술", "narrative": "서사·추리", "puzzle": "퍼즐", "management": "경영", "rhythm": "리듬"}
OVERRIDES = {"curse-ledger":"management", "silent-baton":"rhythm", "the-map-lies":"puzzle", "grafted-memory":"puzzle", "gods-secretary":"management"}

def read(path):
    return path.read_text(encoding="utf-8-sig") if path.exists() else ""

def metadata(text, key):
    match = re.search(r"\*\*"+re.escape(key)+r"\*\*\s*[:：]\s*(.*?)(?=\n\s*\n|\n\*\*|$)", text, re.S)
    return re.sub(r"\s+", " ", re.sub(r"[*`]+", "", match.group(1))).strip() if match else ""

def build():
    revised = {p["slug"]:p for p in json.loads(read(OUT/"revised-concepts.json") or "[]")}
    reverse = {v:k for k,v in PAIRS.items()}
    rows=[]
    for folder in sorted((ROOT/"games").iterdir()):
        if not folder.is_dir(): continue
        slug=folder.name
        html=read(folder/"presentation/index.html")
        meaningful=len(re.sub(r"<!--.*?-->", "", html, flags=re.S).strip())>100
        doc=read(folder/"README.md")
        template="kit/templates/poc" in doc[:100]
        genre=OVERRIDES.get(slug,slug.split("-")[0])
        if genre not in GENRES: genre="narrative"
        if slug=="puzzle-prayer-office": genre="management"
        title=revised.get(slug,{}).get("title",NAMES.get(slug,slug))
        summary=revised.get(slug,{}).get("tagline") or (metadata(doc,"한 줄") if not template else "")
        if not summary: summary="개별 README의 기획 설명이 아직 정리되지 않았습니다."
        status="playable" if slug in revised and (folder/"data/graph.json").exists() and meaningful else "mock" if meaningful else "unity" if (folder/"unity").is_dir() else "pending"
        screenshot=f"png/{slug}.png" if (OUT/f"png/{slug}.png").exists() else None
        rows.append(dict(slug=slug,title=title,summary=summary,genre=genre,genreLabel=GENRES[genre],
            collection="revised" if slug in revised else "original" if slug in PAIRS else "existing",
            status=status,declaredStatus=metadata(doc,"상태") if not template else "README 정리 필요",
            presentation=f"../../games/{slug}/presentation/index.html" if meaningful else None,
            screenshot=screenshot,fullScreenshot=f"png/{slug}-full.png" if (OUT/f"png/{slug}-full.png").exists() else None,
            readme=f"../../games/{slug}/README.md" if (folder/"README.md").exists() else None,
            related=PAIRS.get(slug,reverse.get(slug)),needsReadme=template))
    catalogue={"version":1,"total":len(rows),"entries":rows}
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT/"catalog.json").write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    template=read(ROOT/"kit/templates/gallery.html")
    payload=json.dumps(catalogue,ensure_ascii=False).replace("<","\\u003c")
    for name,view in [("index.html","all"),("shots.html","screenshots")]:
        (OUT/name).write_text(template.replace("__CATALOG__",payload).replace("__DEFAULT_VIEW__",view),encoding="utf-8")
    status_labels={"playable":"브라우저 PoC","mock":"HTML 목업","pending":"목업 준비 중","unity":"Unity 전용"}
    inventory="\n".join(f"| {escape(p['title'])} | `{p['slug']}` | {status_labels[p['status']]} | {'있음' if p['screenshot'] else '없음'} | {p['related'] or '—'} |" for p in rows)
    (OUT/"README.md").write_text(f'''# PoC 통합 갤러리

게임 폴더 {len(rows)}개를 자동 검색해 목록을 생성한다. 원안과 수정안은 독립된 구현이므로 삭제하거나 합치지 않는다.

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
{inventory}
''',encoding="utf-8")
    print(f"Catalogued {len(rows)} projects; {sum(bool(r['screenshot']) for r in rows)} screenshots; {sum(r['status']=='pending' for r in rows)} pending; {sum(r['status']=='unity' for r in rows)} Unity-only.")

if __name__=="__main__":build()
