Shader "Outline/OutlineDetection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Outline Color", Color) = (1, 0, 0, 1)
        _Width ("Outline Width", Int) = 4
        _Iterations ("Iterations", Int) = 3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "OutlineDetection"
            
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
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                half4 _Color;
                int _Width;
                int _Iterations;
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
                half4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 如果中心像素已经是白色（物体内部），不需要绘制轮廓
                if (center.r > 0.5)
                {
                    return half4(0, 0, 0, 0);
                }
                
                // 边缘检测 - 检查周围像素
                float outline = 0.0;
                float totalSamples = 0.0;
                
                // 限制最大迭代次数为 5，避免循环展开问题
                int maxIter = min(_Iterations, 5);
                
                [unroll(5)]
                for (int iter = 1; iter <= maxIter; iter++)
                {
                    float offset = _Width * iter / (float)maxIter;
                    
                    // 8方向采样 - 使用固定数组避免动态索引
                    [unroll]
                    for (int i = 0; i < 8; i++)
                    {
                        float2 sampleUV = input.uv;
                        
                        // 手动展开 8 个方向
                        if (i == 0) sampleUV += float2(offset, 0) * _MainTex_TexelSize.xy;           // 右
                        else if (i == 1) sampleUV += float2(-offset, 0) * _MainTex_TexelSize.xy;    // 左
                        else if (i == 2) sampleUV += float2(0, offset) * _MainTex_TexelSize.xy;     // 上
                        else if (i == 3) sampleUV += float2(0, -offset) * _MainTex_TexelSize.xy;    // 下
                        else if (i == 4) sampleUV += float2(offset, offset) * _MainTex_TexelSize.xy;      // 右上
                        else if (i == 5) sampleUV += float2(-offset, offset) * _MainTex_TexelSize.xy;     // 左上
                        else if (i == 6) sampleUV += float2(offset, -offset) * _MainTex_TexelSize.xy;     // 右下
                        else if (i == 7) sampleUV += float2(-offset, -offset) * _MainTex_TexelSize.xy;    // 左下
                        
                        half4 sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                        outline += sample.r;
                        totalSamples += 1.0;
                    }
                }
                
                outline /= totalSamples;
                
                // 如果检测到边缘，返回轮廓颜色
                if (outline > 0.1)
                {
                    return _Color;
                }
                
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
}
