Shader "Unlit/Grid"
{
    Properties
    {
        _Scale("Scale", Float) = 1
        _Thickness("Thickness", Float) = 1
        _Color("Line Color", Color) =  (0,0,0,1)
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float _Scale;
            float _Thickness;
            float4 _Color;
            
            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolator
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };


            Interpolator vert (MeshData v)
            {
                Interpolator o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (Interpolator i) : SV_Target
            {
                float4 col = float4(i.uv * _Scale, 0,1);
                col = frac(col);
                col = step(_Thickness,col);
                return float4(col);
            }
            ENDCG
        }
    }
}
