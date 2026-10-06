using System;
using System.Collections.Generic;
using UnityEngine;
using FarmErosion.Sim;

// 구역 A — 바다 · 원경 · 안개. 수면 셰이더가 흔들 정점을 만드는 곳이다.
//
// Art.cs 한 파일에 바다·섬·소품이 모두 있어서 여러 사람이 동시에 손댈 수 없었다.
// partial class 라 파일만 갈라도 소유권이 갈린다. **구역 밖 메서드를 이 파일로 옮기지 말 것** —
// 옮기는 순간 다시 한 파일이 되고 병렬 작업이 충돌한다.
//
// ─── 배치의 기준: 월드 반지름이 아니라 화면 좌표다 ────────────────────────
// 이 파일의 바위·원경 섬·안개는 전부 **화면 좌표(sx,sy ∈ [-1,1])로 적어 두고 런타임에
// 월드로 역투영한다.** 월드 좌표로 박아 두면 카메라가 조금만 바뀌어도 전부 화면 밖으로 나간다.
// 실제로 그랬다 — 예전 판은 원경 섬 8개와 등대가 **한 개도 화면에 없었고**, 해상 바위 18개는
// 반지름 12~27 산포라 평균 서넛만 걸쳤다.
//
// ScreenToSea() 가 매 Rebuild 마다 **살아 있는 카메라**에서 크기·화면비·각도를 다시 읽으므로,
// 구역 C 가 직교 크기를 바꿔도 sx,sy 는 그대로 맞는다.
// 가장자리 물건은 |sx| ≤ 0.88 안쪽에만 둔다 — 16:10 으로 좁아져도 |sx| ≤ 0.98 이다.
//
// ─── 2026-09-29 라운드 2 「여유」 ─────────────────────────────────────
// 라운드 1 빌드는 바다가 **뿌연 흰색**이었고 원경 바위가 화면 곳곳에 빽빽했다.
// 이번 라운드는 더하는 작업이 아니라 덜어내는 작업이다. 이 파일에서 바꾼 것:
//
//   · 수면 흰색의 원인은 넷이었다 — ① 물마루 거품 문턱이 낮아 **먼바다 전체**에 흰 얼룩이
//     떴고 ② 링 바깥 물보라가 월드 3.3유닛 폭이었으며 ③ 바위 밑동 "고리"가 반폭=반지름이라
//     실은 **속이 찬 흰 원반**이었고(그게 29개) ④ 안개 띠 셋 중 하나가 근경 수면을 덮었다.
//     넷을 다 줄였다. 같은 기준으로 재면(화면 좌표를 수면으로 역투영해 셰이더 식을 그대로
//     수치 계산) 보이는 바다에서 foam 평균 0.201 → 0.073, 흰색(>0.8) 면적 8.11% → 4.44%,
//     **먼바다(해안선에서 2.5유닛 바깥)는 0.132 → 0.013, 반쯤 흰 면적 12.5% → 0.69%.**
//     남은 흰색은 해안선(해안 0.3유닛 밖에서 1.00, 1.2유닛에서 0.03)과 바위 밑동뿐이다
//   · **포말 링을 경계상자에서 실제 해안선(CoastSegments) 거리장으로 바꿨다.** 십자의
//     오목한 구석과 분리된 조각에도 포말이 붙고, 칸이 사라지면 그 변의 포말도 사라진다
//   · 앞바다 바위 29개 → 12개. 화면 산포를 버리고 **섬 중심 반지름 6.4~9.0 의 극좌표**로
//     모았다. "섬 가까이"는 화면이 아니라 월드의 관계다. 기울기·높이 편차로 기둥 반복을 깼다
//   · 원경 섬 7개+등대 → 6개+등대. 전부 화면 sy ≥ +0.40(수평선 쪽)으로 올리고 폭을 절반으로.
//     사각 Cube 잔디판("네모난 테이블")을 절벽과 같은 9각 뚜껑으로 바꿨다
//   · 안개 띠 3겹 → 2겹. 둘 다 화면 위쪽. 근경 수면에는 안개가 없다
//   · 수심 램프 길이 15 → 16. 최심색이 화면 모서리(중심에서 21.2)에서 정확히 닿는다
//
// **카메라가 직교 5.8 → 7.8 로 넓어졌다(구역 C2 확정).** 화면 UV 배치는 그대로 유효하지만
// 월드 크기는 따라 줄지 않으므로 손으로 깎았다. 화면에서 차지하는 크기는
// (월드 축소) × (5.8/7.8 = 0.74) 의 곱이다 — 바위 높이는 0.56배, 원경 섬 폭은 0.41배가 된다.
// 배치는 7.8·8.2·8.6 과 화면비 16:9·16:10, yaw ±45° 를 모두 계산해 확인했다.
// (바위는 월드에 고정되어야 시차가 맞는다 — 화면에 붙이면 안 된다.)
public sealed partial class LowpolyGame
{
    // 수면 셰이더에 넘기는 "바위 밑동 포말" 슬롯 수. Water.shader 의 STACK_FOAM_MAX 와 같아야 한다.
    const int StackFoamSlots=24;
    // 해안선 선분 슬롯 수. Water.shader 의 COAST_MAX 와 같아야 한다.
    // 초기 24칸(6x4) + 주거지 + 분리 조각 셋이면 34개, 십자로 깎여도 45개 언저리다.
    // 64 는 칸이 흩어져 둘레가 길어지는 최악에도 여유가 있는 값이고, 넘치면 앞에서부터
    // 64개만 쓴다(포말이 몇 변에서 빠질 뿐 화면이 깨지지는 않는다).
    const int CoastSlots=64;

    /// <summary>
    /// 살아 있는 카메라의 기준축. 직교 투영 역산에 필요한 것만 꺼낸다.
    ///
    /// Start() 안의 첫 Rebuild 때는 Update() 가 아직 돌지 않아 카메라 트랜스폼이 원점이다.
    /// 그때만 Update() 와 같은 식으로 세운다 — **LowpolyGame.Update() 의 카메라 식이 바뀌면
    /// 아래 두 줄도 같이 바꿔야 한다.** 그 뒤의 Rebuild 는 전부 실제 카메라를 읽으므로
    /// 구역 C 가 크기·각도를 바꿔도 따라간다.
    /// </summary>
    void CameraBasis(out Vector3 eye,out Vector3 forward,out Vector3 right,out Vector3 up,out float size,out float aspect)
    {
        Vector3 target=new Vector3(1.70f,.20f,.25f);
        if(cam!=null && cam.transform.position!=Vector3.zero)
        {
            eye=cam.transform.position; forward=cam.transform.forward;
            right=cam.transform.right;  up=cam.transform.up;
            size=cam.orthographicSize;  aspect=cam.aspect>.1f?cam.aspect:16f/9f;
        }
        else
        {
            eye=Quaternion.Euler(0,yaw,0)*new Vector3(10,14,-17)+target;
            forward=(target-eye).normalized;
            right=Vector3.Normalize(Vector3.Cross(Vector3.up,forward));
            up=Vector3.Cross(forward,right);
            size=cam!=null?cam.orthographicSize:8.2f;
            aspect=16f/9f;
        }
    }

