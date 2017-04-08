using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal enum SelectionType
	{
		Replace,
		Additive,
		Subtractive,
		Toggle
	};

	internal static class SelectionUtility
	{
		internal static CSGModel lastUsedModelInstance = null;
		internal static CSGModel LastUsedModel
		{
			get
			{
				CSGModel returnModel = null;
				if (lastUsedModelInstance != null)
				{
					var flags = lastUsedModelInstance.gameObject.hideFlags;
					if ((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) == 0)
						returnModel = lastUsedModelInstance;
				}
				if (returnModel != null)
				{
					return returnModel;
				}

				foreach (var model in InternalCSGModelManager.Models)
				{
					var flags = model.gameObject.hideFlags;
					if ((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) == 0)
						return model;
				}
				return null;
			}
			set
			{
				if (value == null)
					return;

				var flags = value.gameObject.hideFlags;
				if ((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) != 0)
					return;

				lastUsedModelInstance = value;
			}
		}
		public static bool IsPrefab(GameObject gameObject)
		{
			return PrefabUtility.GetPrefabParent(gameObject) == null && PrefabUtility.GetPrefabObject(gameObject) != null && gameObject.transform.parent == null; // Is a prefab
		}

		public static Transform FindParentToAssignTo(BrushIntersection intersection)
		{
			if (intersection.brush == null || 
				SelectionUtility.IsPrefab(intersection.brush.gameObject))
			{
				var lastModel = SelectionUtility.LastUsedModel;
				if (lastModel == null || 
					SelectionUtility.IsPrefab(lastModel.gameObject))
					return null;

				return lastModel.transform;
			}

			var hoverParent	= intersection.brush.transform.parent;
			var iterator	= hoverParent;
			while (iterator != null)
			{
				var node = iterator.GetComponent<CSGNode>();
				if (node != null)
					hoverParent = node.transform;
				iterator = iterator.transform.parent;
			}
			return hoverParent;
		}

		public static Quaternion FindDragOrientation(Vector3 normal, PrefabSourceAlignment sourceSurfaceAlignment, PrefabDestinationAlignment destinationSurfaceAlignment)
		{
			Quaternion srcRotation;
			switch (sourceSurfaceAlignment)
			{
				default:
				case PrefabSourceAlignment.AlignedFront:	srcRotation = Quaternion.LookRotation(Vector3.forward); break;
				case PrefabSourceAlignment.AlignedBack:		srcRotation = Quaternion.LookRotation(Vector3.back); break;
				case PrefabSourceAlignment.AlignedLeft:		srcRotation = Quaternion.LookRotation(Vector3.right); break;
				case PrefabSourceAlignment.AlignedRight:	srcRotation = Quaternion.LookRotation(Vector3.left); break;
				case PrefabSourceAlignment.AlignedTop:		srcRotation = Quaternion.LookRotation(Vector3.up, Vector3.forward); break;
				case PrefabSourceAlignment.AlignedBottom:	srcRotation = Quaternion.LookRotation(Vector3.down, Vector3.back); break;
			}

			switch (destinationSurfaceAlignment)
			{
				default:
				case PrefabDestinationAlignment.AlignToSurface:
				{
					var tangent = Vector3.up; // assume up is up in the world
					var absX = Mathf.Abs(normal.x);
					var absY = Mathf.Abs(normal.y);
					var absZ = Mathf.Abs(normal.z);

					// if our surface is a floor / ceiling then assume up is the axis 
					// aligned vector that is most aligned with the camera's up vector
					if (absX <= absY && absX <= absZ && absY > absZ)
					{
						tangent = GeometryUtility.SnapToClosestAxis(Camera.current.transform.up);
					}

					return Quaternion.LookRotation(normal, tangent) * srcRotation;
				}
				case PrefabDestinationAlignment.AlwaysFaceUp:
				{
					return Quaternion.LookRotation(Vector3.up) * srcRotation;
				}
				case PrefabDestinationAlignment.None:
				{
					return Quaternion.identity;
				}
			}
		}

		static bool				shiftPressed		= false;
		static bool				actionKeyPressed	= false;
		static EventModifiers	currentModifiers	= EventModifiers.None;
		public static EventModifiers CurrentModifiers { get { return currentModifiers; } internal set { currentModifiers = value; } } 

		public static void HandleEvents()
		{
			switch (Event.current.type)
			{
				case EventType.MouseDown:
				case EventType.MouseUp:
				case EventType.MouseDrag:
				case EventType.MouseMove:
				case EventType.KeyDown:
				case EventType.KeyUp:
				case EventType.ValidateCommand:
				case EventType.ExecuteCommand:
				case EventType.Repaint:
				{
					shiftPressed		= Event.current.shift;
					actionKeyPressed	= EditorGUI.actionKey;
					currentModifiers	= Event.current.modifiers & (EventModifiers.Alt | EventModifiers.Control | EventModifiers.Shift | EventModifiers.Command);
					break;
				}
			}
		}

		public static void HideObjectsRemoteOnly(List<GameObject> gameObjects)
		{
			if (gameObjects == null || 
				gameObjects.Count == 0)
				return;
			
			for (var i = gameObjects.Count - 1; i >= 0; i--)
			{
				if (gameObjects[i])
					gameObjects[i].SetActive(false);
			}

			InternalCSGModelManager.CheckTransformChanged();
			InternalCSGModelManager.OnHierarchyModified();
			InternalCSGModelManager.UpdateRemoteMeshes();
		}

		public static void ShowObjectsAndUpdate(List<GameObject> gameObjects)
		{
			if (gameObjects == null || 
				gameObjects.Count == 0)
				return;

			for (var i = gameObjects.Count - 1; i >= 0; i--)
			{
				if (gameObjects[i])
					gameObjects[i].SetActive(true);
			}

			InternalCSGModelManager.CheckTransformChanged();
			InternalCSGModelManager.OnHierarchyModified();
			InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
			MeshInstanceManager.UpdateHelperSurfaceVisibility();
		}


		public static bool IsSnappingToggled
		{
			get 
			{
				return actionKeyPressed;
			}
		}

		public static SelectionType GetEventSelectionType()
		{
			if (shiftPressed && actionKeyPressed)
			{
				return SelectionType.Subtractive;
			} else
			if (actionKeyPressed)
			{
				return SelectionType.Toggle;
			} else
			if (shiftPressed)
			{
				return SelectionType.Additive;
			}
			return SelectionType.Replace;
		}

		public static void DeselectAll()
		{
			if (CSGBrushEditorManager.DeselectAll())
				return;
			DeselectAllBrushes();
		}

		public static void DeselectAllBrushes()
		{
			Selection.activeObject = null;
		}

		public static void ToggleSelectedObjectVisibility()
		{
			Undo.IncrementCurrentGroup();
			int undo_group_index = Undo.GetCurrentGroup();

			var selected = Selection.gameObjects.ToArray();
			Undo.RecordObjects(selected, "Toggle Object Visibility");
			bool haveVisibleSelection = false;
			for (int i = 0; i < selected.Length; i++)
			{
				haveVisibleSelection = selected[i].activeInHierarchy || haveVisibleSelection;
			}
			if (haveVisibleSelection)
			{
				for (int i = 0; i < selected.Length; i++)
					selected[i].SetActive(false);
			} else
			{
				for (int i = 0; i < selected.Length; i++)
					selected[i].SetActive(true);
			}

			Undo.CollapseUndoOperations(undo_group_index);
		}

		public static void HideSelectedObjects()
		{
			Undo.IncrementCurrentGroup();
			int undo_group_index = Undo.GetCurrentGroup();

			var selected = Selection.gameObjects.ToArray();
			Undo.RecordObjects(selected, "Hiding Objects");
			for (int i = 0; i < selected.Length; i++)
				selected[i].SetActive(false);

			Undo.CollapseUndoOperations(undo_group_index);
		}

		public static void UnHideAll()
		{
			Undo.IncrementCurrentGroup();
			int undo_group_index = Undo.GetCurrentGroup();

			for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var activeScene = SceneManager.GetSceneAt(sceneIndex);
				var rootGameObjects = activeScene.GetRootGameObjects();
				for (int i = 0; i < rootGameObjects.Length; i++)
				{
					var children = rootGameObjects[i].GetComponentsInChildren<Transform>(true);
					for (int c = 0; c < children.Length; c++)
					{
						var transform = children[c];
						var gameObject = transform.gameObject;
						if (gameObject.activeInHierarchy || (gameObject.hideFlags != HideFlags.None))
							continue;

						Undo.RecordObject(gameObject, "Un-hiding Object");
						gameObject.SetActive(true);
					}
				}
			}
			Undo.CollapseUndoOperations(undo_group_index);
		}

		public static void HideUnselectedObjects()
		{
			Undo.IncrementCurrentGroup();
			var undoGroupIndex  = Undo.GetCurrentGroup();

			var selected		= Selection.gameObjects.ToList();
			var selectedIDs		= new HashSet<int>();

			var models = InternalCSGModelManager.Models;
			for (var i = 0; i < models.Length; i++)
			{
				var model = models[i];
				if (!model)
					continue;

				var modelCache = InternalCSGModelManager.GetModelCache(model);
				if (modelCache == null ||
					!modelCache.GeneratedMeshes)
					continue;

				var meshContainerChildren = modelCache.GeneratedMeshes.GetComponentsInChildren<Transform>();
			//for (int s = 0; s < MeshInstanceManager.SceneStates.Count; s++)
			//{
				//var sceneState		= MeshInstanceManager.SceneStates[s];
				//if (!sceneState.ParentMeshContainer)
				//	continue;
				//var meshContainerChildren = sceneState.ParentMeshContainer.GetComponentsInChildren<Transform>();
				foreach (var child in meshContainerChildren)
					selected.Add(child.gameObject);
			}

			for (int i = 0; i < selected.Count; i++) // we keep adding parents, and their parents until we hit the root-objects
			{
				selectedIDs.Add(selected[i].GetInstanceID());
				var transform = selected[i].transform;
				var parent    = transform.parent;
				if (parent == null)
					continue;
				selected.Add(parent.gameObject);
			}

			for (var sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
			{
				var activeScene = SceneManager.GetSceneAt(sceneIndex);
				var rootGameObjects = activeScene.GetRootGameObjects();
				for (var i = 0; i < rootGameObjects.Length; i++)
				{
					var children = rootGameObjects[i].GetComponentsInChildren<Transform>();
					for (var c = 0; c < children.Length; c++)
					{
						var transform = children[c];
						var gameObject = transform.gameObject;
						if (!gameObject.activeInHierarchy || (gameObject.hideFlags != HideFlags.None))
							continue;

						if (selectedIDs.Contains(gameObject.GetInstanceID()))
							continue;

						Undo.RecordObject(gameObject, "Hiding Object");
						gameObject.SetActive(false);
					}
				}
			}

			Undo.CollapseUndoOperations(undoGroupIndex);
		}


		#region DoSelectionClick
		public static void DoSelectionClick()
		{
			GameObject gameobject;
			SceneQueryUtility.FindClickWorldIntersection(Event.current.mousePosition, out gameobject);

			gameobject = SceneQueryUtility.GetGroupGameObjectIfObjectIsPartOfGroup(gameobject);

			var selectedObjectsOnClick = new List<int>(Selection.instanceIDs);
			bool addedSelection = false;
			if (EditorGUI.actionKey)
			{
				if (gameobject != null)
				{
					var instanceID = gameobject.GetInstanceID();
					if (selectedObjectsOnClick.Contains(instanceID))
					{
						selectedObjectsOnClick.Remove(instanceID);
					}
					else
					{
						selectedObjectsOnClick.Add(instanceID);
						addedSelection = true;
					}

					if (selectedObjectsOnClick.Count == 0)
						Selection.activeTransform = null;
					else
						Selection.instanceIDs = selectedObjectsOnClick.ToArray();
				}
			}
			else
			if (Event.current.shift)
			{
				if (gameobject != null)
				{
					var instanceID = gameobject.GetInstanceID();
					selectedObjectsOnClick.Add(instanceID);
					Selection.instanceIDs = selectedObjectsOnClick.ToArray();
					addedSelection = true;
				}
			}
			else
			if (Event.current.alt)
			{
				if (gameobject != null)
				{
					var instanceID = gameobject.GetInstanceID();
					selectedObjectsOnClick.Remove(instanceID);
					Selection.instanceIDs = selectedObjectsOnClick.ToArray();
					return;
				}
			}
			else
			{
				Selection.activeGameObject = gameobject;
				addedSelection = true;
			}

			if (!addedSelection)
			{
				foreach (var item in Selection.GetFiltered(typeof(CSGBrush), SelectionMode.Deep))
				{
					var brush = item as CSGBrush;
					var brush_cache = InternalCSGModelManager.GetBrushCache(brush);
					if (brush_cache == null ||
						brush_cache.childData == null ||
						!brush_cache.childData.Model)
						continue;
					SelectionUtility.LastUsedModel = brush_cache.childData.Model;
					break;
				}
			}
			else
			if (gameobject != null)
			{
				var brush = gameobject.GetComponent<CSGBrush>();
				if (brush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(brush);
					if (brush_cache != null &&
						brush_cache.childData != null &&
						brush_cache.childData.Model)
					{
						SelectionUtility.LastUsedModel = brush_cache.childData.Model;
					}
				}
			}
		}
		#endregion


		#region DoesSelectionContainCSGNodes
		public static bool DoesSelectionContainCSGNodes()
		{
			var gameObjects = Selection.gameObjects;
			if (gameObjects == null)
				return false;

			foreach (var gameObject in gameObjects)
			{
				if (gameObject.GetComponentInChildren<CSGNode>())
					return true;
			}
			return false;
		}
		#endregion

	}
}
