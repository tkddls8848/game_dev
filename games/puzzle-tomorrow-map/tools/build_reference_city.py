"""Author the reference diorama in Blender, render reviews, and bake real Unity meshes.
blender -b -t 8 --python build_reference_city.py -- --preview
blender -b -t 8 --python build_reference_city.py -- --bake
No reference image is used as scene geometry or a runtime background.
"""
import bpy, math, random, json, sys, argparse
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'games/puzzle-tomorrow-map/art'
RES=ROOT/'games/puzzle-tomorrow-map/unity/Assets/Resources/ReferenceCity'
OUT.mkdir(parents=True,exist_ok=True);RES.mkdir(parents=True,exist_ok=True)
ap=argparse.ArgumentParser();ap.add_argument('--bake',action='store_true');ap.add_argument('--preview',action='store_true');ap.add_argument('--samples',type=int,default=48)
args=ap.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
random.seed(934)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
GROUP='city';objects={'city':[],'desk':[],'vehicle':[]};batches={}
def pos(p):return (-p[0],-p[2],p[1])
def rgba(h):
    rgb=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(((v+.055)/1.055)**2.4 if v>.04045 else v/12.92 for v in rgb)+(1,)
def mat(name,color,noise=0,scale=15,emission=0,rough=.78):
    m=bpy.data.materials.new(name);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;bs=n.get('Principled BSDF');col=rgba(color);bs.inputs['Base Color'].default_value=col;bs.inputs['Roughness'].default_value=rough
    if emission:bs.inputs['Emission Color'].default_value=col;bs.inputs['Emission Strength'].default_value=emission
    if noise:
        tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=scale;tex.inputs['Detail'].default_value=3
        ramp=n.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.15;ramp.color_ramp.elements[0].color=tuple(v*(1-noise) for v in col[:3])+(1,);ramp.color_ramp.elements[1].position=.85;ramp.color_ramp.elements[1].color=col;l.new(tex.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs[0],bs.inputs['Base Color'])
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.018;l.new(tex.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],bs.inputs['Normal'])
    return m
plaster=[mat('Lime plaster '+str(i),c,.24,20) for i,c in enumerate(['cec6b2','bebaa8','d4cbb7','bcbba9'])]
stone=mat('Aged cut limestone','999888',.27,28);curb=mat('Pavement curb','aaa99a',.22,24);iron=mat('Painted dark iron','38463f',.12,13,rough=.43)
timber=mat('Weathered timber','6e543b',.35,7);green=mat('Muted green shop awning','456c5c',.13,23)
glass=mat('Blue grey recessed glass','263b3d',0,rough=.24);lit=mat('Warm interior windows','ffd98d',0,emission=.7);frame=mat('Painted window wood','777b6c',.12,20)
roofmats=[mat('Terracotta tile '+str(i),c,.15,38) for i,c in enumerate(['975d43','9e6246','a2684a','8e573f','a26a50'])]
leaves=[mat('Foliage '+str(i),c,.10,9) for i,c in enumerate(['526748','647747','7a844c','466243','6e7a49'])]
trunk=mat('Bark','665338',.3,7);pot=mat('Clay planters','927551',.3,20);soil=mat('Soil','4d4935',.2,22);paper=mat('Drawing paper','d6ccb1',.15,55);inkmat=mat('Ink','5c675e');road=mat('Fine asphalt','656d67',.32,50);stripe=mat('Road markings','c4c4ae',.18,30)
def register(o):objects[GROUP].append(o);o['group']=GROUP;return o
def mesh(name,vs,fs,material):
    me=bpy.data.meshes.new(name);me.from_pydata([pos(v) for v in vs],[],fs);me.materials.append(material);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);return register(o)
def box(name,p,s,m,bevel=0,angle=0):
    x,y,z=p;w,h,d=s;vs=[]
    for xx,yy,zz in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]:
        dx=xx*w/2;dz=zz*d/2;a=math.radians(angle);vs.append((x+dx*math.cos(a)-dz*math.sin(a),y+yy*h/2,z+dx*math.sin(a)+dz*math.cos(a)))
    # Coordinate conversion mirrors X to preserve Unity camera handedness.
    fs=[(0,4,6,2),(5,1,3,7),(1,0,2,3),(4,5,7,6),(2,6,7,3),(1,5,4,0)]
    if bevel:
        o=mesh(name,vs,fs,m);mod=o.modifiers.new('Tiny softened edges','BEVEL');mod.width=bevel;mod.segments=2;return o
    key=(GROUP,m.name);entry=batches.setdefault(key,[[],[],m]);offset=len(entry[0]);entry[0].extend(vs);entry[1].extend(tuple(i+offset for i in f) for f in fs)
