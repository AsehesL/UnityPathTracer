#ifndef BVH_CGINC
#define BVH_CGINC

#include "Defines.cginc"
#include "Geometry.cginc"

struct BVHNode {
	float3 boundsMin;
	float3 boundsMax;
	int leftChild;
	int rightChild;
	int primitiveId;
	int primitiveType;
};

StructuredBuffer<BVHNode> _BVHTree;
int _BVHRootNodeIndex;
int _BVHNodeCount;

StructuredBuffer<Sphere> _Spheres;
StructuredBuffer<Quad> _Quads;
StructuredBuffer<Cube> _Cubes;
StructuredBuffer<Triangle> _Triangles;

int RaycastPrimitive(Ray ray, int primitiveId, int primitiveType, inout RaycastHit hit)
{
	if (primitiveType == PRIMITIVE_SPHERE)
		return RaycastSphere(ray, _Spheres[primitiveId], hit);
	if (primitiveType == PRIMITIVE_QUAD)
		return RaycastQuad(ray, _Quads[primitiveId], hit);
	if (primitiveType == PRIMITIVE_CUBE)
		return RaycastCube(ray, _Cubes[primitiveId], hit);
	if (primitiveType == PRIMITIVE_TRIANGLE)
		return RaycastTriangle(ray, _Triangles[primitiveId], hit);
	return -1;
}

int RaycastBounds(Ray ray, float3 bmin, float3 bmax, out float hitdistance) {

	float tmin = -999999999;
	float tmax = 999999999;
	float t1, t2;

	if (ray.direction.x != 0.0) {
		t1 = (bmin.x - ray.start.x) / ray.direction.x;
		t2 = (bmax.x - ray.start.x) / ray.direction.x;
		tmin = max(tmin, min(t1, t2));
		tmax = min(tmax, max(t1, t2));
	}
	if (ray.direction.y != 0.0) {
		t1 = (bmin.y - ray.start.y) / ray.direction.y;
		t2 = (bmax.y - ray.start.y) / ray.direction.y;
		tmin = max(tmin, min(t1, t2));
		tmax = min(tmax, max(t1, t2));
	}
	if (ray.direction.z != 0.0) {
		t1 = (bmin.z - ray.start.z) / ray.direction.z;
		t2 = (bmax.z - ray.start.z) / ray.direction.z;
		tmin = max(tmin, min(t1, t2));
		tmax = min(tmax, max(t1, t2));
	}
	if (tmax >= tmin) {
		hitdistance = tmin;
		return 1;
	}
	hitdistance = 0;
	return -1;
}

int BVHTracing(Ray ray, inout RaycastHit hit)
{
	if (_BVHNodeCount == 0)
		return -1;

	BVHNode stack[100];
	BVHNode node, leftChild, rightChild;
	node = _BVHTree[_BVHRootNodeIndex];
	int stackCount = 0;

	float leftDis, rightDis;
	int isHitLeft = RaycastBounds(ray, node.boundsMin, node.boundsMax, leftDis);
	int isHitRight = -1;
	int isHit = -1;
	if (isHitLeft > 0) {
		stack[0] = node;
		stackCount = 1;
	}

	while (stackCount > 0 && stackCount < 100) { 
		node = stack[stackCount - 1];

		stackCount -= 1;

		if (node.primitiveId < 0) {
			leftChild = _BVHTree[node.leftChild];
			rightChild = _BVHTree[node.rightChild];
			isHitLeft = RaycastBounds(ray, leftChild.boundsMin, leftChild.boundsMax, leftDis);
			isHitRight = RaycastBounds(ray, rightChild.boundsMin, rightChild.boundsMax, rightDis);
			if (isHitLeft > 0 && isHitRight > 0) {
				if (leftDis < rightDis) {
					stack[stackCount] = _BVHTree[node.rightChild];
					stack[stackCount + 1] = _BVHTree[node.leftChild];
					stackCount += 2;
				}
				else {
					stack[stackCount] = _BVHTree[node.leftChild];
					stack[stackCount + 1] = _BVHTree[node.rightChild];
					stackCount += 2;
				}
			}
			else if (isHitLeft > 0) {
				stack[stackCount] = _BVHTree[node.leftChild];
				stackCount += 1;
			}
			else if (isHitRight > 0) {
				stack[stackCount] = _BVHTree[node.rightChild];
				stackCount += 1;
			}
		}
		else {
			if (RaycastPrimitive(ray, node.primitiveId, node.primitiveType, hit) > 0) {
				isHit = 1;
			}
		}
	}
	return isHit;
}

#endif