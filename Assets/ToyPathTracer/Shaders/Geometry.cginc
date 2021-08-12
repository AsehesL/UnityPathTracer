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
    float2 uv0;
    float2 uv1;
    float2 uv2; 
    float4 tangent0;
    float4 tangent1;
    float4 tangent2;
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

#if PRIMITIVE_SAMPLE_TEXTURE
    float theta = acos(-hit.normal.y);
    float phi = atan2(-hit.normal.z, hit.normal.x) + PT_PI;
    hit.uv = float2(phi * 0.5 * PT_INVPI, theta * PT_INVPI);
#if PRIMITIVE_HAS_TANGENT
    float3 tang = normalize(cross(float3(0, 1, 0), hit.normal));
    hit.tangent = float4(tang.xyz, 1.0);
#endif
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

#if PRIMITIVE_SAMPLE_TEXTURE
    float3 lp = hit.position - quad.position;
    hit.uv = float2(dot(lp, quad.forward.xyz) / length(quad.forward), -dot(lp, quad.right.xyz) / length(quad.right));
#if PRIMITIVE_HAS_TANGENT
    hit.tangent = float4(normalize(quad.right).xyz, 1.0);
#endif
#endif

    hit.distance = t;
    hit.matId = quad.matId;

    return 1;
}

int RaycastCube(Ray ray, Cube cube, inout RaycastHit hit)
{
    float tmin = PT_FLT_DELTA;
    float tmax = hit.distance;
    float ttmin = 0;
    float t1, t2;

    float3 bmin = -cube.size * 0.5;
    float3 bmax = cube.size * 0.5;

    float3 rayOrigin = mul(cube.worldToLocal, float4(ray.start.xyz, 1.0)).xyz;
    float3 rayDir = mul((float3x3)cube.worldToLocal, ray.direction.xyz).xyz;

    int n = 0;

    if (abs(rayDir.x) > PT_FLT_EPSILON) {
        t1 = (bmin.x - rayOrigin.x) / rayDir.x;
        t2 = (bmax.x - rayOrigin.x) / rayDir.x;
        ttmin = min(t1, t2);
        if (ttmin > tmin)
        {
            tmin = ttmin;
            n = 1;
        }
        tmax = min(tmax, max(t1, t2));
    }
    if (abs(rayDir.y) > PT_FLT_EPSILON) {
        t1 = (bmin.y - rayOrigin.y) / rayDir.y;
        t2 = (bmax.y - rayOrigin.y) / rayDir.y;
        ttmin = min(t1, t2);
        if (ttmin > tmin)
        {
            tmin = ttmin;
            n = 2;
        }
        tmax = min(tmax, max(t1, t2));
    }
    if (abs(rayDir.z) > PT_FLT_EPSILON) {
        t1 = (bmin.z - rayOrigin.z) / rayDir.z;
        t2 = (bmax.z - rayOrigin.z) / rayDir.z;
        ttmin = min(t1, t2);
        if (ttmin > tmin)
        {
            tmin = ttmin;
            n = 3;
        }
        tmax = min(tmax, max(t1, t2));
    }
    if (tmax < tmin) {
        return -1;
    }

    float3 hitP = rayOrigin.xyz + rayDir * tmin;
    float3 normal = sign(hitP);
    if (n == 1)
    {
        normal.yz = 0;
    }
    else if (n == 2)
    {
        normal.xz = 0;
    }
    else if (n == 3)
    {
        normal.xy = 0;
    }

    hitP = mul(cube.localToWorld, float4(hitP, 1.0)).xyz;
    normal = mul((float3x3)cube.localToWorld, normal).xyz;
    SetHitSurface(ray, hitP, normal, hit);
    hit.position += hit.normal * PT_FLT_DELTA * 2;

#if PRIMITIVE_SAMPLE_TEXTURE
    hit.uv = float2(0,0);
#if PRIMITIVE_HAS_TANGENT
    hit.tangent = float4(0,0,0,1);
#endif
#endif

    hit.distance = tmin;
    hit.matId = cube.matId;

    return 1;
}

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

#if PRIMITIVE_SAMPLE_TEXTURE
    hit.uv = (1.0 - u - v) * tri.uv0 + u * tri.uv1 + v * tri.uv2;
#if PRIMITIVE_HAS_TANGENT
    hit.tangent = (1.0 - u - v) * tri.tangent0 + u * tri.tangent1 + v * tri.tangent2;
#endif
#endif

    hit.distance = t;
    hit.matId = tri.matId;

    return 1;
}

#endif