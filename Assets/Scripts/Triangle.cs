using UnityEngine;

public struct Triangle
{
    public Vector3 vertex0;
    public Vector3 vertex1;
    public Vector3 vertex2;

    public Vector3 normal0;
    public Vector3 normal1;
    public Vector3 normal2;

    public Triangle(Vertex vertex0, Vertex vertex1, Vertex vertex2)
    {
        this.vertex0 = vertex0.vertex;
        this.vertex1 = vertex1.vertex;
        this.vertex2 = vertex2.vertex;
        this.normal0 = vertex0.normal;
        this.normal1 = vertex1.normal;
        this.normal2 = vertex2.normal;
    }

    public Vertex GetVertex(int index)
    { 
        if (index == 0)
            return new Vertex {vertex = vertex0, normal = normal0};
        else if (index == 1)
            return new Vertex {vertex = vertex1, normal = normal1 };
        else if (index == 2)
            return new Vertex {vertex = vertex2, normal = normal2 };
        throw new System.IndexOutOfRangeException();
    }
}

public struct Vertex
{
    public Vector3 vertex;
    public Vector3 normal;

    public static Vertex Lerp(Vertex begin, Vertex end, float t)
    {
        return new Vertex
        {
            vertex = Vector3.Lerp(begin.vertex, end.vertex, t),
            normal = Vector3.Lerp(begin.normal, end.normal, t),
        };
    }
}