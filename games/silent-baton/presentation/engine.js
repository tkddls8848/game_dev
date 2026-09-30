/*
  브라우저용 규칙 어댑터 — src/Performance.cs · VisibleFrame.cs · Review.cs 를 그대로 옮긴 것.

  **이 파일이 C# 과 갈라지면 화면이 조용히 거짓이 된다.** 그래서 대조 하네스가 있다:
      C:/Users/PSI/.dotnet/dotnet.exe test games/silent-baton/tests/silent-baton.Tests.csproj
      node games/silent-baton/tests/browser-parity.js
  앞의 것이 붙박이(연주 전체의 박마다의 보이는 프레임·누적 상태·평점)를 내보내고,
  뒤의 것이 이 파일을 박 단위로 견준다. 한 박이라도 다르면 즉시 실패한다.

  정수 규율(설계 원칙 4)도 그대로 옮겼다 — 나눗셈은 전부 0 쪽으로 버리고 나머지는 carry 에 누적한다.
*/
(function (root) {
  "use strict";

  const trunc = x => Math.trunc(x) + 0;
  const clamp = (x, a, b) => Math.max(a, Math.min(b, x)) + 0;

  /** C# Ratio.Quantize — 부호에 따라 갈라지지 않게 대칭으로 둔다. */
  const quantize = (v, step) =>
    step <= 0 ? v : (v >= 0 ? trunc((v + trunc(step / 2)) / step) : -trunc((-v + trunc(step / 2)) / step));

  /** 무리별 눈금 덮어쓰기. -1 이면 공통 눈금 (값 없는 int 는 -1). */
  const breathQuant = (d, i) => d.sections[i].breathQuantMsOverride > 0
    ? d.sections[i].breathQuantMsOverride : d.balance.visible.breathQuantMs;
  const bowQuant = (d, i) => d.sections[i].bowQuantMsOverride > 0
    ? d.sections[i].bowQuantMsOverride : d.balance.visible.bowQuantMs;
  const faceQuant = (d, i) => d.sections[i].faceQuantLevelOverride > 0
    ? d.sections[i].faceQuantLevelOverride : d.balance.visible.faceQuantLevel;

  class Concert {
    constructor(data, piece, cast) {
      this.d = data;
      this.piece = piece;
      this.p = structuredClone(cast);
      this.steps = piece.bars.flatMap(bar =>
        Array.from({ length: bar.beats }, (_, i) => ({ bar, beat: i + 1 })));
      this.index = 0;
      this.records = [];
      this.prepare();
    }

    /** 소절이 시작될 때 그 소절에 들어오는 무리를 세운다. */
    prepare() {
      if (this.done) return;
      const { bar, beat } = this.steps[this.index];
      if (beat === 1)
        for (const id of bar.enteringSectionIds || [])
          this.p.Playing[this.d.sections.findIndex(s => s.id === id)] = true;
    }

    get done() { return this.index >= this.steps.length; }

    /** 지금 이 박에 지휘대에서 **보이는 것 전부.** 여기 없는 값은 알 수 없다. */
    seen() {
      if (this.done) return null;
      const { bar, beat } = this.steps[this.index], p = this.p, v = this.d.balance.visible;
      const q = (x, step, n) => clamp(quantize(x, step), -n, n);
      return {
        Bar: bar.index, Beat: beat, RequiredDynamic: bar.requiredDynamic,
        Playing: [...p.Playing],
        BowReadable: this.d.sections.map(s => s.bowVisible),
        BreathQuantMs: this.d.sections.map((s, i) => breathQuant(this.d, i)),
        BowQuantMs: this.d.sections.map((s, i) => bowQuant(this.d, i)),
        FaceQuantLevel: this.d.sections.map((s, i) => faceQuant(this.d, i)),
        Breath: this.d.sections.map((s, i) =>
          p.Playing[i] && s.breathVisible ? q(p.OffsetMs[i], breathQuant(this.d, i), v.breathLevels) : 0),
        Bow: this.d.sections.map((s, i) =>
          p.Playing[i] && s.bowVisible ? q(p.DeltaMs[i], bowQuant(this.d, i), v.bowLevels) : 0),
        Face: this.d.sections.map((s, i) =>
          p.Playing[i] && s.faceVisible
            ? q(p.Dyn[i] - bar.requiredDynamic, faceQuant(this.d, i), v.faceLevels) : 0)
      };
    }

    /** 한 박. 지휘봉을 한 무리에 주고, 모두가 자기 쏠림만큼 벌어진다. */
    step(section, cueId) {
      if (this.done) throw Error("Concert finished");
      const cue = this.d.cues.find(c => c.id === cueId);
      if (!cue) throw Error("Unknown cue");
      const { bar, beat } = this.steps[this.index], p = this.p, seen = this.seen();
      const scale = (amount, percent, carry, i) => {
        const t = amount * percent + carry[i], r = trunc(t / 100);
        carry[i] = t - r * 100;
        return r;
      };

      // (3) 지휘봉을 받은 무리가 responsiveness 만큼 따라온다
      if (section >= 0 && section < this.d.sections.length && p.Playing[section]) {
        const s = this.d.sections[section];
        if (cue.tempoNudgeMs)
          p.OffsetMs[section] -= scale(cue.tempoNudgeMs, s.responsivenessPercent, p.NudgeCarry, section);
        if (cue.dynamicNudge)
          p.Dyn[section] += scale(cue.dynamicNudge, s.responsivenessPercent, p.DynCarry, section);
      }

      // (4) 몸이 벌어지고, 무리하면 박이 밀리고, 세기가 습관으로 돌아간다
      const strainPercent = this.d.balance.drift.strainPerDynErrorPercent;
      let abs = 0, dyn = 0;
      const offsets = [];
      this.d.sections.forEach((s, i) => {
        if (!p.Playing[i]) { p.DeltaMs[i] = 0; return; }
        const before = p.OffsetMs[i];
        p.OffsetMs[i] += scale(p.BiasMs[i], trunc(bar.beatMs * 100 / this.d.score.referenceBeatMs),
                               p.DriftCarry, i);
        const strain = Math.abs(p.Dyn[i] - bar.requiredDynamic);
        if (strain > 0) p.OffsetMs[i] += scale(strain, strainPercent, p.StrainCarry, i);
        if (p.Dyn[i] < s.dynHabit) p.Dyn[i] = Math.min(s.dynHabit, p.Dyn[i] + s.dynDriftPerBeat);
        else if (p.Dyn[i] > s.dynHabit) p.Dyn[i] = Math.max(s.dynHabit, p.Dyn[i] - s.dynDriftPerBeat);
        p.Dyn[i] = clamp(p.Dyn[i], 0, 100);
        p.DeltaMs[i] = p.OffsetMs[i] - before;
        abs += Math.abs(p.OffsetMs[i]);
        dyn += Math.abs(p.Dyn[i] - bar.requiredDynamic);
        offsets.push(p.OffsetMs[i]);
      });

      // (5) 기록. **지휘자에게 알려 주지 않는다** — 평론은 연주가 끝난 뒤에만 나온다.
      const record = {
        Bar: bar.index, Beat: beat, Seen: seen,
        OffsetAfter: [...p.OffsetMs], DynAfter: [...p.Dyn],
        SpreadMs: offsets.length > 1 ? Math.max(...offsets) - Math.min(...offsets) : 0,
        AbsOffsetSum: abs, AbsDynSum: dyn, PlayingCount: offsets.length
      };
      this.records.push(record);
      this.index++;
      this.prepare();
      return record;
    }

    /** 다음 날 신문. **연주가 끝나야 부를 수 있다** — 그게 이 게임의 규칙이다. */
    review() {
      if (!this.done) throw Error("Review is available tomorrow");
      const b = this.d.balance.review, rs = this.records, n = rs.length;
      const samples = rs.reduce((a, r) => a + r.PlayingCount, 0);
      let carry = 0;
      const scale = (x, p) => { const t = x * p + carry, r = trunc(t / 100); carry = t - r * 100; return r; };
      const mean = (key, div) => div ? trunc(rs.reduce((a, r) => a + r[key], 0) / div) : 0;

      const accuracy = clamp(100 - scale(mean("AbsOffsetSum", samples), b.accuracyPerMsPercent), 0, 100);
      const ensemble = clamp(100 - scale(mean("SpreadMs", n), b.ensemblePerMsPercent), 0, 100);
      const dynamics = clamp(100 - scale(mean("AbsDynSum", samples), b.dynPerLevelPercent), 0, 100);
      carry = 0;
      const total = scale(accuracy, b.weightAccuracy) + scale(ensemble, b.weightEnsemble)
                  + scale(dynamics, b.weightDynamics);

      // 지적. 템포는 큰 것부터 maxTempoNotes 개, 세기는 하나, 갈라짐은 하나.
      const notes = [];
      const signedOffset = this.d.sections.map((s, i) =>
        trunc(rs.reduce((a, r) => a + (r.Seen.Playing[i] ? r.OffsetAfter[i] : 0), 0) / n));
      const order = this.d.sections.map((s, i) => i).sort((x, y) => {
        const mx = Math.abs(signedOffset[x]), my = Math.abs(signedOffset[y]);
        return mx !== my ? my - mx : x - y;            // 같으면 선언 순서 — 결정적이어야 한다
      });
      let taken = 0;
      for (const i of order) {
        if (taken >= b.maxTempoNotes) break;
        if (Math.abs(signedOffset[i]) < b.noteThresholdMs) continue;
        const def = this.d.press.notes.find(x => x.kind === (signedOffset[i] > 0 ? "lag" : "rush"));
        notes.push(def.text.replace("{0}", this.d.sections[i].name));
        taken++;
      }

      const signedDyn = this.d.sections.map((s, i) =>
        trunc(rs.reduce((a, r) => a + (r.Seen.Playing[i] ? r.DynAfter[i] - r.Seen.RequiredDynamic : 0), 0) / n));
      let dWorst = 0;
      signedDyn.forEach((m, i) => { if (Math.abs(m) > Math.abs(signedDyn[dWorst])) dWorst = i; });
      if (Math.abs(signedDyn[dWorst]) >= b.noteThresholdLevel) {
        const def = this.d.press.notes.find(x => x.kind === (signedDyn[dWorst] > 0 ? "loud" : "soft"));
        notes.push(def.text.replace("{0}", this.d.sections[dWorst].name));
      }

      if (mean("SpreadMs", n) >= b.splitThresholdMs) {
        const hi = signedOffset.indexOf(Math.max(...signedOffset));
        const lo = signedOffset.indexOf(Math.min(...signedOffset));
        if (hi !== lo)
          notes.push(this.d.press.notes.find(x => x.kind === "split").text
            .replace("{0}", this.d.sections[hi].name).replace("{1}", this.d.sections[lo].name));
      }
      if (!notes.length) notes.push(this.d.press.notes.find(x => x.kind === "clean").text);

      const grade = this.d.press.grades.find(g => total >= g.minTotal) || this.d.press.grades.at(-1);
      return { total, accuracy, ensemble, dynamics, grade, notes, passed: total >= b.passTotal };
    }
  }

  root.Concert = Concert;
  if (typeof module !== "undefined") module.exports = { Concert };
})(typeof window === "undefined" ? globalThis : window);
