Shader "Zarcade/ComicShineMesh"
{
    Properties
    {
        _Color ("Star Color", Color) = (1,0.85,0.1,1)
        _RimColor ("Rim Brighten Color", Color) = (1,1,0.6,1)
        _RimPower ("Rim Sharpness", Range(0.1,8)) = 2
        _PulseSpeed ("Brightness Pulse Speed", Float) = 2
        _PulseAmount ("Brightness Pulse Amount", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off

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
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            float4 _Color;
            float4 _RimColor;
            float _RimPower;
            float _PulseSpeed;
            float _PulseAmount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                rim = pow(rim, _RimPower);

                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                half4 col = _Color * pulse;
                col.rgb = lerp(col.rgb, _RimColor.rgb, rim);

                return col;
            }
            ENDHLSL
        }
    }
}
