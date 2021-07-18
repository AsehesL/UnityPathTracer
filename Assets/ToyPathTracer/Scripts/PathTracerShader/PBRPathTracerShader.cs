using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PBRPTMaterial
{
    public Color albedo;
    public float roughness;
    public float metallic;
    public int emissive;
    public int checkBoard;

    public PBRPTMaterial(Material material)
    {
        Color emissiveColor = material.GetColor("_EmissionColor");
        Color albedoColor = material.GetColor("_Color");
        float smoothness = material.GetFloat("_Glossiness");
        float metallic = material.GetFloat("_Metallic");
        Texture tex = material.GetTexture("_MainTex");
        checkBoard = tex != null ? 1 : -1;
        if (emissiveColor.grayscale > 0.01f)
        {
            emissive = 1;
            albedo = emissiveColor;
            roughness = 1;
            this.metallic = 0;
        }
        else
        {
            albedo = albedoColor;
            emissive = -1;
            roughness = 1.0f - smoothness;
            this.metallic = metallic;
        }
    }
}

[CreateAssetMenu]
public class PBRPathTracerShader : ToyPathTracingShaderBase
{
    public ComputeShader computeShader;

    public override ComputeShader GetShader()
    {
        return computeShader;
    }

    public override bool IsEmissive(Material material)
    {
        Color emissive = material.GetColor("_EmissionColor");
        if (emissive.grayscale > 0.0001f)
            return true;
        return false;
    }

    public override bool ShouldSampleDirectLight()
    {
        return true;
    }

    public override ComputeBuffer CreateMaterialBuffer(List<Material> materials)
    {
        List<PBRPTMaterial> pbrMats = new List<PBRPTMaterial>();
        if (materials != null && materials.Count > 0)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i])
                    pbrMats.Add(new PBRPTMaterial(materials[i]));
            }
        }
        ComputeBuffer buffer = new ComputeBuffer(pbrMats != null && pbrMats.Count > 0 ? pbrMats.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(PBRPTMaterial)));
        if (pbrMats != null && pbrMats.Count > 0)
            buffer.SetData(pbrMats.ToArray());
        return buffer;
    }

    public override string GetKernelName()
    {
        return "PBRPathTracerMain";
    }
}
