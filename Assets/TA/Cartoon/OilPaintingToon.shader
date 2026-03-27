Shader "Custom/OilPainting_Toon"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex("Texture", 2D) = "white" {}
        _Color("Main Color", Color) = (1, 1, 1, 1)

        [Space(10)]
        [Header(Oil Painting Settings)]
        _Radius ("笔触半径(越大越卡)", Range(1, 10)) = 3
        _Hardness ("边缘硬度", Range(1, 20)) = 8
        _Sharpness ("细节锐度", Range(0, 1)) = 0.5
        _BrushScale ("油画效果", Range(1, 5)) = 2

        [Space(10)]
        [Header(Toon Shading)]
        _RampThreshold("色阶阈值", Range(0, 1)) = 0.5
        _RampSmooth("平滑过渡", Range(0, 1)) = 0.1
        _ToonSteps("色阶层数", Range(1, 10)) = 3
        _HColor("高光颜色", Color) = (0.8, 0.8, 0.8, 1.0)
        _SColor("阴影颜色", Color) = (0.2, 0.2, 0.2, 1.0)
        _AmbientStrength("环境光强度", Range(0, 5)) = 0.5

        [Space(10)]
        [Header(Outline Settings)]
        _OutlineWidth("描边宽度", Range(0, 0.1)) = 0.02
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineZOffset("Z偏移 (防止穿插)", Range(-1, 0)) = -0.001
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        // ========================================
        // Pass 1: Outline (来自 PlayerCartoon 的描边逻辑)
        // ========================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Radius; float _Hardness; float _Sharpness; float _BrushScale;
                float _RampThreshold; float _RampSmooth; float _ToonSteps;
                float4 _HColor; float4 _SColor; float _AmbientStrength;
                float _OutlineWidth;
                float4 _OutlineColor;
                float _OutlineZOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // 简单的沿法线外扩描边
                float4 clipPos = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(input.normalOS));

                float2 offset = normalize(normalCS.xy);
                clipPos.xy += offset * _OutlineWidth * clipPos.w;
                clipPos.z += _OutlineZOffset * clipPos.w;

                output.positionCS = clipPos;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ========================================
        // Pass 2: Forward Lit (油画 + 卡通光照)
        // ========================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            // 标准 URP 光照宏
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Radius; float _Hardness; float _Sharpness; float _BrushScale;
                float _RampThreshold; float _RampSmooth; float _ToonSteps;
                float4 _HColor; float4 _SColor; float _AmbientStrength;
                float _OutlineWidth; float4 _OutlineColor; float _OutlineZOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            // --- 核心：油画算法 ---
            float3 CalculateOilPaintColor(float2 uv)
            {
                float2 texelSize = _MainTex_TexelSize.xy;
                int radius = int(ceil(_Radius));
                
                float3 m[4] = { {0,0,0}, {0,0,0}, {0,0,0}, {0,0,0} };
                float3 s[4] = { {0,0,0}, {0,0,0}, {0,0,0}, {0,0,0} };
                float w[4] = { 0, 0, 0, 0 }; 

                [loop] // 强制循环，优化指令缓存
                for (int y = -radius; y <= radius; y++) 
                {
                    for (int x = -radius; x <= radius; x++) 
                    {
                        float2 offset = float2(x, y) * texelSize * _BrushScale;
                        // 必须用 LOD 0 防止梯度报错
                        float3 c = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv + offset, 0).rgb;
                        
                        // 高斯加权，让笔触变圆
                        float weight = exp(-(x*x + y*y) / (2.0 * (_Radius * 0.35) * (_Radius * 0.35)));

                        if (x <= 0 && y <= 0) { m[0] += c * weight; s[0] += c * c * weight; w[0] += weight; }
                        if (x >= 0 && y <= 0) { m[1] += c * weight; s[1] += c * c * weight; w[1] += weight; }
                        if (x <= 0 && y >= 0) { m[2] += c * weight; s[2] += c * c * weight; w[2] += weight; }
                        if (x >= 0 && y >= 0) { m[3] += c * weight; s[3] += c * c * weight; w[3] += weight; }
                    }
                }

                float3 finalColor = 0;
                float sumWeights = 0;

                for (int k = 0; k < 4; k++) 
                {
                    m[k] /= w[k];
                    s[k] = abs(s[k] / w[k] - m[k] * m[k]);
                    
                    float sigma2 = s[k].r + s[k].g + s[k].b;
                    // 软混合权重
                    float targetWeight = 1.0 / (1.0 + pow(sigma2 * 1000.0, _Sharpness)); 
                    float w_final = pow(targetWeight, _Hardness);

                    finalColor.rgb += m[k] * w_final;
                    sumWeights += w_final;
                }
                return finalColor / sumWeights;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. 先计算油画颜色 (替代原本的纹理采样)
                float3 oilAlbedo = CalculateOilPaintColor(input.uv);
                oilAlbedo *= _Color.rgb;

                // 2. 获取光照数据
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);

                // 3. 卡通光照逻辑 (来自 PlayerCartoon)
                float NdotL = saturate(dot(normalWS, lightDir));
                
                // 阴影衰减 (Receive Shadows)
                float shadowAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float diff = NdotL * shadowAtten;

                // 色阶计算 (Ramp)
                float interval = 1.0 / _ToonSteps;
                float level = round(diff * _ToonSteps) / _ToonSteps;
                float ramp = interval * smoothstep(
                    level - _RampSmooth * interval * 0.5,
                    level + _RampSmooth * interval * 0.5, 
                    diff) + level - interval;
                ramp = smoothstep(_RampThreshold - 0.1, _RampThreshold + 0.1, max(0, ramp));

                // 混合 阴影色 -> 高光色
                float3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp);

                // 4. 环境光
                half3 ambient = SampleSH(normalWS) * _AmbientStrength;
                
                // 5. 最终合成
                half3 directLight = oilAlbedo * mainLight.color * rampColor;
                half3 ambientLight = oilAlbedo * ambient;
                half3 finalColor = directLight + ambientLight;

                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // ========================================
        // Pass 3: ShadowCaster (投射阴影)
        // ========================================
        // 关键修复：完全复制 PlayerCartoon 的手动 Bias 逻辑，不依赖外部库
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;
            float3 _LightPosition;

            // 这里手动计算 Shadow Bias，不调用 Library 里的复杂函数
            float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * 0.1; // 简单的 bias 缩放
                positionWS = normalWS * scale + positionWS;
                return positionWS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                positionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                output.positionCS = TransformWorldToHClip(positionWS);

                #if UNITY_REVERSED_Z
                output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // ========================================
        // Pass 4: DepthOnly
        // ========================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}