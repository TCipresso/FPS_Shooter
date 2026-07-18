Shader "Bloodsport/ZomboDOTS"
{
    Properties
    {
        _BaseMap         ("Base Texture",        2D)         = "white" {}
        _BaseColor       ("Base Color",          Color)      = (1, 1, 1, 1)

        [Toggle(_TOON_SHADING)] _UseToonShading ("Toon Shading", Float) = 1
        [Toggle(_RECEIVE_SHADOWS_ON)] _ReceiveShadows ("Receive Shadows", Float) = 0

        _ToonSteps             ("Toon Steps",              Range(2, 8))   = 3
        _ToonRampSmoothness    ("Toon Ramp Smoothness",    Range(0, 1))   = 0.0
        _ShadowTint            ("Toon Shadow Tint",        Color)         = (0.35, 0.35, 0.45, 1)
        _DitherCellSize        ("Band Edge Cell Size (object units)", Range(0.0001, 1)) = 0.1
        _ShadowReceiveBias      ("Shadow Receive Bias",     Range(0, 0.1)) = 0.02

        _SpecularColor         ("Toon Specular Color",     Color)         = (1, 1, 1, 1)
        _SpecularToonSize      ("Toon Specular Size",      Range(0, 1))   = 0.05
        _SpecularToonSmoothness("Toon Specular Smoothness",Range(0, 1))   = 0.1

        _RimColor              ("Toon Rim Color",          Color)         = (1, 1, 1, 1)
        _RimThreshold           ("Toon Rim Threshold",      Range(0, 1))   = 0.7
        _RimIntensity           ("Toon Rim Intensity",      Range(0, 2))   = 0.0

        _OutlineColor    ("Outline Color",       Color)      = (0, 0, 0, 1)
        _OutlineWidth    ("Outline Width",       Range(0, 0.2)) = 0.03
        _OutlineConstScreenSize ("Constant Screen Size", Range(0,1)) = 1.0
        _OutlineMaxDist  ("Outline Max Distance", Float)     = 60.0

        _FlashColor      ("Flash Color",         Color)      = (1, 1, 1, 1)
        _FlashAmount     ("Flash Amount",        Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;

            float  _ToonSteps;
            float  _ToonRampSmoothness;
            float4 _ShadowTint;
            float  _DitherCellSize;
            float  _ShadowReceiveBias;
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

            float4 _FlashColor;
            float  _FlashAmount;
        CBUFFER_END

        #ifdef UNITY_DOTS_INSTANCING_ENABLED
        UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
            UNITY_DOTS_INSTANCED_PROP(float4, _FlashColor)
            UNITY_DOTS_INSTANCED_PROP(float,  _FlashAmount)
        UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

        #define _FlashColor  UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _FlashColor)
        #define _FlashAmount UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float,  _FlashAmount)
        #endif
        ENDHLSL

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
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            struct AttributesOutline
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsOutline
            {
                float4 positionHCS : SV_POSITION;
            };

            VaryingsOutline vertOutline(AttributesOutline IN)
            {
                VaryingsOutline OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float distToCam   = distance(positionWS, GetCameraPositionWS());
                float distFade    = saturate(distToCam / max(_OutlineMaxDist, 0.001));

                // No per-vertex kill branch here. Writing an invalid clip position
                // (w = -1) for some vertices of a triangle while its neighbors get
                // valid positions creates mixed triangles that smear across the
                // screen after near-plane clipping - the black streak bug on
                // skinned meshes straddling _OutlineMaxDist. Instead the width
                // fades continuously to zero, which collapses far outlines onto
                // the mesh surface with no discontinuity.

                float3 rawNormalOS = IN.normalOS;
                float normalLenSq = dot(rawNormalOS, rawNormalOS);

                // A degenerate (zero-length) normal on some skinned vertex would
                // make normalize() return NaN, which then gets pushed through
                // the position offset below. Falling back to a safe default
                // normal avoids the NaN entirely.
                float3 normalWS = (normalLenSq > 1e-8)
                    ? normalize(TransformObjectToWorldNormal(rawNormalOS))
                    : float3(0.0, 1.0, 0.0);

                float widthWS = lerp(_OutlineWidth, _OutlineWidth * (distToCam * 0.1), _OutlineConstScreenSize);
                widthWS *= saturate(1.0 - distFade);

                // Clamp as a second safety net in case distToCam is ever huge
                // for a frame (e.g. camera not yet positioned on a load frame)
                widthWS = clamp(widthWS, 0.0, _OutlineWidth * 10.0);

                OUT.positionHCS = TransformWorldToHClip(positionWS + normalWS * widthWS);
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
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma shader_feature_local _TOON_SHADING
            #pragma shader_feature_local _RECEIVE_SHADOWS_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS      : SV_POSITION;
                float4 positionWSAndFog : TEXCOORD0;
                float3 normalWS         : TEXCOORD1;
                float2 uv               : TEXCOORD2;
                #if defined(_TOON_SHADING)
                float3 positionOS       : TEXCOORD3;
                float3 normalOS         : TEXCOORD4;
                #endif
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 positionWS       = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS         = TransformWorldToHClip(positionWS);
                OUT.positionWSAndFog    = float4(positionWS, ComputeFogFactor(OUT.positionHCS.z));
                OUT.normalWS            = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv                  = TRANSFORM_TEX(IN.uv, _BaseMap);
                #if defined(_TOON_SHADING)
                OUT.positionOS          = IN.positionOS.xyz;
                OUT.normalOS            = IN.normalOS;
                #endif
                return OUT;
            }

            #if defined(_TOON_SHADING)

            static const float BayerMatrix4x4[16] =
            {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };

            float2 DominantPlaneCoords(float3 positionOS, float3 normalOS)
            {
                float3 absN = abs(normalOS);
                if (absN.x >= absN.y && absN.x >= absN.z)
                    return positionOS.yz;
                else if (absN.y >= absN.x && absN.y >= absN.z)
                    return positionOS.xz;
                else
                    return positionOS.xy;
            }

            float BayerDitherObject(float3 positionOS, float3 normalOS)
            {
                float2 planeCoords = DominantPlaneCoords(positionOS, normalOS);
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

            float3 ComputeToonLighting(InputData inputData, float3 albedo, float3 normalWS, float3 viewDirWS, float ditherValue)
            {
                float3 totalLight = float3(0, 0, 0);

                #if defined(_RECEIVE_SHADOWS_ON)
                    Light mainLight = GetMainLight(inputData.shadowCoord);
                    float mainAtten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                #else
                    Light mainLight = GetMainLight();
                    float mainAtten = mainLight.distanceAttenuation;
                #endif

                totalLight += mainLight.color * ToonRampShade(dot(normalWS, mainLight.direction), mainAtten, ditherValue);

                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specEdge = 1.0 - _SpecularToonSize;
                float specMask = smoothstep(specEdge - _SpecularToonSmoothness, specEdge + _SpecularToonSmoothness, NdotH);
                totalLight += _SpecularColor.rgb * (specMask * mainAtten);

                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rimMask = smoothstep(_RimThreshold - 0.05, _RimThreshold + 0.05, rim);
                totalLight += _RimColor.rgb * (rimMask * _RimIntensity);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        #if defined(_RECEIVE_SHADOWS_ON)
                            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
                            float atten = light.distanceAttenuation * light.shadowAttenuation;
                        #else
                            Light light = GetAdditionalLight(lightIndex, inputData.positionWS);
                            float atten = light.distanceAttenuation;
                        #endif
                        totalLight += light.color * ToonRampShade(dot(normalWS, light.direction), atten, ditherValue);
                    LIGHT_LOOP_END
                #endif

                return albedo * totalLight;
            }

            #endif

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 positionWS = IN.positionWSAndFog.xyz;
                float2 screenUV   = IN.positionHCS.xy / _ScaledScreenParams.xy;

                float4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_PointRepeat, IN.uv);
                float3 albedo    = texSample.rgb * _BaseColor.rgb;

                float3 viewDirWS = normalize(GetCameraPositionWS() - positionWS);
                float3 normalWS  = normalize(IN.normalWS);

                InputData inputData               = (InputData)0;
                inputData.positionWS              = positionWS;
                inputData.normalWS                = normalWS;
                inputData.viewDirectionWS         = viewDirWS;
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask              = float4(1, 1, 1, 1);

                #if defined(_RECEIVE_SHADOWS_ON) || !defined(_TOON_SHADING)
                    inputData.shadowCoord = TransformWorldToShadowCoord(positionWS + normalWS * _ShadowReceiveBias);
                #endif

                float3 finalColor;

                #if defined(_TOON_SHADING)
                    float ditherValue = BayerDitherObject(IN.positionOS, IN.normalOS);
                    finalColor = ComputeToonLighting(inputData, albedo, normalWS, viewDirWS, ditherValue);
                #else
                    SurfaceData surfaceData  = (SurfaceData)0;
                    surfaceData.albedo       = albedo;
                    surfaceData.metallic     = 0.0;
                    surfaceData.smoothness   = 0.5;
                    surfaceData.alpha        = 1.0;
                    surfaceData.occlusion    = 1.0;
                    surfaceData.normalTS     = float3(0, 0, 1);
                    surfaceData.emission     = float3(0, 0, 0);
                    surfaceData.specular     = float3(0, 0, 0);

                    finalColor = UniversalFragmentPBR(inputData, surfaceData).rgb;
                #endif

                // Hit flash override - lerp toward flash color, bypasses lighting so it always reads clearly
                finalColor = lerp(finalColor, _FlashColor.rgb, saturate(_FlashAmount));

                finalColor = MixFog(finalColor, IN.positionWSAndFog.w);
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDepthNormals
            #pragma fragment fragDepthNormals
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            struct AttributesDN
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDN
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };

            VaryingsDN vertDepthNormals(AttributesDN IN)
            {
                VaryingsDN OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 fragDepthNormals(VaryingsDN IN) : SV_Target
            {
                return half4(normalize(IN.normalWS), 0.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDepthOnly
            #pragma fragment fragDepthOnly
            #pragma target 4.5

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            struct AttributesDO
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDO
            {
                float4 positionHCS : SV_POSITION;
            };

            VaryingsDO vertDepthOnly(AttributesDO IN)
            {
                VaryingsDO OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half fragDepthOnly(VaryingsDO IN) : SV_Target
            {
                return 0;
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma target 4.5
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct AttributesSC
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsSC
            {
                float4 positionHCS : SV_POSITION;
            };

            VaryingsSC vertShadow(AttributesSC IN)
            {
                VaryingsSC OUT;
                UNITY_SETUP_INSTANCE_ID(IN);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionHCS.z = min(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionHCS.z = max(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionHCS = positionHCS;
                return OUT;
            }

            half4 fragShadow(VaryingsSC IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
