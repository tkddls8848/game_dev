// 화면 후처리. 시안의 "무게"는 색보정보다 **공기 원근**에서 온다.
//
// 이전 판은 채도를 조금 빼고 비네팅만 걸었다. 그래서 가까운 섬과 먼 바위가 같은 선명도로
// 보였고, 화면이 납작했다. 깊이 버퍼를 읽어 **먼 것을 실제로 안개에 묻는다**.
//
// 넷을 한다:
//   1. 깊이 안개    — 멀수록 옅은 청회색으로 묻힌다. 원경이 뒤로 물러난다
//   2. 색 보정      — 대비·채도를 세우고, 그림자를 차갑게 하이라이트를 따뜻하게(스플릿 토닝)
//   3. 비네팅       — 가장자리를 눌러 사진처럼
//   4. 정지한 입자  — 아주 옅게. 매 프레임 흔들면 TV 노이즈가 된다
//
// ─── 수치는 Properties 가 아니라 전역 변수로 받는다 ──────────────────────
//
// **이 셰이더는 farm-erosion 과 puzzle-tomorrow-map 이 함께 쓴다.** Properties 에 수치를
// 박으면 한쪽을 손볼 때 다른 쪽 연출이 말없이 따라 바뀐다. 게다가 안개 구간은 직교 크기에
// 딸린 값인데 그 크기는 **런타임에 마우스 휠로 바뀐다** — 상수로 박아 두면 맞을 수가 없다.
// 실제로 그랬다: 직교 크기를 6.8 → 5.8 로 줄였을 때 40/88 은 보이는 깊이 구간을 통째로
// 벗어나 **안개 계수가 그냥 0** 이었다(효과가 꺼진 게 아니라 범위가 틀렸다).
//
// 그래서 Staging.cs 의 AirPerspective 가 **살아 있는 카메라에서 매 프레임 다시 재어** 넣는다.
// 값을 주지 않는 장면(puzzle-tomorrow-map)은 아래 legacy 상수를 그대로 쓴다 —
// 즉 구역 C 가 farm 수치를 고쳐도 그쪽 PoC 는 한 픽셀도 바뀌지 않는다.
Shader "PoC/Atmosphere"
{
    Properties
    {
        _MainTex ("Scene", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            // Staging.cs 의 AirPerspective 가 넣는다. 전부 0 이면 "아무도 안 넣었다".
            float4 _AirFog;        // (안개 시작 눈깊이, 끝 눈깊이, 최대 농도, -)
            float4 _AirFogColor;   // 안개 색
            float4 _AirGrade;      // (대비, 들어올림, 하이라이트 숄더 시작, 채도)
            float4 _AirFrame;      // (비네팅 세기, 비네팅 시작 반경, 그레인, 하이라이트 천장)
            float4 _AirTone;       // (그림자 토닝 세기, 하이라이트 토닝 세기, -, -). 1,1 이 이전 판

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 rgb = tex2D(_MainTex, i.uv).rgb;

                // 안개 끝 깊이는 항상 양수다. 0 이면 아무도 값을 넣지 않았다는 뜻이므로
                // 이전 판 수치로 돌아간다(puzzle-tomorrow-map 경로).
                float driven = step(0.001, _AirFog.y);

                float  fogStart = lerp(30.5,  _AirFog.x, driven);
                float  fogEnd   = lerp(36.5,  _AirFog.y, driven);
                float  fogMax   = lerp(0.55,  _AirFog.z, driven);
                float3 fogRGB   = lerp(float3(0.470, 0.545, 0.605), _AirFogColor.rgb, driven);
                float  contrast = lerp(1.08,  _AirGrade.x, driven);
                float  lift     = lerp(0.014, _AirGrade.y, driven);
                float  knee     = lerp(1.0,   _AirGrade.z, driven);   // 1 = 숄더 없음(이전 판)
                float  satAmt   = lerp(1.10,  _AirGrade.w, driven);
                float  vigAmt   = lerp(0.32,  _AirFrame.x, driven);
                float  vigFrom  = lerp(0.26,  _AirFrame.y, driven);
                float  grainAmt = lerp(0.018, _AirFrame.z, driven);
                float  ceiling  = lerp(2.0,   _AirFrame.w, driven);
                float2 tone     = lerp(float2(1, 1), _AirTone.xy, driven);

                // ── 공기 원근 ──
                //
                // **직교 카메라에서는 LinearEyeDepth를 쓰면 안 된다.** 그 함수는 원근 투영의
                // 1/z 분포를 푸는 것이라, 깊이가 이미 선형인 직교에서는 엉뚱한 값을 낸다.
                // (한 번 그렇게 만들어 안개가 통째로 안 걸렸다.)
                // 직교에서는 near~far를 그대로 보간한다. 최신 플랫폼은 역 Z라 뒤집어야 한다.
                float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                #if defined(UNITY_REVERSED_Z)
                    raw = 1.0 - raw;
                #endif
                float depth = lerp(_ProjectionParams.y, _ProjectionParams.z, raw);
                // 시작점은 **가장 먼 땅보다 뒤**, 끝점은 화면 맨 위의 수면이다. 둘 다
                // AirPerspective 가 카메라에서 재어 주므로 직교 크기를 휠로 바꿔도 따라온다.
                // 근경은 여기서 반드시 0 이어야 한다 — 시안의 부드러움은 안개가 아니라
                // 여백과 색에서 오고, 근경에 안개를 얹으면 그냥 우유빛이 된다.
                float fog = saturate((depth - fogStart) / max(0.001, fogEnd - fogStart));
                fog *= sqrt(fog);                      // 곡선을 눕혀(지수 1.5) 섬 바로 뒤는 살짝만 건드린다
                // 위로 갈수록 안개가 두껍다 — 수평선 쪽에 띠가 생긴다. 예전 0.78~1.22 는
                // 기울기가 세서 중경까지 끌어올렸다. 얇게 눕힌다.
                fog *= lerp(0.88, 1.12, saturate(i.uv.y + 0.15));
                rgb = lerp(rgb, fogRGB, saturate(fog) * fogMax);

                // ── 대비 ──
                // 0.46 을 축으로 세운다. **시안은 대비가 센 화면이 아니다** —
                // 두 그림의 휘도 분포를 재면 시안이 오히려 좁고 낮다:
                //            p1     p5    p50    p95    p99   평균
                //   시안    0.178  0.212  0.343  0.562  0.655  0.359
                //   빌드    0.157  0.196  0.362  0.753  0.972  0.410
                // 즉 빌드는 **그늘이 더 어둡고 밝은 곳이 흰색까지 날아간다.** 고칠 것은
                // 중간톤 대비가 아니라 양 끝이다. 그늘은 lift 로 살짝 들고, 밝은 쪽은 아래 숄더로 눕힌다.
                rgb = max(0, (rgb - 0.46) * contrast + 0.46 + lift);

                // ── 하이라이트 숄더 ──
                // knee 위를 천장 ceiling 으로 점근시킨다. 시안의 p99 가 0.655 라 **흰색이
                // 아예 없다** — 포말도 밝은 회백색까지만 간다. 이전 판은 p99 가 0.972 였다.
                // 이 꼴을 쓰는 이유는 **단조성이 보장되기 때문**이다. 이차식으로 누르면
                // 계수가 커지는 순간 곡선이 꺾여 가장 밝은 쪽이 오히려 어두워진다.
                // 도함수 1/(1+hi/(ceiling-knee))^2 은 항상 양수다.
                float3 hi = max(0, rgb - knee);
                rgb = saturate(min(rgb, knee + hi / (1.0 + hi / max(1e-4, ceiling - knee))));

                // ── 스플릿 토닝 ──
                // 그림자는 청록, 하이라이트는 호박. 두 색이 갈라져야 시간대가 읽힌다.
                //
                // **그림자 토닝은 세기를 따로 받는다.** 이 곱은 그림자에서 B/R 을 1.199 배 올리는데,
                // 그늘진 절벽은 광원(1.31)과 알베도 cool tint(1.10)에서 이미 두 번 식은 뒤에
                // 여기로 들어온다. 세 곱이 겹치면 갈색 알베도(B/R 0.55)가 0.88 까지 밀려
                // 채도가 거의 남지 않는다(3차 비평 §5). 그래서 farm 은 0.42 로 줄인다 —
                // 끄지는 않는다. 차가운 그림자 자체는 시안에도 있다.
                // 하이라이트 쪽은 반대로 조금 키운다(1.06). 시안의 흙·잔디 윗면이 빌드보다
                // 주황에 가깝기 때문이다(흙 색상 35도 대 빌드 52도).
                float luma = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                float3 shadowTint = lerp(float3(1,1,1), float3(0.905, 0.985, 1.085), tone.x);
                float3 lightTint  = lerp(float3(1,1,1), float3(1.075, 1.015, 0.915), tone.y);
                rgb *= lerp(shadowTint, lightTint, smoothstep(0.22, 0.72, luma));

                // 채도. 실측에서 빌드 평균 채도가 0.307, 시안이 0.340 이었다.
                // **순서가 중요하다 — 숄더 다음이어야 한다.** 숄더는 채널별로 걸리니까
                // 채널 간격을 좁혀 밝은 쪽을 탈색시킨다(같은 채도 1.14 로도 숄더를 조이면
                // 평균 채도가 0.348 → 0.320 으로 떨어지는 것을 측정했다). 그 손실을
                // 여기서 되돌린다. 앞에 두면 숄더가 다시 깎아 먹는다.
                // 세게 걸면 갈색이 올리브로, 잔디가 형광 노랑으로 돈다 — 그래서 1.25 를 상한으로 뒀다.
                luma = dot(rgb, float3(0.2126, 0.7152, 0.0722));
                rgb = saturate(lerp(luma.xxx, rgb, satAmt));

                // ── 비네팅 ──
                // 세기를 줄이고 시작 반경을 밀어 낸다. 카메라가 넓어지면 모서리는 전부 바다라,
                // 예전 0.26 부터 0.32 로 먹이면 **맑아진 바다를 다시 어둡게 눌러** 무거워진다.
                float2 d = i.uv - float2(0.5, 0.52);
                float vignette = 1 - vigAmt * smoothstep(vigFrom, 0.80, length(d * float2(1.0, 1.15)));
                rgb *= vignette;

                // ── 정지한 입자 ──
                float grain = (hash(floor(i.uv * _ScreenParams.xy * 0.75)) - 0.5) * grainAmt;
                rgb = saturate(rgb + grain);

                return float4(rgb, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
