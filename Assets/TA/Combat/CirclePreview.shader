Shader "Custom/CirclePreview"
{
    Properties
    {
        _Color ("Circle Color", Color) = (0, 0.5, 1, 0.5)
        _Radius ("Radius (0-1)", Float) = 1.0
        _Softness ("Edge Softness", Float) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual

        Pass
        {
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;
            float _Radius;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 转换UV坐标到[-1, 1]范围（中心为0）
                float2 uv = (i.uv - 0.5) * 2.0;

                // 计算到中心的距离
                float dist = length(uv);

                // 使用 smoothstep 创建圆形边界（带软边）
                float circle = smoothstep(_Radius + _Softness, _Radius - _Softness, dist);

                // 应用颜色和透明度
                fixed4 col = _Color;
                col.a *= circle;

                return col;
            }
            ENDCG
        }
    }

    Fallback "Unlit/Color"
}
