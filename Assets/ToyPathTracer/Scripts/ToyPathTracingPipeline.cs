using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToyPathTracingPipeline
{
    private struct PTMaterial
    {
        public Color albedo;
        public float roughness;
        public float metallic;
        public int emission;
        public int textureId;
        public int normalTextureId;
        public int mroTextureId;

        public PTMaterial(Material material)
        {
            Color emissionColor = material.GetColor("_EmissionColor");
            Color albedoColor = material.GetColor("_Color");
            float smoothness = material.GetFloat("_Glossiness");
            float metallic = material.GetFloat("_Metallic");
            float useTexture = material.GetFloat("_UseTexture");
            //useTexture = use > 0.5f ? 1 : -1;
            textureId = useTexture > 0.5f ? 0 : -1;
            normalTextureId = useTexture > 0.5f ? 0 : -1;
            mroTextureId = useTexture > 0.5f ? 0 : -1;
            if (emissionColor.grayscale > 0.01f)
            {
                emission = 1;
                albedo = emissionColor;
                roughness = 1;
                this.metallic = 0;
            }
            else
            {
                albedo = albedoColor;
                emission = -1;
                roughness = 1.0f - smoothness;
                this.metallic = metallic;
            }
        }
    }

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

    private bool m_EnableNEE;

    private bool m_EnableThinLens;

    private bool m_EnableSampleTexture;

    private bool m_EnableTangentSpace;

    private string m_KernelName;

    private List<PTLight> m_Lights;
    private List<float> m_LightWeights;

    private Texture2DArray m_baseColorTextureArray;
    private Texture2DArray m_normalTextureArray;
    private Texture2DArray m_mroTextureArray;

    public bool IsInitialized
    {
        get;
        private set;
    }

    public ToyPathTracingPipeline(Camera camera, ComputeShader shader, string kernelName, bool enableNEE, bool enableThinLens, bool enableSampleTexture, bool enableTangentSpace)
    {
        m_KernelName = kernelName;
        m_PathTracingShader = shader;

        m_Camera = camera;

        m_EnableNEE = enableNEE;
        m_EnableThinLens = enableThinLens;
        m_EnableSampleTexture = enableSampleTexture;
        m_EnableTangentSpace = enableTangentSpace;

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

        if (m_EnableNEE)
            m_PathTracingShader.EnableKeyword("ENABLE_NEXT_EVENT_ESTIMATION");
        else
            m_PathTracingShader.DisableKeyword("ENABLE_NEXT_EVENT_ESTIMATION");

        if (m_EnableThinLens)
            m_PathTracingShader.EnableKeyword("ENABLE_THIN_LENS");
        else
            m_PathTracingShader.DisableKeyword("ENABLE_THIN_LENS");

        if (m_EnableSampleTexture)
            m_PathTracingShader.EnableKeyword("ENABLE_SAMPLE_TEXTURE");
        else
            m_PathTracingShader.DisableKeyword("ENABLE_SAMPLE_TEXTURE");

        if (m_EnableTangentSpace)
            m_PathTracingShader.EnableKeyword("ENABLE_TANGENT_SPACE");
        else
            m_PathTracingShader.DisableKeyword("ENABLE_TANGENT_SPACE");

        m_RenderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        m_RenderTexture.enableRandomWrite = true;
        m_RenderTexture.Create();

        m_DispatchThreadGroupsX = Mathf.CeilToInt((float)m_RenderTexture.width / kTileSize);
        m_DispatchThreadGroupsY = Mathf.CeilToInt((float)m_RenderTexture.height / kTileSize);

        List<PTMaterial> materials;
        List<Primitive> primitives;
        List<Texture2D> baseColorTextures = null;
        List<Texture2D> mroTextures = null;
        List<Texture2D> normalTextures = null;
        if (m_EnableSampleTexture)
            baseColorTextures = new List<Texture2D>();
        if (m_EnableTangentSpace)
        {
            mroTextures = new List<Texture2D>();
            normalTextures = new List<Texture2D>();
        }
        CollectPrimitives(out materials, out primitives, ref baseColorTextures, ref mroTextures, ref normalTextures);
        BVH bvhTree = new BVH();
        bvhTree.Build(primitives);
        List<Sphere> spheres;
        List<Quad> quads;
        List<Cube> cubes;
        List<Triangle> triangles;
        List<LBVHNode> bvh;
        int bvhRoot = BuildLBVH(bvhTree, out bvh, out spheres, out quads, out cubes, out triangles);
        if (m_EnableSampleTexture)
        {
            m_baseColorTextureArray = CreateTextureArray(baseColorTextures);
            m_PathTracingShader.SetTexture(m_KernelIndex, "_Textures", m_baseColorTextureArray);
        }
        if (m_EnableTangentSpace)
        {
            m_mroTextureArray = CreateTextureArray(mroTextures);
            m_normalTextureArray = CreateTextureArray(normalTextures);
            m_PathTracingShader.SetTexture(m_KernelIndex, "_MROTextures", m_mroTextureArray);
            m_PathTracingShader.SetTexture(m_KernelIndex, "_NormalTextures", m_normalTextureArray);
        }
        m_NodesBuffer = CreateComputeBuffer<LBVHNode>(bvh);
        m_SpheresBuffer = CreateComputeBuffer<Sphere>(spheres);
        m_QuadsBuffer = CreateComputeBuffer<Quad>(quads);
        m_CubeBuffer = CreateComputeBuffer<Cube>(cubes);
        m_TrianglesBuffer = CreateComputeBuffer<Triangle>(triangles);
        m_MaterialsBuffer = CreateComputeBuffer<PTMaterial>(materials);

        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Spheres", m_SpheresBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Quads", m_QuadsBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Cubes", m_CubeBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_BVHTree", m_NodesBuffer);
        m_PathTracingShader.SetBuffer(m_KernelIndex, "_Materials", m_MaterialsBuffer);

        m_PathTracingShader.SetInt("_BVHRootNodeIndex", bvhRoot);
        m_PathTracingShader.SetInt("_BVHNodeCount", bvh.Count);

        IsInitialized = true;

        m_PathTracingShader.SetTexture(m_KernelIndex, "_Result", m_RenderTexture);
        m_PathTracingShader.SetInt("_ScreenWidth", m_RenderTexture.width);
        m_PathTracingShader.SetInt("_ScreenHeight", m_RenderTexture.height);

        Debug.Log($"BVH Root:{bvhRoot}, BVH Childs:{bvh.Count}");
    }

    private int BuildLBVH(BVH bvhTree, out List<LBVHNode> lbvh, out List<Sphere> spheres, out List<Quad> quads, out List<Cube> cubes, out List<Triangle> triangles)
    {
        lbvh = new List<LBVHNode>();
        spheres = new List<Sphere>();
        quads = new List<Quad>();
        cubes = new List<Cube>();
        triangles = new List<Triangle>();
        if (bvhTree == null || bvhTree.root == null)
        {
            return -1;
        }
        return BuildLBVHNodes(bvhTree.root, ref lbvh, ref spheres, ref quads, ref cubes, ref triangles);
    }

    private int BuildLBVHNodes(BVH.Node root, ref List<LBVHNode> lbvh, ref List<Sphere> spheres, ref List<Quad> quads, ref List<Cube> cubes, ref List<Triangle> triangles)
    {
        if (root == null)
            return -1;
        var leftNode = root.leftChild;
        var rightNode = root.rightChild;
        int leftNodeIndex = BuildLBVHNodes(leftNode, ref lbvh, ref spheres, ref quads, ref cubes, ref triangles);
        int rightNodeIndex = BuildLBVHNodes(rightNode, ref lbvh, ref spheres, ref quads, ref cubes, ref triangles);
        int primitiveId = -1;
        int primitiveType = 0;

        if (root.primitive != null)
        {
            if (root.primitive.primitiveType == PrimitiveType.Sphere)
            {
                primitiveId = spheres.Count;
                primitiveType = (int)PrimitiveType.Sphere;
                spheres.Add(root.primitive.CreateSphere());
            }
            else if (root.primitive.primitiveType == PrimitiveType.Quad)
            {
                primitiveId = quads.Count;
                primitiveType = (int)PrimitiveType.Quad;
                quads.Add(root.primitive.CreateQuad());
            }
            else if (root.primitive.primitiveType == PrimitiveType.Cube)
            {
                primitiveId = cubes.Count;
                primitiveType = (int)PrimitiveType.Cube;
                cubes.Add(root.primitive.CreateCube());
            }
            else if (root.primitive.primitiveType == PrimitiveType.Triangle)
            {
                primitiveId = triangles.Count;
                primitiveType = (int)PrimitiveType.Triangle;
                triangles.Add(root.primitive.CreateTriangle());
            }
        }

        int rootIndex = lbvh.Count;
        lbvh.Add(new LBVHNode
        {
            boundsMin = root.bounds.min,
            boundsMax = root.bounds.max,
            leftChild = leftNodeIndex,
            rightChild = rightNodeIndex,
            primitiveId = primitiveId,
            primitiveType = primitiveType
        });
        return rootIndex;
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
        if (m_EnableThinLens)
        {
            m_PathTracingShader.SetFloat("_ThinLensFocal", focal);
            m_PathTracingShader.SetFloat("_ThinLensRadius", radius);
        }
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
        if (m_baseColorTextureArray != null)
            Object.Destroy(m_baseColorTextureArray);
        m_baseColorTextureArray = null;
        if (m_normalTextureArray != null)
            Object.Destroy(m_normalTextureArray);
        m_normalTextureArray = null;
        if (m_mroTextureArray != null)
            Object.Destroy(m_mroTextureArray);
        m_mroTextureArray = null;
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

    private void CollectPrimitives(out List<PTMaterial> materials, out List<Primitive> primitives, ref List<Texture2D> baseColorTextures, ref List<Texture2D> mroTextures, ref List<Texture2D> normalTextures)
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
                PTMaterial ptmat = CreateMaterial(kvp.Key, ref baseColorTextures, ref mroTextures, ref normalTextures);

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    Primitive primitive = kvp.Value[i];
                    primitive.matId = materials.Count;
                    if (m_EnableNEE && ptmat.emission > 0 && (primitive.primitiveType == PrimitiveType.Sphere || primitive.primitiveType == PrimitiveType.Quad))
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
                materials.Add(ptmat);
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

    private PTMaterial CreateMaterial(Material material, ref List<Texture2D> baseColorTextures, ref List<Texture2D> mroTextures, ref List<Texture2D> normalTextures)
    {
        PTMaterial ptmat = new PTMaterial(material);
        if (m_EnableSampleTexture && baseColorTextures != null)
        {
            float useTexture = material.GetFloat("_UseTexture");
            Texture baseColorTex = material.GetTexture("_Texture");
            if (useTexture > 0.5f && baseColorTex != null && baseColorTex is Texture2D)
            {
                Texture2D tex2D = baseColorTex as Texture2D;
                int texIndex = baseColorTextures.IndexOf(tex2D);
                if (texIndex < 0)
                {
                    texIndex = baseColorTextures.Count;
                    baseColorTextures.Add(tex2D);
                }
                ptmat.textureId = texIndex;
            }
            if (m_EnableTangentSpace && useTexture > 0.5f)
            {
                Texture mroTex = material.GetTexture("_MroTexture");
                Texture normalTex = material.GetTexture("_NormalTexture");
                if (mroTex != null && mroTex is Texture2D)
                {
                    Texture2D tex2D = mroTex as Texture2D;
                    int texIndex = mroTextures.IndexOf(tex2D);
                    if (texIndex < 0)
                    {
                        texIndex = mroTextures.Count;
                        mroTextures.Add(tex2D);
                    }
                    ptmat.mroTextureId = texIndex;
                }
                if (normalTex != null && normalTex is Texture2D)
                {
                    Texture2D tex2D = normalTex as Texture2D;
                    int texIndex = normalTextures.IndexOf(tex2D);
                    if (texIndex < 0)
                    {
                        texIndex = normalTextures.Count;
                        normalTextures.Add(tex2D);
                    }
                    ptmat.normalTextureId = texIndex;
                }
            }
        }
        return ptmat;
    }

    private ComputeBuffer CreateComputeBuffer<T>(List<T> datas)
    {
        var buffer = new ComputeBuffer(datas != null && datas.Count > 0 ? datas.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
        if (datas != null && datas.Count > 0)
            buffer.SetData(datas.ToArray());
        return buffer;
    }

    private Texture2DArray CreateTextureArray(List<Texture2D> textures)
    {
        Texture2DArray textureArray = new Texture2DArray(1024, 1024, textures.Count, textures[0].format, false);

        for (int i=0;i<textures.Count;i++)
        {
            var colors = new Color[1024 * 1024];
            for (int x = 0; x < 1024; x++)
            {
                for (int y = 0; y < 1024; y++)
                {
                    float u = ((float)x) / (1024 - 1);
                    float v = ((float)y) / (1024 - 1);
                    colors[y * 1024 + x] = textures[i].GetPixelBilinear(u, v);
                }
            }
            textureArray.SetPixels(colors, i);
        }
        textureArray.Apply();
        return textureArray;
    }
}
