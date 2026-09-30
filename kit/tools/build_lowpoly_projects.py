"""Synchronize canonical rules and presentation into two reproducible Unity projects."""
from pathlib import Path
import shutil, json, hashlib
ROOT=Path(__file__).resolve().parents[2]
def copy(src,dst):
    dst.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(src,dst)
# **farm-erosion 은 다른 저장소로 갔다** (github.com/tkddls8848/game).
# 여기서 구울 수 있는 것은 지도 퍼즐 하나뿐이다.
for slug,mode in [('puzzle-tomorrow-map','map')]:
    project=ROOT/'games'/slug/'unity'
    for folder in ['Assets/Resources','Assets/Runtime','Assets/Editor','ProjectSettings','Packages']:
        (project/folder).mkdir(parents=True,exist_ok=True)
    # 에디터가 한 번 열리면 여기에 리비전 해시(m_EditorVersionWithRevision)를 덧붙인다.
    # 매번 덮어쓰면 그 줄이 사라져 실행할 때마다 git 이 변경으로 잡는다. 없을 때만 만든다.
    version=project/'ProjectSettings/ProjectVersion.txt'
    if not version.exists(): version.write_text('m_EditorVersion: 6000.0.81f1\n')
    modules=['animation','audio','imageconversion','imgui','jsonserialize','physics','screencapture','ui','uielements']
    (project/'Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.'+x:'1.0.0' for x in modules}},indent=2))
    (project/'Assets/Resources/mode.txt').write_text(mode)
    # kit 에서 온 .cs/.shader 를 복사한다.
    #
    # **복사만 하면 안 된다.** kit 에서 파일을 지우거나 쪼갰을 때 프로젝트에 옛 사본이 남아
    # 같은 partial class 가 두 번 정의되고 `error CS0111` 로 터진다. 한 번 겪었다 —
    # Art.cs 를 구역별로 쪼갰더니 옛 Art.cs 가 남아 15개 중복 정의가 났다.
    # 그래서 kit 에서 온 것들의 현재 목록을 만들고, 그 밖의 kit 유래 파일은 지운다.
    fresh=set()
    for src in (ROOT/'kit/unity-lowpoly').rglob('*'):
        if src.suffix not in ('.cs','.shader'): continue
        dst=project/'Assets'/src.relative_to(ROOT/'kit/unity-lowpoly')
        copy(src,dst); fresh.add(dst.resolve())
    # **정리 대상은 kit 이 실제로 쓰는 자리뿐이다.** kit 의 배치는 Runtime/ 과 Editor/ 바로 아래
    # 평평하므로, 하위 폴더는 프로젝트 전용이다(예: puzzle-tomorrow-map 의 ReferenceEdition/).
    # rglob 으로 훑었다가 추적 중이던 프로젝트 전용 파일을 지운 적이 있다 - glob 로 한 단계만 본다.
    for folder in ('Assets/Runtime','Assets/Editor'):
        for stale in (project/folder).glob('*'):
            if stale.suffix not in ('.cs','.shader'): continue
            if stale.resolve() not in fresh:
                stale.unlink()
                meta=stale.with_suffix(stale.suffix+'.meta')
                if meta.exists(): meta.unlink()
                print('  pruned',stale.relative_to(project))
    # **규칙 코드는 farm-erosion 의 것이었고 그 게임은 다른 저장소로 갔다.**
    # Assets/Runtime/Rules 에 있는 것은 갈라서던 시점의 사본이다 — 컴파일도 실행도 되지만
    # 갱신되지 않는다. 고쳐야 하면 두 저장소를 나란히 두고 RULES 를 그쪽 src 로 바꾼다.
    RULES=ROOT/'games/farm-erosion/src'
    if not RULES.exists():
        print('  규칙 원본이 없다 — Assets/Runtime/Rules 의 사본을 그대로 쓴다'); continue
    for src in RULES.rglob('*.cs'):
        if src.name=='DataLoader.cs' or 'Report' in src.parts: continue
        copy(src,project/'Assets/Runtime/Rules'/src.relative_to(RULES))
    for src in (ROOT/'games'/slug/'data').glob('*.json'):
        copy(src,project/'Assets/Resources/Data'/src.name)
    for folder,name,content in [('Runtime','Lowpoly.Runtime',{'name':'Lowpoly.Runtime'}),('Editor','Lowpoly.Editor',{'name':'Lowpoly.Editor','references':['Lowpoly.Runtime'],'includePlatforms':['Editor']})]:
        (project/f'Assets/{folder}/{name}.asmdef').write_text(json.dumps(content))
    font=ROOT/'AssetDownloads/concepts/last-tenants/fonts'
    copy(font/'gothica1-GothicA1-Regular.ttf',project/'Assets/Resources/Interface.ttf')
    copy(font/'gothica1-OFL.txt',project/'Assets/ThirdParty/GothicA1-OFL.txt')
    records=[]
    for pack,model,alias in [('city-kit-suburban','building-type-a','house'),('mini-forest','tree','tree'),('mini-forest','fence','fence')]:
        base=ROOT/'AssetDownloads/library/kenney'/pack/'contents'
        src=base/'Models/FBX format'/f'{model}.fbx'
        copy(src,project/'Assets/Resources/Models'/f'{alias}.fbx')
        records.append({'source':str(src.relative_to(ROOT)),'destination':f'Assets/Resources/Models/{alias}.fbx','sha256':hashlib.sha256(src.read_bytes()).hexdigest(),'license':'CC0'})
        copy(base/'License.txt',project/'Assets/ThirdParty'/f'{pack}-License.txt')
        # Keep separate palettes: material remapping is handled by the editor importer.
        copy(base/'Models/FBX format/Textures/colormap.png',project/'Assets/Resources/Palettes'/f'{alias}.png')
    (project/'Assets/ThirdParty/manifest.json').write_text(json.dumps(records,indent=2))
    print(project)
