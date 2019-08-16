using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class Utils
{

	public static List<Triangle> GetTrianglesInScene(bool destroyOriginMesh)
	{
		List<Triangle> triangles = new List<Triangle>();

		MeshFilter[] meshFilters = Object.FindObjectsOfType<MeshFilter>();
		for (int i = 0; i < meshFilters.Length; i++)
		{
			GetTrianglesFromMesh(triangles, meshFilters[i].sharedMesh, meshFilters[i].transform.localToWorldMatrix);
		}

		if (destroyOriginMesh)
		{
			for (int i = 0; i < meshFilters.Length; i++)
			{
				if (meshFilters[i].gameObject)
					Object.Destroy(meshFilters[i].gameObject);
			}
		}

		return triangles;
	}

	private static void GetTrianglesFromMesh(List<Triangle> trianglelist, Mesh mesh, Matrix4x4 matrix)
	{
		int[] triangles = mesh.triangles;
		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;

		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector3 vertex0 = matrix.MultiplyPoint(vertices[triangles[i]]);
			Vector3 vertex1 = matrix.MultiplyPoint(vertices[triangles[i + 1]]);
			Vector3 vertex2 = matrix.MultiplyPoint(vertices[triangles[i + 2]]);
			Vector3 normal0 = matrix.MultiplyVector(normals[triangles[i]]);
			Vector3 normal1 = matrix.MultiplyVector(normals[triangles[i + 1]]);
			Vector3 normal2 = matrix.MultiplyVector(normals[triangles[i + 2]]);

			Triangle triangle = new Triangle
			{
				vertex0 = vertex0,
				vertex1 = vertex1,
				vertex2 = vertex2,
				normal0 = normal0,
				normal1 = normal1,
				normal2 = normal2,
			};
			trianglelist.Add(triangle);
		}
	}
}
