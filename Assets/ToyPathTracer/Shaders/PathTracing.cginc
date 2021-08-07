#ifndef PATH_TRACING_CGINC
#define PATH_TRACING_CGINC

#ifndef USE_THIN_LENS
#define USE_THIN_LENS 0
#endif

#include "Defines.cginc"
#include "Scene.cginc"
#include "Sampler.cginc"
#include "Common.cginc"

float _NearClipWidth;
float _NearClipHeight;
float _NearClipPlane;

#if USE_THIN_LENS
float _ThinLensFocal;
float _ThinLensRadius;
#endif

float4x4 _PathTracerCameraToWorld;

#if SAMPLE_DIRECT_LIGHT
void DirectionalSample(RaycastHit hit, out float3 lightDir, out float pdf)
{
	float3 p;
	float3 n;
	SamplePointOnLight(p, n);
	float3 ltp = p - hit.position;

	lightDir = normalize(ltp);

	float ndl = dot(n, -1.0 * lightDir);

	if (ndl <= 0.0)
	{
		pdf = 0.0;
		return;
	}

	float invG = dot(ltp, ltp) / ndl;

	pdf = invG * _LightPDF;
}
#endif

void CosineSample(RaycastHit hit, out float3 lightDir, out float pdf)
{
	float3 sp = SampleHemiSphere(float2(Rand(), Rand()));
	lightDir = ONB(hit.normal.xyz, sp);
	pdf = sp.z * PT_INVPI;
}

float3 ScreenSpaceToCameraSpace(float2 uv)
{
	float x = (uv.x * 2.0 - 1.0) * _NearClipWidth;
	float y = (uv.y * 2.0 - 1.0) * _NearClipHeight;

	return float3(x, y, _NearClipPlane);
}

Ray CameraSpaceToWorldRay(float3 cameraSpacePos)
{
	Ray ray;
	ray.start = mul(_PathTracerCameraToWorld, float4(cameraSpacePos.xyz, 1.0)).xyz;
	ray.direction = normalize(mul((float3x3)_PathTracerCameraToWorld, cameraSpacePos));
	return ray;
}

Ray ScreenSpaceToWorldRay(float2 uv)
{
#if USE_THIN_LENS
	float x = (uv.x * 2.0 - 1.0) * _NearClipWidth;
	float y = (uv.y * 2.0 - 1.0) * _NearClipHeight;

	float per = _ThinLensFocal / _NearClipPlane;
	x = x * per;
	y = y * per;
	float3 p = float3(x, y, _ThinLensFocal);

	float2 disk = SampleUnitDisk(float2(Rand(), Rand()));

	float3 ori = float3(disk.x * _ThinLensRadius, disk.y * _ThinLensRadius, 0);

	float3 dir = normalize(p - ori);
	Ray ray;
	ray.start = mul(_PathTracerCameraToWorld, float4(ori.xyz, 1.0)).xyz;
	ray.direction = normalize(mul((float3x3)_PathTracerCameraToWorld, dir));
	return ray;
#else
	return CameraSpaceToWorldRay(ScreenSpaceToCameraSpace(uv));
#endif
}

#if PRIMITIVE_HAS_UV
#define INITIALIZE_RAY_CAST_HIT \
	RaycastHit hit; \
	hit.distance = PT_FLT_MAX; \
	hit.position = 0; \
	hit.normal = 0; \
	hit.matId = 0; \
	hit.uv = 0; \

#else
#define INITIALIZE_RAY_CAST_HIT \
	RaycastHit hit; \
	hit.distance = PT_FLT_MAX; \
	hit.position = 0; \
	hit.normal = 0; \
	hit.matId = 0; \

#endif

#define IMPLEMENT_PATH_TRACING(NAME, RAY_MISS_SHADER, RAY_HIT_SHADER) \
float4 Tracing_##NAME##_Func(float2 uv) \
{ \
	Ray ray = ScreenSpaceToWorldRay(uv); \
	INITIALIZE_RAY_CAST_HIT \
	int isHit = -1; \
	int continueTracing = -1; \
	float3 radiance = 0.0; \
	float4 color = float4(1, 1, 1, 1); \
	Ray newray = (Ray)0; \
	newray.start = float3(0,0,0); \
	newray.direction = float3(0,0,0); \
	[unroll] \
	for (int i = 0; i <= MAX_DEPTH; i++) { \
		isHit = SceneTracing(ray, hit); \
		[branch] \
		if (isHit < 0) \
		{ \
			color.rgb *= RAY_MISS_SHADER(ray); \
			break; \
		} \
		if (i == MAX_DEPTH) \
		{ \
			color.rgb *= 0; \
			break; \
		} \
		continueTracing = RAY_HIT_SHADER(ray, hit, radiance, newray); \
		color.rgb *= radiance; \
		[branch] \
		if (continueTracing < 0) \
		{ \
			break; \
		} \
		ray = newray; \
		hit.distance = PT_FLT_MAX; \
	} \
	return color; \
}

#define EXECUTE_PATH_TRACING(NAME, TEXCOORD) \
	Tracing_##NAME##_Func(TEXCOORD)

#endif