def flush():
    global GROUP
    old=GROUP
    for (g,n),(v,f,m) in batches.items():GROUP=g;mesh(n+' details',v,f,m)
    batches.clear();GROUP=old
def rod(name,a,b,r,m,vertices=8):
    av,bv=Vector(pos(a)),Vector(pos(b));delta=bv-av
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=delta.length,location=(av+bv)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=delta.to_track_quat('Z','Y').to_euler();o.data.materials.append(m);register(o);return o
def ico(name,p,s,m,sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=pos(p));o=bpy.context.object;o.name=name;o.scale=(s[0],s[2],s[1]);o.data.materials.append(m);register(o);return o
fontpath=ROOT/'AssetDownloads/concepts/curse-ledger/fonts/GowunBatang-Regular.ttf'
font=bpy.data.fonts.load(str(fontpath))
def text(name,words,p,size,m,rotation=(90,0,180)):
    cu=bpy.data.curves.new(name,'FONT');cu.body=words;cu.font=font;cu.size=size;cu.align_x='CENTER';cu.align_y='CENTER';cu.extrude=.0004;cu.resolution_u=2;o=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(o);o.location=pos(p);o.rotation_euler=tuple(math.radians(a) for a in rotation);cu.materials.append(m);return register(o)
def window(x,y,z,w=.28,h=.43,on=False,side=False,balcony=False):
    # Recess shadow, inner glazing, narrow frame and sill; never thick toy-like borders.
    box('Window recess',(x,y,z),(.025 if side else w+.075,h+.06,w+.075 if side else .025),iron)
    box('Glass pane',(x+.018 if side else x,y,z if side else z-.018),(.018 if side else w,h,w if side else .018),lit if on else glass)
    for q in [-1,1]:
        box('Thin vertical jamb',(x+.035 if side else x+q*w/2,y,z+q*w/2 if side else z-.035),(.035 if side else .026,h+.035,.026 if side else .035),frame)
    box('Window mullion',(x+.041 if side else x,y,z if side else z-.041),(.025,.01 if False else h,.022) if not side else (.022,h,.025),frame)
    box('Sill',(x,y-h/2-.036,z),(.11 if side else w+.12,.06,w+.12 if side else .11),stone,.006)
    if balcony and not side:
        box('Balcony slab',(x,y-h/2-.06,z-.16),(w+.24,.065,.34),stone,.01);rail((x-w/2-.09,y-h/2,z-.30),(x+w/2+.09,y-h/2,z-.30),.24)
def rail(a,b,h=.36):
    n=max(1,int(Vector(b).to_3d().__sub__(Vector(a)).length/.12))
    for i in range(n+1):
        p=Vector(a).lerp(Vector(b),i/n);box('Iron baluster',(p.x,p.y+h/2,p.z),(.018,h,.018),iron)
    rod('Railing handrail',(a[0],a[1]+h,a[2]),(b[0],b[1]+h,b[2]),.018,iron)
def roof(x,y,z,w,d,pitch=.4,hip=False):
    # Small overlapping tiles follow the two slopes. Ceramic variation is per tile.
    for side in [-1,1]:
        rows=max(3,int(w/.22));cols=max(4,int(d/.09));half=w/2
        for r in range(rows):
            t0=r/rows;t1=min(1,(r+1.13)/rows);xx0=x+side*t0*half;xx1=x+side*t1*half;yy0=y+pitch*(1-t0);yy1=y+pitch*(1-t1)
            for c in range(cols):
                zz=z-d/2+c*d/cols;dz=d/cols*.96
                
                def roof_y(xx,zz):
                    tx=abs(xx-x)/(w/2); edge=min(zz-(z-d/2),z+d/2-zz)
                    return y+pitch*max(0,min(1-tx,edge/(w*.45)))+.013 if hip else y+pitch*(1-tx)+.013
                vs=[(xx0,roof_y(xx0,zz),zz),(xx1,roof_y(xx1,zz),zz),(xx1,roof_y(xx1,zz+dz),zz+dz),(xx0,roof_y(xx0,zz+dz),zz+dz)]
                if side<0:vs.reverse()
                key=(GROUP,roofmats[(r*7+c)%5].name);entry=batches.setdefault(key,[[],[],roofmats[(r*7+c)%5]]);off=len(entry[0]);entry[0].extend(vs);entry[1].append(tuple(off+i for i in range(4)))
        rod('Eave gutter',(x+side*half,y-.025,z-d/2),(x+side*half,y-.025,z+d/2),.028,iron)
    if not hip:rod('Roof ridge',(x,y+pitch+.025,z-d/2),(x,y+pitch+.025,z+d/2),.04,roofmats[2])
    for zz in ([] if hip else [z-d/2+.04,z+d/2-.04]):mesh('Plaster gable',[(x-w/2+.08,y,zz),(x+w/2-.08,y,zz),(x,y+pitch-.03,zz)],[(0,1,2)] if zz<z else [(2,1,0)],plaster[0])
    box('Chimney',(x+w*.25,y+pitch+.17,z+.25),(.22,.48,.25),plaster[2],.012);box('Chimney rim',(x+w*.25,y+pitch+.42,z+.25),(.27,.06,.30),stone,.008)
