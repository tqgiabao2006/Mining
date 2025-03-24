Shader "HighlightPlus/Geometry/SeeThroughOccluder" {
Properties {
    _MainTex ("Texture", Any) = "white" {}
    _Color ("Color", Color) = (1,1,1) // not used; dummy property to avoid inspector warning "material has no _Color property"
}
    SubShader
    {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }

        // Create mask
        Pass
        {
			Stencil {
                Ref 3
                WriteMask 3
                Comp always
                Pass replace
        }
            ColorMask 0
            ZWrite Off
            Offset -1, -1

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
                o.pos = UnityObjectToClipPos(v.vertex);
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