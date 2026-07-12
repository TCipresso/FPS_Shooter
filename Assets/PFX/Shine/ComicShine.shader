Shader "Zarcade/ComicShine"
{
    Properties
    {
        _Color ("Star Color", Color) = (1,0.9,0.1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.1)) = 0.02
        _PointCount ("Star Points", Float) = 8
        _InnerRadius ("Inner Radius", Range(0,1)) = 0.35
        _OuterRadius ("Outer Radius", Range(0,1)) = 0.5
        _SnapSpeed ("Snap Steps Per Second", Float) = 6
        _SpinStep ("Rotation Per Snap (deg)", Float) = 15
        _ScalePulse ("Scale Pulse Amount", Range(0,0.5)) = 0.15
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _PointCount;
            float _InnerRadius;
            float _OuterRadius;
            float _SnapSpeed;
            float _SpinStep;
            float _ScalePulse;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float snapTick = floor(_Time.y * _SnapSpeed);

                float rotation = radians(snapTick * _SpinStep);
                float scaleFlicker = 1.0 + (fmod(snapTick, 2.0) * 2.0 - 1.0) * _ScalePulse;

                float2 centered = (IN.uv - 0.5) * scaleFlicker;

                float s = sin(rotation);
                float c = cos(rotation);
                centered = float2(centered.x * c - centered.y * s, centered.x * s + centered.y * c);

                float angle = atan2(centered.y, centered.x);
                float radius = length(centered);

                float sectorAngle = TWO_PI / _PointCount;
                float localAngle = fmod(angle + PI, sectorAngle);
                localAngle = abs(localAngle - sectorAngle * 0.5);
                float t = localAngle / (sectorAngle * 0.5);

                float starEdge = lerp(_OuterRadius, _InnerRadius, t);

                float shape = step(radius, starEdge);
                float outline = step(radius, starEdge) - step(radius, starEdge - _OutlineWidth);

                half4 col = shape * _Color;
                col = lerp(col, _OutlineColor, outline);
                col.a = shape;

                return col;
            }
            ENDHLSL
        }
    }
}
