#ifndef SKY_LIGHT_CGINC
#define SKY_LIGHT_CGINC

#include "Defines.cginc"

Texture2D<float4> _SkyEnv;
SamplerState _LinearRepeat;

TextureCube<float4> _SkyCube;
SamplerState _LinearClamp;

float3 EnvSkyShading(Ray ray)
{
    float fi = atan2(ray.direction.x, ray.direction.z);
    float u = fi * 0.5f * PT_INVPI;
    float theta = acos(ray.direction.y);

    float v = 1.0f - theta * PT_INVPI;

    return _SkyEnv.SampleLevel(_LinearRepeat, float2(u, v), 0).rgb;
}

float3 CubeSkyShading(Ray ray)
{
    return _SkyCube.SampleLevel(_LinearClamp, ray.direction, 0).rgb;
}

#endif