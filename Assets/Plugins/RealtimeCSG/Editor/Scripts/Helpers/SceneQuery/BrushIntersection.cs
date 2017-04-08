#if UNITY_EDITOR
using System;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	[Serializable]
    internal sealed class BrushIntersection
	{
		public Int32		brushID;
		public int			surfaceIndex;
		public int			texGenIndex;

		public GameObject	gameObject;
		public CSGBrush     brush;
		public CSGModel     model;
		
		public CSGPlane		plane;
		public bool         surfaceInverted;

		public Vector2		surfaceIntersection;
		public Vector3		worldIntersection;
		public Vector3[]	triangle;
		public float        distance;
	};
}
#endif