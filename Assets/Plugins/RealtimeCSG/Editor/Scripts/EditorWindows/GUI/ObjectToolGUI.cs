using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class ObjectToolGUI
	{
		static int SceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

		static GUIContent				ContentTitleLabel;
		static readonly GUIContent		RecenterPivotContent	= new GUIContent("Recenter pivot");
		static readonly GUIContent		PivotXContent			= new GUIContent("X");
		static readonly GUIContent		PivotYContent			= new GUIContent("Y");
		static readonly GUIContent		PivotZContent			= new GUIContent("Z");

		static readonly ToolTip			RecenterPivotTooltip    = new ToolTip("Recenter pivot",
																			  "Click this to place the center of rotation\n"+
																			  "(the pivot) to the center of the selection.\n\n"+
																			  "This is disabled when you have no selection\n"+
																			  "or when Unity's pivot mode (top left corner)\n"+
																			  "is set to 'Center'.", 
																			  Keys.CenterPivot);
		static readonly ToolTip			PivotVectorTooltip		= new ToolTip("Set pivot point",
																			  "Here you can manually set the current center\n"+
																			  "of rotation (the pivot).\n\n"+
																			  "This is disabled when you have no selection\n"+
																			  "or when Unity's pivot mode (top left corner)\n"+
																			  "is set to 'Center'.");
		
		static readonly float			Width15Value			= 15;
		static readonly GUILayoutOption	Width15					= GUILayout.Width(Width15Value);
		
		static readonly float			Width22Value			= 22;
		static readonly GUILayoutOption	Width22					= GUILayout.Width(Width22Value);
		
		static readonly GUILayoutOption	MaxWidth150				= GUILayout.Width(150);

		static void InitLocalStyles()
		{
			if (ContentTitleLabel != null)
				return;
			ContentTitleLabel	= new GUIContent(GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Object]);
		}
		
		static Rect lastGuiRect;
		public static Rect GetLastSceneGUIRect(ObjectEditBrushTool tool)
		{
			return lastGuiRect;
		}

		public static void OnSceneGUI(ObjectEditBrushTool tool)
		{
			GUIStyleUtility.InitStyles();
			InitLocalStyles();
			GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
			{
				GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
				{
					GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.FlexibleSpace();

						GUIStyleUtility.ResetGUIState();
						
						GUIStyle windowStyle = GUI.skin.window;
						GUILayout.BeginVertical(ContentTitleLabel, windowStyle, GUIStyleUtility.ContentEmpty);
						{
							OnGUIContents(true, tool);	
						}
						GUILayout.EndVertical();

						var currentArea = GUILayoutUtility.GetLastRect();
						lastGuiRect = currentArea;

						var buttonArea = currentArea;
						buttonArea.x += buttonArea.width - 17;
						buttonArea.y += 2;
						buttonArea.height = 13;
						buttonArea.width = 13;
						if (GUI.Button(buttonArea, GUIContent.none, "WinBtnClose"))
							CSGBrushEditorWindow.GetWindow();
						TooltipUtility.SetToolTip(GUIStyleUtility.PopOutTooltip, buttonArea);

						int controlID = GUIUtility.GetControlID(SceneViewMeshOverlayHash, FocusType.Keyboard, currentArea);
						switch (Event.current.GetTypeForControl(controlID))
						{
							case EventType.MouseDown: { if (currentArea.Contains(Event.current.mousePosition)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
							case EventType.MouseMove: { if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
							case EventType.MouseUp: { if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
							case EventType.MouseDrag: { if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
							case EventType.ScrollWheel: { if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}

		public static void OnInspectorGUI(EditorWindow window)
		{
			lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
			var tool = CSGBrushEditorManager.ActiveTool as ObjectEditBrushTool;
			
			GUIStyleUtility.InitStyles();
			InitLocalStyles();
			OnGUIContents(false, tool);
		}
		
		static void OnGUIContents(bool isSceneGUI, ObjectEditBrushTool tool)
		{
			var filteredSelection = CSGBrushEditorManager.FilteredSelection;
			bool operations_enabled = (tool != null &&
										filteredSelection.NodeTargets.Length > 0 &&
										filteredSelection.NodeTargets.Length == (filteredSelection.BrushTargets.Length + filteredSelection.OperationTargets.Length));
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				EditorGUI.BeginDisabledGroup(!operations_enabled);
				{
					bool mixedValues = (tool != null &&
										filteredSelection.BrushTargets.Length == 0) && (filteredSelection.OperationTargets.Length == 0);
					CSGOperationType operationType = CSGOperationType.Additive;
					if (tool != null)
					{
						if (filteredSelection.BrushTargets.Length > 0)
						{
							operationType = filteredSelection.BrushTargets[0].OperationType;
							for (int i = 1; i < filteredSelection.BrushTargets.Length; i++)
							{
								if (filteredSelection.BrushTargets[i].OperationType != operationType)
								{
									mixedValues = true;
								}
							}
						} else
						if (filteredSelection.OperationTargets.Length > 0)
						{
							operationType = filteredSelection.OperationTargets[0].OperationType;
						}

						if (filteredSelection.OperationTargets.Length > 0)
						{
							for (int i = 0; i < filteredSelection.OperationTargets.Length; i++)
							{
								if (filteredSelection.OperationTargets[i].OperationType != operationType)
								{
									mixedValues = true;
								}
							}
						}
					}

					GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
					{
						bool passThroughValue = false;
						if (tool != null &&
							filteredSelection.BrushTargets.Length == 0 && filteredSelection.OperationTargets.Length > 0 &&
							filteredSelection.OperationTargets.Length == filteredSelection.NodeTargets.Length) // only operations
						{
							bool? passThrough = filteredSelection.OperationTargets[0].PassThrough;
							for (int i = 1; i < filteredSelection.OperationTargets.Length; i++)
							{
								if (passThrough.HasValue && passThrough.Value != filteredSelection.OperationTargets[i].PassThrough)
								{
									passThrough = null;
									break;
								}
							}

							mixedValues = !passThrough.HasValue || passThrough.Value;

							var ptMixedValues = !passThrough.HasValue;
							passThroughValue = passThrough.HasValue ? passThrough.Value : false;
							if (GUIStyleUtility.PassThroughButton(passThroughValue, ptMixedValues))
							{
								Undo.RecordObjects(filteredSelection.OperationTargets, "Changed CSG operation of nodes");
								foreach (var operation in filteredSelection.OperationTargets)
								{
									operation.PassThrough = true;
								}
								InternalCSGModelManager.Refresh();
								EditorApplication.RepaintHierarchyWindow();
							}

							if (passThroughValue)
								operationType = (CSGOperationType)255;
						}
						EditorGUI.BeginChangeCheck();
						{
							operationType = GUIStyleUtility.ChooseOperation(operationType, mixedValues);
						}
						if (EditorGUI.EndChangeCheck() && tool != null)
						{
							Undo.RecordObjects(filteredSelection.NodeTargets, "Changed CSG operation of nodes");
							for (int i = 0; i < filteredSelection.BrushTargets.Length; i++)
							{
								filteredSelection.BrushTargets[i].OperationType = operationType;
							}
							for (int i = 0; i < filteredSelection.OperationTargets.Length; i++)
							{
								filteredSelection.OperationTargets[i].PassThrough = false;
								filteredSelection.OperationTargets[i].OperationType = operationType;
							}
							InternalCSGModelManager.Refresh();
							EditorApplication.RepaintHierarchyWindow();
						}
					}
					GUILayout.EndVertical();
				}
				EditorGUI.EndDisabledGroup();
				if (Tools.current == Tool.Rotate)
				{
					EditorGUILayout.Space();
					var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
					var nextUnit	 = Units.CycleToNextUnit(distanceUnit);
					var unitText	 = Units.GetUnitGUIContent(distanceUnit);
					EditorGUI.BeginDisabledGroup(Tools.pivotMode == PivotMode.Center || !tool.HaveSelection);
					{
						GUILayout.BeginVertical();
						{
							if (GUILayout.Button(RecenterPivotContent))
							{
								tool.RecenterPivot();
							}
							TooltipUtility.SetToolTip(RecenterPivotTooltip);
							Vector3 realNewCenter; 
							if (Tools.pivotRotation == PivotRotation.Local)
								realNewCenter = tool.LocalSpacePivotCenter;
							else
								realNewCenter = tool.WorldSpacePivotCenter;

							Vector3 displayNewCenter = GridUtility.CleanPosition(realNewCenter);
							bool modifiedVector = false;
							bool clickedUnitButton = false;
								
							var areaWidth	= EditorGUIUtility.currentViewWidth;

							const float minWidth =  65;

							var allWidth	= (Width15Value * 3) + (Width22Value * 3) + (minWidth * 3);

							GUILayoutOption[] doubleFieldOptions;
							if (!isSceneGUI)
								doubleFieldOptions = new GUILayoutOption[0];
							else
								doubleFieldOptions = new GUILayoutOption[] { MaxWidth150 };


							var multiLine	= isSceneGUI || (allWidth >= areaWidth);
							if (multiLine)
								GUILayout.BeginVertical();
							GUILayout.BeginHorizontal();
							{
								EditorGUI.BeginChangeCheck();
								{
									EditorGUILayout.LabelField(PivotXContent, Width15);
									displayNewCenter.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, displayNewCenter.x), doubleFieldOptions));
								}
								if (EditorGUI.EndChangeCheck())
								{
									realNewCenter.x = displayNewCenter.x;
									modifiedVector = true;
								}
								clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, Width22) || clickedUnitButton;
								if (multiLine)
								{
									GUILayout.EndHorizontal();
									GUILayout.BeginHorizontal();
								}
								EditorGUI.BeginChangeCheck();
								{
									EditorGUILayout.LabelField(PivotYContent, Width15);
									displayNewCenter.y = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, displayNewCenter.y), doubleFieldOptions));
								}
								if (EditorGUI.EndChangeCheck())
								{
									realNewCenter.y = displayNewCenter.y;
									modifiedVector = true;
								}
								clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, Width22) || clickedUnitButton;
								if (multiLine)
								{
									GUILayout.EndHorizontal();
									GUILayout.BeginHorizontal();
								}
								EditorGUI.BeginChangeCheck();
								{
									EditorGUILayout.LabelField(PivotZContent, Width15);
									displayNewCenter.z = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, displayNewCenter.z), doubleFieldOptions));
								}
								if (EditorGUI.EndChangeCheck())
								{
									realNewCenter.z = displayNewCenter.z;
									modifiedVector = true;
								}
								clickedUnitButton = GUILayout.Button(unitText, EditorStyles.miniLabel, Width22) || clickedUnitButton;
							}
							GUILayout.EndHorizontal();
							if (multiLine)
								GUILayout.EndVertical();
							TooltipUtility.SetToolTip(PivotVectorTooltip);
							if (modifiedVector)
							{
								if (Tools.pivotRotation == PivotRotation.Local)
									tool.LocalSpacePivotCenter = realNewCenter;
								else
									tool.WorldSpacePivotCenter = realNewCenter;
							}
							if (clickedUnitButton)
							{
								distanceUnit = nextUnit;
								RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
								RealtimeCSG.CSGSettings.UpdateSnapSettings();
								RealtimeCSG.CSGSettings.Save();
								SceneView.RepaintAll();
							}
						}
						GUILayout.EndVertical();
					}
					EditorGUI.EndDisabledGroup();
				}
				/*
				if (Tools.current != Tool.Rotate)
				{
					if (!isSceneGUI || !SceneView.currentDrawingSceneView.camera.orthographic)
					{
						if (!isSceneGUI)
						{
							GUILayout.Space(10);
							GUILayout.Label("Tips", EditorStyles.miniLabel);
						}

						GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
						{
							GUILayout.Label(Keys.VerticalMoveMode.ToString() + " to drag brush up/down", EditorStyles.miniLabel);
						}
						GUILayout.EndVertical();
					}
				}
				*/
			}
			GUILayout.EndVertical();
			EditorGUI.showMixedValue = false;
		}
	}
}
