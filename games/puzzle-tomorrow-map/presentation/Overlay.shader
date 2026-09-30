Shader "Tomorrow/Overlay" {
Properties {_Color("Color",Color)=(.3,.8,.9,.7)}
SubShader {Tags{"Queue"="Transparent" "RenderType"="Transparent"}Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off Offset -1,-1
Pass {CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
float4 _Color;struct v2f{float4 vertex:SV_POSITION;};v2f vert(float4 p:POSITION){v2f o;o.vertex=UnityObjectToClipPos(p);return o;}
float4 frag(v2f i):SV_Target{return _Color;}
ENDCG}
}Fallback Off
}
