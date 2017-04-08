using System.Collections.Generic;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
	internal class BrushOutlineManager
	{
		private static readonly Dictionary<int, GeometryWireframe> OutlineCache = new Dictionary<int, GeometryWireframe>();

		#region ClearOutlines
		public static void ClearOutlines()
		{
			OutlineCache.Clear();
		}
		#endregion

		#region ForceUpdateOutlines
		public static void ForceUpdateOutlines(int brushId)
		{
			var externalOutlineGeneration = InternalCSGModelManager.External.GetBrushOutlineGeneration(brushId);
			var outline = new GeometryWireframe();
			if (!InternalCSGModelManager.External.GetBrushOutline(
							brushId,
							ref outline.vertices,
							ref outline.visibleOuterLines,
							ref outline.visibleInnerLines,
							ref outline.invisibleOuterLines,
							ref outline.invisibleInnerLines,
							ref outline.invalidLines))
				return;

			outline.outlineGeneration = externalOutlineGeneration;
			OutlineCache[brushId] = outline;
		}
		#endregion

		#region GetBrushOutline
		public static GeometryWireframe GetBrushOutline(int brushId)
		{
			if (brushId == -1)
				return null;

			var externalOutlineGeneration = InternalCSGModelManager.External.GetBrushOutlineGeneration(brushId);

			GeometryWireframe outline;
			if (!OutlineCache.TryGetValue(brushId, out outline))
				externalOutlineGeneration = externalOutlineGeneration - 1;
			
			if (outline != null &&
				externalOutlineGeneration == outline.outlineGeneration)
				return outline;

			outline = new GeometryWireframe();
			if (!InternalCSGModelManager.External.GetBrushOutline(
				brushId,
				ref outline.vertices,
				ref outline.visibleOuterLines,
				ref outline.visibleInnerLines,
				ref outline.invisibleOuterLines,
				ref outline.invisibleInnerLines,
				ref outline.invalidLines))
				return null;
			
			outline.outlineGeneration = externalOutlineGeneration;
			OutlineCache[brushId] = outline;
			return outline;
		}

		public static GeometryWireframe[] GetBrushOutlines(int[] brushIDs)
		{
			var wireframes = new GeometryWireframe[brushIDs.Length];

			for (var i = 0; i < brushIDs.Length; i++)
			{
				var brushId = brushIDs[i];
				if (brushId == -1)
				{
					wireframes[i] = null;
					continue;
				}
				var externalOutlineGeneration = InternalCSGModelManager.External.GetBrushOutlineGeneration(brushId);

				GeometryWireframe outline;
				if (!OutlineCache.TryGetValue(brushId, out outline))
					externalOutlineGeneration = externalOutlineGeneration - 1;
				
				if (outline == null ||
					externalOutlineGeneration != outline.outlineGeneration)
				{
					outline = new GeometryWireframe();
					if (!InternalCSGModelManager.External.GetBrushOutline(
								brushId,
								ref outline.vertices,
								ref outline.visibleOuterLines,
								ref outline.visibleInnerLines,
								ref outline.invisibleOuterLines,
								ref outline.invisibleInnerLines,
								ref outline.invalidLines))
					{
						outline = null;
					}
					else
					{
						outline.outlineGeneration = externalOutlineGeneration;
						OutlineCache[brushId] = outline;
					}
				}
				wireframes[i] = outline;
			}
			return wireframes;
		}
		#endregion

		#region GetSurfaceOutline
		public static GeometryWireframe GetSurfaceOutline(int brushId, int texGenId)
		{
			if (brushId == -1)
				return null;

			var outline = new GeometryWireframe();
			if (!InternalCSGModelManager.External.GetTexGenOutline(
						brushId,
						texGenId,
						ref outline.vertices,
						ref outline.visibleOuterLines,
						ref outline.visibleInnerLines,
						ref outline.visibleTriangles,
						ref outline.invisibleOuterLines,
						ref outline.invisibleInnerLines,
						ref outline.invalidLines))
			{
				outline = null;
			}
			return outline;
		}

		public static GeometryWireframe[] GetSurfaceOutlines(SelectedBrushSurface[] selectedSurfaces)
		{
			if (selectedSurfaces == null || selectedSurfaces.Length == 0)
				return new GeometryWireframe[0];

			var wireframes = new GeometryWireframe[selectedSurfaces.Length];
			for (var i = 0; i < selectedSurfaces.Length; i++)
			{
				var brushId = -1;
				var brush = selectedSurfaces[i].brush;
				if (brush) brushId = brush.brushID;
				if (brushId == -1)
				{
					wireframes[i] = null;
					continue;
				}
				var surfaceId = selectedSurfaces[i].surfaceIndex;
				var texgenId = brush.Shape.Surfaces[surfaceId].TexGenIndex;

				var outline = new GeometryWireframe();
				if (!InternalCSGModelManager.External.GetTexGenOutline(
							brushId,
							texgenId,
							ref outline.vertices,
							ref outline.visibleOuterLines,
							ref outline.visibleInnerLines,
							ref outline.visibleTriangles,
							ref outline.invisibleOuterLines,
							ref outline.invisibleInnerLines,
							ref outline.invalidLines))
				{
					outline = null;
				}
				wireframes[i] = outline;
			}
			return wireframes;
		}
		#endregion

	}
}
