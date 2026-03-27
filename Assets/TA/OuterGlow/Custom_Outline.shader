Shader "Custom/Outline"
{
	Properties
	{
		_OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
		_Clip("Clip", Float) = 0
		_Intensity("Intensity", Float) = 1
		_ExpPower("Exp Power", Float) = 1
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
			Name "Forward"
			Tags
			{
				"LightMode" = "UniversalForward"
			}
			
			HLSLPROGRAM

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#pragma vertex vert
			#pragma fragment frag

			struct appdata
            {
                float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            	float2 uv : TEXCOORD0;
            	float4 screenPos : TEXCOORD1;
            };

			Varyings vert(appdata input)
			{
				Varyings outPut;
				outPut.positionCS = TransformObjectToHClip(input.positionOS);
				outPut.uv = input.uv;
				outPut.screenPos = ComputeScreenPos(outPut.positionCS);
				return outPut;
			}

			TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);
			float _Clip;

			half4 frag(Varyings input) : SV_Target
			{
				float2 screenUV = UnityStereoTransformScreenSpaceTex(input.screenPos.xy / input.screenPos.w);
				float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV).r;
				depth = LinearEyeDepth(depth, _ZBufferParams);
				float keep = depth + _Clip - input.screenPos.w;
				keep = step(0, keep);
				return half4(keep, 1, 0, 1);
			}
			
			ENDHLSL
		}
		
		Pass
		{
			Name "Outline"

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
            {
                float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            	float2 uv : TEXCOORD0;
            };

			Varyings vert(appdata input)
			{
				Varyings outPut;
				outPut.positionCS = TransformObjectToHClip(input.positionOS);
				outPut.uv = input.uv;
				return outPut;
			}

			TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
			TEXTURE2D(_BlurTex); SAMPLER(sampler_BlurTex);

			half4 frag(Varyings input) : SV_Target
			{
				half4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
				half4 outline = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, input.uv);
				half clipped = outline.g - outline.r;
				clipped = saturate(outline.r - clipped - base.g);
				return half4(clipped, 0, 0, 1);
			}
			
			ENDHLSL
		}

		Pass
		{
			Name "AddOutline"
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct appdata
            {
                float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            	float2 uv : TEXCOORD0;
            };

			Varyings vert(appdata input)
			{
				Varyings outPut;
				outPut.positionCS = TransformObjectToHClip(input.positionOS);
				outPut.uv = input.uv;
				return outPut;
			}

			TEXTURE2D(_OutlineTex); SAMPLER(sampler_OutlineTex);
			half4 _OutlineColor;
			float _Intensity;
			float _ExpPower;

			half4 frag(Varyings input) : SV_Target
			{
				half4 base = SAMPLE_TEXTURE2D(_OutlineTex, sampler_OutlineTex, input.uv);
				float alpha = saturate(pow(base.r * _Intensity, _ExpPower));
				return half4(_OutlineColor.rgb, alpha);
			}
			
			ENDHLSL
		}
	}
}