def planter(x,y,z,w=.3):
    box('Planter',(x,y+.12,z),(w,.24,.30),pot,.02);box('Planter soil',(x,y+.247,z),(w*.88,.008,.24),soil)
    for j in range(max(1,int(w/.16))):ico('Shrub',(x-w*.33+j*.16,y+.35,z),(.12,.19,.13),leaves[j%5])
def tree(x,z,s=1,y=.42):
    rod('Tree trunk',(x,y,z),(x+.04,y+1.35*s,z),.065*s,trunk)
    rod('Tree branch',(x,y+.72*s,z),(x-.28*s,y+1.22*s,z+.05),.025*s,trunk)
    ico('Low-poly tree canopy',(x,y+1.56*s,z),(.48*s,.72*s,.43*s),leaves[1]);ico('Canopy side',(x-.21*s,y+1.28*s,z+.08),(.29*s,.42*s,.3*s),leaves[2])
    box('Tree paving cutout',(x,y-.025,z),(.54,.03,.54),soil)
def house(x,z,w,d,floors,kind):
    base=.54;h=floors*.72
    box('Sidewalk podium',(x,.38,z),(w+.52,.27,d+.50),stone,.022)
    box('Weathered facade',(x,base+h/2,z),(w,h,d),plaster[kind%4],.016)
    box('Stone damp course',(x,.63,z),(w+.035,.18,d+.035),stone,.006)
    flat=kind==2
    if flat:
        box('Flat roof',(x,base+h+.04,z),(w+.06,.12,d+.06),roofmats[2],.012)
        for s in [-1,1]:box('Parapet',(x+s*w/2,base+h+.17,z),(.08,.22,d+.10),plaster[1],.01)
        box('Roof parapet',(x,base+h+.17,z+d/2),(w,.22,.08),plaster[1],.01)
        box('Roof vent',(x+.37,base+h+.25,z+.1),(.25,.26,.3),stone,.012)
    else:roof(x,base+h,z,w+.18,d+.17,.36 if floors<4 else .32,hip=kind in (1,3,4,5))
    for f in range(floors):
        yy=base+.42+f*.72
        for j in range(2 if w<1.8 else 3):
            n=2 if w<1.8 else 3;wx=x+(j-(n-1)/2)*(w*.68/max(1,n-1))
            window(wx,yy,z-d/2-.014,on=(f+j+kind)%5==1,balcony=(kind==2 and f in (1,3) and j==0))
        for j in range(2):window(x+w/2+.015,yy,z+(-.27+j*.54)*d,side=True,on=(f+j+kind)%6==2)
    rod('Downpipe',(x-w/2+.03,.54,z-d/2-.035),(x-w/2+.03,base+h,z-d/2-.035),.026,frame)
    # Doors and frontage vary by building rather than repeating one shop everywhere.
    box('Entrance door',(x+.16,.98,z-d/2-.032),(.36,.85,.05),green,.006)
    box('Door glass',(x+.16,1.10,z-d/2-.066),(.27,.37,.012),lit if kind%2 else glass)
    box('Door step',(x+.16,.58,z-d/2-.2),(.66,.12,.36),stone,.015)
    if kind in (1,4,5):
        box('Shop sign',(x,1.40,z-d/2-.08),(w*.92,.23,.08),green,.006)
        text('Shop lettering',['','','','','초록상점','작은서점'][kind] if kind!=1 else '동네 공방',(x,1.41,z-d/2-.132),.13,paper)
        if kind in (1,4):
            box('Canvas awning',(x,1.64,z-d/2-.27),(w*.94,.07,.50),green,.015)
            for j in range(6):box('Awning stitch',(x-w*.40+j*w*.16,1.68,z-d/2-.26),(.016,.008,.44),frame)
    if kind==1:
        box('Long balcony',(x,1.95,z-d/2-.17),(w+.05,.075,.37),stone,.014);rail((x-w/2,2.0,z-d/2-.34),(x+w/2,2.0,z-d/2-.34),.30)
    if kind==2:
        text('Facade vertical lettering','함께\n사는\n세상',(x+w/2+.03,2.33,z+.05),.18,iron,(90,0,90))
    for side in [-1,1]:planter(x+side*(w/2-.15),.55,z-d/2-.33,.34)
    # Apartment garden retaining wall and rail.
    if kind==2:
        box('Garden wall',(x,.61,z-d/2-.57),(w+.51,.54,.12),stone,.018);rail((x-w/2-.2,.89,z-d/2-.57),(x+w/2+.2,.89,z-d/2-.57),.32)
        for j in range(5):planter(x-w*.43+j*w*.21,.56,z-d/2-.36,.22)
