using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyPathTracingPipeline
{
    private class PTLight
    {
        public Primitive primitive { get; private set; }

        private PTMaterial m_Material;

        private float m_Flux;
        private float m_PDF;

        public PTLight(Primitive primitive, PTMaterial material)
        {
            this.primitive = primitive;
            m_Material = material;
            float irradiance = m_Material.albedo.grayscale;
            float area = this.primitive.GetArea();
            m_Flux = irradiance * area;
            m_PDF = 1.0f / area;
        }

        public float GetFlux()
        {
            return m_Flux;
        }

        public float GetPDF()
        {
            return m_PDF;
        }
    }

    public float focal;
    public float radius;

    public bool isLightChanged;

    private const int kTileSize = 32;

    private int m_DispatchThreadGroupsX;
    private int m_DispatchThreadGroupsY;

    private ComputeShader m_PathTracingShader;

    private RenderTexture m_RenderTexture;

    private Camera m_Camera;

    private int m_KernelIndex;

    private ComputeBuffer m_SpheresBuffer;
    private ComputeBuffer m_QuadsBuffer;
    private ComputeBuffer m_CubeBuffer;
    private ComputeBuffer m_TrianglesBuffer;
    private ComputeBuffer m_NodesBuffer;

    private ComputeBuffer m_MaterialsBuffer;

    private bool m_SampleDirectLight;

    private string m_KernelName;

    private List<PTLight> m_Lights;
    private List<float> m_LightWeights;

    public bool IsInitialized
    {
        get;
        private set;
    }

    public ToyPathTracingPipeline(Camera camera, ComputeShader shader, string kernelName, bool sampleDirectLight)
    {
        m_KernelName = kernelName;
        m_PathTracingShader = shader;

        m_Camera = camera;

        m_SampleDirectLight = sampleDirectLight;

        IsInitialized = false;

        isLightChanged = false;

        m_Lights = new List<PTLight>();
        m_LightWeights = new List<float>();
    }

    public void SetTexture(string name, Texture texture)
    {
        if (m_PathTracingShader != null && m_KernelIndex >= 0)
        {
            isLightChanged = true;
            m_PathTracingShader.SetTexture(m_KernelIndex, name, texture);
        }
    }

    public void BuildPipeline()
    {
        if (IsInitialized)
            return;
        if (m_PathTracingShader == null) return;
        if (m_Camera == null) return;
        m_KernelIndex = m_PathTracingShader.FindKernel(m_KernelName);
        if (m_KernelIndex < 0)
            return;

        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        m_RenderTexture.enableRandomWrite = true;
        m_RenderTexture.Create();

        m_DispatchThreadGroupsX = Mathf.CeilToInt((float)m_RenderTexture.width / kTileSize);
        m_DispatchThreadGroupsY = Mathf.CeilToInt((float)m_RenderTexture.height / kTileSize);

        List<PTMaterial> materials;
        List<Primitive> primitives;
        CollectPrimitives(out materials, out primitives);
        List<Sphere> spheres;
        List<Quad> quads;
        List<Cube> cubes;
        List<Triangle> triangles;
        List<BVHNode> bvh;
        int bvhRoot = BVH.Build(primitives, out bvh, out spheres, out quads, out cubes, out triangles);
        BuildComputeBuffers(bvhRoot, materials, bvh, spheres, quads, cubes, triangles);
        IsInitialized = true;

        Debug.Log($"BVH Root:{bvhRoot}, BVH Childs:{bvh.Count}");
    }

    public RenderTexture ExecutePipeline()
    {
        if (!IsInitialized)
        {
            return null;
        }

        if (m_Lights != null && m_Lights.Count > 0)
        {
            float lightProbability = Random.Range(0.0f, 1.0f);
            float weight = 0.0f;
            PTLight currentLight = null;
            for(int i=0;i<m_Lights.Count;i++)
            {
                if (m_LightWeights[i] + weight > lightProbability)
                {
                    currentLight = m_Lights[i];
                    break;
                }
                weight += m_LightWeights[i];
            }
            if (currentLight != null && (currentLight.primitive.primitiveType == PrimitiveType.Sphere || currentLight.primitive.primitiveType == PrimitiveType.Quad))
            {
                if (currentLight.primitive.primitiveType == PrimitiveType.Sphere)
                {
                    Sphere lightSphere = currentLight.primitive.CreateSphere();
                    Vector4 lightPosAndRadius = lightSphere.positionAndRadius;
                    if (currentLight.primitive.IsChanged())
                        isLightChanged = true;
                    m_PathTracingShader.SetVector("_LightCenterAndRadius", lightPosAndRadius);
                }
                else if(currentLight.primitive.primitiveType == PrimitiveType.Quad)
                {
                    Quad lightQuad = currentLight.primitive.CreateQuad();
                    Vector4 lightRight = lightQuad.right;
                    Vector4 lightForward = lightQuad.forward;
                    Vector3 lightNormal = lightQuad.normal;
                    Vector3 lightPosition = lightQuad.position;
                    if (currentLight.primitive.IsChanged())
                        isLightChanged = true;
                    m_PathTracingShader.SetVector("_LightRight", lightRight);
                    m_PathTracingShader.SetVector("_LightForward", lightForward);
                    m_PathTracingShader.SetVector("_LightPosition", lightPosition);
                    m_PathTracingShader.SetVector("_LightNormal", lightNormal);
                }    
                int lightMat = currentLight.primitive.matId;
                m_PathTracingShader.SetInt("_LightMatId", lightMat);
                m_PathTracingShader.SetInt("_LightPrimivateType", (int)currentLight.primitive.primitiveType);
                m_PathTracingShader.SetFloat("_LightPDF", currentLight.GetPDF());
            }
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
        if (m_CubeBuffer != null)
            m_CubeBuffer.Release();
        if (m_TrianglesBuffer != null)
            m_TrianglesBuffer.Release();
        if (m_NodesBuffer != null)
            m_NodesBuffer.Release();
        if (m_MaterialsBuffer != null)
            m_MaterialsBuffer.Release();
        m_SpheresBuffer = null;
        m_QuadsBuffer = null;
        m_CubeBuffer = null;
        m_TrianglesBuffer = null;
        m_NodesBuffer = null;
        m_MaterialsBuffer = null;
        m_RenderTexture = null;
        m_Camera = null;
        m_PathTracingShader = null;
    }

    private void CollectPrimitives(out List<PTMaterial> materials, out List<Primitive> primitives)
    {
        MeshRenderer[] meshRenderers = Object.FindObjectsOfType<MeshRenderer>();
        Dictionary<Material, List<Primitive>> primitivesWithMaterial = null;
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            Primitive.CreatePrimitives(meshRenderers[i].gameObject, ref primitivesWithMaterial);
        }

        primitives = new List<Primitive>();
        materials = new List<PTMaterial>();
        float totalLightWeight = 0.0f;
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
                    PTMaterial ptmat = new PTMaterial(kvp.Key);
                    if (m_SampleDirectLight && ptmat.emission > 0 && (primitive.primitiveType == PrimitiveType.Sphere || primitive.primitiveType == PrimitiveType.Quad))
                    {
                        PTLight light = new PTLight(primitive, ptmat);
                        m_Lights.Add(light);
                        float flux = light.GetFlux();
                        m_LightWeights.Add(flux);
                        totalLightWeight += flux;
                        continue;
                    }
                    primitives.Add(primitive);
                }
                materials.Add(new PTMaterial(kvp.Key));
            }
        }
        if (m_Lights.Count > 1)
        {
            for (int i=0;i<m_Lights.Count;i++)
            {
                primitives.Add(m_Lights[i].primitive);
            }
        }
        for (int i = 0; i < m_LightWeights.Count; i++)
        {
            m_LightWeights[i] = m_LightWeights[i] / totalLightWeight;
        }
    }

    private void BuildComputeBuffers(int rootIndex, List<PTMaterial> materials, List<BVHNode> bvh, List<Sphere> spheres, List<Quad> quads, List<Cube> cubes, List<Triangle> triangles)
    {
        m_SpheresBuffer = new ComputeBuffer(spheres != null && spheres.Count > 0 ? spheres.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
        if (spheres != null && spheres.Count > 0)
            m_SpheresBuffer.SetData(spheres.ToArray());

        m_QuadsBuffer = new ComputeBuffer(quads != null && quads.Count > 0 ? quads.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Quad)));
        if (quads != null && quads.Count > 0)
            m_QuadsBuffer.SetData(quads.ToArray());

        m_CubeBuffer = new ComputeBuffer(cubes != null && cubes.Count > 0 ? cubes.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Cube)));
        if (cubes != null && cubes.Count > 0)
            m_CubeBuffer.SetData(cubes.ToArray());

        m_TrianglesBuffer = new ComputeBuffer(triangles != null && triangles.Count > 0 ? triangles.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle)));
        if (triangles != null && triangles.Count > 0)
            m_TrianglesBuffer.SetData(triangles.ToArray());

        m_NodesBuffer = new ComputeBuffer(bvh != null && bvh.Count > 0 ? bvh.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(BVHNode)));
        if (bvh != null && bvh.Count > 0)
            m_NodesBuffer.SetData(bvh.ToArray());

        m_MaterialsBuffer = new ComputeBuffer(materials != null && materials.Count > 0 ? materials.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PTMaterial)));
        if (materials != null && materials.Count > 0)
            m_MaterialsBuffer.SetData(materials.ToArray());

        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Spheres", m_SpheresBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Quads", m_QuadsBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Cubes", m_CubeBuffer);
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
