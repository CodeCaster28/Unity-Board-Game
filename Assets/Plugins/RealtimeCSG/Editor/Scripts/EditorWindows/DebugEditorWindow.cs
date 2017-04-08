//#define USE_DEBUG_WINDOW
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace RealtimeCSG
{
#if USE_DEBUG_WINDOW
	[System.Reflection.Obfuscation(Exclude = true)]
	internal sealed class DebugEditorWindow : EditorWindow
    {
	#region GetWindow
	    static DebugEditorWindow GetWindow(bool generate)
	    {
		    if (!generate)
		    {
			    var items = Resources.FindObjectsOfTypeAll<DebugEditorWindow>();
			    if (items.Length == 0)	// NOTE: if we use the item we've found somehow unity 
									    // won't let it be flagged as dirty?
				    return null;
		    }
		    var window = (DebugEditorWindow)EditorWindow.GetWindow<DebugEditorWindow>("Debug Window");
		    return window;
	    }
	#endregion

	#region SetEditorDirty
	    public static void SetEditorDirty()
	    {
			var prevFocus = EditorWindow.focusedWindow;
			var window = GetWindow(generate: false);
			if (window == null)
				return;
		    EditorUtility.SetDirty(window);
			if (prevFocus != null) 
				prevFocus.Focus();
		}
	#endregion
				
		[MenuItem( "Window/CSG Debug Editor")] static void ShowCSGEditorWindow()
		{
			var window = GetWindow(generate: true); window.Show(); 
		}

		#region OnGUI
		void OnGUI()
	    {
		    EditorGUILayout.Space();
		    EditorGUILayout.Space();
            if (GUILayout.Button("Log Diagnostics"))
			{
				Debug.Log("--[C++ side]--\n");
				CSGBindings.LogDiagnostics();

				Debug.Log("--[C# side (registered)]--\n");
				Debug.Log(string.Format("models {0} brushes {1} operations {2}\n", InternalCSGModelManager.models.Length, InternalCSGModelManager.brushes.Length, InternalCSGModelManager.operations.Length));
				foreach(var model in InternalCSGModelManager.models)
				{
					Debug.Log(string.Format("\tmodel \"{0}\" node-id: {1} model-id: {2} \n", model.name, model.nodeID, model.modelID));
				}
				foreach (var operation in InternalCSGModelManager.operations)
				{
					Debug.Log(string.Format("\toperation \"{0}\" node-id: {1} operation-id: {2} \n", operation.name, operation.nodeID, operation.operationID));
				}
				foreach (var brush in InternalCSGModelManager.brushes)
				{
					Debug.Log(string.Format("\tbrush \"{0}\" node-id: {1} brush-id: {2} \n", brush.name, brush.nodeID, brush.brushID));
				}
				
				Debug.Log("--[C# side (unregistered)]--\n");

				var nodes		= CSGSceneUtility.FindComponentsInScene<CSGNode>(EditorSceneManager.GetActiveScene());
				var models		= (from node in nodes where node is CSGModel select node as CSGModel).ToArray();
				var operations	= (from node in nodes where node is CSGOperation select node as CSGOperation).ToArray();
				var brushes		= (from node in nodes where node is CSGBrush select node as CSGBrush).ToArray();

				Debug.Log(string.Format("models {0} brushes {1} operations {2}\n", models.Length, brushes.Length, operations.Length));
				foreach (var model in models)
				{
					if (InternalCSGModelManager.models.Contains(model))
						continue;
					Debug.Log(string.Format("\tmodel \"{0}\" node-id: {1} model-id: {2} \n", model.name, model.nodeID, model.modelID));
				}
				foreach (var operation in operations)
				{
					if (InternalCSGModelManager.operations.Contains(operation))
						continue;
					Debug.Log(string.Format("\toperation \"{0}\" node-id: {1} operation-id: {2} \n", operation.name, operation.nodeID, operation.operationID));
				}
				foreach (var brush in brushes)
				{
					if (InternalCSGModelManager.brushes.Contains(brush))
						continue;
					Debug.Log(string.Format("\tbrush \"{0}\" node-id: {1} brush-id: {2} \n", brush.name, brush.nodeID, brush.brushID));
				}

			}
		}
		#endregion
	}
#endif
}
