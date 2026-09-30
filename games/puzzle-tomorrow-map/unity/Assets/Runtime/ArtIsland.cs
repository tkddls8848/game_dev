using System;
using System.Collections.Generic;
using UnityEngine;
using FarmErosion.Sim;

// 구역 B — 섬 형태 · 절벽 · 밭 배치. FarmScene 이 다른 구역의 진입점을 호출한다.
//
// Art.cs 한 파일에 바다·섬·소품이 모두 있어서 여러 사람이 동시에 손댈 수 없었다.
// partial class 라 파일만 갈라도 소유권이 갈린다. **구역 밖 메서드를 이 파일로 옮기지 말 것** —
// 옮기는 순간 다시 한 파일이 되고 병렬 작업이 충돌한다.
public sealed partial class LowpolyGame
{
    // 시안의 십자(플러스) 실루엣은 **여기서 만들지 않는다.** 화면이 임의로 칸을 숨기면
    // HUD 의 "남은 땅 n / 24" 와 화면의 칸 수가 어긋나고, 그건 모양을 위조한 것이다.
    // 모양은 data/plots.json 의 startErosion 이 정한다 — 모서리 여덟 칸(x 0,1,4,5 x y 0,3)이
    // 임계 2000 에 가장 가까워 먼저 무너지고, 남는 열여섯 칸이 십자가 된다.
    // 이 파일은 p.Active 가 말하는 대로만 그린다.
    //
    //   y=3  . . ■ ■ . .      화면에서 네 팔은 대각선으로 뻗고
    //   y=2  ■ ■ ■ ■ ■ ■      상하좌우 꼭짓점이 패인다. 사각 블록이 아니다.
    //   y=1  ■ ■ ■ ■ ■ ■
    //   y=0  . . ■ ■ . .

    // 어느 칸이 실제 땅인가. 잔디 오버행·해안 포말·붕괴 파편·방벽이 전부 "드러난 변"을 알아야 해서
    // 타일을 하나라도 그리기 전에 한 번 정해 둔다. y=4 는 격자 밖, 집이 있는 뒤쪽 단이다.
    bool[,] landGrid;
    bool Land(int x,int y)=>landGrid!=null&&x>=0&&x<6&&y>=0&&y<5&&landGrid[x,y];

    // 구역 A(바다)가 쓰라고 내놓는 해안선. 드러난 변 하나가 (x1,z1,x2,z2) 한 항목이다.
    // 포말 리본·수심 마스크를 원형 거리 대신 이 선분에서 만들면 침식으로 해안이 바뀌어도 따라온다.
    public readonly List<Vector4> CoastSegments=new List<Vector4>();
    void AddCoastSegment(float x,float z,float half,int dx,int dz)
    {
        Vector3 a=EdgePoint(x,z,half,dx,dz,-half,0,0),b=EdgePoint(x,z,half,dx,dz,half,0,0);
        CoastSegments.Add(new Vector4(a.x,a.z,b.x,b.z));
    }

    // 절벽 지층. 위가 밝고 아래가 어둡다 — 이 명도 차가 없으면 절벽이 한 장의 종이가 된다.
    //
    // **층이 안 읽히는 이유는 색이 아니라 사다리 간격이다.** 옛 네 색의 휘도는
    // 0.506 / 0.436 / 0.350 / 0.281 로 층 사이가 1.16~1.25 배인데, 아래 Cliff() 가 면마다
    // R(.88,1.08) 을 곱한다. 한 층 안의 밝기 폭이 1.23 배라 **이웃 층과 통째로 겹친다.**
    // 색상(27~33도)과 채도(0.45~0.48)는 시안의 절벽(색상 24~43도, 채도 0.35~0.50)과 맞으므로
    // 그대로 두고, 아래 세 층만 눌러 사다리를 1.20/1.27/1.37 배로 벌린다.
    // 휘도 0.506 / 0.421 / 0.331 / 0.242 — 위아래 차이가 1.80 배에서 2.10 배가 된다.
    // 아래로 갈수록 채도를 조금 올려(0.446→0.506) 젖은 흙으로 읽히게 한다.
    static readonly string[] cliffStrata={"9d7d57","876746","6d5037","513a28"};

