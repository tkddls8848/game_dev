using System;
using UnityEngine;

// 구역 C — 광원 · 색감 · 머티리얼. **시안의 무게가 여기서 나온다.**
//
// LowpolyGame.cs 의 Start() 안에 카메라·조명이 섞여 있어서 연출만 손보려 해도
// 게임 루프 파일을 건드려야 했다. 떼어 낸다.
//
// 주의: Mat() 이 여기 있다. 이 함수가 만드는 머티리얼이 화면 대부분을 칠한다 —
// 잔디 윗면 · 밭 · 고랑 · 집 벽 · 울타리 · 작물이 전부 Cube() 를 거쳐 여기로 온다.
// 그래서 셰이더를 바꾸려면 여기다.
public sealed partial class LowpolyGame
{
    /// <summary>카메라 · 광원 · 환경광 · 품질. Start() 가 가장 먼저 부른다.</summary>
    void SetupStage()
    {
        cam=new GameObject("Camera").AddComponent<Camera>(); cam.tag="MainCamera";cam.gameObject.AddComponent<AudioListener>();cam.gameObject.AddComponent<DioramaAtmosphere>();
        // **앞 판의 "줌인" 판단을 뒤집는다.** 5.8 은 "섬이 화면 폭의 55%"라는 한 줄만 보고
        // 정한 값이었고, 그 화면을 본 사용자는 "오브젝트들이 빽빽하고 여유가 없어 보인다,
        // 카메라도 좀 넓게 잡아 달라"고 했다(2026-09-28). 기준을 **섬 둘레의 바다 여백**으로
        // 바꿔 다시 잡는다.
        //
        // round1-compare.png(1656x944, 직교 5.8)에서 본섬 실루엣을 픽셀로 재면
        // 가로 18.0~71.5% · 아래끝 83.8% 다. 직교 크기 s 에서는 화면 중앙을 축으로
        // 5.8/s 배로 줄어드므로 여백이 이렇게 움직인다:
        //
        //   직교   섬폭    좌여백  우여백  아래여백  위여백
        //   5.8   53.5%   18.0%   28.5%   16.2%     9.0%   ← 현재. 좌·하가 20% 미달
        //   6.6   47.0%   21.9%   31.1%   20.3%    14.0%   ← 좌·우·하가 처음 20% 를 넘는 점
        //   7.8   39.8%   26.2%   34.0%   24.9%    19.5%   ← 채택
        //   8.6   36.1%   28.4%   35.5%   27.2%    22.3%
        //
        // 7.8 을 고른 이유는 **요구된 여백을 넘기는 값 중 시안의 섬 크기에서 가장 덜 멀기
        // 때문**이다. 승인 시안의 본섬은 화면 폭의 62.6%(x 208~1254 / 1672)를 차지한다 —
        // 즉 시안은 지금보다도 꽉 찬 구도이고, 시안의 여유는 섬이 작아서가 아니라
        // **바다가 통으로 비어 있어서** 생긴다. 넓힐수록 섬은 시안에서 멀어지므로 하한을 쓴다.
        //
        // HUD 겹침 확인(Hud.cs 는 세로 900 가상 해상도, 1656x944 에서 가로 1579):
        //   우측 침식 예보 패널 x = 1579-336 = 1243 → 78.7%. 섬 우측끝 62.8% — 안 겹친다
        //   하단 행동 버튼 y = 776/900 → 86.2%. 섬 아랫끝 75.1% — 11% 여유
        // ART_TARGET §1(섬 중심 세로 45% · 하단 25% · 상단 15%)도 이 값에서 충족한다:
        //   중심 47.3% · 하단 24.9% · 상단 19.5%. 그래서 LowpolyGame.cs:74 의 target 은 그대로 둔다.
        cam.orthographic=true; cam.orthographicSize=farm?7.8f:7.1f; cam.clearFlags=CameraClearFlags.SolidColor;
        // 배경은 안개와 같은 색이어야 수평선에서 이어진다. 예전 #788b9a 는 휘도 0.53 —
        // 시안의 가장 먼 수면(#2d3842, 휘도 0.215)보다 두 배 이상 밝은 회백색이었다.
        // AirPerspective 의 안개 색과 같은 #465a6a 로 내린다.
        cam.backgroundColor=farm?new Color(.275f,.349f,.416f):new Color(.60f,.69f,.72f);
        cam.nearClipPlane=.1f; cam.farClipPlane=100;
        // 후처리가 공기 원근을 그리려면 깊이 버퍼가 필요하다. 이게 없으면 안개가 전부 균일하게 걸린다.
        cam.depthTextureMode|=DepthTextureMode.Depth;

        // **광원·색보정 변경은 farm 분기 안에만 둔다.** 이 셸은 puzzle-tomorrow-map 과
        // 공유라서 밖에 두면 그 PoC의 연출까지 말없이 바뀐다.
        // 같은 이유로 Atmosphere/Faceted 의 farm 수치는 셰이더 Properties 가 아니라
        // **전역 변수**로 넣는다. 넣지 않은 장면은 셰이더 안의 이전 판 상수를 그대로 쓴다.
        if(farm)
        {
            FarmLighting();
            cam.gameObject.AddComponent<AirPerspective>();
            // (알베도 채도 배수, 림 세기 배수, 차가운 tint 배수, 따뜻한 tint 배수).
            // 이 값을 넣지 않은 장면(puzzle-tomorrow-map)은 넷 다 1 로 읽는다.
            //
            // **라운드 3 화면을 재고 나서 다시 잡은 값이다.** 절벽 그늘면 #3c3d3f(채도 0.046) ·
            // 햇빛면 #74604c(채도 0.347)에 대해 시안의 절벽은 어느 면이든 채도 0.35~0.50 ·
            // 색상 24~43도다. 즉 **밝기는 맞았고 채도만 무너졌다**(그늘면 휘도 0.238 대 시안 0.256).
            //
            //   x 1.0 → 1.0   알베도 채도는 **그대로 둔다.** 모형으로 올려 봐도 그늘면 채도가
            //                 0.30 → 0.33 밖에 안 움직이는 반면 햇빛면은 0.52 → 0.54 로 같이
            //                 올라 시안 상한(0.50)을 넘는다. 여기 1.0 은 "0 이 아니다"는 표시를
            //                 겸한다 — 이 값이 0 이면 아래 셋도 전부 1 로 읽힌다.
            //   y .70 → .22   림. 화면 역산에서 면마다 파랑이 +0.045 더해져 있다(Faceted 참고).
            //                 **더하기라서 어두운 그늘면에서만 색을 뒤집는다.**
            //   z      → .45  차가운 tint. 그늘면이 식는 세 경로 중 하나.
            //   w      → 1.50 따뜻한 tint. 윗면은 반대로 시안보다 덜 따뜻했다(잔디 58.5도 대 53.4도).
            Shader.SetGlobalVector("_FacetTune",new Vector4(1.0f,.22f,.45f,1.50f));
        }
        else MapLighting();

        // **그림자 품질은 캐스케이드까지 지정해야 한다.** shadowCascades 를 두지 않으면
        // 빌드가 어느 품질 단계로 뜨느냐에 따라 1~4로 갈려 재현이 안 된다(프로젝트의
        // Medium은 1, Very High는 2, Ultra는 4다).
        // 거리 48 은 섬(눈깊이 21.8~31.0)과 원경(등대 34.9 · 수평선 섬 37.4)을 덮고 먼
        // 바다는 버리는 값이다. 직교 7.8 에서 화면에 들어오는 수면이 39.4 까지이므로
        // 직교 크기를 휠 상한(11)까지 밀어도 43.9 로 48 안에 남는다.
        QualitySettings.shadows=ShadowQuality.All;
        QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
        QualitySettings.shadowProjection=ShadowProjection.StableFit;   // Q/E 회전에 그림자가 떨리지 않게
        QualitySettings.shadowCascades=4;
        QualitySettings.shadowDistance=48;
        QualitySettings.shadowNearPlaneOffset=1;
        QualitySettings.antiAliasing=4; QualitySettings.pixelLightCount=4;
        // 빌트인 안개가 켜져 있으면 후처리 공기 원근과 이중으로 걸려 화면이 씻긴다.
        RenderSettings.fog=false;
        Application.targetFrameRate=60;
    }

