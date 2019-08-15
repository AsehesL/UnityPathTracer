using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnlyHitTest : MonoBehaviour
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
                triangles.Add(new Triangle(v0, v1, v2));
            }
        }

        List<Triangle> datas = null;
        List<LKDNode> nodes = null;
        int root = LKDNode.BuildKDTree(triangles, 0, 5, ref nodes, ref datas);

        int sizeofnode = System.Runtime.InteropServices.Marshal.SizeOf(typeof(LKDNode));
        int sizeoftriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));

        m_ArgsBuffer = new ComputeBuffer(1, m_Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_NodesBuffer = new ComputeBuffer(nodes.Count, sizeofnode);
        m_ResultBuffer = new ComputeBuffer(datas.Count, sizeoftriangle);
        m_TrianglesBuffer = new ComputeBuffer(datas.Count, sizeoftriangle);

        m_DispatchCount = Mathf.CeilToInt((float)nodes.Count / 16);

        m_KernelIndex = computeShader.FindKernel("CSMain");

        m_NodesBuffer.SetData(nodes.ToArray());
        m_TrianglesBuffer.SetData(datas.ToArray());
        m_ArgsBuffer.SetData(m_Args);

        computeShader.SetBuffer(m_KernelIndex, "_Tree", m_NodesBuffer);
        computeShader.SetBuffer(m_KernelIndex, "_Args", m_ArgsBuffer);
        computeShader.SetBuffer(m_KernelIndex, "_Result", m_ResultBuffer);
        computeShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
        computeShader.SetInt("_NodeCounts", nodes.Count);
        computeShader.SetInt("_RootNode", root);

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
        if (m_Material)
            Destroy(m_Material);
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
    }
}
