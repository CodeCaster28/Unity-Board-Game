using System;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEditor;

namespace RealtimeCSG
{
	[Flags]
	[Serializable]
	internal enum SelectState : byte
	{
		None		= 0,
		Hovering	= 1,
		Selected	= 2
	}

	[Serializable]
	internal sealed class ControlMeshState
	{
		[SerializeField] public Transform		BrushTransform;
		[SerializeField] public Transform		ParentTransform;

		[SerializeField] public Vector3[]		BackupPoints;
		[SerializeField] public SelectState[]	MarqueeBackupPointState;

		[SerializeField] public int[]			PointControlId;
		[SerializeField] public SelectState[]	PointSelectState;
		[SerializeField] public Vector3[]		WorldPoints;
		[SerializeField] public Color[]			WorldPointColors;
		[SerializeField] public float[]			WorldPointSizes;
		[SerializeField] public bool[]			WorldPointBackfaced;

		[SerializeField] public int[]			EdgeControlId;
		[SerializeField] public SelectState[]	EdgeSelectState;
		[SerializeField] public Color[]			EdgeColors;
		[SerializeField] public int[]			Edges;
		[SerializeField] public int[]			EdgeStateToHalfEdge;	 
		[SerializeField] public int[]			HalfEdgeToEdgeStates;    // indicesToEdges[edge-index] = index into state 'edges'
		[SerializeField] public int[]			EdgeSurfaces;
			
		[SerializeField] public Vector3[]		BackupPolygonCenterPoints;
		[SerializeField] public CSGPlane[]		BackupPolygonCenterPlanes;
			
		[SerializeField] public int[]			PolygonControlId;
		[SerializeField] public SelectState[]	PolygonSelectState;
		[SerializeField] public Vector3[]		PolygonCenterPoints;
		[SerializeField] public Color[]			PolygonCenterColors;
		[SerializeField] public float[]			PolygonCenterPointSizes;
		[SerializeField] public CSGPlane[]		PolygonCenterPlanes;
		[SerializeField] public int[][]			PolygonPointIndices;
			
		[SerializeField] public int				TargetControlId;
		[SerializeField] public Vector3			RayIntersectionPoint;
		[SerializeField] public Vector3			BrushCenter;

		public ControlMeshState(CSGBrush brush)
		{
			BrushTransform	= brush.GetComponent<Transform>();
			ParentTransform = InternalCSGModelManager.FindParentTransform(BrushTransform);
			var controlMesh	= brush.ControlMesh;
			if (controlMesh == null)
			{
				brush.ControlMesh = new ControlMesh();
				controlMesh = brush.ControlMesh;
			}

			if (controlMesh.Vertices == null) controlMesh.Vertices = new Vector3 [0];
			if (controlMesh.Edges	 == null) controlMesh.Edges    = new HalfEdge[0];
			if (controlMesh.Polygons == null) controlMesh.Polygons = new Polygon [0];

			AllocatePoints(controlMesh.Vertices.Length);
			AllocateEdges(controlMesh.Edges.Length);
			AllocatePolygons(controlMesh.Polygons.Length);
		}

		private void AllocatePoints(int pointCount)
		{
			PointControlId		= new int[pointCount];
			PointSelectState	= new SelectState[pointCount];
			WorldPoints			= new Vector3[pointCount];
			WorldPointColors	= new Color[pointCount * 2];
			WorldPointSizes		= new float[pointCount];
			WorldPointBackfaced	= new bool[pointCount];
		}

		private void AllocateEdges(int edgeCount)
		{
			Edges					= new int[edgeCount];
			EdgeSurfaces			= new int[edgeCount];
			HalfEdgeToEdgeStates	= new int[edgeCount];
			EdgeStateToHalfEdge		= new int[edgeCount / 2];
			EdgeColors				= new Color[edgeCount / 2];
			EdgeSelectState			= new SelectState[edgeCount / 2];
			EdgeControlId			= new int[edgeCount / 2];
		}

		private void AllocatePolygons(int polygonCount)
		{
			PolygonControlId			= new int[polygonCount];
			PolygonSelectState			= new SelectState[polygonCount];
			PolygonCenterPoints			= new Vector3[polygonCount];
			PolygonCenterColors			= new Color[polygonCount * 2];
			PolygonCenterPointSizes		= new float[polygonCount];
			PolygonCenterPlanes			= new CSGPlane[polygonCount];
			PolygonPointIndices			= new int[polygonCount][];
		}

