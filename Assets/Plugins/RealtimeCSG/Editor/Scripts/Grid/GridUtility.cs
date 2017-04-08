using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[Serializable]
	sealed class SpaceMatrices
	{
		public Matrix4x4 activeLocalToWorld			= MathConstants.identityMatrix;
		public Matrix4x4 activeWorldToLocal			= MathConstants.identityMatrix;
		public Matrix4x4 modelLocalToWorld			= MathConstants.identityMatrix;
		public Matrix4x4 modelWorldToLocal			= MathConstants.identityMatrix;
		

		public static SpaceMatrices Create(Transform transform)
		{
			SpaceMatrices spaceMatrices = new SpaceMatrices();
			if (transform == null || 
				Tools.pivotRotation == PivotRotation.Global)
				return spaceMatrices;
			
			spaceMatrices.activeLocalToWorld		= transform.localToWorldMatrix;
			spaceMatrices.activeWorldToLocal		= transform.worldToLocalMatrix;
			
			var model = InternalCSGModelManager.FindModelTransform(transform);
			if (model != null)
			{
				spaceMatrices.modelLocalToWorld		= model.localToWorldMatrix;
				spaceMatrices.modelWorldToLocal		= model.worldToLocalMatrix;
			}

			return spaceMatrices;
		}
	};



	internal static class GridUtility
	{
		const float EdgeFudgeFactor   = 0.8f;
		const float VertexFudgeFactor = EdgeFudgeFactor * 0.8f;

		static public Vector3 CleanNormal(Vector3 normal)
		{
			if (normal.x >= -MathConstants.EqualityEpsilon && normal.x < MathConstants.EqualityEpsilon) normal.x = 0;
			if (normal.y >= -MathConstants.EqualityEpsilon && normal.y < MathConstants.EqualityEpsilon) normal.y = 0;
			if (normal.z >= -MathConstants.EqualityEpsilon && normal.z < MathConstants.EqualityEpsilon) normal.z = 0;
			
			if (normal.x >= 1-MathConstants.EqualityEpsilon) normal.x = 1;
			if (normal.y >= 1-MathConstants.EqualityEpsilon) normal.y = 1;
			if (normal.z >= 1-MathConstants.EqualityEpsilon) normal.z = 1;
			
			if (normal.x <= -1+MathConstants.EqualityEpsilon) normal.x = -1;
			if (normal.y <= -1+MathConstants.EqualityEpsilon) normal.y = -1;
			if (normal.z <= -1+MathConstants.EqualityEpsilon) normal.z = -1;

			return normal.normalized;
		}

		static public Vector3 CleanPosition(Vector3 position)
		{
			int		intPosX		= Mathf.FloorToInt(position.x);
			int		intPosY		= Mathf.FloorToInt(position.y);
			int		intPosZ		= Mathf.FloorToInt(position.z);
			
			float	fractPosX	= (position.x - intPosX);
			float	fractPosY	= (position.y - intPosY);
			float	fractPosZ	= (position.z - intPosZ);
			
			fractPosX = Mathf.Round(fractPosX * 1000.0f) / 1000.0f;
			fractPosY = Mathf.Round(fractPosY * 1000.0f) / 1000.0f;
			fractPosZ = Mathf.Round(fractPosZ * 1000.0f) / 1000.0f;

			const float epsilon = MathConstants.EqualityEpsilon;

			if (fractPosX >= -epsilon && fractPosX < epsilon) fractPosX = 0;
			if (fractPosY >= -epsilon && fractPosY < epsilon) fractPosY = 0;
			if (fractPosZ >= -epsilon && fractPosZ < epsilon) fractPosZ = 0;
			
			if (fractPosX >= 1-epsilon) fractPosX = 1;
			if (fractPosY >= 1-epsilon) fractPosY = 1;
			if (fractPosZ >= 1-epsilon) fractPosZ = 1;
			
			if (fractPosX <= -1+epsilon) fractPosX = -1;
			if (fractPosY <= -1+epsilon) fractPosY = -1;
			if (fractPosZ <= -1+epsilon) fractPosZ = -1;

			if (!float.IsNaN(fractPosX) && !float.IsInfinity(fractPosX)) position.x = intPosX + fractPosX;
			if (!float.IsNaN(fractPosY) && !float.IsInfinity(fractPosY)) position.y = intPosY + fractPosY;
			if (!float.IsNaN(fractPosZ) && !float.IsInfinity(fractPosZ)) position.z = intPosZ + fractPosZ;

			return position;
		}

		static public Vector3 FixPosition(Vector3 currentPosition, Matrix4x4 worldToLocalMatrix, Matrix4x4 localToWorldMatrix, Vector3 previousPosition, bool toggleSnapToGrid = false, bool ignoreAxisLocking = false)
		{
			if (currentPosition == previousPosition)
				return currentPosition;

			var pivotRotation = UnityEditor.Tools.pivotRotation;
			if (pivotRotation == UnityEditor.PivotRotation.Local)
			{
				previousPosition	= worldToLocalMatrix.MultiplyPoint(previousPosition);
				currentPosition		= worldToLocalMatrix.MultiplyPoint(currentPosition);

				if (!ignoreAxisLocking)
				{ 
					if (RealtimeCSG.CSGSettings.LockAxisX) currentPosition.x = previousPosition.x;
					if (RealtimeCSG.CSGSettings.LockAxisY) currentPosition.y = previousPosition.y;
					if (RealtimeCSG.CSGSettings.LockAxisZ) currentPosition.z = previousPosition.z;
				}

				if (RealtimeCSG.CSGSettings.SnapToGrid ^ toggleSnapToGrid)
				{
					if (Mathf.Abs(currentPosition.x - previousPosition.x) < MathConstants.EqualityEpsilon) currentPosition.x = previousPosition.x;
					if (Mathf.Abs(currentPosition.y - previousPosition.y) < MathConstants.EqualityEpsilon) currentPosition.y = previousPosition.y;
					if (Mathf.Abs(currentPosition.z - previousPosition.z) < MathConstants.EqualityEpsilon) currentPosition.z = previousPosition.z;

					if (currentPosition.x != previousPosition.x) currentPosition.x = Mathf.Round(currentPosition.x / RealtimeCSG.CSGSettings.SnapVector.x) * RealtimeCSG.CSGSettings.SnapVector.x;
					if (currentPosition.y != previousPosition.y) currentPosition.y = Mathf.Round(currentPosition.y / RealtimeCSG.CSGSettings.SnapVector.y) * RealtimeCSG.CSGSettings.SnapVector.y;
					if (currentPosition.z != previousPosition.z) currentPosition.z = Mathf.Round(currentPosition.z / RealtimeCSG.CSGSettings.SnapVector.z) * RealtimeCSG.CSGSettings.SnapVector.z;
				}

				currentPosition	= localToWorldMatrix.MultiplyPoint(currentPosition); 
			} else
			{
				if (!ignoreAxisLocking)
				{
					if (RealtimeCSG.CSGSettings.LockAxisX) currentPosition.x = previousPosition.x;
					if (RealtimeCSG.CSGSettings.LockAxisY) currentPosition.y = previousPosition.y;
					if (RealtimeCSG.CSGSettings.LockAxisZ) currentPosition.z = previousPosition.z;
				}

				if (RealtimeCSG.CSGSettings.SnapToGrid ^ toggleSnapToGrid)
				{
					if (currentPosition.x != previousPosition.x) currentPosition.x = Mathf.Round(currentPosition.x / RealtimeCSG.CSGSettings.SnapVector.x) * RealtimeCSG.CSGSettings.SnapVector.x;
					if (currentPosition.y != previousPosition.y) currentPosition.y = Mathf.Round(currentPosition.y / RealtimeCSG.CSGSettings.SnapVector.y) * RealtimeCSG.CSGSettings.SnapVector.y;
					if (currentPosition.z != previousPosition.z) currentPosition.z = Mathf.Round(currentPosition.z / RealtimeCSG.CSGSettings.SnapVector.z) * RealtimeCSG.CSGSettings.SnapVector.z;
				}
			}

			currentPosition = GridUtility.CleanPosition(currentPosition);
			return currentPosition;
		}

		static public Vector3 FixPosition(Vector3 currentPosition, Transform spaceTransform, Vector3 previousPosition, bool toggleSnapToGrid = false, bool ignoreAxisLocking = false)
		{
			if (spaceTransform == null)
				return FixPosition(currentPosition, MathConstants.identityMatrix, MathConstants.identityMatrix, previousPosition, toggleSnapToGrid, ignoreAxisLocking);
			return FixPosition(currentPosition, spaceTransform.worldToLocalMatrix, spaceTransform.localToWorldMatrix, previousPosition, toggleSnapToGrid, ignoreAxisLocking);
		}

		static public float SnappedAngle(float currentAngle, bool toggleSnapToGrid = false)
		{
			if (RealtimeCSG.CSGSettings.SnapToGrid ^ toggleSnapToGrid)
				currentAngle = Mathf.RoundToInt(currentAngle / RealtimeCSG.CSGSettings.SnapRotation) * RealtimeCSG.CSGSettings.SnapRotation;
			return currentAngle;
		}

		public static void HalfGridSize()
		{
			RealtimeCSG.CSGSettings.SnapVector = RealtimeCSG.CSGSettings.SnapVector * 0.5f;
			RealtimeCSG.CSGSettings.Save();
		}
		
		public static void DoubleGridSize()
		{
			RealtimeCSG.CSGSettings.SnapVector = RealtimeCSG.CSGSettings.SnapVector * 2.0f;
			RealtimeCSG.CSGSettings.Save();
		}

		public static void ToggleShowGrid()
		{
			RealtimeCSG.CSGSettings.GridVisible = !RealtimeCSG.CSGSettings.GridVisible;
			EditorPrefs.SetBool("ShowGrid", RealtimeCSG.CSGSettings.GridVisible);
		}

		public static void ToggleSnapToGrid()
		{
			RealtimeCSG.CSGSettings.SnapToGrid = !RealtimeCSG.CSGSettings.SnapToGrid;
			EditorPrefs.SetBool("ForceSnapToGrid", RealtimeCSG.CSGSettings.SnapToGrid);
		}
		
		
		private static List<Vector3> FindAllEdgesThatTouchPoint(CSGBrush brush, Vector3 point)
		{
			var lines = new List<Vector3>();
			if (!brush)
				return lines;

			var outline = BrushOutlineManager.GetBrushOutline(brush.brushID);
			if (outline == null)
				return lines;
			
			var controlMesh = brush.ControlMesh;
			if (controlMesh == null)
				return lines;
			
			var localToWorld	= brush.transform.localToWorldMatrix;

			var edges	= controlMesh.Edges;
			var points	= controlMesh.Vertices;
			for (int e = 0; e < edges.Length; e ++)
			{
				var vertexIndex1		= edges[e].VertexIndex;
				var vertex1				= localToWorld.MultiplyPoint(points[vertexIndex1]);

				var distance = (point - vertex1).sqrMagnitude;
				if (distance < MathConstants.EqualityEpsilonSqr)
				{
					var twinIndex		= edges[e].TwinIndex;
					var vertexIndex2	= edges[twinIndex].VertexIndex;
					var vertex2			= localToWorld.MultiplyPoint(points[vertexIndex2]);
					lines.Add(vertex1);
					lines.Add(vertex2);
				}
			}
			
			var translation	= brush.transform.position;
			var indices		= outline.visibleInnerLines;
			var vertices	= outline.vertices;
			if (indices != null && vertices != null)
			{
				for (int i = 0; i < indices.Length; i += 2)
				{
					var index1	= indices[i + 0];
					var index2	= indices[i + 1];
					var vertex1 = vertices[index1] + translation;
					var vertex2 = vertices[index2] + translation;

					var distance1 = (point - vertex1).sqrMagnitude;
					var distance2 = (point - vertex2).sqrMagnitude;
					if (distance1 < MathConstants.EqualityEpsilonSqr ||
						distance2 < MathConstants.EqualityEpsilonSqr)
					{
						lines.Add(vertex1);
						lines.Add(vertex2);
					}
				}
			}

			if ((RealtimeCSG.CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowInvisibleSurfaces) == HelperSurfaceFlags.ShowInvisibleSurfaces)
			{
				indices = outline.invisibleInnerLines;
				vertices = outline.vertices;
				if (indices != null && vertices != null)
				{
					for (int i = 0; i < indices.Length; i += 2)
					{
						var index1	= indices[i + 0];
						var index2	= indices[i + 1];
						var vertex1 = vertices[index1] + translation;
						var vertex2 = vertices[index2] + translation;

						var distance1 = (point - vertex1).sqrMagnitude;
						var distance2 = (point - vertex2).sqrMagnitude;
						if (distance1 < MathConstants.EqualityEpsilonSqr ||
							distance2 < MathConstants.EqualityEpsilonSqr)
						{
							lines.Add(vertex1);
							lines.Add(vertex2);
						}
					}
				}
			}

			return lines;
		}
		

		public static Vector3 SnapToWorld(CSGPlane snapPlane, Vector3 unsnappedPosition, Vector3 snappedPosition, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush)
		{
			snappedOnBrush = null;

			var intersections	= new BrushIntersection[0];
			var test_points		= new Vector3[] { unsnappedPosition, snappedPosition };
			BrushIntersection intersection;
			for (int i = 0; i < test_points.Length; i++)
			{
				var test_point2D = HandleUtility.WorldToGUIPoint(test_points[i]);
				if (SceneQueryUtility.FindWorldIntersection(test_point2D, out intersection, MathConstants.GrowBrushFactor))
				{
					if (intersection.brush &&
						intersection.brush.ControlMesh != null)
					{
						intersection.worldIntersection = GeometryUtility.ProjectPointOnPlane(snapPlane, intersection.worldIntersection);
						ArrayUtility.Add(ref intersections, intersection);
					}
				}
			}
				
			var old_difference				= snappedPosition - unsnappedPosition;
			var	old_difference_magnitude	= old_difference.magnitude * 1.5f;
			Vector3 newSnappedPoint			= snappedPosition;

			CSGPlane? snappingPlane = snapPlane;
			for (int i = 0; i < intersections.Length; i++) 
			{
				List<Vector3>	outEdgePoints;
				Vector3			outPosition;
				if (GridUtility.SnapToVertices(intersections[i].brush, 
												snappingPlane, unsnappedPosition, 
												out outEdgePoints, 
												out outPosition))
				{
					var new_difference				= outPosition - unsnappedPosition;
					var new_difference_magnitude	= new_difference.magnitude;
					
					if (new_difference_magnitude <= old_difference_magnitude + MathConstants.EqualityEpsilon)
					{
						old_difference_magnitude	= new_difference_magnitude;
						newSnappedPoint				= outPosition;
						snappingEdges				= outEdgePoints;
						snappedOnBrush				= intersections[i].brush;
					}
				}
				if (GridUtility.SnapToEdge(intersections[i].brush, snappingPlane ?? intersections[i].plane, 
											intersections[i].worldIntersection, 
											out outEdgePoints, 
											out outPosition))
				{
					var new_difference				= outPosition - unsnappedPosition;
					var new_difference_magnitude	= new_difference.magnitude * 1.1f;
					
					if (new_difference_magnitude <= old_difference_magnitude + MathConstants.EqualityEpsilon)
					{
						old_difference_magnitude	= new_difference_magnitude;
						newSnappedPoint				= outPosition;
						snappingEdges				= outEdgePoints;
						snappedOnBrush				= intersections[i].brush;
					}
				}
			}
						
			//snappingEdges = FindAllEdgesThatTouchPoint(snappedOnBrush, newSnappedPoint);
			return newSnappedPoint;
		}

		sealed class SnapData
		{
			public CSGPlane?			snapPlane;
			public Vector3			guiPoint;
			public Vector3			worldPoint;
			public float			closestDistance;
			public float            closestDistanceSqr;
			public List<Vector3>	outEdge;
			public Vector3			snappedWorldPoint;
		}

		static void SnapToLines(int[] indices, Vector3[] localVertices, Matrix4x4 localToWorld, Matrix4x4 worldToLocal, ref SnapData snapData)
		{
			if (indices == null || localVertices == null)
				return;
			
			for (int i = 0; i < indices.Length; i+=2)
			{						
				var index1			= indices[i + 0];
				var index2			= indices[i + 1];
				var worldVertex1	= localToWorld.MultiplyPoint(localVertices[index1]);
				var worldVertex2	= localToWorld.MultiplyPoint(localVertices[index2]);
				
				var worldVertex3 = MathConstants.zeroVector3;
				if (!Grid.SnapToLine(snapData.worldPoint, worldVertex1, worldVertex2, snapData.snapPlane, ref worldVertex3))
					continue;

				if (!snapData.snapPlane.HasValue || 
					Mathf.Abs(snapData.snapPlane.Value.Distance(worldVertex3)) < MathConstants.DistanceEpsilon)
				{ 
					var guiVertex3	= (Vector3)HandleUtility.WorldToGUIPoint(worldVertex3);
					var guiDistance	= (guiVertex3 - snapData.guiPoint).sqrMagnitude * EdgeFudgeFactor;
					if (guiDistance + MathConstants.DistanceEpsilon < snapData.closestDistanceSqr)
					{
						snapData.closestDistanceSqr	= guiDistance;
						snapData.outEdge			= new List<Vector3>() { worldVertex1, worldVertex2 };
						snapData.snappedWorldPoint	= worldVertex3;
					}
				}
			}
		}

		public static bool SnapToEdge(CSGBrush brush, CSGPlane? _snapPlane, Vector3 _worldPoint, out List<Vector3> outEdgePoints, 
										out Vector3 outPosition)//, float _closestDistance = float.PositiveInfinity)
		{
			outPosition = MathConstants.zeroVector3;
			outEdgePoints = null;

			if (!brush)
				return false;

			var controlMesh = brush.ControlMesh;
			if (controlMesh == null || Camera.current == null)
				return false;

			var snapData = new SnapData();

			// Find an edge to snap against the point we're interested in
			snapData.worldPoint			= _worldPoint;
			snapData.guiPoint			= (Vector3)HandleUtility.WorldToGUIPoint(_worldPoint);
			snapData.closestDistance	= float.PositiveInfinity;
			snapData.closestDistanceSqr	= float.PositiveInfinity;
			snapData.snapPlane			= _snapPlane;
			snapData.outEdge			= null;
			snapData.snappedWorldPoint	= new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			
			var points					= controlMesh.Vertices;
			var edges					= controlMesh.Edges;
			var polygons				= controlMesh.Polygons;
			var localToWorld			= brush.transform.localToWorldMatrix;
			var worldToLocal			= brush.transform.worldToLocalMatrix;
			
			for (int p = 0; p < polygons.Length; p++)
			{
				var edgeIndices = polygons[p].EdgeIndices;
				var indices = new List<int>(edgeIndices.Length * 2);
				for (int e = 0; e < edgeIndices.Length; e++)
				{
					var edgeIndex = edgeIndices[e];
					if (!edges[edgeIndex].HardEdge)
						continue;

					var twinIndex = edges[edgeIndex].TwinIndex;

					var vertexIndex1 = edges[edgeIndex].VertexIndex;
					var vertexIndex2 = edges[twinIndex].VertexIndex;

					indices.Add(vertexIndex1);
					indices.Add(vertexIndex2);
				}

				SnapToLines(indices.ToArray(), points, localToWorld, worldToLocal, ref snapData);
			}

			var outline = BrushOutlineManager.GetBrushOutline(brush.brushID);
			if (outline != null)
			{
				var translation		= Matrix4x4.TRS( brush.transform.position, MathConstants.identityQuaternion, MathConstants.oneVector3);
				var invTranslation	= Matrix4x4.TRS(-brush.transform.position, MathConstants.identityQuaternion, MathConstants.oneVector3);
				var vertices	= outline.vertices;

				var indices		= outline.visibleInnerLines;
				SnapToLines(indices, vertices, translation, invTranslation, ref snapData);
					
				if ((RealtimeCSG.CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowInvisibleSurfaces) == HelperSurfaceFlags.ShowInvisibleSurfaces)
				{ 
					indices		= outline.invisibleInnerLines;
					SnapToLines(indices, vertices, translation, invTranslation, ref snapData);
				}
			}
			
			if (snapData.outEdge == null || 
				float.IsInfinity(snapData.closestDistanceSqr))
				return false;

			snapData.closestDistance = Mathf.Sqrt(snapData.closestDistanceSqr);
			outEdgePoints	= snapData.outEdge;
			outPosition		= snapData.snappedWorldPoint;
			return true;
		}
		
		public static bool SnapToVertices(CSGBrush brush, CSGPlane? snapPlane, Vector3 worldPosition, out List<Vector3> outEdgePoints, 
											out Vector3 outPosition, float closestDistance = float.PositiveInfinity)
		{
			outPosition = MathConstants.zeroVector3;
			outEdgePoints = null;

			if (!brush)
				return false;

			var controlMesh = brush.ControlMesh;
			if (controlMesh == null)
				return false;
			
			Vector3?	outPoint		= null;

			// Find an edge to snap against the point we're interested in
			var guiPoint			= (Vector3)HandleUtility.WorldToGUIPoint(worldPosition);
			var closestDistanceSqr	= closestDistance * closestDistance;
			/*
			var points				= controlMesh.vertices;
			var edges				= controlMesh.edges;
			var polygons			= controlMesh.polygons;
			var localToWorld		= brush.transform.localToWorldMatrix;
			for(int p = 0; p < polygons.Length; p++)
			{
				var edgeIndices				= polygons[p].edgeIndices;
				for (int e = 0; e < edgeIndices.Length; e++)
				{
					var edgeIndex			= edgeIndices[e];
					if (!edges[edgeIndex].hardEdge)
						continue;

					var twinIndex			= edges[edgeIndex].twinIndex;

					var vertexIndex1		= edges[edgeIndex].vertexIndex;
					var vertexIndex2		= edges[twinIndex].vertexIndex;
						
					var vertex1				= localToWorld.MultiplyPoint(points[vertexIndex1]);
					var vertex2				= localToWorld.MultiplyPoint(points[vertexIndex2]);
					
					if (!snapPlane.HasValue || 
						Mathf.Abs(snapPlane.Value.Distance(vertex1)) < Constants.DistanceEpsilon)
					{ 
						var guiVertex1		= (Vector3)HandleUtility.WorldToGUIPoint(vertex1);										 					
						var guiDistance		= (guiVertex1 - guiPoint).magnitude * VertexFudgeFactor;
						if (guiDistance + Constants.DistanceEpsilon < closestDistance)
						{
							closestDistance = guiDistance;
							outPoint		= vertex1;
						}
					}
					
					if (!snapPlane.HasValue || 
						Mathf.Abs(snapPlane.Value.Distance(vertex2)) < Constants.DistanceEpsilon)
					{ 
						var guiVertex2		= (Vector3)HandleUtility.WorldToGUIPoint(vertex2);
						var guiDistance		= (guiVertex2 - guiPoint).magnitude * VertexFudgeFactor;
						if (guiDistance + Constants.DistanceEpsilon < closestDistance)
						{
							closestDistance = guiDistance;
							outPoint		= vertex2;
						}
					}
				}
			}*/
			
			var outline = BrushOutlineManager.GetBrushOutline(brush.brushID);
			if (outline != null)
			{
				var translation	= brush.transform.position;
				var indices		= outline.visibleInnerLines;
				var vertices	= outline.vertices;
				if (indices != null && vertices != null)
				{ 
					for (int i = 0; i < indices.Length; i+=2)
					{						
						var index1		= indices[i + 0];
						var index2		= indices[i + 1];
						var vertex1		= vertices[index1] + translation;
						var vertex2		= vertices[index2] + translation;
										 
						if (!snapPlane.HasValue || 
							Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DistanceEpsilon)
						{ 
							var guiVertex1		= HandleUtility.WorldToGUIPoint(vertex1);
							var guiDistance		= ((Vector3)guiVertex1 - guiPoint).sqrMagnitude * VertexFudgeFactor;
							if (guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
							{
								closestDistanceSqr	= guiDistance;
								outPoint			= vertex1;
								continue;
							} 
						}
						
						if (!snapPlane.HasValue || 
							Mathf.Abs(snapPlane.Value.Distance(vertex2)) < MathConstants.DistanceEpsilon)
						{ 
							var guiVertex2		= HandleUtility.WorldToGUIPoint(vertex2);
							var guiDistance		= ((Vector3)guiVertex2 - guiPoint).sqrMagnitude * VertexFudgeFactor;
							if (guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
							{
								closestDistanceSqr	= guiDistance;
								outPoint			= vertex2;
								continue;
							} 
						}
					}
				}
					
				if ((RealtimeCSG.CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowInvisibleSurfaces) == HelperSurfaceFlags.ShowInvisibleSurfaces)
				{ 
					indices		= outline.invisibleInnerLines;
					vertices	= outline.vertices;
					if (indices != null && vertices != null)
					{
						for (int i = 0; i < indices.Length; i+=2)
						{						
							var index1		= indices[i + 0];
							var index2		= indices[i + 1];
							var vertex1		= vertices[index1] + translation;
							var vertex2		= vertices[index2] + translation;					
						 
							if (!snapPlane.HasValue || 
								Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DistanceEpsilon)
							{ 
								var guiVertex1		= HandleUtility.WorldToGUIPoint(vertex1);
								var guiDistance		= ((Vector3)guiVertex1 - guiPoint).sqrMagnitude * VertexFudgeFactor;
								if (guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
								{
									closestDistanceSqr	= guiDistance;
									outPoint			= vertex1;
								} 		
							}							
									 
							if (!snapPlane.HasValue || 
								Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DistanceEpsilon)
							{ 			
								var guiVertex2		= HandleUtility.WorldToGUIPoint(vertex2);
								var guiDistance		= ((Vector3)guiVertex2 - guiPoint).magnitude * VertexFudgeFactor;
								if (guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
								{
									closestDistanceSqr	= guiDistance;
									outPoint			= vertex2;
								} 
							}
						}
					}
				}
			}
			
			if (!outPoint.HasValue || float.IsInfinity(closestDistance))
				return false;

			closestDistance = Mathf.Sqrt(closestDistanceSqr);			
			outPosition		= outPoint.Value;
			outEdgePoints	= FindAllEdgesThatTouchPoint(brush, outPosition);
			return true;
		}
	}
}
