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
        _ShapeSmoothness ("Shape Smoothness",    Range(0,1)) = 0.9
        _ShapeMetallic   ("Shape Metallic",      Range(0,1)) = 0.3
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

                float  _ShapeScale;
                float4 _ShapeColor;
                float  _ShapeOpacity;
                float  _ShapeSmoothness;
                float  _ShapeMetallic;
            CBUFFER_END

            TEXTURE2D(_ShapeTex);
            SAMPLER(sampler_ShapeTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 positionOS  : TEXCOORD1;  // object-space for locked sampling
                float3 normalWS    : TEXCOORD2;
                float3 normalOS    : TEXCOORD3;  // object-space normal for locked weights
                float  fogFactor   : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.positionOS  = IN.positionOS.xyz;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.normalOS    = IN.normalOS;
                OUT.fogFactor   = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            float3 LitColor(float3 albedo, float metallic, float smoothness,
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
                surfaceData.emission     = float3(0,0,0);
                surfaceData.specular     = float3(0,0,0);

                return UniversalFragmentPBR(inputData, surfaceData).rgb;
            }

            // Triplanar blend weights from world normal
            float3 TriplanarWeights(float3 normalWS)
            {
                float3 w = abs(normalWS);
                w = max(w - 0.2, 0.0);   // sharpness bias
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

                // Sample using object-space position so the texture is locked
                // to the mesh and doesn't slide when the object moves.
                // Object-space normal drives the blend weights so they also
                // stay stable regardless of the object's world rotation.
                float3 weights     = TriplanarWeights(IN.normalOS);
                float4 shapeSample = TriplanarSample(
                    TEXTURE2D_ARGS(_ShapeTex, sampler_ShapeTex),
                    IN.positionOS, weights, _ShapeScale
                );

                float  shapeBlend       = shapeSample.a * _ShapeOpacity;
                float3 tintedShapeColor = _ShapeColor.rgb * shapeSample.rgb;

                float3 baseLit = LitColor(
                    _BaseColor.rgb, _BaseMetallic, _BaseSmoothness,
                    IN.normalWS, IN.positionWS, screenUV
                );

                float3 shapeLit = LitColor(
                    tintedShapeColor, _ShapeMetallic, _ShapeSmoothness,
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
