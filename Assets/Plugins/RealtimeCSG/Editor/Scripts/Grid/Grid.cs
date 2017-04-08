using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal enum GridAxis
	{
		AxisXZ = 0,
		AxisYZ = 1,
		AxisXY = 2
	}

	internal enum GridMode
	{
		Regular,
		Ortho,
		WorkPlane
	}
	internal static class Grid
	{
		/*
		static GridRenderer gridRenderer;
		static GridRenderer GridRenderer
		{
			get
			{
				if (gridRenderer == null)
				{
					var scene = SceneManager.GetActiveScene();
					var gameObjects = scene.GetRootGameObjects();
					foreach(var gameobject in gameObjects)
					{
						var gridRenderers = gameobject.GetComponentsInChildren<GridRenderer>();
						for (int i=gridRenderers.Length-1;i>=0;i--)
						{
							GameObject.DestroyImmediate(gridRenderers[i]);
						}
					}

					var gameObject = new GameObject();
					gameObject.name = "GridRenderer";
					gameObject.hideFlags = HideFlags.HideAndDontSave;
					gridRenderer = gameObject.AddComponent<GridRenderer>();
				}
				return gridRenderer;
			}
		}
		*/

		static Material lineMaterial_ = null;
		static int normalID = -1;
		static Material LineMaterial
		{
			get
			{
#if UNITY_EDITOR 
				if (!lineMaterial_)
				{
					var shader = Shader.Find("Hidden/CSG/internal/Grid");
					if (shader == null)
						return null;
					normalID = Shader.PropertyToID("_Normal");
					lineMaterial_ = new Material(shader); 
					lineMaterial_.hideFlags = HideFlags.HideAndDontSave;
					// Turn on alpha blending
					lineMaterial_.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					lineMaterial_.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					// Turn backface culling off
					lineMaterial_.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
					// Turn off depth writes
					lineMaterial_.SetInt("_ZWrite", 0);
					lineMaterial_.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
				}
#endif
				return lineMaterial_;
			}
		}
		static Material orthoLineMaterial_ = null;
		static int alphaID = -1;
		static int depthID = -1;
		static int ztestID = -1;
		static Material OrthoLineMaterial
		{
			get
			{
#if UNITY_EDITOR
				if (!orthoLineMaterial_)
				{
					var shader = Shader.Find("Hidden/CSG/internal/OrthoGrid");
					if (shader == null)
						return null;
					alphaID = Shader.PropertyToID("_Alpha");
					depthID = Shader.PropertyToID("_Depth");
					ztestID = Shader.PropertyToID("_ZTest");
					orthoLineMaterial_ = new Material(shader); 
					orthoLineMaterial_.hideFlags = HideFlags.HideAndDontSave;
					// Turn on alpha blending
					orthoLineMaterial_.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					orthoLineMaterial_.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					// Turn backface culling off
					orthoLineMaterial_.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
					// Turn off depth writes
					orthoLineMaterial_.SetInt("_ZWrite", 0);
					orthoLineMaterial_.SetInt(ztestID, (int)UnityEngine.Rendering.CompareFunction.Always);
				}
#endif
				return orthoLineMaterial_;
			}
		}
		/*
		static Material backgroundMaterial_ = null;
		static int colorID = -1;
		static Material BackgroundMaterial
		{
			get
			{
#if UNITY_EDITOR
				if (!backgroundMaterial_)
				{
					var shader = Shader.Find("Hidden/CSG/internal/Background");
					if (shader == null)
						return null;
					colorID = Shader.PropertyToID("_Color");
					backgroundMaterial_ = new Material(shader); 
					backgroundMaterial_.hideFlags = HideFlags.HideAndDontSave;
					// Turn on alpha blending
					backgroundMaterial_.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					backgroundMaterial_.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					// Turn backface culling off
					backgroundMaterial_.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
					// Turn off depth writes
					backgroundMaterial_.SetInt("_ZWrite", 1);
					backgroundMaterial_.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
				}
#endif
				return backgroundMaterial_;
			}
		}
		*/
		struct GridDescription
		{
			public float snapSizeX;
			public float snapSizeY;
			public float snapSizeZ;
			public int gridLevelDown;
			public float gridFraction;
			public float alpha;
			public GridAxis axis;
			public GridMode gridMode;
			public SceneView sceneView;
			public Mesh mesh;
		}

		static readonly List<GridDescription> GridMeshes = new List<GridDescription>();

		private const int totalVertexCount = (colorCount * ((maxLineCount + 1) * 2)) + (colorCount * ((maxLineCount + 1) * 2));
		private static readonly List<Vector3>	verticesList	= new List<Vector3>(totalVertexCount);
		private static readonly List<Color>		colorsList		= new List<Color>(totalVertexCount);
		private static readonly List<int>		indicesList		= new List<int>(totalVertexCount);


		const int maxLineCount	= 25;
		const int colorCount	= 16;
		
		public static void ClearGridCache()
		{
			foreach (var gridMesh in GridMeshes)
				GameObject.DestroyImmediate(gridMesh.mesh);
			GridMeshes.Clear();
		}
		
		static void FillGridMesh(Mesh mesh, GridDescription gridDescription)
		{
			float	snapSizeX		= gridDescription.snapSizeX;
			float	snapSizeY		= gridDescription.snapSizeY;
			float	snapSizeZ		= gridDescription.snapSizeZ;

			Color[] line_colors_a;
			Color[] line_colors_b;
			float snap_size_a;
			float snap_size_b;
			
			var lineColorsX = new Color[colorCount]
			{
				ColorSettings.gridColor1X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, // 7
				ColorSettings.gridColor1X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X, ColorSettings.gridColor2X  // 15
			};

			var lineColorsY = new Color[colorCount]
			{
				ColorSettings.gridColor1Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, // 7
				ColorSettings.gridColor1Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y, ColorSettings.gridColor2Y  // 15
			};

			var lineColorsZ = new Color[colorCount]
			{
				ColorSettings.gridColor1Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, // 7
				ColorSettings.gridColor1Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z, ColorSettings.gridColor2Z  // 15
			};

			var lineColorsW = new Color[colorCount]
			{
				ColorSettings.gridColor1W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, // 7
				ColorSettings.gridColor1W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W, ColorSettings.gridColor2W  // 15
			};


			if (gridDescription.gridMode == GridMode.WorkPlane)
			{
				Color mainW;
				Color subLineW;
				mainW = ColorSettings.gridColor1W;
				mainW = Color.Lerp(ColorSettings.gridColor1W, ColorSettings.gridColor2W, gridDescription.gridFraction);

				subLineW = ColorSettings.gridColor2W;
				subLineW.a *= 1.0f - gridDescription.gridFraction;

				lineColorsW[ 1] = subLineW;
				lineColorsW[ 3] = subLineW;
				lineColorsW[ 5] = subLineW;
				lineColorsW[ 7] = subLineW;
				lineColorsW[ 8] = mainW;
				lineColorsW[ 9] = subLineW;
				lineColorsW[11] = subLineW;
				lineColorsW[13] = subLineW;
				lineColorsW[15] = subLineW;
				
				line_colors_a = lineColorsW;
				line_colors_b = lineColorsW;
				snap_size_a = snapSizeX;
				snap_size_b = snapSizeZ;
			} else
			{
				Color mainX, mainY, mainZ;
				Color subLineX, subLineY, subLineZ;
				mainX = ColorSettings.gridColor1X; 
				mainY = ColorSettings.gridColor1Y;
				mainZ = ColorSettings.gridColor1Z;
				mainX = Color.Lerp(ColorSettings.gridColor1X, ColorSettings.gridColor2X, gridDescription.gridFraction);
				mainY = Color.Lerp(ColorSettings.gridColor1Y, ColorSettings.gridColor2Y, gridDescription.gridFraction);
				mainZ = Color.Lerp(ColorSettings.gridColor1Z, ColorSettings.gridColor2Z, gridDescription.gridFraction);

				subLineX = ColorSettings.gridColor2X;
				subLineY = ColorSettings.gridColor2Y;
				subLineZ = ColorSettings.gridColor2Z;
				subLineX.a *= 1.0f - gridDescription.gridFraction;
				subLineY.a *= 1.0f - gridDescription.gridFraction;
				subLineZ.a *= 1.0f - gridDescription.gridFraction;

				lineColorsX[ 1] = subLineX;
				lineColorsX[ 3] = subLineX;
				lineColorsX[ 5] = subLineX;
				lineColorsX[ 7] = subLineX;
				lineColorsX[ 8] = mainX;
				lineColorsX[ 9] = subLineX;
				lineColorsX[11] = subLineX;
				lineColorsX[13] = subLineX;
				lineColorsX[15] = subLineX;

				lineColorsY[ 1] = subLineY;
				lineColorsY[ 3] = subLineY;
				lineColorsY[ 5] = subLineY;
				lineColorsY[ 7] = subLineY;
				lineColorsY[ 8] = mainY;
				lineColorsY[ 9] = subLineY;
				lineColorsY[11] = subLineY;
				lineColorsY[13] = subLineY;
				lineColorsY[15] = subLineY;

				lineColorsZ[ 1] = subLineZ;
				lineColorsZ[ 3] = subLineZ;
				lineColorsZ[ 5] = subLineZ;
				lineColorsZ[ 7] = subLineZ;
				lineColorsZ[ 8] = mainZ;
				lineColorsZ[ 9] = subLineZ;
				lineColorsZ[11] = subLineZ;
				lineColorsZ[13] = subLineZ;
				lineColorsZ[15] = subLineZ;

				switch (gridDescription.axis)
				{
					default:
					case GridAxis.AxisXZ:
					{
						line_colors_a = lineColorsZ;
						line_colors_b = lineColorsX;
						snap_size_a = snapSizeX;
						snap_size_b = snapSizeZ;
						break;
					}
					case GridAxis.AxisYZ:
					{
						line_colors_a = lineColorsZ;
						line_colors_b = lineColorsY;
						snap_size_a = snapSizeY;
						snap_size_b = snapSizeZ;
						break;
					}
					case GridAxis.AxisXY:
					{
						line_colors_a = lineColorsY;
						line_colors_b = lineColorsX;
						snap_size_a = snapSizeX;
						snap_size_b = snapSizeY;
						break;
					}
				}
			}
			
			
			var		line_count_a	= maxLineCount;
			var		line_count_b	= maxLineCount;
			var		line_step_a		= (snap_size_a / line_colors_a.Length);
			var		line_step_b		= (snap_size_b / line_colors_b.Length);
			float	line_distance_a = (maxLineCount * snap_size_a);
			float	line_distance_b = (maxLineCount * snap_size_b);


			verticesList.Clear();
			colorsList.Clear();
			indicesList.Clear();

			int vertexCount = 0;
			
			Vector3 v_a = MathConstants.zeroVector3, v_b = MathConstants.zeroVector3;
			float astart = -line_count_a * snap_size_a;
			for (int c = 0; c < line_colors_a.Length; c++, astart += line_step_a)
			{
				if (line_colors_a[c].a > 0.03f)
				{
					Color color = line_colors_a[c];
					color.a *= gridDescription.alpha;
					float n = astart;
					for (int a = -line_count_a; a < +line_count_a; a++, n += snap_size_a)
					{
						v_a.x = n; v_b.x = n;
						v_a.z = -line_distance_b; v_b.z =  line_distance_b;
						indicesList.Add(vertexCount); verticesList.Add(v_a); colorsList.Add(color); vertexCount++;
						indicesList.Add(vertexCount); verticesList.Add(v_b); colorsList.Add(color); vertexCount++;
					}
				}
			}

			float bstart = -line_count_b * snap_size_b;
			for (int c = 0; c < line_colors_b.Length; c++, bstart += line_step_b)
			{
				if (line_colors_b[c].a > 0.03f)
				{
					Color color = line_colors_b[c];
					color.a *= gridDescription.alpha;

					float n = bstart;
					for (int b = -line_count_b; b < +line_count_b; b++, n += snap_size_b)
					{
						v_a.x = -line_distance_a; v_b.x =  line_distance_a;
						v_a.z = n; v_b.z = n;
						indicesList.Add(vertexCount); verticesList.Add(v_a); colorsList.Add(color); vertexCount++;
						indicesList.Add(vertexCount); verticesList.Add(v_b); colorsList.Add(color); vertexCount++;
					}
				}
			}

			mesh.Clear(false);
			mesh.SetVertices(verticesList);
			mesh.SetColors(colorsList);
			mesh.subMeshCount = 1;
			mesh.SetIndices(indicesList.ToArray(), MeshTopology.Lines, 0);
			mesh.UploadMeshData(true);
		}

		static void DrawGrid(Camera camera, Vector3 camera_position, Vector3 grid_center, Quaternion rotation, GridAxis axis, float alpha, GridMode gridMode)
		{
			if (alpha <= 0.03f)
				return;
			
			// find the nearest point on the plane
			var	normal	= rotation * MathConstants.upVector3;
			var d		= Vector3.Dot(normal, grid_center);
			var t		= (Vector3.Dot(normal, camera_position) - d) / Vector3.Dot(normal, normal);
			var	rotated_plane_position	= camera_position - (t * normal);
			
			var	snap_x				= RealtimeCSG.CSGSettings.SnapVector.x;
			var	snap_y				= RealtimeCSG.CSGSettings.SnapVector.y;
			var	snap_z				= RealtimeCSG.CSGSettings.SnapVector.z;
			
			// calculate a point on the camera at the same distance as the camera is to the grid, in world-space
			var	forward				= camera.cameraToWorldMatrix.MultiplyVector(MathConstants.forwardVector3);
			var	projectedCenter		= camera_position - (t * forward);

			// calculate the snap sizes relatively to that point in the center (this makes them relative to camera position, but not rotation)
			var sideways			= camera.cameraToWorldMatrix.MultiplyVector(MathConstants.leftVector3);
			var screenPointCenter	= camera.WorldToScreenPoint(projectedCenter);
			var screenPointAxisX	= camera.WorldToScreenPoint(projectedCenter + (sideways * snap_x));
			var screenPointAxisY	= camera.WorldToScreenPoint(projectedCenter + (sideways * snap_y));
			var screenPointAxisZ	= camera.WorldToScreenPoint(projectedCenter + (sideways * snap_z));
			var snapPixelSizeX		= (screenPointAxisX - screenPointCenter).magnitude; // size in pixels
			var snapPixelSizeY		= (screenPointAxisY - screenPointCenter).magnitude; // size in pixels
			var snapPixelSizeZ		= (screenPointAxisZ - screenPointCenter).magnitude; // size in pixels

			float snapPixelSize;
			switch (axis)
			{
				default:
				case GridAxis.AxisXZ: { snapPixelSize = Mathf.Min(snapPixelSizeX, snapPixelSizeZ); break; } //X/Z
				case GridAxis.AxisYZ: { snapPixelSize = Mathf.Min(snapPixelSizeY, snapPixelSizeZ); break; } //Y/Z
				case GridAxis.AxisXY: { snapPixelSize = Mathf.Min(snapPixelSizeX, snapPixelSizeY); break; } //X/Y
			}

			const float minPixelSize = 16.0f;

			float	gridLevel		= Mathf.Max(0, Mathf.Log(minPixelSize / snapPixelSize, 2.0F));
			int		gridLevelDown	= Mathf.FloorToInt(gridLevel);

			//float	gridFraction	= gridLevel - gridLevelDown;
			//if (gridMode == GridMode.Ortho && alpha >= 1.0f) { gridFraction = 0; }

			float	snapSizeX		= (1 << gridLevelDown) * snap_x * minPixelSize;
			float	snapSizeY		= (1 << gridLevelDown) * snap_y * minPixelSize;
			float	snapSizeZ		= (1 << gridLevelDown) * snap_z * minPixelSize;

			Color axis_color_a;
			Color axis_color_b;
			float snap_size_a;
			float snap_size_b;

			if (gridMode == GridMode.WorkPlane)
			{
				axis_color_a = ColorSettings.gridColorW; 
				axis_color_b = ColorSettings.gridColorW;
				snap_size_a = snapSizeX;
				snap_size_b = snapSizeZ;
			} else
			{
				switch (axis)
				{
					default:
					case GridAxis.AxisXZ:
					{
						axis_color_a = ColorSettings.gridColorZ;
						axis_color_b = ColorSettings.gridColorX;
						snap_size_a = snapSizeX;
						snap_size_b = snapSizeZ;
						break;
					}
					case GridAxis.AxisYZ:
					{
						axis_color_a = ColorSettings.gridColorZ;
						axis_color_b = ColorSettings.gridColorY;
						snap_size_a = snapSizeY;
						snap_size_b = snapSizeZ;
						break;
					}
					case GridAxis.AxisXY:
					{
						axis_color_a = ColorSettings.gridColorY;
						axis_color_b = ColorSettings.gridColorX;
						snap_size_a = snapSizeX;
						snap_size_b = snapSizeY;
						break;
					}
				}
			}


			//var quantizedGridFraction = Mathf.Round(gridFraction * 16);

			var currentSceneView = SceneView.currentDrawingSceneView;
			Mesh gridMesh = null;
			for (int i = GridMeshes.Count - 1; i >= 0; i--)
			{
				if (!GridMeshes[i].sceneView)
				{
					UnityEngine.Object.DestroyImmediate(GridMeshes[i].mesh);
					GridMeshes.RemoveAt(i);
					continue;
				}

				if (GridMeshes[i].sceneView != currentSceneView ||
					GridMeshes[i].gridMode != gridMode)
				{
					continue;
				}
				
				gridMesh = GridMeshes[i].mesh;
				if (!gridMesh ||
					(GridMeshes[i].snapSizeX != snapSizeX ||
					GridMeshes[i].snapSizeY != snapSizeY ||
					GridMeshes[i].snapSizeZ != snapSizeZ ||
					GridMeshes[i].gridLevelDown != gridLevelDown ||
					//GridMeshes[i].gridFraction != quantizedGridFraction ||
					GridMeshes[i].alpha != alpha ||
					GridMeshes[i].axis != axis ||
					GridMeshes[i].gridMode != gridMode))
				{
					if (!gridMesh)
						gridMesh = new Mesh();
					else 
						gridMesh.Clear(true);
					var gridDescription = GridMeshes[i];
					/*
					if (gridDescription.snapSizeX		!= snapSizeX) Debug.Log("snapSizeX");
					if (gridDescription.snapSizeY		!= snapSizeY) Debug.Log("snapSizeY");
					if (gridDescription.snapSizeZ		!= snapSizeZ) Debug.Log("snapSizeZ");
					if (gridDescription.gridLevelDown	!= gridLevelDown) Debug.Log("gridLevelDown");
					if (gridDescription.alpha			!= alpha) Debug.Log("alpha");
					if (gridDescription.axis			!= axis) Debug.Log("axis");
					if (gridDescription.gridMode		!= gridMode) Debug.Log("gridMode");
					//if (gridDescription.gridFraction	!= quantizedGridFraction) Debug.Log("quantizedGridFraction");
					if (gridDescription.sceneView		!= currentSceneView) Debug.Log("currentSceneView");
					*/
					gridDescription.snapSizeX		= snapSizeX;
					gridDescription.snapSizeY		= snapSizeY;
					gridDescription.snapSizeZ		= snapSizeZ;
					gridDescription.gridLevelDown	= gridLevelDown;
					gridDescription.alpha			= alpha;
					gridDescription.axis			= axis;
					gridDescription.gridMode		= gridMode;
					gridDescription.gridFraction	= 0.0f;//gridFraction;
					gridDescription.sceneView		= currentSceneView;
					
					FillGridMesh(gridMesh, gridDescription);

					GridMeshes[i] = gridDescription;
				}
				
				break;
			}

			if (gridMesh == null)
			{
				gridMesh = new Mesh();
				gridMesh.MarkDynamic();

				var gridDescription = new GridDescription();
				gridDescription.snapSizeX		= snapSizeX;
				gridDescription.snapSizeY		= snapSizeY;
				gridDescription.snapSizeZ		= snapSizeZ;
				gridDescription.gridLevelDown	= gridLevelDown;
				gridDescription.alpha			= alpha;
				gridDescription.axis			= axis;
				gridDescription.sceneView		= currentSceneView;
				gridDescription.mesh			= gridMesh;
				gridDescription.gridMode		= gridMode;
				gridDescription.gridFraction	= 0.0f;//gridFraction;
				
				FillGridMesh(gridMesh, gridDescription);
				
				GridMeshes.Add(gridDescription);
			}

			// snap the grid in plane-space
			Vector3 plane_position = (Quaternion.Inverse(rotation) * (rotated_plane_position - grid_center));
			
			plane_position.x = Mathf.Floor(plane_position.x / snap_size_b) * snap_size_b;
			plane_position.z = Mathf.Floor(plane_position.z / snap_size_a) * snap_size_a;
			Vector3 grid_position = (rotation * plane_position) + grid_center;
			var matrix = Matrix4x4.TRS(grid_position, rotation, MathConstants.oneVector3);

			if (gridMode == GridMode.Ortho)
			{
				var material = OrthoLineMaterial;
				if (material != null)
				{
					material.SetFloat(alphaID, alpha);
					material.SetFloat(depthID, 0.0f);
					material.SetInt(ztestID, (int)UnityEngine.Rendering.CompareFunction.Always);
					if (material.SetPass(0))
						Graphics.DrawMeshNow(gridMesh, matrix);
					//Graphics.DrawMesh(gridMesh, matrix, material, 0, Camera.current);
				}
			} else
			{
				var material = LineMaterial;
				if (material != null)
				{
					material.SetVector(normalID, normal);
					if (material.SetPass(0))
						Graphics.DrawMeshNow(gridMesh, matrix);
					if (material.SetPass(1))
						Graphics.DrawMeshNow(gridMesh, matrix);
					material.SetPass(0);
				}
			}

			float line_distance_a = (maxLineCount * snap_size_a);
			float line_distance_b = (maxLineCount * snap_size_b);

			GL.PushMatrix();
			GL.MultMatrix(Matrix4x4.TRS(grid_center, rotation, MathConstants.oneVector3));
			GL.Begin(GL.LINES);
			{
				Color color = axis_color_a;
				color.a = alpha;
				GL.Color(color);
				GL.Vertex3(0, 0, -line_distance_b); GL.Vertex3(0, 0, line_distance_b);
			}
			{
				Color color = axis_color_b;
				color.a = alpha;
				GL.Color(color);
				GL.Vertex3(-line_distance_a, 0, 0); GL.Vertex3(line_distance_a, 0, 0);
			}
			GL.End();
			GL.PopMatrix();

		}
		/*
		public static void DrawBackground(Camera camera)
		{
			var material = BackgroundMaterial;
			if (material == null)
				return;

			material.SetColor(colorID, ColorSettings.Background);
			material.SetPass(0);
			
			GL.PushMatrix();

			var pt0 = camera.ViewportToWorldPoint(new Vector3(0, 0, 0));
			var pt1 = camera.ViewportToWorldPoint(new Vector3(0, 1, 0));
			var pt2 = camera.ViewportToWorldPoint(new Vector3(1, 1, 0));
			var pt3 = camera.ViewportToWorldPoint(new Vector3(1, 0, 0));

			GL.Begin(GL.QUADS);
			GL.Vertex3(pt0.x, pt0.y, pt0.z);
			GL.Vertex3(pt1.x, pt1.y, pt1.z);
			GL.Vertex3(pt2.x, pt2.y, pt2.z);
			GL.Vertex3(pt3.x, pt3.y, pt3.z);

			GL.End();

			GL.PopMatrix();
		}
		*/
		public static bool          ForceGrid           = false;
		static Vector3				forcedGridCenter	= MathConstants.zeroVector3;
		public static Vector3		ForcedGridCenter
		{
			get
			{
				return forcedGridCenter;
			}
			set
			{
				if (forcedGridCenter == value)
					return;
				forcedGridCenter = value;
			}
		}
				 
		static Quaternion			forcedGridRotation	= MathConstants.identityQuaternion;
		public static Quaternion	ForcedGridRotation
		{
			get
			{
				return forcedGridRotation;
			}
			set
			{
				if (forcedGridRotation == value)
					return;
				forcedGridRotation = value;
			}
		}
		public static Vector3		CurrentGridCenter
		{
			get
			{
				UpdateGridOrientation();
				return gridOrientation.grid_center;
			}
		}
		public static CSGPlane		CurrentGridPlane		{ get { UpdateGridOrientation(); return gridOrientation.grid_plane; } }
		public static CSGPlane		CurrentWorkGridPlane	{ get { UpdateGridOrientation(); return gridOrientation.grid_work_plane; } }
		public static Vector3		CurrentGridSnapVector	{ get { UpdateGridOrientation(); return gridOrientation.grid_snap_vector; } }
		
		internal sealed class GridOrientation
		{
			public Camera       grid_camera;
			public Vector3      grid_camera_position;
			public Vector3      grid_camera_snapped;

			public Vector3		grid_center				= MathConstants.zeroVector3;
			public Quaternion	grid_rotation			= MathConstants.identityQuaternion;
			public CSGPlane		grid_plane;
			
			public Quaternion	grid_ortho_x_rotation	= MathConstants.identityQuaternion;
			public Quaternion	grid_ortho_y_rotation	= MathConstants.identityQuaternion;
			public Quaternion	grid_ortho_z_rotation	= MathConstants.identityQuaternion;

			public Vector3		grid_work_center		= MathConstants.zeroVector3;
			public Quaternion	grid_work_rotation		= MathConstants.identityQuaternion;
			public Quaternion	grid_work_inv_rotation	= MathConstants.identityQuaternion;
			public CSGPlane		grid_work_plane;

			public Vector3	    grid_snap_vector;
			public Vector3	    grid_snap_scale;
			
			public bool			grid_ortho				= false;
			public bool			grid_ortho_x_visible	= false;
			public bool			grid_ortho_y_visible	= false;
			public bool			grid_ortho_z_visible	= false;
			public float		grid_ortho_x_alpha		= 0.0f;
			public float		grid_ortho_y_alpha		= 0.0f;
			public float		grid_ortho_z_alpha		= 0.0f;
		}

		internal static GridOrientation gridOrientation = null;

		static void OnRender()
		{
			UpdateGridOrientation();
						
			if (gridOrientation.grid_ortho)
			{
//				var sceneView = SceneView.currentDrawingSceneView;
//				if (sceneView != null &&
//					CSGSettings.IsWireframeShown(sceneView))
//					DrawBackground(gridOrientation.grid_camera);

				if (gridOrientation.grid_ortho_x_visible)
					DrawGrid(gridOrientation.grid_camera, gridOrientation.grid_camera_position,
							 gridOrientation.grid_center, gridOrientation.grid_ortho_x_rotation,
							 GridAxis.AxisYZ, gridOrientation.grid_ortho_x_alpha, GridMode.Ortho);
				
				if (gridOrientation.grid_ortho_y_visible)
					DrawGrid(gridOrientation.grid_camera, gridOrientation.grid_camera_position, 
							 gridOrientation.grid_center, gridOrientation.grid_ortho_y_rotation, 
							 GridAxis.AxisXZ, gridOrientation.grid_ortho_y_alpha, GridMode.Ortho);

				if (gridOrientation.grid_ortho_z_visible)
					DrawGrid(gridOrientation.grid_camera, gridOrientation.grid_camera_position, 
							 gridOrientation.grid_center, gridOrientation.grid_ortho_z_rotation, 
							 GridAxis.AxisXY, gridOrientation.grid_ortho_z_alpha, GridMode.Ortho);
			} else
			{//*
				Vector3 forward			= gridOrientation.grid_rotation      * MathConstants.forwardVector3;
				Vector3 work_forward	= gridOrientation.grid_work_rotation * MathConstants.forwardVector3;
				if (ForceGrid &&
					!((forward - work_forward).sqrMagnitude < 0.001f ||
					  (forward + work_forward).sqrMagnitude < 0.001f
					  // && gridOrientation.grid_work_center   == gridOrientation.grid_center
						))
				{
					DrawGrid(gridOrientation.grid_camera, gridOrientation.grid_camera_position,
							 gridOrientation.grid_work_center, gridOrientation.grid_work_rotation,
							 GridAxis.AxisXZ, 0.75f, GridMode.WorkPlane);
					DrawGrid(gridOrientation.grid_camera, gridOrientation.grid_camera_position,
							 gridOrientation.grid_center, gridOrientation.grid_rotation,
							 GridAxis.AxisXZ, 0.125f, GridMode.Regular);
				} else//*/
					DrawGrid(gridOrientation.grid_camera, gridOrientation.grid_camera_position,
							 gridOrientation.grid_center, gridOrientation.grid_rotation,
							 GridAxis.AxisXZ, 1.0f, GridMode.Regular);
			}	
		}

		public static void RenderGrid()
		{
			if (Event.current.type != EventType.repaint)
				return;

			//var gridRenderer = GridRenderer;
			//if (gridRenderer == null)
			//{
			//	return;
			//}
			//	GameObject.DestroyImmediate(gridRenderer);
			
			//gridRenderer.onRender -= OnRender;
			//gridRenderer.onRender += OnRender;
			OnRender();
		}

		static void UpdateGridOrientation()
		{
			var sceneView		= SceneView.currentDrawingSceneView;
			var camera			= sceneView != null ? sceneView.camera : Camera.current;
			if (camera == null)
				return;

			var camera_position = camera.cameraToWorldMatrix.MultiplyPoint(MathConstants.zeroVector3);
			var camera_forward	= camera.cameraToWorldMatrix.MultiplyVector(MathConstants.forwardVector3);
			
			gridOrientation = new GridOrientation();
			gridOrientation.grid_camera				= camera;
			gridOrientation.grid_camera_position	= camera_position;

			if (Tools.pivotRotation == PivotRotation.Local)
			{
				var activeTransform = Selection.activeTransform;
				if (activeTransform != null)
				{
					var parentCenter	= MathConstants.zeroVector3;
					var parentRotation	= activeTransform.rotation;
					var parent			= activeTransform.parent;
					if (parent != null)
					{
						parentCenter	= parent.position;
					}

					gridOrientation.grid_rotation = parentRotation;// Tools.handleRotation;

					// project the local center to the grid
					//var plane = new Plane(transformation.grid_rotation, active_transform.position);

					gridOrientation.grid_center = parentCenter;// plane.Project(parentCenter);//   Tools.handlePosition;
				}
			}
			
			gridOrientation.grid_ortho_x_visible	= false;
			gridOrientation.grid_ortho_y_visible	= false;
			gridOrientation.grid_ortho_z_visible	= false;
			gridOrientation.grid_ortho				= false;
			gridOrientation.grid_ortho_x_alpha		= 0.0f;
			gridOrientation.grid_ortho_y_alpha		= 0.0f;
			gridOrientation.grid_ortho_z_alpha		= 0.0f;
			
			gridOrientation.grid_work_center	= gridOrientation.grid_center;
			gridOrientation.grid_work_rotation	= gridOrientation.grid_rotation;

			if (camera.orthographic)
			{
				gridOrientation.grid_ortho			= true;
				
				Vector3 dots = new Vector3(
						Mathf.Clamp01(Mathf.Abs(Vector3.Dot(camera_forward, gridOrientation.grid_rotation * MathConstants.rightVector3  )) - 0.6f),
						Mathf.Clamp01(Mathf.Abs(Vector3.Dot(camera_forward, gridOrientation.grid_rotation * MathConstants.upVector3     )) - 0.3f),
						Mathf.Clamp01(Mathf.Abs(Vector3.Dot(camera_forward, gridOrientation.grid_rotation * MathConstants.forwardVector3)) - 0.6f)
					).normalized;

				dots.x *= dots.x;
				dots.y *= dots.y;
				dots.z *= dots.z;
								
				if (dots.x > 0.5f)
				{
					Quaternion rotation = Quaternion.AngleAxis(90.0f, MathConstants.forwardVector3);
					gridOrientation.grid_ortho_x_rotation	= gridOrientation.grid_rotation * rotation;
					gridOrientation.grid_ortho_x_visible	= true;
					gridOrientation.grid_ortho_x_alpha		= dots.x;
				}
				
				if (dots.y > 0.5f)
				{
					gridOrientation.grid_ortho_y_rotation	= gridOrientation.grid_rotation;
					gridOrientation.grid_ortho_y_visible	= true;
					gridOrientation.grid_ortho_y_alpha		= dots.y;
				}

				if (dots.z > 0.5f)
				{
					Quaternion rotation = Quaternion.AngleAxis(90.0f, MathConstants.leftVector3);
					gridOrientation.grid_ortho_z_rotation	= gridOrientation.grid_rotation * rotation;
					gridOrientation.grid_ortho_z_visible	= true;
					gridOrientation.grid_ortho_z_alpha		= dots.z;
				}
				
				if (dots.y > dots.z)
				{
					if (dots.y > dots.x)	gridOrientation.grid_work_rotation = gridOrientation.grid_ortho_y_rotation;
					else					gridOrientation.grid_work_rotation = gridOrientation.grid_ortho_x_rotation;
				} else
				{
					if (dots.z > dots.x)	gridOrientation.grid_work_rotation = gridOrientation.grid_ortho_z_rotation;
					else					gridOrientation.grid_work_rotation = gridOrientation.grid_ortho_x_rotation;
				}
				gridOrientation.grid_plane = new CSGPlane(gridOrientation.grid_work_rotation, gridOrientation.grid_work_center);
			} else
			{
				gridOrientation.grid_plane = new CSGPlane(gridOrientation.grid_work_rotation, gridOrientation.grid_work_center); 
				if (ForceGrid)
				{
					gridOrientation.grid_work_center	= ForcedGridCenter;
					gridOrientation.grid_work_rotation	= ForcedGridRotation;
				}
			}

			gridOrientation.grid_work_inv_rotation = Quaternion.Inverse(gridOrientation.grid_work_rotation);
			
			// find point on the plane that is nearest to camera
			var	normal		= gridOrientation.grid_work_rotation * MathConstants.upVector3;
			var d			= Vector3.Dot(normal, gridOrientation.grid_center);
			var position	= (new CSGPlane(normal, d)).Project(gridOrientation.grid_camera_position);
			gridOrientation.grid_camera_snapped = position;

			gridOrientation.grid_work_plane = new CSGPlane(normal, position);



			var euler	= gridOrientation.grid_work_inv_rotation.eulerAngles;
			euler.x = Mathf.Round(euler.x / 90) * 90;
			euler.y = Mathf.Round(euler.y / 90) * 90;
			euler.z = Mathf.Round(euler.z / 90) * 90;
			
			gridOrientation.grid_snap_vector = Quaternion.Euler(euler) * RealtimeCSG.CSGSettings.SnapVector;
			var snap_scale  = Quaternion.Euler(euler) * 
				new Vector3(RealtimeCSG.CSGSettings.LockAxisX ? 0 : 1,
							RealtimeCSG.CSGSettings.LockAxisY ? 0 : 1,
							RealtimeCSG.CSGSettings.LockAxisZ ? 0 : 1);

			snap_scale.x = Mathf.Abs(snap_scale.x);
			snap_scale.y = Mathf.Abs(snap_scale.y);
			snap_scale.z = Mathf.Abs(snap_scale.z);

			gridOrientation.grid_snap_scale = snap_scale;
		}
		
		public static Matrix4x4	ToGridSpaceMatrix	()	{ return Matrix4x4.TRS(MathConstants.zeroVector3, gridOrientation.grid_work_inv_rotation, MathConstants.oneVector3); }
		public static Matrix4x4	FromGridSpaceMatrix	()	{ return Matrix4x4.TRS(MathConstants.zeroVector3, gridOrientation.grid_work_rotation,     MathConstants.oneVector3); }
		
		public static Quaternion ToGridSpaceQuaternion		()	{ return gridOrientation.grid_work_inv_rotation; }
		public static Quaternion FromGridSpaceQuaternion	()	{ return gridOrientation.grid_work_rotation; }
		
		public static Vector3	PointToGridSpace	(Vector3 pos)	{ return gridOrientation.grid_work_inv_rotation * (pos - gridOrientation.grid_work_center); }
		public static Vector3	VectorToGridSpace	(Vector3 pos)	{ return gridOrientation.grid_work_inv_rotation * pos; }
		public static CSGPlane	PlaneToGridSpace	(CSGPlane p)	{ return new CSGPlane(VectorToGridSpace(p.normal), PointToGridSpace(p.pointOnPlane)); }
		
		public static Vector3	PointFromGridSpace	(Vector3 pos)	{ return (gridOrientation.grid_work_rotation * pos) + gridOrientation.grid_work_center; }		
		public static Vector3	VectorFromGridSpace	(Vector3 pos)	{ return (gridOrientation.grid_work_rotation * pos); }
		public static CSGPlane	PlaneFromGridSpace	(CSGPlane p)	{ return new CSGPlane(VectorFromGridSpace(p.normal), PointFromGridSpace(p.pointOnPlane)); }

		static Vector3 SnapRoundPosition(Vector3 currentPosition, Vector3 snapVector)
		{
			currentPosition.x = Mathf.RoundToInt(currentPosition.x / snapVector.x) * snapVector.x;
			currentPosition.y = Mathf.RoundToInt(currentPosition.y / snapVector.y) * snapVector.y;
			currentPosition.z = Mathf.RoundToInt(currentPosition.z / snapVector.z) * snapVector.z;
			return currentPosition;
		}

		static Vector3 SnapFloorPosition(Vector3 currentPosition, Vector3 snapVector)
		{
			currentPosition.x = Mathf.FloorToInt(currentPosition.x / snapVector.x) * snapVector.x;
			currentPosition.y = Mathf.FloorToInt(currentPosition.y / snapVector.y) * snapVector.y;
			currentPosition.z = Mathf.FloorToInt(currentPosition.z / snapVector.z) * snapVector.z;
			return currentPosition;
		}

		static Vector3 SnapCeilPosition(Vector3 currentPosition, Vector3 snapVector)
		{
			currentPosition.x = Mathf.FloorToInt(currentPosition.x / snapVector.x + 1) * snapVector.x;
			currentPosition.y = Mathf.FloorToInt(currentPosition.y / snapVector.y + 1) * snapVector.y;
			currentPosition.z = Mathf.FloorToInt(currentPosition.z / snapVector.z + 1) * snapVector.z;
			return currentPosition;
		}

		static GridDescription? GetGridDescription(SceneView sceneView)
		{
			for (int i = GridMeshes.Count - 1; i >= 0; i--)
			{
				if (!GridMeshes[i].sceneView)
					continue;
				if (GridMeshes[i].sceneView == sceneView)
					return GridMeshes[i];
			}
			return null;
		}

		public static List<Vector3> FindAllGridEdgesThatTouchPoint(Vector3 point)
		{
			var lines = new List<Vector3>();
			
			UpdateGridOrientation();
			if (gridOrientation == null)
				return lines;

			var gridDescription		= GetGridDescription(SceneView.lastActiveSceneView);
			
			var snapVector			= gridOrientation.grid_snap_vector;

			var gridPoint			= PointToGridSpace(point);
			var snappedGridPoint	= SnapRoundPosition(gridPoint, snapVector);

			var gridPlane			= new CSGPlane(Grid.CurrentWorkGridPlane.normal, point);

			if (Math.Abs(gridPoint.x - snappedGridPoint.x) < MathConstants.EqualityEpsilon)
			{
				var lineSize = gridDescription.HasValue ? (maxLineCount * gridDescription.Value.snapSizeX) : 10000;
				var pointA = new Vector3(-lineSize, gridPoint.y, gridPoint.z);
				var pointB = new Vector3( lineSize, gridPoint.y, gridPoint.z);
				lines.Add(gridPlane.Project(PointFromGridSpace(pointA)));
				lines.Add(gridPlane.Project(PointFromGridSpace(pointB)));
			}

			if (Math.Abs(gridPoint.y - snappedGridPoint.y) < MathConstants.EqualityEpsilon)
			{
				var lineSize = gridDescription.HasValue ? (maxLineCount * gridDescription.Value.snapSizeY) : 10000;
				var pointA = new Vector3(gridPoint.x, -lineSize, gridPoint.z);
				var pointB = new Vector3(gridPoint.x,  lineSize, gridPoint.z);
				lines.Add(gridPlane.Project(PointFromGridSpace(pointA)));
				lines.Add(gridPlane.Project(PointFromGridSpace(pointB)));
			}

			if (Math.Abs(gridPoint.z - snappedGridPoint.z) < MathConstants.EqualityEpsilon)
			{
				var lineSize = gridDescription.HasValue ? (maxLineCount * gridDescription.Value.snapSizeZ) : 10000;
				var pointA = new Vector3(gridPoint.x, gridPoint.y, -lineSize);
				var pointB = new Vector3(gridPoint.x, gridPoint.y,  lineSize);
				lines.Add(gridPlane.Project(PointFromGridSpace(pointA)));
				lines.Add(gridPlane.Project(PointFromGridSpace(pointB)));
			}
			return lines;
		}
		
		public static Vector3 ForceSnapToGrid(Vector3 worldPoint)
		{
			return worldPoint + SnapDeltaToGrid(MathConstants.zeroVector3, new Vector3[] { worldPoint });
		}

		public static Vector3 ForceSnapDeltaToGrid(Vector3 worldDeltaMovement, Vector3 worldPoint)
		{
			return SnapDeltaToGrid(worldDeltaMovement, new Vector3[] { worldPoint });
		}

		public static Vector3 HandleLockedAxi(Vector3 worldDeltaMovement)
		{
			var snapScale				= gridOrientation.grid_snap_scale;
			var gridLocalDeltaMovement	= VectorToGridSpace(worldDeltaMovement);
			gridLocalDeltaMovement.x *= snapScale.x;
			gridLocalDeltaMovement.y *= snapScale.y;
			gridLocalDeltaMovement.z *= snapScale.z;
			return VectorFromGridSpace(gridLocalDeltaMovement);
		}

		public static Vector3 SnapDeltaToGrid(Vector3 worldDeltaMovement, Vector3[] worldPoints, bool snapToGridPlane = true, bool snapToSelf = false)
		{
			UpdateGridOrientation();
			if (gridOrientation == null || worldPoints == null || worldPoints.Length == 0)
				return worldDeltaMovement;
			
			var worldPlane	= gridOrientation.grid_work_plane;
			var scaleVector = gridOrientation.grid_snap_scale;
			var snapVector	= gridOrientation.grid_snap_vector;
			
			var gridLocalDeltaMovement	= VectorToGridSpace(worldDeltaMovement);
			var gridLocalPlane			= PlaneToGridSpace(worldPlane);

			if (snapToGridPlane)
			{
				scaleVector.x *= (Mathf.Abs(gridLocalPlane.a) >= 1 - MathConstants.EqualityEpsilon) ? 0 : 1;
				scaleVector.y *= (Mathf.Abs(gridLocalPlane.b) >= 1 - MathConstants.EqualityEpsilon) ? 0 : 1;
				scaleVector.z *= (Mathf.Abs(gridLocalPlane.c) >= 1 - MathConstants.EqualityEpsilon) ? 0 : 1;
			}
			var snappedDeltaMovement	= gridLocalDeltaMovement;

			if (Mathf.Abs(scaleVector.x) < MathConstants.EqualityEpsilon || Mathf.Abs(worldDeltaMovement.x) < MathConstants.EqualityEpsilon) snappedDeltaMovement.x = 0;
			if (Mathf.Abs(scaleVector.y) < MathConstants.EqualityEpsilon || Mathf.Abs(worldDeltaMovement.y) < MathConstants.EqualityEpsilon) snappedDeltaMovement.y = 0;
			if (Mathf.Abs(scaleVector.z) < MathConstants.EqualityEpsilon || Mathf.Abs(worldDeltaMovement.z) < MathConstants.EqualityEpsilon) snappedDeltaMovement.z = 0;
			
			Vector3[] gridLocalPoints;
			if (worldPoints.Length > 1)
			{ 
				var bounds = new AABB();
				bounds.Reset();
				for (int i = 0; i < worldPoints.Length; i++)
				{
					Vector3 localPoint = PointToGridSpace(worldPoints[i]);
					if (snapToGridPlane)
						localPoint = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, localPoint);
					if (float.IsNaN(localPoint.x) || float.IsNaN(localPoint.y) || float.IsNaN(localPoint.z) ||
						float.IsInfinity(localPoint.x) || float.IsInfinity(localPoint.y) || float.IsInfinity(localPoint.z))
						continue;
					bounds.Add(localPoint);
				}
				gridLocalPoints = bounds.GetCorners();
			} else
			{
				var localGridSpacePoint = PointToGridSpace(worldPoints[0]);
				Vector3 projectedPoint = localGridSpacePoint;
				if (snapToGridPlane)
					projectedPoint		= GeometryUtility.ProjectPointOnPlane(gridLocalPlane, localGridSpacePoint);

				if (float.IsNaN(projectedPoint.x) || float.IsNaN(projectedPoint.y) || float.IsNaN(projectedPoint.z) ||
					float.IsInfinity(projectedPoint.x) || float.IsInfinity(projectedPoint.y) || float.IsInfinity(projectedPoint.z))
					gridLocalPoints = new Vector3[0] {  };
				else
					gridLocalPoints = new Vector3[] { projectedPoint };
			}
			
			for (int i = 0; i < gridLocalPoints.Length; i++)
			{
				var oldPoint = gridLocalPoints[i];
				var newPoint = gridLocalPoints[i] + gridLocalDeltaMovement;
				if (snapToGridPlane)
					newPoint = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, newPoint);				
				newPoint = GridUtility.CleanPosition(newPoint);
				
				var snappedNewPoint = SnapRoundPosition(newPoint, snapVector);
				
				if (snapToGridPlane)
					snappedNewPoint = GeometryUtility.ProjectPointOnPlane(gridLocalPlane, snappedNewPoint);
				snappedNewPoint = GridUtility.CleanPosition(snappedNewPoint);
						
				var foundDeltaMovement = (snappedNewPoint - oldPoint);
				
				foundDeltaMovement.x *= scaleVector.x;
				foundDeltaMovement.y *= scaleVector.y;
				foundDeltaMovement.z *= scaleVector.z;

				if (i == 0 || Math.Abs(foundDeltaMovement.x) < Mathf.Abs(snappedDeltaMovement.x)) snappedDeltaMovement.x = foundDeltaMovement.x;
				if (i == 0 || Math.Abs(foundDeltaMovement.y) < Mathf.Abs(snappedDeltaMovement.y)) snappedDeltaMovement.y = foundDeltaMovement.y;
				if (i == 0 || Math.Abs(foundDeltaMovement.z) < Mathf.Abs(snappedDeltaMovement.z)) snappedDeltaMovement.z = foundDeltaMovement.z;
			}

			if (snapToSelf)
			{ 
				var snapDelta = (snappedDeltaMovement - gridLocalDeltaMovement);
				if (Mathf.Abs(snapDelta.x) > Mathf.Abs(gridLocalDeltaMovement.x)) snappedDeltaMovement.x = 0;
				if (Mathf.Abs(snapDelta.y) > Mathf.Abs(gridLocalDeltaMovement.y)) snappedDeltaMovement.y = 0;
				if (Mathf.Abs(snapDelta.z) > Mathf.Abs(gridLocalDeltaMovement.z)) snappedDeltaMovement.z = 0;
			}
			
			worldDeltaMovement = VectorFromGridSpace(snappedDeltaMovement);
			//Debug.Log(worldDeltaMovement + " " + snappedDeltaMovement);
			return worldDeltaMovement;
		}

		public static Vector3 ForceSnapToRay(Ray worldRay, Vector3 worldPoint)
		{
			return worldPoint + SnapDeltaToRay(worldRay, MathConstants.zeroVector3, new Vector3[] { worldPoint });
		}
		
		public static Vector3 ForceSnapDeltaToRay(Ray worldRay, Vector3 worldDeltaMovement, Vector3 worldPoint)
		{
			return SnapDeltaToRay(worldRay, worldDeltaMovement, new Vector3[] { worldPoint });
		}
		
		public static Vector3 SnapDeltaToRay(Ray worldRay, Vector3 worldDeltaMovement, Vector3[] worldPoints, bool snapToSelf = false)
		{
			UpdateGridOrientation();
			if (gridOrientation == null || worldPoints == null || worldPoints.Length == 0)
				return worldDeltaMovement;
			
			var snapVector	= gridOrientation.grid_snap_vector;
			var scaleVector = gridOrientation.grid_snap_scale;
			
			var localDeltaMovement		= VectorToGridSpace(worldDeltaMovement);
			var localLineDir			= VectorToGridSpace(worldRay.direction);
			var localLineOrg			= PointToGridSpace(worldRay.origin);
			
			scaleVector.x *= ((Mathf.Abs(localLineDir.y) >= 1 - MathConstants.EqualityEpsilon) || (Mathf.Abs(localLineDir.z) >= 1 - MathConstants.EqualityEpsilon)) ? 0 : 1;
			scaleVector.y *= ((Mathf.Abs(localLineDir.x) >= 1 - MathConstants.EqualityEpsilon) || (Mathf.Abs(localLineDir.z) >= 1 - MathConstants.EqualityEpsilon)) ? 0 : 1;
			scaleVector.z *= ((Mathf.Abs(localLineDir.x) >= 1 - MathConstants.EqualityEpsilon) || (Mathf.Abs(localLineDir.y) >= 1 - MathConstants.EqualityEpsilon)) ? 0 : 1;

			var snappedDeltaMovement	= localDeltaMovement;
			if (Mathf.Abs(scaleVector.x) < MathConstants.EqualityEpsilon) snappedDeltaMovement.x = 0;
			if (Mathf.Abs(scaleVector.y) < MathConstants.EqualityEpsilon) snappedDeltaMovement.y = 0;
			if (Mathf.Abs(scaleVector.z) < MathConstants.EqualityEpsilon) snappedDeltaMovement.z = 0;

			Vector3[] localPoints;
			if (worldPoints.Length > 1)
			{ 
				var bounds = new AABB();
				bounds.Reset();
				for (int i = 0; i < worldPoints.Length; i++)
				{
					var localPoint = GeometryUtility.ProjectPointOnInfiniteLine(PointToGridSpace(worldPoints[i]), localLineOrg, localLineDir);
					bounds.Add(localPoint);
				}
				localPoints = bounds.GetCorners();
			} else
			{
				localPoints = new Vector3[] { GeometryUtility.ProjectPointOnInfiniteLine(PointToGridSpace(worldPoints[0]), localLineOrg, localLineDir) };
			}
			
			for (int i = 0; i < localPoints.Length; i++)
			{
				var oldPoint = localPoints[i];
				var newPoint = GeometryUtility.ProjectPointOnInfiniteLine(oldPoint + localDeltaMovement, localLineOrg, localLineDir);

				var snappedNewPoint = SnapRoundPosition(newPoint, snapVector);

				snappedNewPoint = GridUtility.CleanPosition(GeometryUtility.ProjectPointOnInfiniteLine(snappedNewPoint, localLineOrg, localLineDir));
						
				var foundDeltaMovement = (snappedNewPoint - oldPoint);
				
				foundDeltaMovement.x *= scaleVector.x;
				foundDeltaMovement.y *= scaleVector.y;
				foundDeltaMovement.z *= scaleVector.z;

				if (i == 0 || Math.Abs(foundDeltaMovement.x) < Mathf.Abs(snappedDeltaMovement.x)) snappedDeltaMovement.x = foundDeltaMovement.x; 
				if (i == 0 || Math.Abs(foundDeltaMovement.y) < Mathf.Abs(snappedDeltaMovement.y)) snappedDeltaMovement.y = foundDeltaMovement.y; 
				if (i == 0 || Math.Abs(foundDeltaMovement.z) < Mathf.Abs(snappedDeltaMovement.z)) snappedDeltaMovement.z = foundDeltaMovement.z; 
			}

			if (snapToSelf)
			{ 
				var snapDelta = (snappedDeltaMovement - localDeltaMovement);
				if (Mathf.Abs(snapDelta.x) > Mathf.Abs(localDeltaMovement.x)) snappedDeltaMovement.x = 0;
				if (Mathf.Abs(snapDelta.y) > Mathf.Abs(localDeltaMovement.y)) snappedDeltaMovement.y = 0;
				if (Mathf.Abs(snapDelta.z) > Mathf.Abs(localDeltaMovement.z)) snappedDeltaMovement.z = 0;
			}
			
			worldDeltaMovement = VectorFromGridSpace(snappedDeltaMovement);
			return worldDeltaMovement;
		}

		public static bool SnapToLine(Vector3 worldPoint, Vector3 worldVertex1, Vector3 worldVertex2, CSGPlane? snapPlane, ref Vector3 worldSnappedPoint)
		{
			var localGridPoint	= PointToGridSpace(worldPoint);
			var localVertex1	= PointToGridSpace(worldVertex1);
			var localVertex2	= PointToGridSpace(worldVertex2);
			
			var snapVector = gridOrientation.grid_snap_vector;
			
			float minx = Mathf.Min(localVertex1.x, localVertex2.x);
			float maxx = Mathf.Max(localVertex1.x, localVertex2.x);
			
			float miny = Mathf.Min(localVertex1.y, localVertex2.y);
			float maxy = Mathf.Max(localVertex1.y, localVertex2.y);
			
			float minz = Mathf.Min(localVertex1.z, localVertex2.z);
			float maxz = Mathf.Max(localVertex1.z, localVertex2.z);

			var localLengthX = (maxx - minx);
			var localLengthY = (maxy - miny);
			var localLengthZ = (maxz - minz);
			if (localLengthX < MathConstants.AlignmentTestEpsilon &&
				localLengthY < MathConstants.AlignmentTestEpsilon &&
				localLengthZ < MathConstants.AlignmentTestEpsilon)
			{
				worldSnappedPoint = worldPoint;
				return false;
			}
			
			var found_points = new Vector3[6];
			var point_count = 0;

			if (localLengthX > MathConstants.AlignmentTestEpsilon)
			{
				float x1 = Mathf.FloorToInt(localGridPoint.x / snapVector.x) * snapVector.x;
				if (x1 > minx && x1 < maxx)
				{
					var xpos = x1;
					var t = (xpos - minx) / localLengthX;
					if (t > 0 && t < 1.0f)
					{ 
						var ypos = localVertex1.y + (t * (localVertex2.y - localVertex1.y));
						var zpos = localVertex1.z + (t * (localVertex2.z - localVertex1.z));
						var localIntersection = new Vector3(xpos, ypos, zpos);
						var worldIntersection = PointFromGridSpace(localIntersection);
						if (snapPlane.HasValue)
							worldIntersection = snapPlane.Value.Project(worldIntersection);
						float dist = HandleUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);
						if (dist < MathConstants.DistanceEpsilon)
						{ 
							found_points[point_count] = worldIntersection; point_count++;
						}
					}
				}
				
				float x2 = Mathf.CeilToInt (localGridPoint.x / snapVector.x) * snapVector.x;
				if (x2 > minx && x2 < maxx)
				{
					var xpos = x2;
					var t = (xpos - minx) / localLengthX;
					if (t > 0 && t < 1.0f)
					{ 
						var ypos = localVertex1.y + (t * (localVertex2.y - localVertex1.y));
						var zpos = localVertex1.z + (t * (localVertex2.z - localVertex1.z));
						var localIntersection = new Vector3(xpos, ypos, zpos);
						var worldIntersection = PointFromGridSpace(localIntersection);
						if (snapPlane.HasValue)
							worldIntersection = snapPlane.Value.Project(worldIntersection);
						float dist = HandleUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);
						if (dist < MathConstants.DistanceEpsilon)
						{ 
							found_points[point_count] = worldIntersection; point_count++;
						}
					}
				}
			}

			if (localLengthY > MathConstants.AlignmentTestEpsilon)
			{
				float y1 = Mathf.FloorToInt(localGridPoint.y / snapVector.y) * snapVector.y;
				if (y1 > miny && y1 < maxy)
				{
					var ypos = y1;
					var t = (ypos - miny) / localLengthY;
					if (t > 0 && t < 1.0f)
					{ 
						var zpos = localVertex1.z + (t * localLengthZ);
						var xpos = localVertex1.x + (t * localLengthX);
						var localIntersection = new Vector3(xpos, ypos, zpos);
						var worldIntersection = PointFromGridSpace(localIntersection);
						if (snapPlane.HasValue)
							worldIntersection = snapPlane.Value.Project(worldIntersection);
						float dist = HandleUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);
						if (dist < MathConstants.DistanceEpsilon)
						{ 
							found_points[point_count] = worldIntersection; point_count++;
						}
					}
				}
				
				float y2 = Mathf.CeilToInt (localGridPoint.y / snapVector.y) * snapVector.y;
				if (y2 > miny && y2 < maxy)
				{
					var ypos = y2;
					var t = (ypos - miny) / localLengthY;
					if (t > 0 && t < 1.0f)
					{ 
						var zpos = localVertex1.z + (t * localLengthZ);
						var xpos = localVertex1.x + (t * localLengthX);
						var localIntersection = new Vector3(xpos, ypos, zpos);
						var worldIntersection = PointFromGridSpace(localIntersection);
						if (snapPlane.HasValue)
							worldIntersection = snapPlane.Value.Project(worldIntersection);
						float dist = HandleUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);
						if (dist < MathConstants.DistanceEpsilon)
						{ 
							found_points[point_count] = worldIntersection; point_count++;
						}
					}
				}
			}

			if (localLengthZ > MathConstants.AlignmentTestEpsilon)
			{
				float z1 = Mathf.FloorToInt(localGridPoint.z / snapVector.z) * snapVector.z;
				if (z1 > minz && z1 < maxz)
				{
					var zpos = z1;
					var t = (zpos - minz) / localLengthZ;
					if (t > 0 && t < 1.0f)
					{ 
						var xpos = localVertex1.x + (t * (localVertex2.x - localVertex1.x));
						var ypos = localVertex1.y + (t * (localVertex2.y - localVertex1.y));
						var localIntersection = new Vector3(xpos, ypos, zpos);
						var worldIntersection = PointFromGridSpace(localIntersection);
						if (snapPlane.HasValue)
							worldIntersection = snapPlane.Value.Project(worldIntersection);
						float dist = HandleUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);
						if (dist < MathConstants.DistanceEpsilon)
						{ 
							found_points[point_count] = worldIntersection; point_count++;
						}
					}
				}
						
				float z2 = Mathf.CeilToInt (localGridPoint.z / snapVector.z) * snapVector.z;
				if (z2 > minz && z2 < maxz)
				{
					var zpos = z2;
					var t = (zpos - minz) / localLengthZ;
					if (t > 0 && t < 1.0f)
					{ 
						var xpos = localVertex1.x + (t * (localVertex2.x - localVertex1.x));
						var ypos = localVertex1.y + (t * (localVertex2.y - localVertex1.y));
						var localIntersection = new Vector3(xpos, ypos, zpos);
						var worldIntersection = PointFromGridSpace(localIntersection);
						if (snapPlane.HasValue)
							worldIntersection = snapPlane.Value.Project(worldIntersection);
						float dist = HandleUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);
						if (dist < MathConstants.DistanceEpsilon)
						{ 
							found_points[point_count] = worldIntersection; point_count++;
						}
					}
				}
			}

			if (point_count == 0)
				return false;

			if (point_count == 1)
			{
				worldSnappedPoint = found_points[0];
				return true;
			}

			float	found_dist	= (found_points[0] - worldPoint).sqrMagnitude;
			int		found_index = 0;
			for (int i = 1; i < point_count; i++)
			{
				float dist = (found_points[i] - worldPoint).sqrMagnitude;
				if (found_dist > dist)
				{
					found_dist = dist;
					found_index = i;
				}
			}
			
			worldSnappedPoint = found_points[found_index];
			//Debug.Log(worldSnappedPoint + " " + worldVertex1 + " " + worldVertex2);
			return true;
		}
		

		//public static bool YMoveModeActive { get; set; }


		public static bool SetupWorkPlane(Vector3 worldCenterPoint, ref CSGPlane workPlane)
		{
			var camera = Camera.current; 
			if (camera == null || !camera)
				return false;

			if (camera.orthographic)
			{				
				Grid.ForceGrid = false;
				workPlane = Grid.CurrentWorkGridPlane;
				return true;
			}
			
			var normal = Grid.CurrentGridPlane.normal;
			/*
			if (YMoveModeActive)
			{
				var forward = camera.transform.forward;
				Vector3 tangent, binormal;
				GeometryUtility.CalculateTangents(normal, out tangent, out binormal);
				if (Mathf.Abs(Vector3.Dot(forward, tangent)) > Mathf.Abs(Vector3.Dot(forward, binormal)))
					normal = tangent;
				else
					normal = binormal;
			}*/

			workPlane = new CSGPlane(GridUtility.CleanNormal(normal), worldCenterPoint);
			return Grid.SetForcedGrid(workPlane);
		}

		public static bool SetupWorkPlane(Vector3 worldCenterPoint, Vector3 worldDirection, ref CSGPlane workPlane)
		{
			var camera = Camera.current; 
			if (camera == null || !camera)
				return false;

			if (camera.orthographic)
			{				
				Grid.ForceGrid = false;
				workPlane = Grid.CurrentWorkGridPlane;
				return true;
			}
			
			var normal = worldDirection;
			/*
			if (YMoveModeActive)
			{
				var forward = camera.transform.forward;
				Vector3 tangent, binormal;
				GeometryUtility.CalculateTangents(normal, out tangent, out binormal);
				if (Mathf.Abs(Vector3.Dot(forward, tangent)) > Mathf.Abs(Vector3.Dot(forward, binormal)))
					normal = tangent;
				else
					normal = binormal;
			}*/

			workPlane = new CSGPlane(GridUtility.CleanNormal(normal), worldCenterPoint);
			return Grid.SetForcedGrid(workPlane);
		}

		public static bool SetupRayWorkPlane(Vector3 worldOrigin, Vector3 worldDirection, ref CSGPlane outWorkPlane)
		{
			var camera = Camera.current; 
			if (camera == null || !camera)
				return false;			
							
			Vector3 tangent, normal;
			var cameraBackwards			= -camera.transform.forward;
			var closestAxisForward		= GeometryUtility.SnapToClosestAxis(cameraBackwards);
			var closestAxisDirection	= GeometryUtility.SnapToClosestAxis(worldDirection);
			if (Vector3.Dot(closestAxisForward, closestAxisDirection) != 0)
			{
				float dot1 = Mathf.Abs(Vector3.Dot(cameraBackwards, MathConstants.rightVector3));
				float dot2 = Mathf.Abs(Vector3.Dot(cameraBackwards, MathConstants.upVector3));
				float dot3 = Mathf.Abs(Vector3.Dot(cameraBackwards, MathConstants.forwardVector3));
				if (dot1 < dot2)
				{
					if (dot1 < dot3)	tangent = MathConstants.rightVector3;
					else				tangent = MathConstants.forwardVector3;
				} else
				{
					if (dot2 < dot3)	tangent = MathConstants.upVector3;
					else				tangent = MathConstants.forwardVector3;
				}
			} else
				tangent = Vector3.Cross(worldDirection, closestAxisForward);
			
			if (!camera.orthographic)
			{ 
				normal = Vector3.Cross(worldDirection, tangent);
			} else
				normal = cameraBackwards;

			outWorkPlane = new CSGPlane(GridUtility.CleanNormal(normal), worldOrigin);
			
			return Grid.SetForcedGrid(outWorkPlane);
		}

		public static Vector3 CubeProject(CSGPlane plane, Vector3 pos)
		{
			UpdateGridOrientation();
			var closest_axis	= GeometryUtility.SnapToClosestAxis(plane.normal);
			var intersection	= plane.Intersection(pos, pos + closest_axis);

			if (float.IsNaN(intersection.x) || float.IsInfinity(intersection.x) ||
				float.IsNaN(intersection.y) || float.IsInfinity(intersection.y) ||
				float.IsNaN(intersection.z) || float.IsInfinity(intersection.z))
			{
				// should never happen, but if all else fails just do a projection ..
				intersection = plane.Project(pos);
			}
			return intersection;
		}
		
		public static bool SetForcedGrid(CSGPlane plane)
		{			
			if (float.IsNaN(plane.a) || float.IsInfinity(plane.a) ||
				float.IsNaN(plane.b) || float.IsInfinity(plane.b) ||
				float.IsNaN(plane.c) || float.IsInfinity(plane.c) ||
				float.IsNaN(plane.d) || float.IsInfinity(plane.d))
			{
				Debug.Log("Invalid plane passed to SetForcedGrid");
				return false;
			}

			// cube-project the center of the grid so that it lies on the plane
			var center		= CubeProject(plane, Grid.CurrentGridCenter);

			var normal		= Quaternion.Inverse(gridOrientation.grid_rotation) * plane.normal;
			var tangent		= Vector3.Cross(normal, Vector3.Cross(normal, GeometryUtility.CalculateTangent(normal)).normalized).normalized;
			Quaternion q	= gridOrientation.grid_rotation * Quaternion.LookRotation(tangent, normal);
			
			Grid.ForceGrid			= true;
			Grid.ForcedGridCenter	= center;
			Grid.ForcedGridRotation = q;
			return true;
		}		
	}
}
 