    /// <summary>
    /// 화면 좌표(sx,sy ∈ [-1,1]) 를 높이 y 의 수평면 위 한 점으로 역투영한다.
    /// 직교 투영이라 광선이 전부 평행이어서 역산이 한 줄로 끝난다.
    /// </summary>
    Vector3 ScreenToSea(float sx,float sy,float y)
    {
        CameraBasis(out var eye,out var forward,out var right,out var up,out var size,out var aspect);
        // 카메라가 수평이면 수면과 만나지 않는다. 그럴 일은 없지만 0 나눗셈은 막는다.
        float drop=Mathf.Max(-forward.y,.05f);
        Vector3 p=eye+right*(sx*size*aspect)+up*(sy*size);
        return p+forward*((p.y-y)/drop);
    }

    /// <summary>
    /// 잘게 나눈 바다 평면. 정점 변위 셰이더는 정점이 있어야 흔들 수 있다 —
    /// 큐브(정점 8개)에 파도 셰이더를 걸면 아무 일도 일어나지 않는다.
    ///
    /// **평면 크기(±70)는 건드리지 않는다.** 화면에 들어오는 수면은 X -16~17 / Z -14~19 뿐이라
    /// 이미 충분히 덮는다. 문제는 크기가 아니라 표본이었다:
    /// 예전에는 140을 96등분해 간격이 1.458 인데 파도의 최단 파장이 0.927 이었다.
    /// 정점이 파도를 못 따라가니 파면이 지글거리기만 하고 "잔물결"로 읽히지 않았다.
    ///
    /// 균일 385×385(간격 0.365)로 올리면 삼각형이 29.6만 개다. 대신 격자를 **차등**으로 짠다 —
    /// 화면에 보이는 ±22 유닛만 0.52 간격으로 깔고 바깥은 간격을 1.34배씩 불려 수평선까지 뻗는다.
    /// 삼각형 2.25만 개(예전 1.84만)로 보이는 곳의 해상도만 2.8배 올린 셈이다.
    /// 동시에 최단 파장을 0.927 → 1.47 로 늘려 표본이 파장당 2.8개가 되게 맞췄다.
    /// </summary>
    GameObject SeaSurface()
    {
        var axis=GradedAxis(70f,22f,.52f,1.34f);
        var v=new List<Vector3>(); var t=new List<int>(); var colors=new List<Color>();
        for(int i=0;i<axis.Count-1;i++)for(int j=0;j<axis.Count-1;j++)
        {
            float x0=axis[i],x1=axis[i+1],z0=axis[j],z1=axis[j+1];
            Vector3 a=new Vector3(x0,0,z0), b=new Vector3(x1,0,z0), c=new Vector3(x0,0,z1), d=new Vector3(x1,0,z1);
            Tri(v,t,colors,a,c,b,Color.white); Tri(v,t,colors,b,c,d,Color.white);
        }
        var o=new GameObject("Moving sea"); o.transform.SetParent(world.transform); o.transform.position=new Vector3(0,-1.70f,0);
        var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.SetVertices(v); mesh.SetTriangles(t,0); mesh.SetColors(colors); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        generatedMeshes.Add(mesh);
        o.AddComponent<MeshFilter>().sharedMesh=mesh;
        if(waterMaterial==null)waterMaterial=new Material(Shader.Find("PoC/Water"));

        // 수면 색. 시안이 요구하는 구간은 #2f5f72(섬 근처) ~ #1b3345(화면 끝)다.
        // 라운드 1 빌드에서 바다가 희게 보인 것은 **이 색이 틀려서가 아니라 포말이 덮어서**였다
        // (보이는 바다의 foam 평균이 0.198 — 평균 픽셀이 #edf6f8 쪽으로 20% 끌려갔다.
        //  #2f5f72 의 채도 0.42 가 #547c8c 의 0.25 로 주저앉는다. 고친 뒤 평균은 #3c6a7b·0.34).
        //  색은 시안 값으로 못 박고
        // 포말은 아래 ShoreRing() · SeaStacks() · Water.shader 에서 줄였다.
        waterMaterial.SetColor("_Shallow",C("2f5f72"));
        waterMaterial.SetColor("_Mid",C("244a5c"));
        waterMaterial.SetColor("_Deep",C("1b3345"));
        waterMaterial.SetColor("_FoamColor",C("edf6f8"));

        ShoreRing();

        // 파고·법선 과장·계단화. **라운드 3 에서 이 셋이 대각선 띠의 본체였다.**
        // 0.30 × 1.45 = 면 기울기 평균 21.6°·p95 36.0° — 파형 급경사 H/λ 가 0.225 로
        // 실제 파도가 부서지는 한계(0.14)를 넘는다. 잔물결이 아니라 능선이었다.
        // 0.26 × 1.25 로 내리면 평균 11.4°·p95 23.9° 다. 근거와 측정은 Water.shader 머리 주석.
        waterMaterial.SetFloat("_WaveHeight",.26f);
        waterMaterial.SetFloat("_NormalBoost",1.25f);
        waterMaterial.SetFloat("_Posterize",.25f);
        // **무늬를 푸는 셋.** 사인 합은 항을 다섯으로 늘리고 방향을 벌려도 자기상관이
        // 남아(FFT 집중도 0.913 → 0.894) 평행한 띠로 보인다. 좌표를 값 노이즈로 휘고
        // (칸 3.8유닛) 진폭을 얼룩지게(칸 4.5유닛) 만들어야 0.839 까지 내려간다.
        // 시안 0.65~0.71 · 백색잡음 0.06 · 순수 사인 1.00 이 이 척도의 기준점이다.
        waterMaterial.SetFloat("_WarpAmount",2.60f);
        waterMaterial.SetFloat("_WarpScale",.26f);
        waterMaterial.SetFloat("_Gust",.66f);      // 진폭 [0.34, 1.66] 배
        waterMaterial.SetFloat("_GustScale",.22f);
        waterMaterial.SetFloat("_Grain",.08f);     // 방향 없는 결. 알베도에만 들어간다
        // 환경광·환경 반사 감쇠. 같은 화면 자리에서 캡처와 헤드리스 재현의 차이가
        // 채널별로 거의 같은 +0.075~+0.136(무채색 덧셈)이었다 — 짙은 청록이 회청색으로
        // 보이는 원인이다. 근거와 한계는 Water.shader 의 o.Occlusion 주석에 적어 뒀다.
        waterMaterial.SetFloat("_GiDamp",.66f);
        // 섬 둘레는 잔잔하게. 잃은 칸의 점선 윤곽(y=-1.63)과 구역 B 의 Shore foam(y=-1.585)이
        // 수면 위 0.07~0.12 에 있어서, 여기에 파도가 서면 그것들이 물에 잠긴다.
        waterMaterial.SetFloat("_CalmInner",1.02f);
        waterMaterial.SetFloat("_CalmOuter",1.75f);

        o.AddComponent<MeshRenderer>().sharedMaterial=waterMaterial;
        SeaStacks();
        return o;
    }

