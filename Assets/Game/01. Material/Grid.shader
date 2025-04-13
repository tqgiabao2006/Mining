Shader "Unlit/GridSmooth"
{
    Properties
    {
        _Pivot("Inner bound Bottom Left Pivot", Vector) = (0,0,0,0)
        _Size("Rectangle Size", Vector) = (0,0,0,0)
        _Width("Line width", Range(0,1)) = 0.1
        _Line("Number of lines", Int) = 10
        _Color("Line color", Color) = (1,1,1,1)
        _Thickness("Inside line thickness", Range(0,1)) = 0.5
        _NodeRadius("Node radius", Float) = 0.5
        
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Pivot;
            float4 _Size;
            float _Width;
            int _Line;
            float4 _Color;
            float4 _BGColor;
            float _Thickness;
            float _NodeRadius;

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolator
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            Interpolator vert (MeshData v)
            {
                Interpolator o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            bool IsInRectangle(float2 pivot, float2 size, float2 input)
            {
                return (input.x > pivot.x && input.x < pivot.x + size.x &&
                        input.y > pivot.y && input.y < pivot.y + size.y);
            }

            fixed4 frag (Interpolator i) : SV_Target
            {
                float2 size = _Size.xy;
                float2 pivot = _Pivot.xy;

                float2 inPivot = pivot + _Thickness;
                float2 inSize = size - 2.0 * _Thickness;

                bool inInner = IsInRectangle(inPivot, inSize, i.worldPos);
                bool inOuter = IsInRectangle(pivot, size, i.worldPos);

                if (inInner)
                {
                    float edgeX = fmod(i.worldPos.x - pivot.x, _NodeRadius * 2);
                    float edgeY = fmod(i.worldPos.y - pivot.y, _NodeRadius * 2);
                    
                    if ((edgeX < _Thickness || edgeY < _Thickness))
                    {
                        return _Color;
                    }
                    clip(-1);
                }

                if (inOuter && !inInner)
                {
                   return _Color;
                }

                // Diagonal lines (outside the rectangle)
                float diagCoord = i.uv.x - i.uv.y;
                float scaledCoord = frac(diagCoord * _Line);

                float edge = fwidth(scaledCoord);
                float alpha = 1.0 - smoothstep(_Width - edge, _Width + edge, scaledCoord);

                if (alpha < 0.01) clip(-1);

                return float4(_Color.rgb, alpha * _Color.a);
            }


            ENDCG
        }
    }
}