    // farm 전용 광원. **그림자는 원래 켜져 있었다** — 없던 것이 아니라 카메라 뒤로 숨어 있었다.
    //
    // 이전 태양 Euler(38,-42,0) 은 빛이 오는 방향이 (0.527,0.616,-0.585) 였는데 카메라가
    // 앉은 방향이 (0.413,0.579,-0.703) 이다. 사잇각 9.6도 — 태양이 카메라 어깨에 붙어 있었다.
    // 그래서 (1) 그림자가 화면 좌상, 즉 물체 뒤쪽으로만 드리워 거의 안 보이고
    // (2) 윗면 N·L 0.616 / X+ 측면 0.527 / Z- 측면 0.586 으로 **세 면이 똑같이 밝았다**.
    // 고칠 것은 그림자 스위치가 아니라 각도다.
    //
    // 방위각은 승인 시안에서 측정해 정했다. 좌하단 분리 섬은 같은 암석 재질의 두 면이
    // 모두 보이는데, 화면 우하향 면 #8e8146(휘도 0.500) · 좌하향 면 #585535(휘도 0.327)로
    // **우하향 면이 밝다.** 즉 태양은 화면 오른쪽 위에 있다. 집 박공벽·굴뚝의 우측면이
    // 밝고 좌측 긴 벽이 어두운 것도 같은 결론이다.
    //
    // Euler(44,-135,0) → 빛이 오는 방향 (0.509, 0.695, 0.509), 카메라와 75도 벌어진다.
    //   윗면 (0,1,0)   N·L = 0.695
    //   X+ 측면 (1,0,0) N·L = 0.509  ← 화면 우하향. 시안에서 밝은 쪽
    //   Z- 측면 (0,0,-1) N·L = 0     ← 화면 좌하향. 차가운 그림자
    // 고도 44도는 윗면/측면 비를 1.37로 눌러 시안의 완만한 단차에 맞춘 값이다(55도면 2.02가
    // 되어 측면이 과하게 죽는다). 드리운 그림자는 화면 왼쪽으로 물체 높이의 1.03배.
    void FarmLighting()
    {
        var light=new GameObject("Raking sun").AddComponent<Light>(); light.type=LightType.Directional;
        light.transform.rotation=Quaternion.Euler(44,-135,0);
        light.intensity=1.08f; light.color=new Color(1f,.914f,.769f);   // #ffe9c4 — ART_TARGET §5
        light.shadows=LightShadows.Soft; light.shadowStrength=.85f; light.shadowBias=.020f; light.shadowNormalBias=.15f; light.shadowNearPlane=.2f;
        light.gameObject.AddComponent<RakingSun>().baseRotation=light.transform.rotation;

        // 바다에서 올라오는 반사광. **태양이 버린 면을 정확히 맡는다** —
        // 빛이 오는 방향 (-0.700, 0.139, -0.700) 이라 Z- 측면 N·L=0.700, X+ 측면 0, 윗면 0.139.
        // 그늘진 절벽만 차갑게 띄우고 햇빛 받는 면과 잔디 윗면은 건드리지 않는다.
        //
        // **색을 #99b6d6 에서 #babcc7 로 옮긴다.** 이 빛은 그늘면이 받는 빛의 35% 인데
        // B/R 이 1.40 이었다. 나머지 65% 인 equator 환경광(1.257)과 합쳐 그늘면의 입사광
        // B/R 이 1.307 이 되고, 거기에 알베도 cool tint(1.096)와 후처리 그림자 토닝(1.199)이
        // 곱해져 갈색 알베도(B/R 0.554)가 0.876 까지 밀린다 — 세 곱을 합치면 1.72 배다.
        //
        // **"차가운 그림자"의 기준은 절대값이 아니라 햇빛 면과의 비다.** 시안에서 같은 칸의
        // 두 면을 재면 그늘면 #49412d(B/R 0.617) · 햇빛면 #795f3d(B/R 0.505)로 **비가 1.22**다.
        // 라운드 3 빌드는 0.653 대 1.048 — 비가 1.61 이다. 여기와 equator 를 같이 데워
        // 입사광 기준 1.21 로 맞춘다. 그림자는 여전히 파랗고, 갈색은 살아남는다.
        // 세기는 .42 → .41 로만 줄인다. 그늘면의 휘도는 유지해야 한다(시안 0.256 · 빌드 0.238).
        var fill=new GameObject("Sea bounce").AddComponent<Light>(); fill.type=LightType.Directional;
        fill.transform.rotation=Quaternion.Euler(8,45,0);
        fill.intensity=.41f; fill.color=new Color(.73f,.74f,.78f); fill.shadows=LightShadows.None;
        fill.gameObject.AddComponent<RakingSun>().baseRotation=fill.transform.rotation;

        // **환경광은 태양의 색을 죽이지 않을 만큼만 넣는다.**
        // 이전 값은 하늘빛이 (.55,.62,.70) 으로 너무 셌다. 따뜻한 태양에 그만한 청회색을
        // 더하면 윗면 합이 거의 무채색이 되어 "따뜻한 윗면"이 성립하지 않는다.
        // 그래서 **윗면용 sky 는 따뜻하게 낮게, 측면용 equator 는 차갑게 조금 높게** 둔다.
        // 물리적으로는 뒤집힌 배치지만, 윗면은 태양이 다 먹고 측면은 채울 것이 없기 때문이다.
        //
        // 결과(알베도 곱하기 전 합산 광량):
        //   윗면     (1.105, 1.014, 0.875)  휘도 1.023  B/R 0.79  ← 따뜻 (#ffe9c4 비율 1:0.91:0.77)
        //   X+ 측면  (0.876, 0.862, 0.833)  휘도 0.863  B/R 0.95
        //   Z- 측면  (0.504, 0.566, 0.659)  휘도 0.560  B/R 1.31  ← 차가움 (#6b7a8a 비율 1:1.14:1.29)
        //   드리운 그림자(윗면)            휘도 0.435
        // 측면 밝음:어두움 = 1.63 : 1 (시안 측정 1.53). 윗면:밝은측면 = 1.40.
        // 잔디 윗면:드리운 그림자 = 2.59 — 집·나무 그림자가 확실히 읽힌다.
        //
        // **알베도를 화면 색으로 바꾸는 이득**(후처리까지 포함, 구역 B가 팔레트를 고를 때 쓸 것):
        //   윗면 1.12 · 햇빛 받는 측면 0.80 · 그늘진 측면 0.49.
        //   즉 절벽 측면을 #8a6b4a 로 보이게 하려면 알베도는 그보다 밝아야 한다.
        //
        // ── 라운드 3 측정 후 다시 잡은 세 색 ──────────────────────────────────
        //
        // **sky(윗면) 는 내린다.** 잔디 윗면이 시안보다 밝고 노랗다는 지적(3차 비평 7번)을
        // 재면 빌드 #9f9d56(휘도 0.598 · 색상 58.5도), 시안 #928a48(0.528 · 53.4도)이다.
        // 즉 색상은 5도 차이인데 **휘도가 13% 높다** — 같은 노랑도 밝으면 형광으로 읽힌다.
        // sky 휘도를 .291 → .205(-30%)로 내리면 윗면 입사광이 1.023 → 0.939(-8.2%)가 된다.
        // 측면은 equator 를 받으므로 거의 그대로다.
        // 태양 세기(1.08)와 각도(44,-135)는 건드리지 않는다 — 명암 갈림의 기준이다.
        //
        // **equator(측면) 는 휘도는 두고 색만 데운다.** .3567 → .3505(-1.7%)이지만
        // B/R 은 1.257 → 1.085 다. 그늘면 입사광의 65%를 equator 가 내므로
        // 그늘면 탈색의 가장 큰 단일 항목이 이것이다.
        //
        // **ground(아랫면·처마 밑) 도 같은 비율로 데운다.** B/R 1.304 → 1.153.
        //
        // 바뀐 입사광(알베도 곱하기 전):
        //   윗면     (1.018, 0.930, 0.791)  휘도 0.939  B/R 0.78  ← 이전 1.023 / 0.79
        //   X+ 측면  (0.892, 0.853, 0.794)  휘도 0.857  B/R 0.89  ← 이전 0.863 / 0.95
        //   Z- 측면  (0.552, 0.563, 0.595)  휘도 0.563  B/R 1.08  ← 이전 0.559 / **1.31**
        // 측면 밝음:어두움 = 1.52 : 1 (이전 1.54, 시안 측정 1.51~1.53) — 명암 갈림은 그대로다.
        // 그늘면:햇빛면 B/R 비 = 1.21 (이전 1.38, 시안 1.22). **식은 정도만 시안에 맞췄다.**
        //
        // **sky 는 Faceted 밖으로도 나간다.** 수면(PoC/Water 도 Standard 라 SH 를 받는다)의
        // 윗면 확산광이 8% 안팎 어두워진다. 구역 A 의 흰 띠·포말 수치는 이 변경 뒤에 다시 재야 한다.
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.226f,.202f,.169f);
        RenderSettings.ambientEquatorColor=new Color(.342f,.351f,.371f);
        RenderSettings.ambientGroundColor=new Color(.196f,.207f,.226f);
    }

    // 이전 값 그대로. 이 PoC는 별도 판단 대상이라 farm 의 광원 변경을 옮기지 않는다.
    void MapLighting()
    {
        var light=new GameObject("Afternoon sun").AddComponent<Light>(); light.type=LightType.Directional; light.transform.rotation=Quaternion.Euler(38,-42,0); light.intensity=1.32f; light.color=new Color(1f,.90f,.76f); light.shadowStrength=.72f; light.shadowBias=.028f; light.shadowNormalBias=.12f; light.shadows=LightShadows.Soft;
        var bounce=new GameObject("Sea bounce").AddComponent<Light>(); bounce.type=LightType.Directional; bounce.transform.rotation=Quaternion.Euler(-24,145,0); bounce.intensity=.30f; bounce.color=new Color(.62f,.76f,.86f); bounce.shadows=LightShadows.None;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.55f,.62f,.70f);
        RenderSettings.ambientEquatorColor=new Color(.40f,.46f,.50f);
        RenderSettings.ambientGroundColor=new Color(.22f,.26f,.30f);
    }

    Shader facetShader;
    readonly System.Collections.Generic.Dictionary<Color,Material> mats=new System.Collections.Generic.Dictionary<Color,Material>();
    // **Standard 가 아니라 PoC/Faceted 를 쓴다.** farm 장면의 Cube()/Box() 호출은 30군데가
    // 넘고(대부분 반복문 안이다) MeshObject() 는 다섯 군데뿐이라, 잔디 윗면·집·작물·울타리가
    // 전부 Standard 로 칠해지고 있었다. 그래서 Faceted 의 결·색온도 분리·림이 절벽과 바위에만
    // 걸렸다. 다만 이것만으로 납작함이 풀리지는 않는다 — 태양 각도가 주원인이고 이건 두 번째다.
    //
    // CreatePrimitive 큐브에는 COLOR 정점 스트림이 없으므로 _UseVertexColor=0 으로 끈다
    // (켜 두면 정의되지 않은 정점 색이 곱해져 화면이 검어진다).
    Material Mat(Color c)
    {
        if(mats.TryGetValue(c,out var existing))return existing;
        if(facetShader==null)facetShader=Shader.Find("PoC/Faceted");
        var m=new Material(facetShader); m.color=c; m.SetFloat("_UseVertexColor",0);
        mats[c]=m; return m;
    }
}

