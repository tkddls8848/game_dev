const fs=require('fs'),path=require('path'),assert=require('assert/strict');
const {Concert}=require('../presentation/engine.js');const root=path.resolve(__dirname,'..');
const read=n=>JSON.parse(fs.readFileSync(path.join(root,'data',n+'.json'),'utf8'));
const d={sections:read('sections').sections,score:read('score'),cues:read('cues').cues,balance:read('balance'),press:read('press')};
const fixtures=JSON.parse(fs.readFileSync(path.join(root,'TestBuild/browser-fixtures.json'),'utf8'));let beats=0;
for(const f of fixtures){const g=new Concert(d,d.score.pieces.find(p=>p.id===f.pieceId),f.cast);assert.throws(()=>g.review());
for(const step of f.log){assert.deepEqual(g.seen(),step.Seen);const actual=g.step(step.Given.SectionIndex,step.Given.CueId);for(const key of ['OffsetAfter','DynAfter','SpreadMs','AbsOffsetSum','AbsDynSum','PlayingCount'])assert.deepEqual(actual[key],step[key],`${f.pieceId} ${step.Bar}/${step.Beat} ${key}`);beats++;}
assert.equal(g.done,true);const r=g.review();assert.equal(r.total,f.total);assert.equal(r.accuracy,f.review.Accuracy);assert.equal(r.ensemble,f.review.Ensemble);assert.equal(r.dynamics,f.review.Dynamics);assert.deepEqual(r.notes,f.review.Notes.map(n=>n.Text));assert.throws(()=>g.step(0,'c_hold'));}
console.log(`C#/browser parity: ${fixtures.length} performances / ${beats} beats passed`);
