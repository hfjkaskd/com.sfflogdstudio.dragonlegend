Shader "DragonLegend/Recovered PMA Region Rig"
{
 Properties {
  [PerRendererData] _MainTex("Atlas",2D)="white" {}
  _StencilComp("Stencil Comparison",Float)=8
  _Stencil("Stencil ID",Float)=0
  _StencilOp("Stencil Operation",Float)=0
  _StencilWriteMask("Stencil Write Mask",Float)=255
  _StencilReadMask("Stencil Read Mask",Float)=255
  _ColorMask("Color Mask",Float)=15
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend One OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct Input {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct Output {float4 position:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float2 local:TEXCOORD1;};
   sampler2D _MainTex;float4 _ClipRect;
   Output vert(Input v){Output o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.local=v.vertex.xy;return o;}
   fixed4 frag(Output i):SV_Target {
    fixed4 c=tex2D(_MainTex,i.uv)*i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c*=UnityGet2DClipping(i.local,_ClipRect);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
