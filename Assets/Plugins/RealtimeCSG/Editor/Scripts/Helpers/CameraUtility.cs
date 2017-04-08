using System;
using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal sealed class Frustum
	{
		public readonly CSGPlane[] Planes = new CSGPlane[6];
	}

	internal static class CameraUtility
	{
		public static Frustum GetCameraSubFrustumScreen(Camera camera, Rect rect)
		{
			var oldMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;

			var min_x = rect.x;
			var max_x = rect.x + rect.width;
			var min_y = rect.y;
			var max_y = rect.y + rect.height;

			var o0 = new Vector2(min_x, min_y);
			var o1 = new Vector2(max_x, min_y);
			var o2 = new Vector2(max_x, max_y);
			var o3 = new Vector2(min_x, max_y);

			var r0 = camera.ScreenPointToRay(o0);
			var r1 = camera.ScreenPointToRay(o1);
			var r2 = camera.ScreenPointToRay(o2);
			var r3 = camera.ScreenPointToRay(o3);
			Handles.matrix = oldMatrix;

			var n0 = r0.origin;
			var n1 = r1.origin;
			var n2 = r2.origin;
			var n3 = r3.origin;

			var far = camera.farClipPlane;
			var f0 = n0 + (r0.direction * far);
			var f1 = n1 + (r1.direction * far);
			var f2 = n2 + (r2.direction * far);
			var f3 = n3 + (r3.direction * far);

			Frustum frustum = new Frustum();
			frustum.Planes[0] = new CSGPlane(n2, n1, f1); // right  +
			frustum.Planes[1] = new CSGPlane(f3, f0, n0); // left   -
			frustum.Planes[2] = new CSGPlane(n1, n0, f0); // top    -
			frustum.Planes[3] = new CSGPlane(n3, n2, f2); // bottom +
			frustum.Planes[4] = new CSGPlane(n0, n1, n2); // near   -
			frustum.Planes[5] = new CSGPlane(f2, f1, f0); // far    +
			return frustum;
		}
		public static Frustum GetCameraSubFrustumGUI(Camera camera, Rect rect)
		{
			var oldMatrix = Handles.matrix;
			Handles.matrix = MathConstants.identityMatrix;

			var min_x = rect.x;
			var max_x = rect.x + rect.width;
			var min_y = rect.y;
			var max_y = rect.y + rect.height;

			var o0 = new Vector2(min_x, min_y);
			var o1 = new Vector2(max_x, min_y);
			var o2 = new Vector2(max_x, max_y);
			var o3 = new Vector2(min_x, max_y);

			var r0 = HandleUtility.GUIPointToWorldRay(o0);
			var r1 = HandleUtility.GUIPointToWorldRay(o1);
			var r2 = HandleUtility.GUIPointToWorldRay(o2);
			var r3 = HandleUtility.GUIPointToWorldRay(o3);
			Handles.matrix = oldMatrix;

			var n0 = r0.origin;
			var n1 = r1.origin;
			var n2 = r2.origin;
			var n3 = r3.origin;

			var far = camera.farClipPlane;
			var f0 = n0 + (r0.direction * far);
			var f1 = n1 + (r1.direction * far);
			var f2 = n2 + (r2.direction * far);
			var f3 = n3 + (r3.direction * far);

			Frustum frustum = new Frustum();
			frustum.Planes[0] = new CSGPlane(n2, n1, f1); // right  +
			frustum.Planes[1] = new CSGPlane(f3, f0, n0); // left   -
			frustum.Planes[2] = new CSGPlane(n1, n0, f0); // top    -
			frustum.Planes[3] = new CSGPlane(n3, n2, f2); // bottom +
			frustum.Planes[4] = new CSGPlane(n0, n1, n2); // near   -
			frustum.Planes[5] = new CSGPlane(f2, f1, f0); // far    +
			return frustum;
		}

		public static Rect PointsToRect(Vector2 start, Vector2 end)
		{
			start.x = Mathf.Max(start.x, 0);
			start.y = Mathf.Max(start.y, 0);
			end.x = Mathf.Max(end.x, 0);
			end.y = Mathf.Max(end.y, 0);
			Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
			if (r.width < 0)
			{
				r.x += r.width;
				r.width = -r.width;
			}
			if (r.height < 0)
			{
				r.y += r.height;
				r.height = -r.height;
			}
			return r;
		}
	}
}
