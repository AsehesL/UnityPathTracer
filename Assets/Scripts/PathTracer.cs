using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTracer : MonoBehaviour
{
    public Cubemap sky;
    
    public ComputeShader computeShader;

    public Shader shader;

    public float adapted;
    
    private Camera m_Camera;

    private Material m_Material;
    
    private RenderTexture m_RenderTexture;
    private RenderTexture m_TempRT;
    
    private ComputeBuffer m_NodesBuffer;
    private ComputeBuffer m_TrianglesBuffer;
    private ComputeBuffer m_MaterialsBuffer;
    
    private int m_DispatchX;
    private int m_DispatchY;

    private int m_KernelIndex;
    
    private float m_Frame;

    private Matrix4x4 m_LocalToWorld;
    
    void Start()
    {
        m_Material = new Material(shader);

        m_Camera = gameObject.GetComponent<Camera>();

        int width = (int) (((float) Screen.width));
        int height = (int) (((float) Screen.height));

        m_RenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf);
        m_RenderTexture.enableRandomWrite = true;
        m_RenderTexture.Create();

        m_DispatchX = Mathf.CeilToInt((float)m_RenderTexture.width / 2);
        m_DispatchY = Mathf.CeilToInt((float)m_RenderTexture.height / 2);


        List<Node> tree;
        List<Triangle> triangles;
        List<PathTracerMaterial> materials;

        int root = InitScene(out tree, out triangles, out materials);
        InitComputeBuffers(tree, triangles, materials, root, m_RenderTexture);
    }
    
    private void InitComputeBuffers(List<Node> tree, List<Triangle> triangles, List<PathTracerMaterial> materials, int root, RenderTexture texture)
    {
        int sizeofnode = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Node));
        int sizeoftriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));
        int sizeofmaterial = System.Runtime.InteropServices.Marshal.SizeOf(typeof(PathTracerMaterial));

        m_NodesBuffer = new ComputeBuffer(tree.Count, sizeofnode);
        m_TrianglesBuffer = new ComputeBuffer(triangles.Count, sizeoftriangle);
        m_MaterialsBuffer = new ComputeBuffer(materials.Count, sizeofmaterial);

        m_KernelIndex = computeShader.FindKernel("CSMain");

        m_NodesBuffer.SetData(tree.ToArray());
        m_TrianglesBuffer.SetData(triangles.ToArray());
        m_MaterialsBuffer.SetData(materials.ToArray());

        computeShader.SetBuffer(m_KernelIndex, "_Tree", m_NodesBuffer);
        computeShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
        computeShader.SetBuffer(m_KernelIndex, "_Materials", m_MaterialsBuffer);
        computeShader.SetTexture(m_KernelIndex, "_Result", m_RenderTexture);
        computeShader.SetInt("_NodeCounts", tree.Count);
        computeShader.SetInt("_RootNode", root);
        computeShader.SetInt("_TexWidth", texture.width);
        computeShader.SetInt("_TexHeight", texture.height);
        //computeShader.SetInt("_SampleNum", 83);
        computeShader.SetTexture(m_KernelIndex, "_Sky", sky);
        computeShader.SetFloat("_LightRange", 8);
    }
    
    private int InitScene(out List<Node> tree, out List<Triangle> datas, out List<PathTracerMaterial> materials)
    {
        Bounds bounds;
        var triangles = GetTrianglesInScene(out bounds);

        tree = null;
        datas = null;
        materials = new List<PathTracerMaterial>();
        
        List<Triangle> tris = new List<Triangle>();

        foreach (var t in triangles)
        {
            for (int i = 0; i < t.Value.Count; i++)
            {
                Triangle triangle = t.Value[i];
                triangle.matid = materials.Count;
                tris.Add(triangle);
            }

            PathTracerMaterial mat = t.Key.GetMaterial();
            
            materials.Add(mat);
        }

        return LBVH.BuildBVH(tris, bounds, ref tree, ref datas);
    }
    
    private static Dictionary<PTMaterial, List<Triangle>> GetTrianglesInScene(out Bounds bounds)
    {
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;
        Dictionary<PTMaterial, List<Triangle>> triangles = new Dictionary<PTMaterial, List<Triangle>>();

        PTRenderer[] renderers = Object.FindObjectsOfType<PTRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshFilter meshFilter = renderers[i].GetComponent<MeshFilter>();
            if(!meshFilter || !meshFilter.sharedMesh) continue;
            
            PTMaterial material = renderers[i].material;

            if (!material) continue;

            if(!triangles.ContainsKey(material))
                triangles.Add(material, new List<Triangle>());

            List<Triangle> tris = triangles[material];
                
            GetTrianglesFromMesh(tris, meshFilter.sharedMesh, meshFilter.transform.localToWorldMatrix, ref min, ref max);
        }

