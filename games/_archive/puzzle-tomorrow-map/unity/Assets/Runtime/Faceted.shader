// 저폴리 면 재질. **화면의 거의 전부가 이 셰이더로 칠해진다** — 절벽·바위 같은
// 생성 메시(정점 색 있음)와 Cube()/Box() 로 만든 잔디·집·작물(정점 색 없음)이 모두 온다.
//
// 이전 판은 Lambert + 버텍스 컬러뿐이라 한 면이 완전히 균일했다. 그래서 절벽도 잔디도
// 색종이처럼 보였다. 시안은 같은 저폴리인데도 면 안에서 밝기가 흔들리고, 실루엣 가장자리가
// 하늘빛으로 살짝 밝다. 그 둘이 "3D처럼 보임"의 정체다.
//
// 여섯을 한다:
//   1. 높이 그라데이션 — 위쪽 면이 밝다. 하늘빛을 받는 쪽이다
//   2. 색온도 분리      — 윗면은 따뜻하게, 측면·아랫면은 차갑게. **시안의 무게가 여기서 온다**
//   3. 알베도 채도      — 빛이 1보다 크면 색이 씻긴다. 여기서 되돌린다
//   4. 결 노이즈        — 월드 좌표 기반의 아주 옅은 얼룩. 흙·바위의 불균질함
//   5. 림 라이트        — 실루엣 가장자리를 하늘색으로. 물체가 배경에서 떨어져 나온다
//   6. 물리 기반 조명   — Lambert 대신 Standard. 빛의 방향이 실제로 읽힌다
Shader "PoC/Faceted"
{
    Properties
    {
        _Color          ("Tint", Color) = (1,1,1,1)
        _MainTex        ("팔레트", 2D) = "white" {}
        // **정점 색은 선택 사항이다.** CreatePrimitive 큐브에는 COLOR 스트림이 없어서
        // 그대로 곱하면 정의되지 않은 값(D3D 에서는 0)이 들어와 화면이 검어진다.
        // 생성 메시용 머티리얼은 기본값 1 을 그대로 쓰고, Mat() 이 만드는 큐브용은 0 으로 끈다.
        _UseVertexColor ("정점 색 사용", Range(0,1)) = 1
        _Saturation     ("알베도 채도", Range(0.5, 2)) = 1.04
        _TempSplit      ("윗면-측면 색온도 분리", Range(0, 1)) = 0.55
        _RimColor       ("림 라이트", Color) = (0.66, 0.78, 0.90, 1)
        _RimPower       ("림 폭", Range(0.5, 8)) = 3.0
        _RimStrength    ("림 세기", Range(0, 1)) = 0.08
        _Grain          ("결 세기", Range(0, 0.3)) = 0.095
        _HeightTint     ("높이 그라데이션", Range(0, 0.4)) = 0.10
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        struct Input { float2 uv_MainTex; float4 color : COLOR; float3 worldPos; float3 worldNormal; float3 viewDir; };

        sampler2D _MainTex;
        fixed4 _Color, _RimColor;
        float _RimPower, _RimStrength, _Grain, _HeightTint, _UseVertexColor, _Saturation, _TempSplit;

        // **이 셰이더도 puzzle-tomorrow-map 과 공유다.** Properties 기본값을 고치면 그쪽
        // 연출이 말없이 따라 바뀐다. 그래서 farm 전용 보정은 Properties 가 아니라 전역으로
        // 받는다 — Staging.cs 의 SetupStage 가 farm 분기에서만 넣는다.
        //   x = 알베도 채도 배수   y = 림 라이트 세기 배수
        //   z = **차가운 tint 배수**  w = **따뜻한 tint 배수**
        // 아무도 넣지 않으면 (0,0,0,0) 이라 넷 다 1 로 읽혀 이전 판과 같은 화면이 된다.
        //
        // z/w 를 갈라 둔 이유: 3차 비평의 "절벽이 회색으로 탈색됐다"를 재면 **측면만** 무채색이고
        // 윗면은 오히려 노랗다. 즉 고쳐야 할 것은 cool 쪽이고 warm 쪽은 오히려 더 필요하다.
        // _TempSplit 하나로 묶여 있으면 한쪽을 고칠 때 반대쪽이 같이 끌려간다.
        float4 _FacetTune;

        float hash(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }

        float noise(float3 p)
        {
            float3 i = floor(p), f = frac(p);
            f = f * f * (3.0 - 2.0 * f);
            float n000 = hash(i), n100 = hash(i + float3(1,0,0));
            float n010 = hash(i + float3(0,1,0)), n110 = hash(i + float3(1,1,0));
            float n001 = hash(i + float3(0,0,1)), n101 = hash(i + float3(1,0,1));
            float n011 = hash(i + float3(0,1,1)), n111 = hash(i + float3(1,1,1));
            return lerp(lerp(lerp(n000,n100,f.x), lerp(n010,n110,f.x), f.y),
                        lerp(lerp(n001,n101,f.x), lerp(n011,n111,f.x), f.y), f.z);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 vertexTint = lerp(float3(1,1,1), IN.color.rgb, _UseVertexColor);
            float3 albedo = vertexTint * _Color.rgb * tex2D(_MainTex, IN.uv_MainTex).rgb;

            float tuned    = step(0.001, _FacetTune.x);
            float satBoost = lerp(1.0, _FacetTune.x, tuned);
            float rimScale = lerp(1.0, _FacetTune.y, tuned);
            float coolAmt  = _TempSplit * lerp(1.0, _FacetTune.z, tuned);
            float warmAmt  = _TempSplit * lerp(1.0, _FacetTune.w, tuned);

            // 채도. 합산 광량이 1을 넘으면 알베도가 흰색 쪽으로 씻겨 "탁한 황토색"이 된다.
            // **세게 걸지 않는다.** 휘도로 당기는 방식이라 갈색의 파랑이 먼저 죽어
            // 바위가 올리브색이 되고, 잔디는 형광 노랑으로 간다.
            // farm 의 _FacetTune.x 는 1.0 이라 여기서는 1.04 그대로다(후처리 채도가 별도로 1.22).
            // **여기서 올린 채도는 조명이 식혀도 남는다** — 후처리 채도는 휘도로 당기는
            // 연산이라 이미 무채색이 된 면을 되살리지 못한다. 라운드 3 의 절벽 그늘면이
            // 정확히 그 상태였다(채도 0.046). 탁함은 채도만으로 풀리지 않으므로 광원과
            // tint 를 같이 고친다.
            float aluma = dot(albedo, float3(0.2126, 0.7152, 0.0722));
            albedo = lerp(aluma.xxx, albedo, _Saturation * satBoost);

            // 결. 두 배율을 겹쳐야 반복이 보이지 않는다.
            float grain = noise(IN.worldPos * 2.6) * 0.65 + noise(IN.worldPos * 9.1) * 0.35;
            albedo *= 1.0 + (grain - 0.5) * _Grain * 2.0;

            float up   = saturate(IN.worldNormal.y);                  // 윗면 판정 — 색온도용
            float lift = saturate(IN.worldNormal.y * 0.5 + 0.5);      // 위아래 밝기용

            // **색온도 분리.** 윗면은 햇빛(#ffe9c4) 쪽으로, 측면·아랫면은 그늘(#6b7a8a) 쪽으로.
            // 빛의 방향만으로는 부족하다 — 알베도까지 갈라져야 종이 모형에서 벗어난다.
            //
            // **cool 은 그늘면 탈색의 공범이다.** 그늘진 절벽이 받는 빛은 이미 B/R 1.31 인데
            // (Staging.FarmLighting 의 바다 반사광 + equator 환경광) 알베도에 cool 을 한 번 더
            // 곱하면 B/R 이 1.10 배 더 오른다. 갈색(B/R 0.55)이 두 번 식으면 남는 채도가 없다.
            // 그래서 farm 은 cool 만 0.45 로 줄이고 warm 은 1.5 로 키운다 — 윗면의 따뜻함은
            // 시안 대비 오히려 모자라기 때문이다. 측면과 윗면의 갈림은 **광원**이 유지한다.
            float3 warm = lerp(float3(1,1,1), float3(1.075, 1.015, 0.905), warmAmt);
            float3 cool = lerp(float3(1,1,1), float3(0.905, 0.960, 1.070), coolAmt);
            albedo *= lerp(cool, warm, up);

            // 위를 보는 면이 밝다. 하늘에서 오는 빛을 받는 쪽이다.
            // 0.16 은 윗면을 8% 들어 올려 잔디가 시안보다 밝아졌다. 0.10(윗면 +5%)으로 낮춘다.
            albedo *= 1.0 - _HeightTint * 0.5 + _HeightTint * lift;

            // 림 라이트. 실루엣이 배경에서 떨어져 나오게 한다.
            // **약하게 건다.** 발광이라 조명과 무관하게 더해지므로, 세면 그늘진 절벽을
            // 스스로 밝혀 방금 만든 명암 갈림을 도로 덮는다(이전 0.28이 그랬다).
            // 카메라가 넓어지면 물체가 작아지고 실루엣 길이 대비 면적이 줄어서 **같은 세기라도
            // 림이 화면에서 차지하는 비율이 커진다.** 바위마다 하늘색 테가 둘리면 그게 우유빛의
            // 일부가 된다. farm 에서는 배수로 눌러 둔다.
            //
            // **이건 곱이 아니라 더하기라서 어두운 면일수록 치명적이다.** 라운드 3 화면을 역산하면
            // 절벽 면마다 파랑이 +0.045 만큼 더해져 있는데(햇빛 면·그늘 면 모두 거의 같은 양),
            // 휘도 0.39 인 햇빛 면에서는 채도가 0.17 깎이는 데 그치지만 휘도 0.24 인 그늘 면에서는
            // 파랑이 빨강을 추월해 **B/R 0.88 → 1.05**, 즉 갈색이 청회색으로 뒤집힌다.
            // 12각 벽은 카메라를 크게 비껴 보는 면이 많아 rim 이 0.8 을 넘는 곳이 흔하다.
            // 0.08 x 0.70 = 0.056 이던 것을 0.08 x 0.22 = 0.018 로 내린다.
            float rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), IN.worldNormal)), _RimPower);
            o.Emission = _RimColor.rgb * rim * _RimStrength * rimScale;

            o.Albedo = saturate(albedo);
            // 거칠게. 저폴리 지형이 번들거리면 플라스틱이 된다.
            o.Smoothness = 0.06;
            o.Metallic = 0.0;
            o.Alpha = 1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
