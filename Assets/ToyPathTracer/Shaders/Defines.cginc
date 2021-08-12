#ifndef DEFINES_CGINC
#define DEFINES_CGINC

#define PT_FLT_EPSILON		1.401298E-45f
#define PT_FLT_MAX			3.40282347E+38f
#define PT_FLT_MIN			-3.40282347E+38f
#define PT_FLT_DELTA		0.001f
#define PT_PI				3.1415926535897931
#define PT_INVPI			0.31830988618379

#define PRIMITIVE_SPHERE	0
#define PRIMITIVE_QUAD		1
#define PRIMITIVE_CUBE  	2
#define PRIMITIVE_TRIANGLE	3

#define SHADING_TYPE_DIFFUSE	0
#define SHADING_TYPE_REFLECT	1
#define SHADING_TYPE_REFRACT	2
#define SHADING_TYPE_EMISSION	3

struct Ray {
	float3 start;
	float3 direction;
};

struct RaycastHit {
	float distance;
	float3 position;
	float4 normal;
#if PRIMITIVE_SAMPLE_TEXTURE
	float2 uv;
#if PRIMITIVE_HAS_TANGENT
	float4 tangent;
#endif
#endif
	int matId;
};

int _ScreenWidth;
int _ScreenHeight;

struct PTMaterial
{
	float4 albedo;
	float roughness;
	float metallic;
	int emission;
	int textureId;
	int normalTextureId;
	int mroTextureId;
};

StructuredBuffer<PTMaterial> _Materials;

#endif