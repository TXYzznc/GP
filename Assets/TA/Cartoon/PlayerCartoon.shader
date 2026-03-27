Shader "Custom/PlayerCartoon"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex("主贴图", 2D) = "white" {}
        _Color("主颜色", Color) = (1, 1, 1, 1)

        [Header(Stealth Settings)]
        _StealthAlpha("隐身透明度", Range(0, 1)) = 1.0

        [Header(Toon Shading)]
        _RampThreshold("色阶阈值", Range(0, 1)) = 0.5
        _RampSmooth("平滑过渡", Range(0, 1)) = 0.1
        _ToonSteps("色阶层数", Range(1, 10)) = 3
        _HColor("高光颜色", Color) = (0.8, 0.8, 0.8, 1.0)
        _SColor("阴影颜色", Color) = (0.2, 0.2, 0.2, 1.0)
        _AmbientStrength("环境光强度", Range(0, 5)) = 0.3

        [Header(Outline Settings)]
        _OutlineWidth("描边宽度", Range(0, 0.1)) = 0.01
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)

        [Header(Outline Advanced)]
        [Toggle(USE_SMOOTH_NORMAL)] _UseSmoothNormal("使用平滑法线", Float) = 0
        _OutlineZOffset("Z偏移", Range(-1, 0)) = -0.001

        [Header(Outline Distance Control)]
        [Toggle(USE_DISTANCE_OUTLINE)] _UseDistanceOutline("启用距离控制描边(整物体)", Float) = 0
        _OutlineWidthMax("最粗描边", Range(0, 0.2)) = 0.03
        _OutlineDistanceMax("最粗描边距离", Range(0, 50)) = 2.0
        _OutlineWidthMin("最细描边", Range(0, 0.1)) = 0.005
        _OutlineDistanceMin("最细描边距离", Range(0, 100)) = 20.0
        _OutlineDistanceCurve("变化曲线", Range(0.1, 5)) = 1.0

        [Header(Outline Local Depth Control)]
        [Toggle(USE_LOCAL_DEPTH_OUTLINE)] _UseLocalDepthOutline("近距离启用局部近粗远细(视空间)", Float) = 0
        _OutlineLocalEnableDistance("启用距离阈值(视空间深度<该值才生效)", Range(0, 50)) = 5

        // 新增：局部描边专用参数（完全独立）
        _LocalOutlineWidthNear("局部描边-近处宽度", Range(0, 0.2)) = 0.03
        _LocalOutlineWidthFar("局部描边-远处宽度", Range(0, 0.2)) = 0.005
        _LocalOutlineDepthNear("局部深度-开始(近)", Range(0, 50)) = 0.5
        _LocalOutlineDepthFar("局部深度-结束(远)", Range(0, 50)) = 5.0
        _LocalOutlineCurve("局部变化曲线", Range(0.1, 5)) = 1.0
        _LocalOutlineRate("局部变化率(倍速)", Range(0.1, 5)) = 1.0

        // ========================================
        // Dissolve
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
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        // ========================================
        // Pass 1: Outline
        // ========================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_SMOOTH_NORMAL
            #pragma shader_feature USE_DISTANCE_OUTLINE
            #pragma shader_feature USE_LOCAL_DEPTH_OUTLINE
            #pragma shader_feature _DISSOLVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float _StealthAlpha;
                float _OutlineWidth;
                float4 _OutlineColor;
                float _OutlineZOffset;

                // 整物体距离描边参数
                #ifdef USE_DISTANCE_OUTLINE
                    float _OutlineWidthMax;
                    float _OutlineDistanceMax;
                    float _OutlineWidthMin;
                    float _OutlineDistanceMin;
                    float _OutlineDistanceCurve;
                #endif

                // 局部近粗远细参数（独立一套）
                #ifdef USE_LOCAL_DEPTH_OUTLINE
                    float _OutlineLocalEnableDistance;

                    float _LocalOutlineWidthNear;
                    float _LocalOutlineWidthFar;
                    float _LocalOutlineDepthNear;
                    float _LocalOutlineDepthFar;
                    float _LocalOutlineCurve;
                    float _LocalOutlineRate;
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

                // 0) 默认描边宽度
                float finalOutlineWidth = _OutlineWidth;

                // 1) 整物体距离控制描边（原功能）
                #ifdef USE_DISTANCE_OUTLINE
                {
                    float3 posWS_vtx = TransformObjectToWorld(input.positionOS.xyz);
                    float distWS = distance(posWS_vtx, _WorldSpaceCameraPos);

                    float denom = max(1e-5, (_OutlineDistanceMin - _OutlineDistanceMax));
                    float distanceFactor = saturate((distWS - _OutlineDistanceMax) / denom);
                    distanceFactor = pow(distanceFactor, _OutlineDistanceCurve);

                    finalOutlineWidth = lerp(_OutlineWidthMax, _OutlineWidthMin, distanceFactor);
                }
                #endif

                // 2) 局部近粗远细：仅当物体中心“足够近”才启用；启用时覆盖 finalOutlineWidth
                #ifdef USE_LOCAL_DEPTH_OUTLINE
                {
                    float3 objectCenterWS = TransformObjectToWorld(float3(0, 0, 0));
                    float3 objectCenterVS = TransformWorldToView(objectCenterWS);
                    float objectDepthVS = -objectCenterVS.z; // 视空间深度（相机前方为正）

                    if (objectDepthVS < _OutlineLocalEnableDistance)
                    {
                        float3 posWS_vtx2 = TransformObjectToWorld(input.positionOS.xyz);
                        float3 posVS_vtx2 = TransformWorldToView(posWS_vtx2);
                        float vertexDepthVS = -posVS_vtx2.z;

                        // 顶点深度映射到 0..1
                        float denomLocal = max(1e-5, (_LocalOutlineDepthFar - _LocalOutlineDepthNear));
                        float t = saturate((vertexDepthVS - _LocalOutlineDepthNear) / denomLocal);

                        // 曲线 + 变化率（变化率用作“加速”，>1 更快到达远处细描边）
                        t = pow(t, _LocalOutlineCurve);
                        t = saturate(t * _LocalOutlineRate);

                        // 覆盖：近处宽 -> 远处窄
                        finalOutlineWidth = lerp(_LocalOutlineWidthNear, _LocalOutlineWidthFar, t);
                    }
                }
                #endif

                // 挤出描边
                float4 clipPos = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(smoothNormal));

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

                return half4(_OutlineColor.rgb, _OutlineColor.a * _StealthAlpha);
            }
            ENDHLSL
        }

        // ========================================
        // Pass 2: ForwardLit（原样保留）
        // ========================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

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

            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
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
                float _StealthAlpha;

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
                #ifdef _DISSOLVE_ON
                    DissolveData dissolveData;

                    #if defined(_DISSOLVE_MODE_TWOCOLOR)
                        dissolveData = CalculateDissolve_TwoColor(
                            input.uv, _DissolveThreshold,
                            _DissolveOuterColor, _DissolveInnerColor,
                            _DissolveEdgeWidth, _DissolveTex_ST
                        );
                    #elif defined(_DISSOLVE_MODE_THREECOLOR)
                        dissolveData = CalculateDissolve_ThreeColor(
                            input.uv, _DissolveThreshold,
                            _DissolveOuterColor, _DissolveMidColor, _DissolveInnerColor,
                            _DissolveEdgeWidth, _DissolveTex_ST
                        );
                    #elif defined(_DISSOLVE_MODE_RAINBOW)
                        dissolveData = CalculateDissolve_Rainbow(
                            input.uv, _DissolveThreshold,
                            _DissolveEdgeWidth, _RainbowIntensity,
                            _DissolveTex_ST
                        );
                    #else
                        dissolveData = CalculateDissolve(
                            input.uv, _DissolveThreshold,
                            _DissolveEdgeColor, _DissolveEdgeWidth,
                            _DissolveTex_ST
                        );
                    #endif

                    clip(dissolveData.alpha);
                #endif

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);

                float NdotL = saturate(dot(normalWS, lightDir));
                float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                float diff = NdotL * atten;

                float interval = 1.0 / _ToonSteps;
                float level = round(diff * _ToonSteps) / _ToonSteps;

                float ramp = interval * smoothstep(
                    level - _RampSmooth * interval * 0.5,
                    level + _RampSmooth * interval * 0.5,
                    diff
                ) + level - interval;

                ramp = max(0, ramp);
                ramp = smoothstep(_RampThreshold - 0.1, _RampThreshold + 0.1, ramp);

                float3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp);

                half3 ambient = SampleSH(normalWS) * _AmbientStrength;
                ambient *= rampColor;

                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb * _Color.rgb;

                half3 directLight = albedo * mainLight.color * rampColor;
                half3 ambientLight = albedo * ambient;
                half3 finalColor = directLight + ambientLight;

                #ifdef _DISSOLVE_ON
                    finalColor = ApplyDissolveEmission(finalColor, dissolveData);
                #endif

                return half4(finalColor, _StealthAlpha);
            }
            ENDHLSL
        }

        // ========================================
        // Pass 3: ShadowCaster（原样保留）
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
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
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

        // ========================================
        // Pass 4: DepthOnly（原样保留）
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
            #pragma shader_feature _DISSOLVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
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