//        if (destroyOriginMesh)
//        {
//            for (int i = 0; i < meshFilters.Length; i++)
//            {
//                if (meshFilters[i].gameObject)
//                    Object.Destroy(meshFilters[i].gameObject);
//            }
//        }

        bounds = new Bounds((min + max) * 0.5f, max - min);

        return triangles;
    }

    private static void GetTrianglesFromMesh(List<Triangle> trianglelist, Mesh mesh, Matrix4x4 matrix, ref Vector3 min, ref Vector3 max)
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

            min = Vector3.Min(min, vertex0);
            min = Vector3.Min(min, vertex1);
            min = Vector3.Min(min, vertex2);
            max = Vector3.Max(max, vertex0);
            max = Vector3.Max(max, vertex1);
            max = Vector3.Max(max, vertex2);

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
    
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_RenderTexture)
        {
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, source.depth, RenderTextureFormat.DefaultHDR);
            if (!m_TempRT)
            {
                Graphics.Blit(m_RenderTexture, temp);
                m_TempRT = RenderTexture.GetTemporary(m_RenderTexture.width, m_RenderTexture.height, m_RenderTexture.depth, RenderTextureFormat.DefaultHDR);
            }
            else
            {
                if (m_LocalToWorld != transform.localToWorldMatrix)
                {
                    m_LocalToWorld = transform.localToWorldMatrix;
                    Graphics.Blit(m_RenderTexture, temp);
                    m_Frame = 0.0f;
                }
                else
                {

                    m_Material.SetFloat("_Frame", m_Frame);
                    m_Material.SetTexture("_Cache", m_TempRT);
                    Graphics.Blit(m_RenderTexture, temp, m_Material, 0);

                    m_Frame += 1.0f;
                }
            }

            Graphics.Blit(temp, m_TempRT);
            //Graphics.Blit(m_RenderTexture, destination);
            m_Material.SetFloat("_Adapted", adapted);
            Graphics.Blit(temp, destination, m_Material, 2);
            RenderTexture.ReleaseTemporary(temp);

        }
        else
            Graphics.Blit(source, destination);
    }

    void Update()
    {
        float h = m_Camera.nearClipPlane * Mathf.Tan(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * m_Camera.aspect;

        computeShader.SetFloat("_Near", m_Camera.nearClipPlane);
        computeShader.SetFloat("_NearClipWidth", w);
        computeShader.SetFloat("_NearClipHeight", h);
        computeShader.SetMatrix("_CameraToWorld", m_Camera.transform.localToWorldMatrix);

        computeShader.SetFloat("_Time", Time.time);
	    

        computeShader.Dispatch(m_KernelIndex, m_DispatchX, m_DispatchY, 1);

//        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
//            m_MouseDown = true;
//        else
//        {
//            m_MouseDown = false;
//        }
    }
    
    void OnDestroy()
    {
        if (m_TrianglesBuffer != null)
            m_TrianglesBuffer.Release();
        if (m_NodesBuffer != null)
            m_NodesBuffer.Release();
        if(m_MaterialsBuffer != null)
            m_MaterialsBuffer.Release();
        if (m_RenderTexture)
            Destroy(m_RenderTexture);
        if(m_Material)
            Destroy(m_Material);
        if (m_TempRT)
            RenderTexture.ReleaseTemporary(m_TempRT);
        m_TrianglesBuffer = null;
        m_NodesBuffer = null;
        m_MaterialsBuffer = null;
        m_RenderTexture = null;
        m_Material = null;
        m_TempRT = null;
    }
}
