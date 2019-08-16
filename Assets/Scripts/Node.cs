using UnityEngine;
using System.Collections;

public struct Node
{
	public Vector3 min;
	public Vector3 max;
	public int leftChild;
	public int rightChild;
	public int dataBegin;
	public int dataEnd;
}
