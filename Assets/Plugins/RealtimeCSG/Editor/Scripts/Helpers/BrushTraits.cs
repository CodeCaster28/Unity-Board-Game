using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealtimeCSG
{
	internal static class BrushTraits
	{
		public static bool IsSurfaceSelectable(CSGBrush brush, int surfaceIndex)
		{
			if (!brush)
			{
				//Debug.Log("!brush");
				return true;
			}

			var shape = brush.Shape;
			if (shape == null)
			{
				//Debug.Log("shape == null");
				return true;
			}

			var surfaces = shape.Surfaces;
			if (surfaces == null)
			{
				//Debug.Log("surfaces == null");
				return true;
			}

			if (surfaceIndex < 0 || surfaceIndex >= surfaces.Length)
			{
				//Debug.Log("surfaceIndex("+surfaceIndex+") < 0 || surfaceIndex >= surfaces.Length("+surfaces.Length+")");
				return true;
			}

			var texGenIndex = surfaces[surfaceIndex].TexGenIndex;
			var texGenFlags = shape.TexGenFlags;
			if (texGenFlags == null ||
				texGenIndex < 0 || texGenIndex >= texGenFlags.Length)
			{
				return true;
			}

			if ((texGenFlags[texGenIndex] & TexGenFlags.Discarded) == TexGenFlags.Discarded)
				return !CSGSettings.ShowDiscardedSurfaces;

			if ((texGenFlags[texGenIndex] & TexGenFlags.ShadowOnly) == TexGenFlags.ShadowOnly)
				return !CSGSettings.ShowShadowOnlySurfaces;

			return false;
		}
	}
}