    /// <summary>
    /// 해안 포말 · 파도 감쇠 · 수심 램프의 기준을 셰이더에 넘긴다. **셋은 서로 다른 기하다.**
    ///
    /// ── 1. 포말: 실제 해안선 ─────────────────────────────────────────
    /// 라운드 1 까지는 활성 칸의 경계상자로 p-노름(둥근 직사각형)을 만들어 둘렀다. 그래서
    /// **십자의 오목한 구석에는 땅이 없는데도 링이 지나가고**, 분리된 섬 조각에는 포말이
    /// 붙지 않았다. 사라진 칸 위에 후광만 남는 화면이 그 때문이다.
    ///
    /// 지금은 구역 B 의 `CoastSegments`(물에 닿는 변의 선분 목록, Vector4 = a.x,a.z,b.x,b.z)를
    /// 그대로 셰이더에 넘겨 **선분까지의 거리장**으로 두른다. 오목한 구석도, 조각도 따라 붙고,
    /// 칸이 사라지면 그 변의 포말도 같이 사라진다.
    ///
    /// **이게 가능해진 것은 호출 순서가 바뀌었기 때문이다.** 예전에는 SeaSurface() 가
    /// FarmScene() 의 앞에서 불려 그 시점에 CoastSegments 가 비어 있었다(이 자리의 옛 주석이
    /// "구역 B 와 합의가 필요하다"고 적어 둔 이유다). 지금은 FarmScene() 의 **맨 끝**에서
    /// 부르므로 목록이 이미 차 있다 — 옛 주석은 사실이 아니라서 지웠다.
    ///
    /// ── 2. 파도 감쇠: 격자 전체를 덮는 고정 상자 ────────────────────
    /// 파도 감쇠는 해안선을 따라가면 **안 된다.** 잃은 칸의 점선 윤곽(y=-1.63)은 지금 해안선에서
    /// 멀리 떨어진 물 위에 있는데, 거기 파도가 서면(물마루 -1.40) 윤곽이 잠겨 게임 정보가 사라진다.
    /// 그래서 감쇠만은 **침식과 무관하게 격자 전체**(활성 여부를 보지 않는다)를 덮는 상자로 잰다.
    ///
    /// ── 3. 수심 램프 ────────────────────────────────────────────────
    /// 같은 상자의 중심에서 재는 거리. 길이는 화면 안에서 끝나야 한다 — 아래 주석 참고.
    /// </summary>
    void ShoreRing()
    {
        // ── 1. 포말: 해안선 선분을 그대로 넘긴다 ──────────────────────
        // 빈 칸은 아주 먼 곳의 점으로 채운다. 그러면 셰이더 루프가 개수를 세는 분기 없이
        // 돌고(거리가 항상 최솟값을 못 이긴다), Unity 가 기억하는 배열 길이도 늘 같다.
        var segments=new Vector4[CoastSlots];
        for(int i=0;i<CoastSlots;i++)segments[i]=new Vector4(9999f,9999f,9999f,9999f);
        int count=Mathf.Min(CoastSegments.Count,CoastSlots);
        for(int i=0;i<count;i++)segments[i]=CoastSegments[i];
        waterMaterial.SetVectorArray("_Coast",segments);
        // 월드 단위다(정규화가 아니다). **라운드 3 에서야 이 값을 조일 수 있게 됐다** —
        // 구역 B 가 EdgePoint 의 좌표 계약 버그를 고쳐 _Coast 선분이 처음으로 실제 해안에
        // 붙었기 때문이다. 0.30/0.80 은 틀린 기준 위에서 고른 값이라 그대로 두면 안 된다.
        // 0.24/0.46 에서 해안 거리별 포말(시간·흔들림 위상 평균)은
        //   0.0유닛 0.42 · 0.2 0.85 · 0.3 0.86 · 0.45 0.58 · 0.6 0.22 · 0.8 0.01 · 1.2 0.00
        // — 시안의 접촉선(해안 0.3 안쪽은 희고 1.2 에서 사라진다)과 같은 폭이다.
        waterMaterial.SetFloat("_FoamOffset",.24f);
        waterMaterial.SetFloat("_FoamWidth",.46f);
        waterMaterial.SetFloat("_FoamSurge",.11f);   // 밀물. 링이 ±0.11유닛 숨쉰다(주기 7.4초)

        // ── 2·3. 격자 전체를 덮는 고정 상자 ──────────────────────────
        // **Active 를 보지 않는다.** 침식으로 칸이 사라져도 상자가 줄지 않아야
        // 그 자리의 점선 윤곽이 계속 잔잔한 물 위에 있다.
        const float halfTile=.735f;
        float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
        if(sim!=null)
            for(int i=0;i<sim.Plots.Count;i++)
            {
                Vector3 at=PlotPosition(sim.Plots[i]);
                minX=Mathf.Min(minX,at.x-halfTile); maxX=Mathf.Max(maxX,at.x+halfTile);
                minZ=Mathf.Min(minZ,at.z-halfTile); maxZ=Mathf.Max(maxZ,at.z+halfTile);
            }
        // 집이 있는 뒤쪽 단. ArtIsland.FarmScene() 이 격자 (1..3, 4) 에 까는 칸이다.
        for(int gx=1;gx<=3;gx++)
        {
            float lx=(gx-2.5f)*1.47f;
            minX=Mathf.Min(minX,lx-halfTile); maxX=Mathf.Max(maxX,lx+halfTile);
        }
        minZ=Mathf.Min(minZ,3.68f-halfTile); maxZ=Mathf.Max(maxZ,3.68f+halfTile);

        Vector2 centre=new Vector2((minX+maxX)*.5f,(minZ+maxZ)*.5f);
        Vector2 half=new Vector2(Mathf.Max((maxX-minX)*.5f,1.2f)+.10f,
                                 Mathf.Max((maxZ-minZ)*.5f,1.2f)+.10f);
        waterMaterial.SetVector("_ShoreCenter",new Vector4(centre.x,centre.y,0,0));
        waterMaterial.SetVector("_ShoreHalf",new Vector4(half.x,half.y,0,0));
        waterMaterial.SetFloat("_ShorePower",2.8f);
        // 수심 램프. **화면 안에서 끝나야 한다.** 직교 7.8 에서 화면 테두리까지의 거리는
        // 변 가운데가 12.1, 모서리가 21.2 다(계산해 확인). _ShoreRadius 5.41 + 길이 16 이면
        // 최심색 _Deep 에 정확히 모서리(21.4)에서 닿는다 — 램프 전 구간이 화면 안에 보인다.
        // 라운드 1 의 15 는 크기 5.8 에 맞춘 값이라 넓어진 화면에서는 너무 짧고(귀퉁이가
        // 통째로 최심부 + 비네팅으로 #141d2a 까지 죽었다), 22 는 최심색이 화면 밖으로 나간다.
        waterMaterial.SetFloat("_ShoreRadius",Mathf.Max(half.x,half.y)*1.2f);
        waterMaterial.SetFloat("_DepthFade",16.0f);
    }

