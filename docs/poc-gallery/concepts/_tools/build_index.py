#!/usr/bin/env python3
"""컨셉 전체 시안 색인(docs/poc-gallery/concepts/index.html)을 만든다.

    python docs/poc-gallery/concepts/_tools/build_index.py

입력: _tools/concepts.json (번호·제목·한 줄), engine-groups.json (권장 엔진별 번호),
각 <NN>-<slug>/ 폴더의 index.html · mock-a.jpg · mock-b.jpg 실제 존재 여부.
"""
import html
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
concepts = json.loads((ROOT / "_tools" / "concepts.json").read_text(encoding="utf-8"))
# 보관된 안(CONCEPTS.md "보관" 절)은 폴더는 남기고 갤러리에서만 뺀다
concepts = [c for c in concepts if not c.get("archived")]
groups = json.loads((ROOT / "_tools" / "engine-groups.json").read_text(encoding="utf-8"))
# 보관된 번호는 엔진 분류에 남아 있어도 갤러리에는 그리지 않는다
active = {c['num'] for c in concepts}
for g in groups:
    g['numbers'] = [n for n in g['numbers'] if n in active]
assigned = [n for g in groups for n in g['numbers']]
if len(assigned) != len(set(assigned)) or set(assigned) != {c['num'] for c in concepts}:
    raise ValueError('엔진 분류에 중복·누락 또는 알 수 없는 컨셉 번호가 있습니다')
engine_for = {n: g for g in groups for n in g['numbers']}
group_cards = {g['id']: [] for g in groups}

cards, missing = [], []
for c in concepts:
    engine = engine_for[c['num']]
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
        links.append(f'<a href="../../../{c["game_dir"]}/presentation/index.html">기존 PoC</a>')
        links.append(f'<a href="../../../{c["game_dir"]}/README.md">README</a>')
    cards.append(f"""<article class="card" data-engine="{engine['id']}" data-num="{c['num']}" data-built="{int(built)}" data-q="{html.escape((str(c['num'])+' '+c['title']+' '+c['oneline']+' '+c['genre']+' '+engine['label']).lower())}">
<div class="shots">{imgs}</div>
<div class="body"><div class="meta"><span>#{c['num']:02d}</span><span>{html.escape(c['genre'])}</span><span class="st st-{int(built)}">{status}</span></div>
<h2>{html.escape(c['title'])}</h2><p>{html.escape(c['oneline'])}</p>
<div class="links">{''.join(links)}</div></div></article>""")
    group_cards[engine['id']].append(cards[-1])

sections = ''.join(
    f'<section class="engine-group" data-engine="{g["id"]}" id="{g["id"]}">'
    f'<h2 class="group-title">{html.escape(g["label"])} <span class="group-count">{len(g["numbers"])}개</span></h2>'
    f'<p class="group-description">{html.escape(g["description"])}</p>'
    f'<div class="grid">{"".join(group_cards[g["id"]])}</div></section>' for g in groups)
engine_buttons = ''.join(
    f'<button data-engine="{g["id"]}" aria-pressed="false">{html.escape(g["label"])} · {len(g["numbers"])}</button>' for g in groups)

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
.engine-group{{margin:36px 0 48px}}.group-title{{font-size:28px}}.group-count{{font:14px sans-serif;color:var(--accent)}}
.group-description,#result{{color:var(--muted)}}[hidden]{{display:none!important}}
.meta,.links{{flex-wrap:wrap}}
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
<nav><a href="../index.html">← PoC 갤러리</a><a href="../../CONCEPTS.md">CONCEPTS.md</a><a href="../../ENGINE_FIT.md">엔진 분류·제작 공수</a><a href="_critique/index.html">비판 리뷰 전후 (3안)</a><a href="_market/index.html">상업 후보 5선</a></nav>
<header><span class="eyebrow">{len(concepts)} CONCEPTS · HTML MOCKUP + 2 SCREEN MOCKS EACH</span><h1>컨셉 시안 {len(concepts)}</h1>
<p>docs/CONCEPTS.md의 모든 컨셉을 같은 조건으로 놓았다. 카드마다 HTML 시안(화면 A/B, 키 1·2로 전환)과 AI 생성 화면 목업 2장.
<b>목업 이미지는 AI 생성 시안이며 실행 빌드 화면이 아니다.</b> 구현된 컨셉은 기존 PoC 링크도 함께 둔다.</p>
<p>3D 제작 공수와 시각 방향을 기준으로 <b>권장 구동 엔진</b>별로 묶었다. 현재 구현된 엔진을 뜻하지 않는다.</p></header>
<div class="bar"><input id="q" aria-label="번호·제목·장르·엔진 검색" placeholder="번호·제목·장르·엔진 검색"><button data-f="all" class="on" aria-pressed="true">전체 {len(concepts)}</button><button data-f="0" aria-pressed="false">미착수</button><button data-f="1" aria-pressed="false">구현됨</button></div>
<div class="bar" aria-label="권장 구동 엔진 필터"><button data-engine="all" class="on" aria-pressed="true">모든 엔진</button>{engine_buttons}</div>
<p id="result" role="status" aria-live="polite">{len(concepts)}개 표시</p>
<div id="grid">{sections}</div>
<p id="empty" hidden>조건에 맞는 컨셉이 없습니다. 검색어나 필터를 바꿔 보세요.</p>
<footer>생성: <code>python docs/poc-gallery/concepts/_tools/build_index.py</code> · 이미지: <code>_tools/gen_images.py</code> (Codex image_gen) · 누락 {len(missing)}건</footer>
</main><script>
var f='all',engine='all',q=document.getElementById('q'),cards=[].slice.call(document.querySelectorAll('.card'));
function apply(){{
  var s=q.value.trim().toLowerCase(),total=0;
  cards.forEach(function(c){{c.hidden=!((f==='all'||c.dataset.built===f)&&(engine==='all'||c.dataset.engine===engine)&&(!s||c.dataset.q.indexOf(s)>=0));if(!c.hidden)total++}});
  document.querySelectorAll('.engine-group').forEach(function(g){{var n=g.querySelectorAll('.card:not([hidden])').length;g.hidden=n===0;g.querySelector('.group-count').textContent=n+'개'}});
  document.getElementById('result').textContent=total+'개 표시 / 전체 '+cards.length+'개';document.getElementById('empty').hidden=total!==0;
}}
q.addEventListener('input',apply);
function bind(selector,key){{document.querySelectorAll(selector).forEach(function(b){{b.onclick=function(){{if(key==='f')f=b.dataset.f;else engine=b.dataset.engine;document.querySelectorAll(selector).forEach(function(x){{x.classList.toggle('on',x===b);x.setAttribute('aria-pressed',String(x===b))}});apply()}}}})}}
bind('button[data-f]','f');bind('button[data-engine]','engine');apply();
</script></body></html>"""
# .gitattributes 가 `* text=auto eol=lf` 라 작업트리도 LF 로 쓴다
# (write_text 는 윈도우에서 CRLF 를 넣어 한 줄만 고쳐도 파일 전체가 diff 로 잡힌다).
(ROOT / "index.html").write_bytes(page.replace("\r\n", "\n").encode("utf-8"))
print(f"{len(concepts)} cards, missing: {missing}")
