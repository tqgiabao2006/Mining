Shader "HighlightPlus/ClearStencil"
{
	Properties
	{
	}
	SubShader
	{
		Stencil {
			Ref 2
			Comp Always
			Pass zero
		}
		ZTest Always
		ZWrite Off
		Cull Off
		ColorMask 0

		Pass // clear stencil full screen
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct MeshData
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Interpolator
			{
				float4 pos : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			Interpolator vert (MeshData v)
			{
				Interpolator o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(Interpolator, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = float4(v.vertex.xy, 0, 0.5);
				return o;
			}
			
			fixed4 frag (Interpolator i) : SV_Target
			{
				return 0;
			}
			ENDCG
		}

		Pass // clear stencil object-space
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
            #include "CustomVertexTransform.cginc"

			struct MeshData
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Interpolator
			{
				float4 pos : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};


			Interpolator vert (MeshData v)
			{
				Interpolator o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_OUTPUT(Interpolator, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos    = ComputeVertexPosition(v.vertex);
				return o;
			}
			
			fixed4 frag (Interpolator i) : SV_Target
			{
				return 0;
			}
			ENDCG
		}
	}
}
