"""Reproduce the scenario, presentation entry points and metadata for the revised PoCs.

Does not generate/overwrite handwritten Rules.cs. Run from any working directory.
The graph.json files are exported only after the C# verification suite passes.
"""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parents[2]

def item(id, title, text, detail=""):
    return dict(id=id, title=title, text=text, detail=detail)

CONCEPTS = [
    dict(slug="mystery-scent-layers",kind="scent",title="잔향 감식실",english="THE SCENT ARCHIVE",
         tagline="사람은 떠나도, 냄새의 순서는 남는다.",
         instructions="세 시간층을 오가며 냄새를 채취하세요. 남은 기록을 비교한 뒤 외투가 이동한 경로를 추론합니다.",
         question="냄새의 겹침만으로 외투의 이동을 추론하는 순간이 즐거운가?",
         colors=["#101b20","#192a30","#e7eee8","#93dbb8","#a0b5b6"],values=[3],
         items=[item("early","18:10","젖은 흙과 흰 꽃향기","온실: 관리인이 젖은 흙을 밟았다. 외투의 안쪽에서 오래된 흙 냄새가 난다."),
                item("middle","18:40","마른 담배와 외투의 잔향","현관: 관리인이 서기에게 외투를 빌려줬다. 담배 냄새가 흙 냄새 위에 얹혔다."),
                item("late","19:05","서기의 향수와 촛농","서재: 가장 바깥층에는 서기의 향수와 촛농이 남아 있다. 흙은 안쪽에서 희미하게 이어진다.")]),
    dict(slug="narrative-armistice",kind="diplomacy",title="휴전의 문장",english="WORDS OF ARMISTICE",
         tagline="같은 서명 아래, 다른 전쟁이 끝난다.",
         instructions="원문의 약속 강도를 살피고 통역 문장을 선택하세요. 양국의 회담 기록이 어디서 달라지는지 확인합니다.",
         question="정확한 통역 때문에 긴장이 높아져도 의미를 지키고 싶은가?",
         colors=["#1b202b","#272e3d","#eee7d8","#d6b876","#b2b4c3"],values=[3],
         items=[item("withdraw","철수 시점","남국 대표: 의회의 승인이 나면 국경에서 철수하겠습니다.","승인을 조건으로 철수하겠다고 전한다"),
                item("blame","사건의 책임","남국 대표: 민간 피해에 유감을 표합니다. 책임 소재는 조사 중입니다.","유감과 책임 인정은 구분해 전한다"),
                item("inspection","감시단 입국","남국 대표: 감시단의 규모를 합의한 뒤 입국을 허용할 수 있습니다.","규모 합의 이후 입국 가능이라고 전한다"),
                item("force1","즉시 철수를 확약했다고 전한다",""),item("force2","민간 피해의 책임을 인정했다고 전한다",""),item("force3","감시단의 입국을 허용했다고 전한다","")]),
    dict(slug="mystery-locked-phone",kind="phone",title="잠금 해제 불가",english="STILL LOCKED",
         tagline="그가 죽었다는 것을, 이 전화는 아직 모른다.",
         instructions="잠금 화면에서 전화에 반응하고 알림을 펼치세요. 위치 단서를 모아 지금 위험에 처한 사람에게 도움을 보냅니다.",
         question="앱을 열 수 없어도 알림에 반응하는 일이 능동적인 조사로 느껴지는가?",
         colors=["#101219","#202633","#f2f4fa","#8ab9ff","#a6b0c2"],values=[12],
         items=[item("yoon","윤서","부재중 전화 · 어제 17:03","구청 기록실"),item("calendar","일정","원본 인계 · 어제 17:00","구청 B2"),item("owner","도현","잠금 화면의 주인","")]),
    dict(slug="puzzle-memory-suture",kind="memory",title="기억의 봉합사",english="MEMORY SUTURE",
         tagline="한 장면을 바꾸면, 사랑의 이유가 달라진다.",
         instructions="기억 조각의 인물·행동·감정을 교체하세요. 열쇠를 가진 사람이라는 고정 사실과 모순되지 않게 봉합합니다.",
         question="논리를 맞추고도 감정을 남길지 고민하게 되는가?",
         colors=["#201c30","#302940","#f0e7f2","#e4b2cb","#bbb0cb"],values=[3],
         items=[item("person","인물","나 / 형","집 열쇠는 형만 가지고 있다."),item("act","행동","선물을 건넨다 / 문을 잠근다","고정 사실에 맞는 행동을 연결하세요."),item("emotion","감정","온기 / 두려움","감정은 모순과 별도로 후유증을 남긴다.")]),
    dict(slug="puzzle-tomorrow-map",kind="map",title="내일의 지도",english="TOMORROW, AS DRAWN",
         tagline="이 선을 지우면, 내일 누군가의 문이 사라진다.",
         instructions="골목을 넓히고 병원 입구를 연결하세요. 누락된 민원을 읽고 지하 세입자의 출입구를 이전한 뒤 도면을 승인합니다.",
         question="통행 퍼즐을 해결하면서 지도에서 누락된 주민도 살피게 되는가?",
         colors=["#14272d","#1e363d","#eee9d8","#e7b77c","#b1c0bc"],values=[3],
         items=[item("hospital","중앙 병원","구급차 진입 필요","북쪽 담장에 출입구가 없다."),item("tenant","지하 03호","미등록 거주","도로 확장선 안에 계단이 있다."),item("road","서쪽 골목","폭 1.8m","구급차가 지나가려면 확장이 필요하다.")]),
    dict(slug="management-curse-inheritance",kind="curse",title="저주상속",english="THE INHERITED DEBT",
         tagline="할아버지는 빌었고, 아버지는 미뤘다. 청구서는 내게 왔다.",
         instructions="세 번의 결산을 처리하세요. 선대 장부와 주민의 사정을 조사하면 기억을 빼앗지 않는 계약 변경이 열립니다.",
         question="단순한 자원 최적화보다 어떤 대가를 남기는지 고민하게 되는가?",
         colors=["#1d201b","#2b3027","#eee5cc","#d2b578","#b7b59f"],values=[7,3,3],
         items=[item("contract","마르지 않는 우물","약조 제17호 · 풍년력 83년","대가: 매 결산에 한 가족의 소중한 기억 하나"),item("mae","임매월","제례 음식을 차리는 집","지난해에도 어머니의 얼굴을 바쳤어요."),item("receipt","지난해 납부 영수증","선대의 붉은 취소선","이미 납부한 대가가 올해 장부에 다시 올라 있다.")]),
    dict(slug="rhythm-your-tempo",kind="baton",title="당신의 박자",english="A BREATH BETWEEN FOUR",
         tagline="들리지 않아도, 함께 시작할 수 있다.",
         instructions="밝은 구간에 박자가 들어올 때 큐를 보내세요. 무음 시각 리듬입니다. 연습 모드에서는 박자를 직접 한 칸씩 이동할 수 있습니다.",
         question="소리 없이 준비 동작과 박자만으로 합주의 성취를 느낄 수 있는가?",
         colors=["#151719","#22272b","#f1eee4","#dfbd75","#aeb9bc"],values=[2400,8,2,3],
         items=[item("violin1","제1 바이올린","활을 들고 당신의 시선을 기다립니다.","들숨 뒤 진입"),item("violin2","제2 바이올린","긴장한 손끝이 조금 빠르게 움직입니다.","기다렸다 진입"),item("viola","비올라","다른 단원의 활을 보며 호흡합니다.","시선으로 진입"),item("cello","첼로","마지막 저음을 준비합니다.","긴 호흡으로 진입")]),
    dict(slug="narrative-redline",kind="editor",title="붉은 펜으로 남긴 것",english="WHAT THE RED PEN LEFT",
         tagline="문장을 고치는 일은, 누군가를 남기는 일.",
         instructions="원고 위의 편집 도구로 문장을 지우고 문단을 옮기고 표현을 교체하세요. 원고를 돌려보내 작가의 답장을 읽습니다.",
         question="문장의 이해도와 작가의 목소리를 서로 다른 가치로 느끼는가?",
         colors=["#e7e1d5","#f7f2e8","#292723","#ad413d","#71675e"],values=[3],
         items=[item("p1","첫 문단","그날 저녁, 바다가 접혔다. 나는 식탁에 두 개의 그릇을 놓았다.","낯선 표현"),item("p2","어머니의 말","어머니는 늘 말했다. 돌아오는 사람의 밥은 식혀 두지 않는 거라고.","작가가 남겨 달라고 부탁한 문장"),item("p3","마지막 문단","어머니가 죽은 지 사흘째였다. 나는 아직 한 사람의 저녁을 차릴 줄 몰랐다.","사건의 사실")]),
    dict(slug="narrative-last-address",kind="apartment",title="마지막 이삿날",english="THE LAST ADDRESS",
         tagline="내일이면 주소가 사라진다. 오늘은 아직 이웃이다.",
         instructions="세 집의 문에 귀를 기울여 사정을 알아보세요. 작은 도움에 시간을 쓴 뒤 마지막 이삿차에 태울 한 가구를 정합니다.",
         question="마지막 선택보다 그전에 건넨 작은 도움이 기억에 남는가?",
         colors=["#262a2b","#363b3b","#efe4cd","#dfa677","#bdc0b4"],values=[8],
         items=[item("301","301호 · 정복만","상자를 끄는 소리. 혼자서는 장롱을 문밖으로 꺼낼 수 없다.","장롱을 함께 옮기기"),item("302","302호 · 김여진","고양이를 부르는 소리. 이삿짐 사이에서 반려묘가 보이지 않는다.","계단 아래 고양이 찾기"),item("303","303호 · 한난실","아무 소리도 없다. 짐을 쌀 상자도, 갈 곳까지 옮길 차도 없었다.","상자와 테이프 가져오기")]),
    dict(slug="puzzle-prayer-office",kind="prayer",title="기적 배정과",english="THE MIRACLE ALLOCATION OFFICE",
         tagline="한 사람의 기적이, 다른 사람의 청구서가 되지 않도록.",
         instructions="오전과 오후의 날씨를 배정하세요. 하류 주민의 편지를 읽어 수문을 먼저 조정한 뒤 배정표를 발송합니다.",
         question="기도를 승인하는 것보다 서로의 조건을 연결하는 과정이 재미있는가?",
         colors=["#dce7e6","#f3f3e9","#203b43","#a7683f","#627c7e"],values=[2],
         items=[item("farmer","농부의 기도","오늘 하루 중 한 번만 비를 내려 주세요.","오전 또는 오후에 비"),item("performer","공연자의 기도","오후 공연 때만큼은 하늘이 맑았으면 해요.","오후 맑음"),item("downstream","하류에서 온 편지","미개봉 · 같은 유역에서 도착","수문은 오전 강우 전에 열어야 한다.")]),
]

