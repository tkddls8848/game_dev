#!/usr/bin/env python3
# ⚠️ 이 도구의 기본 대상 게임(games/mystery-blackwood)은 다른 저장소로 갔다 —
#    github.com/tkddls8848/game . 여기서 쓰려면 대상 게임을 인자로 주거나
#    아래 기본값을 이 저장소에 있는 게임으로 바꾼다.
"""Package the existing ten Gemini samples. Offline only; never synthesizes or edits the live case."""
import hashlib
import html
import importlib.util
import json
from pathlib import Path
import statistics
import sys
import wave

ROOT = Path(__file__).resolve().parents[2]
GAME = ROOT / 'games/mystery-blackwood'
SAMPLES = ROOT / 'AssetDownloads/audition/gemini-ten'
OUT = GAME / 'docs/voice-audition'
NAMES = {'v1': '클라라', 'v2': '마르코', 'v3': '줄리안', 'v4': '헬렌', 'v5': '에드먼드'}
VOICES = dict(zip(NAMES, ['Despina', 'Algenib', 'Puck', 'Erinome', 'Gacrux']))
TRAITS = dict(zip(NAMES, ['담담하게', '낮고 거칠게', '젊고 조심스럽게', '또박또박', '노년의 낮은 목소리로']))


def inspect_wav(path):
    with wave.open(str(path), 'rb') as w:
        frames = w.getnframes()
        pcm = w.readframes(frames)
        if w.getnchannels() != 1 or w.getsampwidth() != 2 or w.getframerate() != 24000:
            raise ValueError(f'Unexpected WAV format: {path.name}')
        if len(pcm) != frames * 2 or not any(pcm):
            raise ValueError(f'Truncated or silent WAV: {path.name}')
        return (frames * 1000 + w.getframerate() // 2) // w.getframerate()


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    script_path = GAME / 'unity/Assets/Resources/GameData/cases/case_02/script.json'
    script = json.loads(script_path.read_text(encoding='utf-8'))
    by = {u['id']: u for u in script['utterances']}
    rows = []
    for path in sorted(SAMPLES.glob('*.wav')):
        order, voice, uid = path.stem.split('_', 2)
        u = by[uid]
        if u['voiceId'] != voice:
            raise ValueError('Voice mismatch: ' + uid)
        actual = inspect_wav(path)
        end = u['startMs'] + actual
        overlap = [v['id'] for v in script['utterances'] if v['voiceId'] == voice
                   and u['startMs'] < v['startMs'] < end]
        interruptions = [v['id'] for v in script['utterances'] if v['room'] == u['room']
                         and u['startMs'] < v['startMs'] < end]
        live = GAME / f'unity/Assets/Resources/Audio/Voice/case_02/{uid}.mp3'
        rows.append(dict(order=int(order), id=uid, voice=voice, name=NAMES[voice],
                         providerVoice=VOICES[voice], text=u['text'], chars=len(u['text']),
                         direction='한국어 자연스러운 대화 속도(초당 약 6자)로, ' + TRAITS[voice],
                         startMs=u['startMs'], designedMs=u['durationMs'], actualMs=actual,
                         ratio=round(actual/u['durationMs'], 4), selfOverlaps=overlap,
                         followingRoomLines=interruptions, file=path.name,
                         sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                         baselineFile=live.name if live.exists() else None))
    if len(rows) != 10 or any(sum(r['voice'] == v for r in rows) != 2 for v in NAMES):
        raise ValueError('Expected ten samples, two per voice')
    # Exploratory fit only. All scene checks happen in memory in a separate module instance.
    xs=[r['chars'] for r in rows]; ys=[r['actualMs'] for r in rows]
    slope=round(sum((x-statistics.mean(xs))*(y-statistics.mean(ys)) for x,y in zip(xs,ys)) /
                sum((x-statistics.mean(xs))**2 for x in xs))
    base=round(statistics.mean(ys)-slope*statistics.mean(xs))
    spec=importlib.util.spec_from_file_location('case02_audition',GAME/'tools/build_case02.py')
    case=importlib.util.module_from_spec(spec);spec.loader.exec_module(case)
    models=[]
    for name,b,k in [('current',850,128),('sample_fit',base,slope),('candidate_B',1200,270),('candidate_C',2000,230)]:
        case.MS_BASE=b;case.MS_PER_CHAR=k
        _,_,_,_,problems=case.build()
        models.append(dict(name=name,baseMs=b,msPerChar=k,
                           meanAbsoluteErrorMs=round(statistics.mean(abs(b+k*r['chars']-r['actualMs']) for r in rows)),
                           sceneProblems=problems))
    a=next(r for r in rows if r['id']=='c_hall_05'); b=next(r for r in rows if r['id']=='c_dining_06')
    simultaneous=max(0,min(a['startMs']+a['actualMs'],b['startMs']+b['actualMs'])-max(a['startMs'],b['startMs']))
    report=dict(scope='Existing 10 audition clips only; no API requests; live script unchanged',
                model='gemini-3.8-flash-tts (recorded generating session)',
                scriptSha256=hashlib.sha256(script_path.read_bytes()).hexdigest(),
                samples=rows,medianRatio=round(statistics.median(r['ratio'] for r in rows),3),
                models=models,simultaneousSampleOverlapMs=simultaneous,
                limitation='Two clips per voice cannot validate the remaining 180 utterances or a full reflow.')
    OUT.mkdir(parents=True,exist_ok=True)
    (OUT/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    cards=[]
    for r in rows:
        e=html.escape
        original=(f'<label>기존 본편 · 같은 대사</label><audio controls preload="none" src="../../unity/Assets/Resources/Audio/Voice/case_02/{r["baselineFile"]}"></audio>' if r['baselineFile'] else '<p>기존 음성 없음</p>')
        cards.append(f'<article><small>{r["order"]:02d} / {r["voice"]} · {e(r["providerVoice"])}</small><h2>{r["name"]}</h2><p>{e(r["text"])}</p><div class="meta">{r["actualMs"]/1000:.2f}초 · 기존 설계 {r["designedMs"]/1000:.2f}초 · {r["ratio"]:.2f}배</div><label>Gemini · 연기 지시 적용</label><audio controls preload="none" src="../../../../AssetDownloads/audition/gemini-ten/{r["file"]}"></audio>{original}</article>')
    page='''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>블랙우드 · 목소리 청취실</title><style>body{margin:0;background:#141e25;color:#e5dfd0;font-family:'Malgun Gothic',sans-serif}main{max-width:1150px;margin:auto;padding:40px 24px}h1{font-size:42px}header p{max-width:800px;line-height:1.9;color:#b4bdbe}small,label{color:#c7aa78}section{display:grid;grid-template-columns:1fr 1fr;gap:18px}article{padding:24px;border:1px solid #3e4b4d;background:#1d2a30}article p{min-height:65px;line-height:1.8}h2{margin:10px 0;font-size:25px}audio{width:100%;display:block;margin:12px 0 18px}.meta{font-size:13px;color:#a9b6b7;margin:16px 0}label{display:block;font-size:12px}a{color:#c7aa78}@media(max-width:650px){section{grid-template-columns:1fr}h1{font-size:31px}}</style><main><header><small>BLACKWOOD / VOICE AUDITION</small><h1>열 개의 목소리, 같은 대사의 두 연기</h1><p>기존에 생성한 대표 대사 10개입니다. 배역마다 두 줄을 골랐습니다. Gemini와 기존 본편을 같은 문장으로 비교하세요. 한 음성을 재생하면 다른 음성은 멈춥니다.</p><p>연기의 적합성은 청취로 판단합니다. 이 페이지는 본편 교체본이 아닙니다. 자연스러운 호흡을 유지하려면 대사 배치와 장면 길이를 함께 조정해야 합니다.</p><p><a href="README.md">타이밍 분석</a> · <a href="../../../../docs/poc-gallery/index.html">게임 PoC 갤러리</a></p></header><section>'''+''.join(cards)+'''</section></main><script>document.addEventListener('play',e=>{if(e.target.tagName==='AUDIO')document.querySelectorAll('audio').forEach(a=>{if(a!==e.target)a.pause();});},true);</script></html>'''
    (OUT/'index.html').write_text(page,encoding='utf-8')
    md=['# Gemini 대표 대사 10개 — 인계 완료','',
        '이전 요청인 **전량 합성 없이 대표 대사 10개만**을 유지했다. 추가 API 호출 0회. 기존 본편 음성·대본·시간표를 수정하지 않았다.','',
        '[청취실](index.html)에서 같은 대사의 Gemini와 기존 ElevenLabs 음성을 비교할 수 있다. WAV 원본은 기존 `AssetDownloads/audition/gemini-ten`에 있으며 gitignore 대상이다. 다른 컴퓨터에는 원본을 별도로 복사해야 한다.','',
        f'WAV 10개 모두 PCM 24kHz/모노/16bit·데이터 길이·비무음 검증 통과. 실제/설계 길이 중앙값 **{report["medianRatio"]:.2f}배**. 청각적 품질이나 발음 정확성을 자동 검증했다는 뜻은 아니다.','',
        '| 대사 | 실제 ms | 설계 ms | 뒤의 같은 목소리와 겹침 |','|---|---:|---:|---|']
    md += [f'| {r["id"]} | {r["actualMs"]} | {r["designedMs"]} | {", ".join(r["selfOverlaps"]) or "없음"} |' for r in rows]
    md += ['',f'8시 14분 핵심 두 발화의 시작점을 그대로 둘 때 실측 겹침은 **{simultaneous}ms**다. 하지만 앞선 대사가 밀리는 문제까지 해결했다는 뜻은 아니다. 표는 해당 샘플 이후의 원래 시작점과 비교한 국소 진단이다.','',
           '| 길이 모형 | 기본 ms | ms/자 | 표본 평균 절대 오차 ms | 장면 검사 문제 수 |','|---|---:|---:|---:|---:|']
    md += [f'| {m["name"]} | {m["baseMs"]} | {m["msPerChar"]} | {m["meanAbsoluteErrorMs"]} | {len(m["sceneProblems"])} |' for m in models]
    md += ['', '**판정:** 길이 상수만 늘리는 방식은 기존 장면 창과 고정 시각을 위반한다. 전량 전환 시에는 클립별 실측 길이로 장면을 재배치하고, 8시 14분 동시 단서·이동 트랙·사건 시각을 함께 재검증해야 한다. 각 배역 표본이 2개라 이 회귀식으로 나머지 180개 길이를 확정하면 안 된다.', '',
           '추가 합성을 시작하거나 본편 시간표를 바꾸는 결정은 이번 대표 대사 작업에 포함하지 않았다. 출시 관련 판단도 이전 사용자 요청대로 보류 상태다.', '',
           '재생성: `python kit/tools/build_voice_audition.py` — 오프라인 분석과 청취 페이지 생성만 수행한다. 세부 문제·SHA-256·대사·연기 지시는 `manifest.json`.']
    (OUT/'README.md').write_text('\n'.join(md)+'\n',encoding='utf-8')
    print(f'Validated {len(rows)} WAVs; median ratio {report["medianRatio"]}; models '+str([(m['name'],len(m['sceneProblems'])) for m in models]))


if __name__=='__main__':main()
