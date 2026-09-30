#!/usr/bin/env python3
"""비판 리뷰 전후 비교 페이지(docs/poc-gallery/concepts/_critique/index.html)를 만든다.

    python docs/poc-gallery/concepts/_tools/build_critique.py

입력: _critique/<NN>-<slug>/ 의 pitch-v1.md · critique.md · response.md · pitch-v2.md,
      shots/v1-{a,b}.jpg · shots/v2-{a,b}.jpg (HTML 시안 캡처),
      v1 목업 이미지(../<NN>-<slug>/mock-*.jpg) · v2/mock-*.jpg.
"""
import html
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
CRIT = ROOT / "_critique"
concepts = {f"{c['num']:02d}-{c['slug']}": c
            for c in json.loads((ROOT / "_tools" / "concepts.json").read_text(encoding="utf-8"))}


def inline(s):
    s = html.escape(s)
    s = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", s)
    s = re.sub(r"`(.+?)`", r"<code>\1</code>", s)
    s = re.sub(r"\[(.+?)\]\((https?://[^)]+)\)", r'<a href="\2">\1</a>', s)
    return s


def md(text):
    """제목·목록·표·문단만 다루는 작은 변환기."""
    out, lines, i = [], text.splitlines(), 0
    while i < len(lines):
        l = lines[i]
        if not l.strip():
            i += 1
            continue
        if m := re.match(r"(#{1,4}) (.*)", l):
            n = min(len(m.group(1)) + 2, 6)
            out.append(f"<h{n}>{inline(m.group(2))}</h{n}>")
            i += 1
        elif l.lstrip().startswith("|"):
            rows = []
            while i < len(lines) and lines[i].lstrip().startswith("|"):
                cells = [c.strip() for c in lines[i].strip().strip("|").split("|")]
                if not all(re.fullmatch(r":?-+:?", c) for c in cells if c):
                    rows.append(cells)
                i += 1
            head, *body = rows
            out.append("<table><tr>" + "".join(f"<th>{inline(c)}</th>" for c in head) + "</tr>"
                       + "".join("<tr>" + "".join(f"<td>{inline(c)}</td>" for c in r) + "</tr>" for r in body)
                       + "</table>")
        elif re.match(r"\s*([-*]|\d+\.) ", l):
            tag = "ol" if re.match(r"\s*\d+\.", l) else "ul"
            items = []
            while i < len(lines) and (re.match(r"\s*([-*]|\d+\.) ", lines[i]) or (lines[i].startswith("   ") and items)):
                if re.match(r"\s*([-*]|\d+\.) ", lines[i]):
                    items.append(re.sub(r"\s*([-*]|\d+\.) ", "", lines[i], count=1))
                else:
                    items[-1] += " " + lines[i].strip()
                i += 1
            out.append(f"<{tag}>" + "".join(f"<li>{inline(x)}</li>" for x in items) + f"</{tag}>")
        else:
            para = []
            while i < len(lines) and lines[i].strip() and not re.match(r"(#{1,4} |\s*\||\s*([-*]|\d+\.) )", lines[i]):
                para.append(lines[i].strip())
                i += 1
            out.append(f"<p>{inline(' '.join(para))}</p>")
    return "\n".join(out)


def read(p):
    return p.read_text(encoding="utf-8") if p.exists() else "_(없음)_"


def pair(label, v1, v2, link1, link2):
    def fig(src, cap, link):
        return (f'<figure><a href="{link}"><img src="{src}" loading="lazy" alt="{cap}"></a><figcaption>{cap}</figcaption></figure>'
                if src else f'<figure class="empty"><figcaption>{cap} — 없음</figcaption></figure>')
    return f'<div class="pair"><h4>{label}</h4><div class="row">{fig(v1[0], "v1 · " + v1[1], link1)}{fig(v2[0], "v2 · " + v2[1], link2)}</div></div>'


