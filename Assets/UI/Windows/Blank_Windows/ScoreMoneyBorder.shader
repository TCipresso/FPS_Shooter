Shader "UI/ScoreMoneyBorder"
{
    Properties
    {
        [PerRendererData] _MainTex ("Border Sprite", 2D) = "white" {}
        _Color ("Border Tint", Color) = (1,1,1,1)

        [Header(Money Pattern)]
        _MoneyTex ("Money Sign Texture", 2D) = "white" {}
        _MoneyColor ("Money Sign Color", Color) = (1,1,0.35,1)
        _MoneyOpacity ("Money Sign Opacity", Range(0,1)) = 1
        _MoneyScale ("Money Sign Scale", Range(1,40)) = 12
        _ScrollSpeedX ("Scroll Speed X", Range(-3,3)) = 0.35
        _ScrollSpeedY ("Scroll Speed Y", Range(-3,3)) = 0

        [Header(Background)]
        _BackgroundStrength ("Original Border Strength", Range(0,1)) = 1
        _GlowStrength ("Money Glow Strength", Range(0,2)) = 0.35

        [Header(Dither)]
        _DitherAmount ("Dither Amount", Range(0,1)) = 0.5
        _DitherScale ("Dither Scale", Range(1,12)) = 2

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
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 screenPos     : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _MoneyTex;

            fixed4 _Color;
            fixed4 _MoneyColor;

            float _MoneyOpacity;
            float _MoneyScale;
            float _ScrollSpeedX;
            float _ScrollSpeedY;
            float _BackgroundStrength;
            float _GlowStrength;
            float _DitherAmount;
            float _DitherScale;

            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float Bayer4x4(float2 p)
            {
                int x = ((int)floor(p.x)) & 3;
                int y = ((int)floor(p.y)) & 3;

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
                fixed4 border = tex2D(_MainTex, i.uv) * i.color;

                if (border.a <= 0.001)
                    return border;

                float2 moneyUV = i.uv * _MoneyScale;
                moneyUV += float2(_Time.y * _ScrollSpeedX, _Time.y * _ScrollSpeedY);

                fixed4 moneySample = tex2D(_MoneyTex, moneyUV);
                float moneyMask = moneySample.a * _MoneyOpacity;

                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float2 pixelPos = screenUV * _ScreenParams.xy / max(_DitherScale, 1.0);
                float threshold = Bayer4x4(pixelPos);

                float ditheredMask = step(threshold, moneyMask);
                moneyMask = lerp(moneyMask, ditheredMask, _DitherAmount);
                moneyMask *= border.a;

                fixed3 baseRGB = border.rgb * _BackgroundStrength;
                fixed3 moneyRGB = _MoneyColor.rgb;

                fixed3 resultRGB = lerp(baseRGB, moneyRGB, moneyMask);
                resultRGB += moneyRGB * moneyMask * _GlowStrength;

                fixed4 result = fixed4(resultRGB, border.a);

                #ifdef UNITY_UI_CLIP_RECT
                    result.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
