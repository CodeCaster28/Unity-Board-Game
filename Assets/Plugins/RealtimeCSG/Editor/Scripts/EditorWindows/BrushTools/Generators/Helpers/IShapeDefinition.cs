using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
	internal interface IShapeSettings
	{
		void CalculatePlane(ref CSGPlane plane);
		Vector3 GetCenter(Vector3 gridTangent, Vector3 gridBinormal);
		RealtimeCSG.AABB CalculateBounds(Vector3 gridTangent, Vector3 gridBinormal);
	}
}
