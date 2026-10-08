#!/usr/bin/env python3
"""활성 컨셉 전부를 한 장에 모은 열람용 색인(docs/poc-gallery/concepts/all.html)을 만든다.

    python docs/poc-gallery/concepts/_tools/build_all.py

`index.html`(build_index.py)과 다른 점은 두 가지다.
  1. docs/CONCEPTS.md 표의 상태·에셋·헤드리스·메모를 그대로 들고 온다.
     concepts.json 에는 그 네 열이 없다 — 번호·제목·한 줄·결까지만 있다.
  2. HTML 시안을 페이지를 떠나지 않고 iframe 으로 열어 본다.
     이미지 목업도 같은 자리에서 확대한다.

입력
  ../../CONCEPTS.md          `## 컨셉 <개수>` 표 (상태·에셋·헤드리스·메모의 원본)
  _tools/concepts.json       번호 -> slug·game_dir·batch
  <NN>-<slug>/               index.html · mock-a.jpg · mock-b.jpg 존재 여부
  ../png/<game>.png          구현된 컨셉의 실제 실행 스크린샷

출력은 상대 경로만 쓰므로 이 폴더째 옮겨도 열린다.
"""
import html
import json
import re
import sys
from pathlib import Path

# 윈도우 콘솔은 기본이 cp949 라 한글·기호 출력이 터진다.
for _s in (sys.stdout, sys.stderr):
    if hasattr(_s, 'reconfigure'):
        _s.reconfigure(encoding='utf-8', errors='replace')

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent                      # docs/poc-gallery/concepts
REPO = ROOT.parent.parent.parent         # repo root
CONCEPTS_MD = REPO / 'docs' / 'CONCEPTS.md'

# ── docs/CONCEPTS.md 의 `## 컨셉 <개수>` 표만 읽는다 ────────────────────────────
LINK_RE = re.compile(r'\[`?([^\]`]+)`?\]\([^)]*\)')      # [`slug`](path) -> slug
BOLD_RE = re.compile(r'\*\*([^*]+)\*\*')
# 절 제목에 개수가 들어 있다(`## 컨셉 101` -> `## 컨셉 102`). 숫자를 박지 않는다.
HEAD_RE = re.compile(r'^##\s+컨셉\s+\d+\s*$')


def strip_md(s):
    s = LINK_RE.sub(r'\1', s)
    s = BOLD_RE.sub(r'\1', s)
    return s.replace('`', '').strip()


def parse_table():
    lines = CONCEPTS_MD.read_text(encoding='utf-8').splitlines()
    try:
        start = next(i for i, l in enumerate(lines) if HEAD_RE.match(l))
    except StopIteration:
        raise SystemExit('CONCEPTS.md 에서 "## 컨셉 <개수>" 절을 찾지 못했다')

    rows = {}
    for line in lines[start + 1:]:
        if line.startswith('## '):
            break                                    # 다음 절에서 멈춘다
        if not line.startswith('|'):
            continue
        cells = [c.strip() for c in line.strip().strip('|').split('|')]
        if len(cells) != 8 or not cells[0].isdigit():
            continue                                 # 머리글·구분선·다른 표
        num = int(cells[0])
        rows[num] = dict(
            num=num,
            title=strip_md(cells[1]),
            oneline=strip_md(cells[2]),
            genre=strip_md(cells[3]),
            status=strip_md(cells[4]),
            asset=cells[5].strip() or '—',
            headless=cells[6].strip() or '—',
            note=strip_md(cells[7]),
        )
    return rows


table = parse_table()
meta = {c['num']: c for c in json.loads((HERE / 'concepts.json').read_text(encoding='utf-8'))
        if not c.get('archived')}                   # 보관된 안은 CONCEPTS.md 표에도 없다

missing_md = sorted(set(meta) - set(table))
missing_json = sorted(set(table) - set(meta))
if missing_md or missing_json:
    print(f'  주의: CONCEPTS.md 없음 {missing_md} · concepts.json 없음 {missing_json}')

