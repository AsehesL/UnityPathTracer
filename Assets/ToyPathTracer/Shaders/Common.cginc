#ifndef COMMON_CGINC
#define COMMON_CGINC

bool Refract(float3 i, float3 n, float eta, out float3 result)
{
    float cosi = dot(-1.0 * i, n);
    float cost2 = 1.0 - eta * eta * (1.0 - cosi * cosi);
    if (cost2 > 0.0)
    {
        result = eta * i + ((eta * cosi - sqrt(abs(cost2))) * n);
        return true;
    }

    return false;
}

float3 ONB(float3 normal, float3 direction)
{
    float3 w = normal;
    float3 u = normalize(cross(float3(0.00424f, 1, 0.00764f), w));
    float3 v = cross(u, w);
    float3 l = direction.x * u + direction.y * v + direction.z * w;
    //if (dot(l, normal) < 0.0)
    //	l = -direction.x * u - direction.y * v - direction.z * w;
    return normalize(l);
}

#endif