    /// <summary>
    /// 중심이 촘촘하고 바깥이 성긴 1차원 격자. 같은 정점 수로 보이는 곳의 해상도를 높인다.
    /// </summary>
    static List<float> GradedAxis(float half,float inner,float fine,float growth)
    {
        var side=new List<float>();
        for(float x=0;x<inner;x+=fine) side.Add(x);
        float step=fine, at=side[side.Count-1];
        while(at<half) { step*=growth; at+=step; side.Add(Mathf.Min(at,half)); }
        var axis=new List<float>();
        for(int i=side.Count-1;i>0;i--) axis.Add(-side[i]);
        axis.AddRange(side);
        return axis;
    }

    /// <summary>
    /// 바다에 선 돌출 바위. 시안의 바다가 "바다"로 읽히는 이유의 절반이 이것이다 —
    /// 비교 대상이 없으면 수면은 그냥 색칠한 바닥이다.
    ///
    /// **라운드 2: 29개 → 12개, 섬 가까이로 모았다.**
    /// 라운드 1 은 화면 좌표 18자리에 새끼 바위를 붙여 29개를 세웠다. 화면으로 흩으면 개수는
    /// 보장되지만 **섬에서 먼 빈 바다에도 바위가 서서** 화면이 빽빽해진다. 사용자 피드백의
    /// "오브젝트들이 빽빽하고 여유가 없어 보인다"가 정확히 이 배치를 가리킨다.
    ///
    /// 지금은 섬 중심에서 반지름 6.4~9.0 의 극좌표로 박는다 — "섬 가까이"는 화면이 아니라
    /// **월드의 관계**이기 때문이다. 직교 7.8 에서 보이는 바다가 반지름 17 이상이므로
    /// 반지름 9 는 화면 안이 보장된다(7.8·8.2·8.6 × 16:9·16:10 × yaw ±45° 전부 |sx|,|sy| < 0.8).
    /// 월드 고정이라 화면 좌표와 달리 **Q/E 로 돌려도 섬과의 거리가 그대로**인 이점도 있다.
    /// 앞쪽(화면 아래) 방위각은 두 자리만 남겨 **근경 바다를 비운다.**
    ///
    /// 크기도 깎았다. 수면 위 높이 3.0 → 1.15~1.65, 폭 최대 1.09 → 0.96.
    /// 카메라가 5.8 → 7.8 로 넓어지는 것(화면에서 0.74배)과 곱하면 화면 높이로 0.56배다.
    ///
    /// 밑동 포말은 판으로 깔지 않는다 — 파도에 잠기기 때문이다. 수면 셰이더에 위치를
    /// 넘겨 수면 자체에 고리를 그린다. **고리의 반폭을 반지름과 같게 두면 고리가 아니라
    /// 속이 찬 흰 원반이 된다**(라운드 1 이 그랬다: 반지름 w*1.15 에 반폭 w*1.00 이면
    /// 0.15w~2.15w 가 통째로 하얘진다). 반폭을 w*0.42 로 좁혀 테두리만 남긴다.
    /// </summary>
    void SeaStacks()
    {
        // ShoreRing() 이 쓰는 섬 중심과 같은 자리. 칸이 침식돼도 크게 움직이지 않으므로
        // 상수로 둔다(여기서 sim 을 다시 훑으면 반지름 기준이 프레임마다 흔들린다).
        const float cx=0f, cz=.80f;
        // (섬 중심에서의 방위각°, 반지름). 화면으로 옮기면 대략
        // (+0.11,+0.42) (+0.36,+0.25) (+0.34,-0.05) (+0.32,-0.39) (-0.04,-0.48)
        // (-0.32,+0.42) (-0.16,+0.58) (-0.67,+0.15) — 섬을 둘러싸되 화면 맨 아래는 비어 있다.
        // 남서쪽(방위각 200~260°)은 구역 B 의 분리 섬 조각 셋이 반지름 9.5 까지 차지하므로 비웠다.
        var ring=new[]{
            new Vector2( 95f,6.6f), new Vector2( 58f,7.6f), new Vector2( 22f,6.4f),
            new Vector2(350f,8.2f), new Vector2(296f,7.4f), new Vector2(152f,6.9f),
            new Vector2(126f,8.6f), new Vector2(198f,9.0f),
        };
        Color nearRock=C("4d585c"), farRock=C("6f8189");
        var foam=new List<Vector4>();

        foreach(var s in ring)
        {
            float a=s.x*Mathf.Deg2Rad;
            float x=cx+Mathf.Cos(a)*s.y, z=cz+Mathf.Sin(a)*s.y;
            float t=Mathf.Clamp01((s.y-5.6f)/4.2f);      // 0 섬 가까이 … 1 가장 먼 바위
            float w=Mathf.Lerp(.86f,.48f,t)*R(.88f,1.12f);
            // 꼭대기가 수면(-1.70) 위 1.15~1.65 에 온다. 섬 윗면이 0.9 이므로 절벽 높이(2.6)의
            // 44~63% — 바위가 섬과 키를 다투지 않는 선이다. 밑동은 -2.27 언저리라 항상 잠긴다.
            float topY=Mathf.Lerp(-.05f,-.55f,t)+R(-.22f,.22f);
            float height=topY+2.30f;
            Color body=Color.Lerp(nearRock,farRock,t)*R(.92f,1.08f); body.a=1;
            var stack=Rock(new Vector3(x,topY-height*.48f,z),
                 new Vector3(w,height,w*R(.78f,1.05f)),body,"Offshore sea stack");
            // **같은 기울기의 가는 기둥이 반복되면 울타리로 보인다.** 조금씩 눕힌다.
            // MeshObject() 가 무게중심을 원점으로 옮겨 놓으므로 회전축이 바위 한가운데다 —
            // 기울여도 밑동이 0.16 유닛밖에 안 움직이고, 그 밑동은 어차피 물에 잠겨 있다.
            stack.transform.rotation=Quaternion.Euler(R(-9f,9f),R(0f,360f),R(-9f,9f));
            foam.Add(new Vector4(x,z,w*1.05f,w*.42f));

            // 가까운 바위만 짝을 짓는다. 시안의 바위는 혼자 서지 않고 두셋이 무리를 이룬다.
            // 새끼는 **반드시 바깥쪽으로** 민다 — 예전처럼 사방 난수로 흩으면 안쪽으로 밀린
            // 것이 섬 절벽에 얹혀 "물 위에 뜬 덩어리"가 된다.
            if(s.y<7.6f)
            {
                float px=Mathf.Cos(a), pz=Mathf.Sin(a);
                float outward=R(.9f,1.8f), side=R(-1.4f,1.4f);
                float ax=x+px*outward-pz*side, az=z+pz*outward+px*side;
                float aw=w*R(.50f,.68f);
                float atop=-.62f+R(-.12f,.12f); float ah=atop+2.30f;
                Color sat=nearRock*R(.88f,1.06f); sat.a=1;
                var pup=Rock(new Vector3(ax,atop-ah*.48f,az),
                     new Vector3(aw,ah,aw*R(.80f,1.10f)),sat,"Offshore sea stack");
                pup.transform.rotation=Quaternion.Euler(R(-12f,12f),R(0f,360f),R(-12f,12f));
                if(foam.Count<StackFoamSlots) foam.Add(new Vector4(ax,az,aw*1.05f,aw*.45f));
            }
        }

        // 셰이더 배열은 길이가 고정이다. Unity 는 첫 SetVectorArray 의 길이를 기억하므로
        // 항상 24칸을 채워 보낸다 — 빈 칸은 w=0 이라 셰이더에서 꺼진다(지금은 12칸만 쓴다).
        var slots=new Vector4[StackFoamSlots];
        int count=Mathf.Min(foam.Count,StackFoamSlots);
        for(int i=0;i<count;i++)slots[i]=foam[i];
        waterMaterial.SetVectorArray("_StackFoam",slots);
        waterMaterial.SetFloat("_StackFoamCount",count);
    }

