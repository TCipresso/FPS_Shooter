Shader "Zarcade/ShineBillboard"
{
    Properties
    {
        _MainTex ("Star Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _PointCount ("Star Points", Float) = 5
        _DistortAmount ("Point Pulse Amount", Range(0,0.5)) = 0.15
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _SpinSpeed ("Spin Speed", Float) = 10.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend One One
        ZWrite Off
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _Color;
            float _PointCount;
            float _DistortAmount;
            float _PulseSpeed;
            float _SpinSpeed;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 centered = IN.uv - 0.5;

                float angle = atan2(centered.y, centered.x) + radians(_Time.y * _SpinSpeed);
                float radius = length(centered);

                float pointPulse = sin(angle * _PointCount + _Time.y * _PulseSpeed);
                float radialOffset = pointPulse * _DistortAmount * radius;

                float newRadius = radius + radialOffset;
                float2 dir = normalize(centered + 1e-5);
                float2 distortedUV = 0.5 + dir * newRadius;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);

                half4 col = tex * _Color;
                return col;
            }
            ENDHLSL
        }
    }
}
