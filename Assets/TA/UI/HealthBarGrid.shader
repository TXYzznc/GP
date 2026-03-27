Shader "Custom/HealthBarGrid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // 格子相关参数
        _GridLineColor ("Grid Line Color", Color) = (0,0,0,1)  // 分隔线颜色
        _GridWidth ("Grid Width (UV)", Float) = 0.1              // 单个格子的UV宽度（由代码计算传入）
        _LineWidth ("Line Width (UV)", Float) = 0.005            // 分隔线的UV宽度
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            fixed4 _Color;
            fixed4 _GridLineColor;
            float _GridWidth;
            float _LineWidth;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. 采样基础血条纹理
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                
                // 2. 计算当前像素是否在格子分隔线上
                // 用UV的x坐标取模，判断是否接近格子边界
                float gridPos = frac(i.texcoord.x / _GridWidth); // 0~1之间循环
                float distanceToLine = min(gridPos, 1 - gridPos); // 到最近格子边界的距离
                
                // 如果距离小于线宽，就绘制分隔线颜色
                if (distanceToLine < _LineWidth / _GridWidth)
                {
                    col = _GridLineColor;
                }
                
                return col;
            }
            ENDCG
        }
    }
}