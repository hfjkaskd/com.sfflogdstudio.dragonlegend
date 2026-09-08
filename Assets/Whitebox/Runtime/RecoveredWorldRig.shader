Shader "DragonLegend/Recovered PMA World Rig"
{
    Properties { [PerRendererData] _MainTex ("Texture", 2D) = "white" {} }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            Output Vertex(Input value) { Output result; result.vertex=UnityObjectToClipPos(value.vertex); result.uv=value.uv; result.color=value.color; return result; }
            fixed4 Fragment(Output value):SV_Target { return tex2D(_MainTex,value.uv)*value.color; }
            ENDCG
        }
    }
}