# ── 컨셉별로 디스크를 확인해 합친다 ──────────────────────────────────────────
ASSET_LABEL = {'✅': '대부분 확보', '⚠️': '가공 필요', '❌': '제작 필요', '?': '미조사', '—': '해당 없음'}
HEADLESS_LABEL = {'◎': '순수 C# 검증 가능', '△': '일부', '✕': '감각·반사 지배', '—': '해당 없음'}

items, no_mock, no_html = [], [], []
for num in sorted(table):
    t = table[num]
    m = meta.get(num, {})
    slug = m.get('slug', '')
    folder = f'{num:02d}-{slug}' if slug else ''
    d = ROOT / folder if folder else None

    has_html = bool(d and (d / 'index.html').exists())
    shots = [f'{folder}/mock-{k}.jpg' for k in 'ab'
             if d and (d / f'mock-{k}.jpg').exists()]
    if not has_html:
        no_html.append(num)
    if len(shots) < 2:
        no_mock.append(num)

    game_dir = m.get('game_dir', '')
    built = bool(game_dir)
    game_slug = game_dir.rsplit('/', 1)[-1] if game_dir else ''
    # farm-erosion 등 다른 저장소로 간 것은 presentation 이 없다
    pres_file = REPO / game_dir / 'presentation' / 'index.html' if built else None
    pres = f'../../../{game_dir}/presentation/index.html' if pres_file and pres_file.exists() else ''
    # 목업이 fetch 로 데이터를 읽으면 file:// 에서 빈 화면이 된다 (README · POC_FACTORY.md §198)
    pres_needs_server = bool(
        pres and 'fetch(' in pres_file.read_text(encoding='utf-8', errors='replace'))
    readme = f'../../../{game_dir}/README.md' if built and (
        REPO / game_dir / 'README.md').exists() else ''
    shot = f'../png/{game_slug}.png' if built and (
        ROOT.parent / 'png' / f'{game_slug}.png').exists() else ''

    items.append(dict(
        **t, slug=slug, folder=folder, html=has_html, shots=shots,
        built=built, pres=pres, readme=readme, shot=shot,
        pres_needs_server=pres_needs_server,
        batch=m.get('batch', ''),
        group='구현' if built else '미착수',
    ))

n = len(items)
n_built = sum(1 for i in items if i['built'])
n_new = sum(1 for i in items if i['batch'])
n_shots = sum(len(i['shots']) for i in items)
n_html = sum(1 for i in items if i['html'])
n_pres = sum(1 for i in items if i['pres'])
n_server = sum(1 for i in items if i['pres_needs_server'])


def e(s):
    return html.escape(str(s), quote=True)


# ── 카드 ────────────────────────────────────────────────────────────────────
def card(i):
    thumbs = ''.join(
        f'<button class="th" data-img="{e(s)}" data-cap="{e(i["title"])} — 목업 {s[-5].upper()}">'
        f'<img src="{e(s)}" alt="{e(i["title"])} 목업 {s[-5].upper()}" loading="lazy" decoding="async"></button>'
        for s in i['shots'])
    if i['shot']:
        thumbs += (f'<button class="th real" data-img="{e(i["shot"])}" '
                   f'data-cap="{e(i["title"])} — 실행 화면(목업 아님)">'
                   f'<img src="{e(i["shot"])}" alt="{e(i["title"])} 실행 화면" loading="lazy" decoding="async">'
                   f'<span class="badge">실행</span></button>')
    if not thumbs:
        thumbs = '<div class="th empty">이미지 없음</div>'

    acts = []
    if i['html']:
        acts.append(f'<button class="act key" data-frame="{e(i["folder"])}/index.html" '
                    f'data-cap="{e(i["title"])}">시안 화면 보기</button>')
    if i['pres']:
        sup = ('<sup title="fetch 를 쓰므로 정적 서버가 필요하다">서버</sup>'
               if i['pres_needs_server'] else '')
        acts.append(f'<button class="act" data-frame="{e(i["pres"])}" '
                    f'data-srv="{int(i["pres_needs_server"])}" '
                    f'data-cap="{e(i["title"])} — 구현 PoC">구현 PoC{sup}</button>')
    if i['readme']:
        acts.append(f'<a class="act" href="{e(i["readme"])}">README ↗</a>')
    if i['html']:
        acts.append(f'<a class="act thin" href="{e(i["folder"])}/index.html" target="_blank">새 탭 ↗</a>')

    hay = ' '.join([i['title'], i['oneline'], i['genre'], i['note'], i['status'], str(i['num'])]).lower()
    return f'''<article class="card" data-g="{e(i['group'])}" data-a="{e(i['asset'])}" data-h="{e(i['headless'])}" data-q="{e(hay)}">
<div class="thumbs">{thumbs}</div>
<div class="cbody">
<div class="crow"><span class="num">#{i['num']:03d}</span><span class="genre">{e(i['genre'])}</span></div>
<h2>{e(i['title'])}</h2>
<p class="one">{e(i['oneline'])}</p>
<dl class="facts">
<dt>상태</dt><dd class="{'st-built' if i['built'] else 'st-todo'}">{e(i['status'])}</dd>
<dt>에셋</dt><dd><b>{e(i['asset'])}</b> <span class="lbl">{e(ASSET_LABEL.get(i['asset'], ''))}</span></dd>
<dt>헤드리스</dt><dd><b>{e(i['headless'])}</b> <span class="lbl">{e(HEADLESS_LABEL.get(i['headless'], ''))}</span></dd>
</dl>
<p class="note">{e(i['note']) or '—'}</p>
<div class="acts">{''.join(acts)}</div>
</div></article>'''


