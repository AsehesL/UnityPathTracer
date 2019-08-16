using UnityEngine;
using System.Collections.Generic;

public static class LKDTree
{
	private enum SplitAxis
	{
		X,
		Y,
		Z
	}

	private struct SplitPlane
	{
		public SplitAxis axis;
		public float coord;
	}

	public static int BuildKDTree(List<Triangle> triangles, int depth, int maxDepth, ref List<Node> tree, ref List<Triangle> datas)
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

		SplitPlane plane = PickLKDTreePlane(triangles); //计算分割面

		List<Triangle> left = new List<Triangle>();
		List<Triangle> right = new List<Triangle>();

		for (int i = 0; i < triangles.Count; i++)
		{
			SplitTriangles(triangles[i], plane, left, right); //对三角面进行分组，如果位于分割面上，需要对三角形进行切割
		}

		int leftnode = BuildKDTree(left, depth + 1, maxDepth, ref tree, ref datas);
		int rightnode = BuildKDTree(right, depth + 1, maxDepth, ref tree, ref datas);

		if ((leftnode < 0 || leftnode >= tree.Count) && (rightnode < 0 || rightnode >= tree.Count))
			return -1;


		tree.Add(CreateNode(leftnode, rightnode, tree));
		return tree.Count - 1;
	}

	private static SplitPlane PickLKDTreePlane(List<Triangle> triangles)
	{
		Vector3 min = Vector3.one * float.MaxValue;
		Vector3 max = -Vector3.one * float.MaxValue;

		for (int i = 0; i < triangles.Count; i++)
		{
			min = Vector3.Min(min, triangles[i].vertex0);
			max = Vector3.Max(max, triangles[i].vertex0);
			min = Vector3.Min(min, triangles[i].vertex1);
			max = Vector3.Max(max, triangles[i].vertex1);
			min = Vector3.Min(min, triangles[i].vertex2);
			max = Vector3.Max(max, triangles[i].vertex2);
		}

		Vector3 size = max - min;

		SplitAxis axis = default(SplitAxis);
		float coord = default(float);
		if (size.x >= size.y && size.x >= size.z)
		{
			axis = SplitAxis.X;
			coord = min.x + size.x * 0.5f;
		}
		else if (size.y > size.x && size.y >= size.z)
		{
			axis = SplitAxis.Y;
			coord = min.y + size.y * 0.5f;
		}
		else if (size.z > size.x && size.z > size.y)
		{
			axis = SplitAxis.Z;
			coord = min.z + size.z * 0.5f;
		}

		return new SplitPlane { axis = axis, coord = coord };
	}

	private static void SplitTriangles(Triangle triangle, SplitPlane plane, List<Triangle> left,
		List<Triangle> right)
	{
		float pos0 = 0, pos1 = 0, pos2 = 0;
		if (plane.axis == SplitAxis.X)
		{
			pos0 = triangle.vertex0.x;
			pos1 = triangle.vertex1.x;
			pos2 = triangle.vertex2.x;
		}
		else if (plane.axis == SplitAxis.Y)
		{
			pos0 = triangle.vertex0.y;
			pos1 = triangle.vertex1.y;
			pos2 = triangle.vertex2.y;
		}
		else if (plane.axis == SplitAxis.Z)
		{
			pos0 = triangle.vertex0.z;
			pos1 = triangle.vertex1.z;
			pos2 = triangle.vertex2.z;
		}

		if (pos0 <= plane.coord && pos1 <= plane.coord && pos2 <= plane.coord)
		{
			left.Add(triangle);
		}
		else if (pos0 > plane.coord && pos1 > plane.coord && pos2 > plane.coord)
		{
			right.Add(triangle);
		}
		else
		{
			List<Vertex> leftVertices = new List<Vertex>();
			List<Vertex> rightVertices = new List<Vertex>();
			Vertex vertex0 = triangle.GetVertex(0);
			Vertex vertex1 = triangle.GetVertex(1);
			Vertex vertex2 = triangle.GetVertex(2);
			SplitEdge(vertex0, vertex1, plane, leftVertices, rightVertices);
			SplitEdge(vertex1, vertex2, plane, leftVertices, rightVertices);
			SplitEdge(vertex2, vertex0, plane, leftVertices, rightVertices);
			if (leftVertices.Count == 4)
			{
				left.Add(new Triangle(leftVertices[0], leftVertices[1], leftVertices[2]));
				left.Add(new Triangle(leftVertices[0], leftVertices[2], leftVertices[3]));
			}
			else if (leftVertices.Count == 3)
			{
				left.Add(new Triangle(leftVertices[0], leftVertices[1], leftVertices[2]));
			}

			if (rightVertices.Count == 4)
			{
				right.Add(new Triangle(rightVertices[0], rightVertices[1], rightVertices[2]));
				right.Add(new Triangle(rightVertices[0], rightVertices[2], rightVertices[3]));
			}
			else if (rightVertices.Count == 3)
			{
				right.Add(new Triangle(rightVertices[0], rightVertices[1], rightVertices[2]));
			}
		}
	}

	private static void SplitEdge(Vertex begin, Vertex end, SplitPlane plane, List<Vertex> leftVertices,
		List<Vertex> rightVertices)
	{
		float beginPos = 0, endPos = 0;
		if (plane.axis == SplitAxis.X)
		{
			beginPos = begin.vertex.x;
			endPos = end.vertex.x;
		}
		else if (plane.axis == SplitAxis.Y)
		{
			beginPos = begin.vertex.y;
			endPos = end.vertex.y;
		}
		else if (plane.axis == SplitAxis.Z)
		{
			beginPos = begin.vertex.z;
			endPos = end.vertex.z;
		}

		if (beginPos <= plane.coord && endPos <= plane.coord)
		{
			leftVertices.Add(begin);
		}
		else if (beginPos > plane.coord && endPos > plane.coord)
		{
			rightVertices.Add(begin);
		}
		else if (beginPos <= plane.coord && endPos > plane.coord)
		{
			leftVertices.Add(begin);
			leftVertices.Add(Vertex.Lerp(begin, end, (plane.coord - beginPos) / (endPos - beginPos)));
			rightVertices.Add(Vertex.Lerp(begin, end, (plane.coord - beginPos) / (endPos - beginPos)));
		}
		else if (beginPos > plane.coord && endPos <= plane.coord)
		{
			rightVertices.Add(begin);
			rightVertices.Add(Vertex.Lerp(begin, end, (plane.coord - beginPos) / (endPos - beginPos)));
			leftVertices.Add(Vertex.Lerp(begin, end, (plane.coord - beginPos) / (endPos - beginPos)));
		}

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
