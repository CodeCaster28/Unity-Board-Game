using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class FreeDrawGeneratorGUI
	{
		private static readonly GUIContent	CommitContent			= new GUIContent("Commit");
		private static readonly GUIContent	CancelContent			= new GUIContent("Cancel");
		private static readonly ToolTip		CommitTooltip			= new ToolTip("Generate your brush", "Create the brush from the current shape.", Keys.PerformActionKey);
		private static readonly ToolTip		CancelTooltip			= new ToolTip("Cancel brush creation", "Do not generate the brush.", Keys.CancelActionKey);
		private static readonly GUIContent	HeightContent			= new GUIContent("Height");
		private static readonly ToolTip		HeightTooltip			= new ToolTip("Height", "Set the height of the shape.");
		private static readonly GUIContent	CurveSidesContent		= new GUIContent("Splits");
		private static readonly ToolTip		CurveSidesTooltip		= new ToolTip("Curve Splits", "How many sides should each edge that's turned into a curve have.");
		private static readonly GUIContent	EdgeTypeContent			= new GUIContent("Edge");
		private static readonly ToolTip		EdgeTypeTooltip			= new ToolTip("Edge Curve Type", "Set the current selected edges to be Straight, Broken Curve or an Aligned Curve.");
		private static readonly GUIContent	SplitSelectedContent	= new GUIContent("Insert point");
		private static readonly ToolTip		SplitSelectedTooltip	= new ToolTip("Insert point", "Insert a new point in the middle of each selected edge.", Keys.InsertPoint);
		
		
		private static readonly GUILayoutOption width20				= GUILayout.Width(25);
		private static readonly GUILayoutOption width65				= GUILayout.Width(65);
		


		static void FreeDrawSettingsGUI(FreeDrawGenerator generator, bool isSceneGUI)
		{
			var distanceUnit = RealtimeCSG.CSGSettings.DistanceUnit;
			var nextUnit = Units.CycleToNextUnit(distanceUnit);
			var unitText = Units.GetUnitGUIContent(distanceUnit);

			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				{
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(HeightContent, width65);
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
						if (GUILayout.Button(unitText, EditorStyles.miniLabel, width20))
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
#if DEMO
				EditorGUI.BeginDisabledGroup(true);
#endif
				{
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						GUILayout.Label(CurveSidesContent, width65);
						var subdivisions = generator.CurveSides;
						EditorGUI.BeginChangeCheck();
						{
							subdivisions = (uint)Mathf.Clamp(EditorGUILayout.IntField((int)subdivisions), 0, 32);
						}
						if (EditorGUI.EndChangeCheck() && generator.CurveSides != subdivisions)
						{
							generator.CurveSides = subdivisions;
						}
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(CurveSidesTooltip);
				}
				{
					GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
					{
						EditorGUI.BeginDisabledGroup(!generator.HaveSelectedEdgesOrVertices);
						{
							var tangentState = generator.SelectedTangentState;
							EditorGUI.BeginChangeCheck();
							{
								GUILayout.Label(EdgeTypeContent, width65);
								EditorGUI.showMixedValue = !tangentState.HasValue;
								tangentState = (EdgeTangentState)EditorGUILayout.EnumPopup(tangentState.HasValue ? tangentState.Value : EdgeTangentState.Straight);
								EditorGUI.showMixedValue = false;
							}
							if (EditorGUI.EndChangeCheck())
							{
								generator.SelectedTangentState = tangentState;
							}
						}
						EditorGUI.EndDisabledGroup();
					}
					GUILayout.EndHorizontal();
					TooltipUtility.SetToolTip(EdgeTypeTooltip);
				}
				EditorGUILayout.Space();
#if DEMO
				EditorGUI.EndDisabledGroup();
#endif

				EditorGUI.BeginDisabledGroup(!generator.HaveSelectedEdges);
				{
					if (GUILayout.Button(SplitSelectedContent))
					{
						generator.SplitSelectedEdge();
					}
					TooltipUtility.SetToolTip(SplitSelectedTooltip);
				}
				EditorGUI.EndDisabledGroup();

				/*
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				{
					if (isSceneGUI)
						GUILayout.Label(AlphaContent, width75);
					else
						EditorGUILayout.PrefixLabel(AlphaContent);
					var alpha = generator.Alpha;
					EditorGUI.BeginChangeCheck();
					{
						alpha = EditorGUILayout.Slider(alpha, -1.0f, 3.0f);
					}
					if (EditorGUI.EndChangeCheck() && generator.Alpha != alpha)
					{
						generator.Alpha = alpha;
					}
				}
				GUILayout.EndHorizontal();
				*/
			}
			GUILayout.EndVertical();
		}
		
		static void OnGUIContents(FreeDrawGenerator generator, bool isSceneGUI)
		{
			GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
			{
				bool enabled = generator.HaveBrushes;
				GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
				{
					EditorGUI.BeginDisabledGroup(!enabled);
					{
						GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
						{
							GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);
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
						GUILayout.EndHorizontal();
					}
					EditorGUI.EndDisabledGroup();

					if (isSceneGUI)
						FreeDrawSettingsGUI(generator, isSceneGUI);
				}
				GUILayout.EndHorizontal();

				if (!isSceneGUI)
				{
					GUILayout.Space(5);

					FreeDrawSettingsGUI(generator, isSceneGUI);

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
		
		public static bool OnShowGUI(FreeDrawGenerator generator, bool isSceneGUI)
		{
			GUIStyleUtility.InitStyles();
			OnGUIContents(generator, isSceneGUI);
			return true;
		}
	}
}