		public void UpdateTransforms(CSGBrush brush)
		{
			BrushTransform	= brush.GetComponent<Transform>();
			ParentTransform = InternalCSGModelManager.FindParentTransform(BrushTransform);
		}

		public void UpdateMesh(ControlMesh controlMesh, Vector3[] vertices = null)
		{
			if (controlMesh == null ||
				controlMesh.Vertices == null ||
				controlMesh.Edges == null ||
				controlMesh.Polygons == null ||
				PolygonPointIndices == null)
			{
				return;
			}

			if (vertices == null)
				vertices = controlMesh.Vertices;

			if (!BrushTransform)
				return;

			var pointCount = vertices.Length;
			if (WorldPoints.Length != pointCount)
				AllocatePoints(pointCount);

			var edgeCount = controlMesh.Edges.Length;
			if (Edges.Length != edgeCount)
				AllocateEdges(edgeCount);

			var polygonCount = controlMesh.Polygons.Length;
			if (PolygonControlId.Length != polygonCount)
				AllocatePolygons(polygonCount);

			var localToWorldMatrix = BrushTransform.localToWorldMatrix;
			for (var p = 0; p < pointCount; p++)
			{
				var worldPoint = localToWorldMatrix.MultiplyPoint(vertices[p]);
				WorldPoints[p] = worldPoint;
			}

			
			var index = 0;
			for (var e = 0; e < edgeCount; e++)
			{
				if (e >= controlMesh.Edges.Length ||
					index >= Edges.Length)
					continue;

				var twin = controlMesh.Edges[e].TwinIndex;
				if (twin < e || // if it's less than e then we've already handled our twin
					twin >= controlMesh.Edges.Length)
					continue;

				var polygonIndex = controlMesh.Edges[e].PolygonIndex;
				if (polygonIndex < 0 || polygonIndex >= controlMesh.Polygons.Length)
					continue;

				var twinPolygonIndex = controlMesh.Edges[twin].PolygonIndex;
				if (twinPolygonIndex < 0 || twinPolygonIndex >= controlMesh.Polygons.Length)
					continue;

				var vertexIndex1 = controlMesh.Edges[e   ].VertexIndex;
				var vertexIndex2 = controlMesh.Edges[twin].VertexIndex;

				if (vertexIndex1 < 0 || vertexIndex1 >= PointSelectState.Length ||
					vertexIndex2 < 0 || vertexIndex2 >= PointSelectState.Length)
					continue;

				Edges[index    ] = vertexIndex1;
				Edges[index + 1] = vertexIndex2;
				EdgeStateToHalfEdge[index / 2] = e;
				HalfEdgeToEdgeStates[e] = index;
				HalfEdgeToEdgeStates[twin] = index;
				EdgeSurfaces[index] = polygonIndex;
				EdgeSurfaces[index + 1] = twinPolygonIndex;

				if ((PointSelectState[vertexIndex1] & SelectState.Selected) == SelectState.Selected &&
					(PointSelectState[vertexIndex2] & SelectState.Selected) == SelectState.Selected)
					EdgeSelectState[index / 2] |= SelectState.Selected;
				else
					EdgeSelectState[index / 2] &= ~SelectState.Selected;

				//edgeSelectState[index / 2] = SelectState.None;
				index += 2;
			}

			var polygonCountModified = false;
			while (polygonCount > PolygonPointIndices.Length)
			{
				ArrayUtility.Add(ref PolygonPointIndices, null);
				polygonCountModified = true;
			}
			while (polygonCount < PolygonPointIndices.Length)
			{ 
				ArrayUtility.RemoveAt(ref PolygonPointIndices, PolygonPointIndices.Length - 1);
				polygonCountModified = true;
			}

			if (polygonCountModified)
			{
				for (var i = 0; i < polygonCount; i++)
				{
					PolygonPointIndices[i] = null;
				}
			}

			var brushTotalLength = 0.0f;
			BrushCenter = MathConstants.zeroVector3;
			for (var p = 0; p < polygonCount; p++)
			{
				var localCenterPoint = MathConstants.zeroVector3;
				var totalLength = 0.0f;
				var polygon		= controlMesh.Polygons[p];
				if (polygon == null)
					continue;
				
				var edgeIndices = polygon.EdgeIndices; 
				if (edgeIndices == null ||
					edgeIndices.Length == 0)
					continue;

				var halfEdgeIndex0 = edgeIndices[edgeIndices.Length - 1];
				if (halfEdgeIndex0 < 0 || halfEdgeIndex0 >= controlMesh.Edges.Length)
					continue;

				var vertexIndex0 = controlMesh.Edges[halfEdgeIndex0].VertexIndex;
				if (vertexIndex0 < 0 || vertexIndex0 >= vertices.Length)
					continue;

				var vertex0 = vertices[vertexIndex0];

				if (PolygonPointIndices[p] == null ||
					PolygonPointIndices[p].Length != edgeIndices.Length)
					PolygonPointIndices[p] = new int[edgeIndices.Length];

				var indices = PolygonPointIndices[p];
				for (var i = 0; i < edgeIndices.Length; i++)
				{
					var halfEdgeIndex1 = edgeIndices[i];
					if (halfEdgeIndex1 < 0 || 
						halfEdgeIndex1 >= controlMesh.Edges.Length)
						continue;

					var vertexIndex1 = controlMesh.Edges[halfEdgeIndex1].VertexIndex;
					if (vertexIndex1 < 0 || 
						vertexIndex1 >= vertices.Length)
						continue;

					var vertex1 = vertices[vertexIndex1];
					indices[i] = vertexIndex1;

					var length = (vertex1 - vertex0).sqrMagnitude;
					localCenterPoint += (vertex1 + vertex0) * 0.5f * length;
					totalLength		 += length;
					brushTotalLength += length;

					vertex0 = vertex1;
				}

				var worldCenterPoint = Mathf.Abs(totalLength) < MathConstants.EqualityEpsilon ? 
										localToWorldMatrix.MultiplyPoint(vertex0) : 
										localToWorldMatrix.MultiplyPoint(localCenterPoint / totalLength);
				BrushCenter += localCenterPoint;
				PolygonCenterPoints[p] = worldCenterPoint;
				PolygonCenterPlanes[p] = GeometryUtility.CalcPolygonPlane(controlMesh, (short)p);
			}
			if (Mathf.Abs(brushTotalLength) >= MathConstants.EqualityEpsilon)
				BrushCenter /= brushTotalLength;
			BrushCenter = localToWorldMatrix.MultiplyPoint(BrushCenter);
		}
		
