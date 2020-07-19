using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PathTracerMaterial
{
    public Vector3 albedo;
    public Vector3 emissive;
    public float roughness;
    //public int trancparency;
}

public class PTMaterial : MonoBehaviour
{
    public Color albedo;
    public Color emissive;
    public float emissiveIndentity;
    public float roughness;
    //public bool trancparency;

    public PathTracerMaterial GetMaterial()
    {
        PathTracerMaterial mat = new PathTracerMaterial();
        mat.albedo = new Vector3(albedo.r,albedo.g, albedo.b);
        mat.emissive = new Vector3(emissive.r, emissive.g, emissive.b) * emissiveIndentity;
        mat.roughness = roughness;
        //mat.trancparency = trancparency ? 1 : 0;
        return mat;
    }
}
