Shader "Bloodsport/OutlineHull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _OutlineConstantScreenSize ("Constant Screen Size", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }
        LOD 100

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineConstantScreenSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

                float camDist = distance(GetCameraPositionWS(), positionWS);
                float widthScale = lerp(1.0, camDist, _OutlineConstantScreenSize);

                positionWS += normalWS * _OutlineWidth * widthScale;

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
