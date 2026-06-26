Shader "Bloodsport/ArcadeSurface"
{
    Properties
    {
        _BaseColor       ("Base Color",          Color)      = (0.15, 0.15, 0.15, 1)
        _BaseSmoothness  ("Base Smoothness",     Range(0,1)) = 0.1
        _BaseMetallic    ("Base Metallic",       Range(0,1)) = 0.0

        [NoScaleOffset]
        _ShapeTex        ("Shape Texture (RGBA)", 2D)        = "black" {}
        _ShapeTiling     ("Shape Tiling",        Vector)     = (1,1,0,0)
        _ShapeColor      ("Shape Color",         Color)      = (1, 0.4, 0.05, 1)
        _ShapeOpacity    ("Shape Opacity",       Range(0,1)) = 1.0
        _ShapeSmoothness ("Shape Smoothness",    Range(0,1)) = 0.9
        _ShapeMetallic   ("Shape Metallic",      Range(0,1)) = 0.3

        [Header(Emission)]
        [NoScaleOffset]
        _EmissionMap     ("Emission Map (RGB)",  2D)         = "black" {}
        _EmissionTiling  ("Emission Tiling",     Vector)     = (1,1,0,0)
        [HDR]
        _EmissionColor   ("Emission Color",      Color)      = (1, 1, 1, 1)
        _EmissionStrength("Emission Strength",   Range(0,20))= 1.0

        [Header(Procedural Grid)]
        _GridTiling      ("Grid Tiling (cells)", Vector)     = (8,8,0,0)
        _GridLineWidth   ("Grid Line Width",     Range(0,0.5))= 0.04
        _GridColor       ("Grid Line Color",     Color)      = (1, 1, 1, 1)
        _GridSmoothness  ("Grid Smoothness",     Range(0,1)) = 0.9
        _GridMetallic    ("Grid Metallic",       Range(0,1)) = 0.0
        [HDR]
        _GridEmission    ("Grid Emission Color", Color)      = (0, 0, 0, 1)
        _GridEmissionStr ("Grid Emission Strength", Range(0,20)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _BaseSmoothness;
                float  _BaseMetallic;

                float4 _ShapeTiling;
                float4 _ShapeColor;
                float  _ShapeOpacity;
                float  _ShapeSmoothness;
                float  _ShapeMetallic;

                float4 _EmissionTiling;
                float4 _EmissionColor;
                float  _EmissionStrength;

                float4 _GridTiling;
                float  _GridLineWidth;
                float4 _GridColor;
                float  _GridSmoothness;
                float  _GridMetallic;
                float4 _GridEmission;
                float  _GridEmissionStr;
            CBUFFER_END

            TEXTURE2D(_ShapeTex);    SAMPLER(sampler_ShapeTex);
            TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = IN.uv * _ShapeTiling.xy;
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            float3 LitColor(float3 albedo, float metallic, float smoothness,
                            float3 emission,
                            float3 normalWS, float3 positionWS, float2 screenUV)
            {
                float3 viewDirWS = normalize(GetCameraPositionWS() - positionWS);

                InputData inputData               = (InputData)0;
                inputData.positionWS              = positionWS;
                inputData.normalWS                = normalize(normalWS);
                inputData.viewDirectionWS         = viewDirWS;
                inputData.shadowCoord             = TransformWorldToShadowCoord(positionWS);
                inputData.fogCoord                = 0;
                inputData.vertexLighting          = float3(0,0,0);
                inputData.bakedGI                 = float3(0,0,0);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask              = float4(1,1,1,1);

                SurfaceData surfaceData  = (SurfaceData)0;
                surfaceData.albedo       = albedo;
                surfaceData.metallic     = metallic;
                surfaceData.smoothness   = smoothness;
                surfaceData.alpha        = 1.0;
                surfaceData.occlusion    = 1.0;
                surfaceData.normalTS     = float3(0,0,1);
                surfaceData.emission     = emission;
                surfaceData.specular     = float3(0,0,0);

                return UniversalFragmentPBR(inputData, surfaceData).rgb;
            }

            // Returns 1 where a grid line exists, 0 elsewhere.
            // Uses fwidth() for screen-space AA so lines stay crisp at every distance.
            float GridMask(float2 uv, float2 tiling, float lineWidth)
            {
                float2 scaled = uv * tiling;
                float2 f      = frac(scaled);

                // Distance from nearest cell edge (0 = on the edge)
                float2 df = min(f, 1.0 - f);

                // 1 pixel worth of UV space at this screen distance
                float2 fw = fwidth(scaled);

                // Smooth 1px border around the line edge
                float2 lineMask = 1.0 - smoothstep(lineWidth - fw, lineWidth + fw, df);

                return max(lineMask.x, lineMask.y);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // ── Shape layer ──────────────────────────────────────────
                float4 shapeSample      = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, IN.uv);
                float  shapeBlend       = shapeSample.a * _ShapeOpacity;
                float3 tintedShapeColor = _ShapeColor.rgb * shapeSample.rgb;

                // ── Emission map ─────────────────────────────────────────
                float2 emissionUV     = IN.uv / _ShapeTiling.xy * _EmissionTiling.xy;
                float3 emissionSample = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV).rgb;
                float3 emission       = emissionSample * _EmissionColor.rgb * _EmissionStrength;

                // ── Procedural grid ──────────────────────────────────────
                // Use raw (un-shape-tiled) UVs so grid tiling is independent
                float2 rawUV = IN.uv / _ShapeTiling.xy;
                float  grid  = GridMask(rawUV, _GridTiling.xy, _GridLineWidth);

                // ── Lighting passes ──────────────────────────────────────
                float3 baseLit = LitColor(
                    _BaseColor.rgb, _BaseMetallic, _BaseSmoothness,
                    float3(0,0,0),
                    IN.normalWS, IN.positionWS, screenUV
                );

                float3 shapeLit = LitColor(
                    tintedShapeColor, _ShapeMetallic, _ShapeSmoothness,
                    emission,
                    IN.normalWS, IN.positionWS, screenUV
                );

                float3 gridLit = LitColor(
                    _GridColor.rgb, _GridMetallic, _GridSmoothness,
                    _GridEmission.rgb * _GridEmissionStr,
                    IN.normalWS, IN.positionWS, screenUV
                );

                // ── Composite: base → shape → grid ───────────────────────
                float3 finalColor = lerp(baseLit,   shapeLit, shapeBlend);
                finalColor        = lerp(finalColor, gridLit,  grid);
                finalColor        = MixFog(finalColor, IN.fogFactor);
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
