Shader "Tomorrow/Optics" {
Properties{_MainTex("Scene",2D)="white"{}}
SubShader{Cull Off ZWrite Off ZTest Always Pass{CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex;float4 _MainTex_TexelSize;
float4 frag(v2f_img i):SV_Target {
float2 uv=i.uv;float edge=abs(uv.y-.51);float blur=smoothstep(.24,.52,edge)*2.2;float2 px=_MainTex_TexelSize.xy*blur;
float3 col=tex2D(_MainTex,uv).rgb*.4;
col+=(tex2D(_MainTex,uv+float2(px.x,px.y)).rgb+tex2D(_MainTex,uv+float2(-px.x,px.y)).rgb+tex2D(_MainTex,uv+float2(px.x,-px.y)).rgb+tex2D(_MainTex,uv-px).rgb)*.15;
float vignette=1-.20*smoothstep(.3,.72,length(uv-float2(.47,.52)));return float4(col*vignette,1);
}
ENDCG}}Fallback Off
}
