using System;
using InternalRealtimeCSG;
using UnityEngine;
namespace RealtimeCSG
{
#if !DEMO
	public 
#else
	internal
#endif
	enum TextureMatrixSpace
	{
		WorldSpace,
		PlaneSpace
	}

#if !DEMO
	public 
#else
	internal
#endif
	static class BrushFactory
	{
		#region CreateBrush (internal)
		internal static CSGBrush CreateBrush(Transform parent, string brushName, ControlMesh controlMesh, Shape shape)
		{
			var gameObject = OperationsUtility.CreateGameObject(parent, brushName, false);
			if (!gameObject)
				return null;
			var brush = gameObject.AddComponent<CSGBrush>();
			if (!brush)
				return null;
			brush.ControlMesh = controlMesh;
			brush.Shape = shape;
			if (brush.ControlMesh != null)
				brush.ControlMesh.SetDirty();
			if (brush.Shape != null)
				ShapeUtility.EnsureInitialized(brush.Shape);
			return brush;
		}
		#endregion
		
		#region CreateBrushComponent (internal)
		internal static CSGBrush CreateBrushComponent(GameObject gameObject, ControlMesh controlMesh, Shape shape)
		{
			var brush = gameObject.AddComponent<CSGBrush>();
			if (!brush)
				return null;
			brush.ControlMesh = controlMesh;
			brush.Shape = shape;
			if (brush.ControlMesh != null)
				brush.ControlMesh.SetDirty();
			if (brush.Shape != null)
				ShapeUtility.EnsureInitialized(brush.Shape);
			return brush;
		}
		#endregion


		#region CreateControlMeshFromPlanes (internal)
		internal static bool CreateControlMeshFromPlanes(out ControlMesh		controlMesh, 
														 out Shape				shape,
														 UnityEngine.Plane[]	planes, 
														 Vector3[]				tangents = null, 
														 Vector3[]				binormals = null, 
														 Material[]				materials = null, 
														 Matrix4x4[]			textureMatrices = null,
														 TextureMatrixSpace		textureMatrixSpace = TextureMatrixSpace.WorldSpace, 
														 uint[]					smoothingGroups = null, 
														 TexGenFlags[]			texGenFlags = null)
		{
			controlMesh = null;
			shape = null;
			if (planes == null)
			{
				Debug.LogError("The planes array is not allowed to be null");
				return false;
			}
			if (planes.Length < 4)
			{
				Debug.LogError("The planes array must have at least 4 planes");
				return false;
			}
			if (materials == null)
			{
				materials = new Material[planes.Length];
				for (int i = 0; i < materials.Length; i++)
					materials[i] = MaterialUtility.WallMaterial;
			}
			if (planes.Length != materials.Length ||
				(textureMatrices != null && planes.Length != textureMatrices.Length) ||
				(tangents != null && tangents.Length != textureMatrices.Length) ||
				(binormals != null && binormals.Length != textureMatrices.Length) ||
				(smoothingGroups != null && smoothingGroups.Length != materials.Length))
			{
				Debug.LogError("All non null arrays need to be of equal length");
				return false;
			}

			shape = new Shape();
			shape.Materials = materials;
			shape.TexGenFlags = new TexGenFlags[planes.Length];
			shape.Surfaces = new Surface[planes.Length];
			shape.TexGens = new TexGen[planes.Length];
			for (int i = 0; i < planes.Length; i++)
			{
				shape.Surfaces[i].Plane = new CSGPlane(planes[i].normal, -planes[i].distance);
				Vector3 tangent, binormal;
				if (tangents != null && binormals != null)
				{
					tangent = tangents[i];
					binormal = binormals[i];
				}
				else
				{
					GeometryUtility.CalculateTangents(planes[i].normal, out tangent, out binormal);
				}

				shape.Surfaces[i].Tangent  = -tangent;
				shape.Surfaces[i].BiNormal = -binormal;
				shape.Surfaces[i].TexGenIndex = i;
				shape.TexGens[i] = new TexGen(-1);
				if (smoothingGroups != null)
					shape.TexGens[i].SmoothingGroup = smoothingGroups[i];
				if (texGenFlags != null)
					shape.TexGenFlags[i] = texGenFlags[i];
			}

			controlMesh = ControlMeshUtility.CreateFromShape(shape);
			if (controlMesh == null || !ControlMeshUtility.Validate(controlMesh, shape))
				return false;

			if (textureMatrices != null)
			{
				for (var i = 0; i < planes.Length; i++)
				{
					SurfaceUtility.AlignTextureSpaces(textureMatrices[i], textureMatrixSpace == TextureMatrixSpace.PlaneSpace, ref shape.TexGens[i], ref shape.TexGenFlags[i], ref shape.Surfaces[i]);
				}
			}
			ShapeUtility.CreateCutter(shape, controlMesh);
			return true;
		}
		#endregion