# ── 표 ─────────────────────────────────────────────────────────────────────
def row(i):
    link = (f'<button class="tlink" data-frame="{e(i["folder"])}/index.html" '
            f'data-cap="{e(i["title"])}">시안</button>') if i['html'] else '<span class="dash">—</span>'
    hay = ' '.join([i['title'], i['oneline'], i['genre'], i['note'], i['status'], str(i['num'])]).lower()
    return (f'<tr data-g="{e(i["group"])}" data-a="{e(i["asset"])}" data-h="{e(i["headless"])}" data-q="{e(hay)}">'
            f'<td class="n">{i["num"]:03d}</td><td class="ti">{e(i["title"])}</td>'
            f'<td class="on">{e(i["oneline"])}</td><td class="ge">{e(i["genre"])}</td>'
            f'<td class="{"st-built" if i["built"] else "st-todo"}">{e(i["status"])}</td>'
            f'<td class="c">{e(i["asset"])}</td><td class="c">{e(i["headless"])}</td>'
            f'<td class="no">{e(i["note"])}</td><td class="c">{link}</td></tr>')


cards = '\n'.join(card(i) for i in items)
rows_html = '\n'.join(row(i) for i in items)

CSS = """
:root{color-scheme:dark;--bg:#141615;--card:#1c201e;--card2:#232825;--ink:#e8e3d6;--muted:#9c9a92;
--accent:#d2b97c;--cool:#9bb7c9;--line:#ffffff1c;--line2:#ffffff2e}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--ink);
font:14px/1.7 Pretendard,'Malgun Gothic','Noto Sans KR',sans-serif;-webkit-text-size-adjust:100%}
a{color:inherit}
main{max-width:1680px;margin:auto;padding:24px 28px 80px}
nav{display:flex;gap:20px;flex-wrap:wrap;font-size:12px;color:var(--muted);
border-bottom:1px solid var(--line);padding-bottom:16px}
nav a:hover{color:var(--accent)}
header{padding:40px 0 22px}
.eyebrow{font:10px/1.5 Consolas,monospace;letter-spacing:2px;color:var(--accent)}
h1{font:clamp(30px,5vw,46px)/1.25 Batang,'Noto Serif KR',serif;letter-spacing:-1.5px;margin:10px 0}
header p{color:var(--muted);max-width:820px;margin:0 0 18px}
.stats{display:flex;gap:26px;flex-wrap:wrap;font:11px Consolas,monospace;color:var(--muted);
border-top:1px solid var(--line);padding-top:16px}
.stats b{color:var(--ink);font-size:17px;font-family:Batang,serif;display:block}
.serve{color:var(--muted);font-size:12px;max-width:900px;margin:16px 0 0;
border-left:2px solid var(--line2);padding:2px 0 2px 13px}
.serve code{color:var(--cool);font:11.5px Consolas,monospace}
.serve b{color:var(--ink)}
.act sup{color:var(--cool);font-size:9px;margin-left:3px;letter-spacing:.3px}
.warn{color:#e0a86a}
.bar{display:flex;gap:8px;flex-wrap:wrap;align-items:center;margin:22px 0 10px;
position:sticky;top:0;background:var(--bg);padding:12px 0;z-index:20;border-bottom:1px solid var(--line)}
.bar input{flex:1;min-width:180px;background:#0f1110;border:1px solid var(--line2);
color:var(--ink);padding:9px 12px;font:inherit;border-radius:2px}
.bar input:focus{outline:1px solid var(--accent);border-color:var(--accent)}
.seg{display:flex;border:1px solid var(--line2);border-radius:2px;overflow:hidden}
.seg button{background:none;border:0;border-right:1px solid var(--line2);color:var(--muted);
padding:9px 13px;font:inherit;cursor:pointer;white-space:nowrap}
.seg button:last-child{border-right:0}
.seg button:hover{color:var(--ink);background:#ffffff0d}
.seg button.on{color:#141615;background:var(--accent);font-weight:600}
.hint{font:11px Consolas,monospace;color:var(--muted);margin:0 0 18px}
#count{color:var(--accent)}

.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(430px,1fr));gap:20px}
.card{background:var(--card);border:1px solid var(--line);border-radius:3px;
display:flex;flex-direction:column;overflow:hidden}
.card:hover{border-color:var(--line2)}
.thumbs{display:grid;grid-template-columns:repeat(auto-fit,minmax(120px,1fr));gap:2px;background:#000}
.th{position:relative;display:block;aspect-ratio:16/10;overflow:hidden;background:#222;
border:0;padding:0;cursor:zoom-in}
.th img{width:100%;height:100%;object-fit:cover;display:block;transition:transform .3s,opacity .2s}
.th:hover img{transform:scale(1.05)}
.th .badge{position:absolute;left:6px;bottom:6px;background:#141615d9;color:var(--cool);
font:9px/1 Consolas,monospace;padding:4px 6px;letter-spacing:.5px;border:1px solid var(--line2)}
.th.empty{grid-column:1/-1;display:grid;place-items:center;color:var(--muted);font-size:12px;aspect-ratio:auto;padding:28px}
.cbody{padding:16px 18px 18px;display:flex;flex-direction:column;flex:1}
.crow{display:flex;gap:10px;align-items:baseline;font:11px Consolas,monospace;color:var(--muted)}
.num{color:var(--accent)}
h2{font:21px/1.35 Batang,'Noto Serif KR',serif;margin:5px 0 6px}
.one{color:var(--ink);font-size:13px;margin:0 0 12px}
.facts{display:grid;grid-template-columns:auto 1fr;gap:3px 12px;margin:0 0 10px;
font-size:12px;align-items:baseline}
.facts dt{color:var(--muted);font:11px Consolas,monospace}
.facts dd{margin:0}
.facts .lbl{color:var(--muted);font-size:11px}
.st-todo{color:var(--cool)}
.st-built{color:var(--accent)}
.note{color:var(--muted);font-size:12px;margin:0 0 14px;flex:1;
padding-top:9px;border-top:1px dashed var(--line)}
.acts{display:flex;gap:8px;flex-wrap:wrap;align-items:center}
.act{background:none;border:1px solid var(--line2);color:var(--muted);padding:6px 11px;
font:12px inherit;cursor:pointer;text-decoration:none;border-radius:2px}
.act:hover{color:var(--ink);border-color:var(--ink)}
.act.key{border-color:#d2b97c7a;color:var(--accent)}
.act.key:hover{background:var(--accent);color:#141615;border-color:var(--accent)}
.act.thin{border:0;padding:6px 2px;font-size:11px}

.tablewrap{overflow-x:auto;border:1px solid var(--line);border-radius:3px}
table{border-collapse:collapse;width:100%;font-size:12.5px;min-width:1100px}
th,td{text-align:left;padding:9px 11px;border-bottom:1px solid var(--line);vertical-align:top}
thead th{position:sticky;top:61px;background:var(--card2);color:var(--muted);
font:11px Consolas,monospace;letter-spacing:.5px;z-index:10;white-space:nowrap}
tbody tr:hover{background:#ffffff0a}
td.n{font:11px Consolas,monospace;color:var(--accent);white-space:nowrap}
td.ti{font-weight:600;min-width:130px}
td.on{color:var(--muted);min-width:230px}
td.ge{color:var(--muted);font-size:11.5px;white-space:nowrap}
td.no{color:var(--muted);font-size:11.5px;min-width:200px}
td.c{text-align:center;white-space:nowrap}
.dash{color:#5e5c57}
.tlink{background:none;border:1px solid #d2b97c7a;color:var(--accent);
padding:3px 9px;font:11px inherit;cursor:pointer;border-radius:2px}
.tlink:hover{background:var(--accent);color:#141615}
.hide{display:none!important}
.empty-msg{padding:60px 0;text-align:center;color:var(--muted)}

/* 시안 화면 — 페이지를 떠나지 않고 본다 */
#ov{position:fixed;inset:0;background:#0b0c0be8;backdrop-filter:blur(3px);
display:none;flex-direction:column;z-index:100}
#ov.on{display:flex}
.ovbar{display:flex;gap:14px;align-items:center;padding:11px 16px;
background:#141615;border-bottom:1px solid var(--line2);flex-wrap:wrap}
.ovcap{font:12px Consolas,monospace;color:var(--accent);flex:1;min-width:120px;
overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.ovbar button,.ovbar a{background:none;border:1px solid var(--line2);color:var(--muted);
padding:6px 12px;font:12px inherit;cursor:pointer;text-decoration:none;border-radius:2px}
.ovbar button:hover,.ovbar a:hover{color:var(--ink);border-color:var(--ink)}
.ovbody{flex:1;display:grid;place-items:center;overflow:auto;padding:16px}
#ovframe{width:100%;height:100%;border:1px solid var(--line2);background:#fff;display:none}
#ovframe.on{display:block}
#ovimg{max-width:100%;max-height:100%;object-fit:contain;display:none;border:1px solid var(--line2)}
#ovimg.on{display:block}
.ovnote{font:11px Consolas,monospace;color:var(--muted);padding:0 16px 12px;text-align:center}

@media(max-width:700px){
main{padding:16px 14px 60px}
.grid{grid-template-columns:1fr}
.bar{position:static}
thead th{top:0}
.stats{gap:18px}
}
"""