    /// <summary>
    /// 원경. 시안의 깊이는 대부분 여기서 온다 — 안개에 묻히는 먼 절벽과 등대가 있어야
    /// 섬이 "바다 한가운데"에 있는 것으로 읽힌다. 비어 있으면 회색 배경 위의 모형이 된다.
    ///
    /// **라운드 2: 7개+등대 → 6개+등대, 전부 수평선 쪽으로.**
    /// 라운드 1 은 화면 sy 를 -0.76 부터 +0.82 까지 흩어 놓아 **근경 바다 한복판에 폭 4.2 짜리
    /// 녹색 판이 서 있었다.** 화면 좌·우·하단에서 색을 재면 색조가 76~146°(녹색)로 나온다 —
    /// 그 구역은 바다가 아니라 원경 섬이 칠하고 있었다는 뜻이다.
    ///
    /// 지금은 **sy ≥ +0.40 에만 둔다.** 폭 2.4~4.2 → 1.15~2.20, 높이 0.70~1.55 → 0.46~0.90.
    /// 직교 7.8 에서 가장 큰 섬이 화면 113px(가로의 6.8%)다 — 라운드 1 은 295px 였다.
    /// 덤불도 3포기 → 2포기, 키를 1.05 → 0.45 로 낮췄다.
    ///
    /// 가로 자리는 HUD 를 피해 골랐다. 우측 침식예보 패널이 화면 가로 79%(sx +0.58)에서
    /// 시작하므로 그보다 오른쪽은 패널 위(sy +0.9 이상)에만 둔다.
    /// </summary>
    void DistantIslands()
    {
        // (화면 sx, 화면 sy, 폭). 자리는 HUD 를 피해 골랐다 — 꼭대기의 0..1 UV 는 차례대로
        // (0.09,0.29) (0.13,0.11) (0.31,0.07) (0.47,0.14) (0.70,0.06) (0.81,0.06) 이고
        //   · 좌상단 제목 블록   UV x 0.05~0.26 · y 0.01~0.09
        //   · 상단 자원 알약     UV x 0.33~0.66 · y 0.02~0.08
        //   · 우상단 연·계절     UV x 0.85~0.99 · y 0.02~0.09
        //   · 우측 침식예보 패널 UV x 0.79~0.99 · y 0.12~0.47
        // 여섯 개 모두 이 네 상자 밖이다. 불투명 패널 뒤에 섬을 세우면 없는 것과 같다.
        // |sx| ≤ 0.82 로 묶어 16:10 에서도 |sx| ≤ 0.91 이다. 직교 7.8 기준 화면 크기는
        // 113×62 · 77×47 · 61×45 · 72×50 · 59×44 · 66×44 px — 시안의 수평선 섬과 같은 급이다.
        var spots=new[]{
            new Vector3(-0.82f, 0.40f, 2.20f),   // 왼쪽 가장자리. 가장 가깝고 가장 크다
            new Vector3(-0.74f, 0.80f, 1.50f),
            new Vector3(-0.38f, 0.88f, 1.20f),
            new Vector3(-0.06f, 0.74f, 1.40f),   // 자원 알약 **아래**로 내렸다
            new Vector3( 0.40f, 0.90f, 1.15f),
            new Vector3( 0.62f, 0.90f, 1.30f),   // 알약과 우상단 텍스트 사이의 틈
        };
        foreach(var s in spots)
        {
            Vector3 at=ScreenToSea(s.x,s.y,-0.55f);
            // 전부 수평선 쪽으로 모았으므로 예전 (sy+0.85)/1.70 식으로는 t 가 0.74~1.00 에
            // 뭉쳐 원근 차이가 사라진다. 남은 구간 안에서 다시 편다.
            float t=Mathf.Clamp01(Mathf.InverseLerp(.38f,.98f,s.y));
            // 멀수록 낮고 옅다. 직교 카메라는 원근으로 작아지지 않으니 손으로 깎아 줘야 한다.
            float rise=Mathf.Lerp(.90f,.46f,t)*R(.90f,1.10f);
            float haze=Mathf.Lerp(.52f,.84f,t);
            DistantIsle(at.x,at.z,s.z*R(.90f,1.08f),rise,haze);
            // 섬마다 덤불 두 포기. 실루엣이 매끈한 덩어리면 바위인지 섬인지 안 읽힌다.
            // 키를 1.05 → 0.45 로 낮췄다 — 섬보다 덤불이 높으면 섬이 아니라 바위 무리로 보인다.
            // 라운드 3: 섞는 끝색을 하늘색 #a3b8c0 에서 배경 안개색 계열 #47606f 로 내린다.
            // 원경은 밝아지는 게 아니라 **배경에 잠겨야** 멀어 보인다(DistantIsle 주석 참고).
            Color scrub=Color.Lerp(C("4f5c46"),C("47606f"),haze);
            for(int j=0;j<2;j++)
                Rock(new Vector3(at.x+R(-s.z*.26f,s.z*.26f),-1.70f+rise+R(.06f,.16f),at.z+R(-s.z*.26f,s.z*.26f)),
                     new Vector3(R(.16f,.34f),R(.18f,.45f),R(.16f,.34f)),scrub,"Distant scrub");
        }

        // 등대. 시안에 있는 단 하나의 인공물이고, 수평선에 눈이 머무는 자리를 만든다.
        // 화면 (+0.54,+0.68) = UV (0.77,0.16) — 시안의 등대 자리(UV 0.79,0.15)와 같다.
        // 좌우로 더 밀면 침식예보 패널(가로 79% 부터)에 가리고, 위로 더 올리면 랜턴이
        // 상단 HUD 에 들어간다. 직교 7.8 기준 꼭대기가 sy +0.71(UV y 0.147)로 여유가 있다.
        Vector3 light=ScreenToSea(0.54f,0.68f,-0.55f);
        DistantIsle(light.x,light.z,1.55f,.28f,.66f);
        float baseY=-1.70f+.28f+.20f;       // 잔디 윗면. 등대를 얹으려고 섬을 납작하게 깎았다
        // 0.78 → 0.26. 시안의 등대는 탑이 화면 세로의 4.8%(45px/941)뿐인 **작은 표식**이다.
        // 라운드 1 은 s2 0.78 에 밑섬 rise 0.95 라 전체가 화면의 32% 였다 — 원경이 아니라
        // 제2의 주역이 됐다. 지금은 밑섬까지 합쳐 69px, 탑만 45px 로 시안과 같다.
        const float s2=.26f;
        Cube("Lighthouse tower",  light.x,baseY+1.30f*s2,light.z, .80f*s2,2.60f*s2, .80f*s2,C("cfc9b6"));
        Cube("Lighthouse band",   light.x,baseY+1.75f*s2,light.z, .85f*s2, .45f*s2, .85f*s2,C("9d5c4c"));
        Cube("Lighthouse gallery",light.x,baseY+2.67f*s2,light.z,1.10f*s2, .12f*s2,1.10f*s2,C("8d8a78"));
        Cube("Lighthouse lantern",light.x,baseY+2.93f*s2,light.z, .52f*s2, .40f*s2, .52f*s2,C("e8d59a"));
        // 지붕은 Roof() 대신 원뿔형 바위를 쓴다 — Roof() 는 굴뚝을 같이 세워서
        // 등대 꼭대기에 정체불명의 돌기가 생긴다(시안 요구: 정체불명 오브젝트 금지).
        Rock(new Vector3(light.x,baseY+3.26f*s2,light.z),new Vector3(.62f*s2,.56f*s2,.62f*s2),C("6d5344"),"Lighthouse cap");

        // 갈매기. ART_TARGET §7 은 한 마리다. 라운드 1 의 두 마리는 흰 점이 둘로 갈려
        // 잔물결 위에서 눈을 끌었다 — 덜어내는 라운드이니 시안 수치대로 한 마리만 남긴다.
        for(int i=0;i<1;i++)
        {
            var bird=new GameObject("Seabird"); bird.transform.SetParent(world.transform);
            var v=new List<Vector3>(); var t=new List<int>(); var colors=new List<Color>();
            Color feather=C("e6e7e2");
            Tri(v,t,colors,new Vector3(-.18f,0,0),new Vector3(0,.05f,.02f),new Vector3(0,0,-.04f),feather);
            Tri(v,t,colors,new Vector3( .18f,0,0),new Vector3(0,.05f,.02f),new Vector3(0,0,-.04f),feather);
            var mesh=new Mesh(); mesh.SetVertices(v); mesh.SetTriangles(t,0); mesh.SetColors(colors);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); generatedMeshes.Add(mesh);
            bird.AddComponent<MeshFilter>().sharedMesh=mesh;
            if(facetMaterial==null)facetMaterial=new Material(Shader.Find("PoC/Faceted"));
            bird.AddComponent<MeshRenderer>().sharedMaterial=facetMaterial;
            var drift=bird.AddComponent<BirdDrift>();
            drift.radius=R(5.5f,9.5f); drift.height=R(3.4f,5.2f); drift.speed=R(.10f,.19f); drift.phase=R(0,6.28f);
        }
    }

    /// <summary>
    /// 원경의 작은 섬 하나. 본섬의 Cliff() 를 쓰지 않는 이유는 그쪽 색이 고정(갈색 지층)이라
    /// **거리감을 줄 수 없기 때문**이다. 멀리 있는 것은 하늘빛에 섞여 옅어져야 한다.
    /// haze 0 이면 가깝고 1 이면 수평선이다.
    /// </summary>
    void DistantIsle(float x,float z,float width,float rise,float haze)
    {
        // --- 2026-09-29 라운드 3: 흰 뚜껑 쓴 버섯을 고친다 ------------------
        // 라운드 3 캡처에서 원경 섬의 뚜껑 윗면을 재면 #979884 / #a2a08d / #878d86,
        // 휘도 **0.51~0.62** 다. 같은 자리의 승인 시안은 #5e6672 / #53616f / #3d4a54,
        // 휘도 **0.28~0.40** 이고 R<G<B 인 **차가운** 색이다. 즉 지금 원경은 시안보다
        // 0.15~0.25 밝고 한술 더 떠 따뜻하다 — 그래서 버섯 뚜껑·테이블로 읽힌다.
        //
        // 경로는 둘이고 둘 다 코드에서 확인된다:
        //   1. **사전 haze 를 하늘색에 섞었다.** turf 는 #6f8455 를 #a3b8c0 에 haze*0.94
        //      만큼 섞었다. haze 0.84 면 79% — 조명 전 알베도가 이미 #98adaa 다
        //   2. **그 윗면이 씬에서 빛을 가장 잘 받는 면이다.** Staging 의 계산으로 윗면
        //      광량은 (1.105,1.014,0.875), 알베도→화면 이득 1.12 에 따뜻하다
        //
        // 고친 방향: **섞는 끝색을 하늘색이 아니라 배경·안개색(#465a6a 계열)으로** 둔다.
        // 멀리 있는 것은 하얘지는 게 아니라 **배경에 잠긴다.** 카메라 배경이 #465a6a 이므로
        // 그 근처로 수렴해야 실루엣이 녹는다. haze 0.84 에서 윗면 알베도 휘도가
        // 0.655 → 0.341, 화면 예상 0.73 → 0.38 이다(시안 0.28~0.40 의 한가운데).
        // 절벽 측면도 같은 끝색을 쓴다 — haze 0.84 에서 상층 알베도 휘도 0.372,
        // 측면 화면 이득 0.80 을 곱해 0.30 이다(시안의 원경 암벽과 같은 대역).
        Color sky=C("465d6d");
        float top=-1.70f+rise;              // 절벽 윗면 = 수면에서 rise 만큼 위
        float depth=rise+2.60f;             // 수면 아래까지 충분히 내려 잘린 단면이 안 보이게
        const int sides=9;
        var v=new List<Vector3>(); var t=new List<int>(); var colors=new List<Color>();
        var rings=new Vector3[4,sides];
        for(int ring=0;ring<4;ring++)for(int i=0;i<sides;i++)
        {
            float a=i*Mathf.PI*2/sides;
            float r=width*.5f*(1f-ring*.055f)*R(.84f,1.12f);
            rings[ring,i]=new Vector3(x+Mathf.Cos(a)*r,top-ring*depth/3f,z+Mathf.Sin(a)*r);
        }
        // 감는 방향은 본섬 Cliff() 와 똑같이 둔다. 뒤집으면 면이 안쪽을 향해 섬이 사라진다.
        for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++)
        {
            int j=(i+1)%sides;
            // 지층 원색은 그대로 두고 섞는 끝색만 배경색으로 바꿨다 —
            // 층 구조는 원경에서도 읽혀야 한다(비평이 요구한 것은 탈색이 아니라 후퇴다).
            Color col=Color.Lerp(C(ring==0?"8f7c58":ring==1?"6b6151":"4d5250"),sky,haze)*R(.88f,1.08f); col.a=1;
            Tri(v,t,colors,rings[ring,i],rings[ring,j],rings[ring+1,i],col);
            Tri(v,t,colors,rings[ring,j],rings[ring+1,j],rings[ring+1,i],col);
        }
        // 잔디 윗면. 라운드 1 은 폭 .80 x 깊이 .76 의 **사각 Cube** 를 9각 절벽 위에 얹어
        // 원경 섬이 "네모난 테이블"로 보였다. 절벽과 같은 9각으로 뚜껑을 덮는다.
        //
        // **라운드 3: 9각 연결은 그대로 두고 뚜껑을 낮추고 깨뜨린다.**
        // 라운드 2 의 뚜껑은 반지름 1.06배·높이 top+0.16·중앙 +0.05 의 거의 평평한 원반이었다.
        // 몸통이 rise 0.46~0.90 뿐인데 그 위에 0.21 짜리 판이 얹히니 작은 섬일수록 뚜껑이
        // 몸통을 압도한다 — 버섯·테이블 인상의 기하학적 원인이다. 1.03배·평균 top+0.075·
        // 중앙 +0.02 로 낮추고 **꼭짓점마다 높이를 다르게** 해 윗면이 한 장의 판으로 빛나지
        // 않게 한다.
        //
        // 높이 편차는 **R() 을 새로 뽑아 만들지 않는다.** 이 장면은 씨드 729 하나를 전부
        // 공유하고 FarmScene 은 절벽보다 먼저 여기를 부르므로, 여기서 난수를 더 소비하면
        // 뒤에 오는 절벽·작물의 흔들림 배정이 통째로 밀린다(3차 비평 §5). 대신 링을 만들 때
        // 이미 들어간 **반지름 편차 R(.84,1.12) 를 그대로 읽어 높이로 쓴다** — 밖으로 튀어
        // 나온 꼭짓점이 위로도 솟는다. 바위의 결이 실제로 그렇다.
        Color turf=Color.Lerp(C("4c5c3b"),sky,haze*.88f);
        var lip=new Vector3[sides];
        float lipSum=0;
        for(int i=0;i<sides;i++)
        {
            Vector2 flat=new Vector2(rings[0,i].x-x,rings[0,i].z-z);
            float bulge=flat.magnitude/Mathf.Max(width*.5f,1e-3f)-1f;   // -0.16 ~ +0.12
            float lipY=top+.075f+bulge*.55f;                            // top+0.01 ~ top+0.14
            lipSum+=lipY;
            lip[i]=new Vector3(x+flat.x*1.03f,lipY,z+flat.y*1.03f);
        }
        Vector3 crown=new Vector3(x,lipSum/sides+.02f,z);
        for(int i=0;i<sides;i++)
        {
            int j=(i+1)%sides;
            Color face=turf*R(.94f,1.06f); face.a=1;
            Color side=face*.86f; side.a=1;
            // 감는 방향은 위 절벽과 같은 규칙이다(법선 = cross(v1-v0, v2-v0)).
            // 윗면은 lip[j]→lip[i]→중앙 순서라야 법선이 +Y 다 — 뒤집으면 뚜껑이 사라진다.
            Tri(v,t,colors,lip[j],lip[i],crown,face);
            Tri(v,t,colors,lip[i],lip[j],rings[0,i],side);      // 처마 옆면
            Tri(v,t,colors,lip[j],rings[0,j],rings[0,i],side);
        }
        MeshObject("Distant isle",v,t,colors);
        IsleMist(x,z,width,rise);
    }

    /// <summary>
    /// 원경 섬 하나의 밑동에 두르는 안개 자락.
    ///
    /// 시안의 원경은 **밑동이 안개에 묻힌 낮은 덩어리**다. 수평선을 가로지르는 띠
    /// (MistLayers)만으로는 그게 안 된다 — 띠는 화면 좌표에 고정되고 섬은 월드에 고정되니
    /// 깊이가 맞는 섬만 우연히 가려진다. 실제로 화면 sy +0.40 의 가장 큰 섬(눈깊이 약 29.9)은
    /// 두 띠(33.6 · 36.4)보다 **앞에** 있어 지금까지 안개를 한 겹도 못 받았다. 그게 라운드 3
    /// 캡처 왼쪽의 가장 큰 "버섯"이다.
    ///
    /// 그래서 섬마다 제 자리에 한 장 깐다. 눕힌 판이라 Q/E 로 돌려도 같은 자리에 있고,
    /// ZWrite Off 라 섬보다 앞쪽에 걸친 부분만 살아남아 밑동을 덮는다(뒤쪽은 섬이 가린다).
    /// 높이는 수면(-1.70)에서 rise 만큼 올리되 **-1.34 아래로는 내리지 않는다** — 먼바다
    /// 물마루가 최대 -1.27 까지 오므로 그보다 낮으면 판이 파도에 잠긴다. 동시에 등대 탑
    /// 밑동(-1.22)보다는 낮아 탑을 지우지 않는다.
    ///
    /// **근경 수면에는 깔지 않는다.** 라운드 1 이 그렇게 해서 앞바다가 뿌예졌다.
    /// </summary>
    void IsleMist(float x,float z,float width,float rise)
    {
        if(mistMaterial==null)mistMaterial=new Material(Shader.Find("PoC/Mist"));
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name="Isle mist";
        quad.transform.SetParent(world.transform);
        Destroy(quad.GetComponent<Collider>());                 // 밭 클릭을 가로채면 안 된다
        // Quad 는 +Z 를 보므로 X 축으로 90도 돌리면 지면에 눕는다. 셰이더가 Cull Off 라
        // 위를 보든 아래를 보든 그려진다. 로컬 X→월드 X, 로컬 Y→월드 Z 가 된다.
        quad.transform.position=new Vector3(x,Mathf.Max(-1.70f+rise*.42f+.16f,-1.34f),z);
        quad.transform.rotation=Quaternion.Euler(90,0,0);
        // 화면에서 세로는 부감 때문에 눌리므로 Z 를 X 만큼 크게 잡을 필요가 없다.
        quad.transform.localScale=new Vector3(width*2.6f,width*2.0f,1f);
        var m=new Material(mistMaterial);
        m.SetColor("_Color",C("8298a6"));   // 흰 물감이 아니라 공기. 배경 #465a6a 와 같은 계열
        m.SetFloat("_Density",.40f);
        m.SetFloat("_Scale",.17f);
        m.SetFloat("_Speed",.013f);
        m.SetFloat("_FadeU",.06f);          // 띠가 아니라 **덩어리**다 — 사방으로 둥글게 죽인다
        m.SetFloat("_FadeV",.06f);
        m.SetFloat("_Cut",.28f);
        m.SetFloat("_Soft",1.5f);           // 구멍 가장자리를 눕힌다. 작은 판이라 2.4 는 날카롭다
        quad.GetComponent<Renderer>().sharedMaterial=m;
        generatedMaterials.Add(m);
    }

    /// <summary>
    /// 바다 위의 안개. 시안에서 인상을 가장 크게 좌우하는 요소다.
    ///
    /// 길고 얇은 판을 카메라 가로축에 맞춰 눕혀 화면을 가로지르는 띠로 쓴다
    /// (Mist.shader 의 _FadeU/_FadeV 가 그 모양을 만든다).
    ///
    /// **라운드 2: 3겹 → 2겹, 둘 다 화면 위쪽에만.**
    /// 라운드 1 의 셋째 띠는 화면 sy -0.66, 즉 **근경 수면 한복판**에 두께 0.36 으로 깔려 있었다.
    /// 안개 색이 #dee8f0(거의 흰색)이라 그 띠가 앞바다를 뿌옇게 덮었다 — 바다가 흰색으로
    /// 보인 세 원인 중 하나다. 안개의 일은 **원경 섬의 밑동을 가리는 것뿐**이므로 지웠다.
    /// 남은 두 겹도 짙기를 0.30/0.20 → 0.26/0.15, 두께를 0.31/0.41 → 0.24/0.26 으로 줄이고
    /// 색을 청회색으로 낮춰(#b3c3cb / #93a8b4) 흰 물감이 아니라 공기로 읽히게 했다.
    ///
    /// **높이는 전부 섬보다 한참 낮게 유지한다.** 섬 윗면이 y≈0.9 이므로 그보다 높은 판을
    /// 깔면 안개가 밭을 덮어 게임이 보이지 않는다(한 번 그렇게 만들었다).
    /// 동시에 물마루(-1.70+0.30=-1.40)보다는 높아야 판이 물에 잠기지 않는다.
    /// </summary>
    void MistLayers()
    {
        if(mistMaterial==null)mistMaterial=new Material(Shader.Find("PoC/Mist"));
        // (화면 sy, 높이, 화면 세로 대비 두께, 짙기)
        // 원경 섬이 화면 sy +0.40 ~ +0.96 에 있다. 두 띠가 그 구간의 밑동을 지나간다.
        // _FadeV=0 이라 가운데가 가장 짙고 위아래로 풀리므로 실제로 보이는 띠는 두께의 절반쯤이다.
        //
        // **라운드 3: 높이를 -1.30/-1.22 → -1.14/-1.06 으로 올렸다.** 파고를 바꾸면서
        // 먼바다 물마루가 최대 -1.27 까지 오게 됐다(0.26 × Gust 1.66). 예전 높이로 두면
        // 띠가 파도에 잠겨 수평선에서 끊긴다. 섬 윗면(y≈0.9)보다는 여전히 한참 아래라
        // 밭을 덮지 않는다.
        //
        // 짙기는 0.26/0.15 → 0.32/0.22 로 올리고 색은 한 단 내렸다. 라운드 2 가 안개를
        // 줄인 것은 **근경 수면**을 살리기 위해서였고 그 판단은 유효하다(셋째 띠는 그대로
        // 지워 둔다). 하지만 이 둘은 화면 위쪽이라 근경과 무관하고, 시안의 원경은
        // "안개가 옅다"가 아니라 **밑동이 묻혀 있다**. 섬별 자락은 IsleMist() 가 맡는다.
        var bands=new[]{
            new Vector4( 0.88f,-1.14f,.24f,.32f),   // 수평선 띠 — 원경 섬 밑동을 가린다
            new Vector4( 0.62f,-1.06f,.26f,.22f),   // 그 아래 한 겹. 섬 뒤를 지나간다
        };
        // 흰색이 아니라 청회색이다. 후처리 안개·배경(#465a6a)과 계열을 맞춘다.
        // #b3c3cb 는 휘도 0.75 라 그 자체가 흰 물감이었다. 0.64/0.57 로 내린다.
        var tint=new[]{ C("9cafba"), C("8397a4") };
        for(int i=0;i<bands.Length;i++)
        {
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name="Sea mist band "+i;
            quad.transform.SetParent(world.transform);
            Destroy(quad.GetComponent<Collider>());                  // 밭 클릭을 가로채면 안 된다
            var band=quad.AddComponent<MistBand>();
            band.screenY=bands[i].x; band.height=bands[i].y; band.screenThickness=bands[i].z;
            band.Apply();                                            // 첫 프레임에 원점에서 튀지 않게
            var material=new Material(mistMaterial);
            material.SetColor("_Color",tint[i]);
            material.SetFloat("_Density",bands[i].w);
            material.SetFloat("_Scale",.090f+i*.020f);
            material.SetFloat("_Speed",.020f+i*.010f);
            material.SetFloat("_FadeU",.80f);                        // 긴 축은 양 끝에서만 죽인다
            material.SetFloat("_FadeV",.00f);                        // 짧은 축은 중앙에서부터 풀린다
            material.SetFloat("_Cut",.34f+i*.06f);                   // 문턱을 올려 구멍을 더 낸다
            material.SetFloat("_Soft",2.0f);                         // 구멍 가장자리. 2.4 는 칼 같다
            quad.GetComponent<Renderer>().sharedMaterial=material;
            generatedMaterials.Add(material);
        }
    }
}