/// <summary>
/// 공기 원근과 색보정 수치를 **살아 있는 카메라에서 매 프레임 다시 재어** 넣는다.
///
/// 안개 구간을 셰이더에 상수로 박으면 맞을 수가 없다. 이유가 둘이다:
///   1. 직교 크기가 런타임에 바뀐다 — LowpolyGame.Update() 의 마우스 휠이 6.6~11 사이로 민다
///   2. 직교 카메라에서 보이는 깊이 구간은 아주 좁다 — 크기 7.8 에서 17.4~39.4 뿐이다
/// 실제로 한 번 틀렸다: 40/88 로 박아 둔 판은 구간을 통째로 벗어나 **안개 계수가 그냥 0**
/// 이었고(효과가 꺼진 게 아니라 범위가 틀렸다), 그다음 판의 30.5/36.5 는 직교 5.8 전용이라
/// 크기를 넓히는 순간 다시 어긋난다. 그래서 수치를 코드에서 만든다.
///
/// 시작점은 **가장 먼 땅보다 뒤**다. 화면 좌표가 아니라 섬을 감싸는 월드 상자의
/// 먼 꼭짓점에서 재기 때문에 직교 크기·Q/E 회전·카메라 목표점이 뭐로 바뀌어도
/// 근경은 항상 맑게 남는다. 끝점은 화면 맨 위 광선이 수면에 닿는 곳이다.
///
/// **안개로 분위기를 만들지 않는다.** 시안의 부드러움은 여백과 색에서 오고, 근경에 안개를
/// 얹으면 그냥 우유빛이 된다(실측: 휘도 0.52 초과 저채도 픽셀이 시안 2.70% 인데 빌드가 6.90%).
/// </summary>
public class AirPerspective:MonoBehaviour
{
    const float SeaY=-1.70f;
    // 본섬이 들어가는 월드 상자. 안개 시작점을 **화면 좌표가 아니라 이 상자에서** 잡기 때문에
    // 직교 크기를 휠로 밀어도, Q/E 로 돌려도 근경이 안개에 물들지 않는다.
    //   x: 밭 ±4.41(칸 6개) + 밑동 벌어짐 0.40
    //   z: 앞줄 -2.94-0.40 ~ 집이 있는 뒤쪽 단 4.415+0.40
    //   y: 밑동 바닥 -2.05 ~ 위험 칸 점선 0.95
    // **구역 B가 실루엣을 늘리면 이 상자도 같이 늘려야 한다.** 좁으면 섬 뒷단이 안개에 묻힌다.
    static readonly Vector3 LandMin=new Vector3(-4.81f,-2.05f,-3.34f);
    static readonly Vector3 LandMax=new Vector3( 4.81f,  .95f, 4.82f);

