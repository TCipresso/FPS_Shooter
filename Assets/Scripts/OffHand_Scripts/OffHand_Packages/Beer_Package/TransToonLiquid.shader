Shader "Bloodsport/TransToonLiquid"
{
    Properties
    {
        _BaseMap         ("Liquid Texture",       2D)         = "white" {}
        _BaseColor       ("Liquid Color",         Color)      = (0.5, 0.7, 1, 0.8)
        _FillLevel       ("Fill Level (0-1)",     Range(0, 1)) = 0.5
        _WaveAmplitude   ("Wave Amplitude",       Range(0, 0.1)) = 0.02
        _WaveFrequency   ("Wave Frequency",       Range(0, 10)) = 2.0
        _WaveSpeed       ("Wave Speed",           Range(0, 5)) = 1.0
        
        [HideInInspector] _WobbleX ("Wobble X", Float) = 0
        [HideInInspector] _WobbleZ ("Wobble Z", Float) = 0
        
        [Toggle(_TOON_SHADING)] _UseToonShading ("Toon Shading", Float) = 1
        _ToonSteps             ("Toon Steps",              Range(2, 8))   = 3
        _ToonRampSmoothness    ("Toon Ramp Smoothness",    Range(0, 1))   = 0.0
        _ShadowTint            ("Toon Shadow Tint",        Color)         = (0.35, 0.35, 0.45, 1)
        _DitherCellSize        ("Band Edge Cell Size (object units)", Range(0.0001, 1)) = 0.1
        
        _SpecularColor         ("Toon Specular Color",     Color)         = (1, 1, 1, 1)
        _SpecularToonSize      ("Toon Specular Size",      Range(0, 1))   = 0.1
        _SpecularToonSmoothness("Toon Specular Smoothness",Range(0, 1))   = 0.1
        
        _RimColor              ("Toon Rim Color",          Color)         = (1, 1, 1, 1)
        _RimThreshold           ("Toon Rim Threshold",      Range(0, 1))   = 0.7
        _RimIntensity           ("Toon Rim Intensity",      Range(0, 2))   = 0.3
        
        _TopColor         ("Liquid Top Color",     Color)      = (0.7, 0.9, 1, 1)
        _BottomColor      ("Liquid Bottom Color",  Color)      = (0.3, 0.5, 0.8, 1)
        _DepthEffect      ("Color Depth Effect",   Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        LOD 200
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float  _FillLevel;
            float  _WaveAmplitude;
            float  _WaveFrequency;
            float  _WaveSpeed;
            float  _WobbleX;
            float  _WobbleZ;
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
            float4 _TopColor;
            float4 _BottomColor;
            float  _DepthEffect;
        CBUFFER_END
        
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        
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
            float3 positionWS       : TEXCOORD3;
            float3 normalOS         : TEXCOORD4;
            float3 positionOS       : TEXCOORD5;
        };
        
        // Helper function to create rotation matrix around X axis
        float3x3 RotateX(float angle)
        {
            float c = cos(angle);
            float s = sin(angle);
            return float3x3(
                1, 0, 0,
                0, c, -s,
                0, s, c
            );
        }
        
        // Helper function to create rotation matrix around Z axis
        float3x3 RotateZ(float angle)
        {
            float c = cos(angle);
            float s = sin(angle);
            return float3x3(
                c, -s, 0,
                s, c, 0,
                0, 0, 1
            );
        }
        
        Varyings vert(Attributes IN)
        {
            Varyings OUT;
            
            float3 positionOS = IN.positionOS.xyz;
            float3 normalOS = IN.normalOS;
            
            // Get object scale
            float3 objectScale = float3(
                length(float3(GetObjectToWorldMatrix()[0].x, GetObjectToWorldMatrix()[1].x, GetObjectToWorldMatrix()[2].x)),
                length(float3(GetObjectToWorldMatrix()[0].y, GetObjectToWorldMatrix()[1].y, GetObjectToWorldMatrix()[2].y)),
                length(float3(GetObjectToWorldMatrix()[0].z, GetObjectToWorldMatrix()[1].z, GetObjectToWorldMatrix()[2].z))
            );
            
            // Calculate relative height (0 at bottom, 1 at top)
            float relativeHeight = positionOS.y / max(objectScale.y, 0.001);
            
            // IMPORTANT: Apply wobble rotation to the ENTIRE liquid body
            // The rotation pivots from the center of the liquid, not just the surface
            
            // Calculate wobble angles (in radians)
            float wobbleAngleX = _WobbleX * 0.5; // Scale down for reasonable angles
            float wobbleAngleZ = _WobbleZ * 0.5;
            
            // Rotate the entire liquid around its center point
            float3 centerPoint = float3(0, _FillLevel * 0.5, 0); // Pivot at liquid center
            
            // Move vertex to local space relative to pivot
            float3 localPos = positionOS - centerPoint;
            
            // Apply rotation to entire liquid body
            float3x3 rotX = RotateX(wobbleAngleX);
            float3x3 rotZ = RotateZ(wobbleAngleZ);
            float3x3 combinedRot = mul(rotZ, rotX); // Combine rotations
            
            localPos = mul(combinedRot, localPos);
            
            // Move back to original space
            positionOS = localPos + centerPoint;
            
            // Also rotate normals
            normalOS = mul(combinedRot, normalOS);
            
            // Now apply fill level - any vertex above fill level gets pushed down
            if (relativeHeight > _FillLevel)
            {
                // Calculate wave effects on the surface
                float3 positionWS = TransformObjectToWorld(positionOS);
                float time = _Time.y * _WaveSpeed;
                
                float wave1 = sin(positionWS.x * _WaveFrequency + time) * _WaveAmplitude;
                float wave2 = cos(positionWS.z * _WaveFrequency * 1.3 + time * 1.7) * _WaveAmplitude * 0.7;
                float wave3 = sin((positionWS.x + positionWS.z) * _WaveFrequency * 0.7 + time * 2.3) * _WaveAmplitude * 0.5;
                
                // Apply waves only to the top surface
                positionOS.y += (wave1 + wave2 + wave3);
            }
            
            // Transform to world space
            float3 positionWSFinal = TransformObjectToWorld(positionOS);
            float3 normalWSFinal = TransformObjectToWorldNormal(normalOS);
            
            // Transform final position
            OUT.positionHCS = TransformWorldToHClip(positionWSFinal);
            OUT.positionWSAndFog = float4(positionWSFinal, ComputeFogFactor(OUT.positionHCS.z));
            OUT.normalWS = normalWSFinal;
            OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
            OUT.positionWS = positionWSFinal;
            OUT.normalOS = normalOS;
            OUT.positionOS = positionOS;
            
            return OUT;
        }
        
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
        
        float4 frag(Varyings IN) : SV_Target
        {
            float3 positionWS = IN.positionWS;
            float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;
            
            float4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
            float3 albedo = texSample.rgb * _BaseColor.rgb;
            float alpha = texSample.a * _BaseColor.a;
            
            // Calculate depth factor
            float3 objectWorldPos = TransformObjectToWorld(float3(0, 0, 0));
            float3 objectScale = float3(
                length(float3(GetObjectToWorldMatrix()[0].x, GetObjectToWorldMatrix()[1].x, GetObjectToWorldMatrix()[2].x)),
                length(float3(GetObjectToWorldMatrix()[0].y, GetObjectToWorldMatrix()[1].y, GetObjectToWorldMatrix()[2].y)),
                length(float3(GetObjectToWorldMatrix()[0].z, GetObjectToWorldMatrix()[1].z, GetObjectToWorldMatrix()[2].z))
            );
            
            float currentHeight = positionWS.y - objectWorldPos.y;
            float fillHeight = _FillLevel * objectScale.y;
            float depthFactor = saturate(currentHeight / max(fillHeight, 0.001));
            
            float3 depthColor = lerp(_BottomColor.rgb, _TopColor.rgb, depthFactor);
            albedo = lerp(albedo, albedo * depthColor, _DepthEffect);
            
            float3 viewDirWS = normalize(GetCameraPositionWS() - positionWS);
            float3 normalWS = normalize(IN.normalWS);
            
            InputData inputData = (InputData)0;
            inputData.positionWS = positionWS;
            inputData.normalWS = normalWS;
            inputData.viewDirectionWS = viewDirWS;
            inputData.normalizedScreenSpaceUV = screenUV;
            inputData.shadowMask = float4(1, 1, 1, 1);
            
            float3 finalColor = float3(0, 0, 0);
            
            #if defined(_TOON_SHADING)
                Light mainLight = GetMainLight();
                float mainAtten = mainLight.distanceAttenuation;
                
                float ditherValue = BayerDitherObject(IN.positionOS, IN.normalOS);
                finalColor += mainLight.color * ToonRampShade(dot(normalWS, mainLight.direction), mainAtten, ditherValue);
                
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specEdge = 1.0 - _SpecularToonSize;
                float specMask = smoothstep(specEdge - _SpecularToonSmoothness, specEdge + _SpecularToonSmoothness, NdotH);
                finalColor += _SpecularColor.rgb * (specMask * mainAtten);
                
                float rim = 1.0 - saturate(dot(normalWS, viewDirWS));
                float rimMask = smoothstep(_RimThreshold - 0.05, _RimThreshold + 0.05, rim);
                finalColor += _RimColor.rgb * (rimMask * _RimIntensity);
                
                finalColor *= albedo;
            #else
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = 0.0;
                surfaceData.smoothness = 0.7;
                surfaceData.alpha = alpha;
                surfaceData.occlusion = 1.0;
                surfaceData.normalTS = float3(0, 0, 1);
                surfaceData.emission = float3(0, 0, 0);
                surfaceData.specular = float3(0.1, 0.1, 0.1);
                finalColor = UniversalFragmentPBR(inputData, surfaceData).rgb;
            #endif
            
            finalColor = MixFog(finalColor, IN.positionWSAndFog.w);
            return float4(finalColor, alpha);
        }
        ENDHLSL
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _TOON_SHADING
            #pragma multi_compile_fog
            
            ENDHLSL
        }
    }
}