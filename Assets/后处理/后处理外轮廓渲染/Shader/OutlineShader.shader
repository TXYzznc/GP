Shader "Hidden/Outline/OutlineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SceneTex ("Scene Texture", 2D) = "white" {}
        _Color ("Outline Color", Color) = (1, 0, 0, 1)
        _Width ("Outline Width", Int) = 4
        _Iterations ("Iterations", Int) = 3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        // Pass 0: 边缘检测
        Pass
        {
            Name "EdgeDetection"
            
            ZTest Always
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            int _Width;
            int _Iterations;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 center = tex2D(_MainTex, i.uv);
                
                // 如果中心像素已经是白色（物体内部），不需要绘制轮廓
                if (center.r > 0.5)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                // 边缘检测 - 检查周围像素
                float outline = 0.0;
                float totalSamples = 0.0;
                
                // 限制最大迭代次数为 5
                int maxIter = min(_Iterations, 5);
                
                [unroll(5)]
                for (int iter = 1; iter <= maxIter; iter++)
                {
                    float offset = _Width * iter / (float)maxIter;
                    
                    // 8方向采样
                    [unroll]
                    for (int j = 0; j < 8; j++)
                    {
                        float2 sampleUV = i.uv;
                        
                        // 手动展开 8 个方向
                        if (j == 0) sampleUV += float2(offset, 0) * _MainTex_TexelSize.xy;
                        else if (j == 1) sampleUV += float2(-offset, 0) * _MainTex_TexelSize.xy;
                        else if (j == 2) sampleUV += float2(0, offset) * _MainTex_TexelSize.xy;
                        else if (j == 3) sampleUV += float2(0, -offset) * _MainTex_TexelSize.xy;
                        else if (j == 4) sampleUV += float2(offset, offset) * _MainTex_TexelSize.xy;
                        else if (j == 5) sampleUV += float2(-offset, offset) * _MainTex_TexelSize.xy;
                        else if (j == 6) sampleUV += float2(offset, -offset) * _MainTex_TexelSize.xy;
                        else if (j == 7) sampleUV += float2(-offset, -offset) * _MainTex_TexelSize.xy;
                        
                        fixed4 sample = tex2D(_MainTex, sampleUV);
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
                
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
