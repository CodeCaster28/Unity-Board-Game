using System.Globalization;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using System;
using System.Linq;

namespace RealtimeCSG
{
	internal sealed class BottomBarGUI
	{
		static int BottomBarEditorOverlayHash = "BottomBarEditorOverlay".GetHashCode();

		static ToolTip showGridTooltip		= new ToolTip("Show the grid", "Click this to toggle between showing the grid or hiding it.\nWhen hidden you can still snap against it.", Keys.ToggleShowGridKey);
		static ToolTip snapToGridTooltip	= new ToolTip("Toggle snapping", "Click this if you want to turn snapping your movements on or off.", Keys.ToggleSnappingKey);
		static ToolTip showWireframeTooltip	= new ToolTip("Toggle wireframe", "Click this to switch between showing the\nwireframe of your scene or the regular view.");
		static ToolTip rebuildTooltip		= new ToolTip("Rebuild your CSG meshes", "Click this to rebuild your CSG meshes\nin case something didn't go quite right.");
		
		static GUIContent xLabel			= new GUIContent("X");
		static GUIContent yLabel			= new GUIContent("Y");
		static GUIContent zLabel			= new GUIContent("Z");
		
		static ToolTip xTooltipOff			= new ToolTip("Lock X axis", "Click to disable movement on the X axis");
		static ToolTip yTooltipOff			= new ToolTip("Lock Y axis", "Click to disable movement on the Y axis");
		static ToolTip zTooltipOff			= new ToolTip("Lock Z axis", "Click to disable movement on the Z axis");
		static ToolTip xTooltipOn			= new ToolTip("Unlock X axis", "Click to enable movement on the X axis");
		static ToolTip yTooltipOn			= new ToolTip("Unlock Y axis", "Click to enable movement on the Y axis");
		static ToolTip zTooltipOn			= new ToolTip("Unlock Z axis", "Click to enable movement on the Z axis");
		
		static GUIContent positionLargeLabel	= new GUIContent("position");
		static GUIContent positionSmallLabel	= new GUIContent("pos");
		static GUIContent positionPlusLabel		= new GUIContent("+");
		static GUIContent positionMinusLabel	= new GUIContent("-");
		
		static ToolTip positionTooltip			= new ToolTip("Grid size", "Here you can set the size of the grid. Click this\nto switch between setting the grid size for X Y Z\nseparately, or for all of them uniformly.");
		static ToolTip positionPlusTooltip		= new ToolTip("Double grid size", "Multiply the grid size by 2", Keys.DoubleGridSizeKey);
		static ToolTip positionMinnusTooltip	= new ToolTip("Half grid size", "Divide the grid size by 2", Keys.HalfGridSizeKey);
		
		static GUIContent scaleLargeLabel		= new GUIContent("scale");
		static GUIContent scaleSmallLabel		= new GUIContent("scl");
		static GUIContent scalePlusLabel		= new GUIContent("+");
		static GUIContent scaleMinusLabel		= new GUIContent("-");
		static GUIContent scaleUnitLabel		= new GUIContent("%");
		
		static ToolTip scaleTooltip				= new ToolTip("Angle snapping", "Here you can set rotational snapping.");
		static ToolTip scalePlusTooltip			= new ToolTip("Double angle snapping", "Multiply the rotational snapping by 2");
		static ToolTip scaleMinnusTooltip		= new ToolTip("Half angle snapping", "Divide the rotational snapping by 2");

		static GUIContent angleLargeLabel		= new GUIContent("angle");
		static GUIContent angleSmallLabel		= new GUIContent("ang");
		static GUIContent anglePlusLabel		= new GUIContent("+");
		static GUIContent angleMinusLabel		= new GUIContent("-");
		static GUIContent angleUnitLabel		= new GUIContent("°");
		
		static ToolTip angleTooltip				= new ToolTip("Angle snapping", "Here you can set rotational snapping.");
		static ToolTip anglePlusTooltip			= new ToolTip("Double angle snapping", "Multiply the rotational snapping by 2");
		static ToolTip angleMinnusTooltip		= new ToolTip("Half angle snapping", "Divide the rotational snapping by 2");


		static GUILayoutOption EnumMaxWidth		= GUILayout.MaxWidth(165);
		static GUILayoutOption EnumMinWidth		= GUILayout.MinWidth(20);
		static GUILayoutOption MinSnapWidth		= GUILayout.MinWidth(30);
		static GUILayoutOption MaxSnapWidth		= GUILayout.MaxWidth(70);

