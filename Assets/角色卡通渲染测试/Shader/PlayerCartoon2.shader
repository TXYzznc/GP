Shader "Custom/PlayerCartoon2"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)

        [Header(Toon Shading)]
        _RampThreshold("Ramp Threshold", Range(0, 1)) = 0.5
        _RampSmooth("Smooth Threshold", Range(0, 1)) = 0.1
        _HColor("Highlight Color", Color) = (0.8, 0.8, 0.8, 1.0)
        _SColor("Shadow Color", Color) = (0.2, 0.2, 0.2, 1.0)
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.3

        [Header(Outline Settings)]
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.01
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)

        [Header(Outline Advanced)]
        [Toggle(USE_SMOOTH_NORMAL)] _UseSmoothNormal("Use Smooth Normal", Float) = 0
        _OutlineZOffset("Z Offset", Range(-1, 0)) = -0.001
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline" // ✅ 关键：声明URP管线
        }
        LOD 200

        // == == == == == == == == == == == == == == == == == == == ==
        // ✅ URP描边Pass
        // == == == == == == == == == == == == == == == == == == == ==
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" } // ✅ URP无光照标签

            Cull Front
            ZWrite On
            ColorMask RGB

            HLSLPROGRAM // ✅ 使用HLSL替代CG
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_SMOOTH_NORMAL

            // ✅ URP核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ✅ 使用CBUFFER声明属性
            CBUFFER_START(UnityPerMaterial)
            float _OutlineWidth;
            float4 _OutlineColor;
            float _OutlineZOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                #ifdef USE_SMOOTH_NORMAL
                float4 tangent : TANGENT;
                #endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 选择法线
                #ifdef USE_SMOOTH_NORMAL
                float3 smoothNormal = normalize(input.tangent.xyz);
                #else
                float3 smoothNormal = input.normalOS;
                #endif

                // ✅ URP API：TransformObjectToHClip
                float4 clipPos = TransformObjectToHClip(input.positionOS.xyz);

                // 法线转换到裁剪空间
                float3 normalCS = TransformWorldToHClipDir(
                TransformObjectToWorldNormal(smoothNormal));

                // 屏幕空间描边
                float2 offset = normalize(normalCS.xy);
                clipPos.xy += offset * _OutlineWidth * clipPos.w;

                // 深度偏移
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

        // == == == == == == == == == == == == == == == == == == == ==
        // ✅ URP主渲染Pass
        // == == == == == == == == == == == == == == == == == == == ==
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" } // ✅ URP前向渲染标签

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // ✅ URP多编译指令（支持主光源阴影）
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            // ✅ URP核心库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ✅ 材质属性
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _RampThreshold;
            float _RampSmooth;
            float4 _HColor;
            float4 _SColor;
            float _AmbientStrength;
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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // ✅ URP空间转换API
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ✅ 1. 获取主光源数据
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);

                // ✅ 2. Lambert光照计算
                float NdotL = saturate(dot(normalWS, lightDir));

                // ✅ 3. 应用阴影衰减
                NdotL *= mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                // 4. 卡通化Ramp
                float ramp = smoothstep(
                _RampThreshold - _RampSmooth * 0.5,
                _RampThreshold + _RampSmooth * 0.5,
                NdotL
                );

                // 5. 颜色混合
                float3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp);

                // ✅ 6. 环境光（使用URP的SH环境光）
                half3 ambient = SampleSH(normalWS) * _AmbientStrength;

                // 7. 纹理采样
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Color.rgb;

                // 8. 最终合成
                half3 directLight = albedo * mainLight.color * rampColor;
                half3 ambientLight = albedo * ambient;
                half3 finalColor = directLight + ambientLight;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // == == == == == == == == == == == == == == == == == == == ==
        // ✅ URP阴影投射Pass（完全自定义版）
        // == == == == == == == == == == == == == == == == == == == ==
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

            // ✅ 最小依赖包含
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

            // ✅ 从URP源码中提取的光源方向（全局变量）
            float3 _LightDirection;
            float3 _LightPosition;

            // ✅ 简化的阴影偏移函数（避免依赖LerpWhiteTo）
            float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * 0.1; // 简化的常数偏移

                // 法线偏移
                positionWS = normalWS * scale + positionWS;
                return positionWS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // ✅ 应用偏移
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                positionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                output.positionCS = TransformWorldToHClip(positionWS);

                // ✅ 深度裁剪修复
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

        // == == == == == == == == == == == == == == == == == == == ==
        // ✅ 深度Pass（简化版）
        // == == == == == == == == == == == == == == == == == == == ==
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