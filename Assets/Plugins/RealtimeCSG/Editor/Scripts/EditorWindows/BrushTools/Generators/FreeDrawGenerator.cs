using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{	
    internal sealed class FreeDrawGenerator : ExtrudedGeneratorBase, IBrushGenerator
	{	
		[NonSerialized] Vector3		worldPosition;
		[NonSerialized] Vector3		prevWorldPosition;
		[NonSerialized] bool		onLastPoint			= false;
		[NonSerialized] CSGPlane	geometryPlane		= new CSGPlane(0, 1, 0, 0);
		[NonSerialized] CSGPlane?	hoverDefaultPlane;
		
        [NonSerialized] bool		haveDragged			= false;

		[NonSerialized] Vector3		prevDragDifference;
		[NonSerialized] Vector2		clickMousePosition;
		[NonSerialized] int			clickCount			= 0;

		// free-draw specific
		private static readonly int ShapeBuilderEdgeHash	= "CSGShapeBuilderEdge".GetHashCode();
		const float					handle_on_distance	= 4.0f;

		public bool HaveSelectedEdges { get { return CanCommit && settings.HaveSelectedEdges; } }
		public bool HaveSelectedEdgesOrVertices { get { return CanCommit && (settings.HaveSelectedEdges || settings.HaveSelectedVertices); } }
		


		[SerializeField] Outline2D	settings		= new Outline2D();
		
		public uint CurveSides
		{
			get
			{
				return RealtimeCSG.CSGSettings.CurveSides;
			}
			set
			{
				if (RealtimeCSG.CSGSettings.CurveSides == value)
					return;
				
				Undo.RecordObject(this, "Modified Shape Curve Sides");
				RealtimeCSG.CSGSettings.CurveSides = value;
				UpdateBaseShape();
			}
		}
		
		//set from 0-1
		float alpha = 1.0f;
		public float Alpha
		{
			get
			{
				return alpha;
			}
			set
			{
				if (alpha == value)
					return;

				Undo.RecordObject(this, "Modified Shape Subdivision");
				alpha = value;
				UpdateBaseShape();
			}
		}

		protected override IShapeSettings ShapeSettings { get { return settings; } }

		public override void Init()
		{
			base.Init();
            haveDragged = false;
		}
		
		public void GenerateFromPolygon(CSGBrush brush, CSGPlane plane, Vector3 direction, Vector3[] meshVertices, int[] indices, bool drag)
		{
			Init();
			
			base.forceDragHandle = drag;
			base.ignoreOrbit = true;
			base.planeOnGeometry = true;
			base.smearTextures = false;
            settings.Init(meshVertices, indices);

			haveForcedDirection = true;
			forcedDirection = direction;
			
			for (int i = 0; i < indices.Length; i++)
			{
				settings.onGeometryVertices[i]	= true;
				settings.onPlaneVertices[i]	= plane;
				settings.onBrushVertices[i]	= brush;
			}
			
			var realVertices	= settings.GetVertices();
			geometryPlane		= plane;
			var newPlane = GeometryUtility.CalcPolygonPlane(realVertices);
			if (newPlane.normal.sqrMagnitude != 0)
				base.buildPlane		= newPlane;

			var brush_cache = InternalCSGModelManager.GetBrushCache(brush);
			if (brush_cache != null && brush_cache.childData != null && brush_cache.childData.Model)
				base.geometryModel = brush_cache.childData.Model;
			
			StartEditMode();
		}

		
		public override void Reset() 
		{
            settings.Reset();
			base.Reset();
			onLastPoint = false;
			hoverDefaultPlane = null;
		}

		public bool HotKeyReleased()
		{
			ResetVisuals();

			if (base.editMode == EditMode.CreatePlane)
			{
				if (settings.VertexLength < 3)
				{
					Cancel();
					return false;
				} else
					editMode = EditMode.CreateShape;
			}
			if (base.editMode == EditMode.CreateShape)
				return StartEditMode();

			return true;
		}

		internal void SetSubdivisionOnVertex(int index, EdgeTangentState state)
		{
			SetSubdivisionOnVertexSide(index, 0, state);
			SetSubdivisionOnVertexSide(index, 1, state);
		}

		internal void SetSubdivisionOnVertexSide(int index, int side, EdgeTangentState state)
		{
			if (settings.edgeTangentState[(index * 2) + side] == state)
				return;
			
			settings.edgeTangentState[(index * 2) + side] = state;
			if (state != EdgeTangentState.Straight &&
				settings.edgeTangentState[(index * 2) + (1 - side)] != EdgeTangentState.Straight)
			{
				if (state == EdgeTangentState.BrokenCurve &&
					settings.edgeTangentState[(index * 2) + (1 - side)] == EdgeTangentState.AlignedCurve)
					settings.edgeTangentState[(index * 2) + (1 - side)] = EdgeTangentState.BrokenCurve;

				settings.haveTangents[index] = true;
				settings.tangents[(index * 2) + side] = -settings.tangents[(index * 2) + (1 - side)];
				return;
			}

			switch (state)
			{
				case EdgeTangentState.BrokenCurve:
				case EdgeTangentState.AlignedCurve:
				{
					if (settings.haveTangents[index])
						break;
					var count = settings.vertices.Length;
					var prev = (index + count - 1) % count;
					var curr = index;
					var next = (index + count + 1) % count;
					var vertex0 = settings.vertices[prev];
					var vertex1 = settings.vertices[curr];
					var vertex2 = settings.vertices[next];

					var centerA = (vertex0 + vertex1 + vertex2) / 3;

					var deltaA = (centerA - vertex1);

					var tangentA = Vector3.Cross(buildPlane.normal, deltaA);
					if (side == 0)
						tangentA = -tangentA;

					settings.haveTangents[index] = true;
					settings.tangents[(index * 2) + side] = tangentA;
					break;
				}
			}
		}

		internal void ToggleSubdivisionOnVertex(int index)
		{
			int length = settings.edgeSelectionState.Length;
			if (length < 3)
				return;

			Undo.RecordObject(this, "Modified Shape Curvature");
			switch (settings.edgeTangentState[(index * 2) + 0])
			{
				case EdgeTangentState.Straight:		SetSubdivisionOnVertex(index, EdgeTangentState.AlignedCurve);	break;
				case EdgeTangentState.AlignedCurve: SetSubdivisionOnVertex(index, EdgeTangentState.BrokenCurve);	break;
				case EdgeTangentState.BrokenCurve:  SetSubdivisionOnVertex(index, EdgeTangentState.Straight);		break;
			}
			UpdateBaseShape();
		}

		internal EdgeTangentState? SelectedTangentState
		{
			get
			{
				EdgeTangentState? state = null;

				for (int b = 0, a = settings.edgeSelectionState.Length - 1; a >= 0; b = a, a--)
				{
					if ((settings.edgeSelectionState[a] & SelectState.Selected) == 0)
						continue;

					if (state == null)
						state = settings.edgeTangentState[(a * 2) + 1];
					if (state.Value != settings.edgeTangentState[(a * 2) + 1])
						return null;
					if (state.Value != settings.edgeTangentState[(b * 2) + 0])
						return null;
				}
				
				for (int b = settings.edgeSelectionState.Length - 1, a = 0; a < settings.edgeSelectionState.Length; b = a, a++)
				{
					if ((settings.vertexSelectionState[a] & SelectState.Selected) == 0)
						continue;

					if (((settings.edgeSelectionState[a] & SelectState.Selected) == 0) !=
						((settings.edgeSelectionState[b] & SelectState.Selected) == 0))
						continue;

					if (state == null)
						state = settings.edgeTangentState[(a * 2) + 0];
					if (state.Value != settings.edgeTangentState[(a * 2) + 0])
						return null;
					if (state.Value != settings.edgeTangentState[(a * 2) + 1])
						return null;
				}

				return state;
			}
			set
			{
				if (!value.HasValue)
					return;
				
				Undo.RecordObject(this, "Modified Shape Curvature");
				for (int b = settings.edgeSelectionState.Length - 1, a = 0; a < settings.edgeSelectionState.Length; b = a, a++)
				{
					if ((settings.vertexSelectionState[a] & SelectState.Selected) == 0)
						continue;

					if (((settings.edgeSelectionState[a] & SelectState.Selected) == 0 &&
						 (settings.edgeSelectionState[b] & SelectState.Selected) == 0) || 
						 value.Value == EdgeTangentState.AlignedCurve)
						SetSubdivisionOnVertex(a, value.Value);
				}

				for (int b = 0, a = settings.edgeSelectionState.Length - 1; a >= 0; b = a, a--)
				{
					if ((settings.edgeSelectionState[a] & SelectState.Selected) == 0)
						continue;

					SetSubdivisionOnVertexSide(a, 1, value.Value);
					SetSubdivisionOnVertexSide(b, 0, value.Value);
				}
				UpdateBaseShape();
			}
		}

		internal void ToggleSubdivisionOnSelectedEdges()
		{
			int length = settings.edgeSelectionState.Length;
			if (length < 3)
				return;

			Undo.RecordObject(this, "Modified Shape Curvature");
			EdgeTangentState state = EdgeTangentState.AlignedCurve;

			for (int b = 0, a = settings.edgeSelectionState.Length - 1; a >= 0; b = a, a--)
			{
				if ((settings.edgeSelectionState[a] & SelectState.Selected) == 0)
					continue;

				if (settings.edgeTangentState[(a * 2) + 1] != EdgeTangentState.Straight ||
					settings.edgeTangentState[(b * 2) + 0] != EdgeTangentState.Straight)
				{
					state = EdgeTangentState.Straight;
					break;
				}
			}

			if (state == EdgeTangentState.Straight)
			{
				state = EdgeTangentState.BrokenCurve;
				for (int b = 0, a = settings.edgeSelectionState.Length - 1; a >= 0; b = a, a--)
				{
					if ((settings.edgeSelectionState[a] & SelectState.Selected) == 0)
						continue;

					if (settings.edgeTangentState[(a * 2) + 1] != EdgeTangentState.AlignedCurve ||
						settings.edgeTangentState[(b * 2) + 0] != EdgeTangentState.AlignedCurve)
					{
						state = EdgeTangentState.Straight;
						break;
					}
				}
			}	

			for (int b = 0, a = settings.edgeSelectionState.Length - 1; a >= 0; b = a, a--)
			{
				if ((settings.edgeSelectionState[a] & SelectState.Selected) == 0)
					continue;
				SetSubdivisionOnVertexSide(a, 1, state);
				SetSubdivisionOnVertexSide(b, 0, state);
			}
			UpdateBaseShape();
		}

		public void SplitSelectedEdge()
		{
			Undo.RecordObject(this, "Split edges");
			int[][] curvedEdges = null;
			//var realVertices	= settings.GetVertices();
			var curvedVertices	= GetCurvedVertices(settings, out curvedEdges);
			if (curvedEdges == null)
				return;
			for (int i = settings.edgeSelectionState.Length - 1; i >= 0; i--)
			{
				if ((settings.edgeSelectionState[i] & SelectState.Selected) == 0)
					continue;

				if (i >= curvedEdges.Length ||
					curvedEdges[i].Length < 2)
					continue;

				var indices = curvedEdges[i];
				Vector3 origin;
				if (indices.Length == 2)
				{
					origin = (curvedVertices[indices[0]] + curvedVertices[indices[1]]) * 0.5f;
				} else
				{ 
					if (indices.Length > 2 &&
						(indices.Length & 1) == 0)
					{
						origin = (curvedVertices[indices[(indices.Length / 2) - 1]] +
									curvedVertices[indices[(indices.Length / 2)    ]]) * 0.5f;
					} else
						origin = curvedVertices[indices[indices.Length / 2]];
				}

				settings.InsertVertexAfter(i, origin);
			}
		}

		protected override void ProjectShapeOnBuildPlane(CSGPlane plane)
		{
			settings.ProjectShapeOnBuildPlane(plane);
		}
		
		static Vector3 PointOnBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
		{
			return (1 - t) * (1 - t) * (1 - t) * p0 + 3 * t * (1 - t) * (1 - t) * p1 + 3 * t * t * (1 - t) * p2 + t * t * t * p3;
		}

		Vector3[] CurvedEdges(Outline2D outline, out int[][] curvedEdges)
		{
			var newPoints = new List<Vector3>();
			var points = outline.vertices;
			var tangents = outline.tangents;
			var length = points.Length;
			curvedEdges = new int[length][];
			
			for (int i = 0; i < points.Length; i++)
			{
				var index1 = i;
				var p1 = points[index1];
				var index2 = (i + 1) % points.Length;
				var p2 = points[index2];
				var tangentIndex1 = (index1 * 2) + 1;
				var tangentIndex2 = (index2 * 2) + 0;

				if (CurveSides == 0 ||
					tangentIndex1 >= settings.edgeTangentState.Length ||
					(settings.edgeTangentState[tangentIndex1] == EdgeTangentState.Straight &&
					 settings.edgeTangentState[tangentIndex2] == EdgeTangentState.Straight))
				{
					curvedEdges[i] = new int[] { newPoints.Count, newPoints.Count + 1 };
					newPoints.Add(p1);
					continue;
				}

				Vector3 p0, p3;

				if (settings.edgeTangentState[tangentIndex1] != EdgeTangentState.Straight)
					p0 = p1 - tangents[tangentIndex1];
				else
					p0 = p1;
				if (settings.edgeTangentState[tangentIndex2] != EdgeTangentState.Straight)
					p3 = p2 - tangents[tangentIndex2];
				else
					p3 = p2;

				int first_index = newPoints.Count;
				newPoints.Add(p1);
				for (int n = 1; n < CurveSides; n++)
				{
					newPoints.Add(PointOnBezier(p1, p0, p3, p2, n / (float)CurveSides));
				}

				var len = newPoints.Count - first_index + 1;
				curvedEdges[i] = new int[len];
				for (int j = 0; j < len; j++)
				{
					curvedEdges[i][j] = j + first_index;
				}
			}
			var last_indices = curvedEdges[curvedEdges.Length - 1];
			if (last_indices.Length > 0)
				last_indices[last_indices.Length - 1] = 0;
			return newPoints.ToArray(); 
		}

		Vector3[] GetCurvedVertices(Outline2D outline, out int[][] curvedEdges)
		{
			if (outline.vertices.Length < 3)
			{
				curvedEdges = null;
				return outline.vertices;
			}
			var vertices3d = CurvedEdges(outline, out curvedEdges);
			var vertices2d = GeometryUtility.RotatePlaneTo2D(vertices3d, buildPlane);
			if ((vertices2d[0] - vertices2d[vertices2d.Length-1]).sqrMagnitude < MathConstants.EqualityEpsilonSqr)
			{
				if (vertices2d.Length == 3)
				{
					curvedEdges = null;
					return outline.vertices;
				}
			}
			return GeometryUtility.Rotate2DToPlane(vertices2d, buildPlane);
		}

		Vector3[] GetCurvedVertices(Outline2D outline)
		{
			int[][] curvedEdges = null;
			return GetCurvedVertices(outline, out curvedEdges);
		}

		internal override bool UpdateBaseShape(bool registerUndo = true)
		{
			if (editMode != EditMode.EditShape &&
				editMode != EditMode.ExtrudeShape)
				return false;
			
			int[][] curvedEdges = null;
			var curvedVertices = GetCurvedVertices(settings, out curvedEdges);
			OperationsUtility.RemoveDuplicatePoints(ref curvedVertices);

			var usedSmoothingGroupIndices = SurfaceUtility.GetUsedSmoothingGroupIndices();
			var	curvedMaterials		= new Material[curvedVertices.Length];
			var curvedEdgeTexgens	= new TexGen[curvedVertices.Length];
			var	curvedShapeEdges	= new ShapeEdge[curvedVertices.Length];
			for (var i = 0; i < settings.edgeTexgens.Length; i++)
			{
				settings.edgeTexgens[i].SmoothingGroup = 0;
			}

            var smoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex(usedSmoothingGroupIndices);
            usedSmoothingGroupIndices.Add(smoothingGroup);
             
            for (var i = 0; i < settings.edgeMaterials.Length; i++)
			{
				if (i >= curvedEdges.Length)
					continue;

				if (curvedEdges[i].Length <= 2)
				{
					settings.edgeTexgens[i].SmoothingGroup = 0;
				}

				if (settings.edgeTexgens[i].SmoothingGroup != 0)
					continue;

				settings.edgeTexgens[i].SmoothingGroup = smoothingGroup;
				
				if (i == 0 &&
					settings.edgeTangentState[(i * 2) + 0] == EdgeTangentState.AlignedCurve ||
					settings.edgeTangentState[(i * 2) + 1] == EdgeTangentState.AlignedCurve)
				{
					var last = settings.edgeTexgens.Length - 1;
					settings.edgeTexgens[last].SmoothingGroup = smoothingGroup;
				}
				if ((((i + 1) * 2) + 1) < settings.edgeTangentState.Length &&
					(settings.edgeTangentState[((i + 1) * 2) + 0] == EdgeTangentState.AlignedCurve ||
					 settings.edgeTangentState[((i + 1) * 2) + 1] == EdgeTangentState.AlignedCurve))
				{
					settings.edgeTexgens[i + 1].SmoothingGroup = smoothingGroup;
				}
			}
			
			for (int i = 0, n = 0; i < settings.edgeMaterials.Length; i++)
			{
				var material	= settings.edgeMaterials[i];
				var texGen		= settings.edgeTexgens[i];
				if (i >= curvedEdges.Length)
					continue;

				if (curvedEdges[i].Length <= 2)
					texGen.SmoothingGroup = 0;

				for (var j = 0; j < curvedEdges[i].Length - 1; j++, n++)
				{
					curvedMaterials[n] = material;
					curvedEdgeTexgens[n] = texGen;
				}
			}

			Vector3[] projectedVertices;
			var newPolygons = OperationsUtility.CreateCleanPolygonsFromVertices(curvedVertices, 
																				base.centerPoint[0], 
																				buildPlane,
																				out projectedVertices);
			if (newPolygons == null)
			{
				if (registerUndo)
					CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
				return true;
			}
			OperationsUtility.FixMaterials(projectedVertices,
										   newPolygons,
										   Quaternion.identity,
										   base.centerPoint[0],
										   buildPlane,
										   curvedMaterials,
										   curvedEdgeTexgens,
										   curvedShapeEdges);
			if (registerUndo)
				CSGBrushEditorManager.ResetMessage();
			
			GenerateBrushesFromPolygons(newPolygons.ToArray(), curvedShapeEdges);
			UpdateExtrudedShape(Height, registerUndo: registerUndo);
			return true;
		}

		bool BuildPlaneIsReversed
		{
			get
			{/*
				if (planeOnGeometry)
				{
					if (settings.onPlaneVertices.Length > 0)
					{
						var plane0 = settings.onPlaneVertices[0];
						var plane0_neg = plane0.Negated();
						for (int i = 1; i < settings.onPlaneVertices.Length; i++)
						{
							if (settings.onPlaneVertices[i] != plane0 &&
								settings.onPlaneVertices[i] != plane0_neg)
							{
								planeOnGeometry = false;
								break;
							}
						}
					} else
						planeOnGeometry = false;
				}
				*/
				var realPlane = geometryPlane;/*
				if (!planeOnGeometry)
					realPlane = Grid.CurrentGridPlane;*/

				if (Vector3.Dot(realPlane.normal, buildPlane.normal) < 0)
					return true;
				return false;
			}
		}

		internal override bool StartExtrudeMode(bool showErrorMessage = true)
		{
			if (settings.VertexLength < 3)
				return false;
			
			settings.TryFindPlaneMaterial(buildPlane);

			int[][] curvedEdges = null;
			var curvedVertices = GetCurvedVertices(settings, out curvedEdges);
			OperationsUtility.RemoveDuplicatePoints(ref curvedVertices);

			settings.UpdateEdgeMaterials(buildPlane.normal);

			var usedSmoothingGroupIndices = SurfaceUtility.GetUsedSmoothingGroupIndices();
			var curvedMaterials		= new Material[curvedVertices.Length];
			var curvedEdgeTexgens	= new TexGen[curvedVertices.Length];
			var	curvedShapeEdges	= new ShapeEdge[curvedVertices.Length];

			if (curvedEdges != null)
			{
				for (int i = 0; i < settings.edgeTexgens.Length; i++)
				{
					settings.edgeTexgens[i].SmoothingGroup = 0;
				}

				for (int i = 0; i < settings.edgeMaterials.Length; i++)
				{
					if (i >= curvedEdges.Length)
						continue;

					if (curvedEdges[i].Length <= 2)
					{
						settings.edgeTexgens[i].SmoothingGroup = 0;
					}

					if (settings.edgeTexgens[i].SmoothingGroup != 0)
						continue;

					uint smoothingGroup = SurfaceUtility.FindUnusedSmoothingGroupIndex(usedSmoothingGroupIndices);
					usedSmoothingGroupIndices.Add(smoothingGroup);
					settings.edgeTexgens[i].SmoothingGroup = smoothingGroup;

					if (i == 0 &&
						settings.edgeTangentState[(i * 2) + 0] == EdgeTangentState.AlignedCurve ||
						settings.edgeTangentState[(i * 2) + 1] == EdgeTangentState.AlignedCurve)
					{
						var last = settings.edgeTexgens.Length - 1;
						settings.edgeTexgens[last].SmoothingGroup = smoothingGroup;
					}
					if (i < (settings.edgeTexgens.Length - 1) &&
						i < (settings.vertices.Length - 1) &&
						(settings.edgeTangentState[((i + 1) * 2) + 0] == EdgeTangentState.AlignedCurve ||
						settings.edgeTangentState[((i + 1) * 2) + 1] == EdgeTangentState.AlignedCurve))
					{
						settings.edgeTexgens[i + 1].SmoothingGroup = smoothingGroup;
					}
				}

				for (int i = 0, n = 0; i < settings.edgeMaterials.Length; i++)
				{
					if (i >= curvedEdges.Length)
						continue;

					var material	= settings.edgeMaterials[i];
					var texGen		= settings.edgeTexgens[i];

					if (curvedEdges[i].Length <= 2)
						texGen.SmoothingGroup = 0;

					for (int j = 0; j < curvedEdges[i].Length - 1; j++, n++)
					{
						curvedMaterials[n] = material;
						curvedEdgeTexgens[n] = texGen;
					}
				}
			}


			// reverse buildPlane if it's different
			if (BuildPlaneIsReversed)
			{
				buildPlane = buildPlane.Negated();
				settings.Negated();
			}

			Vector3[] projectedVertices;
			var newPolygons = OperationsUtility.CreateCleanPolygonsFromVertices(curvedVertices, 
																				base.centerPoint[0],
																				buildPlane,
																				out projectedVertices);
			if (newPolygons == null)
			{
				ClearPolygons();
				if (showErrorMessage)
					CSGBrushEditorManager.ShowMessage("Could not create brush from given 2D shape");
				HideGenerateBrushes();
				return false;
			}
			OperationsUtility.FixMaterials(projectedVertices,
										   newPolygons,
										   Quaternion.identity,
										   base.centerPoint[0],
										   buildPlane,
										   curvedMaterials,
										   curvedEdgeTexgens,
										   curvedShapeEdges);
			CSGBrushEditorManager.ResetMessage();

			GenerateBrushesFromPolygons(newPolygons.ToArray(), curvedShapeEdges);
			return true;
		}
		
		internal override bool CreateControlMeshForBrushIndex(CSGModel parentModel, CSGBrush brush, ShapePolygon polygon, float height, out ControlMesh newControlMesh, out Shape newShape)
		{
			var direction = haveForcedDirection ? forcedDirection : buildPlane.normal;
			if (!OperationsUtility.GenerateControlMeshFromVertices(polygon, 
																	GeometryUtility.RotatePointIntoPlaneSpace(buildPlane, direction),
																	height, 
																	brush.transform.lossyScale,

																	settings.planeMaterial,
																	settings.planeTexgen, 
																			   
																	null, 
																	true,
																	out newControlMesh,
																	out newShape))
			{
				return false;
			}
						
			if (brush.Shape != null)
				InternalCSGModelManager.UnregisterMaterials(parentModel, brush.Shape, false);
			brush.Shape = newShape;
			brush.ControlMesh = newControlMesh;
			InternalCSGModelManager.ValidateBrush(brush, true);							
			InternalCSGModelManager.RegisterMaterials(parentModel, brush.Shape, true);
			ControlMeshUtility.RebuildShape(brush);
			return true;
		}

		void PaintEdgeSides(Vector3 start, Vector3 end)
		{
			var wireframeColor = ColorSettings.BoundsOutlines;

			var delta		= end - start;
			var center		= (end + start) * 0.5f;
			
			var xdistance = Vector3.Project(delta, gridTangent);
			var zdistance = Vector3.Project(delta, gridBinormal);

			var point0 = start + xdistance + zdistance;
			var point1 = start + xdistance;
			var point2 = start;
			var point3 = start + zdistance;

			var points = new Vector3[] { point0, point1, point1, point2, point2, point3, point3, point0 };
			
			PaintUtility.DrawDottedLines(points, wireframeColor, 4.0f);
			  
			var endPoint = Camera.current.transform.position;

			points = new Vector3[] { point0, point1, point2, point3 };
			int closest_index = -1;
			float closest_distance = float.NegativeInfinity;
			for (int i = 0; i < points.Length; i++)
			{
				float distance = (points[i] - endPoint).sqrMagnitude;
				if (distance > closest_distance)
				{
					closest_index = i;
					closest_distance = distance;
				}
			}

			int indexA1 = (closest_index + 1) % 4;
			int indexA2 = (indexA1 + 1) % 4;
			int indexB1 = (closest_index + 3) % 4;
			int indexB2 = (indexB1 + 3) % 4; 

			var edgeCenterA = (points[indexA1] + points[indexA2]) * 0.5f;
			var edgeCenterB = (points[indexB1] + points[indexB2]) * 0.5f;
			var edgeLengthA = GeometryUtility.CleanLength((points[indexA1] - points[indexA2]).magnitude);
			var edgeLengthB = GeometryUtility.CleanLength((points[indexB1] - points[indexB2]).magnitude);
			if (Mathf.Abs(edgeLengthA) > 0 && Mathf.Abs(edgeLengthB) > 0)
			{
				PaintSideLength(edgeCenterA, center, edgeLengthA, ((indexA1 & 1) == 1) ? "Z:" : "X:");
				PaintSideLength(edgeCenterB, center, edgeLengthB, ((indexB1 & 1) == 1) ? "X:" : "Z:");
			}
		}

		void Paint(int id)
		{
			var temp		= Handles.color;
			var origMatrix	= Handles.matrix;
					
			Handles.matrix = MathConstants.identityMatrix;
			var rotation = Camera.current.transform.rotation;

			var realVertices = settings.GetVertices(); 
			if (realVertices != null && realVertices.Length > 0)
			{
				var wireframeColor		= ColorSettings.WireframeOutline;
				var topWireframeColor	= ColorSettings.BoundsEdgeHover;

				ArrayUtility.Add(ref realVertices, worldPosition);
				var curvedVertices = GetCurvedVertices(settings);
				OperationsUtility.RemoveDuplicatePoints(ref curvedVertices);

				if (curvedVertices.Length >= 3)
				{
					var newPolygons = OperationsUtility.CreateCleanPolygonsFromVertices(curvedVertices, 
																						MathConstants.zeroVector3,
																						buildPlane);
					if (newPolygons != null)
					{
						var matrix = Matrix4x4.TRS(buildPlane.pointOnPlane, Quaternion.FromToRotation(MathConstants.upVector3, buildPlane.normal), MathConstants.oneVector3);
						var color = ColorSettings.ShapeDrawingFill;
						for (int i = 0; i < newPolygons.Count; i++)
						{
							PaintUtility.DrawPolygon(matrix, newPolygons[i].Vertices, color);
						}
					} else
					{
						wireframeColor = Color.red;
					}
				}

				for (int i = 1; i < curvedVertices.Length; i++)
				{
					PaintUtility.DrawLine(curvedVertices[i - 1], curvedVertices[i], ToolConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(curvedVertices[i - 1], curvedVertices[i], wireframeColor, 4.0f);
				}
				if (curvedVertices.Length > 3)
				{
					PaintUtility.DrawLine(curvedVertices[curvedVertices.Length - 1], curvedVertices[0], ToolConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(curvedVertices[curvedVertices.Length - 1], curvedVertices[0], wireframeColor, 4.0f);
				}

				var isReversed = BuildPlaneIsReversed;

				var forward = Camera.current.transform.forward; 
				if (Event.current.button != 1)
					//GUIUtility.hotControl == id || GUIUtility.hotControl == 0)
				{ 
					PaintUtility.DrawLine(realVertices[realVertices.Length - 2], worldPosition, ToolConstants.oldLineScale, topWireframeColor);
					PaintUtility.DrawDottedLine(realVertices[realVertices.Length - 2], worldPosition, topWireframeColor, 4.0f);

					var origin			= (realVertices[realVertices.Length - 2] + worldPosition) * 0.5f;
					var delta			= (realVertices[realVertices.Length - 2] - worldPosition);
					var distance		= delta.magnitude;

					
					PaintEdgeSides(realVertices[realVertices.Length - 2], worldPosition);

					
					var sideways		= Vector3.Cross(delta.normalized, forward);
					
					var textCenter2D	= HandleUtility.WorldToGUIPoint(origin);
					var sideways2D		= (HandleUtility.WorldToGUIPoint(origin + (sideways * 10)) - textCenter2D).normalized;
					var fromCenter		= MathConstants.zeroVector2;

					if (realVertices.Length > 0)
					{
						var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
						var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
						for (int i = 0; i < realVertices.Length; i++)
						{
							min.x = Mathf.Min(min.x, realVertices[i].x);
							min.y = Mathf.Min(min.y, realVertices[i].y);
							min.z = Mathf.Min(min.z, realVertices[i].z);
							max.x = Mathf.Max(max.x, realVertices[i].x);
							max.y = Mathf.Max(max.y, realVertices[i].y);
							max.z = Mathf.Max(max.z, realVertices[i].z);
						}
						var center = (min + max) * 0.5f;
						fromCenter = (textCenter2D - HandleUtility.WorldToGUIPoint(center)).normalized;
					}
					/*
					if (vertices.Length >= 3)
					{
						Vector3 v1 = fromCenter;
						Vector3 v2 = sideways2D;
						Vector3 n = Vector3.Cross(v1, v2);
						var dot = Vector3.Dot(n, buildPlane.normal);
						Debug.Log(v1 + " " + v2 + " " + n + " " + buildPlane.normal + " " + dot);
						if (dot < 0)
						{
							sideways2D = -sideways2D;
						}
					}
					*/

					if (sideways2D == MathConstants.zeroVector2)
					{
						sideways2D = fromCenter;
						if (sideways2D == MathConstants.zeroVector2)
							sideways2D = MathConstants.upVector3;
					} else
					{
						if (isReversed)
							sideways2D = -sideways2D;
					}

					textCenter2D += sideways2D * (hover_text_distance * 2);

					var textCenterRay	= HandleUtility.GUIPointToWorldRay(textCenter2D);
					var textCenter		= textCenterRay.origin + textCenterRay.direction * ((Camera.current.farClipPlane + Camera.current.nearClipPlane) * 0.5f);

					Handles.color = Color.black;
					Handles.DrawLine(origin, textCenter);
					PaintUtility.DrawScreenText(textCenter2D, 
						Units.ToRoundedDistanceString(Mathf.Abs(distance)));

					PaintUtility.DrawDottedLine(worldPosition, realVertices[0], topWireframeColor, 4.0f);
				}

				Handles.color = wireframeColor;
				for (int i = 0; i < realVertices.Length; i++)
				{
					float handleSize = GUIStyleUtility.GetHandleSize(realVertices[i]);
					float scaledHandleSize = handleSize * ToolConstants.handleScale;
					PaintUtility.SquareDotCap(id, realVertices[i], rotation, scaledHandleSize);
				}
			}

			if (Event.current.button != 1)
			{
				Handles.color = Handles.selectedColor;
				//if ((Camera.current != null) && Camera.current.pixelRect.Contains(Event.current.mousePosition))
				{
					float handleSize = GUIStyleUtility.GetHandleSize(worldPosition);
					float scaledHandleSize = handleSize * ToolConstants.handleScale;
					PaintUtility.SquareDotCap(id, worldPosition, rotation, scaledHandleSize);
				}
			}

			Handles.matrix = origMatrix;
			Handles.color = temp;
		}
		
		protected override void CreateControlIDs()
		{
			base.CreateControlIDs();
			
			if (settings.VertexLength > 0 &&
				(editMode == EditMode.EditShape ||
				editMode == EditMode.ExtrudeShape))
			{
				settings.vertexIDs = new int[settings.VertexLength];
				for (int i = 0; i < settings.VertexLength; i++)
				{
					settings.vertexIDs[i] = GUIUtility.GetControlID(ShapeBuilderPointHash, FocusType.Passive);
				}

				settings.tangentIDs = new int[settings.VertexLength * 2];
				for (int i = 0; i < settings.VertexLength * 2; i++)
				{
					settings.tangentIDs[i] = GUIUtility.GetControlID(ShapeBuilderPointHash, FocusType.Passive);
				}

				settings.edgeIDs = new int[settings.VertexLength];
				for (int i = 0; i < settings.VertexLength; i++)
				{
					settings.edgeIDs[i] = GUIUtility.GetControlID(ShapeBuilderEdgeHash, FocusType.Passive);
				}
			}
		}

		public override void PerformDeselectAll()
		{
			if (editMode != EditMode.EditShape || 
				!settings.DeselectAll())
				Cancel();
		}

		public override void PerformDelete()
		{
			if (editMode != EditMode.EditShape &&
				editMode != EditMode.ExtrudeShape)
			{
				Cancel();
				return;
			}

			Undo.RecordObject(this, "Deleted vertices");
			settings.DeleteSelectedVertices();
			if (settings.VertexLength < 3)
			{
				Cancel();
			}
		}

		bool	 havePlane			= false;

		protected override void HandleCreateShapeEvents(Rect sceneRect)
		{
			var		 current			= Event.current;
			bool	 pointOnEdge		= false;
			bool	 vertexOnGeometry	= false;

			CSGBrush vertexOnBrush		= null;
			CSGPlane vertexOnPlane		= buildPlane;
			CSGPlane hoverBuildPlane	= buildPlane;
			var camera = Camera.current;
			if (camera != null && (GUIUtility.hotControl == base.shapeId || GUIUtility.hotControl == 0) &&
				camera.pixelRect.Contains(current.mousePosition))
			{
				if (!hoverDefaultPlane.HasValue ||
					settings.VertexLength == 0)
				{
					bool forceGrid = Grid.ForceGrid;
					Grid.ForceGrid = false;
					hoverDefaultPlane = Grid.CurrentGridPlane;
					Grid.ForceGrid = forceGrid;
					base.geometryModel = null;
				}
				if (settings.VertexLength == 0)
				{
					havePlane = false;
				}
				if (editMode == EditMode.CreatePlane && !havePlane)
				{
					if (settings.VertexLength >= 3)
					{
						hoverBuildPlane = GeometryUtility.CalcPolygonPlane(settings.GetVertices());
						if (hoverBuildPlane.normal.sqrMagnitude != 0)
						{
							buildPlane = hoverBuildPlane;
							editMode = EditMode.CreateShape;
							havePlane = true;
						}
					}
					
					BrushIntersection intersection;
					if (!camera.orthographic && !havePlane &&
						SceneQueryUtility.FindWorldIntersection(current.mousePosition, out intersection, MathConstants.GrowBrushFactor))
					{
						worldPosition	= intersection.worldIntersection;
						if (intersection.surfaceInverted)
							hoverBuildPlane = intersection.plane.Negated();
						else
							hoverBuildPlane = intersection.plane;
						buildPlane		= hoverBuildPlane;
						havePlane		= true;
						vertexOnGeometry = true;
						
						vertexOnBrush = intersection.brush;
						vertexOnPlane = hoverBuildPlane;
					} else
					{
						hoverBuildPlane = hoverDefaultPlane.Value;
						
						var mouseRay = HandleUtility.GUIPointToWorldRay(current.mousePosition);
						worldPosition		= hoverBuildPlane.Intersection(mouseRay);
						vertexOnGeometry	= false;
						vertexOnBrush		= null;
						if (hoverBuildPlane.normal.sqrMagnitude != 0)
						{ 
							vertexOnPlane		= hoverBuildPlane;
							buildPlane			= hoverBuildPlane;
						}
					}

					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(worldPosition, hoverBuildPlane, ref base.visualSnappedEdges, out snappedOnBrush);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
										visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
							vertexOnGeometry = true;
						}
					} else
					{
						Debug.Log("snapFunction == null");
					}           
				} else
				{
					var mouseRay = HandleUtility.GUIPointToWorldRay(current.mousePosition);
					worldPosition = hoverBuildPlane.Intersection(mouseRay);

					ResetVisuals();
					if (snapFunction != null)
					{
						CSGBrush snappedOnBrush;
						worldPosition = snapFunction(worldPosition, hoverBuildPlane, ref base.visualSnappedEdges, out snappedOnBrush);
						if (snappedOnBrush != null)
						{
							pointOnEdge = (visualSnappedEdges != null &&
										visualSnappedEdges.Count > 0);
							vertexOnBrush = snappedOnBrush;
						}
					} else
					{
						Debug.Log("snapFunction == null");
					}
				}

				if (geometryModel == null && vertexOnBrush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(vertexOnBrush);
					if (brush_cache != null && brush_cache.childData != null && brush_cache.childData.Model)
						geometryModel = brush_cache.childData.Model;
				}

				if (settings.VertexLength > 2)
				{
					var first2D			= Camera.current.WorldToScreenPoint(settings.GetPosition(0));
					var current2D		= Camera.current.WorldToScreenPoint(worldPosition);
					var distance		= (current2D - first2D).magnitude;
					var snapDistance	= 2.0f * handle_on_distance;

					if (distance < snapDistance)
					{
						worldPosition = settings.GetPosition(0);
						onLastPoint = true;
					} else
						onLastPoint = false;
				} else 
					onLastPoint = false;
				
				if (worldPosition != prevWorldPosition)
				{
					prevWorldPosition = worldPosition;
					if (current.type != EventType.Repaint)
					{
						SceneView.RepaintAll();
					}
				}
				
				base.visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(worldPosition);
				base.visualSnappedBrush = vertexOnBrush;
			}
			
			Grid.SetForcedGrid(hoverBuildPlane);
			
			if (!SceneTools.IsDraggingObjectInScene &&
				current.type == EventType.Repaint)
			{
				if (settings.realEdge != null)
				{
					Handles.color = ColorSettings.WireframeOutline;
					PaintUtility.DrawDottedLines(settings.realEdge, Handles.secondaryColor, 4.0f);
				}
					
				PaintSnapVisualisation();
				Paint(base.shapeId);
			}
			
			var type = current.GetTypeForControl(base.shapeId);
			switch (type)
			{
				case EventType.layout:
				{
					return;
				}

				case EventType.ValidateCommand:
				case EventType.keyDown:
				{
					if (GUIUtility.hotControl != base.shapeId)
						return;
					
					if (Keys.PerformActionKey.IsKeyPressed() ||
						Keys.DeleteSelectionKey.IsKeyPressed() ||
						Keys.CancelActionKey.IsKeyPressed())
					{
						Event.current.Use();
					}
					return;
				}
				case EventType.keyUp:
				{
					if (GUIUtility.hotControl != base.shapeId)
						return;
					
					if (Keys.FreeBuilderMode.IsKeyPressed() ||
						Keys.PerformActionKey.IsKeyPressed())
					{
						HotKeyReleased();
						Event.current.Use();
						return;
					}
					if (Keys.DeleteSelectionKey.IsKeyPressed() ||
						Keys.CancelActionKey.IsKeyPressed())
					{
						PerformDeselectAll();
						Event.current.Use();
						return;
					}
					return;
				}

				case EventType.MouseDown:
				{
					if (!sceneRect.Contains(Event.current.mousePosition))
						break;
					if ((GUIUtility.hotControl != 0 && 
						GUIUtility.hotControl != base.shapeId && 
						GUIUtility.hotControl != base.shapeEditID) ||
						(Event.current.modifiers != EventModifiers.None) ||
						Event.current.button != 0 || 
						(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
						return;
					
					Event.current.Use();
					if (!settings.HaveVertices)
					{
						if (GUIUtility.hotControl == 0 && base.shapeId != -1)
						{
							base.CalculateWorldSpaceTangents();
							GUIUtility.hotControl = base.shapeId;
							GUIUtility.keyboardControl = base.shapeId;
							EditorGUIUtility.editingTextField = false;
						}
					}

					if (GUIUtility.hotControl == base.shapeId)
					{
						if (!float.IsNaN(worldPosition.x) && !float.IsInfinity(worldPosition.x) &&
							!float.IsNaN(worldPosition.y) && !float.IsInfinity(worldPosition.y) &&
							!float.IsNaN(worldPosition.z) && !float.IsInfinity(worldPosition.z))
						{
							if (hoverBuildPlane.normal.sqrMagnitude != 0)
								buildPlane = hoverBuildPlane;
							CalculateWorldSpaceTangents();

							if (!settings.HaveVertices)
							{
								planeOnGeometry = !pointOnEdge && vertexOnGeometry;
								geometryPlane   = buildPlane;
							} else
							{
								if (!pointOnEdge && vertexOnGeometry)
									planeOnGeometry = true;

								if (vertexOnGeometry)
								{
									var plane = hoverDefaultPlane.Value;
									var distance = plane.Distance(worldPosition);
									plane.d += distance;
									hoverDefaultPlane = plane;

									for (int i = 0; i < settings.VertexLength;i++)
									{
										if (!settings.onGeometryVertices[i])
										{
											var guipoint	= camera.WorldToScreenPoint(settings.GetPosition(i));// HandleUtility.WorldToGUIPoint();
											var cameraRay	= camera.ScreenPointToRay(guipoint);// HandleUtility.GUIPointToWorldRay(guipoint);
											settings.SetPosition(i, plane.Intersection(cameraRay));
											settings.onGeometryVertices[i] = true;
										}
									}
								}
							}
							
							settings.AddVertex(worldPosition, vertexOnBrush, vertexOnPlane, vertexOnGeometry);
							SceneView.RepaintAll();
						}
							
						SceneView.RepaintAll();
					}
					return;
				}
				case EventType.MouseMove:
				{
					clickCount = 0;
					break;
				}
				case EventType.MouseDrag:
				{
					clickCount = 0;
					if (GUIUtility.hotControl != base.shapeId)
						return;

					Event.current.Use();
					return;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl != base.shapeId ||
						Event.current.button != 0 ||
						(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
						return;
					
					Event.current.Use();
					
					if (Event.current.button == 0)
					{
						if (onLastPoint)
						{
							if (settings.VertexLength < 3)
							{
								Cancel();
							} else
							{
								//settings.DeleteVertex(settings.VertexLength - 1);
								editMode = EditMode.CreateShape;
								HotKeyReleased();
							}
						}
						return;
					}
					return;
				}
			}
		}

		static void DrawDotControlState(SelectState renderState, int[] dotIDs, SelectState[] selectionStates, Vector3[] vertices, Quaternion rotation, EdgeTangentState[] state, int colorState)
		{
			for (int i = 0; i < vertices.Length; i++)
			{
				SelectState vertexState = selectionStates[i];
				var id = dotIDs[i];

				if (vertexState != renderState)
					continue;

				float handleSize = GUIStyleUtility.GetHandleSize(vertices[i]);
				if (state != null)
				{
					if (state[i] == EdgeTangentState.AlignedCurve)
					{
						Handles.color = ColorSettings.AlignedCurveStateColor[colorState];
						PaintUtility.CircleDotCap(id, vertices[i], rotation, handleSize * ToolConstants.handleScale * 1.25f);
					} else
					{
						Handles.color = ColorSettings.BrokenCurveStateColor[colorState];
						PaintUtility.DiamondDotCap(id, vertices[i], rotation, handleSize * ToolConstants.handleScale * 1.25f);
					}
				} else
				{
					Handles.color = ColorSettings.PointInnerStateColor[colorState];
					PaintUtility.SquareDotCap(id, vertices[i], rotation, handleSize * ToolConstants.handleScale);
				}
			}
		}

		static void DrawDotControlStates(int[] dotIDs, SelectState[] selectionStates, Vector3[] vertices, Quaternion rotation, EdgeTangentState[] state = null)
		{
			DrawDotControlState(SelectState.None, dotIDs, selectionStates, vertices, rotation, state, 0);
			DrawDotControlState(SelectState.Hovering, dotIDs, selectionStates, vertices, rotation, state, 1);
			DrawDotControlState(SelectState.Selected, dotIDs, selectionStates, vertices, rotation, state, 2);
			DrawDotControlState(SelectState.Selected | SelectState.Hovering, dotIDs, selectionStates, vertices, rotation, state, 3);
		}

		protected override void HandleEditShapeEvents(Rect sceneRect)
		{			
			var wireframeColor		= ColorSettings.WireframeOutline;
			var topWireframeColor	= ColorSettings.BoundsEdgeHover;

			Vector3[] realVertices = null;
			Vector3[] curvedVertices = null;
			int[][] curvedEdges = null;
			if (Event.current.type == EventType.Layout ||
				Event.current.type == EventType.Repaint)
			{
				realVertices = settings.GetVertices();
				curvedVertices = GetCurvedVertices(settings, out curvedEdges);
				OperationsUtility.RemoveDuplicatePoints(ref curvedVertices);
			}

			if (!SceneTools.IsDraggingObjectInScene &&
				Event.current.type == EventType.Repaint)
			{			
				var origMatrix	= Handles.matrix;
				Handles.matrix = MathConstants.identityMatrix;

				
				if (curvedVertices.Length >= 3 && (Height == 0 || editMode == EditMode.EditShape))
				{
					var newPolygons = OperationsUtility.CreateCleanPolygonsFromVertices(curvedVertices, 
																						MathConstants.zeroVector3,
																						buildPlane);
					if (newPolygons != null) 
					{
						var matrix = Matrix4x4.TRS(buildPlane.pointOnPlane, Quaternion.FromToRotation(MathConstants.upVector3, buildPlane.normal), MathConstants.oneVector3);
						var color = ColorSettings.ShapeDrawingFill;
						for (int i = 0; i < newPolygons.Count; i++)
						{
							PaintUtility.DrawPolygon(matrix, newPolygons[i].Vertices, color);
						}
					} else
					{
						wireframeColor = Color.red;
					}
				}

				if (editMode == EditMode.ExtrudeShape && Height != 0)
				{
					for (int i = 1; i < curvedVertices.Length; i++)
					{
						PaintUtility.DrawLine(curvedVertices[i - 1], curvedVertices[i], ToolConstants.oldLineScale, wireframeColor);
						PaintUtility.DrawDottedLine(curvedVertices[i - 1], curvedVertices[i], wireframeColor, 4.0f);
					}

					PaintUtility.DrawLine(curvedVertices[curvedVertices.Length - 1], curvedVertices[0], ToolConstants.oldLineScale, wireframeColor);
					PaintUtility.DrawDottedLine(curvedVertices[curvedVertices.Length - 1], curvedVertices[0], wireframeColor, 4.0f);

					var delta = (base.centerPoint[1] - base.centerPoint[0]);
					for (int i = 1; i < curvedVertices.Length; i++)
					{
						PaintUtility.DrawLine(curvedVertices[i - 1] + delta, curvedVertices[i] + delta, ToolConstants.oldLineScale, ColorSettings.BoundsEdgeHover);
						PaintUtility.DrawDottedLine(curvedVertices[i - 1] + delta, curvedVertices[i] + delta, ColorSettings.BoundsEdgeHover, 4.0f);
					}

					for (int i = 0; i < realVertices.Length; i++)
					{
						PaintUtility.DrawLine(realVertices[i] + delta, realVertices[i], ToolConstants.oldLineScale, wireframeColor);
						PaintUtility.DrawDottedLine(realVertices[i] + delta, realVertices[i], wireframeColor, 4.0f);
					}
				
					PaintUtility.DrawLine(curvedVertices[curvedVertices.Length - 1] + delta, curvedVertices[0] + delta, ToolConstants.oldLineScale, topWireframeColor);
					PaintUtility.DrawDottedLine(curvedVertices[curvedVertices.Length - 1] + delta, curvedVertices[0] + delta, topWireframeColor, 4.0f);
				}
				Handles.matrix = origMatrix;
			}

			if (Event.current.type == EventType.MouseDown ||
				Event.current.type == EventType.MouseDrag)
			{
				if (GUIUtility.hotControl == 0 && forceDragHandle)
				{
					if (StartExtrudeMode())
					{
						GrabHeightHandle(1);
						forceDragHandle = false;
					}
				}
			}
			
			HandleHeightHandles(sceneRect, true);

            if (Event.current.type != EventType.layout &&
				(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
            {
                int nearestControl = HandleUtility.nearestControl;
                int foundVertexIndex	= -1;
                int foundTangentIndex	= -1;
                int foundEdgeIndex		= -1;
                for (int i = 0; i < settings.VertexLength; i++)
                {
                    var vertexID = settings.vertexIDs[i];
                    if (vertexID == nearestControl)
                    {
                        foundVertexIndex = i;
						break;
					}
					var edgeID = settings.edgeIDs[i];
                    if (edgeID == nearestControl)
                    {
                        foundEdgeIndex = i;
						break;
					}
				}
				for (int i = 0; i < settings.tangents.Length; i++)
				{
					var tangentID = settings.tangentIDs[i];
					if (tangentID == nearestControl)
					{
						foundTangentIndex = i;
						break;
					}
				}
				settings.UnHoverAll();
                if (foundVertexIndex != -1)
                    settings.HoverOnVertex(foundVertexIndex);
				if (foundTangentIndex != -1)
					settings.HoverOnTangent(foundTangentIndex);
				if (foundEdgeIndex != -1)
                    settings.HoverOnEdge(foundEdgeIndex);
                if (foundVertexIndex != settings.prevHoverVertex ||
                    foundTangentIndex != settings.prevHoverTangent ||
					foundEdgeIndex != settings.prevHoverEdge)
                {
                    SceneView.RepaintAll();
                    settings.prevHoverVertex	= foundVertexIndex;
					settings.prevHoverTangent	= foundTangentIndex;
					settings.prevHoverEdge		= foundEdgeIndex;
                }
            }

			// render edges
			if (!SceneTools.IsDraggingObjectInScene && settings.edgeIDs != null &&
				Event.current.GetTypeForControl(settings.edgeIDs[0]) == EventType.Repaint)
			{
				var temp = Handles.color;
				var origMatrix = Handles.matrix;

				Handles.matrix = MathConstants.identityMatrix;

				if (settings.realEdge != null)
				{
					Handles.color = ColorSettings.PointInnerStateColor[0];
					PaintUtility.DrawDottedLines(settings.realEdge, ColorSettings.PointInnerStateColor[0], 4.0f);
				}

				if (visualSnappedEdges != null)
					PaintUtility.DrawLines(visualSnappedEdges.ToArray(), ToolConstants.oldThickLineScale, ColorSettings.SnappedEdges);
					
				if (visualSnappedGrid != null)
				{
					var _origMatrix = Handles.matrix;
					Handles.matrix = MathConstants.identityMatrix;
					PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
					Handles.matrix = _origMatrix;
				}

				if (visualSnappedBrush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(visualSnappedBrush);
					if (brush_cache != null &&
						brush_cache.compareTransformation != null &&
						brush_cache.childData != null &&
						brush_cache.childData.ModelTransform != null &&
						brush_cache.childData.ModelTransform)
					{
						var brush_translation = brush_cache.compareTransformation.modelLocalPosition + brush_cache.childData.ModelTransform.position;
						CSGRenderer.DrawOutlines(visualSnappedBrush.brushID, brush_translation, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines);
					}
				}

				//var wireframeColor = ColorSettings.PointInnerStateColor[0];

				//var rotation = Camera.current.transform.rotation;

				var nearControl = HandleUtility.nearestControl;
				var hotControl = GUIUtility.hotControl;

				int hover_index = -1;
				for (int i = 0; i < settings.VertexLength; i++)
				{
					SelectState state = settings.edgeSelectionState[i];
					if (settings.edgeIDs[i] == hotControl ||
						(hotControl == 0 && settings.edgeIDs[i] == nearControl))
						hover_index = i;
					if ((state & (SelectState.Selected | SelectState.Hovering)) != SelectState.None)
						continue;

					if (curvedEdges != null &&
						i < curvedEdges.Length)
					{
						var indices = curvedEdges[i];
						for (int j = 0; j < indices.Length - 1; j++)
						{
							var vertex1 = curvedVertices[indices[j]];
							var vertex2 = curvedVertices[indices[(j + 1)]];

							PaintUtility.DrawLine(vertex1, vertex2, ToolConstants.oldLineScale, wireframeColor);
							PaintUtility.DrawDottedLine(vertex1, vertex2, wireframeColor, 4.0f);
						}
					}
				}

				wireframeColor = ColorSettings.PointInnerStateColor[1];
				for (int i = 0; i < settings.VertexLength; i++)
				{
					SelectState state = settings.edgeSelectionState[i];
					if ((state & (SelectState.Selected | SelectState.Hovering)) != SelectState.Hovering)
						continue;

					var indices = curvedEdges[i];
					for (int j = 0; j < indices.Length - 1; j++)
					{
						var vertex1 = curvedVertices[indices[j]];
						var vertex2 = curvedVertices[indices[(j + 1)]];

						PaintUtility.DrawLine(vertex1, vertex2, ToolConstants.oldLineScale, wireframeColor);
						PaintUtility.DrawDottedLine(vertex1, vertex2, wireframeColor, 4.0f);
					}
				}

				wireframeColor = ColorSettings.PointInnerStateColor[3];
				for (int i = 0; i < settings.VertexLength; i++)
				{
					SelectState state = settings.edgeSelectionState[i];
					if ((state & SelectState.Selected) != SelectState.Selected)
						continue;

					var indices = curvedEdges[i];
					for (int j = 0; j < indices.Length - 1; j++)
					{
						var vertex1 = curvedVertices[indices[j]];
						var vertex2 = curvedVertices[indices[(j + 1)]];

						PaintUtility.DrawLine(vertex1, vertex2, ToolConstants.oldLineScale, wireframeColor);
						PaintUtility.DrawDottedLine(vertex1, vertex2, wireframeColor, 4.0f);
					}
				}

				if (hover_index > -1)
				{
					var camera = Camera.current;
					var forward = camera.transform.forward;
					for (int i = 0; i < settings.VertexLength; i++)
					{
						var prev_index = (i + settings.VertexLength - 1) % settings.VertexLength;
						var curr_index = i;
						var next_index = (i + 1) % settings.VertexLength;
							
						if (haveDragged)
						{
							if (prev_index != hover_index &&
								curr_index != hover_index &&
								next_index != hover_index)
								continue;
						} else
						{
							if (curr_index != hover_index)
								continue;
						}

						var indices = curvedEdges[curr_index];
						Vector3 origin;
						if (indices.Length > 2 &&
							(indices.Length & 1) == 0)
						{
							origin = (curvedVertices[indices[(indices.Length / 2) - 1]] +
									  curvedVertices[indices[(indices.Length / 2)    ]]) * 0.5f;
						} else
							origin = curvedVertices[indices[indices.Length / 2]];

						var vertex1 = settings.GetPosition(curr_index);
						var vertex2 = settings.GetPosition(next_index);
						
						var delta			= (vertex1 - vertex2);
						var sideways		= Vector3.Cross(delta.normalized, forward);
						var distance		= delta.magnitude;

						var textCenter2D	= HandleUtility.WorldToGUIPoint(origin);
						textCenter2D += (HandleUtility.WorldToGUIPoint(origin + (sideways * 10)) - textCenter2D).normalized * (hover_text_distance * 2);

						var textCenterRay	= HandleUtility.GUIPointToWorldRay(textCenter2D);
						var textCenter		= textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);
						
						Handles.color = Color.black;
						Handles.DrawLine(origin, textCenter);
						PaintUtility.DrawScreenText(textCenter2D, 
							Units.ToRoundedDistanceString(Mathf.Abs(distance)));
					}
				}

				Handles.matrix = origMatrix;
				Handles.color = temp;
			}
				
			// render vertices
			if (!SceneTools.IsDraggingObjectInScene &&
				Event.current.GetTypeForControl(settings.vertexIDs[0]) == EventType.Repaint)
			{
				var temp = Handles.color;
				var origMatrix = Handles.matrix;

				Handles.matrix = MathConstants.identityMatrix;
				var rotation = Camera.current.transform.rotation;

				if (settings.realEdge != null)
				{
					Handles.color = ColorSettings.PointInnerStateColor[0];
					PaintUtility.DrawDottedLines(settings.realEdge, ColorSettings.PointInnerStateColor[0], 4.0f);
				}

				if (visualSnappedEdges != null)
					PaintUtility.DrawLines(visualSnappedEdges.ToArray(), ToolConstants.oldThickLineScale, ColorSettings.SnappedEdges);
					
				if (visualSnappedGrid != null)
				{
					var _origMatrix = Handles.matrix;
					Handles.matrix = MathConstants.identityMatrix;
					PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
					Handles.matrix = _origMatrix;
				}

				if (visualSnappedBrush != null)
				{
					var brush_cache = InternalCSGModelManager.GetBrushCache(visualSnappedBrush);
					if (brush_cache != null &&
						brush_cache.compareTransformation != null &&
						brush_cache.childData != null &&
						brush_cache.childData.ModelTransform != null &&
						brush_cache.childData.ModelTransform)
					{
						var brush_translation = brush_cache.compareTransformation.modelLocalPosition + brush_cache.childData.ModelTransform.position;
						CSGRenderer.DrawOutlines(visualSnappedBrush.brushID, brush_translation, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines, ColorSettings.SelectedOutlines);
					}						
				}


				var nearControl = HandleUtility.nearestControl;
				var hotControl = GUIUtility.hotControl;
				int hover_index = -1;

				for (int i = 0,j =0; i < settings.VertexLength; i++,j+=2)
				{
					if (settings.vertexIDs[i] == hotControl ||
						settings.tangentIDs[j + 0] == hotControl ||
						settings.tangentIDs[j + 1] == hotControl ||
						(hotControl == 0 && 
							(settings.vertexIDs[i] == nearControl ||
							settings.tangentIDs[j + 0] == nearControl ||
							settings.tangentIDs[j + 1] == nearControl
							)))
						hover_index = i;
				}

				if (settings.tangents.Length == settings.vertices.Length * 2 &&
					settings.edgeTangentState.Length == settings.tangents.Length)
				{
					int curvedEdgeCount = 0;
					for (int i = 0; i < settings.edgeTangentState.Length; i+=2)
					{
						if (settings.edgeTangentState[i + 0] != EdgeTangentState.Straight ||
							settings.edgeTangentState[i + 1] != EdgeTangentState.Straight)
							curvedEdgeCount += 2;
					}
					if (settings.realTangent == null ||
						settings.realTangent.Length != curvedEdgeCount)
					{
						settings.realTangent = new Vector3[curvedEdgeCount];
						settings.realTangentIDs = new int[curvedEdgeCount];
						settings.realTangentSelection = new SelectState[curvedEdgeCount];
						settings.realTangentState = new EdgeTangentState[curvedEdgeCount];
					}

					int counter = 0;
					for (int i = 0, j = 0; j < settings.edgeTangentState.Length; i++, j += 2)
					{
						var state = (settings.edgeTangentState[j + 0] == EdgeTangentState.AlignedCurve &&
									 settings.edgeTangentState[j + 1] == EdgeTangentState.AlignedCurve) ? EdgeTangentState.AlignedCurve : EdgeTangentState.BrokenCurve;
						if (settings.edgeTangentState[j + 0] != EdgeTangentState.Straight)
						{
							settings.realTangent[counter] = settings.vertices[i] - settings.tangents[j + 0];
							settings.realTangentIDs[counter] = settings.tangentIDs[j + 0];
							settings.realTangentSelection[counter] = settings.tangentSelectionState[j + 0];
							settings.realTangentState[counter] = state;
							PaintUtility.DrawLine(settings.vertices[i], settings.realTangent[counter], Color.black);
							PaintUtility.DrawDottedLine(settings.vertices[i], settings.realTangent[counter], Color.black, 4.0f);
							counter++;
						}
						if (settings.edgeTangentState[j + 1] != EdgeTangentState.Straight)
						{
							settings.realTangent[counter] = settings.vertices[i] - settings.tangents[j + 1];
							settings.realTangentIDs[counter] = settings.tangentIDs[j + 1];
							settings.realTangentSelection[counter] = settings.tangentSelectionState[j + 1];
							settings.realTangentState[counter] = state;
							PaintUtility.DrawLine(settings.vertices[i], settings.realTangent[counter], Color.black);
							PaintUtility.DrawDottedLine(settings.vertices[i], settings.realTangent[counter], Color.black, 4.0f);
							counter++;
						}
					}

					if (counter > 0)
						DrawDotControlStates(settings.realTangentIDs, settings.realTangentSelection, settings.realTangent, rotation, settings.realTangentState);
				}

				DrawDotControlStates(settings.vertexIDs, settings.vertexSelectionState, settings.vertices, rotation);
				
				if (hover_index > -1)
				{ 
					var camera = Camera.current;
					var forward = camera.transform.forward;
					for (int i = 0; i < settings.VertexLength; i++)
					{
						var curr_index = i;
						var next_index = (i + 1) % settings.VertexLength;
							
						if (curr_index != hover_index &&
							next_index != hover_index)
							continue;
						
						if (curvedEdges == null || curr_index >= curvedEdges.Length)
							continue; 

						var indices = curvedEdges[curr_index];
						if (indices.Length < 3)
							continue; 
						Vector3 origin;
						if ((indices.Length & 1) == 0)
						{
							origin = (curvedVertices[indices[(indices.Length / 2) - 1]] +
									  curvedVertices[indices[(indices.Length / 2)    ]]) * 0.5f;
						} else
							origin = curvedVertices[indices[indices.Length / 2]];
						
						var vertex1 = settings.GetPosition(curr_index);
						var vertex2 = settings.GetPosition(next_index);
						
						var delta			= (vertex1 - vertex2);
						var sideways		= Vector3.Cross(delta.normalized, forward);
						var distance		= delta.magnitude;

						var textCenter2D	= HandleUtility.WorldToGUIPoint(origin);
						textCenter2D += (HandleUtility.WorldToGUIPoint(origin + (sideways * 10)) - textCenter2D).normalized * (hover_text_distance * 2);

						var textCenterRay	= HandleUtility.GUIPointToWorldRay(textCenter2D);
						var textCenter		= textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

						Handles.color = Color.black;
						Handles.DrawLine(origin, textCenter);
						PaintUtility.DrawScreenText(textCenter2D, 
							Units.ToRoundedDistanceString(Mathf.Abs(distance)));
					}
				}

				Handles.matrix = origMatrix;
				Handles.color = temp;
			}

			switch (Event.current.type)//.GetTypeForControl(base.shapeId))
			{
				case EventType.ValidateCommand:
				case EventType.keyDown:
				{
					//if (GUIUtility.hotControl != base.shapeId)
					//	return;
					if (Keys.InsertPoint.IsKeyPressed())
					{
						Event.current.Use();
						break;
					}
					break;
				}
				case EventType.keyUp:
				{
					//if (GUIUtility.hotControl != base.shapeId)
					//	return;
					if (Keys.InsertPoint.IsKeyPressed())
					{
						SplitSelectedEdge();
						Event.current.Use();
						return;
					}
					break;
				}
			}

			// edges
			for (int i = 0; i < settings.VertexLength; i++)
			{
				var id = settings.edgeIDs[i];
				//var vertex1 = settings.GetPosition(i);
				//var vertex2 = settings.GetPosition((i + 1) % settings.VertexLength);

				switch (Event.current.GetTypeForControl(id))
				{
					case EventType.Layout:
					{
						if (Camera.current == null ||
							(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
							break;
							
						var cameraPlane	= GUIStyleUtility.GetNearPlane(Camera.current);

						var origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;

						if (curvedEdges != null &&
							curvedEdges[i] != null &&
							curvedEdges.Length > i &&
							curvedEdges[i].Length > 0)
						{
							float distance = float.PositiveInfinity;
							var indices = curvedEdges[i];
							for (int j = 0; j < indices.Length - 1; j++)
							{
								var vertex1 = curvedVertices[curvedEdges[i][j]];
								var vertex2 = curvedVertices[curvedEdges[i][j + 1]];

								var line_distance = GUIStyleUtility.DistanceToLine(cameraPlane, vertex1, vertex2);
								if (line_distance < distance)
									distance = line_distance;
							}

							HandleUtility.AddControl(id, distance);
						}
						Handles.matrix = origMatrix;
						break;
					}

					case EventType.ValidateCommand:
					case EventType.keyDown:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
							{
								Event.current.Use(); 
								break;
							}
						}
						break;
					}
					case EventType.keyUp:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
							{
								GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								EditorGUIUtility.editingTextField = false;
								Event.current.Use(); 
								break;
							}
						}
						break;
					}

					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (GUIUtility.hotControl == 0 &&
							(HandleUtility.nearestControl == id && Event.current.button == 0) &&
							//(Event.current.modifiers == EventModifiers.None) &&
							(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
						{
							GUIUtility.hotControl = id;
							GUIUtility.keyboardControl = id;
							EditorGUIUtility.editingTextField = false; 
							EditorGUIUtility.SetWantsMouseJumping(1);
							Event.current.Use(); 
							clickMousePosition = Event.current.mousePosition;
							//clickSceneView = SceneView.lastActiveSceneView;
							prevDragDifference = MathConstants.zeroVector3;
							haveDragged			= false;
							settings.realEdge	= null;
                            settings.CopyBackupVertices();
                            break;
						}
						break;
					}
					case EventType.MouseMove:
					{
						clickCount = 0;
						break;
					}
					case EventType.MouseDrag:
					{
						clickCount = 0;
						if (GUIUtility.hotControl == id && Event.current.button == 0 &&
							(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
						{
							if (!haveDragged)
							{
								var diff = (Event.current.mousePosition - clickMousePosition).magnitude;
								if (diff < 1)
								{
									Event.current.Use(); 
									break;
								}
							}
							//if (Camera.current == null ||
							//	clickSceneView != SceneView.lastActiveSceneView)
							//{
							//	Event.current.Use(); 
							//	break;
							//}

							Undo.RecordObject(this, "Modify shape");
							if (!settings.IsEdgeSelected(i))
                            {
                                settings.DeselectAll();
                                settings.SelectEdge(i, SelectionType.Replace);
                            }
							var startDragPoint		= buildPlane.Intersection(HandleUtility.GUIPointToWorldRay(clickMousePosition));
							var intersectionPoint	= buildPlane.Intersection(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition));
							var difference			= intersectionPoint - startDragPoint;
								
							var k = (i + settings.VertexLength - 1) % settings.VertexLength;
							var j = (i + 1) % settings.VertexLength;
							var l = (i + 2) % settings.VertexLength;
							var movedVertex1	= settings.backupVertices[i] + difference;
							var movedVertex2	= settings.backupVertices[j] + difference;
							settings.realEdge = new Vector3[] {	settings.backupVertices[k], movedVertex1,
																movedVertex1, movedVertex2,
																movedVertex2, settings.backupVertices[l] };
								
							if (snapFunction != null)
							{
								ResetVisuals();
								CSGBrush snappedOnBrush1;
								CSGBrush snappedOnBrush2;
								var vertexDifference1	= snapFunction(movedVertex1, buildPlane, ref visualSnappedEdges, out snappedOnBrush1)
															//point_moved1
															- settings.backupVertices[i];
								var vertexDifference2	= snapFunction(movedVertex2, buildPlane, ref visualSnappedEdges, out snappedOnBrush2)
															//point_moved2
															- settings.backupVertices[j];
									
								if ((vertexDifference1 - difference).sqrMagnitude < (vertexDifference2 - difference).sqrMagnitude)
								{
									difference = vertexDifference1;
								} else
								{
									difference = vertexDifference2;
								}
									
								float snap_distance = float.PositiveInfinity;
								for (int p = 0; p < settings.backupVertices.Length; p++)
								{
									if (p == i)
										continue;
									float handleSize = GUIStyleUtility.GetHandleSize(settings.backupVertices[p]);
									float scaledHandleSize = handleSize * ToolConstants.handleScale * handle_extension;
									float distance = GeometryUtility.DistancePointToCircle(movedVertex1, settings.backupVertices[p], scaledHandleSize);
									if (distance < handle_on_distance)
									{
										if (distance < snap_distance)
										{
											snap_distance = distance;
											difference = (settings.backupVertices[p] - settings.backupVertices[i]);
										}
									}
									distance = GeometryUtility.DistancePointToCircle(movedVertex2, settings.backupVertices[p], scaledHandleSize);
									if (distance < handle_on_distance)
									{
										if (distance < snap_distance)
										{
											snap_distance = distance;
											difference = (settings.backupVertices[p] - settings.backupVertices[j]);
										}
									}
								}
							} else
							{
								Debug.Log("snapFunction == null");
							}
							if (prevDragDifference != difference)
							{
								for (int p = 0; p < settings.backupVertices.Length; p++)
								{
									if (settings.IsVertexSelected(p))
									{
										settings.SetPosition(p, difference + settings.backupVertices[p]);
									}
								}
								prevDragDifference = difference;
								SceneView.RepaintAll();
							}

							if (editMode == EditMode.ExtrudeShape)
							{
								if (StartExtrudeMode(showErrorMessage: false))
									UpdateBaseShape(registerUndo: false);
							}


							GUI.changed = true;
							Event.current.Use(); 
                            haveDragged = true;
                            break;
						}
						break;
					}
					case EventType.MouseUp:
					{
						if (GUIUtility.hotControl == id && Event.current.button == 0 &&
							(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
                        {
							GUIUtility.hotControl = 0;
							GUIUtility.keyboardControl = 0;
							EditorGUIUtility.editingTextField = false;
							EditorGUIUtility.SetWantsMouseJumping(0);
							Event.current.Use(); 

							settings.realEdge	= null;
							ResetVisuals();
                            if (!haveDragged)
							{
								if (Event.current.modifiers == EventModifiers.None)
								{
									clickCount++;
									if (clickCount > 1)
									{
										if ((clickCount & 2) == 2)
										{
											ToggleSubdivisionOnSelectedEdges();
										}
										break;
									}
								}
								var selectionType = SelectionUtility.GetEventSelectionType();
                                if (selectionType == SelectionType.Replace)
                                    settings.DeselectAll();
                                settings.SelectEdge(i, selectionType);
							} else
							{ 
								var removeVertices = new List<int>();
								for (int p0 = settings.VertexLength - 1, p1 = 0; p1 < settings.VertexLength; p0 = p1, p1++)
								{
									if ((settings.GetPosition(p0) - settings.GetPosition(p1)).sqrMagnitude == 0)
									{
										if (settings.IsVertexSelected(p0))
										{
											removeVertices.Add(p1);
										} else
										if (settings.IsVertexSelected(p1))
										{
											removeVertices.Add(p0);
										}
									}
								}
								if (removeVertices.Count > 0)
									Undo.RecordObject(this, "Deleted vertices");
								for (int r = removeVertices.Count - 1; r >= 0; r--)
								{
									settings.SelectVertex(removeVertices[r]);
								}
								removeVertices.Sort();
								for (int r=removeVertices.Count-1;r>=0;r--)
								{
									settings.DeleteVertex(removeVertices[r]);
								}
							}
								

							if (haveDragged && settings.VertexLength < 3)
							{
								Cancel();
							}
							haveDragged = false;
							break;
						}
						break;
					}
				}
			}


			// tangents
			if (settings.tangentIDs != null &&
				settings.tangents != null)
			{
				for (int i = 0; i < settings.tangentIDs.Length; i++)
				{
					var id = settings.tangentIDs[i];
					
					switch (Event.current.GetTypeForControl(id))
					{
						case EventType.Layout:
						{
							if ((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
								break;
							if (i >= settings.tangents.Length)
								break;
							var origMatrix = Handles.matrix;
							Handles.matrix = MathConstants.identityMatrix;
							var tangent = settings.tangents[i];
							var vertex = settings.vertices[i / 2] - tangent;
							float handleSize = GUIStyleUtility.GetHandleSize(vertex);
							float scaledHandleSize = handleSize * ToolConstants.handleScale * handle_extension;
							HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(vertex, scaledHandleSize));
							Handles.matrix = origMatrix;
							break;
						}

						case EventType.ValidateCommand:
						case EventType.keyDown:
						{
							if (GUIUtility.hotControl == id)
							{
								if (Keys.CancelActionKey.IsKeyPressed())
								{
									Event.current.Use(); 
									break;
								}
							}
							break;
						}
						case EventType.keyUp:
						{
							if (GUIUtility.hotControl == id)
							{
								if (Keys.CancelActionKey.IsKeyPressed())
								{
									GUIUtility.hotControl = 0;
									GUIUtility.keyboardControl = 0;
									EditorGUIUtility.editingTextField = false;
									Event.current.Use(); 
									break;
								}
							}
							break;
						}

						case EventType.MouseDown:
						{
							if (!sceneRect.Contains(Event.current.mousePosition))
								break;
							if (GUIUtility.hotControl == 0 &&
								(HandleUtility.nearestControl == id && Event.current.button == 0) &&
								//(Event.current.modifiers == EventModifiers.None) &&
								(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
							{
								GUIUtility.hotControl = id;
								GUIUtility.keyboardControl = id;
								EditorGUIUtility.editingTextField = false;
								EditorGUIUtility.SetWantsMouseJumping(1);
								Event.current.Use(); 
								clickMousePosition = Event.current.mousePosition;
								prevDragDifference = MathConstants.zeroVector3;
								haveDragged = false;
								settings.CopyBackupVertices();
								break;
							}
							break;
						}
						case EventType.MouseMove:
						{
							clickCount = 0;
							break;
						}
						case EventType.MouseDrag:
						{
							clickCount = 0;
							if (GUIUtility.hotControl == id && Event.current.button == 0 &&
								(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
							{
								if (!haveDragged)
								{
									var diff = (Event.current.mousePosition - clickMousePosition).magnitude;
									if (diff < 1)
									{
										Event.current.Use(); 
										break;
									}
								}
								
								Grid.SetForcedGrid(geometryPlane);

								Undo.RecordObject(this, "Modify shape curvature");
								if (!settings.IsTangentSelected(i))
								{
									settings.DeselectAll();
									settings.SelectTangent(i, SelectionType.Replace);
								}
								var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

								var vertex		 = settings.backupVertices[i / 2] - settings.backupTangents[i];
								var alignedPlane = new CSGPlane(Grid.CurrentWorkGridPlane.normal, vertex);
								worldPosition	 = buildPlane.Project(alignedPlane.Intersection(mouseRay));


								var difference	  = worldPosition - vertex;

								CSGBrush snappedOnBrush = null;
								if (snapFunction != null)
								{
									difference = snapFunction(worldPosition, buildPlane, ref visualSnappedEdges, out snappedOnBrush) - vertex;
								}
								else
								{
									Debug.Log("snapFunction == null");
								}

								visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(difference + vertex);


								ResetVisuals();
								
								if (prevDragDifference != difference)
								{
									for (int p = 0; p < settings.backupTangents.Length; p++)
									{
										if (settings.IsTangentSelected(p))
										{
											settings.SetTangent(p, settings.backupTangents[p] - difference);
										}
									}
									prevDragDifference = difference;
									SceneView.RepaintAll();
								}

								if (editMode == EditMode.ExtrudeShape)
								{
									if (StartExtrudeMode(showErrorMessage: false))
										UpdateBaseShape(registerUndo: false);
								}

								GUI.changed = true;
								Event.current.Use(); 
								haveDragged = true;
								break;
							}
							break;
						}
						case EventType.MouseUp:
						{
							if (GUIUtility.hotControl == id && Event.current.button == 0 &&
								(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
							{
								GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								EditorGUIUtility.editingTextField = false;
								EditorGUIUtility.SetWantsMouseJumping(0);
								Event.current.Use(); 

								ResetVisuals();
								if (!haveDragged)
								{
									var selectionType = SelectionUtility.GetEventSelectionType();
									if (selectionType == SelectionType.Replace)
										settings.DeselectAll();
									settings.SelectTangent(i, selectionType);
								}
								break;
							}
							break;
						}
					}
				}
			}


			// vertices
			for (int i = 0; i < settings.VertexLength; i++)
			{
				var id = settings.vertexIDs[i];

				//float length = (shape2D.GetPosition(i) - prevVertex).sqrMagnitude;
				//base.centerPoint += (shape2D.GetPosition(i) + prevVertex) * 0.5f * length;
				//totalLength += length;
				//prevVertex = shape2D.GetPosition(i);

				switch (Event.current.GetTypeForControl(id))
				{
					case EventType.Layout:
					{
						if ((Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
							break;
						var origMatrix = Handles.matrix;
						Handles.matrix = MathConstants.identityMatrix;
						float handleSize = GUIStyleUtility.GetHandleSize(settings.GetPosition(i));
						float scaledHandleSize = handleSize * ToolConstants.handleScale * handle_extension;
						HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(settings.GetPosition(i), scaledHandleSize));
						Handles.matrix = origMatrix;
						break;
					}

					case EventType.ValidateCommand:
					case EventType.keyDown:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
							{
								Event.current.Use(); 
								break;
							}
						}
						break;
					}
					case EventType.keyUp:
					{
						if (GUIUtility.hotControl == id)
						{
							if (Keys.CancelActionKey.IsKeyPressed())
							{
								GUIUtility.hotControl = 0;
								GUIUtility.keyboardControl = 0;
								EditorGUIUtility.editingTextField = false;
								Event.current.Use(); 
								break;
							}
						}
						break;
					}

					case EventType.MouseDown:
					{
						if (!sceneRect.Contains(Event.current.mousePosition))
							break;
						if (GUIUtility.hotControl == 0 &&
							(HandleUtility.nearestControl == id && Event.current.button == 0) &&
							//(Event.current.modifiers == EventModifiers.None) &&
							(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
						{
							GUIUtility.hotControl = id;
							GUIUtility.keyboardControl = id;
							EditorGUIUtility.editingTextField = false; 
							EditorGUIUtility.SetWantsMouseJumping(1);
							Event.current.Use(); 
							clickMousePosition = Event.current.mousePosition;
							//clickSceneView = SceneView.lastActiveSceneView;
							prevDragDifference = MathConstants.zeroVector3;
							haveDragged = false;
                            settings.CopyBackupVertices();
                            break;
						}
						break;
					}
					case EventType.MouseMove:
					{
						clickCount = 0;
						break;
					}
					case EventType.MouseDrag:
					{
						clickCount = 0;
						if (GUIUtility.hotControl == id && Event.current.button == 0 &&
							(Tools.viewTool == ViewTool.None || Tools.viewTool == ViewTool.Pan))
						{
							if (!haveDragged)
							{
								var diff = (Event.current.mousePosition - clickMousePosition).magnitude;
								if (diff < 1)
								{
									Event.current.Use(); 
									break;
								}
							}
							//if (Camera.current == null ||
							//	clickSceneView != SceneView.lastActiveSceneView)
							//{
							//	Event.current.Use(); 
							//	break;
							//}
							Undo.RecordObject(this, "Modify shape");
                            if (!settings.IsVertexSelected(i))
                            {
                                settings.DeselectAll();
                                settings.SelectVertex(i, SelectionType.Replace);
                            }
							var mouseRay		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
								
							var alignedPlane	= new CSGPlane(Grid.CurrentWorkGridPlane.normal, settings.backupVertices[i]);
							var worldPosition	= buildPlane.Project(alignedPlane.Intersection(mouseRay));
								
							var difference = worldPosition - settings.backupVertices[i];
								
							ResetVisuals();
							CSGBrush snappedOnBrush = null;
							if (snapFunction != null)
							{
								difference = snapFunction(worldPosition, buildPlane, ref visualSnappedEdges, out snappedOnBrush) - settings.backupVertices[i];
							} else
							{
								Debug.Log("snapFunction == null");
							}
								
							visualSnappedGrid = Grid.FindAllGridEdgesThatTouchPoint(difference + settings.backupVertices[i]);
								
							{
								int		snapToVertexIndex	= -1;
								float	snapDistance		= float.PositiveInfinity;
								for (int p = 0; p < settings.backupVertices.Length; p++)
								{
									if (p == i)
										continue;
									float handleSize = GUIStyleUtility.GetHandleSize(settings.backupVertices[p]);
									float scaledHandleSize = handleSize * ToolConstants.handleScale * handle_extension;
									float distance = HandleUtility.DistanceToCircle(settings.backupVertices[p], scaledHandleSize);
									if (distance < handle_on_distance)
									{
										if (distance < snapDistance)
										{
											snapDistance = distance;
											snapToVertexIndex = p;
										}
									}
								}
								if (snapToVertexIndex != -1)
								{
									difference = (settings.backupVertices[snapToVertexIndex] - settings.backupVertices[i]);
								}
							}

							if (prevDragDifference != difference)
							{
								for (int p = 0; p < settings.backupVertices.Length; p++)
								{
									if (settings.IsVertexSelected(p))
									{
										settings.SetPosition(p, difference + settings.backupVertices[p]);
									}
								}
								prevDragDifference = difference;
								SceneView.RepaintAll();
							}

							if (editMode == EditMode.ExtrudeShape)
							{
								if (StartExtrudeMode(showErrorMessage: false))
									UpdateBaseShape(registerUndo: false);
							}

							GUI.changed = true;
							Event.current.Use(); 
                            haveDragged = true;
                            break;
						}
						break;
					}
					case EventType.MouseUp:
					{
						if (GUIUtility.hotControl != id || Event.current.button != 0 ||
							(Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
							continue;
						
						GUIUtility.hotControl = 0;
						GUIUtility.keyboardControl = 0;
						EditorGUIUtility.editingTextField = false;
						EditorGUIUtility.SetWantsMouseJumping(0);
						Event.current.Use(); 

						ResetVisuals();
						if (!haveDragged)
						{
							if (Event.current.modifiers == EventModifiers.None)
							{
								clickCount++;
								if (clickCount > 1)
								{
									if ((clickCount & 2) == 2)
									{
										ToggleSubdivisionOnVertex(i);
									}
									break;
								}
							}

							var selectionType = SelectionUtility.GetEventSelectionType();
							if (selectionType == SelectionType.Replace)
								settings.DeselectAll();
							settings.SelectVertex(i, selectionType);
						} else
						{
							var removeVertices = new List<int>();
							for (int p0 = settings.VertexLength - 1, p1 = 0; p1 < settings.VertexLength; p0 = p1, p1++)
							{
								if ((settings.GetPosition(p0) - settings.GetPosition(p1)).sqrMagnitude == 0)
								{
									if (settings.IsVertexSelected(p0))
									{
										removeVertices.Add(p1);
									}
									else
									if (settings.IsVertexSelected(p1))
									{
										removeVertices.Add(p0);
									}
								}
							}
							if (removeVertices.Count > 0)
								Undo.RecordObject(this, "Deleted vertices");
							for (int r = removeVertices.Count - 1; r >= 0; r--)
							{
								settings.SelectVertex(removeVertices[r]);
							}
							removeVertices.Sort();
							for (int r = removeVertices.Count - 1; r >= 0; r--)
							{
								settings.DeleteVertex(removeVertices[r]);
							}
						}

						if (haveDragged && settings.VertexLength < 3)
						{
							Cancel();
						}

						break;
					}
				}
			}
		}
		
		public bool OnShowGUI(bool isSceneGUI)
		{
			return FreeDrawGeneratorGUI.OnShowGUI(this, isSceneGUI);
		}
	}
}
