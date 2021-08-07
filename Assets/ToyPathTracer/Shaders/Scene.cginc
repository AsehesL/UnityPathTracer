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
float4 _LightRight;
float4 _LightForward;
float3 _LightPosition;
float3 _LightNormal;
int _LightMatId;
int _LightPrimivateType;
float _LightPDF;

void SamplePointOnLight(out float3 surfacePoint, out float3 surfaceNormal)
{
	float2 uv = float2(Rand(), Rand());

	if (_LightPrimivateType == PRIMITIVE_QUAD)
	{
		surfacePoint = _LightPosition + _LightRight.xyz * (uv.x) + _LightForward.xyz * (uv.y);
		surfaceNormal = _LightNormal;
	}
	else if (_LightPrimivateType == PRIMITIVE_SPHERE)
	{
		float x = cos(2.0f * PT_PI * uv.x) * 2.0f * sqrt(uv.y * (1 - uv.y));
		float y = sin(2.0f * PT_PI * uv.x) * 2.0f * sqrt(uv.y * (1 - uv.y));
		float z = 1.0f - 2.0f * uv.y;

		surfaceNormal = normalize(float3(x, y, z));
		surfacePoint = surfaceNormal * _LightCenterAndRadius.w + _LightCenterAndRadius.xyz;
	}
}
#endif

int SceneTracing(Ray ray, inout RaycastHit hit)
{
	int isHit = BVHTracing(ray, hit);

#if SAMPLE_DIRECT_LIGHT
	
	if (_LightPrimivateType == PRIMITIVE_QUAD)
	{
		Quad lightQuad;
		lightQuad.right = _LightRight;
		lightQuad.forward = _LightForward;
		lightQuad.normal = _LightNormal;
		lightQuad.position = _LightPosition;
		lightQuad.matId = _LightMatId;
		if (RaycastQuad(ray, lightQuad, hit) > 0)
			isHit = 1;
	}
	else if (_LightPrimivateType == PRIMITIVE_SPHERE)
	{
		Sphere lightSphere;
		lightSphere.positionAndRadius = _LightCenterAndRadius;
		lightSphere.matId = _LightMatId;
		if (RaycastSphere(ray, lightSphere, hit) > 0)
			isHit = 1;
	}
#endif

	return isHit;
}

#endif