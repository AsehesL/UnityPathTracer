using UnityEngine;
using System.Collections.Generic;

public static class LBVH
{

	public static int BuildBVH(List<Triangle> triangles, Bounds bounds, ref List<Node> tree,
		ref List<Triangle> datas)
	{
		tree = new List<Node>();
		List<uint> sortedMortons = new List<uint>();
		for (int i = 0; i < triangles.Count; i++)
		{
			Vector3 center =
				0.5f * (Vector3.Min(Vector3.Min(triangles[i].vertex0, triangles[i].vertex1), triangles[i].vertex2) +
				        Vector3.Max(Vector3.Max(triangles[i].vertex0, triangles[i].vertex1), triangles[i].vertex2));
			float x = (center.x - bounds.min.x) / bounds.size.x;
			float y = (center.y - bounds.min.y) / bounds.size.y;
			float z = (center.z - bounds.min.z) / bounds.size.z;
			uint morton = Morton3D(x, y, z);
			sortedMortons.Add(morton);
		}
		Sort(triangles, sortedMortons);

		datas = triangles;
		return GenerateHierarchy(triangles, sortedMortons, 0, sortedMortons.Count - 1, ref tree);
	}

	private static int GenerateHierarchy(List<Triangle> sortedTriangles, List<uint> sortedMortons, int first, int last, ref List<Node> tree)
	{
		if (first == last)
		{
			Node node = new Node
			{
				min = Vector3.Min(Vector3.Min(sortedTriangles[first].vertex0, sortedTriangles[first].vertex1),
					sortedTriangles[first].vertex2),
				max = Vector3.Max(Vector3.Max(sortedTriangles[first].vertex0, sortedTriangles[first].vertex1),
					sortedTriangles[first].vertex2),
				leftChild = -1,
				rightChild = -1,
				dataIndex = first,
			};
			tree.Add(node);
			return tree.Count - 1;
		}

		int split = FindSplit(sortedMortons, first, last);

		int child1 = GenerateHierarchy(sortedTriangles, sortedMortons, first, split, ref tree);
		int child2 = GenerateHierarchy(sortedTriangles, sortedMortons, split + 1, last, ref tree);

		Node child1Node = tree[child1];
		Node child2Node = tree[child2];

		Node child = new Node
		{
			min = Vector3.Min(child1Node.min, child2Node.min),
			max = Vector3.Max(child1Node.max, child2Node.max),
			leftChild = child1,
			rightChild = child2,
			dataIndex = -1,
		};

		tree.Add(child);
		return tree.Count - 1;
	}

	private static int FindSplit(List<uint> sortedMortons, int first, int last)
	{
		uint firstCode = sortedMortons[first];
		uint lastCode = sortedMortons[last];

		if (firstCode == lastCode)
			return (first + last) >> 1;

		int commonPrefix = CountLeadingZeros(firstCode ^ lastCode);

		int split = first;
		int step = last - first;

		do
		{
			step = (step + 1) >> 1;
			int newSplit = split + step;

			if (newSplit < last)
			{
				uint splitCode = sortedMortons[newSplit];
				int splitPrefix = CountLeadingZeros(firstCode ^ splitCode);
				if (splitPrefix > commonPrefix)
					split = newSplit;
			}
		}
		while (step > 1);

		return split;
	}

	private static int CountLeadingZeros(uint i)
	{
		int ret = 0;
		uint temp = ~i;

		while ((temp & 0x80000000) > 0)
		{
			temp <<= 1;
			ret++;
		}
		return ret;
	}

	private static void Sort(List<Triangle> sortedDatas, List<uint> sortedMortons)
	{
		QuickSort(sortedDatas, sortedMortons, 0, sortedMortons.Count - 1);
	}

	private static void QuickSort(List<Triangle> sortedDatas, List<uint> sortedMortons, int low, int high)
	{
		int pivot;
		if (low < high)
		{
			pivot = Partition(sortedDatas, sortedMortons, low, high);

			QuickSort(sortedDatas, sortedMortons, low, pivot - 1);
			QuickSort(sortedDatas, sortedMortons, pivot + 1, high);
		}
	}

	private static int Partition(List<Triangle> sortedDatas, List<uint> sortedMortons, int low, int high)
	{
		uint pivotkey = sortedMortons[low];
		while (low < high)
		{
			while (low < high && sortedMortons[high] >= pivotkey)
				high--;
			Swap(sortedDatas, sortedMortons, low, high);
			while (low < high && sortedMortons[low] <= pivotkey)
				low++;
			Swap(sortedDatas, sortedMortons, low, high);
		}
		return low;
	}

	private static void Swap(List<Triangle> sortedDatas, List<uint> sortedMortons, int a, int b)
	{
		var tempData = sortedDatas[a];
		uint tempMorton = sortedMortons[a];
		sortedDatas[a] = sortedDatas[b];
		sortedDatas[b] = tempData;
		sortedMortons[a] = sortedMortons[b];
		sortedMortons[b] = tempMorton;
	}

	private static uint ExpandBits(uint v)
	{
		v = (v * 0x00010001u) & 0xFF0000FFu;
		v = (v * 0x00000101u) & 0x0F00F00Fu;
		v = (v * 0x00000011u) & 0xC30C30C3u;
		v = (v * 0x00000005u) & 0x49249249u;
		return v;
	}

	/// <summary>
	/// 计算莫顿码
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	private static uint Morton3D(float x, float y, float z)
	{
		x = Mathf.Min(Mathf.Max(x * 1024.0f, 0.0f), 1023.0f);
		y = Mathf.Min(Mathf.Max(y * 1024.0f, 0.0f), 1023.0f);
		z = Mathf.Min(Mathf.Max(z * 1024.0f, 0.0f), 1023.0f);
		uint xx = ExpandBits((uint)x);
		uint yy = ExpandBits((uint)y);
		uint zz = ExpandBits((uint)z);
		return xx * 4 + yy * 2 + zz;
	}
}
