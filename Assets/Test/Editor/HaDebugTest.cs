using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HaDebugTest
{
	[MenuItem("Test/ShowSample")]
	static void Show()
	{
		for (int i = 0; i <= 4; i++)
		{
			Debug.Log(Hammersley2D(i, 4).ToString("f4"));
		}
	}


	private static double RadicalInverseVdC(long bits)
	{
		bits = (bits << 16) | (bits >> 16);
		bits = ((bits & 0x55555555) << 1) | ((bits & 0xAAAAAAAA) >> 1);
		bits = ((bits & 0x33333333) << 2) | ((bits & 0xCCCCCCCC) >> 2);
		bits = ((bits & 0x0F0F0F0F) << 4) | ((bits & 0xF0F0F0F0) >> 4);
		bits = ((bits & 0x00FF00FF) << 8) | ((bits & 0xFF00FF00) >> 8);
		return ((double)bits) * 2.3283064365386963e-10;
	}

 	private static Vector2 Hammersley2D(int i, int N)
	{
		return new Vector2((float)i / N, (float)RadicalInverseVdC(i));
	}
}
