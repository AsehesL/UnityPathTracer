using UnityEngine;
using System.Collections.Generic;

public enum PrimitiveType
{
    Sphere = 0,
    Quad = 1,
    Triangle = 2,
    Unknown,
}

public struct BVHNode
{
    public Vector3 boundsMin;
    public Vector3 boundsMax;
    public int leftChild;
    public int rightChild;
    public int primitiveId;
    public int primitiveType;
}

public struct Sphere
{
    public Vector4 positionAndRadius;
    public int matId;
};

public struct Quad
{
    public Vector4 right;
    public Vector4 forward;
    public Vector3 position;
    public Vector3 normal;
    public int matId;
}

public struct Triangle
{
    public Vector3 vertex0;
    public Vector3 vertex1;
    public Vector3 vertex2;
    public Vector3 normal0;
    public Vector3 normal1;
    public Vector3 normal2;
    public int matId;
}

public enum ShadingType
{
    Diffuse,
    Reflect,
    Refract,
    Emissive,
}

public class Primitive
{
    public PrimitiveType primitiveType = PrimitiveType.Unknown;

    public Bounds bounds
    {
        get
        {
            if (primitiveType == PrimitiveType.Quad)
            {
                Vector3 pos0 = this.center;
                Vector3 pos1 = this.center + this.right;
                Vector3 pos2 = this.center + this.forward;
                Vector3 pos3 = this.center + this.right + this.forward;
                Vector3 min = pos0;
                Vector3 max = pos0;
                min = Vector3.Min(min, pos1);
                min = Vector3.Min(min, pos2);
                min = Vector3.Min(min, pos3);
                max = Vector3.Max(max, pos1);
                max = Vector3.Max(max, pos2);
                max = Vector3.Max(max, pos3);
                Vector3 si = max - min;
                Vector3 ct = min + si * 0.5f;
                if (si.x <= 0)
                    si.x = 0.1f;
                if (si.y <= 0)
                    si.y = 0.1f;
                if (si.z <= 0)
                    si.z = 0.1f;
                si += new Vector3(0.01f, 0.01f, 0.01f);

                return new Bounds(ct, si);
            }
            else if (primitiveType == PrimitiveType.Sphere)
            {
                return m_SphereCollider.bounds;
            }
            else if (primitiveType == PrimitiveType.Triangle)
            {
                Vector3 min = vertex0;
                Vector3 max = vertex0;
                min = Vector3.Min(min, vertex1);
                min = Vector3.Min(min, vertex2);
                max = Vector3.Max(max, vertex1);
                max = Vector3.Max(max, vertex2);
                Vector3 si = max - min;
                Vector3 ct = min + si * 0.5f;
                if (si.x <= 0)
                    si.x = 0.1f;
                if (si.y <= 0)
                    si.y = 0.1f;
                if (si.z <= 0)
                    si.z = 0.1f;
                si += new Vector3(0.01f, 0.01f, 0.01f);
                return new Bounds(ct, si);
            }
            return default(Bounds);
        }
    }

    public Vector3 normal
    {
        get
        {
            if (primitiveType == PrimitiveType.Quad)
                return m_BoxCollider.transform.up;
            return default(Vector3);
        }
    }

    public Vector3 center
    {
        get
        {
            if (primitiveType == PrimitiveType.Quad)
            {
                Vector3 scale = new Vector3(m_BoxCollider.size.x * m_BoxCollider.transform.lossyScale.x, m_BoxCollider.size.y * m_BoxCollider.transform.lossyScale.y, m_BoxCollider.size.z * m_BoxCollider.transform.lossyScale.z);
                return m_BoxCollider.transform.position - m_BoxCollider.transform.right.normalized * scale.x * 0.5f - m_BoxCollider.transform.forward.normalized * scale.z * 0.5f;
            }
            else if (primitiveType == PrimitiveType.Sphere)
            {
                return m_SphereCollider.transform.position;
            }
            return default(Vector3);
        }
    }

    public Vector3 right
    {
        get
        {
            if (primitiveType == PrimitiveType.Quad)
            {
                Vector3 scale = new Vector3(m_BoxCollider.size.x * m_BoxCollider.transform.lossyScale.x, m_BoxCollider.size.y * m_BoxCollider.transform.lossyScale.y, m_BoxCollider.size.z * m_BoxCollider.transform.lossyScale.z);
                return m_BoxCollider.transform.right.normalized * scale.x;
            }
            return default(Vector3);
        }
    }

