/* Presentation only: all legal game-state changes come from C#-exported graph.json. */
(() => {
  'use strict';
  const app = document.getElementById('app');
  let graph, nodes, current, history = [], practice = matchMedia('(prefers-reduced-motion: reduce)').matches;
  let manualBeat = 0, epoch = performance.now(), animation;
  const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const h = esc;
  const has = id => current.actions.some(a => a.id === id);
  const act = (id, label, cls='') => has(id) ? `<button class="scene-button ${cls}" data-action="${id}">${label}</button>` : '';
  const folio = (n) => String(n).padStart(2,'0');
  const seal = text => `<span class="seal">${h(text)}</span>`;
  const svg = (body, cls='') => `<svg class="art ${cls}" viewBox="0 0 640 420" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">${body}</svg>`;
  const line = (x1,y1,x2,y2,extra='') => `<line x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}" ${extra}/>`;
  function scenery() {
    const s=current.state, d=graph.scenario, k=d.kind;
    if(k==='scent') return `<div class="scene-caption"><span>흔적 표본 001 / 서재의 외투</span><span>${d.items[s.a].title}</span></div>
      <div class="scent-field">${svg(`<defs><radialGradient id="scentGlow"><stop stop-color="#aee8cd" stop-opacity=".23"/><stop offset="1" stop-color="#aee8cd" stop-opacity="0"/></radialGradient></defs>
        <ellipse cx="320" cy="210" rx="240" ry="180" fill="url(#scentGlow)"/>
        ${[0,1,2,3,4,5,6].map(i=>`<ellipse cx="${310+Math.sin(i)*25}" cy="${195+i*5}" rx="${62+i*24}" ry="${44+i*17}" fill="none" stroke="${['#93dbb8','#c9b084','#b8bfd8'][s.a]}" stroke-opacity="${.8-i*.09}" transform="rotate(${i*14+s.a*20} 320 210)"/>`).join('')}
        <path d="M270 140l-35 46 28 27v92h111v-92l28-27-35-46-32 16h-33z" fill="none" stroke="#dce8e0" stroke-opacity=".3" stroke-dasharray="4 5"/>
        <circle cx="320" cy="207" r="4" fill="#e5ffea"/>
        <text x="320" y="375" text-anchor="middle" fill="#bed2cd" font-size="14">${h(d.items[s.a].text)}</text>`)}</div>
      <div class="time-strip">${d.items.map((v,i)=>has('time'+i)?act('time'+i,`<small>시간층 ${i+1}</small>${v.title}`):`<div class="time-active"><small>현재 시간층</small>${v.title}</div>`).join('')}</div>
      <div class="evidence-strip">${d.items.map((v,i)=>`<span class="${s.flags&(1<<i)?'collected':''}">${s.flags&(1<<i)?'●':'○'} ${h(v.text)}</span>`).join('')}</div>`;
    if(k==='diplomacy') return `<div class="scene-caption"><span>국경 회담 / 통역석</span><span>비공개 속기록</span></div>
      <div class="delegates"><div><span class="flag north"></span><small>북국 대표단</small><strong>“확약이 필요합니다.”</strong></div><div class="meeting-line">Ⅱ</div><div><span class="flag south"></span><small>남국 대표단</small><strong>“조건을 지켜 주십시오.”</strong></div></div>
      <div class="document treaty"><div class="doc-eyebrow">발언 원문 / ${folio(Math.min(s.step+1,3))}</div><blockquote>${h(d.items[Math.min(s.step,2)].text)}</blockquote><div class="signature-line">통역자의 번역을 기다리는 중</div></div>
      <div class="record-pair"><div><small>남국 기록</small><p>조건부 약속 · 승인 후 이행</p></div><div class="${s.a?'disagreement':''}"><small>북국 기록</small><p>${s.a?`${s.a}개 조항이 확약으로 기록됨`:'원문의 조건이 유지됨'}</p></div></div>`;
    if(k==='phone') return `<div class="phone-table"><div class="phone"><div class="phone-status"><span>통신 가능</span><span>${s.a}% ▰</span></div><div class="phone-notch"></div><div class="lock" aria-label="잠금 상태"><svg width="19" height="23" viewBox="0 0 19 23" aria-hidden="true"><path d="M5 10V6a4.5 4.5 0 019 0v4" fill="none" stroke="currentColor" stroke-width="2"/><rect x="2" y="10" width="15" height="11" rx="3" fill="currentColor"/></svg></div><div class="phone-time">19:${folio(42+Math.min(s.step,4)*3)}</div><div class="phone-date">9월 27일 일요일</div>
      <div class="notification"><small>${s.step===0?'수신 전화':'알림 센터'} · 지금</small><h3>${h(current.title)}</h3><p>${h(current.prompt)}</p></div>
      <div class="phone-actions">${current.actions.slice(0,2).map(a=>act(a.id,h(a.label))).join('')}</div><div class="locked-label">잠금 해제 불가 · 앱 접근 제한</div><div class="home-bar"></div></div><div class="phone-evidence"><span class="tiny-label">주인의 마지막 기록</span><p>어제 17:00<br>원본 인계</p><p>배터리 ${s.a}%<br>주인은 돌아오지 않는다.</p></div></div>`;
    if(k==='memory') return `<div class="scene-caption"><span>의뢰인 03 / 생일 기억</span><span>고정 사실: 형만 열쇠를 소유</span></div>
      <div class="memory-photo">${svg(`<rect x="90" y="30" width="460" height="350" rx="2" fill="#e8cfc5" opacity=".08"/><rect x="135" y="65" width="120" height="170" fill="none" stroke="#cbb8d4" opacity=".35"/><path d="M195 65v170M135 150h120" stroke="#cbb8d4" opacity=".25"/><path d="M95 310h450M150 310v50M490 310v50" stroke="#e3c2bc" stroke-width="4" opacity=".5"/>
        <circle cx="330" cy="140" r="30" fill="#d6bfcf" opacity=".6"/><path d="M280 265v-67q50-50 100 0v67" fill="#d6bfcf" opacity=".3"/>
        <circle cx="440" cy="188" r="24" fill="#d6bfcf" opacity=".4"/><path d="M405 290v-56q35-36 70 0v56" fill="#d6bfcf" opacity=".2"/>
        <rect x="244" y="272" width="62" height="36" fill="#dbaec7" opacity=".6"/><path d="M255 272v-20m17 20v-20m17 20v-20" stroke="#f5dbc5" stroke-width="3"/>
        <path d="M170 345C235 180 400 340 493 88" fill="none" stroke="#e8acc9" stroke-dasharray="5 7"/>
        <text x="320" y="405" text-anchor="middle" fill="#bdb0c7" font-size="13">생일 케이크 / 잠긴 문 / 열쇠 하나</text>`)}</div>
      <div class="memory-slots">${act('person',`<small>인물</small><strong>${s.a?'형':'나'}</strong>↻`)}${act('act',`<small>행동</small><strong>${s.b?'문을 잠근다':'선물을 건넨다'}</strong>↻`)}${act('emotion',`<small>감정</small><strong>${s.c?'두려움':'온기'}</strong>↻`)}</div>`;
    if(k==='map') return `<div class="scene-caption"><span>시립 측량국 / 구역 03</span><span>축척 1:500 · 승인 전</span></div>
      <div class="city-map">${svg(`<defs><pattern id="grid" width="25" height="25" patternUnits="userSpaceOnUse"><path d="M25 0H0V25" fill="none" stroke="#abc2bd" stroke-opacity=".15"/></pattern></defs><rect width="640" height="420" fill="url(#grid)"/>
        <path d="M0 295H360V130H455" fill="none" stroke="${s.a?'#d4bc86':'#819593'}" stroke-width="${s.a?44:13}" stroke-opacity=".36"/>
        <path d="M12 295H360V130H450" fill="none" stroke="#e4ceb1" stroke-dasharray="7 8" opacity=".6"/>
        <g fill="#29474c" stroke="#93afa8" stroke-width="2"><rect x="38" y="50" width="130" height="125"/><rect x="202" y="40" width="100" height="135"/><rect x="64" y="333" width="200" height="70"/><rect x="190" y="190" width="115" height="76"/><rect x="451" y="64" width="150" height="145"/></g>
        <g fill="#dce2d4" text-anchor="middle" font-size="15"><text x="100" y="117">01 주택</text><text x="251" y="109">02 상점</text><text x="248" y="226">03 주택</text><text x="526" y="124">중앙 병원</text><text x="526" y="150">＋</text></g>
        <path d="M441 74v${s.b?35:125}" stroke="${s.b?'#9fd6b2':'#e58f77'}" stroke-width="5"/>
        ${s.b?'<path d="M441 152v47" stroke="#9fd6b2" stroke-width="5"/>':''}
        <circle cx="${s.c?210:280}" cy="${s.c?181:275}" r="10" fill="${s.c?'#b6ddb0':'#d88068'}"/>
        <text x="${s.c?194:280}" y="${s.c?169:310}" text-anchor="middle" fill="#efcdb4" font-size="12">${s.c?'이전한 출입구':'지하 계단'}</text>
        <text x="590" y="345" fill="#d1dccb" font-size="17">N</text><path d="M596 350v39m-7-29 7-10 7 10" stroke="#d1dccb" fill="none"/>
        <text x="37" y="245" fill="#dec79d" font-size="13">구급차 →</text>`)}</div>
      <div class="map-toolbar">${act('road','↔ 도로 폭')}${act('gate','⊞ 병원 입구')}${act('door','⌂ 지하 출입구')}</div>
      <div class="map-legend"><span>━ 승인 예정 도로</span><span>● 주민 출입구</span><span>▧ 기존 건물</span></div>`;
    if(k==='curse') return `<div class="scene-caption"><span>청산 종가 / 계약 보관함</span><span>결산일 ${Math.min(s.step+1,3)}</span></div><div class="ledger-book"><div class="ledger-binding"></div><div class="ledger-page"><div class="doc-eyebrow">약조 제 十七 호</div><h2>마르지 않는<br>우물의 계약</h2><p class="contract-text">마을에 물이 마르지 않게 하되,<br>그 대가로 매 결산마다<br>한 집의 소중한 기억을 받는다.</p><div class="contract-rule"></div><div class="ledger-row"><span>이번 청구</span><strong>임매월의 집</strong></div><div class="ledger-row"><span>선대의 기록</span><strong class="red-ink">전년도 미납</strong></div><div class="ledger-row"><span>상속인</span><strong>당신</strong></div>${seal(s.flags&16?'변경':'승계')}<p class="margin-note">물을 빌린 것은 할아버지.<br>기억을 빚진 것은 우리.</p></div></div>
      <div class="receipt ${s.flags&1?'revealed':''}"><span>지난해 영수증</span><strong>${s.flags&1?'납부 완료 · 중복 청구 확인':'봉인됨 · 선대 장부 조사 필요'}</strong></div>`;
    if(k==='baton') return `<div class="scene-caption"><span>실내악 연습실 / 무음</span><span>ADAGIO · 4 VOICES</span></div><div class="quartet">${d.items.map((v,i)=>`<div class="musician ${s.step===i?'active':''} ${s.step>i?'played':''}">${svg(`<ellipse cx="320" cy="366" rx="130" ry="19" fill="#ad9b76" opacity=".15"/><circle cx="320" cy="95" r="40" fill="#bec4bd" opacity=".75"/><path d="M250 285v-116q70-58 140 0v116z" fill="#707c79"/><path d="M253 198l133 75M241 235l159-52" stroke="#e6c38a" stroke-width="12"/><path d="M251 302l-25 69m163-69 25 69" stroke="#7b8582" stroke-width="13"/>`)}<small>${h(v.title)}</small><span>${s.step===i?'들숨 · 큐를 기다림':s.step>i?'진입 완료':'대기'}</span></div>`).join('')}</div>
      <div class="beat-label"><span>호흡을 보고 진입 구간에서 큐를 주세요</span><span id="beat-readout">1 / 8</span></div><div class="beat-track">${Array.from({length:d.values[1]},(_,i)=>`<div class="beat ${i>=d.values[2]&&i<=d.values[3]?'target':''}" data-beat="${i}">${i+1}</div>`).join('')}</div>
      <div class="conductor-controls"><button id="cue" class="primary">큐 보내기 <kbd>Space</kbd></button><button id="practice" aria-pressed="${practice}">${practice?'실시간으로 전환':'연습 모드'}</button>${practice?'<button id="tick">박자 한 칸 →</button>':''}</div><p class="practice-note">${practice?'연습 모드 · 박자를 직접 이동합니다.':'실시간 · 밝은 3–4박 구간에서 진입합니다.'}</p>`;
    if(k==='editor') {
      const paragraphs=[`<p class="manuscript-line"><span class="line-num">01</span>그날 저녁, <mark>${s.c?'해가 저물었다.':'바다가 접혔다.'}</mark> 나는 식탁에 두 개의 그릇을 놓았다.</p>`,
        `<p class="manuscript-line ${s.a?'struck':''}"><span class="line-num">02</span>${h(d.items[1].text)}</p>`,
        `<p class="manuscript-line ${s.b?'moved':''}"><span class="line-num">03</span>${h(d.items[2].text)}</p>`];
      return `<div class="scene-caption"><span>문예지 물결 / 제7호</span><span>서윤 作 · 교정 1쇄</span></div><article class="manuscript document"><div class="doc-eyebrow">단편소설 / 원고지 14매</div><h2>바다가 접히는 밤</h2><div class="author">서윤</div>${(s.b?[paragraphs[2],paragraphs[0],paragraphs[1]]:paragraphs).join('')}<div class="editor-note">이 말만은 남겨 주세요.<br>어머니의 말투예요.</div><div class="manuscript-tools">${act('strike',s.a?'삭제 취소':'<s>문장 삭제</s>')}${act('move',s.b?'순서 복구':'↑ 마지막 문단 이동')}${act('replace',s.c?'원문 복구':'표현 교체')}</div></article>`;
    }
    if(k==='apartment') return `<div class="scene-caption"><span>은하아파트 3층</span><span>출발까지 ${s.a}시간</span></div><div class="corridor"><div class="ceiling-light"></div>${d.items.map((v,i)=>`<div class="apartment"><div class="door ${s.flags&(1<<i)?'listened':''}"><span class="door-number">30${i+1}</span><span class="peephole"></span><span class="door-handle"></span><div class="door-sound">${s.flags&(1<<i)?['상자를 끄는 소리','고양이를 부르는 소리','말없이 앉아 있는 사람'][i]:'···'}</div></div><div class="boxes"><i></i><i></i></div><strong>${h(v.title.split(' · ')[1])}</strong>${act('listen'+i,'귀 기울이기')}${(s.flags&(1<<i))?`<span class="door-status">${s.flags&(1<<(i+3))?'도움 전함':'사정 확인'}</span>`:''}</div>`).join('')}</div><div class="moving-tag"><span>마지막 이삿차</span><strong>한 가구를 더 실을 수 있습니다.</strong><span>18:00 출발</span></div>`;
    if(k==='prayer') return `<div class="scene-caption"><span>기적 배정과 / 날씨 담당</span><span>접수 번호 003</span></div><div class="prayer-letters">${d.items.map((v,i)=>`<div class="envelope ${i===2&&!s.flags?'unopened':''}"><div class="postage">${['雨','晴','水'][i]}</div><small>기도 ${folio(i+1)}</small><h3>${h(v.title)}</h3><p>${h(i===2&&s.flags?'오전 비가 오기 전에 수문을 열어 주세요.':v.text)}</p>${i===2?act('read','편지 개봉'):''}</div>`).join('')}</div>
      <div class="allocation"><div><small>오전 / 06–12</small><span class="weather">${s.a?'☂':'☼'}</span><strong>${s.a?'비':'맑음'}</strong>${act('morning','날씨 바꾸기')}</div><div><small>오후 / 12–18</small><span class="weather">${s.b?'☂':'☼'}</span><strong>${s.b?'비':'맑음'}</strong>${act('afternoon','날씨 바꾸기')}</div><div class="gate-cell"><small>하류 수문</small><span class="weather">${s.c?'≋':'≡'}</span><strong>${s.c?'열림':'닫힘'}</strong>${act('gate','수문 조정')}</div></div>`;
    return '';
  }
  function render(focus=false) {
    cancelAnimationFrame(animation);
    const d=graph.scenario;
    app.dataset.node=current.id;
    app.dataset.terminal=String(current.terminal);
    const visible=d.kind==='baton'?[]:current.actions;
    app.innerHTML=`<header class="topbar"><a href="../../../docs/poc-gallery/revised.html" class="back">← 컨셉 갤러리</a><span class="edition">PLAYABLE STUDIES / ${d.english}</span><button id="restart" class="reset">다시 시작 ↺</button></header>
      <div class="masthead"><div><div class="eyebrow">${h(d.english)}</div><h1>${h(d.title)}</h1><p class="tagline">${h(d.tagline)}</p></div><div class="stats">${current.stats.map(v=>`<div><small>${h(v.label)}</small><strong>${h(v.value)}</strong></div>`).join('')}</div></div>
      <div class="play-area"><aside class="dossier"><span class="tiny-label">당신의 일</span><h2>${h(current.title)}</h2><p>${h(current.prompt)}</p><div class="dossier-divider"></div><span class="tiny-label">${current.terminal?'결산 기록':'새로 알게 된 것'}</span><p class="feedback" role="status">${h(current.feedback)}</p><details><summary>조작 안내</summary><p>${h(d.instructions)}</p></details><div class="study-index">${h(d.kind.toUpperCase())}<br><span>한 장면의 플레이 실험</span></div></aside>
      <section class="stage" aria-label="게임 화면">${scenery()}${current.terminal?`<div class="ending"><div class="ending-paper"><span class="tiny-label">이 플레이의 결과</span><h2 tabindex="-1" id="ending-title">${h(current.ending)}</h2><p>${h(current.prompt)}</p><button class="primary" id="replay">다른 선택으로 다시 시작</button></div></div>`:''}</section>
      <aside class="decision-panel"><span class="tiny-label">${current.terminal?'남겨진 질문':'결정하는 자리'}</span><h2>${current.terminal?'다른 방법은 있었을까':d.kind==='baton'?'지휘자의 시선':'다음 행동'}</h2><div class="choices">${visible.map((a,i)=>`<button class="choice ${['approve','return','dispatch','implant','renegotiate'].includes(a.id)?'emphasized':''}" data-action="${h(a.id)}"><span class="choice-number">${folio(i+1)}</span><span><strong>${h(a.label)}</strong><small>${h(a.hint)}</small></span><span class="choice-arrow">↗</span></button>`).join('')}</div>${d.kind==='baton'&&!current.terminal?`<p>준비 동작을 보고, 밝은 박자 구간에서 큐를 보내세요.</p><div class="cue-card">${h(d.items[Math.min(current.state.step,3)].detail)}<br><small>소리 없는 연습 / 네 번의 진입</small></div><p>키보드 Space로도 큐를 보낼 수 있습니다. 연습 모드에서는 박자를 직접 움직입니다.</p>`:''}${current.terminal?`<p>${h(d.question)}</p><p>새 플레이에서는 이전 선택이 초기화됩니다.</p>`:''}<div class="side-note">${current.terminal?'이 결과는 선택의 흔적입니다.':'행동의 대가를 살핀 뒤 결정하세요.'}</div></aside></div>
      <footer class="history"><span class="tiny-label">진행 기록</span><ol>${history.length?history.slice(-4).map(x=>`<li>${h(x)}</li>`).join(''):'<li>첫 선택을 기다리고 있습니다.</li>'}</ol><span class="prototype-label">CONCEPT PoC · 저장 기능 없음</span></footer>`;
    if(d.kind==='baton'&&!current.terminal) animateBeat();
    if(focus && current.terminal) document.getElementById('ending-title')?.focus();
  }
  function go(id) {
    const action=current.actions.find(a=>a.id===id);
    if(!action) return;
    history.push(action.label);
    current=nodes.get(action.target);
    epoch=performance.now();manualBeat=0;
    render(true);
  }
  function reset() {current=nodes.get(graph.start);history=[];manualBeat=0;epoch=performance.now();render();}
  function beat() {return practice?manualBeat:Math.floor(((performance.now()-epoch)%graph.scenario.values[0])/graph.scenario.values[0]*graph.scenario.values[1]);}
  function animateBeat() {
    const i=beat();
    document.querySelectorAll('[data-beat]').forEach(el=>el.classList.toggle('now',Number(el.dataset.beat)===i));
    const readout=document.getElementById('beat-readout');
    if(readout)readout.textContent=`${i+1} / ${graph.scenario.values[1]}`;
    const player=document.querySelector('.musician.active');
    if(player){
      player.querySelector('.art').style.transform=`translateY(${-Math.sin(i/8*Math.PI*2)*5}px) rotate(${i<2?-2:i<4?1:0}deg)`;
      player.querySelector('span').textContent=i<2?'들이쉼 · 준비':i<4?'시선이 맞았다 · 진입':'내쉼 · 다음 호흡';
    }
    animation=requestAnimationFrame(animateBeat);
  }
  app.addEventListener('click',e=>{
    const b=e.target.closest('button');if(!b)return;
    if(b.dataset.action)return go(b.dataset.action);
    if(b.id==='restart'||b.id==='replay')return reset();
    if(b.id==='cue')return go('cue'+beat());
    if(b.id==='practice'){practice=!practice;manualBeat=0;epoch=performance.now();render();}
    if(b.id==='tick')manualBeat=(manualBeat+1)%graph.scenario.values[1];
  });
  document.addEventListener('keydown',e=>{
    if(e.code==='Space'&&graph?.scenario.kind==='baton'&&!current.terminal&&!['BUTTON','A','SUMMARY','INPUT','TEXTAREA'].includes(document.activeElement.tagName)) {e.preventDefault();go('cue'+beat());}
  });
  fetch('../data/graph.json').then(r=>{if(!r.ok)throw new Error('graph.json '+r.status);return r.json();}).then(data=>{
    graph=data;nodes=new Map(data.nodes.map(n=>[n.id,n]));current=nodes.get(data.start);
    document.body.dataset.kind=data.scenario.kind;
    const names={background:'bg',surface:'surface',paper:'ink',accent:'accent',muted:'muted'};
    Object.entries(data.scenario.palette).forEach(([k,v])=>document.documentElement.style.setProperty('--'+names[k],v));
    render();app.dataset.ready='true';
  }).catch(error=>{
    app.innerHTML=`<div class="load-error"><h1>게임 데이터를 열 수 없습니다.</h1><p>저장소 루트에서 <code>python -m http.server 8000 --bind 127.0.0.1</code>를 실행하고 HTTP 주소로 열어 주세요.</p><p>graph.json이 없다면 이 게임의 README에 있는 C# 테스트를 실행해 생성합니다.</p><p>${h(error.message)}</p><a href="../../../docs/poc-gallery/revised.html">PNG 갤러리 보기</a></div>`;
    app.dataset.error=error.message;
  });
})();