PROJECT = '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><Nullable>enable</Nullable>
    <IsPackable>false</IsPackable><EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <BaseOutputPath>$(MSBuildThisFileDirectory)..\\TestBuild\\</BaseOutputPath>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../src/**/*.cs" />
    <Compile Include="../../../kit/logic/ConceptGraph.cs" />
    <Compile Include="../../../kit/logic/ConceptGraphTests.cs" />
    <Compile Include="*.cs" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="NUnit" Version="3.14.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  </ItemGroup>
</Project>
'''

def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")

def main():
    manifest=[]
    for c in CONCEPTS:
        c=dict(c)
        colors=c.pop("colors")
        c["palette"]=dict(zip(["background","surface","paper","accent","muted"],colors))
        root=ROOT/"games"/c["slug"]
        write(root/"data/scenario.json",json.dumps(c,ensure_ascii=False,indent=2)+"\n")
        write(root/"presentation/palette.json",json.dumps(c["palette"],ensure_ascii=False,indent=2)+"\n")
        write(root/"tests"/(c["slug"]+".Tests.csproj"),PROJECT)
        write(root/"presentation/index.html",f'''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{c['title']} — 플레이 가능한 PoC</title><link rel="icon" href="data:,">
<link rel="stylesheet" href="../../../kit/presentation/concept.css">
</head><body><main id="app"><p class="loading">{c['title']}의 장부를 펼치는 중…</p></main>
<script src="../../../kit/presentation/concept.js" defer></script></body></html>\n''')
        write(root/"README.md",f'''# {c['title']} · {c['slug']}