JS = """
var state={view:'card',g:'all',a:'all',h:'all',q:''};
var cards=[].slice.call(document.querySelectorAll('.card'));
var rows=[].slice.call(document.querySelectorAll('tbody tr'));
var countEl=document.getElementById('count');
var emptyEl=document.getElementById('empty');

function match(el){
  if(state.g!=='all'&&el.dataset.g!==state.g)return false;
  if(state.a!=='all'&&el.dataset.a!==state.a)return false;
  if(state.h!=='all'&&el.dataset.h!==state.h)return false;
  if(state.q&&el.dataset.q.indexOf(state.q)<0)return false;
  return true;
}
function apply(){
  var n=0;
  cards.forEach(function(c){var m=match(c);c.classList.toggle('hide',!m);if(m)n++;});
  rows.forEach(function(r){r.classList.toggle('hide',!match(r));});
  countEl.textContent=n;
  emptyEl.classList.toggle('hide',n>0);
}
document.getElementById('q').addEventListener('input',function(){
  state.q=this.value.trim().toLowerCase();apply();
});
[].forEach.call(document.querySelectorAll('.seg'),function(seg){
  seg.addEventListener('click',function(ev){
    var b=ev.target.closest('button');if(!b)return;
    var k=seg.dataset.key;
    [].forEach.call(seg.querySelectorAll('button'),function(x){x.classList.toggle('on',x===b);});
    if(k==='view'){
      state.view=b.dataset.v;
      document.getElementById('cards').classList.toggle('hide',state.view!=='card');
      document.getElementById('table').classList.toggle('hide',state.view!=='table');
    }else{state[k]=b.dataset.v;}
    apply();
  });
});

// 오버레이: 이미지는 확대, HTML 시안은 iframe
var ov=document.getElementById('ov'),ovf=document.getElementById('ovframe'),
    ovi=document.getElementById('ovimg'),ovc=document.getElementById('ovcap'),
    ovo=document.getElementById('ovopen'),ovn=document.getElementById('ovnote');
var fileProto=location.protocol==='file:';
function open_(cap,url,isFrame,needsServer){
  ovc.textContent=cap;ovo.href=url;
  ovf.classList.toggle('on',isFrame);ovi.classList.toggle('on',!isFrame);
  if(isFrame){
    ovi.removeAttribute('src');ovf.src=url;
    if(needsServer&&fileProto){
      ovn.innerHTML='<b class="warn">이 목업은 fetch 로 데이터를 읽어서 file:// 에서는 빈 화면이 나온다.</b> '+
        '저장소 뿌리에서 <code>python -m http.server</code> 를 띄우고 http:// 로 다시 열면 보인다.';
    }else{
      ovn.textContent='시안 안에서 키 1·2 로 화면 A/B 를 바꾼다 — 먼저 시안을 한 번 클릭해 포커스를 준다.';
    }
  }else{
    ovf.removeAttribute('src');ovi.src=url;
    ovn.textContent='AI 생성 목업이다. 실행 빌드 화면이 아니다.';
  }
  ov.classList.add('on');document.body.style.overflow='hidden';
}
function close_(){
  ov.classList.remove('on');document.body.style.overflow='';
  ovf.removeAttribute('src');ovi.removeAttribute('src');
}
document.addEventListener('click',function(ev){
  var t=ev.target.closest('[data-frame]');
  if(t){ev.preventDefault();open_(t.dataset.cap,t.dataset.frame,true,t.dataset.srv==='1');return;}
  var i=ev.target.closest('[data-img]');
  if(i){ev.preventDefault();open_(i.dataset.cap,i.dataset.img,false,false);}
});
document.getElementById('ovclose').addEventListener('click',close_);
ov.addEventListener('click',function(ev){if(ev.target===ov||ev.target.classList.contains('ovbody'))close_();});
document.addEventListener('keydown',function(ev){if(ev.key==='Escape'&&ov.classList.contains('on'))close_();});
apply();
"""

