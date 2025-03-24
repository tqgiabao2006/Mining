Shader "HighlightPlus/Geometry/JustDepth"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		ColorMask 0
		Name "Write Depth"

		Pass
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
				o.pos = ComputeVertexPosition(v.vertex);
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
