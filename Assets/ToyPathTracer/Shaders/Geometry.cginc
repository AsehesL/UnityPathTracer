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

    hit.distance = t;
    hit.matId = quad.matId;

    return 1;
}

int RaycastTriangle(Ray ray, Triangle tri, inout RaycastHit hit)
{
    float3 e1 = tri.vertex1 - tri.vertex0;
    float3 e2 = tri.vertex2 - tri.vertex0;

    float2 uv = 0;

    float3 n = cross(e1, e2);
    float ndv = dot(ray.direction, n);

    float3 p = cross(ray.direction, e2);

    float det = dot(e1, p);
    float3 t = float3(0, 0, 0);
    if (det > 0.0)
    {
        t = ray.start - tri.vertex0;
    }
    else
    {
        t = tri.vertex0 - ray.start;
        det = -det;
    }
    if (det < PT_FLT_EPSILON)
    {
        return -1;
    }

    uv.x = dot(t, p);
    if (uv.x < 0.0f || uv.x > det)
        return -1;

    float3 q = cross(t, e1);

    uv.y = dot(ray.direction, q);
    if (uv.y < 0.0f || uv.x + uv.y > det)
        return -1;

    float myt = dot(e2, q);

    float finvdet = 1.0f / det;
    myt *= finvdet;
    if (myt < PT_FLT_DELTA)
        return -1;
    if (myt > hit.distance)
        return -1;

    uv.x *= finvdet;
    uv.y *= finvdet;

    hit.distance = myt;
    hit.position = ray.start + ray.direction * hit.distance;
    hit.normal.xyz = (1.0 - uv.x - uv.y) * tri.normal0 + uv.x * tri.normal1 + uv.y * tri.normal2;
    hit.normal.xyz = 1;
    hit.matId = tri.matId;
    if (ndv < 0)
    {
        hit.normal *= -1;
    }

    return 1;
}

#endif