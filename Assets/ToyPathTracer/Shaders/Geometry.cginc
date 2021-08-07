#ifndef GEOMETRY_CGINC
#define GEOMETRY_CGINC

#include "Defines.cginc"

struct Triangle {
    float3 vertex0;
    float3 vertex1;
    float3 vertex2;
    float3 normal0;
    float3 normal1;
    float3 normal2;
	int matId;
};

struct Sphere {
	float4 positionAndRadius;
	int matId;
};

struct Cube
{
    float4x4 localToWorld;
    float4x4 worldToLocal;
    float3 size;
    int matId;
};

struct Quad {
    float4 right;
    float4 forward;
    float3 position;
    float3 normal;
    int matId;
};

void SetHitSurface(Ray ray, float3 hitPoint, float3 hitNormal, inout RaycastHit hit)
{
    hit.position = hitPoint;
    float ndv = dot(hitNormal, ray.start - hitPoint);
    if (ndv < 0)
    {
        hit.normal = float4(hitNormal.xyz * -1.0, -1.0);
    }
    else
    {
        hit.normal = float4(hitNormal.xyz, 1.0);
    }
}

int RaycastSphere(Ray ray, Sphere sphere, inout RaycastHit hit)
{
    float3 tocenter = ray.start - sphere.positionAndRadius.xyz;

    float vala = dot(ray.direction, ray.direction);
    float valhalfb = dot(tocenter, ray.direction);
    float valc = dot(tocenter, tocenter) - sphere.positionAndRadius.w * sphere.positionAndRadius.w;

    float dis = valhalfb * valhalfb - vala * valc;

    if (dis < 0.0)
        return -1;
    float e = sqrt(dis);
    float t = (-valhalfb - e) / vala;
    if (t < PT_FLT_DELTA || hit.distance < t)
    {
        t = (-valhalfb + t) / vala;
        if (t < PT_FLT_DELTA || hit.distance < t)
            return -1;
    }
    SetHitSurface(ray, ray.start + ray.direction * t, (tocenter + ray.direction * t) / sphere.positionAndRadius.w, hit);

#if PRIMITIVE_USE_TEXTURE
    float theta = acos(-hit.normal.y);
    float phi = atan2(-hit.normal.z, hit.normal.x) + PT_PI;
    hit.uv = float2(phi * 0.5 * PT_INVPI, theta * PT_INVPI);
#endif
    hit.distance = t;
    hit.matId = sphere.matId;

    return 1;
}

int RaycastQuad(Ray ray, Quad quad, inout RaycastHit hit)
{
    float t = dot(quad.position - ray.start, quad.normal) / dot(ray.direction, quad.normal);
    if (t <= PT_FLT_DELTA)
        return -1;
    if (t > hit.distance)
        return -1;
    float3 p = ray.start + ray.direction * t;
    float3 d = p - quad.position;
    float ddw = dot(d, quad.right.xyz);
    if (ddw < 0.0 || ddw > quad.right.w)
        return -1;
    float ddh = dot(d, quad.forward.xyz);
    if (ddh < 0.0 || ddh > quad.forward.w)
        return -1;
    SetHitSurface(ray, p, quad.normal, hit);

#if PRIMITIVE_USE_TEXTURE
    float3 lp = hit.position - quad.position;
    hit.uv = float2(dot(lp, quad.forward.xyz) / length(quad.forward), -dot(lp, quad.right.xyz) / length(quad.right));
#endif

    hit.distance = t;
    hit.matId = quad.matId;

    return 1;
}

