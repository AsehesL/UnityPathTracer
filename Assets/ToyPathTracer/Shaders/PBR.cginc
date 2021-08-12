#ifndef PBR_CGINC
#define PBR_CGINC

#include "PathTracing.cginc"
#include "BRDF.cginc"
#include "SkyLight.cginc"

#if PRIMITIVE_SAMPLE_TEXTURE
Texture2DArray<float4> _Textures;
#if PRIMITIVE_HAS_TANGENT
Texture2DArray<float4> _MROTextures;
Texture2DArray<float4> _NormalTextures;
#endif
#endif

#if PRIMITIVE_HAS_TANGENT
float3 RecalculateNormal(float3 normal, float4 tangent, float3 normalColor)
{
    float3 wbnormal = normalize(cross(normal, tangent.xyz)) * tangent.w;

    float3 rnormal = float3(normalColor.r * 2.0 - 1.0, normalColor.g * 2.0 - 1.0, normalColor.b * 2.0 - 1.0) * -1.0;
    float3 worldnormal;
    worldnormal.x = tangent.x * rnormal.x + wbnormal.x * rnormal.y + normal.x * rnormal.z;
    worldnormal.y = tangent.y * rnormal.x + wbnormal.y * rnormal.y + normal.y * rnormal.z;
    worldnormal.z = tangent.z * rnormal.x + wbnormal.z * rnormal.y + normal.z * rnormal.z;
    worldnormal = normalize(worldnormal);
    if (dot(worldnormal, normal) < 0)
        worldnormal *= -1.0;
    return worldnormal;
}
#endif

int PBRShading(Ray ray, RaycastHit hit, out float3 radiance, out Ray newray)
{
    newray = (Ray)0;

    PTMaterial mat = _Materials[hit.matId];

    float3 albedoColor = mat.albedo.rgb;
    float3 rayDir = normalize(ray.direction);

    float roughness = mat.roughness;
    float metallic = mat.metallic;

    if (mat.textureId >= 0)
    {
#if PRIMITIVE_SAMPLE_TEXTURE
        albedoColor = albedoColor * _Textures.SampleLevel(_LinearRepeat, float3(hit.uv, mat.textureId), 0).rgb;
#else
        float2 c = floor(hit.position.xz) * 0.5;
        float checkBoard = frac(c.x + c.y) * 2;
        albedoColor = lerp(float3(0.02, 0.02, 0.02), albedoColor, checkBoard);
        roughness = lerp(0.8, 0.01, checkBoard);
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

    float4 worldNormal = hit.normal;

#if PRIMITIVE_SAMPLE_TEXTURE
#if PRIMITIVE_HAS_TANGENT
    
    if (mat.normalTextureId >= 0)
    { 
        float3 normalColor = _NormalTextures.SampleLevel(_LinearRepeat, float3(hit.uv, mat.normalTextureId), 0).rgb;
        worldNormal.xyz = RecalculateNormal(hit.normal.xyz, hit.tangent, normalColor);
    }
    if (mat.mroTextureId >= 0)
    {
        float3 mroColor = _MROTextures.SampleLevel(_LinearRepeat, float3(hit.uv, mat.mroTextureId), 0).rgb;
        metallic = mroColor.r * metallic;
        roughness = mroColor.g * roughness;
    }
#endif
#endif

    newray.start = hit.position;
    float russianRoulette = Rand();
    if (mat.albedo.a < 0.5)
    {
        float eta = worldNormal.w < 0 ? 1.33f : 1.0f / 1.33f;
        float3 hitNormal = CookTorrance_SampleH(worldNormal.xyz, roughness);
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
    float ndv = max(0.0f, dot(normalize(worldNormal.xyz), normalize(-ray.direction)));
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedoColor.rgb, metallic);
    float3 fresnel = FresnelSchlick(ndv, F0, roughness);
    float fresnelAvg = fresnel.r * 0.22f + fresnel.g * 0.707f + fresnel.b * 0.071f;

#if USE_NEXT_EVENT_ESTIMATION
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
        float3 kd = (1.0 - metallic) * albedoColor;
        indirectLdir = Lambertatian_SampleH(worldNormal.xyz);
        indirectH = normalize(-rayDir + indirectLdir);
        indirectBRDF = Lambertatian_SampleBRDF(indirectLdir, -rayDir, indirectH, worldNormal.xyz, hit.position, roughness, indirectPDF) * kd;
#if USE_NEXT_EVENT_ESTIMATION
        directBRDF = Lambertatian_BRDF(directLdir, -rayDir, directH, worldNormal.xyz, hit.position, roughness) * kd;
#endif
    }
    else
    {
        float3 ks = lerp(1, fresnel, metallic);
        indirectH = CookTorrance_SampleH(worldNormal.xyz, roughness);
        indirectLdir = normalize(reflect(rayDir, indirectH));
        indirectBRDF = CookTorrance_SampleBRDF(indirectLdir, -rayDir, indirectH, worldNormal.xyz, hit.position, roughness, indirectPDF) * ks;
#if USE_NEXT_EVENT_ESTIMATION
        directBRDF = CookTorrance_BRDF(directLdir, -rayDir, directH, worldNormal.xyz, hit.position, roughness) * ks;
#endif
    }

#if USE_NEXT_EVENT_ESTIMATION
    float pdf = 0.0f;
    float sqrPdf0 = indirectPDF * indirectPDF;
    float sqrPdf1 = directPDF * directPDF;
    float w0 = sqrPdf0 / (sqrPdf0 + sqrPdf1);
    float w1 = sqrPdf1 / (sqrPdf0 + sqrPdf1);
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
    if (isnan(pdf) || pdf <= 0)
        radiance = 0;
    else
        radiance = max(0, dot(worldNormal.xyz, newray.direction)) * brdf / pdf * w;
    return 1;
#else
    newray.direction = indirectLdir;
    if (indirectPDF <= 0)
        radiance = 0;
    else
        radiance = max(0, dot(worldNormal.xyz, newray.direction)) * indirectBRDF / indirectPDF;
    return 1;
#endif
}

#endif