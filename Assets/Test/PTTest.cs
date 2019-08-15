using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTTest : MonoBehaviour
{
    public ComputeShader computeShader;

    private ComputeBuffer m_NodesBuffer;
    private ComputeBuffer m_TrianglesBuffer;

    [SerializeField]private RenderTexture m_RenderTexture;

    private int m_DispatchX;
    private int m_DispatchY;

    private int m_KernelIndex;

    void Start()
    {
        var triangles = GetTriangles();

        List<Triangle> datas = null;
        List<LKDNode> nodes = null;

        int root = LKDNode.BuildKDTree(triangles, 0, 5, ref nodes, ref datas);

        int sizeofnode = System.Runtime.InteropServices.Marshal.SizeOf(typeof(LKDNode));
        int sizeoftriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));

        m_NodesBuffer = new ComputeBuffer(nodes.Count, sizeofnode);
        m_TrianglesBuffer = new ComputeBuffer(datas.Count, sizeoftriangle);

        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        m_RenderTexture.enableRandomWrite = true;
        m_RenderTexture.Create();

        m_DispatchX = Mathf.CeilToInt((float)m_RenderTexture.width / 4);
        m_DispatchY = Mathf.CeilToInt((float)m_RenderTexture.height / 4);

        m_KernelIndex = computeShader.FindKernel("CSMain");

        m_NodesBuffer.SetData(nodes.ToArray());
        m_TrianglesBuffer.SetData(datas.ToArray());

        computeShader.SetBuffer(m_KernelIndex, "_Tree", m_NodesBuffer);
        computeShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
        computeShader.SetTexture(m_KernelIndex, "_Result", m_RenderTexture);
        computeShader.SetInt("_NodeCounts", nodes.Count);
        computeShader.SetInt("_RootNode", root);
        computeShader.SetInt("_TexWidth", m_RenderTexture.width);
        computeShader.SetInt("_TexHeight", m_RenderTexture.height);

        float h = Camera.main.farClipPlane * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * Camera.main.aspect;

        computeShader.SetFloat("_Near", Camera.main.nearClipPlane);
        computeShader.SetFloat("_NearClipWidth", w);
        computeShader.SetFloat("_NearClipHeight", h);
        computeShader.SetMatrix("_CameraToWorld", Camera.main.transform.localToWorldMatrix);

        computeShader.Dispatch(m_KernelIndex, m_DispatchX, m_DispatchX, 1);
    }

    void OnGUI()
    {
        if (m_RenderTexture)
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), m_RenderTexture);
    }
    
    void OnDestroy()
    {
        if (m_TrianglesBuffer != null)
            m_TrianglesBuffer.Release();
        if (m_NodesBuffer != null)
            m_NodesBuffer.Release();
        if (m_RenderTexture)
            Destroy(m_RenderTexture);
    }

    private List<Triangle> GetTriangles()
    {
        List<Triangle> triangles = new List<Triangle>();

        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        for (int i = 0; i < meshFilters.Length; i++)
        {
            GetTrianglesFromMesh(triangles, meshFilters[i].sharedMesh, meshFilters[i].transform.localToWorldMatrix);
        }

        return triangles;
    }

    private void GetTrianglesFromMesh(List<Triangle> trianglelist, Mesh mesh, Matrix4x4 matrix)
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
