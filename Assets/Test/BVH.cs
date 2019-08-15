using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BVH
{
	private int m_MaxDepth;

	private enum AxisType
	{
		X,
		Y,
		Z,
	}

	private BVHNode m_Root;

	public BVH(int maxDepth)
	{
		m_MaxDepth = maxDepth;
	}

	public void Build(List<Triangle> triangles)
	{
		m_Root = BuildBVH(triangles, 0);
	}

	public List<BVHNode> GetLinearNodes()
	{
		List<BVHNode> nodes = new List<BVHNode>();
		m_Root.GetLinearNodes(nodes);
		return nodes;
	}

	private BVHNode BuildBVH(List<Triangle> triangles, int depth)
	{
		Bounds bounds = GetTrianglesBounds(triangles);
		if (depth == m_MaxDepth)
		{
			return new BVHNode(triangles, bounds);
		}

		AxisType atype = AxisType.X;

		if (bounds.size.y >= bounds.size.x && bounds.size.y >= bounds.size.z)
		{
			atype = AxisType.Y;
		}
		else if (bounds.size.z >= bounds.size.x && bounds.size.z >= bounds.size.y)
		{
			atype = AxisType.Z;
		}

		float value = GetSplitAxis(triangles, atype);

		List<Triangle> negative = new List<Triangle>();
		List<Triangle> positive = new List<Triangle>();

		for (int i = 0; i < triangles.Count; i++)
		{
			SplitTriangle(triangles[i], negative, positive, atype, value);
		}

		BVHNode left = BuildBVH(negative, depth + 1);
		BVHNode right = BuildBVH(positive, depth + 1);

		return new BVHNode(left, right, bounds);
	}

	private float GetSplitAxis(List<Triangle> triangles, AxisType axisType)
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

	private void SplitTriangle(Triangle triangle, List<Triangle> negative, List<Triangle> positive, AxisType axis,
		float value)
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

		if (value0 <= value)
			maxnvalue = Mathf.Max(maxnvalue, value - value0);
		else
			maxpvalue = Mathf.Max(maxpvalue, value0 - value);
		if (value1 <= value)
			maxnvalue = Mathf.Max(maxnvalue, value - value1);
		else
			maxpvalue = Mathf.Max(maxpvalue, value1 - value);
		if (value2 <= value)
			maxnvalue = Mathf.Max(maxnvalue, value - value2);
		else
			maxpvalue = Mathf.Max(maxpvalue, value2 - value);
		if (maxnvalue < maxpvalue)
			positive.Add(triangle);
		else
			negative.Add(triangle);
	}

	private Bounds GetTrianglesBounds(List<Triangle> triangles)
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
		Vector3 center = min + size * 0.5f;
		return new Bounds(center, size);
	}
}

public class BVHNode
{
	public bool IsLeaf
	{
		get { return m_IsLeaf; }
	}

	public Bounds Bounds
	{
		get { return m_Bounds; }
	}

	public List<Triangle> Triangles
	{
		get { return m_Triangles; }
	}

	public BVHNode LeftNode
	{
		get { return m_Left; }
	}

	public BVHNode RightNode
	{
		get { return m_Right; }
	}

	private BVHNode m_Left;
	private BVHNode m_Right;

	private bool m_IsLeaf;

	private List<Triangle> m_Triangles;

	private Bounds m_Bounds;

	public BVHNode(List<Triangle> triangles, Bounds bounds)
	{
		m_Triangles = triangles;
		m_IsLeaf = true;

		bounds = FixBounds(bounds);

		m_Bounds = bounds;
	}

	public BVHNode(BVHNode left, BVHNode right, Bounds bounds)
	{
		m_Left = left;
		m_Right = right;

		bounds = FixBounds(bounds);

		m_Bounds = bounds;
	}

	private Bounds FixBounds(Bounds bounds)
	{
		float sizex = bounds.size.x;
		float sizey = bounds.size.y;
		float sizez = bounds.size.z;
		if (sizex <= 0.00001f)
			sizex = 0.1f;
		if (sizey <= 0.00001f)
			sizey = 0.1f;
		if (sizez <= 0.00001f)
			sizez = 0.1f;

		return new Bounds(bounds.center, new Vector3(sizex, sizey, sizez));
	}

	public void GetLinearNodes(List<BVHNode> nodes)
	{
		nodes.Add(this);
		if (!m_IsLeaf)
		{
			m_Left.GetLinearNodes(nodes);
			m_Right.GetLinearNodes(nodes);
		}
	}
}