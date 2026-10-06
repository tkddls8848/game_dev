using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class LowpolyGame
{
    readonly List<Mesh> generatedMeshes=new List<Mesh>();
    // 안개 판마다 머티리얼을 따로 만든다(짙기·속도가 다르다). 씬을 다시 지을 때 함께 지운다.
    readonly List<Material> generatedMaterials=new List<Material>();
    AudioSource feedbackSource;AudioClip clickSound,advanceSound,denySound;
    void Tones()
    {
        if(feedbackSource!=null)return;
        feedbackSource=gameObject.AddComponent<AudioSource>();feedbackSource.volume=.14f;
        // 5도 위 배음을 얹으면 딸깍 소리가 나무 두드리는 소리에 가까워진다.
        clickSound=Tone(640,.13f,1.5f);advanceSound=Tone(330,.32f,2f);
        // 거절음만 배음 없이 낮게 — 성공음과 절대 헷갈리면 안 된다.
        denySound=Tone(168,.20f,0f,14f);
    }
    void Sound(bool advance=false){Tones();feedbackSource.PlayOneShot(advance?advanceSound:clickSound);}
    /// <summary>자금 부족·심을 수 없는 밭처럼 **동작이 거절됐을 때.** 무음으로 두면 버튼이 고장 난 것처럼 보인다.</summary>
    void Deny(){Tones();feedbackSource.PlayOneShot(denySound);}
    AudioClip Tone(float freq,float duration,float harmonic=0,float decay=22f)
    {
        const int rate=22050;var values=new float[(int)(rate*duration)];
        for(int i=0;i<values.Length;i++)
        {
            float t=(float)i/rate;
            float wave=Mathf.Sin(t*freq*Mathf.PI*2)+(harmonic>0?Mathf.Sin(t*freq*harmonic*Mathf.PI*2)*.34f:0);
            values[i]=wave/(harmonic>0?1.34f:1)*Mathf.Exp(-t*decay)*Mathf.Min(1,t*300);
        }
        var clip=AudioClip.Create("Soft interaction chime",values.Length,1,rate,false);clip.SetData(values,0);return clip;
    }
    void AddAmbientMotion()
    {
        foreach(Transform child in world.transform)
        {
            if(child.name=="Shore foam")child.gameObject.AddComponent<ShoreMotion>();
            if(child.name=="Survey line"||child.name=="Plot outline")child.gameObject.AddComponent<OutlinePulse>();
        }
        if(farm)
        {
            for(int i=0;i<5;i++)
            {
                var puff=Rock(new Vector3(-.45f,3.56f+i*.25f,3.8f+i*.06f),new Vector3(.10f+i*.032f,.15f+i*.045f,.12f+i*.026f),C("a9b1a4"),"Chimney smoke");
                puff.AddComponent<SmokeDrift>().phase=i*.7f;
            }
        }
    }
    /// <summary>
    /// 밭 한 칸이 무너진다. **Blender 에서 강체로 풀어 구운 애니메이션을 재생한다.**
    ///
    /// 예전에는 바위 15개를 위로 튕겨 놓고 중력을 적분했다(아래 LegacyCollapse).
    /// 그래서 "돌이 튀어올랐다 떨어진다"로 보이지 "땅이 갈라지며 주저앉는다"로 보이지 않았다.
    /// 조각끼리 부딪치며 쌓이는 것과 덩어리가 기울며 미끄러지는 것은 코드로 정점을 찍어서는 안 된다.
    ///
    /// 굽는 스크립트는 games/farm-erosion/tools/blender_collapse.py 다.
    /// **런타임에 물리는 없다** — 구운 것을 틀 뿐이라 결과가 항상 같고(설계 원칙 5) 성능이 예측된다.
    ///
    /// DIRECTION.md §0-1: **가장 참혹한 순간이 가장 예뻐야 한다.**
    /// 부서지는 것이 아니라 내려앉는 것이고, 보기 좋아야 한다.
    /// </summary>
    void CollapseEffects(IEnumerable<Vector3> positions)
    {
        var prefab=Resources.Load<GameObject>("Models/PlotCollapse");
        foreach(var pos in positions)
        {
            if(prefab==null){ Debug.Log("COLLAPSE prefab=null"); LegacyCollapse(pos); continue; }
            var o=Instantiate(prefab,world.transform);
            // 구운 조각들은 상면을 0 으로 잡고 아래로 내려간다. 절벽 상면(.73)에 얹는다.
            o.transform.position=new Vector3(pos.x,.73f,pos.z);
            o.transform.rotation=Quaternion.Euler(0,R(0,360),0);   // 같은 모양이 반복돼 보이지 않게
            Repaint(o);
            // FBX 의 애니메이션은 쓰지 않는다 — 재생하면 조각이 사라졌다(BakedCollapse 주석 참조).
            var anim=o.GetComponent<Animation>(); if(anim!=null)anim.enabled=false;
            if(BakedCollapse.Load()) o.AddComponent<BakedCollapse>().Bind(o.transform);
            else o.AddComponent<CollapseFade>();   // 구운 좌표가 없으면 제자리에 두었다 사라진다
        }
    }

    /// <summary>
    /// FBX 가 들고 온 재질을 게임 팔레트로 덮는다. Blender 쪽 색을 그대로 쓰면
    /// 같은 절벽인데 무너지는 순간에만 색이 달라진다 — 조명도 다르게 받는다.
    /// 이름으로 층을 읽는다(Chunk_&lt;층&gt;_행_열 · Turf_행_열).
    /// </summary>
    void Repaint(GameObject root)
    {
        foreach(var r in root.GetComponentsInChildren<Renderer>())
        {
            string n=r.gameObject.name;
            Color c;
            if(n.StartsWith("Turf")) c=grass;
            else
            {
                int band=0; var parts=n.Split('_');
                if(parts.Length>1) int.TryParse(parts[1],out band);
                c=C(cliffStrata[Mathf.Clamp(band,0,cliffStrata.Length-1)]);
            }
            r.sharedMaterial=Mat(c);
        }
    }

    /// <summary>Blender 자산이 없을 때의 대비. 예전 연출이다.</summary>
    void LegacyCollapse(Vector3 pos)
    {
        for(int i=0;i<15;i++)
        {
            var debris=Rock(pos+new Vector3(R(-.6f,.6f),R(-.2f,.6f),R(-.6f,.6f)),new Vector3(R(.08f,.23f),R(.12f,.30f),R(.07f,.24f)),stone,"Falling cliff fragment");
            var fx=debris.AddComponent<FallingFragment>();fx.velocity=new Vector3(R(-.45f,.45f),R(.3f,1.1f),R(-.45f,.45f));
        }
    }
}
public class ShoreMotion:MonoBehaviour
{
    Vector3 origin;float offset;void Start(){origin=transform.position;offset=origin.x*2+origin.z;}
    void Update(){transform.position=origin+new Vector3(Mathf.Sin(Time.time*.9f+offset)*.07f,Mathf.Sin(Time.time*1.3f+offset)*.025f,0);transform.localScale=Vector3.one*(.85f+Mathf.Sin(Time.time*1.4f+offset)*.15f);}
}
public class OutlinePulse:MonoBehaviour
{
    Vector3 original;void Start(){original=transform.localScale;}void Update(){transform.localScale=original*(1+Mathf.Sin(Time.time*3)*.015f);}
}
public class SmokeDrift:MonoBehaviour
{
    public float phase;Vector3 origin;void Start(){origin=transform.position;}
    void Update(){float t=Mathf.Repeat(Time.time*.22f+phase,1);transform.position=origin+new Vector3(t*.35f,t*.65f,t*.08f);transform.localScale=Vector3.one*Mathf.Sin(t*Mathf.PI);}
}
/// <summary>
/// 구운 붕괴가 끝나면 치운다. **갑자기 사라지면 눈에 띄므로** 잠깐 두었다가 줄여서 없앤다.
/// 잔해를 영구히 남기는 것은 ArtIsland 의 RockRubble 이 한다 — 이쪽은 순간 연출이다.
/// </summary>
public class CollapseFade:MonoBehaviour
{
    float time; const float Hold=2.6f, Fade=1.1f;
    void Update()
    {
        time+=Time.deltaTime;
        if(time<Hold)return;
        float k=1f-(time-Hold)/Fade;
        if(k<=0f){Destroy(gameObject);return;}
        transform.localScale=Vector3.one*k;
    }
}
public class FallingFragment:MonoBehaviour
{
    public Vector3 velocity;float time;
    void Update(){time+=Time.deltaTime;velocity+=Vector3.down*Time.deltaTime*3;transform.position+=velocity*Time.deltaTime;transform.Rotate(32*Time.deltaTime,60*Time.deltaTime,20*Time.deltaTime);if(time>1.5f){transform.localScale*=Mathf.Exp(-Time.deltaTime*5);if(time>2.4f)Destroy(gameObject);}}
}
public class DioramaAtmosphere:MonoBehaviour
{
    Material material;
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {if(material==null)material=new Material(Shader.Find("PoC/Atmosphere"));Graphics.Blit(source,destination,material);}
    void OnDestroy(){if(material!=null)Destroy(material);}
}
public class ArrivalMotion:MonoBehaviour
{
    float elapsed;
    void Update(){elapsed+=Time.deltaTime;float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/2.2f));transform.localPosition=new Vector3(0,0,Mathf.Lerp(-4.57f,0,t));}
}
public class BirdDrift:MonoBehaviour
{
    public float radius,height,speed,phase;
    void Update()
    {
        float a=Time.time*speed+phase;
        // 원을 그리며 돈다. 높이를 조금 흔들어야 활공으로 보인다.
        transform.position=new Vector3(Mathf.Cos(a)*radius,height+Mathf.Sin(a*2.3f)*.35f,Mathf.Sin(a)*radius);
        transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg+90,Mathf.Sin(a*3.1f)*14);
        // 날갯짓. 폭을 좁게 둬야 새로 보이고 넓으면 나비가 된다.
        float flap=.85f+Mathf.Sin(Time.time*7.5f+phase)*.18f;
        transform.localScale=new Vector3(1,flap,1);
    }
}
