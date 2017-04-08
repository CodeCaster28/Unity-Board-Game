using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	sealed class PolygonMeshManager
	{
		sealed class TriangleMesh
		{
			public int VertexCount { get { return vertices.Count; } }

			public List<Vector3>	vertices	= new List<Vector3>(1024);
			public List<Color>		colors		= new List<Color>(1024);
			List<int>               indices     = new List<int>();
			int[]                   indexArray  = null;
		
			Mesh mesh;
		
			public void Clear()
			{
				vertices.Clear();
				colors.Clear();
				indices.Clear();
				indexArray  = null;
			}
		
			public void AddTriangles(Vector3[] triangles, Color[] color)
			{
				if (triangles.Length < 3 ||
					triangles.Length % 3 != 0 ||
					triangles.Length != color.Length)
					return;

				int startIndex = vertices.Count;
				vertices.AddRange(triangles);
				colors.AddRange(colors);
				for (int i = 0; i < triangles.Length; i++, startIndex++)
					indices.Add(startIndex);
				indexArray = null;
			}
		
			public void AddTriangles(Vector3[] triangles, Color color)
			{
				if (triangles.Length < 3 ||
					triangles.Length % 3 != 0)
					return;
				
				int startIndex = vertices.Count;
				vertices.AddRange(triangles);
				for (int i = 0; i < triangles.Length; i++, startIndex++)
				{
					colors.Add(color);
					indices.Add(startIndex);
				}
			}
		
			public void AddTriangles(Vector3[] triangles, int[] triangleIndices, Color[] color)
			{
				if (triangleIndices.Length < 3 ||
					triangleIndices.Length % 3 != 0)
					return;
				
				int startIndex = vertices.Count;
				vertices.AddRange(triangles);
				colors.AddRange(colors);
				for (int i = 0; i < triangleIndices.Length; i++)
				{
					indices.Add(triangleIndices[i] + startIndex);
				}
			}
		
			public void AddTriangles(Vector3[] triangles, int[] triangleIndices, Color color)
			{
				if (triangleIndices.Length < 3 ||
					triangleIndices.Length % 3 != 0)
					return;
				
				int startIndex = vertices.Count;
				vertices.AddRange(triangles);
				for (int i = 0; i < triangles.Length; i++)
					colors.Add(color);
				for (int i = 0; i < triangleIndices.Length; i++)
					indices.Add(triangleIndices[i] + startIndex);
			}
		
			public void AddPolygon(Vector3[] polygon, Color[] color)
			{
				if (polygon.Length < 3 ||
					polygon.Length > color.Length)
					return;
				
				int startIndex = vertices.Count;
				vertices.AddRange(polygon);
				colors.AddRange(color);
				for (int i = 2; i < polygon.Length; i++)
				{
					indices.Add(startIndex + 0);
					indices.Add(startIndex + i - 1);
					indices.Add(startIndex + i    );
				}
			}
		
			public void AddPolygon(Vector3[] polygon, Color color)
			{
				if (polygon.Length < 3)
					return;
				
				int startIndex = vertices.Count;
				vertices.AddRange(polygon);
				for (int i = 0; i < polygon.Length; i++)
					colors.Add(color);
				for (int i = 2; i < polygon.Length; i++)
				{
					indices.Add(startIndex + 0    );
					indices.Add(startIndex + i - 1);
					indices.Add(startIndex + i    );
				}
			}

			public void CommitMesh()
			{
				if (vertices.Count == 0)
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
				if (indexArray == null || indices.Count != indexArray.Length)
					indexArray = indices.ToArray();
				
				mesh.SetVertices(vertices);
				mesh.SetColors(colors);
				mesh.SetIndices(indexArray, MeshTopology.Triangles, 0);
				mesh.UploadMeshData(true);
			}
			
			public void Draw()
			{
				if (vertices.Count == 0 || mesh == null)
					return;
				Graphics.DrawMeshNow(mesh, MathConstants.identityMatrix);
			}

			internal void Destroy()
			{
				if (mesh) UnityEngine.Object.DestroyImmediate(mesh);
				mesh = null;
				indices = null;
				indexArray = null;
			}
		}

		public void Begin()
		{
			if (triangleMeshes == null || triangleMeshes.Count == 0)
				return;
			currentTriangleMesh = 0;
			for (int i = 0; i < triangleMeshes.Count; i++) triangleMeshes[i].Clear();
		}

		public void End()
		{
			if (triangleMeshes == null || triangleMeshes.Count == 0)
				return;
			var max = Mathf.Min(currentTriangleMesh, triangleMeshes.Count);
			for (int i = 0; i <= max; i++)
				triangleMeshes[i].CommitMesh();
		}

		public void Render(Material genericMaterial)
		{
			if (triangleMeshes == null || triangleMeshes.Count == 0)
				return;
			if (genericMaterial &&
				genericMaterial.SetPass(0))
			{
				var max = Mathf.Min(currentTriangleMesh, triangleMeshes.Count - 1);
				for (int i = 0; i <= max; i++)
					triangleMeshes[i].Draw();
			}
		}

		List<TriangleMesh> triangleMeshes = new List<TriangleMesh>();
		int currentTriangleMesh = 0;
		
		public PolygonMeshManager()
		{
			triangleMeshes.Add(new TriangleMesh());
		}
		

		public void DrawPolygon(Matrix4x4 matrix, Vector3[] vertices, Color color)
		{
			Vector3[] transformedVertices = vertices;
			if (!matrix.isIdentity)
			{
				transformedVertices = new Vector3[vertices.Length];
				for (int i = 0; i < transformedVertices.Length; i++)
					transformedVertices[i] = matrix.MultiplyPoint(vertices[i]);
			}	
			DrawPolygon(transformedVertices, color);
		}
		
		public void DrawPolygon(Vector3[] vertices, Color color)
		{
			var triangleMeshIndex	= currentTriangleMesh;
			var triangleMesh		= triangleMeshes[currentTriangleMesh];

			if (triangleMesh.VertexCount + ((vertices.Length * 3) - 2) > 65535) { currentTriangleMesh++; if (currentTriangleMesh > triangleMeshes.Count) triangleMeshes.Add(new TriangleMesh()); triangleMesh = triangleMeshes[currentTriangleMesh]; }

			triangleMesh.AddPolygon(vertices, color);

			currentTriangleMesh = triangleMeshIndex;
		}
		


		public void DrawPolygon(Matrix4x4 matrix, Vector3[] vertices, Color[] colors)
		{
			Vector3[] transformedVertices = vertices;
			if (!matrix.isIdentity)
			{
				transformedVertices = new Vector3[vertices.Length];
				for (int i = 0; i < transformedVertices.Length; i++)
					transformedVertices[i] = matrix.MultiplyPoint(vertices[i]);
			}	
			DrawPolygon(transformedVertices, colors);
		}
		
		public void DrawPolygon(Vector3[] vertices, Color[] colors)
		{
			var triangleMeshIndex	= currentTriangleMesh;
			var triangleMesh		= triangleMeshes[currentTriangleMesh];

			if (triangleMesh.VertexCount + ((vertices.Length * 3) - 2) > 65535) { currentTriangleMesh++; if (currentTriangleMesh > triangleMeshes.Count) triangleMeshes.Add(new TriangleMesh()); triangleMesh = triangleMeshes[currentTriangleMesh]; }

			triangleMesh.AddPolygon(vertices, colors);

			currentTriangleMesh = triangleMeshIndex;
		}
		
		

		public void DrawTriangles(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color color)
		{
			if (matrix.isIdentity)
			{
				DrawTriangles(vertices, indices, color);
			} else
			{
				var transformedVertices = new Vector3[indices.Length];
				for (int i = 0; i < transformedVertices.Length; i++)
					transformedVertices[i] = matrix.MultiplyPoint(vertices[indices[i]]);
				DrawTriangles(transformedVertices, color);
			}
		}

		public void DrawTriangles(Matrix4x4 matrix, Vector3[] vertices, Color color)
		{
			Vector3[] transformedVertices = vertices;
			if (!matrix.isIdentity)
			{
				transformedVertices = new Vector3[vertices.Length];
				for (int i = 0; i < transformedVertices.Length; i++)
					transformedVertices[i] = matrix.MultiplyPoint(vertices[i]);
			}
			DrawTriangles(transformedVertices, color);
		}
		
		public void DrawTriangles(Vector3[] vertices, int[] indices, Color color)
		{
			var triangleMeshIndex	= currentTriangleMesh;
			var triangleMesh		= triangleMeshes[currentTriangleMesh];

			if (triangleMesh.VertexCount + vertices.Length > 65535) { currentTriangleMesh++; if (currentTriangleMesh > triangleMeshes.Count) triangleMeshes.Add(new TriangleMesh()); triangleMesh = triangleMeshes[currentTriangleMesh]; }

			triangleMesh.AddTriangles(vertices, indices, color);

			currentTriangleMesh = triangleMeshIndex;
		}
		
		public void DrawTriangles(Vector3[] vertices, Color color)
		{
			var triangleMeshIndex	= currentTriangleMesh;
			var triangleMesh		= triangleMeshes[currentTriangleMesh];

			if (triangleMesh.VertexCount + vertices.Length > 65535) { currentTriangleMesh++; if (currentTriangleMesh > triangleMeshes.Count) triangleMeshes.Add(new TriangleMesh()); triangleMesh = triangleMeshes[currentTriangleMesh]; }

			triangleMesh.AddTriangles(vertices, color);

			currentTriangleMesh = triangleMeshIndex;
		}
		


		public void DrawTriangles(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color[] colors)
		{
			if (matrix.isIdentity)
			{				
				DrawTriangles(vertices, indices, colors);
			} else
			{
				var transformedVertices = new Vector3[indices.Length];
				var indicedColors		= new Color[indices.Length];
				for (int i = 0; i < transformedVertices.Length; i++)
				{
					var index = indices[i];
					indicedColors[i] = colors[index];
					transformedVertices[i] = matrix.MultiplyPoint(vertices[index]);
				}
				DrawTriangles(transformedVertices, indicedColors);
			} 
		}

		public void DrawTriangles(Matrix4x4 matrix, Vector3[] vertices, Color[] colors)
		{
			Vector3[] transformedVertices = vertices;
			if (!matrix.isIdentity)
			{				
				transformedVertices = new Vector3[vertices.Length];
				for (int i = 0; i < transformedVertices.Length; i++)
					transformedVertices[i] = matrix.MultiplyPoint(vertices[i]);
			} 
			DrawTriangles(transformedVertices, colors);
		}
		
		public void DrawTriangles(Vector3[] vertices, int[] indices, Color[] colors)
		{
			var triangleMeshIndex	= currentTriangleMesh;
			var triangleMesh		= triangleMeshes[currentTriangleMesh];

			if (triangleMesh.VertexCount + vertices.Length > 65535) { currentTriangleMesh++; if (currentTriangleMesh > triangleMeshes.Count) triangleMeshes.Add(new TriangleMesh()); triangleMesh = triangleMeshes[currentTriangleMesh]; }

			triangleMesh.AddTriangles(vertices, indices, colors);

			currentTriangleMesh = triangleMeshIndex;
		}
		
		public void DrawTriangles(Vector3[] vertices, Color[] colors)
		{
			var triangleMeshIndex	= currentTriangleMesh;
			var triangleMesh		= triangleMeshes[currentTriangleMesh];

			if (triangleMesh.VertexCount + vertices.Length > 65535) { currentTriangleMesh++; if (currentTriangleMesh > triangleMeshes.Count) triangleMeshes.Add(new TriangleMesh()); triangleMesh = triangleMeshes[currentTriangleMesh]; }

			triangleMesh.AddTriangles(vertices, colors);

			currentTriangleMesh = triangleMeshIndex;
		}
		

		internal void Destroy()
		{
			for (int i = 0; i < triangleMeshes.Count; i++)
			{
				triangleMeshes[i].Destroy();
			}
			triangleMeshes.Clear();
			currentTriangleMesh = 0;
		}
		
		internal void Clear()
		{
			currentTriangleMesh = 0;
			for (int i = 0; i < triangleMeshes.Count; i++) triangleMeshes[i].Clear();
		}
	}
}
