/*
  화면. **수치는 하나도 여기서 고르지 않는다** — data/*.json 과 palette.json 에서 읽는다.
  규칙은 engine.js 가 갖고 있고, 그것이 C# 과 같은 수를 내는지는 tests/browser-parity.js 가 대조한다.

  소리가 없으므로 감각을 화면이 짊어진다. 여섯 가지로 나눠 싣는다:
    ① 음표 머리의 크기   ← 호흡 (누적 어긋남)
    ② 머리를 지나는 선의 기울기 ← 활 (벌어지는 속도)
    ③ 머리 위의 짧은 획   ← 표정 (세기 어긋남)
    ④ 흐린 잔상          ← 지난 몇 박의 궤적
    ⑤ 지휘봉 고리        ← 지금 보고 있는 무리
    ⑥ 신문 조각          ← 결과 (연주가 끝난 뒤에만)
*/
"use strict";
const $ = id => document.getElementById(id);
let data, P, game, selected = 0, lastReview = "";

/* palette.json → CSS 변수. 색을 코드에 적지 않는다 */
function applyPalette(p) {
  const r = document.documentElement.style, c = p.color, t = p.type, s = p.staff, g = p.clipping;
  const set = (k, v) => r.setProperty(k, v);
  for (const k of ["paper", "paperLit", "paperDim", "staff", "staffFaint", "barline",
                   "ink", "inkSoft", "inkGhost", "clip", "clipLit", "clipInk", "clipDot",
                   "pass", "fail", "baton"]) set("--" + k, c[k]);
  set("--lineGap", s.lineGapPx + "px"); set("--lineW", s.lineWidthPx + "px");
  set("--systemGap", s.systemGapPx + "px"); set("--barW", s.barlineWidthPx + "px");
  set("--cursorW", s.cursorWidthPx + "px"); set("--cursorOp", s.cursorOpacity / 100);
  set("--dotPx", g.halftoneDotPx + "px"); set("--dotPitch", g.halftonePitchPx + "px");
  set("--dotOp", g.halftoneOpacity / 100); set("--clipRot", g.rotateDeg + "deg");
  set("--foldOff", g.foldOffsetPx + "px"); set("--shadowBlur", g.shadowBlurPx + "px");
  set("--shadowOp", g.shadowOpacity / 100); set("--tapeW", g.tapeWidthPx + "px");
  set("--tapeOp", g.tapeOpacity / 100); set("--revealMs", g.revealMs + "ms");
  set("--clipMax", g.maxWidthCh + "ch");
  set("--sTitle", t.sizeTitle + "px"); set("--sMast", t.sizeMasthead + "px");
  set("--sHead", t.sizeHeading + "px"); set("--sBody", t.sizeBody + "px");
  set("--sHint", t.sizeHint + "px"); set("--sNum", t.sizeNumeral + "px");
  set("--sLabel", t.sizeGlyphLabel + "px"); set("--trkMast", t.trackingMasthead + "px");
  set("--vig", p.grade.vignette); set("--ease", p.motion.easing);
  set("--beatMs", p.motion.beatAdvanceMs + "ms"); set("--glyphMs", p.motion.glyphEaseMs + "ms");
}

