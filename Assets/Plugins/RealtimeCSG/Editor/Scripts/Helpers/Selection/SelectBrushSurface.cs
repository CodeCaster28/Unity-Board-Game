using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
	internal sealed class SelectedBrushSurface
	{
		public SelectedBrushSurface(CSGBrush _brush, int _surfaceIndex, bool _surfaceInverted = false)
		{
			brush = _brush; surfaceIndex = _surfaceIndex; surfaceInverted = _surfaceInverted;
		}
		public CSGBrush brush;
		public int      surfaceIndex;
		public bool     surfaceInverted;
	}
}