		public static void GetHandleSizes(Camera cam, float[] sizes, Vector3[] positions)
		{
			if (!cam)
			{
				for (var p = 0; p < sizes.Length; p++)
				{
					sizes[p] = 20.0f;
				}
				return;
			}
			
			const float kHandleSize		= 80.0f / 20.0f;
			const float kHandleMaxSize	= 0.0001f / 20.0f;
			
			//position = Handles.matrix.MultiplyPoint(position);

			var tr				= cam.transform;
			var camPos			= tr.position;
			var camForward		= tr.forward;
			var camRight		= tr.right;
			var worldToClip		= cam.projectionMatrix * cam.worldToCameraMatrix;
			var width			= cam.pixelWidth  * 0.5f;
			var height			= cam.pixelHeight * 0.5f;

			var m00 = worldToClip.m00 * width;
			var m01 = worldToClip.m01 * width;
			var m02 = worldToClip.m02 * width;
			//var m03 = worldToClip.m03;

			var m10 = worldToClip.m10 * height;
			var m11 = worldToClip.m11 * height;
			var m12 = worldToClip.m12 * height;
			//var m13 = worldToClip.m13;

			var m30 = worldToClip.m30;
			var m31 = worldToClip.m31;
			var m32 = worldToClip.m32;
			var m33 = worldToClip.m33;

			//var wr = (m30 * camRight.x + m31 * camRight.y + m32 * camRight.z + m33);
			var offset = camPos + camRight;
				
			for (var p = 0; p < positions.Length; p++)
			{
				var distance = Vector3.Dot(positions[p] - camPos, camForward);
				var p0	= camForward * distance;
				var p2	= offset + p0;

				var w2	= (m30 * p2.x + m31 * p2.y + m32 * p2.z + m33);
				var iw2	= -1.0f / w2;
					
				var ax	= (camRight.x * iw2);
				var ay	= (camRight.y * iw2);
				var az	= (camRight.z * iw2);

				//var p1	= camPos + p0;
				//var w1	= (m30 * p1.x + m31 * p1.y + m32 * p1.z + m33);
				//var iw1	= 1.0f / w1;					
				//var t		= (iw1 + iw2);
				//ax += (p1.x * t);
				//ay += (p1.y * t);
				//az += (p1.z * t);

				var clipPointX = (m00 * ax) + (m01 * ay) + (m02 * az);// + (m03 * t);
				var clipPointY = (m10 * ax) + (m11 * ay) + (m12 * az);// + (m13 * t);

				var screenDist = Mathf.Sqrt((clipPointX * clipPointX) + (clipPointY * clipPointY));
									//new Vector3(clipPointX, clipPointY, distance - Vector3.Dot(p0 + camRight, camForward)).magnitude;
										
#if UNITY_5_4_OR_NEWER
				sizes[p] = (kHandleSize / Mathf.Max(screenDist, kHandleMaxSize)) * EditorGUIUtility.pixelsPerPoint;
#else
				sizes[p] = (kHandleSize / Mathf.Max(screenDist, kHandleMaxSize));
#endif
			}
		}

