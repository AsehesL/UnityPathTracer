using UnityEngine;
using System.Collections.Generic;

public static class LBVH
{
	private enum AxisType
	{
		X,
		Y,
		Z,
	}

	public static int BuildBVH(List<Triangle> triangles, int depth, int maxDepth, ref List<Node> tree,
		ref List<Triangle> datas)
	{
		if (tree == null)
			tree = new List<Node>();
		if (datas == null)
			datas = new List<Triangle>();

		if (triangles == null || triangles.Count == 0)
			return -1;
		if (depth >= maxDepth)
		{
			int dtbegin = datas.Count;
			int dtend = datas.Count + triangles.Count - 1;
			datas.AddRange(triangles);
			tree.Add(CreateLeaf(triangles, dtbegin, dtend));
			return tree.Count - 1;
		}

		AxisType atype = GetSplitAxis(triangles);

		float coord = GetSplitCoord(triangles, atype);

		List<Triangle> negative = new List<Triangle>();
		List<Triangle> positive = new List<Triangle>();

		for (int i = 0; i < triangles.Count; i++)
		{
			SplitTriangle(triangles[i], negative, positive, atype, coord);
		}

		int negativenode = BuildBVH(negative, depth + 1, maxDepth, ref tree, ref datas);
		int positivenode = BuildBVH(negative, depth + 1, maxDepth, ref tree, ref datas);

		if ((negativenode < 0 || negativenode >= tree.Count) && (positivenode < 0 || positivenode >= tree.Count))
			return -1;


		tree.Add(CreateNode(negativenode, negativenode, tree));
		return tree.Count - 1;
	}

	private static void SplitTriangle(Triangle triangle, List<Triangle> negative, List<Triangle> positive, AxisType axis,
		float coord)
	{
		float maxnvalue = 0.0f;
		float maxpvalue = 0.0f;

		float value0 = 0.0f, value1 = 0.0f, value2 = 0.0f;
		if (axis == AxisType.X)
		{
			value0 = triangle.vertex0.x;
			value1 = triangle.vertex1.x;
			value2 = triangle.vertex2.x;
		}
		else if (axis == AxisType.Y)
		{
			value0 = triangle.vertex0.y;
			value1 = triangle.vertex1.y;
			value2 = triangle.vertex2.y;
		}
		else if (axis == AxisType.Z)
		{
			value0 = triangle.vertex0.z;
			value1 = triangle.vertex1.z;
			value2 = triangle.vertex2.z;
		}

		if (value0 <= coord)
			maxnvalue = Mathf.Max(maxnvalue, coord - value0);
		else
			maxpvalue = Mathf.Max(maxpvalue, value0 - coord);
		if (value1 <= coord)
			maxnvalue = Mathf.Max(maxnvalue, coord - value1);
		else
			maxpvalue = Mathf.Max(maxpvalue, value1 - coord);
		if (value2 <= coord)
			maxnvalue = Mathf.Max(maxnvalue, coord - value2);
		else
			maxpvalue = Mathf.Max(maxpvalue, value2 - coord);
		if (maxnvalue < maxpvalue)
			positive.Add(triangle);
		else
			negative.Add(triangle);
	}

	private static AxisType GetSplitAxis(List<Triangle> triangles)
	{
		Vector3 min = Vector3.one * Mathf.Infinity;
		Vector3 max = -Vector3.one * Mathf.Infinity;
		for (int i = 0; i < triangles.Count; i++)
		{
			min = Vector3.Min(triangles[i].vertex0, min);
			min = Vector3.Min(triangles[i].vertex1, min);
			min = Vector3.Min(triangles[i].vertex2, min);
			max = Vector3.Max(triangles[i].vertex0, max);
			max = Vector3.Max(triangles[i].vertex1, max);
			max = Vector3.Max(triangles[i].vertex2, max);
		}

		Vector3 size = max - min;
		if (size.y >= size.x && size.y >= size.z)
			return AxisType.Y;
		else if (size.z >= size.x && size.z >= size.y)
			return AxisType.Z;
		else
			return AxisType.X;
	}

