using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BVHNode
{
    public Vector3 boundsMin;
    public Vector3 boundsMax;
    public int leftChild;
    public int rightChild;
    public int primitiveId;
    public int primitiveType;
}

public static class BVH
{
    class Node
    {
        public Bounds bounds;
        public int leftChild;
        public int rightChild;
        public Primitive primitive;
    }

    public static int Build(List<Primitive> primitives, out List<BVHNode> tree, out List<Sphere> spheres, out List<Quad> quads, out List<Cube> cubes, out List<Triangle> triangles)
    {
        if (primitives == null || primitives.Count == 0)
        {
            tree = null;
            spheres = null;
            quads = null;
            triangles = null;
            cubes = null;
            return -1;
        }
        Bounds bounds = primitives[0].bounds;
        for (int i = 1; i < primitives.Count; i++)
        {
            bounds.Encapsulate(primitives[i].bounds);
        }

        List<uint> sortedMortons = new List<uint>();
        for (int i = 0; i < primitives.Count; i++)
        {
            Vector3 center = primitives[i].bounds.center;
            float x = (center.x - bounds.min.x) / bounds.size.x;
            float y = (center.y - bounds.min.y) / bounds.size.y;
            float z = (center.z - bounds.min.z) / bounds.size.z;
            uint morton = Morton3D(x, y, z);
            sortedMortons.Add(morton);
        }
        Sort(primitives, sortedMortons);

        List<Node> nodes = new List<Node>();
        int rootIndex = GenerateHierarchy(primitives, sortedMortons, 0, sortedMortons.Count - 1, ref nodes);

        tree = new List<BVHNode>();
        spheres = new List<Sphere>();
        quads = new List<Quad>();
        cubes = new List<Cube>();
        triangles = new List<Triangle>();
        for (int i = 0; i < nodes.Count; i++)
        {
            int primitiveId = -1;
            int primitiveType = 0;
            if (nodes[i].primitive != null)
            {
                if (nodes[i].primitive.primitiveType == PrimitiveType.Sphere)
                {
                    primitiveId = spheres.Count;
                    primitiveType = (int)PrimitiveType.Sphere;
                    spheres.Add(nodes[i].primitive.CreateSphere());
                }
                else if (nodes[i].primitive.primitiveType == PrimitiveType.Quad)
                {
                    primitiveId = quads.Count;
                    primitiveType = (int)PrimitiveType.Quad;
                    quads.Add(nodes[i].primitive.CreateQuad());
                }
                else if (nodes[i].primitive.primitiveType == PrimitiveType.Cube)
                {
                    primitiveId = cubes.Count;
                    primitiveType = (int)PrimitiveType.Cube;
                    cubes.Add(nodes[i].primitive.CreateCube());
                }
                else if (nodes[i].primitive.primitiveType == PrimitiveType.Triangle)
                {
                    primitiveId = triangles.Count;
                    primitiveType = (int)PrimitiveType.Triangle;
                    triangles.Add(nodes[i].primitive.CreateTriangle());
                }
            }
            tree.Add(new BVHNode
            {
                boundsMin = nodes[i].bounds.min,
                boundsMax = nodes[i].bounds.max,
                leftChild = nodes[i].leftChild,
                rightChild = nodes[i].rightChild,
                primitiveId = primitiveId,
                primitiveType = primitiveType,
            });
        }
        return rootIndex;
    }

    private static int GenerateHierarchy(List<Primitive> sortedPrimitives, List<uint> sortedMortons, int first, int last, ref List<Node> tree)
    {
        if (first == last)
        {
            Node node = new Node
            {
                bounds = sortedPrimitives[first].bounds,
                leftChild = -1,
                rightChild = -1,
                primitive = sortedPrimitives[first],
            };
            tree.Add(node);
            return tree.Count - 1;
        }

        int split = FindSplit(sortedMortons, first, last);

        int child1 = GenerateHierarchy(sortedPrimitives, sortedMortons, first, split, ref tree);
        int child2 = GenerateHierarchy(sortedPrimitives, sortedMortons, split + 1, last, ref tree);

        Node child1Node = tree[child1];
        Node child2Node = tree[child2];

        Bounds childBounds = child1Node.bounds;
        childBounds.Encapsulate(child2Node.bounds);

        Node child = new Node
        {
            bounds = childBounds,
            leftChild = child1,
            rightChild = child2,
            primitive = null,
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

    private static void Sort(List<Primitive> sortedDatas, List<uint> sortedMortons)
    {
        QuickSort(sortedDatas, sortedMortons, 0, sortedMortons.Count - 1);
    }

    private static void QuickSort(List<Primitive> sortedDatas, List<uint> sortedMortons, int low, int high)
    {
        int pivot;
        if (low < high)
        {
            pivot = Partition(sortedDatas, sortedMortons, low, high);

            QuickSort(sortedDatas, sortedMortons, low, pivot - 1);
            QuickSort(sortedDatas, sortedMortons, pivot + 1, high);
        }
    }

    private static int Partition(List<Primitive> sortedDatas, List<uint> sortedMortons, int low, int high)
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

    private static void Swap(List<Primitive> sortedDatas, List<uint> sortedMortons, int a, int b)
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
