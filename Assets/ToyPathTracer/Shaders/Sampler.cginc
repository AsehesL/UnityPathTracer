#ifndef SAMPLER_CGINC
#define SAMPLER_CGINC

#include "Defines.cginc"

float _Time;

float _Seed = 0;

void InitRandomSeed(uint x, uint y)
{
    _Seed = _Time + (float)(_ScreenHeight * x) / _ScreenWidth + ((float)y) / _ScreenHeight;
}

float Rand() { return frac(sin(_Seed++) * 43758.5453123); }

float3 SampleHemiSphere(float2 uv)
{
    float phi = 2.0 * PT_PI * uv.x;
    float cos_theta = sqrt(1.0 - uv.y);
    float sin_theta = sqrt(1.0 - cos_theta * cos_theta);

    return float3(cos(phi) * sin_theta, sin(phi) * sin_theta, cos_theta);
}

float3 SampleHemiSphereRough(float2 uv, float roughness)
{
    float a = roughness * roughness;

    float phi = 2.0 * PT_PI * uv.x;
    float cos_theta = sqrt((1.0 - uv.y) / (1.0 + (a * a - 1.0) * uv.y));
    float sin_theta = sqrt(1.0 - cos_theta * cos_theta);

    return float3(cos(phi) * sin_theta, sin(phi) * sin_theta, cos_theta);
}

float2 SampleUnitDisk(float2 uv)
{
    uv.x = 2.0 * uv.x - 1.0;
    uv.y = 2.0 * uv.y - 1.0;
    float r = 0.0;
    float phi = 0.0;
    if (uv.x > -uv.y)
    {
        if (uv.x > uv.y)
        {
            r = uv.x;
            phi = uv.y / uv.x;
        }
        else
        {
            r = uv.y;
            phi = 2.0 - uv.x / uv.y;
        }
    }
    else
    {
        if (uv.x < uv.y)
        {
            r = -uv.x;
            phi = 4.0 + uv.y / uv.x;
        }
        else
        {
            r = -uv.y;
            if (uv.y < -PT_FLT_EPSILON || uv.y > PT_FLT_EPSILON)
            {
                phi = 6.0 - uv.x / uv.y;
            }
            else
            {
                phi = 0.0;
            }
        }
    }
    phi *= PT_PI * 0.25;
    return float2(r * cos(phi), r * sin(phi));
}

#endif