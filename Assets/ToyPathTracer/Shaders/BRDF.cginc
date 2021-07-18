#ifndef BRDF_CGINC
#define BRDF_CGINC

float FresnelRefract(float cosine, float eta)
{
    float r0 = (1.0 - eta) / (1.0 + eta);
    r0 = r0 * r0;
    return r0 + (1.0 - r0) * pow((1.0 - cosine), 5.0);
}

float3 FresnelSchlick(float cosTheta, float3 F0, float roughness)
{
    float3 f = float3(max(1.0f - roughness, F0.r), max(1.0f - roughness, F0.g),
        max(1.0f - roughness, F0.b));
    return F0 + (f - F0) * pow(1.0f - cosTheta, 5.0f);
}

#endif