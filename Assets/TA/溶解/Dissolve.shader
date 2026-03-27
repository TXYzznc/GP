Shader "Custom/Dissolve"
{
    Properties
    {
        // 基础 PBR 属性
        _BaseMap ("Base Map (Albedo)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 2)) = 1.0
        
        _MetallicGlossMap ("Metallic Map", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        
        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0
        
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap ("Emission Map", 2D) = "white" {}
        
        // 溶解属性
        _DissolveMap ("Dissolve Noise Map", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        [HDR] _DissolveEdgeColor ("Edge Color", Color) = (1, 0.5, 0, 1)
        _DissolveEdgeWidth ("Edge Width", Range(0, 0.2)) = 0.05
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        // Forward Pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };
            
            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_OcclusionMap);   SAMPLER(sampler_OcclusionMap);
            TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_DissolveMap);    SAMPLER(sampler_DissolveMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float _Metallic;
                float _Smoothness;
                float _OcclusionStrength;
                float4 _EmissionColor;
                float4 _DissolveMap_ST;
                float _DissolveAmount;
                float4 _DissolveEdgeColor;
                float _DissolveEdgeWidth;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w);
                output.shadowCoord = GetShadowCoord(posInputs);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // 溶解裁剪
                float2 dissolveUV = input.uv * _DissolveMap_ST.xy + _DissolveMap_ST.zw;
                float dissolveNoise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, dissolveUV).r;
                float dissolveThreshold = _DissolveAmount;
                
                clip(dissolveNoise - dissolveThreshold);
                
                // 采样贴图
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv);
                half occlusion = lerp(1.0, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r, _OcclusionStrength);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb * _EmissionColor.rgb;
                
                // 法线贴图
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 TBN = float3x3(input.tangentWS.xyz, bitangent, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                
                // PBR 属性
                float metallic = metallicGloss.r * _Metallic;
                float smoothness = metallicGloss.a * _Smoothness;
                
                // 光照计算
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = input.shadowCoord;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseColor.rgb;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = emission;
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = baseColor.a;
                
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                
                // 溶解边缘发光
                float edgeFactor = 1.0 - saturate((dissolveNoise - dissolveThreshold) / _DissolveEdgeWidth);
                color.rgb += _DissolveEdgeColor.rgb * edgeFactor * step(0.001, _DissolveAmount);
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow Caster Pass
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
            
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };
            
            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_DissolveMap); SAMPLER(sampler_DissolveMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float _Metallic;
                float _Smoothness;
                float _OcclusionStrength;
                float4 _EmissionColor;
                float4 _DissolveMap_ST;
                float _DissolveAmount;
                float4 _DissolveEdgeColor;
                float _DissolveEdgeWidth;
            CBUFFER_END
            
            float3 _LightDirection;
            float3 _LightPosition;
            
            float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS)
            {
                float3 lightDirectionWS = _LightDirection;
                
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    lightDirectionWS = normalize(_LightPosition - positionWS);
                #endif
                
                // 应用法线偏移
                float invNdotL = 1.0 - saturate(dot(lightDirectionWS, normalWS));
                float scale = invNdotL * 0.01;
                positionWS = positionWS + normalWS * scale;
                
                // 应用深度偏移
                positionWS = positionWS + lightDirectionWS * 0.01;
                
                float4 positionCS = TransformWorldToHClip(positionWS);
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }
            
            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = GetShadowCasterPositionCS(positionWS, normalWS);
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                
                return output;
            }
            
            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                float2 dissolveUV = input.uv * _DissolveMap_ST.xy + _DissolveMap_ST.zw;
                float dissolveNoise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, dissolveUV).r;
                clip(dissolveNoise - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }
        
        // Depth Only Pass
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
            
            TEXTURE2D(_DissolveMap); SAMPLER(sampler_DissolveMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float _Metallic;
                float _Smoothness;
                float _OcclusionStrength;
                float4 _EmissionColor;
                float4 _DissolveMap_ST;
                float _DissolveAmount;
                float4 _DissolveEdgeColor;
                float _DissolveEdgeWidth;
            CBUFFER_END
            
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return output;
            }
            
            half4 DepthFrag(Varyings input) : SV_Target
            {
                float2 dissolveUV = input.uv * _DissolveMap_ST.xy + _DissolveMap_ST.zw;
                float dissolveNoise = SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, dissolveUV).r;
                clip(dissolveNoise - _DissolveAmount);
                return 0;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}
