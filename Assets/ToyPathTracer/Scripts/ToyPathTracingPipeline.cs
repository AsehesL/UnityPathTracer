using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyPathTracingPipeline
{
    public float focal;
    public float radius;

    public bool IsLightChanged
    {
        get;private set;
    }

    public Cubemap SkyCubeMap
    {
        get
        {
            return m_SkyCubeMap;
        }
        set
        {
            m_SkyCubeMap = value;
            if (m_PathTracingShader != null && m_KernelIndex >= 0)
                m_PathTracingShader.SetTexture(m_KernelIndex, "_Sky", m_SkyCubeMap);
        }
    }

    private Cubemap m_SkyCubeMap = null;

    private const int kTileSize = 32;

    private int m_DispatchThreadGroupsX;
    private int m_DispatchThreadGroupsY;

    private ComputeShader m_PathTracingShader;

    private RenderTexture m_RenderTexture;

    private Camera m_Camera;

    private int m_KernelIndex;

    private ComputeBuffer m_SpheresBuffer;
    private ComputeBuffer m_QuadsBuffer;
    private ComputeBuffer m_TrianglesBuffer;
    private ComputeBuffer m_NodesBuffer;

    private ComputeBuffer m_MaterialsBuffer;

    private bool m_SampleDirectLight;

    private Primitive m_LightPrimitive = null;
    private Vector3 m_LightPos;
    private float m_LightRadius;

    private ToyPathTracingShaderBase m_shader;

    public bool IsInitialized
    {
        get;
        private set;
    }

    public ToyPathTracingPipeline(Camera camera, ToyPathTracingShaderBase shader)
    {
        m_shader = shader;
        m_PathTracingShader = shader.GetShader();

        m_Camera = camera;

        m_SampleDirectLight = shader.ShouldSampleDirectLight();

        IsInitialized = false;

        IsLightChanged = false;
    }

    public void BuildPipeline()
    {
        if (IsInitialized)
            return;
        if (m_PathTracingShader == null) return;
        if (m_Camera == null) return;
        m_KernelIndex = m_PathTracingShader.FindKernel(m_shader.GetKernelName());
        if (m_KernelIndex < 0)
            return;

        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        m_RenderTexture.enableRandomWrite = true;
        m_RenderTexture.Create();

        m_DispatchThreadGroupsX = Mathf.CeilToInt((float)m_RenderTexture.width / kTileSize);
        m_DispatchThreadGroupsY = Mathf.CeilToInt((float)m_RenderTexture.height / kTileSize);

        List<Material> materials;
        List<Primitive> primitives;
        CollectPrimitives(out materials, out primitives);
        List<Sphere> spheres;
        List<Quad> quads;
        List<Triangle> triangles;
        List<BVHNode> bvh;
        int bvhRoot = BVH.Build(primitives, out bvh, out spheres, out quads, out triangles);
        BuildComputeBuffers(bvhRoot, materials, bvh, spheres, quads, triangles);
        IsInitialized = true;

        m_PathTracingShader.SetTexture(m_KernelIndex, "_Sky", Texture2D.blackTexture);
    }

    public RenderTexture ExecutePipeline()
    {
        if (!IsInitialized)
        {
            return null;
        }

        if (m_LightPrimitive != null)
        {
            Vector3 lightPos = m_LightPrimitive.center;
            float lightRadius = m_LightPrimitive.radius;
            if (m_LightPos != lightPos || m_LightRadius != lightRadius)
            {
                IsLightChanged = true;
                m_LightPos = lightPos;
                m_LightRadius = lightRadius;
            }
            else
            {
                IsLightChanged = false;
            }
            int lightMat = m_LightPrimitive.matId;
            float area = 4.0f * Mathf.PI * lightRadius * lightRadius;

            m_PathTracingShader.SetVector("_LightCenterAndRadius", new Vector4(lightPos.x, lightPos.y, lightPos.z, lightRadius));
            m_PathTracingShader.SetInt("_LightMatId", lightMat);
            m_PathTracingShader.SetFloat("_LightPDF", 1.0f / area);
        }

        float h = m_Camera.nearClipPlane * Mathf.Tan(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * m_Camera.aspect;
        m_PathTracingShader.SetFloat("_NearClipPlane", m_Camera.nearClipPlane);
        m_PathTracingShader.SetFloat("_NearClipWidth", w);
        m_PathTracingShader.SetFloat("_NearClipHeight", h);
        m_PathTracingShader.SetMatrix("_PathTracerCameraToWorld", m_Camera.transform.localToWorldMatrix);
        m_PathTracingShader.SetFloat("_Time", Time.time);
        m_PathTracingShader.SetFloat("_ThinLensFocal", focal);
        m_PathTracingShader.SetFloat("_ThinLensRadius", radius);
        m_PathTracingShader.Dispatch(m_KernelIndex, m_DispatchThreadGroupsX, m_DispatchThreadGroupsY, 1);

        return m_RenderTexture;
    }

    public void DestroyPipeline()
    {
        IsInitialized = false;
        if (m_RenderTexture)
            Object.Destroy(m_RenderTexture);
        if (m_SpheresBuffer != null)
            m_SpheresBuffer.Release();
        if (m_QuadsBuffer != null)
            m_QuadsBuffer.Release();
        if (m_TrianglesBuffer != null)
            m_TrianglesBuffer.Release();
        if (m_NodesBuffer != null)
            m_NodesBuffer.Release();
        if (m_MaterialsBuffer != null)
            m_MaterialsBuffer.Release();
        m_SpheresBuffer = null;
        m_QuadsBuffer = null;
        m_TrianglesBuffer = null;
        m_NodesBuffer = null;
        m_MaterialsBuffer = null;
        m_RenderTexture = null;
        m_Camera = null;
        m_PathTracingShader = null;
    }

    private void CollectPrimitives(out List<Material> materials, out List<Primitive> primitives)
    {
        MeshRenderer[] meshRenderers = Object.FindObjectsOfType<MeshRenderer>();
        Dictionary<Material, List<Primitive>> primitivesWithMaterial = null;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            Primitive.CreatePrimitives(meshRenderers[i].gameObject, ref primitivesWithMaterial);
        }

        primitives = new List<Primitive>();
        materials = new List<Material>();
        if (primitivesWithMaterial != null)
        {
            foreach(var kvp in primitivesWithMaterial)
            {
                if (kvp.Key == null || kvp.Value == null || kvp.Value.Count == 0)
                    continue;
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    Primitive primitive = kvp.Value[i];
                    primitive.matId = materials.Count;
                    if (m_SampleDirectLight && m_shader.IsEmissive(kvp.Key))
                    {
                        m_LightPrimitive = primitive;
                        continue;
                    }
                    primitives.Add(primitive);
                }
                materials.Add(kvp.Key);
            }
        }
       
    }

    private void BuildComputeBuffers(int rootIndex, List<Material> materials, List<BVHNode> bvh, List<Sphere> spheres, List<Quad> quads, List<Triangle> triangles)
    {
        m_SpheresBuffer = new ComputeBuffer(spheres != null && spheres.Count > 0 ? spheres.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
        if (spheres != null && spheres.Count > 0)
            m_SpheresBuffer.SetData(spheres.ToArray());

        m_QuadsBuffer = new ComputeBuffer(quads != null && quads.Count > 0 ? quads.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Quad)));
        if (quads != null && quads.Count > 0)
            m_QuadsBuffer.SetData(quads.ToArray());

        m_TrianglesBuffer = new ComputeBuffer(triangles != null && triangles.Count > 0 ? triangles.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle)));
        if (triangles != null && triangles.Count > 0)
            m_TrianglesBuffer.SetData(triangles.ToArray());

        m_NodesBuffer = new ComputeBuffer(bvh.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(BVHNode)));
        m_NodesBuffer.SetData(bvh.ToArray());

        m_MaterialsBuffer = m_shader.CreateMaterialBuffer(materials);
        //m_MaterialsBuffer = new ComputeBuffer(materials.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PathTracerMaterial)));
        //m_MaterialsBuffer.SetData(materials.ToArray());

        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Spheres", m_SpheresBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Quads", m_QuadsBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_BVHTree", m_NodesBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Materials", m_MaterialsBuffer);

        m_PathTracingShader.SetInt("_BVHRootNodeIndex", rootIndex);
        m_PathTracingShader.SetInt("_BVHNodeCount", bvh.Count);

        m_PathTracingShader.SetTexture(m_KernelIndex, "_Result", m_RenderTexture);
        m_PathTracingShader.SetInt("_ScreenWidth", m_RenderTexture.width);
        m_PathTracingShader.SetInt("_ScreenHeight", m_RenderTexture.height);
    }
}