def person(x,z,coat,y=.51):
    skin=mat('Skin '+str(len(bpy.data.materials)),random.choice(['bc9672','d0ab83','bc9c7c']),rough=.8)
    cm=coat
    ico('Jacket',(x,y+.37,z),(.095,.175,.072),cm,2);ico('Head',(x,y+.60,z),(.063,.084,.062),skin,2)
    for side in [-1,1]:
        rod('Trouser leg',(x+side*.045,y+.24,z),(x+side*.055,y+.045,z+side*.027),.028,iron)
        rod('Arm',(x+side*.10,y+.46,z),(x+side*.12,y+.23,z+.025),.026,cm)
        box('Shoe',(x+side*.055,y+.025,z-.025),(.066,.047,.13),iron,.013)
def streetlamp(x,z):
    rod('Lamp stem',(x,.43,z),(x,1.83,z),.025,iron);rod('Lamp bracket',(x,1.83,z),(x+.23,1.83,z),.025,iron)
    box('Lamp base',(x,.48,z),(.14,.10,.14),stone,.025);box('Lantern glass',(x+.23,1.77,z),(.14,.18,.14),lit,.018);box('Lantern cap',(x+.23,1.89,z),(.20,.07,.20),iron,.01)

# Desk: directional wood grain, paper sheets, writing tools and out-of-focus props.
GROUP='desk';wood=mat('Walnut desk','795239',.55,4,rough=.47)
n=wood.node_tree.nodes;l=wood.node_tree.links;tex=next(x for x in n if x.type=='TEX_NOISE');mapping=n.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(.25,12,6);coords=n.new('ShaderNodeTexCoord');l.new(coords.outputs['Generated'],mapping.inputs[0]);l.new(mapping.outputs[0],tex.inputs['Vector'])
box('Solid walnut desk',(0,-.29,0),(28,.44,24),wood,.05)
box('Drafting sheet',(.1,-.043,.0),(13.5,.032,12.4),paper)
grid=mat('Pencil grid','a3a392',.1,30)
for j in range(-13,14):
    box('Pencil grid x',(j*.48,-.023,0),(.007,.003,12.3),grid);box('Pencil grid z',(0,-.021,j*.45),(13.4,.003,.007),grid)
text('Printed caption','오늘의 배치가 내일의 이야기가 된다.',(0,-.015,-5.38),.16,inkmat,(0,0,0))
box('Notes sheet',(-6.0,-.01,.8),(2.6,.02,3.55),paper,angle=-12)
text('Checklist','도 로\n주 거\n공공시설\n사 람\n내 일',(-6.0,.011,.8),.22,inkmat,(0,0,12))
for j in range(5):
    box('Checklist square',(-6.78,.025,1.70-j*.43),(.10,.006,.10),inkmat);box('Checklist inner',(-6.78,.03,1.70-j*.43),(.074,.003,.074),paper)
