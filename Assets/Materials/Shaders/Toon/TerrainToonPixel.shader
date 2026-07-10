Shader "Bloodsport/TerrainToonPixel"
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

        [Toggle(_TOON_SHADING)] _UseToonShading ("Toon Shading", Float) = 1
        _ToonSteps             ("Toon Steps",              Range(2, 8))   = 3
        _ToonRampSmoothness    ("Toon Ramp Smoothness",    Range(0, 1))   = 0.0
        _ShadowTint            ("Toon Shadow Tint",        Color)         = (0.35, 0.35, 0.45, 1)
        _DitherCellSize        ("Band Edge Cell Size (world units)", Range(0.01, 1)) = 0.1

        _SpecularColor         ("Toon Specular Color",     Color)         = (1, 1, 1, 1)
        _SpecularToonSize      ("Toon Specular Size",      Range(0, 1))   = 0.05
        _SpecularToonSmoothness("Toon Specular Smoothness",Range(0, 1))   = 0.1

        _RimColor              ("Toon Rim Color",          Color)         = (1, 1, 1, 1)
        _RimThreshold           ("Toon Rim Threshold",      Range(0, 1))   = 0.7
        _RimIntensity           ("Toon Rim Intensity",      Range(0, 2))   = 0.0

        _OutlineColor    ("Outline Color",       Color)      = (0, 0, 0, 1)
        _OutlineWidth    ("Outline Width",       Range(0, 0.2)) = 0.0
        _OutlineConstScreenSize ("Constant Screen Size", Range(0,1)) = 1.0
        _OutlineMaxDist  ("Outline Max Distance", Float)     = 60.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

                float  _ToonSteps;
                float  _ToonRampSmoothness;
                float4 _ShadowTint;
                float  _DitherCellSize;
                float4 _SpecularColor;
                float  _SpecularToonSize;
                float  _SpecularToonSmoothness;
                float4 _RimColor;
                float  _RimThreshold;
                float  _RimIntensity;

                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineConstScreenSize;
                float  _OutlineMaxDist;
            CBUFFER_END

            struct AttributesOutline
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsOutline
            {
                float4 positionHCS : SV_POSITION;
            };

            VaryingsOutline vertOutline(AttributesOutline IN)
            {
                VaryingsOutline OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = normalize(TransformObjectToWorldNormal(IN.normalOS));

                float distToCam   = distance(positionWS, GetCameraPositionWS());
                float distFade    = saturate(distToCam / max(_OutlineMaxDist, 0.001));
                float widthWS     = lerp(_OutlineWidth, _OutlineWidth * (distToCam * 0.1), _OutlineConstScreenSize);
                widthWS *= (1.0 - distFade * 0.5);

                positionWS += normalWS * widthWS;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            float4 fragOutline(VaryingsOutline IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

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

            #pragma shader_feature_local _TOON_SHADING

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

                float  _ToonSteps;
                float  _ToonRampSmoothness;
                float4 _ShadowTint;
                float  _DitherCellSize;
                float4 _SpecularColor;
                float  _SpecularToonSize;
                float  _SpecularToonSmoothness;
                float4 _RimColor;
                float  _RimThreshold;
                float  _RimIntensity;

                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineConstScreenSize;
                float  _OutlineMaxDist;
            CBUFFER_END

            TEXTURE2D(_ShapeTex);

            // sampler_PointRepeat is already declared by Core.hlsl as a reserved
            // built-in sampler - using it here forces point filtering regardless
            // of the texture's own Filter Mode import setting, which is what
            // keeps the shape texture looking blocky/pixelated.

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float  fogFactor   : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
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

            static const float BayerMatrix4x4[16] =
            {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };

            float2 DominantPlaneCoords(float3 positionWS, float3 normalWS)
            {
                float3 absN = abs(normalWS);
                if (absN.x >= absN.y && absN.x >= absN.z)
                    return positionWS.yz;
                else if (absN.y >= absN.x && absN.y >= absN.z)
                    return positionWS.xz;
                else
                    return positionWS.xy;
            }

            float BayerDitherWorld(float3 positionWS, float3 normalWS)
            {
                float2 planeCoords = DominantPlaneCoords(positionWS, normalWS);
                float2 grid = floor(planeCoords / max(_DitherCellSize, 0.001));
                uint x = (uint)(abs(grid.x)) % 4;
                uint y = (uint)(abs(grid.y)) % 4;
                return BayerMatrix4x4[y * 4 + x] / 16.0;
            }

            float3 ToonRampShade(float NdotL, float atten, float ditherValue)
            {
                float ramp = saturate(NdotL) * atten;
                float steps = max(_ToonSteps, 1.0);
                float dither = (ditherValue - 0.5) / steps;
                float stepped = floor((ramp + dither) * steps) / max(steps - 1.0, 1.0);
                stepped = saturate(lerp(stepped, ramp, _ToonRampSmoothness));
                return lerp(_ShadowTint.rgb, float3(1, 1, 1), stepped);
            }

            float3 ComputeToonLighting(InputData inputData, float3 albedo, float3 normalWS, float3 viewDirWS)
            {
                float3 totalLight = float3(0, 0, 0);
                float ditherValue = BayerDitherWorld(inputData.positionWS, normalWS);

                Light mainLight = GetMainLight(inputData.shadowCoord);
                float mainAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                totalLight += mainLight.color * ToonRampShade(dot(normalWS, mainLight.direction), mainAtten, ditherValue);

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specEdge = 1.0 - _SpecularToonSize;
                float specMask = smoothstep(specEdge - _SpecularToonSmoothness, specEdge + _SpecularToonSmoothness, NdotH);
                totalLight += _SpecularColor.rgb * specMask * mainAtten;

                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rimMask = smoothstep(_RimThreshold - 0.05, _RimThreshold + 0.05, rim);
                totalLight += _RimColor.rgb * rimMask * _RimIntensity;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
                        float atten = light.distanceAttenuation * light.shadowAttenuation;
                        totalLight += light.color * ToonRampShade(dot(normalWS, light.direction), atten, ditherValue);
                    LIGHT_LOOP_END
                #endif

                return albedo * totalLight;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                // Terrain is never statically batched and never rotated, so there
                // is no UNITY_MATRIX_M reset to correct for here - raw world
                // position feeds the triplanar sample directly, no baked
                // origin/rotation needed (unlike the prop-facing shader variant).
                float3 normalOS_approx = normalize(IN.normalWS);
                float3 weights     = TriplanarWeights(normalOS_approx);
                float4 shapeSample = TriplanarSample(
                    TEXTURE2D_ARGS(_ShapeTex, sampler_PointRepeat),
                    IN.positionWS, weights, _ShapeScale
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
                float3 normalWS  = normalize(IN.normalWS);

                InputData inputData               = (InputData)0;
                inputData.positionWS              = IN.positionWS;
                inputData.normalWS                = normalWS;
                inputData.viewDirectionWS         = viewDirWS;
                inputData.shadowCoord             = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord                = 0;
                inputData.vertexLighting          = float3(0,0,0);
                inputData.bakedGI                 = float3(0,0,0);
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask              = float4(1,1,1,1);

                float3 finalColor;

                #if defined(_TOON_SHADING)
                    finalColor = ComputeToonLighting(inputData, albedo, normalWS, viewDirWS) + emission;
                #else
                    SurfaceData surfaceData  = (SurfaceData)0;
                    surfaceData.albedo       = albedo;
                    surfaceData.metallic     = metallic;
                    surfaceData.smoothness   = smoothness;
                    surfaceData.alpha        = 1.0;
                    surfaceData.occlusion    = 1.0;
                    surfaceData.normalTS     = float3(0,0,1);
                    surfaceData.emission     = emission;
                    surfaceData.specular     = float3(0,0,0);

                    finalColor = UniversalFragmentPBR(inputData, surfaceData).rgb;
                #endif

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
