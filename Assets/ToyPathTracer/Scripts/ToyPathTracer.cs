using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class ToyPathTracer : MonoBehaviour
{
    public bool enableFilter = true;

    public Texture skyTexture;

    public Texture albedoTexture;

    public ComputeShader shader;

    public string kernelName;

    public bool sampleDirect;

    public float focal;

    public float radius;

    public float sigma = 0.8f;
    public float KSigma = 1.8f;
    public float threshold = 0.25f;

    [SerializeField]
    [HideInInspector]
    private Shader m_TimeFilterShader;

    [SerializeField]
    [HideInInspector]
    private Shader m_DenoiseShader;

    private ToyPathTracingPipeline m_Pipeline;

    private Material m_TimeFilterMat;

    private Material m_DenoiseMat;

    private RenderTexture m_PreviousFrame;

    private float m_Frame;

    private Matrix4x4 m_LocalToWorld;

    private float m_Focal;

    private float m_Radius;

    private void Start()
    {
        m_Pipeline = new ToyPathTracingPipeline(gameObject.GetComponent<Camera>(), shader, kernelName, sampleDirect);

        m_Pipeline.BuildPipeline();

        m_Pipeline.SkyTexture = skyTexture;
        m_Pipeline.AlbedoTexture = albedoTexture;

        m_TimeFilterMat = m_TimeFilterShader == null ? null : new Material(m_TimeFilterShader);
        m_DenoiseMat = m_DenoiseShader == null ? null : new Material(m_DenoiseShader);
    }

    private void OnDestroy()
    {
        m_Pipeline.DestroyPipeline();
        if (m_TimeFilterMat)
            Destroy(m_TimeFilterMat);
        m_TimeFilterMat = null;
        if (m_DenoiseMat)
            Destroy(m_DenoiseMat);
        m_DenoiseMat = null;
        if (m_PreviousFrame)
            RenderTexture.ReleaseTemporary(m_PreviousFrame);
        m_PreviousFrame = null;
    }

    private void DeNoise(RenderTexture src, RenderTexture dst)
    {
        if (m_DenoiseMat)
        {
            m_DenoiseMat.SetFloat("_Sigma", sigma);
            m_DenoiseMat.SetFloat("_KSigma", KSigma);
            m_DenoiseMat.SetFloat("_Threshold", threshold);
            Graphics.Blit(src, dst, m_DenoiseMat);
        }
        else
            Graphics.Blit(src, dst);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_Pipeline.SkyTexture != skyTexture)
            m_Pipeline.SkyTexture = skyTexture;
        if (m_Pipeline.AlbedoTexture != albedoTexture)
            m_Pipeline.AlbedoTexture = albedoTexture;
        m_Pipeline.focal = focal;
        m_Pipeline.radius = radius;
        RenderTexture rt = m_Pipeline.ExecutePipeline();
        if (rt)
        {
            if (!enableFilter || !m_TimeFilterMat)
            {
                DeNoise(rt, destination);
                if (m_PreviousFrame)
                    RenderTexture.ReleaseTemporary(m_PreviousFrame);
                m_PreviousFrame = null;
                return;
            }
            if (m_PreviousFrame == null || m_LocalToWorld != transform.localToWorldMatrix || m_Focal != focal || m_Radius != radius || m_Pipeline.isLightChanged)
            {
                DeNoise(rt, destination);
                m_LocalToWorld = transform.localToWorldMatrix;
                m_Focal = focal;
                m_Radius = radius;
                m_Frame = 0.0f;
                m_Pipeline.isLightChanged = false;
                if (m_PreviousFrame == null)
                    m_PreviousFrame = RenderTexture.GetTemporary(rt.width, rt.height, rt.depth, rt.format);
                Graphics.Blit(rt, m_PreviousFrame);
            }
            else
            {
                RenderTexture temp = RenderTexture.GetTemporary(rt.width, rt.height, rt.depth, rt.format);
                m_TimeFilterMat.SetFloat("_Frame", m_Frame);
                m_TimeFilterMat.SetTexture("_PreviousFrame", m_PreviousFrame);
                Graphics.Blit(rt, temp, m_TimeFilterMat, 0);

                m_Frame += 1.0f;

                RenderTexture.ReleaseTemporary(m_PreviousFrame);
                m_PreviousFrame = temp;

                DeNoise(m_PreviousFrame, destination);
            }
        }
        else
            Graphics.Blit(source, destination);
    }
}
