using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;

namespace RealtimeCSG
{
	[System.Reflection.Obfuscation(Exclude = true)]
	internal sealed class CSGBrushEditorManager : ScriptableObject
	{
		static CSGBrushEditorManager instance = null;
		
//		[NonSerialized ] bool				isEnabled				= false;
		[NonSerialized ] bool				generateMode			= false;		
		[NonSerialized ] FilteredSelection	filteredSelection = new FilteredSelection();
		
		[SerializeField] ToolEditMode		editMode				= ToolEditMode.Object;			
//		[SerializeField] ToolEditMode		prevUsedEditMode		= ToolEditMode.Object;
		[SerializeField] IBrushTool			activeTool				= null;

		static IBrushTool[]					brushTools              = null;

		
		public static FilteredSelection	FilteredSelection	{ get { InitTools(); return instance.filteredSelection; } }
		public static IBrushTool		ActiveTool			{ get { InitTools(); return instance.activeTool; } }
			
		
		public static ToolEditMode EditMode
		{
			get
			{
				InitTools();
				return instance.editMode;
			}
			set
			{
				if (instance.editMode == value)
					return;

				Undo.RecordObject(instance, "Changed edit mode");

				instance.editMode = value;
				instance.generateMode = false; 
				
				RealtimeCSG.CSGSettings.EditMode = instance.editMode;
				RealtimeCSG.CSGSettings.Save();

				if (ActiveTool != null)
					SceneView.RepaintAll();
			}
		}

		static public IBrushTool CurrentTool
		{
			get 
			{
				InitTools();

				if (instance.generateMode)
					return brushTools[(int)ToolEditMode.Generate] as GenerateBrushTool;
				
				var editMode = instance.editMode;
				if (editMode < firstEditMode ||
					editMode > lastEditModes)
					return brushTools[0];

				return brushTools[(int)editMode];
			}
		}

		
		static ToolEditMode firstEditMode;
		static ToolEditMode lastEditModes;

		const HideFlags scriptableObjectHideflags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.NotEditable | HideFlags.HideInHierarchy;

		static void InitTools()
		{
			if (instance)
				return;


			var values = Enum.GetValues(typeof(ToolEditMode)).Cast<ToolEditMode>().ToList();
			values.Sort();
			firstEditMode = values[0];
			lastEditModes = values[values.Count - 1];
			
			Undo.undoRedoPerformed -= UndoRedoPerformed;
			Undo.undoRedoPerformed += UndoRedoPerformed;

			EditorApplication.modifierKeysChanged -= OnModifierKeysChanged;
			EditorApplication.modifierKeysChanged += OnModifierKeysChanged;


			var managers = FindObjectsOfType<CSGBrushEditorManager>().ToArray();
			for (int i = 0; i < managers.Length; i++)
				DestroyImmediate(managers[i]);
			instance = ScriptableObject.CreateInstance<CSGBrushEditorManager>();
			instance.hideFlags = scriptableObjectHideflags;

			var types = new Type[]
			{
				typeof(ObjectEditBrushTool),
				typeof(GenerateBrushTool),
				typeof(MeshEditBrushTool),
				typeof(ClipBrushTool),
				typeof(SurfaceEditBrushTool)
			};
			if (types.Length != values.Count)
			{
				Debug.Log("types.Length != values.Count");
			}

			brushTools = new IBrushTool[values.Count];
			for (int j = 0; j < types.Length; j++)
			{
				var objects = FindObjectsOfType(types[j]).ToArray();
				for (int i = 0; i < objects.Length; i++)
					DestroyImmediate(objects[i]);

				var obj = ScriptableObject.CreateInstance(types[j]);
				brushTools[j] = obj as IBrushTool;
				if (brushTools[j] == null)
				{
					Debug.Log("brushTools[j] == null");
					continue;
				}
				if (!(brushTools[j] is ScriptableObject))
				{
					Debug.Log("!(brushTools[j] is ScriptableObject)");
					continue;
				}
				obj.hideFlags = scriptableObjectHideflags;
			}

			GenerateBrushTool.ShapeCommitted -= OnShapeCommittedEvent;
			GenerateBrushTool.ShapeCommitted += OnShapeCommittedEvent;
			GenerateBrushTool.ShapeCancelled -= OnShapeCancelledEvent;
			GenerateBrushTool.ShapeCancelled += OnShapeCancelledEvent;
			
			RealtimeCSG.CSGSettings.Reload();
			instance.editMode = RealtimeCSG.CSGSettings.EditMode;
			InitTargets();
		}
				