int RaycastCube(Ray ray, Cube cube, inout RaycastHit hit)
{
    float tmin = PT_FLT_DELTA;
    float tmax = hit.distance;

    float3 bmin = -cube.size * 0.5;
    float3 bmax = cube.size * 0.5;

    float3 rayOrigin = mul(cube.worldToLocal, float4(ray.start.xyz, 1.0)).xyz;
    float3 rayDir = mul((float3x3)cube.worldToLocal, ray.direction.xyz).xyz;

    float3 normal = 0;

    for (int i = 0; i < 3; i++)
    {
        float3 n = float3(i == 0 ? -1 : 0, i == 1 ? -1 : 0, i == 2 ? -1 : 0);
        if (abs(rayDir[i]) < PT_FLT_EPSILON)
        {
            if (rayOrigin[i] < bmin[i] || rayOrigin[i] > bmax[i])
                return -1;
        }
        else
        {
            float ood = 1.0 / rayDir[i];
            float t1 = (bmin[i] - rayOrigin[i]) * ood;
            float t2 = (bmax[i] - rayOrigin[i]) * ood;

            if (t1 > t2)
            {
                float t = t2;
                t2 = t1;
                t1 = t;
                n *= -1;
            }

            if (t1 > tmin)
            {
                tmin = t1;

                normal = n;
            }

            if (t2 < tmax)
            {
                tmax = t2;
            }

            if (tmin > tmax)
                return -1;
        }
    }

    float3 hitP = mul(cube.localToWorld, float4(rayOrigin.xyz + rayDir * tmin, 1.0));
    normal = mul((float3x3)cube.localToWorld, normal).xyz;
    SetHitSurface(ray, hitP, normal, hit);

#if PRIMITIVE_USE_TEXTURE
    hit.uv = float2(0,0);
#endif

    hit.distance = tmin;
    hit.matId = cube.matId;

    return true;
}

//int RaycastTriangle(Ray ray, Triangle tri, inout RaycastHit hit)
//{
//    float3 e1 = tri.vertex1 - tri.vertex0;
//    float3 e2 = tri.vertex2 - tri.vertex0;
//
//    float2 uv = 0;
//
//    float3 n = cross(e1, e2);
//    float ndv = dot(ray.direction, n);
//
//    float3 p = cross(ray.direction, e2);
//
//    float det = dot(e1, p);
//    float3 t = float3(0, 0, 0);
//    if (det > 0.0)
//    {
//        t = ray.start - tri.vertex0;
//    }
//    else
//    {
//        t = tri.vertex0 - ray.start;
//        det = -det;
//    }
//    if (det < PT_FLT_EPSILON)
//    {
//        return -1;
//    }
//
//    uv.x = dot(t, p);
//    if (uv.x < 0.0f || uv.x > det)
//        return -1;
//
//    float3 q = cross(t, e1);
//
//    uv.y = dot(ray.direction, q);
//    if (uv.y < 0.0f || uv.x + uv.y > det)
//        return -1;
//
//    float myt = dot(e2, q);
//
//    float finvdet = 1.0f / det;
//    myt *= finvdet;
//    if (myt < PT_FLT_DELTA)
//        return -1;
//    if (myt > hit.distance)
//        return -1;
//
//    uv.x *= finvdet;
//    uv.y *= finvdet;
//
//    hit.distance = myt;
//    hit.position = ray.start + ray.direction * hit.distance;
//    hit.normal.xyz = (1.0 - uv.x - uv.y) * tri.normal0 + uv.x * tri.normal1 + uv.y * tri.normal2;
//    hit.normal.xyz = 1;
//    hit.matId = tri.matId;
//    if (ndv < 0)
//    {
//        hit.normal *= -1;
//    }
//
//    return 1;
//}

int RaycastTriangle(Ray ray, Triangle tri, inout RaycastHit hit)
{
    float3 v1v0 = tri.vertex1 - tri.vertex0;
    float3 v2v0 = tri.vertex2 - tri.vertex0;
    float3 rov0 = ray.start - tri.vertex0;

    float3  n = cross(v1v0, v2v0);
    float3  q = cross(rov0, ray.direction);
    float d = 1.0 / dot(ray.direction, n);
    float u = d * dot(-q, v2v0);
    float v = d * dot(q, v1v0);
    float t = d * dot(-n, rov0);

    if (u < 0.0 || v < 0.0 || (u + v)>1.0) 
        return -1;
    if (t < PT_FLT_DELTA)
        return -1;
    if (t > hit.distance)
        return -1;

    float3 hitP = ray.start + ray.direction * t;
    float3 normal = (1.0 - u - v) * tri.normal0 + u * tri.normal1 + v * tri.normal2;

    SetHitSurface(ray, hitP, normal, hit);

#if PRIMITIVE_USE_TEXTURE
    hit.uv = float2(0, 0);
#endif

    hit.distance = t;
    hit.matId = tri.matId;

    return 1;
}

#endif