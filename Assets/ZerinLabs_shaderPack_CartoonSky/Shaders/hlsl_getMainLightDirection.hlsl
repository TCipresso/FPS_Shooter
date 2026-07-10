// hlsl_getMainLightDirection.hlsl

#if defined(UNIVERSAL_LIGHTING_INCLUDED)
#define USING_URP 1
#else
#define USING_URP 0
#endif

#if USING_URP
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif

void MainLightDirection_float(out float3 Direction)
{
#if SHADERGRAPH_PREVIEW
    Direction = float3(0.5, 0.5, 0.0); // preview en editor
#elif USING_URP
    Light mainLight = GetMainLight();
    Direction = mainLight.direction;
#else
    Direction = normalize(_WorldSpaceLightPos0.xyz);
#endif
}