		public void UpdateHandles(ControlMesh controlMesh)
		{
			if (controlMesh == null ||
				controlMesh.Vertices == null ||
				controlMesh.Edges == null ||
				controlMesh.Polygons == null ||
				PolygonPointIndices == null)
			{
				return;
			}

			var sceneView		= SceneView.currentDrawingSceneView;
			var camera			= sceneView.camera;
			var cameraPosition	= camera.transform.position;
			var cameraOrtho		= camera.orthographic;
						
			GetHandleSizes(camera, PolygonCenterPointSizes, PolygonCenterPoints);			
			GetHandleSizes(camera, WorldPointSizes, WorldPoints);

			for (var p = 0; p < WorldPointBackfaced.Length; p++)
				WorldPointBackfaced[p] = true;

			for (int p = 0; p < PolygonCenterPoints.Length; p++)
			{
				var handleSize = PolygonCenterPointSizes[p];
				var delta1 = (PolygonCenterPoints[p] - BrushCenter).normalized;
				var delta2 = (PolygonCenterPoints[p] - cameraPosition).normalized;
				var dot = Vector3.Dot(delta1, delta2);
				if (cameraOrtho && Mathf.Abs(dot) > 1 - MathConstants.AngleEpsilon)
				{
					handleSize = 0;
				} else
				if (dot > 0)
				{
					handleSize *= ToolConstants.backHandleScale;
				} else
				{
					handleSize *= ToolConstants.handleScale;
					if (PolygonPointIndices != null && 
						p < PolygonPointIndices.Length)
					{
						var indices = PolygonPointIndices[p];
						if (indices != null)
						{
							for (var i = 0; i < indices.Length; i++)
							{
								if (indices[i] >= WorldPointBackfaced.Length)
								{
									PolygonPointIndices[p] = null;
									break;
								}
								WorldPointBackfaced[indices[i]] = false;
							}
						}
					}
				}

				PolygonCenterPointSizes[p] = handleSize;
			}

			for (var p = 0; p < WorldPointSizes.Length; p++)
			{				
				var handleSize = WorldPointSizes[p];
				if (WorldPointBackfaced[p])
					handleSize *= ToolConstants.backHandleScale;
				else
					handleSize *= ToolConstants.handleScale;
				
				WorldPointSizes[p] = handleSize;
			}
		}

