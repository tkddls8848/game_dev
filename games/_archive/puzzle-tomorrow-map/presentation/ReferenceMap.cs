using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable] public class BakedCityMesh { public float[] positions,normals,uv;public int[] triangles; }
public sealed class MapZone:MonoBehaviour {public string action;}

public sealed class ReferenceMap:MonoBehaviour
{
    Camera view,plan;RenderTexture planImage;MapGraph graph;MapNode node;
    GameObject edits,vehicle;Font headingFont,bodyFont;AudioSource sound;
    GUIStyle title,copy,small,cardTitle,button;Texture2D rounded,circle;bool styled,petition,help;string hint="";float hintUntil,approvalAt=-10,orbit,zoom;
    Material cyan,glass,amber;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
    readonly List<Mesh> ownedMeshes=new List<Mesh>();
    const int CityLayer=9,OverlayLayer=10;
    static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
    void Start()
    {
        graph=JsonUtility.FromJson<MapGraph>(Resources.Load<TextAsset>("Data/graph").text);node=graph.nodes.First(n=>n.id=="0");
        headingFont=Resources.Load<Font>("ReferenceCity/Heading");bodyFont=Resources.Load<Font>("ReferenceCity/Body");
        sound=gameObject.AddComponent<AudioSource>();sound.volume=.42f;
        foreach(string name in new[]{"paper","edit","approve"})clips[name]=Resources.Load<AudioClip>("ReferenceCity/"+name);
        BuildMesh("desk",0);BuildMesh("city",CityLayer);vehicle=BuildMesh("vehicle",CityLayer);
        view=new GameObject("Reference camera").AddComponent<Camera>();view.tag="MainCamera";view.gameObject.AddComponent<AudioListener>();view.orthographic=true;view.orthographicSize=19.3f/(1672f/941f)/2;view.nearClipPlane=.1f;view.farClipPlane=100;view.allowHDR=true;view.depthTextureMode=DepthTextureMode.Depth;
        view.gameObject.AddComponent<ReferenceOptics>();SetCamera();
        planImage=new RenderTexture(512,512,16,RenderTextureFormat.ARGB32);planImage.Create();plan=new GameObject("Live blueprint camera").AddComponent<Camera>();plan.orthographic=true;plan.orthographicSize=5.65f;plan.transform.position=new Vector3(0,25,0);plan.transform.rotation=Quaternion.Euler(90,0,0);plan.cullingMask=(1<<CityLayer)|(1<<OverlayLayer);plan.clearFlags=CameraClearFlags.SolidColor;plan.backgroundColor=Hex("243237");plan.targetTexture=planImage;
        cyan=Overlay(Hex("7adddf"),.82f);glass=Overlay(Hex("8dcfdb"),.12f);amber=Overlay(Hex("ebbf62"),.8f);
        MakeZone("road",new Vector3(.4f,.58f,-.35f),new Vector3(1.5f,.3f,3.3f));MakeZone("gate",new Vector3(.4f,.6f,1.1f),new Vector3(1.5f,.5f,.30f));MakeZone("petition",new Vector3(-1.35f,.6f,-2.7f),new Vector3(1.35f,.5f,1.05f));
        Refresh();Application.targetFrameRate=60;QualitySettings.antiAliasing=4;
        if(Environment.GetCommandLineArgs().Contains("--reference-smoke"))StartCoroutine(Smoke());
    }
    GameObject BuildMesh(string name,int layer)
    {
        var asset=Resources.Load<TextAsset>("ReferenceCity/"+name);
        using var compressed=new MemoryStream(asset.bytes);using var zip=new GZipStream(compressed,CompressionMode.Decompress);using var reader=new BinaryReader(zip);
        int count=reader.ReadInt32();var vertices=new Vector3[count];var normals=new Vector3[count];var uvs=new Vector2[count];
        for(int i=0;i<count;i++)vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
        for(int i=0;i<count;i++)normals[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
        for(int i=0;i<count;i++)uvs[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());
        int indexCount=reader.ReadInt32();var triangles=new int[indexCount];for(int i=0;i<indexCount;i++)triangles[i]=reader.ReadInt32();
        var mesh=new Mesh{name="Baked "+name,indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uvs;mesh.triangles=triangles;mesh.RecalculateBounds();ownedMeshes.Add(mesh);
        var obj=new GameObject("Editable source / "+name);obj.layer=layer;obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();var material=new Material(Shader.Find("Tomorrow/Baked"));material.mainTexture=Resources.Load<Texture2D>("ReferenceCity/"+name);renderer.sharedMaterial=material;return obj;
    }
    void MakeZone(string action,Vector3 position,Vector3 size)
    {var go=new GameObject("Interaction / "+action);go.transform.position=position;go.AddComponent<BoxCollider>().size=size;go.AddComponent<MapZone>().action=action;}
    Material Overlay(Color color,float alpha)
    {var m=new Material(Shader.Find("Tomorrow/Overlay"));color.a=alpha;m.color=color;return m;}
    void Path(string name,Vector3[] points,Material material,float width=.018f,bool loop=false)
    {var obj=new GameObject(name);obj.transform.SetParent(edits.transform);obj.layer=OverlayLayer;var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=points.Length;line.SetPositions(points);line.loop=loop;line.widthMultiplier=width;line.sharedMaterial=material;line.numCornerVertices=2;}
    void Refresh()
    {
        if(edits!=null)Destroy(edits);edits=new GameObject("Live draft revisions");
        bool showRoad=node.state.a==1;
        if(!node.terminal)
        {
            Vector3[] polygon={new Vector3(-.83f,.432f,-.23f),new Vector3(-.20f,.432f,-.23f),new Vector3(-.20f,.432f,-1.60f),new Vector3(1,.432f,-1.60f),new Vector3(1,.432f,1.47f),new Vector3(-.2f,.432f,1.47f),new Vector3(-.2f,.432f,.32f),new Vector3(-.83f,.432f,.32f)};
            Path("Road planning boundary",polygon,showRoad?cyan:amber,showRoad?.032f:.016f,true);
            if(showRoad)
            {
                var tint=Overlay(Hex("54bdc1"),.19f);
                foreach(var shape in new[]{new Vector4(.4f,-.065f,1.20f,3.07f),new Vector4(-.515f,.045f,.63f,.55f)})
                {var fill=GameObject.CreatePrimitive(PrimitiveType.Cube);fill.name="Road revision tint";fill.layer=OverlayLayer;fill.transform.SetParent(edits.transform);fill.transform.position=new Vector3(shape.x,.430f,shape.y);fill.transform.localScale=new Vector3(shape.z,.001f,shape.w);fill.GetComponent<Renderer>().sharedMaterial=tint;Destroy(fill.GetComponent<Collider>());}
            }
            if(node.state.c==1)
            {
                float x=2.20f,z=.27f,w=1.05f,d=1.05f,h=1.3f;
                Vector3[] bottom={new Vector3(x-w/2,.43f,z-d/2),new Vector3(x+w/2,.43f,z-d/2),new Vector3(x+w/2,.43f,z+d/2),new Vector3(x-w/2,.43f,z+d/2)};
                Path("Relocation foundation",bottom,cyan,.013f,true);Path("Relocation roof",bottom.Select(v=>v+Vector3.up*h).ToArray(),cyan,.013f,true);
                Path("Relocation floor",bottom.Select(v=>v+Vector3.up*h*.55f).ToArray(),cyan,.008f,true);
                for(int i=0;i<4;i++)Path("Relocation column",new[]{bottom[i],bottom[i]+Vector3.up*h},cyan,.012f);
                var ghost=GameObject.CreatePrimitive(PrimitiveType.Cube);ghost.name="Proposed entrance building";ghost.layer=OverlayLayer;ghost.transform.SetParent(edits.transform);ghost.transform.position=new Vector3(x,.43f+h/2,z);ghost.transform.localScale=new Vector3(w,h,d);ghost.GetComponent<Renderer>().sharedMaterial=glass;Destroy(ghost.GetComponent<Collider>());
                for(int i=0;i<5;i++)Path("Relocation arrow dash",new[]{new Vector3(1.06f+i*.17f,.46f,.35f),new Vector3(1.16f+i*.17f,.46f,.35f)},cyan,.018f);
            }
        }
        if(node.state.b==0)
        {
            var mat=Overlay(Hex("576258"),1);
            for(int i=0;i<9;i++)Path("Hospital closed gate",new[]{new Vector3(-.2f+i*.15f,.43f,1.15f),new Vector3(-.2f+i*.15f,.85f,1.15f)},mat,.025f);
            Path("Hospital gate rail",new[]{new Vector3(-.23f,.83f,1.15f),new Vector3(1.05f,.83f,1.15f)},mat,.027f);
        }
        if(node.terminal&&node.state.c==1)
        {
            for(int i=0;i<4;i++){var stair=GameObject.CreatePrimitive(PrimitiveType.Cube);stair.layer=CityLayer;stair.transform.SetParent(edits.transform);stair.transform.position=new Vector3(1.9f,.43f+i*.07f,.10f+i*.14f);stair.transform.localScale=new Vector3(.65f,.12f,.15f);stair.GetComponent<Renderer>().sharedMaterial=Overlay(Hex("a6a08b"),1);}
        }
    }
    void SetCamera()
    {
        Vector3 target=new Vector3(1.35f,1,.10f);view.transform.position=target+Quaternion.Euler(0,orbit,0)*(new Vector3(10.2f,13.2f,-18.5f)-target);view.transform.LookAt(target);view.orthographicSize=19.3f/(1672f/941f)/2+zoom;
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)){if(petition)petition=false;else help=!help;}
        if(Input.GetKeyDown(KeyCode.R))ResetDraft();
        if(Input.GetKeyDown(KeyCode.F12)) {string folder=Path.Combine(Application.dataPath,"..","EvidenceReference");Directory.CreateDirectory(folder);ScreenCapture.CaptureScreenshot(Path.Combine(folder,"manual-input.png"));}
        if(!help&&!petition)
        {
            if(Input.GetKeyDown(KeyCode.Alpha1))Act("road");if(Input.GetKeyDown(KeyCode.Alpha2))Act("gate");if(Input.GetKeyDown(KeyCode.Alpha3))ReadPetition();if(Input.GetKeyDown(KeyCode.Alpha4))Act("door");if(Input.GetKeyDown(KeyCode.Return))Act("approve");
            orbit=Mathf.Clamp(orbit+(Input.GetKey(KeyCode.Q)?-1:Input.GetKey(KeyCode.E)?1:0)*Time.deltaTime*10,-12,12);zoom=Mathf.Clamp(zoom-Input.mouseScrollDelta.y*.12f,-.5f,.75f);SetCamera();
            Vector2 p=new Vector2(Input.mousePosition.x/Screen.width*1672,(1-Input.mousePosition.y/Screen.height)*941);
            bool outsideUI=!new Rect(1316,244,332,378).Contains(p)&&!new Rect(24,568,340,307).Contains(p);
            if(outsideUI&&Physics.Raycast(view.ScreenPointToRay(Input.mousePosition),out var hit,100)&&hit.collider.TryGetComponent<MapZone>(out var zone))
            {
                if(Input.GetMouseButtonDown(0)){if(zone.action=="petition")ReadPetition();else Act(zone.action);}
            }
        }
        if(node.terminal&&node.state.a==1&&node.state.b==1){float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-approvalAt)/2.4f));vehicle.transform.position=Vector3.forward*Mathf.Lerp(-2.2f,0,t);}else vehicle.transform.position=Vector3.zero;
    }
    void Play(string name){if(clips.TryGetValue(name,out var clip)&&clip!=null)sound.PlayOneShot(clip);}
    void Act(string action)
    {
        if(node.terminal)return;var choice=node.actions.FirstOrDefault(a=>a.id==action);if(choice==null)return;
        node=graph.nodes.First(n=>n.id==choice.target);hint=choice.hint;hintUntil=Time.time+3.5f;Play(action=="approve"?"approve":"edit");
        Debug.Log("MAP_ACTION "+action+" -> "+node.id);
        if(action=="approve")approvalAt=Time.time;Refresh();
    }
    void ReadPetition(){if(node.terminal)return;if(node.state.flags==0)Act("petition");petition=true;Play("paper");}
    void ResetDraft(){node=graph.nodes.First(n=>n.id=="0");petition=false;help=false;hint="";orbit=zoom=0;Play("paper");Refresh();}
    void InitUI()
    {
        copy=new GUIStyle(GUI.skin.label){font=bodyFont,fontSize=19,wordWrap=true};copy.normal.textColor=Hex("e6e8e0");
        title=new GUIStyle(copy){font=headingFont,fontSize=43};cardTitle=new GUIStyle(copy){font=headingFont,fontSize=25};small=new GUIStyle(copy){fontSize=14};small.normal.textColor=Hex("bac1bb");
        button=new GUIStyle(GUI.skin.button){font=bodyFont,fontSize=19,alignment=TextAnchor.MiddleCenter,border=new RectOffset(7,7,7,7)};
        rounded=Round(new Color(.055f,.075f,.085f,.90f),new Color(.6f,.65f,.65f,.50f));button.normal.background=Round(new Color(.1f,.13f,.15f,.48f),new Color(.5f,.56f,.57f,.45f));button.hover.background=Round(new Color(.1f,.32f,.34f,.9f),Hex("70c3c6"));button.active.background=button.hover.background;button.normal.textColor=Hex("e5e8e2");
        circle=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));circle.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(30-d)*Mathf.Clamp01(d-23)));}circle.Apply();styled=true;
    }
    Texture2D Round(Color fill,Color border)
    {
        var tex=new Texture2D(32,32,TextureFormat.RGBA32,false);tex.wrapMode=TextureWrapMode.Clamp;
        for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(7-x,x-24),dy=Mathf.Max(7-y,y-24);float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;float alpha=Mathf.Clamp01(7-d);bool edge=d>5.4f||x<1||x>30||y<1||y>30;Color c=edge?border:fill;c.a*=alpha;tex.SetPixel(x,y,c);}tex.Apply();return tex;
    }
    void Card(Rect rect){var style=new GUIStyle{border=new RectOffset(7,7,7,7)};style.normal.background=rounded;GUI.Box(rect,GUIContent.none,style);}
    void Text(float x,float y,float w,float h,string value,GUIStyle style=null){GUI.Label(new Rect(x,y,w,h),value,style??copy);}
    void Line(float x,float y,float w){GUI.color=new Color(.73f,.77f,.73f,.65f);GUI.DrawTexture(new Rect(x,y,w,1),Texture2D.whiteTexture);GUI.color=Color.white;}
    bool Button(Rect r,string value){return GUI.Button(r,value,button);}
    void Ring(float x,float y,Color col){GUI.color=col;GUI.DrawTexture(new Rect(x,y,24,24),circle);GUI.color=Color.white;}
    void OnGUI()
    {
        if(view==null)return;if(!styled)InitUI();GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1672f,Screen.height/941f,1));
        Text(55,34,540,61,"내 일 의 지 도",title);Line(54,96,220);Text(55,112,350,64,"작은 배치가,\n조금 더 나은 내일을 만든다.",small);
        Text(1446,48,206,42,node.terminal?"다음 날 · 승인 후":"3일차 · 승인 전",cardTitle);Line(1444,95,182);Text(1444,109,210,28,"오늘도, 조금은 나아지는 도시를.",small);
        Card(new Rect(1316,244,332,378));Text(1342,273,277,40,node.terminal?"다음 날의 도시":"오늘의 민원",cardTitle);Line(1341,314,286);
        if(!node.terminal)
        {
            var hospital=new Rect(1339,330,289,77);GUI.color=new Color(.23f,.31f,.33f,.45f);GUI.DrawTexture(hospital,Texture2D.whiteTexture);GUI.color=Color.white;
            Ring(1348,340,node.state.a==1&&node.state.b==1?Hex("28b4bc"):Hex("889996"));Text(1390,339,233,29,"병원 진입로 연결");Text(1390,373,226,28,"주민들이 안전하게 이동할 수 있도록.",small);
            if(GUI.Button(new Rect(1390,397,112,19),"골목 "+(node.state.a==1?"확장됨":"수정 [1]"),small))Act("road");
            if(GUI.Button(new Rect(1510,397,110,19),"담장 "+(node.state.b==1?"열림":"수정 [2]"),small))Act("gate");
            var tenant=new Rect(1339,423,289,70);GUI.color=new Color(.23f,.31f,.33f,.45f);GUI.DrawTexture(tenant,Texture2D.whiteTexture);GUI.color=Color.white;
            Ring(1348,432,node.state.c==1?Hex("28b4bc"):Hex("edb846"));Text(1390,430,230,28,"지하 세입자 출입구");Text(1390,461,232,28,"잊혀진 공간도, 도시의 일부입니다.",small);
            if(GUI.Button(new Rect(1340,423,289,70),GUIContent.none,GUIStyle.none))ReadPetition();
            Text(1342,501,280,25,node.state.flags==1?"민원 확인됨 · 이전 [4]":"민원 카드를 눌러 내용을 확인하세요.",small);
            if(Button(new Rect(1339,545,127,47),"변경 취소"))ResetDraft();
            GUI.backgroundColor=Hex("32c0c4");if(Button(new Rect(1484,542,145,51),"도면 승인"))Act("approve");GUI.backgroundColor=Color.white;
        }
        else{Text(1340,339,286,70,node.title,cardTitle);Text(1340,424,286,105,node.prompt,copy);if(Button(new Rect(1340,550,287,45),"새 도면으로 다시 시작"))ResetDraft();}
        Card(new Rect(24,568,340,307));Text(46,583,230,30,"수정 도면",copy);Line(45,614,298);
        GUI.DrawTexture(new Rect(47,637,286,220),planImage,ScaleMode.ScaleToFit);Text(328,807,20,25,"N",small);
        if(Time.time<hintUntil&&!petition&&!node.terminal){Card(new Rect(480,868,730,37));Text(496,875,700,27,hint,small);}
        if(petition||help)
        {
            Card(new Rect(465,252,690,364));Text(500,278,614,51,help?"도면을 고치는 법":"누락된 민원 · 지하 03호",cardTitle);Line(500,338,620);
            Text(500,358,620,162,help?"골목 [1]과 병원 담장 [2]을 수정합니다.\n세입자의 민원 [3]을 읽고 출입구 [4]를 옮깁니다.\nEnter: 승인  /  R: 초기화  /  Esc: 닫기\n도로·병원 입구·지하 계단을 직접 눌러도 됩니다.":"이 골목 끝의 지하에는 사람이 살고 있습니다.\n도로를 넓히면 오래된 출입구가 막힙니다.\n병원 길을 연결할 때, 이 집의 출입구도 함께 옮겨 주세요.",copy);
            if(!help&&Button(new Rect(760,540,360,47),node.state.c==1?"출입구 이전 취소":"출입구 이전 도면에 반영")){Act("door");petition=false;}
            if(Button(new Rect(500,540,225,47),"닫기")){petition=false;help=false;}
        }
    }
    IEnumerator Smoke()
    {
        string dir=Path.Combine(Application.dataPath,"..","EvidenceReference");Directory.CreateDirectory(dir);
        yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"initial.png"));yield return new WaitForSeconds(.7f);
        Act("approve");if(!node.terminal||node.state.a!=0)throw new Exception("Blocked outcome failed");ResetDraft();Act("road");Act("gate");Act("approve");if(!node.terminal||node.state.c!=0)throw new Exception("Unsafe outcome failed");
        ResetDraft();Act("road");Act("gate");Act("petition");Act("door");hintUntil=0;
        yield return new WaitForSeconds(2);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"reference-view.png"));yield return new WaitForSeconds(1);Act("approve");
        for(int i=0;i<4;i++){yield return new WaitForSeconds(.7f);ScreenCapture.CaptureScreenshot(Path.Combine(dir,"approval-"+i+".png"));}
        if(!node.terminal||node.state.c!=1)throw new Exception("Safe outcome failed");yield return new WaitForSeconds(1);
        File.WriteAllText(Path.Combine(dir,"result.txt"),"PASS: 3 canonical outcomes, baked meshes, live blueprint, approval motion. Physical input still requires separate validation.");Application.Quit();
    }
    void OnDestroy(){foreach(var m in ownedMeshes)Destroy(m);if(planImage!=null)planImage.Release();}
}
public sealed class ReferenceOptics:MonoBehaviour
{
    Material material;
    void OnRenderImage(RenderTexture src,RenderTexture dst){if(material==null)material=new Material(Shader.Find("Tomorrow/Optics"));Graphics.Blit(src,dst,material);}
    void OnDestroy(){if(material!=null)Destroy(material);}
}
