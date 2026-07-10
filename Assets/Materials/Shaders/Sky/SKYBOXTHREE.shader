Shader "Custom/SKYBOXTHREE"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.15, 0.45, 0.95, 1)
        _HorizonColor ("Horizon Color", Color) = (0.55, 0.82, 1.0, 1)
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudShadowColor ("Cloud Shadow Color", Color) = (0.65, 0.68, 0.75, 1)
        _CloudTex ("Cloud Noise Texture", 2D) = "white" {}
        _CloudSpeed ("Cloud Speed", Float) = 0.015
        _CloudScale ("Cloud Scale", Float) = 2.5
        _CloudThreshold ("Cloud Threshold", Range(0,1)) = 0.45
        _CloudSoftness ("Cloud Softness", Range(0.01, 0.5)) = 0.15
        _CloudSunGlow ("Cloud Sun-Facing Glow", Range(0, 2)) = 0.5
        _CloudPuffScale ("Cloud Puff Detail Scale", Range(0.005, 0.1)) = 0.02
        _CloudPuffStrength ("Cloud Puff Shading Strength", Range(0, 2)) = 0.8

        _CloudLayer2Tint ("Far Cloud Layer Tint", Color) = (0.85, 0.9, 1.0, 1)
        _CloudLayer2HeightOffset ("Far Cloud Layer Height", Range(0.1, 1)) = 0.35
        _CloudLayer2Scale ("Far Cloud Layer Scale Mult", Float) = 1.6
        _CloudLayer2Speed ("Far Cloud Layer Speed Mult", Float) = 0.5
        _CloudLayer2Opacity ("Far Cloud Layer Opacity", Range(0, 1)) = 0.35

        _SunTint ("Sun Tint", Color) = (1, 1, 1, 1)
        _SunSize ("Sun Size (degrees)", Range(0.1, 10)) = 1.5
        _SunEdgeSoftness ("Sun Edge Softness (degrees)", Range(0, 5)) = 0.3
        _SunIntensity ("Sun Disk Intensity", Range(0, 20)) = 4
        _SunGlowIntensity ("Sun Glow Intensity", Range(0, 2)) = 0.4
        _SunGlowFalloff ("Sun Glow Falloff", Range(1, 64)) = 8
        _HorizonGlowIntensity ("Horizon Glow Toward Sun", Range(0, 2)) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" }
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldDir   : TEXCOORD0;
            };
            TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);
            float4 _TopColor, _HorizonColor, _CloudColor, _CloudShadowColor, _SunTint, _CloudLayer2Tint;
            float  _CloudSpeed, _CloudScale, _CloudThreshold, _CloudSoftness, _CloudSunGlow;
            float  _CloudPuffScale, _CloudPuffStrength;
            float  _CloudLayer2HeightOffset, _CloudLayer2Scale, _CloudLayer2Speed, _CloudLayer2Opacity;
            float  _SunSize, _SunEdgeSoftness, _SunIntensity, _SunGlowIntensity, _SunGlowFalloff, _HorizonGlowIntensity;
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.worldDir   = normalize(mul((float3x3)UNITY_MATRIX_M, IN.positionOS.xyz));
                return OUT;
            }
            float CloudDensity(float2 uv, float2 scroll1, float2 scroll2)
            {
                float c1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv + scroll1).r;
                float c2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv * 1.7 + scroll2).r;
                return c1 * c2;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.worldDir);

                Light mainLight = GetMainLight();
                float3 sunDir    = normalize(mainLight.direction);
                float3 lightColor = mainLight.color.rgb * _SunTint.rgb;
                float  sunDot    = dot(dir, sunDir);

                // Sky gradient - horizon to top
                float t = saturate(dir.y * 1.5 + 0.1);
                float3 skyColor = lerp(_HorizonColor.rgb, _TopColor.rgb, t);

                // Sun disk - hard edge with a soft rim, sized in real degrees
                float cosSize = cos(radians(_SunSize));
                float cosEdge = cos(radians(_SunSize + _SunEdgeSoftness));
                float sunDisk = smoothstep(cosEdge, cosSize, sunDot);
                float sunGlow = pow(saturate(sunDot), _SunGlowFalloff) * _SunGlowIntensity;
                skyColor += lightColor * (sunDisk * _SunIntensity + sunGlow);

                // Atmospheric haze that brightens the sky toward the sun, mostly near the horizon
                float horizonGlow = pow(saturate(sunDot), 2.0) * saturate(1.0 - abs(dir.y)) * _HorizonGlowIntensity;
                skyColor += lightColor * horizonGlow;

                // Only show clouds above horizon
                if (dir.y > 0.0)
                {
                    // Far background layer - different projection height/scale/speed
                    // than the main layer gives a parallax depth cue instead of
                    // everything reading as one flat plane.
                    float2 uv2 = dir.xz / (dir.y + _CloudLayer2HeightOffset);
                    uv2 *= _CloudScale * _CloudLayer2Scale;
                    float2 scroll1b = float2(_Time.y * _CloudSpeed * _CloudLayer2Speed, _Time.y * _CloudSpeed * _CloudLayer2Speed * 0.5);
                    float2 scroll2b = float2(-_Time.y * _CloudSpeed * _CloudLayer2Speed * 0.7, _Time.y * _CloudSpeed * _CloudLayer2Speed * 0.3);
                    float clouds2 = CloudDensity(uv2, scroll1b, scroll2b);
                    float cloudMask2 = smoothstep(
                        _CloudThreshold - _CloudSoftness,
                        _CloudThreshold + _CloudSoftness,
                        clouds2
                    );
                    cloudMask2 *= saturate(dir.y * 8.0) * _CloudLayer2Opacity;
                    skyColor = lerp(skyColor, _CloudLayer2Tint.rgb, cloudMask2);

                    // Project onto a flat plane above
                    float2 uv = dir.xz / (dir.y + 0.1);
                    uv *= _CloudScale;
                    // Two scrolling layers for depth
                    float2 scroll1 = float2(_Time.y * _CloudSpeed, _Time.y * _CloudSpeed * 0.5);
                    float2 scroll2 = float2(-_Time.y * _CloudSpeed * 0.7, _Time.y * _CloudSpeed * 0.3);
                    float c1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv + scroll1).r;
                    float c2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv * 1.7 + scroll2).r;
                    float clouds = c1 * c2;
                    float cloudMask = smoothstep(
                        _CloudThreshold - _CloudSoftness,
                        _CloudThreshold + _CloudSoftness,
                        clouds
                    );
                    // Fade clouds near horizon
                    cloudMask *= saturate(dir.y * 8.0);

                    // Fake puffiness: sample density at two nearby offsets to build a
                    // pseudo-normal, then light it like a bump map so each puff gets
                    // a lit side and a shadowed side instead of one flat tone.
                    float cloudsRight = CloudDensity(uv + float2(_CloudPuffScale, 0), scroll1, scroll2);
                    float cloudsUp    = CloudDensity(uv + float2(0, _CloudPuffScale), scroll1, scroll2);
                    float3 pseudoNormal = normalize(float3(clouds - cloudsRight, clouds - cloudsUp, max(_CloudPuffStrength, 0.001)));
                    float3 lightDirUV   = normalize(float3(sunDir.x, sunDir.z, sunDir.y));
                    float cloudLight    = saturate(dot(pseudoNormal, lightDirUV));
                    float puffShade     = lerp(1.0 - _CloudPuffStrength * 0.4, 1.0 + _CloudPuffStrength * 0.4, cloudLight);

                    // Two-tone shading from the noise itself instead of a flat fill, so clouds read as having volume
                    float3 cloudColor = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, saturate(c1 * 1.3));
                    cloudColor *= puffShade;
                    // Backlit/silver-lining glow on clouds near the sun
                    float cloudSunGlow = pow(saturate(sunDot), 3.0) * _CloudSunGlow;
                    cloudColor += lightColor * cloudSunGlow;

                    skyColor = lerp(skyColor, cloudColor, cloudMask);
                }
                return half4(skyColor, 1);
            }
            ENDHLSL
        }
    }
}
