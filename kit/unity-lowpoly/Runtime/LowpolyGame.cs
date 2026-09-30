using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using FarmErosion.Data;
using FarmErosion.Sim;

[Serializable] public class MapState { public int step,a,b,c,flags; }
[Serializable] public class MapAction { public string id,label,hint,target; }
[Serializable] public class MapNode { public string id,title,prompt,feedback,ending; public MapState state; public MapAction[] actions; public bool terminal; }
[Serializable] public class MapGraph { public MapNode[] nodes; }
public class PlotPick : MonoBehaviour { public int index; }

public sealed partial class LowpolyGame : MonoBehaviour
{
    bool farm; GameData data; Simulation sim; MapGraph graph; MapNode node;
    Camera cam; GameObject world; Font font; Soundscape sound; int selected=14; string crop="wheat", message="";
    string compareState="";   // 시안과 대조하는 캡처가 어떤 국면이었는지 증거에 남긴다
    float yaw=0, pitch=0; bool help; GUIStyle text,title,small,button; bool styles;
    readonly Color ink=new Color(.08f,.16f,.18f), cream=new Color(.95f,.92f,.83f), teal=new Color(.22f,.66f,.60f), coral=new Color(.91f,.43f,.29f);
    static T Read<T>(string name) => JsonUtility.FromJson<T>(Resources.Load<TextAsset>("Data/"+name).text);
    bool headless;             // --poc-smoke. 증거 캡처를 장막이 덮으면 안 된다
    string difficultyId;       // 고른 난이도. null 이면 difficulty.json 의 기본값

    public bool Headless=>headless;

    void Start()
    {
        farm=Resources.Load<TextAsset>("mode").text.Trim()=="farm";
        font=Resources.Load<Font>("Interface");
        SetupStage();
        // 소리. 음악·바람·파도가 겹쳐야 섬이 된다.
        sound=gameObject.AddComponent<Soundscape>(); sound.Build();
        headless=Environment.GetCommandLineArgs().Contains("--poc-smoke");
        StartRun();
        // **난이도가 먼저고 질문이 그 다음이다.** 이 게임을 처음 설명하는 자리가 난이도 화면이라
        // 거기서 "어느 쪽을 골라도 끝은 같습니다"를 읽은 뒤에 첫 질문이 와야 한다(DIRECTION §2-6).
        // 고르고 나면 Choose() 가 BeginIntro() 를 부른다.
        if(farm&&!headless)BeginChoice();
        if(headless) StartCoroutine(Smoke());
    }

    /// <summary>
    /// R 로 다시 시작한다. **난이도는 다시 묻지 않는다** — 메뉴가 뜨면 조용하지 않다.
    /// "다시 도전하세요"가 되지 않게 R 은 열려 있기만 한다(DIRECTION §2-4).
    /// </summary>
    void ResetGame(){ StartRun(); }

