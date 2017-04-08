using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
#if !DEMO
	[CustomEditor(typeof(CSGModelExported))]
	[CanEditMultipleObjects]
	[System.Reflection.Obfuscation(Exclude = true)]
	internal sealed class ExportedModelEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			GUILayout.BeginVertical(GUI.skin.box);
			{
				if (GUILayout.Button("Revert back to CSG model"))
				{
					var selection = new List<UnityEngine.Object>();
					var updateScenes = new HashSet<Scene>();
					foreach (var target in targets)
					{
						var exportedModel = target as CSGModelExported;
						if (!exportedModel)
							continue;

						exportedModel.hideFlags = HideFlags.DontSaveInBuild;
						updateScenes.Add(exportedModel.gameObject.scene);
						if (exportedModel.containedModel)
						{
							selection.Add(exportedModel.containedModel.gameObject);
							exportedModel.containedModel.transform.SetParent(exportedModel.transform.parent, true);
							exportedModel.containedModel.transform.SetSiblingIndex(exportedModel.transform.GetSiblingIndex());
							exportedModel.containedModel.gameObject.SetActive(true);
							exportedModel.containedModel.gameObject.hideFlags = HideFlags.None;
							EditorUtility.SetDirty(exportedModel.containedModel);
							GameObject.DestroyImmediate(exportedModel.gameObject);
						} else
						{
							MeshInstanceManager.ReverseExport(exportedModel);
							selection.Add(exportedModel.gameObject);
							EditorUtility.SetDirty(exportedModel);
							GameObject.DestroyImmediate(exportedModel);
						}
					}
					Selection.objects = selection.ToArray();
					InternalCSGModelManager.skipRefresh = true;
					try
					{
						BrushOutlineManager.ClearOutlines();
						//CSGModelManager.Refresh(forceHierarchyUpdate: true);
						InternalCSGModelManager.Rebuild();
						foreach(var scene in updateScenes)
							UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
					}
					finally
					{
						InternalCSGModelManager.skipRefresh = false;
					}
				}
			}
			GUILayout.EndVertical();
		}
	}
#endif
}