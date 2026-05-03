
Shader "Custom/BuildingCartoon"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex("主贴图 (Albedo)", 2D) = "white" {}
        _Color("主颜色", Color) = (1, 1, 1, 1)

        [Header(Detail Map)]
        [Toggle(USE_DETAIL)] _UseDetail("启用细节贴图", Float) = 0
        _DetailTex("细节贴图", 2D) = "gray" {}
        _DetailStrength("细节强度", Range(0, 1)) = 0.5

        [Header(Normal Map)]
        [Toggle(USE_NORMAL)] _UseNormal("启用法线贴图", Float) = 0
        [Normal] _BumpMap("法线贴图", 2D) = "bump" {}
        _BumpScale("法线强度", Range(0, 2)) = 1.0

        [Header(Ambient Occlusion)]
        [Toggle(USE_AO)] _UseAO("启用AO贴图", Float) = 0
        _OcclusionMap("AO贴图", 2D) = "white" {}
        _OcclusionStrength("AO强度", Range(0, 1)) = 1.0

        [Header(Emission)]
        [Toggle(USE_EMISSION)] _UseEmission("启用自发光", Float) = 0
        [HDR] _EmissionColor("自发光颜色", Color) = (0, 0, 0, 1)
        _EmissionMap("自发光贴图 (Mask)", 2D) = "black" {}

        [Header(Vertex Color)]
        [Toggle(USE_VERTEX_COLOR)] _UseVertexColor("启用顶点色AO", Float) = 0
        _VertexColorStrength("顶点色强度", Range(0, 1)) = 1.0

        [Header(Rendering Mode)]
        [Toggle(USE_PBR)] _UsePBR("使用PBR渲染（关闭=卡通风格）", Float) = 0

        [Header(PBR Settings)]
        _MetallicGlossMap("金属度贴图 (R=金属 A=光滑)", 2D) = "white" {}
        _Metallic("金属度", Range(0, 1)) = 0.0
        _Smoothness("光滑度", Range(0, 1)) = 0.5

        [Header(Toon Shading)]
        _RampThreshold("色阶阈值", Range(0, 1)) = 0.5
        _RampSmooth("平滑过渡", Range(0, 1)) = 0.1
        _ToonSteps("色阶层数", Range(1, 5)) = 2
        _HColor("高光颜色", Color) = (0.9, 0.9, 0.85, 1.0)
        _SColor("阴影颜色", Color) = (0.15, 0.2, 0.3, 1.0)
        _AmbientStrength("环境光强度", Range(0, 5)) = 0.5

        [Header(Outline Settings)]
        _OutlineWidth("描边宽度", Range(0, 0.1)) = 0.008
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineZOffset("Z偏移", Range(-1, 0)) = -0.001
        [Toggle(USE_SMOOTH_NORMAL)] _UseSmoothNormal("使用平滑法线", Float) = 0

        [Header(Outline Distance Control)]
        [Toggle(USE_DISTANCE_OUTLINE)] _UseDistanceOutline("启用距离控制描边", Float) = 0
        _OutlineWidthMax("最粗描边", Range(0, 0.2)) = 0.02
        _OutlineDistanceMax("最粗描边距离", Range(0, 50)) = 3.0
        _OutlineWidthMin("最细描边", Range(0, 0.1)) = 0.002
        _OutlineDistanceMin("最细描边距离", Range(0, 100)) = 25.0
        _OutlineDistanceCurve("变化曲线", Range(0.1, 5)) = 1.0

        [Header(Dissolve Effect)]
        [Toggle(_DISSOLVE_ON)] _DissolveEnabled("启用溶解", Float) = 0
        _DissolveTex("溶解噪声贴图", 2D) = "white" {}
        _DissolveAmount("溶解程度", Range(0, 1.1)) = 0
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
        // Pass 1: Outline
        // ========================================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature USE_SMOOTH_NORMAL
            #pragma shader_feature USE_DISTANCE_OUTLINE
            #pragma shader_feature _DISSOLVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                float4 _OutlineColor;
                float _OutlineZOffset;

                #ifdef USE_DISTANCE_OUTLINE
                    float _OutlineWidthMax;
                    float _OutlineDistanceMax;
                    float _OutlineWidthMin;
                    float _OutlineDistanceMin;
                    float _OutlineDistanceCurve;
                #endif

                #ifdef _DISSOLVE_ON
                    float _DissolveAmount;
                    float4 _DissolveTex_ST;
                #endif
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
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

                float finalOutlineWidth = _OutlineWidth;

                #ifdef USE_DISTANCE_OUTLINE
                {
                    float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                    float dist   = distance(posWS, _WorldSpaceCameraPos);
                    float denom  = max(1e-5, _OutlineDistanceMin - _OutlineDistanceMax);
                    float t      = pow(saturate((dist - _OutlineDistanceMax) / denom), _OutlineDistanceCurve);
                    finalOutlineWidth = lerp(_OutlineWidthMax, _OutlineWidthMin, t);
                }
                #endif

                float4 clipPos   = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS  = TransformWorldToHClipDir(TransformObjectToWorldNormal(smoothNormal));
                float2 offset    = normalize(normalCS.xy);
                clipPos.xy      += offset * finalOutlineWidth * clipPos.w;
                clipPos.z       += _OutlineZOffset * clipPos.w;
                output.positionCS = clipPos;

                #ifdef _DISSOLVE_ON
                    output.uv = input.uv;
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                #ifdef _DISSOLVE_ON
                    float dissolveAlpha = GetDissolveAlpha(input.uv, _DissolveAmount, _DissolveTex_ST);
                    clip(dissolveAlpha);
                #endif
                return half4(_OutlineColor.rgb, _OutlineColor.a);
            }
            ENDHLSL
        }

        // ========================================
        // Pass 2: ForwardLit
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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma shader_feature USE_PBR
            #pragma shader_feature USE_DETAIL
            #pragma shader_feature USE_NORMAL
            #pragma shader_feature USE_AO
            #pragma shader_feature USE_EMISSION
            #pragma shader_feature USE_VERTEX_COLOR
            #pragma shader_feature _DISSOLVE_ON
            #pragma shader_feature _DISSOLVE_MODE_SINGLE _DISSOLVE_MODE_TWOCOLOR _DISSOLVE_MODE_THREECOLOR _DISSOLVE_MODE_RAINBOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
            #endif

            // ── 贴图声明 ──────────────────────────────────────────────────
            TEXTURE2D(_MainTex);           SAMPLER(sampler_MainTex);
            TEXTURE2D(_MetallicGlossMap);  SAMPLER(sampler_MetallicGlossMap);

            #ifdef USE_DETAIL
                TEXTURE2D(_DetailTex);     SAMPLER(sampler_DetailTex);
            #endif
            #ifdef USE_NORMAL
                TEXTURE2D(_BumpMap);       SAMPLER(sampler_BumpMap);
            #endif
            #ifdef USE_AO
                TEXTURE2D(_OcclusionMap);  SAMPLER(sampler_OcclusionMap);
            #endif
            #ifdef USE_EMISSION
                TEXTURE2D(_EmissionMap);   SAMPLER(sampler_EmissionMap);
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float4 _DetailTex_ST;
                float  _DetailStrength;

                float  _BumpScale;
                float  _OcclusionStrength;
                float4 _EmissionColor;
                float  _VertexColorStrength;

                // PBR
                float  _Metallic;
                float  _Smoothness;

                // Toon
                float  _RampThreshold;
                float  _RampSmooth;
                float  _ToonSteps;
                float4 _HColor;
                float4 _SColor;
                float  _AmbientStrength;

                // Dissolve
                #ifdef _DISSOLVE_ON
                    float  _DissolveAmount;
                    float4 _DissolveTex_ST;
                    float4 _DissolveEdgeColor;
                    float4 _DissolveOuterColor;
                    float4 _DissolveMidColor;
                    float4 _DissolveInnerColor;
                    float  _DissolveEdgeWidth;
                    float  _RainbowIntensity;
                #endif
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float4 tangentWS   : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
                float4 vertexColor : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs    = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS  = posInputs.positionCS;
                output.positionWS  = posInputs.positionWS;
                output.uv          = TRANSFORM_TEX(input.uv, _MainTex);
                output.normalWS    = normalInputs.normalWS;
                output.tangentWS   = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.shadowCoord = GetShadowCoord(posInputs);
                output.vertexColor = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ── 溶解裁剪 ──────────────────────────────────────────────
                #ifdef _DISSOLVE_ON
                DissolveData dissolveData = (DissolveData)0;
                if (_DissolveAmount > 0.001)
                {
                    #if defined(_DISSOLVE_MODE_TWOCOLOR)
                        dissolveData = CalculateDissolve_TwoColor(input.uv, _DissolveAmount,
                            _DissolveOuterColor, _DissolveInnerColor, _DissolveEdgeWidth, _DissolveTex_ST);
                    #elif defined(_DISSOLVE_MODE_THREECOLOR)
                        dissolveData = CalculateDissolve_ThreeColor(input.uv, _DissolveAmount,
                            _DissolveOuterColor, _DissolveMidColor, _DissolveInnerColor, _DissolveEdgeWidth, _DissolveTex_ST);
                    #elif defined(_DISSOLVE_MODE_RAINBOW)
                        dissolveData = CalculateDissolve_Rainbow(input.uv, _DissolveAmount,
                            _DissolveEdgeWidth, _RainbowIntensity, _DissolveTex_ST);
                    #else
                        dissolveData = CalculateDissolve(input.uv, _DissolveAmount,
                            _DissolveEdgeColor, _DissolveEdgeWidth, _DissolveTex_ST);
                    #endif
                    clip(dissolveData.alpha);
                }
                #endif

                // ── 法线 ──────────────────────────────────────────────────
                float3 normalWS = normalize(input.normalWS);
                #ifdef USE_NORMAL
                {
                    half3 normalTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                    float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                    float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                    normalWS = normalize(mul(normalTS, TBN));
                }
                #endif

                // ── Albedo ────────────────────────────────────────────────
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;

                #ifdef USE_DETAIL
                {
                    float2 detailUV = TRANSFORM_TEX(input.uv, _DetailTex);
                    half3 detail    = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, detailUV).rgb;
                    // overlay 混合：detail 0.5 = 无变化
                    baseColor.rgb   = lerp(baseColor.rgb, baseColor.rgb * detail * 2.0, _DetailStrength);
                }
                #endif

                // ── 顶点色 AO ─────────────────────────────────────────────
                #ifdef USE_VERTEX_COLOR
                    baseColor.rgb *= lerp(1.0, input.vertexColor.rgb, _VertexColorStrength);
                #endif

                // ── AO 贴图 ───────────────────────────────────────────────
                half occlusion = 1.0;
                #ifdef USE_AO
                    occlusion = lerp(1.0,
                        SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r,
                        _OcclusionStrength);
                #endif

                // ── 自发光 ────────────────────────────────────────────────
                half3 emission = 0;
                #ifdef USE_EMISSION
                {
                    half emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).r;
                    emission = _EmissionColor.rgb * emissionMask;
                }
                #endif

                // ── 主光源 ────────────────────────────────────────────────
                Light mainLight = GetMainLight(input.shadowCoord);

                half3 finalColor;

                // ── PBR 分支 ──────────────────────────────────────────────
                #ifdef USE_PBR
                {
                    half4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
                    float metallic   = metallicGloss.r * _Metallic;
                    float smoothness = metallicGloss.a * _Smoothness;

                    InputData inputData = (InputData)0;
                    inputData.positionWS            = input.positionWS;
                    inputData.normalWS              = normalWS;
                    inputData.viewDirectionWS       = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    inputData.shadowCoord           = input.shadowCoord;
                    inputData.bakedGI               = SampleSH(normalWS) * occlusion;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                    SurfaceData surfaceData = (SurfaceData)0;
                    surfaceData.albedo      = baseColor.rgb;
                    surfaceData.metallic    = metallic;
                    surfaceData.smoothness  = smoothness;
                    surfaceData.normalTS    = half3(0, 0, 1);
                    surfaceData.emission    = emission;
                    surfaceData.occlusion   = occlusion;
                    surfaceData.alpha       = baseColor.a;

                    half4 pbr = UniversalFragmentPBR(inputData, surfaceData);
                    finalColor = pbr.rgb;
                }
                // ── Toon 分支 ─────────────────────────────────────────────
                #else
                {
                    float3 lightDir = normalize(mainLight.direction);
                    float  NdotL    = saturate(dot(normalWS, lightDir));
                    float  atten    = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                    float  diff     = NdotL * atten;

                    float interval  = 1.0 / _ToonSteps;
                    float level     = round(diff * _ToonSteps) / _ToonSteps;
                    float ramp      = interval * smoothstep(
                        level - _RampSmooth * interval * 0.5,
                        level + _RampSmooth * interval * 0.5,
                        diff) + level - interval;
                    ramp = max(0, ramp);
                    ramp = smoothstep(_RampThreshold - 0.1, _RampThreshold + 0.1, ramp);

                    float3 rampColor  = lerp(_SColor.rgb, _HColor.rgb, ramp);
                    half3  ambient    = SampleSH(normalWS) * _AmbientStrength * occlusion;
                    ambient          *= rampColor;

                    half3 directLight  = baseColor.rgb * mainLight.color * rampColor;
                    half3 ambientLight = baseColor.rgb * ambient;
                    finalColor = directLight + ambientLight + emission;
                }
                #endif

                // ── 溶解发光叠加 ──────────────────────────────────────────
                #ifdef _DISSOLVE_ON
                if (_DissolveAmount > 0.001)
                    finalColor = ApplyDissolveEmission(finalColor, dissolveData);
                #endif

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // ========================================
        // Pass 3: ShadowCaster
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
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature _DISSOLVE_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                #ifdef _DISSOLVE_ON
                    float  _DissolveAmount;
                    float4 _DissolveTex_ST;
                #endif
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
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

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 posWS    = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float invNdotL = 1.0 - saturate(dot(lightDir, normalWS));
                posWS += normalWS * invNdotL * 0.1;

                output.positionCS = TransformWorldToHClip(posWS);
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

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                #ifdef _DISSOLVE_ON
                    clip(GetDissolveAlpha(input.uv, _DissolveAmount, _DissolveTex_ST));
                #endif
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
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature _DISSOLVE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #ifdef _DISSOLVE_ON
                #include "Assets/TA/Cartoon/Modules/DissolveModule.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                #ifdef _DISSOLVE_ON
                    float  _DissolveAmount;
                    float4 _DissolveTex_ST;
                #endif
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                #ifdef _DISSOLVE_ON
                    float2 uv : TEXCOORD0;
                #endif
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                #ifdef _DISSOLVE_ON
                    output.uv = input.uv;
                #endif
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                #ifdef _DISSOLVE_ON
                    clip(GetDissolveAlpha(input.uv, _DissolveAmount, _DissolveTex_ST));
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