notebook=mat('Charcoal notebook cloth','353c36',.3,48)
box('Notebook',(6.05,.15,-3.1),(2.0,.38,3.3),notebook,.05,angle=-6);box('Notebook paper edge',(6.04,.14,-3.1),(1.96,.24,3.22),paper,.008,angle=-6)
text('Notebook title','사람이\n머무는\n도시',(6.06,.354,-3.1),.22,paper,(0,0,6))
box('Memo card',(4.12,-.003,-5.6),(2.8,.035,1.7),paper,angle=14);text('Memo handwriting','선을 바꾸면,\n내일의 도시가 바뀝니다.',(4.1,.018,-5.58),.19,inkmat,(0,0,-14))
rod('Pencil',(-3.0,.03,-5.80),(1.7,.03,-6.5),.055,iron,12);rod('Pencil metal band',(.8,.03,-6.36),(1,.03,-6.40),.058,roofmats[1],12)
# Large desk plant and pencil cup are behind the model, intentionally outside focus.
cup=mat('Ceramic cup','3b4943',.15,20,rough=.32)
rod('Pencil cup',(5.9,-.04,6.15),(5.9,1.14,6.15),.48,cup,32)
for j in range(8):rod('Pencil in cup',(5.9+random.uniform(-.26,.26),.6,6.15+random.uniform(-.26,.26)),(5.9+random.uniform(-.45,.45),random.uniform(1.5,2.3),6.15+random.uniform(-.4,.4)),.035,timber,8)
rod('Plant pot',(-6.2,-.03,5.8),(-6.2,.72,5.8),.65,cup,20)
for j in range(9):
    a=j*2.4;ico('Desk plant leaf',(-6.2+math.cos(a)*.65,1.0+j*.08,5.8+math.sin(a)*.65),(.5,.16,.83),leaves[j%5],2)

GROUP='city'
box('Miniature city base',(0,.135,0),(10.6,.31,9.75),stone,.025)
pavements=[mat('Paving '+str(i),c,.2,32) for i,c in enumerate(['b5b1a0','afad9e','bdb7a5','aaa99b'])]
for i in range(21):
    for j in range(20):box('Paving square',(-5.05+i*.50,.327,-4.64+j*.47),(.489,.065,.459),pavements[(i*3+j)%4],.004)
box('Main road',(.40,.373,-.05),(1.40,.036,9.35),road)
box('Cross road',(.0,.376,.42),(10.25,.037,1.30),road)
for side in [-1,1]:
    box('Main road curb',(.4+side*.75,.43,-.05),(.1,.15,9.35),curb,.015)
    box('Cross road curb',(0,.43,.42+side*.70),(10.2,.15,.09),curb,.012)
for j in range(18):box('Road dashed centre',(.40,.398,-4.45+j*.51),(.035,.006,.27),stripe)
for j in range(18):box('Cross road dash',(-4.7+j*.54,.399,.42),(.29,.006,.031),stripe)
house(-3.38,1.92,1.62,1.72,4,0)
house(-1.55,3.45,1.94,1.7,2,1)
house(-3.85,-.56,1.60,1.93,4,2)
house(-1.33,-1.15,1.45,1.49,2,3)
house(2.32,-2.68,1.67,1.90,2,4)
house(4.14,-2.52,1.54,1.77,2,5)
# Basement courtyard in front of the small central house.
box('Basement lightwell',(-1.33,.405,-2.60),(1.30,.035,1.05),soil)
for j in range(6):box('Basement stair',(-1.23,.41+j*.033,-2.98+j*.13),(.58,.075,.13),stone,.006)
box('Basement door surround',(-1.34,.70,-2.09),(.74,.74,.10),stone,.012);box('Basement lit doorway',(-1.34,.65,-2.15),(.46,.57,.026),lit)
rail((-2.0,.45,-3.13),(-.65,.45,-3.13),.38);rail((-2.0,.45,-3.13),(-2.0,.45,-2.1),.38)
for j in range(3):planter(-1.90+j*.46,.44,-3.02,.22)
# Distinct municipal hospital with rooftop masses, canopy, upper windows and glazing.
box('Hospital terrace',(2.78,.44,3.00),(3.2,.2,2.65),stone,.018)
box('Hospital main facade',(2.84,1.55,3.16),(2.82,2.06,2.16),plaster[1],.025)
box('Hospital lower canopy',(2.84,1.32,1.97),(2.91,.15,.65),plaster[2],.015)
box('Hospital roof lip',(2.84,2.62,3.16),(2.94,.15,2.29),plaster[0],.014)
box('Hospital roof top',(3.08,2.97,3.53),(1.18,.69,.92),plaster[1],.017)
box('Hospital roof machine',(1.85,2.85,3.68),(.78,.35,.61),plaster[0],.016)
for j in range(4):window(1.85+j*.60,2.18,2.066,w=.32,h=.43)
for j in range(3):window(4.268,1.90,2.50+j*.53,w=.27,h=.46,side=True)
for j in range(5):
    box('Hospital lobby window',(1.95+j*.43,.92,2.028),(.37,.85,.018),lit)
    box('Lobby mullion',(1.74+j*.43,.92,1.998),(.038,.92,.055),iron)
