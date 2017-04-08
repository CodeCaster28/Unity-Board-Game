using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class MeshToolGUI
	{
		static int SceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

		static GUIContent			ContentMeshLabel;
//		static GUIContent			ContentBrushesLabel;
//		static GUIContent			ContentEdgesLabel;

		static GUILayoutOption		labelWidth			= GUILayout.Width(23);

		/*
		static readonly CSGOperationType[] operationValues = new CSGOperationType[]
			{
				CSGOperationType.Additive,
				CSGOperationType.Subtractive,
				CSGOperationType.Intersecting
			};
		*/
		static void InitLocalStyles()
		{
			if (ContentMeshLabel != null)
				return;
			ContentMeshLabel	= new GUIContent(GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Mesh]);
//			ContentBrushesLabel	= new GUIContent(GUIStyleUtility.brushEditModeNames[(int)BrushEditMode.Brushes]);
//			ContentEdgesLabel	= new GUIContent("Edges");
		}

		static GUIContent	ContentFlip			= new GUIContent("Flip");
		static GUIContent	ContentFlipX		= new GUIContent("X");
		static ToolTip		TooltipFlipX		= new ToolTip("Flip X", "Flip the selection in the x direction", Keys.FlipSelectionX);
		static GUIContent	ContentFlipY		= new GUIContent("Y");
		static ToolTip		TooltipFlipY		= new ToolTip("Flip Y", "Flip the selection in the y direction", Keys.FlipSelectionY);
		static GUIContent	ContentFlipZ		= new GUIContent("Z");
		static ToolTip		TooltipFlipZ		= new ToolTip("Flip Z", "Flip the selection in the z direction", Keys.FlipSelectionZ);
		static GUIContent	ContentSnapToGrid	= new GUIContent("Snap to grid");
		static ToolTip		TooltipSnapToGrid	= new ToolTip(ContentSnapToGrid.text, "Snap the selection to the closest grid lines", Keys.SnapToGridKey);
		
		static Rect lastGuiRect;
		public static Rect GetLastSceneGUIRect(MeshEditBrushTool tool)
		{
			return lastGuiRect;
		}

		public static void OnSceneGUI(MeshEditBrushTool tool)
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
						GUILayout.BeginVertical(ContentMeshLabel, windowStyle, GUIStyleUtility.ContentEmpty);
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
			var tool = CSGBrushEditorManager.ActiveTool as MeshEditBrushTool;
			GUIStyleUtility.InitStyles();
			InitLocalStyles();
			OnGUIContents(false, tool);
		}


		static void OnGUIContents(bool isSceneGUI, MeshEditBrushTool tool)
		{
			var filteredSelection = CSGBrushEditorManager.FilteredSelection;
			bool operations_enabled = tool != null && 
									(filteredSelection.NodeTargets.Length > 0 && filteredSelection.ModelTargets.Length == 0);
			
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				EditorGUI.BeginDisabledGroup(!operations_enabled);
				{
					bool mixedValues = tool == null || ((filteredSelection.BrushTargets.Length == 0) && (filteredSelection.OperationTargets.Length == 0));
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
						bool passThroughValue	= false;
						if (tool != null &&
							//filteredSelection.BrushTargets.Length == 0 && 
							filteredSelection.OperationTargets.Length > 0 &&
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

							var ptMixedValues		= !passThrough.HasValue;
							passThroughValue		= passThrough.HasValue ? passThrough.Value : false;
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
								
				if (!isSceneGUI)
					GUILayout.Space(10);

				bool brush_enabled = tool != null && (filteredSelection.NodeTargets.Length > 0);

				EditorGUI.BeginDisabledGroup(!brush_enabled);
				{
					GUIStyle left	= EditorStyles.miniButtonLeft;
					GUIStyle middle	= EditorStyles.miniButtonMid;
					GUIStyle right	= EditorStyles.miniButtonRight;

					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						EditorGUILayout.LabelField(ContentFlip, labelWidth);
						if (GUILayout.Button(ContentFlipX, left  )) { tool.FlipX(); }
						TooltipUtility.SetToolTip(TooltipFlipX);
						if (GUILayout.Button(ContentFlipY, middle)) { tool.FlipY(); }
						TooltipUtility.SetToolTip(TooltipFlipY);
						if (GUILayout.Button(ContentFlipZ, right )) { tool.FlipZ(); }
						TooltipUtility.SetToolTip(TooltipFlipZ);
					}
					GUILayout.EndHorizontal();
				
					if (!isSceneGUI)
						GUILayout.Space(10);

					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						if (GUILayout.Button(ContentSnapToGrid)) { tool.SnapToGrid(); }
						TooltipUtility.SetToolTip(TooltipSnapToGrid);
					}
					GUILayout.EndHorizontal();
					/*
					EditorGUILayout.LabelField(ContentEdgesLabel);
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						EditorGUI.BeginDisabledGroup(!tool.CanSmooth());
						{ 
							if (GUILayout.Button("Smooth"))		{ tool.Smooth(); }
						}
						EditorGUI.EndDisabledGroup();
						EditorGUI.BeginDisabledGroup(!tool.CanUnSmooth());
						{
							if (GUILayout.Button("Un-smooth"))	{ tool.UnSmooth(); }
						}
						EditorGUI.EndDisabledGroup();
					}
					GUILayout.EndHorizontal();
					*/
				}
				EditorGUI.EndDisabledGroup();

				if (!isSceneGUI || !SceneView.currentDrawingSceneView.camera.orthographic)
				{
					if (!isSceneGUI)
					{
						GUILayout.Space(10);
						GUILayout.Label("Tips", EditorStyles.miniLabel);
					}

					GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
					{
						//GUILayout.Label(Keys.VerticalMoveMode.ToString() + " to dragging brush up/down", EditorStyles.miniLabel);
						GUILayout.Label("Control (hold) to drag polygon on it's plane", EditorStyles.miniLabel);
						GUILayout.Label("Shift (hold) to drag extrude polygon", EditorStyles.miniLabel);
					}
					GUILayout.EndVertical();
				}
			}
			GUILayout.EndVertical();
			EditorGUI.showMixedValue = false;
		}
	}
}