    void StartRun()
    {
        seedPicker=false;
        if(farm)
        {
            data=new GameData { Config=Read<ConfigData>("config"), Crops=Read<CropsData>("crops"), Plots=Read<PlotsData>("plots"),
                                Seasons=Read<SeasonsData>("seasons"), Economy=Read<EconomyData>("economy"), Events=Read<EventsData>("events"),
                                Travel=Read<TravelDataFile>("regions"), Difficulty=Read<DifficultyDataFile>("difficulty") };
            data.Index();
            sim=new Simulation(data,PolicyKind.Balanced,20,difficultyId);
            trip=new Travel(data,sim);
            journey=false; journeyRegion=null; journeyErrand=null; journeyReport="";
            endPhase=EndPhase.None; endMemories=null; endOthers=null; endAnswer=""; endAnswered=false;
            selected=14; message="밭을 선택하고 씨앗을 심으세요. 수확·물 배분은 자동입니다.";
        }
        else { graph=Read<MapGraph>("graph"); node=graph.nodes.First(n=>n.id=="0"); message="골목과 병원 입구를 연결하세요. 누락된 민원도 확인해 보세요."; }
        Rebuild();
    }
    GameObject Box(string name,Vector3 p,Vector3 size,Color c)
    {
        var o=GameObject.CreatePrimitive(PrimitiveType.Cube); o.name=name; o.transform.SetParent(world.transform); o.transform.position=p; o.transform.localScale=size; o.GetComponent<Renderer>().sharedMaterial=Mat(c); return o;
    }
    void Model(string resource,Vector3 pos,float width,float rotation=0)
    {
        var prefab=Resources.Load<GameObject>("Models/"+resource); if(prefab==null) throw new Exception("Missing asset "+resource);
        var o=Instantiate(prefab,world.transform); o.transform.rotation=Quaternion.Euler(0,rotation,0);
        var renderers=o.GetComponentsInChildren<Renderer>();
        var palette=Mat(Color.white); palette.mainTexture=Resources.Load<Texture2D>("Palettes/"+resource);
        foreach(var r in renderers) r.sharedMaterials=Enumerable.Repeat(palette,r.sharedMaterials.Length).ToArray();
        var b=renderers[0].bounds; foreach(var r in renderers) b.Encapsulate(r.bounds);
        o.transform.localScale*=width/Mathf.Max(b.size.x,b.size.z);
        b=renderers[0].bounds; foreach(var r in renderers) b.Encapsulate(r.bounds);
        o.transform.position+=pos-new Vector3(b.center.x,b.min.y,b.center.z);
    }
    void Rebuild()
    {
        if(world!=null) { world.SetActive(false); Destroy(world); }
        foreach(var mesh in generatedMeshes)Destroy(mesh);generatedMeshes.Clear();
        foreach(var material in generatedMaterials)Destroy(material);generatedMaterials.Clear();
        world=new GameObject("Playable board");
        artRandom=new System.Random(729); if(farm) FarmScene(); else MapScene(); AddAmbientMotion();
    }
    void Update()
    {
        TickIntro(); TickEnding();
        // **엔딩이 글자를 받는 동안에는 키를 넘기지 않는다.** 'R' 을 치는 순간 게임이
        // 다시 시작돼 방금 쓰던 한 줄이 통째로 날아간다.
        if(!EndingCapturesKeys&&!choosing)
        {
            if(Input.GetKeyDown(KeyCode.Escape)) help=!help;
            if(Input.GetKeyDown(KeyCode.R)) ResetGame();
        }
        // 좌우(Q/E · ←/→)와 상하(W/S · ↑/↓). 마우스 오른쪽 버튼으로 끌어도 돈다.
        float turn=0, tilt=0;
        if(Input.GetKey(KeyCode.Q)||Input.GetKey(KeyCode.LeftArrow)) turn-=1;
        if(Input.GetKey(KeyCode.E)||Input.GetKey(KeyCode.RightArrow)) turn+=1;
        if(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)) tilt+=1;
        if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)) tilt-=1;
        yaw+=turn*Time.deltaTime*35; pitch+=tilt*Time.deltaTime*28;
        if(Input.GetMouseButton(1)){ yaw+=Input.GetAxis("Mouse X")*3.2f; pitch-=Input.GetAxis("Mouse Y")*2.2f; }
        // **위아래는 묶어 둔다.** 너무 내리면 수면과 나란해져 섬이 종잇장이 되고,
        // 너무 올리면 평면도가 되어 절벽과 지층이 통째로 사라진다 — 이 게임이 가진 것이 그 실루엣이다.
        pitch=Mathf.Clamp(pitch,-14f,26f);
        cam.orthographicSize=Mathf.Clamp(cam.orthographicSize-Input.mouseScrollDelta.y*.35f,6.6f,11);   // 하한 6.6 = 섬 둘레 세 여백이 모두 20% 를 넘는 값 (구역 C2 측정)
        // 구역 C: 시선을 올려 섬을 프레임 아래로 내린다 - 직교 5.8 에서 하단 여백이 버튼과 붙는다.
        // **주석을 이 줄들 위에 둔다.** 한 줄에 여러 문장이 있는 줄 끝에 // 를 붙였다가
        // 뒤의 카메라 배치가 통째로 주석이 되어 카메라가 원점(수면 높이)에 남았다.
        Vector3 target=new Vector3(1.70f,.75f,.25f);
        // pitch 는 카메라를 궤도 위에서 올리고 내린다. 기본 (10,14,-17) 이 약 35도 부감이다.
        Vector3 orbit=Quaternion.Euler(0,yaw,0)*(Quaternion.AngleAxis(-pitch,Vector3.right)*new Vector3(10,14,-17));
        cam.transform.position=orbit+target;
        cam.transform.LookAt(target);
        if(farm && !help && !choosing && !EndingCapturesKeys && Input.GetKeyDown(KeyCode.Space)) Advance(1);
        if(farm && !help && !choosing && !seedPicker && Input.GetMouseButtonDown(0) && Input.mousePosition.x<Screen.width*.76f && Input.mousePosition.y>Screen.height*.22f && Input.mousePosition.y<Screen.height*.87f)
        {
            var hits=Physics.RaycastAll(cam.ScreenPointToRay(Input.mousePosition));
            foreach(var h in hits.OrderBy(h=>h.distance)) { var p=h.collider.GetComponent<PlotPick>(); if(p!=null) { selected=p.index; seedPicker=false; Sound(); Rebuild(); break; } }
        }
    }
    void Act(string id)
    {
        var a=node.actions.FirstOrDefault(x=>x.id==id); if(a==null)return;
        Sound(id=="approve"); node=graph.nodes.First(n=>n.id==a.target); message=node.terminal?node.title:a.hint; Rebuild();
    }
    void Advance(int days)
    {
        var held=sim.Plots.Where(p=>p.Active).ToArray();
        int before=sim.ActiveCount(),money=sim.Money,income=sim.Income,old=sim.CurrentDay;
        for(int i=0;i<days;i++) if(!sim.AdvanceDay())break;
        // **그들의 시계도 같이 간다.** 이 한 줄이 없으면 내가 찾아갈 때만 그들의 땅이 줄어
        // 그들이 내 시계로 사는 것이 된다(DIRECTION §2-1).
        if(trip!=null)trip.Sync();
        message=$"{sim.CurrentDay-old}일 경과 · 수확 +{sim.Income-income} G · 현금 변화 {sim.Money-money:+0;-0;0} G";
        if(sound!=null)sound.Advance();
        // 계절이 바뀐 날에만 전환음. 구역 F(Soundscape)가 Season()을 주고 배선은 여기다 —
        // 소리 파일은 그쪽 소유지만 "언제 울릴지"는 게임 루프가 안다.
        if(sound!=null&&data.SeasonOfDay(old).id!=data.SeasonOfDay(sim.CurrentDay).id)
        {
            sound.Season();
            // **계절에 맞춰 곡을 고르지 않는다.** 계절 연출이 아직 없어서 음악만 바뀌면
            // 화면과 어긋난 것으로 들린다. 지금은 Soundscape 가 네 곡을 이어서 돌린다.
            // 계절 연출이 들어오면 여기서 sound.SeasonTrack(계절 index) 를 부르면 된다.
        }
        if(before>sim.ActiveCount())
        {
            message+=$"\n파도가 밭 {before-sim.ActiveCount()}칸을 가져갔습니다.";
            // 이 게임의 유일한 상실이다. 소리가 가장 크고 길어야 한다.
            if(sound!=null)sound.Collapse(before-sim.ActiveCount());
        }
        Rebuild(); CollapseEffects(held.Where(p=>!p.Active).Select(PlotPosition));
    }
    /// <summary>지금 낼 수 있는 의뢰 하나. 없으면 null — 그냥 들르는 것도 선택이다.</summary>
    string Payable(string regionId)
    {
        foreach(var e in trip.ErrandsAt(regionId)) if(trip.CanPay(e)) return e.id;
        return null;
    }
    IEnumerator Smoke()
    {
        yield return new WaitForSeconds(2);
        string dir=Path.Combine(Application.dataPath,"..","Evidence"); Directory.CreateDirectory(dir);
        if(farm)
        {
            // 난이도 화면은 이 게임을 처음 설명하는 자리라 증거에 남긴다(DIRECTION §2-6).
            BeginChoice(); yield return null; yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"choice.png")); yield return new WaitForSeconds(1);
            Choose("normal"); introTime=-1f;          // 연출은 캡처를 덮으므로 여기서만 끈다
            yield return new WaitForSeconds(1);
        }
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,"start.png")); yield return new WaitForSeconds(1);
        if(farm)
        {
            if(!sim.TryPlant(sim.Plots[14].Id,"wheat")||!sim.TryDefend(sim.Plots[14].Id))throw new Exception("Farm actions failed");
            foreach(int i in new[]{7,8,9,13,15,19,20})sim.TryPlant(sim.Plots[i].Id,i%2==0?"clover":"wheat");
            Advance(7); yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Path.Combine(dir,"growing.png")); yield return new WaitForSeconds(1);
            Advance(21); if(sim.Income<=0)throw new Exception("No harvest");
            // **시안과 같은 상태에서 한 장 찍는다.**
            //
            // 승인 시안은 16/24 · 2년차 가을인데 여기서 찍던 growing.png 는 24/24 · 1년차 봄이었다.
            // 서로 다른 국면을 나란히 놓고 "안 닮았다"고 판단하고 있었다는 뜻이다.
            // 칸을 화면에서만 숨겨 모양을 맞추지 않는다 — 시뮬레이션을 실제로 그 지점까지 굴린다.
            while(!sim.Ended&&sim.ActiveCount()>16)
            {
                foreach(var plot in sim.Plots.Where(p=>p.Active&&!p.HasCrop))
                {var pick=sim.Choose(plot,data.SeasonOfDay(sim.CurrentDay),data.YearOfDay(sim.CurrentDay),sim.CurrentWeather);if(pick!=null)sim.TryPlant(plot.Id,pick.id);}
                sim.AdvanceDay();
            }
            Rebuild(); yield return new WaitForSeconds(1);
            compareState=sim.ActiveCount()+"/24 · "+data.YearOfDay(sim.CurrentDay)+"년차 "+data.SeasonOfDay(sim.CurrentDay).nameKo+" · "+sim.CurrentDay+"일";
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"compare.png")); yield return new WaitForSeconds(1);
            var held=sim.Plots.Where(p=>p.Active).ToArray();int count=sim.ActiveCount();
            while(!sim.Ended&&sim.ActiveCount()==count)
            {
                foreach(var plot in sim.Plots.Where(p=>p.Active&&!p.HasCrop))
                {var pick=sim.Choose(plot,data.SeasonOfDay(sim.CurrentDay),data.YearOfDay(sim.CurrentDay),sim.CurrentWeather);if(pick!=null)sim.TryPlant(plot.Id,pick.id);}
                sim.AdvanceDay();
            }
            Rebuild();CollapseEffects(held.Where(p=>!p.Active).Select(PlotPosition));
            yield return null; yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"collapse-0.png"));
            for(int frame=0;frame<4;frame++){yield return new WaitForSeconds(.35f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"erosion-"+frame+".png"));}
            yield return new WaitForSeconds(1);

            // **떠나기.** 다녀오는 동안 날이 실제로 가고 내 땅이 깎인다.
            // 엔딩의 기록은 여기서 준 것으로만 생기므로, 이것을 빠뜨리면 끝이 빈다.
            journey=true; journeyRegion="ford"; journeyErrand=null;
            yield return null; yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"journey.png")); yield return new WaitForSeconds(1);
            int beforeTrip=sim.CurrentDay;
            // **낼 수 있는 의뢰를 고른다.** 여기까지 오면 방벽이 남아 있지 않을 수 있는데,
            // 못 내는 의뢰를 굳이 고르면 Visit 이 실패하고 코루틴이 거기서 죽는다. 한 번 겪었다.
            Depart("ford",Payable("ford"));
            if(trip.VisitCount!=1)throw new Exception("Visit did not happen");
            if(sim.CurrentDay<=beforeTrip)throw new Exception("Travel cost no days");
            yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Path.Combine(dir,"visited.png"));
            yield return new WaitForSeconds(1);
            // 기록이 한 사람으로만 차지 않게 한 사람 더 만난다.
            foreach(var other in new[]{"ridge","ashen","farbank"})
            {
                if(sim.Ended)break;
                if(Payable(other)==null)continue;
                Depart(other,Payable(other)); yield return new WaitForSeconds(1); break;
            }
            if(trip.Log.Count(v=>v.Ok&&v.ErrandId!=null)==0)throw new Exception("No errand was ever paid");

            while(!sim.Ended)sim.AdvanceDay(); Rebuild();
            if(sim.CurrentDay==0)throw new Exception("No progression");

            // **끝난 뒤.** 네 박자를 실제로 지나가며 찍는다(DIRECTION §2-7).
            // 1번 박자는 아무 일도 일어나지 않는 시간이라 화면에 아무것도 없다 — 그것이 증거다.
            yield return new WaitForSeconds(2); ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ending-1-nothing.png"));
            while(endPhase!=EndPhase.Records&&endPhase!=EndPhase.Ask) yield return null;
            // 질문이 붙은 기록에서 찍는다 — 사실과 질문이 한 화면에 같이 있어야 §2-7 이 확인된다.
            bool shot=false;
            while(endPhase==EndPhase.Records)
            {
                if(!shot&&endMemories[endIndex].QuestionKo!=null&&endTime>EndFade+EndRead+EndAskIn*.6f)
                { ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ending-2-record.png")); shot=true; }
                yield return null;
            }
            if(!shot)ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ending-2-record.png"));
            yield return new WaitForSeconds(1.4f); ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ending-3-ask.png"));
            yield return new WaitForSeconds(1);
            // 이 줄은 **이 실행이 실제로 남긴 것**이다. 다음 실행의 엔딩에 그대로 올라간다.
            endAnswer=Environment.GetCommandLineArgs().Contains("--poc-answer")?"끝까지 클로버만 심었다.":"";
            SubmitAnswer();
            while(endPhase!=EndPhase.Others&&endPhase!=EndPhase.Done) yield return null;
            yield return new WaitForSeconds(EndOtherStep*2+1.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ending-4-others.png"));
            yield return new WaitForSeconds(1);
            compareState+="  ·  엔딩 기록 "+endMemories.Count+"줄 · 다른 사람의 말 "+endOthers.Count+"줄";
        }
        else
        {
            Act("approve"); if(!node.terminal||node.state.a!=0)throw new Exception("Blocked ending failed");
            ResetGame(); Act("road");Act("gate");Act("approve"); if(!node.terminal||node.state.c!=0)throw new Exception("Unsafe ending failed");
            ResetGame(); Act("road");Act("gate");Act("petition");Act("door"); yield return new WaitForSeconds(2); ScreenCapture.CaptureScreenshot(Path.Combine(dir,"editing.png")); yield return new WaitForSeconds(1); Act("approve"); if(!node.terminal||node.state.c!=1)throw new Exception("Safe ending failed");
        }
        yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Path.Combine(dir,"outcome.png")); yield return new WaitForSeconds(1);
        File.WriteAllText(Path.Combine(dir,"smoke.txt"),"PASS: "+(farm?"plant, defense, growth, harvest, costs, end state\ncompare.png = "+compareState+"  (승인 시안은 16/24 · 2년차 가을)":"blocked, unsafe, safe endings"));
        Application.Quit(0);
    }
}
