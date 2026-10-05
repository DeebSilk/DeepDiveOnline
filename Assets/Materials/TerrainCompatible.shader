Shader "Nature/Terrain/Diffuse"
{
    Properties
    {
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Normal3("Normal 3 (A)", 2D) = "bump" {}
        [HideInInspector] _Normal2("Normal 2 (B)", 2D) = "bump" {}
        [HideInInspector] _Normal1("Normal 1 (G)", 2D) = "bump" {}
        [HideInInspector] _Normal0("Normal 0 (R)", 2D) = "bump" {}
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}
        [HideInInspector] _Metallic0("Metallic 0", Range(0,1)) = 0
        [HideInInspector] _Metallic1("Metallic 1", Range(0,1)) = 0
        [HideInInspector] _Metallic2("Metallic 2", Range(0,1)) = 0
        [HideInInspector] _Metallic3("Metallic 3", Range(0,1)) = 0
        [HideInInspector] _Smoothness0("Smoothness 0", Range(0,1)) = 0.5
        [HideInInspector] _Smoothness1("Smoothness 1", Range(0,1)) = 0.5
        [HideInInspector] _Smoothness2("Smoothness 2", Range(0,1)) = 0.5
        [HideInInspector] _Smoothness3("Smoothness 3", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "SplatCount" = "4" }

        CGPROGRAM

        #pragma surface surf Standard vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalgbuffer fullforwardshadows nometa noinstancing
        #pragma multi_compile_fog

        #include "UnityCG.cginc"

        sampler2D _Control;
        float4 _Control_ST;
        sampler2D _Splat0, _Splat1, _Splat2, _Splat3;
        sampler2D _Normal0, _Normal1, _Normal2, _Normal3;
        sampler2D _Mask0, _Mask1, _Mask2, _Mask3;
        float _Metallic0, _Metallic1, _Metallic2, _Metallic3;
        float _Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3;

        struct Input
        {
            float2 tc_Control : TEXCOORD0;
            float3 worldNormal;
            UNITY_FOG_COORDS(1)
        };

        void SplatmapVert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.tc_Control = v.texcoord.xy;
            v.tangent.xyz = cross(v.normal, float3(0,0,1));
            v.tangent.w = -1;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float4 splat_control = tex2D(_Control, IN.tc_Control);
            
            float3 col = float3(0, 0, 0);
            float metallic = 0;
            float smoothness = 0;

            col += splat_control.r * tex2D(_Splat0, IN.tc_Control).rgb;
            col += splat_control.g * tex2D(_Splat1, IN.tc_Control).rgb;
            col += splat_control.b * tex2D(_Splat2, IN.tc_Control).rgb;
            col += splat_control.a * tex2D(_Splat3, IN.tc_Control).rgb;

            metallic += splat_control.r * _Metallic0;
            metallic += splat_control.g * _Metallic1;
            metallic += splat_control.b * _Metallic2;
            metallic += splat_control.a * _Metallic3;

            smoothness += splat_control.r * _Smoothness0;
            smoothness += splat_control.g * _Smoothness1;
            smoothness += splat_control.b * _Smoothness2;
            smoothness += splat_control.a * _Smoothness3;

            o.Albedo = col;
            o.Metallic = metallic;
            o.Smoothness = smoothness;
        }

        void SplatmapFinalColor(Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            color.rgb *= o.Albedo;
            UNITY_APPLY_FOG(IN.fogCoord, color);
        }

        void SplatmapFinalgbuffer(Input IN, SurfaceOutputStandard o, inout half4 outGBuffer0, inout half4 outGBuffer1, inout half4 outGBuffer2, inout half4 outEmission)
        {
            outGBuffer0.rgb = o.Albedo;
            outGBuffer0.a = o.Occlusion;
            outGBuffer1 = outGBuffer1;
            outGBuffer2.rgb = o.Normal * 0.5 + 0.5;
        }

        ENDCG
    }

    Fallback "Diffuse"
    CustomEditor "TerrainInspector"
}