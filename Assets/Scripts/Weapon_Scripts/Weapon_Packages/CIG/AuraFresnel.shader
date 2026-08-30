Shader "Custom/AuraFresnel"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,0,1)
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 3
        _Intensity ("Intensity", Range(0, 10)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };
            float4 _Color;
            float _FresnelPower;
            float _Intensity;
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = vpi.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(vpi.positionWS);
                return OUT;
            }
            float4 frag(Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                float3 v = normalize(IN.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(n, v)), _FresnelPower);
                float alpha = fresnel * _Intensity;
                return float4(_Color.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
