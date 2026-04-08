Shader "Custom/OutlineDilate"
{
    Properties
    {
        _DilateSize ("Dilate Size", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Dilate"

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float _DilateSize;

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv[9] : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.positionOS.xyz);
                float2 texel = _MainTex_TexelSize.xy * _DilateSize;
                o.uv[0] = v.uv.xy;
                o.uv[1] = v.uv.xy + float2(0, texel.y);
                o.uv[2] = v.uv.xy - float2(0, texel.y);
                o.uv[3] = v.uv.xy + float2(texel.x, 0);
                o.uv[4] = v.uv.xy - float2(texel.x, 0);
                o.uv[5] = v.uv.xy + float2(texel.x, texel.y);
                o.uv[6] = v.uv.xy - float2(texel.x, texel.y);
                o.uv[7] = v.uv.xy + float2(texel.x, -texel.y);
                o.uv[8] = v.uv.xy - float2(texel.x, -texel.y);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Max filter: 取 9 个采样点的最大值（膨胀操作）
                half4 result = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[0]);
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[1]));
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[2]));
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[3]));
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[4]));
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[5]));
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[6]));
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[7]));
                result = max(result, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv[8]));
                return result;
            }

            ENDHLSL
        }
    }
}