box('Hospital green cross',(2.84,1.91,2.03),(.51,.15,.06),green);box('Hospital green cross',(2.84,1.91,2.00),(.15,.48,.06),green)
text('Hospital sign','시민병원',(2.84,1.57,1.98),.245,iron)
text('Hospital wing sign','응급 진료',(1.86,1.48,1.615),.10,green)
for j in range(4):planter(4.35,.54,2.1+j*.60,.27)
rail((1.15,.52,4.3),(4.43,.52,4.3),.43)
for x,z,s in [(-4.48,-.15,.9),(-4.42,3.70,.88),(-2.03,4.35,.80),(.1,3.88,.78),(4.47,3.74,.95),(4.6,.93,.8),(1.25,-4.12,.72),(4.72,-3.98,.68)]:tree(x,z,s)
for x,z in [(-.44,-3.9),(1.35,-3.85),(-.44,-.47),(1.35,1.33),(-2.32,1.3),(4.8,-.8)]:streetlamp(x,z)
for i,(x,z) in enumerate([(-.50,-3.10),(.99,-3.60),(1.30,-2.55),(1.42,-4.25),(-.5,-.20),(1.3,.95),(-2.2,2.0),(-3.6,1.75),(.95,2.0),(3.65,1.44),(4.65,-2.80),(-2.15,-3.1)]):person(x,z,[green,timber,plaster[1],iron][i%4])
for x,z in [(-2.28,-1.93),(4.55,-4.04)]:
    box('Park bench seat',(x,.67,z),(.65,.07,.26),timber,.012);box('Park bench back',(x,.86,z+.12),(.65,.22,.045),timber,.01)
    for side in [-1,1]:box('Bench leg',(x+side*.25,.53,z),(.045,.28,.23),iron)
# Freestanding traffic sign, utility cabinet, bicycle silhouettes and small frontage props.
rod('Warning sign post',(-2.17,.45,-2.72),(-2.17,1.08,-2.72),.022,iron)
yellow=mat('Warning yellow','e2ae43');mesh('Warning triangle',[(-2.35,1.00,-2.74),(-1.99,1.00,-2.74),(-2.17,1.35,-2.74)],[(0,1,2)],yellow)
text('Warning exclamation','!',(-2.17,1.12,-2.76),.21,iron)
box('Utility cabinet',(.68,.71,4.22),(.35,.64,.30),green,.02)
for x,z in [(-.5,-2.4),(1.27,-1.4)]:
    for dz in [-.23,.23]:
        bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,location=pos((x,.63,z+dz)),major_radius=.16,minor_radius=.014,rotation=(0,math.pi/2,0));o=bpy.context.object;o.data.materials.append(iron);register(o)
    rod('Bicycle frame',(x,.63,z-.23),(x,.83,z),.016,iron);rod('Bicycle frame',(x,.83,z),(x,.63,z+.23),.016,iron)
GROUP='vehicle';ivory=mat('Ambulance paint','d6d4bd',.06,30,rough=.3);red=mat('Ambulance stripe','954c3c');rubber=mat('Tyres','282f2d',rough=.9)
vx,vz=.43,1.60
box('Ambulance shell',(vx,.78,vz),(.58,.53,1.03),ivory,.07)
box('Windscreen',(vx,.88,vz-.524),(.46,.24,.017),glass,.016)
for side in [-1,1]:
    box('Side emergency stripe',(vx+side*.294,.72,vz),(.018,.07,.84),red)
    for j in range(3):box('Van side glazing',(vx+side*.303,.89,vz-.27+j*.27),(.018,.19,.2),glass,.006)
    for dz in [-.32,.32]:rod('Ambulance wheel',(vx+side*.27,.55,vz+dz),(vx+side*.33,.55,vz+dz),.108,rubber,16)
