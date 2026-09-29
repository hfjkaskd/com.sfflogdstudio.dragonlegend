Shader "DragonLegend/Main Skin/Warm Bitmap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glyph atlas", 2D) = "white" {}
        _WarmTint ("Gold face", Color) = (1,0.87,0.48,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float2 local:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _WarmTint;
            float4 _ClipRect;
            Output vert(Input v)
            {
                Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                o.color=v.color; o.local=v.vertex.xy; return o;
            }
            fixed4 frag(Output i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                // Keep black outline and highlights, recolor green glyph faces.
                fixed amount=saturate((c.g-max(c.r,c.b))*8);
                c.rgb=lerp(c.rgb,max(c.r,max(c.g,c.b))*_WarmTint.rgb,amount);
                c*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a-0.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
