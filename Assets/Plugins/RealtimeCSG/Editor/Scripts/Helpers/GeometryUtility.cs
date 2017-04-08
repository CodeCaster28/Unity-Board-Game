using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal enum PrincipleAxis
	{
		X,Y,Z
	}

	internal static class GeometryUtility
	{
		public static float SignedAngle(Vector2 v1, Vector2 v2)
		{
			//  Acute angle [0,180]
			var angle = Vector2.Angle(v1, v2);

			//  -Acute angle [180,-179]
			var sign = Mathf.Sign(Vector3.Dot(MathConstants.forwardVector3, Vector3.Cross(v1, v2)));
			var signedAngle = angle * sign;

			//  360 angle
			return signedAngle;
		}

		public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 n)
		{
			//  Acute angle [0,180]
			var angle = Vector3.Angle(v1, v2);

			//  -Acute angle [180,-179]
			var sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(v1, v2)));
			var signedAngle = angle * sign;

			//  360 angle
			return signedAngle;
		}

		public static Vector3 ProjectPointOnPlane(CSGPlane plane, Vector3 point)
		{ 
			 var distance = -Vector3.Dot(plane.normal, (point - plane.pointOnPlane));
			 return point + plane.normal * distance;
		}
		

		public static float CounterClockwise(Vector2 a, Vector2 b, Vector2 c)
		{
			return ((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
		}

		public static bool Intersects(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)
		{
		   if (CounterClockwise(A1, A2, B1) * CounterClockwise(A1, A2, B2) > 0) return false;
		   if (CounterClockwise(B1, B2, A1) * CounterClockwise(B1, B2, A2) > 0) return false;
		   return true;
		}

		public static bool TryIntersection(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out Vector2 intersection)
		{
			var deltaYa = A2.y - A1.y;
			var deltaXa = A1.x - A2.x;
			var dotA	= deltaYa * A1.x + deltaXa * A1.y;
			
			var deltaYb = B2.y - B1.y;
			var deltaXb = B1.x - B2.x;
			var dotB	= deltaYb * B1.x + deltaXb * B1.y;
			
			float magnitude = deltaYa * deltaXb - deltaYb * deltaXa;
			if (magnitude <= MathConstants.EqualityEpsilon)
			{
				intersection = MathConstants.zeroVector2;
				return false;
			}

			// now return the Vector2 intersection point
			intersection = new Vector2( (deltaXb * dotA - deltaXa * dotB) / magnitude, (deltaYa * dotB - deltaYb * dotA) / magnitude);
			return true;
		}

		private struct Line { public int Index0, Index1; }


		public static bool SelfIntersecting(List<Vector2> points)
		{
			var lines = new Line[points.Count];
			for (int i = points.Count - 1, j = 0; j < points.Count; i = j, j++)
			{
				lines[j].Index0 = i;
				lines[j].Index1 = j;
			}

			for (var i = 0; i < lines.Length - 2; i++)
			{
				for (var j = i + 1; j < lines.Length - 1; j++)
				{
					if (lines[i].Index1 == lines[j].Index0 ||
						lines[i].Index0 == lines[j].Index1)
						continue;

					if (Intersects(points[lines[i].Index0], points[lines[i].Index1], 
								   points[lines[j].Index0], points[lines[j].Index1]))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool SelfIntersecting(Vector2[] points)
		{
			var lines = new Line[points.Length];
			for (int i = points.Length - 1, j = 0; j < points.Length; i = j, j++)
			{
				lines[j].Index0 = i;
				lines[j].Index1 = j;
			}

			for (var i = 0; i < lines.Length - 2; i++)
			{
				for (var j = i + 1; j < lines.Length - 1; j++)
				{
					if (lines[i].Index1 == lines[j].Index0 ||
						lines[i].Index0 == lines[j].Index1)
						continue;

					if (Intersects(points[lines[i].Index0], points[lines[i].Index1], 
								   points[lines[j].Index0], points[lines[j].Index1]))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static Vector2[] RotatePlaneTo2D(Matrix4x4 extraMatrix, Vector3[] points, CSGPlane plane)
		{
			var mat = RotatePlaneTo2DMatrix(plane) * extraMatrix;

			var points2D = new Vector2[points.Length];
			for (var i = 0; i < points2D.Length; i++)
			{
				var point = mat.MultiplyPoint(points[i]);
				points2D[i].x = point.x;
				points2D[i].y = point.z;
			}
			return points2D;
		}

		public static Vector2[] RotatePlaneTo2D(Vector3[] points, CSGPlane plane)
		{
			var mat = RotatePlaneTo2DMatrix(plane);
			
			var points2D = new Vector2[points.Length];
			for (var i = 0; i < points2D.Length; i++)
			{
				var point = mat.MultiplyPoint(points[i]);
				points2D[i].x = point.x;
				points2D[i].y = point.z;
			}
			return points2D;
		}

		public static Vector3 RotatePointIntoPlaneSpace(CSGPlane plane, Vector3 point)
		{
			var mat = RotatePlaneTo2DMatrix(plane);
			return mat.MultiplyPoint(point);
		}

		public static Matrix4x4 RotatePlaneTo2DMatrix(CSGPlane plane)
		{
			return Matrix4x4.TRS(MathConstants.zeroVector3, Quaternion.FromToRotation(plane.normal, MathConstants.upVector3), MathConstants.oneVector3);
		}
		public static Matrix4x4 RotateSurfaceTo2DMatrix(Surface surface)
		{
			var from = Quaternion.LookRotation(surface.BiNormal, -surface.Plane.normal);
			return Matrix4x4.TRS(MathConstants.zeroVector3, Quaternion.Inverse(from), MathConstants.oneVector3);
		}
		
		public static Matrix4x4 Rotate2DToPlaneMatrix(CSGPlane plane)
		{
			return RotatePlaneTo2DMatrix(plane).inverse;
			//return   Matrix4x4.TRS(plane.pointOnPlane, MathConstants.identityQuaternion, MathConstants.oneVector3)
			//	   * Matrix4x4.TRS(MathConstants.zeroVector3, Quaternion.FromToRotation(MathConstants.upVector3, plane.normal), MathConstants.oneVector3);
		}
		
		public static void RotateTransform2DToPlane(CSGPlane plane, Vector3 origin, Transform transform)
		{
			// rotate upward shape into direction of plane
			transform.rotation = Quaternion.FromToRotation(MathConstants.upVector3, plane.normal);
			transform.position = origin;
		}

		public static Vector3[] Rotate2DToPlane(Vector2[] points2D, CSGPlane plane)
		{
			var mat = Rotate2DToPlaneMatrix(plane);
			var points = new Vector3[points2D.Length];
			for (var i = 0; i < points2D.Length; i++)
			{
				var input = new Vector3(points2D[i].x, 0, points2D[i].y);
				var point = mat.MultiplyPoint(input);
				points[i] = ProjectPointOnPlane(plane, point);
			}
			return points;
		}
		
		public static Vector3[] ToVector3XZ(List<Vector2> points2D)
		{
			var points = new Vector3[points2D.Count];
			for (var i = 0; i < points2D.Count; i++)
			{
				points[i].x = points2D[i].x;
				points[i].z = points2D[i].y;
			}
			return points;
		}
		public static Vector3[] ToVector3XZReversed(List<Vector2> points2D)
		{
			var points = new Vector3[points2D.Count];
			
			for (int i = 0, last = points2D.Count - 1; i <= last; i++)
			{
				points[last - i].x = points2D[i].x;
				points[last - i].z = points2D[i].y;
			}
			return points;
		}

		public static Vector3[] ToVector3XZ(Vector2[] points2D)
		{
			var points = new Vector3[points2D.Length];
			for (var i = 0; i < points2D.Length; i++)
			{
				points[i].x = points2D[i].x;
				points[i].z = points2D[i].y;
			}
			return points;
		}

		public static float CleanLength(float length)
		{
			var intLength	= Mathf.FloorToInt(length);

			var fractLength	= (length - intLength);

			fractLength = Mathf.Round(fractLength * 2048.0f) / 2048.0f;

			const float epsilon = MathConstants.EqualityEpsilon;

			if (fractLength >=    - epsilon && 
				fractLength <       epsilon) fractLength = 0;			
			if (fractLength >=  1 - epsilon) fractLength = 1;			
			if (fractLength <= -1 + epsilon) fractLength = -1;
			
			return intLength + fractLength;
			//return new Vector3(intPosX, intPosY, intPosZ);
		}

		public static bool IsNonConvex(List<Vector2> points)
		{
			for (int i = points.Count - 2, j = points.Count - 1, k = 0; k < points.Count; i = j, j = k, k++)
			{
				if (CounterClockwise(points[i],points[j],points[k]) < 0)
				{
					return true;
				}
			}
			return false;
		}

		public static CSGPlane CalcPolygonPlane(Vector3[] points)
		{
			if (points.Length == 3)
			{
				var v0 = points[0];
				var v1 = points[1];
				var v2 = points[2];

				return new CSGPlane(v0, v1, v2);
			}

			// newell's method to calculate a normal for a concave polygon
			var normal = MathConstants.zeroVector3;
			var prevIndex = points.Length - 1;
			var prevVertex = points[prevIndex];
			for (var e = 0; e < points.Length; e++)
			{
				var currVertex = points[e];
				normal.x = normal.x + ((prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z));
				normal.y = normal.y + ((prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x));
				normal.z = normal.z + ((prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y));

				prevVertex = currVertex;
			}
			normal = normal.normalized;

			var d = 0.0f;
			var count = 0;
			for (var e = 0; e < points.Length; e++)
			{
				var currVertex = points[e];
				d += Vector3.Dot(normal, currVertex);
				count++;
			}
			d /= count;

			return new CSGPlane(normal, d);
		}

		public static float CalcPolygonSign(Vector2[] points)
		{
			if (points.Length == 3)
			{
				var v0 = points[0];
				var v1 = points[1];
				var v2 = points[2];
				
				var ab = (v1 - v0);
				var ac = (v2 - v0);

				return (ab.x * ac.y - ab.y * ac.x);
			}
			
			var length = 0.0f;
			var prevIndex = points.Length - 1;
			var prevVertex = points[prevIndex];
			for (var e = 0; e < points.Length; e++)
			{
				var currVertex = points[e];
				length = length + ((prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y));
				prevVertex = currVertex;
			}
			return length;
		}



		public static CSGPlane CalcPolygonPlane(ControlMesh controlMesh, short polygonIndex)
		{
			var edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;
			if (edgeIndices.Length == 3)
			{
				var v0 = controlMesh.GetVertex(edgeIndices[0]);
				var v1 = controlMesh.GetVertex(edgeIndices[1]);
				var v2 = controlMesh.GetVertex(edgeIndices[2]);

				return new CSGPlane(v0, v1, v2);
			}

			// newell's method to calculate a normal for a concave polygon
			var normal = MathConstants.zeroVector3;
			var prevIndex = edgeIndices.Length - 1;
			if (prevIndex < 0)
			{
				return new CSGPlane(MathConstants.upVector3, MathConstants.zeroVector3);
			}

			var prevVertex = controlMesh.GetVertex(edgeIndices[prevIndex]);
			for (var e = 0; e < edgeIndices.Length; e++)
			{
				var currVertex = controlMesh.GetVertex(edgeIndices[e]);
				normal.x = normal.x + ((prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z));
				normal.y = normal.y + ((prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x));
				normal.z = normal.z + ((prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y));

				prevVertex = currVertex;
			}
			normal = normal.normalized;

			var d = 0.0f;
			var count = 0;
			for (var e = 0; e < edgeIndices.Length; e++)
			{
				var currVertex = controlMesh.GetVertex(edgeIndices[e]);
				d += Vector3.Dot(normal, currVertex);
				count++;
			}
			d /= count;

			return new CSGPlane(normal, d);
		}

		public static PrincipleAxis GetPrincipleAxis(Vector3 vector)
		{
			var absX = Mathf.Abs(vector.x);
			var absY = Mathf.Abs(vector.y);
			var absZ = Mathf.Abs(vector.z);
			if (absX > absY)
				return absX > absZ ? PrincipleAxis.X : PrincipleAxis.Z;
			return absY > absZ ? PrincipleAxis.Y : PrincipleAxis.Z;
		}

		public static Vector3 SnapToClosestAxis(Vector3 vector)
		{
			//Vector3 tangent;
			var absX = Mathf.Abs(vector.x);
			var absY = Mathf.Abs(vector.y);
			var absZ = Mathf.Abs(vector.z);
			if (absX > absY)
				return absX > absZ ? new Vector3(Mathf.Sign(vector.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(vector.z));
			return absY > absZ ? new Vector3(0, Mathf.Sign(vector.y), 0) : new Vector3(0, 0, Mathf.Sign(vector.z));
		}
		
		public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
		{
			// Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
			Quaternion q;
			q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
			q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
			q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
			q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
			q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
			q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
			q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
			return q;
		}

		public static Vector3 CalculateTangent(Vector3 vector)
		{
			var absX = Mathf.Abs(vector.x);
			var absY = Mathf.Abs(vector.y);
			var absZ = Mathf.Abs(vector.z);
			if (absX > absY && absX > absZ)
				return new Vector3(0, -1, 0);
			return absY > absZ ? new Vector3(0, 0, 1.0f) : new Vector3(0, -1, 0);
		}

		public static void CalculateTangents(Vector3 normal, out Vector3 tangent, out Vector3 binormal)
		{
			tangent		= Vector3.Cross(normal, GeometryUtility.CalculateTangent(normal)).normalized;
			binormal	= Vector3.Cross(normal, tangent).normalized;
		}

		public static Vector3 ProjectPointOnInfiniteLine(Vector3 point, Vector3 lineOrigin, Vector3 normalizedLineDirection)
		{
			var dot = Vector3.Dot(normalizedLineDirection, point - lineOrigin);
			return lineOrigin + normalizedLineDirection * dot;
		}
		
		public static CSGPlane InverseTransformPlane(Matrix4x4 inverseMatrix, CSGPlane plane) 
		{
			var dstMatrix = Matrix4x4.Transpose(inverseMatrix);
			var dstPlaneV = dstMatrix * new Vector4(plane.a, plane.b, plane.c, -plane.d);
			return new CSGPlane(dstPlaneV.x, dstPlaneV.y, dstPlaneV.z, -dstPlaneV.w);
		}
		
		public static CSGPlane TransformPlane(Matrix4x4 matrix, CSGPlane plane) 
		{
			var dstMatrix = Matrix4x4.Transpose(Matrix4x4.Inverse(matrix));
			var dstPlaneV = dstMatrix * new Vector4(plane.a, plane.b, plane.c, -plane.d);
			return new CSGPlane(dstPlaneV.x, dstPlaneV.y, dstPlaneV.z, -dstPlaneV.w);
		}

		public static Quaternion GetOrientationForPlane(CSGPlane plane)
		{
			var normal = plane.normal;
			var tangent = Vector3.Cross(normal, Vector3.Cross(normal, CalculateTangent(normal)).normalized).normalized;
			Quaternion q = Quaternion.LookRotation(tangent, normal);
			return q;
		}
		
		public static float DistancePointToCircle(Vector3 point, Vector3 circleCenter, float circleRadius)
		{
			var pointCenter  = HandleUtility.WorldToGUIPoint(point);
			var screenCenter = HandleUtility.WorldToGUIPoint(circleCenter);
			var cam			 = Camera.current;
			if (cam)
			{
				var screenEdge = HandleUtility.WorldToGUIPoint(circleCenter + cam.transform.right * circleRadius);
				circleRadius = (screenCenter - screenEdge).magnitude;
			}
			var dist = (screenCenter - pointCenter).magnitude;
			if (dist < circleRadius)
				return 0;
			return dist - circleRadius;
		}
		
		public static void MoveControlMeshVertices(CSGBrush[] brushes, Vector3 offset)
		{			
			for (int b = 0; b < brushes.Length; b++)
			{
				var controlMesh = brushes[b].ControlMesh;
				if (controlMesh == null ||
					controlMesh.Vertices == null)
					continue;

				var localOffset = brushes[b].transform.worldToLocalMatrix.MultiplyVector(offset);
				for (var p = 0; p < controlMesh.Vertices.Length; p++)
					controlMesh.Vertices[p] = controlMesh.Vertices[p] + localOffset;
			}
		}
		
		public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
		{
			Vector2 ret;

			// TODO: see if we can re-use calculations across list of points

			var t0 = t; // TODO: turn this into t * 0.5f?
			var t1 = t0 * t;
			var t2 = t1 * t;


			ret.x = 0.5f * // TODO: remove this?
					(
						(p1.x * 2.0f) + // TODO: change to 1.0f?
						(-p0.x + p2.x) * t0 +
						(2.0f * p0.x - 5.0f * p1.x + 4.0f * p2.x - p3.x) * t1 +
						(      -p0.x + 3.0f * p1.x - 3.0f * p2.x + p3.x) * t2
					);

			ret.y = 0.5f * // TODO: remove this?
					(
						(p1.y * 2.0f) + // TODO: change to 1.0f?
						(-p0.y + p2.y) * t0 +
						(2.0f * p0.y - 5.0f * p1.y + 4.0f * p2.y - p3.y) * t1 +
						(      -p0.y + 3.0f * p1.y - 3.0f * p2.y + p3.y) * t2
					);

			return ret;
		}

		
		
		/*
		public static bool ContinueTexGenFromSurfaceToSurface(CSGBrush srcBrush, int srcSurfaceIndex, CSGBrush dstBrush, int dstSurfaceIndex)
		{
			if (srcSurfaceIndex < 0 || srcSurfaceIndex >= srcBrush.Shape.Materials.Length ||
				srcBrush == null)
				return false;
			
			var src_brush_cache = CSGModelManager.GetBrushCache(srcBrush);
			if (src_brush_cache == null ||
				src_brush_cache.childData == null ||
				src_brush_cache.childData.modelTransform == null)
				return false;

			var dst_brush_cache = CSGModelManager.GetBrushCache(dstBrush);
			if (dst_brush_cache == null ||
				dst_brush_cache.childData == null ||
				dst_brush_cache.childData.modelTransform == null)
				return false;

			var dstPlane	= dstBrush.Shape.Surfaces[dstSurfaceIndex].Plane;
			var srcPlane	= srcBrush.Shape.Surfaces[srcSurfaceIndex].Plane;

			// convert planes into worldspace
			dstPlane = GeometryUtility.InverseTransformPlane(dstBrush.transform.worldToLocalMatrix, dstPlane);
			srcPlane = GeometryUtility.InverseTransformPlane(srcBrush.transform.worldToLocalMatrix, srcPlane);
				
			var dstNormal	= dstPlane.normal;
			var srcNormal	= srcPlane.normal;
				
			var srcTexGenIndex = srcBrush.Shape.Surfaces[srcSurfaceIndex].TexGenIndex;
			var dstTexGenIndex = dstBrush.Shape.Surfaces[dstSurfaceIndex].TexGenIndex;
				
			var scrShape = srcBrush.Shape;
			
			dstBrush.Shape.Materials[dstSurfaceIndex] = scrShape.Materials[srcSurfaceIndex];
			Vector3 srcPoint1, srcPoint2;
			Vector3 dstPoint1, dstPoint2;
			var edgeDirection = Vector3.Cross(dstNormal, srcNormal);
			var det = edgeDirection.sqrMagnitude;
			if (det < Constants.AlignmentTestEpsilon)
			{
				// Find 2 points on intersection of 2 planes
				srcPoint1 = srcPlane.pointOnPlane;
				srcPoint2 = GeometryUtility.ProjectPointOnPlane(srcPlane, srcPoint1 + MathConstants.oneVector3);

				dstPoint1 = GeometryUtility.ProjectPointOnPlane(dstPlane, srcPoint1);
				dstPoint2 = GeometryUtility.ProjectPointOnPlane(dstPlane, srcPoint2);
			} else
			{	
				// Find 2 points on intersection of 2 planes
				srcPoint1 = ((Vector3.Cross(edgeDirection, srcNormal) * -dstPlane.d) +
								(Vector3.Cross(dstNormal, edgeDirection) * -srcPlane.d)) / det;
				srcPoint2 = srcPoint1 + edgeDirection;
				dstPoint1 = srcPoint1;
				dstPoint2 = srcPoint2;
			}
				
			Vector3 srcLocalPoint1 = srcBrush.transform.InverseTransformPoint(srcPoint1);
			Vector3 srcLocalPoint2 = srcBrush.transform.InverseTransformPoint(srcPoint2);

			Vector3 dstLocalPoint1 = dstBrush.transform.InverseTransformPoint(dstPoint1);
			Vector3 dstLocalPoint2 = dstBrush.transform.InverseTransformPoint(dstPoint2);

			var srcShape	= srcBrush.Shape;
			var srcTexGens	= srcShape.TexGens;
			var srcSurfaces	= srcShape.Surfaces;
			var dstShape	= dstBrush.Shape;
			var dstTexGens	= dstShape.TexGens;
			var dstSurfaces	= dstShape.Surfaces;


			// Reset destination shape to simplify calculations
			dstTexGens[dstTexGenIndex].Scale			= scrShape.TexGens[srcTexGenIndex].Scale;
			dstTexGens[dstTexGenIndex].Translation		= MathConstants.zeroVector2;
			dstTexGens[dstTexGenIndex].RotationAngle	= 0;

			if (!CSGModelManager.AlignTextureSpacesInLocalSpace(ref srcTexGens[srcTexGenIndex], ref srcSurfaces[srcSurfaceIndex], 
																srcLocalPoint1, srcLocalPoint2,
																ref dstTexGens[dstTexGenIndex], ref dstSurfaces[dstSurfaceIndex], 
																dstLocalPoint1, dstLocalPoint2))
				return false;
			return true;			
		}
		*/
		
		public static bool ContinueTexGenFromSurfaceToSurface(CSGBrush brush, int srcSurfaceIndex, int dstSurfaceIndex)
		{
			if (srcSurfaceIndex < 0 || srcSurfaceIndex >= brush.Shape.Materials.Length ||
				!brush)
				return false;
						
			var shape		= brush.Shape;
			var texGens		= shape.TexGens;
			var texGenFlags = shape.TexGenFlags;
			var surfaces	= shape.Surfaces;

			var dstPlane	= surfaces[dstSurfaceIndex].Plane;
			var srcPlane	= surfaces[srcSurfaceIndex].Plane;

			// convert planes into worldspace
			dstPlane = InverseTransformPlane(brush.transform.worldToLocalMatrix, dstPlane);
			srcPlane = InverseTransformPlane(brush.transform.worldToLocalMatrix, srcPlane);
				
			var dstNormal	= dstPlane.normal;
			var srcNormal	= srcPlane.normal;
				
			var srcTexGenIndex = surfaces[srcSurfaceIndex].TexGenIndex;
			var dstTexGenIndex = surfaces[dstSurfaceIndex].TexGenIndex;
			
			shape.Materials[dstSurfaceIndex] = shape.Materials[srcSurfaceIndex];
			Vector3 srcPoint1, srcPoint2;
			Vector3 dstPoint1, dstPoint2;
			var edgeDirection = Vector3.Cross(dstNormal, srcNormal);
			var det = edgeDirection.sqrMagnitude;
			if (det < MathConstants.AlignmentTestEpsilon)
			{
				// Find 2 points on intersection of 2 planes
				srcPoint1 = srcPlane.pointOnPlane;
				srcPoint2 = GeometryUtility.ProjectPointOnPlane(srcPlane, srcPoint1 + MathConstants.oneVector3);

				dstPoint1 = GeometryUtility.ProjectPointOnPlane(dstPlane, srcPoint1);
				dstPoint2 = GeometryUtility.ProjectPointOnPlane(dstPlane, srcPoint2);
			} else
			{	
				// Find 2 points on intersection of 2 planes
				srcPoint1 = ((Vector3.Cross(edgeDirection, srcNormal) * -dstPlane.d) +
								(Vector3.Cross(dstNormal, edgeDirection) * -srcPlane.d)) / det;
				srcPoint2 = srcPoint1 + edgeDirection;
				dstPoint1 = srcPoint1;
				dstPoint2 = srcPoint2;
			}
				
			// Reset destination shape to simplify calculations
			texGens[dstTexGenIndex].Scale			= shape.TexGens[srcTexGenIndex].Scale;
			texGens[dstTexGenIndex].Translation		= MathConstants.zeroVector2;
			texGens[dstTexGenIndex].RotationAngle	= 0;

			return SurfaceUtility.AlignTextureSpaces(brush.transform, ref texGens[srcTexGenIndex], texGenFlags[srcTexGenIndex], ref surfaces[srcSurfaceIndex], srcPoint1, srcPoint2, false,
													 brush.transform, ref texGens[dstTexGenIndex], texGenFlags[dstTexGenIndex], ref surfaces[dstSurfaceIndex], dstPoint1, dstPoint2, false);
		}
		
	}
}