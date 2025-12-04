Shader "Outline/OutlineComposite"
{
    Properties
    {
        _MainTex ("Scene Texture", 2D) = "white" {}
        _OutlineTex ("Outline Texture", 2D) = "black" {}
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "OutlineComposite"
            
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
            
            TEXTURE2D(_OutlineTex);
            SAMPLER(sampler_OutlineTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _OutlineColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 采样场景纹理
                half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 采样轮廓纹理
                half4 outlineColor = SAMPLE_TEXTURE2D(_OutlineTex, sampler_OutlineTex, input.uv);
                
                // 检查轮廓是否有颜色
                float outlineStrength = max(max(outlineColor.r, outlineColor.g), outlineColor.b);
                
                // 如果有轮廓，叠加到场景上
                if (outlineStrength > 0.01)
                {
                    // 使用 Alpha 混合，保留场景颜色
                    return half4(lerp(sceneColor.rgb, outlineColor.rgb, outlineColor.a), 1.0);
                }
                
                // 没有轮廓，返回原始场景
                return sceneColor;
            }
            ENDHLSL
        }
    }
}