	private static float GetSplitCoord(List<Triangle> triangles, AxisType axisType)
	{
		float min = 0, max = 0;
		for (int i = 0; i < triangles.Count; i++)
		{
			float cmin = 0;
			float cmax = 0;
			if (axisType == AxisType.X)
				cmin = Mathf.Min(triangles[i].vertex0.x, triangles[i].vertex1.x, triangles[i].vertex2.x);
			else if (axisType == AxisType.Y)
				cmin = Mathf.Min(triangles[i].vertex0.y, triangles[i].vertex1.y, triangles[i].vertex2.y);
			else if (axisType == AxisType.Z)
				cmin = Mathf.Min(triangles[i].vertex0.z, triangles[i].vertex1.z, triangles[i].vertex2.z);

			if (axisType == AxisType.X)
				cmax = Mathf.Max(triangles[i].vertex0.x, triangles[i].vertex1.x, triangles[i].vertex2.x);
			else if (axisType == AxisType.Y)
				cmax = Mathf.Max(triangles[i].vertex0.y, triangles[i].vertex1.y, triangles[i].vertex2.y);
			else if (axisType == AxisType.Z)
				cmax = Mathf.Max(triangles[i].vertex0.z, triangles[i].vertex1.z, triangles[i].vertex2.z);

			min += cmin;
			max += cmax;
		}

		min /= triangles.Count;
		max /= triangles.Count;
		return (min + max) * 0.5f;
	}

	private static Node CreateLeaf(List<Triangle> triangles, int dataBegin, int dataEnd)
	{
		Vector3 min = Vector3.one * float.MaxValue;
		Vector3 max = -Vector3.one * float.MaxValue;

		for (int i = 0; i < triangles.Count; i++)
		{
			min = Vector3.Min(min, triangles[i].vertex0);
			min = Vector3.Min(min, triangles[i].vertex1);
			min = Vector3.Min(min, triangles[i].vertex2);
			max = Vector3.Max(max, triangles[i].vertex0);
			max = Vector3.Max(max, triangles[i].vertex1);
			max = Vector3.Max(max, triangles[i].vertex2);
		}

		Vector3 si = max - min;
		Vector3 ct = (min + max) * 0.5f;

		if (si.x <= 0)
			si.x = 0.1f;
		if (si.y <= 0)
			si.y = 0.1f;
		if (si.z <= 0)
			si.z = 0.1f;

		min = ct - si * 0.5f;
		max = ct + si * 0.5f;

		return new Node
		{
			min = min,
			max = max,
			leftChild = -1,
			rightChild = -1,
			dataBegin = dataBegin,
			dataEnd = dataEnd,
		};
	}

	private static Node CreateNode(int left, int right, List<Node> nodes)
	{
		Vector3 min = Vector3.one * float.MaxValue;
		Vector3 max = Vector3.one * -float.MaxValue;

		if (left >= 0 && left < nodes.Count)
		{
			Node leftnode = nodes[left];
			min = Vector3.Min(min, leftnode.min);
			max = Vector3.Max(max, leftnode.max);
		}

		if (right >= 0 && right < nodes.Count)
		{
			Node rightnode = nodes[right];
			min = Vector3.Min(min, rightnode.min);
			max = Vector3.Max(max, rightnode.max);
		}

		Vector3 si = max - min;
		Vector3 ct = (min + max) * 0.5f;

		if (si.x <= 0)
			si.x = 0.1f;
		if (si.y <= 0)
			si.y = 0.1f;
		if (si.z <= 0)
			si.z = 0.1f;

		min = ct - si * 0.5f;
		max = ct + si * 0.5f;


		return new Node
		{
			min = min,
			max = max,
			dataBegin = -1,
			dataEnd = -1,
			leftChild = left,
			rightChild = right,
		};
	}
}
