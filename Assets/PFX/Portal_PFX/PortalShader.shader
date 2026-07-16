Shader "Zarcade/PortalShader"
{
    Properties
    {
        [HDR] _ColorA ("Inner Color", Color) = (0.2, 0.8, 1, 1)
        [HDR] _ColorB ("Outer Color", Color) = (0.6, 0.1, 1, 1)
        [HDR] _EdgeColor ("Edge Glow Color", Color) = (1, 1, 1, 1)
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Float) = 2
        _SwirlSpeed ("Swirl Speed", Float) = 1.5
        _SwirlAmount ("Swirl Amount", Float) = 4
        _RingCount ("Ring Count", Float) = 6
        _RingSpeed ("Ring Speed", Float) = 1
        _EdgeWidth ("Edge Width", Range(0,0.5)) = 0.08
        _EdgePower ("Edge Power", Float) = 3
        _PortalRadius ("Portal Radius", Range(0,0.5)) = 0.48
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.015
        _PixelSize ("Pixel Size (0 = Off)", Float) = 0
        _Alpha ("Overall Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define PORTAL_TWO_PI 6.28318530718

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _ColorA;
            float4 _ColorB;
            float4 _EdgeColor;
            float4 _OutlineColor;
            float _GlowIntensity;
            float _SwirlSpeed;
            float _SwirlAmount;
            float _RingCount;
            float _RingSpeed;
            float _EdgeWidth;
            float _EdgePower;
            float _PortalRadius;
            float _OutlineWidth;
            float _PixelSize;
            float _Alpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 patternUV = uv;

                if (_PixelSize > 0.0)
                    patternUV = floor(uv * _PixelSize) / _PixelSize;

                float2 centered = uv - 0.5;
                float radius = length(centered);

                float2 patternCentered = patternUV - 0.5;
                float patternRadius = length(patternCentered);
                float patternAngle = atan2(patternCentered.y, patternCentered.x);

                float swirl = patternAngle + patternRadius * _SwirlAmount - _Time.y * _SwirlSpeed;
                float rings = sin(patternRadius * _RingCount * PORTAL_TWO_PI - _Time.y * _RingSpeed);
                float pattern = sin(swirl * 3.0) * 0.5 + rings * 0.5;
                pattern = pattern * 0.5 + 0.5;

                half4 col = lerp(_ColorA, _ColorB, pattern);

                float aa = 0.0025;
                float fillMask = 1.0 - smoothstep(_PortalRadius - aa, _PortalRadius + aa, patternRadius);

                float edgeGlow = pow(saturate(1.0 - abs(radius - _PortalRadius) / _EdgeWidth), _EdgePower);
                col.rgb += _EdgeColor.rgb * edgeGlow * fillMask;

                float outlineOuterRadius = _PortalRadius + _OutlineWidth;
                float outlineOuterMask = 1.0 - smoothstep(outlineOuterRadius - aa, outlineOuterRadius + aa, patternRadius);
                float outlineMask = saturate(outlineOuterMask - fillMask);
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, outlineMask);

                col.rgb *= _GlowIntensity;

                float coverage = saturate(fillMask + outlineMask);
                col.a = coverage * _Alpha;

                return col;
            }
            ENDHLSL
        }
    }
}

