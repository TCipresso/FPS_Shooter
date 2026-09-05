Shader "TextMeshPro/Distance Field Animated Dither"
{
    Properties
    {
        _FaceTex ("Face Texture", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Thickness", Range(0,1)) = 0

        _MainTex ("Font Atlas", 2D) = "white" {}
        _TextureWidth ("Texture Width", Float) = 512
        _TextureHeight ("Texture Height", Float) = 512
        _GradientScale ("Gradient Scale", Float) = 5
        _ScaleX ("Scale X", Float) = 1
        _ScaleY ("Scale Y", Float) = 1

        _WeightNormal ("Weight Normal", Float) = 0
        _WeightBold ("Weight Bold", Float) = 0.5

        _DitherDarkColor ("Dither Dark Color", Color) = (0.1,0.5,0.1,1)
        _DitherBrightColor ("Dither Bright Color", Color) = (0.4,1,0.4,1)

        _DitherScale ("Dither Scale", Float) = 2
        _SweepSpeed ("Sweep Speed", Float) = 1
        _SweepWidth ("Sweep Width", Range(0.01,1)) = 0.25
        _SweepFrequency ("Sweep Frequency", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex VertShader
            #pragma fragment PixShader
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _FaceColor;
            float4 _OutlineColor;

            float _OutlineWidth;
            float _GradientScale;
            float _WeightNormal;
            float _WeightBold;

            float4 _DitherDarkColor;
            float4 _DitherBrightColor;

            float _DitherScale;
            float _SweepSpeed;
            float _SweepWidth;
            float _SweepFrequency;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 localUV : TEXCOORD1;
            };

            v2f VertShader(appdata_t v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.texcoord0;
                o.localUV = v.vertex.xy;

                return o;
            }

            float Bayer4x4(float2 pixelPos)
            {
                int x = ((int)floor(pixelPos.x)) & 3;
                int y = ((int)floor(pixelPos.y)) & 3;

                static const float bayer[16] =
                {
                     0,  8,  2, 10,
                    12,  4, 14,  6,
                     3, 11,  1,  9,
                    15,  7, 13,  5
                };

                return (bayer[y * 4 + x] + 0.5) / 16.0;
            }

            fixed4 PixShader(v2f i) : SV_Target
            {
                float sdf = tex2D(_MainTex, i.uv).a;

                float width = fwidth(sdf);
                float faceAlpha = smoothstep(0.5 - width, 0.5 + width, sdf);

                float outlineEdge = 0.5 - (_OutlineWidth * 0.25);
                float outlineAlpha = smoothstep(outlineEdge - width, outlineEdge + width, sdf);

                float sweepPos = frac(
                    i.localUV.x * _SweepFrequency +
                    _Time.y * _SweepSpeed
                );

                float distToCenter = abs(sweepPos - 0.5) * 2.0;

                float sweep = saturate(
                    1.0 - distToCenter / max(_SweepWidth, 0.001)
                );

                float2 pixelPos = i.vertex.xy / max(_DitherScale, 0.001);

                float threshold = Bayer4x4(pixelPos);
                float dither = step(threshold, sweep);

                float4 face =
                    lerp(_DitherDarkColor, _DitherBrightColor, dither);

                face *= i.color;
                face.a *= faceAlpha;

                float4 outline = _OutlineColor;
                outline.a *= saturate(outlineAlpha - faceAlpha);

                float4 result = outline + face * (1.0 - outline.a);

                return result;
            }
            ENDCG
        }
    }
}