    void Cliff(float x,float z,float size,float top,float depth)
    {
        var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
        const int count=12,bands=4;
        // 층 경계를 같은 간격으로 두면 자로 그은 줄무늬가 된다. 퇴적층처럼 두께를 달리한다.
        // 세 번째 경계(0.618)는 일부러 높이 .73-1.76 = -1.03 에 놓았다 — 여기서부터 벽이
        // 안으로 물러나 **눈에 보이는 수직 벽이 1.76** 이 된다(칸 한 변 1.47 의 1.2배).
        // 허리는 살짝만 좁힌다. 많이 좁히면 이웃 칸의 절벽 사이가 벌어져 섬 속이 들여다보인다.
        float[] cut={0f,.22f,.42f,.618f,1f};
        float[] waist={1f,.99f,.97f,.95f,.88f};
        var rings=new Vector3[bands+1,count];
        for(int ring=0;ring<=bands;ring++)for(int i=0;i<count;i++)
        {
            int side=i/3;float u=(i%3)/3f;
            float xx=side==0?-size/2+u*size:side==1?size/2:side==2?size/2-u*size:-size/2;
            float zz=side==0?-size/2:side==1?-size/2+u*size:side==2?size/2:size/2-u*size;
            xx*=waist[ring];zz*=waist[ring];
            // 맨 위 링은 잔디 밑에 딱 맞아야 하므로 흔들지 않는다. 층 경계는 흔들어야 띠가 살아난다.
            float jitter=ring==0?.015f:.115f;
            float lift=(ring==0||ring==bands)?0f:R(-.10f,.10f);
            rings[ring,i]=new Vector3(x+xx+R(-jitter,jitter),top-depth*cut[ring]+lift,z+zz+R(-jitter,jitter));
        }
        for(int ring=0;ring<bands;ring++)for(int i=0;i<count;i++)
        {
            int j=(i+1)%count;Color col=C(cliffStrata[ring])*R(.88f,1.08f);col.a=1;
            Tri(v,t,colors,rings[ring,i],rings[ring,j],rings[ring+1,i],col);
            Tri(v,t,colors,rings[ring,j],rings[ring+1,j],rings[ring+1,i],col);
        }
        MeshObject("Stratified cliff",v,t,colors);
    }

    /// <summary>잔디 오버행. 윗면 풀이 절벽 모서리 위로 넘어와야 흙덩이에 잔디를 얹은 모형으로 보이지 않는다.</summary>
    void GrassLip(float x,float z,float half,int dx,int dz,float y)
    {
        const float over=.115f,thick=.125f;
        if(dx!=0)Cube("Grass overhang",x+dx*(half+over*.5f),y,z,over,thick,half*2+over*1.4f,C("70873f"));
        else     Cube("Grass overhang",x,y,z+dz*(half+over*.5f),half*2+over*1.4f,thick,over,C("70873f"));
        // 처마 밑은 그늘이다. 얇은 어두운 띠 하나가 오버행을 실루엣으로 읽히게 한다.
        if(dx!=0)Cube("Overhang shade",x+dx*(half+over*.45f),y-.085f,z,over*.8f,.05f,half*2+over,C("4d5b2c"));
        else     Cube("Overhang shade",x,y-.085f,z+dz*(half+over*.45f),half*2+over,.05f,over*.8f,C("4d5b2c"));
    }

    /// <summary>
    /// 칸(x,z)의 (dx,dz) 변 위의 한 점.
    ///
    /// **alongOffset 은 칸 중심 기준 상대 오프셋이다.** 절대 좌표가 아니다 —
    /// 안쪽에서 x/z 를 이미 더하므로 호출자가 z 나 x 를 섞어 넣으면 중심이 두 번 더해져
    /// 메시가 원점에서 멀어질수록 크게 날아간다. 원점 근처에서만 우연히 멀쩡해 보이므로
    /// 눈으로는 안 잡힌다 — 실제로 바다 위에 검은 경사판이 흩어지고 나서야 찾았다.
    /// 범위는 -half ~ +half.
    /// </summary>
    Vector3 EdgePoint(float x,float z,float half,int dx,int dz,float alongOffset,float y,float outward)
        =>new Vector3(x+(dx!=0?dx*(half+outward):alongOffset),y,z+(dz!=0?dz*(half+outward):alongOffset));

