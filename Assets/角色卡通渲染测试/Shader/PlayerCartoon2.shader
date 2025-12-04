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
        _ToonSteps("Toon Steps", Range(1, 10)) = 3  // ✅ 新增：色阶层数
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
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        // ========================================
        // Pass 1: Outline（保持不变）
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
            #pragma shader_feature USE_SMOOTH_NORMAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

                #ifdef USE_SMOOTH_NORMAL
                float3 smoothNormal = normalize(input.tangent.xyz);
                #else
                float3 smoothNormal = input.normalOS;
                #endif

                float4 clipPos = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = TransformWorldToHClipDir(
                    TransformObjectToWorldNormal(smoothNormal));

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
        // Pass 2: Forward Rendering（✅ 多阶色阶实现）
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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _RampThreshold;
            float _RampSmooth;
            float _ToonSteps;  // ✅ 新增变量
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
                // ========== 1. 光照数据 ==========
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);

                // ========== 2. Lambert 光照 ==========
                float NdotL = saturate(dot(normalWS, lightDir));

                // ========== 3. 基础漫反射（带阴影衰减）==========
                float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float diff = NdotL * atten;

                // ========== 4. 多阶色阶计算（✅ 核心实现）==========
                float ramp;
                
                // 方法：平滑多阶色阶
                float interval = 1.0 / _ToonSteps;  // 每个色阶的间隔
                float level = round(diff * _ToonSteps) / _ToonSteps;  // 当前色阶级别
                
                // 在色阶边界处进行平滑过渡
                ramp = interval * smoothstep(
                    level - _RampSmooth * interval * 0.5,
                    level + _RampSmooth * interval * 0.5,
                    diff
                ) + level - interval;
                
                ramp = max(0, ramp);  // 防止负值
                
                // 应用阈值控制（可选，用于整体调整明暗比例）
                ramp = smoothstep(
                    _RampThreshold - 0.1,
                    _RampThreshold + 0.1,
                    ramp
                );

                // ========== 5. 颜色混合 ==========
                float3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp);

                // ========== 6. 环境光 ==========
                half3 ambient = SampleSH(normalWS) * _AmbientStrength;
                ambient *= rampColor;  // 环境光也受色阶影响

                // ========== 7. 纹理采样 ==========
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Color.rgb;

                // ========== 8. 最终合成 ==========
                half3 directLight = albedo * mainLight.color * rampColor;
                half3 ambientLight = albedo * ambient;
                half3 finalColor = directLight + ambientLight;

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // ========================================
        // Pass 3 & 4: Shadow Caster & Depth（保持不变）
        // ========================================
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

            float3 ApplyShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * 0.1;
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