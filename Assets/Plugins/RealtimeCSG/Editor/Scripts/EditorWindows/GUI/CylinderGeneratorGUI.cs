using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class CylinderGeneratorGUI
	{
		private static readonly GUIContent	CommitContent				= new GUIContent("Commit");
		private static readonly	GUIContent	CancelContent				= new GUIContent("Cancel");
		private static readonly ToolTip		CommitTooltip				= new ToolTip("Generate your brush", "Create the brush from the current cylinder shape.", Keys.PerformActionKey);
		private static readonly ToolTip		CancelTooltip				= new ToolTip("Cancel brush creation", "Do not generate the brush.", Keys.CancelActionKey);

		private static readonly GUIContent	SmoothShadingContent		= new GUIContent("Smooth shading");
		private static readonly ToolTip		SmoothShadingTooltip		= new ToolTip("Smooth shading", "Toggle if you want the sides of the cylinder have smooth lighting or have a faceted look.");
		private static readonly GUIContent	SingleSurfaceEndsContent	= new GUIContent("Single surface ends");
		private static readonly ToolTip		SingleSurfaceEndsTooltip	= new ToolTip("Single surface ends", "Toggle if you want the top and bottom of the cylinder be a single polygon, or have a triangle per side.");
		private static readonly GUIContent	AlignToSideContent			= new GUIContent("Align to straight side");
		private static readonly ToolTip		AlignToSideTooltip			= new ToolTip("Align to straight side", "Toggle if you want the cylinder to begin in the center of a side, or at a point.");
		private static readonly GUIContent	FitShapeContent				= new GUIContent("Fit shape");
		private static readonly ToolTip		FitShapeTooltip				= new ToolTip("Fit shape", "Toggle if you want the cylinder to be fitted to the square that encapsulates the full circle that's defined by its radius. This makes the shapes more predictable when they have only a few sides, but it may change its shape slightly.");
		private static readonly GUIContent	OffsetContent				= new GUIContent("Angle");
		private static readonly ToolTip		OffsetTooltip				= new ToolTip("Offset angle", "Set the offset angle at which the cylinder starts.");
		private static readonly GUIContent	SidesContent				= new GUIContent("Sides");
		private static readonly ToolTip		SidesTooltip				= new ToolTip("Number of sides", "Set the number of sides the cylinder has when generated.");
		private static readonly GUIContent	HeightContent				= new GUIContent("Height");
		private static readonly ToolTip		HeightTooltip				= new ToolTip("Height", "Set the height of the cylinder.");
		private static readonly GUIContent	RadiusContent				= new GUIContent("Radius");
		private static readonly ToolTip		RadiusTooltip				= new ToolTip("Radius", "Set the radius of the cylinder. The radius is half of the width of a cylinder.");
		
		private static readonly GUILayoutOption width20					= GUILayout.Width(25);
		private static readonly GUILayoutOption width65					= GUILayout.Width(65);
		private static readonly GUILayoutOption width100				= GUILayout.Width(100);
		private static readonly GUILayoutOption width140				= GUILayout.Width(140);
		private static readonly GUILayoutOption width200				= GUILayout.Width(200);

		static bool SettingsToggle(bool value, GUIContent content, bool isSceneGUI)
		{
			if (isSceneGUI)
				return EditorGUILayout.ToggleLeft(content, value, width140);
			else
				return EditorGUILayout.Toggle(content, value);
		}

		static float SettingsSlider(float value, float minValue, float maxValue, GUIContent content, bool isSceneGUI)
		{
			if (isSceneGUI)
			{
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.Slider(value, minValue, maxValue, width200);
				GUILayout.EndHorizontal();
				return result;
			} else
			{
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.Slider(value, minValue, maxValue);
				GUILayout.EndHorizontal();
				return result;
			}
		}

		static int IntSettingsSlider(int value, int minValue, int maxValue, GUIContent content, bool isSceneGUI)
		{
			if (isSceneGUI)
			{
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.IntSlider(value, minValue, maxValue, width200);
				GUILayout.EndHorizontal();
				return result;
			} else
			{
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				GUILayout.Label(content, width65);
				var result = EditorGUILayout.IntSlider(value, minValue, maxValue);
				GUILayout.EndHorizontal();
				return result;
			}
		}

		static void CylinderSettingsGUI(CylinderGenerator generator, bool isSceneGUI)
		{
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				generator.CircleSmoothShading		= SettingsToggle(generator.CircleSmoothShading,		SmoothShadingContent,		isSceneGUI);
				TooltipUtility.SetToolTip(SmoothShadingTooltip);
				generator.CircleSingleSurfaceEnds	= SettingsToggle(generator.CircleSingleSurfaceEnds, SingleSurfaceEndsContent,	isSceneGUI);
				TooltipUtility.SetToolTip(SingleSurfaceEndsTooltip);
				generator.CircleDistanceToSide		= SettingsToggle(generator.CircleDistanceToSide,	AlignToSideContent,			isSceneGUI);
				TooltipUtility.SetToolTip(AlignToSideTooltip);
				generator.CircleRecenter			= SettingsToggle(generator.CircleRecenter,			FitShapeContent,			isSceneGUI);
				TooltipUtility.SetToolTip(FitShapeTooltip);
			}
			GUILayout.EndVertical();
		}

		static void OnGUIContents(CylinderGenerator generator, bool isSceneGUI)
		{
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				bool enabled = generator.HaveBrushes;
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				{
					EditorGUI.BeginDisabledGroup(!enabled);
					{
						if (isSceneGUI)
							GUILayout.BeginVertical(GUI.skin.box, width100);
						else
							GUILayout.BeginVertical(GUIStyle.none);
						{
							bool mixedValues = !enabled;
							CSGOperationType operation = generator.CurrentCSGOperationType;
							EditorGUI.BeginChangeCheck();
							operation = GUIStyleUtility.ChooseOperation(operation, mixedValues);
							if (EditorGUI.EndChangeCheck())
							{
								generator.CurrentCSGOperationType = operation;
							}
						}
						GUILayout.EndVertical();
					}
					EditorGUI.EndDisabledGroup();

					if (isSceneGUI)
						CylinderSettingsGUI(generator, isSceneGUI);
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5);

				GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
				{
					var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
					var nextUnit = Units.CycleToNextUnit(distanceUnit);
					var unitText = Units.GetUnitGUIContent(distanceUnit);
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(HeightContent, width65);
						if (isSceneGUI)
							TooltipUtility.SetToolTip(HeightTooltip);
						var height = generator.HaveHeight ? generator.Height : generator.DefaultHeight;
						EditorGUI.BeginChangeCheck();
						{
							if (!isSceneGUI)
								height = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height)));
							else
								height = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height), width65));
						}
						if (EditorGUI.EndChangeCheck())
						{
							if (generator.HaveHeight)
								generator.Height = height;
							else
								generator.DefaultHeight = height;
						}
						if (GUILayout.Button(unitText, EditorStyles.miniLabel, width20))
						{
							distanceUnit = nextUnit;
							RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
							RealtimeCSG.CSGSettings.UpdateSnapSettings();
							RealtimeCSG.CSGSettings.Save();
							SceneView.RepaintAll();
						}
					}
					if (!isSceneGUI)
					{
						GUILayout.EndHorizontal();
						TooltipUtility.SetToolTip(HeightTooltip);
						GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					}
					else
					{
						GUILayout.Space(12);
					}
					{
						EditorGUI.BeginDisabledGroup(!generator.CanCommit);
						{
							GUILayout.Label(RadiusContent, width65);
							if (isSceneGUI)
								TooltipUtility.SetToolTip(RadiusTooltip);
							var radius = generator.RadiusA;
							EditorGUI.BeginChangeCheck();
							{
								if (!isSceneGUI)
									radius = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius)));
								else
									radius = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius), width65));
							}
							if (EditorGUI.EndChangeCheck())
							{
								generator.RadiusA = radius;
							}
							if (GUILayout.Button(unitText, EditorStyles.miniLabel, width20))
							{
								distanceUnit = nextUnit;
								RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
								RealtimeCSG.CSGSettings.UpdateSnapSettings();
								RealtimeCSG.CSGSettings.Save();
								SceneView.RepaintAll();
							}
						}
						EditorGUI.EndDisabledGroup();
					}
					GUILayout.EndHorizontal();
					if (!isSceneGUI)
						TooltipUtility.SetToolTip(RadiusTooltip);
				}
				GUILayout.EndVertical();

				{
					generator.CircleSides = IntSettingsSlider(generator.CircleSides, 3, 144, SidesContent, isSceneGUI);
					TooltipUtility.SetToolTip(SidesTooltip);
				}
				{
					generator.CircleOffset = SettingsSlider(generator.CircleOffset, 0, 360, OffsetContent, isSceneGUI);
					TooltipUtility.SetToolTip(OffsetTooltip);
				}



				if (!isSceneGUI)
				{
					GUILayout.Space(5);

					CylinderSettingsGUI(generator, isSceneGUI);

					GUILayout.Space(10);
				} else
				{
					GUILayout.Space(10);
				}

				EditorGUI.BeginDisabledGroup(!generator.CanCommit);
				{ 
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						if (GUILayout.Button(CommitContent)) { generator.DoCommit(); }
						TooltipUtility.SetToolTip(CommitTooltip);
						if (GUILayout.Button(CancelContent)) { generator.DoCancel(); }
						TooltipUtility.SetToolTip(CancelTooltip);
					}
					GUILayout.EndHorizontal();
				}
				EditorGUI.EndDisabledGroup();
			}
			GUILayout.EndVertical();
		}
		
		public static bool OnShowGUI(CylinderGenerator generator, bool isSceneGUI)
		{
			GUIStyleUtility.InitStyles();
			OnGUIContents(generator, isSceneGUI);
			return true;
		}
	}
}
