﻿Shader "HighlightPlus/Geometry/BlurOutline" {
Properties {
    _Color ("Color", Color) = (1,1,0) // not used; dummy property to avoid inspector warning "material has no _Color property"
    _BlurScale("Blur Scale", Float) = 2.0
    _BlurScaleFirstHoriz("Blur Scale First Horiz", Float) = 4
}
    SubShader
    {
        ZTest Always
        ZWrite Off
        Cull Off
        CGINCLUDE

    #include "UnityCG.cginc"

    UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
    float4     _MainTex_TexelSize;
    float4     _MainTex_ST;
    float     _BlurScale;
    float _BlurScaleFirstHoriz;

    struct MeshData {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2fCross {
        float4 pos : SV_POSITION;
        float2 uv: TEXCOORD0;
        float2 uv1: TEXCOORD1;
        float2 uv2: TEXCOORD2;
        float2 uv3: TEXCOORD3;
        float2 uv4: TEXCOORD4;
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    v2fCross vertCross(MeshData v) {
        v2fCross o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	    o.pos = v.vertex;
	    o.pos.y *= _ProjectionParams.x;

	    o.uv = v.texcoord;
        float3 offsets = _MainTex_TexelSize.xyx * float3(1, 1, -1);
        o.uv1 = v.texcoord - offsets.xy;
        o.uv2 = v.texcoord - offsets.zy;
        o.uv3 = v.texcoord + offsets.zy;
        o.uv4 = v.texcoord + offsets.xy;
        return o;
    }

    
    v2fCross vertBlur(MeshData v, float multiplier) {
        v2fCross o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.pos = v.vertex;
		o.pos.y *= _ProjectionParams.x;
        o.uv = v.texcoord;

        float2 inc = float2(_MainTex_TexelSize.x * 1.3846153846 * multiplier, 0);
        o.uv1 = v.texcoord - inc;   
        o.uv2 = v.texcoord + inc;
        float2 inc2 = float2(_MainTex_TexelSize.x * 3.2307692308 * multiplier, 0);   
        o.uv3 = v.texcoord - inc2;
        o.uv4 = v.texcoord + inc2;  
        return o;
    }   
    
    v2fCross vertBlurH(MeshData v) {
        return vertBlur(v, _BlurScale);
    }   

    v2fCross vertBlurH2(MeshData v) {
        return vertBlur(v, _BlurScaleFirstHoriz);
    }


    v2fCross vertBlurV(MeshData v) {
        v2fCross o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		o.pos = v.vertex;
		o.pos.y *= _ProjectionParams.x;

        o.uv = v.texcoord;
        float2 inc = float2(0, _MainTex_TexelSize.y * 1.3846153846 * _BlurScale);    
        o.uv1 = v.texcoord - inc;   
        o.uv2 = v.texcoord + inc;   
        float2 inc2 = float2(0, _MainTex_TexelSize.y * 3.2307692308 * _BlurScale);   
        o.uv3 = v.texcoord - inc2;  
        o.uv4 = v.texcoord + inc2;  
        return o;
    }
    
    float4 fragBlur (v2fCross i): SV_Target {
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        float4 pixel = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv) * 0.2270270270
                    + (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1) + UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv2)) * 0.3162162162
                    + (UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv3) + UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv4)) * 0.0702702703;
        return pixel;
    }   

    float4 fragResample(v2fCross i) : SV_Target {
        UNITY_SETUP_INSTANCE_ID(i);
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        float4 c1 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv1);
        float4 c2 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv2);
        float4 c3 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv3);
        float4 c4 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv4);
        return (c1+c2+c3+c4) * 0.25;
    }


    ENDCG

    Pass {
        Name "Blur Horizontal"
        CGPROGRAM
        #pragma vertex vertBlurH
        #pragma fragment fragBlur
        #pragma fragmentoption ARB_precision_hint_fastest
        #pragma target 3.0
        ENDCG
    }

    Pass {
        Name "Blur Vertical"
        CGPROGRAM
        #pragma vertex vertBlurV
        #pragma fragment fragBlur
        #pragma fragmentoption ARB_precision_hint_fastest
        #pragma target 3.0
        ENDCG
    }

    Pass {
        Name "Resample"
        CGPROGRAM
        #pragma vertex vertCross
        #pragma fragment fragResample
        #pragma fragmentoption ARB_precision_hint_fastest
        #pragma target 3.0
        ENDCG
    }

    Pass {
        Name "Blur Horizontalx2"
        CGPROGRAM
        #pragma vertex vertBlurH2
        #pragma fragment fragBlur
        #pragma fragmentoption ARB_precision_hint_fastest
        #pragma target 3.0
        ENDCG
    }

    }
}