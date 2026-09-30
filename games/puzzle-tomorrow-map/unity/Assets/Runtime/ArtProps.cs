using System;
using System.Collections.Generic;
using UnityEngine;
using FarmErosion.Sim;

// 구역 E — 나무 · 풀포기 · 울타리 · 집 · 작물.
//
// **이 파일의 유일한 목표는 실루엣이다.** 멀리서 한 칸만 봐도 무엇이 심겼는지 알아야 한다.
// 이전 판은 나무가 노란 덩어리 하나였고 작물은 종류와 상관없이 창백한 막대였다.
// 시안(docs/poc-gallery/lowpoly/farm-erosion-v1.png)은 양배추·옥수수·호박·밀·새싹이
// 색이 아니라 **형태**로 갈린다. 색만 바꾸면 회색조에서 다시 같아진다.
//
// **2026-09-28 라운드: 덜어내기.** 시안의 편안함은 오브젝트가 아니라 여백에서 온다 —
// 밭마다 흙고랑이 보이고 빈 잔디는 그냥 비어 있다. 이전 판은 칸마다 뭔가로 꽉 차 눈이 쉴 곳이 없었다.
// 이 파일의 밀도 수치를 올릴 때는 "무엇이 더 보이나"가 아니라 "흙이 아직 보이나"를 먼저 본다.
//
// 한 칸을 오브젝트 수십 개로 세우면 Rebuild() — 클릭·하루 진행마다 돈다 — 가 무거워진다.
// 그래서 삼각형을 Scrap 에 쌓아 두고 MeshObject 를 칸마다 한 번만 부른다.
//
// Art.cs 한 파일에 바다·섬·소품이 모두 있어서 여러 사람이 동시에 손댈 수 없었다.
// partial class 라 파일만 갈라도 소유권이 갈린다. **구역 밖 메서드를 이 파일로 옮기지 말 것** —
// 옮기는 순간 다시 한 파일이 되고 병렬 작업이 충돌한다.
public sealed partial class LowpolyGame
{
    // 밭 흙의 윗면. 밭 타일(.755+.075) 위에 경작지(.846+.0175)와 고랑이 얹혀 있다.
    const float cropSoilY=.886f;

    // ── 삼각형 적재기 ────────────────────────────────────────────────────────────
    sealed class Scrap
    {
        public readonly List<Vector3> verts=new List<Vector3>();
        public readonly List<int> tris=new List<int>();
        public readonly List<Color> cols=new List<Color>();
        public bool Empty=>verts.Count==0;
    }
    static Color Shade(Color c,float k){var x=c*k;x.a=1;return x;}
    void Tri(Scrap s,Vector3 a,Vector3 b,Vector3 c,Color col)=>Tri(s.verts,s.tris,s.cols,a,b,c,col);
    void Quad(Scrap s,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color col){Tri(s,a,b,c,col);Tri(s,a,c,d,col);}
    /// <summary>양면 사각형. 저폴리 잎은 두께가 없어 뒷면을 함께 그려야 보인다 — 뒷면은 어둡게.</summary>
    void Quad2(Scrap s,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color face,Color back){Quad(s,a,b,c,d,face);Quad(s,a,d,c,b,back);}

    /// <summary>임의 축의 각기둥. 줄기 · 가지 · 둥치 · 이삭을 하나의 메시에 쌓는다.</summary>
    void Stalk(Scrap s,Vector3 a,Vector3 b,float ra,float rb,Color col,int sides=4,bool cap=true)
    {
        Vector3 up=b-a; float len=up.magnitude; if(len<1e-4f)return; up/=len;
        Vector3 right=Vector3.Cross(up,Vector3.forward); if(right.sqrMagnitude<1e-5f)right=Vector3.Cross(up,Vector3.right);
        right.Normalize(); Vector3 fwd=Vector3.Cross(right,up);
        for(int i=0;i<sides;i++)
        {
            float a0=i*Mathf.PI*2/sides,a1=(i+1)*Mathf.PI*2/sides;
            Vector3 d0=right*Mathf.Cos(a0)+fwd*Mathf.Sin(a0),d1=right*Mathf.Cos(a1)+fwd*Mathf.Sin(a1);
            // 한 기둥 안에서도 면마다 밝기가 갈려야 원통이 아니라 깎은 각재로 보인다.
            Color face=Shade(col,.86f+.26f*(Mathf.Cos(a0-.7f)*.5f+.5f));
            Tri(s,a+d0*ra,b+d0*rb,a+d1*ra,face); Tri(s,a+d1*ra,b+d0*rb,b+d1*rb,face);
            if(cap&&rb>.002f)Tri(s,b+d0*rb,b+up*rb*.55f,b+d1*rb,Shade(col,1.14f));
        }
    }

    /// <summary>휘어지는 잎날. 뿌리에서 넓고 끝으로 갈수록 좁아진다 — 잎이 있어야 종류가 읽힌다.</summary>
    void Leaf(Scrap s,Vector3 root,Vector3 ctrl,Vector3 tip,float width,Color face,Color back,int seg=3)
    {
        Vector3 flat=tip-root; flat.y=0; if(flat.sqrMagnitude<1e-5f)flat=Vector3.forward;
        Vector3 side=Vector3.Cross(flat.normalized,Vector3.up).normalized*width*.5f;
        Vector3 prev=root; float prevW=1;
        for(int i=1;i<=seg;i++)
        {
            float t=(float)i/seg;
            Vector3 p=Vector3.Lerp(Vector3.Lerp(root,ctrl,t),Vector3.Lerp(ctrl,tip,t),t);
            float w=Mathf.Max(.07f,1-t*t);
            Quad2(s,prev-side*prevW,prev+side*prevW,p+side*w,p-side*w,Shade(face,1-.05f*i),back);
            prev=p; prevW=w;
        }
    }

