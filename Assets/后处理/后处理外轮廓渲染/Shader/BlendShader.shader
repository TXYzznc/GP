Shader "Custom/Outline/BlendShader"
{
    Properties
    {
        _MainTex ("Scene Texture", 2D) = "white" {}
        _OutlineTex ("Outline Texture", 2D) = "black" {}
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        // Pass 0: 混合轮廓和场景
        Pass
        {
            Name "BlendOutline"
            
            ZTest Always
            ZWrite Off
            Cull Off
            
            Blend SrcAlpha OneMinusSrcAlpha

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
            sampler2D _OutlineTex;
            float4 _MainTex_ST;
            fixed4 _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 采样场景纹理
                fixed4 sceneColor = tex2D(_MainTex, i.uv);
                
                // 采样轮廓纹理
                fixed4 outlineColor = tex2D(_OutlineTex, i.uv);
                
                // 混合轮廓和场景
                // 如果轮廓纹理有颜色，使用轮廓颜色，否则使用场景颜色
                float outlineStrength = outlineColor.a;
                
                // 使用 lerp 混合
                fixed4 finalColor = lerp(sceneColor, outlineColor, outlineStrength);
                
                return finalColor;
            }
            ENDCG
        }
    }
}