sections, nav = [], []
for d in sorted(p for p in CRIT.iterdir() if p.is_dir() and re.match(r"\d{2,3}-", p.name)):
    c = concepts.get(d.name, {"title": d.name, "num": 0})
    rel = d.name
    def ex(p):
        return p if (CRIT / p).exists() else None
    pairs = []
    for k in "ab":
        pairs.append(pair(f"HTML 시안 · 화면 {k.upper()}",
                          (ex(f"{rel}/shots/v1-{k}.jpg"), "HTML"), (ex(f"{rel}/shots/v2-{k}.jpg"), "HTML"),
                          f"../{rel}/index.html#{k}", f"{rel}/v2/index.html#{k}"))
    for k in "ab":
        v1img = f"../{rel}/mock-{k}.jpg" if (ROOT / rel / f"mock-{k}.jpg").exists() else None
        pairs.append(pair(f"목업 이미지 · {k.upper()}",
                          (v1img, "이미지"), (ex(f"{rel}/v2/mock-{k}.jpg"), "이미지"),
                          v1img or "#", f"{rel}/v2/mock-{k}.jpg"))
    nav.append(f'<a href="#{rel}">#{c["num"]} {html.escape(c["title"])}</a>')
    sections.append(f"""<section id="{rel}"><header><span class="eyebrow">#{c['num']} · {html.escape(c.get('genre',''))}</span>
<h2>{html.escape(c['title'])}</h2><div class="links"><a href="../{rel}/index.html">v1 HTML ↗</a><a href="{rel}/v2/index.html">v2 HTML ↗</a></div></header>
{''.join(pairs)}
<div class="docs">
<details open><summary>Codex 비판 (critique.md)</summary><div class="md">{md(read(d / 'critique.md'))}</div></details>
<details open><summary>수용 내역 (response.md)</summary><div class="md">{md(read(d / 'response.md'))}</div></details>
<details><summary>컨셉 v1</summary><div class="md">{md(read(d / 'pitch-v1.md'))}</div></details>
<details open><summary>컨셉 v2</summary><div class="md">{md(read(d / 'pitch-v2.md'))}</div></details>
</div></section>""")

page = f"""<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>비판 리뷰 전후</title><link rel="icon" href="data:,">
<style>
:root{{color-scheme:dark;--bg:#131514;--card:#1b1f1d;--ink:#e8e3d6;--muted:#9c9a92;--accent:#d2b97c;--line:#ffffff1c;--v2:#8fd0b0}}
*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--ink);font:14px/1.75 'Pretendard','Malgun Gothic','Noto Sans KR',sans-serif}}
a{{color:inherit}}main{{max-width:1480px;margin:auto;padding:28px 32px 80px}}
nav{{display:flex;gap:20px;flex-wrap:wrap;font-size:12px;color:var(--muted);border-bottom:1px solid var(--line);padding-bottom:16px}}
.eyebrow{{font:10px/1.5 Consolas,monospace;letter-spacing:2px;color:var(--accent)}}
h1{{font:40px/1.3 Batang,'Noto Serif KR',serif;margin:34px 0 8px}}.lead{{color:var(--muted);max-width:820px}}
section{{background:var(--card);border:1px solid var(--line);margin:34px 0;padding:24px 26px}}
section>header{{display:flex;align-items:baseline;gap:18px;flex-wrap:wrap;border-bottom:1px solid var(--line);padding-bottom:12px;margin-bottom:14px}}
h2{{font:28px/1.3 Batang,'Noto Serif KR',serif;margin:0}}.links{{display:flex;gap:14px;font-size:12px;margin-left:auto}}
.pair h4{{margin:14px 0 6px;font-size:12px;color:var(--muted);font-weight:500}}.row{{display:grid;grid-template-columns:1fr 1fr;gap:10px}}
figure{{margin:0;background:#000}}figure img{{width:100%;display:block;aspect-ratio:16/10;object-fit:cover}}
figcaption{{font:11px Consolas,monospace;padding:4px 8px;color:var(--muted)}}.row figure:last-child figcaption{{color:var(--v2)}}
figure.empty{{display:grid;place-items:center;aspect-ratio:16/10}}
.docs{{display:grid;grid-template-columns:1fr 1fr;gap:14px;margin-top:18px}}details{{background:#0f1110;border:1px solid var(--line);padding:10px 14px}}
summary{{cursor:pointer;color:var(--accent);font-size:13px}}.md{{font-size:13px}}.md h3,.md h4,.md h5{{font-size:14px;margin:14px 0 4px}}
.md table{{border-collapse:collapse;width:100%;font-size:12px}}.md td,.md th{{border:1px solid var(--line);padding:4px 6px;vertical-align:top;text-align:left}}
code{{font-size:12px;color:var(--accent)}}
@media(max-width:900px){{main{{padding:18px 16px}}.row,.docs{{grid-template-columns:1fr}}h1{{font-size:30px}}}}
</style></head><body><main>
<nav><a href="../index.html">← 컨셉 시안 목록</a>{''.join(nav)}</nav>
<span class="eyebrow" style="display:block;margin-top:30px">CRITIQUE ROUND · CODEX CRITIC → ACCEPT → V2</span>
<h1>비판 리뷰 전후</h1>
<p class="lead">구상안 3개를 골라 Codex 워커에게 비판적 리뷰어 역할을 맡기고(<code>critique.md</code>), 그 비판을 수용해 컨셉 규칙과 시안을 v2로 고쳤다(<code>response.md</code>, <code>pitch-v2.md</code>, <code>v2/</code>).
왼쪽이 v1, 오른쪽이 v2. v1 원본 폴더는 그대로 두었다. 목업 이미지는 AI 생성 시안이며 실행 빌드 화면이 아니다.</p>
{''.join(sections)}
</main></body></html>"""
(CRIT / "index.html").write_text(page, encoding="utf-8")
print("sections:", len(sections))
