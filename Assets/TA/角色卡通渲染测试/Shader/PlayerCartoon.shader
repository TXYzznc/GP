Shader "Custom/PlayerCartoon"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex("主贴图", 2D) = "white" {}
        _Color("主颜色", Color) = (1, 1, 1, 1)

        [Header(Toon Shading)]
        _RampThreshold("色阶阈值", Range(0, 1)) = 0.5
        _RampSmooth("平滑过渡", Range(0, 1)) = 0.1
        _ToonSteps("色阶层数", Range(1, 10)) = 3
        _HColor("高光颜色", Color) = (0.8, 0.8, 0.8, 1.0)
        _SColor("阴影颜色", Color) = (0.2, 0.2, 0.2, 1.0)
        _AmbientStrength("环境光强度", Range(0, 1)) = 0.3

        [Header(Outline Settings)]
        _OutlineWidth("描边宽度", Range(0, 0.1)) = 0.01
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)

        [Header(Outline Advanced)]
        [Toggle(USE_SMOOTH_NORMAL)] _UseSmoothNormal("使用平滑法线", Float) = 0
        _OutlineZOffset("Z偏移", Range(-1, 0)) = -0.001

        [Header(Outline Distance Control)]
        [Toggle(USE_DISTANCE_OUTLINE)] _UseDistanceOutline("启用距离控制描边", Float) = 0
        _OutlineWidthMax("最粗描边", Range(0, 0.2)) = 0.03
        _OutlineDistanceMax("最粗描边距离", Range(0, 50)) = 2.0
        _OutlineWidthMin("最细描边", Range(0, 0.1)) = 0.005
        _OutlineDistanceMin("最细描边距离", Range(0, 100)) = 20.0
        _OutlineDistanceCurve("变化曲线", Range(0.1, 5)) = 1.0

        // ========================================
        // 溶解效果属性（增强版）
        // ========================================
        [Header(Dissolve Effect)]
        [Toggle(_DISSOLVE_ON)] _DissolveEnabled("启用溶解", Float) = 0
        _DissolveTex("溶解噪声贴图", 2D) = "white" {}
        _DissolveThreshold("溶解程度", Range(0, 1.1)) = 0
        _DissolveEdgeWidth("边缘宽度", Range(0.0, 0.5)) = 0.1
        
        [Header(Dissolve Edge Colors)]
        [KeywordEnum(Single, TwoColor, ThreeColor, Rainbow)] _DISSOLVE_MODE("边缘颜色模式", Float) = 0
        [HDR]_DissolveEdgeColor("边缘颜色(单色)", Color) = (3, 1, 0, 2)
        [HDR]_DissolveOuterColor("外边缘颜色", Color) = (5, 5, 2, 2)
        [HDR]_DissolveMidColor("中间颜色", Color) = (3, 1, 0, 2)
        [HDR]_DissolveInnerColor("内边缘颜色", Color) = (1, 0, 0, 1)
        _RainbowIntensity("彩虹强度", Range(0, 5)) = 2
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
        // Pass 1: Outline（增加距离控制和溶解支持）
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
            #pragma shader_feature USE_DISTANCE_OUTLINE
            #pragma shader_feature _DISSOLVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Shader/Modules/DissolveModule.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
            float _OutlineWidth;
            float4 _OutlineColor;
            float _OutlineZOffset;
            
            // 距离控制参数
            #ifdef USE_DISTANCE_OUTLINE
                float _OutlineWidthMax;
                float _OutlineDistanceMax;
                float _OutlineWidthMin;
                float _OutlineDistanceMin;
                float _OutlineDistanceCurve;
            #endif
            
            #ifdef _DISSOLVE_ON
                float _DissolveThreshold;
                float4 _DissolveTex_ST;
            #endif
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                #ifdef USE_SMOOTH_NORMAL
                float4 tangent : TANGENT;
                #endif
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                #ifdef _DISSOLVE_ON
                float2 uv : TEXCOORD0;
                #endif
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                #ifdef USE_SMOOTH_NORMAL
                float3 smoothNormal = normalize(input.tangent.xyz);
                #else
                float3 smoothNormal = input.normalOS;
                #endif

                // 计算最终的描边宽度
                float finalOutlineWidth = _OutlineWidth;
                
                #ifdef USE_DISTANCE_OUTLINE
                    // 计算到摄像机的距离
                    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                    float distanceToCamera = distance(positionWS, _WorldSpaceCameraPos);
                    
                    // 计算插值因子（从近到远：0到1）
                    float distanceFactor = saturate((distanceToCamera - _OutlineDistanceMax) / 
                                                   (_OutlineDistanceMin - _OutlineDistanceMax));
                    
                    // 应用曲线
                    distanceFactor = pow(distanceFactor, _OutlineDistanceCurve);
                    
                    // 在最粗和最细描边之间插值
                    finalOutlineWidth = lerp(_OutlineWidthMax, _OutlineWidthMin, distanceFactor);
                #endif

                float4 clipPos = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = TransformWorldToHClipDir(
                    TransformObjectToWorldNormal(smoothNormal));

                float2 offset = normalize(normalCS.xy);
                clipPos.xy += offset * finalOutlineWidth * clipPos.w;
                clipPos.z += _OutlineZOffset * clipPos.w;

                output.positionCS = clipPos;
                
                #ifdef _DISSOLVE_ON
                output.uv = input.uv;
                #endif
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                #ifdef _DISSOLVE_ON
                    float dissolveAlpha = GetDissolveAlpha(input.uv, _DissolveThreshold, _DissolveTex_ST);
                    clip(dissolveAlpha);
                #endif
                
                return _OutlineColor;
            }
            ENDHLSL
        }

        // ========================================
        // Pass 2: Forward Rendering（集成溶解效果）
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
            #pragma shader_feature _DISSOLVE_ON
            #pragma shader_feature _DISSOLVE_MODE_SINGLE _DISSOLVE_MODE_TWOCOLOR _DISSOLVE_MODE_THREECOLOR _DISSOLVE_MODE_RAINBOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // 引入溶解模块
            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Shader/Modules/DissolveModule.hlsl"
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _RampThreshold;
            float _RampSmooth;
            float _ToonSteps;
            float4 _HColor;
            float4 _SColor;
            float _AmbientStrength;
            
            // 溶解参数
            #ifdef _DISSOLVE_ON
                float _DissolveThreshold;
                float4 _DissolveTex_ST;
                float4 _DissolveEdgeColor;
                float4 _DissolveOuterColor;
                float4 _DissolveMidColor;
                float4 _DissolveInnerColor;
                float _DissolveEdgeWidth;
                float _RainbowIntensity;
            #endif
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
                // ========================================
                // 溶解效果计算（多颜色模式）
                // ========================================
                #ifdef _DISSOLVE_ON
                    DissolveData dissolveData;
                    
                    #if defined(_DISSOLVE_MODE_TWOCOLOR)
                        dissolveData = CalculateDissolve_TwoColor(
                            input.uv,
                            _DissolveThreshold,
                            _DissolveOuterColor,
                            _DissolveInnerColor,
                            _DissolveEdgeWidth,
                            _DissolveTex_ST
                        );
                    #elif defined(_DISSOLVE_MODE_THREECOLOR)
                        dissolveData = CalculateDissolve_ThreeColor(
                            input.uv,
                            _DissolveThreshold,
                            _DissolveOuterColor,
                            _DissolveMidColor,
                            _DissolveInnerColor,
                            _DissolveEdgeWidth,
                            _DissolveTex_ST
                        );
                    #elif defined(_DISSOLVE_MODE_RAINBOW)
                        dissolveData = CalculateDissolve_Rainbow(
                            input.uv,
                            _DissolveThreshold,
                            _DissolveEdgeWidth,
                            _RainbowIntensity,
                            _DissolveTex_ST
                        );
                    #else // _DISSOLVE_MODE_SINGLE
                        dissolveData = CalculateDissolve(
                            input.uv,
                            _DissolveThreshold,
                            _DissolveEdgeColor,
                            _DissolveEdgeWidth,
                            _DissolveTex_ST
                        );
                    #endif
                    
                    // 裁剪溶解的像素
                    clip(dissolveData.alpha);
                #endif

                // ========================================
                // 原始卡通渲染逻辑
                // ========================================
                
                // 1. 光照数据
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);

                // 2. Lambert 光照
                float NdotL = saturate(dot(normalWS, lightDir));

                // 3. 基础漫反射（带阴影衰减）
                float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float diff = NdotL * atten;

                // 4. 多阶色阶计算
                float ramp;
                float interval = 1.0 / _ToonSteps;
                float level = round(diff * _ToonSteps) / _ToonSteps;
                
                ramp = interval * smoothstep(
                    level - _RampSmooth * interval * 0.5,
                    level + _RampSmooth * interval * 0.5,
                    diff
                ) + level - interval;
                
                ramp = max(0, ramp);
                
                ramp = smoothstep(
                    _RampThreshold - 0.1,
                    _RampThreshold + 0.1,
                    ramp
                );

                // 5. 颜色混合
                float3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp);

                // 6. 环境光
                half3 ambient = SampleSH(normalWS) * _AmbientStrength;
                ambient *= rampColor;

                // 7. 纹理采样
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Color.rgb;

                // 8. 最终合成
                half3 directLight = albedo * mainLight.color * rampColor;
                half3 ambientLight = albedo * ambient;
                half3 finalColor = directLight + ambientLight;

                // ========================================
                // 应用溶解边缘发光
                // ========================================
                #ifdef _DISSOLVE_ON
                    finalColor = ApplyDissolveEmission(finalColor, dissolveData);
                #endif

                return half4(finalColor, 1);
            }
            ENDHLSL
        }

        // ========================================
        // Pass 3 & 4: Shadow Caster & Depth（添加溶解支持）
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
            #pragma shader_feature _DISSOLVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            
            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Shader/Modules/DissolveModule.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
            #ifdef _DISSOLVE_ON
                float _DissolveThreshold;
                float4 _DissolveTex_ST;
            #endif
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                #ifdef _DISSOLVE_ON
                float2 uv : TEXCOORD0;
                #endif
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

                #ifdef _DISSOLVE_ON
                output.uv = input.uv;
                #endif

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                #ifdef _DISSOLVE_ON
                    float dissolveAlpha = GetDissolveAlpha(input.uv, _DissolveThreshold, _DissolveTex_ST);
                    clip(dissolveAlpha);
                #endif
                
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
            #pragma shader_feature _DISSOLVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Shader/Modules/DissolveModule.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
            #ifdef _DISSOLVE_ON
                float _DissolveThreshold;
                float4 _DissolveTex_ST;
            #endif
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                #ifdef _DISSOLVE_ON
                float2 uv : TEXCOORD0;
                #endif
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                
                #ifdef _DISSOLVE_ON
                output.uv = input.uv;
                #endif
                
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                #ifdef _DISSOLVE_ON
                    float dissolveAlpha = GetDissolveAlpha(input.uv, _DissolveThreshold, _DissolveTex_ST);
                    clip(dissolveAlpha);
                #endif
                
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}