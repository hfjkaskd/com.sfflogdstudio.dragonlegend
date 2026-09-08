Shader "DragonLegend/Recovered PMA Sprite"
{
    Properties {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        _DestinationBlend ("Destination Blend", Float) = 10
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off
        Blend One [_DestinationBlend]
        Pass {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"
            fixed4 Fragment(v2f input):SV_Target {
                fixed4 value=SampleSpriteTexture(input.texcoord)*input.color;
                value.rgb*=input.color.a;
                return value;
            }
            ENDCG
        }
    }
}
