using System;
using UnityEngine;

namespace InternalRealtimeCSG
{
#if UNITY_EDITOR
	[System.Serializable]
	public enum BrushFlags
	{
		None = 0,
		InfiniteBrush = 1 // used to create inverted world
	}

	public enum PrefabInstantiateBehaviour
	{
		Reference,
		Copy
	}

#endif
}
