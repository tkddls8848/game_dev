"""Generate a file://-safe gallery without touching other projects' gallery entries."""
from pathlib import Path
import json
from html import escape

ROOT=Path(__file__).resolve().parents[2]
GALLERY=ROOT/"docs/poc-gallery"
items=json.loads((GALLERY/"revised-concepts.json").read_text(encoding="utf-8"))
cards=[]
for i,c in enumerate(items):
    slug=c["slug"]
    cards.append(f'''<article class="card" id="{slug}">
      <a class="preview" href="png/{slug}.png"><img src="png/{slug}.png" alt="{escape(c['title'])} 실제 PoC 화면" loading="lazy" width="1600" height="1000"></a>
      <div class="card-body"><div class="eyebrow">STUDY {i+1:02} / {escape(c['english'])}</div>
      <h2>{escape(c['title'])}</h2><p>{escape(c['tagline'])}</p>
      <div class="links"><a class="play" href="../../games/{slug}/presentation/index.html">직접 플레이 ↗</a><a href="png/{slug}.png">PNG 원본</a><a href="../../games/{slug}/README.md">구현 범위</a></div>
      <details><summary>이 PoC에서 확인할 것</summary><p>{escape(c['question'])}</p></details></div></article>''')
html='''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>수정 컨셉 10종 — PoC 갤러리</title><link rel="icon" href="data:,">
<style>
:root{color-scheme:dark;--ink:#e6e1d5;--muted:#a09e96;--accent:#cbb787}*{box-sizing:border-box}body{margin:0;background:#161918;color:var(--ink);font:14px/1.8 'Malgun Gothic',sans-serif}a{color:inherit}main{max-width:1512px;margin:auto;padding:30px 35px 70px}nav{display:flex;gap:25px;flex-wrap:wrap;border-bottom:1px solid #ffffff20;padding-bottom:20px;font-size:12px;color:var(--muted)}header{padding:50px 0 35px;display:grid;grid-template-columns:1fr 360px;gap:30px}.eyebrow{font:10px/1.5 Consolas,monospace;letter-spacing:2px;color:var(--accent)}h1{font:46px/1.4 Batang,serif;letter-spacing:-2px;margin:12px 0}header p{color:var(--muted);max-width:650px}header aside{align-self:center;border-left:1px solid #ffffff25;padding-left:25px;font-size:12px;color:var(--muted)}code{font-size:11px;color:var(--ink);overflow-wrap:anywhere}.grid{display:grid;grid-template-columns:1fr 1fr;gap:28px}.card{background:#1d2220;border:1px solid #ffffff17;overflow:hidden}.preview{display:block;overflow:hidden;background:#252a27}.preview img{display:block;width:100%;height:auto;transition:transform .25s}.preview:hover img{transform:scale(1.025)}.card-body{padding:24px 26px}.card h2{font:27px/1.5 Batang,serif;margin:8px 0}.card p{color:var(--muted);font-size:12px;margin:0 0 18px}.links{display:flex;align-items:center;gap:20px;font-size:11px}.links a{text-decoration:none}.links a:hover{text-decoration:underline}.links .play{border:1px solid #cbb78770;padding:7px 12px;color:var(--accent)}details{margin-top:20px;color:var(--muted);font-size:11px}summary{cursor:pointer}details p{margin:10px 0!important}footer{margin-top:35px;padding-top:22px;border-top:1px solid #ffffff20;color:var(--muted);font-size:11px}.file-notice{border:1px solid #cbb78770;background:#cbb7870b;padding:14px 18px;margin-bottom:25px;font-size:12px}button{font:inherit}.hidden{display:none}@media(max-width:850px){main{padding:20px 18px 50px}header{grid-template-columns:1fr;padding:30px 0}header aside{padding-left:18px}h1{font-size:34px}.grid{grid-template-columns:1fr}.card-body{padding:20px}.links{gap:15px}.eyebrow{font-size:9px}}
</style></head><body><main><nav><a href="index.html">← 기존 PoC 갤러리</a><a href="shots.html">기존 PNG 모음</a><a href="revised-verification.json">브라우저 검증 기록</a></nav>
<header><div><span class="eyebrow">TEN CONCEPTS / TEN PLAYABLE STUDIES</span><h1>설정에서, 플레이로.</h1><p>수정한 게임 컨셉 10종의 작은 플레이 실험.<br>장부를 조사하고, 지도를 고치고, 문장을 지우며 선택의 결과를 확인합니다.</p></div><aside>각 화면은 실제 게임 데이터로 렌더한 1600×1000 캡처입니다. 완성된 상업 게임이 아닌 핵심 조작의 짧은 PoC이며, 재미는 직접 플레이하며 판단합니다.<br><br>로컬 실행:<br><code>python -m http.server 8000 --bind 127.0.0.1</code><br>저장소 루트에서 실행한 뒤<br><code>http://127.0.0.1:8000/docs/poc-gallery/revised.html</code></aside></header>
<div id="file-notice" class="file-notice hidden">PNG는 파일로 바로 볼 수 있습니다. <strong>직접 플레이</strong>는 위 명령으로 서버를 실행한 뒤 HTTP 주소로 열어 주세요.</div>
<section class="grid">'''+"\n".join(cards)+'''</section><footer>규칙: 순수 C# · 시나리오: JSON · 화면: HTML/CSS/SVG · 각 게임의 두 결말, 재시작, 데스크톱/태블릿/모바일 조작 검증.<br>기존 프로젝트는 수정안과 독립적으로 보존합니다. 무음 지휘 외에도 이 PoC 모음에는 음성·환경음 에셋이 포함되지 않습니다.</footer></main><script>if(location.protocol==='file:')document.getElementById('file-notice').classList.remove('hidden');</script></body></html>'''
(GALLERY/"revised.html").write_text(html,encoding="utf-8")
for name in ["index.html","shots.html"]:
    target=GALLERY/name
    text=target.read_text(encoding="utf-8")
    if 'id="revised-concepts-link"' not in text:
        text=text.replace('<header>','<header>\n  <p id="revised-concepts-link"><a href="revised.html"><strong>새 수정 컨셉 10종 · 플레이 가능한 PoC와 PNG 목업 →</strong></a></p>',1)
        target.write_text(text,encoding="utf-8")
print("Built docs/poc-gallery/revised.html; linked from existing galleries.")
