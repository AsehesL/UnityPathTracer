#ifndef SCENE_CGINC
#define SCENE_CGINC

#ifndef SAMPLE_DIRECT_LIGHT
#define SAMPLE_DIRECT_LIGHT 0
#endif

#include "Geometry.cginc"
#include "BVH.cginc"
#include "Sampler.cginc"

#if SAMPLE_DIRECT_LIGHT
float4 _LightCenterAndRadius;
int _LightMatId;
float _LightPDF;

void SamplePointOnLight(out float3 surfacePoint, out float3 surfaceNormal)
{
	float2 uv = float2(Rand(), Rand());

	float x = cos(2.0f * PT_PI * uv.x) * 2.0f * sqrt(uv.y * (1 - uv.y));
	float y = sin(2.0f * PT_PI * uv.x) * 2.0f * sqrt(uv.y * (1 - uv.y));
	float z = 1.0f - 2.0f * uv.y;

	surfaceNormal = normalize(float3(x, y, z));
	surfacePoint = surfaceNormal * _LightCenterAndRadius.w + _LightCenterAndRadius.xyz;
}
#endif

int SceneTracing(Ray ray, inout RaycastHit hit)
{
	int isHit = BVHTracing(ray, hit);

#if SAMPLE_DIRECT_LIGHT
	
	Sphere lightSphere;
	lightSphere.positionAndRadius = _LightCenterAndRadius;
	lightSphere.matId = _LightMatId;
	if (RaycastSphere(ray, lightSphere, hit) > 0)
		isHit = 1;
#endif

	return isHit;
}

#endif