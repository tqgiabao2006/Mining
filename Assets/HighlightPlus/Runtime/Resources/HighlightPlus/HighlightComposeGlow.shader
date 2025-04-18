Shader "HighlightPlus/Geometry/ComposeGlow" {
Properties {
    _MainTex ("Texture", Any) = "black" {}
    _Color ("Color", Color) = (1,1,1) // not used; dummy property to avoid inspector warning "material has no _Color property"
    [HideInInspector] _Cull ("Cull Mode", Int) = 2
    [HideInInspector] _ZTest ("ZTest Mode", Int) = 0
	[HideInInspector] _Flip("Flip", Vector) = (0, 1, 0)
	[HideInInspector] _BlendSrc("Blend Src", Int) = 1
	[HideInInspector] _BlendDst("Blend Dst", Int) = 1
	_Debug("Debug Color", Color) = (0,0,0,0)
    [HideInInspector] _GlowStencilComp ("Stencil Comp", Int) = 6
}
    SubShader
    {
        Tags { "Queue"="Transparent+102" "RenderType"="Transparent" "DisableBatching" = "True" }
        Blend [_BlendSrc] [_BlendDst]

        // Compose effect on camera target
        Pass
        {
            ZWrite Off
			ZTest Always // [_ZTest]
			Cull Off //[_Cull]
        	Stencil {
                Ref 2
                Comp [_GlowStencilComp]
                Pass keep 
				ReadMask 2
				WriteMask 2
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_local _ HP_MASK_CUTOUT

            #include "UnityCG.cginc"

			UNITY_DECLARE_SCREENSPACE_TEXTURE(_HPComposeGlowFinal);
			UNITY_DECLARE_SCREENSPACE_TEXTURE(_HPSourceRT);
			float4 _HPSourceRT_TexelSize;

			fixed4 _Color;
			float3 _Flip;
			fixed4 _Debug;

            struct MeshData
            {
                float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolator
            {
				float4 pos: SV_POSITION;
				float4 scrPos: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

            Interpolator vert (MeshData v)
            {
				Interpolator o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(Interpolator, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.scrPos = ComputeScreenPos(o.pos);
				o.scrPos.y = o.scrPos.w * _Flip.x + o.scrPos.y * _Flip.y;
				return o;
            }
            
            fixed4 frag (Interpolator i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				float2 uv = i.scrPos.xy/i.scrPos.w;
            	fixed glow = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HPComposeGlowFinal, uv).r;

				#if HP_MASK_CUTOUT
					fixed maskN = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HPSourceRT, uv + float2(0, 1) * _HPSourceRT_TexelSize.xy).r;
					fixed maskS = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HPSourceRT, uv + float2(0, -1) * _HPSourceRT_TexelSize.xy).r;
					fixed maskW = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HPSourceRT, uv + float2(-1, 0) * _HPSourceRT_TexelSize.xy).r;
					fixed maskE = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_HPSourceRT, uv + float2(1, 0) * _HPSourceRT_TexelSize.xy).r;
					glow *= maskN == 0 || maskS == 0 || maskW == 0 || maskE == 0;
				#endif

            	fixed4 color = _Color;
            	color *= glow;
				

				color += _Debug;
                color.a = saturate(color.a);
            	return color;
            }
            ENDCG
        }

		// Compose effect on camera target (full-screen blit)
			Pass
				{
					ZWrite Off
					ZTest Always //[_ZTest]
					Cull Off //[_Cull]

					Stencil {
						Ref 2
						Comp NotEqual
						Pass keep
						ReadMask 2
						WriteMask 2
					}

					CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag

					#include "UnityCG.cginc"

					UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
					float4 _MainTex_ST;
					fixed4 _Color;
					float3 _Flip;

					struct MeshData
					{
						float4 vertex : POSITION;
						float2 uv     : TEXCOORD0;
						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct Interpolator
					{
						float4 pos: SV_POSITION;
						float2 uv     : TEXCOORD0;
						UNITY_VERTEX_INPUT_INSTANCE_ID
						UNITY_VERTEX_OUTPUT_STEREO
					};

					Interpolator vert(MeshData v)
					{
						Interpolator o;
						UNITY_SETUP_INSTANCE_ID(v);
						UNITY_INITIALIZE_OUTPUT(Interpolator, o);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
						o.pos = UnityObjectToClipPos(v.vertex);
						o.uv = UnityStereoScreenSpaceUVAdjust(v.uv, _MainTex_ST);
						o.uv.y = _Flip.x + o.uv.y * _Flip.y;
						return o;
					}

					fixed4 frag(Interpolator i) : SV_Target
					{
						UNITY_SETUP_INSTANCE_ID(i);
						UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
						fixed4 glow = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
						fixed4 color = _Color;
						color *= glow.r;
                        color.a = saturate(color.a);
						return color;
					}
					ENDCG
				}

    }
}