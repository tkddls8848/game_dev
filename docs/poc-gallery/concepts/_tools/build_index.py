#!/usr/bin/env python3
"""컨셉 전체 시안 색인(docs/poc-gallery/concepts/index.html)을 만든다.

    python docs/poc-gallery/concepts/_tools/build_index.py

입력: _tools/concepts.json (docs/CONCEPTS.md 표에서 뽑은 번호·제목·한 줄)과
각 <NN>-<slug>/ 폴더의 index.html · mock-a.jpg · mock-b.jpg 실제 존재 여부.
"""
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
concepts = json.loads((ROOT / "_tools" / "concepts.json").read_text(encoding="utf-8"))

cards, missing = [], []
for c in concepts:
    folder = f"{c['num']:02d}-{c['slug']}"
    d = ROOT / folder
    has_html = (d / "index.html").exists()
    shots = [f"{folder}/mock-{k}.jpg" for k in "ab" if (d / f"mock-{k}.jpg").exists()]
    if not has_html or len(shots) < 2:
        missing.append(folder)
    built = "game_dir" in c
    status = "구현됨" if built else ("새 컨셉" if c.get("batch") else "미착수")
    imgs = "".join(
        f'<a class="shot" href="{s}"><img src="{s}" alt="{html.escape(c["title"])} 목업 {s[-5].upper()}" loading="lazy"></a>'
        for s in shots
    ) or '<div class="shot empty">이미지 없음</div>'
    links = [f'<a class="play" href="{folder}/index.html#a">HTML 시안 ↗</a>'] if has_html else []
    if built:
        links.append(f'<a href="../../{c["game_dir"]}/presentation/index.html">기존 PoC</a>')
        links.append(f'<a href="../../{c["game_dir"]}/README.md">README</a>')
    cards.append(f"""<article class="card" data-built="{int(built)}" data-q="{html.escape((c['title']+' '+c['oneline']+' '+c['genre']).lower())}">
<div class="shots">{imgs}</div>
<div class="body"><div class="meta"><span>#{c['num']:02d}</span><span>{html.escape(c['genre'])}</span><span class="st st-{int(built)}">{status}</span></div>
<h2>{html.escape(c['title'])}</h2><p>{html.escape(c['oneline'])}</p>
<div class="links">{''.join(links)}</div></div></article>""")

page = f"""<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>컨셉 시안 {len(concepts)}</title><link rel="icon" href="data:,">
<style>
:root{{color-scheme:dark;--bg:#141615;--card:#1c201e;--ink:#e8e3d6;--muted:#9c9a92;--accent:#d2b97c;--line:#ffffff1c}}
*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--ink);font:14px/1.7 'Pretendard','Malgun Gothic','Noto Sans KR',sans-serif}}
a{{color:inherit}}main{{max-width:1560px;margin:auto;padding:28px 32px 70px}}
nav{{display:flex;gap:22px;flex-wrap:wrap;font-size:12px;color:var(--muted);border-bottom:1px solid var(--line);padding-bottom:18px}}
header{{padding:44px 0 26px}}.eyebrow{{font:10px/1.5 Consolas,monospace;letter-spacing:2px;color:var(--accent)}}
h1{{font:44px/1.3 Batang,'Noto Serif KR',serif;letter-spacing:-1.5px;margin:10px 0}}header p{{color:var(--muted);max-width:760px;margin:0}}
.bar{{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin:0 0 24px}}.bar input{{flex:1;min-width:200px;background:#0f1110;border:1px solid var(--line);color:var(--ink);padding:9px 12px;font:inherit}}
.bar button{{background:none;border:1px solid var(--line);color:var(--muted);padding:8px 14px;font:inherit;cursor:pointer}}.bar button.on{{border-color:var(--accent);color:var(--accent)}}
.grid{{display:grid;grid-template-columns:repeat(auto-fill,minmax(460px,1fr));gap:22px}}
.card{{background:var(--card);border:1px solid var(--line);overflow:hidden;display:flex;flex-direction:column}}
.shots{{display:grid;grid-template-columns:1fr 1fr;gap:2px;background:#000}}.shot{{display:block;aspect-ratio:16/9;overflow:hidden;background:#222}}
.shot img{{width:100%;height:100%;object-fit:cover;display:block;transition:transform .25s}}.shot:hover img{{transform:scale(1.04)}}
.shot.empty{{grid-column:1/3;display:grid;place-items:center;color:var(--muted);font-size:12px}}
.body{{padding:18px 20px 20px}}.meta{{display:flex;gap:10px;font:11px Consolas,monospace;color:var(--muted)}}.st-0{{color:#9bb7c9}}.st-1{{color:var(--accent)}}
h2{{font:23px/1.4 Batang,'Noto Serif KR',serif;margin:6px 0 4px}}.body p{{color:var(--muted);font-size:13px;margin:0 0 14px}}
.links{{display:flex;gap:16px;font-size:12px;align-items:center}}.links a{{text-decoration:none}}.links a:hover{{text-decoration:underline}}
.links .play{{border:1px solid #d2b97c70;color:var(--accent);padding:5px 11px}}
footer{{margin-top:34px;padding-top:18px;border-top:1px solid var(--line);color:var(--muted);font-size:11px}}
@media(max-width:560px){{main{{padding:18px 16px 50px}}h1{{font-size:32px}}.grid{{grid-template-columns:1fr}}}}
</style></head><body><main>
<nav><a href="../index.html">← PoC 갤러리</a><a href="../../CONCEPTS.md">CONCEPTS.md</a><a href="_critique/index.html">비판 리뷰 전후 (3안)</a></nav>
<header><span class="eyebrow">{len(concepts)} CONCEPTS · HTML MOCKUP + 2 SCREEN MOCKS EACH</span><h1>컨셉 시안 {len(concepts)}</h1>
<p>docs/CONCEPTS.md의 모든 컨셉을 같은 조건으로 놓았다. 카드마다 HTML 시안(화면 A/B, 키 1·2로 전환)과 AI 생성 화면 목업 2장.
<b>목업 이미지는 AI 생성 시안이며 실행 빌드 화면이 아니다.</b> 구현된 컨셉은 기존 PoC 링크도 함께 둔다.</p></header>
<div class="bar"><input id="q" placeholder="제목·장르 검색"><button data-f="all" class="on">전체 {len(concepts)}</button><button data-f="0">미착수</button><button data-f="1">구현됨</button></div>
<section class="grid" id="grid">{''.join(cards)}</section>
<footer>생성: <code>python docs/poc-gallery/concepts/_tools/build_index.py</code> · 이미지: <code>_tools/gen_images.py</code> (Codex image_gen) · 누락 {len(missing)}건</footer>
</main><script>
var f='all',q=document.getElementById('q'),cards=[].slice.call(document.querySelectorAll('.card'));
function apply(){{var s=q.value.trim().toLowerCase();cards.forEach(function(c){{c.style.display=((f==='all'||c.dataset.built===f)&&(!s||c.dataset.q.indexOf(s)>=0))?'':'none'}})}}
q.addEventListener('input',apply);[].forEach.call(document.querySelectorAll('.bar button'),function(b){{b.onclick=function(){{f=b.dataset.f;[].forEach.call(document.querySelectorAll('.bar button'),function(x){{x.classList.toggle('on',x===b)}});apply()}}}});
</script></body></html>"""
(ROOT / "index.html").write_text(page, encoding="utf-8")
print(f"{len(concepts)} cards, missing: {missing}")
