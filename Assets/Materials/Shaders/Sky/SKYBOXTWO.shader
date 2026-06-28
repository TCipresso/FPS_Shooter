Shader "Custom/SKYBOXTWO"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.15, 0.45, 0.95, 1)
        _HorizonColor ("Horizon Color", Color) = (0.55, 0.82, 1.0, 1)
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudTex ("Cloud Noise Texture", 2D) = "white" {}
        _CloudSpeed ("Cloud Speed", Float) = 0.015
        _CloudScale ("Cloud Scale", Float) = 2.5
        _CloudThreshold ("Cloud Threshold", Range(0,1)) = 0.45
        _CloudSoftness ("Cloud Softness", Range(0.01, 0.5)) = 0.15
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldDir   : TEXCOORD0;
            };

            TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);
            float4 _TopColor, _HorizonColor, _CloudColor;
            float  _CloudSpeed, _CloudScale, _CloudThreshold, _CloudSoftness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldDir   = normalize(mul((float3x3)UNITY_MATRIX_M, IN.positionOS.xyz));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.worldDir);

                // Sky gradient - horizon to top
                float t = saturate(dir.y * 1.5 + 0.1);
                float3 skyColor = lerp(_HorizonColor.rgb, _TopColor.rgb, t);

                // Only show clouds above horizon
                if (dir.y > 0.0)
                {
                    // Project onto a flat plane above
                    float2 uv = dir.xz / (dir.y + 0.1);
                    uv *= _CloudScale;

                    // Two scrolling layers for depth
                    float2 scroll1 = float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.5);
                    float2 scroll2 = float2(-_Time.y * _CloudSpeed * 0.7, _Time.y * _CloudSpeed * 0.3);

                    float c1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv + scroll1).r;
                    float c2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv * 1.7 + scroll2).r;
                    float clouds = c1 * c2;

                    float cloudMask = smoothstep(
                        _CloudThreshold - _CloudSoftness,
                        _CloudThreshold + _CloudSoftness,
                        clouds
                    );

                    // Fade clouds near horizon
                    cloudMask *= saturate(dir.y * 8.0);

                    skyColor = lerp(skyColor, _CloudColor.rgb, cloudMask);
                }

                return half4(skyColor, 1);
            }
            ENDHLSL
        }
    }
}
