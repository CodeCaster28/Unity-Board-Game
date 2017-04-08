using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	sealed class LineMeshManager
	{
		sealed class LineMesh
		{
			public int VertexCount { get { return vertices1.Count; } }

			public List<Vector3>	vertices1	= new List<Vector3>(1024);
			public List<Vector3>	vertices2	= new List<Vector3>(1024);
			public List<Vector4>	offsets		= new List<Vector4>(1024);
			public List<Color>		colors		= new List<Color>(1024);
			int[]                   indices     = null;
		
			Mesh mesh;
		
			public void Clear()
			{
				vertices1.Clear();
				vertices2.Clear();
				offsets.Clear();
				colors.Clear();
			}
		
			public void AddLine(Vector3 A, Vector3 B, float thickness, float dashSize, Color color)
			{
				vertices1.Add(B); vertices2.Add(A); offsets.Add(new Vector4(+1, -1, thickness, dashSize)); colors.Add(color);
				vertices1.Add(B); vertices2.Add(A); offsets.Add(new Vector4(+1, +1, thickness, dashSize)); colors.Add(color);
				vertices1.Add(A); vertices2.Add(B); offsets.Add(new Vector4(-1, +1, thickness, dashSize)); colors.Add(color);
				vertices1.Add(A); vertices2.Add(B); offsets.Add(new Vector4(-1, -1, thickness, dashSize)); colors.Add(color);
			}

			public void CommitMesh()
			{
				if (vertices1.Count == 0)
				{
					if (mesh != null && mesh.vertexCount != 0)
					{
						mesh.Clear(true);
					}
					return;
				}

				if (mesh)
				{
					mesh.Clear(true);
				} else
					mesh = new Mesh();

				mesh.MarkDynamic();
				int req_size = vertices1.Count * 6 / 4;
				if (indices == null || indices.Length != req_size)
					indices = new int[req_size];
				for (int i = 0, j = 0; i < vertices1.Count; i += 4, j += 6)
				{
					indices[j+0] = i+0;
					indices[j+1] = i+1;
					indices[j+2] = i+2;
					
					indices[j+3] = i+0;
					indices[j+4] = i+2;
					indices[j+5] = i+3;
				}

				mesh.SetVertices(vertices1);
				mesh.SetUVs(0, vertices2);
				mesh.SetUVs(1, offsets);
				mesh.SetColors(colors);
				mesh.SetIndices(indices, MeshTopology.Triangles, 0);
				mesh.UploadMeshData(true);
			}
			
			public void Draw()
			{
				if (vertices1.Count == 0 || mesh == null)
					return;
				Graphics.DrawMeshNow(mesh, MathConstants.identityMatrix);
			}

			internal void Destroy()
			{
				if (mesh) UnityEngine.Object.DestroyImmediate(mesh);
				mesh = null;
				indices = null;
			}
		}

		public void Begin()
		{
			if (lineMeshes == null || lineMeshes.Count == 0)
				return;
			currentLineMesh = 0;
			for (int i = 0; i < lineMeshes.Count; i++) lineMeshes[i].Clear();
		}

		public void End()
		{
			if (lineMeshes == null || lineMeshes.Count == 0)
				return;
			var max = Mathf.Min(currentLineMesh, lineMeshes.Count);
			for (int i = 0; i <= max; i++)
				lineMeshes[i].CommitMesh();
		}

		public void Render(Material genericLineMaterial)
		{
			if (lineMeshes == null || lineMeshes.Count == 0)
				return;
			MaterialUtility.InitGenericLineMaterial(genericLineMaterial);
			if (genericLineMaterial &&
				genericLineMaterial.SetPass(0))
			{
				var max = Mathf.Min(currentLineMesh, lineMeshes.Count - 1);
				for (int i = 0; i <= max; i++)
					lineMeshes[i].Draw();
			}
		}

		List<LineMesh> lineMeshes = new List<LineMesh>();
		int currentLineMesh = 0;
		
		public LineMeshManager()
		{
			lineMeshes.Add(new LineMesh());
		}

		public void DrawLine(Vector3 A, Vector3 B, Color color, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMesh = lineMeshes[currentLineMesh];
			if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
			lineMesh.AddLine(A, B, thickness, dashSize, color);
		}

		//*
		public void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color color, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			while (lineMeshIndex >= lineMeshes.Count) lineMeshes.Add(new LineMesh());
			if (lineMeshes[lineMeshIndex].VertexCount + (indices.Length * 2) < 65535)
			{
				var lineMesh	= lineMeshes[lineMeshIndex];
				var vertices1	= lineMesh.vertices1;
				var vertices2	= lineMesh.vertices2;
				var offsets		= lineMesh.offsets;
				var colors		= lineMesh.colors;

				for (int i = 0; i < indices.Length; i += 2)
				{
					var A = matrix.MultiplyPoint(vertices[indices[i + 0]]);
					var B = matrix.MultiplyPoint(vertices[indices[i + 1]]);
					vertices1.Add(B); vertices2.Add(A); offsets.Add(new Vector4(+1, -1, thickness, dashSize)); colors.Add(color);
					vertices1.Add(B); vertices2.Add(A); offsets.Add(new Vector4(+1, +1, thickness, dashSize)); colors.Add(color);
					vertices1.Add(A); vertices2.Add(B); offsets.Add(new Vector4(-1, +1, thickness, dashSize)); colors.Add(color);
					vertices1.Add(A); vertices2.Add(B); offsets.Add(new Vector4(-1, -1, thickness, dashSize)); colors.Add(color);
				}
			} else
			{  
				for (int i = 0; i < indices.Length; i += 2)
				{
					var lineMesh	= lineMeshes[lineMeshIndex];
					if (lineMesh.VertexCount + 4 > 65535) { lineMeshIndex++; if (lineMeshIndex >= lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[lineMeshIndex]; }
					var vertices1	= lineMesh.vertices1;
					var vertices2	= lineMesh.vertices2;
					var offsets		= lineMesh.offsets;
					var colors		= lineMesh.colors;

					var A = matrix.MultiplyPoint(vertices[indices[i + 0]]);
					var B = matrix.MultiplyPoint(vertices[indices[i + 1]]);
					vertices1.Add(B); vertices2.Add(A); offsets.Add(new Vector4(+1, -1, thickness, dashSize)); colors.Add(color);
					vertices1.Add(B); vertices2.Add(A); offsets.Add(new Vector4(+1, +1, thickness, dashSize)); colors.Add(color);
					vertices1.Add(A); vertices2.Add(B); offsets.Add(new Vector4(-1, +1, thickness, dashSize)); colors.Add(color);
					vertices1.Add(A); vertices2.Add(B); offsets.Add(new Vector4(-1, -1, thickness, dashSize)); colors.Add(color);
				}
				currentLineMesh = lineMeshIndex;
			}
		}

		public void DrawLines(Vector3[] vertices, int[] indices, Color color, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			var lineMesh = lineMeshes[currentLineMesh];
			for (int i = 0; i < indices.Length; i += 2)
			{
				if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
				var index0 = indices[i + 0];
				var index1 = indices[i + 1];
				if (index0 < 0 || index1 < 0 || index0 >= vertices.Length || index1 >= vertices.Length)
					continue;
				lineMesh.AddLine(vertices[index0], vertices[index1], thickness, dashSize, color);
			}
			currentLineMesh = lineMeshIndex;
		}

		public void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Color color, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			var lineMesh = lineMeshes[currentLineMesh];
			for (int i = 0; i < vertices.Length; i += 2)
			{
				if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
				lineMesh.AddLine(matrix.MultiplyPoint(vertices[i + 0]), matrix.MultiplyPoint(vertices[i + 1]), thickness, dashSize, color);
			}
			currentLineMesh = lineMeshIndex;
		}

		public void DrawLines(Vector3[] vertices, Color color, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			var lineMesh = lineMeshes[currentLineMesh];
			for (int i = 0; i < vertices.Length; i += 2)
			{
				if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
				lineMesh.AddLine(vertices[i + 0], vertices[i + 1], thickness, dashSize, color);
			}
			currentLineMesh = lineMeshIndex;
		}



		public void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			var lineMesh = lineMeshes[currentLineMesh];
			for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
			{
				if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
				var index0 = indices[i + 0];
				var index1 = indices[i + 1];
				if (index0 < 0 || index1 < 0 || index0 >= vertices.Length || index1 >= vertices.Length)
					continue;
				lineMesh.AddLine(matrix.MultiplyPoint(vertices[index0]), matrix.MultiplyPoint(vertices[index1]), thickness, dashSize, colors[c]);
			}
			currentLineMesh = lineMeshIndex;
		}

		public void DrawLines(Vector3[] vertices, int[] indices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			var lineMesh = lineMeshes[currentLineMesh];
			for (int i = 0, c = 0; i < indices.Length; i += 2, c++)
			{
				if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
				var index0 = indices[i + 0];
				var index1 = indices[i + 1];
				if (index0 < 0 || index1 < 0 || index0 >= vertices.Length || index1 >= vertices.Length)
					continue;
				lineMesh.AddLine(vertices[index0], vertices[index1], thickness, dashSize, colors[c]);
			}
			currentLineMesh = lineMeshIndex;
		}

		public void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			var lineMesh = lineMeshes[currentLineMesh];
			for (int i = 0, c = 0; i < vertices.Length; i += 2, c++)
			{
				if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
				lineMesh.AddLine(matrix.MultiplyPoint(vertices[i + 0]), matrix.MultiplyPoint(vertices[i + 1]), thickness, dashSize, colors[c]);
			}
			currentLineMesh = lineMeshIndex;
		}

		public void DrawLines(Vector3[] vertices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f)
		{
			var lineMeshIndex = currentLineMesh;
			var lineMesh = lineMeshes[currentLineMesh];
			for (int i = 0, c = 0; i < vertices.Length; i += 2, c++)
			{
				if (lineMesh.VertexCount + 4 > 65535) { currentLineMesh++; if (currentLineMesh > lineMeshes.Count) lineMeshes.Add(new LineMesh()); lineMesh = lineMeshes[currentLineMesh]; }
				lineMesh.AddLine(vertices[i + 0], vertices[i + 1], thickness, dashSize, colors[c]);
			}
			currentLineMesh = lineMeshIndex;
		}

		internal void Destroy()
		{
			for (int i = 0; i < lineMeshes.Count; i++)
			{
				lineMeshes[i].Destroy();
			}
			lineMeshes.Clear();
			currentLineMesh = 0;
		}
		
		internal void Clear()
		{
			currentLineMesh = 0;
			for (int i = 0; i < lineMeshes.Count; i++) lineMeshes[i].Clear();
		}
	}
}
