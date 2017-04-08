using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Reflection.Obfuscation(Exclude = true)]
public class CSGBrushEditorWindow : EditorWindow
{
	private const string WindowTitle = "Realtime-CSG";
	private static List<CSGBrushEditorWindow> windows = new List<CSGBrushEditorWindow>();
	
	public static List<CSGBrushEditorWindow> GetEditorWindows()
	{
		for (int i = windows.Count - 1; i >= 0; i--)
		{
			if (!windows[i])
				windows.Remove(windows[i]);
		}
		return windows; 
	}

	[MenuItem ("Window/Realtime-CSG window %F2")]
	public static CSGBrushEditorWindow GetWindow ()
	{
		var editorWindow = EditorWindow.GetWindow<CSGBrushEditorWindow>(WindowTitle, true, typeof(CSGBrushEditor));
		editorWindow.minSize = new Vector2(32,  64);
		editorWindow.Show();
		return editorWindow;
	}

	public void OnGUI()
	{
		RealtimeCSG.CSGBrushEditorGUI.HandleWindowGUI(this);
	}

	void Awake()
	{
		windows.Add(this);
	}

	//bool prevFail = false;

	void Update()
	{
		// apparently 'Awake' is not reliable ...
		if (windows.Contains(this))
			return;
		/*
		if (!prevFail)
		{
			prevFail = true;
			prevFail = EditorUtility.GetObjectEnabled(this) == 1;
			if (!prevFail)
			{
				windows.Add(this);
			}
		} else
		{ 
			this.Close();
			GetWindow();
		}
		/*/
		windows.Add(this);
		//*/
	}
	
	void OnDestroy()
	{
		windows.Remove(this);
	}
}
