Shader "Bloodsport/Gun_Gen_Emissive"
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
        [HDR]
        _EmissionColor   ("Emission Color",      Color)      = (0, 0, 0, 1)
        _EmissionStrength("Emission Strength",   Range(0,20))= 0.0
        [NoScaleOffset]
        _EmissionMap     ("Emission Map (RGB, optional)", 2D) = "white" {}
        _EmissionTiling  ("Emission Tiling",     Vector)     = (1,1,0,0)
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

                float4 _EmissionColor;
                float  _EmissionStrength;
                float4 _EmissionTiling;
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

            float4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // Shape layer
                float4 shapeSample      = SAMPLE_TEXTURE2D(_ShapeTex, sampler_ShapeTex, IN.uv);
                float  shapeBlend       = shapeSample.a * _ShapeOpacity;
                float3 tintedShapeColor = _ShapeColor.rgb * shapeSample.rgb;

                // Emission — map defaults to white so leaving it unset = solid color emission.
                // Plug in a texture to mask/tint where the glow appears.
                float2 emissionUV    = IN.uv / _ShapeTiling.xy * _EmissionTiling.xy;
                float3 emissionMask  = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV).rgb;
                float3 emission      = _EmissionColor.rgb * emissionMask * _EmissionStrength;

                // Base gets the emission; shape layer sits on top unlit
                float3 baseLit = LitColor(
                    _BaseColor.rgb, _BaseMetallic, _BaseSmoothness,
                    emission,
                    IN.normalWS, IN.positionWS, screenUV
                );

                float3 shapeLit = LitColor(
                    tintedShapeColor, _ShapeMetallic, _ShapeSmoothness,
                    float3(0,0,0),
                    IN.normalWS, IN.positionWS, screenUV
                );

                float3 finalColor = lerp(baseLit, shapeLit, shapeBlend);
                finalColor = MixFog(finalColor, IN.fogFactor);
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