		// Create a brush from an array of planes that define the convex space.
		// optionally it's possible, for each plane, to supply a material and a texture-matrix that defines how the texture is translated/rotated/scaled.
		#region CreateBrushFromPlanes (public)
		public static CSGBrush CreateBrushFromPlanes(Transform parent, string brushName, UnityEngine.Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
		{
			ControlMesh controlMesh;
			Shape shape;
			if (!CreateControlMeshFromPlanes(out controlMesh, out shape, planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace))
				return null;
			return CreateBrush(parent, brushName, controlMesh, shape);
		}

		public static CSGBrush CreateBrushFromPlanes(string brushName, UnityEngine.Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
		{
			return CreateBrushFromPlanes(null, brushName, planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace);
		}

		public static CSGBrush CreateBrushFromPlanes(UnityEngine.Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
		{
			return CreateBrushFromPlanes(null, "Brush", planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace);
		}

		public static CSGBrush CreateBrushFromPlanes(GameObject				gameObject,
													 UnityEngine.Plane[]	planes, 
													 Vector3[]				tangents = null, 
													 Vector3[]				binormals = null, 
													 Material[]				materials = null, 
													 Matrix4x4[]			textureMatrices = null,
													 TextureMatrixSpace		textureMatrixSpace = TextureMatrixSpace.WorldSpace, 
													 uint[]					smoothingGroups = null, 
													 TexGenFlags[]			texGenFlags = null)
		{ 
			ControlMesh controlMesh; 
			Shape shape;
				
			if (!BrushFactory.CreateControlMeshFromPlanes(out controlMesh, 
														  out shape,
														  planes,
														  tangents,
														  binormals,
														  materials,
														  textureMatrices,
														  textureMatrixSpace,
														  smoothingGroups,
														  texGenFlags))
				return null;

			return BrushFactory.CreateBrushComponent(gameObject, controlMesh, shape);
		}
		#endregion

		#region SetBrushFromPlanes (public)
		public static bool SetBrushFromPlanes(CSGBrush brush, UnityEngine.Plane[] planes, Vector3[] tangents = null, Vector3[] binormals = null, Material[] materials = null, Matrix4x4[] textureMatrices = null, TextureMatrixSpace textureMatrixSpace = TextureMatrixSpace.WorldSpace)
		{
			if (!brush)
				return false;

			ControlMesh controlMesh;
			Shape shape;
			if (!CreateControlMeshFromPlanes(out controlMesh, out shape, planes, tangents, binormals, materials, textureMatrices, textureMatrixSpace))
				return false;

			brush.ControlMesh = controlMesh;
			brush.Shape = shape;
			if (brush.ControlMesh != null)
				brush.ControlMesh.SetDirty();
			if (brush.Shape != null)
				ShapeUtility.EnsureInitialized(brush.Shape);
			return true;
		}
		#endregion



		#region CreateCubeControlMesh (internal)
		internal static void CreateCubeControlMesh(out ControlMesh controlMesh, out Shape shape, Vector3 size)
		{
			size *= 0.5f;
			controlMesh = new ControlMesh();
			controlMesh.Vertices = new Vector3[]
			{
				new Vector3(-size.x,-size.y,-size.z),
				new Vector3(-size.x, size.y,-size.z),
				new Vector3( size.x, size.y,-size.z),
				new Vector3( size.x,-size.y,-size.z),

				new Vector3(-size.x,-size.y, size.z),
				new Vector3(-size.x, size.y, size.z),
				new Vector3( size.x, size.y, size.z),
				new Vector3( size.x,-size.y, size.z)
			};

			controlMesh.Edges = new HalfEdge[]
			{
				new HalfEdge(0, 21,  0, true),	//  0
				new HalfEdge(0,  9,  1, true),	//  1
				new HalfEdge(0, 13,  2, true),	//  2
				new HalfEdge(0, 17,  3, true),	//  3

				new HalfEdge(1, 23,  7, true),	//  4
				new HalfEdge(1, 19,  6, true),	//  5
				new HalfEdge(1, 15,  5, true),	//  6
				new HalfEdge(1, 11,  4, true),	//  7

				new HalfEdge(2, 14,  1, true),	//  8
				new HalfEdge(2,  1,  0, true),	//  9
				new HalfEdge(2, 20,  4, true),	// 10
				new HalfEdge(2,  7,  5, true),	// 11

				new HalfEdge(3, 18,  2, true),	// 12
				new HalfEdge(3,  2,  1, true),	// 13
				new HalfEdge(3,  8,  5, true),	// 14
				new HalfEdge(3,  6,  6, true),	// 15

				new HalfEdge(4, 22,  3, true),	// 16
				new HalfEdge(4,  3,  2, true),	// 17
				new HalfEdge(4, 12,  6, true),	// 18
				new HalfEdge(4,  5,  7, true),	// 19

				new HalfEdge(5, 10,  0, true),	// 20
				new HalfEdge(5,  0,  3, true),	// 21
				new HalfEdge(5, 16,  7, true),	// 22
				new HalfEdge(5,  4,  4, true)	// 23
			};

			controlMesh.Polygons = new Polygon[]
			{
				// left/right
				new Polygon(new int[] {  0,  1,  2,  3 }, 0),	// 0
				new Polygon(new int[] {  7,  4,  5,  6 }, 1),   // 1
				
				// front/back
				new Polygon(new int[] {  9, 10, 11,  8 }, 2),	// 2
				new Polygon(new int[] { 13, 14, 15, 12 }, 3),	// 3
				
				// top/down
				new Polygon(new int[] { 16, 17, 18, 19 }, 4),	// 4
				new Polygon(new int[] { 20, 21, 22, 23 }, 5)	// 5
			};

			shape = new Shape();

			shape.Surfaces = new Surface[6];
			shape.Surfaces[0].TexGenIndex = 0;
			shape.Surfaces[1].TexGenIndex = 1;
			shape.Surfaces[2].TexGenIndex = 2;
			shape.Surfaces[3].TexGenIndex = 3;
			shape.Surfaces[4].TexGenIndex = 4;
			shape.Surfaces[5].TexGenIndex = 5;

			shape.Surfaces[0].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 0);
			shape.Surfaces[1].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 1);
			shape.Surfaces[2].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 2);
			shape.Surfaces[3].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 3);
			shape.Surfaces[4].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 4);
			shape.Surfaces[5].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, 5);

			GeometryUtility.CalculateTangents(shape.Surfaces[0].Plane.normal, out shape.Surfaces[0].Tangent, out shape.Surfaces[0].BiNormal);
			GeometryUtility.CalculateTangents(shape.Surfaces[1].Plane.normal, out shape.Surfaces[1].Tangent, out shape.Surfaces[1].BiNormal);
			GeometryUtility.CalculateTangents(shape.Surfaces[2].Plane.normal, out shape.Surfaces[2].Tangent, out shape.Surfaces[2].BiNormal);
			GeometryUtility.CalculateTangents(shape.Surfaces[3].Plane.normal, out shape.Surfaces[3].Tangent, out shape.Surfaces[3].BiNormal);
			GeometryUtility.CalculateTangents(shape.Surfaces[4].Plane.normal, out shape.Surfaces[4].Tangent, out shape.Surfaces[4].BiNormal);
			GeometryUtility.CalculateTangents(shape.Surfaces[5].Plane.normal, out shape.Surfaces[5].Tangent, out shape.Surfaces[5].BiNormal);

			shape.TexGens = new TexGen[6];
			shape.TexGens[0].Scale = MathConstants.oneVector3;
			shape.TexGens[1].Scale = MathConstants.oneVector3;
			shape.TexGens[2].Scale = MathConstants.oneVector3;
			shape.TexGens[3].Scale = MathConstants.oneVector3;
			shape.TexGens[4].Scale = MathConstants.oneVector3;
			shape.TexGens[5].Scale = MathConstants.oneVector3;


			shape.TexGens[0].Color = Color.white;
			shape.TexGens[1].Color = Color.white;
			shape.TexGens[2].Color = Color.white;
			shape.TexGens[3].Color = Color.white;
			shape.TexGens[4].Color = Color.white;
			shape.TexGens[5].Color = Color.white;


			shape.TexGenFlags = new TexGenFlags[6];
			shape.TexGenFlags[0] = TexGenFlags.None;
			shape.TexGenFlags[1] = TexGenFlags.None;
			shape.TexGenFlags[2] = TexGenFlags.None;
			shape.TexGenFlags[3] = TexGenFlags.None;
			shape.TexGenFlags[4] = TexGenFlags.None;
			shape.TexGenFlags[5] = TexGenFlags.None;

			shape.Materials = new Material[6];
			shape.Materials[0] = MaterialUtility.WallMaterial;
			shape.Materials[1] = MaterialUtility.WallMaterial;
			shape.Materials[2] = MaterialUtility.WallMaterial;
			shape.Materials[3] = MaterialUtility.FloorMaterial;
			shape.Materials[4] = MaterialUtility.WallMaterial;
			shape.Materials[5] = MaterialUtility.FloorMaterial;

			//controlMesh.Validate();
			ShapeUtility.CreateCutter(shape, controlMesh);
			ShapeUtility.EnsureInitialized(shape);
			controlMesh.IsValid = ControlMeshUtility.Validate(controlMesh, shape);
		}
		#endregion

		#region SetBrushCubeMesh (public)
		public static bool SetBrushCubeMesh(CSGBrush brush, Vector3 size)
		{
			if (!brush)
				return false;

			ControlMesh controlMesh;
			Shape shape;
			BrushFactory.CreateCubeControlMesh(out controlMesh, out shape, size);

			brush.ControlMesh = controlMesh;
			brush.Shape = shape;
			if (brush.ControlMesh != null)
				brush.ControlMesh.SetDirty();
			if (brush.Shape != null)
				ShapeUtility.EnsureInitialized(brush.Shape);
			return true;
		}

		public static bool SetBrushCubeMesh(CSGBrush brush)
		{
			return SetBrushCubeMesh(brush, Vector3.one);
		}
		#endregion

		#region CreateCubeBrush (public)
		public static CSGBrush CreateCubeBrush(Transform parent, string brushName, Vector3 size)
		{
			ControlMesh controlMesh;
			Shape shape;
			BrushFactory.CreateCubeControlMesh(out controlMesh, out shape, size);
			
			return CreateBrush(parent, brushName, controlMesh, shape);
		}
		public static CSGBrush CreateCubeBrush(string brushName, Vector3 size)
		{
			return CreateCubeBrush(null, brushName, size);
		}
		public static CSGBrush CreateCubeBrush(Vector3 size)
		{
			return CreateCubeBrush(null, "Brush", size);
		}
		#endregion
	}
}