box('Ambulance bumper',(vx,.57,vz-.54),(.53,.07,.08),iron,.01)
for side in [-1,1]:box('Headlamp',(vx+side*.19,.69,vz-.54),(.12,.10,.025),lit,.01)
box('Emergency rooftop light',(vx,1.07,vz+.20),(.35,.07,.15),red,.02)
flush()

# Lighting: broad warm window key, subdued blue-grey fill and warm practical windows.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=args.samples;scene.cycles.use_denoising=True;scene.cycles.max_bounces=6
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='CUDA';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='CUDA'
    scene.cycles.device='GPU'
except Exception:scene.cycles.device='CPU'
world=bpy.data.worlds.new('Muted studio ambience');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.36,.42,.49,1);world.node_tree.nodes['Background'].inputs[1].default_value=.35
def area(name,p,target,power,size,color):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color;o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=pos(p);o.rotation_euler=(Vector(pos(target))-o.location).to_track_quat('-Z','Y').to_euler()
area('Large warm window',(-6,10,1),(0,0,0),1800,5,(1,.80,.56));area('Soft room fill',(5,6,-7),(0,1,0),220,7,(.65,.76,1))
for x,z in [(-1.34,-2.25),(2.84,1.80)]:area('Interior spill',(x,.8,z),(x,.3,z-.6),8,.4,(1,.57,.19))
camera_data=bpy.data.cameras.new('Reference camera');camera=bpy.data.objects.new('Reference camera',camera_data);bpy.context.collection.objects.link(camera)
camera.location=pos((10.2,13.2,-18.5));target=Vector(pos((1.35,1.0,.10)));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera_data.type='ORTHO';camera_data.ortho_scale=19.3;scene.camera=camera
camera_data.dof.use_dof=True;camera_data.dof.focus_distance=(Vector(pos((0,1,0)))-camera.location).length;camera_data.dof.aperture_fstop=5.6
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=.25
scene.render.resolution_x=1672;scene.render.resolution_y=941;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(OUT/'reference-city-review.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-city.blend'))
print('CITY_AUTHORED',sum(len(v) for v in objects.values()),flush=True)
if args.preview or not args.bake:bpy.ops.render.render(write_still=True)
if args.bake:
    # Join each atlas group with evaluated bevels; unwrapped geometry remains real 3D.
    joined={}
    for group,parts in objects.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in parts:o.select_set(True)
        bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();o=bpy.context.object;o.name='Baked '+group
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.0015,area_weight=.8,correct_aspect=True);bpy.ops.object.mode_set(mode='OBJECT');joined[group]=o
        print('UNWRAPPED',group,len(o.data.polygons),flush=True)
    scene.render.bake.use_pass_direct=True;scene.render.bake.use_pass_indirect=True;scene.render.bake.use_pass_color=True;scene.render.bake.use_pass_glossy=False;scene.render.bake.use_pass_transmission=False;scene.render.bake.margin=6
    for group,o in joined.items():
        size=8192 if group=='city' else 4096 if group=='desk' else 1024
        image=bpy.data.images.new(group+' baked lighting',width=size,height=size,alpha=False,float_buffer=True)
        for m in o.data.materials:
            n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=image;m.node_tree.nodes.active=n
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
        print('BAKE_START',group,size,flush=True);bpy.ops.object.bake(type='COMBINED')
        # Save linear HDR as EXR so Unity can preserve lighting before tone mapping.
        image.filepath_raw=str(RES/(group+'.exr'));image.file_format='OPEN_EXR';image.save()
        me=o.data;me.calc_loop_triangles();uv=me.uv_layers.active.data;positions=[];normals=[];uvs=[];indices=[]
        for tri in me.loop_triangles:
            for li in tri.loops:
                co=me.vertices[me.loops[li].vertex_index].co;no=tri.normal
                positions.extend([round(-co.x,6),round(co.z,6),round(-co.y,6)]);normals.extend([round(-no.x,6),round(no.z,6),round(-no.y,6)]);uvs.extend([round(uv[li].uv.x,7),round(uv[li].uv.y,7)]);indices.append(len(indices))
        (RES/(group+'.json')).write_text(json.dumps({'positions':positions,'normals':normals,'uv':uvs,'triangles':indices},separators=(',',':')))
        print('BAKE_EXPORTED',group,len(indices),flush=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'reference-city-baked.blend'))
print('DONE',flush=True)
