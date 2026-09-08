Shader "DragonLegend/Recovered PMA UI"
{
    Properties {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _DestinationBlend ("Destination Blend", Float) = 10
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend One [_DestinationBlend]
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            Output vert(Input v) {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(Output i):SV_Target {
                fixed4 c=tex2D(_MainTex,i.uv)*i.color;
                c.rgb*=i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
