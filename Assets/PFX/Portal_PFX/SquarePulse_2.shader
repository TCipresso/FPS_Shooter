Shader "Zarcade/SquarePulse"
{
    Properties
    {
        [HDR] _ColorA ("Square Color A", Color) = (0.2, 1, 0.6, 1)
        [HDR] _ColorB ("Square Color B", Color) = (0.1, 0.6, 1, 1)
        [HDR] _OutlineColor ("Outer Boundary Color", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Float) = 2
        _ExpandSpeed ("Expand Speed", Float) = 1.2
        _RingDensity ("Square Count (Density)", Float) = 5
        _LineWidth ("Line Width", Range(0,0.2)) = 0.04
        _LineSharpness ("Line Sharpness", Float) = 4
        _PadRadius ("Pad Size", Range(0,0.5)) = 0.46
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.015
        _CornerRound ("Corner Rounding", Range(0,0.5)) = 0.02
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
            float4 _OutlineColor;
            float _GlowIntensity;
            float _ExpandSpeed;
            float _RingDensity;
            float _LineWidth;
            float _LineSharpness;
            float _PadRadius;
            float _OutlineWidth;
            float _CornerRound;
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
            // rounded-square distance field: 0 at center, grows outward, ~radius at the square edge
            float SquareDist(float2 p, float corner)
            {
                float2 d = abs(p) - (0.5 - corner);
                float outside = length(max(d, 0.0));
                float inside = min(max(d.x, d.y), 0.0);
                return outside + inside + corner;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 patternUV = uv;
                if (_PixelSize > 0.0)
                    patternUV = floor(uv * _PixelSize) / _PixelSize;
                float2 centered = uv - 0.5;
                float2 patternCentered = patternUV - 0.5;
                float sqDist = SquareDist(centered, _CornerRound);
                float patternSqDist = SquareDist(patternCentered, _CornerRound);
                // squares expanding outward from center, looping forever
                float phase = frac(patternSqDist * _RingDensity - _Time.y * _ExpandSpeed);
                float distToLine = min(phase, 1.0 - phase);
                float ringLine = pow(saturate(1.0 - distToLine / _LineWidth), _LineSharpness);
                // alternate colors between successive squares for the trippy look
                float squareIndex = floor(patternSqDist * _RingDensity - _Time.y * _ExpandSpeed);
                float colorPhase = frac(squareIndex * 0.5);
                half4 ringColor = lerp(_ColorA, _ColorB, colorPhase);
                half4 col = ringColor;
                col.rgb *= ringLine * _GlowIntensity;
                float aa = 0.0025;
                float fillMask = 1.0 - smoothstep(_PadRadius - aa, _PadRadius + aa, patternSqDist);
                float outlineOuterRadius = _PadRadius + _OutlineWidth;
                float outlineOuterMask = 1.0 - smoothstep(outlineOuterRadius - aa, outlineOuterRadius + aa, sqDist);
                float outlineMask = saturate(outlineOuterMask - fillMask);
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, outlineMask);
                float coverage = saturate(fillMask * ringLine + outlineMask);
                col.a = coverage * _Alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
