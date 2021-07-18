using UnityEngine;
using System.Collections.Generic;

public class ToyPathTracingShaderBase : ScriptableObject
{

    public virtual bool ShouldSampleDirectLight()
    {
        return false;
    }

    public virtual ComputeShader GetShader()
    {
        return null;
    }

    public virtual bool IsEmissive(Material material)
    {
        return false;
    }

    public virtual ComputeBuffer CreateMaterialBuffer(List<Material> materials)
    {
        return null;
    }

    public virtual string GetKernelName()
    {
        return null;
    }
}
