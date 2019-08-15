using UnityEngine;

public struct Triangle
{
    public Vector3 vertex0;
    public Vector3 vertex1;
    public Vector3 vertex2;

    public Triangle(Vertex vertex0, Vertex vertex1, Vertex vertex2)
    {
        this.vertex0 = vertex0.vertex;
        this.vertex1 = vertex1.vertex;
        this.vertex2 = vertex2.vertex;
    }

    public Vertex GetVertex(int index)
    { 
        if (index == 0)
            return new Vertex {vertex = vertex0,};
        else if (index == 1)
            return new Vertex {vertex = vertex1,};
        else if (index == 2)
            return new Vertex {vertex = vertex2,};
        throw new System.IndexOutOfRangeException();
    }
}

public struct Vertex
{
    public Vector3 vertex;

    public static Vertex Lerp(Vertex begin, Vertex end, float t)
    {
        return new Vertex
        {
            vertex = Vector3.Lerp(begin.vertex, end.vertex, t),
        };
    }
}