		static GUIStyle			miniTextStyle;
		static GUIStyle			textInputStyle;

		static bool localStyles = false;

		static int BottomBarGUIHash = "BottomBarGUI".GetHashCode();

		private static string[] helperSurfaceFlagStrings;

		public static void ShowGUI(SceneView sceneView, bool haveOffset = true)
		{
			if (!localStyles)
			{
				miniTextStyle = new GUIStyle(EditorStyles.miniLabel);
				miniTextStyle.contentOffset = new Vector2(0, -1);
				textInputStyle = new GUIStyle(EditorStyles.miniTextField);
				textInputStyle.padding.top--;
				textInputStyle.margin.top+=2;
				localStyles = true;
				helperSurfaceFlagStrings = Enum.GetNames(typeof(HelperSurfaceFlags)).Select(x => ObjectNames.NicifyVariableName(x)).ToArray();
			}
			GUIStyleUtility.InitStyles();
			if (sceneView != null)
			{
				float height	= sceneView.position.height;//Screen.height;
				float width		= sceneView.position.width;//Screen.width;
				Rect bottomBarRect;
				if (haveOffset)
				{
#if UNITY_5_5_OR_NEWER
					bottomBarRect = new Rect(0, height - (GUIStyleUtility.BottomToolBarHeight + 18), 
											  width, GUIStyleUtility.BottomToolBarHeight);
#else
					bottomBarRect = new Rect(0, height - (GUIStyleUtility.BottomToolBarHeight + SceneView.kToolbarHeight + 1),
												width, GUIStyleUtility.BottomToolBarHeight);
#endif
				} else
					bottomBarRect = new Rect(0, height - (GUIStyleUtility.BottomToolBarHeight + 1), width, GUIStyleUtility.BottomToolBarHeight);

				try
				{ 
					Handles.BeginGUI();
					
					GUILayout.BeginArea(bottomBarRect, GUIStyleUtility.BottomToolBarStyle);
					OnBottomBarGUI(sceneView);
					GUILayout.EndArea();
					
					int controlID = GUIUtility.GetControlID(BottomBarGUIHash, FocusType.Keyboard, bottomBarRect);
					var type = Event.current.GetTypeForControl(controlID);
					//Debug.Log(controlID + " " + GUIUtility.hotControl + " " + type + " " + bottomBarRect.Contains(Event.current.mousePosition));
					switch (type)
					{
						case EventType.MouseDown: { if (bottomBarRect.Contains(Event.current.mousePosition)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
						case EventType.MouseMove: { if (bottomBarRect.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
						case EventType.MouseUp:   { if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
						case EventType.MouseDrag: { if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
						case EventType.ScrollWheel: { if (bottomBarRect.Contains(Event.current.mousePosition)) { Event.current.Use(); } break; }
					}

					//TooltipUtility.HandleAreaOffset(new Vector2(-bottomBarRect.xMin, -bottomBarRect.yMin));
				}
				finally
				{
					Handles.EndGUI();
				}
			}
		}

		static void OnBottomBarGUI(SceneView sceneView)
		{
			var snapToGrid		= RealtimeCSG.CSGSettings.SnapToGrid;
			var uniformGrid		= RealtimeCSG.CSGSettings.UniformGrid;
			var moveSnapVector  = RealtimeCSG.CSGSettings.SnapVector;
			var rotationSnap	= RealtimeCSG.CSGSettings.SnapRotation;
			var scaleSnap		= RealtimeCSG.CSGSettings.SnapScale;
			var showGrid		= RealtimeCSG.CSGSettings.GridVisible;
			var lockAxisX		= RealtimeCSG.CSGSettings.LockAxisX;
			var lockAxisY		= RealtimeCSG.CSGSettings.LockAxisY;
			var lockAxisZ		= RealtimeCSG.CSGSettings.LockAxisZ;
			var distanceUnit	= RealtimeCSG.CSGSettings.DistanceUnit;
			var helperSurfaces  = RealtimeCSG.CSGSettings.VisibleHelperSurfaces;
			var showWireframe	= RealtimeCSG.CSGSettings.IsWireframeShown(sceneView);
			var updateSurfaces	= false;
			bool wireframeModified = false;

			var viewWidth = sceneView.position.width;

			bool modified = false;
			EditorGUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
			{				
				EditorGUI.BeginChangeCheck();
				{
					var skin = GUIStyleUtility.Skin;
					if (showGrid)
					{
						showGrid = GUILayout.Toggle(showGrid, skin.gridIconOn, EditorStyles.toolbarButton);
					} else
					{
						showGrid = GUILayout.Toggle(showGrid, skin.gridIcon,   EditorStyles.toolbarButton);
					}
					TooltipUtility.SetToolTip(showGridTooltip);

					if (viewWidth >= 800)
					{
						EditorGUILayout.Space();
					}

					if (viewWidth >= 200)
					{
						var prevBackgroundColor = GUI.backgroundColor;
						var lockedBackgroundColor = skin.lockedBackgroundColor;
						if (lockAxisX)
							GUI.backgroundColor = lockedBackgroundColor;
						lockAxisX = !GUILayout.Toggle(!lockAxisX, xLabel, skin.xToolbarButton);
						if (lockAxisX)
							TooltipUtility.SetToolTip(xTooltipOn);
						else
							TooltipUtility.SetToolTip(xTooltipOff);
						GUI.backgroundColor = prevBackgroundColor;

						if (lockAxisY)
							GUI.backgroundColor = lockedBackgroundColor;
						lockAxisY = !GUILayout.Toggle(!lockAxisY, yLabel, skin.yToolbarButton);
						if (lockAxisY)
							TooltipUtility.SetToolTip(yTooltipOn);
						else
							TooltipUtility.SetToolTip(yTooltipOff);
						GUI.backgroundColor = prevBackgroundColor;

						if (lockAxisZ)
							GUI.backgroundColor = lockedBackgroundColor;
						lockAxisZ = !GUILayout.Toggle(!lockAxisZ, zLabel, skin.zToolbarButton);
						if (lockAxisZ)
							TooltipUtility.SetToolTip(zTooltipOn);
						else
							TooltipUtility.SetToolTip(zTooltipOff);
						GUI.backgroundColor = prevBackgroundColor;
					}
				}
				modified = EditorGUI.EndChangeCheck() || modified;

				if (viewWidth >= 800)
				{
					EditorGUILayout.Space();
				}

				EditorGUI.BeginChangeCheck();
				{
					if (viewWidth >= 475)
					{ 
						if (snapToGrid && viewWidth >= 310)
						{
							snapToGrid = GUILayout.Toggle(snapToGrid, GUIStyleUtility.Skin.snappingIconOn, EditorStyles.toolbarButton);
							TooltipUtility.SetToolTip(snapToGridTooltip);
							if (viewWidth >= 865)
								uniformGrid = GUILayout.Toggle(uniformGrid, positionLargeLabel, miniTextStyle);
							else
								uniformGrid = GUILayout.Toggle(uniformGrid, positionSmallLabel, miniTextStyle);
							TooltipUtility.SetToolTip(positionTooltip);
						}
						else
						{
							snapToGrid = GUILayout.Toggle(snapToGrid, GUIStyleUtility.Skin.snappingIcon, EditorStyles.toolbarButton);
							TooltipUtility.SetToolTip(snapToGridTooltip);
						}
					}
				}
				modified = EditorGUI.EndChangeCheck() || modified;
				if (viewWidth >= 310)
				{
					if (snapToGrid)
					{
						if (uniformGrid || viewWidth < 515)
						{
							EditorGUI.showMixedValue = !(moveSnapVector.x == moveSnapVector.y && moveSnapVector.x == moveSnapVector.z);
							EditorGUI.BeginChangeCheck();
							{
								moveSnapVector.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.x), textInputStyle, MinSnapWidth, MaxSnapWidth));
							}
							if (EditorGUI.EndChangeCheck())
							{
								modified = true;
								moveSnapVector.y = moveSnapVector.x;
								moveSnapVector.z = moveSnapVector.x;
							}
							EditorGUI.showMixedValue = false;
						} else
						{
							EditorGUI.BeginChangeCheck();
							{
								moveSnapVector.x = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.x), textInputStyle, MinSnapWidth, MaxSnapWidth));
								moveSnapVector.y = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.y), textInputStyle, MinSnapWidth, MaxSnapWidth));
								moveSnapVector.z = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, moveSnapVector.z), textInputStyle, MinSnapWidth, MaxSnapWidth));
							}
							modified = EditorGUI.EndChangeCheck() || modified;
						}
						DistanceUnit nextUnit = Units.CycleToNextUnit(distanceUnit);
						GUIContent   unitText = Units.GetUnitGUIContent(distanceUnit);
						if (GUILayout.Button(unitText, miniTextStyle))
						{
							distanceUnit = nextUnit;
							modified = true;
						}
						if (viewWidth >= 700)
						{
							if (GUILayout.Button(positionPlusLabel,  EditorStyles.miniButtonLeft))  { GridUtility.DoubleGridSize(); moveSnapVector = RealtimeCSG.CSGSettings.SnapVector; }
							TooltipUtility.SetToolTip(positionPlusTooltip);
							if (GUILayout.Button(positionMinusLabel, EditorStyles.miniButtonRight)) { GridUtility.HalfGridSize(); moveSnapVector = RealtimeCSG.CSGSettings.SnapVector; }
							TooltipUtility.SetToolTip(positionMinnusTooltip);
						}
						if (viewWidth >= 750)
						{
							if (viewWidth >= 865)
								GUILayout.Label(angleLargeLabel, miniTextStyle);
							else
								GUILayout.Label(angleSmallLabel, miniTextStyle);
							TooltipUtility.SetToolTip(angleTooltip);
						}
						EditorGUI.BeginChangeCheck();
						{
							rotationSnap = EditorGUILayout.FloatField(rotationSnap, textInputStyle, MinSnapWidth, MaxSnapWidth);
							if (viewWidth <= 750)
								TooltipUtility.SetToolTip(angleTooltip);
						}
						modified = EditorGUI.EndChangeCheck() || modified;

						if (viewWidth >= 370)
							GUILayout.Label(angleUnitLabel, miniTextStyle);
						if (viewWidth >= 700)
						{
							if (GUILayout.Button(anglePlusLabel, EditorStyles.miniButtonLeft)) { rotationSnap *= 2.0f; modified = true; }
							TooltipUtility.SetToolTip(anglePlusTooltip);
							if (GUILayout.Button(angleMinusLabel, EditorStyles.miniButtonRight)) { rotationSnap /= 2.0f; modified = true; }
							TooltipUtility.SetToolTip(angleMinnusTooltip);
						}
						
						if (viewWidth >= 750)
						{
							if (viewWidth >= 865)
								GUILayout.Label(scaleLargeLabel, miniTextStyle);
							else
								GUILayout.Label(scaleSmallLabel, miniTextStyle);
							TooltipUtility.SetToolTip(scaleTooltip);
						}
						EditorGUI.BeginChangeCheck();
						{
							scaleSnap = EditorGUILayout.FloatField(scaleSnap, textInputStyle, MinSnapWidth, MaxSnapWidth);
							if (viewWidth <= 750)
								TooltipUtility.SetToolTip(scaleTooltip);
						}
						modified = EditorGUI.EndChangeCheck() || modified;
						if (viewWidth >= 370)
							GUILayout.Label(scaleUnitLabel, miniTextStyle);
						if (viewWidth >= 700)
						{
							if (GUILayout.Button(scalePlusLabel, EditorStyles.miniButtonLeft)) { scaleSnap *= 10.0f; modified = true; }
							TooltipUtility.SetToolTip(scalePlusTooltip);
							if (GUILayout.Button(scaleMinusLabel, EditorStyles.miniButtonRight)) { scaleSnap /= 10.0f; modified = true; }
							TooltipUtility.SetToolTip(scaleMinnusTooltip);
						}

					}
				}

				if (viewWidth >= 750)
				{
					GUILayout.FlexibleSpace();
				}
				EditorGUI.BeginChangeCheck();
				if (showWireframe)
				{
					showWireframe = GUILayout.Toggle(showWireframe, GUIStyleUtility.Skin.wireframe, EditorStyles.toolbarButton);
				} else
				{
					showWireframe = GUILayout.Toggle(showWireframe, GUIStyleUtility.Skin.wireframeOn, EditorStyles.toolbarButton);
				}
				TooltipUtility.SetToolTip(showWireframeTooltip);
				if (EditorGUI.EndChangeCheck())
				{
					wireframeModified = true;
					modified = true;
				}

				if (viewWidth >= 250)
				{
					EditorGUI.BeginChangeCheck();
					{
						helperSurfaces = (HelperSurfaceFlags)EditorGUILayout.MaskField((int)helperSurfaces, helperSurfaceFlagStrings, EnumMaxWidth, EnumMinWidth);
					}
					if (EditorGUI.EndChangeCheck())
					{
						updateSurfaces = true;
						modified = true;
					}
				}

				if (viewWidth >= 800)
					EditorGUILayout.Space ();
				
				if (GUILayout.Button(GUIStyleUtility.Skin.rebuildIcon, EditorStyles.toolbarButton))
				{
					Debug.Log("Starting complete rebuild");
					InternalCSGModelManager.skipRefresh = true;
					RealtimeCSG.CSGSettings.Reload();
					SceneViewEventHandler.UpdateDefines();

					var startTime = EditorApplication.timeSinceStartup;
					InternalCSGModelManager.Rebuild();
					InternalCSGModelManager.OnHierarchyModified();
					var hierarchy_update_endTime = EditorApplication.timeSinceStartup;

					CSGBindings.RebuildAll();
					var csg_endTime = EditorApplication.timeSinceStartup;

					InternalCSGModelManager.UpdateMeshes();
					MeshInstanceManager.UpdateHelperSurfaceVisibility();
					var meshupdate_endTime = EditorApplication.timeSinceStartup;

					updateSurfaces = true;
					SceneViewEventHandler.ResetUpdateRoutine();
					RealtimeCSG.CSGSettings.Save();
					InternalCSGModelManager.skipRefresh = false;
					Debug.Log(string.Format(CultureInfo.InvariantCulture, 
							"Full hierarchy rebuild in {0:F} ms. Full CSG rebuild done in {1:F} ms. Mesh update done in {2:F} ms.", 
								(hierarchy_update_endTime - startTime) * 1000, 
								(csg_endTime - hierarchy_update_endTime) * 1000, 
								(meshupdate_endTime - csg_endTime) * 1000
							));
				}
				TooltipUtility.SetToolTip(rebuildTooltip);
			}
			EditorGUILayout.EndHorizontal();

			var mousePoint  = Event.current.mousePosition;
			var currentArea = GUILayoutUtility.GetLastRect();
			int controlID = GUIUtility.GetControlID(BottomBarEditorOverlayHash, FocusType.Passive, currentArea);
			switch (Event.current.GetTypeForControl(controlID))
			{
				case EventType.MouseDown:	{ if (currentArea.Contains(mousePoint)) { GUIUtility.hotControl = controlID; GUIUtility.keyboardControl = controlID; Event.current.Use(); } break; }
				case EventType.MouseMove:	{ if (currentArea.Contains(mousePoint)) { Event.current.Use(); } break; }
				case EventType.MouseUp:		{ if (GUIUtility.hotControl == controlID) { GUIUtility.hotControl = 0; GUIUtility.keyboardControl = 0; Event.current.Use(); } break; }
				case EventType.MouseDrag:	{ if (GUIUtility.hotControl == controlID) { Event.current.Use(); } break; }
				case EventType.ScrollWheel: { if (currentArea.Contains(mousePoint)) { Event.current.Use(); } break; }
			}

			rotationSnap = Mathf.Max(1.0f, Mathf.Abs((360 + (rotationSnap % 360))) % 360);
			moveSnapVector.x = Mathf.Max(1.0f / 1024.0f, moveSnapVector.x);
			moveSnapVector.y = Mathf.Max(1.0f / 1024.0f, moveSnapVector.y);
			moveSnapVector.z = Mathf.Max(1.0f / 1024.0f, moveSnapVector.z);
			
			scaleSnap = Mathf.Max(MathConstants.MinimumScale, scaleSnap);
						
			RealtimeCSG.CSGSettings.SnapToGrid				= snapToGrid;
			RealtimeCSG.CSGSettings.SnapVector				= moveSnapVector;
			RealtimeCSG.CSGSettings.SnapRotation			= rotationSnap;
			RealtimeCSG.CSGSettings.SnapScale				= scaleSnap;
			RealtimeCSG.CSGSettings.UniformGrid				= uniformGrid;
//			RealtimeCSG.Settings.SnapVertex					= vertexSnap;
			RealtimeCSG.CSGSettings.GridVisible				= showGrid;
			RealtimeCSG.CSGSettings.LockAxisX				= lockAxisX;
			RealtimeCSG.CSGSettings.LockAxisY				= lockAxisY;
			RealtimeCSG.CSGSettings.LockAxisZ				= lockAxisZ;
			RealtimeCSG.CSGSettings.DistanceUnit			= distanceUnit;
			RealtimeCSG.CSGSettings.VisibleHelperSurfaces	= helperSurfaces;

			if (wireframeModified)
			{
				RealtimeCSG.CSGSettings.SetWireframeShown(sceneView, showWireframe);
			}

			if (updateSurfaces)
			{
				MeshInstanceManager.UpdateHelperSurfaceVisibility();
			}
			
			if (modified)
			{
				GUI.changed = true;
				RealtimeCSG.CSGSettings.UpdateSnapSettings();
				RealtimeCSG.CSGSettings.Save();
				SceneView.RepaintAll();
			}
		}
		
	}
}
