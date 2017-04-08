using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class SceneTools
	{
		public static bool IsDraggingObjectInScene { get; internal set; }

		static int meshGeneration = -1;

		static LineMeshManager lineMeshManager = new LineMeshManager();

		internal static void Cleanup()
		{
			meshGeneration = -1;
			lineMeshManager.Destroy();
		}

		internal static bool forceOutlineUpdate = false;
		
		internal static void OnPaint(SceneView sceneView)
		{
			if (sceneView == null ||
				Event.current.type != EventType.repaint)
				return;

			if (RealtimeCSG.CSGSettings.GridVisible)
				Grid.RenderGrid();
			
			if (RealtimeCSG.CSGSettings.IsWireframeShown(sceneView))
			{
				if (forceOutlineUpdate || meshGeneration != InternalCSGModelManager.MeshGeneration)
				{
					forceOutlineUpdate = false;
					meshGeneration = InternalCSGModelManager.MeshGeneration;
					lineMeshManager.Begin();
					for (int i = 0; i < InternalCSGModelManager.Brushes.Length; i++)
					{
						var brush = InternalCSGModelManager.Brushes[i];
						if (!brush)
							continue;

						var brush_cache = InternalCSGModelManager.GetBrushCache(brush);
						if (brush_cache == null)
							continue;

						if (!brush.outlineColor.HasValue)
							brush.outlineColor = ColorSettings.GetBrushOutlineColor(brush);
						
						var brush_translation = brush_cache.compareTransformation.modelLocalPosition +
												brush_cache.childData.ModelTransform.position;
						CSGRenderer.DrawSimpleOutlines(lineMeshManager, brush.brushID, brush_translation, brush.outlineColor.Value);
					}
					lineMeshManager.End();
				}

				MaterialUtility.LineDashMultiplier = 1.0f;
				MaterialUtility.LineThicknessMultiplier = 1.0f;
				lineMeshManager.Render(MaterialUtility.NoZTestGenericLine);
			}
		}
	}
}
