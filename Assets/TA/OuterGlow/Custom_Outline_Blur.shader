Shader "Custom/OutlineBlur"
{
	Properties
	{
		_BlurSize ("Blur Size", Float) = 1.0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
		}

		Pass
		{
			Name "Blur"
			
			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
	        float4 _MainTex_TexelSize;
			float _BlurSize;

			#pragma vertex vert
			#pragma fragment frag

			struct appdata
            {
                float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
            };

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv[9]: TEXCOORD0;
			};

			TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);

			float4 GetGaussBlurColorFast(float2 uv[9])
			{
				float3 weight = float3(0.0453, 0.0566, 0.0707);
				float weightTotal = 0.4783; 
				
				float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[0]) * weight.z;
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[1]) * weight.y;
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[2]) * weight.y;
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[3]) * weight.y;
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[4]) * weight.y;
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[5]) * weight.x;
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[6]) * weight.x;					
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[7]) * weight.x;
				color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv[8]) * weight.x;
				color = clamp(color,0,1);
				return float4(color.rgb / weightTotal, 1.0);
			}

			v2f vert( appdata v ) {
				v2f o;
				o.pos = TransformObjectToHClip(v.positionOS.xyz);
				o.uv[0] = v.uv.xy;
				o.uv[1] = o.uv[0] + float2(0, _MainTex_TexelSize.y) * _BlurSize;
				o.uv[2] = o.uv[0] - float2(0, _MainTex_TexelSize.y) * _BlurSize;
				o.uv[3] = o.uv[0] + float2(_MainTex_TexelSize.x, 0) * _BlurSize;
				o.uv[4] = o.uv[0] - float2(_MainTex_TexelSize.x, 0) * _BlurSize;
				o.uv[5] = o.uv[0] + float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * _BlurSize;
				o.uv[6] = o.uv[0] - float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * _BlurSize;
				o.uv[7] = o.uv[0] + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _BlurSize;
				o.uv[8] = o.uv[0] - float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _BlurSize;
				return o;
			} 

			half4 frag(v2f i) : SV_Target 
			{
				return GetGaussBlurColorFast(i.uv);
			}
			
			ENDHLSL
		}
	}
}