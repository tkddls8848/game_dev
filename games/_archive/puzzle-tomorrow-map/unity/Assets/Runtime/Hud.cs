using System;
using System.Linq;
using UnityEngine;
using FarmErosion.Sim;

// 구역 D — HUD. 승인 시안(docs/poc-gallery/lowpoly/farm-erosion-v1.png)의 정보 위계를 그대로 옮긴다.
//
// 시안과의 격차 세 가지를 여기서 없앤다:
//   · 밋밋한 사각 패널   → 반투명 짙은 남색 · 둥근 모서리 · 옅은 테두리 (RoundRect 9슬라이스)
//   · 아이콘이 없다      → 폰트 글리프가 아니라 부호거리장(SDF)으로 구운 벡터 아이콘 (Glyph)
//   · 디버그·조작키 문구 → 화면에서 뺀다. 조작 설명은 도움말 오버레이 안에만 둔다
//
// 좌표계: **세로만 900으로 고정하고 가로는 화면 비율을 따른다.**
// 예전처럼 1440x900 캔버스를 화면에 늘려 맞추면 16:9에서 글자와 아이콘이 가로로 11% 늘어난다.
// 그래서 오른쪽에 붙는 것은 전부 화면 폭(w)에서 빼서 배치한다.
public sealed partial class LowpolyGame
{
    bool seedPicker; int action;   // 선택된 행동 0 심기 · 1 방벽 · 2 계절
    GUIStyle caption,heading,headRight,capRight,pillLabel,pillValue,btnTitle,btnCaption,forecast,toastText;
    GUIStyle panelFrame,pillFrame,btnFrame,btnHot,btnPick,btnOff;
    Texture2D icSprout,icLeaf,icLand,icWood,icWater,icWave,icSeed,icWall,icFold,icNext,icAlert;

