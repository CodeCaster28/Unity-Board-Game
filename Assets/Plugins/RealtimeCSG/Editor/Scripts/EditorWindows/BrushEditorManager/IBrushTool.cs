using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal interface IBrushTool
	{
		bool UsesUnitySelection { get; }
		bool IgnoreUnityRect	{ get; }

		void HandleEvents		(Rect rect);
		
		Rect GetLastSceneGUIRect	();
		bool OnSceneGUI			();
		void OnInspectorGUI		(EditorWindow window);

		void OnDisableTool		();
		void OnEnableTool		();
		bool UndoRedoPerformed	();
		bool DeselectAll		();

		void SetTargets			(FilteredSelection filteredSelection);
	
	}
}
