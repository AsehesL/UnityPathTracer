#ifndef BRDF_CGINC
#define BRDF_CGINC

#include "Sampler.cginc"
#include "Common.cginc"

float3 FresnelSchlick(float cosTheta, float3 F0, float roughness)
{
    float3 f = float3(max(1.0f - roughness, F0.r), max(1.0f - roughness, F0.g),
        max(1.0f - roughness, F0.b));
    return F0 + (f - F0) * pow(1.0f - cosTheta, 5.0f);
}

float Fresnel(float cosine, float eta)
{
    float r0 = (1.0 - eta) / (1.0 + eta);
    r0 = r0 * r0;
    return r0 + (1.0 - r0) * pow((1.0 - cosine), 5.0);
}

float D_GGX(float ndh, float a)
{
    float root = a / ((ndh * ndh * (a * a - 1.0f) + 1.0f) + 0.001);

    return PT_INVPI * (root * root);
}

float GeometryGGX(float ndv, float a)
{
    float r = (a + 1.0f);
    float k = (r * r) / 8.0f;

    float nom = ndv;
    float denom = ndv * (1.0f - k) + k;

    return nom / denom;
}

float G_SmithGGX(float3 wi, float3 wo, float3 n, float a)
{
    float ndi = max(0, dot(wi, n));
    float ndo = max(0, dot(wo, n));

    float ggx2 = GeometryGGX(ndi, a);
    float ggx1 = GeometryGGX(ndo, a);

    return ggx1 * ggx2;
}

float3 Lambertatian_SampleH(float3 n)
{
    float3 sp = SampleHemiSphere(float2(Rand(), Rand()));
    return ONB(n, sp);
}

float3 Lambertatian_SampleBRDF(float3 wi, float3 wo, float3 h, float3 n, float3 p, float roughness, out float pdf)
{
    float ndi = dot(wi, n);
    pdf = ndi * PT_INVPI;
    return PT_INVPI;
}

float3 Lambertatian_BRDF(float3 wi, float3 wo, float3 h, float3 n, float3 p, float roughness)
{
    return PT_INVPI;
}

float3 CookTorrance_SampleH(float3 n, float roughness)
{
    roughness = clamp(roughness, 0.0001, 1);

    float a = roughness * roughness;

    float2 sp = float2(Rand(), Rand());

    float phi = 2.0f * PT_PI * sp.x;

    float cos_theta = sqrt((1.0 - sp.y) / (1.0 + (a * a - 1.0) * sp.y));
    float sin_theta = sqrt(1.0 - cos_theta * cos_theta);

    float3 hemiSample = float3(cos(phi) * sin_theta, sin(phi) * sin_theta, cos_theta);

    return ONB(n, hemiSample);
}

float3 CookTorrance_SampleBRDF(float3 wi, float3 wo, float3 h, float3 n, float3 p, float roughness, out float pdf)
{
    roughness = clamp(roughness, 0.0001, 1);

    float a = roughness * roughness;

    float ndh = dot(h, n);

    float D = D_GGX(ndh, a);

    float denominator = 4.0 * max(dot(n, wi), 0.0) * max(dot(n, wo), 0.0) + 0.0001;
    float nominator = D * G_SmithGGX(wi, wo, n, a);

    pdf = ndh / (4.0f * dot(wi, h)) * D;

    return nominator / denominator;
}

float3 CookTorrance_BRDF(float3 wi, float3 wo, float3 h, float3 n, float3 p, float roughness)
{
    roughness = clamp(roughness, 0.0001, 1);

    float a = roughness * roughness;

    float ndh = max(0, dot(h, n));

    float D = D_GGX(ndh, a);

    float denominator = 4.0 * max(dot(n, wi), 0.0) * max(dot(n, wo), 0.0) + 0.0001;
    float nominator = D * G_SmithGGX(wi, wo, n, a);

    return nominator / denominator;
}

#endif