    public Vector3 forward
    {
        get
        {
            if (primitiveType == PrimitiveType.Quad)
            {
                Vector3 scale = new Vector3(m_BoxCollider.size.x * m_BoxCollider.transform.lossyScale.x, m_BoxCollider.size.y * m_BoxCollider.transform.lossyScale.y, m_BoxCollider.size.z * m_BoxCollider.transform.lossyScale.z);
                return m_BoxCollider.transform.forward.normalized * scale.z;
            }
            return default(Vector3);
        }
    }

    public float sqrWidth
    {
        get
        {
            if (primitiveType == PrimitiveType.Quad)
            {
                return right.sqrMagnitude;
            }
            return 0;
        }
    }

    public float sqrHeight
    {
        get
        {
            if (primitiveType == PrimitiveType.Quad)
            {
                return forward.sqrMagnitude;
            }
            return 0;
        }
    }

    public float radius
    {
        get
        {
            if (primitiveType == PrimitiveType.Sphere)
            {
                float scale = Mathf.Max(Mathf.Max(m_SphereCollider.transform.lossyScale.x, m_SphereCollider.transform.lossyScale.y), m_SphereCollider.transform.lossyScale.z);
                return m_SphereCollider.radius * scale;
            }
            return 0;
        }
    }

    public int matId;

    public Vector3 vertex0;
    public Vector3 vertex1;
    public Vector3 vertex2;
    public Vector3 normal0;
    public Vector3 normal1;
    public Vector3 normal2;

    private BoxCollider m_BoxCollider = null;
    private SphereCollider m_SphereCollider = null;

    public Primitive(BoxCollider boxCollider)
    {
        primitiveType = PrimitiveType.Quad;
        m_BoxCollider = boxCollider;
    }

    public Primitive(SphereCollider sphereCollider)
    {
        primitiveType = PrimitiveType.Sphere;
        m_SphereCollider = sphereCollider;
    }

    public Primitive(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, Vector3 normal0, Vector3 normal1, Vector3 normal2)
    {
        primitiveType = PrimitiveType.Triangle;

        this.vertex0 = vertex0;
        this.normal0 = normal0;

        this.vertex1 = vertex1;
        this.normal1 = normal1;

        this.vertex2 = vertex2;
        this.normal2 = normal2;
    }

    public static void CreatePrimitives(GameObject gameObject, ref Dictionary<Material, List<Primitive>> primitives)
    {
        if (primitives == null)
            primitives = new Dictionary<Material, List<Primitive>>();
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        Primitive primitive = null;
        if (!meshRenderer || !meshRenderer.sharedMaterial)
            return;
        if (primitive == null)
        {
            BoxCollider boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider)
            {
                primitive = new Primitive(boxCollider);
            }
        }
        if (primitive == null)
        {
            SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>();
            if (sphereCollider)
            {
                primitive = new Primitive(sphereCollider);
            }
        }
        if (primitive == null)
        {
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter && meshFilter.sharedMesh)
            {
                int[] triangles = meshFilter.sharedMesh.triangles;
                Vector3[] vertices = meshFilter.sharedMesh.vertices;
                Vector3[] normals = meshFilter.sharedMesh.normals;
                Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 vertex0 = localToWorld.MultiplyPoint(vertices[triangles[i]]);
                    Vector3 vertex1 = localToWorld.MultiplyPoint(vertices[triangles[i + 1]]);
                    Vector3 vertex2 = localToWorld.MultiplyPoint(vertices[triangles[i + 2]]);
                    Vector3 normal0 = localToWorld.MultiplyVector(normals[triangles[i]]);
                    Vector3 normal1 = localToWorld.MultiplyVector(normals[triangles[i + 1]]);
                    Vector3 normal2 = localToWorld.MultiplyVector(normals[triangles[i + 2]]);

                    Primitive trianglePrimitive = new Primitive(vertex0, vertex1, vertex2, normal0, normal1, normal2);
                    if (primitives.ContainsKey(meshRenderer.sharedMaterial) == false)
                        primitives.Add(meshRenderer.sharedMaterial, new List<Primitive>());
                    primitives[meshRenderer.sharedMaterial].Add(trianglePrimitive);
                }
            }
        }
        else
        {
            if (primitives.ContainsKey(meshRenderer.sharedMaterial) == false)
                primitives.Add(meshRenderer.sharedMaterial, new List<Primitive>());
            primitives[meshRenderer.sharedMaterial].Add(primitive);
        }    
    }
}
