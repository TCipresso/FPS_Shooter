Shader "Custom/TileLoop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Tiling ("World Tiling", Float) = 1.0
        _BlendSharpness ("Blend Sharpness", Range(1, 16)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Tint;
                float _Tiling;
                float _BlendSharpness;
            CBUFFER_END

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
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs =
                    GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);

                float3 blend = pow(abs(normal), _BlendSharpness);
                blend /= max(blend.x + blend.y + blend.z, 0.0001);

                float2 uvX = input.positionWS.zy * _Tiling;
                float2 uvY = input.positionWS.xz * _Tiling;
                float2 uvZ = input.positionWS.xy * _Tiling;

                half4 texX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                half4 texY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                half4 texZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                half4 albedo = texX * blend.x + texY * blend.y + texZ * blend.z;
                albedo *= _Tint;

                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normal, mainLight.direction));
                half3 ambient = SampleSH(normal);
                half3 lighting =
                    ambient +
                    mainLight.color * NdotL * mainLight.shadowAttenuation;

                return half4(albedo.rgb * lighting, albedo.a);
            }
            ENDHLSL
        }
    }
}
