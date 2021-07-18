using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class ToyPathTracer : MonoBehaviour
{
    public bool enableFilter = true;

    public Cubemap skyCubeMap;

    public ToyPathTracingShaderBase shader;

    public float focal;

    public float radius;

    [SerializeField]
    private Shader m_TimeFilterShader;

    private ToyPathTracingPipeline m_Pipeline;

    private Material m_TimeFilterMat;

    private RenderTexture m_PreviousFrame;

    private float m_Frame;

    private Matrix4x4 m_LocalToWorld;

    private float m_Focal;

    private float m_Radius;

    private void Start()
    {
        m_Pipeline = new ToyPathTracingPipeline(gameObject.GetComponent<Camera>(), shader);

        m_Pipeline.BuildPipeline();

        m_Pipeline.SkyCubeMap = skyCubeMap;

        m_TimeFilterMat = new Material(m_TimeFilterShader);
    }

    private void OnDestroy()
    {
        m_Pipeline.DestroyPipeline();
        if (m_TimeFilterMat)
            Destroy(m_TimeFilterMat);
        m_TimeFilterMat = null;
        if (m_PreviousFrame)
            RenderTexture.ReleaseTemporary(m_PreviousFrame);
        m_PreviousFrame = null;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        m_Pipeline.focal = focal;
        m_Pipeline.radius = radius;
        RenderTexture rt = m_Pipeline.ExecutePipeline();
        if (rt)
        {
            if (!enableFilter)
            {
                Graphics.Blit(rt, destination);
                if (m_PreviousFrame)
                    RenderTexture.ReleaseTemporary(m_PreviousFrame);
                m_PreviousFrame = null;
                return;
            }
            if (m_PreviousFrame == null || m_LocalToWorld != transform.localToWorldMatrix || m_Focal != focal || m_Radius != radius || m_Pipeline.IsLightChanged)
            {
                Graphics.Blit(rt, destination);
                m_LocalToWorld = transform.localToWorldMatrix;
                m_Focal = focal;
                m_Radius = radius;
                m_Frame = 0.0f;
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

                Graphics.Blit(m_PreviousFrame, destination);
            }
        }
        else
            Graphics.Blit(source, destination);
    }
}
