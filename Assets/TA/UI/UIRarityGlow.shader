Shader "UI/RarityGlow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (0, 1, 0, 1)
        _GlowIntensity ("Glow Intensity", Range(0.1, 5)) = 2.0
        _GlowRadius ("Glow Radius", Range(0.1, 2)) = 1.0
        _EdgeSoftness ("Edge Softness", Range(0.1, 2)) = 0.6
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowRadius;
            float _EdgeSoftness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color;
                OUT.worldPos = v.texcoord - 0.5; // 相对于中心的坐标
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 计算距离中心的距离（UV坐标 0.5 是中心）
                float dist = length(IN.worldPos);

                // 高斯衰减：从中心扩散的柔和光晕
                float glow = exp(-dist * dist / (_EdgeSoftness * _EdgeSoftness));

                // 在 Glow Radius 处逐渐消失
                glow *= 1.0 - smoothstep(_GlowRadius - 0.1, _GlowRadius + 0.1, dist);

                // 应用强度
                glow *= _GlowIntensity;

                // 最终颜色
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
