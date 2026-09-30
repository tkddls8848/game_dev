// 바다 위를 흐르는 안개. 시안에서 인상을 가장 크게 좌우하는 요소다.
//
// 섬 주위에 얇은 판 몇 장을 깔고 노이즈로 구멍을 뚫는다. 판이 천천히 흐르면
// 물 위에 낀 아침 안개가 된다. 파티클을 쓰지 않는 이유는 이 게임의 카메라가
// 직교 고정이라 판 몇 장으로 충분하고, 파티클은 저폴리 화면에서 오히려 지저분해지기 때문이다.
//
// **판을 화면 전체에 까는 대신 띠로 쓴다.** 시안이 요구하는 것은 균일한 뿌연 막이 아니라
// 중경을 가로지르는 수평 띠 두세 겹이다. 그래서 가로(U)와 세로(V)의 감쇠를 따로 준다:
//   - U(긴 축)는 양 끝에서만 죽인다   → 띠가 화면을 가로질러 끊기지 않는다
//   - V(짧은 축)는 중앙에서부터 죽인다 → 띠의 위아래가 부드럽게 풀린다
// 예전처럼 둘 다 0.55 에서 죽이면 사각 판의 네 변이 그대로 보인다.
//
// ─── 2026-09-29 라운드 2 ─────────────────────────────────────────────
// 기본 안개 색이 #dee8f0 이었다. 거의 흰색이다. 그 판을 근경 수면 위에 깔았더니
// **바다가 물색이 아니라 뿌연 흰색으로** 보였다(라운드 1 빌드에서 바다가 희게 나온
// 세 원인 중 하나). 안개는 흰 물감이 아니라 공기다 — 기본값을 후처리 안개와 같은
// 계열의 청회색 #a3b7c2 로 내린다. ArtSea.MistLayers() 가 띠마다 다시 덮어쓴다.
// 띠 자체도 3겹 → 2겹으로 줄이고 둘 다 화면 위쪽(원경 섬 밑동)에만 둔다.
//
// --- 2026-09-29 라운드 3 ---------------------------------------------
// 이 셰이더는 이제 **두 가지 모양**으로 쓰인다:
//   · 화면을 가로지르는 띠   (MistLayers) — _FadeU 0.80 / _FadeV 0.00
//   · 원경 섬 하나를 감싸는 덩어리 (IsleMist) — _FadeU 0.06 / _FadeV 0.06
// 그래서 구멍의 **가장자리 기울기**를 값으로 뺐다(_Soft). 고정 2.4 는 화면을 가로지르는
// 큰 띠에서는 맞지만, 섬 하나만 한 작은 판에서는 구멍 테두리가 칼처럼 보인다.
// 기본값은 예전과 같은 2.4 라 값을 넣지 않은 재질의 화면은 바뀌지 않는다.
//
// 기본 색도 #a3b7c2(휘도 0.71) → #90a3af(휘도 0.63) 로 한 단 내렸다. 안개가 배경
// (#465a6a)보다 밝으면 그 자체가 광원처럼 읽혀 원경이 앞으로 나온다.
Shader "PoC/Mist"
{
    Properties
    {
        _Color   ("안개 색",   Color) = (0.565, 0.639, 0.686, 1)
        _Density ("짙기",      Range(0,1)) = 0.42
        _Speed   ("흐름",      Float) = 0.035
        _Scale   ("무늬 크기",  Float) = 0.055
        _FadeU   ("가로 감쇠 시작", Range(0,1)) = 0.80
        _FadeV   ("세로 감쇠 시작", Range(0,1)) = 0.00
        _Cut     ("구멍 문턱",  Range(0,1)) = 0.34
        _Soft    ("구멍 가장자리 기울기", Range(0.6,4)) = 2.4
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Density, _Speed, _Scale, _FadeU, _FadeV, _Cut, _Soft;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);                    // 부드러운 보간
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                            lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            // 결이 여러 겹이어야 안개가 솜처럼 보인다. 한 겹이면 얼룩이 된다.
            float fbm(float2 p)
            {
                float sum = 0, amp = 0.5;
                for (int i = 0; i < 4; i++) { sum += noise(p) * amp; p *= 2.03; amp *= 0.5; }
                return sum;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.world.xz * _Scale;
                float t = _Time.y * _Speed;

                // 두 방향으로 흘려 겹친다. 한 방향이면 무늬가 미끄러지는 것이 보인다.
                float a = fbm(p + float2(t, t * 0.6));
                float b = fbm(p * 1.7 - float2(t * 0.8, t * 1.3));
                // _Soft 가 구멍 가장자리의 기울기다. 크면 안개가 솜이 아니라 얼룩이 된다.
                float mist = saturate((a * 0.65 + b * 0.35 - _Cut) * _Soft);

                // 판의 가장자리를 죽인다. 이게 없으면 네모난 판으로 보인다.
                // 가로·세로를 따로 쓰는 이유는 파일 맨 위 주석 참고.
                float2 edge = abs(i.uv - 0.5) * 2.0;
                float fade = (1.0 - smoothstep(_FadeU, 1.0, edge.x))
                           * (1.0 - smoothstep(_FadeV, 1.0, edge.y));

                return float4(_Color.rgb, mist * fade * _Density);
            }
            ENDCG
        }
    }
    Fallback Off
}
