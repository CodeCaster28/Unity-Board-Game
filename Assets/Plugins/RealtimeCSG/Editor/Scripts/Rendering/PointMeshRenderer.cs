using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	sealed class PointMeshManager
	{
		sealed class PointMesh
		{
			public int VertexCount { get { return vertices.Count; } }

			public List<Vector3>	vertices		= new List<Vector3>(1024);
			public List<int>		pointIndices	= new List<int>(1024);
			public List<Color>		pointColors		= new List<Color>(1024);
			public List<int>		lineIndices		= new List<int>(1024);
			public List<Color>		lineColors		= new List<Color>(1024);
		
			Mesh pointMesh;
			Mesh lineMesh;
		
			public void Clear()
			{
				vertices.Clear();
				pointIndices.Clear();
				pointColors.Clear();
				lineIndices.Clear();
				lineColors.Clear();
			}
			
			public void CommitMesh()
			{
				if (vertices.Count == 0)
				{
					if (pointMesh != null && pointMesh.vertexCount != 0)
					{
						pointMesh.Clear(true);
					}
					if (lineMesh != null && lineMesh.vertexCount != 0)
					{
						lineMesh.Clear(true);
					}
					return;
				}

				if (pointMesh != null) pointMesh.Clear(true); else pointMesh = new Mesh();
				if (lineMesh  != null) lineMesh .Clear(true); else lineMesh = new Mesh();

				pointMesh.MarkDynamic();
				lineMesh.MarkDynamic();
				pointMesh.SetVertices(vertices);
				pointMesh.SetColors(pointColors);
				pointMesh.SetIndices(pointIndices.ToArray(), MeshTopology.Triangles, 0);
				pointMesh.UploadMeshData(true);
				
				lineMesh.SetVertices(vertices);
				lineMesh.SetColors(lineColors);
				lineMesh.SetIndices(lineIndices.ToArray(), MeshTopology.Lines, 0);
				lineMesh.UploadMeshData(true);
			}
			
			public void DrawPoints()
			{
				if (vertices.Count == 0)
					return;
				Graphics.DrawMeshNow(pointMesh, MathConstants.identityMatrix);
			}

			public void DrawLines()
			{
				if (vertices.Count == 0)
					return;
				Graphics.DrawMeshNow(lineMesh, MathConstants.identityMatrix);
			}

			internal void Destroy()
			{
				if (pointMesh) UnityEngine.Object.DestroyImmediate(pointMesh);
				if (lineMesh)  UnityEngine.Object.DestroyImmediate(lineMesh);
				pointMesh = null;
				lineMesh = null;
			}
		}

		public void Begin()
		{
			currentPointMesh = 0;
			for (int i = 0; i < pointMeshes.Count; i++) pointMeshes[i].Clear();
		}

		public void End()
		{
			for (int i = 0; i <= currentPointMesh; i++) pointMeshes[i].CommitMesh();
		}

		public void Render(Material pointMaterial, Material lineMaterial)
		{
			if (pointMaterial.SetPass(0))
			{
				for (int i = 0; i <= currentPointMesh; i++)
					pointMeshes[i].DrawPoints();
			}
			if (lineMaterial.SetPass(0))
			{
				for (int i = 0; i <= currentPointMesh; i++)
					pointMeshes[i].DrawLines();
			}
		}

		List<PointMesh> pointMeshes = new List<PointMesh>();
		int currentPointMesh = 0;
		
		public PointMeshManager()
		{
			pointMeshes.Add(new PointMesh());
		}

		public void DrawPoint(Vector3 position, float size, Color innerColor, Color outerColor)
		{
			var camera	= Camera.current;
			var right	= camera.transform.right;
			var up		= camera.transform.up;
				
			var p0		= (  right + up);
			var p1		= (  right - up);
			var p2		= (- right - up);
			var p3		= (- right + up);

			var pointMesh		= pointMeshes[currentPointMesh];
			if (pointMesh.VertexCount + 4 > 65535) { currentPointMesh++; if (currentPointMesh > pointMeshes.Count) pointMeshes.Add(new PointMesh()); pointMesh = pointMeshes[currentPointMesh]; }
			var dstVertices		= pointMesh.vertices;
			var dstLineColors	= pointMesh.lineColors;
			var dstPointColors	= pointMesh.pointColors;
			var dstPointIndices	= pointMesh.pointIndices;
			var dstLineIndices	= pointMesh.lineIndices;

			var index = dstVertices.Count;
			dstVertices.Add(position + (p0 * size)); 
			dstVertices.Add(position + (p1 * size)); 
			dstVertices.Add(position + (p2 * size)); 
			dstVertices.Add(position + (p3 * size)); 

			dstPointColors.Add(innerColor);
			dstPointColors.Add(innerColor);
			dstPointColors.Add(innerColor);
			dstPointColors.Add(innerColor);

			dstLineColors.Add(outerColor);
			dstLineColors.Add(outerColor);
			dstLineColors.Add(outerColor);
			dstLineColors.Add(outerColor);

			dstPointIndices.Add(index + 0);
			dstPointIndices.Add(index + 1);
			dstPointIndices.Add(index + 2);
			dstPointIndices.Add(index + 1);
			dstPointIndices.Add(index + 2);
			dstPointIndices.Add(index + 3);

			dstLineIndices.Add(index + 0);
			dstLineIndices.Add(index + 1);
			dstLineIndices.Add(index + 1);
			dstLineIndices.Add(index + 2);
			dstLineIndices.Add(index + 2);
			dstLineIndices.Add(index + 3);
			dstLineIndices.Add(index + 3);
			dstLineIndices.Add(index + 0);
		}

		public void DrawPoints(Vector3[] vertices, float[] sizes, Color[] colors)
		{
			var camera	= Camera.current;
			var right	= camera.transform.right;
			var up		= camera.transform.up;
				
			var p0		= (  right + up);
			var p1		= (  right - up);
			var p2		= (- right - up);
			var p3		= (- right + up);

			var pointMeshIndex	= currentPointMesh;
			var pointMesh		= pointMeshes[currentPointMesh];
			var dstVertices		= pointMesh.vertices;
			var dstLineColors	= pointMesh.lineColors;
			var dstPointColors	= pointMesh.pointColors;
			var dstPointIndices	= pointMesh.pointIndices;
			var dstLineIndices	= pointMesh.lineIndices;
			for (int i = 0, c = 0; i < vertices.Length; i ++, c += 2)
			{
				var index = dstVertices.Count;
				if (pointMesh.VertexCount + 4 > 65535)
				{
					currentPointMesh++;
					if (currentPointMesh > pointMeshes.Count) pointMeshes.Add(new PointMesh());
					pointMesh		= pointMeshes[currentPointMesh];
					dstVertices		= pointMesh.vertices;
					dstLineColors	= pointMesh.lineColors;
					dstPointColors	= pointMesh.pointColors;
					dstPointIndices	= pointMesh.pointIndices;
					dstLineIndices	= pointMesh.lineIndices;
					index			= dstVertices.Count;
				}
				var position	= vertices[i];
				var innerColor	= colors[c + 1];
				var outerColor	= colors[c + 0];
				var size		= sizes[i];
				dstVertices.Add(position + (p0 * size)); 
				dstVertices.Add(position + (p1 * size)); 
				dstVertices.Add(position + (p2 * size)); 
				dstVertices.Add(position + (p3 * size)); 

				dstPointColors.Add(innerColor);
				dstPointColors.Add(innerColor);
				dstPointColors.Add(innerColor);
				dstPointColors.Add(innerColor);

				dstLineColors.Add(outerColor);
				dstLineColors.Add(outerColor);
				dstLineColors.Add(outerColor);
				dstLineColors.Add(outerColor);

				dstPointIndices.Add(index + 0);
				dstPointIndices.Add(index + 1);
				dstPointIndices.Add(index + 2);
				dstPointIndices.Add(index + 0);
				dstPointIndices.Add(index + 2);
				dstPointIndices.Add(index + 3);

				dstLineIndices.Add(index + 0);
				dstLineIndices.Add(index + 1);
				dstLineIndices.Add(index + 1);
				dstLineIndices.Add(index + 2);
				dstLineIndices.Add(index + 2);
				dstLineIndices.Add(index + 3);
				dstLineIndices.Add(index + 3);
				dstLineIndices.Add(index + 0);
			}
			currentPointMesh = pointMeshIndex;
		}

		internal void Destroy()
		{
			for (int i = 0; i < pointMeshes.Count; i++)
			{
				pointMeshes[i].Destroy();
			}
			pointMeshes.Clear();
		}
	}
}