page = f'''<!doctype html>
<html lang="ko"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>컨셉 시안 {n} — 전체 정리</title>
<link rel="icon" href="data:,">
<style>{CSS}</style></head>
<body><main>
<nav>
<a href="index.html">← 컨셉 시안 색인</a>
<a href="../index.html">PoC 갤러리</a>
<a href="../../CONCEPTS.md">CONCEPTS.md</a>
<a href="_market/index.html">상업 후보 5선</a>
<a href="_critique/index.html">비판 리뷰 전후</a>
</nav>

<header>
<span class="eyebrow">{n} CONCEPTS · ONE PAGE · MOCKUP VIEWER INLINE</span>
<h1>컨셉 시안 {n} — 전체 정리</h1>
<p><code>docs/CONCEPTS.md</code> 의 컨셉 {n}개를 한 장에 모았다. 그 표의
<b>상태 · 에셋 · 헤드리스 · 메모</b>를 그대로 들고 왔고, <b>시안 화면은 이 페이지를 떠나지 않고</b>
열어 본다 — 카드의 <b>시안 화면 보기</b>는 그 컨셉의 HTML 시안을 띄우고, 썸네일은 목업을 확대한다.
<b>목업 이미지는 AI 생성 시안이지 실행 빌드 화면이 아니다</b>(실행 화면에는 <b>실행</b> 표시를 달았다).
검사기를 통과한 {n_built}개도 <b>재미 판정은 아직 아무도 하지 않았다</b> — 뿌리 CLAUDE.md 원칙 7.</p>
<div class="stats">
<span><b>{n}</b>컨셉</span>
<span><b>{n - n_built}</b>미착수</span>
<span><b>{n_built}</b>구현 · 검사기 통과</span>
<span><b>{n_new}</b>2026-09-29 추가</span>
<span><b>{n_html}</b>HTML 시안</span>
<span><b>{n_shots}</b>목업 이미지</span>
</div>
<p class="serve"><b>이 파일은 그냥 두 번 눌러 열어도 된다</b> — 컨셉 시안 {n_html}개는 전부
자족형이라 <code>file://</code> 에서 그대로 보인다. 다만 <b>구현 PoC {n_pres}개 중 {n_server}개</b>는
<code>fetch</code> 로 데이터를 읽어 <code>file://</code> 에서 빈 화면이 된다(<b>서버</b> 표시를 달아 두었다).
그것까지 보려면 저장소 뿌리에서 <code>python -m http.server</code> 를 띄우고
<code>localhost:8000/docs/poc-gallery/concepts/all.html</code> 로 연다.</p>
</header>

<div class="bar">
<input id="q" placeholder="제목 · 한 줄 · 결 · 메모 · 번호 검색" autocomplete="off">
<div class="seg" data-key="view"><button data-v="card" class="on">카드</button><button data-v="table">표</button></div>
<div class="seg" data-key="g"><button data-v="all" class="on">전체</button><button data-v="미착수">미착수</button><button data-v="구현">구현</button></div>
<div class="seg" data-key="a"><button data-v="all" class="on">에셋 전체</button><button data-v="✅">✅ 확보</button><button data-v="⚠️">⚠️ 가공</button><button data-v="?">? 미조사</button></div>
<div class="seg" data-key="h"><button data-v="all" class="on">헤드리스 전체</button><button data-v="◎">◎ 가능</button><button data-v="△">△ 일부</button><button data-v="✕">✕ 불가</button></div>
</div>
<p class="hint"><span id="count">{n}</span>개 표시 · 에셋 ✅ 대부분 확보 / ⚠️ 가공 필요 / ❌ 제작 필요 / ? 미조사 ·
헤드리스 ◎ 순수 C# 검증 가능 / △ 일부 / ✕ 감각·반사 지배 · Esc 로 시안 닫기</p>

<section class="grid" id="cards">
{cards}
</section>

<div class="tablewrap hide" id="table">
<table><thead><tr>
<th>#</th><th>컨셉</th><th>한 줄</th><th>결</th><th>상태</th><th>에셋</th><th>헤드리스</th><th>메모</th><th>시안</th>
</tr></thead><tbody>
{rows_html}
</tbody></table>
</div>

<div class="empty-msg hide" id="empty">조건에 맞는 컨셉이 없다.</div>

<footer class="hint" style="margin-top:38px;padding-top:18px;border-top:1px solid var(--line)">
생성: <code>python docs/poc-gallery/concepts/_tools/build_all.py</code> ·
원본 표: <code>docs/CONCEPTS.md</code> · 시안 HTML·목업: <code>docs/poc-gallery/concepts/&lt;NN&gt;-&lt;slug&gt;/</code>
{'· HTML 시안 없음 ' + str(len(no_html)) + '건' if no_html else ''}
{'· 목업 2장 미달 ' + str(len(no_mock)) + '건' if no_mock else ''}
</footer>
</main>

<div id="ov">
<div class="ovbar">
<span class="ovcap" id="ovcap"></span>
<a id="ovopen" href="#" target="_blank">새 탭 ↗</a>
<button id="ovclose">닫기 (Esc)</button>
</div>
<div class="ovbody"><iframe id="ovframe" title="시안 화면"></iframe><img id="ovimg" alt=""></div>
<p class="ovnote" id="ovnote"></p>
</div>

<script>{JS}</script>
</body></html>'''

out = ROOT / 'all.html'
# .gitattributes 가 `* text=auto eol=lf` 라 작업트리도 LF 로 쓴다
# (write_text 는 윈도우에서 CRLF 를 넣어 파일 전체가 diff 로 잡힌다).
out.write_bytes(page.replace('\r\n', '\n').encode('utf-8'))
print(f'{out.relative_to(REPO)} — {n} concepts ({n_built} built, {n_new} new), '
      f'{n_html} html mockups, {n_shots} mock images, '
      f'{n_pres} presentations ({n_server} need a static server)')
if no_html:
    print(f'  HTML 시안 없음: {no_html}')
if no_mock:
    print(f'  목업 2장 미달: {no_mock}')
