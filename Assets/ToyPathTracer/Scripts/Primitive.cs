using UnityEngine;
using System.Collections.Generic;

public enum PrimitiveType
{
    Sphere = 0,
    Quad = 1,
    Cube = 2,
    Triangle = 3,
}

public struct Sphere
{
    public Vector4 positionAndRadius;
    public int matId;
}

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
    public Vector2 uv0;
    public Vector2 uv1;
    public Vector2 uv2;
    public Vector4 tangent0;
    public Vector4 tangent1;
    public Vector4 tangent2;
    public int matId;
}

public struct Cube
{
    public Matrix4x4 localToWorld;
    public Matrix4x4 worldToLocal;
    public Vector3 size;
    public int matId;
}

public abstract class Primitive
{
    public abstract PrimitiveType primitiveType
    {
        get;
    }

    public abstract Bounds bounds
    {
        get;
    }

    public int matId;

    public virtual Quad CreateQuad()
    {
        return default(Quad);
    }

    public virtual Sphere CreateSphere()
    {
        return default(Sphere);
    }

    public virtual Triangle CreateTriangle()
    {
        return default(Triangle);
    }

    public virtual Cube CreateCube()
    {
        return default(Cube);
    }

    public abstract float GetArea();

    public abstract bool IsChanged();

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
                Vector3 scale = new Vector3(boxCollider.size.x * boxCollider.transform.lossyScale.x, boxCollider.size.y * boxCollider.transform.lossyScale.y, boxCollider.size.z * boxCollider.transform.lossyScale.z);
                if ((scale.x < 0.02f && scale.y >= 0.02f && scale.z >= 0.02f)
                    || (scale.y < 0.02f && scale.z >= 0.02f && scale.x >= 0.02f)
                    || (scale.z < 0.02f && scale.x >= 0.02f && scale.y >= 0.02f))
                {
                    primitive = new QuadPrimitive(boxCollider);
                }
                else
                    primitive = new CubePrimitive(boxCollider);
            }
        }
        if (primitive == null)
        {
            SphereCollider sphereCollider = gameObject.GetComponent<SphereCollider>();
            if (sphereCollider)
            {
                primitive = new SpherePrimitive(sphereCollider);
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

                    Primitive trianglePrimitive = new TrianglePrimitive(vertex0, vertex1, vertex2, normal0, normal1, normal2);
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

public class CubePrimitive : Primitive
{
    public override PrimitiveType primitiveType => PrimitiveType.Cube;

    public override float GetArea()
    {
        Vector3 s = size;
        return s.x * s.y * 2.0f + s.x * s.z * 2.0f + s.y * s.z * 2.0f;
    }

    public override Bounds bounds
    {
        get
        {
            Vector3 halfSize = size * 0.5f;
            Vector3 p0 = localToWorld.MultiplyPoint(new Vector3(halfSize.x, halfSize.y, halfSize.z));
            Vector3 p1 = localToWorld.MultiplyPoint(new Vector3(halfSize.x, halfSize.y, -halfSize.z));
            Vector3 p2 = localToWorld.MultiplyPoint(new Vector3(-halfSize.x, halfSize.y, halfSize.z));
            Vector3 p3 = localToWorld.MultiplyPoint(new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
            Vector3 p4 = localToWorld.MultiplyPoint(new Vector3(halfSize.x, -halfSize.y, halfSize.z));
            Vector3 p5 = localToWorld.MultiplyPoint(new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
            Vector3 p6 = localToWorld.MultiplyPoint(new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
            Vector3 p7 = localToWorld.MultiplyPoint(new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));

            Vector3 min = p0;
            Vector3 max = p0;
            min = Vector3.Min(min, p1);
            min = Vector3.Min(min, p2);
            min = Vector3.Min(min, p3);
            min = Vector3.Min(min, p4);
            min = Vector3.Min(min, p5);
            min = Vector3.Min(min, p6);
            min = Vector3.Min(min, p7);
            max = Vector3.Max(max, p1);
            max = Vector3.Max(max, p2);
            max = Vector3.Max(max, p3);
            max = Vector3.Max(max, p4);
            max = Vector3.Max(max, p5);
            max = Vector3.Max(max, p6);
            max = Vector3.Max(max, p7);
            Vector3 si = (max - min);
            Vector3 ct = min + si * 0.5f;
            si *= 1.001f;
            if (si.x <= 0)
                si.x = 0.1f;
            if (si.y <= 0)
                si.y = 0.1f;
            if (si.z <= 0)
                si.z = 0.1f;

            return new Bounds(ct, si);
        }
    }

    public Matrix4x4 localToWorld
    {
        get
        {
            Vector3 position = m_BoxCollider.transform.position + m_BoxCollider.center;
            Quaternion rot = m_BoxCollider.transform.rotation;
            return Matrix4x4.TRS(position, rot, Vector3.one);
        }
    }

    public Matrix4x4 worldToLocal
    {
        get
        {
            return localToWorld.inverse;
        }
    }

    public Vector3 size
    {
        get
        {
            return new Vector3(m_BoxCollider.size.x * m_BoxCollider.transform.lossyScale.x, m_BoxCollider.size.y * m_BoxCollider.transform.lossyScale.y, m_BoxCollider.size.z * m_BoxCollider.transform.lossyScale.z);
        }
    }

    public override Cube CreateCube()
    {
        return new Cube
        {
            localToWorld = localToWorld,
            worldToLocal = worldToLocal,
            size = size,
            matId = matId
        };
    }

    private BoxCollider m_BoxCollider;

    private Matrix4x4 m_LocalToWorld;
    private Vector3 m_Size;

    public CubePrimitive(BoxCollider boxCollider)
    {
        m_BoxCollider = boxCollider;
    }

    public override bool IsChanged()
    {
        if (m_LocalToWorld != localToWorld || m_Size != size)
        {
            m_LocalToWorld = localToWorld;
            m_Size = size;
            return true;
        }
        return false;
    }
}

public class SpherePrimitive : Primitive
{
    public override PrimitiveType primitiveType => PrimitiveType.Sphere;

    public override float GetArea()
    {
        return 4.0f * Mathf.PI * radius * radius;
    }

    public override Bounds bounds
    {
        get
        {
            return m_SphereCollider.bounds;
        }
    }

    public Vector3 center
    {
        get
        {
            return m_SphereCollider.transform.position + m_SphereCollider.center;
        }
    }

    public float radius
    {
        get
        {
            float scale = Mathf.Max(Mathf.Max(m_SphereCollider.transform.lossyScale.x, m_SphereCollider.transform.lossyScale.y), m_SphereCollider.transform.lossyScale.z);
            return m_SphereCollider.radius * scale;
        }
    }

    public override Sphere CreateSphere()
    {
        return new Sphere
        {
            positionAndRadius = new Vector4(center.x, center.y, center.z, radius),
            matId = matId,
        };
    }

    private SphereCollider m_SphereCollider;

    private Vector3 m_Center;
    private float m_Radius;

    public SpherePrimitive(SphereCollider sphereCollider)
    {
        m_SphereCollider = sphereCollider;
    }

    public override bool IsChanged()
    {
        if (m_Center != center || m_Radius != radius)
        {
            m_Center = center;
            m_Radius = radius;
            return true;
        }
        return false;
    }
}

public class QuadPrimitive : Primitive
{
    public override PrimitiveType primitiveType => PrimitiveType.Quad;

    public override Bounds bounds
    {
        get
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
    }

    public override float GetArea()
    {
        return Mathf.Sqrt(this.sqrWidth * this.sqrHeight);
    }

    public Vector3 center
    {
        get
        {
            return m_BoxCollider.transform.position - right * 0.5f - forward * 0.5f;
        }
    }

    public Vector3 normal
    {
        get
        {
            Vector3 scale = new Vector3(m_BoxCollider.size.x * m_BoxCollider.transform.lossyScale.x, m_BoxCollider.size.y * m_BoxCollider.transform.lossyScale.y, m_BoxCollider.size.z * m_BoxCollider.transform.lossyScale.z);
            if (scale.y < 0.02f)
                return m_BoxCollider.transform.up.normalized;
            if (scale.x < 0.02f)
                return m_BoxCollider.transform.right.normalized;
            if (scale.z < 0.02f)
                return m_BoxCollider.transform.forward.normalized;
            return m_BoxCollider.transform.up.normalized;
        }
    }

    public Vector3 right
    {
        get
        {
            Vector3 scale = new Vector3(m_BoxCollider.size.x * m_BoxCollider.transform.lossyScale.x, m_BoxCollider.size.y * m_BoxCollider.transform.lossyScale.y, m_BoxCollider.size.z * m_BoxCollider.transform.lossyScale.z);

            if (scale.y < 0.02f)
                return m_BoxCollider.transform.right.normalized * scale.x;
            if (scale.x < 0.02f)
                return m_BoxCollider.transform.forward.normalized * scale.z;
            if (scale.z < 0.02f)
                return m_BoxCollider.transform.right.normalized * scale.x;
            return m_BoxCollider.transform.right.normalized * scale.x;
        }
    }

    public Vector3 forward
    {
        get
        {
            Vector3 scale = new Vector3(m_BoxCollider.size.x * m_BoxCollider.transform.lossyScale.x, m_BoxCollider.size.y * m_BoxCollider.transform.lossyScale.y, m_BoxCollider.size.z * m_BoxCollider.transform.lossyScale.z);
            
            if (scale.y < 0.02f)
                return m_BoxCollider.transform.forward.normalized * scale.z;
            if (scale.x < 0.02f)
                return m_BoxCollider.transform.up.normalized * scale.y;
            if (scale.z < 0.02f)
                return m_BoxCollider.transform.up.normalized * scale.y;
            return m_BoxCollider.transform.forward.normalized * scale.z;
        }
    }

    public float sqrWidth
    {
        get
        {
            return right.sqrMagnitude;
        }
    }

    public float sqrHeight
    {
        get
        {
            return forward.sqrMagnitude;
        }
    }

    public override Quad CreateQuad()
    {
        return new Quad
        {
            right = new Vector4(this.right.x, this.right.y, this.right.z, this.sqrWidth),
            forward = new Vector4(this.forward.x, this.forward.y, this.forward.z, this.sqrHeight),
            normal = this.normal,
            position = this.center,
            matId = this.matId,
        };
    }

    private BoxCollider m_BoxCollider;

    private Vector3 m_Right;
    private Vector3 m_Forward;
    private Vector3 m_Position;

    public QuadPrimitive(BoxCollider boxCollider)
    {
        m_BoxCollider = boxCollider;
    }

    public override bool IsChanged()
    {
        if (m_Right != right || m_Forward != forward || m_Position != center)
        {
            m_Right = right;
            m_Forward = forward;
            m_Position = center;
            return true;
        }
        return false;
    }
}

public class TrianglePrimitive : Primitive
{
    public override PrimitiveType primitiveType => PrimitiveType.Triangle;

    public override Bounds bounds
    {
        get
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
    }

    public Vector3 vertex0;
    public Vector3 vertex1;
    public Vector3 vertex2;
    public Vector3 normal0;
    public Vector3 normal1;
    public Vector3 normal2;

    public override Triangle CreateTriangle()
    {
        return new Triangle
        {
            vertex0 = vertex0,
            vertex1 = vertex1,
            vertex2 = vertex2,
            normal0 = normal0,
            normal1 = normal1,
            normal2 = normal2,
            matId = matId,
        };
    }

    public TrianglePrimitive(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2)
    {
        vertex0 = v0;
        vertex1 = v1;
        vertex2 = v2;
        normal0 = n0;
        normal1 = n1;
        normal2 = n2;
    }

    public override float GetArea()
    {
        float a = Vector3.Distance(vertex0, vertex1);
        float b = Vector3.Distance(vertex0, vertex2);
        float c = Vector3.Distance(vertex1, vertex2);
        float p = (a + b + c) * 0.5f;
        return Mathf.Sqrt(p * (p - a) * (p - b) * (p - c));
    }

    public override bool IsChanged()
    {
        return false;
    }
}