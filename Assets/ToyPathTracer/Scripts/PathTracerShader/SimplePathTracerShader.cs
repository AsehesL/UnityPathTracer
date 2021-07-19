using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SimplePTMaterial
{
    public Vector3 albedo;
    public int shadingType;

    public SimplePTMaterial(Material material)
    {
        Color emissiveColor = material.GetColor("_EmissionColor");
        Color albedoColor = material.GetColor("_Color");
        float smoothness = material.GetFloat("_Glossiness");
        if (emissiveColor.grayscale > 0.01f)
        {
            shadingType = (int)ShadingType.Emissive;
            albedo = new Vector3(emissiveColor.r, emissiveColor.g, emissiveColor.b);
        }
        else
        {
            albedo = new Vector3(albedoColor.r, albedoColor.g, albedoColor.b);
            if (albedoColor.a <= 0.01f)
            {
                shadingType = (int)ShadingType.Refract;
            }
            else if (smoothness >= 0.9f)
            {
                shadingType = (int)ShadingType.Reflect;
            }
            else
            {
                shadingType = (int)ShadingType.Diffuse;
            }
        }
    }
}

[CreateAssetMenu]
public class SimplePathTracerShader : ToyPathTracingShaderBase
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
        List<SimplePTMaterial> simplePTMaterials = new List<SimplePTMaterial>();
        if (materials != null && materials.Count > 0)
        {
            for(int i=0;i<materials.Count;i++)
            {
                if (materials[i])
                    simplePTMaterials.Add(new SimplePTMaterial(materials[i]));
            }
        }
        ComputeBuffer buffer = new ComputeBuffer(simplePTMaterials != null && simplePTMaterials.Count > 0 ? simplePTMaterials.Count : 1, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SimplePTMaterial)));
        if (simplePTMaterials != null && simplePTMaterials.Count > 0)
            buffer.SetData(simplePTMaterials.ToArray());
        return buffer;
    }

    public override string GetKernelName()
    {
        return "SimplePT";
    }
}
