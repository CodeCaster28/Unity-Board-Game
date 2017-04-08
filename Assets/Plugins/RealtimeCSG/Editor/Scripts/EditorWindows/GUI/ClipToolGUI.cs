using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class ClipToolGUI
	{
		static int SceneViewMeshOverlayHash = "SceneViewClipOverlay".GetHashCode();

		static GUIContent			ContentClipLabel;

		static readonly ClipMode[] clipModeValues = new ClipMode[]
			{
				ClipMode.RemovePositive,
				ClipMode.RemoveNegative,
				ClipMode.Split
//				,ClipEditBrushTool.ClipMode.Mirror			
			};
		
		static GUIContent	ContentCommit		= new GUIContent("Commit");
		static GUIContent	ContentCancel		= new GUIContent("Cancel");
		static ToolTip		CommitTooltip		= new ToolTip("Commit your changes", "Split the selected brush(es) with the current clipping plane. This makes your changes final.", Keys.PerformActionKey);
		static ToolTip		CancelTooltip		= new ToolTip("Cancel your changes", "Do not clip your selected brushes and return them to their original state.", Keys.CancelActionKey);
		static GUIStyle		ButtonStyle;


		static void InitLocalStyles()
		{
			if (ContentClipLabel != null)
				return;


			ButtonStyle = new GUIStyle(GUI.skin.button);
			ButtonStyle.richText = true;

			ContentClipLabel	= new GUIContent(GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Clip]);
		}
		
		static bool doCommit = false; // unity bug workaround
		static bool doCancel = false; // unity bug workaround

		static Rect lastGuiRect;
		public static Rect GetLastSceneGUIRect(ClipBrushTool tool)
		{
			return lastGuiRect;
		}

		public static void OnSceneGUI(ClipBrushTool tool)
		{
			doCommit = false; // unity bug workaround
			doCancel = false; // unity bug workaround

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
						GUILayout.BeginVertical(ContentClipLabel, windowStyle, GUIStyleUtility.ContentEmpty);
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

						int controlID = GUIUtility.GetControlID(SceneViewMeshOverlayHash, FocusType.Passive, currentArea);
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

			if (tool != null)
			{ 
				if (doCommit) tool.Commit();	// unity bug workaround
				if (doCancel) tool.Cancel();	// unity bug workaround
			}
		}

		static void OnGUIContents(bool isSceneGUI, ClipBrushTool tool)
		{
			if (tool.ClipBrushCount == 0)
			{
				GUILayout.Label(string.Format("no brushes selected", tool.ClipBrushCount), GUIStyleUtility.redTextArea);
			} else
			{ 
				if (tool.ClipBrushCount == 1)
					GUILayout.Label(string.Format("{0} brush selected", tool.ClipBrushCount));
				else
					GUILayout.Label(string.Format("{0} brushes selected", tool.ClipBrushCount));
			}
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(tool == null);
			{ 
				GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
				{
					var newClipMode = (tool != null) ? tool.clipMode : ((ClipMode)999);
					var skin = GUIStyleUtility.Skin;
					for (int i = 0; i < clipModeValues.Length; i++)
					{
						var selected = newClipMode == clipModeValues[i];
						GUIContent content;
						GUIStyle style;
						if (selected)	{ style = GUIStyleUtility.selectedIconLabelStyle;   content = skin.clipNamesOn[i]; }
						else			{ style = GUIStyleUtility.unselectedIconLabelStyle; content = skin.clipNames[i];   }
						if (GUILayout.Toggle(selected, content, style))
						{
							newClipMode = clipModeValues[i];
						}
						TooltipUtility.SetToolTip(GUIStyleUtility.clipTooltips[i]);
					}
					if (tool != null && tool.clipMode != newClipMode)
					{
						tool.SetClipMode(newClipMode);
					}
				}
				GUILayout.EndVertical();
				if (!isSceneGUI)
					GUILayout.Space(10);

				bool disabled = (tool == null || tool.editMode != ClipBrushTool.EditMode.EditPoints);
				
				EditorGUI.BeginDisabledGroup(disabled);
				{ 
					GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
						{
							if (GUILayout.Button(ContentCommit, ButtonStyle)) { doCommit = true; }
							TooltipUtility.SetToolTip(CommitTooltip);
							if (GUILayout.Button(ContentCancel, ButtonStyle)) { doCancel = true; }
							TooltipUtility.SetToolTip(CancelTooltip);
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndVertical();
				}
				EditorGUI.EndDisabledGroup();
			}
			EditorGUI.EndDisabledGroup();
		}

		public static void OnInspectorGUI(EditorWindow window)
		{
			lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
			var tool = CSGBrushEditorManager.ActiveTool as ClipBrushTool;

			doCommit = false; // unity bug workaround
			doCancel = false; // unity bug workaround
			
			GUIStyleUtility.InitStyles();
			InitLocalStyles();
			OnGUIContents(false, tool);

			if (tool != null)
			{ 
				if (doCommit) tool.Commit();	// unity bug workaround
				if (doCancel) tool.Cancel();	// unity bug workaround
			}
		}
	}
}
