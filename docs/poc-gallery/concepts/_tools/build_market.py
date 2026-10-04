#!/usr/bin/env python3
"""상업 후보 5선 비교 페이지(docs/poc-gallery/concepts/_market/index.html)를 만든다.

    python docs/poc-gallery/concepts/_tools/build_market.py

입력: _market/README.md(선정 근거), _market/<NN>-<slug>/pitch.md,
      shots/v2-{a,b}.jpg (HTML 시안 캡처), v1 목업(../<NN>-<slug>/mock-*.jpg), v2/mock-*.jpg.
"""
import html
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MKT = ROOT / "_market"
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



def fig(src, cap, link):
    return (f'<figure><a href="{link}"><img src="{src}" loading="lazy" alt="{cap}"></a><figcaption>{cap}</figcaption></figure>'
            if src else f'<figure class="empty"><figcaption>{cap} — 없음</figcaption></figure>')


sections, nav = [], []
for d in sorted((p for p in MKT.iterdir() if p.is_dir() and re.match(r"\d{2,3}-", p.name)), key=lambda p: int(p.name.split("-")[0])):
    c = concepts.get(d.name, {"title": d.name, "num": 0})
    rel = d.name
    def ex(p):
        return p if (MKT / p).exists() else None
    rows = []
    for k in "ab":
        v1img = f"../{rel}/mock-{k}.jpg" if (ROOT / rel / f"mock-{k}.jpg").exists() else None
        rows.append(f'<div class="pair"><h4>화면 {k.upper()}</h4><div class="row3">'
                    + fig(v1img, "v1 · 이미지", v1img or "#")
                    + fig(ex(f"{rel}/v2/mock-{k}.jpg"), "상업판 · 이미지", f"{rel}/v2/mock-{k}.jpg")
                    + fig(ex(f"{rel}/shots/v2-{k}.jpg"), "상업판 · HTML", f"{rel}/v2/index.html#{k}")
                    + "</div></div>")
    nav.append(f'<a href="#{rel}">#{c["num"]} {html.escape(c["title"])}</a>')
    sections.append(f"""<section id="{rel}"><header><span class="eyebrow">#{c['num']} · {html.escape(c.get('genre',''))}</span>
<h2>{html.escape(c['title'])}</h2><div class="links"><a href="../{rel}/index.html">v1 HTML ↗</a><a href="{rel}/v2/index.html">상업판 HTML ↗</a><a href="{rel}/pitch.md">pitch.md ↗</a></div></header>
{''.join(rows)}
<details open><summary>상업판 컨셉 (pitch.md)</summary><div class="md">{md(read(d / 'pitch.md'))}</div></details>
</section>""")

page = f"""<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>상업 후보 5선</title><link rel="icon" href="data:,">
<style>
:root{{color-scheme:dark;--bg:#131514;--card:#1b1f1d;--ink:#e8e3d6;--muted:#9c9a92;--accent:#d2b97c;--line:#ffffff1c;--v2:#8fd0b0}}
*{{box-sizing:border-box}}body{{margin:0;background:var(--bg);color:var(--ink);font:14px/1.75 'Pretendard','Malgun Gothic','Noto Sans KR',sans-serif}}
a{{color:inherit}}main{{max-width:1480px;margin:auto;padding:28px 32px 80px}}
nav{{display:flex;gap:20px;flex-wrap:wrap;font-size:12px;color:var(--muted);border-bottom:1px solid var(--line);padding-bottom:16px}}
.eyebrow{{font:10px/1.5 Consolas,monospace;letter-spacing:2px;color:var(--accent)}}
h1{{font:40px/1.3 Batang,'Noto Serif KR',serif;margin:34px 0 8px}}.lead{{color:var(--muted);max-width:900px}}
.lead table{{color:var(--ink)}}
section{{background:var(--card);border:1px solid var(--line);margin:34px 0;padding:24px 26px}}
section>header{{display:flex;align-items:baseline;gap:18px;flex-wrap:wrap;border-bottom:1px solid var(--line);padding-bottom:12px;margin-bottom:14px}}
h2{{font:28px/1.3 Batang,'Noto Serif KR',serif;margin:0}}.links{{display:flex;gap:14px;font-size:12px;margin-left:auto}}
.pair h4{{margin:14px 0 6px;font-size:12px;color:var(--muted);font-weight:500}}.row3{{display:grid;grid-template-columns:1fr 1fr 1fr;gap:10px}}
figure{{margin:0;background:#000}}figure img{{width:100%;display:block;aspect-ratio:16/10;object-fit:cover}}
figcaption{{font:11px Consolas,monospace;padding:4px 8px;color:var(--muted)}}.row3 figure:not(:first-child) figcaption{{color:var(--v2)}}
figure.empty{{display:grid;place-items:center;aspect-ratio:16/10}}
details{{background:#0f1110;border:1px solid var(--line);padding:10px 14px;margin-top:18px}}
summary{{cursor:pointer;color:var(--accent);font-size:13px}}.md{{font-size:13px;max-width:1100px}}.md h3,.md h4,.md h5{{font-size:14px;margin:14px 0 4px}}
.md table{{border-collapse:collapse;width:100%;font-size:12px}}.md td,.md th{{border:1px solid var(--line);padding:4px 6px;vertical-align:top;text-align:left}}
code{{font-size:12px;color:var(--accent)}}
@media(max-width:900px){{main{{padding:18px 16px}}.row3{{grid-template-columns:1fr}}h1{{font-size:30px}}}}
</style></head><body><main>
<nav><a href="../index.html">← 컨셉 시안 목록</a>{''.join(nav)}</nav>
<span class="eyebrow" style="display:block;margin-top:30px">MARKET PICKS · 2026-10-02</span>
<h1>상업 후보 5선</h1>
<div class="lead md">{md(read(MKT / 'README.md').split(chr(10), 1)[1])}</div>
{''.join(sections)}
</main></body></html>"""
(MKT / "index.html").write_text(page, encoding="utf-8")
print("sections:", len(sections))
