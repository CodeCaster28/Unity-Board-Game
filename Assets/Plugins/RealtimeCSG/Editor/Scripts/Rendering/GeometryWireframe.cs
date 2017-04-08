#if UNITY_EDITOR
using System;
using UnityEngine;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class GeometryWireframe
	{
		public Vector3[]	vertices                = null;
		public Int32[]		visibleOuterLines       = null;
		public Int32[]		visibleInnerLines       = null;
		public Int32[]		visibleTriangles		= null;
		public Int32[]		invisibleOuterLines     = null;
		public Int32[]		invisibleInnerLines     = null;
		public Int32[]		invalidLines            = null;
		public UInt64		outlineGeneration		= 0;

		public GeometryWireframe Clone()
		{
			var clone = new GeometryWireframe();

			if (vertices != null)
			{
				clone.vertices = new Vector3[vertices.Length];
				Array.Copy(vertices, clone.vertices, vertices.Length);
			} else
				clone.vertices = null;
			
			if (visibleOuterLines != null)
			{
				clone.visibleOuterLines = new Int32[visibleOuterLines.Length];
				Array.Copy(visibleOuterLines, clone.visibleOuterLines, visibleOuterLines.Length);
			} else
				clone.visibleOuterLines = null;
			
			if (visibleInnerLines != null)
			{
				clone.visibleInnerLines = new Int32[visibleInnerLines.Length];
				Array.Copy(visibleInnerLines, clone.visibleInnerLines, visibleInnerLines.Length);
			} else
				clone.visibleInnerLines = null;
			
			if (visibleTriangles != null)
			{
				clone.visibleTriangles = new Int32[visibleTriangles.Length];
				Array.Copy(visibleTriangles, clone.visibleTriangles, visibleTriangles.Length);
			} else
				clone.visibleTriangles = null;
			
			if (invisibleOuterLines != null)
			{
				clone.invisibleOuterLines = new Int32[invisibleOuterLines.Length];
				Array.Copy(invisibleOuterLines, clone.invisibleOuterLines, invisibleOuterLines.Length);
			} else
				clone.invisibleOuterLines = null;
			
			if (invisibleInnerLines != null)
			{
				clone.invisibleInnerLines = new Int32[invisibleInnerLines.Length];
				Array.Copy(invisibleInnerLines, clone.invisibleInnerLines, invisibleInnerLines.Length);
			} else
				clone.invisibleInnerLines = null;
			
			if (invalidLines != null)
			{
				clone.invalidLines = new Int32[invalidLines.Length];
				Array.Copy(invalidLines, clone.invalidLines, invalidLines.Length);
			} else
				clone.invalidLines = null;

			return clone;
		}
	}
}
#endif