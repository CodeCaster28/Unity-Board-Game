using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class SphereGeneratorGUI
	{
		private static readonly GUIContent	CommitContent				= new GUIContent("Commit");
		private static readonly	GUIContent	CancelContent				= new GUIContent("Cancel");
		private static readonly ToolTip		CommitTooltip				= new ToolTip("Generate your brush", "Create the brush from the current sphere shape.", Keys.PerformActionKey);
		private static readonly ToolTip		CancelTooltip				= new ToolTip("Cancel brush creation", "Do not generate the brush.", Keys.CancelActionKey);

		private static readonly GUIContent	SmoothShadingContent		= new GUIContent("Smooth shading");
		private static readonly ToolTip		SmoothShadingTooltip		= new ToolTip("Smooth shading", "Toggle if you want the sides of the sphere have smooth lighting or have a faceted look.");
		private static readonly GUIContent	HemiSphereContent			= new GUIContent("Hemisphere");
		private static readonly ToolTip		HemiSphereTooltip			= new ToolTip("Hemisphere", "When toggled, create a hemisphere instead of a sphere.");
		private static readonly GUIContent	OffsetContent				= new GUIContent("Offset");
		private static readonly ToolTip		OffsetTooltip				= new ToolTip("Offset angle", "Set the offset angle at which the cylinder starts.");
		private static readonly GUIContent	SplitsContent				= new GUIContent("Splits");
		private static readonly ToolTip		SplitsTooltip				= new ToolTip("Number of splits", "Set the number of times the sides of the spherical cube to be split.");
		private static readonly GUIContent	RadiusContent				= new GUIContent("Radius");
		private static readonly ToolTip		RadiusTooltip				= new ToolTip("Radius", "Set the radius of the cylinder. The radius is half of the width of a cylinder.");
		
		private static readonly GUILayoutOption width25					= GUILayout.Width(25);
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

		static void SphereSettingsGUI(SphereGenerator generator, bool isSceneGUI)
		{
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				generator.SphereSmoothShading		= SettingsToggle(generator.SphereSmoothShading,		SmoothShadingContent,		isSceneGUI);
				TooltipUtility.SetToolTip(SmoothShadingTooltip);
				generator.IsHemiSphere				= SettingsToggle(generator.IsHemiSphere,			HemiSphereContent,			isSceneGUI);
				TooltipUtility.SetToolTip(HemiSphereTooltip);
			}
			GUILayout.EndVertical();
		}

		static void OnGUIContents(SphereGenerator generator, bool isSceneGUI)
		{
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var nextUnit = Units.CycleToNextUnit(distanceUnit);
			var unitText = Units.GetUnitGUIContent(distanceUnit);
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
						SphereSettingsGUI(generator, isSceneGUI);
				}
				GUILayout.EndHorizontal();

				GUILayout.Space(5);

				GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
				{
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						EditorGUI.BeginDisabledGroup(!generator.CanCommit);
						{
							GUILayout.Label(RadiusContent, width65);
							if (isSceneGUI)
								TooltipUtility.SetToolTip(RadiusTooltip);
							var radius = generator.SphereRadius;
							EditorGUI.BeginChangeCheck();
							{
								if (!isSceneGUI)
									radius = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius)));
								else
									radius = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius), width65));
							}
							if (EditorGUI.EndChangeCheck())
							{
								generator.SphereRadius = radius; 
							}
							if (GUILayout.Button(unitText, EditorStyles.miniLabel, width25))
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

					{
						generator.SphereSplits		= IntSettingsSlider(generator.SphereSplits, 1, 9, SplitsContent, isSceneGUI);
						TooltipUtility.SetToolTip(SplitsTooltip);
					}
					{
						generator.SphereOffset		= SettingsSlider(generator.SphereOffset, 0, 360, OffsetContent, isSceneGUI);
						TooltipUtility.SetToolTip(OffsetTooltip);
					}

				}
				GUILayout.EndVertical();

				if (!isSceneGUI)
				{
					GUILayout.Space(5);

					SphereSettingsGUI(generator, isSceneGUI);

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
		
		public static bool OnShowGUI(SphereGenerator generator, bool isSceneGUI)
		{
			GUIStyleUtility.InitStyles();
			OnGUIContents(generator, isSceneGUI);
			return true;
		}
	}
}
