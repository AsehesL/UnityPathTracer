#ifndef PBR_CGINC
#define PBR_CGINC

#include "PathTracing.cginc"
#include "BRDF.cginc"
#include "SkyLight.cginc"

#if PRIMITIVE_HAS_UV
Texture2D<float4> _AlbedoTex;
#endif

int PBRShading(Ray ray, RaycastHit hit, out float3 radiance, out Ray newray)
{
    newray = (Ray)0;

    PTMaterial mat = _Materials[hit.matId];

    float3 albedoColor = mat.albedo.rgb;
    float3 rayDir = normalize(ray.direction);

    if (mat.checkBoard > 0)
    {
#if PRIMITIVE_HAS_UV
        albedoColor = albedoColor * _AlbedoTex.SampleLevel(_LinearRepeat, hit.uv, 0).rgb;
#else
        float2 c = floor(hit.position.xz) * 0.5;
        albedoColor = lerp(float3(0.02, 0.02, 0.02), albedoColor, frac(c.x + c.y) * 2);
#endif
    }
    if (mat.emission > 0)
    {
        float em = 1.0;
        if (hit.normal.w < 0)
            em = 0.0;
        radiance = albedoColor.rgb * em;
        return -1;
    }
    newray.start = hit.position;
    float russianRoulette = Rand();
    if (mat.albedo.a < 0.5)
    {
        float eta = hit.normal.w < 0 ? 1.33f : 1.0f / 1.33f;
        float3 hitNormal = CookTorrance_SampleH(hit.normal, mat.roughness);
        float3 litDir;
        float ndotv = dot(-rayDir, hitNormal);
        if (Refract(rayDir, hitNormal, eta, litDir) && Fresnel(ndotv, eta) <= russianRoulette)
        {
            newray.direction = normalize(litDir);
            newray.start -= hitNormal * PT_FLT_DELTA;
            radiance = albedoColor;
        }
        else
        {
            newray.direction = normalize(reflect(rayDir, hitNormal));
            radiance = 1;
        }
        return 1;
    }
    float ndv = max(0.0f, dot(normalize(hit.normal.xyz), normalize(-ray.direction)));
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedoColor.rgb, mat.metallic);
    float3 fresnel = FresnelSchlick(ndv, F0, mat.roughness);
    float fresnelAvg = (fresnel.r + fresnel.g + fresnel.b) / 3.0;

#if SAMPLE_DIRECT_LIGHT
    float3 directLdir;
    float directPDF;
    DirectionalSample(hit, directLdir, directPDF);
    float3 directH = normalize(-rayDir + directLdir);
    float3 directBRDF = 0.0;
#endif
    
    int shadingType = SHADING_TYPE_DIFFUSE;
    if (fresnelAvg > russianRoulette)
    {
        shadingType = SHADING_TYPE_REFLECT;
    }

    float3 indirectLdir;
    float indirectPDF;
    float3 indirectH;
    float3 indirectBRDF;
    if (shadingType == SHADING_TYPE_DIFFUSE)
    {
        float3 kd = (1.0 - mat.metallic) * albedoColor;
        indirectLdir = Lambertatian_SampleH(hit.normal);
        indirectH = normalize(-rayDir + indirectLdir);
        indirectBRDF = Lambertatian_SampleBRDF(indirectLdir, -rayDir, indirectH, hit.normal, hit.position, mat.roughness, indirectPDF) * kd;
#if SAMPLE_DIRECT_LIGHT
        directBRDF = Lambertatian_BRDF(directLdir, -rayDir, directH, hit.normal, hit.position, mat.roughness) * kd;
#endif
    }
    else
    {
        float3 ks = lerp(1, fresnel, mat.metallic);
        indirectH = CookTorrance_SampleH(hit.normal, mat.roughness);
        indirectLdir = normalize(reflect(rayDir, indirectH));
        indirectBRDF = CookTorrance_SampleBRDF(indirectLdir, -rayDir, indirectH, hit.normal, hit.position, mat.roughness, indirectPDF) * ks;
#if SAMPLE_DIRECT_LIGHT
        directBRDF = CookTorrance_BRDF(directLdir, -rayDir, directH, hit.normal, hit.position, mat.roughness) * ks;
#endif
    }

#if SAMPLE_DIRECT_LIGHT
    float pdf = 0.0f;
    float w0 = indirectPDF / (indirectPDF + directPDF);
    float w1 = directPDF / (indirectPDF + directPDF);
    float w = 0;
    float3 brdf;

    float p = Rand();
    if (p < w0)
    {
        newray.direction = indirectLdir;
        w = w0;
        pdf = indirectPDF;
        brdf = indirectBRDF;
    }
    else
    {
        newray.direction = directLdir;
        w = w1;
        pdf = directPDF;
        brdf = directBRDF;
    }
    if (pdf <= 0)
        radiance = 0;
    else
        radiance = max(0, dot(hit.normal.xyz, newray.direction)) * brdf / pdf * w;
    return 1;
#else
    newray.direction = indirectLdir;
    if (indirectPDF <= 0)
        radiance = 0;
    else
        radiance = max(0, dot(hit.normal.xyz, newray.direction)) * indirectBRDF / indirectPDF;
    return 1;
#endif
}

#endif