#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	[InitializeOnLoad]
	internal sealed class SceneViewEventHandler
	{
		static SceneViewEventHandler editor = null;
		static SceneViewEventHandler()
		{
			if (editor != null)
			{
				editor.Shutdown();
				editor = null;
			}
			editor = new SceneViewEventHandler();
			editor.Initialize();
		}
		/*
		~SceneViewEventHandler()
		{
			Shutdown(finalizing: true);
			editor = null;
		}*/


		bool initialized = false;
		bool had_first_update = false;

		void Initialize()
		{
			if (initialized)
				return;

			CSGKeysPreferences.ReadKeys();

			initialized = true;
			
			CSGSceneManagerRedirector.Interface = new CSGSceneManagerInstance();
			
			EditorApplication.update					-= OnFirstUpdate;
			EditorApplication.update					+= OnFirstUpdate;

			EditorApplication.hierarchyWindowChanged	-= OnHierarchyWindowChanged;
			EditorApplication.hierarchyWindowChanged	+= OnHierarchyWindowChanged;

			EditorApplication.hierarchyWindowItemOnGUI	-= OnHierarchyWindowItemOnGUI;
			EditorApplication.hierarchyWindowItemOnGUI	+= OnHierarchyWindowItemOnGUI;

			//Selection.selectionChanged				-= CSGBrushEditorManager.UpdateSelection;
			//Selection.selectionChanged				+= CSGBrushEditorManager.UpdateSelection;

			UpdateDefines();
		}


		const string RealTimeCSGDefine			= "RealtimeCSG";

		public static bool IsObsolete(Enum value)
		{
			var fi = value.GetType().GetField(value.ToString());
			var attributes = (ObsoleteAttribute[])
				fi.GetCustomAttributes(typeof(ObsoleteAttribute), false);
			return (attributes != null && attributes.Length > 0);
		}

		public static void UpdateDefines()
		{
			var targetGroups = Enum.GetValues(typeof(BuildTargetGroup)).Cast<BuildTargetGroup>().ToArray();
			foreach (var targetGroup in targetGroups)
			{
				if (IsObsolete(targetGroup))
					continue;
				if (targetGroup == BuildTargetGroup.Unknown
#if UNITY_5_6_OR_NEWER
					|| targetGroup == (BuildTargetGroup)27
#endif
					)
					continue;
				
				var symbol_string = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
				if (symbol_string == null)
					continue;
				var symbols = symbol_string.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = symbols.Length - 1; i >= 0; i--)
				{
					symbols[i] = symbols[i].Trim();
					if (symbols[i].Length == 0 ||
						symbols[i].StartsWith(RealTimeCSGDefine))
					{
						ArrayUtility.RemoveAt(ref symbols, i);
					}
				}

				if (!symbols.Contains(RealTimeCSGDefine)) ArrayUtility.Add(ref symbols, RealTimeCSGDefine);
				
				string v = ToolConstants.PluginVersion;
				int index = v.IndexOf('.');
				string release_version_part = v.Remove(index);
				string lower_part = v.Substring(index + 1);
				string major_version_part = lower_part.Remove(1);
				var minor_version_part = lower_part.Substring(1);

				var release_version = RealTimeCSGDefine + "_" + release_version_part;
				var major_version	= release_version + "_" + major_version_part;
				var minor_version	= major_version + "_" + minor_version_part;

				if (!symbols.Contains(release_version)) ArrayUtility.Add(ref symbols, release_version);
				if (!symbols.Contains(major_version)) ArrayUtility.Add(ref symbols, major_version);
				if (!symbols.Contains(minor_version)) ArrayUtility.Add(ref symbols, minor_version);


				var stringBuilder = new System.Text.StringBuilder();
				for (int i = 0; i < symbols.Length; i++)
				{
					if (stringBuilder.Length != 0)
						stringBuilder.Append(';');
					stringBuilder.Append(symbols[i]);
				}

				PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, stringBuilder.ToString());
			}
		}

		void Shutdown(bool finalizing = false)
		{
			if (editor != this)
				return;

			editor = null;
			CSGSceneManagerRedirector.Interface = null;
			if (!initialized)
				return;
			
			EditorApplication.update					-= OnFirstUpdate;
			EditorApplication.hierarchyWindowChanged	-= OnHierarchyWindowChanged;
			EditorApplication.hierarchyWindowItemOnGUI	-= OnHierarchyWindowItemOnGUI;
			SceneView.onSceneGUIDelegate				-= OnScene;
			Undo.undoRedoPerformed						-= UndoRedoPerformed;

			initialized = false;

			// make sure the C++ side of things knows to clear the method pointers
			// so that we don't accidentally use them while closing unity
			CSGBindings.ClearUnityMethods();
			CSGBindings.ClearExternalMethods();

			if (!finalizing)
				SceneTools.Cleanup();
		}

		void OnSceneUnloaded()
		{
			if (this.initialized)
				this.Shutdown();
			
			MeshInstanceManager.Shutdown();
			InternalCSGModelManager.Shutdown();

			editor = new SceneViewEventHandler();
			editor.Initialize();
		}

		public static void EnsureFirstUpdate()
		{
			if (editor == null)
				return;
			if (!editor.had_first_update)
				editor.OnFirstUpdate();
		}

		void OnHierarchyWindowChanged()
		{
			InternalCSGModelManager.UpdateHierarchy();
		}

		// Delegate for generic updates
		void OnFirstUpdate()
		{
			had_first_update = true;
			EditorApplication.update -= OnFirstUpdate;
			RealtimeCSG.CSGSettings.Reload();
			
			GetReflectedData();

			// register unity methods in the c++ code so that some unity functions
			// (such as debug.log) can be called from within the c++ code.
			CSGBindings.RegisterUnityMethods();

			// register dll methods so we can use them
			CSGBindings.RegisterExternalMethods();
			
			RunOnce();
			//CreateSceneChangeDetector();
		}

		void UndoRedoPerformed()
		{
			InternalCSGModelManager.UndoRedoPerformed();
		}

		void RunOnce()
		{
			if (EditorApplication.isPlaying)
			{
				// when you start playing the game in the editor, it'll call 
				// RunOnce before playing the game, but not after.
				// so we need to wait until the game has stopped, after which we'll 
				// run first update again.
				EditorApplication.update -= OnWaitUntillStoppedPlaying;
				EditorApplication.update += OnWaitUntillStoppedPlaying;
				return;
			}
			
			SceneView.onSceneGUIDelegate -= OnScene;
			SceneView.onSceneGUIDelegate += OnScene;
			Undo.undoRedoPerformed		 -= UndoRedoPerformed;
			Undo.undoRedoPerformed		 += UndoRedoPerformed;
			
			InternalCSGModelManager.UpdateHierarchy();

			// we use a co-routine for updates because EditorApplication.update
			// works at a ridiculous rate and the co-routine is only fired in the
			// editor when something has happened.
			ResetUpdateRoutine();
		}

		void OnWaitUntillStoppedPlaying()
		{
			if (!EditorApplication.isPlaying)
			{
				EditorApplication.update -= OnWaitUntillStoppedPlaying;

				EditorApplication.update -= OnFirstUpdate;	
				EditorApplication.update += OnFirstUpdate;
			}
		}
		
		static void RunEditorUpdate()
		{
			if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
				return;

			UpdateOnSceneChange();
			if (EditorApplication.isPlaying
				|| EditorApplication.isPaused
				|| EditorApplication.isPlayingOrWillChangePlaymode
				//|| EditorApplication.isCompiling
				//|| EditorApplication.isUpdating
				)
				return;
			
			try
			{
				InternalCSGModelManager.Refresh(forceHierarchyUpdate: false);
				TooltipUtility.CleanCache();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}
		/*
		static IEnumerator OnEditorUpdate()
		{
			while (true)
			{
				RunEditorUpdate();
				yield return null;
			}
		}
		*/
		public static void ResetUpdateRoutine()
		{
			if (editor != null &&
				!editor.initialized)
			{
				editor = null;
			}
			if (editor == null)
			{
				editor = new SceneViewEventHandler();
				editor.Initialize();
			}
			
			//CoroutineExecuter.StartCoroutine(OnEditorUpdate());
			EditorApplication.update -= RunEditorUpdate;
			EditorApplication.update += RunEditorUpdate;
			InternalCSGModelManager.skipRefresh = false;
		}

		public static bool IsActive()
		{
			return (editor != null && editor.initialized);
		}

		static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
		{
			var o = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

			if (selectionRect.Contains(Event.current.mousePosition))
			{
				Transform t = (o == null) ? null : o.transform;
				OnHandleDragAndDrop(inSceneView: false, transformInInspector: t, selectionRect: selectionRect);
			}

			if (o == null)
				return;
			
			GUIStyleUtility.InitStyles();

			var node = o.GetComponent<CSGNode>();
			if (node == null ||
				!node.enabled || (node.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
				return;

			CSGOperationType operationType = CSGOperationType.Additive;

			var brush = node as CSGBrush;
			if (brush != null)
			{
				operationType = brush.OperationType;
				var skin = GUIStyleUtility.Skin;
				GUI.Label(selectionRect, skin.hierarchyOperations[(int)operationType], GUIStyleUtility.rightAlignedLabel);
				return;
			}
			var operation = node as CSGOperation;
			if (operation != null)
			{
				var skin = GUIStyleUtility.Skin;
				if (!operation.PassThrough)
				{
					operationType = operation.OperationType;
					var operationTypeIndex = (int)operationType;
					if (operationTypeIndex >= 0 && operationTypeIndex < skin.hierarchyOperations.Length)
						GUI.Label(selectionRect, skin.hierarchyOperations[operationTypeIndex], GUIStyleUtility.rightAlignedLabel);
				} else
				{
					GUI.Label(selectionRect, skin.hierarchyPassThrough, GUIStyleUtility.rightAlignedLabel);
				}
				return;
			}
		}


		static Type			UnitySceneViewType;
		static Type			UnityRectSelectionType;
		static Type			UnityEnumSelectionType;

		static object		SelectionType_Additive;
		static object		SelectionType_Subtractive;
		static object		SelectionType_Normal;
			
		static FieldInfo	m_RectSelection_field;
		static FieldInfo	m_RectSelecting_field;
		static FieldInfo	s_RectSelectionID_field;
		static FieldInfo	m_SelectStartPoint_field;
		static FieldInfo	m_SelectMousePoint_field;
		static FieldInfo	m_SelectionStart_field;
		static FieldInfo	m_LastSelection_field;
		static FieldInfo	m_CurrentSelection_field;
		static MethodInfo	UpdateSelection_method;

		static Type			UnityAnnotationUtility;
		static PropertyInfo UnityShowGridProperty;
	
		static bool			reflectionSucceeded = false;

		static void GetReflectedData()
		{
			UnitySceneViewType			= typeof(SceneView);
			
			var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
			var types = new List<System.Type>();
			foreach(var assembly in assemblies)
			{
				try
				{
					types.AddRange(assembly.GetTypes());
				}
				catch { }
			}
			UnityRectSelectionType		= types.FirstOrDefault(t => t.FullName == "UnityEditor.RectSelection");
			UnityEnumSelectionType 		= types.FirstOrDefault(t => t.FullName == "UnityEditor.RectSelection+SelectionType");
			UnityAnnotationUtility		= types.FirstOrDefault(t => t.FullName == "UnityEditor.AnnotationUtility");
			

			if (UnitySceneViewType != null)
			{
				m_RectSelection_field		= UnitySceneViewType.GetField("m_RectSelection", BindingFlags.NonPublic | BindingFlags.Instance);
			} else
			{
				m_RectSelection_field = null;
			}

			if (UnityAnnotationUtility != null)
			{
				UnityShowGridProperty = UnityAnnotationUtility.GetProperty("showGrid", BindingFlags.NonPublic | BindingFlags.Static);
				if (UnityShowGridProperty != null)
					UnityShowGridProperty.SetValue(UnityAnnotationUtility, false, null);
			} else
			{
				UnityShowGridProperty = null;
			}


			if (UnityRectSelectionType != null) 
			{
				m_RectSelecting_field		= UnityRectSelectionType.GetField("m_RectSelecting",	BindingFlags.NonPublic | BindingFlags.Instance);
				s_RectSelectionID_field		= UnityRectSelectionType.GetField("s_RectSelectionID",	BindingFlags.NonPublic | BindingFlags.Static);
				m_SelectStartPoint_field	= UnityRectSelectionType.GetField("m_SelectStartPoint",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_SelectionStart_field		= UnityRectSelectionType.GetField("m_SelectionStart",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_LastSelection_field		= UnityRectSelectionType.GetField("m_LastSelection",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_CurrentSelection_field	= UnityRectSelectionType.GetField("m_CurrentSelection",	BindingFlags.NonPublic | BindingFlags.Instance);
				m_SelectMousePoint_field	= UnityRectSelectionType.GetField("m_SelectMousePoint",	BindingFlags.NonPublic | BindingFlags.Instance);
				
				if (UnityEnumSelectionType != null)
				{
					SelectionType_Additive		= Enum.Parse(UnityEnumSelectionType, "Additive");
					SelectionType_Subtractive	= Enum.Parse(UnityEnumSelectionType, "Subtractive");
					SelectionType_Normal		= Enum.Parse(UnityEnumSelectionType, "Normal");
			
					UpdateSelection_method		= UnityRectSelectionType.GetMethod("UpdateSelection", BindingFlags.NonPublic | BindingFlags.Static,
																					null,
																					new Type[] {
																						typeof(UnityEngine.Object[]),
																						typeof(UnityEngine.Object[]),
																						UnityEnumSelectionType,
																						typeof(bool)
																					},
																					null);
				}
			}

			reflectionSucceeded =	s_RectSelectionID_field  != null &&
									m_RectSelection_field    != null &&
									m_RectSelecting_field    != null &&
									m_SelectStartPoint_field != null &&
									m_SelectMousePoint_field != null &&
									UpdateSelection_method   != null;
		}

		static bool Showgrid
		{
			get
			{
				if (UnityShowGridProperty != null)
					return (bool)UnityShowGridProperty.GetValue(UnityAnnotationUtility, null);
				return true;
			}
			set
			{
				if (UnityShowGridProperty != null)
					UnityShowGridProperty.SetValue(UnityAnnotationUtility, value, null);
			}
		}

		
		static HashSet<GameObject>	rectFoundGameObjects = new HashSet<GameObject>();
		static Vector2				prevStartGUIPoint;
		static Vector2				prevMouseGUIPoint;
		static Vector2				prevStartScreenPoint;
		static Vector2				prevMouseScreenPoint;
//		static Rect?			 currentMarqueRect = null;

		// Update rectangle selection using reflection
		// This is hacky, dangerous & the only way to do this ..
		static void UpdateRectSelection(SceneView sceneView)
		{
			if (!reflectionSucceeded)
			{
//				currentMarqueRect = null;
				prevStartGUIPoint = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
				prevMouseGUIPoint = prevStartGUIPoint;
				prevStartScreenPoint = MathConstants.zeroVector2;
				prevMouseScreenPoint = MathConstants.zeroVector2;
				rectFoundGameObjects.Clear();
				return;
			}

			//var mousePosition	= Event.current.mousePosition;
			var s_RectSelectionID_instance	= (int)s_RectSelectionID_field.GetValue(null);	
			// check if we're rect-selecting
			if (GUIUtility.hotControl == s_RectSelectionID_instance)
			{
				var typeForControl	= Event.current.GetTypeForControl(s_RectSelectionID_instance);
				if (typeForControl == EventType.used ||
					Event.current.commandName == "ModifierKeysChanged")
				{
					// m_RectSelection field of SceneView
					var m_RectSelection_instance = m_RectSelection_field.GetValue(sceneView);

					// m_RectSelecting field of RectSelection instance
					var m_RectSelecting_instance = (bool)m_RectSelecting_field.GetValue(m_RectSelection_instance);
					if (m_RectSelecting_instance)
					{
						// m_SelectStartPoint of RectSelection instance
						var m_SelectStartPoint_instance = (Vector2)m_SelectStartPoint_field.GetValue(m_RectSelection_instance);

						// m_SelectMousePoint of RectSelection instance
						var m_SelectMousePoint_instance = (Vector2)m_SelectMousePoint_field.GetValue(m_RectSelection_instance);

						// determine if our frustum changed since the last time
						bool modified = false;
						bool needUpdate = false;
						if (prevStartGUIPoint != m_SelectStartPoint_instance)
						{
							prevStartGUIPoint = m_SelectStartPoint_instance;
							prevStartScreenPoint = Event.current.mousePosition;
							needUpdate = true;
						}
						if (prevMouseGUIPoint != m_SelectMousePoint_instance)
						{
							prevMouseGUIPoint = m_SelectMousePoint_instance;
							prevMouseScreenPoint = Event.current.mousePosition;
							needUpdate = true;
						}
						if (needUpdate)
						{
							//var rect	= CameraUtility.PointsToRect(prevStartGUIPoint, prevMouseGUIPoint);
							//var frustum = CameraUtility.GetCameraSubFrustumGUI(Camera.current, rect);
							
							var rect	= CameraUtility.PointsToRect(prevStartScreenPoint, prevMouseScreenPoint);
							if (rect.width > 3 && rect.height > 3)
							{ 
								var frustum = CameraUtility.GetCameraSubFrustumGUI(Camera.current, rect);

	//							currentMarqueRect = rect;

								// Find all the brushes (and it's gameObjects) that are in the frustum
								if (SceneQueryUtility.GetItemsInFrustum(frustum.Planes, 
																	  rectFoundGameObjects))
								{ 
									modified = true;
								} else
								{
									if (rectFoundGameObjects != null &&
										rectFoundGameObjects.Count > 0)
									{
										rectFoundGameObjects.Clear();
										modified = true;
									}
								}
							}
						}

						GameObject[] currentSelection = null;
						var m_LastSelection_instance	= (Dictionary<GameObject, bool>)m_LastSelection_field.GetValue(m_RectSelection_instance);
						var m_SelectionStart_instance	= (UnityEngine.Object[])m_SelectionStart_field.GetValue(m_RectSelection_instance);
						if (modified &&
							rectFoundGameObjects != null &&
							rectFoundGameObjects.Count > 0)
						{
							if (CSGBrushEditorManager.ActiveTool == null)
							{
								if (CSGBrushEditorManager.EditMode != ToolEditMode.Object ||
									CSGBrushEditorManager.EditMode != ToolEditMode.Mesh)
								{
									CSGBrushEditorManager.EditMode = ToolEditMode.Object;
								}
							}

							foreach(var obj in rectFoundGameObjects)
							{
								// if it hasn't already been added, add the obj
								if (!m_LastSelection_instance.ContainsKey(obj))
								{
									m_LastSelection_instance.Add(obj, false);
								}


								// Remove models that we may have selected when we should be selecting it's brushes
								var model = obj.GetComponentInParent<CSGModel>();
								if (model != null)
								{
									var modelObj = model.gameObject;
									if (model != null &&
										modelObj != obj &&
										m_LastSelection_instance.ContainsKey(modelObj) &&
										!ArrayUtility.Contains(m_SelectionStart_instance, modelObj))
									{
										m_LastSelection_instance.Remove(modelObj);
										modified = true;
									}
								}
							}
							
							currentSelection = m_LastSelection_instance.Keys.ToArray();
							m_CurrentSelection_field.SetValue(m_RectSelection_instance, currentSelection);
						}
						for (int j = m_SelectionStart_instance.Length - 1; j >= 0; j--)
						{
							var obj = m_SelectionStart_instance[j] as GameObject;
							if (obj == null)
								continue;

							if (obj.GetComponent<GeneratedMeshInstance>() != null)
							{
								ArrayUtility.RemoveAt(ref m_SelectionStart_instance, j);
								m_LastSelection_instance.Remove(obj);
								m_SelectionStart_field.SetValue(m_RectSelection_instance, m_SelectionStart_instance);
								modified = true;
							}
						}

						if (//(rectFoundGameObjects != null && rectFoundGameObjects.Length > 0) &&
							(Event.current.commandName == "ModifierKeysChanged" || modified))
						{
							if (currentSelection == null || modified) { currentSelection = m_LastSelection_instance.Keys.ToArray(); }
							var foundObjects = currentSelection;

							for (int j = foundObjects.Length - 1; j >= 0; j--)
							{
								var obj = foundObjects[j];
								if (obj == null || obj.GetComponent<GeneratedMeshInstance>() != null)
								{
									ArrayUtility.RemoveAt(ref foundObjects, j);
									m_LastSelection_instance.Remove(obj);
									m_SelectionStart_field.SetValue(m_RectSelection_instance, m_SelectionStart_instance);
								}
							}


							var selectionTypeNormal = SelectionType_Normal;
							if (Event.current.shift) { selectionTypeNormal = SelectionType_Additive; } else
							if (EditorGUI.actionKey) { selectionTypeNormal = SelectionType_Subtractive; }

							// calling static method UpdateSelection of RectSelection 
							UpdateSelection_method.Invoke(null, 
								new object[] {
									m_SelectionStart_instance,
									foundObjects,
									selectionTypeNormal,
									m_RectSelecting_instance
								});
						}

					}
				}
			}
			if (GUIUtility.hotControl != s_RectSelectionID_instance)
			{
				prevStartGUIPoint = MathConstants.zeroVector2;
				prevMouseGUIPoint = MathConstants.zeroVector2;
				rectFoundGameObjects.Clear();
//				currentMarqueRect = null;
			}
		}
		

		static bool				rectClickDown		= false;
		static bool				mouseDragged		= false;
		static bool				draggingInScene		= false;
		static Vector2			clickMousePosition	= MathConstants.zeroVector2;

		static BrushDragOnSurfaceTool	    brushDragTool		= new BrushDragOnSurfaceTool();
		static MaterialDragOnSurfaceTool    materialDragTool    = new MaterialDragOnSurfaceTool();
		static SceneDragTool				currentDragTool     = null;
		static bool                         currentDragToolActive = false;
		
		static void OnHandleDragAndDrop(bool inSceneView, Transform transformInInspector = null, Rect? selectionRect = null)
		{
			switch (Event.current.type)
			{
				case EventType.DragUpdated:
				{
					if (!draggingInScene)
					{
						if (currentDragTool != null)
							currentDragTool.Reset();
						currentDragTool = null;
						currentDragToolActive = false;
						if (materialDragTool.ValidateDrop(inSceneView))
						{
							currentDragTool = materialDragTool;
						} else
						if (brushDragTool.ValidateDrop(inSceneView))
						{
							currentDragTool = brushDragTool;
						}
					}

					if (currentDragTool != null)
					{
						if (!currentDragTool.ValidateDropPoint(inSceneView))
						{
							if (currentDragTool != null && currentDragToolActive)
							{
								currentDragTool.DragExited(inSceneView);
							}
							currentDragToolActive = false;
						} else
						{
							currentDragToolActive = true;
							SceneTools.IsDraggingObjectInScene = true;
							DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
							if (inSceneView)
							{
								if (currentDragTool.DragUpdated())
								{
									HandleUtility.Repaint();
								}
							} else
							{
								if (currentDragTool.DragUpdated(transformInInspector, selectionRect.Value))
								{
									SceneView.RepaintAll();
								}
							}
							Event.current.Use();
							draggingInScene = true;
						}
					}
					break;
				}
				case EventType.DragPerform:
				{
					if (currentDragTool != null)
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
						if (currentDragToolActive)
						{
							currentDragTool.DragPerform(inSceneView);
							currentDragTool.Reset();
							Event.current.Use();
						}
						currentDragTool = null;
						draggingInScene = false;
					}
					break;
				}
				case EventType.DragExited:
				{
					if (currentDragTool != null)
					{
						if (currentDragToolActive)
						{
							currentDragTool.DragExited(inSceneView);
							Event.current.Use();
						}
						SceneTools.IsDraggingObjectInScene = false;
						currentDragTool = null;
						draggingInScene = false;
						SceneView.RepaintAll();
					}
					break;
				}
			}
		}
				
		[MenuItem("Edit/Realtime-CSG/Turn Realtime-CSG on or off %F3", false, 30)]
		static void ToggleRealtimeCSG()
		{
			RealtimeCSG.CSGSettings.EnableRealtimeCSG = !RealtimeCSG.CSGSettings.EnableRealtimeCSG;
			if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
			{
				Tools.hidden = false;
				Showgrid = CSGSettings.GridVisible;
			} else
				CSGSettings.GridVisible = Showgrid;
			RealtimeCSG.CSGSettings.UpdateWireframeModes();
			RealtimeCSG.CSGSettings.Save();
		}

		static Scene currentScene;
		static bool mousePressed;

		static void UpdateOnSceneChange()
		{
			var activeScene = SceneManager.GetActiveScene();
			if (currentScene != activeScene)
			{
				editor.OnSceneUnloaded();
				currentScene = activeScene;
				InternalCSGModelManager.InitOnNewScene();
			}
		}
		
		static void OnScene(SceneView sceneView)
		{
			if (!RealtimeCSG.CSGSettings.EnableRealtimeCSG)
				return;
			UpdateOnSceneChange();
			if (Event.current.type == EventType.layout)
				ColorSettings.Update();

			if (!IsActive())
				ResetUpdateRoutine();

			if (Event.current.type == EventType.MouseDown ||
				Event.current.type == EventType.MouseDrag) { mousePressed = true; }
			else if (Event.current.type == EventType.MouseUp ||
				Event.current.type == EventType.MouseMove) { mousePressed = false; }

			UpdateRectSelection(sceneView);
			OnHandleDragAndDrop(inSceneView: true);

			var s_RectSelectionID_instance	= (int)s_RectSelectionID_field.GetValue(null);
			var eventType					= Event.current.GetTypeForControl(s_RectSelectionID_instance);
			
			if (GUIUtility.hotControl == s_RectSelectionID_instance && 
				CSGBrushEditorManager.ActiveTool.IgnoreUnityRect)
				GUIUtility.hotControl = 0;
			switch (eventType)
			{
				case EventType.MouseDown:
				{
					rectClickDown = (Event.current.button == 0 && GUIUtility.hotControl == s_RectSelectionID_instance);
					clickMousePosition = Event.current.mousePosition;
					mouseDragged = false;
					break;
				}
				case EventType.MouseUp:
				{
					rectClickDown = false;
					break;
				}
				case EventType.MouseMove:
				{
					rectClickDown = false;
					break;
				}
				case EventType.Used:
				{
					if (clickMousePosition != Event.current.mousePosition)
					{
						mouseDragged = true;
					}
					if (!mouseDragged && rectClickDown && 
						Event.current.button == 0)
					{
						// m_RectSelection field of SceneView
						var m_RectSelection_instance = m_RectSelection_field.GetValue(sceneView);

						var m_RectSelecting_instance = (bool)m_RectSelecting_field.GetValue(m_RectSelection_instance);
						if (!m_RectSelecting_instance)
						{
							SelectionUtility.DoSelectionClick();
							Event.current.Use();
						}

					}
					rectClickDown = false;
					break;
				}


				case EventType.ValidateCommand:
				{
					if (Event.current.commandName == "SelectAll")
					{
						Event.current.Use();
						break;
					}
					if (Keys.HandleSceneValidate(CSGBrushEditorManager.CurrentTool, true))
					{
						Event.current.Use();
						HandleUtility.Repaint();
					}				
					break; 
				}
				case EventType.ExecuteCommand:
				{
					if (Event.current.commandName == "SelectAll")
					{
						var transforms = new List<UnityEngine.Object>();
						for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
						{
							var scene = SceneManager.GetSceneAt(sceneIndex);
							foreach (var gameObject in scene.GetRootGameObjects())
							{
								foreach (var transform in gameObject.GetComponentsInChildren<Transform>())
								{
									if ((transform.hideFlags & (HideFlags.NotEditable | HideFlags.HideInHierarchy)) == (HideFlags.NotEditable | HideFlags.HideInHierarchy))
										continue;
									transforms.Add(transform.gameObject);
								}
							}
						}
						Selection.objects = transforms.ToArray();

						Event.current.Use();
						break;
					}
					break;
				}

				case EventType.KeyDown:
				{
					if (Keys.HandleSceneKeyDown(CSGBrushEditorManager.CurrentTool, true))
					{
						Event.current.Use();
						HandleUtility.Repaint();
					}
					break;
				}

				case EventType.KeyUp:
				{
					if (Keys.HandleSceneKeyUp(CSGBrushEditorManager.CurrentTool, true))
					{
						Event.current.Use();
						HandleUtility.Repaint();
					}
					break;
				}

				case EventType.Layout:
				{
					if (currentDragTool != null)
					{
						currentDragTool.Layout();
					}
					break;
				}

				case EventType.Repaint:
				{	
					break;
				}
			}

			CSGBrushEditorManager.UpdateSelection();
			//bool fallbackGUI = EditorWindow.focusedWindow != sceneView;
			//fallbackGUI = 
				CSGBrushEditorManager.InitSceneGUI(sceneView);// || fallbackGUI;
															  //fallbackGUI = true;


			if (Event.current.type == EventType.repaint)
				MeshInstanceManager.RenderHelperSurfaces(sceneView.camera); 

			if (Event.current.type == EventType.repaint)
			{
				if (currentDragTool != null)
					currentDragTool.OnPaint();

				SceneTools.OnPaint(sceneView);
			} else
			//if (fallbackGUI)
			{
				BottomBarGUI.ShowGUI(sceneView);
			}


			CSGBrushEditorManager.OnSceneGUI(sceneView);

			//if (fallbackGUI)
			{
				TooltipUtility.InitToolTip(sceneView);
				if (Event.current.type == EventType.repaint)
				{
					BottomBarGUI.ShowGUI(sceneView);
				}
				if (!mousePressed)
				{
					Handles.BeginGUI();
					TooltipUtility.DrawToolTip(getLastRect: false);
					Handles.EndGUI();
				}
			}
		}
	}
}
#endif