		public void UpdateColors(CSGBrush brush, ControlMesh controlMesh)
		{
			if (controlMesh == null)
				return;
			var isValid = controlMesh.IsValid;

			var cameraPosition	= Camera.current.transform.position;
			var cameraOrtho		= Camera.current.orthographic;

			var polygonCount = PolygonCenterPoints.Length;
			for (int j = 0, p = 0; p < polygonCount; p++, j += 2)
			{
				var state = (int)PolygonSelectState[p];
				Color color1, color2;
				if (isValid)
				{
					color1 = ColorSettings.MeshEdgeOutline;
					color2 = ColorSettings.PolygonInnerStateColor[state];
				} else
				{
					color1 = ColorSettings.outerInvalidColor;
					color2 = ColorSettings.innerInvalidColor;
				}
				
				var delta1 = (PolygonCenterPoints[p] - BrushCenter).normalized;
				var delta2 = (PolygonCenterPoints[p] - cameraPosition).normalized;
				var dot = Vector3.Dot(delta1, delta2);
				if (!cameraOrtho || Mathf.Abs(dot) <= 1 - MathConstants.AngleEpsilon)
				{
					color1.a *= ToolConstants.backfaceTransparency;
					color2.a *= ToolConstants.backfaceTransparency;
				}
				
				PolygonCenterColors[j + 0] = color1;
				PolygonCenterColors[j + 1] = color2;
			}
			
			for (int j = 0, p = 0; p < PointSelectState.Length; p++, j += 2)
			{
				var state = (int)PointSelectState[p];
				Color color1, color2;
				if (isValid)
				{
					color1 = ColorSettings.MeshEdgeOutline;
					color2 = ColorSettings.PointInnerStateColor[state];
				} else
				{
					color1 = ColorSettings.MeshEdgeOutline;
					color2 = ColorSettings.InvalidInnerStateColor[state];
				}
				
				if (WorldPointBackfaced[p])
				{
					color1.a *= ToolConstants.backfaceTransparency;
					color2.a *= ToolConstants.backfaceTransparency;
				} else
				{
					color2.a = 1.0f;
				}

				WorldPointColors[j + 0] = color1;
				WorldPointColors[j + 1] = color2;
			}
			var edgeCount = Edges.Length;
			for (int j = 0, e = 0; j < edgeCount; e++, j += 2)
			{
				var state = (int)(EdgeSelectState[e]
//									  | surfaceSelectState[edgeSurfaces[j    ]] 
//									  | surfaceSelectState[edgeSurfaces[j + 1]]
								);
				if (isValid)
				{
					var color = ColorSettings.PointInnerStateColor[state];
					color.a = 1.0f;
					EdgeColors[e] = color;
				} else
				{
					EdgeColors[e] = ColorSettings.InvalidInnerStateColor[state];
				}
			}
		}

		
		public bool HavePointSelection
		{
			get
			{
				for (var p = 0; p < PointSelectState.Length; p++)
				{
					if ((PointSelectState[p] & SelectState.Selected) == SelectState.Selected)
						return true;
				}
				return false;
			}
		}

		public bool HaveEdgeSelection
		{
			get
			{
				for (var e = 0; e < EdgeSelectState.Length; e++)
				{
					if ((EdgeSelectState[e] & SelectState.Selected) == SelectState.Selected)
						return true;
				}
				return false;
			}
		}

		public bool DeSelectAll()
		{
			var hadSelection = false;
			for (var p = 0; p < PointSelectState.Length; p++)
			{
				hadSelection = hadSelection || (PointSelectState[p] & SelectState.Selected) == SelectState.Selected;
				PointSelectState[p] &= ~SelectState.Selected;
			}

			for (var e = 0; e < EdgeSelectState.Length; e++)
			{
				hadSelection = hadSelection || (EdgeSelectState[e] & SelectState.Selected) == SelectState.Selected;
				EdgeSelectState[e] &= ~SelectState.Selected;
			}

			for (var p = 0; p < PolygonSelectState.Length; p++)
			{
				hadSelection = hadSelection || (PolygonSelectState[p] & SelectState.Selected) == SelectState.Selected;
				PolygonSelectState[p] &= ~SelectState.Selected;
			}
			return hadSelection;
		}

		public void UnHoverAll()
		{
			for (var p = 0; p < PointSelectState.Length; p++)
				PointSelectState[p] &= ~SelectState.Hovering;

			for (var e = 0; e < EdgeSelectState.Length; e++)
				EdgeSelectState[e] &= ~SelectState.Hovering;

			for (var p = 0; p < PolygonSelectState.Length; p++)
				PolygonSelectState[p] &= ~SelectState.Hovering;
		}

		public bool IsPointSelected(int pointIndex)
		{
			var newState = PointSelectState[pointIndex];
			return ((newState & SelectState.Selected) == SelectState.Selected);
		}

		public bool IsEdgeSelected(int edgeIndex, bool selectIfPointsAreSelected)
		{
			var newState = EdgeSelectState[edgeIndex];
			if ((newState & SelectState.Selected) == SelectState.Selected)
				return true;
			if (!selectIfPointsAreSelected)
				return false;
			
			var pointIndex1	= Edges[(edgeIndex * 2) + 0];
			var pointIndex2	= Edges[(edgeIndex * 2) + 1];
			return IsPointSelected(pointIndex1) && IsPointSelected(pointIndex2);				 
		}

		public bool IsPolygonSelected(int polygonIndex)
		{
			var newState = PolygonSelectState[polygonIndex];
			return ((newState & SelectState.Selected) == SelectState.Selected);
		}

		public int PolygonCount
		{
			get
			{
				return PolygonSelectState.Length;
			}
		}

