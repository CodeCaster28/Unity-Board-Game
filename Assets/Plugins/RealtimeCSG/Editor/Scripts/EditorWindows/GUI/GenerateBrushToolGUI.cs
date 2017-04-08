using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class GenerateBrushToolGUI
	{
		static int SceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

		static GUIContent			ContentTitleLabel;

		static void InitLocalStyles()
		{
			if (ContentTitleLabel != null)
				return;
			ContentTitleLabel	= new GUIContent(GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Generate]);
		}

		static Rect lastGuiRect;
		public static Rect GetLastSceneGUIRect(GenerateBrushTool tool)
		{
			return lastGuiRect;
		}

		public static bool OnSceneGUI(GenerateBrushTool tool)
		{
			tool.CurrentGenerator.StartGUI();
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
						
						GUILayout.BeginVertical(ContentTitleLabel, windowStyle, GUILayout.Width(275));
						{
							OnGUIContents(tool, true);
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
							case EventType.MouseDown:	{ if (currentArea.Contains(Event.current.mousePosition)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
							case EventType.MouseMove:	{ if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
							case EventType.MouseUp:		{ if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
							case EventType.MouseDrag:	{ if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
							case EventType.ScrollWheel: { if (currentArea.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();			
			tool.CurrentGenerator.FinishGUI();
			return true;
		}

		static void OnGUIContents(GenerateBrushTool tool, bool isSceneGUI)
		{							
			GUIStyleUtility.InitStyles();
			if (!isSceneGUI)
			{
				GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
				{
					var csg_skin = GUIStyleUtility.Skin;
					tool.BuilderMode = (ShapeMode)GUIStyleUtility.ToolbarWrapped((int)tool.BuilderMode, csg_skin.shapeModeNames, tooltips: GUIStyleUtility.shapeModeTooltips, areaWidth: EditorGUIUtility.currentViewWidth - 4);
				}
				GUILayout.EndVertical();
				EditorGUILayout.Space();
			}
			tool.CurrentGenerator.OnShowGUI(isSceneGUI);
			if (isSceneGUI)
			{
				EditorGUILayout.Space();
				GUILayout.BeginVertical(GUILayout.Width(275));
				{
					var csg_skin = GUIStyleUtility.Skin;
					tool.BuilderMode = (ShapeMode)GUIStyleUtility.ToolbarWrapped((int)tool.BuilderMode, csg_skin.shapeModeNames, tooltips: GUIStyleUtility.shapeModeTooltips, areaWidth: 300); 
					//tool.BuilderMode = (ShapeMode)GUILayout.Toolbar((int)tool.BuilderMode, csg_skin.shapeModeNames);
				}
				GUILayout.EndVertical();
			}
		}

		public static void OnInspectorGUI(GenerateBrushTool tool, EditorWindow window)
		{
			lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
			tool.CurrentGenerator.StartGUI();
			GUIStyleUtility.InitStyles();
			InitLocalStyles();
			OnGUIContents(tool, false);
			tool.CurrentGenerator.FinishGUI();
		}
		
	}
}