**한 줄**: {c['tagline']}
**장르 표기**: {c['kind']} · 내러티브 · 싱글플레이
**상태**: 슬라이스 — 순수 C# 규칙 + JSON + 플레이 가능한 HTML
**판정**: 2026-09-27. 기계 검증과 사람의 재미 판정은 별개. 사람의 판정은 미실시.
**검증**: `C:/Users/PSI/.dotnet/dotnet.exe test games/{c['slug']}/tests/{c['slug']}.Tests.csproj`
**다음**: {c['question']}

## 플레이

저장소 루트에서 `python -m http.server 8000 --bind 127.0.0.1` 실행 후
`http://127.0.0.1:8000/games/{c['slug']}/presentation/index.html`.

{c['instructions']}

## 실제 구현 범위

짧은 한 장면의 핵심 조작과 복수 결과. 앞선 기획의 전체 캠페인이 아니다.
게임 규칙은 `src/Rules.cs`, 문구·초기값은 `data/scenario.json`에 있다.
테스트가 모든 도달 가능한 상태를 검사하고 `data/graph.json`으로 내보낸다.
브라우저는 이 그래프를 따라가므로 규칙을 JavaScript로 재구현하지 않는다.
난수·네트워크 API·외부 계정·음성 합성·유료 에셋을 사용하지 않는다.
`다시 시작`은 모든 진행을 초기화하며 세이브는 제공하지 않는다.

## 재생성

`python kit/tools/scaffold_revised_concepts.py` — 시나리오·엔트리·메타데이터 재생성.
그 뒤 위 테스트 명령으로 상태 그래프를 재생성한다.
`node kit/tools/verify_revised_pocs.js` — 브라우저 조작 검증과 갤러리 PNG 재생성.
공유 UI는 `kit/presentation/concept.js`와 `concept.css`.
재미 질문: **{c['question']}**
''')
        write(root/"CREDITS.md","# 출처\n\n이 PoC를 위해 작성한 텍스트·코드·CSS·SVG 도형을 사용한다. 외부 이미지·음악·폰트 파일은 포함하지 않는다. 설치된 시스템 글꼴을 사용한다.\n")
        manifest.append({k:c[k] for k in ["slug","kind","title","english","tagline","question"]})
    write(ROOT/"docs/poc-gallery/revised-concepts.json",json.dumps(manifest,ensure_ascii=False,indent=2)+"\n")
    print("Scaffolded",len(manifest),"revised concepts (handwritten rules preserved).")

if __name__=="__main__":main()
