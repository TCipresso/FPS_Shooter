Shader "Custom/WeaponPunch"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Tint Color", Color) = (1, 1, 1, 1)

        _ScrollSpeed ("Scroll Speed (X, Y)", Vector) = (0.05, 0.02, 0, 0)
        _TextureScale ("Texture Scale", Range(0.1, 10)) = 2.0

        _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.5
        _GlowThreshold ("Glow Threshold", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainColor;
                float4 _ScrollSpeed;
                float  _TextureScale;
                float4 _GlowColor;
                float  _GlowIntensity;
                float  _GlowThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                // Use the mesh's own UVs — scale and scroll over time
                OUT.uv = IN.uv * _TextureScale;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // Scroll the UVs over time using the mesh's own UV space
                float2 scrolledUV = IN.uv + float2(_ScrollSpeed.x, _ScrollSpeed.y) * time;

                half4 texSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrolledUV);
                half3 baseColor = texSample.rgb * _MainColor.rgb;

                // Toon diffuse
                Light mainLight = GetMainLight();
                float3 normalWS = normalize(IN.normalWS);
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float toon = NdotL > 0.5 ? 1.0 : 0.5;
                half3 lit = baseColor * mainLight.color * toon;

                // Glow: only on bright parts of the texture, fully opaque
                // We lerp between the lit color and the glow color — no additive blending
                float glowMask = smoothstep(_GlowThreshold, 1.0, texSample.r);
                half3 glowColor = _GlowColor.rgb * _GlowIntensity;
                half3 final = lerp(lit, glowColor, glowMask);

                return half4(final, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
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

    FallBack "Universal Render Pipeline/Lit"
}
