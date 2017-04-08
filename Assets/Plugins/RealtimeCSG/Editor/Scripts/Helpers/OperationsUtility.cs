using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[Serializable]
	internal sealed class ShapePolygon
	{
		public ShapePolygon() { }
		public ShapePolygon(Vector3[] vertices) { this.Vertices = vertices; }

		// NOTE: an edge is defined as [vertex n, vertex (n+1)%vertices.length]
		public Vector3[]		Vertices;
		public Material[]		EdgeMaterials;
		public TexGen[]			EdgeTexgens;
//		public TexGenFlags[]	EdgeTexgenFlags;
	}

	[Serializable]
	internal struct ShapeEdge
	{
		public int PolygonIndex;
		public int EdgeIndex;
	}

	internal sealed class OperationsUtility
	{
		public static bool CanModifyOperationsOnSelected()
		{
			foreach (var gameObject in Selection.gameObjects)
			{
				var brush	= gameObject.GetComponentInChildren<CSGBrush>();
				if (brush != null)
					return true;

				var operation	= gameObject.GetComponentInChildren<CSGOperation>();
				if (operation != null)
					return true;
			}
			return false;
		}
		

		public static void SetPassThroughOnSelected()
		{
			var modified = false;
			foreach(var gameObject in Selection.gameObjects)
			{
				var operation	= gameObject.GetComponent<CSGOperation>();
				if (operation == null || operation.PassThrough)
					continue;

				modified = true;
				Undo.RecordObject(operation, "Modifying csg operation of operation component");
				operation.PassThrough = true;
			}

			if (!modified)
				return;

			InternalCSGModelManager.Refresh();
			EditorApplication.RepaintHierarchyWindow();
		}

		public static void ModifyOperationsOnSelected(CSGOperationType operationType)
		{
			var modified = false;
			foreach(var gameObject in Selection.gameObjects)
			{
				var brush		= gameObject.GetComponent<CSGBrush>();
				if (brush != null &&
					brush.OperationType != operationType)
				{
					modified = true;
					Undo.RecordObject(brush, "Modifying csg operation of brush component");
					brush.OperationType = operationType;
				}

				var operation	= gameObject.GetComponent<CSGOperation>();
				if (operation == null || operation.OperationType == operationType)
					continue;

				modified = true;
				Undo.RecordObject(operation, "Modifying csg operation of operation component");
				operation.PassThrough = false;
				operation.OperationType = operationType;
			}
			if (modified)
			{
				InternalCSGModelManager.Refresh();
				EditorApplication.RepaintHierarchyWindow();
			}
		}
		
		
		// FIXME: move this to a more logical class
		public static bool GenerateControlMeshFromVertices(ShapePolygon		shape2DPolygon,
														   Vector3			direction,
														   float			height,
														   Vector3			scale,
														   Material			capMaterial,
														   TexGen			capTexgen, 
														   bool?			smooth, 
														   bool				singleSurfaceEnds, //Plane buildPlane, 
														   out ControlMesh	controlMesh, 
														   out Shape		shape)
		{
			if (shape2DPolygon == null)
			{
				controlMesh = null; 
				shape = null;
				return false;
			}

			var vertices = shape2DPolygon.Vertices;
			if (vertices.Length < 3)
			{
				controlMesh = null; 
				shape = null;
				return false;
			}
			if (height == 0.0f)
			{
				controlMesh = null; 
				shape = null;
				return false;
			}
						
			Vector3 from;
			Vector3 to;
			
			if (height > 0)
			{
				from = direction * height;// buildPlane.normal * height;
				to   = MathConstants.zeroVector3;
			} else
			{ 
				from = MathConstants.zeroVector3;
				to   = direction * height;//buildPlane.normal * height;
			}
			
			var count			= vertices.Length;
			var doubleCount		= (count * 2);
			var extraPoints		= 0;
			var extraEdges		= 0;
			var endsPolygons	= 2;
			var startEdgeOffset	= doubleCount;
			
			if (!singleSurfaceEnds)
			{
				extraPoints		= 2;
				extraEdges		= (4 * count);
				endsPolygons	= doubleCount;
				startEdgeOffset += extraEdges;
			}

			
			var dstPoints	= new Vector3 [doubleCount + extraPoints];
			var dstEdges	= new HalfEdge[(count * 6) + extraEdges];
			var dstPolygons	= new Polygon [count + endsPolygons];

			var center1 = MathConstants.zeroVector3;
			var center2 = MathConstants.zeroVector3;


			for (int i = 0; i < count; i++)
			{
				var point1 = vertices[i];
				var point2 = vertices[(count + i-1) % count];
				
				point1 += from;
				point2 += to;

				// swap y/z to solve texgen issues
				dstPoints[i].x				= point1.x;
				dstPoints[i].y				= point1.y;
				dstPoints[i].z				= point1.z;
				
				center1 += dstPoints[i];

				dstEdges [i].VertexIndex	= (short)i;
				dstEdges [i].HardEdge		= true;
				
				// swap y/z to solve texgen issues
				dstPoints[i + count].x				= point2.x;
				dstPoints[i + count].y				= point2.y;
				dstPoints[i + count].z				= point2.z;
				center2 += dstPoints[i + count];

				dstEdges [i + count].VertexIndex	= (short)(i + count);
				dstEdges [i + count].HardEdge		= true;
			}

			if (!singleSurfaceEnds)
			{
				dstPoints[doubleCount    ] = center1 / count;
				dstPoints[doubleCount + 1] = center2 / count;

				int edge_offset		= doubleCount;
				short polygon_index	= (short)count;

				// 'top' 
				for (int i = 0, j = count-1; i < count; j=i, i++)
				{
					var jm = (j) % count;
					var im = (i) % count;

					var edgeOut0	= edge_offset + (jm * 2) + 1;
					var edgeIn0		= edge_offset + (im * 2) + 0;
					var edgeOut1	= edge_offset + (im * 2) + 1;

					dstEdges[edgeIn0 ].VertexIndex		= (short)(doubleCount);
					dstEdges[edgeIn0 ].HardEdge		= true;
					dstEdges[edgeIn0 ].TwinIndex		= edgeOut1;

					dstEdges[edgeOut1].VertexIndex		= (short)im;
					dstEdges[edgeOut1].HardEdge		= true;
					dstEdges[edgeOut1].TwinIndex		= edgeIn0;
					
					dstEdges[im       ].PolygonIndex	= polygon_index;
					dstEdges[edgeIn0 ].PolygonIndex	= polygon_index;
					dstEdges[edgeOut0].PolygonIndex	= polygon_index;
					
					dstPolygons[polygon_index] = new Polygon(new int[] { im, edgeIn0, edgeOut0 }, polygon_index);
					polygon_index++;
				}

				edge_offset = doubleCount * 2;
				// 'bottom'
				for (int i = 0, j = count-1; j >= 0; i=j, j--)
				{
					var jm = (count + count - j) % count;
					var im = (count + count - i) % count;

					var edgeOut0	= edge_offset + (jm * 2) + 1;
					var edgeIn0		= edge_offset + (im * 2) + 0;
					var edgeOut1	= edge_offset + (im * 2) + 1;
					
					dstEdges[edgeIn0 ].VertexIndex		= (short)(doubleCount + 1);
					dstEdges[edgeIn0 ].HardEdge		= true;
					dstEdges[edgeIn0 ].TwinIndex		= edgeOut1;

					dstEdges[edgeOut1].VertexIndex		= (short)(im + count);
					dstEdges[edgeOut1].HardEdge		= true;
					dstEdges[edgeOut1].TwinIndex		= edgeIn0;
					
					dstEdges[im+count ].PolygonIndex	= polygon_index;
					dstEdges[edgeIn0 ].PolygonIndex	= polygon_index;
					dstEdges[edgeOut0].PolygonIndex	= polygon_index;
					
					dstPolygons[polygon_index] = new Polygon(new int[] { im+count, edgeIn0, edgeOut0 }, polygon_index);
					polygon_index++;
				}
			} else
			{			
				var polygon0Edges	= new int[count];
				var polygon1Edges	= new int[count];
				for (var i = 0; i < count; i++)
				{
					dstEdges [i        ].PolygonIndex	= (short)(count + 0);
					dstEdges [i + count].PolygonIndex	= (short)(count + 1);
					polygon0Edges[i]			= i;
					polygon1Edges[count - (i+1)] = i + count;
				}
				dstPolygons[count + 0] = new Polygon(polygon0Edges, count + 0);
				dstPolygons[count + 1] = new Polygon(polygon1Edges, count + 1);
			}


			for (int v0 = count - 1, v1 = 0; v1 < count; v0 = v1, v1++)
			{
				var polygonIndex = (short)(v1);
				
				var nextOffset = startEdgeOffset + (((v1         + 1) % count) * 4);
				var currOffset = startEdgeOffset + (((v1            )        ) * 4);
				var prevOffset = startEdgeOffset + (((v1 + count - 1) % count) * 4);

				var nextTwin = nextOffset + 1;
				var prevTwin = prevOffset + 3;

				dstEdges[v1        ].TwinIndex = currOffset + 0;
				dstEdges[v1 + count].TwinIndex = currOffset + 2;

				dstEdges[currOffset + 0].PolygonIndex = polygonIndex;
				dstEdges[currOffset + 1].PolygonIndex = polygonIndex;
				dstEdges[currOffset + 2].PolygonIndex = polygonIndex;
				dstEdges[currOffset + 3].PolygonIndex = polygonIndex;

				dstEdges[currOffset + 0].TwinIndex = (v1        );
				dstEdges[currOffset + 1].TwinIndex = prevTwin   ;
				dstEdges[currOffset + 2].TwinIndex = (v1 + count);
				dstEdges[currOffset + 3].TwinIndex = nextTwin   ;

				dstEdges[currOffset + 0].VertexIndex = (short)(v0        );
				dstEdges[currOffset + 1].VertexIndex = (short)(v1 + count);
				dstEdges[currOffset + 2].VertexIndex = (short)(((v1   + 1) % count) + count);
				dstEdges[currOffset + 3].VertexIndex = (short)(v1        );

				dstEdges[currOffset + 0].HardEdge = true;
				dstEdges[currOffset + 1].HardEdge = true;
				dstEdges[currOffset + 2].HardEdge = true;
				dstEdges[currOffset + 3].HardEdge = true;

				dstPolygons[polygonIndex] = new Polygon(new [] { currOffset + 0,
																 currOffset + 1,
																 currOffset + 2,
																 currOffset + 3 }, polygonIndex);
			}

			for (int i = 0; i < dstPoints.Length; i++)
			{
				dstPoints[i].x /= scale.x;
				dstPoints[i].y /= scale.y;
				dstPoints[i].z /= scale.z;
			}

			controlMesh = new ControlMesh
			{
				Vertices	= dstPoints,
				Edges		= dstEdges,
				Polygons	= dstPolygons
			};
			controlMesh.SetDirty();

			shape = new Shape
			{
				Materials	= new Material[dstPolygons.Length],
				Surfaces	= new Surface[dstPolygons.Length],
				TexGenFlags = new TexGenFlags[dstPolygons.Length],
				TexGens		= new TexGen[dstPolygons.Length]
			};


			var smoothinggroup = (smooth.HasValue && smooth.Value) ? SurfaceUtility.FindUnusedSmoothingGroupIndex() : 0;


			var containedMaterialCount = 0;
			if (shape2DPolygon.EdgeMaterials != null &&
				shape2DPolygon.EdgeTexgens != null/* &&
				shape2DPolygon.edgeTexgenFlags != null*/)
			{
				containedMaterialCount = Mathf.Min(shape2DPolygon.EdgeMaterials.Length, 
												   shape2DPolygon.EdgeTexgens.Length/*, 
												   shape2DPolygon.edgeTexgenFlags.Length*/);
			}

			if (capMaterial == null)
			{ 
				capMaterial = MaterialUtility.WallMaterial;
				capTexgen	= new TexGen(-1);
			}
			
			for (var i = 0; i < dstPolygons.Length; i++)
			{
				if (i < containedMaterialCount)
				{
					//shape.TexGenFlags[i] = shape2DPolygon.edgeTexgenFlags[i];
					shape.Materials  [i] = shape2DPolygon.EdgeMaterials[i];
					shape.TexGens	 [i] = shape2DPolygon.EdgeTexgens[i];
					shape.Surfaces   [i].TexGenIndex = i;
					shape.TexGens[i].MaterialIndex	= -1;
				} else
				{  
					shape.Materials[i] = capMaterial;
					shape.TexGens[i] = capTexgen;
					//shape.TexGenFlags[i]			= TexGenFlags.None;
					shape.Surfaces[i].TexGenIndex = i;
					shape.TexGens[i].MaterialIndex	= -1;
				}
				if (smooth.HasValue)
				{ 
					if (i < count)
					{
						shape.TexGens[i].SmoothingGroup = smoothinggroup;
					} else
					{
						shape.TexGens[i].SmoothingGroup = 0;
					}
				}
			}

			for (var s = 0; s < dstPolygons.Length; s++)
			{
				var normal		= shape.Surfaces[s].Plane.normal;
				shape.Surfaces[s].Plane = GeometryUtility.CalcPolygonPlane(controlMesh, (short)s);
				Vector3 tangent, binormal;
				GeometryUtility.CalculateTangents(normal, out tangent, out binormal);
				//var tangent		= Vector3.Cross(GeometryUtility.CalculateTangent(normal), normal).normalized;
				//var binormal	= Vector3.Cross(normal, tangent);
				shape.Surfaces[s].Tangent  = tangent;
				shape.Surfaces[s].BiNormal = binormal;
				shape.Surfaces[s].TexGenIndex = s;
			}

			controlMesh.IsValid = ControlMeshUtility.Validate(controlMesh, shape);
			if (controlMesh.IsValid)
				return true;

			controlMesh = null; 
			shape = null;
			return false;
		}


		// FIXME: move this to a more logical class
		public static void RemoveDuplicatePoints(ref Vector3[] vertices)

		{
			// remove any points that are too close to one another
			for (int j = vertices.Length - 1, i = vertices.Length - 2; i >= 0; j = i, i--)
			{
				if ((vertices[j] - vertices[i]).sqrMagnitude < MathConstants.DistanceEpsilon)
				{
					ArrayUtility.RemoveAt(ref vertices, j);
				}
			}
			while (vertices.Length > 3 && (vertices[0] - vertices[vertices.Length - 1]).sqrMagnitude < MathConstants.DistanceEpsilon)
			{
				var lastIndex = vertices.Length - 1;
				ArrayUtility.RemoveAt(ref vertices, lastIndex);
			}
		}

		// FIXME: move this to a more logical class
		public static void RemoveDuplicatePoints(ShapePolygon shapePolygon)
		{
			var vertices		= shapePolygon.Vertices;
			var edgeMaterials	= shapePolygon.EdgeMaterials;
			var edgeTexgens		= shapePolygon.EdgeTexgens;

			// remove any points that are too close to one another
			for (int j = vertices.Length - 1, i = vertices.Length - 2; i >= 0; j = i, i--)
			{
				if ((vertices[j] - vertices[i]).sqrMagnitude < MathConstants.DistanceEpsilon)
				{
					ArrayUtility.RemoveAt(ref vertices, j);
					ArrayUtility.RemoveAt(ref edgeMaterials, j);
					ArrayUtility.RemoveAt(ref edgeTexgens, j);
				}
			}
			while (vertices.Length > 3 && (vertices[0] - vertices[vertices.Length - 1]).sqrMagnitude < MathConstants.DistanceEpsilon)
			{
				var lastIndex = vertices.Length - 1;
				ArrayUtility.RemoveAt(ref vertices, lastIndex);
				ArrayUtility.RemoveAt(ref edgeMaterials, lastIndex);
				ArrayUtility.RemoveAt(ref edgeTexgens, lastIndex);
			}

			shapePolygon.Vertices = vertices;
			shapePolygon.EdgeMaterials = edgeMaterials;
			shapePolygon.EdgeTexgens = edgeTexgens;
		}

		// FIXME: move this to a more logical class
		public static List<ShapePolygon> CreateCleanPolygonsFromVertices(Vector3[] vertices,
																		 Vector3 origin,
																		 CSGPlane buildPlane)
		{
			Vector3[] projectedVertices;
			return	CreateCleanPolygonsFromVertices(vertices,
												   origin,
												   buildPlane,
												   out projectedVertices);
		}
		
		// FIXME: move this to a more logical class
		public static List<ShapePolygon> CreateCleanPolygonsFromVertices(Vector3[]	 vertices,
																		 Vector3	 origin,
																		 CSGPlane	 buildPlane,
																		 out Vector3[] projectedVertices)
		{
			if (vertices.Length < 3)
			{
				projectedVertices = null;
				return null;
			}
			var m			= Matrix4x4.TRS(-origin, Quaternion.identity, Vector3.one);
			var vertices2d	= GeometryUtility.RotatePlaneTo2D(m, vertices, buildPlane);
			/*
			for (int i0 = vertices2d.Length - 1, i1 = 0; i1 < vertices2d.Length; i1++)
			{
				reset_loop:;
				var vertex_i0 = vertices2d[i0];
				var vertex_i1 = vertices2d[i1];
				for (int j0 = i1 + 1, j1 = i1 + 2; j1 < vertices2d.Length; j1++)
				{
					if ((i1 == j0 && j1 == i0) || 
						i0 == j0)
						continue;

					var vertex_j0 = vertices2d[j0];
					var vertex_j1 = vertices2d[j1];
					if (i1 == j0)
					{

					} else
					if (j1 == i0)
					{

					} else
					{
						if (!GeometryUtility.Intersects(vertex_i0, vertex_i1, vertex_j0, vertex_j1))
							continue;
						 
						Vector2 intersection;
						if (!GeometryUtility.TryIntersection(vertex_i0, vertex_i1, vertex_j0, vertex_j1, out intersection))
							continue;

						if (i1 > j1)
						{
							ArrayUtility.Insert(ref vertices2d, i1, intersection);
							ArrayUtility.Insert(ref vertices2d, j1, intersection);
						} else
						{
							ArrayUtility.Insert(ref vertices2d, j1, intersection);
							ArrayUtility.Insert(ref vertices2d, i1, intersection);
						}
						goto reset_loop;
					}
				}
			}
			*/
			projectedVertices = GeometryUtility.ToVector3XZ(vertices2d);
			return CreateCleanSubPolygonsFromVertices(vertices2d, origin, buildPlane);
		}

		// FIXME: move this to a more logical class
		static List<ShapePolygon> CreateCleanSubPolygonsFromVertices(Vector2[]	vertices2d,
																	 Vector3	origin,
																	 CSGPlane	buildPlane)
		{
			if (vertices2d.Length < 3)
				return null;
			 
			for (int i = 0; i < vertices2d.Length - 2; i++)
			{
				for (int j = i + 2; j < vertices2d.Length; j++)
				{
					if ((vertices2d[j] - vertices2d[i]).sqrMagnitude < MathConstants.DistanceEpsilon)
					{
						List<ShapePolygon> combined_polygons = null;
						
						var left_length  = i;
						var right_length = (vertices2d.Length - j);
						var other_length = left_length + right_length;

						if (other_length > 2)
						{
							var	other_vertices		= new Vector2[other_length];

							if (left_length > 0)
								Array.Copy(vertices2d, 0, other_vertices, 0, left_length);
								
							Array.Copy(vertices2d, j, other_vertices, left_length, right_length);
							combined_polygons = CreateCleanSubPolygonsFromVertices(other_vertices, origin, buildPlane);
						}

						var center_length = (j - i);
						if (center_length > 2)
						{
							var first_vertices		= new Vector2[center_length];

							Array.Copy(vertices2d, i, first_vertices, 0, center_length);

							var first_polygons = CreateCleanSubPolygonsFromVertices(first_vertices, origin, buildPlane);
							if (combined_polygons != null)
								combined_polygons.AddRange(first_polygons);
							else
								combined_polygons = first_polygons;
						}

						return combined_polygons;
					}
				}
			}

			var polygonSign = GeometryUtility.CalcPolygonSign(vertices2d);
			if (polygonSign == 0)
				return null;
			
			if (polygonSign < 0)
				Array.Reverse(vertices2d);
			
			List<List<Vector2>> outlines = null;
			try { outlines = InternalCSGModelManager.External.ConvexPartition(vertices2d); } catch { outlines = null; }
			if (outlines == null)
				return null;

			var polygons = new List<ShapePolygon>();
			for (int b = 0; b < outlines.Count; b++)
			{
				if (GeometryUtility.IsNonConvex(outlines[b]))
					return null;

				polygons.Add(new ShapePolygon(GeometryUtility.ToVector3XZReversed(outlines[b])));
			} 
				
			return polygons;
		}

		// FIXME: move this to a more logical class
		public static void FixMaterials(Vector3[]			originalVertices,
										List<ShapePolygon>	polygons,
										Quaternion			rotation,
										Vector3				origin,
										CSGPlane			buildPlane,
										Material[]			edgeMaterials = null,
										TexGen[]			edgeTexgens = null,
										ShapeEdge[]			shapeEdges = null)
		{
			if (shapeEdges != null)
			{
				for (int e = 0; e < shapeEdges.Length; e++)
				{
					shapeEdges[e].EdgeIndex = -1;
					shapeEdges[e].PolygonIndex = -1;
				}
			}

			for (int p = 0; p < polygons.Count; p++)
			{
				var shapePolygon	= polygons[p];
				var vertices3d		= shapePolygon.Vertices;

				var shapeMaterials	= new Material[vertices3d.Length];
				var shapeTexgens	= new TexGen[vertices3d.Length];
				//var shapeTexgenFlags	= new TexGenFlags[vertices3d.Length];

				if (edgeMaterials != null &&
					edgeTexgens != null)
				{
					var indices = new int[vertices3d.Length];
					for (int n0 = 0; n0 < vertices3d.Length; n0++)
					{
						indices[n0] = -1;
						for (int n1 = 0; n1 < originalVertices.Length; n1++)
						{
							float diff = (vertices3d[n0] - originalVertices[n1]).sqrMagnitude;
							if (diff > MathConstants.EqualityEpsilonSqr)
								continue;

							indices[n0] = n1;
							break;
						}
					}

					for (int n0 = indices.Length - 1, n1 = 0; n1 < indices.Length; n0 = n1, n1++)
					{
						var vertex0 = indices[n0] % edgeMaterials.Length;
						var vertex1 = indices[n1] % edgeMaterials.Length;

						if (vertex0 == -1 || vertex1 == -1)
						{
							shapeMaterials[n1] = MaterialUtility.WallMaterial;
							shapeTexgens[n1] = new TexGen(n0);
							//shapeTexgenFlags[n1]	= TexGenFlags.Discarded;
						} else
						if ((Mathf.Abs(vertex1 - vertex0) == 1 ||
							vertex0 == vertices3d.Length - 1 && vertex1 == 0))
						{
							if (shapeEdges != null)
							{
								shapeEdges[vertex0].PolygonIndex = p;
								shapeEdges[vertex0].EdgeIndex    = n1;
							}
							shapeMaterials[n1] = edgeMaterials[vertex0];
							shapeTexgens  [n1] = edgeTexgens[vertex0];
							//shapeTexgenFlags[n1]	= TexGenFlags.None;
						} else
						{
							shapeMaterials[n1] = edgeMaterials[vertex0];// MaterialUtility.WallMaterial;
							shapeTexgens  [n1] = new TexGen(n0);
							//shapeTexgenFlags[n1]	= TexGenFlags.None;
						}
					}
				} else
				{
					for (int n0 = 0; n0 < vertices3d.Length; n0++)
					{
						shapeMaterials[n0] = MaterialUtility.WallMaterial;
						shapeTexgens[n0] = new TexGen(n0);
						//shapeTexgenFlags[n0]	= TexGenFlags.None;
					}
				}

				shapePolygon.EdgeMaterials = shapeMaterials;
				shapePolygon.EdgeTexgens = shapeTexgens;

				OperationsUtility.RemoveDuplicatePoints(shapePolygon);
			}
		}



		public static GameObject CreateGameObject(Transform parent, string name, bool worldPositionStays)
		{
			GameObject gameObject;
			if (name == null) gameObject = new GameObject();
			else gameObject = new GameObject(name);
			if (parent != null && parent)
			{
				gameObject.transform.SetParent(parent, worldPositionStays);
				if (name != null)
					gameObject.name = GameObjectUtility.GetUniqueNameForSibling(parent, name);
			}
			return gameObject;
		}


		[UnityEditor.MenuItem("GameObject/Realtime-CSG/Model", false, 30)]
		public static CSGModel CreateModelInstanceInScene()
		{
			var gameObject = new GameObject("Model");
			gameObject.name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(null, "Model");
			var model = InternalCSGModelManager.CreateCSGModel(gameObject);

			UnityEditor.Selection.activeGameObject = gameObject;
			SelectionUtility.LastUsedModel = model;
			Undo.RegisterCreatedObjectUndo(gameObject, "Created model");
			InternalCSGModelManager.Refresh();
			return model;
		}

		public static CSGModel CreateModelInstanceInScene(bool selectModel)
		{
			var gameObject = new GameObject("Model");
			gameObject.name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(null, "Model");
			var model = InternalCSGModelManager.CreateCSGModel(gameObject);

			if (selectModel)
			{
				UnityEditor.Selection.activeGameObject = gameObject;
			}
			SelectionUtility.LastUsedModel = model;
			Undo.RegisterCreatedObjectUndo(gameObject, "Created model");
			InternalCSGModelManager.Refresh();
			return model;
		}

		public static GameObject CreateOperation(Transform parent, string name, bool worldPositionStays)
		{
			var gameObject = CreateGameObject(parent, name, worldPositionStays);
			//var operation = 
			gameObject.AddComponent<CSGOperation>();
			Undo.RegisterCreatedObjectUndo(gameObject, "Created operation");
			InternalCSGModelManager.Refresh();
			return gameObject;
		}

		[UnityEditor.MenuItem("GameObject/Realtime-CSG/Operation", false, 31)]
		public static CSGOperation CreateOperationInstanceInScene()
		{
			
			var lastUsedModelTransform = SelectionUtility.LastUsedModel == null ? null : SelectionUtility.LastUsedModel.transform;
			if (lastUsedModelTransform == null)
				lastUsedModelTransform = CreateModelInstanceInScene().transform;

			var name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(lastUsedModelTransform, "Operation"); ;
			var gameObject = new GameObject(name);
			gameObject.transform.SetParent(lastUsedModelTransform, true);
			var operation = gameObject.AddComponent<CSGOperation>();

			UnityEditor.Selection.activeGameObject = gameObject;
			Undo.RegisterCreatedObjectUndo(gameObject, "Created operation");
			InternalCSGModelManager.Refresh();
			InternalCSGModelManager.UpdateMeshes();
			return operation;
		}

		public static GameObject CreateBrush(ControlMesh controlMesh, Shape shape, Transform parent, string name, bool worldPositionStays)
		{
#if DEMO
			if (CSGBindings.BrushesAvailable() <= 0)
			{
				return null;
			}
#endif
			var gameObject = CreateGameObject(parent, name, worldPositionStays);
			var brush = gameObject.AddComponent<CSGBrush>();
			brush.ControlMesh = controlMesh;
			brush.Shape = shape;
			gameObject.SetActive(shape != null && controlMesh != null);
			Undo.RegisterCreatedObjectUndo(gameObject, "Created brush");
			InternalCSGModelManager.Refresh();
			return gameObject;
		}

		[UnityEditor.MenuItem("GameObject/Realtime-CSG/Brush", false, 31)]
		public static CSGBrush CreateBrushInstanceInScene()
		{
#if DEMO
			if (CSGBindings.BrushesAvailable() <= 0)
			{
				return null;
			}
#endif
			var lastUsedModelTransform = SelectionUtility.LastUsedModel == null ? null : SelectionUtility.LastUsedModel.transform;
			if (lastUsedModelTransform == null)
				lastUsedModelTransform = CreateModelInstanceInScene().transform;

			var name = UnityEditor.GameObjectUtility.GetUniqueNameForSibling(lastUsedModelTransform, "Brush");
			var gameObject = new GameObject(name);
			var brush = gameObject.AddComponent<CSGBrush>();

			gameObject.transform.SetParent(lastUsedModelTransform, true);
			gameObject.transform.position = new Vector3(0.5f, 0.5f, 0.5f); // this aligns it's vertices to the grid
			BrushFactory.CreateCubeControlMesh(out brush.ControlMesh, out brush.Shape, Vector3.one);

			UnityEditor.Selection.activeGameObject = gameObject;
			Undo.RegisterCreatedObjectUndo(gameObject, "Created brush");
			InternalCSGModelManager.Refresh();
			InternalCSGModelManager.UpdateMeshes();
			return brush;
		}

		[UnityEditor.MenuItem("GameObject/Group selection %G", false, 32)]
		public static void GroupSelectionInOperation()
		{
			if (Selection.activeObject == null)
				return;

			var childTransforms = new List<Transform>(Selection.transforms);
			Selection.activeObject = null;
			if (childTransforms.Count == 0)
				return;

			for (int i = childTransforms.Count - 1; i >= 0; i--)
			{
				var iterator = childTransforms[i].parent;
				bool found = false;
				while (iterator != null)
				{
					if (childTransforms.Contains(iterator))
					{
						found = true;
						break;
					}
					iterator = iterator.parent;
				}
				if (found)
				{
					childTransforms.RemoveAt(i);
				}
			}
			var sortedChildTransform = new SortedList<int, Transform>();
			for (int i = 0; i < childTransforms.Count; i++)
			{
				sortedChildTransform.Add(childTransforms[i].GetSiblingIndex(), childTransforms[i]);
			}

			childTransforms.Clear();
			childTransforms.AddRange(sortedChildTransform.Values);
			
			var parentTransforms	= new List<Transform>(childTransforms.Count);
			var groupTransforms		= new List<Transform>(childTransforms.Count);
			var parentIndices		= new List<int>(childTransforms.Count);
			for (int i = 0; i < childTransforms.Count; i ++)
			{
				var index = parentTransforms.IndexOf(childTransforms[i].parent);
				if (index == -1)
				{
					var parent			= childTransforms[i].parent;
					parentTransforms    .Add(parent);
					index = parentTransforms.Count - 1;
				}
				parentIndices.Add(index);
			}

			Undo.IncrementCurrentGroup();
			var undo_group_index = Undo.GetCurrentGroup();
			
			foreach (var parentTransform in parentTransforms)
			{
				var name		= UnityEditor.GameObjectUtility.GetUniqueNameForSibling(parentTransform, "Operation");
				var group		= CreateGameObject(parentTransform, name, false);
				var operation	= group.AddComponent<CSGOperation>();
				operation.PassThrough = true;
				groupTransforms.Add(group.transform);
				Undo.RegisterCreatedObjectUndo(group, "Created operation");
			}


			Transform prevGroup = null;
			//for (int i = childTransforms.Count - 1; i >= 0; i--)
			for (int i = 0; i < childTransforms.Count; i++)
			{
				var group	= groupTransforms[parentIndices[i]];
				var index	= childTransforms[i].GetSiblingIndex();
				Undo.SetTransformParent(childTransforms[i], group, "Moved gameObject under operation");
				Undo.RecordObject(childTransforms[i], "Set sibling index of operation");
				if (prevGroup != group)
					group.SetSiblingIndex(index);
				prevGroup = group;
			}

			Undo.CollapseUndoOperations(undo_group_index);
		}
	}
}
