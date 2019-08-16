using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public class SkyCreator : EditorWindow
{
	private Texture2D m_Front;
	private Texture2D m_Back;
	private Texture2D m_Right;
	private Texture2D m_Left;
	private Texture2D m_Up;
	private Texture2D m_Down;

	private static MethodInfo m_CopyTextureIntoCubemapFace;

	[MenuItem("Test/SkyCreator")]
	static void Init()
	{
		SkyCreator.GetWindow<SkyCreator>();
	}

	void OnGUI()
	{
		m_Front = EditorGUILayout.ObjectField("Front", m_Front, typeof(Texture2D), false) as Texture2D;
		m_Back = EditorGUILayout.ObjectField("Back", m_Back, typeof(Texture2D), false) as Texture2D;
		m_Right = EditorGUILayout.ObjectField("Right", m_Right, typeof(Texture2D), false) as Texture2D;
		m_Left = EditorGUILayout.ObjectField("Left", m_Left, typeof(Texture2D), false) as Texture2D;
		m_Up = EditorGUILayout.ObjectField("Up", m_Up, typeof(Texture2D), false) as Texture2D;
		m_Down = EditorGUILayout.ObjectField("Down", m_Down, typeof(Texture2D), false) as Texture2D;
		if (GUILayout.Button("Bake"))
		{
			Bake();
		}
	}

	private void Bake()
	{
		Cubemap cubemap = CreateCubeMap(m_Front, m_Left, m_Back, m_Right, m_Down, m_Up);
		AssetDatabase.CreateAsset(cubemap, "Assets/Cube.cubemap");
	}

	private static Cubemap CreateCubeMap(Texture2D front, Texture2D left, Texture2D back, Texture2D right, Texture2D down, Texture2D up)
	{
		Cubemap cubemap = new Cubemap(1024, TextureFormat.RGBA32, false);

		CopyTextureIntoCubemapFace(front, cubemap, CubemapFace.PositiveZ);
		CopyTextureIntoCubemapFace(left, cubemap, CubemapFace.NegativeX);
		CopyTextureIntoCubemapFace(back, cubemap, CubemapFace.NegativeZ);
		CopyTextureIntoCubemapFace(right, cubemap, CubemapFace.PositiveX);
		CopyTextureIntoCubemapFace(down, cubemap, CubemapFace.NegativeY);
		CopyTextureIntoCubemapFace(up, cubemap, CubemapFace.PositiveY);

		return cubemap;
	}

	private static void CopyTextureIntoCubemapFace(Texture2D textureRef, Cubemap cubemapRef, CubemapFace face)
	{
		if (m_CopyTextureIntoCubemapFace == null)
		{
			var textureutil = typeof(Editor).Assembly.GetType("UnityEditor.TextureUtil");
			m_CopyTextureIntoCubemapFace =
				textureutil.GetMethod("CopyTextureIntoCubemapFace", BindingFlags.Static | BindingFlags.Public);
		}

		if (m_CopyTextureIntoCubemapFace != null)
			m_CopyTextureIntoCubemapFace.Invoke(null, new object[] { textureRef, cubemapRef, face });
	}
}
