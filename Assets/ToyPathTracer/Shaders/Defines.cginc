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
#define PRIMITIVE_TRIANGLE	2

#define SHADING_TYPE_DIFFUSE	0
#define SHADING_TYPE_REFLECT	1
#define SHADING_TYPE_REFRACT	2
#define SHADING_TYPE_EMISSIVE	3

struct Ray {
	float3 start;
	float3 direction;
};

struct RaycastHit {
	float distance;
	float3 position;
	float4 normal;
	int matId;
};

int _ScreenWidth;
int _ScreenHeight;

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

#endif