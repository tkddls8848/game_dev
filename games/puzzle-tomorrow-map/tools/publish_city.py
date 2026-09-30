"""Convert Cycles HDR lightmaps to the approved view transform and compact Unity meshes.
Run in Blender after the bake. Does not modify the editable .blend source.
"""
import bpy,json,struct,gzip,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];GAME=ROOT/'games/puzzle-tomorrow-map';RES=GAME/'unity/Assets/Resources/ReferenceCity';RAW=GAME/'art/bake-source';RAW.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(GAME/'art/reference-city.blend'))
scene=bpy.context.scene;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB';scene.render.image_settings.color_depth='16'
for group in ['city','desk','vehicle']:
    source=RES/(group+'.exr')
    if not source.exists():source=RAW/(group+'.exr')
    image=bpy.data.images.load(str(source),check_existing=False)
    image.save_render(str(RES/(group+'.png')),scene=scene);bpy.data.images.remove(image)
    path=RES/(group+'.json')
    if not path.exists():path=RAW/(group+'.json')
    data=json.loads(path.read_text())
    with gzip.open(RES/(group+'.bytes'),'wb',compresslevel=6) as f:
        f.write(struct.pack('<i',len(data['positions'])//3))
        for key in ['positions','normals','uv']:f.write(struct.pack('<'+'f'*len(data[key]),*data[key]))
        f.write(struct.pack('<i',len(data['triangles'])));f.write(struct.pack('<'+'i'*len(data['triangles']),*data['triangles']))
    for ext in ['exr','json']:
        source=RES/(group+'.'+ext)
        if source.exists():shutil.move(str(source),str(RAW/source.name))
    print('PUBLISHED',group,flush=True)
print('DONE',flush=True)
