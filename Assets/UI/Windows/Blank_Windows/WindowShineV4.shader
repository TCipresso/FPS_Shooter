Shader "UI/WindowShineV4"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ShineColor ("Shine Color", Color) = (1,1,1,1)
        _ShineStrength ("Shine Strength", Range(0,2)) = 1
        _ShineWidth ("Shine Width", Range(0.01,0.75)) = 0.18
        _ShineSpeed ("Shine Speed", Range(-3,3)) = 0.45
        _ShineAngle ("Shine Angle", Range(-2,2)) = 0.65
        _ShineCount ("Shine Count", Range(1, 20)) = 1
        _DitherScale ("Dither Scale", Range(1,12)) = 2
        _DitherAmount ("Dither Amount", Range(0,1)) = 1

        [HideInInspector] _UnscaledTime ("Unscaled Time", Float) = 0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos     : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 _ShineColor;
            float _ShineStrength;
            float _ShineWidth;
            float _ShineSpeed;
            float _ShineAngle;
            float _ShineCount;
            float _DitherScale;
            float _DitherAmount;
            float _UnscaledTime;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                o.screenPos = ComputeScreenPos(o.vertex);
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

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;

                if (col.a <= 0.001)
                    return col;

                float totalShine = 0.0;

                float axis = i.texcoord.x + i.texcoord.y * _ShineAngle;

                // Uses Time.unscaledTime supplied by WindowShineUnscaledTime.cs.
                // This continues advancing even when Time.timeScale == 0.
                float travel = _UnscaledTime * _ShineSpeed;
                float normalizedTravel = frac(travel);

                for (int lineIndex = 0; lineIndex < (int)_ShineCount; lineIndex++)
                {
                    float linePos = (float)lineIndex / _ShineCount;
                    float shiftedPos = frac(linePos + normalizedTravel);
                    float lineTravel = shiftedPos * 2.4 - 0.7;

                    float distanceFromBand = abs(axis - lineTravel);

                    float smoothBand =
                        saturate(1.0 - distanceFromBand / max(_ShineWidth, 0.0001));

                    totalShine = max(totalShine, smoothBand);
                }

                totalShine *= totalShine;

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 pixelPos =
                    screenUV * _ScreenParams.xy / max(_DitherScale, 1.0);

                float threshold = Bayer4x4(pixelPos);

                float ditheredBand = step(threshold, totalShine);
                float shineMask = lerp(totalShine, ditheredBand, _DitherAmount);
                shineMask *= col.a;

                col.rgb = lerp(
                    col.rgb,
                    _ShineColor.rgb,
                    saturate(shineMask * _ShineStrength)
                );

                #ifdef UNITY_UI_CLIP_RECT
                    col.a *= UnityGet2DClipping(
                        i.worldPosition.xy,
                        _ClipRect
                    );
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