    /// <summary>
    /// 수중으로 흘러내리는 절벽 밑동. 수면 -1.70 에서 상면 .73 까지 2.43 을 곧게 세우면
    /// 칸 한 변 1.47 의 1.65배짜리 기둥이 된다. 어깨(상면-1.76)부터 바깥으로 눕혀
    /// 보이는 수직 벽을 1.2배로 줄이고 밑동을 두껍게 만든다. 드러난 변에만 붙이므로
    /// 칸과 칸 사이 이음매는 그대로 붙어 있는다.
    /// </summary>
    void CliffApron(float x,float z,float half,int dx,int dz,float shoulder)
    {
        var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
        const int steps=4;
        for(int i=0;i<steps;i++)
        {
            float u0=-half+i*(half*2/steps),u1=-half+(i+1)*(half*2/steps);
            float o0=R(.30f,.48f),o1=R(.30f,.48f);
            Vector3 a=EdgePoint(x,z,half,dx,dz,u0,shoulder,.02f);
            Vector3 b=EdgePoint(x,z,half,dx,dz,u1,shoulder,.02f);
            Vector3 c=EdgePoint(x,z,half,dx,dz,u0,-2.05f,o0);
            Vector3 d=EdgePoint(x,z,half,dx,dz,u1,-2.05f,o1);
            Color col=C("5c4430")*R(.82f,1.04f);col.a=1;
            // 네 방향에 같은 코드를 쓰려면 앞뒷면을 모두 깔아야 한다. 한쪽만 감으면 절반이 사라진다.
            Tri(v,t,colors,a,c,b,col);Tri(v,t,colors,b,c,d,col);
            Tri(v,t,colors,a,b,c,col);Tri(v,t,colors,b,d,c,col);
        }
        MeshObject("Cliff talus",v,t,colors);
        for(int j=0;j<2;j++)
            Rock(EdgePoint(x,z,half,dx,dz,R(-half*.8f,half*.8f),-1.60f+R(0f,.20f),R(.10f,.34f)),
                 new Vector3(R(.16f,.30f),R(.14f,.26f),R(.16f,.28f)),C("6a5238"),"Cliff scree");
    }

    /// <summary>해안 포말. 드러난 변에만 붙는다 — 섬 모양이 바뀌면 포말도 같이 따라온다.</summary>

    /// <summary>붕괴 자리에 남는 바위 파편 더미(시안 우하단). 무너진 절벽 발치에 각진 덩어리가 쌓이고 바다로 흘러내린다.</summary>
    void RockRubble(float x,float z,int dx,int dz)
    {
        float ax=dz!=0?1f:0f, az=dx!=0?1f:0f;   // 무너진 벽을 따라가는 축
        for(int j=0;j<3;j++)
        {
            float u=R(-.42f,.42f);
            Rock(new Vector3(x+dx*(.56f-j*.05f)+ax*u,-1.30f+j*.38f,z+dz*(.56f-j*.05f)+az*u),
                 new Vector3(R(.34f,.52f),R(.28f,.44f),R(.34f,.52f)),
                 C(j==0?"6a5238":j==1?"7b6041":"8a6b4a"),"Collapse rubble");
        }
        for(int j=0;j<5;j++)
        {
            float u=R(-.64f,.64f),outward=R(.05f,.54f);
            Rock(new Vector3(x+dx*(.56f-outward)+ax*u,-1.60f+R(0f,.26f),z+dz*(.56f-outward)+az*u),
                 new Vector3(R(.15f,.35f),R(.14f,.28f),R(.15f,.35f)),
                 C(j%2==0?"705942":"5b4832"),"Collapse debris");
        }
    }

    /// <summary>사라진 칸. 예전엔 y=-1.63 의 흐린 두 변이라 파도에 묻혔다 — 수면 위로 띄우고 그림자를 깔아 읽히게 한다.</summary>
    void LostPlot(int gx,int gy,float x,float z)
    {
        DashedOutline(x,-1.585f,z,1.335f,.085f,.03f,.255f,.155f,C("1d3b48"),"Lost plot shadow");
        DashedOutline(x,-1.455f,z,1.300f,.055f,.05f,.255f,.155f,C("d9e8ea"),"Lost plot boundary");
        for(int d=0;d<4;d++)
        {
            int dx=d==0?0:d==1?0:d==2?-1:1, dz=d==0?-1:d==1?1:0;
            if(!Land(gx+dx,gy+dz))continue;
            RockRubble(x,z,dx,dz);break;   // 무너진 절벽은 한 면이다. 사방에 쌓으면 더미가 섬을 두른다
        }
    }

