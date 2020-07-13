using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestPT : MonoBehaviour
{
	public Cubemap sky;

	public ComputeShader computeShader;
	
	private ComputeBuffer m_TrianglesBuffer;

	private RenderTexture m_RenderTexture;

	private int m_DispatchX;
	private int m_DispatchY;

	private int m_KernelIndex;

	private Camera m_Camera;

	private Material m_Material;

    private float m_Frame;

	void Start()
	{
		m_Camera = gameObject.GetComponent<Camera>();

		int width = (int) (((float) Screen.width) * 0.3f);
		int height = (int) (((float) Screen.height) * 0.3f);

		m_RenderTexture = new RenderTexture(width, height, 24);
		m_RenderTexture.enableRandomWrite = true;
		m_RenderTexture.Create();

		m_DispatchX = Mathf.CeilToInt((float)m_RenderTexture.width / 2);
		m_DispatchY = Mathf.CeilToInt((float)m_RenderTexture.height / 2);
		
		InitComputeBuffers(m_RenderTexture);

	}
	

	private void InitComputeBuffers(RenderTexture texture)
	{
		Bounds bounds;
		var triangles = Utils.GetTrianglesInScene(true, out bounds);
		
		int sizeoftriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));

		m_TrianglesBuffer = new ComputeBuffer(triangles.Count, sizeoftriangle);

		m_KernelIndex = computeShader.FindKernel("CSMain");

		m_TrianglesBuffer.SetData(triangles.ToArray());

		//computeShader.SetBuffer(m_KernelIndex, "_Tree", m_NodesBuffer);
		computeShader.SetBuffer(m_KernelIndex, "_Triangles", m_TrianglesBuffer);
		computeShader.SetTexture(m_KernelIndex, "_Result", m_RenderTexture);
		//computeShader.SetInt("_NodeCounts", tree.Count);
		//computeShader.SetInt("_RootNode", root);
		computeShader.SetInt("_TriangleCount", triangles.Count);
		computeShader.SetInt("_TexWidth", texture.width);
		computeShader.SetInt("_TexHeight", texture.height);
		//computeShader.SetInt("_SampleNum", 83);
		computeShader.SetTexture(m_KernelIndex, "_Sky", sky);
	    //computeShader.SetFloat("_LightRange", 8);
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
		//computeShader.SetFloat("_Time", Time.time);
//        computeShader.SetVector("_LightDir", -light.transform.forward);
//        computeShader.SetVector("_LightColor", light.color);
//        computeShader.SetFloat("_LightIntensity", lightIntensity);
//        computeShader.SetFloat("_SkyHDR", skyHDR);
	    

		computeShader.Dispatch(m_KernelIndex, m_DispatchX, m_DispatchY, 1);
	}
	
	void OnDestroy()
	{
		if (m_TrianglesBuffer != null)
			m_TrianglesBuffer.Release();
		if (m_RenderTexture)
			Destroy(m_RenderTexture);
		m_TrianglesBuffer = null;
		m_RenderTexture = null;
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (m_RenderTexture)
		{
			Graphics.Blit(m_RenderTexture, destination);
		}
		else
			Graphics.Blit(source, destination);
	}
}