    /// <summary>면이 깎인 덩어리. 수관 층 · 양배추 통 · 호박 열매가 모두 이것이다.</summary>
    void Ball(Scrap s,Vector3 c,Vector3 r,Color top,Color bottom,int sides=7,float jitter=.10f)
    {
        var low=new Vector3[sides];var mid=new Vector3[sides];var high=new Vector3[sides];
        for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides; Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            low[i] =c+Vector3.Scale(d*(.58f+R(-jitter,jitter)),r)-Vector3.up*r.y*.62f;
            mid[i] =c+Vector3.Scale(d*(1f+R(-jitter,jitter)),r);
            high[i]=c+Vector3.Scale(d*(.56f+R(-jitter,jitter)),r)+Vector3.up*r.y*.66f;
        }
        Vector3 apex=c+Vector3.up*r.y, nadir=c-Vector3.up*r.y*.94f;
        Color waist=Color.Lerp(bottom,top,.55f);
        for(int i=0;i<sides;i++)
        {
            int j=(i+1)%sides;
            Color lo=Shade(bottom,R(.90f,1.08f)),me=Shade(waist,R(.90f,1.08f)),hi=Shade(top,R(.93f,1.10f));
            Tri(s,low[j],nadir,low[i],lo);
            Tri(s,low[i],mid[i],low[j],lo); Tri(s,low[j],mid[i],mid[j],lo);
            Tri(s,mid[i],high[i],mid[j],me); Tri(s,mid[j],high[i],high[j],me);
            Tri(s,high[i],apex,high[j],hi);
        }
    }

    /// <summary>발광 머티리얼. Rebuild() 때 generatedMaterials 와 함께 지워진다.</summary>
    Material Glow(Color c,float strength)
    {
        var m=new Material(Shader.Find("Standard")); m.color=c; m.SetFloat("_Glossiness",.08f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",c*strength);
        m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;
        generatedMaterials.Add(m); return m;
    }

    // ── 나무 ─────────────────────────────────────────────────────────────────────
    /// <summary>줄기 + 층진 수관. 덩어리 하나가 아니다 — 층이 갈려야 나무로 보인다.</summary>
    void Tree(float x,float y,float z,float s,bool autumn=false)
    {
        Vector3 root=new Vector3(x,y,z);
        float lean=R(-.07f,.07f)*s, leanZ=R(-.06f,.06f)*s, trunkTop=1.30f*s;
        Color bark=C("4e3726"), barkLit=C("6d5033");

        var trunk=new Scrap();
        // 밑동이 퍼진다. 이게 없으면 막대를 땅에 꽂은 것으로 보인다.
        Stalk(trunk,root-Vector3.up*.07f*s,root+Vector3.up*.20f*s,.170f*s,.104f*s,bark,6,false);
        Vector3 waist=root+new Vector3(lean*.55f,trunkTop*.58f,leanZ*.55f);
        Vector3 crown=root+new Vector3(lean,trunkTop,leanZ);
        Stalk(trunk,root+Vector3.up*.20f*s,waist,.104f*s,.072f*s,barkLit,6,false);
        Stalk(trunk,waist,crown,.072f*s,.046f*s,barkLit,5);
        for(int b=0;b<3;b++)
        {
            float a=b*2.31f+R(-.35f,.35f), t=.60f+b*.15f;
            Vector3 from=Vector3.Lerp(root,crown,t);
            Stalk(trunk,from,from+new Vector3(Mathf.Cos(a)*.36f*s,.32f*s,Mathf.Sin(a)*.36f*s),.042f*s,.020f*s,bark,4);
        }
        MeshObject("Tree trunk",trunk.verts,trunk.tris,trunk.cols);

        // 가을 주황 #e08a3c 는 시안이 지정한 색이다. 여름 나무는 짙은 녹색 한 그루.
        Color low   =autumn?C("a35420"):C("27401e");
        Color middle=autumn?C("e08a3c"):C("3f6229");
        Color high  =autumn?C("f2b062"):C("5a8439");
        var leaves=new Scrap();
        // 시안의 나무는 수관이 한 칸(1.47)보다 좁다. 폭 1.40 짜리 수관은 밭을 덮어 여백을 지웠다 — 1.12 로 줄인다.
        float[] radius={.56f,.44f,.30f}, thick={.34f,.29f,.25f}, height={1.10f,1.40f,1.66f};
        for(int tier=0;tier<3;tier++)
        {
            Vector3 c=root+new Vector3(lean*.95f+R(-.05f,.05f)*s,height[tier]*s,leanZ*.95f+R(-.05f,.05f)*s);
            Ball(leaves,c,new Vector3(radius[tier],thick[tier],radius[tier]*.94f)*s,
                 tier==2?high:Color.Lerp(middle,high,tier*.45f), tier==0?low:Color.Lerp(low,middle,.5f),7,.15f);
            // 층마다 곁덩이 하나. 완전한 원 셋을 쌓으면 다시 덩어리 하나가 되지만,
            // 곁덩이가 둘이면 층 사이 틈까지 메워져 수관이 통째로 부풀고 밭을 덮는다.
            {
                float a=R(0,6.283f);
                Ball(leaves,c+new Vector3(Mathf.Cos(a)*radius[tier]*.74f*s,R(-.07f,.10f)*s,Mathf.Sin(a)*radius[tier]*.74f*s),
                     Vector3.one*R(.14f,.21f)*s,Color.Lerp(middle,high,R(.15f,.95f)),low,6,.22f);
            }
        }
        var canopy=MeshObject("Tree canopy",leaves.verts,leaves.tris,leaves.cols);
        canopy.AddComponent<LeafSway>().phase=x*1.7f+z;
    }

    // ── 풀포기 ───────────────────────────────────────────────────────────────────
    /// <summary>
    /// 휘는 잎날 셋. 조약돌·들꽃은 아주 가끔만 섞인다.
    /// 시안에서 풀포기는 눈에 띄는 오브젝트가 아니라 잔디의 결이다.
    /// 잎이 포기마다 다섯이고 여덟 포기 중 하나꼴로 돌이나 꽃이 박히면
    /// 잔디밭이 자갈밭으로 읽히고, 빈 잔디에 눈 쉴 곳이 없어진다.
    /// </summary>
    void GrassTuft(float x,float y,float z,float size)
    {
        var s=new Scrap(); Vector3 root=new Vector3(x,y,z);
        Color blade=C("7c9648"), tip=C("aac067"), dark=C("4a6631");
        for(int j=0;j<3;j++)
        {
            float a=j*2.11f+R(-.35f,.35f); Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
            float len=Mathf.Min(size*R(1.3f,2.0f),.26f);
            Vector3 b=root+d*size*.18f;
            Leaf(s,b,b+d*len*.30f+Vector3.up*len*.82f,b+d*len*.66f+Vector3.up*len*.92f,size*.60f,
                 Color.Lerp(blade,tip,R(0,1)),dark,3);
        }
        if(R(0,1)<.04f)  // 조약돌
            Ball(s,root+new Vector3(R(-.22f,.22f),.012f,R(-.22f,.22f)),
                 new Vector3(size*R(.8f,1.5f),size*R(.5f,.9f),size*R(.8f,1.4f)),C("a5a695"),C("6c6f64"),6,.22f);
        if(R(0,1)<.035f)  // 들꽃
        {
            Vector3 f=root+new Vector3(R(-.18f,.18f),0,R(-.18f,.18f));
            float stemH=size*1.6f; Stalk(s,f,f+Vector3.up*stemH,.008f,.006f,C("5c7b3a"),3);
            Ball(s,f+Vector3.up*(stemH+.012f),Vector3.one*size*.32f,R(0,1)<.5f?C("efe4c6"):C("d99a63"),C("b08a52"),5,.2f);
        }
        MeshObject("Grass tuft",s.verts,s.tris,s.cols);
    }

    // ── 울타리 ───────────────────────────────────────────────────────────────────
    /// <summary>끝을 깎은 말뚝 + 가로대 둘. 시안의 울타리는 밝은 나무이고 말뚝 높이가 고르지 않다.</summary>
    void Fence(Vector3 a,Vector3 b,float height=.65f)
    {
        Color post=C("b09168"), rail=C("9a7d55"), cap=C("7a6041");
        int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.78f));
        for(int i=0;i<=count;i++)
        {
            var p=Vector3.Lerp(a,b,(float)i/count);
            Vector3 top=p+new Vector3(R(-.025f,.025f),height*R(.93f,1.07f),R(-.025f,.025f));
            Beam(p-Vector3.up*.07f,top,.085f,post,"Fence post");
            Box("Fence post cap",top+Vector3.up*.015f,new Vector3(.115f,.035f,.115f),cap);
        }
        Beam(a+Vector3.up*height*.42f,b+Vector3.up*height*.42f,.062f,rail,"Fence rail");
        Beam(a+Vector3.up*height*.80f,b+Vector3.up*height*.80f,.062f,rail,"Fence rail");
    }

    // ── 집 ───────────────────────────────────────────────────────────────────────
    void Cottage(float x,float z)
    {
        Cube("Farmhouse foundation",x,.87f,z,2.32f,.22f,1.97f,C("8a8067"));
        Cube("Farmhouse plaster",x,1.68f,z,2.12f,1.48f,1.77f,C("cbb891"));
        Roof(x,2.43f,z,2.43f,2.12f,.78f,C("8b4f3a"));   // 시안의 적갈색 지붕. 굴뚝은 Roof 가 세운다
        for(int j=-1;j<=1;j++) {Cube("Timber frame",x+j*1.01f,1.7f,z-.905f,.10f,1.51f,.08f,wood);Cube("Timber side",x+1.08f,1.7f,z+j*.8f,.06f,1.51f,.10f,wood);}
        Cube("Timber foundation",x,1.04f,z-.93f,2.16f,.13f,.10f,wood);Cube("Timber lintel",x,2.36f,z-.93f,2.19f,.13f,.10f,wood);

        // 불이 켜진 창. 유리가 실제로 발광하고, 그 빛이 현관 바닥까지 떨어진다.
        LitWindow(x-.55f,1.74f,z-.90f,false,.30f,.40f);
        LitWindow(x+1.075f,1.77f,z-.30f,true,.30f,.36f);
        Cube("Gable batten",x,2.52f,z-1.05f,.46f,.045f,.05f,wood);
        LitWindow(x,2.72f,z-1.045f,false,.20f,.18f);

        Cube("Farmhouse door",x+.44f,1.42f,z-.93f,.55f,.98f,.10f,C("5c4a30"));
        for(int j=0;j<3;j++)Cube("Door plank",x+.44f-.18f+j*.18f,1.42f,z-.985f,.045f,.94f,.025f,C("74603f"));
        Cube("Door lintel",x+.44f,1.94f,z-.97f,.72f,.10f,.13f,wood);
        Porch(x+.44f,z);

        // 시안의 집 둘레는 물통 하나 · 상자 하나 · 장작 한 줄이 전부다.
        // 문 왼쪽에 깔려 있던 정원석 셋은 현관 앞 여백을 먹어 덜어냈다.
        Barrel(x-1.47f,z-.48f);
        Woodpile(x-2.05f,z-.05f);
        Crates(x+1.58f,z-.35f);
        HomesteadYard(x,z);
    }

    /// <summary>틀 → 발광 유리 → 창살 → 창턱. 깊이를 겹치지 않게 띄워 Z 파이팅을 피한다.</summary>
    void LitWindow(float x,float y,float z,bool side,float w,float h)
    {
        Cube("Window frame",x,y,z,side?.06f:w+.10f,h+.10f,side?w+.10f:.06f,C("e2d6bc"));
        var glass=Box("Window glass",new Vector3(x+(side?.035f:0),y,z+(side?0:-.035f)),
                      new Vector3(side?.02f:w,h,side?w:.02f),C("ffc87a"));
        glass.GetComponent<Renderer>().sharedMaterial=Glow(C("ffc87a"),1.55f);
        Cube("Mullion",x+(side?.058f:0),y,z+(side?0:-.058f),side?.022f:.028f,h+.02f,side?.028f:.022f,C("b9b399"));
        Cube("Mullion",x+(side?.058f:0),y,z+(side?0:-.058f),side?.022f:w,.028f,side?w:.022f,C("b9b399"));
        Cube("Sill",x,y-h/2-.055f,z,side?.17f:w+.20f,.055f,side?w+.20f:.17f,stone);
    }

    /// <summary>현관 — 널 깐 바닥 · 기둥 둘 · 차양 · 계단 · 등불. 시안의 집은 문 앞이 비어 있지 않다.</summary>
    void Porch(float px,float z)
    {
        Cube("Porch deck",px,.905f,z-1.28f,1.32f,.11f,.72f,C("9c8358"));
        for(int j=0;j<5;j++)Cube("Porch plank",px,.965f,z-1.60f+j*.16f,1.30f,.02f,.125f,C(j%2==0?"8a744c":"a08b5f"));
        for(int s=-1;s<=1;s+=2)Beam(new Vector3(px+s*.56f,.93f,z-1.58f),new Vector3(px+s*.56f,2.02f,z-1.58f),.095f,wood,"Porch post");

        var canopy=new Scrap();
        Vector3 fl=new Vector3(px-.74f,2.00f,z-1.72f),fr=new Vector3(px+.74f,2.00f,z-1.72f);
        Vector3 bl=new Vector3(px-.74f,2.30f,z-0.92f),br=new Vector3(px+.74f,2.30f,z-0.92f);
        Quad2(canopy,fl,fr,br,bl,C("7a4b39"),C("4a3026"));
        Quad2(canopy,fl+Vector3.down*.07f,fr+Vector3.down*.07f,fr,fl,C("6b4232"),C("4a3026"));
        Quad2(canopy,fl,bl,bl+Vector3.down*.07f,fl+Vector3.down*.07f,C("5f3b2d"),C("4a3026"));
        Quad2(canopy,fr+Vector3.down*.07f,br+Vector3.down*.07f,br,fr,C("5f3b2d"),C("4a3026"));
        MeshObject("Porch canopy",canopy.verts,canopy.tris,canopy.cols);

        Cube("Doorstep",px,.877f,z-1.73f,1.00f,.09f,.24f,C("b5a482"));
        Cube("Doorstep",px,.856f,z-1.93f,1.12f,.075f,.24f,C("a99a79"));

        Cube("Porch lantern",px+.56f,1.72f,z-1.62f,.13f,.19f,.13f,C("4b4238"));
        var flame=Box("Lantern flame",new Vector3(px+.56f,1.72f,z-1.695f),new Vector3(.085f,.12f,.02f),C("ffcf8a"));
        flame.GetComponent<Renderer>().sharedMaterial=Glow(C("ffcf8a"),1.9f);
        // 실제 광원. 발광 재질만으로는 바닥이 어두워 "불이 켜졌다"가 읽히지 않는다.
        var lamp=new GameObject("Hearth light"); lamp.transform.SetParent(world.transform);
        lamp.transform.position=new Vector3(px,1.55f,z-1.35f);
        var light=lamp.AddComponent<Light>(); light.type=LightType.Point; light.color=C("ffb265");
        light.intensity=2.1f; light.range=3.6f; light.shadows=LightShadows.None;
    }

    /// <summary>빗물통. 나무 테를 두르고 물이 차 있다.</summary>
    void Barrel(float x,float z)
    {
        var barrel=GameObject.CreatePrimitive(PrimitiveType.Cylinder);barrel.name="Rain barrel";barrel.transform.SetParent(world.transform);
        barrel.transform.position=new Vector3(x,1.11f,z);barrel.transform.localScale=new Vector3(.50f,.34f,.50f);
        barrel.GetComponent<Renderer>().sharedMaterial=Mat(C("4f6572"));
        for(int j=0;j<3;j++)
        {
            var ring=GameObject.CreatePrimitive(PrimitiveType.Cylinder);ring.name="Barrel hoop";ring.transform.SetParent(world.transform);
            ring.transform.position=barrel.transform.position+Vector3.up*(j-1)*.21f;
            ring.transform.localScale=new Vector3(.535f,.028f,.535f);
            ring.GetComponent<Renderer>().sharedMaterial=Mat(C("8a6a42"));
        }
        var water=GameObject.CreatePrimitive(PrimitiveType.Cylinder);water.name="Barrel water";water.transform.SetParent(world.transform);
        water.transform.position=barrel.transform.position+Vector3.up*.27f;water.transform.localScale=new Vector3(.44f,.01f,.44f);
        water.GetComponent<Renderer>().sharedMaterial=Mat(C("6f9aa4"));
    }

    /// <summary>장작더미. '목재' 가 이 게임의 자원이니 마당에 쌓여 있어야 한다.</summary>
    void Woodpile(float x,float z)
    {
        // 한 줄 세 토막. 두 단 다섯 토막은 집 왼쪽 벽을 가려 마당이 꽉 차 보였다.
        for(int j=0;j<3;j++)
        {
            float y=.95f, zz=z-.21f+j*.21f;
            var log=GameObject.CreatePrimitive(PrimitiveType.Cylinder);log.name="Firewood";log.transform.SetParent(world.transform);
            log.transform.position=new Vector3(x,y,zz);log.transform.localScale=new Vector3(.19f,.34f,.19f);
            log.transform.rotation=Quaternion.Euler(0,0,90);
            log.GetComponent<Renderer>().sharedMaterial=Mat(C(j%2==0?"7c5b3c":"6a4d33"));
        }
        var s=new Scrap();
        for(int j=0;j<2;j++)Ball(s,new Vector3(x-.38f,.95f+j*.015f,z-.2f+j*.24f),new Vector3(.10f,.09f,.10f),C("c9b48a"),C("9a8760"),6,.2f);
        MeshObject("Cut log ends",s.verts,s.tris,s.cols);
    }

    /// <summary>나무 상자 하나. 시안의 집 오른쪽에 놓인 것은 하나뿐이다 —
    /// 쌓아 올린 둘째 상자와 곡물 자루는 처마 밑을 메워 집의 실루엣을 뭉갰다.</summary>
    void Crates(float x,float z)
    {
        Cube("Vegetable crate",x,.99f,z,.55f,.40f,.50f,wood);
        for(int j=0;j<3;j++)Cube("Crate slat",x,.87f+j*.12f,z-.265f,.59f,.075f,.035f,C("ab8d57"));
    }

    /// <summary>마당. 섬 가장자리 울타리 배치는 ArtIsland(구역 B) 소유라 집 주변만 여기서 세운다.</summary>
    void HomesteadYard(float x,float z)
    {
        Fence(new Vector3(x-3.13f,.84f,z-.61f),new Vector3(x-2.10f,.84f,z-.61f));
        Fence(new Vector3(x+2.50f,.84f,z-.61f),new Vector3(x+3.60f,.84f,z-.61f));
        // 시안의 "짙은 녹색 나무 한 그루". 가을 나무 둘은 ArtIsland 가 섬 양끝에 세운다.
        Tree(x+2.30f,.84f,z-.10f,.82f);
        for(int j=0;j<2;j++)Rock(new Vector3(x+3.05f+R(-.30f,.30f),.88f,z-.35f+R(-.30f,.30f)),
                                 new Vector3(R(.10f,.20f),R(.07f,.14f),R(.10f,.18f)),C("9b9d8c"),"Yard stone");
    }

    // ── 작물 ─────────────────────────────────────────────────────────────────────
    //
    // 시안의 실루엣 다섯을 데이터의 작물 여섯에 얹는다. 작물표(data/crops.json)는 이 구역 소유가
    // 아니라 이름을 바꾸지 않았다. 형태만 시안에 맞춘다:
    //   호밀   → 밀        빽빽한 황금 이삭. 다 자라면 고개를 숙인다
    //   순무   → 양배추    겉잎이 깔리고 가운데 둥근 통이 앉는다
    //   해바라기 → 키 큰 작물  가장 높은 줄기 + 길게 휘는 잎 + 노란 원반
    //   감자   → 호박형    넓은 잎이 땅을 덮고 그 사이로 둥근 주황 열매
    //   클로버 → 피복      가장 낮고 칸 전체를 덮는다
    //   아마   → 가는 줄기 + 푸른 꽃 (위 다섯과 겹치지 않는 여섯째)
    // 심은 직후(성장 17% 미만)에는 무엇이든 **새싹**이다 — 시안의 선택된 칸이 그 모습이다.
    void CropArt(PlotState p,Vector3 pos)
    {
        float progress=Mathf.Clamp01((float)p.GrowthPoints/(data.Crop(p.CropId).growDays*100));
        var s=new Scrap(); string name;
        if(progress<.17f){Seedbed(s,pos,progress);name="Seedlings";}
        else switch(p.CropId)
        {
            case "wheat":       Grain(s,pos,progress,false); name="Wheat field";     break;
            case "flax":      Grain(s,pos,progress,true);  name="Flax field";      break;
            case "sunflower": Sunflower(s,pos,progress);   name="Sunflower rows";  break;
            case "pumpkin":    Pumpkin(s,pos,progress);      name="Pumpkin patch";    break;
            case "cabbage":    Cabbage(s,pos,progress);      name="Cabbage bed";     break;
            default:          Clover(s,pos,progress);      name="Clover cover";    break;
        }
        if(!s.Empty)MeshObject(name,s.verts,s.tris,s.cols);
    }

    /// <summary>
    /// 심지 않은 칸의 장식 — 돌무더기 · 상자 · 덤불 · 그루터기 중 하나.
    /// **빈 칸 셋 중 하나에만 놓는다.** 칸마다 뭔가가 서 있으면 눈이 쉴 곳이 없다 —
    /// 시안의 빈 잔디는 정말로 비어 있고, 그 여백이 편안함을 만든다.
    /// 나머지 두 칸은 자갈 하나로 끝낸다.
    /// **호출부는 ArtIsland(구역 B) 소유다.** 그쪽에서 `if(p.HasCrop)CropArt(p,pos); else PlotDecor(p,pos);`
    /// 로 한 줄만 이어 주면 켜진다.
    /// </summary>
    void PlotDecor(PlotState p,Vector3 pos)
    {
        int hash=Mathf.Abs((p.Id??"").GetHashCode());
        if((hash/4)%3!=0){FallowPebble(pos);return;}
        var s=new Scrap();
        int kind=hash%4;
        Vector3 at=pos+new Vector3(R(-.26f,.26f),0,R(-.22f,.22f)); at.y=cropSoilY-.03f;
        switch(kind)
        {
            case 0:   // 돌무더기
                for(int i=0;i<3;i++)
                    Ball(s,at+new Vector3(R(-.16f,.16f),R(0,.06f),R(-.16f,.16f)),
                         new Vector3(R(.10f,.20f),R(.07f,.15f),R(.10f,.18f)),C("a6a897"),C("6d7167"),6,.2f);
                break;
            case 1:   // 버려둔 상자
                Cube("Idle crate",at.x,at.y+.16f,at.z,.42f,.31f,.38f,C("8a6d42"));
                for(int i=0;i<2;i++)Cube("Idle crate slat",at.x,at.y+.09f+i*.13f,at.z-.20f,.45f,.055f,.03f,C("ab8d57"));
                break;
            case 2:   // 덤불
                for(int i=0;i<2;i++)
                    Ball(s,at+new Vector3(R(-.12f,.12f),.10f+R(0,.05f),R(-.12f,.12f)),
                         new Vector3(R(.14f,.21f),R(.10f,.16f),R(.13f,.20f)),C("4f7a3d"),C("2f4c28"),6,.18f);
                break;
            default:  // 베어 낸 그루터기
                Stalk(s,at,at+Vector3.up*.17f,.13f,.115f,C("5b4028"),6);
                Ball(s,at+Vector3.up*.175f,new Vector3(.115f,.02f,.115f),C("b59a6e"),C("8a7350"),6,.06f);
                break;
        }
        Ball(s,new Vector3(pos.x+R(-.42f,.42f),cropSoilY-.02f,pos.z+R(-.42f,.42f)),
             new Vector3(R(.05f,.10f),R(.03f,.06f),R(.05f,.09f)),C("9fa091"),C("6a6d63"),5,.25f);
        if(!s.Empty)MeshObject("Fallow decor",s.verts,s.tris,s.cols);
    }

    /// <summary>자갈 하나. 장식을 놓지 않은 빈 칸이 완전한 민둥판으로 보이지 않을 최소한.</summary>
    void FallowPebble(Vector3 pos)
    {
        var s=new Scrap();
        Ball(s,new Vector3(pos.x+R(-.42f,.42f),cropSoilY-.02f,pos.z+R(-.42f,.42f)),
             new Vector3(R(.05f,.09f),R(.03f,.055f),R(.05f,.08f)),C("9fa091"),C("6a6d63"),5,.25f);
        MeshObject("Fallow pebble",s.verts,s.tris,s.cols);
    }

    /// <summary>새싹. 짧은 대에 떡잎 둘 — 흙이 많이 보이고 키가 가장 낮다.</summary>
    void Seedbed(Scrap s,Vector3 pos,float progress)
    {
        float h=.05f+progress*.95f;
        Color leafC=C("a5d162"), leafBack=C("658a3c");
        // 네 고랑(x = ±.13 · ±.39) 위에 두 줄. 4x3=12 포기는 고랑을 덮어 흙이 보이지 않았다.
        for(int i=0;i<8;i++)
        {
            float x=pos.x-.39f+(i%4)*.26f+R(-.03f,.03f), z=pos.z-.26f+(i/4)*.52f+R(-.035f,.035f);
            Vector3 root=new Vector3(x,cropSoilY,z), top=root+Vector3.up*h;
            Stalk(s,root,top,.012f,.009f,C("6f9440"),3);
            for(int leaf=0;leaf<2;leaf++)
            {
                float a=R(0,6.283f)+leaf*3.142f; Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Leaf(s,top,top+d*.062f+Vector3.up*.050f,top+d*.135f+Vector3.up*.022f,.115f,leafC,leafBack,2);
            }
        }
    }

    /// <summary>밀 — 촘촘한 줄기 끝에 방추형 이삭과 까끄라기. 아마는 가늘고 성기며 끝에 푸른 꽃.</summary>
    void Grain(Scrap s,Vector3 pos,float progress,bool flax)
    {
        Color stem=flax?C("87a06a"):Color.Lerp(C("8fa155"),C("c2a44b"),progress);
        Color head=flax?C("7f9fc6"):Color.Lerp(C("b9ba62"),C("e3c469"),progress);
        // 밀 5x5x3=75 대 → 4x4x2=32 대, 아마 4x4x2=32 대 → 3x3x2=18 대.
        // 열을 네 고랑 위에 맞춰 열 사이의 흙이 그대로 드러나게 한다.
        int cols=flax?3:4, rows=flax?3:4, stalks=2;
        float h=(flax?.30f:.26f)+progress*(flax?.30f:.34f);
        for(int i=0;i<cols*rows;i++)
        {
            float x=pos.x-.40f+(i%cols)*(.80f/(cols-1))+R(-.04f,.04f);
            float z=pos.z-.40f+(i/cols)*(.80f/(rows-1))+R(-.04f,.04f);
            for(int k=0;k<stalks;k++)
            {
                float tall=h*R(.86f,1.14f);
                // 익을수록 이삭 무게로 기운다. 곧게 선 밀밭은 덜 익어 보인다.
                float nod=flax?R(-.02f,.02f):progress*progress*R(.06f,.15f);
                Vector3 root=new Vector3(x+R(-.055f,.055f),cropSoilY,z+R(-.055f,.055f));
                Vector3 neck=root+new Vector3(nod*.30f,tall*.70f,nod*.18f);
                Vector3 tip =root+new Vector3(nod,tall,nod*.55f);
                Stalk(s,root,neck,.014f,.011f,stem,3,false);
                Stalk(s,neck,tip,.011f,.008f,stem,3,false);
                if(flax)
                {
                    if(progress>.40f)Ball(s,tip+Vector3.up*.012f,new Vector3(.034f,.022f,.034f),head,Shade(head,.72f),5,.18f);
                }
                else if(progress>.22f)
                {
                    Vector3 e1=tip+new Vector3(nod*.45f,.055f,nod*.26f), e2=tip+new Vector3(nod*1.0f,.150f,nod*.55f);
                    Stalk(s,tip,e1,.012f,.033f,head,4,false);
                    Stalk(s,e1,e2,.033f,.006f,head,4);
                    for(int awn=0;awn<2;awn++)
                    {
                        float a=awn*2.6f+R(-.4f,.4f);
                        Stalk(s,e2,e2+new Vector3(Mathf.Cos(a)*.030f,.072f,Mathf.Sin(a)*.030f),.005f,.001f,Shade(head,1.12f),3,false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 밭에서 가장 키가 크다. 굵은 줄기 + 길게 휘는 잎 셋 + 노란 원반.
    /// **키 큰 작물이 밭을 가리면 안 된다.** 예전 최고 1.16 은 칸 한 변(1.47)에 육박해
    /// 뒤쪽 칸과 집을 통째로 지웠다 — 0.84 로 낮추고 포기를 열둘에서 여섯으로 줄인다.
    /// </summary>
    void Sunflower(Scrap s,Vector3 pos,float progress)
    {
        Color stalkC=C("5f7d3c"), leafC=C("54783a"), leafBack=C("39552a");
        float tall=.34f+progress*.50f;
        for(int i=0;i<6;i++)
        {
            float x=pos.x-.39f+(i%3)*.39f+R(-.025f,.025f), z=pos.z-.26f+(i/3)*.52f+R(-.04f,.04f);
            float h=tall*R(.90f,1.08f);
            Vector3 root=new Vector3(x,cropSoilY,z), top=root+new Vector3(R(-.05f,.05f),h,R(-.04f,.04f));
            Vector3 mid=Vector3.Lerp(root,top,.5f);
            Stalk(s,root,mid,.034f,.026f,stalkC,5,false);
            Stalk(s,mid,top,.026f,.018f,stalkC,5,false);
            for(int leaf=0;leaf<3;leaf++)
            {
                float a=leaf*2.17f+R(-.35f,.35f); Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 b=Vector3.Lerp(root,top,.30f+leaf*.20f);
                Leaf(s,b,b+d*.14f+Vector3.up*.11f,b+d*.26f-Vector3.up*.035f,.18f,leafC,leafBack,3);
            }
            if(progress>.42f)
            {
                // 꽃은 해를 보고 기운다 — 카메라(-z) 쪽이다. 원반이 정면으로 보여야 알아본다.
                Vector3 face=top+new Vector3(0,.025f,-.06f);
                Stalk(s,top,face,.020f,.020f,stalkC,4,false);
                for(int petal=0;petal<9;petal++)
                {
                    float a=petal*.698f; Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    Leaf(s,face+d*.056f+Vector3.up*.010f,face+d*.100f+Vector3.up*.026f,face+d*.152f+Vector3.up*.006f,
                         .066f,C("efc247"),C("bf9530"),2);
                }
                Ball(s,face,new Vector3(.086f,.038f,.086f),C("d2ab3c"),C("7d6a2e"),9,.05f);
                Ball(s,face+Vector3.up*.020f,new Vector3(.046f,.028f,.046f),C("6b4a28"),C("48311b"),7,.09f);
            }
        }
    }

    /// <summary>넓은 잎이 땅을 덮고 그 사이로 둥근 주황 열매가 드러난다 — 시안의 호박 칸 실루엣.</summary>
    void Pumpkin(Scrap s,Vector3 pos,float progress)
    {
        Color leafC=C("4f7736"), leafBack=C("32502a"), vine=C("597f3a");
        // 포기 넷 → 셋. 지그재그로 앉혀 포기 사이로 흙과 주황 열매가 같이 보이게 한다.
        for(int i=0;i<3;i++)
        {
            float x=pos.x-.34f+i*.34f+R(-.04f,.04f), z=pos.z-.20f+(i%2)*.40f+R(-.05f,.05f);
            Vector3 crown=new Vector3(x,cropSoilY,z);
            int leaves=progress>.5f?5:4;
            for(int leaf=0;leaf<leaves;leaf++)
            {
                float a=leaf*(6.283f/leaves)+R(-.25f,.25f); Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                float len=(.20f+progress*.15f)*R(.85f,1.15f);
                Leaf(s,crown+Vector3.up*.015f,crown+d*len*.45f+Vector3.up*(.10f+progress*.05f),
                     crown+d*len+Vector3.up*.03f,.24f,leafC,leafBack,3);
            }
            Stalk(s,crown,crown+new Vector3(R(-.20f,.20f),.025f,R(-.20f,.20f)),.015f,.010f,vine,3,false);
            if(progress>.50f)
            {
                for(int f=0;f<(progress>.80f?2:1);f++)
                {
                    float a=R(0,6.283f), rr=.072f+progress*.048f;
                    Vector3 c=crown+new Vector3(Mathf.Cos(a)*.18f,rr*.72f,Mathf.Sin(a)*.18f);
                    Ball(s,c,new Vector3(rr,rr*.78f,rr),C("d98a37"),C("a55c20"),7,.07f);
                    Stalk(s,c+Vector3.up*rr*.55f,c+Vector3.up*(rr*.55f+.05f),.019f,.012f,C("6b7f3a"),4);
                }
            }
        }
    }

    /// <summary>양배추. 겉잎이 땅에 넓게 깔리고 가운데 통이 앉는다 — 매끈한 구면이면 돌로 보인다.</summary>
    void Cabbage(Scrap s,Vector3 pos,float progress)
    {
        Color outer=C("47703a"), outerBack=C("2e4b27"), ballTop=C("86ab58"), ballLow=C("4e7339");
        // 3x3=9 통 → 3x2=6 통. 간격을 .30 에서 .34 / .44 로 벌려 통 사이에 흙고랑이 읽힌다.
        for(int i=0;i<6;i++)
        {
            float x=pos.x-.34f+(i%3)*.34f+R(-.035f,.035f), z=pos.z-.22f+(i/3)*.44f+R(-.04f,.04f);
            Vector3 c=new Vector3(x,cropSoilY,z);
            for(int leaf=0;leaf<4;leaf++)
            {
                float a=leaf*1.571f+R(-.22f,.22f); Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Leaf(s,c+Vector3.up*.010f,c+d*.09f+Vector3.up*.075f,c+d*.190f+Vector3.up*.012f,.18f,outer,outerBack,3);
            }
            float r=.053f+progress*.076f;
            Ball(s,c+Vector3.up*(r*.82f+.012f),new Vector3(r,r*.92f,r),ballTop,ballLow,7,.09f);
            // 통을 감싸는 속잎. 매끈한 구는 돌로 보인다 — 잎이 겹친 덩어리여야 양배추다.
            for(int wrap=0;wrap<3;wrap++)
            {
                float a=wrap*2.094f+.45f; Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 b=c+d*r*.88f+Vector3.up*.015f;
                Leaf(s,b,b+d*.022f+Vector3.up*r*1.15f,c+Vector3.up*(r*1.80f),.11f,Shade(ballTop,1.07f),ballLow,3);
            }
        }
    }

    /// <summary>가장 낮다. 칸을 고르게 덮되 흙을 완전히 지우지는 않는다 —
    /// 44 포기는 잔디 한 장이 되어 밭으로 읽히지 않았다.</summary>
    void Clover(Scrap s,Vector3 pos,float progress)
    {
        Color leafC=C("62964b"), leafBack=C("3d6832");
        for(int i=0;i<20;i++)
        {
            Vector3 root=new Vector3(pos.x+R(-.46f,.46f),cropSoilY,pos.z+R(-.46f,.46f));
            float h=.030f+progress*.052f;
            Vector3 top=root+Vector3.up*h;
            Stalk(s,root,top,.009f,.007f,C("52803c"),3,false);
            for(int leaf=0;leaf<3;leaf++)
            {
                float a=leaf*2.094f+R(-.25f,.25f); Vector3 d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Leaf(s,top,top+d*.040f+Vector3.up*.022f,top+d*.090f+Vector3.up*.010f,.095f,leafC,leafBack,2);
            }
            if(progress>.55f&&i%3==0)
                Ball(s,top+Vector3.up*.034f,new Vector3(.030f,.032f,.030f),C("efeadb"),C("c4c0ab"),6,.16f);
        }
    }
}

/// <summary>수관만 흔든다. 줄기까지 흔들면 나무가 통째로 미끄러진다.</summary>
public class LeafSway:MonoBehaviour
{
    public float phase; Quaternion rest;
    void Start(){rest=transform.localRotation;}
    void Update()
    {
        float t=Time.time*.8f+phase;
        transform.localRotation=rest*Quaternion.Euler(Mathf.Sin(t*1.27f)*1.1f,0,Mathf.Sin(t)*1.6f);
    }
}