    /// <summary>본섬에서 떨어져 나온 잔디 조각. 시안의 좌하단·좌중단.</summary>
    void IslandFragment(float x,float z,float size,float top,int tufts)
    {
        Cliff(x,z,size,top,top+2.55f);
        Cube("Severed turf",x,top+.028f,z,size*.98f,.15f,size*.98f,grass);
        for(int d=0;d<4;d++)
        {
            int dx=d==0?-1:d==1?1:0,dz=d==2?-1:d==3?1:0;
            GrassLip(x,z,size*.5f,dx,dz,top-.026f);
            CliffApron(x,z,size*.5f,dx,dz,top-1.32f);
            AddCoastSegment(x,z,size*.5f,dx,dz);
        }
        for(int j=0;j<tufts;j++)GrassTuft(x+R(-size*.40f,size*.40f),top+.10f,z+R(-size*.40f,size*.40f),R(.08f,.18f));
        for(int j=0;j<2;j++)Rock(new Vector3(x+R(-size*.30f,size*.30f),top+.12f,z+R(-size*.30f,size*.30f)),new Vector3(R(.12f,.24f),R(.09f,.16f),R(.12f,.22f)),C("8b8574"));
    }

    void FarmScene()
    {
        // **바다는 이 함수의 맨 끝에서 만든다.** 해안 포말이 실제 해안선(CoastSegments)을 따라야
        // 하는데, 그 목록은 아래 밭 배치가 만든다. 예전에는 바다를 먼저 만들어서 포말이
        // 섬 중심으로부터의 거리, 즉 원으로 그려졌다 — 밭이 다 사라져도 같은 링이 남았다.
        // 원경과 안개는 해안선과 무관하므로 여기서 만든다.
        // 앞바다 바위는 구역 A 의 SeaStacks() 가 만든다. 여기서 또 만들면 두 벌이 겹친다.
        DistantIslands();
        MistLayers();

        // 땅의 지도를 먼저 만든다. 이 배열 하나가 실루엣·오버행·포말·파편·방벽을 전부 결정한다.
        CoastSegments.Clear();
        landGrid=new bool[6,5];
        for(int i=0;i<sim.Plots.Count;i++){var p=sim.Plots[i];landGrid[p.X,p.Y]=p.Active;}
        landGrid[1,4]=landGrid[2,4]=landGrid[3,4]=true;

        // 집이 있는 뒤쪽 단. 예전엔 6칸을 통째로 깔아 십자 위에 사각 블록을 얹은 꼴이었다 —
        // 위쪽 팔(x=2,3) 위와 그 왼쪽 한 칸까지만 남겨 실루엣이 사각형으로 되돌아가지 않게 한다.
        for(int i=0;i<3;i++)
        {
            int gx=i+1;float lx=(gx-2.5f)*1.47f;
            Cliff(lx,3.68f,1.47f,.75f,2.8f);
            Cube("Homestead turf",lx,.77f,3.68f,1.47f,.15f,1.47f,grass);
            for(int d=0;d<4;d++)
            {
                int dx=d==0?-1:d==1?1:0,dz=d==2?-1:d==3?1:0;
                if(Land(gx+dx,4+dz))continue;
                GrassLip(lx,3.68f,.735f,dx,dz,.726f);CliffApron(lx,3.68f,.735f,dx,dz,-1.01f);AddCoastSegment(lx,3.68f,.735f,dx,dz);
            }
        }
        Cottage(-.73f,3.6f);
        // 나무는 시안처럼 좌우 팔 위에 선다. 뒤쪽 단이 좁아져 예전 자리(±3.8, z=3.7)는 허공이다.
        Tree(-3.90f,.832f,1.28f,1.02f,true);Tree(3.50f,.832f,1.30f,.95f,true);
        Model("fence",new Vector3(.95f,.845f,3.95f),1.5f);
        Fence(new Vector3(-2.80f,.845f,4.32f),new Vector3(-1.45f,.845f,4.32f));Fence(new Vector3(-.15f,.845f,4.32f),new Vector3(1.35f,.845f,4.32f));

        for(int i=0;i<sim.Plots.Count;i++)
        {
            var p=sim.Plots[i];Vector3 pos=PlotPosition(p);float x=pos.x,z=pos.z;
            if(!Land(p.X,p.Y)){LostPlot(p.X,p.Y,x,z);continue;}
            // **누적 손상과 예보 위험은 다른 축이다.** 판정은 둘 다 Hud.cs 의 PlotScarred/PlotAtRisk
            // 한 쌍뿐이고, 예보 패널의 미니격자·숫자·문장이 같은 함수를 부른다. 예전에는 여기서만
            // 누적 침식 .68 을 따로 재서, 붉은 점선이 깔린 화면 옆에 "0칸 위험" 예보가 같이 떠 있었다.
            bool scarred=PlotScarred(p);   // 이미 깎여 나갔다 → 갈색: 맨흙 · 드러난 돌 · 균열
            bool atRisk=PlotAtRisk(p);     // 지금 추세로 곧 사라진다 → 붉은색: 점선 테두리 하나뿐
            Cliff(x,z,1.47f,.73f,2.85f);
            var tile=Cube(p.Id,x,.755f,z,1.445f,.15f,1.445f,scarred?Color.Lerp(grass,C("7b6245"),.70f):grass);
            tile.AddComponent<PlotPick>().index=i;
            Cube("Cultivated earth",x,.846f,z,1.08f,.035f,1.07f,C(scarred?"55402c":"645037"));
            for(int row=0;row<4;row++)Cube("Ploughed furrow",x-.39f+row*.26f,.871f,z,.058f,.045f,.97f,C(scarred?"6a513a":"796144"));
            // 드러난 변마다 잔디 처마와 해안 포말. 십자 모양이 바뀌면 여기가 따라 바뀐다.
            for(int d=0;d<4;d++)
            {
                int dx=d==0?-1:d==1?1:0,dz=d==2?-1:d==3?1:0;
                if(Land(p.X+dx,p.Y+dz))continue;
                GrassLip(x,z,.735f,dx,dz,.706f);CliffApron(x,z,.735f,dx,dz,-1.03f);AddCoastSegment(x,z,.735f,dx,dz);
                // 처마 위 풀은 **오버행의 실루엣을 읽히게 하는 용도**다. 변마다 셋이면 테두리가
                // 연속된 띠가 되어 오히려 처마가 안 보인다. 둘이면 끊겨서 처마 선이 드러난다.
                for(int j=0;j<2;j++)GrassTuft(x+(dx!=0?dx*.79f:R(-.66f,.66f)),.775f,z+(dz!=0?dz*.79f:R(-.66f,.66f)),R(.09f,.19f));
            }
            if(scarred)for(int j=0;j<6;j++)Rock(new Vector3(x+R(-.62f,.62f),.862f,z+R(-.62f,.62f)),new Vector3(R(.07f,.15f),R(.05f,.10f),R(.07f,.14f)),C("8b7c63"),"Exposed stone");
            // 성한 칸의 테두리 풀. 예전엔 여덟 포기라 네 변마다 둘씩 붙어 칸이 풀 액자에 갇혔다 —
            // 시안은 빈 잔디를 그냥 비워 둔다. 변마다 하나면 밭의 네 귀퉁이가 숨을 쉰다.
            else for(int j=0;j<4;j++){float side=j%4;float u=R(-.62f,.62f);float xx=side==0?-.63f:side==1?.63f:u;float zz=side==2?-.63f:side==3?.63f:u;GrassTuft(x+xx,.84f,z+zz,R(.06f,.16f));}
            if(p.HasCrop)CropArt(p,pos); else PlotDecor(p,pos);   // 빈 칸 장식은 구역 E 소유
            if(p.DefenseDaysLeft>0)
            {
                // 방벽은 실제로 물에 닿는 면에 선다. 늘 z- 면에 세우면 안쪽 칸의 방벽이 흙에 파묻힌다.
                int wx=0,wz=-1;
                for(int d=0;d<4;d++)
                {
                    int dx=d==2?-1:d==3?1:0,dz=d==0?-1:d==1?1:0;
                    if(!Land(p.X+dx,p.Y+dz)){wx=dx;wz=dz;break;}
                }
                RetainingWall(x,z,wx,wz);
            }
            if(i==selected)Outline(x,z,1.22f,C("f0cd68"));
            // 균열은 이미 깎여 나간 흔적이다 — 예보가 아니라 손상 쪽에 붙는다.
            if(scarred)for(int j=0;j<4;j++){Vector3 a=new Vector3(x+R(-.6f,.6f),.886f,z+R(-.6f,.6f));Beam(a,a+new Vector3(R(-.27f,.27f),0,R(.10f,.32f)),.021f,C("352e26"),"Erosion crack");}
            // 붉은 점선은 **예보 하나만** 쓴다. 미니격자의 붉은 테두리와 같은 칸이고 같은 함수다.
            // 성한 잔디 위에 떠 있어도 맞다 — 방벽 없는 모서리 칸은 깎이기 전부터 수명이 짧다.
            if(atRisk)
            {
                DashedOutline(x,.928f,z,1.335f,.052f,.055f,.16f,.10f,C("e2613f"),"Risk boundary");
                DashedOutline(x,.906f,z,1.335f,.072f,.03f,.16f,.10f,C("5e2a1c"),"Risk boundary shade");
            }
        }

        // 본섬과 떨어진 잔디 조각 셋. 시안에서 좌중단·좌하단에 있는 것들이고,
        // 이것이 있어야 "섬이 깎여 나간 자리"가 한 장면 안에서 읽힌다.
        // 조각의 풀은 밭보다 훨씬 좁은 면에 얹힌다 — 일곱 포기면 제곱 단위당 열 포기가 넘어
        // 조각이 풀 뭉치로 뭉개졌다. 잘려 나간 잔디 단면이 보이도록 성기게 둔다.
        IslandFragment(-6.30f,-2.40f,1.55f,.28f,4);
        IslandFragment(-3.20f,-5.30f,1.92f,.52f,5);
        IslandFragment(-5.10f,-7.20f,1.02f,.06f,3);

        // 집터 앞마당. 5.7 제곱 단위에 마흔 포기면 평당 일곱 포기라 잔디밭이 아니라 덤불이 된다.
        // 열여섯이면 집·울타리·나무의 실루엣이 풀 위로 올라온다.
        for(int j=0;j<16;j++)GrassTuft(R(-2.85f,1.40f),.86f,R(3.00f,4.35f),R(.1f,.22f));
        // 서쪽 절벽에 기대선 바위. 예전 자리(z=1.5, 옛 6x4 서쪽 변)는 십자에서는 허공이라 팔 쪽으로 당겼다 —
        // 절벽면에 닿아 있어야 물 위에 뜬 덩어리로 보이지 않는다.
        for(int j=0;j<4;j++)Rock(new Vector3(-4.62f-j*.05f,-.15f,.30f+j*.27f),new Vector3(.28f,.48f,.25f),C("767b69"));

        // 이제 CoastSegments 가 채워졌다. 바다는 그것을 읽어 포말을 세운다.
        SeaSurface();
    }