const esc = s => String(s).replace(/[&<>"]/g, m => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[m]));

/* ── 음표 하나. 세 채널이 세 가지 모양이다 ─────────────────────────────── */
function glyph(i, breath, bow, face, bowReadable, playing, x, y, opacity, ringed) {
  const G = P.glyph, c = P.color;
  const rx = G.headRadiusBasePx + breath * G.headRadiusPerBreathPx;
  const ry = Math.max(3, Math.round(rx * G.headSquashPercent / 100));
  if (!playing)
    return `<ellipse cx="${x}" cy="${y}" rx="${G.headRadiusBasePx}" ry="${ry}" fill="none"
             stroke="${c.inkGhost}" stroke-dasharray="${G.restDashPx.join(" ")}" opacity=".7"/>`;

  let out = "";
  // ① 머리 — 호흡
  out += `<ellipse cx="${x}" cy="${y}" rx="${Math.max(3, rx)}" ry="${ry}" fill="${c.ink}" opacity="${opacity}"/>`;
  // ② 머리를 지나는 선 — 활 (없는 무리는 그리지 않는다)
  if (bowReadable) {
    const deg = bow * G.bowDegPerLevel, rad = deg * Math.PI / 180;
    const hx = Math.cos(rad) * G.bowLengthPx / 2, hy = Math.sin(rad) * G.bowLengthPx / 2;
    out += `<line x1="${(x - hx).toFixed(1)}" y1="${(y - hy).toFixed(1)}"
             x2="${(x + hx).toFixed(1)}" y2="${(y + hy).toFixed(1)}"
             stroke="${c.ink}" stroke-width="${G.bowWidthPx}" opacity="${opacity}"/>`;
  }
  // ③ 머리 위의 짧은 획 — 표정
  if (face !== 0) {
    const w = G.strokeWidthBasePx + Math.abs(face) * G.strokeWidthPerFacePx;
    const dir = face > 0 ? -1 : 1;                    // 넘치면 위로, 모자라면 아래로
    const y0 = y + dir * (ry + 3), y1 = y0 + dir * G.strokeLengthPx;
    out += `<line x1="${x}" y1="${y0}" x2="${x + (face > 0 ? 6 : -6)}" y2="${y1}"
             stroke="${c.ink}" stroke-width="${w}" stroke-linecap="round" opacity="${opacity}"/>`;
  }
  // ⑤ 지휘봉 고리
  if (ringed)
    out += `<circle cx="${x}" cy="${y}" r="${rx + P.glyph.batonRingPx + 3}" fill="none"
             stroke="${c.baton}" stroke-width="2"/>`;
  return out;
}

/* ── 오선지 전체 ───────────────────────────────────────────────────────── */
function drawStaff() {
  const svg = $("staff"), c = P.color, S = P.staff, G = P.glyph;
  const n = data.sections.length;
  const W = 1040, padL = 132, padR = 26;
  const rowH = 62, top = 30;
  const H = top + rowH * n + 26;
  svg.setAttribute("viewBox", `0 0 ${W} ${H}`);

  const beats = game.steps.length;
  const step = (W - padL - padR) / Math.max(1, beats);
  const xOf = k => padL + step * (k + 0.5);

  let out = "";
  // 마디선 · 박 눈금
  let acc = 0;
  for (const bar of game.piece.bars) {
    const x = padL + step * acc;
    out += `<line x1="${x.toFixed(1)}" y1="${top - 12}" x2="${x.toFixed(1)}" y2="${top + rowH * n}"
             stroke="${c.barline}" stroke-width="${S.barlineWidthPx}" opacity=".55"/>`;
    out += `<text x="${(x + 4).toFixed(1)}" y="${top - 16}" font-size="${P.type.sizeHint}"
             fill="${c.inkGhost}">${bar.index}</text>`;
    out += `<text x="${(x + 4).toFixed(1)}" y="${(top + rowH * n + 16).toFixed(1)}"
             font-size="${P.type.sizeHint}" fill="${c.inkGhost}">${bar.requiredDynamic}</text>`;
    acc += bar.beats;
  }
  out += `<line x1="${(padL + step * acc).toFixed(1)}" y1="${top - 12}"
           x2="${(padL + step * acc).toFixed(1)}" y2="${top + rowH * n}"
           stroke="${c.barline}" stroke-width="${S.barlineWidthPx * 2}" opacity=".7"/>`;

  // 무리마다 한 시스템(오선 다섯 줄)
  data.sections.forEach((sec, i) => {
    const mid = top + rowH * i + rowH / 2;
    for (let L = -2; L <= 2; L++)
      out += `<line x1="${padL}" y1="${(mid + L * S.lineGapPx).toFixed(1)}"
               x2="${W - padR}" y2="${(mid + L * S.lineGapPx).toFixed(1)}"
               stroke="${L === 0 ? c.staff : c.staffFaint}" stroke-width="${S.lineWidthPx}"/>`;
    out += `<text x="10" y="${(mid - 4).toFixed(1)}" font-size="${P.type.sizeNumeral}"
             fill="${c.ink}">${esc(sec.name)}</text>`;
    out += `<text x="10" y="${(mid + 12).toFixed(1)}" font-size="${P.type.sizeHint}"
             fill="${c.inkGhost}">${esc(sec.bowVisible ? "활 보임" : "활 없음")} · 숨 ${
               sec.breathQuantMsOverride > 0 ? sec.breathQuantMsOverride : data.balance.visible.breathQuantMs}ms</text>`;

    // ④ 잔상 — 지난 몇 박의 궤적
    const from = Math.max(0, game.index - G.trailBeats);
    for (let k = from; k < game.index; k++) {
      const rec = game.records[k];
      const age = game.index - k;
      const op = (G.trailOpacityStart / 100) * (1 - age / (G.trailBeats + 1));
      out += glyph(i, rec.Seen.Breath[i], rec.Seen.Bow[i], rec.Seen.Face[i],
                   rec.Seen.BowReadable[i], rec.Seen.Playing[i], xOf(k), mid, op.toFixed(3), false);
    }
    // 지금 박
    const seen = game.seen();
    if (seen)
      out += glyph(i, seen.Breath[i], seen.Bow[i], seen.Face[i], seen.BowReadable[i],
                   seen.Playing[i], xOf(game.index), mid, "1", selected === i);
  });

  // 박 커서
  if (!game.done) {
    const x = xOf(game.index);
    out += `<line x1="${x.toFixed(1)}" y1="${top - 8}" x2="${x.toFixed(1)}" y2="${top + rowH * n + 4}"
             stroke="${c.baton}" stroke-width="${S.cursorWidthPx}" opacity="${S.cursorOpacity / 100}"/>`;
  }
  svg.innerHTML = out;
}

/* ── 단원 고르기 칸 ───────────────────────────────────────────────────── */
function drawStage(seen) {
  $("stage").replaceChildren(...data.sections.map((sec, i) => {
    const b = document.createElement("button");
    b.className = "player" + (selected === i ? " selected" : "");
    b.disabled = !seen.Playing[i];
    b.setAttribute("aria-pressed", selected === i ? "true" : "false");
    // aria-label 은 kit/tools/verify_takeover.js 가 이름으로 집는다. 바꾸면 그 검증이 깨진다.
    b.setAttribute("aria-label", sec.name + " 선택");
    const breath = seen.Breath[i], bow = seen.Bow[i], face = seen.Face[i];
    b.innerHTML =
      `<small>${esc(sec.seat)} · ${esc(sec.instrument)}</small><h3>${esc(sec.name)}</h3>`
      + `<svg viewBox="0 0 200 84" aria-hidden="true">`
      + `<line x1="12" y1="42" x2="188" y2="42" stroke="${P.color.staffFaint}" stroke-width="1"/>`
      + glyph(i, breath, bow, face, seen.BowReadable[i], seen.Playing[i], 100, 42, "1", selected === i)
      + `</svg>`
      + `<div class="signals"><span>호흡 <b>${breath > 0 ? "+" : ""}${breath}</b></span>`
      + `<span>활 <b>${seen.BowReadable[i] ? (bow > 0 ? "+" : "") + bow : "—"}</b></span>`
      + `<span>표정 <b>${face > 0 ? "+" : ""}${face}</b></span></div>`;
    b.onclick = () => { selected = i; render(); };
    return b;
  }));
}

function render() {
  const seen = game.seen();
  $("review").hidden = !game.done;
  $("cues").querySelectorAll("button").forEach(b => b.disabled = game.done);
  drawStaff();

  if (game.done) {
    const r = game.review();
    $("headline").textContent = r.grade.headline;
    // 점수는 #reviewBody 에 둔다 — kit/tools/verify_takeover.js 가 여기서 "점" 을 찾는다.
    $("reviewBody").textContent = `${r.grade.body} — ${r.grade.name} ${r.total}점`
      + ` (정확 ${r.accuracy} · 합주 ${r.ensemble} · 세기 ${r.dynamics})`;
    $("verdict").innerHTML =
      `<span class="${r.passed ? "pass" : "fail"}">${r.passed ? "자리를 지켰다" : "낙방"}</span>`
      + ` · 합격선 ${data.balance.review.passTotal} · 이 지적이 다음 연주의 유일한 사전 지식이다`;
    $("notes").replaceChildren(...r.notes.map(t => {
      const p = document.createElement("p"); p.textContent = t; return p;
    }));
    lastReview = [r.grade.name + " " + r.total + "점", ...r.notes].join(" / ");
    $("status").textContent = "연주가 끝났습니다. 다음 날 아침 신문이 도착했습니다.";
    $("sheetLabel").textContent = "연주가 끝난 오선지";
    return;
  }

  $("sheetLabel").textContent = game.piece.name;
  $("bar").textContent = `${seen.Bar}소절 ${seen.Beat}박 · 악보가 요구하는 세기 ${seen.RequiredDynamic}`;
  let acc = 0;
  const marks = [];
  for (const bar of game.piece.bars) {
    for (let k = 0; k < bar.beats; k++, acc++)
      marks.push(`<i class="${acc < game.index ? "on" : ""}${k === 0 && acc > 0 ? " bar" : ""}"></i>`);
  }
  $("beats").innerHTML = marks.join("");
  drawStage(seen);
  $("target").textContent = data.sections[selected].name + "에 지휘를 보낸다";
}

function start() {
  const piece = data.score.pieces.find(p => p.id === $("piece").value);
  const entry = data.casts.find(c => c.pieceId === piece.id);
  game = new Concert(data, piece, entry.cast);
  selected = 0;
  $("status").textContent = "단원 무리를 고르고 동작을 누르면 한 박이 진행됩니다.";
  render();
}

(async () => {
  const names = ["sections", "score", "cues", "balance", "press", "browser-casts"];
  const values = await Promise.all(names.map(n =>
    fetch("../data/" + n + ".json").then(r => {
      if (!r.ok) throw Error("data/" + n + ".json 을 불러오지 못했습니다");
      return r.json();
    })));
  P = await (await fetch("palette.json")).json();
  applyPalette(P);
  data = {
    sections: values[0].sections, score: values[1], cues: values[2].cues,
    balance: values[3], press: values[4], casts: values[5]
  };

  $("piece").replaceChildren(...data.score.pieces.map(p => {
    const o = document.createElement("option");
    o.value = p.id; o.textContent = p.name;
    return o;
  }));
  $("cues").replaceChildren(...data.cues.map(c => {
    const b = document.createElement("button");
    b.textContent = c.name;
    if (c.dynamicNudge) b.className = "dyn";
    b.title = c.gesture + (c.tempoNudgeMs ? ` (${c.tempoNudgeMs > 0 ? "+" : ""}${c.tempoNudgeMs}ms)` : "")
              + (c.dynamicNudge ? ` (세기 ${c.dynamicNudge > 0 ? "+" : ""}${c.dynamicNudge})` : "");
    b.onclick = () => {
      game.step(selected, c.id);
      $("status").textContent = data.sections[selected].name + " · " + c.gesture;
      render();
    };
    return b;
  }));
  $("piece").onchange = start;
  $("reset").onclick = () => { lastReview = ""; $("previous").hidden = true; start(); };
  $("again").onclick = () => {
    $("previous").hidden = false;
    $("previous").textContent = "지난 평론: " + lastReview + " — 이 지적이 다음 연주의 유일한 사전 지식이다";
    start();
  };
  start();
  window.silentReady = true;
})().catch(e => {
  $("status").textContent = "불러오지 못했습니다: " + e.message
    + " · 저장소 뿌리에서 python -m http.server 8000 으로 열어 주세요 (fetch 가 file:// 에서 막힙니다).";
  console.error(e);
});
