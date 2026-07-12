Shader "Bloodsport/LevelUpShader"
{
    Properties
    {
        _BaseMap         ("Base Texture",        2D)         = "white" {}
        _BaseColor       ("Base Color",          Color)      = (1, 1, 1, 1)

        _GlowMap             ("Glow / Scroll Texture", 2D)       = "white" {}
        _GlowColor           ("Glow Color",            Color)     = (1, 1, 1, 1)
        _GlowIntensity        ("Glow Intensity",        Range(0, 10)) = 2.0
        _ScrollSpeedX         ("Glow Scroll Speed X",   Float)     = 0.5
        _ScrollSpeedY         ("Glow Scroll Speed Y",   Float)     = 0.0
        _GlowPulseSpeed       ("Glow Pulse Speed",      Float)     = 0.0
        _GlowPulseAmount      ("Glow Pulse Amount",     Range(0, 1)) = 0.0

        _OutlineColor    ("Outline Color",       Color)      = (0, 0, 0, 1)
        _OutlineWidth    ("Outline Width",       Range(0, 0.2)) = 0.03
        _OutlineConstScreenSize ("Constant Screen Size", Range(0,1)) = 1.0
        _OutlineMaxDist  ("Outline Max Distance", Float)     = 60.0
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

            float4 _GlowMap_ST;
            float4 _GlowColor;
            float  _GlowIntensity;
            float  _ScrollSpeedX;
            float  _ScrollSpeedY;
            float  _GlowPulseSpeed;
            float  _GlowPulseAmount;

            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineConstScreenSize;
            float  _OutlineMaxDist;
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
                float distFade    = saturate(distToCam / max(_OutlineMaxDist, 0.001));

                if (_OutlineWidth <= 0.0 || distFade >= 1.0)
                {
                    OUT.positionHCS = float4(0.0, 0.0, 0.0, -1.0);
                    return OUT;
                }

                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                float widthWS   = lerp(_OutlineWidth, _OutlineWidth * (distToCam * 0.1), _OutlineConstScreenSize);
                widthWS *= (1.0 - distFade * 0.5);

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

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_GlowMap);
            SAMPLER(sampler_GlowMap);

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
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS       = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS         = TransformWorldToHClip(positionWS);
                OUT.positionWSAndFog    = float4(positionWS, ComputeFogFactor(OUT.positionHCS.z));
                OUT.normalWS            = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv                  = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float3 positionWS = IN.positionWSAndFog.xyz;
                float2 screenUV   = IN.positionHCS.xy / _ScaledScreenParams.xy;

                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo     = baseSample.rgb * _BaseColor.rgb;

                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetCameraPositionWS() - positionWS);

                InputData inputData               = (InputData)0;
                inputData.positionWS              = positionWS;
                inputData.normalWS                = normalWS;
                inputData.viewDirectionWS         = viewDirWS;
                inputData.normalizedScreenSpaceUV = screenUV;
                inputData.shadowMask              = float4(1, 1, 1, 1);
                inputData.shadowCoord             = TransformWorldToShadowCoord(positionWS);

                SurfaceData surfaceData  = (SurfaceData)0;
                surfaceData.albedo       = albedo;
                surfaceData.metallic     = 0.0;
                surfaceData.smoothness   = 0.3;
                surfaceData.alpha        = 1.0;
                surfaceData.occlusion    = 1.0;
                surfaceData.normalTS     = float3(0, 0, 1);
                surfaceData.specular     = float3(0, 0, 0);

                float2 scrollOffset = frac(_Time.y * float2(_ScrollSpeedX, _ScrollSpeedY));
                float2 glowUV       = TRANSFORM_TEX(IN.uv, _GlowMap) + scrollOffset;
                float glowMask      = SAMPLE_TEXTURE2D(_GlowMap, sampler_GlowMap, glowUV).r;

                float pulse = 1.0 + sin(_Time.y * _GlowPulseSpeed) * _GlowPulseAmount;
                float3 glow = _GlowColor.rgb * glowMask * _GlowIntensity * pulse;

                surfaceData.emission = glow;

                float3 finalColor = UniversalFragmentPBR(inputData, surfaceData).rgb;
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