    void RetainingWall(float x,float z,int dx=0,int dz=-1)
    {
        // Tall beams support the cliff face, as in the reference, rather than a garden fence.
        float wx=x+dx*.73f,wz=z+dz*.73f;
        float w=dx!=0?.14f:1.40f,d=dz!=0?.14f:1.40f;
        for(int j=0;j<5;j++)Cube("Retaining plank",wx,-.55f+j*.32f,wz,w,.26f,d,C(j%2==0?"8b7350":"766043"));
        for(int side=-1;side<=1;side+=2)
        {
            float px=dx!=0?wx-dx*.07f:x+side*.56f;
            float pz=dz!=0?wz-dz*.07f:z+side*.56f;
            Cube("Retaining post",px,-.02f,pz,dx!=0?.17f:.13f,2.0f,dz!=0?.17f:.13f,C("a28a5e"));
        }
    }

    /// <summary>선택 칸. 시안처럼 끊기지 않는 금색 띠라서 위험 칸의 붉은 점선과 한눈에 구별된다.</summary>
    void Outline(float x,float z,float size,Color color)
    {
        for(int side=-1;side<=1;side+=2)
        {
            Cube("Plot outline",x+side*size/2,.912f,z,.042f,.038f,size+.042f,color);
            Cube("Plot outline",x,.912f,z+side*size/2,size+.042f,.038f,.042f,color);
        }
    }

    /// <summary>네 변을 두르는 점선. 사라진 칸의 윤곽과 위험 칸의 붉은 테두리가 같은 문법을 쓴다.</summary>
    void DashedOutline(float x,float y,float z,float size,float thickness,float height,float dash,float gap,Color color,string name)
    {
        float half=size*.5f;
        int n=Mathf.Max(3,Mathf.RoundToInt(size/(dash+gap)));
        float step=size/n;
        for(int i=0;i<n;i++)
        {
            float o=-half+step*(i+.5f);
            Cube(name,x-half,y,z+o,thickness,height,dash,color);
            Cube(name,x+half,y,z+o,thickness,height,dash,color);
            Cube(name,x+o,y,z-half,dash,height,thickness,color);
            Cube(name,x+o,y,z+half,dash,height,thickness,color);
        }
    }
}
