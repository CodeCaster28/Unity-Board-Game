using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
	[Serializable]
	internal enum PlaneSideResult : byte
	{
		Intersects,	// On plane
		Inside,		// Negative side of plane / 'inside' half-space
		Outside     // Positive side of plane / 'outside' half-space
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
	public struct CSGPlane
	{
		public float a;
		public float b;
		public float c;
		public float d;
		
		public Vector4 vector { get { return new Vector4(a, b, c, d); } set { a = value.x; b = value.y; c = value.z; d = value.w; } }

		public Vector3 normal
		{
			get { return new Vector3(a, b, c); }
			set { a = value.x; b = value.y; c = value.z; } 
		}
		
        public Vector3 pointOnPlane { get { return normal * d; } }

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3})", a,b,c,d);
		}

		#region Constructors
		//public Plane() { }

		public CSGPlane(CSGPlane inPlane)
		{
			a = inPlane.a;
			b = inPlane.b;
			c = inPlane.c;
			d = inPlane.d;
		}

		public CSGPlane(Vector3 inNormal, float inD)
		{
			a = inNormal.x;
			b = inNormal.y;
			c = inNormal.z;
			d = inD;
		}

		public CSGPlane(Vector3 inNormal, Vector3 pointOnPlane)
		{
			a = inNormal.x;
			b = inNormal.y;
			c = inNormal.z;
			d = Vector3.Dot(inNormal, pointOnPlane);
		}

		public CSGPlane(Quaternion inRotation, Vector3 pointOnPlane)
		{
			var normal	= inRotation * MathConstants.upVector3;
			a	= normal.x;
			b	= normal.y;
			c	= normal.z;
			d	= Vector3.Dot(normal, pointOnPlane);
		}

		public CSGPlane(float inA, float inB, float inC, float inD)
		{
			a = inA;
			b = inB;
			c = inC;
			d = inD;
		}

		public CSGPlane(Vector4 inVector)
		{
			a = inVector.x;
			b = inVector.y;
			c = inVector.z;
			d = inVector.w;
		}
	
		public CSGPlane(Vector3 point1, Vector3 point2, Vector3 point3)
		{
			var ab = (point2 - point1);
			var ac = (point3 - point1);

			var normal = Vector3.Cross(ab, ac).normalized;

			a = normal.x;
			b = normal.y;
			c = normal.z;
			d = Vector3.Dot(normal, point1);
		}

		#endregion

		#region Ray Intersection
		public Vector3 Intersection(UnityEngine.Ray ray)
		{
			var start_x			= (double)ray.origin.x;
			var start_y			= (double)ray.origin.y;
			var start_z			= (double)ray.origin.z;

			var direction_x		= (double)ray.direction.x;
			var direction_y		= (double)ray.direction.y;
			var direction_z		= (double)ray.direction.z;

			var distanceA	= (a * start_x) +
							  (b * start_y) +
							  (c * start_z) -
							  (d);
			var length		= (a * direction_x) +
							  (b * direction_y) +
							  (c * direction_z);
			var delta		= distanceA / length;

			var x = start_x - (delta * direction_x);
			var y = start_y - (delta * direction_y);
			var z = start_z - (delta * direction_z);

			return new Vector3((float)x, (float)y, (float)z);
		}
		
		public bool TryIntersection(UnityEngine.Ray ray, out Vector3 intersection)
		{
			var start		= ray.origin;
			var end			= ray.origin + ray.direction * 1000.0f;
			var distanceA	= Distance(start);
			if (float.IsInfinity(distanceA) || float.IsNaN(distanceA))
			{
				intersection = MathConstants.zeroVector3;
				return false;
			}
			var distanceB	= Distance(end);
			if (float.IsInfinity(distanceB) || float.IsNaN(distanceB))
			{
				intersection = MathConstants.zeroVector3;
				return false;
			}
			intersection = Intersection(start, end, distanceA, distanceB);
			if (float.IsInfinity(intersection.x) || float.IsNaN(intersection.x) ||
				float.IsInfinity(intersection.y) || float.IsNaN(intersection.y) ||
				float.IsInfinity(intersection.z) || float.IsNaN(intersection.z))
			{
				intersection = MathConstants.zeroVector3;
				return false;
			}
			return true;
		}

		public static Vector3 Intersection(Vector3 start, Vector3 end, float sdist, float edist)
		{
			Vector3 vector	= end - start;
			float length	= edist - sdist;
			float delta		= edist / length;

			return end - (delta * vector);
		}

		public Vector3 Intersection(Vector3 start, Vector3 end)
		{
			return Intersection(start, end, Distance(start), Distance(end));
		}


		static public Vector3 Intersection(CSGPlane inPlane1,
										   CSGPlane inPlane2,
										   CSGPlane inPlane3)
		{
			// intersection point with 3 planes
			//  {
			//      x = -( c2*b1*d3-c2*b3*d1+b3*c1*d2+c3*b2*d1-b1*c3*d2-c1*b2*d3)/
			//           (-c2*b3*a1+c3*b2*a1-b1*c3*a2-c1*b2*a3+b3*c1*a2+c2*b1*a3), 
			//      y =  ( c3*a2*d1-c3*a1*d2-c2*a3*d1+d2*c1*a3-a2*c1*d3+c2*d3*a1)/
			//           (-c2*b3*a1+c3*b2*a1-b1*c3*a2-c1*b2*a3+b3*c1*a2+c2*b1*a3), 
			//      z = -(-a2*b1*d3+a2*b3*d1-a3*b2*d1+d3*b2*a1-d2*b3*a1+d2*b1*a3)/
			//           (-c2*b3*a1+c3*b2*a1-b1*c3*a2-c1*b2*a3+b3*c1*a2+c2*b1*a3)
			//  }
			
			double bc1 = (inPlane1.b * inPlane3.c) - (inPlane3.b * inPlane1.c);
			double bc2 = (inPlane2.b * inPlane1.c) - (inPlane1.b * inPlane2.c);
			double bc3 = (inPlane3.b * inPlane2.c) - (inPlane2.b * inPlane3.c);

			double ad1 = (inPlane1.a * inPlane3.d) - (inPlane3.a * inPlane1.d);
			double ad2 = (inPlane2.a * inPlane1.d) - (inPlane1.a * inPlane2.d);
			double ad3 = (inPlane3.a * inPlane2.d) - (inPlane2.a * inPlane3.d);

			double x = -((inPlane1.d * bc3) + (inPlane2.d * bc1) + (inPlane3.d * bc2));
			double y = -((inPlane1.c * ad3) + (inPlane2.c * ad1) + (inPlane3.c * ad2));
			double z = +((inPlane1.b * ad3) + (inPlane2.b * ad1) + (inPlane3.b * ad2));
			double w = -((inPlane1.a * bc3) + (inPlane2.a * bc1) + (inPlane3.a * bc2));

			// better to have detectable invalid values than to have reaaaaaaally big values
			if (w > -MathConstants.DistanceEpsilon && 
				w <  MathConstants.DistanceEpsilon)
			{
				return new Vector3( Single.NaN,
									Single.NaN,
									Single.NaN);
			} else
				return new Vector3( (float)(x / w),
									(float)(y / w),
									(float)(z / w));
		}
		#endregion

		#region Distance
		public float Distance(float x, float y, float z)
		{
			return
				(
					(a * x) +
					(b * y) +
					(c * z) -
					(d)
				);
		}

		public float Distance(Vector3 vertex)
		{
			return
				(
					(a * vertex.x) +
					(b * vertex.y) +
					(c * vertex.z) -
					(d)
				);
		}
		#endregion

		#region Normalize
		public void Normalize()
		{
			var magnitude = 1.0f / Mathf.Sqrt((a * a) + (b * b) + (c * c));
			a *= magnitude;
			b *= magnitude;
			c *= magnitude;
			d *= magnitude;
		}
		#endregion

		#region Plane Negation
		public CSGPlane Negated()
		{
			return new CSGPlane(-a, -b, -c, -d);
		}
		public void Negate()
		{
			a = -a;
			b = -b;
			c = -c;
			d = -d;
		}
		#endregion

		#region Plane Transform
		public void Transform(Matrix4x4 transformation)
		{
			var ittrans = transformation.inverse.transpose;
			var vector = ittrans * new Vector4(this.a, this.b, this.c, -this.d);
			this.a =  vector.x;
			this.b =  vector.y;
			this.c =  vector.z;
			this.d = -vector.w;
		}

		public void InverseTransform(Matrix4x4 transformation)
		{
			var ttrans = transformation.transpose;
			var vector = ttrans * new Vector4(this.a, this.b, this.c, -this.d);
			this.a =  vector.x;
			this.b =  vector.y;
			this.c =  vector.z;
			this.d = -vector.w;
		}

		public static CSGPlane Transformed(CSGPlane plane, Matrix4x4 transformation)
		{
			var ittrans = transformation.inverse.transpose;
			var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
			return new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
		}

		public static CSGPlane InverseTransformed(CSGPlane plane, Matrix4x4 transformation)
		{
			var ttrans = transformation.transpose;
			var vector = ttrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
			return new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
		}

		public static void Transform(List<CSGPlane> src, Matrix4x4 transformation, out CSGPlane[] dst)
		{
			var ittrans = transformation.inverse.transpose;
			dst = new CSGPlane[src.Count];
			for (int i = 0; i < src.Count; i++)
			{
				var plane = src[i];
				var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
				dst[i] = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
			}
		}

		public static void Transform(CSGPlane[] srcPlanes, Matrix4x4 transformation, out CSGPlane[] dstPlanes)
		{
			var ittrans = transformation.inverse.transpose;
			dstPlanes = new CSGPlane[srcPlanes.Length];
			for (int i = 0; i < srcPlanes.Length; i++)
			{
				var plane  = srcPlanes[i];
				var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
				dstPlanes[i] = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
			}
		}
		public static void Transform(CSGPlane[] planes, Matrix4x4 transformation)
		{
			var ittrans = transformation.inverse.transpose;
			for (int i = 0; i < planes.Length; i++)
			{
				var plane = planes[i];
				var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
				planes[i] = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
			}
		}

		public static void Transform(CSGPlane[] srcPlanes, Vector3[] srcTangents, Matrix4x4 transformation, out CSGPlane[] dstPlanes, out Vector3[] dstTangents)
		{
			var itrans	= transformation.inverse;
			var ittrans = itrans.transpose;
			dstPlanes   = new CSGPlane[srcPlanes.Length];
			for (int i = 0; i < srcPlanes.Length; i++)
			{
				var plane = srcPlanes[i];
				var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
				dstPlanes[i] = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
			}

			dstTangents = new Vector3[srcTangents.Length];
			for (int i = 0; i < srcTangents.Length; i++)
			{
				var tangent = srcTangents[i];
				var vector = ittrans * new Vector4(tangent.x, tangent.y, tangent.z, 1);
				dstTangents[i] = (new Vector3(vector.x / vector.w, vector.y / vector.w, vector.z / vector.w)).normalized;
			}
		}


		public static CSGPlane InverseTransform(CSGPlane srcPlane, Matrix4x4 transformation)
		{
			var ittrans = transformation.transpose;
			var vector = ittrans * new Vector4(srcPlane.a, srcPlane.b, srcPlane.c, -srcPlane.d);
			var dstPlane = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
			dstPlane.Normalize();
			return dstPlane;
		}

		public static void InverseTransform(CSGPlane[] srcPlanes, Matrix4x4 transformation, out CSGPlane[] dstPlanes)
		{
			var ittrans = transformation.transpose;
			dstPlanes = new CSGPlane[srcPlanes.Length];
			for (int i = 0; i < srcPlanes.Length; i++)
			{
				var plane = srcPlanes[i];
				var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
				dstPlanes[i] = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
				dstPlanes[i].Normalize();
			}
		}

		public static void InverseTransform(List<CSGPlane> srcPlanes, Matrix4x4 transformation, out CSGPlane[] dstPlanes)
		{
			var ittrans = transformation.transpose;
			dstPlanes = new CSGPlane[srcPlanes.Count];
			for (int i = 0; i < srcPlanes.Count; i++)
			{
				var plane = srcPlanes[i];
				var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
				dstPlanes[i] = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
				dstPlanes[i].Normalize();
			}
		}

		public static void InverseTransform(CSGPlane[] srcPlanes, Vector3[] srcTangents, Matrix4x4 transformation, out CSGPlane[] dstPlanes, out Vector3[] dstTangents)
		{
			var ittrans = transformation.transpose;
			dstPlanes = new CSGPlane[srcPlanes.Length];
			for (int i = 0; i < srcPlanes.Length; i++)
			{
				var plane = srcPlanes[i];
				var vector = ittrans * new Vector4(plane.a, plane.b, plane.c, -plane.d);
				dstPlanes[i] = new CSGPlane(vector.x, vector.y, vector.z, -vector.w);
			}

			dstTangents = new Vector3[srcTangents.Length];
			for (int i = 0; i < srcTangents.Length; i++)
			{
				var tangent = srcTangents[i];
				var vector = ittrans * new Vector4(tangent.x, tangent.y, tangent.z, 1);
				dstTangents[i] = (new Vector3(vector.x / vector.w, vector.y / vector.w, vector.z / vector.w)).normalized;
			}
		}
		#endregion

		#region Plane Translation
		public static CSGPlane Translated(CSGPlane plane, Vector3 translation)
		{
			return new CSGPlane(plane.a, plane.b, plane.c,
				// translated offset = plane.Normal.Dotproduct(translation)
				// normal = A,B,C
								plane.d + (plane.a * translation.x) +
										  (plane.b * translation.y) +
										  (plane.c * translation.z));
		}

		public static CSGPlane Translated(CSGPlane plane, float translateX, float translateY, float translateZ)
		{
			return new CSGPlane(plane.a, plane.b, plane.c,
				// translated offset = plane.Normal.Dotproduct(translation)
				// normal = A,B,C
								plane.d + (plane.a * translateX) +
										  (plane.b * translateY) +
										  (plane.c * translateZ));
		}

		public CSGPlane Translated(Vector3 translation)
		{
			return new CSGPlane(a, b, c,
				// translated offset = Normal.Dotproduct(translation)
				// normal = A,B,C
								d + (a * translation.x) +
									(b * translation.y) +
									(c * translation.z));
		}

		public void Translate(Vector3 translation)
		{
			// translated offset = plane.Normal.Dotproduct(translation)
			// normal = A,B,C
			d += (a * translation.x) +
				 (b * translation.y) +
				 (c * translation.z);
		}

		public static void Translate(List<CSGPlane> src, Vector3 translation, out CSGPlane[] dst)
		{
			dst = new CSGPlane[src.Count];
			for (int i = 0; i < src.Count; i++)
			{
				var plane = src[i];
				dst[i] = new CSGPlane(plane.a, plane.b, plane.c,
								   plane.d + (plane.a * translation.x) +
											 (plane.b * translation.y) +
											 (plane.c * translation.z));
			}
		}

		public static void Translate(CSGPlane[] src, Vector3 translation, out CSGPlane[] dst)
		{
			dst = new CSGPlane[src.Length];
			for (int i = 0; i < src.Length; i++)
			{
				var plane = src[i];
				dst[i] = new CSGPlane(plane.a, plane.b, plane.c,
								   plane.d + (plane.a * translation.x) +
											 (plane.b * translation.y) +
											 (plane.c * translation.z));
			}
		}
		#endregion

		#region Plane comparisons
		public override int GetHashCode()
		{
			return a.GetHashCode() ^
					b.GetHashCode() ^
					c.GetHashCode() ^
					d.GetHashCode();
		}

		public bool Equals(CSGPlane other)
		{
			if (System.Object.ReferenceEquals(this, other))
				return true;
			if (System.Object.ReferenceEquals(other, null))
				return false;
			return	Mathf.Abs(this.Distance(other.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
					Mathf.Abs(other.Distance(this.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
					Mathf.Abs(a - other.a) <= MathConstants.NormalEpsilon &&
					Mathf.Abs(b - other.b) <= MathConstants.NormalEpsilon &&
					Mathf.Abs(c - other.c) <= MathConstants.NormalEpsilon;
		}

		public override bool Equals(object obj)
		{
			if (System.Object.ReferenceEquals(this, obj))
				return true;
			if (!(obj is CSGPlane))
				return false;
			CSGPlane other = (CSGPlane)obj;
			if (System.Object.ReferenceEquals(other, null))
				return false;
			return	Mathf.Abs(this.Distance(other.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
					Mathf.Abs(other.Distance(this.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
					Mathf.Abs(a - other.a) <= MathConstants.NormalEpsilon &&
					Mathf.Abs(b - other.b) <= MathConstants.NormalEpsilon &&
					Mathf.Abs(c - other.c) <= MathConstants.NormalEpsilon;
		}

		public static bool operator ==(CSGPlane left, CSGPlane right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return true;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return false;
			return	Mathf.Abs(left.Distance(right.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
					Mathf.Abs(right.Distance(left.pointOnPlane)) <= MathConstants.DistanceEpsilon &&
                    Mathf.Abs(left.a - right.a) <= MathConstants.NormalEpsilon &&
					Mathf.Abs(left.b - right.b) <= MathConstants.NormalEpsilon &&
					Mathf.Abs(left.c - right.c) <= MathConstants.NormalEpsilon;
		}

		public static bool operator !=(CSGPlane left, CSGPlane right)
		{
			if (System.Object.ReferenceEquals(left, right))
				return false;
			if (System.Object.ReferenceEquals(left, null) ||
				System.Object.ReferenceEquals(right, null))
				return true;
			return	Mathf.Abs(left.Distance(right.pointOnPlane)) > MathConstants.DistanceEpsilon &&
					Mathf.Abs(right.Distance(left.pointOnPlane)) > MathConstants.DistanceEpsilon &&
					Mathf.Abs(left.a - right.a) > MathConstants.NormalEpsilon ||
					Mathf.Abs(left.b - right.b) > MathConstants.NormalEpsilon ||
					Mathf.Abs(left.c - right.c) > MathConstants.NormalEpsilon;
		}
		#endregion

		#region Project
		/*
		public Vector3 Project(Vector3 point)
		{
			var t = (Vector3.Dot(normal, point) - d) / Vector3.Dot(normal, normal);
			return point - (t * normal);
		}
		*/

		public Vector3 Project(Vector3 point)
		{
			float px = point.x;
			float py = point.y;
			float pz = point.z;

			float nx = normal.x;
			float ny = normal.y;
			float nz = normal.z;

			float ax  = (px - (nx * d)) * nx;
			float ay  = (py - (ny * d)) * ny;
			float az  = (pz - (nz * d)) * nz;
			float dot = ax + ay + az;

			float rx = px - (dot * nx);
			float ry = py - (dot * ny);
			float rz = pz - (dot * nz);

			return new Vector3(rx, ry, rz);
        }
		#endregion
	}
}