		static HashSet<CSGNode>		selectedNodes = new HashSet<CSGNode>();
		static HashSet<Transform>	selectedOthers = new HashSet<Transform>();
		public static void UpdateSelection(bool forceUpdate = false)
		{
			InitTools();
			
			GetTargetSelection(ref selectedNodes, ref selectedOthers);
				
			//filteredSelection.Validate();
			if (!instance.filteredSelection.UpdateSelection(selectedNodes, selectedOthers) &&
				!forceUpdate)
			{
				return;
			}
			
			InternalCSGModelManager.skipRefresh = true;
			try
			{
				InternalCSGModelManager.Refresh(); 
			}
			finally
			{
				InternalCSGModelManager.skipRefresh = false;
			}

			//if (nodes.Count > 0)
			{/*
				if (!instance.isEnabled)
				{
					InitTargets();
					instance.isEnabled = true;
				}*/
			
				foreach (var tool in brushTools)
					tool.SetTargets(instance.filteredSelection);
			} /*else
			if (isEnabled)
			{
				CleanupTargets();
				activeTool		= null;
				isEnabled		= false;
			}*/
		}
		
		public static void ResetMessage()
		{
			if (SceneView.lastActiveSceneView     != null) { SceneView.lastActiveSceneView.RemoveNotification(); return; }
			if (SceneView.currentDrawingSceneView != null) { SceneView.lastActiveSceneView.RemoveNotification(); return; }
		}

