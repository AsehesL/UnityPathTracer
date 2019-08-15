﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GPUBVHHitTest : MonoBehaviour
{
	public ComputeShader computeShader;
	public Shader shader;

	private ComputeBuffer m_NodesBuffer;
	private ComputeBuffer m_TrianglesBuffer;
	private ComputeBuffer m_ArgsBuffer;
	private ComputeBuffer m_ResultBuffer;

	private uint[] m_Args = { 1, 0, 0, 0 };

	private int m_DispatchCount;

	private int m_KernelIndex;

	private Material m_Material;

	void Start()
	{
		m_Material = new Material(shader);

		MeshFilter[] mfs = FindObjectsOfType<MeshFilter>();

		List<Triangle> triangles = new List<Triangle>();

		for (int i = 0; i < mfs.Length; i++)
		{
			Vector3[] vlist = mfs[i].sharedMesh.vertices;
			Vector3[] nlist = mfs[i].sharedMesh.normals;
			Vector2[] ulist = mfs[i].sharedMesh.uv;
			int[] ilist = mfs[i].sharedMesh.triangles;

			for (int j = 0; j < ilist.Length; j += 3)
			{
				Vertex v0 = new Vertex
				{
					vertex = mfs[i].transform.localToWorldMatrix.MultiplyPoint(vlist[ilist[j]]),
					normal = nlist[ilist[j]],
				};
				Vertex v1 = new Vertex
				{
					vertex = mfs[i].transform.localToWorldMatrix.MultiplyPoint(vlist[ilist[j + 1]]),
					normal = nlist[ilist[j + 1]],
				};
				Vertex v2 = new Vertex
				{
					vertex = mfs[i].transform.localToWorldMatrix.MultiplyPoint(vlist[ilist[j + 2]]),
					normal = nlist[ilist[j + 2]],
				};
				triangles.Add(new Triangle (v0, v1, v2));
			}
		}

		BVH bvh = new BVH(5);
		bvh.Build(triangles);

		List<BVHNode> linearNodes = bvh.GetLinearNodes();

		List<Triangle> bvhtriangles = new List<Triangle>();
		List<LKDNode> bvhnodes = new List<LKDNode>();

		for (int i = 0; i < linearNodes.Count; i++)
		{
			int leftindex = -1;
			int rightindex = -1;
			int begin = -1;
			int end = -1;
			if (linearNodes[i].IsLeaf)
			{
				begin = bvhtriangles.Count;
				end = bvhtriangles.Count + linearNodes[i].Triangles.Count - 1;
				for (int j = 0; j < linearNodes[i].Triangles.Count; j++)
				{
					bvhtriangles.Add(linearNodes[i].Triangles[j]);
				}
			}
			else
			{
				leftindex = linearNodes.IndexOf(linearNodes[i].LeftNode);
				rightindex = linearNodes.IndexOf(linearNodes[i].RightNode);
			}

		    LKDNode node = new LKDNode
            {
				min = linearNodes[i].Bounds.min,
				max = linearNodes[i].Bounds.max,
				dataBegin = begin,
				dataEnd = end,
				leftChild = leftindex,
				rightChild = rightindex
			};
			bvhnodes.Add(node);
		}

		int sizeofnode = System.Runtime.InteropServices.Marshal.SizeOf(typeof(LKDNode));
		int sizeoftriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));

		m_ArgsBuffer = new ComputeBuffer(1, m_Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		m_NodesBuffer = new ComputeBuffer(bvhnodes.Count, sizeofnode);
		m_ResultBuffer = new ComputeBuffer(bvhtriangles.Count, sizeoftriangle);
		m_TrianglesBuffer = new ComputeBuffer(bvhtriangles.Count, sizeoftriangle);

		m_DispatchCount = Mathf.CeilToInt((float)bvhnodes.Count / 16);

		m_KernelIndex = computeShader.FindKernel("CSMain");

		m_NodesBuffer.SetData(bvhnodes.ToArray());
		m_TrianglesBuffer.SetData(bvhtriangles.ToArray());
		m_ArgsBuffer.SetData(m_Args);

		computeShader.SetBuffer(m_KernelIndex, "_Tree", m_NodesBuffer);
		computeShader.SetBuffer(m_KernelIndex, "_Args", m_ArgsBuffer);
		computeShader.SetBuffer(m_KernelIndex, "_Result", m_ResultBuffer);
		computeShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
		computeShader.SetInt("_NodeCounts", bvhnodes.Count);
		computeShader.SetInt("_RootNode", 0);

		m_Material.SetBuffer("_Result", m_ResultBuffer);
	}
	
	void OnDestroy()
	{
		if (m_NodesBuffer != null)
			m_NodesBuffer.Release();
		if (m_ArgsBuffer != null)
			m_ArgsBuffer.Release();
		if (m_ResultBuffer != null)
			m_ResultBuffer.Release();
		if (m_TrianglesBuffer != null)
			m_TrianglesBuffer.Release();
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			computeShader.SetVector("_Origin", ray.origin);
			computeShader.SetVector("_Direction", ray.direction);
			computeShader.Dispatch(m_KernelIndex, m_DispatchCount, 1, 1);
		}
		
	}

	void OnRenderObject()
	{
		if (Camera.current != Camera.main)
			return;
		m_Material.SetPass(0);
		Graphics.DrawProceduralIndirect(MeshTopology.Points, m_ArgsBuffer);

		//m_ArgsBuffer.SetData(m_Args);
	}
}