    // ── 패널 · 알약 텍스처 ────────────────────────────────────────────────
    // 9슬라이스로 늘려 쓴다. 가운데 2픽셀만 늘어나므로 어떤 크기로 그려도 모서리 반지름이 같다.
    Texture2D RoundRect(int radius,Color fill,Color edge,float stroke)
    {
        int size=radius*2+2;
        var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float dx=Mathf.Abs(x+.5f-size*.5f)-1,dy=Mathf.Abs(y+.5f-size*.5f)-1;
            float d=new Vector2(Mathf.Max(dx,0),Mathf.Max(dy,0)).magnitude+Mathf.Min(Mathf.Max(dx,dy),0)-radius;
            float inside=Mathf.Clamp01(.5f-d),line=Mathf.Clamp01(stroke*.5f+.5f-Mathf.Abs(d+stroke*.5f));
            var c=Color.Lerp(fill,edge,line*edge.a); c.a=Mathf.Max(fill.a*inside,edge.a*line);
            pixels[y*size+x]=c;
        }
        tex.SetPixels(pixels);tex.Apply();return tex;
    }
    GUIStyle Frame(Texture2D tex,int radius)
    { var s=new GUIStyle{border=new RectOffset(radius,radius,radius,radius)}; s.normal.background=tex; return s; }

    // ── 아이콘 ────────────────────────────────────────────────────────────
    // 이모지 폰트도 스프라이트 아틀라스도 없다. 도형을 부호거리장으로 직접 구우면
    // 글리프 유무에 기대지 않고, 어느 해상도에서 그려도 가장자리가 부드럽다.
    // 좌표는 -0.5..0.5, +y가 위다.
    struct Ink { public Func<Vector2,float> field; public Color color; }
    static Ink Layer(Func<Vector2,float> field,Color color)=>new Ink{field=field,color=color};
    static float SdCircle(Vector2 p,float cx,float cy,float r)=>(p-new Vector2(cx,cy)).magnitude-r;
    static float SdBox(Vector2 p,float cx,float cy,float hw,float hh)
    { var d=new Vector2(Mathf.Abs(p.x-cx)-hw,Mathf.Abs(p.y-cy)-hh); return new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude+Mathf.Min(Mathf.Max(d.x,d.y),0); }
    static float SdSeg(Vector2 p,Vector2 a,Vector2 b,float half)
    { var pa=p-a;var ba=b-a;float h=Mathf.Clamp01(Vector2.Dot(pa,ba)/Vector2.Dot(ba,ba)); return (pa-ba*h).magnitude-half; }
    static float SdPoly(Vector2 p,Vector2[] v)
    {
        float d=Vector2.Dot(p-v[0],p-v[0]); bool inside=false;
        for(int i=0,j=v.Length-1;i<v.Length;j=i++)
        {
            var e=v[j]-v[i];var w=p-v[i];
            var b=w-e*Mathf.Clamp01(Vector2.Dot(w,e)/Vector2.Dot(e,e));
            d=Mathf.Min(d,Vector2.Dot(b,b));
            bool c1=p.y>=v[i].y,c2=p.y<v[j].y,c3=e.x*w.y>e.y*w.x;
            if((c1&&c2&&c3)||(!c1&&!c2&&!c3))inside=!inside;
        }
        return (inside?-1:1)*Mathf.Sqrt(d);
    }
    static float SdWave(Vector2 p,float y,float amp,float half)
    {
        float d=9;
        for(int i=0;i<10;i++){float x0=-.42f+i*.084f,x1=x0+.084f;d=Mathf.Min(d,SdSeg(p,new Vector2(x0,y+Mathf.Sin(x0*14f)*amp),new Vector2(x1,y+Mathf.Sin(x1*14f)*amp),half));}
        return d;
    }
    Texture2D Glyph(params Ink[] layers)
    {
        const int size=64; float aa=1f/size;   // 한 픽셀 폭. 이 폭으로 경계를 흐려야 계단이 안 보인다
        var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            var p=new Vector2((x+.5f)/size-.5f,(y+.5f)/size-.5f);
            var c=new Color(1,1,1,0);
            foreach(var layer in layers)
            {
                float cover=Mathf.Clamp01(.5f-layer.field(p)/aa); if(cover<=0)continue;
                float a=cover*layer.color.a;
                c=new Color(Mathf.Lerp(c.r,layer.color.r,a),Mathf.Lerp(c.g,layer.color.g,a),Mathf.Lerp(c.b,layer.color.b,a),c.a+(1-c.a)*a);
            }
            pixels[y*size+x]=c;
        }
        tex.SetPixels(pixels);tex.Apply();return tex;
    }
    void BuildIcons()
    {
        Color leafA=C("8fc766"),leafB=C("6e9f4c");
        // 새싹: 줄기 하나에 잎 둘. 두 원의 교집합(렌즈)이 잎 실루엣이다.
        icSprout=Glyph(
            Layer(p=>SdSeg(p,new Vector2(-.02f,-.44f),new Vector2(.01f,.10f),.045f),leafB),
            Layer(p=>Mathf.Max(SdCircle(p,.33f,.02f,.38f),SdCircle(p,.05f,.34f,.38f)),leafA),
            Layer(p=>Mathf.Max(SdCircle(p,-.30f,.10f,.32f),SdCircle(p,-.04f,.34f,.32f)),leafB));
        // 계절 잎: 흰 마스크로 굽고 그릴 때 계절색으로 물들인다(봄 연두 → 가을 주황).
        Func<Vector2,float> leaf=p=>Mathf.Max(SdCircle(p,.36f,-.12f,.56f),SdCircle(p,-.12f,.36f,.56f));
        icLeaf=Glyph(
            Layer(leaf,Color.white),
            Layer(p=>Mathf.Max(leaf(p)+.055f,SdSeg(p,new Vector2(-.34f,-.34f),new Vector2(.34f,.34f),.022f)),new Color(0,0,0,.30f)));
        // 남은 땅: 잔디를 인 흙 블록. 윗면·측면 색을 갈라야 3D 화면의 칸과 같은 물건으로 읽힌다.
        var top=new[]{new Vector2(0,.36f),new Vector2(.44f,.10f),new Vector2(0,-.16f),new Vector2(-.44f,.10f)};
        var lft=new[]{new Vector2(-.44f,.10f),new Vector2(0,-.16f),new Vector2(0,-.42f),new Vector2(-.44f,-.16f)};
        var rgt=new[]{new Vector2(0,-.16f),new Vector2(.44f,.10f),new Vector2(.44f,-.16f),new Vector2(0,-.42f)};
        icLand=Glyph(Layer(p=>SdPoly(p,lft),C("55402c")),Layer(p=>SdPoly(p,rgt),C("7a5c3e")),Layer(p=>SdPoly(p,top),C("7fa650")));
        // 목재: 통나무. 마구리의 나이테 하나가 있어야 막대가 아니라 나무로 읽힌다.
        icWood=Glyph(
            Layer(p=>SdSeg(p,new Vector2(-.26f,-.13f),new Vector2(.28f,.13f),.17f),C("8a6440")),
            Layer(p=>SdCircle(p,-.26f,-.13f,.165f),C("b58a5c")),
            Layer(p=>Mathf.Abs(SdCircle(p,-.26f,-.13f,.095f))-.024f,C("8a6440")));
        var dropTip=new[]{new Vector2(0,.46f),new Vector2(-.29f,-.13f),new Vector2(.29f,-.13f)};
        icWater=Glyph(
            Layer(p=>Mathf.Min(SdCircle(p,0,-.13f,.29f),SdPoly(p,dropTip)),C("5aa9e6")),
            Layer(p=>SdCircle(p,-.10f,-.14f,.072f),C("c6e6ff")));
        icWave=Glyph(
            Layer(p=>SdWave(p,.13f,.055f,.033f),C("8fc4dd")),
            Layer(p=>SdWave(p,-.02f,.055f,.033f),C("6fa9c6")),
            Layer(p=>SdWave(p,-.17f,.055f,.033f),C("5991b0")));
        icSeed=Glyph(
            Layer(p=>Mathf.Max(SdCircle(p,.30f,-.06f,.50f),SdCircle(p,-.10f,.34f,.50f)),Color.white),
            Layer(p=>SdSeg(p,new Vector2(-.30f,-.36f),new Vector2(.02f,.02f),.038f),Color.white));
        // 방벽: 기둥 둘에 널 둘. 3D의 나무 널 방벽과 같은 실루엣이다.
        icWall=Glyph(
            Layer(p=>SdBox(p,-.27f,0,.085f,.42f),Color.white),Layer(p=>SdBox(p,.27f,0,.085f,.42f),Color.white),
            Layer(p=>SdBox(p,0,.15f,.40f,.075f),Color.white),Layer(p=>SdBox(p,0,-.15f,.40f,.075f),Color.white));
        // 접기: 층이 아래로 내려앉는 모양. 방벽(버팀)과 반대 방향이라 한눈에 갈린다.
        icFold=Glyph(
            Layer(p=>SdBox(p,0,.30f,.40f,.070f),Color.white),
            Layer(p=>SdBox(p,0,.04f,.30f,.070f),Color.white),
            Layer(p=>SdBox(p,0,-.22f,.20f,.070f),Color.white));
        icNext=Glyph(
            Layer(p=>SdPoly(p,new[]{new Vector2(-.40f,.30f),new Vector2(-.02f,0),new Vector2(-.40f,-.30f)}),Color.white),
            Layer(p=>SdPoly(p,new[]{new Vector2(.02f,.30f),new Vector2(.40f,0),new Vector2(.02f,-.30f)}),Color.white));
        icAlert=Glyph(
            Layer(p=>Mathf.Abs(SdCircle(p,0,0,.38f))-.055f,Color.white),
            Layer(p=>SdBox(p,0,.07f,.048f,.155f),Color.white),
            Layer(p=>SdCircle(p,0,-.19f,.058f),Color.white));
    }
    void InitStyles()
    {
        text=new GUIStyle(GUI.skin.label){font=font,fontSize=19,wordWrap=true};text.normal.textColor=cream;
        title=new GUIStyle(text){fontSize=36,fontStyle=FontStyle.Bold,wordWrap=false};
        small=new GUIStyle(text){fontSize=16,wordWrap=false};small.normal.textColor=C("aebfc7");
        caption=new GUIStyle(text){fontSize=15};caption.normal.textColor=C("9fb2bb");
        heading=new GUIStyle(text){fontSize=23,fontStyle=FontStyle.Bold,wordWrap=false};
        headRight=new GUIStyle(heading){alignment=TextAnchor.MiddleRight};
        capRight=new GUIStyle(caption){alignment=TextAnchor.MiddleRight,wordWrap=false};
        pillLabel=new GUIStyle(text){fontSize=18,alignment=TextAnchor.MiddleLeft,wordWrap=false};pillLabel.normal.textColor=C("bccdd4");
        pillValue=new GUIStyle(text){fontSize=25,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,wordWrap=false};pillValue.normal.textColor=Color.white;
        btnTitle=new GUIStyle(text){fontSize=22,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleLeft,wordWrap=false};
        btnCaption=new GUIStyle(text){fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=false};btnCaption.normal.textColor=C("94a7b0");
        forecast=new GUIStyle(text){fontSize=22,fontStyle=FontStyle.Bold,richText=true,wordWrap=false};
        toastText=new GUIStyle(text){fontSize=16,alignment=TextAnchor.MiddleLeft,wordWrap=true};toastText.normal.textColor=C("dfe7ea");

        // 반투명 짙은 남색 + 옅은 테두리. 바다 위에 떠 있어야 하므로 완전히 막지 않는다.
        panelFrame=Frame(RoundRect(13,new Color(.055f,.082f,.125f,.86f),new Color(.44f,.58f,.66f,.40f),1.4f),13);
        pillFrame =Frame(RoundRect(23,new Color(.065f,.098f,.147f,.88f),new Color(.44f,.58f,.66f,.44f),1.4f),23);
        btnFrame  =Frame(RoundRect(13,new Color(.070f,.100f,.150f,.90f),new Color(.44f,.58f,.66f,.38f),1.4f),13);
        btnHot    =Frame(RoundRect(13,new Color(.105f,.147f,.205f,.94f),new Color(.60f,.74f,.80f,.52f),1.5f),13);
        btnPick   =Frame(RoundRect(13,new Color(.105f,.128f,.150f,.94f),new Color(.878f,.702f,.337f,1f),2.2f),13);   // 금색 테두리
        btnOff    =Frame(RoundRect(13,new Color(.055f,.075f,.105f,.72f),new Color(.40f,.50f,.56f,.22f),1.2f),13);
        button=new GUIStyle(GUI.skin.button){font=font,fontSize=18,wordWrap=true,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(12,12,6,6),border=new RectOffset(13,13,13,13)};
        button.normal.background=btnFrame.normal.background;button.hover.background=btnHot.normal.background;button.active.background=btnPick.normal.background;
        button.normal.textColor=cream;button.hover.textColor=Color.white;button.active.textColor=Color.white;
        BuildIcons(); styles=true;
    }

    // ── 그리기 원시 함수 ──────────────────────────────────────────────────
    void Panel(float x,float y,float w,float h,Color color)
    { GUI.color=color;GUI.DrawTexture(new Rect(x,y,w,h),Texture2D.whiteTexture);GUI.color=Color.white; }
    void Card(float x,float y,float w,float h)=>GUI.Box(new Rect(x,y,w,h),GUIContent.none,panelFrame);
    void Pill(float x,float y,float w,float h)=>GUI.Box(new Rect(x,y,w,h),GUIContent.none,pillFrame);
    void Label(float x,float y,float w,float h,string value,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,h),value,style??text);
    void Label(float x,float y,float w,float h,string value,GUIStyle style,Color color)
    { var keep=style.normal.textColor;style.normal.textColor=color;GUI.Label(new Rect(x,y,w,h),value,style);style.normal.textColor=keep; }
    void ShadowText(float x,float y,float w,float h,string value,GUIStyle style)
    { Color col=style.normal.textColor;style.normal.textColor=new Color(.02f,.035f,.05f,.80f);Label(x+1,y+2,w,h,value,style);style.normal.textColor=col;Label(x,y,w,h,value,style); }
    void Icon(Texture2D tex,float x,float y,float size,Color tint)
    { GUI.color=tint;GUI.DrawTexture(new Rect(x,y,size,size),tex);GUI.color=Color.white; }
    void Stroke(float x,float y,float w,float h,float t,Color color)
    { Panel(x,y,w,t,color);Panel(x,y+h-t,w,t,color);Panel(x,y,t,h,color);Panel(x+w-t,y,t,h,color); }
    void Rule(float x,float y,float w)=>Panel(x,y,w,1,new Color(.62f,.74f,.80f,.30f));
    float Width(GUIStyle style,string value)=>style.CalcSize(new GUIContent(value)).x;
    bool Button(float x,float y,float w,float h,string value,bool enabled=true)
    { bool was=GUI.enabled;GUI.enabled=was&&enabled;bool result=GUI.Button(new Rect(x,y,w,h),value,button);GUI.enabled=was;if(result)Sound();return result; }

    void OnGUI()
    {
        if(cam==null)return; if(!styles)InitStyles();
        // 세로만 고정한다. 가로로 늘리면 글자와 아이콘이 찌그러진다.
        float s=Screen.height/900f; GUI.matrix=Matrix4x4.Scale(new Vector3(s,s,1));
        float w=Screen.width/s;
        // 도움말이 열려 있으면 뒤쪽 HUD 는 흐려지고 눌리지 않는다. 안 그러면 카드 뒤 버튼이 눌린다.
        GUI.enabled=!help;
        if(!EndingHidesAll)TitleBlock();
        if(farm)FarmUI(w); else MapUI(w);
        GUI.enabled=true;
        // 조작키 안내는 화면에서 뺀다. 도움말 안에만 둔다.
        if(!EndingHidesActions&&Button(w-62,842,40,40,"?"))help=!help;
        if(help)HelpUI(w);
        ChoiceUI(w);  // 난이도. 여기서 §0-1 의 대비가 처음 드러난다
        IntroUI(w);   // 그 위에 그린다 — HUD 를 덮어야 첫인상이 산다
        EndingUI(w);  // 그보다 위다. 끝은 아무것에도 가리지 않는다
    }
    void TitleBlock()
    {
        Icon(icSprout,30,22,42,Color.white);
        // 고른 난이도는 부제 **옆에** 조용히 한 번만 적는다. 화면 어디에도 다시 나오지 않는다.
        // 아래에 두면 부제(y 66, 높이 26)와 겹친다 — 한 번 겪었다.
        if(farm&&sim!=null&&sim.Level!=null&&data.Difficulty!=null)
            Label(336,66,240,26,sim.Level.nameKo,caption);
        ShadowText(82,16,860,50,farm?"밭이 줄어드는 세계":"내 일 의 지 도",title);
        ShadowText(85,66,700,26,farm?"오늘도, 조금 더 작아진 우리의 땅":"작은 배치가, 조금 더 나은 내일을 만든다.",small);
    }
    Color SeasonTint(string id)=>id=="spring"?C("9ed16f"):id=="summer"?C("5fae57"):id=="autumn"?C("e08a3c"):C("a8c4d6");

    // ── 위험 정의 — **화면 전체에서 여기 하나뿐이다** ──────────────────────────
    //
    // 예전에는 두 곳이 서로 **다른 것**을 재고 있었다:
    //   월드(ArtIsland.FarmScene) : 누적 침식 / 임계 > .68   — 지금까지 받은 손상
    //   예보(ForecastPanel)       : EstimatedDaysLeft <= 28  — 지금 추세의 잔여 수명
    // 그래서 붉은 점선이 여러 칸에 떠 있는데 예보는 "0칸 위험"이라고 말했다. 숫자를 억지로
    // 맞추는 건 해결이 아니다 — **정의를 하나로 줄이는 것**이 해결이다.
    //
    // 남긴 정의는 **잔여 수명**이다. 누적 손상은 같은 값이라도 방벽·작물·노출 변에 따라 실제로
    // 남은 날이 다르다(침식 1400 이어도 방벽을 세우면 한참 버틴다). 플레이어가 알아야 하는 것은
    // "이 칸에 지금 손을 써야 하나"이고, 그 질문에 답하는 것은 잔여 수명뿐이다.
    // 누적 손상은 버리지 않는다 — **붉은 예보 테두리와 다른 표현**(맨흙·드러난 돌·균열, 미니격자의
    // 갈색 바탕)으로 남겨 둔다. 두 정보가 같은 기호를 쓰면 화면이 다시 거짓말을 한다.
    //
    // 시간 범위도 여기 하나뿐이다. **'다음 계절'이 아니라 오늘부터 이만큼의 날**이다 —
    // Simulation.EstimatedDaysLeft() 는 현재 계절·현재 연차의 하루 침식을 그대로 연장하지
    // 계절 경계·전환 침식·방벽 만료·수확 후 guard 소실을 굴리지 않는다. 문구도 그렇게 쓴다.
    public int RiskHorizonDays=>data.Seasons.daysPerSeason;

    /// <summary>지금 추세로 이 칸이 며칠 더 버티는가. 이미 사라진 칸은 0.</summary>
    public int PlotDaysLeft(PlotState p)
    {
        if(!p.Active)return 0;
        var crop=p.HasCrop?data.Crop(p.CropId):null;
        return sim.EstimatedDaysLeft(p,data.SeasonOfDay(sim.CurrentDay),data.YearOfDay(sim.CurrentDay),crop!=null?crop.erosionGuard:0);
    }

    /// <summary>붉은 예보 표시가 붙는 칸. 월드 점선·미니격자 테두리·예보 숫자·예보 문장이 전부 이 하나를 부른다.</summary>
    public bool PlotAtRisk(PlotState p)=>p.Active&&PlotDaysLeft(p)<=RiskHorizonDays;

    /// <summary>누적 손상 비율. 위험과 **다른 축**이다 — 얼마나 깎여 나갔는가만 말하고 남은 날은 말하지 않는다.</summary>
    public float PlotWear(PlotState p)=>Mathf.Clamp01((float)p.Erosion/Mathf.Max(1,data.Plots.lossThresholdErosion));

    /// <summary>흙이 드러날 만큼 깎인 칸. 갈색 계열(맨흙·드러난 돌·균열·미니격자 갈색 바탕)만 쓴다.</summary>
    public bool PlotScarred(PlotState p)=>p.Active&&PlotWear(p)>.68f;

    // ── 농장 ──────────────────────────────────────────────────────────────
    void FarmUI(float w)
    {
        // **끝난 뒤에는 화면을 덜어 낸다.** 어둡게 하는 것이 아니라 치우는 것이다(DIRECTION §2-7).
        // 1번 박자에서는 아무것도 치우지 않는다 — HUD 가 그대로 도는 것이 "쓸려가버렸다"다.
        if(!EndingHidesAll){ ResourcePills(w); SeasonBlock(w); ForecastPanel(w); }
        if(!EndingHidesActions){ ActionButtons(w); Toast(w); }
        if(seedPicker&&!sim.Ended)SeedPicker(w);
        JourneyUI(w);
    }
    void ResourcePills(float w)
    {
        int timber=sim.Money/Math.Max(1,data.Plots.defense.cost);   // 방벽 한 채 = 목재 한 묶음
        var icons=new[]{icLand,icWood,icWater};
        var labels=new[]{"남은 땅","목재","물"};
        var values=new[]{$"{sim.ActiveCount()} / {sim.Plots.Count}",timber.ToString(),
                         Math.Max(0,data.Economy.wellWaterPerDay+sim.CurrentWeather.waterDelta).ToString()};
        const float h=46,gap=12,pad=20,ic=26,inner=9;
        var widths=new float[3];float total=gap*2;
        for(int i=0;i<3;i++){widths[i]=pad*2+ic+inner+Width(pillLabel,labels[i])+inner+Width(pillValue,values[i]);total+=widths[i];}
        float x=w/2-total/2,y=17;
        for(int i=0;i<3;i++)
        {
            Pill(x,y,widths[i],h);
            Icon(icons[i],x+pad,y+(h-ic)/2,ic,Color.white);
            float lw=Width(pillLabel,labels[i]);
            Label(x+pad+ic+inner,y,lw+4,h,labels[i],pillLabel);
            Label(x+pad+ic+inner+lw+inner,y,widths[i],h,values[i],pillValue);
            x+=widths[i]+gap;
        }
    }
    void SeasonBlock(float w)
    {
        var season=data.SeasonOfDay(sim.CurrentDay);
        Icon(icLeaf,w-68,18,36,SeasonTint(season.id));
        ShadowText(w-640,16,560,40,$"{data.YearOfDay(sim.CurrentDay)}년차 · {season.nameKo}",headRight);
        ShadowText(w-640,58,606,26,$"{data.DayInSeason(sim.CurrentDay)}일 · {sim.CurrentWeather.nameKo}",capRight);
    }
    void ForecastPanel(float w)
    {
        int gh=data.Plots.gridHeight,horizon=RiskHorizonDays;
        // 격자·숫자·문장이 어긋나면 안 된다. 한 번 세어 셋 다 쓴다.
        // **월드의 붉은 점선도 같은 PlotAtRisk 를 부른다** — 그래서 화면과 예보가 같은 칸을 가리킨다.
        var risky=new bool[sim.Plots.Count];int danger=0,alive=0;
        for(int i=0;i<sim.Plots.Count;i++)
        {
            var p=sim.Plots[i]; if(!p.Active)continue;
            alive++; risky[i]=PlotAtRisk(p); if(risky[i])danger++;
        }
        const float pw=302,cell=20,gap=3;
        float px=w-pw-34,py=116;
        float gx=px+82,gy=py+78,gridH=gh*cell+(gh-1)*gap;
        Card(px,py,pw,340);
        Label(px+24,py+13,200,34,"침식 예보",heading);
        Rule(px+24,py+54,pw-48);
        Icon(icWave,px+14,gy+gridH/2-26,52,Color.white);
        Icon(icWave,px+236,gy+gridH/2-26,52,Color.white);
        for(int i=0;i<sim.Plots.Count;i++)
        {
            var p=sim.Plots[i];
            float cx=gx+p.X*(cell+gap),cy=gy+(gh-1-p.Y)*(cell+gap);
            // 바탕색은 **누적 손상**이다. 월드에서 잔디가 벗겨진 칸과 같은 갈색을 쓴다 — 두 화면이 같은 말을 한다.
            Panel(cx,cy,cell,cell,p.Active?Color.Lerp(C("7fa650"),C("7b6245"),PlotScarred(p)?.70f:0f)
                                          :new Color(.14f,.22f,.28f,.75f));
            if(!p.Active)Stroke(cx,cy,cell,cell,1,new Color(.53f,.61f,.61f,.35f));
            // 붉은 테두리는 **예보 위험 하나만** 쓴다. 누적 손상은 위의 갈색 바탕이 맡는다.
            if(risky[i])Stroke(cx,cy,cell,cell,2,C("e0614a"));
            if(i==selected)Stroke(cx-2,cy-2,cell+4,cell+4,2,C("f0cd7f"));
        }
        // '다음 계절'이라고 쓰던 자리다. 실제 계산은 계절 경계를 넘지 않고 **오늘부터 horizon 일**을
        // 현재 추세로 연장한 값이므로, 화면도 그대로 말한다. 0칸이면 숫자도 붉지 않다.
        Label(px+24,py+178,pw-48,34,$"앞으로 {horizon}일 · <color=#{(danger>0?"e8604a":"9ec97a")}>{danger}칸</color> 위험",forecast);
        Label(px+24,py+216,pw-48,50,
            alive==0  ?"남은 땅이 없습니다."
            :danger>0 ?$"지금 추세라면 {danger}칸을\n파도가 가져갑니다."
                      :$"지금 추세라면 {horizon}일 동안\n잃는 땅이 없습니다.",caption);
        // 범례. 붉은 테두리(예보)와 갈색 바탕(누적 손상)이 다른 이야기라는 것을 화면 안에서 말한다.
        Rule(px+24,py+266,pw-48);
        Panel(px+24,py+280,14,14,C("7fa650"));Stroke(px+24,py+280,14,14,2,C("e0614a"));
        Label(px+46,py+275,pw-70,24,$"{horizon}일 안에 사라질 땅",caption);
        Panel(px+24,py+304,14,14,C("7b6245"));
        Label(px+46,py+299,pw-70,24,"이미 깎여 나간 흙",caption);
    }
    bool ActionButton(float x,float y,float w,float h,Texture2D icon,string label,string caption,bool picked,bool enabled)
    {
        var rect=new Rect(x,y,w,h);
        bool hot=enabled&&GUI.enabled&&rect.Contains(Event.current.mousePosition);
        GUI.Box(rect,GUIContent.none,!enabled?btnOff:picked?btnPick:hot?btnHot:btnFrame);
        Color tint=!enabled?new Color(.58f,.64f,.68f,.50f):picked?C("f0cd7f"):cream;
        float lw=Width(btnTitle,label),group=30+12+lw,gx=x+(w-group)/2;
        Icon(icon,gx,y+14,30,tint);
        Label(gx+42,y+12,lw+8,34,label,btnTitle,tint);
        Label(x+12,y+49,w-24,26,caption,btnCaption,enabled?C("94a7b0"):new Color(.52f,.58f,.62f,.45f));
        if(!enabled)return false;
        if(!GUI.Button(rect,GUIContent.none,GUIStyle.none))return false;
        Sound();return true;
    }
    void ActionButtons(float w)
    {
        var plot=sim.Plots[selected];bool ended=sim.Ended;
        // **파산은 끝이 아니다.** 씨앗도 방벽도 살 수 없을 뿐, 날은 계속 간다.
        // 그래서 버튼을 숨기지 않고 **눌리지 않는 채로 남겨 둔다** — 할 수 있었던 것이
        // 그 자리에 그대로 보여야 무력감이 생긴다. 지워 버리면 그냥 화면이 단순해질 뿐이다.
        bool broke=sim.Bankrupt;
        // 버튼이 다섯이 됐다 — 심기 · 방벽 · 접기 · 떠나기 · 계절.
        // 접기가 없으면 "버틸까 뜯을까"를 누를 수 없고, 떠나기가 없으면 자급만 남아
        // **설계상 모자란 쪽 하나로만** 게임을 하게 된다(DIRECTION §2-2).
        const float bw=214,bh=84,gap=13,y=776;
        float x=w/2-(bw*5+gap*4)/2;
        // 심기 — 선택한 밭의 상태를 부제가 대신 말한다. 별도 디버그 패널을 두지 않는다.
        var growing=plot.HasCrop?data.Crop(plot.CropId):null;
        string sow=broke?"씨앗을 살 돈이 없습니다."
                  :growing!=null?$"{growing.nameKo}  {Math.Min(100,plot.GrowthPoints/Math.Max(1,growing.growDays))}% 자랐습니다"
                  :!plot.Active?"이 땅은 파도가 가져갔습니다.":"작은 씨앗이, 내일을 만듭니다.";
        if(ActionButton(x,y,bw,bh,icSeed,"심기",sow,action==0,!ended&&!broke&&plot.Active&&!plot.HasCrop)){action=0;seedPicker=!seedPicker;}
        // **값이 칸마다 다르다.** 노출된 변이 많을수록 비싸다 — 그 숫자가 보여야 판단이 된다.
        int wallCost=plot.Active?sim.DefenseCost(plot):0;
        string wall=broke?"방벽을 세울 목재가 없습니다."
                   :plot.DefenseDaysLeft>0?$"방벽이 {plot.DefenseDaysLeft}일 더 버팁니다."
                   :!plot.Active?"지킬 땅이 남아 있지 않습니다."
                   :$"{wallCost} G · 열린 변 {sim.OpenSides(plot)}곳";
        // 자금이 모자랄 때도 누를 수 있게 둔다 — 죽은 버튼은 아무것도 알려 주지 않는다.
        if(ActionButton(x+bw+gap,y,bw,bh,icWall,"방벽 설치",wall,action==1,!ended&&!broke&&plot.Active&&plot.DefenseDaysLeft==0))
        {
            action=1;seedPicker=false;
            if(sim.Money<wallCost){message=$"{wallCost} G 가 필요합니다. 바깥 칸일수록 비쌉니다.";Deny();}
            else
            {
                bool built=sim.TryDefend(plot.Id);
                message=built?"목재 방벽이 침식을 늦춥니다.":"이 땅에는 방벽을 세울 수 없습니다.";
                if(built&&sound!=null)sound.Defend(); else Deny();
                Rebuild();
            }
        }
        // **접기.** 무너지기를 기다리지 않고 지금 내주고 자재를 회수한다.
        // 그냥 두고 무너지면 0 이라, 이 버튼이 있어야 "버틸까 지금 뜯을까"가 선택이 된다.
        float ax=x+(bw+gap)*2;
        var sal=data.Plots.salvage;
        int back=(sal!=null&&plot.Active)?sal.coinPerPlot+sal.coinPerDefenseDay*Math.Max(0,plot.DefenseDaysLeft):0;
        bool canFold=!ended&&plot.Active&&sim.ActiveCount()>1&&sal!=null&&sal.enabled!=0;
        string fold=!plot.Active?"이미 사라진 땅입니다."
                   :sim.ActiveCount()<=1?"마지막 한 칸은 접을 수 없습니다."
                   :$"{back} G 회수 · 되돌릴 수 없습니다";
        if(ActionButton(ax,y,bw,bh,icFold,"접기",fold,action==3,canFold))
        {
            action=3;seedPicker=false;
            if(sim.TryAbandon(plot.Id))
            {
                message=$"{back} G 를 거뒀습니다. 흙의 일부는 옆 밭으로 갑니다.";
                if(sound!=null)sound.Collapse(1);
                // **Rebuild 가 먼저다.** world 를 새로 만들면서 붕괴 오브젝트도 같이 지워진다.
                var where=PlotPosition(plot);
                Rebuild();
                CollapseEffects(new[]{where});
            }
            else Deny();
        }
        // **떠나기.** 자급만으로는 못 버틴다. 그렇다고 돌아다니기만 해도 못 버틴다.
        float jx=x+(bw+gap)*3;
        var far=data.Travel!=null&&data.Travel.regions.Length>0?trip.Region(data.Travel.regions[0].id):null;
        // **부제는 한 줄에 들어가야 한다.** btnCaption 은 wordWrap 이 꺼져 있어 넘치면 잘린다.
        string away=!ended&&far!=null
            ?$"가장 가까운 곳이 왕복 {trip.DaysFor(data.Travel.regions[0].id)}일"
            :"더 갈 곳이 없습니다.";
        if(ActionButton(jx,y,bw,bh,icWave,"떠나기",away,journey,!ended&&trip!=null&&data.Travel!=null)){action=4;OpenJourney();}

        float nx=x+(bw+gap)*4;
        if(ended){ if(ActionButton(nx,y,bw,bh,icNext,"다시 시작","새로운 땅에서 다시 시작합니다.",action==2,true)){action=2;seedPicker=false;ResetGame();} }
        else if(ActionButton(nx,y,bw,bh,icNext,"계절 넘기기","다가오는 계절을 맞이합니다.",action==2,true)){action=2;seedPicker=false;Advance(data.Seasons.daysPerSeason);}

        // **우선 급수.** 물은 하루 총량이 정해져 있어 모든 칸을 적실 수 없다.
        // 어느 칸을 굴릴지는 사람이 정해야 그 부족함이 선택이 된다.
        if(!ended&&plot.Active&&data.Plots.waterPriority!=null&&data.Plots.waterPriority.enabled!=0)
        {
            // **마지막 버튼 위다.** 첫 버튼 위에 두면 토스트(ToastX..ToastX+ToastW)와 겹친다.
            string wlab=plot.WaterFirst?"우선 급수 ON":"우선 급수";
            if(Button(x+(bw+gap)*4,y-46,bw,36,wlab,true)){ plot.WaterFirst=!plot.WaterFirst;
                message=plot.WaterFirst?"이 밭에 먼저 물을 줍니다.":"우선 급수를 껐습니다."; Sound(); }
        }
    }
    void SeedPicker(float w)
    {
        var plot=sim.Plots[selected];var season=data.SeasonOfDay(sim.CurrentDay);
        const float pw=690,ph=176;float px=w/2-pw/2,py=578;
        Card(px,py,pw,ph);
        Label(px+24,py+13,pw-48,28,"이번 계절에 심을 씨앗을 고르세요",small);
        Rule(px+24,py+46,pw-48);
        for(int i=0;i<data.Crops.crops.Length;i++)
        {
            var c=data.Crops.crops[i];
            bool available=data.CropFitsSeason(c,season.id)&&plot.Soil>=Math.Max(c.minSoil,data.Plots.soilFloorForPlanting)&&sim.Money>=data.Shop(c.id).buySeed;
            if(Button(px+18+(i%3)*222,py+58+(i/3)*54,210,46,c.nameKo+"   "+data.Shop(c.id).buySeed+" G",available))
            {
                crop=c.id;bool sown=sim.TryPlant(plot.Id,crop);
                message=sown?c.nameKo+" 씨앗을 심었습니다.":"이 땅에는 심을 수 없습니다.";
                if(sown&&sound!=null)sound.Plant(); else Deny();
                seedPicker=false;Rebuild();
            }
        }
    }
    // 매일 메시지가 뜨는 자리. **끝난 뒤의 기록도 정확히 여기에 뜬다**(DIRECTION §2-7).
    public const float ToastX=30, ToastW=640, ToastBottom=762;

    void Toast(float w)
    {
        // 끝에서 의미를 설명하지 않는다. 남은 것은 사실 하나뿐이다.
        string line=sim.Ended?"남은 땅이 없습니다."
                   :sim.Bankrupt?"이제는 지켜보는 일만 남았습니다."
                   :message;
        if(string.IsNullOrEmpty(line))return;
        // **버튼 옆이 아니라 버튼 위다.**
        //
        // 버튼이 다섯이 되면서(214x5 + 13x4 = 1122) 1600 폭에서 왼쪽 여백이 250 도 안 남았다.
        // 거기에 맞춰 폭을 줄였더니 "21일 경과 · 수확 +576 G ..." 한 줄이 여섯 줄로 쪼개져
        // 카드 밖으로 넘쳤다. 폭을 더 줄이는 것은 해결이 아니다 — **자리를 옮기는 것**이 해결이다.
        //
        // 그리고 이 자리는 끝난 뒤의 기록이 그대로 쓰는 자리다(DIRECTION §2-7
        // "매일 메시지가 뜨던 것과 같은 자리, 같은 크기"). 두 곳의 좌표가 하나여야 한다 →
        // ToastX · ToastW · ToastBottom.
        float th=Mathf.Max(56,toastText.CalcHeight(new GUIContent(line),ToastW-100)+24);
        float y=ToastBottom-th;
        Card(ToastX,y,ToastW,th);
        Icon(icAlert,ToastX+18,y+th/2-13,26,C("e8b16a"));
        Label(ToastX+54,y+7,ToastW-100,th-14,line,toastText);
    }

    // ── 내일의 지도 (다른 PoC. 같은 패널 언어를 쓴다) ──────────────────────
    void MapUI(float w)
    {
        float rx=w-1440;   // 오른쪽에 붙는 것들은 화면 폭을 따라간다
        ShadowText(rx+900,16,520,40,node.terminal?"승 인 된 아 침":"제03구역 · 승인 전",headRight);
        ShadowText(rx+900,58,520,26,"오늘도, 조금은 나아지는 도시를.",capRight);
        Card(rx+1110,116,308,501);Label(rx+1134,133,268,34,node.terminal?"다음 날의 기록":"오늘의 민원",heading);Rule(rx+1134,174,260);
        Color route=node.state.a==1&&node.state.b==1?teal:C("d0ad63");Panel(rx+1134,204,5,61,route);
        Label(rx+1152,198,245,34,"병원 진입로 연결");Label(rx+1152,233,245,40,node.state.a==1&&node.state.b==1?"구급차가 병원까지 진입할 수 있습니다.":"골목 폭과 병원 담장을 수정하세요.",caption);
        Panel(rx+1134,296,5,61,node.state.c==1?teal:C("dfb455"));Label(rx+1152,290,245,32,"지하 세입자 출입구");Label(rx+1152,326,245,40,node.state.c==1?"이전된 출입구를 보존했습니다.":"지도에서 빠진 주민의 주소를 살피세요.",caption);
        if(node.terminal){Label(rx+1134,395,258,62,node.title,heading);Label(rx+1134,464,258,140,node.prompt,small);}
        else
        {
            int row=0;
            foreach(var a in node.actions.Where(a=>a.id!="approve")){if(Button(rx+1132,390+row*46,268,40,a.label))Act(a.id);row++;}
            if(Button(rx+1132,545,268,56,"도면 승인   →"))Act("approve");
        }
        // 카메라 방향과 무관한 축소 도면.
        Card(24,552,283,283);Label(46,568,240,27,"수정 도면",small);Rule(45,602,245);
        for(int i=0;i<10;i++){Panel(47+i*24,619,1,190,new Color(.65f,.75f,.70f,.16f));Panel(47,619+i*20,239,1,new Color(.65f,.75f,.70f,.16f));}
        Panel(147,624,node.state.a==1?24:12,176,C("428f8b"));Panel(64,685,208,15,C("777f70"));
        for(int i=0;i<7;i++){float x=i<4?78:207,y=638+(i%4)*44;Panel(x,y,34,30,C(i%2==0?"ac8160":"aeab8f"));}
        Panel(200,628,59,42,C("c7c8b2"));Panel(node.state.c==1?106:133,758,8,8,node.state.c==1?teal:C("e4bb5c"));Label(264,787,20,23,"N",caption);
        float th=Mathf.Max(56,toastText.CalcHeight(new GUIContent(message),330)+24);
        Card(330,876-th,430,th);Icon(icAlert,348,876-th+th/2-13,26,C("e8b16a"));Label(384,876-th+7,330,th-14,message,toastText);
    }
    void HelpUI(float w)
    {
        float pw=1020,px=w/2-pw/2;
        Card(px,170,pw,576);
        Icon(icSprout,px+34,196,44,Color.white);
        Label(px+88,192,900,50,"플레이 안내",title);
        Rule(px+36,262,pw-72);
        Label(px+36,286,pw-72,366,farm
            ?"밭을 눌러 고르고, 심기로 씨앗을 고릅니다. 수확과 물 배분은 자동입니다.\n\n작물은 계절·토질·잔고 조건을 충족해야 합니다. 클로버는 수익 대신 토질을 회복합니다.\n방벽은 목재 한 묶음(90 G)으로 56일 동안 침식을 줄입니다. 사라진 땅은 복구할 수 없습니다.\n계절 넘기기로 28일을, Space 로 하루를 진행합니다. 물은 기본 24에 날씨 보정을 더합니다.\n매 계절 생활비 700 G, 매년 남은 밭당 세금 12 G를 냅니다.\n현금 -400 G 미만 또는 모든 밭을 잃으면 끝납니다.\n\n마우스 휠로 확대, Q/E 로 섬을 돌립니다. R 로 다시 시작합니다.\n\n붉은 점선 테두리는 28일 안에 사라질 땅입니다. 예보 격자의 붉은 칸과 같은 칸입니다.\n갈색 맨흙과 균열은 이미 깎여 나간 정도이며, 붉은 테두리와는 다른 이야기입니다.\n예보는 현재 계절·연차의 하루 침식을 28일 늘린 추정이라 날씨·방벽에 따라 달라집니다."
            :"골목을 넓히고 병원 담장에 입구를 만드세요.\n\n누락된 민원을 읽으면 지하 세입자의 출입구를 옮길 수 있습니다.\n청록색 선은 수정한 도로, 금색은 확인이 필요한 출입 위치입니다.\n승인 전까지 수정 항목을 다시 누르면 되돌릴 수 있습니다.\n도면 승인으로 다음 날의 도시와 세 가지 결말을 확인합니다.\n\n마우스 휠로 확대, Q/E 로 시점을 돌립니다. R 로 다시 시작합니다.\n좌측 아래 도면은 현재 변경 상태를 표시합니다.",text);
        if(Button(px+pw-290,668,250,54,"플레이로 돌아가기"))help=false;
    }
}
