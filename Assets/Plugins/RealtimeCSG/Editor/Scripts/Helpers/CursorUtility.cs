using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
	internal static class CursorUtility
	{
		static readonly MouseCursor[] segmentCursors = new MouseCursor[]
		{
			MouseCursor.ResizeVertical,
			MouseCursor.ResizeUpRight,
			MouseCursor.ResizeHorizontal,
			MouseCursor.ResizeUpLeft,
			MouseCursor.ResizeVertical,
			MouseCursor.ResizeUpRight,
			MouseCursor.ResizeHorizontal,
			MouseCursor.ResizeUpLeft
		};
		public static MouseCursor GetCursorForDirection(Matrix4x4 matrix, Vector3 center, Vector3 direction, float angleOffset = 0)
		{
			var worldCenterPoint1 = matrix.MultiplyPoint(center);
			var worldCenterPoint2 = worldCenterPoint1 +
									matrix.MultiplyVector(direction * 10.0f);
			Vector2 gui_point1 = HandleUtility.WorldToGUIPoint(worldCenterPoint1);
			Vector2 gui_point2 = HandleUtility.WorldToGUIPoint(worldCenterPoint2);
			Vector2 delta = (gui_point2 - gui_point1).normalized;

			return GetCursorForDirection(delta, angleOffset);
		}

		public static MouseCursor GetCursorForDirection(Vector2 direction, float angleOffset = 0)
		{
			const float segment_angle = 360 / 8.0f;
			var angle = (360 + (GeometryUtility.SignedAngle(MathConstants.upVector2, direction) + 180 + angleOffset)) % 360;// (Vector2.Angle(MathConstants.upVector2, direction) / 8) - (180 / 8);
			var segment = Mathf.FloorToInt(((angle / segment_angle) + 0.5f) % 8.0f);

			return segmentCursors[segment];
		}

		public static MouseCursor GetCursorForEdge(Vector2 direction)
		{
			const float segment_angle = 360 / 8.0f;
			var angle = (360 + (GeometryUtility.SignedAngle(MathConstants.upVector2, direction) + 180)) % 360;// (Vector2.Angle(MathConstants.upVector2, direction) / 8) - (180 / 8);
			var segment = Mathf.FloorToInt(((angle / segment_angle) + 2.5f) % 8.0f);

			return segmentCursors[segment];
		}
		

		public static MouseCursor GetToolCursor()
		{
			switch (Tools.current)
			{
				case Tool.Move:		return MouseCursor.MoveArrow;
				case Tool.Rotate:	return MouseCursor.RotateArrow;
				case Tool.Scale:	return MouseCursor.ScaleArrow;
				case Tool.Rect:		return MouseCursor.SlideArrow;
				case Tool.View:		return MouseCursor.Orbit;
				default:			return MouseCursor.Arrow;
			}
		}
	}
}
