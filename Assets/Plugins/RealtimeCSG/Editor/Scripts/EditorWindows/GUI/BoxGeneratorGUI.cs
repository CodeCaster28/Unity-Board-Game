using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class BoxGeneratorGUI
	{
		private static readonly GUIContent	CommitContent		= new GUIContent("Commit");
		private static readonly GUIContent	CancelContent		= new GUIContent("Cancel");
		private static readonly ToolTip		CommitTooltip		= new ToolTip("Generate your brush", "Create the brush from the current box shape.", Keys.PerformActionKey);
		private static readonly ToolTip		CancelTooltip		= new ToolTip("Cancel brush creation", "Do not generate the brush.", Keys.CancelActionKey);

		private static readonly GUIContent	LengthContent		= new GUIContent("Length (X)");
		private static readonly ToolTip		LengthTooltip		= new ToolTip("Length (X)", "Set the length of the box, this is in the X direction.");
		private static readonly GUIContent	HeightContent		= new GUIContent("Height (Y)");
		private static readonly ToolTip		HeightTooltip		= new ToolTip("Height (Y)", "Set the height of the box, this is in the Y direction.");
		private static readonly GUIContent	WidthContent		= new GUIContent("Width (Z)");
		private static readonly ToolTip		WidthTooltip		= new ToolTip("Width (Z)", "Set the width of the box, this is in the Z direction.");
		
		private static readonly GUILayoutOption Width25			= GUILayout.Width(25);
		private static readonly GUILayoutOption Width65			= GUILayout.Width(65);
		private static readonly GUILayoutOption Width100		= GUILayout.Width(100);

		static void BoxSettingsGUI(BoxGenerator generator, bool isSceneGUI)
		{
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var nextUnit = Units.CycleToNextUnit(distanceUnit);
			var unitText = Units.GetUnitGUIContent(distanceUnit);

			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				{
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(HeightContent, Width65);
						var height = generator.HaveHeight ? generator.Height : generator.DefaultHeight;
						EditorGUI.BeginChangeCheck();
						{
							height = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height)));
						}
						if (EditorGUI.EndChangeCheck())
						{
							if (generator.HaveHeight)
								generator.Height = height;
							else
								generator.DefaultHeight = height;
						}
						if (GUILayout.Button(unitText, EditorStyles.miniLabel, Width25))
						{
							distanceUnit = nextUnit;
							RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
							RealtimeCSG.CSGSettings.UpdateSnapSettings();
							RealtimeCSG.CSGSettings.Save();
							SceneView.RepaintAll();
						}
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(HeightTooltip);
				}

				EditorGUI.BeginDisabledGroup(!generator.CanCommit);
				{
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(LengthContent, Width65);
						var length = generator.Length;
						EditorGUI.BeginChangeCheck();
						{
							length = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, length)));
						}
						if (EditorGUI.EndChangeCheck())
						{
							generator.Length = length;
						}
						if (GUILayout.Button(unitText, EditorStyles.miniLabel, Width25))
						{
							distanceUnit = nextUnit;
							RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
							RealtimeCSG.CSGSettings.UpdateSnapSettings();
							RealtimeCSG.CSGSettings.Save();
							SceneView.RepaintAll();
						}
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(LengthTooltip);
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(WidthContent, Width65);
						var width = generator.Width;
						EditorGUI.BeginChangeCheck();
						{
							width = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, width)));
						}
						if (EditorGUI.EndChangeCheck())
						{
							generator.Width = width;
						}
						if (GUILayout.Button(unitText, EditorStyles.miniLabel, Width25))
						{
							distanceUnit = nextUnit;
							RealtimeCSG.CSGSettings.DistanceUnit = distanceUnit;
							RealtimeCSG.CSGSettings.UpdateSnapSettings();
							RealtimeCSG.CSGSettings.Save();
							SceneView.RepaintAll();
						}
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(WidthTooltip);
				}
				EditorGUI.EndDisabledGroup();
			}
			GUILayout.EndVertical();
		}

		static void OnGUIContents(BoxGenerator generator, bool isSceneGUI)
		{
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				bool enabled = generator.HaveBrushes;
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				{
					EditorGUI.BeginDisabledGroup(!enabled);
					{
						if (isSceneGUI)
							GUILayout.BeginVertical(GUI.skin.box, Width100);
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
						BoxSettingsGUI(generator, isSceneGUI: true);
				}
				GUILayout.EndHorizontal();
				
				if (!isSceneGUI)
				{
					GUILayout.Space(5);

					BoxSettingsGUI(generator, isSceneGUI: false);

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
		
		public static bool OnShowGUI(BoxGenerator generator, bool isSceneGUI)
		{
			GUIStyleUtility.InitStyles();
			OnGUIContents(generator, isSceneGUI);
			return true;
		}
	}
}
