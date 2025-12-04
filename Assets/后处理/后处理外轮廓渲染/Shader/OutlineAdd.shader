Shader "Outline/OutlineAdd"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "black" {}
        _AddTex ("Add Texture", 2D) = "black" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "OutlineAdd"
            
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_AddTex);
            SAMPLER(sampler_AddTex);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 简单的加法混合
                half4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 add = SAMPLE_TEXTURE2D(_AddTex, sampler_AddTex, input.uv);
                
                return base + add;
            }
            ENDHLSL
        }
    }
}
