Shader "Custom/PlayerCartoon"
{
    Properties
    {
        _MainTex ("主纹理", 2D) = "white" {}
        _Color("主颜色", Color) = (1, 1, 1, 1)
        
        _RampThreshold("明暗分界线", Range(0, 1)) = 0.5
        _RampSmooth("过渡平滑度", Range(0, 1)) = 0.1
        _HColor ("高光颜色", Color) = (0.8, 0.8, 0.8, 1.0)
        _SColor ("阴影颜色", Color) = (0.2, 0.2, 0.2, 1.0)
        _AmbientStrength("环境光强度", Range(0, 1)) = 0.3
        
        [Toggle(ENABLE_SPECULAR)] _EnableSpecular("启用高光", Float) = 1
        _SpecularColor("高光颜色", Color) = (1, 1, 1, 1)
        _SpecularGloss("光泽度", Range(0, 1)) = 0.8
        _SpecularThreshold("高光阈值", Range(0, 1)) = 0.9
        _SpecularSmooth("高光平滑度", Range(0, 0.1)) = 0.01
        
        [Toggle(ENABLE_RIM)] _EnableRim("启用边缘光", Float) = 1
        _RimColor("边缘光颜色", Color) = (1, 1, 1, 1)
        _RimPower("边缘光范围", Range(0, 10)) = 3
        _RimThreshold("边缘光阈值", Range(0, 1)) = 0.7
        _RimSmooth("边缘光平滑度", Range(0, 0.5)) = 0.1
        
        [Toggle(ENABLE_OUTLINE)] _EnableOutline("启用描边", Float) = 1
        _OutlineColor("描边颜色", Color) = (0, 0, 0, 1)
        _OutlineWidth("描边宽度", Range(0, 0.1)) = 0.005
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200
        
        // ========================================
        // Pass 1: 描边 (条件编译)
        // ========================================
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }
            
            // ✅ 条件编译：只有启用描边时才渲染
            Cull Front
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature ENABLE_OUTLINE  // ✅ 着色器变体
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            float _OutlineWidth;
            float4 _OutlineColor;
            
            v2f vert(appdata v)
            {
                v2f o;
                
                #ifdef ENABLE_OUTLINE
                    // 沿法线外扩
                    float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                    float4 clipPos = UnityObjectToClipPos(v.vertex);
                    float3 clipNormal = mul((float3x3)UNITY_MATRIX_P, norm);
                    clipPos.xy += clipNormal.xy * _OutlineWidth * clipPos.w;
                    o.pos = clipPos;
                #else
                    // 不启用时：退化到原点（不可见）
                    o.pos = float4(0, 0, 0, 1);
                #endif
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                #ifdef ENABLE_OUTLINE
                    return _OutlineColor;
                #else
                    discard;  // 完全丢弃片段
                    return fixed4(0, 0, 0, 0);
                #endif
            }
            ENDCG
        }
        
        // ========================================
        // Pass 2: 主渲染
        // ========================================
        Pass
        {
            Name "FORWARD"
            Tags {"LightMode" = "ForwardBase"}
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            // ✅ 着色器变体：根据Toggle生成不同版本
            #pragma shader_feature ENABLE_SPECULAR
            #pragma shader_feature ENABLE_RIM
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                
                #if defined(ENABLE_SPECULAR) || defined(ENABLE_RIM)
                    float3 viewDir : TEXCOORD3;  // 只在需要时传递
                #endif
                
                SHADOW_COORDS(4)
            };
            
            // 基础变量
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _RampThreshold;
            float _RampSmooth;
            float4 _HColor;
            float4 _SColor;
            float _AmbientStrength;
            
            // 高光变量
            #ifdef ENABLE_SPECULAR
                float4 _SpecularColor;
                float _SpecularGloss;
                float _SpecularThreshold;
                float _SpecularSmooth;
            #endif
            
            // 边缘光变量
            #ifdef ENABLE_RIM
                float4 _RimColor;
                float _RimPower;
                float _RimThreshold;
                float _RimSmooth;
            #endif
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                #if defined(ENABLE_SPECULAR) || defined(ENABLE_RIM)
                    o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                #endif
                
                TRANSFER_SHADOW(o);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // ========== 1. 基础光照 ==========
                #if defined(USING_DIRECTIONAL_LIGHT)
                    fixed3 lightDir = _WorldSpaceLightPos0.xyz;
                #else
                    fixed3 lightDir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
                #endif
                
                float3 worldNormal = normalize(i.worldNormal);
                float NdotL = max(0, dot(worldNormal, lightDir));
                
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                NdotL *= atten;
                
                // ========== 2. 卡通化漫反射 ==========
                float ramp = smoothstep(
                    _RampThreshold - _RampSmooth * 0.5,
                    _RampThreshold + _RampSmooth * 0.5,
                    NdotL
                );
                float3 rampColor = lerp(_SColor.rgb, _HColor.rgb, ramp);
                
                // ========== 3. 环境光 ==========
                fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * _AmbientStrength;
                
                // ========== 4. 高光 (条件编译) ==========
                fixed3 specular = fixed3(0, 0, 0);
                fixed3 rim = fixed3(0, 0, 0);
                
                // ✅ 修复：只定义一次 viewDir
                #if defined(ENABLE_SPECULAR) || defined(ENABLE_RIM)
                    float3 viewDir = normalize(i.viewDir);
                #endif
                
                #ifdef ENABLE_SPECULAR
                    float3 halfDir = normalize(lightDir + viewDir);
                    float NdotH = max(0, dot(worldNormal, halfDir));
                    
                    float specularIntensity = pow(NdotH, _SpecularGloss * 128);
                    float specularRamp = smoothstep(
                        _SpecularThreshold - _SpecularSmooth,
                        _SpecularThreshold + _SpecularSmooth,
                        specularIntensity
                    );
                    
                    specular = _SpecularColor.rgb * specularRamp * _LightColor0.rgb;
                #endif
                
                #ifdef ENABLE_RIM
                    float NdotV = max(0, dot(worldNormal, viewDir));
                    float rimIntensity = pow(1.0 - NdotV, _RimPower);
                    
                    float rimRamp = smoothstep(
                        _RimThreshold - _RimSmooth,
                        _RimThreshold + _RimSmooth,
                        rimIntensity
                    );
                    
                    rim = _RimColor.rgb * rimRamp;
                #endif
                
                // ========== 5. 最终合成 ==========
                fixed3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                fixed3 diffuse = albedo * _LightColor0.rgb * rampColor;
                fixed3 ambientLight = albedo * ambient;
                
                fixed3 finalColor = diffuse + ambientLight + specular + rim;
                
                return fixed4(finalColor, 1);
            }
            ENDCG
        }
        
        // ========================================
        // Pass 3: 阴影投射
        // ========================================
        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            
            struct v2f {
                V2F_SHADOW_CASTER;
            };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
    CustomEditor "ToonShaderGUI"  // ✅ 可选：自定义编辑器
}