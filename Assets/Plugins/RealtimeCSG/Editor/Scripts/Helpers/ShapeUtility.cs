using System;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal static class ShapeUtility
	{
		#region CheckMaterials
		public static bool CheckMaterials(Shape shape)
		{
			bool dirty = false;
			if (shape.Surfaces == null ||
				shape.Surfaces.Length == 0)
			{
				Debug.Log("Surfaces == null || Surfaces.Length == 0");
				return true;
			}

			if (shape.CutNodes == null || shape.CutNodes.Length < 1)
			{
				shape.CutNodes = new CutNode[shape.Surfaces.Length - 1];
				dirty = true;
			}
			 
			int maxTexGenIndex = 0;
			for (int i = 0; i < shape.Surfaces.Length; i++)
			{
				maxTexGenIndex = Mathf.Max(maxTexGenIndex, shape.Surfaces[i].TexGenIndex);
			}
			maxTexGenIndex++;

			if (shape.TexGens == null ||
				shape.TexGens.Length < maxTexGenIndex)
			{
				dirty = true;
				var newTexGens		= new TexGen[maxTexGenIndex];
				var newTexGenFlags	= new TexGenFlags[maxTexGenIndex];
				if (shape.TexGens != null &&
					shape.TexGens.Length > 0)
				{
					for (int i = 0; i < shape.TexGens.Length; i++)
					{
						newTexGens[i] = shape.TexGens[i];
						newTexGenFlags[i] = shape.TexGenFlags[i];
					}
					for (int i = shape.TexGens.Length; i < newTexGens.Length; i++)
					{
						newTexGens[i].Color = Color.white;
					}
				}
				shape.TexGens = newTexGens;
				shape.TexGenFlags = newTexGenFlags;
			}
			
			for (int i = 0; i < shape.TexGens.Length; i++)
			{
				if (shape.TexGens[i].Color == Color.clear)
					shape.TexGens[i].Color = Color.white;
			}

			if (shape.Materials == null ||
				shape.Materials.Length == 0)
			{
				dirty = true;
				shape.Materials = new Material[shape.TexGens.Length];
			}

			if (shape.TexGens.Length != shape.Materials.Length)
			{
				dirty = true;
				var new_materials = new Material[shape.TexGens.Length];
				Array.Copy(shape.Materials, new_materials, Math.Min(shape.Materials.Length, shape.TexGens.Length));
				shape.Materials = new_materials;
			}
			return dirty;
		}
		#endregion

		#region EnsureInitialized
		public static bool EnsureInitialized(Shape shape)
		{
			bool dirty = CheckMaterials(shape);
			
			for (int i = 0; i < shape.TexGens.Length; i++)
			{
				if ((shape.TexGens[i].Scale.x >= -MathConstants.MinimumScale && shape.TexGens[i].Scale.x <= MathConstants.MinimumScale) ||
					(shape.TexGens[i].Scale.y >= -MathConstants.MinimumScale && shape.TexGens[i].Scale.y <= MathConstants.MinimumScale))
				{
					dirty = true;
					if (shape.TexGens[i].Scale.x == 0 &&
						shape.TexGens[i].Scale.y == 0)
					{
						shape.TexGens[i].Scale.x = 1.0f;
						shape.TexGens[i].Scale.y = 1.0f;
					}
					
                    if (shape.TexGens[i].Scale.x < 0)
					    shape.TexGens[i].Scale.x = -Mathf.Max(MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.x));
                    else
					    shape.TexGens[i].Scale.x = Mathf.Max(MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.x));
                    if (shape.TexGens[i].Scale.y < 0)
					    shape.TexGens[i].Scale.y = -Mathf.Max(MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.y));
                    else
					    shape.TexGens[i].Scale.y = Mathf.Max(MathConstants.MinimumScale, Mathf.Abs(shape.TexGens[i].Scale.y));
				}
			}
			
			Vector3 tangent = MathConstants.zeroVector3, binormal = MathConstants.zeroVector3;
			for (int i = 0; i < shape.Surfaces.Length; i++)
			{
				if (shape.Surfaces[i].Tangent == MathConstants.zeroVector3 ||
					shape.Surfaces[i].BiNormal == MathConstants.zeroVector3)
				{
					dirty = true;
					var normal = shape.Surfaces[i].Plane.normal;
					GeometryUtility.CalculateTangents(normal, out tangent, out binormal);
					shape.Surfaces[i].Tangent  = tangent;
					shape.Surfaces[i].BiNormal = binormal;
				}
//				if (Surfaces[i].Stretch == MathConstants.zeroVector2)
//				{
//					Surfaces[i].Stretch = MathConstants.oneVector2;
//					dirty = true;
//				}
			}
			return dirty;
		}
		#endregion

		#region CreateCutter
		public static void CreateCutter(Shape shape, ControlMesh mesh)
		{
			if (shape.Surfaces.Length == 0)
				return;
			
			// sort planes by putting the most different planes before more similar planes
			var unsortedPlaneIndices = new int[shape.Surfaces.Length];
			for (int i = 0; i < shape.Surfaces.Length; i++)
			{
				unsortedPlaneIndices[i] = i;
			}

			int first_plane = -1;
			float largestAxis = 0;
			for (int i = 0; i < unsortedPlaneIndices.Length; i++)
			{
				var normal = shape.Surfaces[i].Plane.normal;
				var normalx = Mathf.Abs(normal.x);
				var normaly = Mathf.Abs(normal.y);
				var normalz = Mathf.Abs(normal.z);
				if (normalx > largestAxis)
				{
					largestAxis = normalx;
					first_plane = i;
				}
				if (normalz > largestAxis)
				{
					largestAxis = normalz;
					first_plane = i;
				}
				if (normaly > largestAxis)
				{
					largestAxis = normaly;
					first_plane = i;
				}
			}

			if (first_plane == -1)
				first_plane = 0;
			
			var sortedPlaneIndices = new int[unsortedPlaneIndices.Length];
			ArrayUtility.RemoveAt(ref unsortedPlaneIndices, first_plane);
			sortedPlaneIndices[0] = first_plane;
			var prevIndex = first_plane;
			var sortedPlaneCount = 1;
			while (unsortedPlaneIndices.Length > 0)
			{
				var self_normal = shape.Surfaces[prevIndex].Plane.normal;
				int new_plane = -1;
				float smallestDot = 2;
				for (int i = 0; i < unsortedPlaneIndices.Length; i++)
				{
					var other_normal = shape.Surfaces[unsortedPlaneIndices[i]].Plane.normal;
					var dot = Vector3.Dot(self_normal, other_normal);
					if (dot < smallestDot)
					{
						smallestDot = dot;
						new_plane = i;
					}
				}
				
				prevIndex = 
				sortedPlaneIndices[sortedPlaneCount] = unsortedPlaneIndices[new_plane];
				sortedPlaneCount++;
				ArrayUtility.RemoveAt(ref unsortedPlaneIndices, new_plane);
			}
			
			// create the cutters
			shape.CutNodes = new CutNode[shape.Surfaces.Length];
			for (int i = 0; i < shape.Surfaces.Length; i++)
			{
				shape.CutNodes[i].planeIndex = sortedPlaneIndices[i];
				shape.CutNodes[i].frontNodeIndex = CutNode.Outside;
				shape.CutNodes[i].backNodeIndex = (Int16)(i + 1);
			}
			shape.CutNodes[shape.Surfaces.Length - 1].backNodeIndex = CutNode.Inside;
		}
		#endregion

	}
}
