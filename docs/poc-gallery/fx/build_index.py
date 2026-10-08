#!/usr/bin/env python3
"""효과 구현 샘플 색인(docs/poc-gallery/fx/index.html)을 만든다.

    python docs/poc-gallery/fx/build_index.py

입력: catalog.json(효과 24종 · 쓰는 컨셉 번호 · 보관된 안은 archived_concepts 에 구번호)과 각 <slug>/index.html · thumb.jpg 존재 여부,
      컨셉 제목은 ../concepts/_tools/concepts.json.
"""
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
fx = json.loads((ROOT / "catalog.json").read_text(encoding="utf-8"))
_all = json.loads((ROOT.parent / "concepts" / "_tools" / "concepts.json").read_text(encoding="utf-8"))
# 보관된 안은 구번호를 그대로 쓰므로 활성 번호와 겹친다 — 따로 찾는다
concepts = {c["num"]: c for c in _all if not c.get("archived")}
archived = {c["num"]: c for c in _all if c.get("archived")}

cards, missing = [], []
for e in fx:
    d = ROOT / e["slug"]
    if not (d / "index.html").exists():
        missing.append(e["slug"])
        continue
    thumb = (f'<img src="{e["slug"]}/thumb.jpg" alt="{html.escape(e["title"])}" loading="lazy">'
             if (d / "thumb.jpg").exists() else '<div class="ph">미리보기 없음</div>')
    uses = "".join(
        f'<a href="../concepts/{n:02d}-{concepts[n]["slug"]}/index.html">#{n} {html.escape(concepts[n]["title"])}</a>'
        for n in e["concepts"] if n in concepts) + "".join(
        f'<a href="../concepts/_archive/{n:02d}-{archived[n]["slug"]}/index.html">구#{n} {html.escape(archived[n]["title"])}</a>'
        for n in e.get("archived_concepts", []) if n in archived)
    cards.append(f"""<article class="card"><a class="shot" href="{e['slug']}/index.html">{thumb}</a>
<div class="body"><h2><a href="{e['slug']}/index.html">{html.escape(e['title'])}</a></h2>
<p>{html.escape(e['desc'])}</p><div class="uses">{uses}</div></div></article>""")

page = f"""<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>효과 구현 샘플</title><link rel="icon" href="data:,">
<style>
:root{{color-scheme:dark;--bg:#121413;--card:#1b1f1d;--ink:#e8e3d6;--muted:#9c9a92;--accent:#8fd0b0;--line:#ffffff1c}}
*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--ink);font:14px/1.7 'Pretendard','Malgun Gothic','Noto Sans KR',sans-serif}}
a{{color:inherit}}main{{max-width:1560px;margin:auto;padding:28px 32px 70px}}
nav{{display:flex;gap:20px;font-size:12px;color:var(--muted);border-bottom:1px solid var(--line);padding-bottom:16px}}
.eyebrow{{font:10px/1.5 Consolas,monospace;letter-spacing:2px;color:var(--accent)}}h1{{font:40px/1.3 Batang,'Noto Serif KR',serif;margin:10px 0}}
.lead{{color:var(--muted);max-width:860px}}.grid{{display:grid;grid-template-columns:repeat(auto-fill,minmax(360px,1fr));gap:20px;margin-top:26px}}
.card{{background:var(--card);border:1px solid var(--line);overflow:hidden}}.shot{{display:block;aspect-ratio:16/10;background:#000;overflow:hidden}}
.shot img{{width:100%;height:100%;object-fit:cover;display:block;transition:transform .25s}}.shot:hover img{{transform:scale(1.04)}}
.ph{{height:100%;display:grid;place-items:center;color:var(--muted);font-size:12px}}.body{{padding:14px 16px 16px}}
h2{{font-size:18px;margin:0 0 4px}}h2 a{{text-decoration:none}}.body p{{color:var(--muted);font-size:12.5px;margin:0 0 10px}}
.uses{{display:flex;flex-wrap:wrap;gap:6px}}.uses a{{font-size:11px;border:1px solid var(--line);padding:2px 7px;text-decoration:none;color:var(--muted)}}
.uses a:hover{{color:var(--ink);border-color:var(--accent)}}footer{{margin-top:30px;color:var(--muted);font-size:11px}}
@media(max-width:560px){{main{{padding:18px 16px}}.grid{{grid-template-columns:1fr}}h1{{font-size:30px}}}}
</style></head><body><main>
<nav><a href="../index.html">← PoC 갤러리</a><a href="../concepts/index.html">컨셉 시안</a><a href="../../MUSIC_DIRECTION.md">음악 방향</a><a href="../../ENGINE_FIT.md">엔진 적합성</a></nav>
<span class="eyebrow" style="display:block;margin-top:28px">EFFECT SAMPLES · {len(cards)} KINDS · 3–4 VARIANTS EACH</span>
<h1>효과 구현 샘플</h1>
<p class="lead">컨셉들이 공통으로 필요로 하는 효과를 효과별로 모았다. 각 샘플은 단일 HTML(Canvas2D/WebGL + WebAudio)로 파일에서 바로 열린다.
변형마다 쓰는 컨셉이 붙어 있고, 조작→시각·소리 피드백, 수치 슬라이더(게임 이식용 JSON), Unity 이식 메모가 있다.
<b>헤드리스 소프트웨어 렌더링에서만 확인했다 — 실제 GPU 프레임과 소리의 만족도는 사람이 판정해야 한다.</b></p>
<section class="grid">{''.join(cards)}</section>
<footer>생성: <code>python docs/poc-gallery/fx/build_index.py</code> · 누락 {len(missing)}건 {' '.join(missing)}</footer>
</main></body></html>"""
(ROOT / "index.html").write_text(page, encoding="utf-8")
print(len(cards), "cards; missing:", missing)
