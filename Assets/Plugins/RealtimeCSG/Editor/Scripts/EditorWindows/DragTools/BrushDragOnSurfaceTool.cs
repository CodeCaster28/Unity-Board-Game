using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed class BrushDragOnSurfaceTool : SceneDragTool
	{
		SelectedBrushSurface		hoverBrushSurface			= null;
		Vector3						hoverPosition;
		Quaternion					hoverRotation;
		Transform					hoverParent;
		int							hoverSiblingIndex;
		List<GameObject>			dragGameObjects				= null;
		List<GameObject>			visualDragGameObject		= null;
		Vector3[]					projectedBounds				= null;
		bool						haveNoParent				= false;
		PrefabSourceAlignment		sourceSurfaceAlignment		= PrefabSourceAlignment.AlignedFront;
		PrefabDestinationAlignment	destinationSurfaceAlignment	= PrefabDestinationAlignment.AlignToSurface;


//		bool prevForceGrid = false;
		Vector3 prevForcedGridCenter = MathConstants.zeroVector3;
		Quaternion prevForcedGridRotation = MathConstants.identityQuaternion;

		#region ValidateDrop
		public override bool ValidateDrop(bool inSceneView)
		{
			Reset();
			if (DragAndDrop.objectReferences == null ||
				DragAndDrop.objectReferences.Length == 0)
			{
				dragGameObjects = null;
				return false;
			}

			dragGameObjects = new List<GameObject>();
			foreach (var obj in DragAndDrop.objectReferences)
			{
				var gameObject = obj as GameObject;
				if (gameObject == null)
					continue;

				if (gameObject.GetComponentInChildren<CSGBrush>() == null)
					continue;

				if (PrefabUtility.GetPrefabType(gameObject) == PrefabType.None)
					continue;

				if (PrefabUtility.GetPrefabParent(gameObject) == null && 
					PrefabUtility.GetPrefabObject(gameObject) != null)
					dragGameObjects.Add(gameObject);
			}
			if (dragGameObjects.Count != 1)
			{
				dragGameObjects = null;
				return false;
			}

			var dragGameObjectBounds = new AABB();
			dragGameObjectBounds.Reset();
			foreach (var gameObject in dragGameObjects)
			{
				var brushes = gameObject.GetComponentsInChildren<CSGBrush>();
				if (brushes.Length == 0)
					continue;
				dragGameObjectBounds.Add(BoundsUtilities.GetLocalBounds(brushes, gameObject.transform.worldToLocalMatrix));
			}

			if (!dragGameObjectBounds.Valid)
				dragGameObjectBounds.Add(MathConstants.zeroVector3);
						
			projectedBounds = new Vector3[8];
			BoundsUtilities.GetBoundsVertices(dragGameObjectBounds, projectedBounds);
			/*
			var upPlane = new Plane(MathConstants.upVector3, MathConstants.zeroVector3);
			for (int i = 7; i >= 0; i--)
			{
				projectedBounds[i] = upPlane.Project(projectedBounds[i]);
				for (int j = i+1; j < projectedBounds.Length; j++)
				{
					if (projectedBounds[i] == projectedBounds[j])
					{
						ArrayUtility.RemoveAt(ref projectedBounds, j);
						break;
					}
				}
			}*/

			haveNoParent = false;
			return true;
		}
		#endregion

		#region ValidateDropPoint
		public override bool ValidateDropPoint(bool inSceneView)
		{
			return true;
			/*
			GameObject foundObject;
			if (!InternalCSGModelManager.FindClickWorldIntersection(Event.current.mousePosition, out foundObject))
				return true;

			if (!foundObject.GetComponent<CSGBrush>())
				return false;

			return true;*/
		}
		#endregion

		#region Reset
		public override void Reset()
		{
			CleanUp();
			visualDragGameObject = null;
			hoverBrushSurface	= null;
			dragGameObjects		= null;

			hoverPosition = MathConstants.zeroVector3;
			hoverRotation = MathConstants.identityQuaternion;
			hoverParent = null;
			hoverSiblingIndex = int.MaxValue;
		}
		#endregion

		void CleanUp()
		{
			if (visualDragGameObject != null)
			{
				for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
				{
					if (!visualDragGameObject[i])
						continue;
					GameObject.DestroyImmediate(visualDragGameObject[i]);
				}
			}
			visualDragGameObject = null;
		}

		public SelectedBrushSurface[] HoverOnBrush(CSGBrush hoverBrush, int surfaceIndex)
		{
			if (hoverBrush == null)
				return null;
			
			return new SelectedBrushSurface[] 
			{
				new SelectedBrushSurface(hoverBrush, surfaceIndex)
			};
		}

		void DisableVisualObjects()
		{
			if (visualDragGameObject != null)
			{
				for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
				{
					var obj = visualDragGameObject[i];
					if (obj == null || !obj)
						continue;
					if (obj.activeSelf)
					{
						obj.SetActive(false);
					}
				}

				InternalCSGModelManager.CheckTransformChanged();
				InternalCSGModelManager.OnHierarchyModified();
				InternalCSGModelManager.UpdateRemoteMeshes();
			}
		}

		void EnableVisualObjects()
		{
			if (visualDragGameObject == null ||
				visualDragGameObject.Count != dragGameObjects.Count)
			{
				CreateVisualObjects();
			} else
			{
				for (int i = 0; i < dragGameObjects.Count; i++)
				{
					if (visualDragGameObject[i] == null || !visualDragGameObject[i])
						continue;
					visualDragGameObject[i].SetActive(dragGameObjects[i].activeSelf);
				}
			}

			int counter = 0;
			foreach (var obj in visualDragGameObject)
			{
				if (obj == null || !obj)
					continue;
				obj.transform.rotation = hoverRotation;
				obj.transform.position = hoverPosition;
				if (hoverParent != null && !SelectionUtility.IsPrefab(hoverParent.gameObject))
					obj.transform.SetParent(hoverParent, true);
				obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
				counter++;
			}

			InternalCSGModelManager.CheckTransformChanged();
			InternalCSGModelManager.OnHierarchyModified();
			InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
			MeshInstanceManager.UpdateHelperSurfaceVisibility();
		}
		
		void CreateVisualObjects()
		{
			CleanUp();
			
			//Debug.Log("CreateVisualObjects");
			//prevForceGrid = Grid.ForceGrid;
			prevForcedGridCenter = Grid.ForcedGridCenter;
			prevForcedGridRotation = Grid.ForcedGridRotation;

			sourceSurfaceAlignment = PrefabSourceAlignment.AlignedFront;
			destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

			
			visualDragGameObject = new List<GameObject>();

			foreach (var obj in dragGameObjects)
			{
				CSGNode node = obj.GetComponent<CSGNode>();
				if (node == null)
					continue;
				sourceSurfaceAlignment = node.PrefabSourceAlignment;
				destinationSurfaceAlignment = node.PrefabDestinationAlignment;

				GameObject copy;
				if (node && node.PrefabBehaviour == PrefabInstantiateBehaviour.Copy)
				{
					copy = GameObject.Instantiate<GameObject>(obj);
				} else
				{
					copy = PrefabUtility.InstantiatePrefab(obj) as GameObject;
				}
				if (copy == null)
					continue;

				copy.name = obj.name;
				visualDragGameObject.Add(copy);
			}
			
			var model	= SelectionUtility.LastUsedModel;
			if (model != null)
			{ 
				var parent	= model.transform;			
				int counter = 0;
				foreach (var obj in visualDragGameObject)
				{
					if (obj == null || !obj)
						continue;
					if (obj.activeSelf)
					{
						obj.transform.SetParent(parent, false);
						obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
						counter++;
					}
				}
			}
		}

		#region DragUpdated
		public override bool DragUpdated(Transform transformInInspector, Rect selectionRect)
		{
			InternalCSGModelManager.skipRefresh = true;
			try
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

				hoverBrushSurface	= null;
				hoverParent			= (transformInInspector == null) ? null : transformInInspector.parent;
				hoverSiblingIndex	= (transformInInspector == null) ? int.MaxValue : transformInInspector.transform.GetSiblingIndex();

				float middle = (selectionRect.yMax + selectionRect.yMin) * 0.5f;
				if (Event.current.mousePosition.y > middle)
					hoverSiblingIndex++;

				hoverRotation = MathConstants.identityQuaternion;
				hoverPosition = MathConstants.zeroVector3;
				haveNoParent = true;
				return true;
			}
			finally
			{
				if (!SceneViewEventHandler.IsActive())
					SceneViewEventHandler.ResetUpdateRoutine();
			}
		}

		public override bool DragUpdated()
		{
			InternalCSGModelManager.skipRefresh = true;
			try
			{
				DisableVisualObjects();
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				
				var intersection		= SceneQueryUtility.FindMeshIntersection(Event.current.mousePosition);
				var normal				= intersection.plane.normal;

				hoverPosition			= intersection.worldIntersection;
				hoverParent				= SelectionUtility.FindParentToAssignTo(intersection);
				hoverBrushSurface		= intersection.brush != null ? new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex) : null;
				hoverRotation			= SelectionUtility.FindDragOrientation(normal, sourceSurfaceAlignment, destinationSurfaceAlignment);
				haveNoParent			= (hoverParent == null);
				hoverSiblingIndex		= int.MaxValue;

				Grid.SetForcedGrid(intersection.plane);

				var toggleSnapping	= (Event.current.modifiers & EventModifiers.Control) == EventModifiers.Control;
				var doSnapping		= RealtimeCSG.CSGSettings.SnapToGrid ^ toggleSnapping;
				if (doSnapping)
				{
					var localPoints = new Vector3[8];
					var localPlane	= intersection.plane;
					for (var i = 0; i < localPoints.Length; i++)
						localPoints[i] = GeometryUtility.ProjectPointOnPlane(localPlane, (hoverRotation * projectedBounds[i]) + hoverPosition);

					hoverPosition += Grid.SnapDeltaToGrid(MathConstants.zeroVector3, localPoints);
				}
				hoverPosition	= GeometryUtility.ProjectPointOnPlane(intersection.plane, hoverPosition) + (normal * 0.01f);

				EnableVisualObjects();
				return true;
			}
			finally
			{
				if (!SceneViewEventHandler.IsActive())
					SceneViewEventHandler.ResetUpdateRoutine();
			}
		}
		#endregion
		
		#region DragPerform
		public override void DragPerform(bool inSceneView)
		{
			//Debug.Log("DragPerform");
			try
			{
				InternalCSGModelManager.skipRefresh = true;
				if (haveNoParent)
				{
					var model = SelectionUtility.LastUsedModel;
					if (model == null)
					{
						model = OperationsUtility.CreateModelInstanceInScene(selectModel: false);
						InternalCSGModelManager.EnsureInitialized(model);
						InternalCSGModelManager.CheckTransformChanged();
						InternalCSGModelManager.OnHierarchyModified();
					}
					var parent = model.transform;

					if (visualDragGameObject == null)
					{
						CreateVisualObjects();
					}

					int counter = 0;
					foreach (var obj in visualDragGameObject)
					{
						if (obj == null || !obj)
							continue;
						if (obj.activeSelf)
						{
							obj.transform.SetParent(parent, false);
							obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
							counter++;
						}
					}
				}

				if (visualDragGameObject != null)
				{
					var selection = new List<GameObject>();
					for (int i = visualDragGameObject.Count - 1; i >= 0; i--)
					{
						if (!visualDragGameObject[i])
							continue;
						if (visualDragGameObject[i].activeSelf)
						{
							Undo.RegisterCreatedObjectUndo(visualDragGameObject[i], "Instantiated prefab");
							selection.Add(visualDragGameObject[i]);
						} else
						{
							GameObject.DestroyImmediate(visualDragGameObject[i]);
						}
					}
					visualDragGameObject = null;

					if (selection.Count > 0)
					{
						UnityEditor.Selection.objects = selection.ToArray();
					}
				}

				for (int i = 0; i < SceneView.sceneViews.Count; i++)
				{
					var sceneview = SceneView.sceneViews[i] as SceneView;
					if (sceneview == null)
						continue;

					if (sceneview.camera.pixelRect.Contains(Event.current.mousePosition))
						sceneview.Focus();
				}
				visualDragGameObject = null;

				InternalCSGModelManager.Refresh(forceHierarchyUpdate: true);
				//MeshInstanceManager.UpdateHelperSurfaceVisibility(CSGSettings.HelperSurfaces);
			}
			finally
			{
				InternalCSGModelManager.skipRefresh = false;
				Grid.ForcedGridCenter	= prevForcedGridCenter;
				Grid.ForcedGridRotation = prevForcedGridRotation;
				Grid.ForceGrid			= false;
			}
		}
		#endregion

		#region DragExited
		public override void DragExited(bool inSceneView)
		{
			try
			{
				InternalCSGModelManager.skipRefresh = true;
				CleanUp();
				InternalCSGModelManager.Refresh(forceHierarchyUpdate: true);
				HandleUtility.Repaint();
			}
			finally
			{
				InternalCSGModelManager.skipRefresh = false;
				Grid.ForcedGridCenter	= prevForcedGridCenter;
				Grid.ForcedGridRotation = prevForcedGridRotation;
				Grid.ForceGrid			= false;
			}
		}
		#endregion

		#region Paint
		public override void OnPaint()
		{
			Grid.RenderGrid();
			if (hoverBrushSurface == null)
				return;
			
			var brush = hoverBrushSurface.brush;
			var brush_cache = InternalCSGModelManager.GetBrushCache(brush);
			if (brush_cache == null ||
				brush_cache.childData == null ||
				brush_cache.childData.ModelTransform == null)
				return;
				
			var highlight_surface	= hoverBrushSurface.surfaceIndex;
			var highlight_texGen	= brush.Shape.Surfaces[highlight_surface].TexGenIndex;
			var model_translation	= brush_cache.childData.Model.transform.position;
			var brush_translation	= brush_cache.compareTransformation.modelLocalPosition + model_translation;
			CSGRenderer.DrawSelectedBrush(brush.brushID, brush.Shape, 
											brush_translation, ColorSettings.WireframeOutline, 
											highlight_texGen, 
											false, ToolConstants.oldLineScale);
		}
		#endregion
	}
}