/// <summary>
/// 안개 띠를 카메라에 붙들어 둔다.
///
/// 띠는 화면 가로로 누워야 "수평 안개"로 읽힌다. 그런데 월드 좌표에 고정해 두면
/// Q/E 로 카메라를 돌리는 순간 띠가 비스듬히 잘리고, 구역 C 가 직교 크기를 줄이면
/// 화면에서의 두께와 높이가 전부 어긋난다.
/// 매 프레임 카메라의 지면 방향·크기·화면비를 다시 읽어 **같은 화면 자리에 같은 두께로** 둔다.
/// (카메라 위치는 LowpolyGame.Update() 가 정하므로 LateUpdate 에서 따라간다.)
///
/// 바위·원경 섬에는 이 방식을 쓰지 않는다. 그것들은 월드에 고정되어야 시차가 맞는다 —
/// 화면에 붙이면 카메라를 돌릴 때 바다가 통째로 따라 도는 것처럼 보인다.
/// </summary>
public class MistBand:MonoBehaviour
{
    public float screenY, height, screenThickness;

    void LateUpdate(){ Apply(); }

    public void Apply()
    {
        var c=Camera.main; if(c==null)return;
        Vector3 forward=c.transform.forward;
        float drop=-forward.y; if(drop<.05f)return;             // 카메라가 수평이면 수면과 안 만난다
        float size=c.orthographic?c.orthographicSize:8.2f;
        float aspect=c.aspect>.1f?c.aspect:16f/9f;

        // 화면 (0, screenY) 광선이 y=height 평면과 만나는 점.
        Vector3 p=c.transform.position+c.transform.up*(screenY*size);
        Vector3 at=p+forward*((p.y-height)/drop);
        transform.position=new Vector3(at.x,height,at.z);
        transform.rotation=Quaternion.Euler(90,c.transform.eulerAngles.y,0);

        // 지면에 눕힌 판의 세로 1유닛이 화면 세로로 얼마인지. 카메라 부감각에 따라 달라진다.
        Vector3 ground=new Vector3(forward.x,0,forward.z);
        if(ground.sqrMagnitude<1e-4f)return;
        float perUnit=Vector3.Dot(ground.normalized,c.transform.up)/size;
        if(perUnit<1e-4f)return;
        // 가로는 화면 폭의 2배. _FadeU=0.80 이라 짙은 구간만 화면 폭을 덮는다.
        transform.localScale=new Vector3(4f*size*aspect,screenThickness/perUnit,1f);
    }
}