		public static void ShowMessage(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			if (SceneView.lastActiveSceneView     != null) { SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message)); return; }
			if (SceneView.currentDrawingSceneView != null) { SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message)); return; }
			Debug.LogWarning(message);
		}
				
		static void OnEnableTool(IBrushTool tool)
		{
			if (tool != null)
				tool.OnEnableTool();
		}

		static void OnDisableTool(IBrushTool tool)
		{ 
			if (tool != null)
				tool.OnDisableTool();
		}

		public static bool DeselectAll()
		{
			if (instance.activeTool != null &&
				instance.activeTool.DeselectAll())
				return true;
			return false;
		}
		
		static void UndoRedoPerformed()
		{
			if (instance.activeTool != null)
			{
				instance.activeTool.UndoRedoPerformed();
			}
			CSGBrushEditorManager.UpdateSelection(forceUpdate: true);
		}

		static void OnModifierKeysChanged()
		{
			SceneView.RepaintAll();
		}
		
		static bool GetTargetSelection(ref HashSet<CSGNode> nodes, ref HashSet<Transform> others)
		{
			selectedNodes.Clear();
			selectedOthers.Clear();

			if (Selection.gameObjects == null)
				return false;
			
			foreach (var gameObject in Selection.gameObjects)
			{
				if (!gameObject)
					continue;
				var node = gameObject.GetComponent<CSGNode>();
				if (node && node.enabled && (node.hideFlags & HideFlags.HideInInspector) == HideFlags.None)
					nodes.Add(node);
				else
					others.Add(gameObject.transform);
			}
			return true;
		}

		static void InitTargets()
		{
			var newTool = CurrentTool;
			if (newTool == instance.activeTool)
			{
				if (instance.filteredSelection.NodeTargets != null && 
					instance.filteredSelection.NodeTargets.Length > 0)
					OnEnableTool(instance.activeTool);
				else
					OnDisableTool(instance.activeTool);
			} else
			{
				UpdateTool();
			}
		}

		static void CleanupTargets()
		{
			InitTools();

			var newTool = CurrentTool;
			if (newTool == instance.activeTool)
			{
				if (instance.activeTool != null)
				{
					if (instance.filteredSelection.NodeTargets != null && 
						instance.filteredSelection.NodeTargets.Length > 0)
						OnDisableTool(instance.activeTool);
				}
			} else
			{
				UpdateTool();
			}

			if (newTool == null)
			{
				Tools.hidden = false;
			}
		}

		static void NextEditMode()
		{
			InitTools();
			if (instance.editMode == lastEditModes)
			{
				EditMode = firstEditMode;
			} else
			{
				EditMode = (ToolEditMode)(instance.editMode + 1);
			}
		}

		static void PrevEditMode()
		{
			InitTools();
			if (instance.editMode == firstEditMode)
			{
				EditMode = lastEditModes;
			} else
			{
				EditMode = (ToolEditMode)(instance.editMode - 1);
			}
		}

		static void UpdateTool()
		{
			if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
				return;

			var newTool = CurrentTool;
			if (instance.activeTool != newTool)
			{
				if (instance.activeTool != null)
					OnDisableTool(instance.activeTool);

				instance.activeTool		= newTool;

				if (newTool != null)
					OnEnableTool(newTool);
			}
		}
		

		static void FinishShapeBuilder()
		{	   
			Tools.hidden = false;
			//filteredSelection.Validate();
			//SceneView.RepaintAll();
		}
		
		public static Vector3 SnapPointToGrid(Vector3 point, CSGPlane plane, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush, bool ignoreBrushes = false)
		{
			snappedOnBrush = null;
			var toggleSnapping	= SelectionUtility.IsSnappingToggled;
			var doSnapping		= RealtimeCSG.CSGSettings.SnapToGrid ^ toggleSnapping;
			
			var snappedPoint = point;
			if (doSnapping)
			{
				snappedPoint = snappedPoint + Grid.ForceSnapDeltaToGrid(MathConstants.zeroVector3, snappedPoint);
				snappedPoint = Grid.PointFromGridSpace(Grid.CubeProject(Grid.PlaneToGridSpace(plane), Grid.PointToGridSpace(snappedPoint)));
			
				// snap twice to get rid of some tiny movements caused by the projection in depth	
				snappedPoint = snappedPoint + Grid.ForceSnapDeltaToGrid(MathConstants.zeroVector3, snappedPoint);
				snappedPoint = Grid.PointFromGridSpace(Grid.CubeProject(Grid.PlaneToGridSpace(plane), Grid.PointToGridSpace(snappedPoint)));
			} else
				snappedPoint = GeometryUtility.ProjectPointOnPlane(plane, snappedPoint);

			//GeometryUtility.ProjectPointOnPlane(plane, snappedPoint);
			if (doSnapping && !ignoreBrushes)
			{
				return GridUtility.SnapToWorld(plane, point, snappedPoint, ref snappingEdges, out snappedOnBrush);
			}
			return snappedPoint;
		}

		public static Vector3 SnapPointToRay(Vector3 point, Ray ray, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush)
		{
			snappedOnBrush = null;
			var toggleSnapping	= SelectionUtility.IsSnappingToggled;
			var doSnapping		= RealtimeCSG.CSGSettings.SnapToGrid ^ toggleSnapping;
			
			var snappedPoint = point;
			
			snappingEdges = null;
			if (doSnapping)
			{
				snappedPoint = snappedPoint + Grid.ForceSnapDeltaToRay(ray, MathConstants.zeroVector3, snappedPoint);
			}
			return snappedPoint;

		}


		static void OnShapeCancelledEvent()
		{
			if (!instance.generateMode)
				return;
			instance.generateMode = false;
			//EditMode = instance.prevUsedEditMode;
		}

		static void OnShapeCommittedEvent()
		{
			instance.generateMode = false;
			//EditMode = instance.prevUsedEditMode;
		}
		
		static bool HandleBuilderEvents()
        {
			if (RealtimeCSG.CSGSettings.EditMode != instance.editMode)
			{
				RealtimeCSG.CSGSettings.EditMode = instance.editMode;
				RealtimeCSG.CSGSettings.Save();
			}

			//if (GUIUtility.hotControl != 0)
			//	return false;
			
			switch (Event.current.type) 
			{
				case EventType.KeyDown:
				{
					if (GUIUtility.hotControl == 0 &&
						Keys.FreeBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as GenerateBrushTool;
						generateBrushTool.BuilderMode = ShapeMode.FreeDraw;
//						instance.prevUsedEditMode = EditMode;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					if (Keys.CylinderBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as GenerateBrushTool;
						generateBrushTool.BuilderMode = ShapeMode.Cylinder;
//						instance.prevUsedEditMode = EditMode;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					if (Keys.BoxBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as GenerateBrushTool;
						generateBrushTool.BuilderMode = ShapeMode.Box;
//						instance.prevUsedEditMode = EditMode;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					if (Keys.SphereBuilderMode.IsKeyPressed() && !instance.generateMode)
					{
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as GenerateBrushTool;
						generateBrushTool.BuilderMode = ShapeMode.Sphere;
//						instance.prevUsedEditMode = EditMode;
						instance.generateMode = true;
						Event.current.Use();
						return true;
					}
					else if (Keys.SwitchToObjectEditMode	.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToGenerateEditMode	.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToMeshEditMode		.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToClipEditMode		.IsKeyPressed()) { Event.current.Use(); return true; }
					else if (Keys.SwitchToSurfaceEditMode	.IsKeyPressed()) { Event.current.Use(); return true; }
					break;
				}
				case EventType.KeyUp:
				{
					if (instance.generateMode &&
						(Keys.FreeBuilderMode.IsKeyPressed() ||
						Keys.CylinderBuilderMode.IsKeyPressed() ||
						Keys.BoxBuilderMode.IsKeyPressed() ||
                        Keys.SphereBuilderMode.IsKeyPressed()))
					{
						Event.current.Use();
						var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as GenerateBrushTool;
						if (!generateBrushTool.HotKeyReleased())
						{
							instance.generateMode = false;
						}
					}
					else if (Keys.SwitchToObjectEditMode	.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Object; Event.current.Use(); return true; }
					else if (Keys.SwitchToGenerateEditMode	.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Generate; Event.current.Use(); return true; }
					else if (Keys.SwitchToMeshEditMode		.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Mesh; Event.current.Use(); return true; }
					else if (Keys.SwitchToClipEditMode		.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Clip; Event.current.Use(); return true; }
					else if (Keys.SwitchToSurfaceEditMode	.IsKeyPressed()) { InitTools(); EditMode = ToolEditMode.Surfaces; Event.current.Use(); return true; }
					break;
				}
				case EventType.ValidateCommand:
				{
					if ((GUIUtility.hotControl == 0 && Keys.FreeBuilderMode  .IsKeyPressed()) ||
						Keys.CylinderBuilderMode.IsKeyPressed() ||
                        Keys.SphereBuilderMode.IsKeyPressed() ||
                        Keys.BoxBuilderMode.IsKeyPressed() ||
						Keys.SwitchToObjectEditMode.IsKeyPressed() ||
						Keys.SwitchToGenerateEditMode.IsKeyPressed() ||
						Keys.SwitchToMeshEditMode.IsKeyPressed() ||
						Keys.SwitchToClipEditMode.IsKeyPressed() ||
						Keys.SwitchToSurfaceEditMode.IsKeyPressed())
					{
						Event.current.Use();
						return true;
					}
					break;
				}
			}
            return false;
        }
		/*
		static bool HandleYMode()
        {
			switch (Event.current.type) 
			{
				case EventType.KeyDown:
				{
					if (Keys.VerticalMoveMode.IsKeyPressed())
					{
						Grid.YMoveModeActive = true; Event.current.Use();
						return true; 
					}
					break;
				}
				case EventType.KeyUp:
				{
					if (Keys.VerticalMoveMode.IsKeyPressed())
					{
						Grid.YMoveModeActive = false; Event.current.Use();
						return true; 
					}
					break;
				}
				case EventType.ValidateCommand:
				{
					if (Keys.VerticalMoveMode.IsKeyPressed())
					{
						Event.current.Use();
						return true;
					}
					break;
				}
			}
            return false;
		}
		*/
		[NonSerialized] LineMeshManager zTestLineMeshManager	= new LineMeshManager();
		[NonSerialized] LineMeshManager noZTestLineMeshManager	= new LineMeshManager();

		void OnDestroy()
		{
			zTestLineMeshManager.Destroy();
			noZTestLineMeshManager.Destroy();
		} 

		private static readonly int SceneWindowHash		= "SceneWindowHash".GetHashCode();
		//		private static readonly int BottomBarInputHash	= "BottomBarInputHash".GetHashCode();
		//private static bool holdingDownMouse = false;

		static List<CSGBrushEditorWindow>  currentEditorWindows = new List<CSGBrushEditorWindow>();

		public static bool InitSceneGUI(SceneView sceneView)
		{
			currentEditorWindows	= CSGBrushEditorWindow.GetEditorWindows();

			return (currentEditorWindows.Count == 0);
		}
		

		//void OnSceneGUI() <- paints -everything- again for every selected brush
		public static void OnSceneGUI(SceneView sceneView)
		{
			/*
			int prevHotControl		= GUIUtility.hotControl;
			int prevKeyboardControl = GUIUtility.keyboardControl;

			// make it impossible for the tool to click in the bottom bar area
			int bottomBarId	 = GUIUtility.GetControlID (BottomBarInputHash, FocusType.Passive);
			bool forceRevert = false;
			//if (GUIUtility.hotControl == 0)
			{
				var bottomBarRect = sceneView.position;
				var min = bottomBarRect.min;
				var max = bottomBarRect.max;

				min.y = max.y - (GUIStyleUtility.BottomToolBarHeight + 18);

				bottomBarRect.min = min;
				bottomBarRect.max = max;

				if (bottomBarRect.Contains(Event.current.mousePosition))
				{
					GUIUtility.hotControl	   = bottomBarId;
					GUIUtility.keyboardControl = bottomBarId;
					forceRevert = true;
				}
			}
			*/
			SelectionUtility.HandleEvents();
			InitTools();

			HandleBuilderEvents();
			//HandleYMode();
			{
				UpdateTool();
				
				if (instance.activeTool != null)
				{
					if (RealtimeCSG.CSGSettings.EnableRealtimeCSG)
					{
						// handle the tool
						var sceneSize = sceneView.position.size;
						var sceneRect = new Rect(0, 0, sceneSize.x, sceneSize.y - ((GUIStyleUtility.BottomToolBarHeight + 4) + 17));
						//var originalEventType = Event.current.type;
						//if (originalEventType == EventType.MouseMove ||
						//	originalEventType == EventType.MouseUp)
						//	holdingDownMouse = false;

						//var mousePos = Event.current.mousePosition;
						//if (originalEventType == EventType.Layout ||
						//	originalEventType == EventType.Repaint || 
						//	sceneRect.Contains(mousePos) || 
						//	holdingDownMouse)
							instance.activeTool.HandleEvents(sceneRect);

						//if (originalEventType == EventType.MouseDown ||
						//	originalEventType == EventType.MouseDrag)
						//	holdingDownMouse = true;
					} else
					{
						if (Event.current.type == EventType.repaint)
						{
							var brushes = instance.filteredSelection.BrushTargets;
							var wireframes = new List<GeometryWireframe>(brushes.Length);
							var translations = new List<Vector3>(brushes.Length);
							for (int i = 0; i < brushes.Length; i++)
							{
								var brush = brushes[i];
								if (!brush)
									continue;

								var brushCache = InternalCSGModelManager.GetBrushCache(brush);
								if (brushCache == null ||
									brushCache.childData == null ||
									!brushCache.childData.Model)
									continue;

								var brushTranslation = brushCache.compareTransformation.modelLocalPosition + brushCache.childData.ModelTransform.position;

								wireframes.Add(BrushOutlineManager.GetBrushOutline(brushes[i].brushID));
								translations.Add(brushTranslation);
							}
							if (wireframes.Count > 0)
							{
								CSGRenderer.DrawSelectedBrushes(instance.zTestLineMeshManager, instance.noZTestLineMeshManager,
									wireframes.ToArray(), translations.ToArray(),
									ColorSettings.SelectedOutlines, ToolConstants.thickLineScale);
							}
							MaterialUtility.LineDashMultiplier = 1.0f;
							MaterialUtility.LineThicknessMultiplier = 1.0f;
							instance.zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
							instance.zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
						}
					}
				}
			}
			/*
			// reset the control so the bottom bar can take over
			if (forceRevert)
			{
				GUIUtility.hotControl		= prevHotControl;
				GUIUtility.keyboardControl	= prevKeyboardControl;
			}
			*/

			int sceneWindowId	= GUIUtility.GetControlID (SceneWindowHash, FocusType.Passive);			
			var sceneWindowType = Event.current.GetTypeForControl(sceneWindowId);
			if (sceneWindowType == EventType.repaint)
			{
				if (currentEditorWindows.Count > 0)
				{
					for (int i = 0; i < currentEditorWindows.Count; i++)
						currentEditorWindows[i].Repaint();
					return;
				}
			}

			if (sceneWindowType == EventType.MouseMove)
			{
				SceneTools.IsDraggingObjectInScene = false;
			}

			if (RealtimeCSG.CSGSettings.EnableRealtimeCSG)
			{
				if (sceneView != null && sceneWindowType != EventType.used && !SceneTools.IsDraggingObjectInScene)
				{
					if (currentEditorWindows.Count == 0)
					{
						try
						{
							Handles.BeginGUI();
							Rect windowRect = new Rect(Vector2.zero, sceneView.position.size); 
							CSGBrushEditorGUI.HandleWindowGUI(windowRect);
						}
						finally
						{
							Handles.EndGUI();
						}
					}
				}
			}
		}


		public static void GenerateFromSurface(CSGBrush cSGBrush, CSGPlane polygonPlane, Vector3 direction, Vector3[] points, int[] pointIndices, bool drag)
		{
			CSGBrushEditorManager.EditMode = ToolEditMode.Generate;
			UpdateTool();
			var generateBrushTool = brushTools[(int)ToolEditMode.Generate] as GenerateBrushTool;
			generateBrushTool.GenerateFromPolygon(cSGBrush, polygonPlane, direction, points, pointIndices, drag);
		}

		public delegate void SetTransformation(Transform newTransform, Transform originalTransform);

				
		public static Transform[] CloneTargets(SetTransformation setTransform = null)
		{
			if (instance.filteredSelection.NodeTargets.Length == 0)
				return new Transform[0];

			var groupId = Undo.GetCurrentGroup();
			Undo.IncrementCurrentGroup();

			var newTargets	= new GameObject[instance.filteredSelection.NodeTargets.Length];
			var newTransforms = new Transform[instance.filteredSelection.NodeTargets.Length];
			for (int i = 0; i < instance.filteredSelection.NodeTargets.Length; i++)
			{
				var originalGameObject	= instance.filteredSelection.NodeTargets[i].gameObject;
				var originalTransform	= originalGameObject.GetComponent<Transform>();
				newTargets[i] = UnityEngine.Object.Instantiate(originalGameObject) as GameObject;
				var newTransform = newTargets[i].GetComponent<Transform>();
				if (originalTransform.parent != null)
				{
					newTransform.SetParent(originalTransform.parent, false);
					newTransform.SetSiblingIndex(originalTransform.GetSiblingIndex() + 1);
					newTransform.name = GameObjectUtility.GetUniqueNameForSibling(originalTransform.parent, originalTransform.name);
				}
				if (setTransform == null)
				{
					newTransform.localScale		= originalTransform.localScale;
					newTransform.localPosition	= originalTransform.localPosition;
					newTransform.localRotation	= originalTransform.localRotation;
				} else
					setTransform(newTransform, originalTransform);

				var childBrushes = newTargets[i].GetComponentsInChildren<CSGBrush>();

				Dictionary<uint, uint> uniqueSmoothingGroups = new Dictionary<uint, uint>();
				foreach (var childBrush in childBrushes)
				{
					for (int g = 0; g < childBrush.Shape.TexGens.Length; g++)
					{
						var smoothingGroup = childBrush.Shape.TexGens[g].SmoothingGroup;
						if (smoothingGroup == 0)
							continue;

						uint newSmoothingGroup;
						if (!uniqueSmoothingGroups.TryGetValue(smoothingGroup, out newSmoothingGroup))
						{
							newSmoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex();
							uniqueSmoothingGroups[smoothingGroup] = newSmoothingGroup;
						}

						childBrush.Shape.TexGens[g].SmoothingGroup = newSmoothingGroup;
					}
				}

				newTransforms[i] = newTransform;
				Undo.RegisterCreatedObjectUndo(newTargets[i], "Created clone of " + originalGameObject.name);
			}

			Selection.objects = newTargets;
			Undo.CollapseUndoOperations(groupId);

			return newTransforms;
		}
	}
}
