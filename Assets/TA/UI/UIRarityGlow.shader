Shader "UI/RarityGlow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0, 1, 0, 1)
        _GlowIntensity ("Glow Intensity", Range(0.1, 5)) = 2.0
        _GlowRadius ("Glow Radius", Range(0.1, 2)) = 1.0
        _EdgeSoftness ("Edge Softness", Range(0.1, 2)) = 0.6

        // Unity UI Mask 所需属性
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        // RectMask2D 裁剪矩形
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowRadius;
            float _EdgeSoftness;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color;
                OUT.worldPos = v.texcoord - 0.5;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // RectMask2D 裁剪
                float2 xy = IN.worldPosition.xy;
                if (xy.x < _ClipRect.x || xy.y < _ClipRect.y ||
                    xy.x > _ClipRect.z || xy.y > _ClipRect.w)
                    discard;

                float dist = length(IN.worldPos);

                float glow = exp(-dist * dist / (_EdgeSoftness * _EdgeSoftness));
                glow *= 1.0 - smoothstep(_GlowRadius - 0.1, _GlowRadius + 0.1, dist);
                glow *= _GlowIntensity;

                fixed4 result = _GlowColor * glow;
                result.a = glow;
                result.rgb *= IN.color.rgb;
                result.a *= IN.color.a;

                return result;
            }
            ENDCG
        }
    }
}
