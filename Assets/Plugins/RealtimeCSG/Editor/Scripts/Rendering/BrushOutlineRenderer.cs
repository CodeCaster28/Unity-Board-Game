using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;

namespace RealtimeCSG
{
	internal class BrushOutlineRenderer
	{
		private readonly LineMeshManager _outlinesManager = new LineMeshManager();
		private readonly LineMeshManager _edgeColorsManager = new LineMeshManager();

		public void Destroy()
		{
			_outlinesManager.Destroy();
			_edgeColorsManager.Destroy();
		}

		public void Update(CSGBrush[] brushes, ControlMesh[] controlMeshes, ControlMeshState[] meshStates)
		{
			_outlinesManager.Begin();
			_edgeColorsManager.Begin();
			for (var t = 0; t < brushes.Length; t++)
			{
				var brush = brushes[t];
				if (!brush)
					continue;

				var meshState = meshStates[t];
				if (meshState.WorldPoints.Length == 0 &&
					meshState.Edges.Length == 0)
					continue;

				meshState.UpdateColors(brush, controlMeshes[t]);

				_edgeColorsManager.DrawLines(meshState.WorldPoints, meshState.Edges, ColorSettings.MeshEdgeOutline, thickness: 1.0f);//, zTest: false);
				_outlinesManager.DrawLines(meshState.WorldPoints, meshState.Edges, meshState.EdgeColors, dashSize: 4.0f);//, zTest: false);
			}
			_edgeColorsManager.End();
			_outlinesManager.End();
		}

		public void RenderOutlines()
		{
			var zTestGenericLineMaterial = MaterialUtility.ZTestGenericLine;
			var noZTestGenericLineMaterial = MaterialUtility.NoZTestGenericLine;

			MaterialUtility.LineDashMultiplier = 0.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			_edgeColorsManager.Render(noZTestGenericLineMaterial);

			MaterialUtility.LineDashMultiplier = 0.0f;
			MaterialUtility.LineThicknessMultiplier = ToolConstants.thickLineScale + 2.0f;
			_edgeColorsManager.Render(zTestGenericLineMaterial);

			MaterialUtility.LineDashMultiplier = 1.0f;
			MaterialUtility.LineThicknessMultiplier = 1.0f;
			_outlinesManager.Render(noZTestGenericLineMaterial);

			MaterialUtility.LineDashMultiplier = 0.0f;
			MaterialUtility.LineThicknessMultiplier = ToolConstants.thickLineScale;
			_outlinesManager.Render(zTestGenericLineMaterial);
		}
	}
}
