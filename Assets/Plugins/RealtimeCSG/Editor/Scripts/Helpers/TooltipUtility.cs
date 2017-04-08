using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal class ToolTip
	{
		public ToolTip(string _title, string _contents) { title = _title; contents = _contents; }
		public ToolTip(string _title, string _contents, KeyEvent _key) { title = _title; contents = _contents;  key = _key; }
		public string	title;
		public string	contents;
		public KeyEvent key;

		public string TitleString()
		{
			return string.Format("<size=13><color=white><b>{0}</b></color></size>", title);
		}

		public string ContentsString()
		{
			return string.Format("<size=12><color=white>{0}</color></size>", contents);
        }

		public string KeyString()
		{
			if (key == null || key.IsEmpty())
				return string.Empty;

			return string.Format("<color=white><size=11><b>hotkey:</b> <i>{0}</i></size></color>", key.ToString());
		}

		public override string ToString()
		{
			return TitleString() + ContentsString() + KeyString();
		}
	}
	
	internal class TooltipContentState
	{
		public Rect			tooltipArea;
		public ToolTip		prevToolTip;
		public ToolTip		currentToolTip;

		public GUIContent   currentToolTipTitle;
		public GUIContent   currentToolTipContents;
		public GUIContent   currentToolTipKeyCodes;
		
		public Rect			currentToolTipTitleArea = new Rect();
		public Rect			currentToolTipContentsArea = new Rect();
		public Rect			currentToolTipKeyCodesArea = new Rect();

		public Rect			currentToolTipArea;
		//public Vector2    rectOffset;
		public float		tooltipTimer = 0;
		public bool         drawnThisFrame = false;

		public Rect         maxArea;

		public EditorWindow editorWindow;
		public Editor		editor;
	}

	internal static class TooltipUtility
	{
		static TooltipContentState currentState;
		static readonly Dictionary<UnityEngine.Object, TooltipContentState> currentStateCache = new Dictionary<UnityEngine.Object, TooltipContentState>();

		//static  EditorWindow editorWindow;

		const float TooltipWaitTime = 0.125f;

		public static void CleanCache()
		{
			var objects = currentStateCache.Keys.ToArray();
			for (int i=0;i<objects.Length;i++)
			{
				if (!objects[i])
					currentStateCache.Remove(objects[i]);
			}
		}

		public static void InitToolTip(SceneView sceneView)
		{
            //Debug.Log(sceneView.name);
			TooltipContentState state;
			if (!currentStateCache.TryGetValue(sceneView, out state))
			{
				state = new TooltipContentState();
				currentStateCache[sceneView] = state;
			}

			InitToolTip(state, sceneView);
		}
		
		public static void InitToolTip(EditorWindow editorWindow)
		{
			TooltipContentState state;
			if (!currentStateCache.TryGetValue(editorWindow, out state))
			{
				state = new TooltipContentState();
				currentStateCache[editorWindow] = state;
			}

			InitToolTip(state, editorWindow);
		}
		
		public static void InitToolTip(Editor editor)
		{
			TooltipContentState state;
			if (!currentStateCache.TryGetValue(editor, out state))
			{
				state = new TooltipContentState();
				currentStateCache[editor] = state;
			}

			InitToolTip(state, editor);
		}

		static void InitToolTip(TooltipContentState state, EditorWindow editorWindow)
		{
			currentState = state;
            if (Event.current.type != EventType.Repaint)
                return;

            currentState.currentToolTip = null;
			currentState.drawnThisFrame = false;
			//currentState.rectOffset = Vector2.zero;
			currentState.editorWindow = editorWindow;
			currentState.editor = null;

			currentState.maxArea = new Rect(0, 0, editorWindow.position.width, editorWindow.position.height);
		}

		static void InitToolTip(TooltipContentState state, Editor editor)
		{
			currentState = state;
            if (Event.current.type != EventType.Repaint &&
				Event.current.type != EventType.Layout)
                return;

            currentState.currentToolTip = null;
			currentState.drawnThisFrame = false;
			//currentState.rectOffset = Vector2.zero;
			currentState.editorWindow = null;
			currentState.editor = editor;

			currentState.maxArea = new Rect(0, 0, EditorGUIUtility.currentViewWidth, Screen.height);
		}

		public static bool FoundToolTip()
		{
			return (currentState.currentToolTip != null);
		}

        /*
		public static void HandleAreaOffset()
		{
			HandleAreaOffset(Vector2.zero);
		}
		
		public static void HandleAreaOffset(Vector2 scrollPos)
		{
			if (currentState == null)
				return;

			if (currentState.currentToolTip == null)
				return;

			var rect = GUILayoutUtility.GetLastRect();

			currentState.rectOffset.x += rect.x - scrollPos.x;
			currentState.rectOffset.y += rect.y - scrollPos.y;
		}*/

        static void UpdateToolTip(bool getLastRect = false, bool goUp = false)
        {
            if (currentState == null)
				return;


            //Debug.Log("!");
            if (currentState.prevToolTip != currentState.currentToolTip)
			{
				currentState.prevToolTip = currentState.currentToolTip;
				currentState.tooltipTimer = Time.realtimeSinceStartup;
				currentState.currentToolTipTitle = null;
				currentState.currentToolTipContents = null;
				currentState.currentToolTipKeyCodes = null;
			}

			if (currentState.currentToolTip == null)
				return;

			//if ((Time.realtimeSinceStartup - currentState.tooltipTimer) < TooltipWaitTime)
			//	return;
			
			var titleStyle = GUIStyleUtility.toolTipTitleStyle;
			var contentsStyle = GUIStyleUtility.toolTipContentsStyle;
			var keycodesStyle = GUIStyleUtility.toolTipKeycodesStyle;
			if (currentState.currentToolTipTitle == null)
			{
				var currentToolTipTitleString		= currentState.currentToolTip.TitleString();
				var currentToolTipContentsString	= currentState.currentToolTip.ContentsString();
				var currentToolTipKeyCodesString	= currentState.currentToolTip.KeyString();

				currentState.currentToolTipTitle    = new GUIContent(currentToolTipTitleString);
				currentState.currentToolTipContents = new GUIContent(currentToolTipContentsString);
				if (string.IsNullOrEmpty(currentToolTipKeyCodesString))
					currentState.currentToolTipKeyCodes = null;
				else
					currentState.currentToolTipKeyCodes = new GUIContent(currentToolTipKeyCodesString);
				
				var currentToolTipTitleSize		= titleStyle.CalcSize(currentState.currentToolTipTitle);
				var currentToolTipContentsSize	= contentsStyle.CalcSize(currentState.currentToolTipContents);
				var currentToolTipKeyCodesSize	= currentState.currentToolTipKeyCodes != null ? keycodesStyle.CalcSize(currentState.currentToolTipKeyCodes) : Vector2.zero;


				var currentArea = getLastRect ? GUILayoutUtility.GetLastRect() : currentState.maxArea;

				Vector2 size = currentToolTipTitleSize;
				size.x = Mathf.Max(currentToolTipTitleSize.x, currentToolTipContentsSize.x, currentToolTipKeyCodesSize.x);
				size.y = currentToolTipTitleSize.y + currentToolTipContentsSize.y + currentToolTipKeyCodesSize.y;
				currentState.currentToolTipArea = GUIUtility.ScreenToGUIRect(currentState.tooltipArea);
				//currentState.currentToolTipArea.x += currentState.rectOffset.x;
				//currentState.currentToolTipArea.y += currentState.rectOffset.y;
				//currentToolTipArea.x = (currentToolTipArea.x + currentToolTipArea.width) - size.x;
				currentState.currentToolTipArea.width = size.x;
				
				if (currentState.currentToolTipArea.xMax > currentArea.xMax)
				{
					currentState.currentToolTipArea.xMin -= currentState.currentToolTipArea.xMax - currentArea.xMax;
				}
				if (currentState.currentToolTipArea.xMin < 0)
				{
					currentState.currentToolTipArea.xMin = 0;
					currentState.currentToolTipArea.width = currentArea.xMax;
				}

				var currentToolTipTitleHeight		= titleStyle.CalcHeight(currentState.currentToolTipTitle, currentState.currentToolTipArea.width);
				var currentToolTipContentsHeight	= contentsStyle.CalcHeight(currentState.currentToolTipContents, currentState.currentToolTipArea.width);
				var currentToolTipKeyCodesHeight	= (currentState.currentToolTipKeyCodes != null ? keycodesStyle.CalcHeight(currentState.currentToolTipKeyCodes, currentState.currentToolTipArea.width) : 0);
				
				currentState.currentToolTipArea.height = currentToolTipTitleHeight + currentToolTipContentsHeight + currentToolTipKeyCodesHeight + 4;

				if (goUp)
				{
					if (currentState.currentToolTipArea.y - currentState.tooltipArea.height < 0)
					{
						currentState.currentToolTipArea.y += currentState.tooltipArea.height;
					} else
					{
						currentState.currentToolTipArea.y -= currentState.currentToolTipArea.height;
					}
				} else
				{
					if (currentState.currentToolTipArea.yMax + currentState.tooltipArea.height < currentArea.yMax)
					{
						currentState.currentToolTipArea.y += currentState.tooltipArea.height;
					} else
					{
						currentState.currentToolTipArea.y -= currentState.currentToolTipArea.height;
					}
				}
				if (currentState.currentToolTipArea.y < 0)
					currentState.currentToolTipArea.y = 0;

				
				currentState.currentToolTipTitleArea.x = currentState.currentToolTipArea.x;
				currentState.currentToolTipTitleArea.width = currentState.currentToolTipArea.width;
				
				currentState.currentToolTipContentsArea.x = currentState.currentToolTipArea.x;
				currentState.currentToolTipContentsArea.width = currentState.currentToolTipArea.width;
				
				currentState.currentToolTipKeyCodesArea.x = currentState.currentToolTipArea.x;
				currentState.currentToolTipKeyCodesArea.width = currentState.currentToolTipArea.width;
				
				currentState.currentToolTipTitleArea.height = currentToolTipTitleHeight;
				currentState.currentToolTipContentsArea.height = currentToolTipContentsHeight;
				currentState.currentToolTipKeyCodesArea.height = currentToolTipKeyCodesHeight;

				currentState.currentToolTipTitleArea.y = currentState.currentToolTipArea.y;
				currentState.currentToolTipContentsArea.y = currentState.currentToolTipTitleArea.y + currentState.currentToolTipTitleArea.height;
				currentState.currentToolTipKeyCodesArea.y = currentState.currentToolTipContentsArea.y + currentState.currentToolTipContentsArea.height;

				if (currentState.currentToolTipKeyCodes != null)
					currentState.currentToolTipKeyCodesArea.height += 4;
				else
					currentState.currentToolTipContentsArea.height += 4;
			}
        }

		public static void DrawToolTip(bool getLastRect = true, bool goUp = false)
        {
            if (currentState == null)
                return;

			if (Event.current.type != EventType.Repaint/* ||
                currentState.drawnThisFrame*/)
			{
				if (currentState.prevToolTip != currentState.currentToolTip &&
					currentState.editor != null)
					currentState.editor.Repaint();
				return;
			}
            
            if (currentState.currentToolTip == null)
                return;
            
            UpdateToolTip(getLastRect, goUp);
            
            var titleStyle      = GUIStyleUtility.toolTipTitleStyle;
            var contentsStyle   = GUIStyleUtility.toolTipContentsStyle;
            var keycodesStyle   = GUIStyleUtility.toolTipKeycodesStyle;

            var prevBackgroundColor = GUI.backgroundColor;
            var prevColor = GUI.color;
            var prevDepth = GUI.depth;
            GUI.backgroundColor = Color.black;
            GUI.color = Color.white;
            //GUI.depth = -100000;
            
            EditorGUI.LabelField(currentState.currentToolTipTitleArea, currentState.currentToolTipTitle, titleStyle);
            EditorGUI.LabelField(currentState.currentToolTipContentsArea, currentState.currentToolTipContents, contentsStyle);
            if (currentState.currentToolTipKeyCodes != null)
                EditorGUI.LabelField(currentState.currentToolTipKeyCodesArea, currentState.currentToolTipKeyCodes, keycodesStyle);
            GUI.backgroundColor = prevBackgroundColor;
            GUI.color = prevColor;
            GUI.depth = prevDepth;
        }


        public static void SetToolTip(ToolTip tooltip)
        {
            if (tooltip == null || currentState == null)
                return;
            SetToolTip(tooltip, GUILayoutUtility.GetLastRect());
        }

        public static Rect GUIToScreenRect(Rect rect)
        {
            var min = GUIUtility.GUIToScreenPoint(new Vector2(rect.xMin, rect.yMin));
            var max = GUIUtility.GUIToScreenPoint(new Vector2(rect.xMax, rect.yMax));
            return new Rect(min, max - min);
        }

        public static void SetToolTip(ToolTip tooltip, Rect currentArea)
        {
            if (tooltip == null || currentState == null)
				return;

			if (currentArea.Contains(Event.current.mousePosition) &&
				(EditorWindow.mouseOverWindow == currentState.editorWindow || 
				currentState.editorWindow == null))
            {
				var newArea = GUIToScreenRect(currentArea);
				if (currentState.prevToolTip == tooltip &&
					currentState.tooltipArea != newArea)
				{
					currentState.prevToolTip = null;
				}
                currentState.tooltipArea = newArea;
				currentState.currentToolTip = tooltip;
			}
		}
	}
}
