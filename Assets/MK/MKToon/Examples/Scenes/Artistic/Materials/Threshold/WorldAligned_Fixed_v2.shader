Shader "Bloodsport/WorldAligned_Fixed"
{
    Properties
    {
        _BaseColor       ("Base Color",          Color)      = (0.15, 0.15, 0.15, 1)
        _BaseSmoothness  ("Base Smoothness",     Range(0,1)) = 0.1
        _BaseMetallic    ("Base Metallic",       Range(0,1)) = 0.0

        [NoScaleOffset]
        _ShapeTex        ("Shape Texture (RGBA)", 2D)        = "black" {}
        _ShapeScale      ("Shape Scale",         Float)      = 1.0
        _ShapeColor      ("Shape Color",         Color)      = (1, 0.4, 0.05, 1)
        _ShapeOpacity    ("Shape Opacity",       Range(0,1)) = 1.0
        _ShapeContrast   ("Shape Contrast",      Range(0.5,5)) = 1.5
        _ShapeSmoothness ("Shape Smoothness",    Range(0,1)) = 0.9
        _ShapeMetallic   ("Shape Metallic",      Range(0,1)) = 0.3
        _ShapeEmission   ("Shape Emission Strength", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

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

                float  _ShapeScale;
                float4 _ShapeColor;
                float  _ShapeOpacity;
                float  _ShapeContrast;
                float  _ShapeSmoothness;
                float  _ShapeMetallic;
                float  _ShapeEmission;
            CBUFFER_END

            TEXTURE2D(_ShapeTex);
            SAMPLER(sampler_ShapeTex);

            struct Attributes
            {
                float4 positionOS    : POSITION;
                float3 normalOS      : NORMAL;
                float3 bakedOriginWS : TEXCOORD4;
                float3 bakedRotRow0  : TEXCOORD5;
                float3 bakedRotRow1  : TEXCOORD6;
                float3 bakedRotRow2  : TEXCOORD7;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 normalOS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                float3 originWS    : TEXCOORD4;
                float3 rotRow0     : TEXCOORD5;
                float3 rotRow1     : TEXCOORD6;
                float3 rotRow2     : TEXCOORD7;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.normalOS    = IN.normalOS;
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                OUT.originWS    = IN.bakedOriginWS;
                OUT.rotRow0     = IN.bakedRotRow0;
                OUT.rotRow1     = IN.bakedRotRow1;
                OUT.rotRow2     = IN.bakedRotRow2;
                return OUT;
            }

            float3 TriplanarWeights(float3 normalWS)
            {
                float3 w = abs(normalWS);
                w = max(w - 0.2, 0.0);
                w = pow(w, 4.0);
                return w / (w.x + w.y + w.z + 1e-5);
            }

            float4 TriplanarSample(TEXTURE2D_PARAM(tex, samp), float3 pos, float3 weights, float scale)
            {
                float4 xProj = SAMPLE_TEXTURE2D(tex, samp, pos.zy * scale);
                float4 yProj = SAMPLE_TEXTURE2D(tex, samp, pos.xz * scale);
                float4 zProj = SAMPLE_TEXTURE2D(tex, samp, pos.xy * scale);
                return xProj * weights.x + yProj * weights.y + zProj * weights.z;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // Both origin and rotation are baked per-vertex at edit time (see
                // BakeWorldOrigin.cs), since static batching bakes each object's
                // full world transform - including rotation - into its vertex
                // data and resets UNITY_MATRIX_M to identity. Without undoing
                // rotation too, rotated static objects sample inconsistently
                // from unrotated ones sharing the same material.
                float3 relativeWS   = IN.positionWS - IN.originWS;
                float3x3 modelRot   = float3x3(IN.rotRow0, IN.rotRow1, IN.rotRow2);
                float3 posForSample = mul(transpose(modelRot), relativeWS);

                float3 weights     = TriplanarWeights(IN.normalOS);
                float4 shapeSample = TriplanarSample(
                    TEXTURE2D_ARGS(_ShapeTex, sampler_ShapeTex),
                    posForSample, weights, _ShapeScale
                );

                // Luminance-based mask instead of alpha - works even when the
                // source texture has no meaningful alpha channel.
                float lum = dot(shapeSample.rgb, float3(0.299, 0.587, 0.114));
                float shapeMask = saturate(lum * _ShapeContrast) * _ShapeOpacity;

                float3 albedo     = lerp(_BaseColor.rgb, _ShapeColor.rgb, shapeMask);
                float  metallic   = lerp(_BaseMetallic, _ShapeMetallic, shapeMask);
                float  smoothness = lerp(_BaseSmoothness, _ShapeSmoothness, shapeMask);
                float3 emission   = _ShapeColor.rgb * shapeMask * _ShapeEmission;

                float3 viewDirWS = normalize(GetCameraPositionWS() - IN.positionWS);

                InputData inputData               = (InputData)0;
                inputData.positionWS              = IN.positionWS;
                inputData.normalWS                = normalize(IN.normalWS);
                inputData.viewDirectionWS         = viewDirWS;
                inputData.shadowCoord             = TransformWorldToShadowCoord(IN.positionWS);
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

                float3 finalColor = UniversalFragmentPBR(inputData, surfaceData).rgb;
                finalColor = MixFog(finalColor, IN.fogFactor);
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
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
