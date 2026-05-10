Shader "Custom/Triplanar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Tile Scale", Float) = 5.0
        _MinLight ("Minimum Light", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 shadowCoord : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Scale;
                float _MinLight;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.worldPos = posInputs.positionWS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 blendWeights = abs(IN.worldNormal);
                blendWeights = pow(blendWeights, 4);
                blendWeights /= (blendWeights.x + blendWeights.y + blendWeights.z);

                float2 uvX = IN.worldPos.zy / _Scale;
                float2 uvY = IN.worldPos.xz / _Scale;
                float2 uvZ = IN.worldPos.xy / _Scale;

                half4 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvX);
                half4 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvY);
                half4 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvZ);

                half4 albedo = colX * blendWeights.x + colY * blendWeights.y + colZ * blendWeights.z;

                // Main light
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 normal = normalize(IN.worldNormal);
                float NdotL = max(0, dot(normal, mainLight.direction));
                
                // Clamp light so sides are never fully dark
                float lightIntensity = max(NdotL, _MinLight);
                float3 lighting = mainLight.color * lightIntensity;

                return half4(albedo.rgb * lighting, 1);
            }
            ENDHLSL
        }
    }
}