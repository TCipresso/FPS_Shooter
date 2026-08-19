Shader "Bloodsport/ToonOutlinePixel"
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
        _OutlineFadeStart ("Outline Fade Start (0-1 of Max Dist)", Range(0, 0.99)) = 0.8
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
            float  _OutlineFadeStart;
        CBUFFER_END
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
                float distToCam   = distance(positionWS, GetCameraPositionWS());

                // NOTE ON THE FIX:
                // The old code teleported any vertex past the cutoff to a fixed,
                // arbitrary clip-space point (0,0,0,-1) that has nothing to do with
                // that vertex's real position. Because that happens per-VERTEX, any
                // triangle straddling the cutoff distance ended up with one corner
                // snapped to that fake point while its neighbors stayed at their
                // real (still large) offset positions -> the GPU rasterizes a huge
                // sliver stretching between them. Fading the width alone didn't fix
                // this, because the teleport-to-a-fake-point branch was still firing
                // right at the threshold - only smaller.
                //
                // Real fix: remove the teleport branch completely. Let widthWS fade
                // smoothly to (and stay at) exactly 0 and ALWAYS use the real
                // transformed position. There is no longer any discontinuous jump -
                // at zero width the outline shell is simply coincident with the
                // mesh surface and disappears via normal depth occlusion instead of
                // doing anything dramatic.
                float distFade = saturate(distToCam / max(_OutlineMaxDist, 0.001));
                float fadeOut  = 1.0 - smoothstep(_OutlineFadeStart, 1.0, distFade);

                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                float widthWS   = lerp(_OutlineWidth, _OutlineWidth * (distToCam * 0.1), _OutlineConstScreenSize);
                widthWS *= fadeOut; // _OutlineWidth == 0 already yields widthWS == 0, no extra branch needed

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
            #pragma shader_feature_local _TOON_SHADING
            #pragma shader_feature_local _RECEIVE_SHADOWS_ON
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
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
            };
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
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
            struct AttributesDN
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct VaryingsDN
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };
            VaryingsDN vertDepthNormals(AttributesDN IN)
            {
                VaryingsDN OUT;
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
            struct AttributesDO
            {
                float4 positionOS : POSITION;
            };
            struct VaryingsDO
            {
                float4 positionHCS : SV_POSITION;
            };
            VaryingsDO vertDepthOnly(AttributesDO IN)
            {
                VaryingsDO OUT;
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
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct AttributesSC
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct VaryingsSC
            {
                float4 positionHCS : SV_POSITION;
            };
            VaryingsSC vertShadow(AttributesSC IN)
            {
                VaryingsSC OUT;
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