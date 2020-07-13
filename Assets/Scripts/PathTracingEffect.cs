using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PathTracingEffect : MonoBehaviour
{
	public Cubemap sky;

	public ComputeShader computeShader;

	public Shader shader;

    public Light light;

    public float lightIntensity = 1.0f;
    public float skyHDR = 1.0f;


    private ComputeBuffer m_NodesBuffer;
	private ComputeBuffer m_TrianglesBuffer;

	private RenderTexture m_RenderTexture;

	private int m_DispatchX;
	private int m_DispatchY;

	private int m_KernelIndex;

	private Camera m_Camera;

	//private int m_SampleIndex = 0;

	private RenderTexture m_TempRt;

	private Material m_Material;

    private float m_Frame;

    private bool m_MouseDown;

	void Start()
	{
		m_Material = new Material(shader);

		m_Camera = gameObject.GetComponent<Camera>();

		int width = (int) (((float) Screen.width));
		int height = (int) (((float) Screen.height));

		m_RenderTexture = new RenderTexture(width, height, 24);
		m_RenderTexture.enableRandomWrite = true;
		m_RenderTexture.Create();

		m_DispatchX = Mathf.CeilToInt((float)m_RenderTexture.width / 2);
		m_DispatchY = Mathf.CeilToInt((float)m_RenderTexture.height / 2);


		List<Node> tree;
		List<Triangle> triangles;

		int root = InitScene(out tree, out triangles);
		InitComputeBuffers(tree, triangles, root, m_RenderTexture);

		
	}

	private int InitScene(out List<Node> tree, out List<Triangle> datas)
	{
		Bounds bounds;
		var triangles = Utils.GetTrianglesInScene(true, out bounds);

		tree = null;
		datas = null;

		//return LKDTree.BuildKDTree(triangles, 0, 7, ref tree, ref datas);
		return LBVH.BuildBVH(triangles, bounds, ref tree, ref datas);
	}

	private void InitComputeBuffers(List<Node> tree, List<Triangle> datas, int root, RenderTexture texture)
	{
		int sizeofnode = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Node));
		int sizeoftriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));

		m_NodesBuffer = new ComputeBuffer(tree.Count, sizeofnode);
		m_TrianglesBuffer = new ComputeBuffer(datas.Count, sizeoftriangle);

		m_KernelIndex = computeShader.FindKernel("CSMain");

		m_NodesBuffer.SetData(tree.ToArray());
		m_TrianglesBuffer.SetData(datas.ToArray());

		computeShader.SetBuffer(m_KernelIndex, "_Tree", m_NodesBuffer);
		computeShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
		computeShader.SetTexture(m_KernelIndex, "_Result", m_RenderTexture);
		computeShader.SetInt("_NodeCounts", tree.Count);
		computeShader.SetInt("_RootNode", root);
		computeShader.SetInt("_TexWidth", texture.width);
		computeShader.SetInt("_TexHeight", texture.height);
		//computeShader.SetInt("_SampleNum", 83);
		computeShader.SetTexture(m_KernelIndex, "_Sky", sky);
	    computeShader.SetFloat("_LightRange", 8);
    }

	void Update()
	{
		float h = m_Camera.nearClipPlane * Mathf.Tan(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		float w = h * m_Camera.aspect;

		computeShader.SetFloat("_Near", m_Camera.nearClipPlane);
		computeShader.SetFloat("_NearClipWidth", w);
		computeShader.SetFloat("_NearClipHeight", h);
		computeShader.SetMatrix("_CameraToWorld", m_Camera.transform.localToWorldMatrix);
		//computeShader.SetInt("_SampleIndex", m_SampleIndex);
		//m_SampleIndex += 1;
		//if (m_SampleIndex >= 2000)
		//	m_SampleIndex = 0;

		//int offset = Random.Range(0, 82);
		//computeShader.SetInt("_SampleIndex", offset);
		computeShader.SetFloat("_Time", Time.time);
        computeShader.SetVector("_LightDir", -light.transform.forward);
        computeShader.SetVector("_LightColor", light.color);
        computeShader.SetFloat("_LightIntensity", lightIntensity);
        computeShader.SetFloat("_SkyHDR", skyHDR);
	    

		computeShader.Dispatch(m_KernelIndex, m_DispatchX, m_DispatchY, 1);

		if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2) || Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f)
			m_MouseDown = true;
		else
		{
			m_MouseDown = false;
		}
	}
	
	void OnDestroy()
	{
		if (m_TrianglesBuffer != null)
			m_TrianglesBuffer.Release();
		if (m_NodesBuffer != null)
			m_NodesBuffer.Release();
		if (m_RenderTexture)
			Destroy(m_RenderTexture);
		if (m_TempRt)
			RenderTexture.ReleaseTemporary(m_TempRt);
		m_TrianglesBuffer = null;
		m_NodesBuffer = null;
		m_RenderTexture = null;
		//m_TempRt = null;
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (m_RenderTexture)
		{
			if (!m_TempRt)
			{
				Graphics.Blit(m_RenderTexture, destination);
				m_TempRt = RenderTexture.GetTemporary(m_RenderTexture.width, m_RenderTexture.height);
			}
			else
			{
				if (m_MouseDown)
				{
					Graphics.Blit(m_RenderTexture, destination);
					m_Frame = 0.0f;
				}
				else
				{

					m_Material.SetFloat("_Frame", m_Frame);
					m_Material.SetTexture("_Cache", m_TempRt);
					Graphics.Blit(m_RenderTexture, destination, m_Material);

					m_Frame += 1.0f;
				}
			}

			Graphics.Blit(destination, m_TempRt);
			//Graphics.Blit(m_RenderTexture, destination);


		}
		else
			Graphics.Blit(source, destination);
	}
}