    void LateUpdate()
    {
        var view=Camera.main; if(view==null||!view.orthographic)return;
        Vector3 eye=view.transform.position,forward=view.transform.forward,up=view.transform.up;
        float drop=Mathf.Max(-forward.y,.05f);           // 카메라가 수평이면 수면과 안 만난다
        // 화면 맨 위(sy=+1) 광선이 수면에 닿는 눈깊이. 직교라 광선이 평행해서 한 줄로 끝난다.
        // dot(up,forward)=0 이므로 위로 올린 몫은 깊이에 더해지지 않는다.
        float horizon=(eye.y+up.y*view.orthographicSize-SeaY)/drop;
        // 상자에서 **가장 먼 꼭짓점**. 축마다 부호만 보면 되므로 여덟 개를 다 재지 않아도 된다.
        // Q/E 로 돌면 forward 가 바뀌어 먼 꼭짓점도 바뀌므로 매 프레임 다시 고른다.
        Vector3 far=new Vector3(forward.x<0?LandMin.x:LandMax.x,
                                forward.y<0?LandMin.y:LandMax.y,
                                forward.z<0?LandMin.z:LandMax.z);
        float land=Vector3.Dot(far-eye,forward);         // 직교 7.8 · yaw 0 에서 31.7
        float start=land+1.0f;                           // 섬 뒤 1 유닛부터
        float end=Mathf.Max(start+2.5f,horizon);         // 뒤집히지 않게 최소 폭을 보장한다

        // (시작, 끝, 최대 농도, -). 농도 0.55 → 0.30: 시안은 안개가 원경에만 얇게 있다.
        Shader.SetGlobalVector("_AirFog",new Vector4(start,end,.30f,0));
        // 안개 색. 예전 #788b9a 는 휘도 0.53 으로 시안의 먼 수면(#2d3842, 0.215)보다
        // 두 배 이상 밝았다 — 그걸 55% 섞었으니 바다가 통째로 떠올랐다.
        // #465a6a(휘도 0.334)는 시안 상단 수면 평균(#4d5f70, 0.363)보다 어둡고 더 청록이다.
        Shader.SetGlobalColor("_AirFogColor",new Color(.275f,.349f,.416f));
        // 색보정. **눈대중이 아니라 시안의 휘도 분포에 맞춰 풀었다.** round1-compare.png 에서
        // 이전 판 색보정을 역산해(고정점 반복, 평균 오차 0.0008) 보정 전 화면을 복원한 뒤,
        // 그 위에 후보 곡선을 씌워 시안과 같은 통계가 나오는 값을 골랐다:
        //
        //             p5     p50    p95    p99   평균   채도  휘도>0.70
        //   시안     0.212  0.343  0.562  0.655  0.359  0.340   0.69%
        //   빌드 r1  0.196  0.362  0.753  0.972  0.410  0.307   7.25%
        //   이 값    0.190  0.373  0.660  0.700  0.403  0.336   0.95%   ← 예측
        //
        // 읽을 것 둘:
        //   * **시안은 대비가 센 화면이 아니다.** 흰색이 아예 없다(p99 0.655). 그래서
        //     중간톤을 세우는 대신 숄더로 밝은 쪽을 눕혔다 — 휘도 0.70 초과가 7.25% → 0.95%.
        //   * **평균 휘도는 여기서 못 맞춘다.** 어떤 곡선을 넣어도 0.40 아래로 안 내려간다.
        //     밝은 픽셀의 **개수**가 문제이기 때문이다(구역 A 의 바위 밑동 포말이 반지름
        //     2유닛 흰 원판으로 깔려 근경 바다를 덮는다). 곡선은 흰색을 어둡게 할 수 있어도
        //     흰 면적을 줄이지는 못한다.
        //
        // 무제약으로 풀면 숄더 시작 0.22 · 채도 1.57 이 최적이지만(통계 오차 0.0004)
        // 그건 화면 전체를 눌러 놓고 채도로 되살리는 곡선이라 갈색이 올리브로 간다.
        // 쓸 수 있는 범위(숄더 시작 ≥ 0.45, 채도 ≤ 1.25)로 제한해 고른 값이다.
        //
        // **숄더(0.56)와 천장(0.74)은 건드리지 않는다.** 3차 비평은 절벽 탈색의 용의자로
        // 숄더를 지목했지만 화면을 재면 아니다: 대비 적용 직후 값이 그늘면 0.31 · 햇빛면 0.51 로
        // **두 면 모두 knee 아래**라 숄더를 통과하지 않는다. 숄더가 실제로 무는 곳은 잔디
        // 윗면(0.62)과 포말뿐이고, 포말은 구역 A 가 고치는 중이라 지금 흔들면 안 된다.
        // 안개도 아니다 — 시작점이 섬 뒤(눈깊이 약 32.7)라 본섬 절벽(21.8~31.0)은 계수가 0 이다.
        Shader.SetGlobalVector("_AirGrade",new Vector4(1.12f,.008f,.56f,1.22f));
        // (그림자 토닝 세기, 하이라이트 토닝 세기, -, -). 1,1 이 이전 판이다.
        // 그림자 토닝은 어두운 픽셀의 B/R 을 1.199 배 올린다. 그늘진 절벽은 광원(1.31)과
        // 알베도 cool tint(1.10)에서 이미 두 번 식은 뒤라 여기서 세 번째로 식으면 색이 죽는다.
        // 0.42 로 줄이면 이 단계의 배수는 1.079 가 된다. 하이라이트 쪽은 1.06 으로 조금 키운다 —
        // 시안의 흙(#a27e4d, 색상 34.7도)이 빌드(#968d50, 52.1도)보다 훨씬 주황이다.
        Shader.SetGlobalVector("_AirTone",new Vector4(.42f,1.06f,0,0));
        // (비네팅 세기, 비네팅 시작 반경, 그레인, 하이라이트 천장).
        // 비네팅 0.32/0.26 → 0.16/0.38. 넓어진 화면의 모서리는 전부 바다라, 맑게 만든 바다를
        // 비네팅으로 다시 누르면 가운데만 밝은 무거운 화면이 된다. 그레인 0.018 → 0.011.
        Shader.SetGlobalVector("_AirFrame",new Vector4(.16f,.38f,.011f,.74f));
    }
}

// 카메라는 Q/E 로 돈다. 광원을 월드에 고정해 두면 어떤 각도에서는 태양이 다시
// 카메라 축과 겹쳐 그림자가 물체 뒤로 숨고 화면이 납작해진다 — 이전 판이 정확히
// 그 상태였다. 디오라마는 광원을 카메라와 함께 돌려 **화면 기준 각도**를 지킨다.
public class RakingSun:MonoBehaviour
{
    public Quaternion baseRotation;
    float baseYaw; bool locked;
    // 첫 LateUpdate 에서 기준 각을 잡는다. SetupStage 시점에는 LowpolyGame.Update 의
    // LookAt 이 아직 돌지 않아 카메라 회전이 단위 회전이다(그때 읽으면 30도가 어긋난다).
    void LateUpdate()
    {
        var view=Camera.main; if(view==null)return;
        float yaw=view.transform.eulerAngles.y;
        if(!locked){baseYaw=yaw;locked=true;}
        transform.rotation=Quaternion.Euler(0,Mathf.DeltaAngle(baseYaw,yaw),0)*baseRotation;
    }
}