		private static bool Select(ref SelectState state, SelectionType selectionType, bool onlyOnHover = true)
		{
			var oldState = state;
			if (onlyOnHover && (oldState & SelectState.Hovering) != SelectState.Hovering)
				return false;

			var newState = oldState;
			switch (selectionType)
			{
				case SelectionType.Subtractive:		newState &= ~SelectState.Selected; break;
				case SelectionType.Toggle:			newState ^=  SelectState.Selected; break;
				case SelectionType.Replace:
				case SelectionType.Additive:		newState |=  SelectState.Selected; break;

				default:
					throw new ArgumentOutOfRangeException("selectionType", selectionType, null);
			}

			if (oldState == newState)
				return false;

			state = newState;
			return true;
		}

		public bool SelectPoint(int pointIndex, SelectionType selectionType, bool onlyOnHover = true)
		{
			if (pointIndex >= PointSelectState.Length)
				return false;
			if (!Select(ref PointSelectState[pointIndex], selectionType, onlyOnHover))
				return false;

			var expectedSelection = PointSelectState[pointIndex] & SelectState.Selected;
			for (var p = 0; p < PointSelectState.Length; p++)
			{
				if (p == pointIndex)
					continue;
				if ((PointSelectState[p] & SelectState.Hovering) != SelectState.Hovering)
					continue;

				if ((PointSelectState[p] & SelectState.Selected) == expectedSelection)
					continue;

				if (expectedSelection == SelectState.Selected)
					PointSelectState[p] |= SelectState.Selected;
				else
					PointSelectState[p] &= ~SelectState.Selected;
			}

			for (var e = 0; e < EdgeSelectState.Length; e++)
			{
				if ((EdgeSelectState[e] & SelectState.Selected) != SelectState.Selected)
				{
					var pointIndex1 = Edges[(e*2) + 0];
					var pointIndex2 = Edges[(e*2) + 1];
					if (IsPointSelected(pointIndex1) && IsPointSelected(pointIndex2))
					{
						EdgeSelectState[e] |= SelectState.Selected;
					}
				} else
				{
					var pointIndex1 = Edges[(e*2) + 0];
					var pointIndex2 = Edges[(e*2) + 1];
					if (!IsPointSelected(pointIndex1) || !IsPointSelected(pointIndex2))
					{
						EdgeSelectState[e] &= ~SelectState.Selected;
					}
				}
			}

			return true;
		}

		public bool SelectEdge(int edgeIndex, SelectionType selectionType, bool onlyOnHover = true)
		{
			return Select(ref EdgeSelectState[edgeIndex], selectionType, onlyOnHover);
		}

		public bool SelectPolygon(int polygonIndex, SelectionType selectionType, bool onlyOnHover = true)
		{
			return Select(ref PolygonSelectState[polygonIndex], selectionType, onlyOnHover);
		}

		public static bool DeselectAll(ControlMeshState[] controlMeshStates)
		{
			var hadSelection = false;
			for (var t = 0; t < controlMeshStates.Length; t++)
			{
				hadSelection = controlMeshStates[t].DeSelectAll() || hadSelection;
			}
			return hadSelection;
		}

		public static void SelectPoints(ControlMeshState[] controlMeshStates, PointSelection[] selectedPoints, SelectionType selectionType, bool onlyOnHover = true)
		{
			if (selectionType == SelectionType.Replace)
			{
				for (var t = 0; t < controlMeshStates.Length; t++)
				{
					controlMeshStates[t].DeSelectAll();
				}
			}

			switch (selectionType)
			{
				case SelectionType.Additive:
				case SelectionType.Replace:
				{
					for (var i = 0; i < selectedPoints.Length; i++)
					{
						var brushIndex = selectedPoints[i].BrushIndex;
						var pointIndex = selectedPoints[i].PointIndex;
						controlMeshStates[brushIndex].SelectPoint(pointIndex, SelectionType.Replace, onlyOnHover);
					}
					break;
				}
				case SelectionType.Subtractive:
				{
					for (var i = 0; i < selectedPoints.Length; i++)
					{
						var brushIndex = selectedPoints[i].BrushIndex;
						var pointIndex = selectedPoints[i].PointIndex;
						controlMeshStates[brushIndex].SelectPoint(pointIndex, SelectionType.Subtractive, onlyOnHover);
					}
					break;
				}
				case SelectionType.Toggle:
				{
					for (var i = 0; i < selectedPoints.Length; i++)
					{
						var brushIndex = selectedPoints[i].BrushIndex;
						var pointIndex = selectedPoints[i].PointIndex;
						controlMeshStates[brushIndex].SelectPoint(pointIndex, SelectionType.Toggle, onlyOnHover);
					}
					break;
				}
			}
		}
	}
}
